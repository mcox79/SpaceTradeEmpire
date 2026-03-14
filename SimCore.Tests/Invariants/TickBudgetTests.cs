using System;
using System.Collections.Generic;
using System.Diagnostics;
using SimCore;
using SimCore.Gen;
using NUnit.Framework;

namespace SimCore.Tests.Invariants;

// GATE.X.PERF.TICK_BUDGET.001: Assert tick execution stays within performance budget.
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
}
