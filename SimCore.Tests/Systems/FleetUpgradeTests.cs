using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimCore.Content;
using SimCore.Entities;

namespace SimCore.Tests.Systems;

// GATE.S4.UPGRADE.CORE.001: Contract tests for refit slot model and upgrade content.
[TestFixture]
[Category("FleetUpgrade")]
public sealed class FleetUpgradeTests
{
    [Test]
    public void Fleet_HasSlots()
    {
        var fleet = new Fleet { Id = "f1" };
        Assert.That(fleet.Slots, Is.Not.Null);
        Assert.That(fleet.Slots, Has.Count.EqualTo(0));
    }

    [Test]
    public void ModuleSlot_DefaultValues()
    {
        var slot = new ModuleSlot();
        Assert.That(slot.SlotId, Is.EqualTo(""));
        Assert.That(slot.SlotKind, Is.EqualTo(SlotKind.Cargo));
        Assert.That(slot.InstalledModuleId, Is.Null);
    }

    [Test]
    public void Fleet_CanAddSlots_AndInstallModules()
    {
        var fleet = new Fleet { Id = "f1" };
        fleet.Slots.Add(new ModuleSlot { SlotId = "weapon_0", SlotKind = SlotKind.Weapon });
        fleet.Slots.Add(new ModuleSlot { SlotId = "engine_0", SlotKind = SlotKind.Engine });
        fleet.Slots.Add(new ModuleSlot { SlotId = "utility_0", SlotKind = SlotKind.Utility });

        Assert.That(fleet.Slots, Has.Count.EqualTo(3));

        fleet.Slots[0].InstalledModuleId = WellKnownModuleIds.WeaponCannonMk1;
        Assert.That(fleet.Slots[0].InstalledModuleId, Is.EqualTo(WellKnownModuleIds.WeaponCannonMk1));
    }

    [Test]
    public void UpgradeContentV0_AllModules_NonEmpty()
    {
        Assert.That(UpgradeContentV0.AllModules, Has.Count.GreaterThanOrEqualTo(5));
        foreach (var mod in UpgradeContentV0.AllModules)
        {
            Assert.That(mod.ModuleId, Is.Not.Empty, "ModuleId must not be empty");
            Assert.That(mod.CreditCost, Is.GreaterThanOrEqualTo(0), $"Module {mod.ModuleId} cost must be >= 0");
        }
    }

    [Test]
    public void UpgradeContentV0_GetById_ReturnsCorrect()
    {
        var def = UpgradeContentV0.GetById(WellKnownModuleIds.WeaponCannonMk1);
        Assert.That(def, Is.Not.Null);
        Assert.That(def!.DisplayName, Is.EqualTo("Cannon Mk1"));
        Assert.That(def.SlotKind, Is.EqualTo(SlotKind.Weapon));
    }

    [Test]
    public void UpgradeContentV0_GetById_NullForUnknown()
    {
        Assert.That(UpgradeContentV0.GetById("nonexistent"), Is.Null);
        Assert.That(UpgradeContentV0.GetById(""), Is.Null);
    }

    [Test]
    public void UpgradeContentV0_CanInstall_NoTechReq()
    {
        var unlocked = new HashSet<string>();
        Assert.That(UpgradeContentV0.CanInstall(WellKnownModuleIds.WeaponCannonMk1, unlocked), Is.True);
    }

    [Test]
    public void UpgradeContentV0_CanInstall_TechGated()
    {
        var unlocked = new HashSet<string>();
        Assert.That(UpgradeContentV0.CanInstall("weapon_cannon_mk2", unlocked), Is.False);

        unlocked.Add("weapon_systems_2");
        Assert.That(UpgradeContentV0.CanInstall("weapon_cannon_mk2", unlocked), Is.True);
    }

    [Test]
    public void UpgradeContentV0_UniqueIds()
    {
        var ids = UpgradeContentV0.AllModules.Select(m => m.ModuleId).ToList();
        Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count), "Module IDs must be unique");
    }

    [Test]
    public void SlotKind_EnumValues_Exist()
    {
        Assert.That((int)SlotKind.Cargo, Is.EqualTo(0));
        Assert.That((int)SlotKind.Weapon, Is.EqualTo(1));
        Assert.That((int)SlotKind.Engine, Is.EqualTo(2));
        Assert.That((int)SlotKind.Utility, Is.EqualTo(3));
    }
}
