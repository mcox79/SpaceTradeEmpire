using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

// GATE.S9.MISSION_EVOL.TRIGGERS.001
[TestFixture]
public sealed class MissionTriggerTests
{
    [Test]
    public void EvaluateTrigger_ReputationMin_Met()
    {
        var state = new SimState(42);
        state.FactionReputation["faction_a"] = 50;

        var step = new MissionActiveStep
        {
            TriggerType = MissionTriggerType.ReputationMin,
            TargetFactionId = "faction_a",
            TargetQuantity = 30
        };

        Assert.That(MissionSystem.EvaluateTrigger(state, step), Is.True);
    }

    [Test]
    public void EvaluateTrigger_ReputationMin_NotMet()
    {
        var state = new SimState(42);
        state.FactionReputation["faction_a"] = 10;

        var step = new MissionActiveStep
        {
            TriggerType = MissionTriggerType.ReputationMin,
            TargetFactionId = "faction_a",
            TargetQuantity = 30
        };

        Assert.That(MissionSystem.EvaluateTrigger(state, step), Is.False);
    }

    [Test]
    public void EvaluateTrigger_CreditsMin_Met()
    {
        var state = new SimState(42);
        state.PlayerCredits = 5000;

        var step = new MissionActiveStep
        {
            TriggerType = MissionTriggerType.CreditsMin,
            TargetQuantity = 3000
        };

        Assert.That(MissionSystem.EvaluateTrigger(state, step), Is.True);
    }

    [Test]
    public void EvaluateTrigger_CreditsMin_NotMet()
    {
        var state = new SimState(42);
        state.PlayerCredits = 100;

        var step = new MissionActiveStep
        {
            TriggerType = MissionTriggerType.CreditsMin,
            TargetQuantity = 3000
        };

        Assert.That(MissionSystem.EvaluateTrigger(state, step), Is.False);
    }

    [Test]
    public void EvaluateTrigger_TechUnlocked_Met()
    {
        var state = new SimState(42);
        state.Tech.UnlockedTechIds.Add("fracture_drive");

        var step = new MissionActiveStep
        {
            TriggerType = MissionTriggerType.TechUnlocked,
            TargetTechId = "fracture_drive"
        };

        Assert.That(MissionSystem.EvaluateTrigger(state, step), Is.True);
    }

    [Test]
    public void EvaluateTrigger_TechUnlocked_NotMet()
    {
        var state = new SimState(42);

        var step = new MissionActiveStep
        {
            TriggerType = MissionTriggerType.TechUnlocked,
            TargetTechId = "fracture_drive"
        };

        Assert.That(MissionSystem.EvaluateTrigger(state, step), Is.False);
    }

    [Test]
    public void EvaluateTrigger_TimerExpired_Met()
    {
        var state = new SimState(42);
        // Advance to tick 100.
        for (int i = 0; i < 100; i++) state.AdvanceTick();

        var step = new MissionActiveStep
        {
            TriggerType = MissionTriggerType.TimerExpired,
            DeadlineTick = 50
        };

        Assert.That(MissionSystem.EvaluateTrigger(state, step), Is.True);
    }

    [Test]
    public void EvaluateTrigger_TimerExpired_NotMet()
    {
        var state = new SimState(42);
        // Advance to tick 10.
        for (int i = 0; i < 10; i++) state.AdvanceTick();

        var step = new MissionActiveStep
        {
            TriggerType = MissionTriggerType.TimerExpired,
            DeadlineTick = 50
        };

        Assert.That(MissionSystem.EvaluateTrigger(state, step), Is.False);
    }
}
