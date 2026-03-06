using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.S12.PROGRESSION.STATS.001: Player progression statistics.
// Tracked by relevant systems each tick. Consumed by MilestoneSystem and bridge.
public class PlayerStats
{
    [JsonInclude] public int NodesVisited { get; set; }
    [JsonInclude] public int GoodsTraded { get; set; }
    [JsonInclude] public long TotalCreditsEarned { get; set; }
    [JsonInclude] public int TechsUnlocked { get; set; }
    [JsonInclude] public int MissionsCompleted { get; set; }

    // GATE.S12.PROGRESSION.MILESTONES.001: Achieved milestone IDs.
    [JsonInclude] public List<string> AchievedMilestoneIds { get; set; } = new();
}
