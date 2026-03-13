namespace SimCore.Tweaks;

// GATE.S8.HAVEN.ENTITY.001: Haven starbase tuning constants.
public static class HavenTweaksV0
{
    // Hangar bay counts per tier.
    public const int HangarBaysTier1 = 1;  // Current ship only (no storage)
    public const int HangarBaysTier3 = 2;  // +1 stored ship
    public const int HangarBaysTier5 = 3;  // +2 stored ships

    // Upgrade durations (ticks).
    public const int UpgradeDurationTier2 = 50;
    public const int UpgradeDurationTier3 = 100;
    public const int UpgradeDurationTier4 = 200;
    public const int UpgradeDurationTier5 = 500;

    // Upgrade credit costs.
    public const int UpgradeCreditsTier2 = 500;
    public const int UpgradeCreditsTier3 = 1000;
    public const int UpgradeCreditsTier4 = 2000;
    public const int UpgradeCreditsTier5 = 5000;

    // Upgrade exotic matter costs.
    public const int UpgradeExoticMatterTier2 = 20;
    public const int UpgradeExoticMatterTier3 = 50;
    public const int UpgradeExoticMatterTier4 = 100;
    public const int UpgradeExoticMatterTier5 = 200;

    // Upgrade composites costs.
    public const int UpgradeCompositesTier2 = 10;
    public const int UpgradeCompositesTier3 = 20;

    // Upgrade electronics costs.
    public const int UpgradeElectronicsTier2 = 10;
    public const int UpgradeElectronicsTier4 = 30;
    public const int UpgradeElectronicsTier5 = 50;

    // Upgrade rare metals costs.
    public const int UpgradeRareMetalsTier3 = 20;
    public const int UpgradeRareMetalsTier4 = 30;
    public const int UpgradeRareMetalsTier5 = 50;

    // Upgrade exotic crystals costs.
    public const int UpgradeExoticCrystalsTier4 = 20;
    public const int UpgradeExoticCrystalsTier5 = 50;

    // Upgrade salvaged tech costs.
    public const int UpgradeSalvagedTechTier5 = 10;

    // Fragment requirements per tier.
    public const int FragmentsRequiredTier3 = 1;  // 1 navigation fragment
    public const int FragmentsRequiredTier4 = 1;  // 1 structural fragment
    public const int FragmentsRequiredTier5 = 3;  // 3 any-category

    // Sustain costs (exotic matter per sustain interval).
    public const int SustainIntervalTicks = 100;
    public const int SustainCostTier2 = 2;
    public const int SustainCostTier3 = 5;
    public const int SustainCostTier4 = 10;
    public const int SustainCostTier5 = 8;  // Reduced due to self-optimization

    // Tier 5 passive generation (per sustain interval).
    public const int PassiveExoticMatterTier5 = 2;
    public const int PassiveExoticCrystalsTier5 = 2;

    // Market stock levels per tier.
    public const int MarketStockTier1 = 10;
    public const int MarketStockTier2 = 20;
    public const int MarketStockTier3 = 30;
    public const int MarketStockTier4 = 50;

    // Faction goods sell penalty (50% = sell at half price).
    public const int FactionGoodsSellPenaltyPct = 50;

    // Tractor module constants.
    public const int TractorFallbackRange = 5;
    public const int TractorT1Range = 15;
    public const int TractorT2Range = 30;
    public const int TractorT3Range = 50;

    // Discovery: Communion rep threshold for breadcrumb.
    public const int CommunionRepBreadcrumbThreshold = 50;
}
