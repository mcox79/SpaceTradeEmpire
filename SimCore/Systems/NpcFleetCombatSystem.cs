using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S7.TERRITORY.PATROL_RESPONSE.001: Patrol engagement response types.
public enum PatrolResponse { None, ScanWarning, Pursue, AttackOnSight }

/// <summary>
/// Processes NPC fleet destruction (HullHp == 0) and respawn after cooldown.
/// GATE.S16.NPC_ALIVE.FLEET_DESTROY.001
/// GATE.S16.NPC_ALIVE.FLEET_RESPAWN.001
/// </summary>
public static class NpcFleetCombatSystem
{
    private sealed class Scratch
    {
        public readonly List<string> DestroyedIds = new();
        public readonly List<int> RespawnedIndices = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();

    public static void Process(SimState state)
    {
        if (state is null) return;

        ProcessDestruction(state);
        ProcessRespawn(state);
    }

    private static void ProcessDestruction(SimState state)
    {
        // Collect destroyed NPC fleets (OwnerId != "player", HullHp == 0, HullHpMax > 0).
        // HullHpMax > 0 ensures we only check fleets that have been initialized for combat.
        var scratch = s_scratch.GetOrCreateValue(state);
        var destroyed = scratch.DestroyedIds;
        destroyed.Clear();
        foreach (var fleet in state.Fleets.Values)
        {
            if (string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal)) continue;
            if (fleet.HullHpMax <= 0) continue;
            if (fleet.HullHp > 0) continue;
            destroyed.Add(fleet.Id);
        }

        foreach (var fleetId in destroyed)
        {
            if (!state.Fleets.TryGetValue(fleetId, out var fleet)) continue;

            // Free edge capacity if fleet was traveling.
            if (fleet.State == FleetState.Traveling
                && !string.IsNullOrWhiteSpace(fleet.CurrentEdgeId)
                && state.Edges.TryGetValue(fleet.CurrentEdgeId, out var edge))
            {
                edge.UsedCapacity = Math.Max(0, edge.UsedCapacity - 1);
            }

            // Queue for respawn using the fleet's current (or last known) node.
            var homeNode = !string.IsNullOrWhiteSpace(fleet.CurrentNodeId)
                ? fleet.CurrentNodeId
                : fleet.DestinationNodeId ?? "";
            if (!string.IsNullOrWhiteSpace(homeNode))
            {
                state.NpcRespawnQueue.Add(new NpcRespawnEntry
                {
                    FleetId = fleetId,
                    HomeNodeId = homeNode,
                    DestructionTick = state.Tick,
                    Role = fleet.Role,
                    OwnerId = fleet.OwnerId ?? "ai",
                });
            }

            // GATE.S5.LOOT.DROP_SYSTEM.001 + GATE.T55.COMBAT.PIRATE_FACTION.001: Roll loot before removing fleet.
            // GATE.T56.FIX.PIRATE_LOOT_COLLECT.001: Place loot at the player's node, not the NPC's.
            // NPC homeNode may differ from the player's location during lane transit, causing
            // GetNearbyLootV0 to miss the drop (node mismatch).
            var lootNode = homeNode;
            if (state.Fleets.TryGetValue("fleet_trader_1", out var playerFleet)
                && !string.IsNullOrWhiteSpace(playerFleet.CurrentNodeId))
            {
                lootNode = playerFleet.CurrentNodeId;
            }
            // Pirates drop enhanced loot (salvaged_tech + rare_metals + credits).
            // GATE.T61.SALVAGE.LOOT_TABLE.001: Non-pirate fleets use role-based salvage loot.
            if (StringComparer.Ordinal.Equals(fleet.OwnerId, Tweaks.FactionTweaksV0.PirateId))
                LootTableSystem.RollPirateLoot(state, fleetId, lootNode);
            else
                LootTableSystem.RollSalvageLoot(state, fleet, lootNode);

            // GATE.T65.COMBAT.LOOT_WIRE.001: Auto-collect loot immediately after kill.
            // Loot was just placed at the player's node. Collect it so cargo updates instantly.
            var dropId = $"loot_{fleetId}_{state.Tick}";
            if (state.LootDrops.ContainsKey(dropId))
            {
                new Commands.CollectLootCommand(dropId).Execute(state);
            }

            // GATE.S7.DIPLOMACY.BOUNTY.001: Check bounty completion on NPC destruction.
            DiplomacySystem.CheckBountyCompletion(state, fleetId);

            state.Fleets.Remove(fleetId);

            // Record destruction for bridge observation.
            state.NpcFleetsDestroyedThisTick.Add(fleetId);

            // GATE.S19.ONBOARD.FO_TRIGGERS.003: Increment persistent kill counter.
            if (state.PlayerStats != null)
            {
                state.PlayerStats.NpcFleetsDestroyed++;
                // GATE.T64.FO.COMBAT_REACTION.001: Record tick for delayed FO combat reaction.
                state.PlayerStats.LastCombatWinTick = state.Tick;
            }
        }
    }

