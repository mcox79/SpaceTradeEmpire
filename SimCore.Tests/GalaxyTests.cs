using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using SimCore.Schemas;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System;

namespace SimCore.Tests;

public class GalaxyTests
{
    [Test]
    public void Generation_IsDeterministic()
    {
        var simA = new SimKernel(999);
        GalaxyGenerator.Generate(simA.State, 12, 100f);
        string hashA = simA.State.GetSignature();

        var simB = new SimKernel(999);
        GalaxyGenerator.Generate(simB.State, 12, 100f);
        string hashB = simB.State.GetSignature();

        Assert.That(hashA, Is.EqualTo(hashB));
        Assert.That(simA.State.Nodes.Count, Is.EqualTo(12));
        Assert.That(simA.State.Edges.Count, Is.GreaterThanOrEqualTo(18));

        // Risk scalar default is emitted as r=0; the Edge model does not store risk yet (default assumed).

        var dumpA = GalaxyGenerator.BuildTopologyDump(simA.State);

        var simA3 = new SimKernel(999);
        GalaxyGenerator.Generate(simA3.State, 12, 100f);
        var dumpA3 = GalaxyGenerator.BuildTopologyDump(simA3.State);

        Assert.That(dumpA, Is.EqualTo(dumpA3));
        // Write dump to repo-root docs/generated (dotnet test working dir is not repo root).
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                break;

            dir = dir.Parent;
        }

        if (dir == null)
        {
            Assert.Fail("Could not locate repo root containing .git from AppContext.BaseDirectory.");
            return; // keeps compiler happy; Assert.Fail will throw, but compiler doesn't assume that
        }

        var outDir = Path.Combine(dir.FullName, "docs", "generated");
        Directory.CreateDirectory(outDir);

        var outPath = Path.Combine(outDir, "galaxy_topology_dump_seed_999_starcount_12_radius_100.txt");
        File.WriteAllText(outPath, dumpA, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        TestContext.WriteLine($"WROTE_TOPOLOGY_DUMP: {outPath}");
    }

    [Test]
    public void Generation_CreatesValidMarkets_WithGoods()
    {
        var sim = new SimKernel(123);
        GalaxyGenerator.Generate(sim.State, 5, 50f);

        var firstNode = sim.State.Nodes.Values.First();
        Assert.That(sim.State.Markets.ContainsKey(firstNode.MarketId), Is.True);

        var market = sim.State.Markets[firstNode.MarketId];

        // ASSERT: Market has dictionary inventory, not int
        Assert.That(market.Inventory.ContainsKey("fuel"), Is.True);
        Assert.That(market.Inventory["fuel"], Is.GreaterThan(0));
    }

    [Test]
    public void FactionSeeding_Report_IsDeterministic_AndDiffsAcrossSeeds()
    {
        var simA1 = new SimKernel(777);
        GalaxyGenerator.Generate(simA1.State, 12, 100f);
        var r1 = GalaxyGenerator.BuildFactionSeedReport(simA1.State, 777);

        var simA2 = new SimKernel(777);
        GalaxyGenerator.Generate(simA2.State, 12, 100f);
        var r2 = GalaxyGenerator.BuildFactionSeedReport(simA2.State, 777);

        Assert.That(r1, Is.EqualTo(r2));

        var simB = new SimKernel(778);
        GalaxyGenerator.Generate(simB.State, 12, 100f);
        var r3 = GalaxyGenerator.BuildFactionSeedReport(simB.State, 778);

        Assert.That(r1, Is.Not.EqualTo(r3));
    }

