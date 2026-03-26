using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

/// <summary>
/// Dynamic fleet population: periodically evaluates faction fleet counts vs target,
/// spawns replacement fleets at prosperous nodes using local resources.
/// Called from SimKernel.Step() at low frequency.
/// </summary>
public static class FleetPopulationSystem
{
    // STRUCTURAL: evaluation cadence constants.
    private const int STRUCT_POPULATION_EVAL_INTERVAL = 500;
    private const int STRUCT_TARGET_RATIO_NUM = 8;   // 80% threshold
    private const int STRUCT_TARGET_RATIO_DEN = 10;

    private sealed class Scratch
    {
        public readonly Dictionary<string, int> FactionCounts = new(StringComparer.Ordinal);
        public readonly List<string> FleetKeys = new();
        public readonly HashSet<string> FactionIdSet = new(StringComparer.Ordinal);
        public readonly List<string> FactionIds = new();
        public readonly Dictionary<string, int> FactionNodeCounts = new(StringComparer.Ordinal);
        public readonly List<string> NodeKeys = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();

    public static void Process(SimState state)
    {
        if (state.Tick % STRUCT_POPULATION_EVAL_INTERVAL != 0) return;
        if (state.Tick < STRUCT_POPULATION_EVAL_INTERVAL) return;

        var scratch = s_scratch.GetOrCreateValue(state);

        // Count active fleets per faction (deterministic iteration).
        var factionCounts = scratch.FactionCounts;
        factionCounts.Clear();
        var fleetKeys = scratch.FleetKeys;
        fleetKeys.Clear();
        foreach (var k in state.Fleets.Keys) fleetKeys.Add(k);
        fleetKeys.Sort(StringComparer.Ordinal);
        foreach (var fk in fleetKeys)
        {
            var fleet = state.Fleets[fk];
            if (fleet.OwnerId == "player") continue;
            if (fleet.IsStored) continue;
            factionCounts[fleet.OwnerId] = factionCounts.GetValueOrDefault(fleet.OwnerId) + 1;
        }

        // Pre-compute faction node counts in a single pass (avoids O(n²) inner loop).
        var factionNodeCounts = scratch.FactionNodeCounts;
        factionNodeCounts.Clear();
        foreach (var v in state.NodeFactionId.Values)
        {
            if (string.IsNullOrEmpty(v)) continue;
            factionNodeCounts[v] = factionNodeCounts.GetValueOrDefault(v) + 1;
        }

        // Evaluate each faction in deterministic order (Distinct via HashSet).
        var factionIdSet = scratch.FactionIdSet;
        factionIdSet.Clear();
        var factionIds = scratch.FactionIds;
        factionIds.Clear();
        foreach (var v in state.NodeFactionId.Values)
        {
            if (!string.IsNullOrEmpty(v) && factionIdSet.Add(v))
                factionIds.Add(v);
        }
        factionIds.Sort(StringComparer.Ordinal);

        foreach (var factionId in factionIds)
        {
            if (string.IsNullOrEmpty(factionId)) continue;

            int nodeCount = factionNodeCounts.GetValueOrDefault(factionId);

            var doctrine = FleetPopulationTweaksV0.GetComposition(factionId);
            int target = nodeCount * (doctrine.Traders + doctrine.Haulers + doctrine.Patrols);
            int current = factionCounts.GetValueOrDefault(factionId);

            if (current >= target * STRUCT_TARGET_RATIO_NUM / STRUCT_TARGET_RATIO_DEN) continue;

            // Find most prosperous faction node (highest total inventory).
            string? bestNode = null;
            int bestProsperity = -1;
            var nodeKeys = scratch.NodeKeys;
            nodeKeys.Clear();
            foreach (var k in state.NodeFactionId.Keys) nodeKeys.Add(k);
            nodeKeys.Sort(StringComparer.Ordinal);
            foreach (var nid in nodeKeys)
            {
                if (!string.Equals(state.NodeFactionId[nid], factionId, StringComparison.Ordinal)) continue;
                if (!state.Markets.TryGetValue(nid, out var mkt)) continue;

                int total = 0;
                foreach (var v in mkt.Inventory.Values) total += v;
                if (total > bestProsperity)
                {
                    bestProsperity = total;
                    bestNode = nid;
                }
            }
            if (bestNode == null) continue;

            // Check if station can afford replacement (metal + components).
            if (!state.Markets.TryGetValue(bestNode, out var market)) continue;
            int metalStock = market.Inventory.GetValueOrDefault(FleetPopulationTweaksV0.ReplacementGood1);
            int compStock = market.Inventory.GetValueOrDefault(FleetPopulationTweaksV0.ReplacementGood2);
            if (metalStock < FleetPopulationTweaksV0.ReplacementMetalCost
                || compStock < FleetPopulationTweaksV0.ReplacementComponentsCost) continue;

            // Deduct goods.
            market.Inventory[FleetPopulationTweaksV0.ReplacementGood1] =
                metalStock - FleetPopulationTweaksV0.ReplacementMetalCost;
            market.Inventory[FleetPopulationTweaksV0.ReplacementGood2] =
                compStock - FleetPopulationTweaksV0.ReplacementComponentsCost;

            // Spawn as Trader (most economically impactful role).
            // GATE.T59.SHIP.NPC_FACTION_FLEET.001: Use faction-appropriate ship variant.
            string fleetId = $"ai_fleet_{bestNode}_r{state.Tick}";
            var newFleet = new Fleet
            {
                Id = fleetId,
                OwnerId = factionId,
                Role = FleetRole.Trader,
                ShipClassId = FleetPopulationTweaksV0.PickShipClass(factionId, fleetId, FleetRole.Trader),
                CurrentNodeId = bestNode,
                Speed = FleetSeedTweaksV0.TraderSpeed,
                State = FleetState.Idle,
                FuelCapacity = NpcShipTweaksV0.DefaultFuelCapacity,
                FuelCurrent = NpcShipTweaksV0.DefaultFuelCapacity,
            };
            state.Fleets.Add(newFleet.Id, newFleet);

            // Max 1 replacement per eval cycle per faction.
            break;
        }
    }
}