    private static void ProcessRespawn(SimState state)
    {
        if (state.NpcRespawnQueue.Count == 0) return;

        var cooldown = NpcShipTweaksV0.RespawnCooldownTicks;
        var scratch = s_scratch.GetOrCreateValue(state);
        var respawned = scratch.RespawnedIndices;
        respawned.Clear();

        for (int i = 0; i < state.NpcRespawnQueue.Count; i++)
        {
            var entry = state.NpcRespawnQueue[i];
            if (state.Tick - entry.DestructionTick < cooldown) continue;

            // Don't respawn if fleet ID already exists (shouldn't happen, but defensive).
            if (state.Fleets.ContainsKey(entry.FleetId)) { respawned.Add(i); continue; }

            // GATE.T30.GALPOP.RESPAWN_ENTRY.002: Use stored Role + OwnerId from respawn entry.
            float speed = entry.Role switch
            {
                FleetRole.Trader => FleetSeedTweaksV0.TraderSpeed,
                FleetRole.Hauler => FleetSeedTweaksV0.HaulerSpeed,
                FleetRole.Patrol => FleetSeedTweaksV0.PatrolSpeed,
                _ => FleetSeedTweaksV0.TraderSpeed,
            };

            var fleet = new Fleet
            {
                Id = entry.FleetId,
                OwnerId = entry.OwnerId,
                Role = entry.Role,
                CurrentNodeId = entry.HomeNodeId,
                Speed = speed,
                State = FleetState.Idle,
                FuelCapacity = NpcShipTweaksV0.DefaultFuelCapacity,
                FuelCurrent = NpcShipTweaksV0.DefaultFuelCapacity,
            };
            state.Fleets[fleet.Id] = fleet;
            respawned.Add(i);
        }

        // Remove respawned entries in reverse order to preserve indices.
        for (int i = respawned.Count - 1; i >= 0; i--)
        {
            state.NpcRespawnQueue.RemoveAt(respawned[i]);
        }
    }

    // GATE.S7.TERRITORY.PATROL_RESPONSE.001: Determine patrol engagement by territory regime.
    // Open=None, Guarded=ScanWarning, Restricted=Pursue if cargo>threshold, Hostile=AttackOnSight.
    public static PatrolResponse GetPatrolResponse(TerritoryRegime regime, int playerCargoVolume)
    {
        return regime switch
        {
            TerritoryRegime.Open => PatrolResponse.None,
            TerritoryRegime.Guarded => PatrolResponse.ScanWarning,
            TerritoryRegime.Restricted => playerCargoVolume > FactionTweaksV0.CargoThresholdForPursuit
                ? PatrolResponse.Pursue
                : PatrolResponse.ScanWarning,
            TerritoryRegime.Hostile => PatrolResponse.AttackOnSight,
            _ => PatrolResponse.None
        };
    }

    // Convenience: compute patrol response for a patrol fleet at a given node.
    public static PatrolResponse GetPatrolResponse(SimState state, string nodeId, string playerFleetId)
    {
        if (state is null) return PatrolResponse.None;
        var regime = ReputationSystem.GetEffectiveRegime(state, nodeId);
        int cargo = 0;
        if (state.Fleets.TryGetValue(playerFleetId, out var fleet))
            foreach (var v in fleet.Cargo.Values) cargo += v;
        return GetPatrolResponse(regime, cargo);
    }
}
