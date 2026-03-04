using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

// GATE.S1.MISSION.MODEL.001: Contract tests for mission schema and state model.
[TestFixture]
[Category("MissionContract")]
public sealed class MissionContractTests
{
    [Test]
    public void MissionDef_CanBeCreated_WithAllFields()
    {
        var def = new MissionDef
        {
            MissionId = "test_mission",
            Title = "Test",
            Description = "A test mission",
            Prerequisites = new List<string> { "prereq_1" },
            CreditReward = 100,
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef
                {
                    StepIndex = 0,
                    ObjectiveText = "Go somewhere",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "node_a",
                },
            },
        };

        Assert.That(def.MissionId, Is.EqualTo("test_mission"));
        Assert.That(def.Title, Is.EqualTo("Test"));
        Assert.That(def.CreditReward, Is.EqualTo(100));
        Assert.That(def.Steps, Has.Count.EqualTo(1));
        Assert.That(def.Steps[0].TriggerType, Is.EqualTo(MissionTriggerType.ArriveAtNode));
        Assert.That(def.Prerequisites, Has.Count.EqualTo(1));
    }

    [Test]
    public void MissionTriggerType_EnumValues_Exist()
    {
        Assert.That((int)MissionTriggerType.ArriveAtNode, Is.EqualTo(0));
        Assert.That((int)MissionTriggerType.HaveCargoMin, Is.EqualTo(1));
        Assert.That((int)MissionTriggerType.NoCargoAtNode, Is.EqualTo(2));
    }

    [Test]
    public void MissionState_DefaultsEmpty()
    {
        var ms = new MissionState();
        Assert.That(ms.ActiveMissionId, Is.EqualTo(""));
        Assert.That(ms.CurrentStepIndex, Is.EqualTo(0));
        Assert.That(ms.CompletedMissionIds, Is.Not.Null);
        Assert.That(ms.CompletedMissionIds, Has.Count.EqualTo(0));
        Assert.That(ms.ActiveSteps, Is.Not.Null);
        Assert.That(ms.ActiveSteps, Has.Count.EqualTo(0));
    }

    [Test]
    public void SimState_HasMissionState_NonNull()
    {
        var state = new SimState(seed: 42);
        Assert.That(state.Missions, Is.Not.Null);
        Assert.That(state.Missions.ActiveMissionId, Is.EqualTo(""));
    }

    [Test]
    public void MissionState_SurvivesSerialization()
    {
        var kernel = new SimKernel(seed: 42);
        var state = kernel.State;

        // Set up some mission state.
        state.Missions.ActiveMissionId = "mission_test";
        state.Missions.CurrentStepIndex = 1;
        state.Missions.CompletedMissionIds.Add("mission_prev");
        state.Missions.ActiveSteps.Add(new MissionActiveStep
        {
            StepIndex = 0,
            ObjectiveText = "Step 0",
            TriggerType = MissionTriggerType.ArriveAtNode,
            TargetNodeId = "node_a",
            Completed = true,
        });
        state.Missions.ActiveSteps.Add(new MissionActiveStep
        {
            StepIndex = 1,
            ObjectiveText = "Step 1",
            TriggerType = MissionTriggerType.HaveCargoMin,
            TargetGoodId = "fuel",
            TargetQuantity = 5,
            Completed = false,
        });

        var json = kernel.SaveToString();
        Assert.That(json, Is.Not.Null.And.Not.Empty);

        var kernel2 = new SimKernel(seed: 42);
        kernel2.LoadFromString(json);
        var loaded = kernel2.State;

        Assert.That(loaded.Missions, Is.Not.Null);
        Assert.That(loaded.Missions.ActiveMissionId, Is.EqualTo("mission_test"));
        Assert.That(loaded.Missions.CurrentStepIndex, Is.EqualTo(1));
        Assert.That(loaded.Missions.CompletedMissionIds, Has.Count.EqualTo(1));
        Assert.That(loaded.Missions.CompletedMissionIds[0], Is.EqualTo("mission_prev"));
        Assert.That(loaded.Missions.ActiveSteps, Has.Count.EqualTo(2));
        Assert.That(loaded.Missions.ActiveSteps[0].Completed, Is.True);
        Assert.That(loaded.Missions.ActiveSteps[1].TargetGoodId, Is.EqualTo("fuel"));
        Assert.That(loaded.Missions.ActiveSteps[1].TargetQuantity, Is.EqualTo(5));
    }

    [Test]
    public void MissionActiveStep_FieldsRoundTrip()
    {
        var step = new MissionActiveStep
        {
            StepIndex = 2,
            ObjectiveText = "Do thing",
            TriggerType = MissionTriggerType.NoCargoAtNode,
            TargetNodeId = "n1",
            TargetGoodId = "g1",
            TargetQuantity = 3,
            Completed = true,
        };

        Assert.That(step.StepIndex, Is.EqualTo(2));
        Assert.That(step.TriggerType, Is.EqualTo(MissionTriggerType.NoCargoAtNode));
        Assert.That(step.TargetNodeId, Is.EqualTo("n1"));
        Assert.That(step.Completed, Is.True);
    }

    // GATE.S1.MISSION.SYSTEM.001: Trigger evaluation + step advance tests.

    [Test]
    public void AcceptMission_PopulatesActiveSteps()
    {
        var state = MakeTradeWorldState();
        var accepted = MissionSystem.AcceptMission(state, "mission_matched_luggage");

        Assert.That(accepted, Is.True);
        Assert.That(state.Missions.ActiveMissionId, Is.EqualTo("mission_matched_luggage"));
        Assert.That(state.Missions.CurrentStepIndex, Is.EqualTo(0));
        Assert.That(state.Missions.ActiveSteps, Has.Count.EqualTo(4));

        // Verify resolved targets (not binding tokens).
        foreach (var step in state.Missions.ActiveSteps)
        {
            Assert.That(step.TargetNodeId, Does.Not.StartWith("$"));
            Assert.That(step.TargetGoodId, Does.Not.StartWith("$"));
        }

        // Verify event emitted.
        Assert.That(state.Missions.EventLog, Has.Count.EqualTo(1));
        Assert.That(state.Missions.EventLog[0].EventType, Is.EqualTo("Accepted"));
    }

    [Test]
    public void AcceptMission_CannotAcceptTwice()
    {
        var state = MakeTradeWorldState();
        Assert.That(MissionSystem.AcceptMission(state, "mission_matched_luggage"), Is.True);
        Assert.That(MissionSystem.AcceptMission(state, "mission_matched_luggage"), Is.False);
    }

    [Test]
    public void AcceptMission_CannotAcceptCompleted()
    {
        var state = MakeTradeWorldState();
        state.Missions.CompletedMissionIds.Add("mission_matched_luggage");
        Assert.That(MissionSystem.AcceptMission(state, "mission_matched_luggage"), Is.False);
    }

    [Test]
    public void EvaluateTrigger_ArriveAtNode_True()
    {
        var state = MakeTradeWorldState();
        state.PlayerLocationNodeId = "node_a";

        var step = new MissionActiveStep
        {
            TriggerType = MissionTriggerType.ArriveAtNode,
            TargetNodeId = "node_a",
        };

        Assert.That(MissionSystem.EvaluateTrigger(state, step), Is.True);
    }

    [Test]
    public void EvaluateTrigger_ArriveAtNode_False()
    {
        var state = MakeTradeWorldState();
        state.PlayerLocationNodeId = "node_b";

        var step = new MissionActiveStep
        {
            TriggerType = MissionTriggerType.ArriveAtNode,
            TargetNodeId = "node_a",
        };

        Assert.That(MissionSystem.EvaluateTrigger(state, step), Is.False);
    }

    [Test]
    public void EvaluateTrigger_HaveCargoMin_True()
    {
        var state = MakeTradeWorldState();
        state.PlayerCargo["fuel"] = 5;

        var step = new MissionActiveStep
        {
            TriggerType = MissionTriggerType.HaveCargoMin,
            TargetGoodId = "fuel",
            TargetQuantity = 3,
        };

        Assert.That(MissionSystem.EvaluateTrigger(state, step), Is.True);
    }

    [Test]
    public void EvaluateTrigger_HaveCargoMin_False()
    {
        var state = MakeTradeWorldState();
        // No cargo.

        var step = new MissionActiveStep
        {
            TriggerType = MissionTriggerType.HaveCargoMin,
            TargetGoodId = "fuel",
            TargetQuantity = 3,
        };

        Assert.That(MissionSystem.EvaluateTrigger(state, step), Is.False);
    }

    [Test]
    public void EvaluateTrigger_NoCargoAtNode_True()
    {
        var state = MakeTradeWorldState();
        state.PlayerLocationNodeId = "node_b";
        // No fuel in cargo.

        var step = new MissionActiveStep
        {
            TriggerType = MissionTriggerType.NoCargoAtNode,
            TargetNodeId = "node_b",
            TargetGoodId = "fuel",
        };

        Assert.That(MissionSystem.EvaluateTrigger(state, step), Is.True);
    }

    [Test]
    public void EvaluateTrigger_NoCargoAtNode_False_WrongNode()
    {
        var state = MakeTradeWorldState();
        state.PlayerLocationNodeId = "node_a"; // Wrong node.

        var step = new MissionActiveStep
        {
            TriggerType = MissionTriggerType.NoCargoAtNode,
            TargetNodeId = "node_b",
            TargetGoodId = "fuel",
        };

        Assert.That(MissionSystem.EvaluateTrigger(state, step), Is.False);
    }

    [Test]
    public void Process_AdvancesStep_WhenTriggerMet()
    {
        var state = MakeTradeWorldState();
        MissionSystem.AcceptMission(state, "mission_matched_luggage");

        // Step 0: ArriveAtNode $PLAYER_START (already there).
        MissionSystem.Process(state);

        Assert.That(state.Missions.ActiveSteps[0].Completed, Is.True);
        Assert.That(state.Missions.CurrentStepIndex, Is.EqualTo(1));
    }

    [Test]
    public void Process_CompleteMission_AwardsCredits()
    {
        var state = MakeTradeWorldState();
        var initialCredits = state.PlayerCredits;

        MissionSystem.AcceptMission(state, "mission_matched_luggage");

        // Capture resolved targets.
        var goodId = state.Missions.ActiveSteps[1].TargetGoodId;
        var destNode = state.Missions.ActiveSteps[2].TargetNodeId;

        // Step 0: arrive at start (already there).
        MissionSystem.Process(state);
        Assert.That(state.Missions.CurrentStepIndex, Is.EqualTo(1));

        // Step 1: have cargo.
        state.PlayerCargo[goodId] = 1;
        MissionSystem.Process(state);
        Assert.That(state.Missions.CurrentStepIndex, Is.EqualTo(2));

        // Step 2: arrive at destination.
        state.PlayerLocationNodeId = destNode;
        MissionSystem.Process(state);
        Assert.That(state.Missions.CurrentStepIndex, Is.EqualTo(3));

        // Step 3: sell cargo (no cargo at destination).
        state.PlayerCargo.Remove(goodId);
        MissionSystem.Process(state);

        // Mission complete.
        Assert.That(state.Missions.ActiveMissionId, Is.EqualTo(""));
        Assert.That(state.Missions.CompletedMissionIds, Contains.Item("mission_matched_luggage"));
        Assert.That(state.PlayerCredits, Is.EqualTo(initialCredits + 50));

        // Events: Accepted + 4 StepCompleted + MissionCompleted = 6.
        Assert.That(state.Missions.EventLog, Has.Count.EqualTo(6));
        Assert.That(state.Missions.EventLog.Last().EventType, Is.EqualTo("MissionCompleted"));
    }

    [Test]
    public void GetAvailableMissions_ExcludesCompleted()
    {
        var state = MakeTradeWorldState();
        state.Missions.CompletedMissionIds.Add("mission_matched_luggage");

        var available = MissionSystem.GetAvailableMissions(state);
        Assert.That(available.Any(m => m.MissionId == "mission_matched_luggage"), Is.False);
    }

    [Test]
    public void GetAvailableMissions_ExcludesActive()
    {
        var state = MakeTradeWorldState();
        MissionSystem.AcceptMission(state, "mission_matched_luggage");

        var available = MissionSystem.GetAvailableMissions(state);
        Assert.That(available.Any(m => m.MissionId == "mission_matched_luggage"), Is.False);
    }

    // --- helpers ---

    private static SimState MakeTradeWorldState()
    {
        var state = new SimState(seed: 42);
        state.PlayerCredits = 1000;
        state.PlayerLocationNodeId = "node_a";

        var nodeA = new Node { Id = "node_a", Name = "Alpha Station", Kind = NodeKind.Station, MarketId = "mkt_a" };
        var nodeB = new Node { Id = "node_b", Name = "Beta Station", Kind = NodeKind.Station, MarketId = "mkt_b" };
        state.Nodes["node_a"] = nodeA;
        state.Nodes["node_b"] = nodeB;

        var mktA = new Market { Id = "mkt_a" };
        mktA.Inventory["fuel"] = 50;
        mktA.Inventory["ore"] = 30;
        state.Markets["mkt_a"] = mktA;

        var mktB = new Market { Id = "mkt_b" };
        mktB.Inventory["fuel"] = 10;
        state.Markets["mkt_b"] = mktB;

        var edge = new Edge { Id = "edge_a_b", FromNodeId = "node_a", ToNodeId = "node_b", Distance = 1.0f };
        state.Edges["edge_a_b"] = edge;

        state.Fleets["fleet_trader_1"] = new Fleet
        {
            Id = "fleet_trader_1",
            OwnerId = "player",
            CurrentNodeId = "node_a",
            State = FleetState.Docked,
        };

        return state;
    }
}
