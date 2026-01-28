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
        Assert.That(simA.State.Edges.Count, Is.GreaterThan(0));
    }

    [Test]
    public void Generation_CreatesValidMarkets()
    {
        var sim = new SimKernel(123);
        GalaxyGenerator.Generate(sim.State, 5, 50f);

        var firstNode = sim.State.Nodes.Values.First();
        Assert.That(firstNode.MarketId, Is.Not.Empty);
        Assert.That(sim.State.Markets.ContainsKey(firstNode.MarketId), Is.True);
    }
}