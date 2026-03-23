using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.T48.TELEMETRY.SESSION_WRITER.001: Dev telemetry — periodic economy health snapshots.
// Snapshots are stored in SimState.TelemetrySnapshots (capped ring buffer).
public static class TelemetrySystem
{
    private sealed class Scratch
    {
        public readonly List<string> SortedKeys = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();

    public static void Process(SimState state)
    {
        if (state is null) return; // STRUCTURAL: null guard
        if (TelemetryTweaksV0.SnapshotIntervalTicks <= 0) return; // STRUCTURAL: disabled guard
        if (state.Tick % TelemetryTweaksV0.SnapshotIntervalTicks != 0) return; // STRUCTURAL: cycle check

        var scratch = s_scratch.GetOrCreateValue(state);

        // Count active NPC trade routes (NPCs that are moving with cargo).
        int activeNpcRoutes = 0; // STRUCTURAL: init counter
        int totalNpcIdleTicks = 0; // STRUCTURAL: init counter
        int npcCount = 0; // STRUCTURAL: init counter
        foreach (var fleet in state.Fleets.Values)
        {
            if (string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal)) continue;
            if (fleet.IsLatticeDrone) continue;
            npcCount++;
            if (fleet.IsMoving && fleet.Cargo is not null && fleet.Cargo.Count > 0)
                activeNpcRoutes++;
            // Approximate idle ticks: if not moving and has no cargo, assume idle.
            if (!fleet.IsMoving && (fleet.Cargo is null || fleet.Cargo.Count == 0))
                totalNpcIdleTicks += TelemetryTweaksV0.SnapshotIntervalTicks;
        }
        int avgNpcIdleTicks = npcCount > 0 ? totalNpcIdleTicks / npcCount : 0; // STRUCTURAL: div guard

        // Goods velocity: total inventory across all markets (proxy for trade volume).
        int goodsVelocity = 0; // STRUCTURAL: init counter
        long totalPriceVariance = 0; // STRUCTURAL: init counter
        int priceCount = 0; // STRUCTURAL: init counter
        int stockoutCount = 0; // STRUCTURAL: init counter

        var sortedMarketKeys = scratch.SortedKeys;
        sortedMarketKeys.Clear();
        foreach (var k in state.Markets.Keys) sortedMarketKeys.Add(k);
        sortedMarketKeys.Sort(StringComparer.Ordinal);

        foreach (var mktId in sortedMarketKeys)
        {
            if (!state.Markets.TryGetValue(mktId, out var market)) continue;
            foreach (var kv in market.Inventory)
            {
                goodsVelocity += kv.Value;
                if (kv.Value == 0) stockoutCount++; // STRUCTURAL: zero-stock check

                int price = market.GetPrice(kv.Key);
                if (price > 0)
                {
                    totalPriceVariance += price;
                    priceCount++;
                }
            }
        }
        int priceVarianceAvg = priceCount > 0 ? (int)(totalPriceVariance / priceCount) : 0; // STRUCTURAL: div guard

        // Credit inflation: total credits in economy (player + NPC).
        long totalCredits = state.PlayerCredits;
        // NPC fleets don't hold credits in this model, so player credits is the primary measure.

        var snapshot = new TelemetrySnapshot
        {
            Tick = state.Tick,
            ActiveNpcTradeRoutes = activeNpcRoutes,
            AvgNpcIdleTicks = avgNpcIdleTicks,
            GoodsVelocity = goodsVelocity,
            PriceVarianceAvg = priceVarianceAvg,
            StockoutCount = stockoutCount,
            CreditInflation = totalCredits,
            PlayerCredits = state.PlayerCredits,
            PlayerNodesVisited = state.PlayerVisitedNodeIds?.Count ?? 0, // STRUCTURAL: null guard
        };

        state.TelemetrySnapshots ??= new List<TelemetrySnapshot>();
        state.TelemetrySnapshots.Add(snapshot);

        // Cap at MaxSnapshots (remove oldest).
        while (state.TelemetrySnapshots.Count > TelemetryTweaksV0.MaxSnapshots)
        {
            state.TelemetrySnapshots.RemoveAt(0); // STRUCTURAL: remove oldest
        }
    }

    // GATE.T51.TELEMETRY.LOCAL_STORE.001: Log a telemetry event.
    // Event types: "trade", "combat", "death", "dock", "mission"
    public static void LogEvent(SimState state, string eventType, string nodeId, string detail)
    {
        if (state is null) return; // STRUCTURAL: null guard
        state.TelemetryEvents ??= new List<TelemetryEvent>();

        state.TelemetryEvents.Add(new TelemetryEvent
        {
            Tick = state.Tick,
            EventType = eventType,
            NodeId = nodeId,
            Detail = detail,
            Credits = state.PlayerCredits,
        });

        // Cap at 500 events per session to prevent unbounded growth.
        while (state.TelemetryEvents.Count > TelemetryTweaksV0.MaxEvents) // STRUCTURAL: cap
        {
            state.TelemetryEvents.RemoveAt(0); // STRUCTURAL: remove oldest
        }
    }
}
