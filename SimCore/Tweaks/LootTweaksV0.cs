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

    // Common loot: credits only.
    public const int CommonCreditsMin = 10;
    public const int CommonCreditsRange = 20; // actual = Min + hash % Range

    // Uncommon loot: goods + credits.
    public const int UncommonCreditsMin = 25;
    public const int UncommonCreditsRange = 50;
    public const int UncommonGoodsQty = 3;

    // Rare loot: module drop.
    public const int RareCreditsMin = 50;
    public const int RareCreditsRange = 100;

    // Epic loot: premium module drop + credits.
    public const int EpicCreditsMin = 100;
    public const int EpicCreditsRange = 200;

    // Loot despawn after N ticks.
    public const int DespawnTicks = 3600;

    // Goods pool for uncommon drops (deterministic pick by hash).
    public static readonly string[] UncommonGoodsPool =
    {
        Content.WellKnownGoodIds.Fuel,
        Content.WellKnownGoodIds.Metal,
        Content.WellKnownGoodIds.Ore,
        Content.WellKnownGoodIds.Electronics,
        Content.WellKnownGoodIds.Munitions,
    };
}
