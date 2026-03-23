using System;
using System.Collections.Generic;
using System.Diagnostics;
using SimCore;
using SimCore.Entities;
using SimCore.Gen;
using SimCore.Systems;
using NUnit.Framework;

namespace SimCore.Tests.Invariants;

// GATE.X.PERF.TICK_BUDGET.001: Assert tick execution stays within performance budget.
// GATE.T50.PERF.TICK_BUDGET.001: Per-system stress tests with 50+ entities.
// Runs multiple seeds to catch worst-case performance regressions.
public class TickBudgetTests
{
    private const int SeedCount = 5;        // STRUCTURAL: number of seeds to test
    private const int TicksPerSeed = 500;   // STRUCTURAL: ticks per seed
    private const double MaxAverageTickMs = 20.0; // STRUCTURAL: max average tick time
    private const int WarmupTicks = 50;     // STRUCTURAL: warmup ticks (excluded from measurement)

    [Test]
    public void AverageTick_WithinBudget_AcrossSeeds()
    {
        var seedResults = new List<(int Seed, double AvgMs, double MaxMs)>();
        var overallTimes = new List<double>();

        for (int seedIdx = 0; seedIdx < SeedCount; seedIdx++)
        {
            int seed = 42 + seedIdx; // STRUCTURAL: deterministic seed sequence
            var kernel = new SimKernel(seed);
            GalaxyGenerator.Generate(kernel.State, 20, 100f);

            // Warmup phase (not measured)
            for (int i = 0; i < WarmupTicks; i++)
                kernel.Step();

            // Measurement phase
            var sw = new Stopwatch();
            double maxMs = 0;

            for (int i = 0; i < TicksPerSeed; i++)
            {
                sw.Restart();
                kernel.Step();
                sw.Stop();

                double elapsed = sw.Elapsed.TotalMilliseconds;
                overallTimes.Add(elapsed);
                if (elapsed > maxMs) maxMs = elapsed;
            }

            double avgMs = 0;
            int start = overallTimes.Count - TicksPerSeed;
            for (int i = start; i < overallTimes.Count; i++)
                avgMs += overallTimes[i];
            avgMs /= TicksPerSeed;

            seedResults.Add((seed, avgMs, maxMs));
        }

        // Compute overall average
        double totalAvg = 0;
        foreach (var t in overallTimes)
            totalAvg += t;
        totalAvg /= overallTimes.Count;

        // Report
        var report = new System.Text.StringBuilder();
        report.AppendLine($"Tick Budget Report ({SeedCount} seeds x {TicksPerSeed} ticks):");
        report.AppendLine($"Overall average: {totalAvg:F3}ms (budget: {MaxAverageTickMs}ms)");
        foreach (var (s, avg, max) in seedResults)
        {
            report.AppendLine($"  Seed {s}: avg={avg:F3}ms, max={max:F3}ms");
        }

        Assert.That(totalAvg, Is.LessThan(MaxAverageTickMs),
            $"Average tick time {totalAvg:F3}ms exceeds budget of {MaxAverageTickMs}ms.\n{report}");
    }

    // GATE.T50.PERF.TICK_BUDGET.001: Per-system stress tests under 50+ entity load.
    private const int StressFleetCount = 60;   // STRUCTURAL: fleet count for stress
    private const int StressTickCount = 200;   // STRUCTURAL: ticks to measure
    private const double MaxSystemTickMs = 5.0; // STRUCTURAL: per-system budget

    private SimKernel CreateStressKernel()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 20, 100f);

        // Add 60 NPC fleets to stress entity iteration.
        var nodeIds = new List<string>(kernel.State.Nodes.Keys);
        for (int i = 0; i < StressFleetCount; i++)
        {
            var nid = nodeIds[i % nodeIds.Count];
            var fleet = new Fleet
            {
                Id = $"stress_npc_{i}",
                OwnerId = $"faction_{i % 3}",
                CurrentNodeId = nid,
                HullHp = 100,
                HullHpMax = 100,
                ShieldHp = 50,
                ShieldHpMax = 50,
                State = FleetState.Docked,
            };
            kernel.State.Fleets[fleet.Id] = fleet;
        }

        // Warmup
        for (int i = 0; i < WarmupTicks; i++)
            kernel.Step();

        return kernel;
    }

    [Test]
    public void NpcTradeSystem_StressLoad_WithinBudget()
    {
        var kernel = CreateStressKernel();
        var sw = new Stopwatch();
        double totalMs = 0;

        for (int i = 0; i < StressTickCount; i++)
        {
            sw.Restart();
            NpcTradeSystem.ProcessNpcTrade(kernel.State);
            sw.Stop();
            totalMs += sw.Elapsed.TotalMilliseconds;
        }

        double avgMs = totalMs / StressTickCount;
        Assert.That(avgMs, Is.LessThan(MaxSystemTickMs),
            $"NpcTradeSystem avg={avgMs:F3}ms exceeds {MaxSystemTickMs}ms with {StressFleetCount} fleets");
    }

    [Test]
    public void IntelSystem_StressLoad_WithinBudget()
    {
        var kernel = CreateStressKernel();
        var sw = new Stopwatch();
        double totalMs = 0;

        for (int i = 0; i < StressTickCount; i++)
        {
            sw.Restart();
            IntelSystem.Process(kernel.State);
            sw.Stop();
            totalMs += sw.Elapsed.TotalMilliseconds;
        }

        double avgMs = totalMs / StressTickCount;
        Assert.That(avgMs, Is.LessThan(MaxSystemTickMs),
            $"IntelSystem avg={avgMs:F3}ms exceeds {MaxSystemTickMs}ms with {StressFleetCount} fleets");
    }

    [Test]
    public void FirstOfficerSystem_StressLoad_WithinBudget()
    {
        var kernel = CreateStressKernel();
        // Promote an FO so the system actually runs trigger checks.
        FirstOfficerSystem.PromoteCandidate(kernel.State, FirstOfficerCandidate.Analyst);

        var sw = new Stopwatch();
        double totalMs = 0;

        for (int i = 0; i < StressTickCount; i++)
        {
            sw.Restart();
            FirstOfficerSystem.Process(kernel.State);
            sw.Stop();
            totalMs += sw.Elapsed.TotalMilliseconds;
        }

        double avgMs = totalMs / StressTickCount;
        Assert.That(avgMs, Is.LessThan(MaxSystemTickMs),
            $"FirstOfficerSystem avg={avgMs:F3}ms exceeds {MaxSystemTickMs}ms with {StressFleetCount} fleets");
    }

    [Test]
    public void LatticeDroneCombatSystem_StressLoad_WithinBudget()
    {
        var kernel = CreateStressKernel();
        var sw = new Stopwatch();
        double totalMs = 0;

        for (int i = 0; i < StressTickCount; i++)
        {
            sw.Restart();
            LatticeDroneCombatSystem.Process(kernel.State);
            sw.Stop();
            totalMs += sw.Elapsed.TotalMilliseconds;
        }

        double avgMs = totalMs / StressTickCount;
        Assert.That(avgMs, Is.LessThan(MaxSystemTickMs),
            $"LatticeDroneCombatSystem avg={avgMs:F3}ms exceeds {MaxSystemTickMs}ms with {StressFleetCount} fleets");
    }
}
