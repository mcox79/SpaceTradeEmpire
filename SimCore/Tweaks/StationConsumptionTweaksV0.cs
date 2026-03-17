namespace SimCore.Tweaks;

// Station population consumption: stations consume food and fuel every tick,
// representing crew/population life-support. This creates permanent demand
// sinks that prevent surplus goods from collapsing prices galaxy-wide.
// Lore: "organic nutrition packs, essential for crewed stations" (NarrativeContent_TBA).
public static class StationConsumptionTweaksV0
{
    // Food consumed per station per tick.
    // 20 nodes × 1 food/tick = 20/tick galaxy-wide.
    // Food production: ~12/tick (4 processors × 3). Net deficit of ~8/tick
    // means food is always in demand — stations slowly starve without trade.
    public const int FoodPerTick = 1;

    // Fuel consumed per station per tick for life-support and station systems.
    // 20 nodes × 1 fuel/tick = 20/tick galaxy-wide.
    // Fuel production: 15-20/tick (fuel wells). Combined with industry fuel use
    // (26-27/tick), total demand ~46-47 vs 15-20 supply. Fuel stays scarce.
    public const int FuelPerTick = 1;

    // Consumption cadence: every Nth tick. Prevents per-tick micro-drain.
    // At CadenceTicks=10, each station consumes FoodPerTick * 10 food every 10 ticks.
    // This batches consumption into visible market events.
    public const int CadenceTicks = 10;

    // Batch quantity per consumption event = PerTick * CadenceTicks.
    // Food: 1 * 10 = 10 per event. Fuel: 1 * 10 = 10 per event.
    // These are the actual amounts removed from inventory each cadence.
}
