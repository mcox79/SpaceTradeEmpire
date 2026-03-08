using System;
using System.Collections.Generic;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S5.NPC_TRADE.SYSTEM.001: NPC trade circulation — autonomous NPC traders evaluate and execute trades.
// NPC traders are economic background actors: they move goods from surplus to deficit nodes
// without a credits model. This creates price convergence the player can observe and exploit.
public static class NpcTradeSystem
{
    public sealed class TradeOpportunity
    {
        public string GoodId { get; set; } = "";
        public string SourceNodeId { get; set; } = "";
        public string DestNodeId { get; set; } = "";
        public int BuyPrice { get; set; }
        public int SellPrice { get; set; }
        public int ProfitPerUnit { get; set; }
        public int Units { get; set; }
    }

    /// <summary>
    /// Process NPC trade fleet evaluations and execute profitable trades.
    /// Called once per tick from SimKernel.Step().
    /// </summary>
    public static void ProcessNpcTrade(SimState state)
    {
        if (state == null) return;

        // Only evaluate every N ticks (skip early ticks to let galaxy stabilize)
        if (state.Tick < NpcTradeTweaksV0.EvalIntervalTicks) return;
        if (state.Tick % NpcTradeTweaksV0.EvalIntervalTicks != 0) return;

        // Iterate NPC-owned fleets in deterministic order
        var fleetIds = new List<string>(state.Fleets.Keys);
        fleetIds.Sort(StringComparer.Ordinal);

        foreach (var fleetId in fleetIds)
        {
            var fleet = state.Fleets[fleetId];

            if (fleet.OwnerId == "player") continue;
            if (string.IsNullOrEmpty(fleet.CurrentNodeId)) continue;
            if (!string.IsNullOrEmpty(fleet.CurrentEdgeId)) continue; // in transit
            if (!string.IsNullOrEmpty(fleet.FinalDestinationNodeId)) continue; // already traveling

            if (fleet.Role == FleetRole.Trader)
            {
                if (fleet.CurrentJob != null) continue; // has active logistics job
                ProcessFleetTrade(state, fleet);
            }
            else if (fleet.Role == FleetRole.Patrol)
            {
                // GATE.S12.NPC_CIRC.CIRCUIT_ROUTES.001: Patrol circuit advancement.
                ProcessPatrolCircuit(state, fleet);
            }
        }
    }

    // GATE.S12.NPC_CIRC.CIRCUIT_ROUTES.001: Advance patrol fleet to next circuit stop.
    private static void ProcessPatrolCircuit(SimState state, Fleet fleet)
    {
        if (fleet.PatrolCircuit == null || fleet.PatrolCircuit.Count < 2)
        {
            fleet.PatrolCircuit = GenerateCircuit(state, fleet.Id, fleet.CurrentNodeId);
            fleet.PatrolCircuitIndex = 0;
        }

        if (fleet.PatrolCircuit.Count < 2) return;

        fleet.PatrolCircuitIndex = (fleet.PatrolCircuitIndex + 1) % fleet.PatrolCircuit.Count;
        var nextNodeId = fleet.PatrolCircuit[fleet.PatrolCircuitIndex];
        fleet.FinalDestinationNodeId = nextNodeId;
        fleet.DestinationNodeId = nextNodeId;
    }

    /// <summary>
    /// Generate a deterministic multi-hop circuit (3-5 nodes) from galaxy topology.
    /// Uses FNV1a hash of fleet ID to select among adjacent nodes at each step.
    /// </summary>
    public static List<string> GenerateCircuit(SimState state, string fleetId, string startNodeId)
    {
        var circuit = new List<string> { startNodeId };
        var visited = new HashSet<string>(StringComparer.Ordinal) { startNodeId };
        int targetHops = 3 + (int)(Fnv1aHash(fleetId) % 3);

        var currentNode = startNodeId;
        for (int hop = 0; hop < targetHops; hop++)
        {
            var candidates = new List<string>();
            foreach (var edge in state.Edges.Values)
            {
                string adj = "";
                if (edge.FromNodeId == currentNode) adj = edge.ToNodeId;
                else if (edge.ToNodeId == currentNode) adj = edge.FromNodeId;
                if (adj.Length > 0 && !visited.Contains(adj))
                    candidates.Add(adj);
            }
            if (candidates.Count == 0) break;

            candidates.Sort(StringComparer.Ordinal);
            ulong pickHash = Fnv1aHash(fleetId + "_" + hop);
            int idx = (int)(pickHash % (ulong)candidates.Count);
            circuit.Add(candidates[idx]);
            visited.Add(candidates[idx]);
            currentNode = candidates[idx];
        }
        return circuit;
    }

