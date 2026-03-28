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

    // GATE.T52.ECON.TRADE_DIVERSITY.001 + GATE.T63.ECON.ROUTE_DECAY.001: Recent-trade margin dampening.
    // When player trades the same good at the same node repeatedly, price
    // adjusts to reduce margin (supply/demand response to player activity).
    // RecentTradeDecayTicks: how quickly the dampening fades (ticks).
    // GATE.T63: Reduced from 600→400 ticks so penalty decays faster when rotating routes,
    // but hits harder per-trade (1200 bps) to discourage same-route grinding.
    // fh_5 grind score 4.0+ on 3/5 seeds → needed stronger anti-grind.
    public const int RecentTradeDecayTicks = 400;
    // RecentTradeDampenBps: margin reduction per recent trade (basis points).
    // GATE.T63: Raised from 800→1200 bps/trade. After 3 trades: 3600 bps = 36% margin reduction.
    // With 400-tick decay = decay rate 3 bps/tick → faster recovery when rotating.
    public const int RecentTradeDampenBps = 1200;
    // GATE.T65.ECON.ROUTE_NOVELTY.001: Bonus for trading new routes.
    // First trade on a new market+good pair gets NoveltyBonusBps margin bonus.
    // Decays linearly over NoveltyDecayTrades trades (e.g., full bonus on trade 1, 2/3 on trade 2, 1/3 on trade 3).
    public const int NoveltyBonusBps = 3000; // 30% bonus on first trade
    public const int NoveltyDecayTrades = 3; // Bonus fades over 3 trades

    // GATE.T65.ECON.DAMPEN_CAP.001: cap lowered from 95% to 50%. 9500 caused late margins
    // to go negative (-37 avg). 5000 keeps late trades profitable but still penalizes grinding.
    public const int RecentTradeMaxDampenBps = 5000;
}
