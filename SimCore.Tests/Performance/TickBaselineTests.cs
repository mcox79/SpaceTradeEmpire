using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Gen;

namespace SimCore.Tests.Performance;

// GATE.X.PERF.TICK_BASELINE.001: Tick budget performance baseline.
[TestFixture]
[Category("Performance")]
public sealed class TickBaselineTests
{
    private const int WarmUpTicks = 50;
    private const int MeasuredTicks = 1000;
    private const int NodeCount = 12;
    private const float MapRadius = 100f;
    private const double BudgetMsMean = 3.0;
    private const double BudgetMsP99 = 10.0;

    [Test]
    public void TickBaseline_1000Ticks_MeanAndPercentiles()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, NodeCount, MapRadius);
        kernel.State.PlayerCredits = 1_000_000;

        // Warm up (JIT, first-tick allocations).
        for (int i = 0; i < WarmUpTicks; i++)
            kernel.Step();

        // Measure individual tick durations.
        var tickDurations = new double[MeasuredTicks];
        var sw = new Stopwatch();

        for (int i = 0; i < MeasuredTicks; i++)
        {
            sw.Restart();
            kernel.Step();
            sw.Stop();
            tickDurations[i] = sw.Elapsed.TotalMilliseconds;
        }

        Array.Sort(tickDurations);

        double mean = tickDurations.Average();
        double p50 = Percentile(tickDurations, 50);
        double p95 = Percentile(tickDurations, 95);
        double p99 = Percentile(tickDurations, 99);
        double min = tickDurations[0];
        double max = tickDurations[^1];
        double total = tickDurations.Sum();

        // Write baseline report to docs/generated/.
        string reportPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "docs", "generated", "tick_baseline_v0.txt");
        reportPath = Path.GetFullPath(reportPath);

        var lines = new List<string>
        {
            "# Tick Baseline Report (auto-generated)",
            $"Date:       {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            $"Seed:       42",
            $"Nodes:      {NodeCount}",
            $"Warm-up:    {WarmUpTicks} ticks",
            $"Measured:   {MeasuredTicks} ticks",
            $"Fleets:     {kernel.State.Fleets.Count}",
            $"Markets:    {kernel.State.Markets.Count}",
            "",
            "## Tick Duration (ms)",
            $"  Mean:   {mean:F4}",
            $"  P50:    {p50:F4}",
            $"  P95:    {p95:F4}",
            $"  P99:    {p99:F4}",
            $"  Min:    {min:F4}",
            $"  Max:    {max:F4}",
            $"  Total:  {total:F2} ms ({total / 1000.0:F3} s)",
            "",
            "## Budget",
            $"  Mean budget:  {BudgetMsMean} ms — {(mean < BudgetMsMean ? "PASS" : "FAIL")}",
            $"  P99 budget:   {BudgetMsP99} ms — {(p99 < BudgetMsP99 ? "PASS" : "FAIL")}",
        };

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            File.WriteAllLines(reportPath, lines);
            TestContext.Out.WriteLine($"Baseline report written to: {reportPath}");
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"Could not write report: {ex.Message}");
        }

        // Print to test output for CI visibility.
        foreach (var line in lines)
            TestContext.Out.WriteLine(line);

        Assert.That(mean, Is.LessThan(BudgetMsMean),
            $"Mean tick {mean:F4}ms exceeds {BudgetMsMean}ms budget");
        Assert.That(p99, Is.LessThan(BudgetMsP99),
            $"P99 tick {p99:F4}ms exceeds {BudgetMsP99}ms budget");
    }

    private static double Percentile(double[] sorted, int pct)
    {
        double rank = pct / 100.0 * (sorted.Length - 1);
        int lo = (int)Math.Floor(rank);
        int hi = (int)Math.Ceiling(rank);
        if (lo == hi) return sorted[lo];
        double frac = rank - lo;
        return sorted[lo] * (1 - frac) + sorted[hi] * frac;
    }
}
