using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using System.Linq;

namespace SimCore.Tests.Invariants;

public class BasicStateInvariantsTests
{
    [Test]
    public void Basic_Invariants_Hold_After_Stepping()
    {
        const int seed = 7;
        const int ticks = 250;

        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        for (int i = 0; i < ticks; i++)
        {
            sim.Step();
        }

        // Player credits should not be negative.
        Assert.That(sim.State.PlayerCredits, Is.GreaterThanOrEqualTo(0), "PlayerCredits went negative.");

        // Player cargo quantities should not be negative.
        foreach (var kv in sim.State.PlayerCargo)
        {
            Assert.That(kv.Value, Is.GreaterThanOrEqualTo(0), $"PlayerCargo {kv.Key} went negative.");
        }

        // Market inventory quantities should not be negative.
        foreach (var m in sim.State.Markets.Values)
        {
            foreach (var kv in m.Inventory)
            {
                Assert.That(kv.Value, Is.GreaterThanOrEqualTo(0), $"Market inventory {kv.Key} went negative.");
            }
        }

        // Pending intents should have strictly increasing Seq values.
        if (sim.State.PendingIntents.Count > 1)
        {
            var seqs = sim.State.PendingIntents.Select(x => x.Seq).ToArray();
            for (int i = 1; i < seqs.Length; i++)
            {
                Assert.That(seqs[i], Is.GreaterThan(seqs[i - 1]), "Pending intent seq not strictly increasing.");
            }
        }
    }
}
