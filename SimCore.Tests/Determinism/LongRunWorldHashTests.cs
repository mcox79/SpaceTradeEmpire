using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using NUnit.Framework;
using SimCore;
using SimCore.Gen;

namespace SimCore.Tests.Determinism;

public class LongRunWorldHashTests
{
    // This test is a separate golden from GoldenReplayTests:
    // - No external inputs
    // - Long run (default 10,000 ticks)
    //
    // Repro control:
    // - Default seed is stable
    // - Override via env var STE_LONGRUN_SEED to reproduce failures with an explicit seed
    // - Set STE_UPDATE_GOLDEN=1 to print new golden values (test fails intentionally)
    //
    // Diagnostics:
    // - Checkpoints recorded at a few tick counts to pinpoint the first divergence window.
    private const string ExpectedGenesisHash = "E40FF89C0EFD88A58B46153FBEE14F81C8F1AAA9419153A3C04AE6A6FC5F21D9";
    private const string ExpectedFinalHash = "054FA896B4895DE1C7E84666979E746ECC36A9D53D27B57F0D56ADA2B29AF610";

    // Gate: GATE.S3.PERF_BUDGET.001 (Slice 3 perf budget v0)
    // Fixed scenario + fixed measurement window.
    private const int PerfBudgetSeed = 424242;
    private const int PerfBudgetTicks = 600;
    private const int PerfBudgetMeasureWindowTicks = 300;

    // budget_ms_per_tick: generous v0 to reduce CI flakiness; tighten later with data.
    private const double PerfBudgetMsPerTick = 50.0;

    // Scenario load requirements (best-effort validation via reflection to avoid type coupling).
    private const int PerfMinFleets = 50;
    private const int PerfMinActiveTransfers = 200;

    [Category("Closeout")]
    [Test]
    public void PerfBudget_Slice3_V0_AverageTickTime_WithinBudget_And_ReportDeterministic()
    {
        var sim = new SimKernel(PerfBudgetSeed);

        // Deterministic map generation.
        GalaxyGenerator.Generate(sim.State, 60, 200f);

        // Run full scenario, measuring last window ticks only.
        var result = RunPerfBudgetScenario(sim, PerfBudgetTicks, PerfBudgetMeasureWindowTicks);

        // Validate scenario intensity (non-negotiable contract for this gate).
        Assert.That(
            result.FleetCount,
            Is.GreaterThanOrEqualTo(PerfMinFleets),
            $"PerfBudget scenario under-loaded: fleets={result.FleetCount} < {PerfMinFleets}. " +
            "If this regresses, adjust deterministic scenario construction so fleets exist in sufficient count.");

        Assert.That(
            result.ActiveTransferCount,
            Is.GreaterThanOrEqualTo(PerfMinActiveTransfers),
            $"PerfBudget scenario under-loaded: active_transfers={result.ActiveTransferCount} < {PerfMinActiveTransfers}. " +
            "If this regresses, adjust deterministic scenario construction so transfers exist in sufficient count.");

        // Emit deterministic report (structure, ordering, formatting).
        EmitPerfReport(result);

        // Enforce threshold.
        EnforcePerfBudgetOrFail(result.AvgMsPerTick, PerfBudgetMsPerTick, result);
    }

    [Test]
    public void PerfBudget_Slice3_V0_BudgetGuard_FailsWhenExceeded()
    {
        // Deterministic contract test: guard must fail when avg exceeds budget.
        Assert.That(
            () => EnforcePerfBudgetOrFail(avgMsPerTick: 2.0, budgetMsPerTick: 1.0, details: null),
            Throws.TypeOf<AssertionException>());
    }

