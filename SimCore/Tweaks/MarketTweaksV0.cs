namespace SimCore.Tweaks;

// GATE.S18.TRADE_GOODS.PRICE_BANDS.001: Market pricing gameplay knobs.
public static class MarketTweaksV0
{
    // Price bands: Low (50-100), Mid (150-300), High (400-800), Very High (1000-2000).
    // BasePrice is the center of the band, PriceSpread defines the +/- range.
    // Supply/demand modifiers scale price within [BasePrice - Spread, BasePrice + Spread].
    public const int PriceLowBase       = 75;
    public const int PriceLowSpread     = 25;
    public const int PriceMidBase       = 225;
    public const int PriceMidSpread     = 75;
    public const int PriceHighBase      = 600;
    public const int PriceHighSpread    = 200;
    public const int PriceVeryHighBase  = 1500;
    public const int PriceVeryHighSpread = 500;

    // Supply/demand price scaling: how much inventory affects price.
    // DemandThreshold: below this qty, price rises. Above, price falls.
    public const int DemandThreshold    = 100;
    // Max price multiplier in basis points (200% = 20000 bps).
    public const int MaxPriceMultBps    = 20000;
    // Min price multiplier in basis points (25% = 2500 bps).
    public const int MinPriceMultBps    = 2500;

    // Starter arbitrage guarantee: ensures first-trade has a profitable route.
    // MinStarterMargin: minimum buy→sell margin (cr/unit) required at player start.
    // GATE.T64.ECON.ELECTRONICS_FIX.001: Reduced from 200→100. Previous value caused
    // extreme stock pushes on high-value goods (electronics 529cr margin → target 80-150).
    public const int MinStarterMargin   = 100;
    // Stock targets when adjusting starter node inventory to guarantee margin.
    // GATE.T64.ECON.ELECTRONICS_FIX.001: Softened from 300/5 to 60/35 to prevent
    // collapsing buy price to 1cr and inflating sell to 530+cr on high-base-price goods.
    public const int StarterHighStock   = 60;   // Modest push above IdealStock (50).
    public const int StarterLowStock    = 35;   // Modest push below IdealStock (50).

    // MaxStarterBuyPrice: max affordable buy price so the player can buy ≥3 units.
    // With starting credits ~997, price ≤250 allows buying 3+ units for meaningful profit.
    public const int MaxStarterBuyPrice = 250;
    // StarterBuyFloorStock: stock level to push if buy price is too high.
    // GATE.T64.ECON.ELECTRONICS_FIX.001: Reduced from 500→150 (softer affordability push).
    public const int StarterBuyFloorStock = 150;

    // GATE.T52.ECON.TRADE_DIVERSITY.001: Proportional scarcity curve.
    // Each unit away from IdealStock shifts price by ScarcityBpsPerUnit basis
    // points of the good's base price. With IdealStock=50 and ScarcityBpsPerUnit=200,
    // buying 10 units (stock 40) shifts price by 10*200=2000 bps = +20%.
    // Higher-value goods respond more strongly, discouraging single-good dominance.
    public const int ScarcityBpsPerUnit = 200;

    // GATE.T52.ECON.TRADE_DIVERSITY.001 + GATE.T63.ECON.ROUTE_DECAY.001 + GATE.T66.ECON.GRIND_REDUCTION.001:
    // Recent-trade margin dampening. When player trades the same good at the same node
    // repeatedly, price adjusts to reduce margin (supply/demand response to player activity).
    // GATE.T68.ECON.ROUTE_DAMPEN_V2.001: Raised from 4000→6000 bps/trade.
    // fh_12: diversity=0.21 (target>0.3), route_repeat_max=20. More aggressive per-trade penalty.
    // After 2 trades: 12000 bps = effectively zero margin. Forces route rotation.
    public const int RecentTradeDampenBps = 6000;
    // fh_14: Reduced from 80→50 ticks. With 10x experienced decay multiplier,
    // experienced traders recover routes in ~5 ticks (near-instant after one trade elsewhere).
    // This rewards exploration: visit new route → old routes already recovering.
    public const int RecentTradeDecayTicks = 50;
    // GATE.T68.ECON.ROUTE_DAMPEN_V2.001: Reduced from 3→2. Penalty kicks in after just 2 repeats.
    // fh_12: grind_score=14.0 — must be much more aggressive to break route locking.
    public const int ExponentialPenaltyThreshold = 2;
    // fh_14: Raised from 1000→2000 bps/repeat^2. VFY route_repeat_max=20-30 (target <15).
    // At 3 repeats: (3-2)^2 * 2000 = 2000 extra bps. At 5: (5-2)^2 * 2000 = 18000 extra bps.
    // Much harsher penalty makes 4+ repeats economically unviable, forcing route switching.
    public const int ExponentialPenaltyBpsPerSq = 2000;
    // GATE.T68: Raised from 5000→7000 bps. Even stronger pull toward new routes.
    // NMS pattern: push notifications for new trade opportunities drive exploration.
    public const int NoveltyBonusBps = 7000; // 70% bonus on first trade at new route
    // GATE.T68: Reduced from 3→2. Concentrated bonus on first 2 trades rewards route switching.
    public const int NoveltyDecayTrades = 2; // Bonus fades over 2 trades