    [Test]
    public void DiscoverySeedingV0_Report_IsDeterministic_AndDiffsAcrossSeeds()
    {
        var simA1 = new SimKernel(901);
        GalaxyGenerator.Generate(simA1.State, 12, 100f);
        var r1 = BuildDiscoverySeedingReportV0(simA1, seed: 901);

        var simA2 = new SimKernel(901);
        GalaxyGenerator.Generate(simA2.State, 12, 100f);
        var r2 = BuildDiscoverySeedingReportV0(simA2, seed: 901);

        Assert.That(r1, Is.EqualTo(r2));

        var simB = new SimKernel(902);
        GalaxyGenerator.Generate(simB.State, 12, 100f);
        var r3 = BuildDiscoverySeedingReportV0(simB, seed: 902);

        Assert.That(r1, Is.Not.EqualTo(r3));
    }

    [Test]
    public void DiscoverySeedingV0_Batch_Seeds1To100_HasNoViolations()
    {
        for (int seed = 1; seed <= 100; seed++)
        {
            var sim = new SimKernel(seed);
            GalaxyGenerator.Generate(sim.State, 20, 100f);

            var report = BuildDiscoverySeedingReportV0(sim, seed);

            const string needle = "ViolationsCount=";
            var ix = report.LastIndexOf(needle, StringComparison.Ordinal);
            Assert.That(ix, Is.GreaterThanOrEqualTo(0), "Missing ViolationsCount line.\n" + report);

            var line = report.Substring(ix).Split('\n')[0];
            var countText = line.Substring(needle.Length).Trim();
            var count = int.Parse(countText);

            Assert.That(count, Is.EqualTo(0), report);
        }
    }

    // GATE.S2_5.WGEN.DISCOVERY_SEEDING.002: deterministic anomaly families + resource marker seeding v0 report.
    // Required violation row ordering:
    // - sort by ReasonCode, then PrimaryId, then Seed (all ordinal / numeric asc as applicable).
    private static string BuildDiscoverySeedingReportV0(SimKernel sim, int seed)
    {
        // World class map (deterministic; matches world class v0 assignment rule).
        var classByNode = GalaxyGenerator.GetWorldClassIdByNodeIdV0(sim.State);

        // Resource marker seeds from existing deterministic surface (industry outputs).
        var surface = GalaxyGenerator.BuildDiscoverySeedSurfaceV0(sim.State, seed);
        var resourceMarkers = surface
            .Where(s => string.Equals(s.DiscoveryKind, DiscoverySeedKindsV0.ResourcePoolMarker, StringComparison.Ordinal))
            .ToList();

        // Deterministic per-class anomaly family seed: pick 1 host node per WorldClass by stable score.
        // NOTE: this is report-only; no state mutation, no wall-clock, no global RNG.
        var families = new[] { "DERELICT", "RUIN", "SIGNAL" };

        // Deterministic ordering for class iteration.
        var classes = classByNode.Values.Distinct().ToList();
        classes.Sort(StringComparer.Ordinal);

        var nodesByClass = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var wc in classes) nodesByClass[wc] = new List<string>();

        foreach (var kv in classByNode.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            nodesByClass[kv.Value].Add(kv.Key);
        }

        foreach (var wc in classes)
        {
            nodesByClass[wc].Sort(StringComparer.Ordinal);
        }

        var anomalyRows = new List<(int Seed, string WorldClass, string Family, string NodeId, string AnomalyId)>();

        foreach (var wc in classes)
        {
            var candidates = nodesByClass[wc];
            if (candidates.Count == 0) continue;

            // Choose the lowest score node (seed, nodeId, quantized position).
            string bestNode = candidates[0];
            uint bestScore = uint.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                var nid = candidates[i];
                var n = sim.State.Nodes[nid];

                var qx = Quantize1e3(n.Position.X);
                var qz = Quantize1e3(n.Position.Z);

                uint score = Fnv1a32Utf8($"{seed}|{nid}|{qx}|{qz}");
                if (score < bestScore)
                {
                    bestScore = score;
                    bestNode = nid;
                }
            }

            // Family selection is deterministic from (seed, worldClass) only.
            uint fhash = Fnv1a32Utf8($"{seed}|{wc}|anomaly_family_v0");
            var fam = families[(int)(fhash % (uint)families.Length)];

