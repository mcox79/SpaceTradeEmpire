using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Gen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SimCore.Tests.Systems;

/// <summary>
/// GATE.S6.FRACTURE_ECON.INVARIANT.001: 100-seed sweep with fracture goods enabled.
/// Asserts lane_volume_fraction > 0.80 and fracture_unique_goods >= 2 per seed.
/// Deterministic, stable ordering, no timestamps, hard-fail on drift.
/// </summary>
[TestFixture]
[Category("FractureEconInvariant")]
public sealed class FractureEconInvariantTests
{
    private const int SeedCount = 100;
    private const int StarCount = 20;
    private const float Radius = 100f;

    private const double MinLaneVolumeFraction = 0.80;
    private const int MinFractureUniqueGoods = 2;

    private static readonly string[] FractureGoodIds = new[]
    {
        WellKnownGoodIds.AnomalySamples,
        WellKnownGoodIds.ExoticCrystals,
        WellKnownGoodIds.SalvagedTech
    }.OrderBy(x => x, StringComparer.Ordinal).ToArray();

    private sealed class SeedResult
    {
        public int Seed;
        public int LaneNodeCount;
        public int FractureNodeCount;
        public long LaneVolume;
        public long TotalVolume;
        public double LaneVolumeFraction;
        public int FractureUniqueGoods;
        public bool PassVolume;
        public bool PassGoods;
        public bool Pass => PassVolume && PassGoods;
    }

    [Test]
    public void FractureEconInvariant_100Seed_LaneVolumeFractionAndUniqueGoods()
    {
        var results1 = RunSweep();
        var results2 = RunSweep();

        // Determinism check.
        for (int i = 0; i < SeedCount; i++)
        {
            var a = results1[i];
            var b = results2[i];
            Assert.That(b.Seed, Is.EqualTo(a.Seed), $"determinism_drift idx={i} field=Seed");
            Assert.That(b.LaneVolume, Is.EqualTo(a.LaneVolume), $"determinism_drift idx={i} field=LaneVolume");
            Assert.That(b.TotalVolume, Is.EqualTo(a.TotalVolume), $"determinism_drift idx={i} field=TotalVolume");
            Assert.That(b.FractureUniqueGoods, Is.EqualTo(a.FractureUniqueGoods), $"determinism_drift idx={i} field=FractureUniqueGoods");
        }

        // Invariant checks.
        var failures = new List<string>();
        foreach (var r in results1)
        {
            if (!r.PassVolume)
                failures.Add($"seed={r.Seed} lane_vol_frac={r.LaneVolumeFraction:F4} lane={r.LaneVolume} total={r.TotalVolume}");
            if (!r.PassGoods)
                failures.Add($"seed={r.Seed} fracture_unique_goods={r.FractureUniqueGoods} min={MinFractureUniqueGoods}");
        }

        Assert.That(failures.Count, Is.EqualTo(0),
            $"FRACTURE_ECON_INVARIANT_FAIL count={failures.Count}\n" + string.Join("\n", failures));
    }

    [Test]
    public void FractureEconInvariant_StableOrdering_AcrossRuns()
    {
        var r1 = RunSweep();
        var r2 = RunSweep();

        var s1 = BuildReport(r1);
        var s2 = BuildReport(r2);

        Assert.That(s2, Is.EqualTo(s1), "Report must be byte-identical across runs.");
    }

    private static SeedResult[] RunSweep()
    {
        var results = new SeedResult[SeedCount];
        for (int seed = 0; seed < SeedCount; seed++)
            results[seed] = EvaluateSeed(seed);
        return results;
    }

    private static SeedResult EvaluateSeed(int seed)
    {
        var sim = new SimKernel(seed);
        var state = sim.State;
        GalaxyGenerator.Generate(state, StarCount, Radius);

        // Inject 2 fracture nodes with fracture goods (small stock, capped).
        var fractureNodeIds = new List<string>();
        for (int fi = 0; fi < 2; fi++)
        {
            var fNodeId = $"fracture_{seed}_{fi}";
            float angle = (seed * 137 + fi * 73) % 360;
            float rad = Radius * 1.5f;
            float x = rad * (float)Math.Cos(angle * Math.PI / 180.0);
            float z = rad * (float)Math.Sin(angle * Math.PI / 180.0);

            state.Nodes.Add(fNodeId, new Node
            {
                Id = fNodeId,
                Position = new Vector3(x, 0, z),
                IsFractureNode = true,
                FractureTier = fi + 1,
                MarketId = fNodeId
            });
            fractureNodeIds.Add(fNodeId);

            var fMarket = new Market { Id = fNodeId };
            for (int gi = 0; gi < FractureGoodIds.Length; gi++)
            {
                // Deterministic stock: small amount to avoid dominating lane economy.
                int stock = 3 + (seed % 5) + gi;
                fMarket.Inventory[FractureGoodIds[gi]] = stock;
            }
            state.Markets.Add(fNodeId, fMarket);
        }

        // Measure: count all goods across all markets.
        long laneVolume = 0;
        long fractureVolume = 0;
        var fractureGoodsPresent = new HashSet<string>(StringComparer.Ordinal);

        foreach (var kv in state.Markets.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var market = kv.Value;
            foreach (var inv in market.Inventory.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                if (inv.Value <= 0) continue;
                if (IsFractureGood(inv.Key))
                {
                    fractureVolume += inv.Value;
                    fractureGoodsPresent.Add(inv.Key);
                }
                else
                {
                    laneVolume += inv.Value;
                }
            }
        }

        long totalVolume = laneVolume + fractureVolume;
        double laneVolFrac = totalVolume > 0 ? (double)laneVolume / totalVolume : 1.0;

        return new SeedResult
        {
            Seed = seed,
            LaneNodeCount = state.Nodes.Values.Count(n => !n.IsFractureNode),
            FractureNodeCount = fractureNodeIds.Count,
            LaneVolume = laneVolume,
            TotalVolume = totalVolume,
            LaneVolumeFraction = laneVolFrac,
            FractureUniqueGoods = fractureGoodsPresent.Count,
            PassVolume = laneVolFrac > MinLaneVolumeFraction,
            PassGoods = fractureGoodsPresent.Count >= MinFractureUniqueGoods
        };
    }

    private static bool IsFractureGood(string goodId)
    {
        return string.Equals(goodId, WellKnownGoodIds.AnomalySamples, StringComparison.Ordinal)
            || string.Equals(goodId, WellKnownGoodIds.ExoticCrystals, StringComparison.Ordinal)
            || string.Equals(goodId, WellKnownGoodIds.SalvagedTech, StringComparison.Ordinal);
    }

    private static string BuildReport(SeedResult[] results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("FRACTURE_ECON_INVARIANT_V0");
        sb.AppendLine("seed|lane_nodes|frac_nodes|lane_vol|total_vol|lane_vol_frac|frac_goods|pass");
        foreach (var r in results)
        {
            sb.AppendLine(
                $"{r.Seed}|{r.LaneNodeCount}|{r.FractureNodeCount}|" +
                $"{r.LaneVolume}|{r.TotalVolume}|{r.LaneVolumeFraction:F6}|" +
                $"{r.FractureUniqueGoods}|{(r.Pass ? "PASS" : "FAIL")}");
        }
        return sb.ToString().Replace("\r\n", "\n");
    }
}