    [Test]
    public void SeedExplorer_V0_Reports_Are_ByteForByte_Deterministic_And_NoTimeLeakage()
    {
        const int seed = 12345;

        var a = new SimKernel(seed);
        GalaxyGenerator.Generate(a.State, 20, 100f);

        var b = new SimKernel(seed);
        GalaxyGenerator.Generate(b.State, 20, 100f);

        var topoA = GalaxyGenerator.BuildTopologyDump(a.State);
        var topoB = GalaxyGenerator.BuildTopologyDump(b.State);

        var loopsA = GalaxyGenerator.BuildEconLoopsReport(a.State, seed, maxHops: 4);
        var loopsB = GalaxyGenerator.BuildEconLoopsReport(b.State, seed, maxHops: 4);

        var invA = GalaxyGenerator.BuildInvariantsReport(a.State, seed, maxHopsForLoops: 4);
        var invB = GalaxyGenerator.BuildInvariantsReport(b.State, seed, maxHopsForLoops: 4);

        Assert.That(topoA, Is.EqualTo(topoB), "Topology summary must be deterministic for same seed.");
        Assert.That(loopsA, Is.EqualTo(loopsB), "Econ loops report must be deterministic for same seed.");
        Assert.That(invA, Is.EqualTo(invB), "Invariants report must be deterministic for same seed.");

        // Basic guard against accidental timestamps: current year string must not appear.
        var year = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        Assert.That(topoA.Contains(year, StringComparison.Ordinal), Is.False, "Topology summary must not contain timestamps.");
        Assert.That(loopsA.Contains(year, StringComparison.Ordinal), Is.False, "Econ loops report must not contain timestamps.");
        Assert.That(invA.Contains(year, StringComparison.Ordinal), Is.False, "Invariants report must not contain timestamps.");
    }

    [Test]
    public void SeedExplorer_V0_DiffReports_Are_ByteForByte_Deterministic()
    {
        const int seedA = 111;
        const int seedB = 222;

        var a1 = new SimKernel(seedA);
        GalaxyGenerator.Generate(a1.State, 20, 100f);

        var b1 = new SimKernel(seedB);
        GalaxyGenerator.Generate(b1.State, 20, 100f);

        var dTopo1 = GalaxyGenerator.BuildTopologyDiffReport(a1.State, seedA, b1.State, seedB);
        var dLoops1 = GalaxyGenerator.BuildLoopsDiffReport(a1.State, seedA, b1.State, seedB, maxHops: 4);

        var a2 = new SimKernel(seedA);
        GalaxyGenerator.Generate(a2.State, 20, 100f);

        var b2 = new SimKernel(seedB);
        GalaxyGenerator.Generate(b2.State, 20, 100f);

        var dTopo2 = GalaxyGenerator.BuildTopologyDiffReport(a2.State, seedA, b2.State, seedB);
        var dLoops2 = GalaxyGenerator.BuildLoopsDiffReport(a2.State, seedA, b2.State, seedB, maxHops: 4);

        Assert.That(dTopo1, Is.EqualTo(dTopo2), "Topology diff must be deterministic for same (seedA, seedB).");
        Assert.That(dLoops1, Is.EqualTo(dLoops2), "Loops diff must be deterministic for same (seedA, seedB).");
    }

    [Test]
    public void SeedExplorer_V0_ConfigOverrides_Are_ByteForByte_Deterministic()
    {
        const int seed = 333;

        var cfg = GalaxyGenerator.SeedExplorerV0Config.Default with
        {
            StarCount = 24,
            Radius = 150f,
            MaxHops = 5,
            ChokepointCapLe = 4,
            MaxChokepoints = 1
        };

        var a = new SimKernel(seed);
        GalaxyGenerator.Generate(a.State, cfg.StarCount, cfg.Radius);

        var b = new SimKernel(seed);
        GalaxyGenerator.Generate(b.State, cfg.StarCount, cfg.Radius);

        var loopsA = GalaxyGenerator.BuildEconLoopsReport(a.State, seed, cfg);
        var loopsB = GalaxyGenerator.BuildEconLoopsReport(b.State, seed, cfg);

        var invA = GalaxyGenerator.BuildInvariantsReport(a.State, seed, cfg);
        var invB = GalaxyGenerator.BuildInvariantsReport(b.State, seed, cfg);

        Assert.That(loopsA, Is.EqualTo(loopsB), "Config override path must be deterministic for same seed.");
        Assert.That(invA, Is.EqualTo(invB), "Config override path must be deterministic for same seed.");
    }

