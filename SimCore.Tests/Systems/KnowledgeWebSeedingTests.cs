using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

[TestFixture]
[Category("KnowledgeWebSeeding")]
public sealed class KnowledgeWebSeedingTests
{
    [Test]
    public void DataLogs_SomePreDiscoveredAtStart()
    {
        var sim = new SimKernel(42);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        int discovered = 0;
        foreach (var dl in sim.State.DataLogs.Values)
        {
            if (dl.IsDiscovered) discovered++;
        }

        TestContext.Out.WriteLine($"DataLogs discovered at start: {discovered}/{sim.State.DataLogs.Count}");
        Assert.That(discovered, Is.GreaterThan(0), "At least 1 DataLog should be pre-discovered near player start");
    }

    [Test]
    public void KnowledgeConnections_RevealAfterTicks()
    {
        var sim = new SimKernel(42);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        int totalConnections = sim.State.Intel.KnowledgeConnections.Count;
        TestContext.Out.WriteLine($"Total connections: {totalConnections}");

        // Run enough ticks for KnowledgeGraphSystem to process
        for (int i = 0; i < 10; i++) sim.Step();

        int revealed = 0;
        int questionMarks = 0;
        foreach (var conn in sim.State.Intel.KnowledgeConnections)
        {
            if (conn.IsRevealed) revealed++;
            else if (KnowledgeGraphSystem.IsConnectionVisible(sim.State, conn))
                questionMarks++;
        }

        TestContext.Out.WriteLine($"After 10 ticks: {revealed} revealed, {questionMarks} question_marks");
        Assert.That(revealed + questionMarks, Is.GreaterThan(0),
            "At least 1 connection should be visible when DataLogs are pre-discovered");
    }
}
