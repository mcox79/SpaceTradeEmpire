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

    // GATE.S8.HAVEN.KEEPER.001: Keeper tier progression thresholds.
    // Keeper advances based on cumulative player investment in Haven.
    public const int KeeperAwareExoticMatter = 10;      // Delivered exotic matter for Aware
    public const int KeeperGuidingExoticMatter = 30;    // For Guiding
    public const int KeeperGuidingFragments = 2;         // Installed fragments for Guiding
    public const int KeeperCommunicatingExoticMatter = 80; // For Communicating
    public const int KeeperCommunicatingFragments = 4;   // Installed fragments for Communicating
    public const int KeeperCommunicatingDataLogs = 10;   // Data logs discovered for Communicating
    public const int KeeperAwakenedExoticMatter = 150;   // For Awakened
    public const int KeeperAwakenedFragments = 8;        // Installed fragments for Awakened
    public const int KeeperAwakenedDataLogs = 20;        // Data logs discovered for Awakened

    // GATE.S8.HAVEN.RESONANCE.001: Resonance Chamber cooldown (ticks between activations).
    public const int ResonanceCooldownTicks = 50;

    // GATE.S8.HAVEN.FABRICATOR.001: T3 module fabrication costs.
    public const int FabricateExoticMatterCost = 50;
    public const int FabricateExoticCrystalsCost = 20;
    public const int FabricateDurationTicks = 100;

    // GATE.S8.HAVEN.MARKET_EVOLUTION.001: Haven market restock interval.
    public const int MarketRestockIntervalTicks = 50;

    // GATE.S8.HAVEN.RESEARCH_LAB.001: Haven research lab slot counts per tier.
    public const int ResearchSlotsTier2 = 1;  // T3 utility only
    public const int ResearchSlotsTier3 = 2;  // T3 weapons/defense added
    public const int ResearchSlotsTier4 = 3;  // All T3 categories

    // Haven research speed multiplier (percentage of normal speed).
    public const int ResearchLabSpeedPct = 100;

    // Credit cost per tick per active research slot at Haven.
    public const int ResearchLabCreditPerTick = 5;

    // GATE.S8.HAVEN.ACCOMMODATION_FX.001: Per-thread bonus percentages at 33/66/100 progress.
    // Discovery: scan range bonus.
    public const int AccDiscoveryScanTier1Pct = 5;
    public const int AccDiscoveryScanTier2Pct = 10;
    public const int AccDiscoveryScanTier3Pct = 15;
    // Commerce: market price discount.
    public const int AccCommercePriceTier1Pct = 3;
    public const int AccCommercePriceTier2Pct = 6;
    public const int AccCommercePriceTier3Pct = 10;
    // Conflict: damage bonus.
    public const int AccConflictDamageTier1Pct = 5;
    public const int AccConflictDamageTier2Pct = 10;
    public const int AccConflictDamageTier3Pct = 15;
    // Harmony: rep gain bonus.
    public const int AccHarmonyRepTier1Pct = 10;
    public const int AccHarmonyRepTier2Pct = 20;
    public const int AccHarmonyRepTier3Pct = 30;

    // Progress thresholds for bonus tiers.
    public const int AccBonusTier1Threshold = 33;
    public const int AccBonusTier2Threshold = 66;
    public const int AccBonusTier3Threshold = 100;

    // GATE.S8.HAVEN.REVEAL_THREAD.001: Reveal Haven to faction.
    public const int RevealMinFactionRep = 25;  // Minimum rep to reveal
    public const int RevealRepBonus = 15;       // One-time rep boost on reveal
}
