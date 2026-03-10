using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

// GATE.S4.UPGRADE.SYSTEM.001: Contract tests for RefitSystem.
[TestFixture]
[Category("RefitSystem")]
public sealed class RefitSystemTests
{
    private SimState CreateState()
    {
        var state = new SimState(42);
        state.PlayerCredits = 1000;
        var fleet = new Fleet { Id = "fleet_trader_1", ShipClassId = "corvette" };
        fleet.Slots.Add(new ModuleSlot { SlotId = "weapon_0", SlotKind = SlotKind.Weapon });
        fleet.Slots.Add(new ModuleSlot { SlotId = "engine_0", SlotKind = SlotKind.Engine });
        fleet.Slots.Add(new ModuleSlot { SlotId = "utility_0", SlotKind = SlotKind.Utility });
        state.Fleets["fleet_trader_1"] = fleet;
        return state;
    }

    [Test]
    public void InstallModule_Succeeds_NoTechReq()
    {
        var state = CreateState();
        var result = RefitSystem.InstallModule(state, "fleet_trader_1", 0, WellKnownModuleIds.WeaponCannonMk1);
        Assert.That(result.Success, Is.True);
        Assert.That(state.Fleets["fleet_trader_1"].Slots[0].InstalledModuleId, Is.EqualTo(WellKnownModuleIds.WeaponCannonMk1));
    }

    [Test]
    public void InstallModule_DeductsCost()
    {
        var state = CreateState();
        long before = state.PlayerCredits;
        RefitSystem.InstallModule(state, "fleet_trader_1", 0, WellKnownModuleIds.WeaponCannonMk1);
        var def = UpgradeContentV0.GetById(WellKnownModuleIds.WeaponCannonMk1)!;
        Assert.That(state.PlayerCredits, Is.EqualTo(before - def.CreditCost));
    }

