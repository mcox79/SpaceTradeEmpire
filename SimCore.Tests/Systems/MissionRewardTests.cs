using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Content;
using System.Collections.Generic;

namespace SimCore.Tests.Systems;

// GATE.S9.MISSION_EVOL.REWARDS.001
[TestFixture]
public sealed class MissionRewardTests
{
    private const string TestMissionId = "test_reward_mission";

    private static MissionDef CreateRewardMission(List<MissionRewardDef> rewards)
    {
        return new MissionDef
        {
            MissionId = TestMissionId,
            Title = "Reward Test Mission",
            Description = "Test",
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef
                {
                    StepIndex = 0,
                    ObjectiveText = "Go somewhere",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "n1"
                }
            },
            CreditReward = 100,
            Rewards = rewards
        };
    }

    private static SimState CreateStateForMission()
    {
        var state = new SimState(42);
        state.PlayerLocationNodeId = "n1";
        state.Nodes["n1"] = new Node { Id = "n1", Kind = NodeKind.Station };
        return state;
    }

    [Test]
    public void CompleteMission_ReputationReward_AdjustsReputation()
    {
        var def = CreateRewardMission(new List<MissionRewardDef>
        {
            new MissionRewardDef { ReputationFactionId = "faction_a", ReputationAmount = 25 }
        });

        // Register the mission.
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateStateForMission();
            state.FactionReputation["faction_a"] = 50;

            MissionSystem.AcceptMission(state, TestMissionId);
            MissionSystem.Process(state); // Player at n1, step completes.

            Assert.That(state.FactionReputation["faction_a"], Is.EqualTo(75));
            Assert.That(state.Missions.CompletedMissionIds, Does.Contain(TestMissionId));
        }
        finally
        {
            MissionContentV0.UnregisterTestMission(TestMissionId);
        }
    }

    [Test]
    public void CompleteMission_TechUnlockReward_UnlocksTech()
    {
        var def = CreateRewardMission(new List<MissionRewardDef>
        {
            new MissionRewardDef { TechUnlockId = "advanced_sensors" }
        });

        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateStateForMission();

            MissionSystem.AcceptMission(state, TestMissionId);
            MissionSystem.Process(state);

            Assert.That(state.Tech.UnlockedTechIds, Does.Contain("advanced_sensors"));
        }
        finally
        {
            MissionContentV0.UnregisterTestMission(TestMissionId);
        }
    }

    [Test]
    public void CompleteMission_IntelReward_CreatesRumorLead()
    {
        var def = CreateRewardMission(new List<MissionRewardDef>
        {
            new MissionRewardDef { IntelLeadNodeId = "n2" }
        });

        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateStateForMission();

            MissionSystem.AcceptMission(state, TestMissionId);
            MissionSystem.Process(state);

            var leadId = $"LEAD.MISSION.{TestMissionId}.n2";
            Assert.That(state.Intel.RumorLeads, Contains.Key(leadId));
        }
        finally
        {
            MissionContentV0.UnregisterTestMission(TestMissionId);
        }
    }

    [Test]
    public void CompleteMission_MultipleRewards_AllDistributed()
    {
        var def = CreateRewardMission(new List<MissionRewardDef>
        {
            new MissionRewardDef { ReputationFactionId = "faction_b", ReputationAmount = 10 },
            new MissionRewardDef { TechUnlockId = "warp_drive" }
        });

        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateStateForMission();
            state.FactionReputation["faction_b"] = 0;

            MissionSystem.AcceptMission(state, TestMissionId);
            MissionSystem.Process(state);

            Assert.That(state.FactionReputation["faction_b"], Is.EqualTo(10));
            Assert.That(state.Tech.UnlockedTechIds, Does.Contain("warp_drive"));
            Assert.That(state.PlayerCredits, Is.GreaterThanOrEqualTo(100)); // CreditReward
        }
        finally
        {
            MissionContentV0.UnregisterTestMission(TestMissionId);
        }
    }
}
