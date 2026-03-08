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
}
