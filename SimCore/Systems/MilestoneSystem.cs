using System.Collections.Generic;
using SimCore.Content;
using SimCore.Entities;

namespace SimCore.Systems;

// GATE.S12.PROGRESSION.MILESTONES.001: Milestone evaluation system.
// Checks PlayerStats against MilestoneContentV0 thresholds each tick.
// Records achieved milestones in PlayerStats.AchievedMilestoneIds.
public static class MilestoneSystem
{
    public static void Process(SimState state)
    {
        if (state?.PlayerStats == null) return;

        var stats = state.PlayerStats;
        stats.AchievedMilestoneIds ??= new List<string>();

        foreach (var def in MilestoneContentV0.All)
        {
            if (stats.AchievedMilestoneIds.Contains(def.Id)) continue;

            long current = GetStatValue(stats, def.StatKey);
            if (current >= def.Threshold)
            {
                stats.AchievedMilestoneIds.Add(def.Id);
            }
        }
    }

    public static long GetStatValue(PlayerStats stats, string statKey)
    {
        return statKey switch
        {
            "NodesVisited" => stats.NodesVisited,
            "GoodsTraded" => stats.GoodsTraded,
            "TotalCreditsEarned" => stats.TotalCreditsEarned,
            "TechsUnlocked" => stats.TechsUnlocked,
            "MissionsCompleted" => stats.MissionsCompleted,
            _ => 0
        };
    }
}