    private static ulong Fnv1aHash(string input)
    {
        ulong hash = 14695981039346656037UL;
        foreach (char c in input) { hash ^= (byte)c; hash *= 1099511628211UL; }
        return hash;
    }

    private static void ProcessFleetTrade(SimState state, Fleet fleet)
    {
        var currentNodeId = fleet.CurrentNodeId;
        if (!state.Markets.TryGetValue(currentNodeId, out var localMarket)) return;

        // If fleet has cargo, deliver to current location (add to market inventory)
        var cargoGoodsToDeliver = new List<string>(fleet.Cargo.Keys);
        cargoGoodsToDeliver.Sort(StringComparer.Ordinal);

        foreach (var goodId in cargoGoodsToDeliver)
        {
            int qty = fleet.Cargo[goodId];
            if (qty <= 0) continue;

            // Deliver cargo to local market
            localMarket.Inventory[goodId] = (localMarket.Inventory.TryGetValue(goodId, out var curStock) ? curStock : 0) + qty;
            fleet.Cargo[goodId] = 0;
        }

        // Clean up zero-qty cargo entries
        var toRemove = new List<string>();
        foreach (var kv in fleet.Cargo)
            if (kv.Value <= 0) toRemove.Add(kv.Key);
        foreach (var k in toRemove)
            fleet.Cargo.Remove(k);

        // Find best trade opportunity among adjacent nodes
        var best = FindBestOpportunity(state, currentNodeId, localMarket);
        if (best == null) return;

        // Pick up goods from local market
        int unitsToPick = Math.Min(best.Units, NpcTradeTweaksV0.MaxTradeUnitsPerTrip);
        if (unitsToPick <= 0) return;

        int newLocalStock = (localMarket.Inventory.TryGetValue(best.GoodId, out var ls) ? ls : 0) - unitsToPick;
        localMarket.Inventory[best.GoodId] = Math.Max(0, newLocalStock);
        fleet.Cargo[best.GoodId] = (fleet.Cargo.TryGetValue(best.GoodId, out var cargoQty) ? cargoQty : 0) + unitsToPick;

        // Set travel destination
        fleet.FinalDestinationNodeId = best.DestNodeId;
        fleet.DestinationNodeId = best.DestNodeId;
    }

    public static TradeOpportunity? FindBestOpportunity(
        SimState state, string currentNodeId, Market localMarket)
    {
        TradeOpportunity? best = null;

        // Check adjacent nodes via edges
        var edgeIds = new List<string>(state.Edges.Keys);
        edgeIds.Sort(StringComparer.Ordinal);

        foreach (var edgeId in edgeIds)
        {
            var edge = state.Edges[edgeId];
            string adjNodeId;

            if (edge.FromNodeId == currentNodeId)
                adjNodeId = edge.ToNodeId;
            else if (edge.ToNodeId == currentNodeId)
                adjNodeId = edge.FromNodeId;
            else
                continue;

            if (!state.Markets.TryGetValue(adjNodeId, out var adjMarket)) continue;

            // Evaluate each good available locally
            var goodIds = new List<string>(localMarket.Inventory.Keys);
            goodIds.Sort(StringComparer.Ordinal);

            foreach (var goodId in goodIds)
            {
                int localStock = localMarket.Inventory.TryGetValue(goodId, out var lq) ? lq : 0;
                if (localStock <= 0) continue;

                int buyPrice = localMarket.GetBuyPrice(goodId);
                int sellPrice = adjMarket.GetSellPrice(goodId);
                int profitPerUnit = sellPrice - buyPrice;

                if (profitPerUnit < NpcTradeTweaksV0.ProfitThresholdCredits) continue;

                int units = Math.Min(localStock, NpcTradeTweaksV0.MaxTradeUnitsPerTrip);
                if (units <= 0) continue;

                // GATE.S18.TRADE_GOODS.NPC_TRADE_UPDATE.001: Weight-adjusted scoring.
                int weight = NpcTradeTweaksV0.GoodTradeWeights.TryGetValue(goodId, out var w)
                    ? w : NpcTradeTweaksV0.DefaultGoodWeight;
                long score = (long)profitPerUnit * units * weight;
                long bestScore = best != null
                    ? (long)best.ProfitPerUnit * best.Units * (NpcTradeTweaksV0.GoodTradeWeights.TryGetValue(best.GoodId, out var bw) ? bw : NpcTradeTweaksV0.DefaultGoodWeight)
                    : 0;

                if (best == null || score > bestScore)
                {
                    best = new TradeOpportunity
                    {
                        GoodId = goodId,
                        SourceNodeId = currentNodeId,
                        DestNodeId = adjNodeId,
                        BuyPrice = buyPrice,
                        SellPrice = sellPrice,
                        ProfitPerUnit = profitPerUnit,
                        Units = units,
                    };
                }
            }
        }

        return best;
    }
}
