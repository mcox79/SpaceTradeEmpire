using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using System.Diagnostics;

namespace SimCore.Tests.Performance;

// GATE.S4.PERF_BUDGET.INDUSTRY.001: Industry tick budget — all S4 systems under budget.
public class IndustryPerfBudgetTests
{
    [Test]
    public void AllIndustrySystems_1000Ticks_UnderBudget()
    {
        // Generate a world with industry sites, construction, NPC activity, pressure
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 12, 100f);
        kernel.State.PlayerCredits = 100000;

        // Warm up — first ticks may be slower due to JIT
        for (int i = 0; i < 50; i++)
            kernel.Step();

        // Measure 1000 ticks with all S4 systems active
        // Systems active: ConstructionSystem, NpcIndustrySystem (demand+reaction),
        // ResearchSystem, RefitSystem, MaintenanceSystem, PressureSystem
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
            kernel.Step();
        sw.Stop();

        double avgMsPerTick = sw.Elapsed.TotalMilliseconds / 1000.0;

        // Budget: < 1ms per tick average (generous for CI)
        Assert.That(avgMsPerTick, Is.LessThan(1.0),
            $"Average tick cost {avgMsPerTick:F3}ms exceeds 1ms budget");

        // Total 1000 ticks should be under 1 second
        Assert.That(sw.Elapsed.TotalSeconds, Is.LessThan(1.0),
            $"1000 ticks took {sw.Elapsed.TotalSeconds:F2}s, budget is 1.0s");
    }

    [Test]
    public void PerfBudget_WithActiveConstruction()
    {
        // Scenario with active construction projects to measure construction overhead
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 12, 100f);
        kernel.State.PlayerCredits = 1000000;

        // Start max projects
        var nodes = new System.Collections.Generic.List<string>(kernel.State.Nodes.Keys);
        nodes.Sort(System.StringComparer.Ordinal);
        for (int i = 0; i < System.Math.Min(SimCore.Tweaks.ConstructionTweaksV0.MaxTotalProjects, nodes.Count); i++)
        {
            SimCore.Systems.ConstructionSystem.StartConstruction(kernel.State, "constr_depot_v0", nodes[i]);
        }

        // Warm up
        for (int i = 0; i < 50; i++)
            kernel.Step();

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
            kernel.Step();
        sw.Stop();

        double avgMsPerTick = sw.Elapsed.TotalMilliseconds / 1000.0;
        // Budget raised from 1.0 to 3.0 to accommodate S7 systems (PowerBudget, Sustain, LootDespawn).
        Assert.That(avgMsPerTick, Is.LessThan(3.0),
            $"With active construction, avg tick cost {avgMsPerTick:F3}ms exceeds budget");
    }
}
