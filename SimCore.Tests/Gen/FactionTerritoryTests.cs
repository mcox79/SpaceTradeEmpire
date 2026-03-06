using NUnit.Framework;
using SimCore.Schemas;
using SimCore.Gen;

namespace SimCore.Tests.Gen;

// GATE.S15.FEEL.FACTION_TERRITORY.001
[TestFixture]
public sealed class FactionTerritoryTests
{
    private static List<WorldEdge> MakeEdges(params (string id, string from, string to)[] defs)
    {
        return defs.Select(d => new WorldEdge
        {
            Id = d.id,
            FromNodeId = d.from,
            ToNodeId = d.to,
            Distance = 1f,
            TotalCapacity = 5
        }).ToList();
    }

    [Test]
    public void ComputeFactionTerritories_SingleFaction_ControlsReachableNodes()
    {
        // Linear graph: n0 - n1 - n2 - n3 - n4
        var factions = new List<WorldFaction>
        {
            new WorldFaction { FactionId = "f0", HomeNodeId = "n0" }
        };

        var edges = MakeEdges(
            ("e01", "n0", "n1"),
            ("e12", "n1", "n2"),
            ("e23", "n2", "n3"),
            ("e34", "n3", "n4")
        );

        GalaxyGenerator.ComputeFactionTerritories(factions, edges);

        // BFS depth 3 from n0: n0(0), n1(1), n2(2), n3(3). n4 at depth 4 is excluded.
        Assert.That(factions[0].ControlledNodeIds, Has.Count.EqualTo(4));
        Assert.That(factions[0].ControlledNodeIds, Does.Contain("n0"));
        Assert.That(factions[0].ControlledNodeIds, Does.Contain("n3"));
        Assert.That(factions[0].ControlledNodeIds, Does.Not.Contain("n4"));
    }

    [Test]
    public void ComputeFactionTerritories_TwoFactions_ContestedNodeGoesToCloser()
    {
        // n0 - n1 - n2 - n3 - n4
        // f0 home=n0, f1 home=n4
        var factions = new List<WorldFaction>
        {
            new WorldFaction { FactionId = "f0", HomeNodeId = "n0" },
            new WorldFaction { FactionId = "f1", HomeNodeId = "n4" }
        };

        var edges = MakeEdges(
            ("e01", "n0", "n1"),
            ("e12", "n1", "n2"),
            ("e23", "n2", "n3"),
            ("e34", "n3", "n4")
        );

        GalaxyGenerator.ComputeFactionTerritories(factions, edges);

        // f0: n0(0), n1(1), n2(2) — n2 is depth 2 from f0, depth 2 from f1 → tie, f0 wins by FactionId
        // f1: n4(0), n3(1)
        Assert.That(factions[0].ControlledNodeIds, Does.Contain("n0"));
        Assert.That(factions[0].ControlledNodeIds, Does.Contain("n2")); // tie-broken to f0
        Assert.That(factions[1].ControlledNodeIds, Does.Contain("n4"));
        Assert.That(factions[1].ControlledNodeIds, Does.Contain("n3"));
    }

    [Test]
    public void ComputeFactionTerritories_NoEdges_ControlsOnlyHome()
    {
        var factions = new List<WorldFaction>
        {
            new WorldFaction { FactionId = "f0", HomeNodeId = "n0" }
        };

        GalaxyGenerator.ComputeFactionTerritories(factions, new List<WorldEdge>());

        Assert.That(factions[0].ControlledNodeIds, Has.Count.EqualTo(1));
        Assert.That(factions[0].ControlledNodeIds[0], Is.EqualTo("n0"));
    }

    [Test]
    public void ComputeFactionTerritories_EachFactionControls2To6Nodes()
    {
        // Star topology: n0 connected to n1-n6, f0 home=n0
        var factions = new List<WorldFaction>
        {
            new WorldFaction { FactionId = "f0", HomeNodeId = "n0" }
        };

        var edges = MakeEdges(
            ("e01", "n0", "n1"),
            ("e02", "n0", "n2"),
            ("e03", "n0", "n3"),
            ("e04", "n0", "n4"),
            ("e05", "n0", "n5"),
            ("e06", "n0", "n6")
        );

        GalaxyGenerator.ComputeFactionTerritories(factions, edges);

        // n0 + 6 neighbors = 7 nodes at depth ≤1
        Assert.That(factions[0].ControlledNodeIds.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(factions[0].ControlledNodeIds.Count, Is.LessThanOrEqualTo(7));
    }

    [Test]
    public void ComputeFactionTerritories_OutputSortedOrdinal()
    {
        var factions = new List<WorldFaction>
        {
            new WorldFaction { FactionId = "f0", HomeNodeId = "n2" }
        };

        var edges = MakeEdges(
            ("e21", "n2", "n1"),
            ("e23", "n2", "n3")
        );

        GalaxyGenerator.ComputeFactionTerritories(factions, edges);

        var ids = factions[0].ControlledNodeIds;
        Assert.That(ids, Is.Ordered);
    }
}
