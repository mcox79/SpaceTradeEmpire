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
    public static readonly List<MilestoneDef> All = new()
    {
        new() { Id = "first_trade", Name = "First Trade", StatKey = "GoodsTraded", Threshold = 1 },
        new() { Id = "explorer_5", Name = "Explorer", StatKey = "NodesVisited", Threshold = 5 },
        new() { Id = "merchant_1000", Name = "Merchant", StatKey = "TotalCreditsEarned", Threshold = 1000 },
        new() { Id = "researcher_1", Name = "Researcher", StatKey = "TechsUnlocked", Threshold = 1 },
        new() { Id = "captain_1", Name = "Captain", StatKey = "MissionsCompleted", Threshold = 1 },
        new() { Id = "trader_100", Name = "Bulk Trader", StatKey = "GoodsTraded", Threshold = 100 },
        new() { Id = "explorer_15", Name = "Pathfinder", StatKey = "NodesVisited", Threshold = 15 },
        new() { Id = "tycoon_10000", Name = "Tycoon", StatKey = "TotalCreditsEarned", Threshold = 10000 },
    };

    public static MilestoneDef? GetById(string id)
    {
        foreach (var m in All)
            if (m.Id == id) return m;
        return null;
    }
}
