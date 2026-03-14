using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Collections.Generic;

namespace SimCore.Tests.Systems;

// GATE.S7.WARFRONT.OBJECTIVES.001
[TestFixture]
public sealed class WarfrontObjectivesTests
{
    private static WarfrontState CreateWarfrontWithObjective(ObjectiveType type = ObjectiveType.SupplyDepot)
    {
        return new WarfrontState
        {
            Id = "wf1",
            CombatantA = "faction_a",
            CombatantB = "faction_b",
            Intensity = WarfrontIntensity.Skirmish,
            WarType = WarType.Hot,
            FleetStrengthA = 80,
            FleetStrengthB = 60,
            Objectives = new List<WarfrontObjective>
            {
                new WarfrontObjective { NodeId = "n1", Type = type }
            }
        };
    }

    [Test]
    public void ProcessObjectives_DominantFaction_AccumulatesDominanceTicks()
    {
        var wf = CreateWarfrontWithObjective();
        // A is dominant (80 > 60).
        WarfrontEvolutionSystem.ProcessObjectives(null, wf);

        Assert.That(wf.Objectives[0].DominantFactionId, Is.EqualTo("faction_a"));
        Assert.That(wf.Objectives[0].DominanceTicks, Is.EqualTo(1));
    }

    [Test]
    public void ProcessObjectives_SameDominant_Accumulates()
    {
        var wf = CreateWarfrontWithObjective();
        for (int i = 0; i < 5; i++)
            WarfrontEvolutionSystem.ProcessObjectives(null, wf);

        Assert.That(wf.Objectives[0].DominanceTicks, Is.EqualTo(5));
    }

    [Test]
    public void ProcessObjectives_DominantSwitches_Resets()
    {
        var wf = CreateWarfrontWithObjective();
        WarfrontEvolutionSystem.ProcessObjectives(null, wf); // A dominant.
        Assert.That(wf.Objectives[0].DominanceTicks, Is.EqualTo(1));

        wf.FleetStrengthA = 50;
        wf.FleetStrengthB = 70; // B now dominant.
        WarfrontEvolutionSystem.ProcessObjectives(null, wf);

        Assert.That(wf.Objectives[0].DominantFactionId, Is.EqualTo("faction_b"));
        Assert.That(wf.Objectives[0].DominanceTicks, Is.EqualTo(1));
    }

    [Test]
    public void ProcessObjectives_Tied_NoDominance()
    {
        var wf = CreateWarfrontWithObjective();
        wf.FleetStrengthA = 70;
        wf.FleetStrengthB = 70;
        WarfrontEvolutionSystem.ProcessObjectives(null, wf);

        // No dominant faction when tied.
        Assert.That(wf.Objectives[0].DominanceTicks, Is.EqualTo(0));
    }

    [Test]
    public void ProcessObjectives_CaptureAfterThreshold()
    {
        var wf = CreateWarfrontWithObjective();
        // Run enough ticks to exceed CaptureDominanceTicks.
        for (int i = 0; i < WarfrontTweaksV0.CaptureDominanceTicks; i++)
            WarfrontEvolutionSystem.ProcessObjectives(null, wf);

        Assert.That(wf.Objectives[0].ControllingFactionId, Is.EqualTo("faction_a"));
    }

    [Test]
    public void ProcessObjectives_NoCaptureBeforeThreshold()
    {
        var wf = CreateWarfrontWithObjective();
        for (int i = 0; i < WarfrontTweaksV0.CaptureDominanceTicks - 1; i++)
            WarfrontEvolutionSystem.ProcessObjectives(null, wf);

        Assert.That(wf.Objectives[0].ControllingFactionId, Is.EqualTo(""));
    }

    [Test]
    public void Factory_ControlledByA_RegensFleetStrengthA()
    {
        var wf = CreateWarfrontWithObjective(ObjectiveType.Factory);
        wf.Objectives[0].ControllingFactionId = "faction_a";
        wf.FleetStrengthA = 50;

        WarfrontEvolutionSystem.ProcessObjectives(null, wf);

        Assert.That(wf.FleetStrengthA, Is.EqualTo(50 + WarfrontTweaksV0.FactoryRegenPerTick));
    }

    [Test]
    public void Factory_RegenCapsAtMax()
    {
        var wf = CreateWarfrontWithObjective(ObjectiveType.Factory);
        wf.Objectives[0].ControllingFactionId = "faction_a";
        wf.FleetStrengthA = WarfrontTweaksV0.MaxFleetStrength;

        WarfrontEvolutionSystem.ProcessObjectives(null, wf);

        Assert.That(wf.FleetStrengthA, Is.EqualTo(WarfrontTweaksV0.MaxFleetStrength));
    }

    [Test]
    public void GetDominantFaction_AStronger_ReturnsA()
    {
        var wf = CreateWarfrontWithObjective();
        wf.FleetStrengthA = 90;
        wf.FleetStrengthB = 50;

        Assert.That(WarfrontEvolutionSystem.GetDominantFaction(wf), Is.EqualTo("faction_a"));
    }

    [Test]
    public void GetDominantFaction_Equal_ReturnsEmpty()
    {
        var wf = CreateWarfrontWithObjective();
        wf.FleetStrengthA = 50;
        wf.FleetStrengthB = 50;

        Assert.That(WarfrontEvolutionSystem.GetDominantFaction(wf), Is.EqualTo(""));
    }
}
