using System;
using System.Collections.Generic;
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
    private const string ExpectedGenesisHash = "B7AD78A118F06FC53C7133D2D9FD251C6F082932F01A7C76A8E7F4B095C53853";
    private const string ExpectedFinalHash = "B88935FD5DFACD4C9F49A90D055D1528A0147300881CA627D3A22F9C2719DBF1";


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

    private readonly record struct RunResult(
        string GenesisHash,
        string FinalHash,
        IReadOnlyDictionary<int, string> Checkpoints);
}
