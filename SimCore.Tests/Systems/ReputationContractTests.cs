using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Content;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.Systems;

// GATE.S7.REPUTATION.CONTRACTS.001
[TestFixture]
public sealed class ReputationContractTests
{
    private const string FactionMissionId = "test_faction_contract";
    private const string FactionId = "test_faction";

    private static MissionDef CreateFactionContract(int requiredRepTier)
    {
        return new MissionDef
        {
            MissionId = FactionMissionId,
            Title = "Faction Contract",
            Description = "A mission from a faction",
            FactionId = FactionId,
            RequiredRepTier = requiredRepTier,
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef
                {
                    StepIndex = 0,
                    ObjectiveText = "Arrive",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "n1"
                }
            },
            CreditReward = 200,
        };
    }

    private static SimState CreateState(int reputation)
    {
        var state = new SimState(42);
        state.PlayerLocationNodeId = "n1";
        state.Nodes["n1"] = new Node { Id = "n1", Kind = NodeKind.Station };
        state.FactionReputation[FactionId] = reputation;
        return state;
    }

    [Test]
    public void FriendlyContract_PlayerFriendly_Available()
    {
        // RequiredRepTier = 1 (Friendly). Player rep = 30 → Friendly.
        var def = CreateFactionContract((int)RepTier.Friendly);
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState(30);
            var available = MissionSystem.GetAvailableMissions(state);
            Assert.That(available.Any(m => m.MissionId == FactionMissionId), Is.True,
                "Friendly player should see Friendly-gated contract.");
        }
        finally { MissionContentV0.UnregisterTestMission(FactionMissionId); }
    }

    [Test]
    public void FriendlyContract_PlayerNeutral_NotAvailable()
    {
        // RequiredRepTier = 1 (Friendly). Player rep = 0 → Neutral (tier 2).
        var def = CreateFactionContract((int)RepTier.Friendly);
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState(0);
            var available = MissionSystem.GetAvailableMissions(state);
            Assert.That(available.Any(m => m.MissionId == FactionMissionId), Is.False,
                "Neutral player should NOT see Friendly-gated contract.");
        }
        finally { MissionContentV0.UnregisterTestMission(FactionMissionId); }
    }

    [Test]
    public void AlliedContract_PlayerAllied_Available()
    {
        // RequiredRepTier = 0 (Allied). Player rep = 80 → Allied.
        var def = CreateFactionContract((int)RepTier.Allied);
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState(80);
            var available = MissionSystem.GetAvailableMissions(state);
            Assert.That(available.Any(m => m.MissionId == FactionMissionId), Is.True);
        }
        finally { MissionContentV0.UnregisterTestMission(FactionMissionId); }
    }

    [Test]
    public void AlliedContract_PlayerFriendly_NotAvailable()
    {
        // RequiredRepTier = 0 (Allied). Player rep = 30 → Friendly (tier 1).
        var def = CreateFactionContract((int)RepTier.Allied);
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState(30);
            var available = MissionSystem.GetAvailableMissions(state);
            Assert.That(available.Any(m => m.MissionId == FactionMissionId), Is.False,
                "Friendly player should NOT see Allied-gated contract.");
        }
        finally { MissionContentV0.UnregisterTestMission(FactionMissionId); }
    }

    [Test]
    public void NeutralContract_PlayerNeutral_Available()
    {
        // RequiredRepTier = 2 (Neutral). Player rep = 0 → Neutral.
        var def = CreateFactionContract((int)RepTier.Neutral);
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState(0);
            var available = MissionSystem.GetAvailableMissions(state);
            Assert.That(available.Any(m => m.MissionId == FactionMissionId), Is.True);
        }
        finally { MissionContentV0.UnregisterTestMission(FactionMissionId); }
    }

    [Test]
    public void NeutralContract_PlayerHostile_NotAvailable()
    {
        // RequiredRepTier = 2 (Neutral). Player rep = -30 → Hostile (tier 3).
        var def = CreateFactionContract((int)RepTier.Neutral);
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState(-30);
            var available = MissionSystem.GetAvailableMissions(state);
            Assert.That(available.Any(m => m.MissionId == FactionMissionId), Is.False,
                "Hostile player should NOT see Neutral-gated contract.");
        }
        finally { MissionContentV0.UnregisterTestMission(FactionMissionId); }
    }

    [Test]
    public void UniversalMission_NoFaction_AlwaysAvailable()
    {
        // RequiredRepTier = -1, FactionId = "" → no faction gating.
        var def = new MissionDef
        {
            MissionId = "test_universal",
            Title = "Universal",
            Description = "Anyone can do this",
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef { StepIndex = 0, ObjectiveText = "Go", TriggerType = MissionTriggerType.ArriveAtNode, TargetNodeId = "n1" }
            },
            CreditReward = 50,
        };
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState(-100); // Even enemy rep.
            var available = MissionSystem.GetAvailableMissions(state);
            Assert.That(available.Any(m => m.MissionId == "test_universal"), Is.True);
        }
        finally { MissionContentV0.UnregisterTestMission("test_universal"); }
    }

    [Test]
    public void FactionContract_ImproveRep_BecomesAvailable()
    {
        var def = CreateFactionContract((int)RepTier.Friendly);
        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = CreateState(0); // Neutral.
            Assert.That(MissionSystem.GetAvailableMissions(state).Any(m => m.MissionId == FactionMissionId), Is.False);

            // Improve reputation to Friendly.
            state.FactionReputation[FactionId] = 30;
            Assert.That(MissionSystem.GetAvailableMissions(state).Any(m => m.MissionId == FactionMissionId), Is.True);
        }
        finally { MissionContentV0.UnregisterTestMission(FactionMissionId); }
    }
}
