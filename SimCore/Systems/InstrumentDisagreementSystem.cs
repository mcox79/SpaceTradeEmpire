using SimCore.Entities;
using SimCore.Tweaks;
using System;

namespace SimCore.Systems;

// GATE.T18.NARRATIVE.INSTRUMENT_DISAGREEMENT.001: Dual-readout system.
// Standard sensors and the fracture module disagree in unstable space.
// Neither is always right: standard is accurate for goods pricing,
// fracture module is accurate for navigation/timing.
// This creates a "which instrument do I trust?" decision surface.
// Pure query — no state mutation.
public static class InstrumentDisagreementSystem
{
    /// <summary>
    /// Compute the standard sensor reading for a good's price at a node.
    /// In stable space, this equals the market price exactly.
    /// In unstable space, it drifts from the true price — but is still
    /// MORE accurate than the fracture reading for pricing.
    /// </summary>
    public static int ComputeStandardPriceReading(SimState state, string nodeId, string goodId)
    {
        if (!state.Nodes.TryGetValue(nodeId, out var node)) return 0;
        if (!state.Markets.TryGetValue(nodeId, out var market)) return 0;

        int truePrice = market.GetPrice(goodId);
        if (truePrice <= 0) return 0;

        int instability = node.InstabilityLevel;
        if (instability < FractureWeightTweaksV0.STRUCT_PhaseShimmerMin)
            return truePrice; // stable = exact

        int driftBps = ComputeStandardDriftBps(instability);
        ulong h = DeterministicHash(nodeId, goodId, state.Tick, InstrumentDisagreementTweaksV0.STRUCT_SaltStandard);
        bool positive = (h % 2) == 0;
        int drift = (int)((long)truePrice * driftBps / 10000);

        return positive ? truePrice + drift : Math.Max(1, truePrice - drift);
    }

    /// <summary>
    /// Compute the fracture module reading for a good's price at a node.
    /// In unstable space, this diverges MORE from true price than standard
    /// sensors — the fracture module is tuned for navigation, not commerce.
    /// </summary>
    public static int ComputeFracturePriceReading(SimState state, string nodeId, string goodId)
    {
        if (!state.Nodes.TryGetValue(nodeId, out var node)) return 0;
        if (!state.Markets.TryGetValue(nodeId, out var market)) return 0;

        int truePrice = market.GetPrice(goodId);
        if (truePrice <= 0) return 0;

        int instability = node.InstabilityLevel;
        if (instability < FractureWeightTweaksV0.STRUCT_PhaseShimmerMin)
            return truePrice; // stable = exact

        int driftBps = ComputeFracturePriceDriftBps(instability);
        ulong h = DeterministicHash(nodeId, goodId, state.Tick, InstrumentDisagreementTweaksV0.STRUCT_SaltFracture);
        bool positive = (h % 2) == 0;
        int drift = (int)((long)truePrice * driftBps / 10000);

        return positive ? truePrice + drift : Math.Max(1, truePrice - drift);
    }

    /// <summary>
    /// Compute the standard sensor ETA estimate for an edge.
    /// In unstable space, standard sensors overestimate travel time
    /// (they assume worst-case because they can't read the fracture topology).
    /// </summary>
    public static int ComputeStandardEtaReading(SimState state, string edgeId)
    {
        if (!state.Edges.TryGetValue(edgeId, out var edge)) return 1;

        int instability = 0;
        if (state.Nodes.TryGetValue(edge.ToNodeId, out var destNode))
            instability = destNode.InstabilityLevel;

        // STRUCTURAL: 0.5f is the default fleet speed constant from Fleet entity
        float STRUCT_defaultSpeed = 0.5f;
        int baseTicks = Math.Max(1, (int)Math.Ceiling(edge.Distance / STRUCT_defaultSpeed));

        if (instability < FractureWeightTweaksV0.STRUCT_PhaseDriftMin)
            return baseTicks; // Phase 0-1: standard is accurate

        int overestimate = baseTicks * GetStandardEtaOverestimatePct(instability) / 100;
        return baseTicks + overestimate;
    }

