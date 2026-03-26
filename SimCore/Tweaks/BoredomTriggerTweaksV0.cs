namespace SimCore.Tweaks;

// GATE.T57.CENTAUR.BOREDOM_TRIGGERS.001: Circuit breaker thresholds.
// These fire FO triggers when the player might be stagnating.
public static class BoredomTriggerTweaksV0
{
    // Trigger 1: No new discovery for this many ticks.
    public static int NoDiscoveryThresholdTicks { get; } = 300;

    // Trigger 2: Number of routes with NPC margin compression flagging.
    public static int CompressedRoutesThreshold { get; } = 3;
    public static int CompressedRouteBpsThreshold { get; } = 500;

    // Trigger 3: Sustain programs generating > this % of total revenue.
    public static int SustainRevenueThresholdPct { get; } = 40;

    // Trigger 4: Chain intel breadcrumb available but not pursued for this many ticks.
    public static int ChainIntelStaleThresholdTicks { get; } = 200;

    // Trigger 5: Ticks since last revelation discovery (Analyzed phase).
    public static int SinceRevelationThresholdTicks { get; } = 500;

    // Cadence: how often to check boredom triggers.
    public static int BoredomCheckCadenceTicks { get; } = 50;
}