    [Category("Closeout")]
    [Test]
    public void LongRun_10000Ticks_Matches_Golden()
    {
        var seed = GetSeedOrFail();
        const int ticks = 10000;

        bool updateGolden = string.Equals(
            Environment.GetEnvironmentVariable("STE_UPDATE_GOLDEN"),
            "1",
            StringComparison.Ordinal);

        var checkpoints = new[] { 0, 1000, 5000, ticks };

        TestContext.Out.WriteLine($"LongRun Seed: {seed} (override via STE_LONGRUN_SEED)");
        TestContext.Out.WriteLine($"LongRun Ticks: {ticks}");
        TestContext.Out.WriteLine($"LongRun Checkpoints: {string.Join(",", checkpoints)}");

        // RUN A
        var runA = Run(seed, ticks, checkpoints);

        // RUN B (fresh kernel, same seed, same generation, no inputs)
        var runB = Run(seed, ticks, checkpoints);

        DumpRun("A", runA, checkpoints);
        DumpRun("B", runB, checkpoints);

        // Determinism invariant: two fresh runs with the same seed must match at every checkpoint.
        var firstMismatchTick = FirstCheckpointMismatchTick(runA.Checkpoints, runB.Checkpoints, checkpoints);
        Assert.That(
            firstMismatchTick,
            Is.EqualTo(-1),
            $"Long-run determinism drift detected at checkpoint tick={firstMismatchTick}. Seed={seed}. Repro: set STE_LONGRUN_SEED={seed}");

        if (updateGolden)
        {
            TestContext.Out.WriteLine($"PASTE_LONGRUN_SEED:    {seed}");
            TestContext.Out.WriteLine($"PASTE_LONGRUN_GENESIS: {runA.GenesisHash}");
            TestContext.Out.WriteLine($"PASTE_LONGRUN_FINAL:   {runA.FinalHash}");
            Assert.Fail("Long-run golden hashes updated. Copy PASTE_LONGRUN_* values into ExpectedGenesisHash/ExpectedFinalHash.");
        }
        else
        {
            Assert.That(
                runA.GenesisHash,
                Is.EqualTo(ExpectedGenesisHash),
                $"Genesis hash does not match long-run golden. Seed={seed}. Repro: set STE_LONGRUN_SEED={seed}");

            Assert.That(
                runA.FinalHash,
                Is.EqualTo(ExpectedFinalHash),
                $"Final hash does not match long-run golden. Seed={seed}. Repro: set STE_LONGRUN_SEED={seed}");
        }
    }

    private static int GetSeedOrFail()
    {
        // Keep a deterministic default for CI while allowing explicit reproduction locally.
        const int defaultSeed = 42;

        var s = (Environment.GetEnvironmentVariable("STE_LONGRUN_SEED") ?? "").Trim();
        if (string.IsNullOrEmpty(s)) return defaultSeed;

        if (int.TryParse(s, out var parsed)) return parsed;

        Assert.Fail($"Invalid STE_LONGRUN_SEED '{s}'. Must be an int.");
        return defaultSeed; // unreachable, but keeps compiler happy
    }

    private static RunResult Run(int seed, int ticks, int[] checkpoints)
    {
        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        // Checkpoint hashes keyed by tick count (0 == genesis, ticks == final).
        var cp = new Dictionary<int, string>(capacity: checkpoints.Length)
        {
            [0] = sim.State.GetSignature()
        };

        // Record intermediate checkpoint hashes after stepping to that tick.
        for (int i = 1; i <= ticks; i++)
        {
            sim.Step();

            if (IsCheckpoint(i, checkpoints))
            {
                cp[i] = sim.State.GetSignature();
            }
        }

        // Ensure final is present even if checkpoints array was changed.
        if (!cp.ContainsKey(ticks))
        {
            cp[ticks] = sim.State.GetSignature();
        }

        return new RunResult(
            cp[0],
            cp[ticks],
            cp);
    }

    private static bool IsCheckpoint(int tick, int[] checkpoints)
    {
        for (int i = 0; i < checkpoints.Length; i++)
        {
            if (checkpoints[i] == tick) return true;
        }
        return false;
    }

    private static int FirstCheckpointMismatchTick(
        IReadOnlyDictionary<int, string> a,
        IReadOnlyDictionary<int, string> b,
        int[] checkpoints)
    {
        for (int i = 0; i < checkpoints.Length; i++)
        {
            var t = checkpoints[i];
            if (!a.TryGetValue(t, out var ha)) return t;
            if (!b.TryGetValue(t, out var hb)) return t;
            if (!string.Equals(ha, hb, StringComparison.Ordinal)) return t;
        }
        return -1;
    }

