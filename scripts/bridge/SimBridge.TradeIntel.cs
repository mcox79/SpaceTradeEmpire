#nullable enable

using Godot;
using SimCore;
using SimCore.Systems;
using SimCore.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceTradeEmpire.Bridge;

// GATE.S10.TRADE_INTEL.BRIDGE.001: Trade intel bridge queries.
public partial class SimBridge
{
    private Godot.Collections.Array _cachedTradeRoutesV0 = new();
    private Godot.Collections.Array _cachedPriceIntelV0 = new();

    // GATE.S10.TRADE_INTEL.BRIDGE.001
    /// <summary>
    /// Returns all discovered trade routes from state.Intel.TradeRoutes.
    /// Each dict: route_id, source_node_id, dest_node_id, good_id,
    ///            estimated_profit_per_unit, discovered_tick, last_validated_tick, status.
    /// Sorted by good_id then route_id for determinism.
    /// Nonblocking: returns last cached snapshot if read lock is unavailable.
    /// </summary>
    public Godot.Collections.Array GetTradeRoutesV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            if (state.Intel is null)
            {
                lock (_snapshotLock) { _cachedTradeRoutesV0 = arr; }
                return;
            }

            var routes = state.Intel.TradeRoutes.Values
                .OrderBy(r => r.GoodId, StringComparer.Ordinal)
                .ThenBy(r => r.RouteId, StringComparer.Ordinal);

            foreach (var route in routes)
            {
                var d = new Godot.Collections.Dictionary
                {
                    ["route_id"]                   = route.RouteId,
                    ["source_node_id"]              = route.SourceNodeId,
                    ["dest_node_id"]                = route.DestNodeId,
                    ["good_id"]                     = route.GoodId,
                    ["estimated_profit_per_unit"]   = route.EstimatedProfitPerUnit,
                    ["discovered_tick"]             = route.DiscoveredTick,
                    ["last_validated_tick"]         = route.LastValidatedTick,
                    ["status"]                      = route.Status.ToString(),
                };
                arr.Add(d);
            }

            lock (_snapshotLock) { _cachedTradeRoutesV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedTradeRoutesV0; }
    }

    // GATE.S10.TRADE_INTEL.BRIDGE.001
    /// <summary>
    /// Returns price observations for the given node.
    /// Filters state.Intel.Observations by keys starting with "nodeId|".
    /// Each dict: good_id, buy_price, sell_price, mid_price, observed_tick, inventory_qty.
    /// Sorted by good_id.
    /// Nonblocking: returns last cached snapshot if read lock is unavailable.
    /// </summary>
    public Godot.Collections.Array GetPriceIntelV0(string nodeId)
    {
        var prefix = (nodeId ?? "") + "|";

        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            if (state.Intel is null)
            {
                lock (_snapshotLock) { _cachedPriceIntelV0 = arr; }
                return;
            }

            var observations = state.Intel.Observations
                .Where(kv => kv.Key.StartsWith(prefix, StringComparison.Ordinal))
                .Select(kv =>
                {
                    // Key format: marketId|goodId  — extract goodId after the first '|'
                    var separatorIdx = kv.Key.IndexOf('|', StringComparison.Ordinal);
                    var goodId = separatorIdx >= 0 ? kv.Key[(separatorIdx + 1)..] : kv.Key;
                    return (goodId, obs: kv.Value);
                })
                .OrderBy(x => x.goodId, StringComparer.Ordinal);

            foreach (var (goodId, obs) in observations)
            {
                var d = new Godot.Collections.Dictionary
                {
                    ["good_id"]       = goodId,
                    ["buy_price"]     = obs.ObservedBuyPrice,
                    ["sell_price"]    = obs.ObservedSellPrice,
                    ["mid_price"]     = obs.ObservedMidPrice,
                    ["observed_tick"] = obs.ObservedTick,
                    ["inventory_qty"] = obs.ObservedInventoryQty,
                };
                arr.Add(d);
            }

            lock (_snapshotLock) { _cachedPriceIntelV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedPriceIntelV0; }
    }

    // GATE.S10.TRADE_INTEL.BRIDGE.001
    /// <summary>
    /// Returns the player's current scanner range (0, 1, or 2 hops) based on unlocked techs.
    /// Delegates to IntelSystem.GetScanRange(state).
    /// </summary>
    public int GetScannerRangeV0()
    {
        int range = 0;
        TryExecuteSafeRead(state =>
        {
            range = IntelSystem.GetScanRange(state);
        });
        return range;
    }

    // GATE.S11.GAME_FEEL.PRICE_HISTORY.001: Price history query for trend charts.
    private Godot.Collections.Array _cachedPriceHistoryV0 = new();

    /// <summary>
    /// Returns the last 10 price history entries for a given node+good pair.
    /// Each dict: buy_price (int), sell_price (int), tick (long).
    /// Sorted by tick ascending (oldest first).
    /// Nonblocking: returns last cached snapshot if read lock is unavailable.
    /// </summary>
    public Godot.Collections.Array GetPriceHistoryV0(string nodeId, string goodId)
    {
        var nId = nodeId ?? "";
        var gId = goodId ?? "";

        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            if (state.Intel is null || state.Intel.PriceHistory.Count == 0)
            {
                lock (_snapshotLock) { _cachedPriceHistoryV0 = arr; }
                return;
            }

            // Filter matching entries, take last 10 (list is chronologically ordered).
            var matching = new List<PriceSnapshot>();
            foreach (var snap in state.Intel.PriceHistory)
            {
                if (string.Equals(snap.NodeId, nId, StringComparison.Ordinal)
                    && string.Equals(snap.GoodId, gId, StringComparison.Ordinal))
                {
                    matching.Add(snap);
                }
            }

            // Take last 10 entries (most recent).
            int startIdx = matching.Count > 10 ? matching.Count - 10 : 0;
            for (int i = startIdx; i < matching.Count; i++)
            {
                var snap = matching[i];
                var d = new Godot.Collections.Dictionary
                {
                    ["buy_price"] = snap.BuyPrice,
                    ["sell_price"] = snap.SellPrice,
                    ["tick"] = snap.Tick,
                };
                arr.Add(d);
            }

            lock (_snapshotLock) { _cachedPriceHistoryV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedPriceHistoryV0; }
    }
}
