using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Gen;

namespace SimCore.Tests.Gen;

// GATE.S7.STARTER_PLACEMENT.VIABILITY.001: Contract tests for starter system viability.
// Across seeds 1-100, starter system must have:
// - >=3 viable early trade loops (adjacent markets with price differentials)
// - >=1 discovery site within 2 hops
[TestFixture]
public sealed class StarterPlacementTests
{
    [TestCase(1, 100)]
    public void AllSeeds_HaveViableTradeLoops(int seedMin, int seedMax)
    {
        var failures = new List<string>();

        for (int seed = seedMin; seed <= seedMax; seed++)
        {
            var kernel = new SimKernel(seed: seed);
            GalaxyGenerator.Generate(kernel.State, 20, 100f);
            var state = kernel.State;

            var starterIds = GalaxyGenerator.GetStarterRegionNodeIdsSortedV0(state);
            if (starterIds.Count == 0)
            {
                failures.Add($"Seed {seed}: no starter region nodes");
                continue;
            }

            // Find trade loops: pairs of adjacent starter-region markets with
            // at least one good having a price differential >= 10 credits.
            int tradeLoopCount = CountTradeLoops(state, starterIds);

            if (tradeLoopCount < 3)
            {
                failures.Add($"Seed {seed}: only {tradeLoopCount} trade loops (need >=3)");
            }
        }

        Assert.That(failures, Is.Empty,
            $"Seeds with insufficient trade loops:\n{string.Join("\n", failures)}");
    }

    [TestCase(1, 100)]
    public void AllSeeds_HaveDiscoveryWithin2Hops(int seedMin, int seedMax)
    {
        var failures = new List<string>();

        for (int seed = seedMin; seed <= seedMax; seed++)
        {
            var kernel = new SimKernel(seed: seed);
            GalaxyGenerator.Generate(kernel.State, 20, 100f);
            var state = kernel.State;

            var starterIds = GalaxyGenerator.GetStarterRegionNodeIdsSortedV0(state);
            if (starterIds.Count == 0)
            {
                failures.Add($"Seed {seed}: no starter region nodes");
                continue;
            }

            // Check: any node within 2 hops of any starter node has a discovery site.
            bool hasDiscovery = HasDiscoveryWithinHops(state, starterIds, 2);

            if (!hasDiscovery)
            {
                failures.Add($"Seed {seed}: no discovery site within 2 hops of starter region");
            }
        }

        Assert.That(failures, Is.Empty,
            $"Seeds without nearby discovery:\n{string.Join("\n", failures)}");
    }

    private static int CountTradeLoops(SimState state, IReadOnlyList<string> starterIds)
    {
        var starterSet = new HashSet<string>(starterIds, StringComparer.Ordinal);
        var loopPairs = new HashSet<string>(StringComparer.Ordinal);

        foreach (var edge in state.Edges.Values)
        {
            // Both endpoints must be in (or adjacent to) starter region.
            string fromId = edge.FromNodeId;
            string toId = edge.ToNodeId;
            if (!starterSet.Contains(fromId) && !starterSet.Contains(toId)) continue;

            // Both need markets.
            if (!state.Nodes.TryGetValue(fromId, out var fromNode)) continue;
            if (!state.Nodes.TryGetValue(toId, out var toNode)) continue;
            if (string.IsNullOrEmpty(fromNode.MarketId) || string.IsNullOrEmpty(toNode.MarketId)) continue;
            if (!state.Markets.TryGetValue(fromNode.MarketId, out var fromMarket)) continue;
            if (!state.Markets.TryGetValue(toNode.MarketId, out var toMarket)) continue;

            // Check for goods with price differential >= 10 credits.
            var allGoods = new HashSet<string>(StringComparer.Ordinal);
            foreach (var g in fromMarket.Inventory.Keys) allGoods.Add(g);
            foreach (var g in toMarket.Inventory.Keys) allGoods.Add(g);

            foreach (var goodId in allGoods)
            {
                int buyAtFrom = fromMarket.GetBuyPrice(goodId);
                int sellAtTo = toMarket.GetSellPrice(goodId);
                int buyAtTo = toMarket.GetBuyPrice(goodId);
                int sellAtFrom = fromMarket.GetSellPrice(goodId);

                // Trade loop: buy at one, sell at other for profit.
                bool profitAtoB = sellAtTo - buyAtFrom >= 10;
                bool profitBtoA = sellAtFrom - buyAtTo >= 10;

                if (profitAtoB || profitBtoA)
                {
                    // Canonical pair key to avoid double-counting.
                    string pairKey = StringComparer.Ordinal.Compare(fromId, toId) < 0
                        ? $"{fromId}:{toId}:{goodId}"
                        : $"{toId}:{fromId}:{goodId}";
                    loopPairs.Add(pairKey);
                }
            }
        }

        return loopPairs.Count;
    }

    private static bool HasDiscoveryWithinHops(SimState state, IReadOnlyList<string> starterIds, int maxHops)
    {
        // BFS from all starter nodes up to maxHops. Collect all reachable node IDs.
        var reachable = new HashSet<string>(StringComparer.Ordinal);
        var frontier = new HashSet<string>(starterIds, StringComparer.Ordinal);
        reachable.UnionWith(frontier);

        for (int hop = 0; hop < maxHops; hop++)
        {
            var nextFrontier = new HashSet<string>(StringComparer.Ordinal);
            foreach (var nodeId in frontier)
            {
                foreach (var edge in state.Edges.Values)
                {
                    string? neighbor = null;
                    if (StringComparer.Ordinal.Equals(edge.FromNodeId, nodeId))
                        neighbor = edge.ToNodeId;
                    else if (StringComparer.Ordinal.Equals(edge.ToNodeId, nodeId))
                        neighbor = edge.FromNodeId;

                    if (neighbor != null && !reachable.Contains(neighbor))
                    {
                        reachable.Add(neighbor);
                        nextFrontier.Add(neighbor);
                    }
                }
            }
            frontier = nextFrontier;
        }

        // Check SeededDiscoveryIds on reachable nodes.
        foreach (var nodeId in reachable)
        {
            if (state.Nodes.TryGetValue(nodeId, out var node) &&
                node.SeededDiscoveryIds != null && node.SeededDiscoveryIds.Count > 0)
                return true;
        }

        // Also check DiscoverySeedSurface: discovery seeds have NodeId.
        var seeds = DiscoverySeedGen.BuildDiscoverySeedSurfaceV0(state);
        foreach (var seed in seeds)
        {
            if (!string.IsNullOrEmpty(seed.NodeId) && reachable.Contains(seed.NodeId))
                return true;
        }

        return false;
    }
}
