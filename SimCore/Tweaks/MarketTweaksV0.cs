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
}
