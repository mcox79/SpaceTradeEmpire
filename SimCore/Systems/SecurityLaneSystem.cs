using System;
using System.Collections.Generic;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S5.SEC_LANES.SYSTEM.001: Security lane system — patrol presence + piracy heat to security level.
public static class SecurityLaneSystem
{
    /// <summary>
    /// Process security levels on all edges each tick.
    /// Patrol fleets on adjacent nodes raise security. Economic heat lowers it.
    /// Natural drift pulls toward default.
    /// </summary>
    public static void ProcessSecurityLanes(SimState state)
    {
        if (state == null) return;

        var edgeIds = new List<string>(state.Edges.Keys);
        edgeIds.Sort(StringComparer.Ordinal);

        foreach (var edgeId in edgeIds)
        {
            var edge = state.Edges[edgeId];

            // Count patrol fleets on adjacent nodes
            int patrolCount = CountPatrolFleetsAtNode(state, edge.FromNodeId)
                            + CountPatrolFleetsAtNode(state, edge.ToNodeId);

            // Patrol boost
            int patrolBoost = patrolCount * SecurityTweaksV0.PatrolBoostBps;

            // Heat penalty (economic heat on the edge drives insecurity)
            int heatPenalty = (int)(edge.Heat * SecurityTweaksV0.HeatPenaltyBps);

            // Natural drift toward default
            int drift = CalculateDrift(edge.SecurityLevelBps);

            // Apply delta
            int newLevel = edge.SecurityLevelBps + patrolBoost - heatPenalty + drift;
            edge.SecurityLevelBps = Math.Clamp(newLevel,
                SecurityTweaksV0.MinSecurityBps,
                SecurityTweaksV0.MaxSecurityBps);
        }
    }

    private static int CountPatrolFleetsAtNode(SimState state, string nodeId)
    {
        int count = 0;
        foreach (var fleet in state.Fleets.Values)
        {
            if (fleet.Role == FleetRole.Patrol && fleet.CurrentNodeId == nodeId)
                count++;
        }
        return count;
    }

    private static int CalculateDrift(int currentBps)
    {
        if (currentBps < SecurityTweaksV0.DefaultSecurityBps)
            return SecurityTweaksV0.DriftToDefaultBps;
        if (currentBps > SecurityTweaksV0.DefaultSecurityBps)
            return -SecurityTweaksV0.DriftToDefaultBps;
        return 0;
    }

    /// <summary>
    /// Classify the security band for a given BPS value.
    /// </summary>
    public static string GetSecurityBand(int bps)
    {
        if (bps < SecurityTweaksV0.HostileBps) return "hostile";
        if (bps < SecurityTweaksV0.DangerousBps) return "dangerous";
        if (bps < SecurityTweaksV0.ModerateBps) return "moderate";
        if (bps >= SecurityTweaksV0.SafeBps) return "safe";
        return "moderate";
    }
}
