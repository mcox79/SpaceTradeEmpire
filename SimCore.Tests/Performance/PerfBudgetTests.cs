using System;
using NUnit.Framework;
using SimCore;
using SimCore.Gen;

namespace SimCore.Tests.Performance;

// GATE.T46.PERF.MEMORY_BUDGET.001: Memory budget — 50-node galaxy stays under 256 MB.
[TestFixture]
[Category("Performance")]
public sealed class PerfBudgetTests
{
    private const int Seed = 42;
    private const int NodeCount = 50;
    private const float MapRadius = 200f;
    private const int TickCount = 500;
    private const long MemoryBudgetBytes = 256L * 1024 * 1024; // 256 MB
    private const int CollectionSanityLimit = 10_000;

    [Test]
    public void SimState_50NodeGalaxy_MemoryUnder256MB()
    {
        var kernel = new SimKernel(Seed);
        GalaxyGenerator.Generate(kernel.State, NodeCount, MapRadius);
        kernel.State.PlayerCredits = 100_000;

        for (int i = 0; i < TickCount; i++)
            kernel.Step();

        // Force full GC to get accurate measurement.
        long memoryBytes = GC.GetTotalMemory(forceFullCollection: true);

        TestContext.Out.WriteLine($"Memory after {TickCount} ticks ({NodeCount} nodes): {memoryBytes / (1024.0 * 1024.0):F2} MB");

        Assert.That(memoryBytes, Is.LessThan(MemoryBudgetBytes),
            $"Memory {memoryBytes / (1024.0 * 1024.0):F2} MB exceeds 256 MB budget");
    }

    [Test]
    public void SimState_CollectionCounts_Reasonable()
    {
        var kernel = new SimKernel(Seed);
        GalaxyGenerator.Generate(kernel.State, NodeCount, MapRadius);
        kernel.State.PlayerCredits = 100_000;

        for (int i = 0; i < TickCount; i++)
            kernel.Step();

        var state = kernel.State;

        // Log all collection sizes for visibility.
        TestContext.Out.WriteLine($"Fleets:          {state.Fleets.Count}");
        TestContext.Out.WriteLine($"Markets:         {state.Markets.Count}");
        TestContext.Out.WriteLine($"Nodes:           {state.Nodes.Count}");
        TestContext.Out.WriteLine($"Edges:           {state.Edges.Count}");
        TestContext.Out.WriteLine($"IndustrySites:   {state.IndustrySites.Count}");
        TestContext.Out.WriteLine($"IndustryBuilds:  {state.IndustryBuilds.Count}");
        TestContext.Out.WriteLine($"Warfronts:       {state.Warfronts.Count}");
        TestContext.Out.WriteLine($"Megaprojects:    {state.Megaprojects.Count}");
        TestContext.Out.WriteLine($"Embargoes:       {state.Embargoes.Count}");
        TestContext.Out.WriteLine($"InFlightTransfers: {state.InFlightTransfers.Count}");

        // Sanity: no collection should have runaway growth.
        Assert.That(state.Fleets.Count, Is.LessThan(CollectionSanityLimit),
            $"Fleets count {state.Fleets.Count} exceeds sanity limit");
        Assert.That(state.Markets.Count, Is.LessThan(CollectionSanityLimit),
            $"Markets count {state.Markets.Count} exceeds sanity limit");
        Assert.That(state.Nodes.Count, Is.LessThan(CollectionSanityLimit),
            $"Nodes count {state.Nodes.Count} exceeds sanity limit");
        Assert.That(state.Edges.Count, Is.LessThan(CollectionSanityLimit),
            $"Edges count {state.Edges.Count} exceeds sanity limit");
        Assert.That(state.IndustrySites.Count, Is.LessThan(CollectionSanityLimit),
            $"IndustrySites count {state.IndustrySites.Count} exceeds sanity limit");
        Assert.That(state.IndustryBuilds.Count, Is.LessThan(CollectionSanityLimit),
            $"IndustryBuilds count {state.IndustryBuilds.Count} exceeds sanity limit");
        Assert.That(state.Warfronts.Count, Is.LessThan(CollectionSanityLimit),
            $"Warfronts count {state.Warfronts.Count} exceeds sanity limit");
        Assert.That(state.Megaprojects.Count, Is.LessThan(CollectionSanityLimit),
            $"Megaprojects count {state.Megaprojects.Count} exceeds sanity limit");
        Assert.That(state.Embargoes.Count, Is.LessThan(CollectionSanityLimit),
            $"Embargoes count {state.Embargoes.Count} exceeds sanity limit");
        Assert.That(state.InFlightTransfers.Count, Is.LessThan(CollectionSanityLimit),
            $"InFlightTransfers count {state.InFlightTransfers.Count} exceeds sanity limit");
    }
}
