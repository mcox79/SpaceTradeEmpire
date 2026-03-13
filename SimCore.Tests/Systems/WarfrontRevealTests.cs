using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

// GATE.S7.REVEALS.WARFRONT_REVEAL.001
[TestFixture]
public sealed class WarfrontRevealTests
{
    private static SimState CreateStateWithWarfront(string playerNode, string contestedNode)
    {
        var state = new SimState(42);
        state.PlayerLocationNodeId = playerNode;

        state.Nodes[playerNode] = new Node { Id = playerNode, Kind = NodeKind.Station };
        state.Nodes[contestedNode] = new Node { Id = contestedNode, Kind = NodeKind.Station };

        // Edge connecting them (1 hop).
        state.Edges["e1"] = new Edge { Id = "e1", FromNodeId = playerNode, ToNodeId = contestedNode, Distance = 1f };

        // Warfront involving the contested node.
        state.Warfronts["wf1"] = new WarfrontState
        {
            Id = "wf1",
            CombatantA = "faction_a",
            CombatantB = "faction_b",
            Intensity = WarfrontIntensity.Skirmish,
            WarType = WarType.Hot,
            ContestedNodeIds = new List<string> { contestedNode }
        };

        return state;
    }

    [Test]
    public void GetWarfrontIntelLevel_NoRange_ReturnsTier0()
    {
        // Player at n1, contested node n3 is 2 hops away, scan range 0.
        var state = new SimState(42);
        state.PlayerLocationNodeId = "n1";
        state.Nodes["n1"] = new Node { Id = "n1", Kind = NodeKind.Station };
        state.Nodes["n2"] = new Node { Id = "n2", Kind = NodeKind.Station };
        state.Nodes["n3"] = new Node { Id = "n3", Kind = NodeKind.Station };
        state.Edges["e1"] = new Edge { Id = "e1", FromNodeId = "n1", ToNodeId = "n2", Distance = 1f };
        state.Edges["e2"] = new Edge { Id = "e2", FromNodeId = "n2", ToNodeId = "n3", Distance = 1f };

        int level = IntelSystem.GetWarfrontIntelLevel(state, "n3");
        Assert.That(level, Is.EqualTo(0), "Out-of-range unvisited node should be tier 0.");
    }

    [Test]
    public void GetWarfrontIntelLevel_InScanRange_ReturnsTier1()
    {
        var state = CreateStateWithWarfront("n1", "n2");
        // Unlock sensor_suite for scan range 1.
        state.Tech.UnlockedTechIds.Add("sensor_suite");

        int level = IntelSystem.GetWarfrontIntelLevel(state, "n2");
        Assert.That(level, Is.EqualTo(IntelTweaksV0.WarfrontIntelTier1));
    }

    [Test]
    public void GetWarfrontIntelLevel_VisitedNode_ReturnsTier2()
    {
        var state = CreateStateWithWarfront("n1", "n2");
        state.PlayerVisitedNodeIds.Add("n2");

        int level = IntelSystem.GetWarfrontIntelLevel(state, "n2");
        Assert.That(level, Is.EqualTo(IntelTweaksV0.WarfrontIntelTier2));
    }

    [Test]
    public void GetWarfrontIntelLevel_SustainedObservation_ReturnsTier3()
    {
        var state = CreateStateWithWarfront("n1", "n1");
        state.PlayerVisitedNodeIds.Add("n1");
        // Simulate sustained observation.
        state.Intel.NodeObservationTicks["n1"] = IntelTweaksV0.WarfrontIntelTier3ObservationTicks;

        int level = IntelSystem.GetWarfrontIntelLevel(state, "n1");
        Assert.That(level, Is.EqualTo(IntelTweaksV0.WarfrontIntelTier3));
    }

    [Test]
    public void GetWarfrontIntelLevel_AtNodeButNotEnoughTicks_ReturnsTier2()
    {
        var state = CreateStateWithWarfront("n1", "n1");
        state.PlayerVisitedNodeIds.Add("n1");
        state.Intel.NodeObservationTicks["n1"] = IntelTweaksV0.WarfrontIntelTier3ObservationTicks - 1;

        int level = IntelSystem.GetWarfrontIntelLevel(state, "n1");
        Assert.That(level, Is.EqualTo(IntelTweaksV0.WarfrontIntelTier2), "Below observation threshold should be tier 2.");
    }

    [Test]
    public void UpdateNodeObservation_IncrementsCurrentNode()
    {
        var state = new SimState(42);
        state.PlayerLocationNodeId = "n1";

        IntelSystem.UpdateNodeObservation(state);
        Assert.That(state.Intel.NodeObservationTicks["n1"], Is.EqualTo(1));

        IntelSystem.UpdateNodeObservation(state);
        Assert.That(state.Intel.NodeObservationTicks["n1"], Is.EqualTo(2));
    }

    [Test]
    public void UpdateNodeObservation_ResetsOtherNodes()
    {
        var state = new SimState(42);
        state.PlayerLocationNodeId = "n1";
        state.Intel.NodeObservationTicks["n2"] = 10;

        IntelSystem.UpdateNodeObservation(state);

        Assert.That(state.Intel.NodeObservationTicks.ContainsKey("n2"), Is.False, "Old node observation should be reset.");
        Assert.That(state.Intel.NodeObservationTicks["n1"], Is.EqualTo(1));
    }

    [Test]
    public void GetWarfrontIntelLevel_NullState_ReturnsTier0()
    {
        Assert.That(IntelSystem.GetWarfrontIntelLevel(null, "n1"), Is.EqualTo(0));
    }
}
