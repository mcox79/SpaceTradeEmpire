using System;
using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Gen;

namespace SimCore.Tests.SaveLoad;

public class FleetCargoSaveLoadContractTests
{
    [Test]
    public void SaveLoad_RoundTrip_Preserves_FleetCargo_Exactly()
    {
        const int seed = 123;

        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        // Deterministic pick: lowest Fleet.Id
        var fleet = sim.State.Fleets.Values
            .OrderBy(f => f.Id, StringComparer.Ordinal)
            .First();

        // Set cargo deterministically. Use stable, known strings.
        fleet.Cargo.Clear();
        fleet.Cargo["food"] = 7;
        fleet.Cargo["ore"] = 3;

        var beforeHash = sim.State.GetSignature();
        var json = sim.SaveToString();

        var sim2 = new SimKernel(seed);
        sim2.LoadFromString(json);

        var afterHash = sim2.State.GetSignature();

        var fleet2 = sim2.State.Fleets.Values
            .OrderBy(f => f.Id, StringComparer.Ordinal)
            .First();

        Assert.That(afterHash, Is.EqualTo(beforeHash), "Save/load roundtrip changed world hash.");

        Assert.That(fleet2.Cargo.Count, Is.EqualTo(2));
        Assert.That(fleet2.Cargo.ContainsKey("food"), Is.True);
        Assert.That(fleet2.Cargo.ContainsKey("ore"), Is.True);
        Assert.That(fleet2.Cargo["food"], Is.EqualTo(7));
        Assert.That(fleet2.Cargo["ore"], Is.EqualTo(3));
    }
}
