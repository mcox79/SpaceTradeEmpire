namespace SimCore.Tweaks;

// GATE.T61.MARKET.DEPTH_MODEL.001: Market depth and dynamic spread constants.
// GATE.T61.MARKET.BID_ASK.001: Bid/ask spread with volatility + trust + heat.
public static class MarketDepthTweaksV0
{
    // Base market depth (liquidity units). Higher = less price impact per trade.
    // Typical early-game trade of 5 units → 5/200 = 2.5% impact (barely noticeable).
    // Automated program trading 50 units → 50/200 = 25% of max = 750 bps = 7.5%.
    public const int BaseDepth = 200;

    // Maximum price impact in basis points when qty approaches depth.
    // At qty >= depth, impact is capped at ImpactMaxBps.
    public const int ImpactMaxBps = 3000;

    // Depth recovery per tick (toward BaseDepth). After a 50-unit trade,
    // depth drops by 50, recovers in 25 ticks (~25 sim-minutes).
    public const int DepthRecoveryPerTick = 2;

    // --- Dynamic spread adjustments (GATE.T61.MARKET.BID_ASK.001) ---

    // Volatility: each trade adds to a market's volatility score.
    // Score decays over time. High volatility → wider spread.
    public const int VolatilityPerTrade = 200;
    public const int VolatilityDecayPerTick = 1;
    public const int VolatilityMaxScore = 5000;
    // Spread widening: 1 bps of spread per point of volatility score.
    public const int VolatilitySpreadBpsPerPoint = 1;
    public const int VolatilitySpreadMaxBps = 2000;

    // Trust: low faction rep at a market's controlling faction widens spread.
    // Below this rep threshold, each rep point deficit adds spread.
    public const int TrustRepThreshold = 25;
    public const int TrustSpreadBpsPerRepPoint = 20;
    public const int TrustSpreadMaxBps = 1500;

    // Heat: edge congestion near a market widens spread.
    // Uses max heat of any edge connected to the market's node.
    // Heat is float; multiply by this factor to get bps.
    public const int HeatSpreadBpsPerUnit = 100;
    public const int HeatSpreadMaxBps = 1000;

    // --- Price smoothing (GATE.T61.MARKET.PRICE_SMOOTH.001) ---

    // EMA smoothing factor in basis points (0-10000). Higher = more weight to new price.
    // 3000 bps = 30% new + 70% old → gentle smoothing, ~3 publish windows to converge.
    public const int SmoothingAlphaBps = 3000;

    // Depth decay: per-tick depth loss when no trades occur at a market.
    // Markets with no trade activity slowly lose depth, widening spreads.
    // 0 = no decay. Depth floor is BaseDepth / 4 (never fully empty).
    public const int DepthInactivityDecayPerTick = 1;
    public const int DepthFloorDivisor = 4; // Depth never falls below BaseDepth / this.

    // Ticks of inactivity before depth decay starts. Gives a grace period.
    public const int DepthDecayGraceTicks = 60;
}
