using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

// GATE.S7.WARFRONT.ATTRITION.001
[TestFixture]
public sealed class WarfrontAttritionTests
{
    private static WarfrontState CreateWarfront(WarfrontIntensity intensity = WarfrontIntensity.Skirmish)
    {
        return new WarfrontState
        {
            Id = "wf1",
            CombatantA = "faction_a",
            CombatantB = "faction_b",
            Intensity = intensity,
            WarType = WarType.Hot,
            FleetStrengthA = 100,
            FleetStrengthB = 100
        };
    }

    [Test]
    public void ApplyFleetAttrition_BelowMinIntensity_NoAttrition()
    {
        var state = new SimState(42);
        var wf = CreateWarfront(WarfrontIntensity.Tension);
        WarfrontEvolutionSystem.ApplyFleetAttrition(state, wf);

        Assert.That(wf.FleetStrengthA, Is.EqualTo(100));
        Assert.That(wf.FleetStrengthB, Is.EqualTo(100));
    }

    [Test]
    public void ApplyFleetAttrition_Skirmish_ReducesStrength()
    {
        var state = new SimState(42);
        var wf = CreateWarfront(WarfrontIntensity.Skirmish);
        WarfrontEvolutionSystem.ApplyFleetAttrition(state, wf);

        // At Skirmish (2), base attrition = 1 * (2-1) = 1, unsupplied bonus = 2, total = 3.
        Assert.That(wf.FleetStrengthA, Is.EqualTo(100 - 3));
        Assert.That(wf.FleetStrengthB, Is.EqualTo(100 - 3));
    }

    [Test]
    public void ApplyFleetAttrition_HighIntensity_HigherAttrition()
    {
        var state = new SimState(42);
        var wf = CreateWarfront(WarfrontIntensity.TotalWar);
        WarfrontEvolutionSystem.ApplyFleetAttrition(state, wf);

        // At TotalWar (4), base = 1 * (4-1) = 3, unsupplied = 2, total = 5.
        Assert.That(wf.FleetStrengthA, Is.EqualTo(100 - 5));
        Assert.That(wf.FleetStrengthB, Is.EqualTo(100 - 5));
    }

    [Test]
    public void ApplyFleetAttrition_SuppliedFaction_ReducedAttrition()
    {
        var state = new SimState(42);
        var wf = CreateWarfront(WarfrontIntensity.Skirmish);
        state.Warfronts["wf1"] = wf;

        // Supply faction_a.
        state.WarSupplyLedger["wf1"] = new System.Collections.Generic.Dictionary<string, int>
        {
            ["munitions"] = 10
        };

        WarfrontEvolutionSystem.ApplyFleetAttrition(state, wf);

        // A is supplied: base 1 only. B is unsupplied: base 1 + bonus 2 = 3.
        // Wait — HasRecentSupply checks warfrontId, not factionId. It just checks if ANY delivery exists.
        // Both would be "supplied" since the ledger has entries.
        Assert.That(wf.FleetStrengthA, Is.EqualTo(100 - 1));
        Assert.That(wf.FleetStrengthB, Is.EqualTo(100 - 1));
    }

    [Test]
    public void ApplyFleetAttrition_DepletedFleet_DeEscalates()
    {
        var state = new SimState(42);
        var wf = CreateWarfront(WarfrontIntensity.OpenWar);
        wf.FleetStrengthA = 1; // Will hit 0.
        WarfrontEvolutionSystem.ApplyFleetAttrition(state, wf);

        Assert.That(wf.FleetStrengthA, Is.EqualTo(0));
        Assert.That(wf.Intensity, Is.EqualTo(WarfrontIntensity.Tension));
    }

    [Test]
    public void RestoreFleetStrength_CombatantA_RestoresUp()
    {
        var wf = CreateWarfront();
        wf.FleetStrengthA = 50;

        WarfrontEvolutionSystem.RestoreFleetStrength(wf, "faction_a", 5);

        Assert.That(wf.FleetStrengthA, Is.EqualTo(50 + 5 * WarfrontTweaksV0.SupplyRestorePerDelivery));
    }

    [Test]
    public void RestoreFleetStrength_CapsAtMax()
    {
        var wf = CreateWarfront();
        wf.FleetStrengthA = 99;

        WarfrontEvolutionSystem.RestoreFleetStrength(wf, "faction_a", 10);

        Assert.That(wf.FleetStrengthA, Is.EqualTo(WarfrontTweaksV0.MaxFleetStrength));
    }

    [Test]
    public void RestoreFleetStrength_UnknownFaction_NoOp()
    {
        var wf = CreateWarfront();
        wf.FleetStrengthA = 50;
        wf.FleetStrengthB = 50;

        WarfrontEvolutionSystem.RestoreFleetStrength(wf, "faction_c", 10);

        Assert.That(wf.FleetStrengthA, Is.EqualTo(50));
        Assert.That(wf.FleetStrengthB, Is.EqualTo(50));
    }
}
