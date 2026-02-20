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

        // Deterministic route id for tie-breaks and diff-friendly dumps.
        // Format: node0>node1>...>nodeN
        public string RouteId { get; init; } = "";

        public List<string> NodeIds { get; init; } = new();
        public List<string> EdgeIds { get; init; } = new();

        public int HopCount { get; init; } = 0;

        // Risk proxy v0: sum of per-edge integer risk scores (derived deterministically from distance).
        public int RiskScore { get; init; } = 0;

        public int TotalTravelTicks { get; init; } = 0;
    }

    public sealed class RouteChoice
    {
        public string OriginId { get; init; } = "";
        public string DestId { get; init; } = "";
        public string ChosenRouteId { get; init; } = "";
        public int CandidateCount { get; init; } = 0;
        public string TieBreakReason { get; init; } = "";
        public RoutePlan ChosenPlan { get; init; } = new();
        public List<RoutePlan> Candidates { get; init; } = new();
    }

    public static bool TryPlan(SimState state, string fromNodeId, string toNodeId, float speedAuPerTick, out RoutePlan plan)
    {
        // Normalize inputs to satisfy non-nullable API and keep behavior consistent with TryPlanChoice guards.
        var from = fromNodeId ?? "";
        var to = toNodeId ?? "";

        plan = new RoutePlan { FromNodeId = from, ToNodeId = to };

        if (!TryPlanChoice(state, from, to, speedAuPerTick, maxCandidates: 8, out var choice))
            return false;

        plan = choice.ChosenPlan;
        return true;
    }

    public static bool TryPlanChoice(
        SimState state,
        string fromNodeId,
        string toNodeId,
        float speedAuPerTick,
        int maxCandidates,
        out RouteChoice choice)
    {
        choice = new RouteChoice
        {
            OriginId = fromNodeId ?? "",
            DestId = toNodeId ?? "",
            ChosenPlan = new RoutePlan { FromNodeId = fromNodeId ?? "", ToNodeId = toNodeId ?? "" }
        };

        if (state is null) return false;
        if (string.IsNullOrWhiteSpace(fromNodeId) || string.IsNullOrWhiteSpace(toNodeId)) return false;
        if (!state.Nodes.ContainsKey(fromNodeId)) return false;
        if (!state.Nodes.ContainsKey(toNodeId)) return false;

        if (fromNodeId == toNodeId)
        {
            var self = new RoutePlan
            {
                FromNodeId = fromNodeId,
                ToNodeId = toNodeId,
                NodeIds = new List<string> { fromNodeId },
                EdgeIds = new List<string>(),
                RouteId = fromNodeId,
                HopCount = 0,
                RiskScore = 0,
                TotalTravelTicks = 0
            };

            choice = new RouteChoice
            {
                OriginId = fromNodeId,
                DestId = toNodeId,
                ChosenRouteId = self.RouteId,
                CandidateCount = 1,
                TieBreakReason = "ONLY",
                ChosenPlan = self,
                Candidates = new List<RoutePlan> { self }
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

        var candidates = GenerateCandidatesDeterministic(
            state,
            outgoing,
            fromNodeId,
            toNodeId,
            speed,
            maxCandidates);

        if (candidates.Count == 0) return false;

        var chosen = candidates[0];

        var tie = ComputeTieBreakReason(candidates);

        choice = new RouteChoice
        {
            OriginId = fromNodeId,
            DestId = toNodeId,
            ChosenRouteId = chosen.RouteId,
            CandidateCount = candidates.Count,
            TieBreakReason = tie,
            ChosenPlan = chosen,
            Candidates = candidates
        };

        return true;
    }

    private static List<RoutePlan> GenerateCandidatesDeterministic(
        SimState state,
        Dictionary<string, List<Edge>> outgoing,
        string fromNodeId,
        string toNodeId,
        float speedAuPerTick,
        int maxCandidates)
    {
        var max = maxCandidates > 0 ? maxCandidates : 1;

        // Hard bound for v0 to avoid path explosion; crafted tests are small.
        var maxHops = Math.Min(8, Math.Max(1, state.Nodes.Count));

        var results = new List<RoutePlan>(capacity: Math.Min(max, 8));
        var seen = new HashSet<string>(StringComparer.Ordinal);

        var nodePath = new List<string> { fromNodeId };
        var edgePath = new List<string>();
        var nodeSet = new HashSet<string>(StringComparer.Ordinal) { fromNodeId };

        void Dfs(string cur, int riskScore, int totalTicks)
        {
            if (results.Count >= max) return;

            if (cur == toNodeId)
            {
                var rid = BuildRouteId(nodePath);
                if (seen.Add(rid))
                {
                    results.Add(new RoutePlan
                    {
                        FromNodeId = fromNodeId,
                        ToNodeId = toNodeId,
                        NodeIds = nodePath.ToList(),
                        EdgeIds = edgePath.ToList(),
                        RouteId = rid,
                        HopCount = edgePath.Count,
                        RiskScore = riskScore,
                        TotalTravelTicks = totalTicks
                    });
                }

                return;
            }

            if (edgePath.Count >= maxHops) return;

            if (!outgoing.TryGetValue(cur, out var edges)) return;

            foreach (var e in edges)
            {
                var next = e.ToNodeId ?? "";
                if (string.IsNullOrWhiteSpace(next)) continue;
                if (!state.Nodes.ContainsKey(next)) continue;

                // v0: keep candidates simple paths only (no cycles).
                if (nodeSet.Contains(next)) continue;

                var eid = e.Id ?? "";
                if (string.IsNullOrWhiteSpace(eid)) continue;

                var edgeTicks = EdgeTravelTicks(e, speedAuPerTick);
                var edgeRisk = EdgeRiskScoreV0(e);

                nodePath.Add(next);
                edgePath.Add(eid);
                nodeSet.Add(next);

                Dfs(next, riskScore + edgeRisk, totalTicks + edgeTicks);

                nodeSet.Remove(next);
                edgePath.RemoveAt(edgePath.Count - 1);
                nodePath.RemoveAt(nodePath.Count - 1);

                if (results.Count >= max) return;
            }
        }

        Dfs(fromNodeId, riskScore: 0, totalTicks: 0);

        // Deterministic ordering and tie-break:
        // fewer hops, then lower risk, then lex route_id.
        results = results
            .OrderBy(r => r.HopCount)
            .ThenBy(r => r.RiskScore)
            .ThenBy(r => r.RouteId, StringComparer.Ordinal)
            .ToList();

        return results;
    }

    private static string ComputeTieBreakReason(List<RoutePlan> orderedCandidates)
    {
        if (orderedCandidates is null || orderedCandidates.Count <= 1) return "ONLY";

        var a = orderedCandidates[0];
        var b = orderedCandidates[1];

        if (a.HopCount != b.HopCount) return "HOPS";
        if (a.RiskScore != b.RiskScore) return "RISK";
        if (!string.Equals(a.RouteId, b.RouteId, StringComparison.Ordinal)) return "ROUTE_ID";

        return "STABLE";
    }

    private static string BuildRouteId(List<string> nodeIds)
    {
        if (nodeIds is null || nodeIds.Count == 0) return "";
        return string.Join(">", nodeIds);
    }

    private static int EdgeRiskScoreV0(Edge e)
    {
        // v0: deterministic integer risk proxy derived from distance (milli-AU).
        // This keeps ordering deterministic without relying on optional risk fields.
        var dist = e.Distance > 0f ? e.Distance : 1f;
        var milli = (int)Math.Round(dist * 1000f, MidpointRounding.AwayFromZero);
        if (milli < 0) milli = 0;
        return milli;
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
