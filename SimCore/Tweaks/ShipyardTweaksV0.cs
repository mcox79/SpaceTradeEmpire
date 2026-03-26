namespace SimCore.Tweaks;

// GATE.T59.SHIP.VARIANT_DEFS.001: Shipyard purchase prices and disclosure rules.
public static class ShipyardTweaksV0
{
    // --- Purchase prices (credits) ---
    // Progression: Shuttle affordable early, Dreadnought is endgame investment.
    // Variants cost 30% more than their base class per faction_equipment_and_research_v0.md.
    public const int PriceShuttle = 500;
    public const int PriceCorvette = 2000;
    public const int PriceClipper = 2500;
    public const int PriceFrigate = 4000;
    public const int PriceHauler = 3000;
    public const int PriceCruiser = 8000;
    public const int PriceCarrier = 6500;
    public const int PriceDreadnought = 25000;

    // Concord variants (base * 1.3)
    public const int PriceWatchman = 5200;
    public const int PriceSentinel = 10400;
    public const int PriceGuardian = 8450;

    // Chitin variants
    public const int PriceGambit = 2600;
    public const int PriceWager = 3250;

    // Weavers variants
    public const int PriceSpindle = 3900;
    public const int PriceLoom = 10400;

    // Valorin variants
    public const int PriceFang = 2600;
    public const int PriceRunner = 3250;
    public const int PriceRaider = 5200;

    // Communion variants
    public const int PriceWanderer = 650;
    public const int PricePilgrim = 3250;

    // --- Sell-back ---
    // 80% of purchase price returned on sell.
    public const int SellBackPctBps = 8000;
    public const int BpsDivisor = 10000;

    // --- Faction rep requirement for variants ---
    public const int VariantRepRequired = 75;

    // --- Progressive catalog disclosure thresholds ---
    // Base classes: always visible at any shipyard.
    // Mid-tier (Cruiser, Carrier, Dreadnought): visible after visiting 3+ systems.
    public const int MidTierSystemsRequired = 3;
    // Capital ships (Dreadnought): visible after visiting faction capital or rep 25+.
    public const int CapitalRepRequired = 25;

    // --- Station shipyard capability ---
    // Stations with these faction IDs have shipyards (all faction capitals + major stations).
    // Actual capability determined by station tier in ShipyardSystem.
    public const int MinStationTierForShipyard = 2;
}
