using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using System.Linq;
using System.IO;
using System.Text;

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
}
