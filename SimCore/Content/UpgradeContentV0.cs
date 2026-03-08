using System.Collections.Generic;
using SimCore.Entities;

namespace SimCore.Content;

// GATE.S4.UPGRADE.CORE.001: Static module/upgrade definitions.
public sealed class ModuleDef
{
    public string ModuleId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public SlotKind SlotKind { get; set; } = SlotKind.Cargo;
    public int CreditCost { get; set; } = 0;
    public string TechPrerequisite { get; set; } = "";

    // GATE.S4.UPGRADE_PIPELINE.TIMED_REFIT.001: Ticks required to install this module.
    public int InstallTicks { get; set; } = 5;

    // Stat bonuses (integer pct: 20 = +20%)
    public int SpeedBonusPct { get; set; } = 0;
    public int ShieldBonusFlat { get; set; } = 0;
    public int HullBonusFlat { get; set; } = 0;
    public int DamageBonusPct { get; set; } = 0;

    // GATE.S18.SHIP_MODULES.FITTING_BUDGET.001: Power draw of this module.
    public int PowerDraw { get; set; } = 5;

    // GATE.S18.TRADE_GOODS.SUSTAIN_ALIGN.001: Goods consumed per sustain cycle.
    public Dictionary<string, int> SustainInputs { get; set; } = new();
}

public static class UpgradeContentV0
{
    public static readonly IReadOnlyList<ModuleDef> AllModules = new List<ModuleDef>
    {
        // Starter modules (no tech prerequisite)
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.WeaponCannonMk1,
            DisplayName = "Cannon Mk1",
            SlotKind = SlotKind.Weapon,
            CreditCost = 50,
            TechPrerequisite = "",
            InstallTicks = 3,
            PowerDraw = 5,
            DamageBonusPct = 0,
            SustainInputs = new Dictionary<string, int> { [WellKnownGoodIds.Munitions] = 1 },
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.WeaponLaserMk1,
            DisplayName = "Laser Mk1",
            SlotKind = SlotKind.Weapon,
            CreditCost = 50,
            TechPrerequisite = "",
            InstallTicks = 3,
            PowerDraw = 8,
            DamageBonusPct = 0,
            SustainInputs = new Dictionary<string, int> { [WellKnownGoodIds.Munitions] = 1, [WellKnownGoodIds.Fuel] = 1 },
        },
        // Tech-gated upgrades
        new ModuleDef
        {
            ModuleId = "weapon_cannon_mk2",
            DisplayName = "Cannon Mk2",
            SlotKind = SlotKind.Weapon,
            CreditCost = 120,
            TechPrerequisite = "weapon_systems_2",
            InstallTicks = 6,
            PowerDraw = 10,
            DamageBonusPct = 25,
            SustainInputs = new Dictionary<string, int> { [WellKnownGoodIds.Munitions] = 2 },
        },
        new ModuleDef
        {
            ModuleId = "shield_mk2",
            DisplayName = "Shield Mk2",
            SlotKind = SlotKind.Utility,
            CreditCost = 100,
            TechPrerequisite = "shield_mk2",
            InstallTicks = 8, // complex shield integration
            PowerDraw = 12,
            ShieldBonusFlat = 30,
        },
        new ModuleDef
        {
            ModuleId = "engine_booster_mk1",
            DisplayName = "Engine Booster Mk1",
            SlotKind = SlotKind.Engine,
            CreditCost = 80,
            TechPrerequisite = "improved_thrusters",
            InstallTicks = 10, // engine overhaul is most complex
            PowerDraw = 8,
            SpeedBonusPct = 20,
        },
        // GATE.S4.CATALOG.MODULE_WAVE.001: 5 new modules
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.EngineMk2,
            DisplayName = "Engine Mk2",
            SlotKind = SlotKind.Engine,
            CreditCost = 150,
            TechPrerequisite = "engine_efficiency",
            InstallTicks = 12,
            PowerDraw = 15,
            SpeedBonusPct = 35,
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.CargoBayMk2,
            DisplayName = "Cargo Bay Mk2",
            SlotKind = SlotKind.Cargo,
            CreditCost = 90,
            TechPrerequisite = "cargo_expansion",
            InstallTicks = 6,
            PowerDraw = 3,
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.ScannerMk2,
            DisplayName = "Scanner Mk2",
            SlotKind = SlotKind.Utility,
            CreditCost = 110,
            TechPrerequisite = "sensor_suite",
            InstallTicks = 5,
            PowerDraw = 5,
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.HullPlatingMk2,
            DisplayName = "Hull Plating Mk2",
            SlotKind = SlotKind.Utility,
            CreditCost = 120,
            TechPrerequisite = "reinforced_hull",
            InstallTicks = 8,
            PowerDraw = 6,
            HullBonusFlat = 40,
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.LaserMk2,
            DisplayName = "Laser Mk2",
            SlotKind = SlotKind.Weapon,
            CreditCost = 140,
            TechPrerequisite = "weapon_calibration",
            InstallTicks = 7,
            PowerDraw = 15,
            DamageBonusPct = 30,
            SustainInputs = new Dictionary<string, int> { [WellKnownGoodIds.Munitions] = 2, [WellKnownGoodIds.Fuel] = 1 },
        },
    };

    private static readonly Dictionary<string, ModuleDef> _byId;

    static UpgradeContentV0()
    {
        _byId = new Dictionary<string, ModuleDef>(System.StringComparer.Ordinal);
        foreach (var m in AllModules)
            _byId[m.ModuleId] = m;
    }

    public static ModuleDef? GetById(string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId)) return null;
        return _byId.TryGetValue(moduleId, out var def) ? def : null;
    }

    public static bool CanInstall(string moduleId, HashSet<string> unlockedTechs)
    {
        var def = GetById(moduleId);
        if (def == null) return false;
        if (string.IsNullOrEmpty(def.TechPrerequisite)) return true;
        return unlockedTechs.Contains(def.TechPrerequisite);
    }
}
