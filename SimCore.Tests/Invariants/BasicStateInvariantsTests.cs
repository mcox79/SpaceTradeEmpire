using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimCore.Tests.Invariants;

public class BasicStateInvariantsTests
{
    [Test]
    public void Basic_Invariants_Hold_After_Stepping()
    {
        const int seed = 7;
        const int ticks = 250;

        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        for (int i = 0; i < ticks; i++)
        {
            sim.Step();
        }

        Assert.That(sim.State.PlayerCredits, Is.GreaterThanOrEqualTo(0), "PlayerCredits went negative.");

        foreach (var kv in sim.State.PlayerCargo)
        {
            Assert.That(kv.Value, Is.GreaterThanOrEqualTo(0), $"PlayerCargo {kv.Key} went negative.");
        }

        foreach (var m in sim.State.Markets.Values)
        {
            foreach (var kv in m.Inventory)
            {
                Assert.That(kv.Value, Is.GreaterThanOrEqualTo(0), $"Market inventory {kv.Key} went negative.");
            }
        }

        if (sim.State.PendingIntents.Count > 1)
        {
            var seqs = sim.State.PendingIntents.Select(x => x.Seq).ToArray();
            for (int i = 1; i < seqs.Length; i++)
            {
                Assert.That(seqs[i], Is.GreaterThan(seqs[i - 1]), "Pending intent seq not strictly increasing.");
            }
        }
    }

    [Test]
    public void Worldgen_Bounds_MinProducersAndSinks_Are_Sourced_From_Tweaks_V0()
    {
        const int seed = 7;

        // Starter goods set for v0 bounds checks.
        // Keep this list stable and ordered for deterministic reporting.
        var goods = new[] { "fuel", "ore", "metal" };

        static (bool Pass, string Report) EvaluateBounds(SimState state, IReadOnlyList<string> goodsOrdered)
        {
            var starterNodes = new HashSet<string>(
                GalaxyGenerator.GetStarterRegionNodeIdsSortedV0(state),
                StringComparer.Ordinal
            );

            var producers = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var sinks = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var g in goodsOrdered)
            {
                producers[g] = new HashSet<string>(StringComparer.Ordinal);
                sinks[g] = new HashSet<string>(StringComparer.Ordinal);
            }

            foreach (var site in state.IndustrySites.Values.OrderBy(s => s.Id, StringComparer.Ordinal))
            {
                if (site is null) continue;
                if (!starterNodes.Contains(site.NodeId)) continue;

                if (site.Outputs is not null)
                {
                    foreach (var kv in site.Outputs.OrderBy(k => k.Key, StringComparer.Ordinal))
                    {
                        if (kv.Value <= 0) continue;
                        if (!producers.TryGetValue(kv.Key, out var set)) continue;
                        set.Add(site.NodeId);
                    }
                }

                if (site.Inputs is not null)
                {
                    foreach (var kv in site.Inputs.OrderBy(k => k.Key, StringComparer.Ordinal))
                    {
                        if (kv.Value <= 0) continue;
                        if (!sinks.TryGetValue(kv.Key, out var set)) continue;
                        set.Add(site.NodeId);
                    }
                }
            }

            var minP = Math.Max(0, state.Tweaks.WorldgenMinProducersPerGood);
            var minS = Math.Max(0, state.Tweaks.WorldgenMinSinksPerGood);

            var sb = new StringBuilder(256);
            sb.Append("worldgen_bounds_v0 ");
            sb.Append("minP=").Append(minP).Append(" ");
            sb.Append("minS=").Append(minS);

            bool pass = true;
            foreach (var g in goodsOrdered)
            {
                var p = producers[g].Count;
                var s = sinks[g].Count;
                sb.Append(" | ").Append(g).Append(":P=").Append(p).Append(",S=").Append(s);
                if (p < minP || s < minS) pass = false;
            }

            sb.Append(pass ? " PASS" : " FAIL");
            return (pass, sb.ToString());
        }

        void RunCase(string? tweaksJson, bool enableDistributionSinksV0, bool expectedPass)
        {
            var sim = new SimKernel(seed);
            sim.State.LoadTweaksFromJsonOverride(tweaksJson);

            var opts = new GalaxyGenerator.GalaxyGenOptions
            {
                EnableDistributionSinksV0 = enableDistributionSinksV0
            };

            GalaxyGenerator.Generate(sim.State, 20, 100f, opts);

            var (pass, report) = EvaluateBounds(sim.State, goods);
            Assert.That(pass, Is.EqualTo(expectedPass), report);
        }

        // Case 1: defaults should PASS.
        RunCase(tweaksJson: null, enableDistributionSinksV0: false, expectedPass: true);

        // Case 2: requiring sinks via tweaks forces distribution sinks in generator => PASS even if option is false.
        RunCase(
            tweaksJson: "{\"worldgen_min_producers_per_good\":1,\"worldgen_min_sinks_per_good\":1}",
            enableDistributionSinksV0: false,
            expectedPass: true
        );

        // Case 3: impossible sink requirement => deterministic generator failure with stable report.
        var simFail = new SimKernel(seed);
        simFail.State.LoadTweaksFromJsonOverride("{\"worldgen_min_producers_per_good\":1,\"worldgen_min_sinks_per_good\":4}");
        var ex = Assert.Throws<InvalidOperationException>(() => GalaxyGenerator.Generate(simFail.State, 20, 100f));
        Assert.That(ex!.Message, Does.Contain("worldgen_bounds_v0"));
    }
}
