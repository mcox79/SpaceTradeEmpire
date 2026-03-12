using SimCore.Entities;
using SimCore.Tweaks;
using System;

namespace SimCore.Systems;

// GATE.T18.NARRATIVE.ROUTE_UNCERTAINTY.001: Phase 2+ travel times vary
// deterministically. ETA displays as range. Fracture module exposure
// (FractureExposureJumps) narrows the range over time — the nav computer
// gets smarter because the player is getting smarter.
public static class RouteUncertaintySystem
{
    /// <summary>
    /// Compute the ETA range (min, max) in ticks for travel along an edge.
    /// In stable space, min == max (exact). In Phase 2+, returns a variance range
    /// that narrows with scanner adaptation (FractureExposureJumps).
    /// </summary>
    public static (int MinTicks, int MaxTicks) ComputeEtaRange(
        SimState state, string edgeId, string fleetId)
    {
        if (!state.Edges.TryGetValue(edgeId, out var edge)) return (1, 1);
        if (!state.Fleets.TryGetValue(fleetId, out var fleet)) return (1, 1);

        // Base travel time from edge distance and fleet speed
        float baseTicks = edge.Distance / Math.Max(fleet.Speed, 0.01f);
        int baseTicksInt = Math.Max(1, (int)Math.Ceiling(baseTicks));

        // Determine instability phase at the edge's destination
        int instabilityLevel = 0;
        if (state.Nodes.TryGetValue(edge.ToNodeId, out var destNode))
            instabilityLevel = destNode.InstabilityLevel;

        int variancePct = GetVariancePct(instabilityLevel);
        if (variancePct <= 0) return (baseTicksInt, baseTicksInt);

        // Apply scanner adaptation narrowing
        int retainedPct = GetRetainedPct(state.FractureExposureJumps);
        int effectiveVariancePct = variancePct * retainedPct / 100;

        // Enforce minimum variance in Phase 3+
        if (instabilityLevel >= FractureWeightTweaksV0.STRUCT_PhaseFractureMin &&
            effectiveVariancePct < RouteUncertaintyTweaksV0.MinVariancePct)
        {
            effectiveVariancePct = RouteUncertaintyTweaksV0.MinVariancePct;
        }

        if (effectiveVariancePct <= 0) return (baseTicksInt, baseTicksInt);

        int delta = Math.Max(1, baseTicksInt * effectiveVariancePct / 100);
        int minTicks = Math.Max(1, baseTicksInt - delta);
        int maxTicks = baseTicksInt + delta;

        return (minTicks, maxTicks);
    }

    /// <summary>
    /// Compute the deterministic actual travel time variation for an edge at a
    /// given tick. Applied by MovementSystem for Phase 2+ edges.
    /// </summary>
    public static int ComputeActualTravelTicks(
        SimState state, string edgeId, string fleetId)
    {
        var (minTicks, maxTicks) = ComputeEtaRange(state, edgeId, fleetId);
        if (minTicks == maxTicks) return minTicks;

        // Deterministic selection within the range
        int range = maxTicks - minTicks + 1;
        ulong h = DeterministicHash(edgeId, state.Tick);
        int offset = (int)(h % (ulong)range);
        return minTicks + offset;
    }

    /// <summary>
    /// Increment FractureExposureJumps when fleet completes a fracture jump.
    /// Called by MovementSystem on fracture travel completion.
    /// </summary>
    public static void RecordFractureJump(SimState state)
    {
        state.FractureExposureJumps++;
    }

    /// <summary>
    /// Get the scanner adaptation stage based on fracture jumps completed.
    /// Stage 1: default (wide range). Stage 2: weighted. Stage 3: near-exact.
    /// </summary>
    public static int GetAdaptationStage(int fractureJumps)
    {
        if (fractureJumps >= RouteUncertaintyTweaksV0.Stage3JumpsRequired) return 3;
        if (fractureJumps >= RouteUncertaintyTweaksV0.Stage2JumpsRequired) return 2;
        return 1;
    }

    private static int GetVariancePct(int instabilityLevel)
    {
        // Phase 4 (Void) uses Phase 4 variance
        if (instabilityLevel >= FractureWeightTweaksV0.STRUCT_PhaseVoidMin)
            return RouteUncertaintyTweaksV0.Phase4VariancePct;
        if (instabilityLevel >= FractureWeightTweaksV0.STRUCT_PhaseFractureMin)
            return RouteUncertaintyTweaksV0.Phase3VariancePct;
        if (instabilityLevel >= FractureWeightTweaksV0.STRUCT_PhaseDriftMin)
            return RouteUncertaintyTweaksV0.Phase2VariancePct;
        return 0; // Phase 0-1: no variance
    }

    private static int GetRetainedPct(int fractureJumps)
    {
        int stage = GetAdaptationStage(fractureJumps);
        return stage switch
        {
            3 => RouteUncertaintyTweaksV0.Stage3RetainedPct,
            2 => RouteUncertaintyTweaksV0.Stage2RetainedPct,
            _ => RouteUncertaintyTweaksV0.Stage1RetainedPct
        };
    }

    private static ulong DeterministicHash(string edgeId, int tick)
    {
        ulong h = 14695981039346656037UL;
        foreach (char c in edgeId)
        {
            h ^= c;
            h *= 1099511628211UL;
        }
        h ^= (uint)tick;
        h *= 1099511628211UL;
        return h;
    }
}
