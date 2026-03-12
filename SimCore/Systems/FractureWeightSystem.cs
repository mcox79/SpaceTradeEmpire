using SimCore.Entities;
using SimCore.Tweaks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Systems;

// GATE.T18.NARRATIVE.FRACTURE_WEIGHT.001: Cargo loaded in unstable space weighs
// differently when brought to stable space. Dynamic ratios per instability phase
// prevent wiki-lookup optimization. Integer math, min 1 qty.
public static class FractureWeightSystem
{
    /// <summary>
    /// On fleet arrival at a stable-space node from fracture space, recalculate
    /// cargo quantities based on origin instability phase. Called after MovementSystem.
    /// </summary>
    public static void Process(SimState state)
    {
        if (state.ArrivalsThisTick.Count == 0) return;

        foreach (var (fleetId, edgeId, nodeId) in state.ArrivalsThisTick)
        {
            if (!state.Fleets.TryGetValue(fleetId, out var fleet)) continue;
            if (fleet.OwnerId != "player") continue; // only player fleet for now
            if (!state.Nodes.TryGetValue(nodeId, out var arrivalNode)) continue;

            // Only apply weight shift when arriving at stable space (Phase 0)
            if (arrivalNode.InstabilityLevel >= FractureWeightTweaksV0.STRUCT_PhaseShimmerMin) continue;

            // Check if any cargo has fracture-origin phase > 0
            if (fleet.CargoOriginPhase.Count == 0) continue;

            var goodsToAdjust = new List<(string GoodId, int OriginPhase)>();
            foreach (var kv in fleet.CargoOriginPhase)
            {
                if (kv.Value > 0 && fleet.Cargo.TryGetValue(kv.Key, out var qty) && qty > 0)
                {
                    goodsToAdjust.Add((kv.Key, kv.Value));
                }
            }

            foreach (var (goodId, originPhase) in goodsToAdjust)
            {
                int currentQty = fleet.GetCargoUnits(goodId);
                if (currentQty <= 0) continue;

                int weightBps = ComputeWeightBps(state, fleetId, goodId, originPhase);
                int adjustedQty = (int)((long)currentQty * weightBps / 10000);
                if (adjustedQty < 1) adjustedQty = 1; // min 1 unit

                fleet.Cargo[goodId] = adjustedQty;

                // Clear the origin phase — weight shift applied once
                fleet.CargoOriginPhase.Remove(goodId);
            }
        }
    }

    /// <summary>
    /// Compute the weight multiplier in basis points for a good loaded at a given
    /// instability phase. Deterministic from hash(fleetId, goodId, tick).
    /// Dynamic ratios shift per tick to prevent wiki-lookup.
    /// </summary>
    public static int ComputeWeightBps(SimState state, string fleetId, string goodId, int originPhase)
    {
        int minBps, maxBps;

        if (originPhase >= FractureWeightTweaksV0.STRUCT_PhaseFractureMin)
        {
            minBps = FractureWeightTweaksV0.Phase3MinWeightBps;
            maxBps = FractureWeightTweaksV0.Phase3MaxWeightBps;
        }
        else if (originPhase >= FractureWeightTweaksV0.STRUCT_PhaseDriftMin)
        {
            minBps = FractureWeightTweaksV0.Phase2MinWeightBps;
            maxBps = FractureWeightTweaksV0.Phase2MaxWeightBps;
        }
        else if (originPhase >= FractureWeightTweaksV0.STRUCT_PhaseShimmerMin)
        {
            minBps = FractureWeightTweaksV0.Phase1MinWeightBps;
            maxBps = FractureWeightTweaksV0.Phase1MaxWeightBps;
        }
        else
        {
            return FractureWeightTweaksV0.Phase0WeightBps; // stable = no change
        }

        // Deterministic hash for dynamic ratio
        int range = maxBps - minBps;
        if (range <= 0) return minBps;

        ulong h = DeterministicHash(fleetId, goodId, state.Tick);
        int offset = (int)(h % (ulong)range);
        return minBps + offset;
    }

    /// <summary>
    /// Record the instability phase of the current node into cargo origin when
    /// goods are loaded (bought). Called by trade commands.
    /// </summary>
    public static void RecordCargoOrigin(SimState state, Fleet fleet, string goodId)
    {
        if (!state.Nodes.TryGetValue(fleet.CurrentNodeId, out var node)) return;

        int phase = GetInstabilityPhase(node.InstabilityLevel);
        if (phase > 0)
        {
            fleet.CargoOriginPhase[goodId] = phase;
        }
    }

    /// <summary>
    /// Map instability level to phase index (0-4).
    /// </summary>
    public static int GetInstabilityPhase(int instabilityLevel)
    {
        if (instabilityLevel >= FractureWeightTweaksV0.STRUCT_PhaseVoidMin) return 4;
        if (instabilityLevel >= FractureWeightTweaksV0.STRUCT_PhaseFractureMin) return 3;
        if (instabilityLevel >= FractureWeightTweaksV0.STRUCT_PhaseDriftMin) return 2;
        if (instabilityLevel >= FractureWeightTweaksV0.STRUCT_PhaseShimmerMin) return 1;
        return 0;
    }

    // FNV-1a 64-bit hash for deterministic weight computation
    private static ulong DeterministicHash(string fleetId, string goodId, int tick)
    {
        ulong h = 14695981039346656037UL;
        foreach (char c in fleetId)
        {
            h ^= c;
            h *= 1099511628211UL;
        }
        h ^= (uint)tick;
        h *= 1099511628211UL;
        foreach (char c in goodId)
        {
            h ^= c;
            h *= 1099511628211UL;
        }
        return h;
    }
}
