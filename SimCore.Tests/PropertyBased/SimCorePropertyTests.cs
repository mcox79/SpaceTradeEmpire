using NUnit.Framework;
using FsCheck;
using FsCheck.NUnit;
using SimCore;
using SimCore.Systems;
using System;
using System.Collections.Generic;
using System.Linq;

// Alias to disambiguate FsCheck.Gen from SimCore.Gen
using FsGen = FsCheck.Gen;
using GalaxyGen = SimCore.Gen.GalaxyGenerator;

namespace SimCore.Tests.PropertyBased;

/// <summary>
/// Property-based tests for SimCore using FsCheck.
/// These tests verify simulation invariants hold across randomly generated inputs,
/// catching edge cases that fixed-seed tests miss.
/// </summary>
[TestFixture]
public class SimCorePropertyTests
{
    // FsCheck 2.x: seed generator producing ints in [1, 100_000]
    private static Arbitrary<int> SeedArb() =>
        Arb.From(FsGen.Choose(1, 100_000));

    private static Arbitrary<int> TickArb(int min, int max) =>
        Arb.From(FsGen.Choose(min, max));

    // ── Determinism ──

    [FsCheck.NUnit.Property(MaxTest = 20)]
    public Property Determinism_TwoRunsSameSeed_ProduceIdenticalSignature()
    {
        return Prop.ForAll(SeedArb(), TickArb(50, 300), (seed, ticks) =>
        {
            var sim1 = CreateSim(seed);
            var sim2 = CreateSim(seed);

            for (int i = 0; i < ticks; i++)
            {
                sim1.Step();
                sim2.Step();
            }

            return sim1.State.GetSignature() == sim2.State.GetSignature();
        });
    }

    // ── Economy conservation ──

    [FsCheck.NUnit.Property(MaxTest = 30)]
    public Property PlayerCredits_NeverGoNegative_AnySeed()
    {
        return Prop.ForAll(SeedArb(), seed =>
        {
            var sim = CreateSim(seed);

            for (int tick = 0; tick < 500; tick++)
            {
                sim.Step();

                if (sim.State.PlayerCredits < 0)
                    return false.Label($"Credits went negative at tick {tick}: {sim.State.PlayerCredits}");
            }

            return true.ToProperty();
        });
    }

    [FsCheck.NUnit.Property(MaxTest = 30)]
    public Property PlayerCargo_NeverGoNegative_AnySeed()
    {
        return Prop.ForAll(SeedArb(), seed =>
        {
            var sim = CreateSim(seed);

            for (int tick = 0; tick < 500; tick++)
            {
                sim.Step();

                foreach (var kv in sim.State.PlayerCargo)
                {
                    if (kv.Value < 0)
                        return false.Label($"Cargo {kv.Key} went negative at tick {tick}: {kv.Value}");
                }
            }

            return true.ToProperty();
        });
    }

    [FsCheck.NUnit.Property(MaxTest = 30)]
    public Property MarketInventory_NeverGoNegative_AnySeed()
    {
        return Prop.ForAll(SeedArb(), seed =>
        {
            var sim = CreateSim(seed);

            for (int tick = 0; tick < 500; tick++)
            {
                sim.Step();

                foreach (var market in sim.State.Markets.Values)
                {
                    foreach (var kv in market.Inventory)
                    {
                        if (kv.Value < 0)
                            return false.Label($"Market inventory {kv.Key} negative at tick {tick}: {kv.Value}");
                    }
                }
            }

            return true.ToProperty();
        });
    }

    // ── Price invariants ──

    [FsCheck.NUnit.Property(MaxTest = 30)]
    public Property BuyPrice_AlwaysGreaterOrEqual_SellPrice_AnySeed()
    {
        return Prop.ForAll(SeedArb(), seed =>
        {
            var sim = CreateSim(seed);

            for (int tick = 0; tick < 500; tick++)
            {
                sim.Step();

                if (tick % 50 == 0)
                {
                    foreach (var market in sim.State.Markets.Values)
                    {
                        foreach (var goodId in market.Inventory.Keys)
                        {
                            int buy = market.GetBuyPrice(goodId);
                            int sell = market.GetSellPrice(goodId);
                            if (buy < sell)
                                return false.Label($"Spread inverted: buy={buy} < sell={sell} for {goodId} at tick {tick}");
                        }
                    }
                }
            }

            return true.ToProperty();
        });
    }

    [FsCheck.NUnit.Property(MaxTest = 30)]
    public Property Prices_StayPositive_AnySeed()
    {
        return Prop.ForAll(SeedArb(), seed =>
        {
            var sim = CreateSim(seed);

            for (int tick = 0; tick < 500; tick++)
            {
                sim.Step();

                if (tick % 50 == 0)
                {
                    foreach (var market in sim.State.Markets.Values)
                    {
                        foreach (var goodId in market.Inventory.Keys)
                        {
                            int buy = market.GetBuyPrice(goodId);
                            int sell = market.GetSellPrice(goodId);
                            if (buy < 1 || sell < 1)
                                return false.Label($"Price below 1: buy={buy} sell={sell} for {goodId} at tick {tick}");
                        }
                    }
                }
            }

            return true.ToProperty();
        });
    }

