using System.Collections.Generic;

namespace SimCore.Entities;

// GATE.S5.LOOT.DROP_SYSTEM.001: Loot drop entity from NPC kills.
public enum LootRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3
}

public class LootDrop
{
    public string Id { get; set; } = "";
    public string NodeId { get; set; } = "";
    public LootRarity Rarity { get; set; } = LootRarity.Common;
    public int TickCreated { get; set; }
    // Goods loot: keyed by good ID, values are quantities.
    public Dictionary<string, int> Goods { get; set; } = new();
    // Credits loot.
    public int Credits { get; set; }
    // Module loot (Rare/Epic only).
    public string? ModuleId { get; set; }
}
