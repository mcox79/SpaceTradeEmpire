using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SimCore.Entities;

namespace SimCore.Systems;

/// <summary>
/// Slice 1: lane flow with deterministic delay arrivals.
/// Delay is computed as Ceil(edge.Distance) ticks, clamped to >= 1.
/// </summary>
public static class LaneFlowSystem
{
    private sealed class PerStateReport
    {
        public long LastTick;
        public string LastLaneUtilizationReport = "";
    }

    private static readonly ConditionalWeakTable<SimState, PerStateReport> _reports = new();

    public static string GetLastLaneUtilizationReport(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        return _reports.TryGetValue(state, out var r) ? (r.LastLaneUtilizationReport ?? "") : "";
    }

    public static bool TryEnqueueTransfer(
        SimState state,
        string fromNodeId,
        string toNodeId,
        string goodId,
        int quantity,
        string transferId)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (string.IsNullOrWhiteSpace(fromNodeId)) return false;
        if (string.IsNullOrWhiteSpace(toNodeId)) return false;
        if (fromNodeId == toNodeId) return false;
        if (string.IsNullOrWhiteSpace(goodId)) return false;
        if (quantity <= 0) return false;
        if (string.IsNullOrWhiteSpace(transferId)) return false;

        if (!MapQueries.TryGetEdgeId(state, fromNodeId, toNodeId, out var edgeId)) return false;
        if (!state.Edges.TryGetValue(edgeId, out var edge)) return false;

        if (!state.Nodes.TryGetValue(fromNodeId, out var fromNode)) return false;
        if (!state.Nodes.TryGetValue(toNodeId, out var toNode)) return false;

        var fromMarketId = fromNode.MarketId ?? "";
        var toMarketId = toNode.MarketId ?? "";
        if (string.IsNullOrWhiteSpace(fromMarketId)) return false;
        if (string.IsNullOrWhiteSpace(toMarketId)) return false;

        if (!state.Markets.TryGetValue(fromMarketId, out var fromMarket)) return false;
        if (!state.Markets.TryGetValue(toMarketId, out var toMarket)) return false;

        if (state.InFlightTransfers.Any(x => string.Equals(x.Id, transferId, StringComparison.Ordinal)))
        {
            return false;
        }

        var removed = InventoryLedger.TryRemoveMarket(fromMarket.Inventory, goodId, quantity);
        if (!removed) return false;

        var delayTicks = ComputeDelayTicks(edge);
        var departTick = state.Tick;
        var arriveTick = checked(departTick + delayTicks);

        state.InFlightTransfers.Add(new InFlightTransfer
        {
            Id = transferId,
            EdgeId = edgeId,
            FromNodeId = fromNodeId,
            ToNodeId = toNodeId,
            FromMarketId = fromMarketId,
            ToMarketId = toMarketId,
            GoodId = goodId,
            Quantity = quantity,
            DepartTick = departTick,
            ArriveTick = arriveTick
        });

        return true;
    }

    public static void Process(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (state.InFlightTransfers.Count == 0) return;

        var now = state.Tick;

        // Collect due transfers deterministically.
        var due = state.InFlightTransfers
            .Where(x => x.ArriveTick <= now)
            .OrderBy(x => x.ArriveTick)
            .ThenBy(x => x.EdgeId, StringComparer.Ordinal)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .ToList();

        if (due.Count == 0) return;

        // Capacity scarcity v0:
        // For each lane per tick, deliver up to edge.TotalCapacity (if > 0).
        // Overflow is queued deterministically by setting ArriveTick = now + 1.
        // Sustained overload creates multi-tick delay via repeated next-tick deferrals.
        var deliveredByLane = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var laneGroup in due.GroupBy(x => x.EdgeId, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var laneId = laneGroup.Key;

            var capacity = int.MaxValue;
            if (state.Edges.TryGetValue(laneId, out var edge))
            {
                // TotalCapacity > 0 is explicit.
                // TotalCapacity <= 0 uses tweak default if > 0, else unlimited (preserves pre-migration behavior).
                if (edge.TotalCapacity > 0) capacity = edge.TotalCapacity;
                else
                {
                    var k = state.Tweaks?.DefaultLaneCapacityK ?? 0;
                    if (k > 0) capacity = k;
                }
            }

            var remaining = capacity;

            // laneGroup preserves due's ordering (already ordered by ArriveTick, EdgeId, Id).
            foreach (var t in laneGroup)
            {
                if (t.Quantity <= default(int)) continue;

                if (remaining <= default(int))
                {
                    // Fully queued.
                    t.ArriveTick = checked(now + 1);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(t.ToMarketId))
                {
                    // Invalid destination; keep deterministic behavior: drop from due by marking queued and letting it retry next tick.
                    t.ArriveTick = checked(now + 1);
                    continue;
                }

                if (!state.Markets.TryGetValue(t.ToMarketId, out var toMarket))
                {
                    t.ArriveTick = checked(now + 1);
                    continue;
                }

                var deliverQty = Math.Min(t.Quantity, remaining);
                if (deliverQty <= 0)
                {
                    t.ArriveTick = checked(now + 1);
                    continue;
                }

                InventoryLedger.AddMarket(toMarket.Inventory, t.GoodId, deliverQty);

                t.Quantity -= deliverQty;
                remaining -= deliverQty;

                if (!deliveredByLane.TryGetValue(laneId, out var cur)) cur = 0;
                deliveredByLane[laneId] = cur + deliverQty;

                if (t.Quantity > 0)
                {
                    // Partial fill; queue remainder deterministically.
                    t.ArriveTick = checked(now + 1);
                }
            }
        }

        // Remove transfers that are fully delivered (Quantity <= 0) and were due this tick.
        var dueIds = new HashSet<string>(due.Select(x => x.Id), StringComparer.Ordinal);
        state.InFlightTransfers.RemoveAll(x => dueIds.Contains(x.Id) && x.Quantity <= 0);

        // Emit deterministic lane utilization report sorted by lane_id.
        var laneIds = state.Edges.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();
        var lines = new List<string>(capacity: 4 + laneIds.Count)
        {
            "LANE_UTILIZATION_REPORT_V0",
            $"tick={now}",
            "lane_id|delivered|capacity|queued"
        };

        foreach (var laneId in laneIds)
        {
            var delivered = deliveredByLane.TryGetValue(laneId, out var d) ? d : default(int);

            var cap = int.MaxValue;
            if (state.Edges.TryGetValue(laneId, out var e))
            {
                if (e.TotalCapacity > 0) cap = e.TotalCapacity;
                else
                {
                    var k = state.Tweaks?.DefaultLaneCapacityK ?? default(int);
                    if (k > default(int)) cap = k;
                }
            }

            var queued = state.InFlightTransfers
                .Where(x => string.Equals(x.EdgeId, laneId, StringComparison.Ordinal))
                .Sum(x => Math.Max(default(int), x.Quantity));

            var capText = cap == int.MaxValue ? "inf" : cap.ToString();
            lines.Add($"{laneId}|{delivered}|{capText}|{queued}");
        }

        var report = string.Join("\n", lines) + "\n";
        var per = _reports.GetOrCreateValue(state);
        per.LastTick = now;
        per.LastLaneUtilizationReport = report;
    }

    private static int ComputeDelayTicks(Edge edge)
    {
        var d = edge.Distance;
        if (float.IsNaN(d) || float.IsInfinity(d)) return 1;
        if (d <= 0f) return 1;

        var ticks = (int)MathF.Ceiling(d);
        return Math.Max(1, ticks);
    }
}