    /// <summary>
    /// Compute the fracture module ETA estimate for an edge.
    /// In unstable space, the fracture module is MORE accurate for navigation
    /// — it reads the topology directly. This is the module's strength.
    /// </summary>
    public static int ComputeFractureEtaReading(SimState state, string edgeId)
    {
        if (!state.Edges.TryGetValue(edgeId, out var edge)) return 1;

        int instability = 0;
        if (state.Nodes.TryGetValue(edge.ToNodeId, out var destNode))
            instability = destNode.InstabilityLevel;

        // STRUCTURAL: 0.5f is the default fleet speed constant from Fleet entity
        float STRUCT_defaultSpeed = 0.5f;
        int baseTicks = Math.Max(1, (int)Math.Ceiling(edge.Distance / STRUCT_defaultSpeed));

        if (instability < FractureWeightTweaksV0.STRUCT_PhaseShimmerMin)
            return baseTicks; // stable = both agree

        // Fracture module has slight navigation drift in low-instability space
        ulong h = DeterministicHash(edgeId, "", state.Tick, InstrumentDisagreementTweaksV0.STRUCT_SaltNavigation);
        // STRUCTURAL: -1, 0, or +1 tick variation
        int STRUCT_driftRange = 3;
        int smallDrift = (int)(h % (ulong)STRUCT_driftRange) - 1;
        return Math.Max(1, baseTicks + smallDrift);
    }

    private static int ComputeStandardDriftBps(int instability)
    {
        if (instability >= FractureWeightTweaksV0.STRUCT_PhaseFractureMin)
            return InstrumentDisagreementTweaksV0.StandardDriftBpsPhase3;
        if (instability >= FractureWeightTweaksV0.STRUCT_PhaseDriftMin)
            return InstrumentDisagreementTweaksV0.StandardDriftBpsPhase2;
        if (instability >= FractureWeightTweaksV0.STRUCT_PhaseShimmerMin)
            return InstrumentDisagreementTweaksV0.StandardDriftBpsPhase1;
        return 0;
    }

    private static int ComputeFracturePriceDriftBps(int instability)
    {
        if (instability >= FractureWeightTweaksV0.STRUCT_PhaseFractureMin)
            return InstrumentDisagreementTweaksV0.FractureDriftBpsPhase3;
        if (instability >= FractureWeightTweaksV0.STRUCT_PhaseDriftMin)
            return InstrumentDisagreementTweaksV0.FractureDriftBpsPhase2;
        if (instability >= FractureWeightTweaksV0.STRUCT_PhaseShimmerMin)
            return InstrumentDisagreementTweaksV0.FractureDriftBpsPhase1;
        return 0;
    }

    private static int GetStandardEtaOverestimatePct(int instability)
    {
        if (instability >= FractureWeightTweaksV0.STRUCT_PhaseFractureMin)
            return InstrumentDisagreementTweaksV0.StandardEtaOverestimatePctPhase3;
        if (instability >= FractureWeightTweaksV0.STRUCT_PhaseDriftMin)
            return InstrumentDisagreementTweaksV0.StandardEtaOverestimatePctPhase2;
        return InstrumentDisagreementTweaksV0.StandardEtaOverestimatePctPhase1;
    }

    private static ulong DeterministicHash(string id1, string id2, int tick, uint salt)
    {
        ulong h = 14695981039346656037UL;
        foreach (char c in id1)
        {
            h ^= c;
            h *= 1099511628211UL;
        }
        h ^= salt;
        h *= 1099511628211UL;
        h ^= (uint)tick;
        h *= 1099511628211UL;
        foreach (char c in id2)
        {
            h ^= c;
            h *= 1099511628211UL;
        }
        return h;
    }
}
