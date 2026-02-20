using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using System.Linq;

namespace SimCore.Tests;

public class GalaxyTests
{
    [Test]
    public void Generation_IsDeterministic()
    {
        var simA = new SimKernel(999);
        GalaxyGenerator.Generate(simA.State, 10, 100f);
        string hashA = simA.State.GetSignature();

        var simB = new SimKernel(999);
        GalaxyGenerator.Generate(simB.State, 10, 100f);
        string hashB = simB.State.GetSignature();

        Assert.That(hashA, Is.EqualTo(hashB));
        Assert.That(simA.State.Nodes.Count, Is.EqualTo(10));
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
        GalaxyGenerator.Generate(simA1.State, 10, 100f);
        var r1 = GalaxyGenerator.BuildFactionSeedReport(simA1.State, 777);

        var simA2 = new SimKernel(777);
        GalaxyGenerator.Generate(simA2.State, 10, 100f);
        var r2 = GalaxyGenerator.BuildFactionSeedReport(simA2.State, 777);

        Assert.That(r1, Is.EqualTo(r2));

        var simB = new SimKernel(778);
        GalaxyGenerator.Generate(simB.State, 10, 100f);
        var r3 = GalaxyGenerator.BuildFactionSeedReport(simB.State, 778);

        Assert.That(r1, Is.Not.EqualTo(r3));
    }
}
