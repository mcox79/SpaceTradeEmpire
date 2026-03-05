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
///
/// All tests share a single [OneTimeSetUp] simulation run per seed (5 seeds × 5000 ticks)
/// to avoid redundant computation. Intermediate data is captured at milestones.
/// </summary>
[TestFixture]
public class EconomyStressTests
{
    private const int StarCount = 12;
    private const float Radius = 100f;

    private static readonly int[] StressSeeds = { 42, 99, 1000, 31337, 77777 };

    // ── Shared simulation data ──

    private class SeedRunData
    {
        public SimState FinalState = null!;
        public List<(int tick, long credits)> CreditSamples = new();
        public List<string> SpreadViolations = new();
        public int ViableRoutesAt2000;
        public long InitialGoodsCount;
        public long GoodsCountAt3000;
        public int AiFleetStartCount;
        public int AiFleetMoveCount;
    }

    private Dictionary<int, SeedRunData> _runData = null!;

    [OneTimeSetUp]
    public void RunStressSimulations()
    {
        _runData = new Dictionary<int, SeedRunData>();

        foreach (var seed in StressSeeds)
        {
            var sim = CreateWithGalaxy(seed);
            var data = new SeedRunData();
            data.InitialGoodsCount = CountTotalGoods(sim.State);

            // Record AI fleet starting positions
            var aiFleetStartNodes = new Dictionary<string, string>();
            foreach (var fleet in sim.State.Fleets.Values)
            {
                if (string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal))
                    continue;
                aiFleetStartNodes[fleet.Id] = fleet.CurrentNodeId ?? "";
            }
            data.AiFleetStartCount = aiFleetStartNodes.Count;

            for (int tick = 0; tick < 5000; tick++)
            {
                // Credit sampling every 100 ticks
                if (tick % 100 == 0)
                    data.CreditSamples.Add((tick, sim.State.PlayerCredits));

                // Spread sampling every 200 ticks in first 2000 ticks
                if (tick < 2000 && tick % 200 == 0)
                {
                    foreach (var market in sim.State.Markets.Values)
                    {
                        foreach (var goodId in market.Inventory.Keys)
                        {
                            int buy = market.GetBuyPrice(goodId);
                            int sell = market.GetSellPrice(goodId);
                            if (buy < sell)
                                data.SpreadViolations.Add(
                                    $"Seed {seed} tick {tick}: spread inverted for {goodId} buy={buy} sell={sell}");
                        }
                    }
                }

                sim.Step();

                int ticksCompleted = tick + 1;

                // Capture AI fleet movement at tick 500
                if (ticksCompleted == 500)
                {
                    foreach (var kvp in aiFleetStartNodes)
                    {
                        if (sim.State.Fleets.TryGetValue(kvp.Key, out var fleet) &&
                            fleet.CurrentNodeId != kvp.Value)
                            data.AiFleetMoveCount++;
                    }
                }

                // Capture viable routes at tick 2000
                if (ticksCompleted == 2000)
                    data.ViableRoutesAt2000 = CountViableRoutes(sim.State);

                // Capture goods count at tick 3000
                if (ticksCompleted == 3000)
                    data.GoodsCountAt3000 = CountTotalGoods(sim.State);
            }

            data.FinalState = sim.State;
            _runData[seed] = data;
        }
    }

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
            var state = _runData[seed].FinalState;
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
            foreach (var market in _runData[seed].FinalState.Markets.Values)
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
            Assert.That(_runData[seed].ViableRoutesAt2000, Is.GreaterThan(0),
                $"Seed {seed}: no viable trade routes after 2000 ticks — economy collapsed");
        }
    }

    // ── Fleet movement ──

    [Test]
    public void AiFleets_MoveWithin500Ticks()
    {
        // AI fleets should move at least once within 500 ticks.
        foreach (var seed in StressSeeds)
        {
            var data = _runData[seed];
            if (data.AiFleetStartCount == 0)
                continue; // No AI fleets in this seed

            Assert.That(data.AiFleetMoveCount, Is.GreaterThan(0),
                $"Seed {seed}: no AI fleet moved in 500 ticks ({data.AiFleetStartCount} fleets)");
        }
    }

    // ── Inventory conservation ──

    [Test]
    public void TotalGoodsUnits_DoNotExplode()
    {
        // Total goods in the economy (markets + fleet cargo) should not grow
        // unboundedly. Allow 25x the initial amount as upper bound.
        foreach (var seed in StressSeeds)
        {
            var data = _runData[seed];

            // Industry (station + planet) produces goods and markets restock, so growth is expected.
            // But >25x initial over 3000 ticks suggests a duplication bug.
            // (Raised from 20x for GATE.S7 planet industry sites with conservative output rates.)
            long maxAllowed = Math.Max(data.InitialGoodsCount * 25, 100000);
            Assert.That(data.GoodsCountAt3000, Is.LessThanOrEqualTo(maxAllowed),
                $"Seed {seed}: total goods exploded from {data.InitialGoodsCount} to {data.GoodsCountAt3000}");
        }
    }

    // ── Spread invariant ──

    [Test]
    public void BuyPrice_AlwaysExceedsSellPrice()
    {
        // The market spread must never invert (buy < sell).
        // Sampled every 200 ticks during the first 2000 ticks of each seed run.
        foreach (var seed in StressSeeds)
        {
            var violations = _runData[seed].SpreadViolations;
            Assert.That(violations, Is.Empty,
                string.Join("\n", violations));
        }
    }

    // ── Deterministic replay (NOT batched — requires dual independent runs) ──

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
        // Sampled every 100 ticks over a 5000-tick run.
        foreach (var seed in StressSeeds)
        {
            foreach (var (tick, credits) in _runData[seed].CreditSamples)
            {
                Assert.That(credits, Is.GreaterThanOrEqualTo(0),
                    $"Seed {seed} tick {tick}: player credits went negative ({credits})");
            }
        }
    }

    // ── Helpers ──

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

    private static int CountViableRoutes(SimState state)
    {
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
        return viableRoutes;
    }
}
