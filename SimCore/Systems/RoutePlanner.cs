using System;
using System.Collections.Generic;
using System.Linq;
using SimCore.Entities;

namespace SimCore.Systems;

/// <summary>
/// Deterministic lane-route planning over the current world graph.
/// Stability rules:
/// - Uses integer "travel ticks" cost per edge: ceil(distance / speed), min 1.
/// - Chooses lowest total cost path.
/// - Tie-breaks by (cost, nodeId) when selecting next frontier node.
/// - Tie-breaks by edgeId order when exploring outgoing edges.
/// </summary>
public static class RoutePlanner
{
    public sealed class RoutePlan
    {
        public string FromNodeId { get; init; } = "";
        public string ToNodeId { get; init; } = "";
        public List<string> NodeIds { get; init; } = new();
        public List<string> EdgeIds { get; init; } = new();
        public int TotalTravelTicks { get; init; } = 0;
    }

    public static bool TryPlan(SimState state, string fromNodeId, string toNodeId, float speedAuPerTick, out RoutePlan plan)
    {
        plan = new RoutePlan { FromNodeId = fromNodeId ?? "", ToNodeId = toNodeId ?? "" };

        if (state is null) return false;
        if (string.IsNullOrWhiteSpace(fromNodeId) || string.IsNullOrWhiteSpace(toNodeId)) return false;
        if (!state.Nodes.ContainsKey(fromNodeId)) return false;
        if (!state.Nodes.ContainsKey(toNodeId)) return false;

        if (fromNodeId == toNodeId)
        {
            plan = new RoutePlan
            {
                FromNodeId = fromNodeId,
                ToNodeId = toNodeId,
                NodeIds = new List<string> { fromNodeId },
                EdgeIds = new List<string>(),
                TotalTravelTicks = 0
            };
            return true;
        }

        var speed = speedAuPerTick > 0f ? speedAuPerTick : 1f;

        // Build deterministic adjacency: FromNodeId -> edges sorted by edge id.
        var outgoing = state.Edges.Values
            .GroupBy(e => e.FromNodeId ?? "")
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(e => e.Id, StringComparer.Ordinal).ToList(),
                StringComparer.Ordinal);

        // Dijkstra with deterministic frontier selection (no PriorityQueue to avoid instability).
        var dist = new Dictionary<string, int>(StringComparer.Ordinal);
        var prevNode = new Dictionary<string, string>(StringComparer.Ordinal);
        var prevEdge = new Dictionary<string, string>(StringComparer.Ordinal);
        var open = new List<string>();

        dist[fromNodeId] = 0;
        open.Add(fromNodeId);

        while (open.Count > 0)
        {
            // Pick the open node with smallest (dist, nodeId) deterministically.
            var bestIdx = 0;
            var bestNode = open[0];
            var bestCost = dist.TryGetValue(bestNode, out var c0) ? c0 : int.MaxValue;

            for (var i = 1; i < open.Count; i++)
            {
                var n = open[i];
                var cost = dist.TryGetValue(n, out var c) ? c : int.MaxValue;

                if (cost < bestCost || (cost == bestCost && string.CompareOrdinal(n, bestNode) < 0))
                {
                    bestIdx = i;
                    bestNode = n;
                    bestCost = cost;
                }
            }

            open.RemoveAt(bestIdx);

            if (bestNode == toNodeId) break;

            if (!outgoing.TryGetValue(bestNode, out var edges)) continue;

            foreach (var e in edges)
            {
                var to = e.ToNodeId ?? "";
                if (string.IsNullOrWhiteSpace(to)) continue;
                if (!state.Nodes.ContainsKey(to)) continue;

                var edgeTicks = EdgeTravelTicks(e, speed);
                var nextCostLong = (long)bestCost + (long)edgeTicks;
                if (nextCostLong > int.MaxValue) continue;

                var nextCost = (int)nextCostLong;

                if (!dist.TryGetValue(to, out var oldCost) || nextCost < oldCost)
                {
                    dist[to] = nextCost;
                    prevNode[to] = bestNode;
                    prevEdge[to] = e.Id ?? "";

                    if (!open.Contains(to))
                        open.Add(to);
                }
                // If equal cost, do not overwrite.
                // Deterministic tie-break is achieved by stable exploration order + stable node selection.
            }
        }

        if (!dist.ContainsKey(toNodeId)) return false;

        // Reconstruct path.
        var nodePath = new List<string>();
        var edgePath = new List<string>();
        var cur = toNodeId;

        nodePath.Add(cur);

        while (cur != fromNodeId)
        {
            if (!prevNode.TryGetValue(cur, out var p)) return false;
            if (!prevEdge.TryGetValue(cur, out var pe)) return false;

            edgePath.Add(pe);
            cur = p;
            nodePath.Add(cur);
        }

        nodePath.Reverse();
        edgePath.Reverse();

        plan = new RoutePlan
        {
            FromNodeId = fromNodeId,
            ToNodeId = toNodeId,
            NodeIds = nodePath,
            EdgeIds = edgePath,
            TotalTravelTicks = dist[toNodeId]
        };

        return true;
    }

    private static int EdgeTravelTicks(Edge e, float speedAuPerTick)
    {
        var speed = speedAuPerTick > 0f ? speedAuPerTick : 1f;
        var dist = e.Distance > 0f ? e.Distance : 1f;

        // ceil(dist / speed), min 1.
        var raw = dist / speed;
        var ticks = (int)Math.Ceiling(raw);
        if (ticks < 1) ticks = 1;
        return ticks;
    }
}