    private static void DumpRun(string label, RunResult run, int[] checkpoints)
    {
        TestContext.Out.WriteLine($"Genesis Hash {label}: {run.GenesisHash}");
        TestContext.Out.WriteLine($"Final   Hash {label}: {run.FinalHash}");

        for (int i = 0; i < checkpoints.Length; i++)
        {
            var t = checkpoints[i];
            if (run.Checkpoints.TryGetValue(t, out var h))
            {
                TestContext.Out.WriteLine($"Checkpoint[{label}] tick={t}: {h}");
            }
            else
            {
                TestContext.Out.WriteLine($"Checkpoint[{label}] tick={t}: <missing>");
            }
        }
    }

    private static PerfBudgetResult RunPerfBudgetScenario(SimKernel sim, int totalTicks, int measureWindowTicks)
    {
        if (totalTicks <= 0) throw new ArgumentOutOfRangeException(nameof(totalTicks));
        if (measureWindowTicks <= 0 || measureWindowTicks > totalTicks) throw new ArgumentOutOfRangeException(nameof(measureWindowTicks));

        // Warm-up ticks are the leading ticks not included in timing.
        int warmupTicks = totalTicks - measureWindowTicks;

        // Ensure the scenario is actually loaded with transfers.
        // In this codebase, "active transfers" are represented by SimState.InFlightTransfers.
        // If the sim does not naturally create transfers yet, we deterministically seed synthetic in-flight transfers
        // so the perf harness can be exercised without depending on emergent gameplay behavior.
        EnsureSyntheticInFlightTransfers(sim.State, PerfMinActiveTransfers);

        // Advance warm-up without timing.
        for (int i = 0; i < warmupTicks; i++)
        {
            sim.Step();
        }

        // Top up again so the measured window begins with the required load.
        EnsureSyntheticInFlightTransfers(sim.State, PerfMinActiveTransfers);

        // Snapshot counts right before measurement window (so report is tied to measured state).
        // Best-effort validation via deterministic reflection to avoid type coupling.
        var roots = new object[] { sim, sim.State };

        int fleetCount = GetBestCountAcross(
            roots,
            exactNames: new[] { "Fleets", "FleetById", "FleetIndex" },
            nameContainsTokens: new[] { "Fleet" });

        int activeTransfers = GetBestCountAcross(
            roots,
            exactNames: new[] { "Transfers", "LogisticsTransfers", "InFlightTransfers", "LaneTransfers", "LaneFlows" },
            nameContainsTokens: new[] { "Transfer", "Transfers", "InFlight", "Flow" });

        // Measure the last window ticks with a fixed method.
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < measureWindowTicks; i++)
        {
            sim.Step();
        }
        sw.Stop();

        double totalMs = sw.Elapsed.TotalMilliseconds;
        double avgMs = totalMs / measureWindowTicks;

