using NUnit.Framework;
using SimCore;
using SimCore.Gen;

namespace SimCore.Tests.SaveLoad;

public class SaveLoadWorldHashTests
{
    [Test]
    public void SaveLoad_RoundTrip_Preserves_WorldHash()
    {
        const int seed = 123;
        const int ticks = 500;

        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        for (int i = 0; i < ticks; i++)
        {
            sim.Step();
        }

        var beforeHash = sim.State.GetSignature();
        var json = sim.SaveToString();

        var sim2 = new SimKernel(seed);
        sim2.LoadFromString(json);

        var afterHash = sim2.State.GetSignature();

        TestContext.Out.WriteLine($"SaveLoad Before Hash: {beforeHash}");
        TestContext.Out.WriteLine($"SaveLoad After  Hash: {afterHash}");

        Assert.That(afterHash, Is.EqualTo(beforeHash), "Save/load roundtrip changed world hash.");
    }
}
