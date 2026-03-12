namespace SimCore.Tweaks;

// GATE.T18.NARRATIVE.ROUTE_UNCERTAINTY.001: Route uncertainty mechanic constants.
// Phase 2+ travel times vary deterministically. ETA displays as range.
// Fracture module exposure narrows the range over time (adaptation).
public static class RouteUncertaintyTweaksV0
{
    // ETA variance by instability phase (percentage of base travel time).
    public const int Phase2VariancePct = 15;
    public const int Phase3VariancePct = 35;
    public const int Phase4VariancePct = 50;

    // Scanner adaptation stages: fracture jumps needed to reach each stage.
    // Stage 1 (default): wide range. Stage 2: weighted range. Stage 3: near-exact.
    public const int Stage2JumpsRequired = 5;
    public const int Stage3JumpsRequired = 15;

    // Variance reduction at each stage (percentage of base variance retained).
    public const int Stage1RetainedPct = 100; // full variance
    public const int Stage2RetainedPct = 50;  // half variance
    public const int Stage3RetainedPct = 10;  // near-exact

    // Minimum variance (even at Stage 3, some uncertainty remains in Phase 3+).
    public const int MinVariancePct = 3;
}
