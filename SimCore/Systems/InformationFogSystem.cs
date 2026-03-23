using System;
using System.Collections.Generic;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.T45.DEEP_DREAD.INFO_FOG.001: Information fog at distance.
// Market data for distant/unvisited nodes becomes stale over time.
// Players must physically visit nodes to refresh their intel.
// Near nodes (0-3 hops) = always fresh. Mid (4-5) = stale after 100 ticks.
// Deep (6+) = stale after 50 ticks.
public static class InformationFogSystem
{
    public static void Process(SimState state)
    {
        // Record current node visit tick.
        var playerNodeId = state.PlayerLocationNodeId;
        if (!string.IsNullOrEmpty(playerNodeId))
        {
            state.NodeLastVisitTick[playerNodeId] = state.Tick;
        }
    }

    /// <summary>
    /// Returns the staleness level for a node's market data.
    /// 0 = fresh, 1 = stale, 2 = very stale (show ?).
    /// Called by bridge queries to determine data visibility.
    /// </summary>
    public static int GetDataStaleness(SimState state, string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return 0; // STRUCTURAL: null guard

        // Player's current node is always fresh.
        if (string.Equals(nodeId, state.PlayerLocationNodeId, StringComparison.Ordinal)) return 0;

        // Compute hop distance from player start (not current location — uses BFS).
        int hops = ComputeHopsFromPlayer(state, nodeId);

        // Near range = always fresh.
        if (hops <= DeepDreadTweaksV0.FogNearHopsMax) return 0;

        // Check last visit time.
        int lastVisitTick = state.NodeLastVisitTick.TryGetValue(nodeId, out var t) ? t : -1; // STRUCTURAL: -1 = never visited
        int ticksSinceVisit = lastVisitTick < 0 ? int.MaxValue : state.Tick - lastVisitTick;

        // Mid range: stale after FogMidStaleTicks.
        if (hops <= DeepDreadTweaksV0.FogMidHopsMax)
        {
            return ticksSinceVisit > DeepDreadTweaksV0.FogMidStaleTicks ? 1 : 0; // STRUCTURAL: staleness threshold
        }

        // Deep range: stale faster, very stale at 2x.
        if (ticksSinceVisit > DeepDreadTweaksV0.FogDeepStaleTicks * 2) return 2; // STRUCTURAL: 2x for very stale
        if (ticksSinceVisit > DeepDreadTweaksV0.FogDeepStaleTicks) return 1; // STRUCTURAL: staleness threshold
        return 0;
    }

    /// <summary>
    /// BFS from player's current location to target node. Returns hop count.
    /// </summary>
    private static int ComputeHopsFromPlayer(SimState state, string nodeId)
    {
        var playerNodeId = state.PlayerLocationNodeId;
        if (string.IsNullOrEmpty(playerNodeId)) return int.MaxValue;
        if (string.Equals(playerNodeId, nodeId, StringComparison.Ordinal)) return 0;

        var visited = new HashSet<string>(StringComparer.Ordinal) { playerNodeId };
        var frontier = new List<string> { playerNodeId };
        int depth = 0;

        while (frontier.Count > 0)
        {
            depth++;
            var nextFrontier = new List<string>();
            foreach (var current in frontier)
            {
                foreach (var edge in state.Edges.Values)
                {
                    string adj = "";
                    if (edge.FromNodeId == current) adj = edge.ToNodeId;
                    else if (edge.ToNodeId == current) adj = edge.FromNodeId;
                    if (adj.Length > 0 && visited.Add(adj)) // STRUCTURAL: empty string guard
                    {
                        if (string.Equals(adj, nodeId, StringComparison.Ordinal)) return depth;
                        nextFrontier.Add(adj);
                    }
                }
            }
            frontier = nextFrontier;
        }
        return int.MaxValue;
    }
}
