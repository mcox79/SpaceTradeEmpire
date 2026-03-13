using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Gen;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.Gen;

// GATE.T18.NARRATIVE.LOG_PLACEMENT.001
[TestFixture]
public sealed class NarrativePlacementTests
{
    private SimState MakeMinimalGalaxy(int seed)
    {
        var state = new SimState(seed);

        // 60 nodes to accommodate 44 data logs + 6 kepler pieces with room to spare
        state.PlayerLocationNodeId = "star_0";
        string[] factions = { "Concord", "Valorin", "Communion", "Weavers", "Chitin" };
        for (int i = 0; i < 60; i++)
        {
            state.Nodes[$"star_{i}"] = new Node
            {
                Id = $"star_{i}",
                InstabilityLevel = (i * 5) % 100
            };
            state.NodeFactionId[$"star_{i}"] = factions[i % 5];
        }

        // Linear chain for connectivity
        for (int i = 0; i < 59; i++)
        {
            string eid = $"edge_{i}_{i + 1}";
            state.Edges[eid] = new Edge
            {
                Id = eid,
                FromNodeId = $"star_{i}",
                ToNodeId = $"star_{i + 1}",
                Distance = 5f
            };
        }
        // Cross-links for shorter paths
        state.Edges["edge_0_10"] = new Edge
        {
            Id = "edge_0_10", FromNodeId = "star_0", ToNodeId = "star_10", Distance = 15f
        };
        state.Edges["edge_5_20"] = new Edge
        {
            Id = "edge_5_20", FromNodeId = "star_5", ToNodeId = "star_20", Distance = 10f
        };

        // Discovery sites (disc_v0|KIND|NodeId|RefId|SourceId format)
        for (int i = 1; i < 40; i++)
        {
            string kind = (i % 3) switch { 0 => "RUIN", 1 => "SIGNAL", _ => "DERELICT" };
            string discId = $"disc_v0|{kind}|star_{i}|ref_{i}|src_{i}";
            state.Intel.Discoveries[discId] = new DiscoveryStateV0
            {
                DiscoveryId = discId,
                Phase = DiscoveryPhase.Seen,
            };
        }

        // Warfront for warfront node detection
        state.Warfronts["wf_1"] = new WarfrontState
        {
            Id = "wf_1",
            CombatantA = "Valorin",
            CombatantB = "Communion",
            Intensity = WarfrontIntensity.Skirmish
        };

        return state;
    }

    [Test]
    public void PlaceDataLogs_AllLogsHaveLocations()
    {
        var state = MakeMinimalGalaxy(42);
        NarrativePlacementGen.PlaceDataLogs(state);

        foreach (var kv in state.DataLogs)
        {
            Assert.That(kv.Value.LocationNodeId, Is.Not.Empty,
                $"Log {kv.Value.LogId} has no location");
        }
    }

    [Test]
    public void PlaceDataLogs_IsDeterministic()
    {
        var state1 = MakeMinimalGalaxy(42);
        NarrativePlacementGen.PlaceDataLogs(state1);

        var state2 = MakeMinimalGalaxy(42);
        NarrativePlacementGen.PlaceDataLogs(state2);

        Assert.That(state1.DataLogs.Count, Is.EqualTo(state2.DataLogs.Count));
        foreach (var kv in state1.DataLogs)
        {
            Assert.That(state2.DataLogs.ContainsKey(kv.Key), Is.True,
                $"Log {kv.Key} missing in second run");
            Assert.That(state2.DataLogs[kv.Key].LocationNodeId,
                Is.EqualTo(kv.Value.LocationNodeId),
                $"Log {kv.Key} placed differently across runs");
        }
    }

    [Test]
    public void PlaceKeplerChain_PlacesAllPieces()
    {
        var state = MakeMinimalGalaxy(42);
        NarrativePlacementGen.PlaceDataLogs(state);
        NarrativePlacementGen.PlaceKeplerChain(state);

        // Kepler pieces are stored in DataLogs with "KEPLER." prefix
        var keplerLogs = state.DataLogs
            .Where(kv => kv.Key.StartsWith("KEPLER."))
            .ToList();

        Assert.That(keplerLogs.Count, Is.GreaterThan(0));
    }

    [Test]
    public void PlaceKeplerChain_IsDeterministic()
    {
        var state1 = MakeMinimalGalaxy(42);
        NarrativePlacementGen.PlaceDataLogs(state1);
        NarrativePlacementGen.PlaceKeplerChain(state1);

        var state2 = MakeMinimalGalaxy(42);
        NarrativePlacementGen.PlaceDataLogs(state2);
        NarrativePlacementGen.PlaceKeplerChain(state2);

        var kepler1 = state1.DataLogs
            .Where(kv => kv.Key.StartsWith("KEPLER."))
            .OrderBy(kv => kv.Key)
            .ToList();
        var kepler2 = state2.DataLogs
            .Where(kv => kv.Key.StartsWith("KEPLER."))
            .OrderBy(kv => kv.Key)
            .ToList();

        Assert.That(kepler1.Count, Is.EqualTo(kepler2.Count));
        for (int i = 0; i < kepler1.Count; i++)
        {
            Assert.That(kepler1[i].Value.LocationNodeId,
                Is.EqualTo(kepler2[i].Value.LocationNodeId));
        }
    }

    [Test]
    public void PlaceDataLogs_NoLogAtStarterNode()
    {
        var state = MakeMinimalGalaxy(42);
        NarrativePlacementGen.PlaceDataLogs(state);

        // With enough nodes available, logs should avoid the starter node
        // (the BFS distance-range mapping starts at minHops=1)
        foreach (var kv in state.DataLogs)
        {
            Assert.That(kv.Value.LocationNodeId, Is.Not.EqualTo("star_0"),
                $"Log {kv.Key} was placed at starter node star_0");
        }
    }

    [Test]
    public void MultiSeed_AllLogIdsPresent()
    {
        int[] seeds = { 1, 42, 100, 999, 12345 };
        var expectedLogIds = DataLogContentV0.AllLogs.Select(l => l.LogId).ToHashSet();

        foreach (int seed in seeds)
        {
            var state = MakeMinimalGalaxy(seed);
            NarrativePlacementGen.PlaceDataLogs(state);

            foreach (var logId in expectedLogIds)
            {
                Assert.That(state.DataLogs.ContainsKey(logId), Is.True,
                    $"Log {logId} missing in seed {seed}");
            }
        }
    }

    [Test]
    public void PlaceDataLogs_NullPlayerLocation_NoException()
    {
        var state = MakeMinimalGalaxy(42);
        state.PlayerLocationNodeId = "";
        NarrativePlacementGen.PlaceDataLogs(state);
        Assert.That(state.DataLogs, Is.Empty);
    }

    [Test]
    public void PlaceDataLogs_NoWarfronts_StillPlacesAllLogs()
    {
        var state = MakeMinimalGalaxy(42);
        state.Warfronts.Clear();
        NarrativePlacementGen.PlaceDataLogs(state);

        // LOG.ECON.001 landmark falls through to tier-based placement
        var expectedCount = DataLogContentV0.AllLogs.Count;
        Assert.That(state.DataLogs.Count, Is.EqualTo(expectedCount));
    }
}
