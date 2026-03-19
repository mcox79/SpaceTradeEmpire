using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Content;
using SimCore.Entities;

namespace SimCore.Systems;

// GATE.S12.PROGRESSION.MILESTONES.001: Milestone evaluation system.
// Checks PlayerStats against MilestoneContentV0 thresholds each tick.
// Records achieved milestones in PlayerStats.AchievedMilestoneIds.
public static class MilestoneSystem
{
    private sealed class Scratch
    {
        public readonly HashSet<string> AchievedSet = new(StringComparer.Ordinal);
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();

    public static void Process(SimState state)
    {
        if (state?.PlayerStats == null) return;

        var stats = state.PlayerStats;
        stats.AchievedMilestoneIds ??= new List<string>();

        var scratch = s_scratch.GetOrCreateValue(state);
        var achievedSet = scratch.AchievedSet;
        achievedSet.Clear();
        foreach (var id in stats.AchievedMilestoneIds) achievedSet.Add(id);

        foreach (var def in MilestoneContentV0.All)
        {
            if (achievedSet.Contains(def.Id)) continue;

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
            "NpcFleetsDestroyed" => stats.NpcFleetsDestroyed,
            _ => 0
        };
    }
}
