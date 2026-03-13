using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Content;
using SimCore.Tweaks;

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

    // ── GATE.S9.MISSION_EVOL.CONTRACT.001: Phase 1 evolution feature coverage ──

    private static MissionDef CreateEvolutionTestMission(string id, MissionTriggerType triggerType)
    {
        var step = new MissionStepDef
        {
            StepIndex = 0, ObjectiveText = "Phase 1 test", TriggerType = triggerType,
            TargetNodeId = "node_a", TargetGoodId = "fuel", TargetQuantity = 5,
        };
        return new MissionDef
        {
            MissionId = id, Title = "Evo " + id, Description = "Test",
            Steps = new List<MissionStepDef> { step }, CreditReward = 100,
        };
    }

    private void WithEvoMission(MissionDef def, System.Action<SimState> test)
    {
        MissionContentV0.RegisterTestMission(def);
        try { test(MakeTradeWorldState()); }
        finally { MissionContentV0.UnregisterTestMission(def.MissionId); }
    }

    [Test]
    public void EvoTrigger_ReputationMin_Fires()
    {
        var def = CreateEvolutionTestMission("evo_rep", MissionTriggerType.ReputationMin);
        def.Steps[0].TargetFactionId = "faction_x";
        def.Steps[0].TargetQuantity = 50;
        WithEvoMission(def, state =>
        {
            state.FactionReputation["faction_x"] = 60;
            MissionSystem.AcceptMission(state, "evo_rep");
            MissionSystem.Process(state);
            Assert.That(state.Missions.CompletedMissionIds, Does.Contain("evo_rep"));
        });
    }

    [Test]
    public void EvoTrigger_CreditsMin_Fires()
    {
        var def = CreateEvolutionTestMission("evo_cred", MissionTriggerType.CreditsMin);
        def.Steps[0].TargetQuantity = 500;
        WithEvoMission(def, state =>
        {
            state.PlayerCredits = 600;
            MissionSystem.AcceptMission(state, "evo_cred");
            MissionSystem.Process(state);
            Assert.That(state.Missions.CompletedMissionIds, Does.Contain("evo_cred"));
        });
    }

    [Test]
    public void EvoTrigger_TechUnlocked_Fires()
    {
        var def = CreateEvolutionTestMission("evo_tech", MissionTriggerType.TechUnlocked);
        def.Steps[0].TargetTechId = "warp_drive";
        WithEvoMission(def, state =>
        {
            state.Tech.UnlockedTechIds.Add("warp_drive");
            MissionSystem.AcceptMission(state, "evo_tech");
            MissionSystem.Process(state);
            Assert.That(state.Missions.CompletedMissionIds, Does.Contain("evo_tech"));
        });
    }

    [Test]
    public void EvoTrigger_TimerExpired_Fires()
    {
        var def = CreateEvolutionTestMission("evo_timer", MissionTriggerType.TimerExpired);
        def.Steps[0].DeadlineTicks = 10;
        WithEvoMission(def, state =>
        {
            MissionSystem.AcceptMission(state, "evo_timer");
            for (int i = 0; i < 11; i++) state.AdvanceTick();
            MissionSystem.Process(state);
            Assert.That(state.Missions.CompletedMissionIds, Does.Contain("evo_timer"));
        });
    }

    [Test]
    public void EvoReward_ReputationAndTech_BothApplied()
    {
        var def = CreateEvolutionTestMission("evo_rew", MissionTriggerType.ArriveAtNode);
        def.Steps[0].TargetNodeId = "node_a";
        def.Rewards = new List<MissionRewardDef>
        {
            new MissionRewardDef { ReputationFactionId = "faction_a", ReputationAmount = 20 },
            new MissionRewardDef { TechUnlockId = "sensors_v2" },
        };
        WithEvoMission(def, state =>
        {
            state.FactionReputation["faction_a"] = 50;
            MissionSystem.AcceptMission(state, "evo_rew");
            MissionSystem.Process(state);
            Assert.That(state.FactionReputation["faction_a"], Is.EqualTo(70));
            Assert.That(state.Tech.UnlockedTechIds, Does.Contain("sensors_v2"));
        });
    }

    [Test]
    public void EvoFailure_AbandonAppliesRepPenalty()
    {
        var def = CreateEvolutionTestMission("evo_abandon", MissionTriggerType.ArriveAtNode);
        def.Steps[0].TargetNodeId = "node_b"; // Not there.
        WithEvoMission(def, state =>
        {
            state.FactionReputation["faction_a"] = 50;
            MissionSystem.AcceptMission(state, "evo_abandon");
            MissionSystem.AbandonMission(state);
            Assert.That(state.Missions.FailedMissionIds, Does.Contain("evo_abandon"));
            Assert.That(state.FactionReputation["faction_a"], Is.EqualTo(50 + MissionEvolutionTweaksV0.AbandonRepPenalty));
        });
    }

    [Test]
    public void EvoFailure_DeadlineExceeded_AutoFails()
    {
        var def = CreateEvolutionTestMission("evo_dl", MissionTriggerType.ArriveAtNode);
        def.Steps[0].TargetNodeId = "node_b";
        def.DeadlineTicks = 5;
        WithEvoMission(def, state =>
        {
            MissionSystem.AcceptMission(state, "evo_dl");
            for (int i = 0; i < 6; i++) state.AdvanceTick();
            MissionSystem.Process(state);
            Assert.That(state.Missions.FailedMissionIds, Does.Contain("evo_dl"));
        });
    }

    [Test]
    public void EvoBranching_ChoiceJumps_ToCorrectPath()
    {
        var def = new MissionDef
        {
            MissionId = "evo_branch", Title = "Branch", Description = "Test",
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef { StepIndex = 0, ObjectiveText = "Start", TriggerType = MissionTriggerType.ArriveAtNode, TargetNodeId = "node_a" },
                new MissionStepDef
                {
                    StepIndex = 1, ObjectiveText = "Choose", TriggerType = MissionTriggerType.Choice,
                    ChoiceOptions = new List<MissionChoiceOption>
                    {
                        new MissionChoiceOption { Label = "A", TargetStepIndex = 2 },
                        new MissionChoiceOption { Label = "B", TargetStepIndex = 3 },
                    }
                },
                new MissionStepDef { StepIndex = 2, ObjectiveText = "Path A", TriggerType = MissionTriggerType.ArriveAtNode, TargetNodeId = "node_b" },
                new MissionStepDef { StepIndex = 3, ObjectiveText = "Path B", TriggerType = MissionTriggerType.ArriveAtNode, TargetNodeId = "node_a" },
            },
            CreditReward = 100,
        };
        WithEvoMission(def, state =>
        {
            MissionSystem.AcceptMission(state, "evo_branch");
            MissionSystem.Process(state); // Step 0.
            MissionSystem.MakeChoice(state, 1); // Path B → step 3.
            Assert.That(state.Missions.CurrentStepIndex, Is.EqualTo(3));
            MissionSystem.Process(state); // At node_a, step 3 completes.
            Assert.That(state.Missions.CompletedMissionIds, Does.Contain("evo_branch"));
        });
    }

    [Test]
    public void EvoFactionContract_GatedByRep()
    {
        var def = CreateEvolutionTestMission("evo_fc", MissionTriggerType.ArriveAtNode);
        def.FactionId = "faction_x";
        def.RequiredRepTier = (int)RepTier.Friendly;
        WithEvoMission(def, state =>
        {
            state.FactionReputation["faction_x"] = 0;
            Assert.That(MissionSystem.GetAvailableMissions(state).Any(m => m.MissionId == "evo_fc"), Is.False);
            state.FactionReputation["faction_x"] = 30;
            Assert.That(MissionSystem.GetAvailableMissions(state).Any(m => m.MissionId == "evo_fc"), Is.True);
        });
    }

    [Test]
    public void EvoReward_IntelLead_CreatesRumorLead()
    {
        var def = CreateEvolutionTestMission("evo_intel", MissionTriggerType.ArriveAtNode);
        def.Steps[0].TargetNodeId = "node_a";
        def.Rewards = new List<MissionRewardDef>
        {
            new MissionRewardDef { IntelLeadNodeId = "node_b" }
        };
        WithEvoMission(def, state =>
        {
            MissionSystem.AcceptMission(state, "evo_intel");
            MissionSystem.Process(state);
            Assert.That(state.Intel.RumorLeads, Contains.Key("LEAD.MISSION.evo_intel.node_b"));
        });
    }

    [Test]
    public void EvoChoice_InvalidIndex_ReturnsFalse()
    {
        var def = new MissionDef
        {
            MissionId = "evo_bad_choice", Title = "Bad", Description = "Test",
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef { StepIndex = 0, ObjectiveText = "Start", TriggerType = MissionTriggerType.ArriveAtNode, TargetNodeId = "node_a" },
                new MissionStepDef
                {
                    StepIndex = 1, ObjectiveText = "Choose", TriggerType = MissionTriggerType.Choice,
                    ChoiceOptions = new List<MissionChoiceOption>
                    {
                        new MissionChoiceOption { Label = "Only", TargetStepIndex = 2 },
                    }
                },
                new MissionStepDef { StepIndex = 2, ObjectiveText = "End", TriggerType = MissionTriggerType.ArriveAtNode, TargetNodeId = "node_a" },
            },
            CreditReward = 50,
        };
        WithEvoMission(def, state =>
        {
            MissionSystem.AcceptMission(state, "evo_bad_choice");
            MissionSystem.Process(state);
            Assert.That(MissionSystem.MakeChoice(state, 99), Is.False);
        });
    }

    [Test]
    public void EvoTriggers_NegativeCases_DoNotFire()
    {
        // ReputationMin: not enough rep.
        var step1 = new MissionActiveStep { TriggerType = MissionTriggerType.ReputationMin, TargetFactionId = "f", TargetQuantity = 50 };
        var state = MakeTradeWorldState();
        state.FactionReputation["f"] = 10;
        Assert.That(MissionSystem.EvaluateTrigger(state, step1), Is.False);

        // CreditsMin: not enough credits.
        var step2 = new MissionActiveStep { TriggerType = MissionTriggerType.CreditsMin, TargetQuantity = 5000 };
        state.PlayerCredits = 100;
        Assert.That(MissionSystem.EvaluateTrigger(state, step2), Is.False);

        // TechUnlocked: tech not present.
        var step3 = new MissionActiveStep { TriggerType = MissionTriggerType.TechUnlocked, TargetTechId = "nonexistent" };
        Assert.That(MissionSystem.EvaluateTrigger(state, step3), Is.False);

        // TimerExpired: before deadline.
        var step4 = new MissionActiveStep { TriggerType = MissionTriggerType.TimerExpired, DeadlineTick = 999 };
        Assert.That(MissionSystem.EvaluateTrigger(state, step4), Is.False);
    }

    [Test]
    public void EvoPrerequisites_BlockUntilSatisfied()
    {
        var prereq = CreateEvolutionTestMission("evo_p1", MissionTriggerType.ArriveAtNode);
        prereq.Steps[0].TargetNodeId = "node_a";
        var gated = CreateEvolutionTestMission("evo_p2", MissionTriggerType.ArriveAtNode);
        gated.Steps[0].TargetNodeId = "node_a";
        gated.Prerequisites = new List<string> { "evo_p1" };

        MissionContentV0.RegisterTestMission(prereq);
        MissionContentV0.RegisterTestMission(gated);
        try
        {
            var state = MakeTradeWorldState();
            Assert.That(MissionSystem.GetAvailableMissions(state).Any(m => m.MissionId == "evo_p2"), Is.False);
            MissionSystem.AcceptMission(state, "evo_p1");
            MissionSystem.Process(state);
            Assert.That(MissionSystem.GetAvailableMissions(state).Any(m => m.MissionId == "evo_p2"), Is.True);
        }
        finally
        {
            MissionContentV0.UnregisterTestMission("evo_p1");
            MissionContentV0.UnregisterTestMission("evo_p2");
        }
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
