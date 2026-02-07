using System;
using System.IO;
using NUnit.Framework;
using SimCore;
using SimCore.Gen;

namespace SimCore.Tests.Determinism;

public class LongRunWorldHashTests
{
    [Test]
    public void LongRun_10000Ticks_Matches_GoldenReplay_Snapshot()
    {
        const int seed = 42;
        const int ticks = 10000;

        // RUN A
        var simA = new SimKernel(seed);
        GalaxyGenerator.Generate(simA.State, 20, 100f);

        var genesisA = simA.State.GetSignature();

        for (int i = 0; i < ticks; i++)
        {
            simA.Step();
        }

        var finalA = simA.State.GetSignature();

        // RUN B (fresh kernel, same seed, same generation, no inputs)
        var simB = new SimKernel(seed);
        GalaxyGenerator.Generate(simB.State, 20, 100f);

        var genesisB = simB.State.GetSignature();

        for (int i = 0; i < ticks; i++)
        {
            simB.Step();
        }

        var finalB = simB.State.GetSignature();

        TestContext.Out.WriteLine($"Genesis Hash A: {genesisA}");
        TestContext.Out.WriteLine($"Genesis Hash B: {genesisB}");
        TestContext.Out.WriteLine($"LongRun Final Hash A: {finalA}");
        TestContext.Out.WriteLine($"LongRun Final Hash B: {finalB}");

        Assert.That(genesisB, Is.EqualTo(genesisA), "Genesis determinism drift detected.");
        Assert.That(finalB, Is.EqualTo(finalA), "Long-run determinism drift detected.");

        var (expectedGenesis, expectedFinal) = ReadGoldenReplaySnapshot();

        Assert.That(genesisA, Is.EqualTo(expectedGenesis), "Genesis hash does not match golden snapshot.");
        Assert.That(finalA, Is.EqualTo(expectedFinal), "Final hash does not match golden snapshot.");
    }

    private static (string expectedGenesis, string expectedFinal) ReadGoldenReplaySnapshot()
    {
        var repoRoot = FindRepoRoot();
        var snapshotPath = Path.Combine(repoRoot, "docs", "generated", "snapshots", "golden_replay_hashes.txt");

        if (!File.Exists(snapshotPath))
        {
            Assert.Fail($"Missing golden replay snapshot file: {snapshotPath}");
        }

        string? genesis = null;
        string? final = null;

        foreach (var rawLine in File.ReadAllLines(snapshotPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;

            if (line.StartsWith("Genesis=", StringComparison.OrdinalIgnoreCase))
            {
                genesis = line.Substring("Genesis=".Length).Trim();
            }
            else if (line.StartsWith("Final=", StringComparison.OrdinalIgnoreCase))
            {
                final = line.Substring("Final=".Length).Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(genesis) || string.IsNullOrWhiteSpace(final))
        {
            Assert.Fail($"Snapshot file malformed. Expected lines 'Genesis=<hash>' and 'Final=<hash>' in: {snapshotPath}");
        }

        return (genesis!, final!);
    }

    private static string FindRepoRoot()
    {
        // NUnit work directory is usually <repo>\SimCore.Tests\bin\Debug\net8.0
        // Walk upward until we find a .git folder.
        var dir = new DirectoryInfo(TestContext.CurrentContext.WorkDirectory);

        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                return dir.FullName;

            dir = dir.Parent;
        }

        Assert.Fail("Unable to locate repo root (no .git found walking upward from test work directory).");
        return "";
    }
}
