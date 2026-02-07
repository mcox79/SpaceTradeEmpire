using NUnit.Framework;
using SimCore;
using SimCore.Gen;

namespace SimCore.Tests.Determinism;

public class LongRunWorldHashTests
{
    [Test]
    public void LongRun_10000Ticks_Has_Stable_FinalHash()
    {
        const int seed = 42;
        const int ticks = 10000;

        // RUN A
        var simA = new SimKernel(seed);
        GalaxyGenerator.Generate(simA.State, 20, 100f);

        for (int i = 0; i < ticks; i++)
        {
            simA.Step();
        }

        var hashA = simA.State.GetSignature();

        // RUN B (fresh kernel, same seed, same generation, no inputs)
        var simB = new SimKernel(seed);
        GalaxyGenerator.Generate(simB.State, 20, 100f);

        for (int i = 0; i < ticks; i++)
        {
            simB.Step();
        }

        var hashB = simB.State.GetSignature();

        TestContext.Out.WriteLine($"LongRun Final Hash A: {hashA}");
        TestContext.Out.WriteLine($"LongRun Final Hash B: {hashB}");

        Assert.That(hashB, Is.EqualTo(hashA), "Long-run determinism drift detected.");
    }
}