    // GATE.T68.ECON.ROUTE_DAMPEN_V2.001: Raised from 4000→6000 bps first-visit bonus.
    // Starsector pattern: route health indicator + discovery bonus for new stations.
    // NMS pattern: FO announces profitable opportunities at new stations.
    public const int FirstVisitBonusBps = 6000; // 60% bonus on first trade at newly discovered station
    public const int FirstVisitBonusTrades = 3; // Bonus applies to first 3 trades at new station

    // GATE.T65.ECON.DAMPEN_CAP.001 + GATE.T66.ECON.GRIND_REDUCTION.001:
    // Cap raised from 5000→6000 bps (60%). With margin floor (GATE.T66), margins stay positive
    // but grinding becomes clearly suboptimal vs exploring new routes.
    public const int RecentTradeMaxDampenBps = 6000;

    // GATE.T68.ECON.MARGIN_CURVE_V2.001: Lower threshold from 8→6 nodes. Kicks in sooner.
    // fh_12: -64% margin decline. Experienced trader benefits must activate earlier.
    // Inverted-U curve: margins peak mid-game (new routes + novelty), then stabilize (not decline).
    public const int ExperiencedTraderNodeThreshold = 6;
    // fh_14: Raised from 5→10. With 200-decision VFY run, competence margin collapsed -97%.
    // At 5x, dampening accumulated faster than decay even for experienced traders.
    // 10x makes routes recover in ~8 ticks — experienced traders should always have viable routes.
    public const int ExperiencedDampenDecayMultiplier = 10; // 10x faster decay after threshold

    // fh_14: Raised from 5000→7000 bps. VFY competence margin -97% at 200 decisions.
    // 70% extra price variance for experienced traders creates meaningful late-game opportunities.
    // Factorio pattern: 4x tier multiplier — bigger factory = bigger opportunities.
    public const int LateGameVarianceBonusBps = 7000; // 70% extra price variance for experienced traders

    // GATE.T68.ECON.MARGIN_CURVE_V2.001: Raised from 3000→5000 bps.
    // After visiting 6+ nodes, high-tier goods sell at 50% premium at non-producing stations.
    // Factorio 4x tier multiplier: late-game goods = late-game margins.
    public const int LateGoodsPremiumBps = 5000; // 50% premium on late-game goods
    public static readonly string[] LateGameGoodIds =
    {
        Content.WellKnownGoodIds.Electronics,
        Content.WellKnownGoodIds.Components,
        Content.WellKnownGoodIds.ExoticCrystals,
        Content.WellKnownGoodIds.SalvagedTech,
    };

    // GATE.T66.ECON.MARGIN_FLOOR.001: Minimum trade margin guarantee.
    // fh_8: margin decline -52% (late margins went negative). This floor ensures trades
    // are always at least minimally profitable, preventing frustration while still
    // penalizing route grinding through reduced margins.
    // Tiered goods pattern (Elite/X4/EVE): margins constant per tier, not zero.
    public const int MinSellMarginBps = 500; // 5% minimum margin floor on sell price
    // Session-local warmup: first 3 trades at a station get a "fresh stock premium."
    // Factorio pattern: early interactions are more rewarding to build confidence.
    public const int FreshStockPremiumBps = 1500; // 15% bonus on first 3 trades per station
    public const int FreshStockPremiumTrades = 3; // Premium applies to first N trades

    // GATE.T67.ECON.ROUTE_DECAY.001: Absolute dampening cap (95% = 9500 bps).
    // Always leaves 5% margin floor to prevent fully zero-margin trades.
    public const int AbsoluteDampenCapBps = 9500;
}
