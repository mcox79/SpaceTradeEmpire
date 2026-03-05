namespace SimCore.Tweaks;

// GATE.S4.CONSTR_PROG.MODEL.001: Construction pacing constants (integers only for determinism).
public static class ConstructionTweaksV0
{
    // Max concurrent construction projects per node.
    public const int MaxProjectsPerNode = 1;

    // Max total concurrent construction projects across all nodes.
    public const int MaxTotalProjects = 3;

    // Progress ticks added per sim tick while constructing.
    public const int ProgressPerTick = 1;

    // Minimum credits to avoid stalling (if credits < cost, construction stalls).
    public const int MinCreditsToProgress = 0;
}
