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
    // Fixed-point scale for deterministic risk scoring (micro-units).
    // This is representation, not a gameplay knob.
    private const long RiskMicroScale = 1_000_000;

    // Structural constants (not gameplay knobs).
    private const int STRUCT_MIN_POSITIVE = 1; // STRUCTURAL: clamp for non-zero defaults and min tick rules
    private const int STRUCT_THOUSAND = 1000;  // STRUCTURAL: scale factor for milli-AU conversion and formatting math

    private const int DefaultMaxCandidates = 8;

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

        if (!TryPlanChoice(state, from, to, speedAuPerTick, maxCandidates: DefaultMaxCandidates, out var choice))
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

        var speed = speedAuPerTick > default(float) ? speedAuPerTick : STRUCT_MIN_POSITIVE;

        // Build deterministic adjacency: FromNodeId -> edges sorted by edge id.
        // Avoid LINQ allocations (GroupBy/ToDictionary/OrderBy/ToList) on this hot path.
        var outgoing = state.GetOutgoingEdgesByFromNodeDeterministic();

        GetRiskKnobsMicro(state, out var riskScalarMicro, out var riskTolMicro, out var useRiskScore);

        var candidates = GenerateCandidatesDeterministic(
            state,
            outgoing,
            fromNodeId,
            toNodeId,
            speed,
            maxCandidates,
            riskScalarMicro,
            riskTolMicro,
            useRiskScore);

        if (candidates.Count == 0) return false;

        var chosen = candidates[0];

        var tie = ComputeTieBreakReason(candidates, riskScalarMicro, riskTolMicro, useRiskScore);

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
        int maxCandidates,
        long riskScalarMicro,
        long riskTolMicro,
        bool useRiskScore)
    {
        var max = maxCandidates > default(int) ? maxCandidates : STRUCT_MIN_POSITIVE;

        // Hard bound for v0 to avoid path explosion; crafted tests are small.
        var maxHops = Math.Min(DefaultMaxCandidates, Math.Max(STRUCT_MIN_POSITIVE, state.Nodes.Count));

        var results = new List<RoutePlan>(capacity: Math.Min(max, DefaultMaxCandidates));
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
                    // Copy path lists without LINQ to reduce per-candidate overhead.
                    var nodeCopy = new List<string>(nodePath.Count);
                    for (int i = default(int); i < nodePath.Count; i++) nodeCopy.Add(nodePath[i]);

                    var edgeCopy = new List<string>(edgePath.Count);
                    for (int i = default(int); i < edgePath.Count; i++) edgeCopy.Add(edgePath[i]);

                    results.Add(new RoutePlan
                    {
                        FromNodeId = fromNodeId,
                        ToNodeId = toNodeId,
                        NodeIds = nodeCopy,
                        EdgeIds = edgeCopy,
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
                edgePath.RemoveAt(edgePath.Count - STRUCT_MIN_POSITIVE);
                nodePath.RemoveAt(nodePath.Count - STRUCT_MIN_POSITIVE);

                if (results.Count >= max) return;
            }
        }

        Dfs(fromNodeId, riskScore: default(int), totalTicks: default(int));

        // Deterministic ordering and tie-break:
        // - Legacy default (no override): fewer hops, then lower risk, then lex route_id.
        // - When risk knobs are overridden: lower (travel_ticks + scaled_risk_cost), then fewer hops, then lex route_id.
        // Avoid LINQ allocations by sorting in-place.
        if (!useRiskScore)
        {
            results.Sort((a, b) =>
            {
                int c = a.HopCount.CompareTo(b.HopCount);
                if (c != default) return c;

                c = a.RiskScore.CompareTo(b.RiskScore);
                if (c != default) return c;

                return string.CompareOrdinal(a.RouteId ?? "", b.RouteId ?? "");
            });
        }
        else
        {
            results.Sort((a, b) =>
            {
                int c = ComputeTotalScore(a, riskScalarMicro, riskTolMicro).CompareTo(ComputeTotalScore(b, riskScalarMicro, riskTolMicro));
                if (c != default) return c;

                c = a.HopCount.CompareTo(b.HopCount);
                if (c != default) return c;

                return string.CompareOrdinal(a.RouteId ?? "", b.RouteId ?? "");
            });
        }

        return results;
    }

    private static string ComputeTieBreakReason(List<RoutePlan> orderedCandidates, long riskScalarMicro, long riskTolMicro, bool useRiskScore)
    {
        if (orderedCandidates is null || orderedCandidates.Count <= STRUCT_MIN_POSITIVE) return "ONLY";

        var a = orderedCandidates[default(int)];
        var b = orderedCandidates[STRUCT_MIN_POSITIVE];

        if (!useRiskScore)
        {
            if (a.HopCount != b.HopCount) return "HOPS";
            if (a.RiskScore != b.RiskScore) return "RISK";
            if (!string.Equals(a.RouteId, b.RouteId, StringComparison.Ordinal)) return "ROUTE_ID";
            return "STABLE";
        }

        var sa = ComputeTotalScore(a, riskScalarMicro, riskTolMicro);
        var sb = ComputeTotalScore(b, riskScalarMicro, riskTolMicro);

        if (sa != sb) return "SCORE";
        if (a.HopCount != b.HopCount) return "HOPS";
        if (!string.Equals(a.RouteId, b.RouteId, StringComparison.Ordinal)) return "ROUTE_ID";

        return "STABLE";
    }

    private static void GetRiskKnobsMicro(SimState state, out long riskScalarMicro, out long riskTolMicro, out bool useRiskScore)
    {
        // Fixed-point conversion avoids floating point comparisons in ordering logic.
        // Only activates score-based ordering when the tweak values differ from defaults.
        if (state?.Tweaks is null)
        {
            riskScalarMicro = RiskMicroScale;
            riskTolMicro = RiskMicroScale;
            useRiskScore = false;
            return;
        }

        // Tweaks layer is responsible for defaulting (missing fields resolve to stable defaults).
        var scalar = state.Tweaks.RiskScalar;
        var tol = state.Tweaks.RoleRiskToleranceDefault;

        riskScalarMicro = ToMicroNonNegative(scalar, defaultMicro: RiskMicroScale);
        riskTolMicro = ToMicroPositive(tol, defaultMicro: RiskMicroScale);

        useRiskScore = !(riskScalarMicro == RiskMicroScale && riskTolMicro == RiskMicroScale);
    }

    private const long ZeroL = 0;

    private static long ToMicroNonNegative(double v, long defaultMicro)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) return defaultMicro;
        var micro = (long)Math.Round(v * (double)RiskMicroScale, MidpointRounding.AwayFromZero);
        if (micro < ZeroL) micro = ZeroL;
        return micro;
    }

    private static long ToMicroPositive(double v, long defaultMicro)
    {
        if (double.IsNaN(v) || double.IsInfinity(v)) return defaultMicro;
        var micro = (long)Math.Round(v * (double)RiskMicroScale, MidpointRounding.AwayFromZero);
        if (micro <= ZeroL) micro = defaultMicro; // failure-safe: avoid div0 and preserve legacy.
        return micro;
    }

    private static int ComputeTotalScore(RoutePlan p, long riskScalarMicro, long riskTolMicro)
    {
        long travel = p?.TotalTravelTicks ?? ZeroL;
        long risk = p?.RiskScore ?? ZeroL;

        if (travel < ZeroL) travel = ZeroL;
        if (risk < ZeroL) risk = ZeroL;

        long scaledRisk;
        if (riskScalarMicro <= ZeroL)
        {
            scaledRisk = ZeroL;
        }
        else
        {
            var denom = riskTolMicro > ZeroL ? riskTolMicro : RiskMicroScale;
            scaledRisk = (risk * riskScalarMicro) / denom;
            if (scaledRisk < ZeroL) scaledRisk = ZeroL;
        }

        long sum = travel + scaledRisk;
        if (sum > int.MaxValue) return int.MaxValue;
        if (sum < int.MinValue) return int.MinValue;
        return (int)sum;
    }

    private static string BuildRouteId(List<string> nodeIds)
    {
        if (nodeIds is null || nodeIds.Count == default) return "";
        return string.Join(">", nodeIds);
    }

    // Exposed helper for Slice 3 risk incident modeling.
    // Determinism: band is derived only from EdgeRiskScoreV0 (distance-derived milli-AU),
    // using fixed thresholds and no floating locale-sensitive formatting.
    public static int EdgeRiskBandV0(Edge e)
    {
        var s = EdgeRiskScoreV0(e);

        // Bands: 0=LOW,1=MED,2=HIGH,3=EXTREME
        // Thresholds are representation defaults for v0, not a tuning knob surface.
        if (s < SimCore.RiskModelV0.RiskBand0Max) return SimCore.RiskModelV0.BandLow;
        if (s < SimCore.RiskModelV0.RiskBand1Max) return SimCore.RiskModelV0.BandMed;
        if (s < SimCore.RiskModelV0.RiskBand2Max) return SimCore.RiskModelV0.BandHigh;
        return SimCore.RiskModelV0.BandExtreme;
    }

    private static int EdgeRiskScoreV0(Edge e)
    {
        // v0: deterministic integer risk proxy derived from distance (milli-AU).
        // This keeps ordering deterministic without relying on optional risk fields.
        var dist = e.Distance > default(float) ? e.Distance : STRUCT_MIN_POSITIVE;
        var milli = (int)Math.Round(dist * (float)STRUCT_THOUSAND, MidpointRounding.AwayFromZero);
        if (milli < default(int)) milli = default(int);
        return milli;
    }

    private static int EdgeTravelTicks(Edge e, float speedAuPerTick)
    {
        var speed = speedAuPerTick > default(float) ? speedAuPerTick : STRUCT_MIN_POSITIVE;
        var dist = e.Distance > default(float) ? e.Distance : STRUCT_MIN_POSITIVE;

        // ceil(dist / speed), min 1.
        var raw = dist / speed;
        var ticks = (int)Math.Ceiling(raw);
        if (ticks < 1) ticks = 1;
        return ticks;
    }
}
