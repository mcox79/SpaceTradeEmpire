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
    // Raised from 50→200 to reduce profit variance across seeds (range=4 → target ≤2).
    public const int MinStarterMargin   = 200;
    // Stock targets when adjusting starter node inventory to guarantee margin.
    public const int StarterHighStock   = 300;  // Push buy price low — stronger guarantee.
    public const int StarterLowStock    = 5;    // Push sell price high — stronger guarantee.

    // MaxStarterBuyPrice: max affordable buy price so the player can buy ≥3 units.
    // With starting credits ~997, price ≤250 allows buying 3+ units for meaningful profit.
    public const int MaxStarterBuyPrice = 250;
    // StarterBuyFloorStock: stock level to push if buy price is too high.
    public const int StarterBuyFloorStock = 500;

    // GATE.T52.ECON.TRADE_DIVERSITY.001: Proportional scarcity curve.
    // Each unit away from IdealStock shifts price by ScarcityBpsPerUnit basis
    // points of the good's base price. With IdealStock=50 and ScarcityBpsPerUnit=200,
    // buying 10 units (stock 40) shifts price by 10*200=2000 bps = +20%.
    // Higher-value goods respond more strongly, discouraging single-good dominance.
    public const int ScarcityBpsPerUnit = 200;

    // GATE.T52.ECON.TRADE_DIVERSITY.001: Recent-trade margin dampening.
    // When player trades the same good at the same node repeatedly, price
    // adjusts to reduce margin (supply/demand response to player activity).
    // RecentTradeDecayTicks: how quickly the dampening fades (ticks).
    public const int RecentTradeDecayTicks = 120;
    // RecentTradeDampenBps: margin reduction per recent trade (basis points).
    // 5 trades of the same good = 5 * 500 = 2500 bps = 25% margin reduction.
    public const int RecentTradeDampenBps = 500;
    // RecentTradeMaxDampenBps: cap on total dampening (80% max reduction).
    public const int RecentTradeMaxDampenBps = 8000;
}
