using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

// GATE.S1.MISSION.CONTENT.001: Mission 1 "Matched Luggage" content validation.
[TestFixture]
[Category("MissionContent")]
public sealed class MissionContentTests
{
    [Test]
    public void MatchedLuggage_Exists_InRegistry()
    {
        var def = MissionSystem.GetMissionDef("mission_matched_luggage");
        Assert.That(def, Is.Not.Null);
        Assert.That(def!.MissionId, Is.EqualTo("mission_matched_luggage"));
        Assert.That(def.Title, Is.EqualTo("Matched Luggage"));
    }

    [Test]
    public void MatchedLuggage_Has4Steps_WellFormed()
    {
        var def = MissionSystem.GetMissionDef("mission_matched_luggage")!;
        Assert.That(def.Steps, Has.Count.EqualTo(4));

        Assert.That(def.Steps[0].TriggerType, Is.EqualTo(MissionTriggerType.ArriveAtNode));
        Assert.That(def.Steps[1].TriggerType, Is.EqualTo(MissionTriggerType.HaveCargoMin));
        Assert.That(def.Steps[2].TriggerType, Is.EqualTo(MissionTriggerType.ArriveAtNode));
        Assert.That(def.Steps[3].TriggerType, Is.EqualTo(MissionTriggerType.NoCargoAtNode));

        // Step indices sequential.
        for (int i = 0; i < def.Steps.Count; i++)
        {
            Assert.That(def.Steps[i].StepIndex, Is.EqualTo(i));
        }

        // Non-empty objective text.
        foreach (var step in def.Steps)
        {
            Assert.That(step.ObjectiveText, Is.Not.Null.And.Not.Empty);
        }
    }

    [Test]
    public void MatchedLuggage_Reward50Credits()
    {
        var def = MissionSystem.GetMissionDef("mission_matched_luggage")!;
        Assert.That(def.CreditReward, Is.EqualTo(50));
    }

    [Test]
    public void MatchedLuggage_NoPrerequisites()
    {
        var def = MissionSystem.GetMissionDef("mission_matched_luggage")!;
        Assert.That(def.Prerequisites, Is.Empty);
    }

    [Test]
    public void MatchedLuggage_ResolvesAgainstMicroWorld()
    {
        var state = MakeMicroWorld();

        var accepted = MissionSystem.AcceptMission(state, "mission_matched_luggage");
        Assert.That(accepted, Is.True);

        // Step 0 target should be player start node.
        Assert.That(state.Missions.ActiveSteps[0].TargetNodeId, Is.EqualTo("node_alpha"));

        // Step 1 target good should be a real good from the market.
        var good = state.Missions.ActiveSteps[1].TargetGoodId;
        Assert.That(good, Is.Not.Empty);
        Assert.That(state.Markets["mkt_alpha"].Inventory.ContainsKey(good), Is.True);

        // Step 2 target should be an adjacent node.
        var dest = state.Missions.ActiveSteps[2].TargetNodeId;
        Assert.That(dest, Is.Not.Empty);
        Assert.That(dest, Is.Not.EqualTo("node_alpha"));
        Assert.That(state.Nodes.ContainsKey(dest), Is.True);

        // Step 3 target node same as step 2, target good same as step 1.
        Assert.That(state.Missions.ActiveSteps[3].TargetNodeId, Is.EqualTo(dest));
        Assert.That(state.Missions.ActiveSteps[3].TargetGoodId, Is.EqualTo(good));
    }

    [Test]
    public void MatchedLuggage_FullCompletion_AgainstMicroWorld()
    {
        var state = MakeMicroWorld();
        var initialCredits = state.PlayerCredits;

        MissionSystem.AcceptMission(state, "mission_matched_luggage");
        var goodId = state.Missions.ActiveSteps[1].TargetGoodId;
        var destNode = state.Missions.ActiveSteps[2].TargetNodeId;

        // Step 0: already at start.
        MissionSystem.Process(state);

        // Step 1: buy.
        state.PlayerCargo[goodId] = 1;
        MissionSystem.Process(state);

        // Step 2: travel.
        state.PlayerLocationNodeId = destNode;
        MissionSystem.Process(state);

        // Step 3: sell.
        state.PlayerCargo.Remove(goodId);
        MissionSystem.Process(state);

        Assert.That(state.Missions.ActiveMissionId, Is.EqualTo(""));
        Assert.That(state.Missions.CompletedMissionIds, Contains.Item("mission_matched_luggage"));
        Assert.That(state.PlayerCredits, Is.EqualTo(initialCredits + 50));
    }

    [Test]
    public void GetAllMissionDefs_NotEmpty()
    {
        var all = MissionSystem.GetAllMissionDefs();
        Assert.That(all, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(all.Any(m => m.MissionId == "mission_matched_luggage"), Is.True);
    }

    private static SimState MakeMicroWorld()
    {
        var state = new SimState(seed: 1);
        state.PlayerCredits = 500;
        state.PlayerLocationNodeId = "node_alpha";

        state.Nodes["node_alpha"] = new Node
        {
            Id = "node_alpha", Name = "Alpha", Kind = NodeKind.Station, MarketId = "mkt_alpha"
        };
        state.Nodes["node_beta"] = new Node
        {
            Id = "node_beta", Name = "Beta", Kind = NodeKind.Station, MarketId = "mkt_beta"
        };

        var mktA = new Market { Id = "mkt_alpha" };
        mktA.Inventory["fuel"] = 100;
        mktA.Inventory["ore"] = 50;
        state.Markets["mkt_alpha"] = mktA;

        var mktB = new Market { Id = "mkt_beta" };
        mktB.Inventory["fuel"] = 20;
        state.Markets["mkt_beta"] = mktB;

        state.Edges["edge_a_b"] = new Edge
        {
            Id = "edge_a_b", FromNodeId = "node_alpha", ToNodeId = "node_beta", Distance = 1.0f
        };

        state.Fleets["fleet_trader_1"] = new Fleet
        {
            Id = "fleet_trader_1", OwnerId = "player",
            CurrentNodeId = "node_alpha", State = FleetState.Docked,
        };

        return state;
    }
}
