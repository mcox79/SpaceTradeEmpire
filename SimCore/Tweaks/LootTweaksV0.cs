namespace SimCore.Tweaks;

// GATE.S5.LOOT.DROP_SYSTEM.001: Loot drop rarity weights and reward tables.
public static class LootTweaksV0
{
    // Rarity weights (sum = 100). Roll against cumulative weights.
    public const int CommonWeight = 60;
    public const int UncommonWeight = 25;
    public const int RareWeight = 12;
    public const int EpicWeight = 3;
    public const int TotalWeight = CommonWeight + UncommonWeight + RareWeight + EpicWeight;

    // Common loot: salvaged materials only (no credits — salvage is THINGS, not magic money).
    public const int CommonGoodsQtyMin = 1;
    public const int CommonGoodsRange = 3; // 1-3 units of a basic material

    // Uncommon loot: goods + small credits (data chips/valuables found in wreckage).
    public const int UncommonCreditsMin = 10;
    public const int UncommonCreditsRange = 30;
    public const int UncommonGoodsQty = 3;

    // Rare loot: valuable salvage + modest credits.
    public const int RareCreditsMin = 20;
    public const int RareCreditsRange = 50;
    public const int RareGoodsQtyMin = 2;
    public const int RareGoodsRange = 4; // 2-5 units of uncommon material

    // Epic loot: premium salvage + credits (encrypted data cores, intact systems).
    public const int EpicCreditsMin = 50;
    public const int EpicCreditsRange = 100;
    public const int EpicGoodsQtyMin = 3;
    public const int EpicGoodsRange = 5; // 3-7 units of rare material

    // GATE.T67.COMBAT.LOOT_FLOOR.001: Guaranteed minimum salvage on every kill.
    // FTL pattern: every kill yields at least basic scrap. Prevents "empty kill" frustration.
    public const int GuaranteedScrapQty = 1; // 1 unit of fuel or ore on every kill
    public static readonly string[] GuaranteedScrapPool =
    {
        Content.WellKnownGoodIds.Fuel,
        Content.WellKnownGoodIds.Ore,
    };

    // GATE.T64.COMBAT.PITY_JACKPOT.001: Pity timer — force Uncommon+ after N consecutive Commons.
    // GATE.T67.COMBAT.LOOT_FLOOR.001: Reduced from 5→3. Destiny pity-timer research: 3 streak max.
    public const int PityThreshold = 3;
    // GATE.T64.COMBAT.PITY_JACKPOT.001: Jackpot — force Rare+ every Nth kill.
    public const int JackpotKillInterval = 7;

    // Loot despawn after N ticks.
    public const int DespawnTicks = 3600;

    // Common goods pool: basic hull/cargo salvage (fuel, ore, metal — what a wrecked ship is made of).
    public static readonly string[] CommonGoodsPool =
    {
        Content.WellKnownGoodIds.Fuel,
        Content.WellKnownGoodIds.Ore,
        Content.WellKnownGoodIds.Metal,
    };

    // Uncommon goods pool: systems and components from the wreckage.
    public static readonly string[] UncommonGoodsPool =
    {
        Content.WellKnownGoodIds.Electronics,
        Content.WellKnownGoodIds.Munitions,
        Content.WellKnownGoodIds.Components,
        Content.WellKnownGoodIds.Metal,
        Content.WellKnownGoodIds.Composites,
    };

    // Rare goods pool: valuable salvage from well-equipped ships.
    public static readonly string[] RareGoodsPool =
    {
        Content.WellKnownGoodIds.SalvagedTech,
        Content.WellKnownGoodIds.Electronics,
        Content.WellKnownGoodIds.Components,
        Content.WellKnownGoodIds.RareMetals,
    };

    // Epic goods pool: exotic materials from advanced ships.
    public static readonly string[] EpicGoodsPool =
    {
        Content.WellKnownGoodIds.SalvagedTech,
        Content.WellKnownGoodIds.RareMetals,
        Content.WellKnownGoodIds.ExoticCrystals,
        Content.WellKnownGoodIds.Composites,
    };
}
