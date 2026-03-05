namespace SimCore.Tweaks;

// GATE.S4.NPC_INDU.DEMAND.001: NPC industry pacing constants (integers only for determinism).
public static class NpcIndustryTweaksV0
{
    // How often NPC demand is processed (every N ticks).
    public const int ProcessIntervalTicks = 10;

    // Units consumed from market per input good per demand cycle.
    public const int NpcDemandConsumptionUnits = 2;

    // How often NPC production reactions are processed (every N ticks, slower than demand).
    public const int ReactionIntervalTicks = 20;

    // Stock threshold below which NPC industry boosts production.
    public const int LowStockThreshold = 10;

    // Units produced per reaction cycle when stock is low.
    public const int ReactionProductionBoost = 5;
}
