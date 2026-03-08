using NUnit.Framework;
using SimCore.Tweaks;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.Systems;

// GATE.S7.FACTION.PENTAGON_RING.001: Pentagon dependency ring contract tests.
[TestFixture]
public class FactionDependencyTests
{
    [Test]
    public void PentagonRing_HasExactly5Entries()
    {
        Assert.That(FactionTweaksV0.PentagonRing.Length, Is.EqualTo(5));
    }

    [Test]
    public void PentagonRing_FormsACycle()
    {
        // Each entry's supplier should be the next entry's consumer (wrapping).
        var ring = FactionTweaksV0.PentagonRing;
        for (int i = 0; i < ring.Length; i++)
        {
            var next = (i + 1) % ring.Length;
            Assert.That(ring[i].Supplier, Is.EqualTo(ring[next].Consumer),
                $"Ring break at index {i}: {ring[i].Supplier} should match {ring[next].Consumer}");
        }
    }

    [Test]
    public void PentagonRing_CoversAllFiveFactions()
    {
        var consumers = FactionTweaksV0.PentagonRing.Select(r => r.Consumer).ToHashSet();
        var suppliers = FactionTweaksV0.PentagonRing.Select(r => r.Supplier).ToHashSet();

        foreach (var fid in FactionTweaksV0.AllFactionIds)
        {
            Assert.That(consumers, Does.Contain(fid), $"Faction {fid} is not a consumer in the ring");
            Assert.That(suppliers, Does.Contain(fid), $"Faction {fid} is not a supplier in the ring");
        }
    }

    [Test]
    public void PentagonRing_GoodsAreValid()
    {
        var validGoods = new HashSet<string>
        {
            SimCore.Content.WellKnownGoodIds.Fuel, SimCore.Content.WellKnownGoodIds.Ore,
            SimCore.Content.WellKnownGoodIds.Food, SimCore.Content.WellKnownGoodIds.Metal,
            SimCore.Content.WellKnownGoodIds.Electronics, SimCore.Content.WellKnownGoodIds.Composites,
            SimCore.Content.WellKnownGoodIds.Munitions, SimCore.Content.WellKnownGoodIds.RareMetals,
            SimCore.Content.WellKnownGoodIds.ExoticCrystals, SimCore.Content.WellKnownGoodIds.ExoticMatter,
            SimCore.Content.WellKnownGoodIds.Organics, SimCore.Content.WellKnownGoodIds.Components,
            SimCore.Content.WellKnownGoodIds.SalvagedTech,
        };

        foreach (var entry in FactionTweaksV0.PentagonRing)
        {
            Assert.That(validGoods, Does.Contain(entry.Good),
                $"Ring entry {entry.Consumer}->{entry.Supplier} has invalid good '{entry.Good}'");
        }
    }

    [Test]
    public void PentagonRing_NoDuplicateConsumers()
    {
        var consumers = FactionTweaksV0.PentagonRing.Select(r => r.Consumer).ToList();
        Assert.That(consumers.Distinct().Count(), Is.EqualTo(consumers.Count),
            "Pentagon ring has duplicate consumers");
    }

    [Test]
    public void SecondaryCrossLinks_AreValid()
    {
        var allFactions = FactionTweaksV0.AllFactionIds.ToHashSet();
        foreach (var link in FactionTweaksV0.SecondaryCrossLinks)
        {
            Assert.That(allFactions, Does.Contain(link.Consumer), $"Invalid consumer: {link.Consumer}");
            Assert.That(allFactions, Does.Contain(link.Supplier), $"Invalid supplier: {link.Supplier}");
            Assert.That(link.Consumer, Is.Not.EqualTo(link.Supplier), "Self-link detected");
        }
    }

    [Test]
    public void AllFactionIds_AreSortedOrdinal()
    {
        var sorted = FactionTweaksV0.AllFactionIds.OrderBy(x => x, System.StringComparer.Ordinal).ToArray();
        Assert.That(FactionTweaksV0.AllFactionIds, Is.EqualTo(sorted));
    }

    [Test]
    public void SeedFactionsFromNodesSorted_Produces5Factions()
    {
        var sim = new SimKernel(42);
        SimCore.Gen.GalaxyGenerator.Generate(sim.State, 20, 100f);

        var nodeIds = sim.State.Nodes.Keys.OrderBy(x => x, System.StringComparer.Ordinal).ToList();
        var factions = SimCore.Gen.GalaxyGenerator.SeedFactionsFromNodesSorted(nodeIds);

        Assert.That(factions.Count, Is.EqualTo(5));
        foreach (var f in factions)
        {
            Assert.That(f.Species, Is.Not.Empty, $"Faction {f.FactionId} missing species");
            Assert.That(f.Philosophy, Is.Not.Empty, $"Faction {f.FactionId} missing philosophy");
            Assert.That(f.ProducesGoods.Count, Is.GreaterThan(0), $"Faction {f.FactionId} has no produces");
            Assert.That(f.NeedsGoods.Count, Is.GreaterThan(0), $"Faction {f.FactionId} has no needs");
        }
    }

    [Test]
    public void SeedFactionsFromNodesSorted_RelationsIncludeWarfronts()
    {
        var sim = new SimKernel(42);
        SimCore.Gen.GalaxyGenerator.Generate(sim.State, 20, 100f);

        var nodeIds = sim.State.Nodes.Keys.OrderBy(x => x, System.StringComparer.Ordinal).ToList();
        var factions = SimCore.Gen.GalaxyGenerator.SeedFactionsFromNodesSorted(nodeIds);

        var byId = factions.ToDictionary(f => f.FactionId);

        // Hot war: Valorin-Weavers = -1
        Assert.That(byId[FactionTweaksV0.ValorinId].Relations[FactionTweaksV0.WeaversId], Is.EqualTo(-1));
        Assert.That(byId[FactionTweaksV0.WeaversId].Relations[FactionTweaksV0.ValorinId], Is.EqualTo(-1));

        // Cold war: Concord-Chitin = -1
        Assert.That(byId[FactionTweaksV0.ConcordId].Relations[FactionTweaksV0.ChitinId], Is.EqualTo(-1));
        Assert.That(byId[FactionTweaksV0.ChitinId].Relations[FactionTweaksV0.ConcordId], Is.EqualTo(-1));
    }
}
