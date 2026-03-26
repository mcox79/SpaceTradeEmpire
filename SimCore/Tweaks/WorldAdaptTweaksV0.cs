namespace SimCore.Tweaks;

// GATE.T57.CENTAUR.WORLD_ADAPT.001: World adaptation detection thresholds.
public static class WorldAdaptTweaksV0
{
    // Check cadence (ticks between adaptation scans).
    public static int AdaptCadenceTicks { get; } = 25;

    // IntelAged: flag route when age exceeds this % of freshness window.
    public static int IntelAgedThresholdPct { get; } = 75;

    // MarketShift: flag when actual profit deviates from estimate by this %.
    public static int MarketShiftThresholdPct { get; } = 40;

    // PlayerOverlap: flag when NPC compression on route exceeds this bps.
    public static int PlayerOverlapCompressionBps { get; } = 800;

    // FactionConflict: flag routes through embargoed nodes.
    // (Detection uses existing Embargoes list on SimState.)

    // TariffImposed: flag routes through nodes with tariff > this %.
    public static int TariffThresholdPct { get; } = 15;

    // Pause escalation: flag → pause after this many ticks unfixed.
    public static int FlagToPauseEscalationTicks { get; } = 50;
}
