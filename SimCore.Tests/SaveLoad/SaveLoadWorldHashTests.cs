using System.Text.Json;
using NUnit.Framework;
using SimCore;
using SimCore.Gen;

namespace SimCore.Tests.SaveLoad;

public class SaveLoadWorldHashTests
{
    [Test]
    public void SaveLoad_RoundTrip_Preserves_WorldHash_And_Seed()
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
        var savedSeed = ReadEnvelopeSeed(json);

        // Prove load restores identity independent of constructor seed by using a different value.
        var sim2 = new SimKernel(seed: 999);
        sim2.LoadFromString(json);

        var afterHash = sim2.State.GetSignature();

        var json2 = sim2.SaveToString();
        var resavedSeed = ReadEnvelopeSeed(json2);

        TestContext.Out.WriteLine($"SaveLoad Seed (expected): {seed}");
        TestContext.Out.WriteLine($"SaveLoad Seed (saved): {savedSeed}");
        TestContext.Out.WriteLine($"SaveLoad Seed (resaved): {resavedSeed}");
        TestContext.Out.WriteLine($"SaveLoad Before Hash: {beforeHash}");
        TestContext.Out.WriteLine($"SaveLoad After  Hash: {afterHash}");

        Assert.That(afterHash, Is.EqualTo(beforeHash), $"Save/load roundtrip changed world hash (Seed={seed}).");
        Assert.That(savedSeed, Is.EqualTo(seed), $"Save payload did not include expected seed (Seed={seed}).");
        Assert.That(resavedSeed, Is.EqualTo(seed), $"Loaded world did not preserve seed identity on re-save (Seed={seed}).");
    }

    private static int ReadEnvelopeSeed(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                Assert.Fail("Save JSON root is not an object. Seed identity envelope is required.");
                return 0;
            }

            if (!root.TryGetProperty("Seed", out var seedEl))
            {
                Assert.Fail("Save JSON missing Seed property. Seed identity envelope is required.");
                return 0;
            }

            if (!root.TryGetProperty("State", out _))
            {
                Assert.Fail("Save JSON missing State property. Seed identity envelope is required.");
                return 0;
            }

            if (seedEl.ValueKind == JsonValueKind.Number && seedEl.TryGetInt32(out var seed))
            {
                return seed;
            }

            Assert.Fail("Save JSON Seed property is not an int32 number.");
            return 0;
        }
        catch (JsonException je)
        {
            Assert.Fail($"Save JSON was not valid JSON: {je.Message}");
            return 0;
        }
    }
}
