namespace SimCore.Tweaks;

// GATE.S9.SYSTEMIC.STATION_CONTEXT.001: Station economic context thresholds.
public static class StationContextTweaksV0
{
    /// <summary>Stock level below which a good is considered in shortage (pct of IdealStock).</summary>
    public const int ShortageThresholdPct = 40;

    /// <summary>Price premium above base price that signals an opportunity (pct above base).</summary>
    public const int OpportunityPremiumPct = 30;

    /// <summary>Interval in ticks between context recalculations.</summary>
    public const int ContextUpdateIntervalTicks = 60;

    /// <summary>Default good ID used for warfront demand context.</summary>
    public const string WarfrontDemandGoodId = "munitions";
}
