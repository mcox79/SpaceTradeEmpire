using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;
using System.Collections.Generic;

namespace SimCore.Tests.Systems;

// GATE.S7.POWER.BUDGET_ENFORCE.001: PowerBudgetSystem contract tests.
[TestFixture]
[Category("PowerBudget")]
public sealed class PowerBudgetSystemTests
{
    private SimState CreateMinimalState()
    {
        return new SimState(42);
    }

    private Fleet CreateFleetWithSlots(string id, string shipClass, params (SlotKind kind, string? moduleId, int powerDraw)[] slots)
    {
        var fleet = new Fleet
        {
            Id = id,
            OwnerId = "player",
            CurrentNodeId = "node_a",
            ShipClassId = shipClass,
        };
        foreach (var (kind, moduleId, powerDraw) in slots)
        {
            fleet.Slots.Add(new ModuleSlot
            {
                SlotId = $"slot_{fleet.Slots.Count}",
                SlotKind = kind,
                InstalledModuleId = moduleId,
                PowerDraw = powerDraw,
            });
        }
        return fleet;
    }

    [Test]
    public void WithinBudget_NoModulesDisabled()
    {
        var state = CreateMinimalState();
        // Corvette: BasePower = 40
        var fleet = CreateFleetWithSlots("fleet_1", "corvette",
            (SlotKind.Weapon, "mod_a", 15),
            (SlotKind.Weapon, "mod_b", 15)
        );
        state.Fleets["fleet_1"] = fleet;

        PowerBudgetSystem.Process(state);

        Assert.That(fleet.Slots[0].Disabled, Is.False);
        Assert.That(fleet.Slots[1].Disabled, Is.False);
    }

    [Test]
    public void OverBudget_LastSlotDisabled()
    {
        var state = CreateMinimalState();
        // Shuttle: BasePower = 25
        var fleet = CreateFleetWithSlots("fleet_1", "shuttle",
            (SlotKind.Weapon, "mod_a", 15),
            (SlotKind.Engine, "mod_b", 15) // Total 30 > 25
        );
        state.Fleets["fleet_1"] = fleet;

        PowerBudgetSystem.Process(state);

        Assert.That(fleet.Slots[0].Disabled, Is.False, "Higher priority slot stays enabled");
        Assert.That(fleet.Slots[1].Disabled, Is.True, "Lower priority slot gets disabled");
    }

    [Test]
    public void OverBudget_MultipleDisabled_UntilWithinBudget()
    {
        var state = CreateMinimalState();
        // Shuttle: BasePower = 20
        var fleet = CreateFleetWithSlots("fleet_1", "shuttle",
            (SlotKind.Weapon, "mod_a", 10),
            (SlotKind.Weapon, "mod_b", 10),
            (SlotKind.Engine, "mod_c", 10) // Total 30 > 20
        );
        state.Fleets["fleet_1"] = fleet;

        PowerBudgetSystem.Process(state);

        // Only 20 budget: slots 0+1 = 20, slot 2 must be disabled
        Assert.That(fleet.Slots[0].Disabled, Is.False);
        Assert.That(fleet.Slots[1].Disabled, Is.False);
        Assert.That(fleet.Slots[2].Disabled, Is.True);
    }

    [Test]
    public void ReEnablesModule_WhenBudgetFreed()
    {
        var state = CreateMinimalState();
        // Corvette: BasePower = 40
        var fleet = CreateFleetWithSlots("fleet_1", "corvette",
            (SlotKind.Weapon, "mod_a", 15),
            (SlotKind.Engine, "mod_b", 10)
        );
        fleet.Slots[1].Disabled = true; // Previously disabled
        state.Fleets["fleet_1"] = fleet;

        PowerBudgetSystem.Process(state);

        // Total 25 <= 40 budget: should re-enable
        Assert.That(fleet.Slots[1].Disabled, Is.False);
    }

    [Test]
    public void EmptySlots_NoDisabling()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleetWithSlots("fleet_1", "corvette",
            (SlotKind.Weapon, null, 0),
            (SlotKind.Engine, null, 0)
        );
        state.Fleets["fleet_1"] = fleet;

        PowerBudgetSystem.Process(state);

        Assert.That(fleet.Slots[0].Disabled, Is.False);
        Assert.That(fleet.Slots[1].Disabled, Is.False);
    }

    [Test]
    public void ComputeActivePowerDraw_ExcludesDisabled()
    {
        var fleet = CreateFleetWithSlots("fleet_1", "corvette",
            (SlotKind.Weapon, "mod_a", 15),
            (SlotKind.Engine, "mod_b", 10)
        );
        fleet.Slots[1].Disabled = true;

        int draw = PowerBudgetSystem.ComputeActivePowerDraw(fleet);
        Assert.That(draw, Is.EqualTo(15));
    }

    [Test]
    public void NoSlotsFleet_NoCrash()
    {
        var state = CreateMinimalState();
        var fleet = new Fleet { Id = "fleet_1", OwnerId = "player", ShipClassId = "corvette" };
        state.Fleets["fleet_1"] = fleet;

        Assert.DoesNotThrow(() => PowerBudgetSystem.Process(state));
    }
}
