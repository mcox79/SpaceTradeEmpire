#nullable enable

using Godot;
using SimCore;
using SimCore.Systems;
using System;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge
{
    // ── GATE.S6.FRACTURE.BRIDGE.001: Fracture access and market queries ──

    /// <summary>
    /// Checks whether a fleet may enter a fracture node.
    /// Returns {allowed (bool), reason (string)}.
    /// Nonblocking read — returns denied with reason on lock failure.
    /// </summary>
    public Godot.Collections.Dictionary GetFractureAccessV0(string fleetId, string nodeId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["allowed"] = false,
            ["reason"] = "",
        };

        TryExecuteSafeRead(state =>
        {
            var check = FractureSystem.FractureAccessCheck(state, fleetId, nodeId);
            result["allowed"] = check.Allowed;
            result["reason"] = check.Reason;
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns fracture-adjusted market pricing for a good at a node.
    /// Returns {mid (int), buy (int), sell (int), volume_cap (int), error (string)}.
    /// Uses current market inventory stock as input to FracturePricingV0.
    /// Nonblocking read — returns error dict if lock unavailable or node/market not found.
    /// </summary>
    public Godot.Collections.Dictionary GetFractureMarketV0(string nodeId, string goodId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["mid"] = 0,
            ["buy"] = 0,
            ["sell"] = 0,
            ["volume_cap"] = 0,
            ["error"] = "",
        };

        TryExecuteSafeRead(state =>
        {
            if (!state.Nodes.TryGetValue(nodeId, out var node))
            {
                result["error"] = $"node not found: {nodeId}";
                return;
            }

            if (string.IsNullOrEmpty(node.MarketId) || !state.Markets.TryGetValue(node.MarketId, out var market))
            {
                result["error"] = $"no market at node: {nodeId}";
                return;
            }

            int stock = market.Inventory.TryGetValue(goodId, out var s) ? s : 0;
            var pricing = FractureSystem.FracturePricingV0(stock);

            result["mid"] = pricing.Mid;
            result["buy"] = pricing.Buy;
            result["sell"] = pricing.Sell;
            result["volume_cap"] = pricing.VolumeCap;
        }, 0);

        return result;
    }
}