    // ── Economy doesn't explode ──

    [FsCheck.NUnit.Property(MaxTest = 20, Replay = "1,1")]
    public Property TotalGoods_DontExplode_AnySeed()
    {
        return Prop.ForAll(SeedArb(), seed =>
        {
            var sim = CreateSim(seed);
            long initialGoods = CountTotalGoods(sim.State);

            for (int tick = 0; tick < 1000; tick++)
                sim.Step();

            long finalGoods = CountTotalGoods(sim.State);
            long maxAllowed = Math.Max(initialGoods * 25, 100_000);

            return (finalGoods <= maxAllowed)
                .Label($"Goods exploded: {initialGoods} -> {finalGoods} (max {maxAllowed})");
        });
    }

    // ── Pending intent ordering ──

    [FsCheck.NUnit.Property(MaxTest = 30)]
    public Property PendingIntents_StrictlyIncreasingSeq_AnySeed()
    {
        return Prop.ForAll(SeedArb(), seed =>
        {
            var sim = CreateSim(seed);

            for (int tick = 0; tick < 500; tick++)
            {
                sim.Step();

                if (sim.State.PendingIntents.Count > 1)
                {
                    var seqs = sim.State.PendingIntents.Select(x => x.Seq).ToArray();
                    for (int i = 1; i < seqs.Length; i++)
                    {
                        if (seqs[i] <= seqs[i - 1])
                            return false.Label($"Intent seq not increasing at tick {tick}: {seqs[i - 1]} >= {seqs[i]}");
                    }
                }
            }

            return true.ToProperty();
        });
    }

    // ── Save/Load round-trip ──

    [FsCheck.NUnit.Property(MaxTest = 15, Replay = "1,1")]
    public Property SaveLoad_RoundTrip_PreservesSignature()
    {
        return Prop.ForAll(SeedArb(), TickArb(50, 200), (seed, ticks) =>
        {
            var sim = CreateSim(seed);
            for (int i = 0; i < ticks; i++)
                sim.Step();

            string sigBefore = sim.State.GetSignature();
            string saved = sim.SaveToString();

            var sim2 = new SimKernel(seed);
            sim2.LoadFromString(saved);
            string sigAfter = sim2.State.GetSignature();

            return (sigBefore == sigAfter)
                .Label($"Signature mismatch after save/load at tick {ticks}");
        });
    }

    // ── Ledger transfer conservation ──

    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property LedgerTransfer_ConservesTotal()
    {
        return Prop.ForAll(
            Arb.From(FsGen.Choose(0, 1000)),
            Arb.From(FsGen.Choose(0, 1000)),
            Arb.From(FsGen.Choose(1, 500)),
            (srcQty, dstQty, transfer) =>
            {
                var src = new Dictionary<string, int> { ["ore"] = srcQty };
                var dst = new Dictionary<string, int> { ["ore"] = dstQty };
                int totalBefore = srcQty + dstQty;

                bool ok = InventoryLedger.TryTransferMarket(src, dst, "ore", transfer);

                int totalAfter = InventoryLedger.Get(src, "ore") + InventoryLedger.Get(dst, "ore");

                if (transfer > srcQty)
                    return (!ok).Label("Should reject transfer exceeding source");

                return (ok && totalAfter == totalBefore)
                    .Label($"Conservation violated: {totalBefore} -> {totalAfter}");
            });
    }

    // ── Fleet positions valid ──

    [FsCheck.NUnit.Property(MaxTest = 20)]
    public Property FleetPositions_AlwaysOnValidNodeOrInTransit()
    {
        return Prop.ForAll(SeedArb(), seed =>
        {
            var sim = CreateSim(seed);
            var validNodeIds = new HashSet<string>(sim.State.Nodes.Keys, StringComparer.Ordinal);

            for (int tick = 0; tick < 300; tick++)
            {
                sim.Step();

                foreach (var fleet in sim.State.Fleets.Values)
                {
                    if (string.IsNullOrEmpty(fleet.CurrentNodeId))
                        continue; // in-transit fleets may have null current node

                    if (!validNodeIds.Contains(fleet.CurrentNodeId))
                        return false.Label($"Fleet {fleet.Id} at invalid node '{fleet.CurrentNodeId}' tick {tick}");
                }
            }

            return true.ToProperty();
        });
    }

    // ── Helpers ──

    private static SimKernel CreateSim(int seed)
    {
        var sim = new SimKernel(seed);
        GalaxyGen.Generate(sim.State, 12, 100f);
        return sim;
    }

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
