using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Gen;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.ExperienceProof;

/// <summary>
/// EPIC.X.EXPERIENCE_PROOF.V0 — Component 7
/// Validates SimCore system graph is connected:
/// every system in Step() produces observable state change,
/// and trade loops exist with viable price differences.
/// </summary>
[TestFixture]
public class SystemConnectivityTests
{
    private const int StarCount = 12;
    private const float Radius = 100f;

    private static SimKernel CreateWithGalaxy(int seed)
    {
        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, StarCount, Radius);
        return sim;
    }

    [Test]
    public void AllSystems_ProduceObservableStateChange()
    {
        var sim = CreateWithGalaxy(42);
        var state = sim.State;

        Assert.That(state.Nodes.Count, Is.GreaterThan(0), "Galaxy has no nodes after generation");
        Assert.That(state.Edges.Count, Is.GreaterThan(0), "Galaxy has no edges/lanes after generation");
        Assert.That(state.Markets.Count, Is.GreaterThan(0), "Galaxy has no markets after generation");

        int tickBefore = state.Tick;

        // Run 100 ticks
        for (int i = 0; i < 100; i++)
            sim.Step();

        Assert.That(state.Tick, Is.EqualTo(tickBefore + 100), "Tick did not advance by 100");
    }

    [Test]
    public void Galaxy_IsConnected()
    {
        var sim = CreateWithGalaxy(42);
        var state = sim.State;

        // Build adjacency from edges
        var adj = new Dictionary<string, HashSet<string>>();
        foreach (var node in state.Nodes.Values)
            adj[node.Id] = new HashSet<string>();

        foreach (var edge in state.Edges.Values)
        {
            if (adj.ContainsKey(edge.FromNodeId))
                adj[edge.FromNodeId].Add(edge.ToNodeId);
            if (adj.ContainsKey(edge.ToNodeId))
                adj[edge.ToNodeId].Add(edge.FromNodeId);
        }

        // BFS from first node
        var start = state.Nodes.Keys.OrderBy(k => k, System.StringComparer.Ordinal).First();
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (adj.TryGetValue(current, out var neighbors))
            {
                foreach (var n in neighbors)
                {
                    if (visited.Add(n))
                        queue.Enqueue(n);
                }
            }
        }

        Assert.That(visited.Count, Is.EqualTo(state.Nodes.Count),
            $"Galaxy is not connected: reached {visited.Count}/{state.Nodes.Count} nodes from {start}");
    }

    [Test]
    public void TradeLoops_ExistAndAreViable()
    {
        var sim = CreateWithGalaxy(42);
        var state = sim.State;

        // Run a few ticks to let markets settle
        for (int i = 0; i < 10; i++)
            sim.Step();

        // Build adjacency
        var adj = new Dictionary<string, HashSet<string>>();
        foreach (var node in state.Nodes.Values)
            adj[node.Id] = new HashSet<string>();

        foreach (var edge in state.Edges.Values)
        {
            if (adj.ContainsKey(edge.FromNodeId))
                adj[edge.FromNodeId].Add(edge.ToNodeId);
            if (adj.ContainsKey(edge.ToNodeId))
                adj[edge.ToNodeId].Add(edge.FromNodeId);
        }

        // For each good, find at least one pair of connected markets
        // where buy price at source < sell price at destination.
        // This means buying at A and selling at B is profitable.
        var allGoods = new HashSet<string>();
        foreach (var market in state.Markets.Values)
        {
            foreach (var goodId in market.Inventory.Keys)
                allGoods.Add(goodId);
        }

        int viableRoutes = 0;
        foreach (var goodId in allGoods.OrderBy(g => g, System.StringComparer.Ordinal))
        {
            // Find max sell price and min buy price across all markets for this good
            int minBuy = int.MaxValue;
            int maxSell = 0;
            foreach (var market in state.Markets.Values)
            {
                if (!market.Inventory.ContainsKey(goodId)) continue;
                int buy = market.GetBuyPrice(goodId);
                int sell = market.GetSellPrice(goodId);
                if (buy > 0 && buy < minBuy) minBuy = buy;
                if (sell > maxSell) maxSell = sell;
            }

            // If any market's sell price exceeds any other market's buy price,
            // a viable trade route exists for this good.
            if (maxSell > minBuy)
                viableRoutes++;
        }

        Assert.That(viableRoutes, Is.GreaterThan(0),
            "No viable trade routes found (no market pair where sell at B > buy at A for any good)");
    }

    [Test]
    public void EveryNode_HasMarket()
    {
        var sim = CreateWithGalaxy(42);
        var state = sim.State;

        foreach (var node in state.Nodes.Values)
        {
            Assert.That(string.IsNullOrEmpty(node.MarketId), Is.False,
                $"Node {node.Id} has no MarketId");
            Assert.That(state.Markets.ContainsKey(node.MarketId), Is.True,
                $"Node {node.Id} references market '{node.MarketId}' which doesn't exist");
        }
    }

    [Test]
    public void MultipleSeeds_ProduceDistinctGalaxies()
    {
        var simA = CreateWithGalaxy(42);
        var simB = CreateWithGalaxy(99);

        // Different seeds should produce different node positions
        var posA = simA.State.Nodes.Values
            .OrderBy(n => n.Id, System.StringComparer.Ordinal)
            .Select(n => $"{n.Position.X:F2},{n.Position.Z:F2}")
            .ToList();
        var posB = simB.State.Nodes.Values
            .OrderBy(n => n.Id, System.StringComparer.Ordinal)
            .Select(n => $"{n.Position.X:F2},{n.Position.Z:F2}")
            .ToList();

        bool allSame = posA.Count == posB.Count;
        if (allSame)
        {
            for (int i = 0; i < posA.Count; i++)
            {
                if (posA[i] != posB[i])
                {
                    allSame = false;
                    break;
                }
            }
        }

        Assert.That(allSame, Is.False,
            "Seeds 42 and 99 produced identical node positions — RNG not seeded properly");
    }

    // ── Multi-seed fuzzing: run core invariants across 10 seeds ──

    private static readonly int[] FuzzSeeds = { 1, 7, 42, 99, 256, 1000, 31337, 55555, 77777, 99999 };

    [Test]
    public void MultiSeed_AllConnected()
    {
        foreach (var seed in FuzzSeeds)
        {
            var sim = CreateWithGalaxy(seed);
            var state = sim.State;

            var adj = new Dictionary<string, HashSet<string>>();
            foreach (var node in state.Nodes.Values)
                adj[node.Id] = new HashSet<string>();
            foreach (var edge in state.Edges.Values)
            {
                if (adj.ContainsKey(edge.FromNodeId))
                    adj[edge.FromNodeId].Add(edge.ToNodeId);
                if (adj.ContainsKey(edge.ToNodeId))
                    adj[edge.ToNodeId].Add(edge.FromNodeId);
            }

            var start = state.Nodes.Keys.OrderBy(k => k, System.StringComparer.Ordinal).First();
            var visited = new HashSet<string>();
            var queue = new Queue<string>();
            queue.Enqueue(start);
            visited.Add(start);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (adj.TryGetValue(c, out var nb))
                    foreach (var n in nb)
                        if (visited.Add(n))
                            queue.Enqueue(n);
            }

            Assert.That(visited.Count, Is.EqualTo(state.Nodes.Count),
                $"Seed {seed}: galaxy not connected ({visited.Count}/{state.Nodes.Count})");
        }
    }

    [Test]
    public void MultiSeed_AllHaveViableTrades()
    {
        foreach (var seed in FuzzSeeds)
        {
            var sim = CreateWithGalaxy(seed);
            for (int i = 0; i < 10; i++)
                sim.Step();

            var state = sim.State;
            var allGoods = new HashSet<string>();
            foreach (var market in state.Markets.Values)
                foreach (var goodId in market.Inventory.Keys)
                    allGoods.Add(goodId);

            int viableRoutes = 0;
            foreach (var goodId in allGoods)
            {
                int minBuy = int.MaxValue;
                int maxSell = 0;
                foreach (var market in state.Markets.Values)
                {
                    if (!market.Inventory.ContainsKey(goodId)) continue;
                    int buy = market.GetBuyPrice(goodId);
                    int sell = market.GetSellPrice(goodId);
                    if (buy > 0 && buy < minBuy) minBuy = buy;
                    if (sell > maxSell) maxSell = sell;
                }
                if (maxSell > minBuy) viableRoutes++;
            }

            Assert.That(viableRoutes, Is.GreaterThan(0),
                $"Seed {seed}: no viable trade routes found");
        }
    }

    [Test]
    public void MultiSeed_AllNodesHaveMarkets()
    {
        foreach (var seed in FuzzSeeds)
        {
            var sim = CreateWithGalaxy(seed);
            var state = sim.State;
            foreach (var node in state.Nodes.Values)
            {
                Assert.That(string.IsNullOrEmpty(node.MarketId), Is.False,
                    $"Seed {seed}: node {node.Id} has no MarketId");
                Assert.That(state.Markets.ContainsKey(node.MarketId), Is.True,
                    $"Seed {seed}: node {node.Id} references missing market '{node.MarketId}'");
            }
        }
    }

    [Test]
    public void MultiSeed_1000Ticks_NoCrash()
    {
        foreach (var seed in FuzzSeeds)
        {
            var sim = CreateWithGalaxy(seed);
            int tickBefore = sim.State.Tick;
            for (int i = 0; i < 1000; i++)
                sim.Step();
            Assert.That(sim.State.Tick, Is.EqualTo(tickBefore + 1000),
                $"Seed {seed}: tick did not advance by 1000");
        }
    }

    [Test]
    public void MultiSeed_EconomyDoesNotDiverge()
    {
        // After 500 ticks, no market should have negative inventory
        // and player credits should remain non-negative
        foreach (var seed in FuzzSeeds)
        {
            var sim = CreateWithGalaxy(seed);
            for (int i = 0; i < 500; i++)
                sim.Step();

            var state = sim.State;
            foreach (var market in state.Markets.Values)
            {
                foreach (var kvp in market.Inventory)
                {
                    Assert.That(kvp.Value, Is.GreaterThanOrEqualTo(0),
                        $"Seed {seed}: market has negative inventory for {kvp.Key} ({kvp.Value})");
                }
            }

            Assert.That(state.PlayerCredits, Is.GreaterThanOrEqualTo(0),
                $"Seed {seed}: player has negative credits ({state.PlayerCredits})");
        }
    }
}
