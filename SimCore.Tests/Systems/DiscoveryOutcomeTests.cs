using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

// GATE.S6.OUTCOME.REWARD_MODEL.001
[TestFixture]
public sealed class DiscoveryOutcomeTests
{
    private static SimState CreateStateWithAnalyzedDiscovery(string discoveryId, string nodeId)
    {
        var state = new SimState(42);
        var node = new Node { Id = nodeId, Kind = NodeKind.Station, Name = "TestNode" };
        node.SeededDiscoveryIds.Add(discoveryId);
        state.Nodes[nodeId] = node;

        state.Intel.Discoveries[discoveryId] = new DiscoveryStateV0
        {
            DiscoveryId = discoveryId,
            Phase = DiscoveryPhase.Analyzed
        };
        return state;
    }

    [Test]
    public void Process_AnalyzedDiscovery_GeneratesOutcome()
    {
        var state = CreateStateWithAnalyzedDiscovery("disc_v0|RESOURCE_POOL_MARKER|n1|ref1|src1", "n1");
        long creditsBefore = state.PlayerCredits;

        DiscoveryOutcomeSystem.Process(state);

        Assert.That(state.AnomalyEncounters, Has.Count.EqualTo(1));
        Assert.That(state.PlayerCredits, Is.GreaterThan(creditsBefore));
    }

    [Test]
    public void Process_ResourcePoolMarker_GivesRuinFamily()
    {
        var state = CreateStateWithAnalyzedDiscovery("disc_v0|RESOURCE_POOL_MARKER|n1|ref1|src1", "n1");
        DiscoveryOutcomeSystem.Process(state);

        var outcome = state.AnomalyEncounters.Values.First();
        Assert.That(outcome.Family, Is.EqualTo("RUIN"));
        Assert.That(outcome.LootItems, Contains.Key("anomaly_samples"));
    }

    [Test]
    public void Process_CorridorTrace_GivesSignalFamily()
    {
        var state = CreateStateWithAnalyzedDiscovery("disc_v0|CORRIDOR_TRACE|n1|ref1|src1", "n1");

        // Add an adjacent node via an edge so discovery lead can be generated.
        state.Nodes["n2"] = new Node { Id = "n2", Kind = NodeKind.Station };
        state.Edges["e1"] = new Edge { Id = "e1", FromNodeId = "n1", ToNodeId = "n2", Distance = 1f };

        DiscoveryOutcomeSystem.Process(state);

        var outcome = state.AnomalyEncounters.Values.First();
        Assert.That(outcome.Family, Is.EqualTo("SIGNAL"));
        Assert.That(outcome.DiscoveryLeadNodeId, Is.EqualTo("n2"));
    }

    [Test]
    public void Process_NoDuplicateOutcomes()
    {
        var state = CreateStateWithAnalyzedDiscovery("disc_v0|RESOURCE_POOL_MARKER|n1|ref1|src1", "n1");
        DiscoveryOutcomeSystem.Process(state);
        DiscoveryOutcomeSystem.Process(state);

        Assert.That(state.AnomalyEncounters, Has.Count.EqualTo(1));
    }

    [Test]
    public void Process_NonAnalyzedDiscovery_NoOutcome()
    {
        var state = new SimState(42);
        state.Nodes["n1"] = new Node { Id = "n1", Kind = NodeKind.Station };
        state.Nodes["n1"].SeededDiscoveryIds.Add("disc1");

        state.Intel.Discoveries["disc1"] = new DiscoveryStateV0
        {
            DiscoveryId = "disc1",
            Phase = DiscoveryPhase.Seen
        };

        DiscoveryOutcomeSystem.Process(state);
        Assert.That(state.AnomalyEncounters, Is.Empty);
    }

    [Test]
    public void ParseDiscoveryKind_ValidFormat_ReturnsKind()
    {
        Assert.That(DiscoveryOutcomeSystem.ParseDiscoveryKind("disc_v0|RESOURCE_POOL_MARKER|n1|ref1|src1"),
            Is.EqualTo("RESOURCE_POOL_MARKER"));
    }

    [Test]
    public void ParseDiscoveryKind_EmptyString_ReturnsEmpty()
    {
        Assert.That(DiscoveryOutcomeSystem.ParseDiscoveryKind(""), Is.EqualTo(""));
    }
}
