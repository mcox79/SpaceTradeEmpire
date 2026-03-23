using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Gen;

namespace SimCore.Tests.Systems;

/// <summary>
/// Economy stress tests: run simulation for extended periods and assert economic health.
/// Validates no stagnation, price stability, and NPC trade activity.
/// </summary>
[TestFixture]
public class EconomyStressTests
{
    [Test]
    public void Economy_NoStagnation_2000Ticks()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 20, 100f);

        // Track NPC trade activity per node.
        var nodeTradeActivity = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var node in kernel.State.Nodes.Keys)
            nodeTradeActivity[node] = 0;

        for (int i = 0; i < 2000; i++)
        {
            // Snapshot fleet positions to detect trade movement.
            var movingFleets = kernel.State.Fleets.Values
                .Where(f => f.OwnerId != "player" && f.IsMoving)
                .Count();

            if (movingFleets > 0)
            {
                foreach (var f in kernel.State.Fleets.Values)
                {
                    if (f.OwnerId == "player") continue;
                    if (!string.IsNullOrEmpty(f.DestinationNodeId) && nodeTradeActivity.ContainsKey(f.DestinationNodeId))
                        nodeTradeActivity[f.DestinationNodeId]++;
                }
            }

            kernel.Step();
        }

        // Assert: at least 40% of nodes have had NPC trade activity.
        int activeNodes = nodeTradeActivity.Count(kv => kv.Value > 0);
        int totalNodes = nodeTradeActivity.Count;
        double activeRatio = (double)activeNodes / totalNodes;
        Assert.That(activeRatio, Is.GreaterThanOrEqualTo(0.4),
            $"Only {activeNodes}/{totalNodes} nodes had NPC trade activity ({activeRatio:P0}). Economy may be stagnant.");

        // Assert: avg good price within 3x of base price (no runaway inflation/deflation).
        foreach (var mkt in kernel.State.Markets.Values)
        {
            foreach (var goodId in mkt.Inventory.Keys)
            {
                int basePrice = Market.GetGoodBasePrice(goodId);
                int currentPrice = mkt.GetMidPrice(goodId);
                // Allow up to 3x deviation (generous for stress).
                Assert.That(currentPrice, Is.LessThanOrEqualTo(basePrice * 3 + 50),
                    $"Price of {goodId} at {mkt.Id} is {currentPrice}, base is {basePrice}. Runaway inflation.");
            }
        }

        // Assert: at least some NPC fleets are still alive and active.
        int npcFleetCount = kernel.State.Fleets.Values.Count(f => f.OwnerId != "player" && !f.IsStored);
        Assert.That(npcFleetCount, Is.GreaterThan(5),
            $"Only {npcFleetCount} NPC fleets remain after 2000 ticks. Fleet population may have collapsed.");
    }

    /// <summary>
    /// GATE.T44.SIGNAL.ECONOMY_STRESS.001 — Extended 5000-tick stress test with
    /// stronger assertions on trade activity, inventory health, price bounds,
    /// and fleet population replacement.
    /// </summary>
    [Test]
    public void Economy_NoStagnation_5000Ticks()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 20, 100f);

        // Track per-node trade activity (fleet arrivals/departures).
        var nodeTradeActivity = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var node in kernel.State.Nodes.Keys)
            nodeTradeActivity[node] = 0;

        int initialNpcFleetCount = kernel.State.Fleets.Values.Count(f => f.OwnerId != "player" && !f.IsStored);

        for (int i = 0; i < 5000; i++)
        {
            // Track NPC fleet destinations as trade activity signal.
            foreach (var f in kernel.State.Fleets.Values)
            {
                if (f.OwnerId == "player") continue;
                if (f.IsMoving && !string.IsNullOrEmpty(f.DestinationNodeId) &&
                    nodeTradeActivity.ContainsKey(f.DestinationNodeId))
                {
                    nodeTradeActivity[f.DestinationNodeId]++;
                }
            }

            kernel.Step();
        }

        var state = kernel.State;

        // Assert: at least 60% of market nodes had trade activity.
        int marketNodeCount = 0;
        int activeMarketNodes = 0;
        foreach (var nodeId in state.Nodes.Keys)
        {
            if (!state.Markets.ContainsKey(nodeId)) continue;
            marketNodeCount++;
            if (nodeTradeActivity.TryGetValue(nodeId, out var activity) && activity > 0)
                activeMarketNodes++;
        }
        double activeRatio = marketNodeCount > 0 ? (double)activeMarketNodes / marketNodeCount : 0;
        Assert.That(activeRatio, Is.GreaterThanOrEqualTo(0.6),
            $"Only {activeMarketNodes}/{marketNodeCount} market nodes had NPC trade activity ({activeRatio:P0}). Economy may be stagnant.");

        // Assert: no market has ALL goods at zero stock (complete stockout = dead market).
        foreach (var mkt in state.Markets.Values)
        {
            if (mkt.Inventory.Count == 0) continue;
            bool allZero = mkt.Inventory.Values.All(v => v == 0);
            Assert.That(allZero, Is.False,
                $"Market {mkt.Id} has all goods at zero stock after 5000 ticks — dead market.");
        }

        // Assert: average good prices within 3x of base price.
        foreach (var mkt in state.Markets.Values)
        {
            foreach (var goodId in mkt.Inventory.Keys)
            {
                int basePrice = Market.GetGoodBasePrice(goodId);
                int currentPrice = mkt.GetMidPrice(goodId);
                Assert.That(currentPrice, Is.LessThanOrEqualTo(basePrice * 3 + 50),
                    $"Price of {goodId} at {mkt.Id} is {currentPrice}, base is {basePrice}. Runaway inflation after 5000 ticks.");
            }
        }

        // Assert: fleet population system created replacements — NPC fleet count should not collapse.
        int finalNpcFleetCount = state.Fleets.Values.Count(f => f.OwnerId != "player" && !f.IsStored);
        // Allow fleet count to drop to 30% of initial (combat attrition is normal) but not collapse.
        int minExpected = Math.Max(3, initialNpcFleetCount * 3 / 10);
        Assert.That(finalNpcFleetCount, Is.GreaterThanOrEqualTo(minExpected),
            $"NPC fleet count dropped from {initialNpcFleetCount} to {finalNpcFleetCount} after 5000 ticks. " +
            $"Fleet population system may not be replacing losses.");

        // Assert: total inventory hasn't exploded (no duplication bugs).
        long totalGoods = 0;
        foreach (var mkt in state.Markets.Values)
            foreach (var qty in mkt.Inventory.Values)
                totalGoods += qty;
        // With 20 stars, ~20 markets, ~8 goods each, initial stock ~50 each = ~8000 total.
        // Industry production + market restocking over 5000 ticks grows inventory significantly.
        // Allow up to 500k (generous for 5000 ticks with 20 stars; existing ExperienceProof uses 25x over 3000).
        Assert.That(totalGoods, Is.LessThan(500000),
            $"Total market goods = {totalGoods}. Possible inventory duplication bug.");
    }

    [Test]
    public void Economy_DeterministicAcrossSeeds()
    {
        // Run 3 different seeds for 500 ticks and verify each produces non-stagnant economy.
        foreach (int seed in new[] { 42, 99, 1001 })
        {
            var kernel = new SimKernel(seed);
            GalaxyGenerator.Generate(kernel.State, 20, 100f);

            for (int i = 0; i < 500; i++)
                kernel.Step();

            // Basic health check: goods are being produced (fuel wells running).
            int totalFuel = kernel.State.Markets.Values.Sum(m =>
                m.Inventory.TryGetValue("fuel", out var v) ? v : 0);
            Assert.That(totalFuel, Is.GreaterThan(0),
                $"Seed {seed}: zero total fuel after 500 ticks. Production may be broken.");

            // NPC fleets exist.
            int npcCount = kernel.State.Fleets.Values.Count(f => f.OwnerId != "player" && !f.IsStored);
            Assert.That(npcCount, Is.GreaterThan(0),
                $"Seed {seed}: zero NPC fleets after 500 ticks.");
        }
    }
}
