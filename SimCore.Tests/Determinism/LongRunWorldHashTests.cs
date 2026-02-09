using System;
using NUnit.Framework;
using SimCore;
using SimCore.Gen;

namespace SimCore.Tests.Determinism;

public class LongRunWorldHashTests
{
    // This test is a separate golden from GoldenReplayTests:
    // - No external inputs
    // - 10,000 ticks
    private const string ExpectedGenesisHash = "DAB2BB84ADD27BC3C1CE13472CAB3DE7B912D8E6316671B7B7545E409412BBFF";
    private const string ExpectedFinalHash = "46269C0A98116FBFBD0FC2C8EE34AB70204AFAB905F7F00E498783F76E98161E";

    [Test]
    public void LongRun_10000Ticks_Matches_Golden()
    {
        const int seed = 42;
        const int ticks = 10000;

        bool updateGolden = string.Equals(
            Environment.GetEnvironmentVariable("STE_UPDATE_GOLDEN"),
            "1",
            StringComparison.Ordinal);

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

        if (updateGolden)
        {
            TestContext.Out.WriteLine($"PASTE_LONGRUN_GENESIS: {genesisA}");
            TestContext.Out.WriteLine($"PASTE_LONGRUN_FINAL:   {finalA}");
            Assert.Fail("Long-run golden hashes updated. Copy PASTE_LONGRUN_* values into ExpectedGenesisHash/ExpectedFinalHash.");
        }
        else
        {
            Assert.That(genesisA, Is.EqualTo(ExpectedGenesisHash), "Genesis hash does not match long-run golden.");
            Assert.That(finalA, Is.EqualTo(ExpectedFinalHash), "Final hash does not match long-run golden.");
        }
    }
}
