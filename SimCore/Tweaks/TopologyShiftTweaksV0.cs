namespace SimCore.Tweaks;

// GATE.T18.NARRATIVE.TOPOLOGY_SHIFT.001: Topology shift mechanic constants.
// In Phase 3+ space, edge connections mutate on player arrival.
// Routes become unreliable — player navigates by topology, not memory.
public static class TopologyShiftTweaksV0
{
    // Minimum instability level for topology mutation.
    // STRUCTURAL: matches Phase 3 (Fracture) threshold.
    public const int STRUCT_MinPhaseForMutation = 75;

    // Probability of each eligible edge mutating per arrival (basis points).
    public const int MutationProbabilityBps = 1500; // 15%

    // Maximum edge mutations per player arrival event.
    public const int MaxMutationsPerArrival = 2;

    // Minimum edges a node must retain (connectivity preservation).
    // STRUCTURAL: ensures no node becomes orphaned.
    public const int STRUCT_MinEdgesPerNode = 1;
}
