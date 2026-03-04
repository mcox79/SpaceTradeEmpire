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
            InstallTicks = 3, // simple starter weapon
            DamageBonusPct = 0, // base weapon, no bonus
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.WeaponLaserMk1,
            DisplayName = "Laser Mk1",
            SlotKind = SlotKind.Weapon,
            CreditCost = 50,
            TechPrerequisite = "",
            InstallTicks = 3, // simple starter weapon
            DamageBonusPct = 0,
        },
        // Tech-gated upgrades
        new ModuleDef
        {
            ModuleId = "weapon_cannon_mk2",
            DisplayName = "Cannon Mk2",
            SlotKind = SlotKind.Weapon,
            CreditCost = 120,
            TechPrerequisite = "weapon_systems_2",
            InstallTicks = 6, // advanced weapon
            DamageBonusPct = 25,
        },
        new ModuleDef
        {
            ModuleId = "shield_mk2",
            DisplayName = "Shield Mk2",
            SlotKind = SlotKind.Utility,
            CreditCost = 100,
            TechPrerequisite = "shield_mk2",
            InstallTicks = 8, // complex shield integration
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
            SpeedBonusPct = 20,
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
