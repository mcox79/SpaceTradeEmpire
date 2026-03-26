namespace SimCore.Tweaks;

// GATE.T58.FO.EMPIRE_HEALTH.001 + GATE.T58.FO.DOCK_RECAP.001 + GATE.T58.FO.LOA_MODEL.001
// + GATE.T58.FO.SERVICE_RECORD.001 + GATE.T58.FO.FLIP_MOMENT.001
// Shared tweaks for the FO Trade Manager system per fo_trade_manager_v0.md v6.
public static class FOManagerTweaksV0
{
    // ── Empire Health thresholds ──
    // Route margin below this % → Degraded contributor
    public static int DegradedMarginPct { get; } = 5;
    // Sustain stock below this many cycles → Degraded contributor
    public static int DegradedSustainCycles { get; } = 3;
    // Any dead route or critical sustain → Critical
    public static int CriticalSustainCycles { get; } = 1;
    // Cadence: evaluate every N ticks
    public static int HealthEvalCadenceTicks { get; } = 10;

    // ── Dock Recap thresholds ──
    // Minimum ticks since last dock to trigger recap
    public static int RecapMinTicksSinceLastDock { get; } = 100;
    // Maximum recap lines (pull model: "Details in the Empire tab")
    public static int RecapMaxLines { get; } = 3;

    // ── LOA defaults per domain ──
    // LOA levels per Sheridan & Verplank (adapted): 4-7 range
    public static int LOARouteCreation { get; } = 5;       // Execute if approved
    public static int LOARouteOptimization { get; } = 6;   // Act, inform after
    public static int LOASustainLogistics { get; } = 7;    // Act, report on exception
    public static int LOAShipPurchase { get; } = 4;        // Suggest one alternative
    public static int LOAWarfrontResponse { get; } = 4;    // Suggest with rationale
    public static int LOAConstruction { get; } = 5;        // Execute if approved
    // Route revert window (ticks) for LOA 6 auto-actions
    public static int RouteRevertWindowTicks { get; } = 200;

    // ── Flip Moment detection ──
    // Sustained net-positive revenue for this many ticks → flip fires
    public static int FlipSustainedTicks { get; } = 50;
    // Minimum managed routes before flip can trigger
    public static int FlipMinRoutes { get; } = 3;

    // ── Service Record ──
    // History ring buffer size (entries)
    public static int ServiceRecordMaxEntries { get; } = 50;
}