            var anomalyId = $"anom_v0|{fam}|{bestNode}";
            anomalyRows.Add((seed, wc, fam, bestNode, anomalyId));
        }

        // Per-class guarantee checks.
        var resourceCountByClass = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var wc in classes) resourceCountByClass[wc] = 0;

        foreach (var rm in resourceMarkers.OrderBy(r => r.DiscoveryId, StringComparer.Ordinal))
        {
            if (!classByNode.TryGetValue(rm.NodeId, out var wc)) continue;
            resourceCountByClass[wc] = resourceCountByClass[wc] + 1;
        }

        var anomaliesByClass = anomalyRows
            .GroupBy(a => a.WorldClass)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        var violations = new List<(int Seed, string WorldClass, string ReasonCode, string PrimaryId)>();

        foreach (var wc in classes)
        {
            if (!resourceCountByClass.TryGetValue(wc, out var rc) || rc <= 0)
            {
                violations.Add((seed, wc, "MISSING_RESOURCE_MARKER", wc));
            }

            if (!anomaliesByClass.TryGetValue(wc, out var ac) || ac <= 0)
            {
                violations.Add((seed, wc, "MISSING_ANOMALY_FAMILY", wc));
            }
        }

        // Deterministic sort: ReasonCode, PrimaryId, Seed.
        violations.Sort((a, b) =>
        {
            int c = string.CompareOrdinal(a.ReasonCode, b.ReasonCode);
            if (c != 0) return c;

            c = string.CompareOrdinal(a.PrimaryId, b.PrimaryId);
            if (c != 0) return c;

            return a.Seed.CompareTo(b.Seed);
        });

        // Deterministic anomaly row ordering for diff-friendliness (not required by the gate, but keeps report stable).
        anomalyRows.Sort((a, b) =>
        {
            int c = string.CompareOrdinal(a.WorldClass, b.WorldClass);
            if (c != 0) return c;
            return string.CompareOrdinal(a.AnomalyId, b.AnomalyId);
        });

        var sb = new StringBuilder();
        sb.Append("DISCOVERY_SEEDING_V0").Append('\n');
        sb.Append("Seed=").Append(seed).Append('\n');
        sb.Append("WorldClasses=").Append(classes.Count).Append('\n');

        sb.Append("ANOMALIES").Append('\n');
        sb.Append("Seed\tWorldClass\tFamily\tNodeId\tAnomalyId").Append('\n');
        for (int i = 0; i < anomalyRows.Count; i++)
        {
            var a = anomalyRows[i];
            sb.Append(a.Seed).Append('\t')
              .Append(a.WorldClass).Append('\t')
              .Append(a.Family).Append('\t')
              .Append(a.NodeId).Append('\t')
              .Append(a.AnomalyId)
              .Append('\n');
        }

        sb.Append("VIOLATIONS").Append('\n');
        sb.Append("Seed\tWorldClass\tReasonCode\tPrimaryId").Append('\n');
        for (int i = 0; i < violations.Count; i++)
        {
            var v = violations[i];
            sb.Append(v.Seed).Append('\t')
              .Append(v.WorldClass).Append('\t')
              .Append(v.ReasonCode).Append('\t')
              .Append(v.PrimaryId)
              .Append('\n');
        }

        sb.Append("Result=").Append(violations.Count == 0 ? "PASS" : "FAIL").Append('\n');
        sb.Append("ViolationsCount=").Append(violations.Count).Append('\n');

        return sb.ToString();
    }

    private static int Quantize1e3(float v) => (int)MathF.Round(v * 1000f);

    private static uint Fnv1a32Utf8(string s)
    {
        unchecked
        {
            uint h = 2166136261;
            var bytes = Encoding.UTF8.GetBytes(s);
            for (int i = 0; i < bytes.Length; i++)
            {
                h ^= bytes[i];
                h *= 16777619;
            }
            return h;
        }
    }
}