    [Test]
    public void InstallModule_Fails_SlotKindMismatch()
    {
        var state = CreateState();
        // Try to install weapon in engine slot (index 1)
        var result = RefitSystem.InstallModule(state, "fleet_trader_1", 1, WellKnownModuleIds.WeaponCannonMk1);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("slot_kind_mismatch"));
    }

    [Test]
    public void InstallModule_Fails_TechNotUnlocked()
    {
        var state = CreateState();
        var result = RefitSystem.InstallModule(state, "fleet_trader_1", 0, "weapon_cannon_mk2");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("tech_not_unlocked"));
    }

    [Test]
    public void InstallModule_Succeeds_WithTechUnlocked()
    {
        var state = CreateState();
        state.Tech.UnlockedTechIds.Add("weapon_systems_2");
        var result = RefitSystem.InstallModule(state, "fleet_trader_1", 0, "weapon_cannon_mk2");
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void InstallModule_Fails_InsufficientCredits()
    {
        var state = CreateState();
        state.PlayerCredits = 0;
        var result = RefitSystem.InstallModule(state, "fleet_trader_1", 0, WellKnownModuleIds.WeaponCannonMk1);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("insufficient_credits"));
    }

    [Test]
    public void InstallModule_Fails_InvalidSlotIndex()
    {
        var state = CreateState();
        var result = RefitSystem.InstallModule(state, "fleet_trader_1", 99, WellKnownModuleIds.WeaponCannonMk1);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("invalid_slot_index"));
    }

    [Test]
    public void RemoveModule_Succeeds()
    {
        var state = CreateState();
        RefitSystem.InstallModule(state, "fleet_trader_1", 0, WellKnownModuleIds.WeaponCannonMk1);
        var result = RefitSystem.RemoveModule(state, "fleet_trader_1", 0);
        Assert.That(result.Success, Is.True);
        Assert.That(state.Fleets["fleet_trader_1"].Slots[0].InstalledModuleId, Is.Null);
    }

    [Test]
    public void RemoveModule_Fails_EmptySlot()
    {
        var state = CreateState();
        var result = RefitSystem.RemoveModule(state, "fleet_trader_1", 0);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("slot_empty"));
    }

    [Test]
    public void ComputeBonuses_EmptySlots_ZeroBonuses()
    {
        var state = CreateState();
        var bonuses = RefitSystem.ComputeBonuses(state.Fleets["fleet_trader_1"]);
        Assert.That(bonuses.SpeedBonusPct, Is.EqualTo(0));
        Assert.That(bonuses.ShieldBonusFlat, Is.EqualTo(0));
        Assert.That(bonuses.DamageBonusPct, Is.EqualTo(0));
    }

    [Test]
    public void ComputeBonuses_WithInstalledModules()
    {
        var state = CreateState();
        state.Tech.UnlockedTechIds.Add("improved_thrusters");
        RefitSystem.InstallModule(state, "fleet_trader_1", 1, "engine_booster_mk1");
        var bonuses = RefitSystem.ComputeBonuses(state.Fleets["fleet_trader_1"]);
        Assert.That(bonuses.SpeedBonusPct, Is.EqualTo(20));
    }

    // GATE.S4.UPGRADE_PIPELINE.TIMED_REFIT.001: Timed refit queue tests.

    [Test]
    public void QueueInstall_AddsToQueue()
    {
        var state = CreateState();
        var result = RefitSystem.QueueInstall(state, "fleet_trader_1", 0, WellKnownModuleIds.WeaponCannonMk1);
        Assert.That(result.Success, Is.True);

        var fleet = state.Fleets["fleet_trader_1"];
        Assert.That(fleet.RefitQueue.Count, Is.EqualTo(1));
        Assert.That(fleet.RefitQueue[0].ModuleId, Is.EqualTo(WellKnownModuleIds.WeaponCannonMk1));
        Assert.That(fleet.RefitQueue[0].SlotIndex, Is.EqualTo(0));

        var def = UpgradeContentV0.GetById(WellKnownModuleIds.WeaponCannonMk1)!;
        Assert.That(fleet.RefitQueue[0].TicksRemaining, Is.EqualTo(def.InstallTicks));

        // Module should NOT be installed yet.
        Assert.That(fleet.Slots[0].InstalledModuleId, Is.Null.Or.Empty);
    }

    [Test]
    public void ProcessRefitQueue_Decrements()
    {
        var state = CreateState();
        RefitSystem.QueueInstall(state, "fleet_trader_1", 0, WellKnownModuleIds.WeaponCannonMk1);

        var def = UpgradeContentV0.GetById(WellKnownModuleIds.WeaponCannonMk1)!;
        int initialTicks = def.InstallTicks;

        RefitSystem.ProcessRefitQueue(state);

        var fleet = state.Fleets["fleet_trader_1"];
        Assert.That(fleet.RefitQueue.Count, Is.EqualTo(1));
        Assert.That(fleet.RefitQueue[0].TicksRemaining, Is.EqualTo(initialTicks - 1));
    }

    [Test]
    public void ProcessRefitQueue_InstallsOnCompletion()
    {
        var state = CreateState();
        RefitSystem.QueueInstall(state, "fleet_trader_1", 0, WellKnownModuleIds.WeaponCannonMk1);

        var def = UpgradeContentV0.GetById(WellKnownModuleIds.WeaponCannonMk1)!;

        // Tick down to completion.
        for (int i = 0; i < def.InstallTicks; i++)
            RefitSystem.ProcessRefitQueue(state);

        var fleet = state.Fleets["fleet_trader_1"];
        // Queue should be empty after install completes.
        Assert.That(fleet.RefitQueue.Count, Is.EqualTo(0));
        // Module should now be installed.
        Assert.That(fleet.Slots[0].InstalledModuleId, Is.EqualTo(WellKnownModuleIds.WeaponCannonMk1));
    }

    [Test]
    public void QueueInstall_InvalidModule_Fails()
    {
        var state = CreateState();
        var result = RefitSystem.QueueInstall(state, "fleet_trader_1", 0, "nonexistent_module_xyz");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("unknown_module"));
    }

    // GATE.S18.SHIP_MODULES.FITTING_BUDGET.001: Power budget tests.

    [Test]
    public void ComputeTotalPowerDraw_EmptySlots_Zero()
    {
        var state = CreateState();
        int draw = RefitSystem.ComputeTotalPowerDraw(state.Fleets["fleet_trader_1"]);
        Assert.That(draw, Is.EqualTo(0));
    }

    [Test]
    public void ComputeTotalPowerDraw_WithModule_ReturnsDraw()
    {
        var state = CreateState();
        RefitSystem.InstallModule(state, "fleet_trader_1", 0, WellKnownModuleIds.WeaponCannonMk1);
        int draw = RefitSystem.ComputeTotalPowerDraw(state.Fleets["fleet_trader_1"]);
        var def = UpgradeContentV0.GetById(WellKnownModuleIds.WeaponCannonMk1)!;
        Assert.That(draw, Is.EqualTo(def.PowerDraw));
    }

    [Test]
    public void GetPowerBudget_Corvette_Returns40()
    {
        var state = CreateState();
        int budget = RefitSystem.GetPowerBudget(state.Fleets["fleet_trader_1"]);
        Assert.That(budget, Is.EqualTo(40)); // corvette BasePower
    }

    [Test]
    public void InstallModule_ExceedsPowerBudget_Fails()
    {
        // Shuttle has BasePower=20, Laser Mk2 has PowerDraw=15
        var state = CreateState();
        state.PlayerCredits = 5000;
        state.Tech.UnlockedTechIds.Add("weapon_calibration");
        var fleet = state.Fleets["fleet_trader_1"];
        fleet.ShipClassId = "shuttle"; // BasePower = 20

        // Install Laser Mk2 (15 power) — should succeed, 15/20
        var r1 = RefitSystem.InstallModule(state, "fleet_trader_1", 0, WellKnownModuleIds.LaserMk2);
        Assert.That(r1.Success, Is.True);

        // Add another weapon slot and try another Laser Mk2 (15 power) — total 30 > 20
        fleet.Slots.Add(new ModuleSlot { SlotId = "weapon_1", SlotKind = SlotKind.Weapon });
        var r2 = RefitSystem.InstallModule(state, "fleet_trader_1", 3, WellKnownModuleIds.LaserMk2);
        Assert.That(r2.Success, Is.False);
        Assert.That(r2.Reason, Is.EqualTo("power_exceeded"));
    }

    [Test]
    public void InstallModule_ReplaceModule_PowerBudgetAccountsForRemoval()
    {
        var state = CreateState();
        state.PlayerCredits = 5000;
        state.Tech.UnlockedTechIds.Add("weapon_calibration");
        var fleet = state.Fleets["fleet_trader_1"];
        fleet.ShipClassId = "shuttle"; // BasePower = 20

        // Install Cannon Mk1 (5 power) — 5/20
        RefitSystem.InstallModule(state, "fleet_trader_1", 0, WellKnownModuleIds.WeaponCannonMk1);
        // Replace with Laser Mk2 (15 power) — net change is +10, total 15/20 — should pass
        var r = RefitSystem.InstallModule(state, "fleet_trader_1", 0, WellKnownModuleIds.LaserMk2);
        Assert.That(r.Success, Is.True);
    }

    [Test]
    public void InstallModule_SetsPowerDrawOnSlot()
    {
        var state = CreateState();
        RefitSystem.InstallModule(state, "fleet_trader_1", 0, WellKnownModuleIds.WeaponCannonMk1);
        var slot = state.Fleets["fleet_trader_1"].Slots[0];
        var def = UpgradeContentV0.GetById(WellKnownModuleIds.WeaponCannonMk1)!;
        Assert.That(slot.PowerDraw, Is.EqualTo(def.PowerDraw));
    }

    [Test]
    public void RemoveModule_ClearsPowerDraw()
    {
        var state = CreateState();
        RefitSystem.InstallModule(state, "fleet_trader_1", 0, WellKnownModuleIds.WeaponCannonMk1);
        RefitSystem.RemoveModule(state, "fleet_trader_1", 0);
        Assert.That(state.Fleets["fleet_trader_1"].Slots[0].PowerDraw, Is.EqualTo(0));
    }

    [Test]
    public void QueueInstall_ExceedsPowerBudget_Fails()
    {
        var state = CreateState();
        state.PlayerCredits = 5000;
        state.Tech.UnlockedTechIds.Add("weapon_calibration");
        var fleet = state.Fleets["fleet_trader_1"];
        fleet.ShipClassId = "shuttle"; // BasePower = 20

        RefitSystem.InstallModule(state, "fleet_trader_1", 0, WellKnownModuleIds.LaserMk2); // 15 power
        fleet.Slots.Add(new ModuleSlot { SlotId = "weapon_1", SlotKind = SlotKind.Weapon });
        var r = RefitSystem.QueueInstall(state, "fleet_trader_1", 3, WellKnownModuleIds.LaserMk2);
        Assert.That(r.Success, Is.False);
        Assert.That(r.Reason, Is.EqualTo("power_exceeded"));
    }

    [Test]
    public void ProcessRefitQueue_SetsPowerDrawOnSlot()
    {
        var state = CreateState();
        RefitSystem.QueueInstall(state, "fleet_trader_1", 0, WellKnownModuleIds.WeaponCannonMk1);
        var def = UpgradeContentV0.GetById(WellKnownModuleIds.WeaponCannonMk1)!;

        for (int i = 0; i < def.InstallTicks; i++)
            RefitSystem.ProcessRefitQueue(state);

        var slot = state.Fleets["fleet_trader_1"].Slots[0];
        Assert.That(slot.PowerDraw, Is.EqualTo(def.PowerDraw));
    }

    // GATE.S7.T2_MODULES.FITTING.001: Faction reputation gating tests.

    [Test]
    public void InstallModule_T2_Fails_InsufficientFactionRep()
    {
        var state = CreateState();
        state.PlayerCredits = 5000;
        // Unlock the tech prerequisite for Railgun T2.
        var railgunDef = UpgradeContentV0.GetById(WellKnownModuleIds.WeaponRailgunT2)!;
        state.Tech.UnlockedTechIds.Add(railgunDef.TechPrerequisite);
        // Don't set faction rep — defaults to 0, which is less than FactionRepRequired (25).
        var result = RefitSystem.InstallModule(state, "fleet_trader_1", 0, WellKnownModuleIds.WeaponRailgunT2);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("faction_rep_insufficient"));
    }

    [Test]
    public void InstallModule_T2_Succeeds_WithSufficientFactionRep()
    {
        var state = CreateState();
        state.PlayerCredits = 5000;
        var railgunDef = UpgradeContentV0.GetById(WellKnownModuleIds.WeaponRailgunT2)!;
        state.Tech.UnlockedTechIds.Add(railgunDef.TechPrerequisite);
        // Set faction rep above threshold.
        state.FactionReputation[railgunDef.FactionId!] = railgunDef.FactionRepRequired;
        var result = RefitSystem.InstallModule(state, "fleet_trader_1", 0, WellKnownModuleIds.WeaponRailgunT2);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void QueueInstall_T2_Fails_InsufficientFactionRep()
    {
        var state = CreateState();
        state.PlayerCredits = 5000;
        var railgunDef = UpgradeContentV0.GetById(WellKnownModuleIds.WeaponRailgunT2)!;
        state.Tech.UnlockedTechIds.Add(railgunDef.TechPrerequisite);
        // No faction rep set.
        var result = RefitSystem.QueueInstall(state, "fleet_trader_1", 0, WellKnownModuleIds.WeaponRailgunT2);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("faction_rep_insufficient"));
    }

    [Test]
    public void QueueInstall_T2_Succeeds_WithSufficientFactionRep()
    {
        var state = CreateState();
        state.PlayerCredits = 5000;
        var railgunDef = UpgradeContentV0.GetById(WellKnownModuleIds.WeaponRailgunT2)!;
        state.Tech.UnlockedTechIds.Add(railgunDef.TechPrerequisite);
        state.FactionReputation[railgunDef.FactionId!] = railgunDef.FactionRepRequired + 10;
        var result = RefitSystem.QueueInstall(state, "fleet_trader_1", 0, WellKnownModuleIds.WeaponRailgunT2);
        Assert.That(result.Success, Is.True);
    }
}
