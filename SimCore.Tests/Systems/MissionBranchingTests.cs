using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Content;
using System.Collections.Generic;

namespace SimCore.Tests.Systems;

// GATE.S9.MISSION_EVOL.BRANCHING.001
[TestFixture]
public sealed class MissionBranchingTests
{
    private const string TestMissionId = "test_branch_mission";

    private static MissionDef CreateBranchingMission()
    {
        return new MissionDef
        {
            MissionId = TestMissionId,
            Title = "Branching Mission",
            Description = "Choose your path",
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef // Step 0: arrive at start
                {
                    StepIndex = 0,
                    ObjectiveText = "Arrive at start",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "n1"
                },
                new MissionStepDef // Step 1: choice
                {
                    StepIndex = 1,
                    ObjectiveText = "Choose your path",
                    TriggerType = MissionTriggerType.Choice,
                    ChoiceOptions = new List<MissionChoiceOption>
                    {
                        new MissionChoiceOption { Label = "Path A: Trade", TargetStepIndex = 2 },
                        new MissionChoiceOption { Label = "Path B: Explore", TargetStepIndex = 3 },
                    }
                },
                new MissionStepDef // Step 2: trade path
                {
                    StepIndex = 2,
                    ObjectiveText = "Deliver goods",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "n2"
                },
                new MissionStepDef // Step 3: explore path
                {
                    StepIndex = 3,
                    ObjectiveText = "Explore distant node",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "n3"
                },
            },
            CreditReward = 100,
        };
    }

    private static SimState CreateState()
    {
        var state = new SimState(42);
        state.PlayerLocationNodeId = "n1";
        state.Nodes["n1"] = new Node { Id = "n1", Kind = NodeKind.Station };
        state.Nodes["n2"] = new Node { Id = "n2", Kind = NodeKind.Station };
        state.Nodes["n3"] = new Node { Id = "n3", Kind = NodeKind.Station };
        return state;
    }

    [Test]
    public void ChoiceStep_DoesNotAutoAdvance()
    {
        var def = CreateBranchingMission();
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState();
            MissionSystem.AcceptMission(state, TestMissionId);
            MissionSystem.Process(state); // Step 0 completes (player at n1).

            Assert.That(state.Missions.CurrentStepIndex, Is.EqualTo(1), "Should be at choice step.");
            MissionSystem.Process(state); // Choice step should NOT auto-advance.
            Assert.That(state.Missions.CurrentStepIndex, Is.EqualTo(1), "Choice step must not auto-advance.");
        }
        finally
        {
            MissionContentV0.UnregisterTestMission(TestMissionId);
        }
    }

    [Test]
    public void MakeChoice_PathA_JumpsToStep2()
    {
        var def = CreateBranchingMission();
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState();
            MissionSystem.AcceptMission(state, TestMissionId);
            MissionSystem.Process(state); // Complete step 0.

            bool result = MissionSystem.MakeChoice(state, 0); // Choose Path A.
            Assert.That(result, Is.True);
            Assert.That(state.Missions.CurrentStepIndex, Is.EqualTo(2), "Should jump to step 2 (trade path).");
        }
        finally
        {
            MissionContentV0.UnregisterTestMission(TestMissionId);
        }
    }

    [Test]
    public void MakeChoice_PathB_JumpsToStep3()
    {
        var def = CreateBranchingMission();
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState();
            MissionSystem.AcceptMission(state, TestMissionId);
            MissionSystem.Process(state); // Complete step 0.

            bool result = MissionSystem.MakeChoice(state, 1); // Choose Path B.
            Assert.That(result, Is.True);
            Assert.That(state.Missions.CurrentStepIndex, Is.EqualTo(3), "Should jump to step 3 (explore path).");
        }
        finally
        {
            MissionContentV0.UnregisterTestMission(TestMissionId);
        }
    }

    [Test]
    public void MakeChoice_InvalidIndex_ReturnsFalse()
    {
        var def = CreateBranchingMission();
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState();
            MissionSystem.AcceptMission(state, TestMissionId);
            MissionSystem.Process(state);

            bool result = MissionSystem.MakeChoice(state, 5);
            Assert.That(result, Is.False);
        }
        finally
        {
            MissionContentV0.UnregisterTestMission(TestMissionId);
        }
    }

    [Test]
    public void MakeChoice_OnNonChoiceStep_ReturnsFalse()
    {
        var def = CreateBranchingMission();
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState();
            state.PlayerLocationNodeId = "n0"; // Don't complete step 0.
            MissionSystem.AcceptMission(state, TestMissionId);

            bool result = MissionSystem.MakeChoice(state, 0);
            Assert.That(result, Is.False, "Cannot make choice on non-Choice step.");
        }
        finally
        {
            MissionContentV0.UnregisterTestMission(TestMissionId);
        }
    }

    [Test]
    public void BranchPath_CompletesToEnd()
    {
        var def = CreateBranchingMission();
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState();
            MissionSystem.AcceptMission(state, TestMissionId);
            MissionSystem.Process(state); // Step 0 done.

            MissionSystem.MakeChoice(state, 1); // Choose path B → step 3.
            Assert.That(state.Missions.CurrentStepIndex, Is.EqualTo(3));

            // Move player to n3 to complete step 3.
            state.PlayerLocationNodeId = "n3";
            MissionSystem.Process(state);

            Assert.That(state.Missions.CompletedMissionIds, Does.Contain(TestMissionId));
        }
        finally
        {
            MissionContentV0.UnregisterTestMission(TestMissionId);
        }
    }
}
