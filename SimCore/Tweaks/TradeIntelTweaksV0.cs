namespace SimCore.Tweaks;

// GATE.S10.TRADE_INTEL.TWEAKS.001: Trade intel scanner and route discovery constants.
public static class TradeIntelTweaksV0
{
    // Scanner cadence: how often the passive scanner records prices (matches market publish window).
    public const int ScanCadenceTicks = 720;

    // Intel staleness threshold: observations older than this are considered stale.
    public const int StaleAgeTicks = 2160; // 3 publish windows

    // Scanner range by tech tier (0 = local only, 1 = adjacent, 2 = two hops).
    public const int BaseScanRange = 0;
    public const int SensorSuiteScanRange = 1;
    public const int TradeNetworkScanRange = 2;

    // Minimum profit per unit to surface a route as Discovered (filters noise).
    public const int MinProfitThreshold = 5;

    // GATE.S11.GAME_FEEL.PRICE_HISTORY.001: How often price history snapshots are recorded.
    // Uses half of scanner cadence for finer-grained trend data.
    public const int PriceHistoryCadenceTicks = 360;

    // Maximum number of price history entries retained per node+good pair.
    public const int PriceHistoryMaxEntries = 50;
}