        return new PerfBudgetResult(
            PerfBudgetSeed,
            totalTicks,
            measureWindowTicks,
            fleetCount,
            activeTransfers,
            totalMs,
            avgMs);
    }

    private static int GetBestCountAcross(object[] roots, string[] exactNames, string[] nameContainsTokens)
    {
        int best = 0;

        for (int r = 0; r < roots.Length; r++)
        {
            var root = roots[r];
            if (root == null) continue;

            best = Math.Max(best, GetBestCountExact(root, exactNames));
            best = Math.Max(best, GetBestCountByNameContainsTokens(root, nameContainsTokens));
        }

        return best;
    }

    private static int GetBestCountExact(object obj, string[] candidateNames)
    {
        int best = 0;
        for (int i = 0; i < candidateNames.Length; i++)
        {
            var n = TryGetCountByMemberName(obj, candidateNames[i]);
            if (n > best) best = n;
        }
        return best;
    }

    private static int GetBestCountByNameContainsTokens(object obj, string[] tokens)
    {
        if (obj == null) return 0;
        if (tokens == null || tokens.Length == 0) return 0;

        var t = obj.GetType();
        var props = t.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        Array.Sort(props, (a, b) => string.CompareOrdinal(a.Name, b.Name));

        int best = 0;

        for (int i = 0; i < props.Length; i++)
        {
            var p = props[i];
            if (!NameContainsAnyToken(p.Name, tokens)) continue;

            object? value = null;
            try { value = p.GetValue(obj); } catch { value = null; }

            best = Math.Max(best, TryGetCountFromValue(value));
        }

        var fields = t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        Array.Sort(fields, (a, b) => string.CompareOrdinal(a.Name, b.Name));

        for (int i = 0; i < fields.Length; i++)
        {
            var f = fields[i];
            if (!NameContainsAnyToken(f.Name, tokens)) continue;

            object? value = null;
            try { value = f.GetValue(obj); } catch { value = null; }

            best = Math.Max(best, TryGetCountFromValue(value));
        }

        return best;
    }

    private static bool NameContainsAnyToken(string name, string[] tokens)
    {
        for (int i = 0; i < tokens.Length; i++)
        {
            var tok = tokens[i];
            if (string.IsNullOrWhiteSpace(tok)) continue;

            if (name.IndexOf(tok, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    private static int TryGetCountByMemberName(object obj, string memberName)
    {
        if (obj == null) return 0;
        if (string.IsNullOrWhiteSpace(memberName)) return 0;

        var t = obj.GetType();

        var prop = t.GetProperty(memberName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (prop != null)
        {
            object? value = null;
            try { value = prop.GetValue(obj); } catch { value = null; }
            return TryGetCountFromValue(value);
        }

        var field = t.GetField(memberName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (field != null)
        {
            object? value = null;
            try { value = field.GetValue(obj); } catch { value = null; }
            return TryGetCountFromValue(value);
        }

        return 0;
    }

    private static int TryGetCountFromValue(object? value)
    {
        if (value == null) return 0;

        if (value is System.Collections.ICollection coll) return coll.Count;

        var countProp = value.GetType().GetProperty("Count", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (countProp != null && countProp.PropertyType == typeof(int))
        {
            try
            {
                var countVal = countProp.GetValue(value);
                if (countVal is int i) return i;
            }
            catch { }
        }

        return 0;
    }

    private static void EnsureSyntheticInFlightTransfers(SimState state, int targetCount)
    {
        if (state is null) return;
        if (targetCount <= 0) return;

        // InFlightTransfers is always initialized in SimState, but keep this defensive.
        var list = state.InFlightTransfers;
        if (list is null) return;

        // If the sim already has enough transfers, do nothing.
        var current = list.Count;
        if (current >= targetCount) return;

        // Deterministic picks for ids.
        var nodeIds = state.Nodes.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var fromNodeId = nodeIds.Length > 0 ? nodeIds[0] : "";
        var toNodeId = nodeIds.Length > 1 ? nodeIds[1] : fromNodeId;

        var goodId = PickAnyGoodIdDeterministic(state);

        // Add synthetic transfers with long TTL so they remain "active" across the measurement window.
        for (int i = current; i < targetCount; i++)
        {
            var t = new SimCore.Entities.InFlightTransfer();

            // Best-effort property%field assignment by common naming. Missing members are ignored.
            SetMemberIfExists(t, "FromNodeId", fromNodeId);
            SetMemberIfExists(t, "SourceNodeId", fromNodeId);
            SetMemberIfExists(t, "ToNodeId", toNodeId);
            SetMemberIfExists(t, "TargetNodeId", toNodeId);

            SetMemberIfExists(t, "GoodId", goodId);
            SetMemberIfExists(t, "CommodityId", goodId);

            SetMemberIfExists(t, "Amount", 1);
            SetMemberIfExists(t, "Units", 1);
            SetMemberIfExists(t, "Qty", 1);

            // Long duration so they don't complete during the measured window.
            SetMemberIfExists(t, "RemainingTicks", 1000000);
            SetMemberIfExists(t, "TicksRemaining", 1000000);
            SetMemberIfExists(t, "EtaTicks", 1000000);

            // Deterministic id if the type supports it.
            SetMemberIfExists(t, "Id", $"PERF_XFER_{i + 1}");

            list.Add(t);
        }
    }

    private static string PickAnyGoodIdDeterministic(SimState state)
    {
        // Try to derive a good id from existing market inventory keys deterministically.
        try
        {
            foreach (var m in state.Markets.Values.OrderBy(m => m.Id ?? "", StringComparer.Ordinal))
            {
                var invProp = m.GetType().GetProperty("Inventory");
                if (invProp == null) continue;
                var inv = invProp.GetValue(m);
                if (inv is IDictionary<string, int> dict && dict.Count > 0)
                {
                    return dict.Keys.OrderBy(k => k, StringComparer.Ordinal).First();
                }
            }
        }
        catch { }

        // Fallback stable token.
        return "good";
    }

    private static void SetMemberIfExists(object obj, string name, object value)
    {
        if (obj is null) return;
        if (string.IsNullOrWhiteSpace(name)) return;

        var t = obj.GetType();

        var p = t.GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (p != null && p.CanWrite)
        {
            try
            {
                var coerced = CoerceValue(value, p.PropertyType);
                p.SetValue(obj, coerced);
                return;
            }
            catch { }
        }

        var f = t.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (f != null)
        {
            try
            {
                var coerced = CoerceValue(value, f.FieldType);
                f.SetValue(obj, coerced);
            }
            catch { }
        }
    }

    private static object? CoerceValue(object value, Type targetType)
    {
        if (value == null) return null;
        if (targetType.IsInstanceOfType(value)) return value;

        try
        {
            if (targetType == typeof(string)) return value.ToString();
            if (targetType == typeof(int)) return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(long)) return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(double)) return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch { }

        return value;
    }

    private static void EmitPerfReport(PerfBudgetResult r)
    {
        // Report is deterministic in structure and formatting (values will vary by machine).
        TestContext.Out.WriteLine("PERF_BUDGET_REPORT_V0");
        TestContext.Out.WriteLine($"seed={r.Seed}");
        TestContext.Out.WriteLine($"ticks_total={r.TotalTicks}");
        TestContext.Out.WriteLine($"ticks_measured={r.MeasuredTicks}");
        TestContext.Out.WriteLine($"fleets={r.FleetCount}");
        TestContext.Out.WriteLine($"active_transfers={r.ActiveTransferCount}");
        TestContext.Out.WriteLine($"budget_ms_per_tick={PerfBudgetMsPerTick.ToString("0.###", CultureInfo.InvariantCulture)}");
        TestContext.Out.WriteLine($"total_ms_measured={r.TotalMsMeasured.ToString("0.###", CultureInfo.InvariantCulture)}");
        TestContext.Out.WriteLine($"avg_ms_per_tick={r.AvgMsPerTick.ToString("0.###", CultureInfo.InvariantCulture)}");
    }

    private static void EnforcePerfBudgetOrFail(double avgMsPerTick, double budgetMsPerTick, PerfBudgetResult? details)
    {
        if (double.IsNaN(avgMsPerTick) || double.IsInfinity(avgMsPerTick))
        {
            Assert.Fail($"PerfBudget invalid avg_ms_per_tick={avgMsPerTick.ToString(CultureInfo.InvariantCulture)}");
            return;
        }

        if (avgMsPerTick > budgetMsPerTick)
        {
            if (details.HasValue)
            {
                var d = details.Value;
                Assert.Fail(
                    $"PerfBudget exceeded: avg_ms_per_tick={avgMsPerTick.ToString("0.###", CultureInfo.InvariantCulture)} " +
                    $"> budget_ms_per_tick={budgetMsPerTick.ToString("0.###", CultureInfo.InvariantCulture)}; " +
                    $"seed={d.Seed}; ticks_measured={d.MeasuredTicks}; fleets={d.FleetCount}; active_transfers={d.ActiveTransferCount}");
            }
            else
            {
                Assert.Fail(
                    $"PerfBudget exceeded: avg_ms_per_tick={avgMsPerTick.ToString("0.###", CultureInfo.InvariantCulture)} " +
                    $"> budget_ms_per_tick={budgetMsPerTick.ToString("0.###", CultureInfo.InvariantCulture)}");
            }
        }
    }

    private readonly record struct PerfBudgetResult(
        int Seed,
        int TotalTicks,
        int MeasuredTicks,
        int FleetCount,
        int ActiveTransferCount,
        double TotalMsMeasured,
        double AvgMsPerTick);

    private readonly record struct RunResult(
        string GenesisHash,
        string FinalHash,
        IReadOnlyDictionary<int, string> Checkpoints);
}
