using System;
using System.Collections.Generic;
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

    private sealed class Scratch
    {
        public readonly List<InFlightTransfer> Due = new();
        public readonly Dictionary<string, int> DeliveredByLane = new(StringComparer.Ordinal);
        public readonly HashSet<string> DueIds = new(StringComparer.Ordinal);
        public readonly List<string> SortedLaneIds = new();
        public readonly Dictionary<string, List<InFlightTransfer>> LaneGroups = new(StringComparer.Ordinal);
        public readonly List<string> SortedGroupKeys = new();
        public readonly Dictionary<string, int> QueuedByLane = new(StringComparer.Ordinal);
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();

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

        // Duplicate-ID guard: foreach avoids LINQ delegate allocation; N is bounded by active logistics jobs.
        foreach (var t in state.InFlightTransfers)
            if (string.Equals(t.Id, transferId, StringComparison.Ordinal)) return false;

        // GATE.S7.INSTABILITY_EFFECTS.LANE.001: Void phase severs lanes — reject transfer.
        int srcPhase = Tweaks.InstabilityTweaksV0.GetPhaseIndex(fromNode.InstabilityLevel);
        int dstPhase = Tweaks.InstabilityTweaksV0.GetPhaseIndex(toNode.InstabilityLevel);
        if (srcPhase >= 4 || dstPhase >= 4) return false; // Void = lane severed

        var removed = InventoryLedger.TryRemoveMarket(fromMarket.Inventory, goodId, quantity);
        if (!removed) return false;

        var delayTicks = ComputeDelayTicks(edge);

        // GATE.S7.INSTABILITY_EFFECTS.LANE.001: Phase-scaled lane delay.
        // Shimmer=+10%, Drift=+20%, Fracture=+40%. Uses max phase of both endpoints.
        int maxPhase = Math.Max(srcPhase, dstPhase);
        if (maxPhase >= 1)
        {
            int bonusPct = maxPhase switch
            {
                >= 3 => Tweaks.InstabilityTweaksV0.FractureLaneDelayPct,
                2 => Tweaks.InstabilityTweaksV0.DriftLaneDelayPct,
                _ => Tweaks.InstabilityTweaksV0.ShimmerLaneDelayPct,
            };
            delayTicks += Math.Max(1, delayTicks * bonusPct / 100);
        }

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
        var scratch = s_scratch.GetOrCreateValue(state);

        // Collect due transfers deterministically.
        var due = scratch.Due;
        due.Clear();
        foreach (var x in state.InFlightTransfers)
        {
            if (x.ArriveTick <= now) due.Add(x);
        }
        if (due.Count == 0) return;

        // Sort: ArriveTick, then EdgeId (Ordinal), then Id (Ordinal).
        due.Sort((a, b) =>
        {
            int c = a.ArriveTick.CompareTo(b.ArriveTick);
            if (c != 0) return c;
            c = StringComparer.Ordinal.Compare(a.EdgeId, b.EdgeId);
            if (c != 0) return c;
            return StringComparer.Ordinal.Compare(a.Id, b.Id);
        });

        // Capacity scarcity v0:
        // For each lane per tick, deliver up to edge.TotalCapacity (if > 0).
        // Overflow is queued deterministically by setting ArriveTick = now + 1.
        // Sustained overload creates multi-tick delay via repeated next-tick deferrals.
        var deliveredByLane = scratch.DeliveredByLane;
        deliveredByLane.Clear();

        // Group by EdgeId manually.
        var laneGroups = scratch.LaneGroups;
        foreach (var lg in laneGroups.Values) lg.Clear();
        foreach (var t in due)
        {
            if (!laneGroups.TryGetValue(t.EdgeId, out var list))
            {
                list = new List<InFlightTransfer>();
                laneGroups[t.EdgeId] = list;
            }
            list.Add(t);
        }

        var sortedGroupKeys = scratch.SortedGroupKeys;
        sortedGroupKeys.Clear();
        foreach (var k in laneGroups.Keys)
        {
            if (laneGroups[k].Count > 0) sortedGroupKeys.Add(k);
        }
        sortedGroupKeys.Sort(StringComparer.Ordinal);

        foreach (var laneId in sortedGroupKeys)
        {
            var group = laneGroups[laneId];

            var capacity = int.MaxValue;
            if (state.Edges.TryGetValue(laneId, out var edge))
            {
                if (edge.TotalCapacity > default(int)) capacity = edge.TotalCapacity;
                else
                {
                    var k = state.Tweaks?.DefaultLaneCapacityK ?? default(int);
                    if (k > default(int)) capacity = k;
                }
            }

            var remaining = capacity;

            foreach (var t in group)
            {
                if (t.Quantity <= default(int)) continue;

                if (remaining <= default(int))
                {
                    t.ArriveTick = checked(now + 1);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(t.ToMarketId))
                {
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
                    t.ArriveTick = checked(now + 1);
                }
            }
        }

        // Remove transfers that are fully delivered (Quantity <= 0) and were due this tick.
        var dueIds = scratch.DueIds;
        dueIds.Clear();
        foreach (var x in due) dueIds.Add(x.Id);
        for (int i = state.InFlightTransfers.Count - 1; i >= 0; i--)
        {
            var x = state.InFlightTransfers[i];
            if (dueIds.Contains(x.Id) && x.Quantity <= 0)
                state.InFlightTransfers.RemoveAt(i);
        }

        // Pre-compute queued amounts per lane in a single pass (avoids O(n²) nested loop).
        var queuedByLane = deliveredByLane; // Reuse dict after report — clear and repurpose.
        // deliveredByLane is consumed below, so save values first by building report inline.

        // Emit deterministic lane utilization report sorted by lane_id.
        var laneIds = scratch.SortedLaneIds;
        laneIds.Clear();
        foreach (var k in state.Edges.Keys) laneIds.Add(k);
        laneIds.Sort(StringComparer.Ordinal);

        // Single-pass queued tally.
        var queuedTally = scratch.QueuedByLane;
        queuedTally.Clear();
        foreach (var x in state.InFlightTransfers)
        {
            if (x.Quantity > default(int))
                queuedTally[x.EdgeId] = queuedTally.GetValueOrDefault(x.EdgeId) + x.Quantity;
        }

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

            int queued = queuedTally.GetValueOrDefault(laneId);

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
