using System.Collections.Generic;

namespace SimCore.Content;

// GATE.S12.PROGRESSION.MILESTONES.001: Milestone definitions.
// Each milestone has a stat key, threshold, and display info.
public sealed class MilestoneDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string StatKey { get; set; } = ""; // matches PlayerStats property name
    public long Threshold { get; set; }
}

public static class MilestoneContentV0
{
    // GATE.S9.STEAM.ACHIEVEMENTS.001: Expanded milestone set for Steam achievements.
    // Categories: trade, exploration, research, mission, combat.
    public static readonly List<MilestoneDef> All = new()
    {
        // Trade milestones
        new() { Id = "first_trade", Name = "First Trade", StatKey = "GoodsTraded", Threshold = 1 },
        new() { Id = "trader_100", Name = "Bulk Trader", StatKey = "GoodsTraded", Threshold = 100 },
        new() { Id = "trader_1000", Name = "Trade Baron", StatKey = "GoodsTraded", Threshold = 1000 },
        new() { Id = "merchant_1000", Name = "Merchant", StatKey = "TotalCreditsEarned", Threshold = 1000 },
        new() { Id = "tycoon_10000", Name = "Tycoon", StatKey = "TotalCreditsEarned", Threshold = 10000 },
        new() { Id = "magnate_100000", Name = "Magnate", StatKey = "TotalCreditsEarned", Threshold = 100000 },

        // Exploration milestones
        new() { Id = "explorer_5", Name = "Explorer", StatKey = "NodesVisited", Threshold = 5 },
        new() { Id = "explorer_15", Name = "Pathfinder", StatKey = "NodesVisited", Threshold = 15 },
        new() { Id = "explorer_30", Name = "Star Cartographer", StatKey = "NodesVisited", Threshold = 30 },

        // Research milestones
        new() { Id = "researcher_1", Name = "Researcher", StatKey = "TechsUnlocked", Threshold = 1 },
        new() { Id = "researcher_5", Name = "Innovator", StatKey = "TechsUnlocked", Threshold = 5 },
        new() { Id = "researcher_15", Name = "Polymath", StatKey = "TechsUnlocked", Threshold = 15 },

        // Mission milestones
        new() { Id = "captain_1", Name = "Captain", StatKey = "MissionsCompleted", Threshold = 1 },
        new() { Id = "captain_5", Name = "Veteran Captain", StatKey = "MissionsCompleted", Threshold = 5 },
        new() { Id = "captain_15", Name = "Legendary Captain", StatKey = "MissionsCompleted", Threshold = 15 },

        // Combat milestones
        new() { Id = "combat_1", Name = "First Blood", StatKey = "NpcFleetsDestroyed", Threshold = 1 },
        new() { Id = "combat_10", Name = "Warfighter", StatKey = "NpcFleetsDestroyed", Threshold = 10 },
        new() { Id = "combat_25", Name = "Fleet Destroyer", StatKey = "NpcFleetsDestroyed", Threshold = 25 },
    };

    public static MilestoneDef? GetById(string id)
    {
        foreach (var m in All)
            if (m.Id == id) return m;
        return null;
    }
}
