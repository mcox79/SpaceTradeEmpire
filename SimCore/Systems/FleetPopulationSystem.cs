using System;
using System.Collections.Generic;
using System.Linq;
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

    public static void Process(SimState state)
    {
        if (state.Tick % STRUCT_POPULATION_EVAL_INTERVAL != 0) return;
        if (state.Tick < STRUCT_POPULATION_EVAL_INTERVAL) return;

        // Count active fleets per faction (deterministic iteration).
        var factionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var fleetKeys = state.Fleets.Keys.ToList();
        fleetKeys.Sort(StringComparer.Ordinal);
        foreach (var fk in fleetKeys)
        {
            var fleet = state.Fleets[fk];
            if (fleet.OwnerId == "player") continue;
            if (fleet.IsStored) continue;
            factionCounts[fleet.OwnerId] = factionCounts.GetValueOrDefault(fleet.OwnerId) + 1;
        }

        // Evaluate each faction in deterministic order.
        var factionIds = state.NodeFactionId.Values.Distinct().ToList();
        factionIds.Sort(StringComparer.Ordinal);

        foreach (var factionId in factionIds)
        {
            if (string.IsNullOrEmpty(factionId)) continue;

            int nodeCount = 0;
            foreach (var v in state.NodeFactionId.Values)
                if (string.Equals(v, factionId, StringComparison.Ordinal)) nodeCount++;

            var doctrine = FleetPopulationTweaksV0.GetComposition(factionId);
            int target = nodeCount * (doctrine.Traders + doctrine.Haulers + doctrine.Patrols);
            int current = factionCounts.GetValueOrDefault(factionId);

            if (current >= target * STRUCT_TARGET_RATIO_NUM / STRUCT_TARGET_RATIO_DEN) continue;

            // Find most prosperous faction node (highest total inventory).
            string? bestNode = null;
            int bestProsperity = -1;
            var nodeKeys = state.NodeFactionId.Keys.ToList();
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
            var newFleet = new Fleet
            {
                Id = $"ai_fleet_{bestNode}_r{state.Tick}",
                OwnerId = factionId,
                Role = FleetRole.Trader,
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
