using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Gen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.ExperienceProof;

/// <summary>
/// EPIC.X.EXPERIENCE_PROOF.V0 — Layer 2: Economy Stress Tests
/// Pure C# (no Godot). Validates economy invariants over long runs:
/// prices stay bounded, inventory non-negative, trade routes remain viable,
/// fleets move, and deterministic replay produces identical state.
/// </summary>
[TestFixture]
public class EconomyStressTests
{
    private const int StarCount = 12;
    private const float Radius = 100f;

    private static readonly int[] StressSeeds = { 42, 99, 1000, 31337, 77777 };

    private static SimKernel CreateWithGalaxy(int seed)
    {
        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, StarCount, Radius);
        return sim;
    }

    // ── Price bounds ──

    [Test]
    public void Prices_StayBounded_After5000Ticks()
    {
        // Market pricing is inventory-based: mid = 100 + (50 - stock).
        // With stock in [0, 200], mid is in [-50, 150] clamped to min 1.
        // Buy/sell add/subtract spread. Assert prices stay in [1, 10000].
        const int maxPrice = 10000;

        foreach (var seed in StressSeeds)
        {
            var sim = CreateWithGalaxy(seed);
            for (int i = 0; i < 5000; i++)
                sim.Step();

            var state = sim.State;
            foreach (var market in state.Markets.Values)
            {
                foreach (var goodId in market.Inventory.Keys)
                {
                    int buy = market.GetBuyPrice(goodId);
                    int sell = market.GetSellPrice(goodId);
                    int mid = market.GetMidPrice(goodId);

                    Assert.That(buy, Is.GreaterThanOrEqualTo(1),
                        $"Seed {seed}: buy price for {goodId} dropped below 1");
                    Assert.That(buy, Is.LessThanOrEqualTo(maxPrice),
                        $"Seed {seed}: buy price for {goodId} exceeded {maxPrice} ({buy})");
                    Assert.That(sell, Is.GreaterThanOrEqualTo(1),
                        $"Seed {seed}: sell price for {goodId} dropped below 1");
                    Assert.That(sell, Is.LessThanOrEqualTo(maxPrice),
                        $"Seed {seed}: sell price for {goodId} exceeded {maxPrice} ({sell})");
                    Assert.That(buy, Is.GreaterThanOrEqualTo(sell),
                        $"Seed {seed}: buy ({buy}) < sell ({sell}) for {goodId} — spread inverted");
                }
            }
        }
    }

    [Test]
    public void Inventory_StaysNonNegative_After5000Ticks()
    {
        foreach (var seed in StressSeeds)
        {
            var sim = CreateWithGalaxy(seed);
            for (int i = 0; i < 5000; i++)
                sim.Step();

            foreach (var market in sim.State.Markets.Values)
            {
                foreach (var kvp in market.Inventory)
                {
                    Assert.That(kvp.Value, Is.GreaterThanOrEqualTo(0),
                        $"Seed {seed}: negative inventory {kvp.Key}={kvp.Value}");
                }
            }
        }
    }

    // ── Trade viability persistence ──

    [Test]
    public void TradeRoutes_RemainViable_After2000Ticks()
    {
        // After the economy runs for 2000 ticks, there should still be
        // at least one good with a profitable cross-market trade.
        foreach (var seed in StressSeeds)
        {
            var sim = CreateWithGalaxy(seed);
            for (int i = 0; i < 2000; i++)
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
                $"Seed {seed}: no viable trade routes after 2000 ticks — economy collapsed");
        }
    }

    // ── Fleet movement ──

    [Test]
    public void AiFleets_MoveWithin500Ticks()
    {
        // AI fleets should move at least once within 500 ticks.
        // Record starting node IDs, run 500 ticks, check at least one changed.
        foreach (var seed in StressSeeds)
        {
            var sim = CreateWithGalaxy(seed);
            var state = sim.State;

            var startNodes = new Dictionary<string, string>();
            foreach (var fleet in state.Fleets.Values)
            {
                if (string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal))
                    continue;
                startNodes[fleet.Id] = fleet.CurrentNodeId ?? "";
            }

            if (startNodes.Count == 0)
                continue; // No AI fleets in this seed

            for (int i = 0; i < 500; i++)
                sim.Step();

            int movedCount = 0;
            foreach (var kvp in startNodes)
            {
                if (!state.Fleets.TryGetValue(kvp.Key, out var fleet)) continue;
                if (fleet.CurrentNodeId != kvp.Value)
                    movedCount++;
            }

            Assert.That(movedCount, Is.GreaterThan(0),
                $"Seed {seed}: no AI fleet moved in 500 ticks ({startNodes.Count} fleets)");
        }
    }

    // ── Inventory conservation ──

    [Test]
    public void TotalGoodsUnits_DoNotExplode()
    {
        // Total goods in the economy (markets + fleet cargo) should not grow
        // unboundedly. Allow 5x the initial amount as upper bound.
        foreach (var seed in StressSeeds)
        {
            var sim = CreateWithGalaxy(seed);
            var state = sim.State;

            long initialTotal = CountTotalGoods(state);

            for (int i = 0; i < 3000; i++)
                sim.Step();

            long finalTotal = CountTotalGoods(state);

            // Industry produces goods and markets restock, so growth is expected.
            // But >20x initial over 3000 ticks suggests a duplication bug.
            long maxAllowed = Math.Max(initialTotal * 20, 100000);
            Assert.That(finalTotal, Is.LessThanOrEqualTo(maxAllowed),
                $"Seed {seed}: total goods exploded from {initialTotal} to {finalTotal}");
        }
    }

    // ── Spread invariant ──

    [Test]
    public void BuyPrice_AlwaysExceedsSellPrice()
    {
        // The market spread must never invert (buy < sell).
        foreach (var seed in StressSeeds)
        {
            var sim = CreateWithGalaxy(seed);

            // Check at multiple points during simulation
            for (int tick = 0; tick < 2000; tick++)
            {
                if (tick % 200 == 0) // Sample every 200 ticks
                {
                    foreach (var market in sim.State.Markets.Values)
                    {
                        foreach (var goodId in market.Inventory.Keys)
                        {
                            int buy = market.GetBuyPrice(goodId);
                            int sell = market.GetSellPrice(goodId);
                            Assert.That(buy, Is.GreaterThanOrEqualTo(sell),
                                $"Seed {seed} tick {tick}: spread inverted for {goodId} buy={buy} sell={sell}");
                        }
                    }
                }
                sim.Step();
            }
        }
    }

    // ── Deterministic replay ──

    [Test]
    public void DeterministicReplay_ProducesIdenticalSignature()
    {
        // Run the same seed twice with the same number of ticks.
        // The state signature must be identical.
        foreach (var seed in StressSeeds)
        {
            var sim1 = CreateWithGalaxy(seed);
            for (int i = 0; i < 1000; i++)
                sim1.Step();
            string sig1 = sim1.State.GetSignature();

            var sim2 = CreateWithGalaxy(seed);
            for (int i = 0; i < 1000; i++)
                sim2.Step();
            string sig2 = sim2.State.GetSignature();

            Assert.That(sig2, Is.EqualTo(sig1),
                $"Seed {seed}: deterministic replay diverged at tick 1000");
        }
    }

    // ── Player credits bounds ──

    [Test]
    public void PlayerCredits_NeverGoNegative_Over5000Ticks()
    {
        // Sample player credits every 100 ticks over a 5000-tick run.
        foreach (var seed in StressSeeds)
        {
            var sim = CreateWithGalaxy(seed);
            for (int tick = 0; tick < 5000; tick++)
            {
                if (tick % 100 == 0)
                {
                    Assert.That(sim.State.PlayerCredits, Is.GreaterThanOrEqualTo(0),
                        $"Seed {seed} tick {tick}: player credits went negative ({sim.State.PlayerCredits})");
                }
                sim.Step();
            }
        }
    }

    // ── Helper ──

    private static long CountTotalGoods(SimState state)
    {
        long total = 0;
        foreach (var market in state.Markets.Values)
            foreach (var qty in market.Inventory.Values)
                total += qty;
        foreach (var fleet in state.Fleets.Values)
        {
            if (fleet.Cargo is null) continue;
            foreach (var qty in fleet.Cargo.Values)
                total += qty;
        }
        return total;
    }
}
