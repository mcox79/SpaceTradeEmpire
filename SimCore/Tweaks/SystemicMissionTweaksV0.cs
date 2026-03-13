namespace SimCore.Tweaks;

/// <summary>
/// Systemic mission trigger thresholds (GATE.S9.SYSTEMIC.TRIGGER_ENGINE.001).
/// All thresholds are int-based for determinism.
/// </summary>
public static class SystemicMissionTweaksV0
{
    // Scan interval: evaluate triggers every N ticks (not every tick for perf).
    public const int ScanIntervalTicks = 60;

    // WAR_DEMAND: market inventory threshold below which a war goods shortage triggers.
    public const int WarDemandInventoryThreshold = 10;

    // PRICE_SPIKE: price must exceed BasePricePct of Market.BasePrice to trigger (200 = 2x).
    public const int PriceSpikeThresholdPct = 200;

    // SUPPLY_SHORTAGE: instability level above which production shortage scan activates.
    public const int SupplyShortageInstabilityMin = 50;
    // SUPPLY_SHORTAGE: inventory below this threshold at a high-instability node triggers.
    public const int SupplyShortageInventoryThreshold = 15;

    // Offer expiry: systemic offers expire after N ticks if not accepted.
    public const int OfferExpiryTicks = 300;

    // Max concurrent systemic offers.
    public const int MaxSystemicOffers = 10;

    // GATE.S9.SYSTEMIC.OFFER_GEN.001: Mission template rewards and quantities.
    public const int WarDemandDeliveryQty = 5;
    public const long WarDemandCreditReward = 500;
    public const long PriceSpikeCreditReward = 200;
    public const int SupplyRunDeliveryQty = 8;
    public const long SupplyRunCreditReward = 400;
}
