#nullable enable

using Godot;
using SimCore;
using SimCore.Commands;
using SimCore.Entities;
using SimCore.Tweaks;
using System;
using System.Linq;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge
{
    // ── Gate transit cost queries for approach popup ──

    /// <summary>
    /// Returns transit cost preview for the popup: credit_cost, congestion_pct,
    /// can_afford, destination_name, current_credits.
    /// Nonblocking read — returns defaults on lock failure.
    /// </summary>
    public Godot.Collections.Dictionary GetTransitCostV0(string fleetId, string targetNodeId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["credit_cost"] = TransitTweaksV0.BaseCreditCost,
            ["congestion_pct"] = 0,
            ["can_afford"] = true,
            ["destination_name"] = targetNodeId ?? "",
            ["current_credits"] = 0L,
        };

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(fleetId) || string.IsNullOrEmpty(targetNodeId)) return;

            // Destination display name.
            if (state.Nodes.TryGetValue(targetNodeId, out var node))
                result["destination_name"] = node.Name ?? targetNodeId;

            // Find connecting edge for congestion-based cost.
            var edge = state.Edges.Values.FirstOrDefault(e =>
                (e.FromNodeId == state.PlayerLocationNodeId && e.ToNodeId == targetNodeId) ||
                (e.FromNodeId == targetNodeId && e.ToNodeId == state.PlayerLocationNodeId)
            );

            int cost = TransitTweaksV0.BaseCreditCost;
            int congestionPct = 0;
            if (edge != null)
            {
                cost = TravelCommand.ComputeTransitCost(edge);
                if (edge.TotalCapacity > 0)
                    congestionPct = edge.UsedCapacity * 100 / edge.TotalCapacity;
            }

            result["credit_cost"] = cost;
            result["congestion_pct"] = congestionPct;
            result["current_credits"] = state.PlayerCredits;
            result["can_afford"] = state.PlayerCredits >= cost;
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns true if the player has NOT previously visited this node.
    /// Uses the PlayerVisitedNodeIds set.
    /// </summary>
    public bool IsFirstVisitV0(string nodeId)
    {
        bool isFirst = true;
        TryExecuteSafeRead(state =>
        {
            isFirst = !state.PlayerVisitedNodeIds.Contains(nodeId ?? "");
        }, 0);
        return isFirst;
    }
}
