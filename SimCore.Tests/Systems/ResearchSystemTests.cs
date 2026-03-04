using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

// GATE.S4.TECH.SYSTEM.001: Contract tests for ResearchSystem.
[TestFixture]
[Category("ResearchSystem")]
public sealed class ResearchSystemTests
{
    private SimState CreateState()
    {
        var state = new SimState(42);
        state.PlayerCredits = 1000;
        // Add player fleet for unlock effects
        state.Fleets["fleet_trader_1"] = new Fleet { Id = "fleet_trader_1", TechLevel = 0 };
        return state;
    }

    [Test]
    public void StartResearch_Succeeds_ForNoPrereqTech()
    {
        var state = CreateState();
        var result = ResearchSystem.StartResearch(state, "improved_thrusters");
        Assert.That(result.Success, Is.True);
        Assert.That(state.Tech.IsResearching, Is.True);
        Assert.That(state.Tech.CurrentResearchTechId, Is.EqualTo("improved_thrusters"));
        Assert.That(state.Tech.ResearchTotalTicks, Is.EqualTo(8));
    }

    [Test]
    public void StartResearch_Fails_UnknownTech()
    {
        var state = CreateState();
        var result = ResearchSystem.StartResearch(state, "nonexistent");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("unknown_tech"));
    }

    [Test]
    public void StartResearch_Fails_AlreadyResearching()
    {
        var state = CreateState();
        ResearchSystem.StartResearch(state, "improved_thrusters");
        var result = ResearchSystem.StartResearch(state, "shield_mk2");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("already_researching"));
    }

    [Test]
    public void StartResearch_Fails_PrerequisitesNotMet()
    {
        var state = CreateState();
        var result = ResearchSystem.StartResearch(state, "advanced_refining");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("prerequisites_not_met"));
    }

    [Test]
    public void StartResearch_Fails_AlreadyUnlocked()
    {
        var state = CreateState();
        state.Tech.UnlockedTechIds.Add("improved_thrusters");
        var result = ResearchSystem.StartResearch(state, "improved_thrusters");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("already_unlocked"));
    }

    [Test]
    public void ProcessResearch_AdvancesProgress()
    {
        var state = CreateState();
        ResearchSystem.StartResearch(state, "improved_thrusters");
        ResearchSystem.ProcessResearch(state);
        Assert.That(state.Tech.ResearchProgressTicks, Is.EqualTo(1));
        Assert.That(state.Tech.IsResearching, Is.True);
    }

    [Test]
    public void ProcessResearch_CompletesWhenDone()
    {
        var state = CreateState();
        ResearchSystem.StartResearch(state, "improved_thrusters");
        // Tick enough times to complete (8 ticks)
        for (int i = 0; i < 8; i++)
        {
            ResearchSystem.ProcessResearch(state);
        }
        Assert.That(state.Tech.IsResearching, Is.False);
        Assert.That(state.Tech.UnlockedTechIds.Contains("improved_thrusters"), Is.True);
    }

    [Test]
    public void ProcessResearch_Stalls_WhenNoCredits()
    {
        var state = CreateState();
        state.PlayerCredits = 0;
        ResearchSystem.StartResearch(state, "improved_thrusters");
        ResearchSystem.ProcessResearch(state);
        Assert.That(state.Tech.ResearchProgressTicks, Is.EqualTo(0), "Should stall with no credits");
    }

    [Test]
    public void ProcessResearch_DeductsCredits()
    {
        var state = CreateState();
        long before = state.PlayerCredits;
        ResearchSystem.StartResearch(state, "improved_thrusters");
        ResearchSystem.ProcessResearch(state);
        Assert.That(state.PlayerCredits, Is.LessThan(before));
    }

    [Test]
    public void CompleteResearch_FractureDrive_IncreasesTechLevel()
    {
        var state = CreateState();
        state.Tech.TechLevel = 2; // GATE.S4.TECH_INDUSTRIALIZE.TIER_SCALING.001: need TechLevel 2 for Tier 3
        state.Tech.UnlockedTechIds.Add("shield_mk2");
        state.Tech.UnlockedTechIds.Add("weapon_systems_2");
        state.PlayerCredits = 100000; // enough for tier 3 costs
        ResearchSystem.StartResearch(state, "fracture_drive");
        // Complete it
        for (int i = 0; i < 25; i++)
            ResearchSystem.ProcessResearch(state);
        Assert.That(state.Fleets["fleet_trader_1"].TechLevel, Is.EqualTo(1));
    }

    [Test]
    public void EventLog_RecordsStartAndComplete()
    {
        var state = CreateState();
        ResearchSystem.StartResearch(state, "improved_thrusters");
        for (int i = 0; i < 8; i++)
            ResearchSystem.ProcessResearch(state);

        Assert.That(state.Tech.EventLog, Has.Count.EqualTo(2));
        Assert.That(state.Tech.EventLog[0].EventType, Is.EqualTo("Started"));
        Assert.That(state.Tech.EventLog[1].EventType, Is.EqualTo("Completed"));
    }

    // GATE.S4.TECH_INDUSTRIALIZE.TIER_SCALING.001: Tier scaling tests

    [Test]
    public void TierField_DefaultsToOne()
    {
        var def = new TechDef();
        Assert.That(def.Tier, Is.EqualTo(1));
    }

    [Test]
    public void StartResearch_TierLocked_Fails()
    {
        var state = CreateState();
        // fracture_drive is Tier 3, player TechLevel is 0 → can only research Tier 1
        state.Tech.UnlockedTechIds.Add("shield_mk2");
        state.Tech.UnlockedTechIds.Add("weapon_systems_2");
        var result = ResearchSystem.StartResearch(state, "fracture_drive");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("tier_locked"));
    }

    [Test]
    public void StartResearch_TierUnlocked_Succeeds()
    {
        var state = CreateState();
        state.Tech.TechLevel = 2;
        state.Tech.UnlockedTechIds.Add("shield_mk2");
        state.Tech.UnlockedTechIds.Add("weapon_systems_2");
        var result = ResearchSystem.StartResearch(state, "fracture_drive");
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void TechLevel_IncreasesOnUnlock()
    {
        var state = CreateState();
        state.Tech.TechLevel = 2; // allow Tier 3 research
        state.Tech.UnlockedTechIds.Add("shield_mk2");
        state.Tech.UnlockedTechIds.Add("weapon_systems_2");
        state.PlayerCredits = 100000; // enough credits for tier 3 cost
        ResearchSystem.StartResearch(state, "fracture_drive");
        for (int i = 0; i < 25; i++)
            ResearchSystem.ProcessResearch(state);
        Assert.That(state.Tech.TechLevel, Is.EqualTo(3));
    }

    [Test]
    public void CostMultiplier_ScalesWithTier()
    {
        // Tier 1 tech: cost per tick = CreditCostPerTickBase * 1 = 5
        var state1 = CreateState();
        state1.PlayerCredits = 10000;
        ResearchSystem.StartResearch(state1, "improved_thrusters"); // Tier 1
        long before1 = state1.PlayerCredits;
        ResearchSystem.ProcessResearch(state1);
        long tier1Cost = before1 - state1.PlayerCredits;

        // Tier 2 tech: cost per tick = CreditCostPerTickBase * 2 = 10
        var state2 = CreateState();
        state2.PlayerCredits = 10000;
        state2.Tech.UnlockedTechIds.Add("improved_thrusters"); // prereq met
        state2.Tech.TechLevel = 1; // allow Tier 2
        ResearchSystem.StartResearch(state2, "advanced_refining"); // Tier 2
        long before2 = state2.PlayerCredits;
        ResearchSystem.ProcessResearch(state2);
        long tier2Cost = before2 - state2.PlayerCredits;

        Assert.That(tier2Cost, Is.EqualTo(tier1Cost * 2), "Tier 2 should cost 2x Tier 1");
    }

    [Test]
    public void Determinism_SameSeedSameResult()
    {
        long credits1, credits2;
        int events1, events2;

        {
            var s = CreateState();
            ResearchSystem.StartResearch(s, "improved_thrusters");
            for (int i = 0; i < 10; i++) ResearchSystem.ProcessResearch(s);
            credits1 = s.PlayerCredits;
            events1 = s.Tech.EventLog.Count;
        }
        {
            var s = CreateState();
            ResearchSystem.StartResearch(s, "improved_thrusters");
            for (int i = 0; i < 10; i++) ResearchSystem.ProcessResearch(s);
            credits2 = s.PlayerCredits;
            events2 = s.Tech.EventLog.Count;
        }

        Assert.That(credits1, Is.EqualTo(credits2));
        Assert.That(events1, Is.EqualTo(events2));
    }
}
