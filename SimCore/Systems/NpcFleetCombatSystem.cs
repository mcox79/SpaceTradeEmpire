using System;
using System.Collections.Generic;
using System.Linq;
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
        var destroyed = new List<string>();
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
                    DestructionTick = state.Tick
                });
            }

            state.Fleets.Remove(fleetId);

            // Record destruction for bridge observation.
            state.NpcFleetsDestroyedThisTick.Add(fleetId);
        }
    }

    private static void ProcessRespawn(SimState state)
    {
        if (state.NpcRespawnQueue.Count == 0) return;

        var cooldown = NpcShipTweaksV0.RespawnCooldownTicks;
        var respawned = new List<int>();

        for (int i = 0; i < state.NpcRespawnQueue.Count; i++)
        {
            var entry = state.NpcRespawnQueue[i];
            if (state.Tick - entry.DestructionTick < cooldown) continue;

            // Don't respawn if fleet ID already exists (shouldn't happen, but defensive).
            if (state.Fleets.ContainsKey(entry.FleetId)) { respawned.Add(i); continue; }

            // Respawn with same role derived from fleet ID (same deterministic hash as SeedAiFleets).
            uint roleHash = SimCore.Gen.GalaxyGenerator.Fnv1a32Utf8(entry.HomeNodeId + "_fleet_role");
            int bucket = (int)(roleHash % FleetSeedTweaksV0.BucketSize);
            FleetRole role;
            float speed;
            if (bucket < FleetSeedTweaksV0.TraderThreshold) { role = FleetRole.Trader; speed = FleetSeedTweaksV0.TraderSpeed; }
            else if (bucket < FleetSeedTweaksV0.HaulerThreshold) { role = FleetRole.Hauler; speed = FleetSeedTweaksV0.HaulerSpeed; }
            else { role = FleetRole.Patrol; speed = FleetSeedTweaksV0.PatrolSpeed; }

            var fleet = new Fleet
            {
                Id = entry.FleetId,
                OwnerId = "ai",
                Role = role,
                CurrentNodeId = entry.HomeNodeId,
                Speed = speed,
                State = FleetState.Idle,
                Supplies = NpcShipTweaksV0.RespawnSupplies
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
        var regime = ReputationSystem.ComputeTerritoryRegime(state, nodeId);
        int cargo = 0;
        if (state.Fleets.TryGetValue(playerFleetId, out var fleet))
            cargo = fleet.Cargo.Values.Sum();
        return GetPatrolResponse(regime, cargo);
    }
}
