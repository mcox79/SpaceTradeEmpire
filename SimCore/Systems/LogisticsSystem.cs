using System;
using System.Collections.Generic;
using System.Linq;
using SimCore.Entities;

namespace SimCore.Systems;

public static class LogisticsSystem
{
    public static void Process(SimState state)
    {
        // 1. Identify Shortages (Industry Demand vs Market Inventory)
        var shortages = new List<(string MarketId, string GoodId, int Amount)>();
        
        foreach (var site in state.IndustrySites.Values)
        {
            // Resolve Market for this Site (via Node)
            if (string.IsNullOrEmpty(site.NodeId)) continue;
            
            // Assumption: MarketId is often same as NodeId or linked. 
            // For now, look up Market by ID matching Node, or check Node's MarketId if nodes exist
            string marketId = site.NodeId; // Default fallback
            if (state.Nodes.TryGetValue(site.NodeId, out var node) && !string.IsNullOrEmpty(node.MarketId))
            {
                marketId = node.MarketId;
            }

            if (!state.Markets.TryGetValue(marketId, out var market)) continue;

            foreach (var input in site.Inputs)
            {
                string goodId = input.Key;
                int required = input.Value;
                int current = market.Inventory.GetValueOrDefault(goodId, 0);
                
                if (current < required)
                {
                    shortages.Add((market.Id, goodId, required - current));
                }
            }
        }

        // 2. Assign Fleets to Shortages
        foreach (var task in shortages)
        {
            // Find a fleet that is IDLE
            var fleet = state.Fleets.Values.FirstOrDefault(f => f.State == FleetState.Idle);
            if (fleet == null) continue;

            // Find a supplier
            var supplier = FindSupplier(state, task.GoodId, task.MarketId);
            if (supplier != null)
            {
                PlanLogistics(state, fleet, supplier.Id, task.MarketId, task.GoodId, task.Amount);
            }
        }
    }

    private static Market? FindSupplier(SimState state, string goodId, string excludeMarketId)
    {
        return state.Markets.Values
            .FirstOrDefault(m => m.Id != excludeMarketId && 
                                 m.Inventory.GetValueOrDefault(goodId, 0) > 10);
    }

    public static void PlanLogistics(SimState state, Fleet fleet, string sourceMarketId, string destMarketId, string goodId, int amount)
    {
        string? sourceNode = GetNodeForMarket(state, sourceMarketId);
        string? destNode = GetNodeForMarket(state, destMarketId);

        if (sourceNode == null || destNode == null) return;

        // Route Verification
        string? nextHop = GetNextHop(state, fleet.CurrentNodeId, sourceNode);
        if (nextHop == null && fleet.CurrentNodeId != sourceNode) return;

        // Assign Orders
        fleet.State = FleetState.Traveling;
        fleet.DestinationNodeId = sourceNode;
        fleet.CurrentTask = $"Fetching {goodId} from {sourceMarketId}";
        
        fleet.CurrentJob = new LogisticsJob
        {
            GoodId = goodId,
            SourceNodeId = sourceNode,
            TargetNodeId = destNode,
            Amount = amount
        };
    }

    private static string? GetNodeForMarket(SimState state, string marketId)
    {
        // Try explicit link
        var linkedNode = state.Nodes.Values.FirstOrDefault(n => n.MarketId == marketId);
        if (linkedNode != null) return linkedNode.Id;
        
        // Fallback: Check if MarketId is itself a NodeId
        if (state.Nodes.ContainsKey(marketId)) return marketId;
        
        return null;
    }

    public static string? GetNextHop(SimState state, string startId, string endId)
    {
        if (startId == endId) return startId;
        if (MapQueries.AreConnected(state, startId, endId)) return endId;

        var frontier = new Queue<string>();
        frontier.Enqueue(startId);
        var cameFrom = new Dictionary<string, string>();
        cameFrom[startId] = startId; // Sentinel

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current == endId) break;

            foreach (var edge in state.Edges.Values)
            {
                string? next = null;
                if (edge.FromNodeId == current) next = edge.ToNodeId;
                else if (edge.ToNodeId == current) next = edge.FromNodeId;

                if (next != null && !cameFrom.ContainsKey(next))
                {
                    frontier.Enqueue(next);
                    cameFrom[next] = current;
                }
            }
        }

        if (!cameFrom.ContainsKey(endId)) return null;

        var curr = endId;
        while (cameFrom[curr] != startId)
        {
            curr = cameFrom[curr];
        }
        return curr;
    }
}