using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.Gen;

/// <summary>
/// Monte Carlo procedural content quality tests.
/// Validates galaxy generation across multiple seeds per industry best practices:
/// - Graph connectivity (Stellaris/Paradox)
/// - Resource distribution and profitable routes (EVE Online/CCP)
/// - Faction territory balance (Stellaris)
/// - Starting position quality (NMS/Hello Games)
/// - Name uniqueness (NMS)
/// - Degenerate outcome rate (general Monte Carlo)
/// </summary>
[TestFixture]
public sealed class GalaxyQualityTests
{
    private static readonly int[] TestSeeds = { 42, 99, 1001, 31337, 77777, 12345, 54321, 88888, 7, 999 };
    private const int StarCount = 20;
    private const float GalaxyRadius = 100f;

    private SimKernel GenerateGalaxy(int seed)
    {
        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, StarCount, GalaxyRadius);
        return sim;
    }

    // =========================================
    // Graph Connectivity
    // =========================================

    [Test]
    public void AllStationNodes_ReachableFromPlayerStart_AllSeeds()
    {
        foreach (var seed in TestSeeds)
        {
            var sim = GenerateGalaxy(seed);
            var state = sim.State;

            var startNode = state.PlayerLocationNodeId;
            Assert.That(startNode, Is.Not.Null.And.Not.Empty,
                $"Seed {seed}: PlayerLocationNodeId is empty");

            var reachable = BfsReachable(state, startNode);
            var stationNodes = state.Nodes.Values
                .Where(n => !string.IsNullOrEmpty(n.MarketId))
                .Select(n => n.Id)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var stationId in stationNodes)
            {
                Assert.That(reachable, Does.Contain(stationId),
                    $"Seed {seed}: Station node {stationId} unreachable from player start");
            }
        }
    }

    [Test]
    public void GraphDiameter_NotExcessive_AllSeeds()
    {
        foreach (var seed in TestSeeds)
        {
            var sim = GenerateGalaxy(seed);
            var state = sim.State;
            var diameter = ComputeGraphDiameter(state);

            Assert.That(diameter, Is.LessThanOrEqualTo(StarCount),
                $"Seed {seed}: Graph diameter {diameter} exceeds star count {StarCount}");
            Assert.That(diameter, Is.GreaterThan(0),
                $"Seed {seed}: Graph diameter is 0 (degenerate)");
        }
    }

    // =========================================
    // Resource Distribution
    // =========================================

    [Test]
    public void ProfitableRoutes_ExistWithin3Hops_AllSeeds()
    {
        foreach (var seed in TestSeeds)
        {
            var sim = GenerateGalaxy(seed);
            var state = sim.State;
            var startNode = state.PlayerLocationNodeId;

            var nearbyNodes = BfsWithinHops(state, startNode, 3);

            // Check for profitable trades: any good where buy price at A < sell price at B.
            bool foundProfitable = false;
            foreach (var nodeId in nearbyNodes)
            {
                if (!state.Markets.TryGetValue(nodeId, out var market)) continue;
                foreach (var goodId in market.Inventory.Keys)
                {
                    int buyPrice = market.GetBuyPrice(goodId);
                    if (buyPrice <= 0) continue;

                    foreach (var otherNodeId in nearbyNodes)
                    {
                        if (otherNodeId == nodeId) continue;
                        if (!state.Markets.TryGetValue(otherNodeId, out var otherMarket)) continue;
                        int sellPrice = otherMarket.GetSellPrice(goodId);
                        if (sellPrice > buyPrice)
                        {
                            foundProfitable = true;
                            break;
                        }
                    }
                    if (foundProfitable) break;
                }
                if (foundProfitable) break;
            }

            Assert.That(foundProfitable, Is.True,
                $"Seed {seed}: No profitable trade route within 3 hops of start");
        }
    }

    [Test]
    public void MarketPriceVariance_NotZero_AllSeeds()
    {
        foreach (var seed in TestSeeds)
        {
            var sim = GenerateGalaxy(seed);
            var state = sim.State;

            // Collect all buy prices per good across all stations.
            var pricesByGood = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            foreach (var (nodeId, market) in state.Markets)
            {
                foreach (var goodId in market.Inventory.Keys)
                {
                    if (!pricesByGood.TryGetValue(goodId, out var list))
                    {
                        list = new List<int>();
                        pricesByGood[goodId] = list;
                    }
                    int bp = market.GetBuyPrice(goodId);
                    if (bp > 0) list.Add(bp);
                }
            }

            int goodsWithVariance = 0;
            foreach (var (goodId, prices) in pricesByGood)
            {
                if (prices.Count < 2) continue;
                double mean = prices.Average();
                double variance = prices.Sum(p => (p - mean) * (p - mean)) / prices.Count;
                if (variance > 0.01) goodsWithVariance++;
            }

            Assert.That(goodsWithVariance, Is.GreaterThan(0),
                $"Seed {seed}: No goods have price variance across stations");
        }
    }

    // =========================================
    // Faction Territory Balance
    // =========================================

    [Test]
    public void FactionTerritories_AllFactionsPresent_AllSeeds()
    {
        foreach (var seed in TestSeeds)
        {
            var sim = GenerateGalaxy(seed);
            var state = sim.State;

            var factionsPresent = state.NodeFactionId.Values
                .Where(f => !string.IsNullOrEmpty(f))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            Assert.That(factionsPresent.Count, Is.GreaterThanOrEqualTo(3),
                $"Seed {seed}: Only {factionsPresent.Count} factions present (need 3+)");
        }
    }

    [Test]
    public void FactionBalance_NoSingleFactionDominates_AllSeeds()
    {
        foreach (var seed in TestSeeds)
        {
            var sim = GenerateGalaxy(seed);
            var state = sim.State;

            var factionCounts = state.NodeFactionId.Values
                .Where(f => !string.IsNullOrEmpty(f))
                .GroupBy(f => f)
                .ToDictionary(g => g.Key, g => g.Count());

            if (factionCounts.Count == 0) continue;

            int totalFactionNodes = factionCounts.Values.Sum();
            int maxFactionNodes = factionCounts.Values.Max();
            double dominanceRatio = (double)maxFactionNodes / totalFactionNodes;

            Assert.That(dominanceRatio, Is.LessThan(0.6),
                $"Seed {seed}: Single faction controls {dominanceRatio:P0} of territory");
        }
    }

    // =========================================
    // Starting Position Quality
    // =========================================

    [Test]
    public void StartingPosition_HasStation_AllSeeds()
    {
        foreach (var seed in TestSeeds)
        {
            var sim = GenerateGalaxy(seed);
            var state = sim.State;
            var startNode = state.PlayerLocationNodeId;

            Assert.That(state.Nodes.ContainsKey(startNode), Is.True,
                $"Seed {seed}: Start node doesn't exist");
            Assert.That(state.Markets.ContainsKey(state.Nodes[startNode].MarketId),
                Is.True, $"Seed {seed}: Start node has no market");
        }
    }

    [Test]
    public void StartingPosition_HasNeighborStations_AllSeeds()
    {
        foreach (var seed in TestSeeds)
        {
            var sim = GenerateGalaxy(seed);
            var state = sim.State;
            var neighbors = GetNeighbors(state, state.PlayerLocationNodeId);

            int stationNeighbors = neighbors.Count(n =>
                state.Nodes.TryGetValue(n, out var node) && !string.IsNullOrEmpty(node.MarketId));

            Assert.That(stationNeighbors, Is.GreaterThanOrEqualTo(1),
                $"Seed {seed}: Start node has {stationNeighbors} station neighbors (need 1+)");
        }
    }

    // =========================================
    // Name Uniqueness
    // =========================================

    [Test]
    public void NodeNames_NoDuplicates_AllSeeds()
    {
        foreach (var seed in TestSeeds)
        {
            var sim = GenerateGalaxy(seed);
            var state = sim.State;

            var names = state.Nodes.Values
                .Where(n => !string.IsNullOrEmpty(n.Name))
                .Select(n => n.Name)
                .ToList();

            var dupes = names
                .GroupBy(n => n, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            Assert.That(dupes, Is.Empty,
                $"Seed {seed}: Duplicate node names: {string.Join(", ", dupes)}");
        }
    }

    // =========================================
    // Cross-Seed Degenerate Outcome Rate
    // =========================================

    [Test]
    public void DegenerateOutcomeRate_BelowThreshold()
    {
        int failures = 0;
        var failureReasons = new List<string>();

        foreach (var seed in TestSeeds)
        {
            try
            {
                var sim = GenerateGalaxy(seed);
                var state = sim.State;

                if (string.IsNullOrEmpty(state.PlayerLocationNodeId))
                    { failures++; failureReasons.Add($"Seed {seed}: no start"); continue; }
                if (state.Nodes.Count < 5)
                    { failures++; failureReasons.Add($"Seed {seed}: too few nodes"); continue; }
                if (state.Markets.Count == 0)
                    { failures++; failureReasons.Add($"Seed {seed}: no markets"); continue; }

                var reachable = BfsReachable(state, state.PlayerLocationNodeId);
                if (reachable.Count < state.Nodes.Count / 2)
                    { failures++; failureReasons.Add($"Seed {seed}: < 50% reachable"); continue; }
            }
            catch (Exception ex)
            {
                failures++;
                failureReasons.Add($"Seed {seed}: exception {ex.Message}");
            }
        }

        double failureRate = (double)failures / TestSeeds.Length;
        Assert.That(failureRate, Is.LessThan(0.1),
            $"Degenerate outcome rate {failureRate:P0} exceeds 10%: {string.Join("; ", failureReasons)}");
    }

    // =========================================
    // Helpers
    // =========================================

    private static HashSet<string> BfsReachable(SimState state, string startNode)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        queue.Enqueue(startNode);
        visited.Add(startNode);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var edge in state.Edges.Values)
            {
                string adj = "";
                if (edge.FromNodeId == current) adj = edge.ToNodeId;
                else if (edge.ToNodeId == current) adj = edge.FromNodeId;
                if (adj.Length > 0 && visited.Add(adj)) queue.Enqueue(adj);
            }
        }
        return visited;
    }

    private static HashSet<string> BfsWithinHops(SimState state, string startNode, int maxHops)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<(string, int)>();
        queue.Enqueue((startNode, 0));
        visited.Add(startNode);
        while (queue.Count > 0)
        {
            var (current, hops) = queue.Dequeue();
            if (hops >= maxHops) continue;
            foreach (var edge in state.Edges.Values)
            {
                string adj = "";
                if (edge.FromNodeId == current) adj = edge.ToNodeId;
                else if (edge.ToNodeId == current) adj = edge.FromNodeId;
                if (adj.Length > 0 && visited.Add(adj)) queue.Enqueue((adj, hops + 1));
            }
        }
        return visited;
    }

    private static int ComputeGraphDiameter(SimState state)
    {
        int maxDist = 0;
        foreach (var node in state.Nodes.Keys)
        {
            var distances = BfsDistances(state, node);
            foreach (var d in distances.Values)
                if (d > maxDist) maxDist = d;
        }
        return maxDist;
    }

    private static Dictionary<string, int> BfsDistances(SimState state, string startNode)
    {
        var dist = new Dictionary<string, int>(StringComparer.Ordinal) { [startNode] = 0 };
        var queue = new Queue<string>();
        queue.Enqueue(startNode);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var d = dist[current];
            foreach (var edge in state.Edges.Values)
            {
                string adj = "";
                if (edge.FromNodeId == current) adj = edge.ToNodeId;
                else if (edge.ToNodeId == current) adj = edge.FromNodeId;
                if (adj.Length > 0 && !dist.ContainsKey(adj))
                {
                    dist[adj] = d + 1;
                    queue.Enqueue(adj);
                }
            }
        }
        return dist;
    }

    private static List<string> GetNeighbors(SimState state, string nodeId)
    {
        var neighbors = new List<string>();
        foreach (var edge in state.Edges.Values)
        {
            if (edge.FromNodeId == nodeId) neighbors.Add(edge.ToNodeId);
            else if (edge.ToNodeId == nodeId) neighbors.Add(edge.FromNodeId);
        }
        return neighbors;
    }
}
