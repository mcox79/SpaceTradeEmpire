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
        var fleet = new Fleet { Id = "fleet_trader_1" };
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
}
