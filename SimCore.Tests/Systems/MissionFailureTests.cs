using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Content;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.Systems;

// GATE.S9.MISSION_EVOL.FAILURE.001
[TestFixture]
public sealed class MissionFailureTests
{
    private const string TestMissionId = "test_fail_mission";

    private static MissionDef CreateTimedMission(int deadlineTicks)
    {
        return new MissionDef
        {
            MissionId = TestMissionId,
            Title = "Timed Mission",
            Description = "Complete before deadline",
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef
                {
                    StepIndex = 0,
                    ObjectiveText = "Go somewhere far",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "n_far"
                }
            },
            CreditReward = 200,
            DeadlineTicks = deadlineTicks
        };
    }

    private static SimState CreateState()
    {
        var state = new SimState(42);
        state.PlayerLocationNodeId = "n1";
        state.Nodes["n1"] = new Node { Id = "n1", Kind = NodeKind.Station };
        state.Nodes["n_far"] = new Node { Id = "n_far", Kind = NodeKind.Station };
        return state;
    }

    [Test]
    public void AbandonMission_ClearsActive()
    {
        var def = CreateTimedMission(0);
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState();
            MissionSystem.AcceptMission(state, TestMissionId);

            Assert.That(state.Missions.ActiveMissionId, Is.EqualTo(TestMissionId));

            bool result = MissionSystem.AbandonMission(state);

            Assert.That(result, Is.True);
            Assert.That(state.Missions.ActiveMissionId, Is.Empty);
            Assert.That(state.Missions.FailedMissionIds, Does.Contain(TestMissionId));
        }
        finally
        {
            MissionContentV0.UnregisterTestMission(TestMissionId);
        }
    }

    [Test]
    public void AbandonMission_EmitsAbandonedEvent()
    {
        var def = CreateTimedMission(0);
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState();
            MissionSystem.AcceptMission(state, TestMissionId);
            MissionSystem.AbandonMission(state);

            var events = state.Missions.EventLog;
            Assert.That(events.Any(e => e.EventType == "Abandoned"), Is.True);
        }
        finally
        {
            MissionContentV0.UnregisterTestMission(TestMissionId);
        }
    }

    [Test]
    public void AbandonMission_AppliesRepPenalty()
    {
        var def = CreateTimedMission(0);
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState();
            state.FactionReputation["faction_a"] = 50;
            MissionSystem.AcceptMission(state, TestMissionId);
            MissionSystem.AbandonMission(state);

            Assert.That(state.FactionReputation["faction_a"], Is.EqualTo(50 + SimCore.Tweaks.MissionEvolutionTweaksV0.AbandonRepPenalty));
        }
        finally
        {
            MissionContentV0.UnregisterTestMission(TestMissionId);
        }
    }

    [Test]
    public void Process_DeadlineExceeded_FailsMission()
    {
        var def = CreateTimedMission(5); // 5 tick deadline.
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState();
            MissionSystem.AcceptMission(state, TestMissionId);

            // Advance past deadline.
            for (int i = 0; i < 6; i++)
            {
                state.AdvanceTick();
                MissionSystem.Process(state);
            }

            Assert.That(state.Missions.ActiveMissionId, Is.Empty);
            Assert.That(state.Missions.FailedMissionIds, Does.Contain(TestMissionId));
            Assert.That(state.Missions.EventLog.Any(e => e.EventType == "Failed"), Is.True);
        }
        finally
        {
            MissionContentV0.UnregisterTestMission(TestMissionId);
        }
    }

    [Test]
    public void Process_BeforeDeadline_MissionStillActive()
    {
        var def = CreateTimedMission(100);
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState();
            MissionSystem.AcceptMission(state, TestMissionId);

            for (int i = 0; i < 3; i++)
            {
                state.AdvanceTick();
                MissionSystem.Process(state);
            }

            Assert.That(state.Missions.ActiveMissionId, Is.EqualTo(TestMissionId));
        }
        finally
        {
            MissionContentV0.UnregisterTestMission(TestMissionId);
        }
    }

    [Test]
    public void AbandonMission_NoActiveMission_ReturnsFalse()
    {
        var state = CreateState();
        bool result = MissionSystem.AbandonMission(state);
        Assert.That(result, Is.False);
    }
}
