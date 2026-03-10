using System.Collections.Generic;
using SimCore.Entities;
using SimCore.Tweaks;

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

    // GATE.S7.T2_MODULES.CATALOG.001: Faction reputation gating for T2 modules.
    // Null/empty = no faction rep requirement. Otherwise, player must have >= FactionRepRequired
    // reputation with the faction identified by FactionId to purchase/install this module.
    public string? FactionId { get; set; } = null;
    public int FactionRepRequired { get; set; } = 0;

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
        // GATE.S7.T2_MODULES.CATALOG.001: 7 T2 faction-gated modules
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.WeaponRailgunT2,
            DisplayName = "Railgun T2",
            SlotKind = SlotKind.Weapon,
            CreditCost = T2ModuleTweaksV0.RailgunCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.RailgunTechPrereq,
            InstallTicks = T2ModuleTweaksV0.RailgunInstallTicks,
            PowerDraw = T2ModuleTweaksV0.RailgunPowerDraw,
            DamageBonusPct = T2ModuleTweaksV0.RailgunDamageBonusPct,
            FactionId = FactionTweaksV0.ValorinId,
            FactionRepRequired = T2ModuleTweaksV0.FactionRepRequired,
            SustainInputs = new Dictionary<string, int> { [WellKnownGoodIds.Munitions] = T2ModuleTweaksV0.RailgunSustainMunitions },
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.WeaponPlasmaT2,
            DisplayName = "Plasma T2",
            SlotKind = SlotKind.Weapon,
            CreditCost = T2ModuleTweaksV0.PlasmaCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.PlasmaTechPrereq,
            InstallTicks = T2ModuleTweaksV0.PlasmaInstallTicks,
            PowerDraw = T2ModuleTweaksV0.PlasmaPowerDraw,
            DamageBonusPct = T2ModuleTweaksV0.PlasmaDamageBonusPct,
            FactionId = FactionTweaksV0.CommunionId,
            FactionRepRequired = T2ModuleTweaksV0.FactionRepRequired,
            SustainInputs = new Dictionary<string, int> { [WellKnownGoodIds.Munitions] = T2ModuleTweaksV0.PlasmaSustainMunitions, [WellKnownGoodIds.Fuel] = T2ModuleTweaksV0.PlasmaSustainFuel },
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.ShieldMatrixT2,
            DisplayName = "Shield Matrix T2",
            SlotKind = SlotKind.Utility,
            CreditCost = T2ModuleTweaksV0.ShieldMatrixCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.ShieldMatrixTechPrereq,
            InstallTicks = T2ModuleTweaksV0.ShieldMatrixInstallTicks,
            PowerDraw = T2ModuleTweaksV0.ShieldMatrixPowerDraw,
            ShieldBonusFlat = T2ModuleTweaksV0.ShieldMatrixShieldBonusFlat,
            FactionId = FactionTweaksV0.ConcordId,
            FactionRepRequired = T2ModuleTweaksV0.FactionRepRequired,
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.EngineFusionT2,
            DisplayName = "Fusion Engine T2",
            SlotKind = SlotKind.Engine,
            CreditCost = T2ModuleTweaksV0.FusionEngineCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.FusionEngineTechPrereq,
            InstallTicks = T2ModuleTweaksV0.FusionEngineInstallTicks,
            PowerDraw = T2ModuleTweaksV0.FusionEnginePowerDraw,
            SpeedBonusPct = T2ModuleTweaksV0.FusionEngineSpeedBonusPct,
            FactionId = FactionTweaksV0.WeaversId,
            FactionRepRequired = T2ModuleTweaksV0.FactionRepRequired,
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.ScannerDeepT2,
            DisplayName = "Deep Scanner T2",
            SlotKind = SlotKind.Utility,
            CreditCost = T2ModuleTweaksV0.DeepScannerCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.DeepScannerTechPrereq,
            InstallTicks = T2ModuleTweaksV0.DeepScannerInstallTicks,
            PowerDraw = T2ModuleTweaksV0.DeepScannerPowerDraw,
            FactionId = FactionTweaksV0.ChitinId,
            FactionRepRequired = T2ModuleTweaksV0.FactionRepRequired,
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.PointDefenseT2,
            DisplayName = "Point Defense T2",
            SlotKind = SlotKind.Weapon,
            CreditCost = T2ModuleTweaksV0.PointDefenseCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.PointDefenseTechPrereq,
            InstallTicks = T2ModuleTweaksV0.PointDefenseInstallTicks,
            PowerDraw = T2ModuleTweaksV0.PointDefensePowerDraw,
            DamageBonusPct = T2ModuleTweaksV0.PointDefenseDamageBonusPct,
            FactionId = FactionTweaksV0.ConcordId,
            FactionRepRequired = T2ModuleTweaksV0.FactionRepRequired,
            SustainInputs = new Dictionary<string, int> { [WellKnownGoodIds.Munitions] = T2ModuleTweaksV0.PointDefenseSustainMunitions },
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.HullNaniteT2,
            DisplayName = "Nanite Hull T2",
            SlotKind = SlotKind.Utility,
            CreditCost = T2ModuleTweaksV0.HullNaniteCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.HullNaniteTechPrereq,
            InstallTicks = T2ModuleTweaksV0.HullNaniteInstallTicks,
            PowerDraw = T2ModuleTweaksV0.HullNanitePowerDraw,
            HullBonusFlat = T2ModuleTweaksV0.HullNaniteHullBonusFlat,
            FactionId = FactionTweaksV0.WeaversId,
            FactionRepRequired = T2ModuleTweaksV0.FactionRepRequired,
        },
        // ════════════════════════════════════════════════════════════════════════
        // GATE.S7.T2_MODULES.EXPANSION.001: 19 additional T2 faction-gated modules
        // ════════════════════════════════════════════════════════════════════════

        // ── Weapons ──
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.WeaponAutocannonT2,
            DisplayName = "Autocannon T2",
            SlotKind = SlotKind.Weapon,
            CreditCost = T2ModuleTweaksV0.AutocannonCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.AutocannonTechPrereq,
            InstallTicks = T2ModuleTweaksV0.AutocannonInstallTicks,
            PowerDraw = T2ModuleTweaksV0.AutocannonPowerDraw,
            DamageBonusPct = T2ModuleTweaksV0.AutocannonDamageBonusPct,
            FactionId = FactionTweaksV0.ValorinId,
            FactionRepRequired = T2ModuleTweaksV0.ValorinRepRequired,
            SustainInputs = new Dictionary<string, int> { [WellKnownGoodIds.Munitions] = T2ModuleTweaksV0.AutocannonSustainMunitions },
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.WeaponPlasmaCannonT2,
            DisplayName = "Plasma Cannon T2",
            SlotKind = SlotKind.Weapon,
            CreditCost = T2ModuleTweaksV0.PlasmaCannonCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.PlasmaCannonTechPrereq,
            InstallTicks = T2ModuleTweaksV0.PlasmaCannonInstallTicks,
            PowerDraw = T2ModuleTweaksV0.PlasmaCannonPowerDraw,
            DamageBonusPct = T2ModuleTweaksV0.PlasmaCannonDamageBonusPct,
            FactionId = FactionTweaksV0.CommunionId,
            FactionRepRequired = T2ModuleTweaksV0.CommunionRepRequired,
            SustainInputs = new Dictionary<string, int> { [WellKnownGoodIds.Munitions] = T2ModuleTweaksV0.PlasmaCannonSustainMunitions, [WellKnownGoodIds.Fuel] = T2ModuleTweaksV0.PlasmaCannonSustainFuel },
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.WeaponMissileLauncherT2,
            DisplayName = "Missile Launcher T2",
            SlotKind = SlotKind.Weapon,
            CreditCost = T2ModuleTweaksV0.MissileLauncherCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.MissileLauncherTechPrereq,
            InstallTicks = T2ModuleTweaksV0.MissileLauncherInstallTicks,
            PowerDraw = T2ModuleTweaksV0.MissileLauncherPowerDraw,
            DamageBonusPct = T2ModuleTweaksV0.MissileLauncherDamageBonusPct,
            FactionId = FactionTweaksV0.CommunionId,
            FactionRepRequired = T2ModuleTweaksV0.CommunionRepRequired,
            SustainInputs = new Dictionary<string, int> { [WellKnownGoodIds.Munitions] = T2ModuleTweaksV0.MissileLauncherSustainMunitions },
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.WeaponPointDefenseT2,
            DisplayName = "Point Defense Array T2",
            SlotKind = SlotKind.Weapon,
            CreditCost = T2ModuleTweaksV0.WeaponPointDefenseCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.WeaponPointDefenseTechPrereq,
            InstallTicks = T2ModuleTweaksV0.WeaponPointDefenseInstallTicks,
            PowerDraw = T2ModuleTweaksV0.WeaponPointDefensePowerDraw,
            DamageBonusPct = T2ModuleTweaksV0.WeaponPointDefenseDamageBonusPct,
            FactionId = FactionTweaksV0.ChitinId,
            FactionRepRequired = T2ModuleTweaksV0.ChitinRepRequired,
            SustainInputs = new Dictionary<string, int> { [WellKnownGoodIds.Munitions] = T2ModuleTweaksV0.WeaponPointDefenseSustainMunitions },
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.WeaponGaussCannonT2,
            DisplayName = "Gauss Cannon T2",
            SlotKind = SlotKind.Weapon,
            CreditCost = T2ModuleTweaksV0.GaussCannonCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.GaussCannonTechPrereq,
            InstallTicks = T2ModuleTweaksV0.GaussCannonInstallTicks,
            PowerDraw = T2ModuleTweaksV0.GaussCannonPowerDraw,
            DamageBonusPct = T2ModuleTweaksV0.GaussCannonDamageBonusPct,
            FactionId = FactionTweaksV0.WeaversId,
            FactionRepRequired = T2ModuleTweaksV0.WeaversRepRequired,
            SustainInputs = new Dictionary<string, int> { [WellKnownGoodIds.Munitions] = T2ModuleTweaksV0.GaussCannonSustainMunitions },
        },

        // ── Shields ──
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.ShieldDeflectorT2,
            DisplayName = "Deflector Shield T2",
            SlotKind = SlotKind.Utility,
            CreditCost = T2ModuleTweaksV0.ShieldDeflectorCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.ShieldDeflectorTechPrereq,
            InstallTicks = T2ModuleTweaksV0.ShieldDeflectorInstallTicks,
            PowerDraw = T2ModuleTweaksV0.ShieldDeflectorPowerDraw,
            ShieldBonusFlat = T2ModuleTweaksV0.ShieldDeflectorShieldBonusFlat,
            FactionId = FactionTweaksV0.ConcordId,
            FactionRepRequired = T2ModuleTweaksV0.ConcordRepRequired,
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.ShieldHardenedT2,
            DisplayName = "Hardened Shield T2",
            SlotKind = SlotKind.Utility,
            CreditCost = T2ModuleTweaksV0.ShieldHardenedCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.ShieldHardenedTechPrereq,
            InstallTicks = T2ModuleTweaksV0.ShieldHardenedInstallTicks,
            PowerDraw = T2ModuleTweaksV0.ShieldHardenedPowerDraw,
            ShieldBonusFlat = T2ModuleTweaksV0.ShieldHardenedShieldBonusFlat,
            FactionId = FactionTweaksV0.ValorinId,
            FactionRepRequired = T2ModuleTweaksV0.ValorinRepRequired,
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.ShieldAdaptiveT2,
            DisplayName = "Adaptive Shield T2",
            SlotKind = SlotKind.Utility,
            CreditCost = T2ModuleTweaksV0.ShieldAdaptiveCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.ShieldAdaptiveTechPrereq,
            InstallTicks = T2ModuleTweaksV0.ShieldAdaptiveInstallTicks,
            PowerDraw = T2ModuleTweaksV0.ShieldAdaptivePowerDraw,
            ShieldBonusFlat = T2ModuleTweaksV0.ShieldAdaptiveShieldBonusFlat,
            FactionId = FactionTweaksV0.ChitinId,
            FactionRepRequired = T2ModuleTweaksV0.ChitinRepRequired,
        },

        // ── Engines ──
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.EngineIonT2,
            DisplayName = "Ion Engine T2",
            SlotKind = SlotKind.Engine,
            CreditCost = T2ModuleTweaksV0.EngineIonCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.EngineIonTechPrereq,
            InstallTicks = T2ModuleTweaksV0.EngineIonInstallTicks,
            PowerDraw = T2ModuleTweaksV0.EngineIonPowerDraw,
            SpeedBonusPct = T2ModuleTweaksV0.EngineIonSpeedBonusPct,
            FactionId = FactionTweaksV0.WeaversId,
            FactionRepRequired = T2ModuleTweaksV0.WeaversRepRequired,
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.EnginePlasmaT2,
            DisplayName = "Plasma Engine T2",
            SlotKind = SlotKind.Engine,
            CreditCost = T2ModuleTweaksV0.EnginePlasmaCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.EngineplasmaTechPrereq,
            InstallTicks = T2ModuleTweaksV0.EnginePlasmaInstallTicks,
            PowerDraw = T2ModuleTweaksV0.EnginePlasmaPowerDraw,
            SpeedBonusPct = T2ModuleTweaksV0.EnginePlasmaSpeedBonusPct,
            FactionId = FactionTweaksV0.ValorinId,
            FactionRepRequired = T2ModuleTweaksV0.ValorinRepRequired,
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.EngineWarpT2,
            DisplayName = "Warp Engine T2",
            SlotKind = SlotKind.Engine,
            CreditCost = T2ModuleTweaksV0.EngineWarpCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.EngineWarpTechPrereq,
            InstallTicks = T2ModuleTweaksV0.EngineWarpInstallTicks,
            PowerDraw = T2ModuleTweaksV0.EngineWarpPowerDraw,
            SpeedBonusPct = T2ModuleTweaksV0.EngineWarpSpeedBonusPct,
            FactionId = FactionTweaksV0.CommunionId,
            FactionRepRequired = T2ModuleTweaksV0.CommunionRepRequired,
        },

        // ── Utility ──
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.UtilityCargoExpanderT2,
            DisplayName = "Cargo Expander T2",
            SlotKind = SlotKind.Cargo,
            CreditCost = T2ModuleTweaksV0.CargoExpanderCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.CargoExpanderTechPrereq,
            InstallTicks = T2ModuleTweaksV0.CargoExpanderInstallTicks,
            PowerDraw = T2ModuleTweaksV0.CargoExpanderPowerDraw,
            FactionId = FactionTweaksV0.ConcordId,
            FactionRepRequired = T2ModuleTweaksV0.ConcordRepRequired,
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.UtilityScannerArrayT2,
            DisplayName = "Scanner Array T2",
            SlotKind = SlotKind.Utility,
            CreditCost = T2ModuleTweaksV0.ScannerArrayCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.ScannerArrayTechPrereq,
            InstallTicks = T2ModuleTweaksV0.ScannerArrayInstallTicks,
            PowerDraw = T2ModuleTweaksV0.ScannerArrayPowerDraw,
            FactionId = FactionTweaksV0.ValorinId,
            FactionRepRequired = T2ModuleTweaksV0.ValorinRepRequired,
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.UtilityRepairDroneT2,
            DisplayName = "Repair Drone T2",
            SlotKind = SlotKind.Utility,
            CreditCost = T2ModuleTweaksV0.RepairDroneCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.RepairDroneTechPrereq,
            InstallTicks = T2ModuleTweaksV0.RepairDroneInstallTicks,
            PowerDraw = T2ModuleTweaksV0.RepairDronePowerDraw,
            HullBonusFlat = T2ModuleTweaksV0.RepairDroneHullBonusFlat,
            FactionId = FactionTweaksV0.WeaversId,
            FactionRepRequired = T2ModuleTweaksV0.WeaversRepRequired,
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.UtilityEcmT2,
            DisplayName = "ECM Suite T2",
            SlotKind = SlotKind.Utility,
            CreditCost = T2ModuleTweaksV0.EcmCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.EcmTechPrereq,
            InstallTicks = T2ModuleTweaksV0.EcmInstallTicks,
            PowerDraw = T2ModuleTweaksV0.EcmPowerDraw,
            FactionId = FactionTweaksV0.ChitinId,
            FactionRepRequired = T2ModuleTweaksV0.ChitinRepRequired,
        },

        // ── Defense ──
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.DefenseHullPlatingT2,
            DisplayName = "Hull Plating T2",
            SlotKind = SlotKind.Utility,
            CreditCost = T2ModuleTweaksV0.DefenseHullPlatingCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.DefenseHullPlatingTechPrereq,
            InstallTicks = T2ModuleTweaksV0.DefenseHullPlatingInstallTicks,
            PowerDraw = T2ModuleTweaksV0.DefenseHullPlatingPowerDraw,
            HullBonusFlat = T2ModuleTweaksV0.DefenseHullPlatingHullBonusFlat,
            FactionId = FactionTweaksV0.ConcordId,
            FactionRepRequired = T2ModuleTweaksV0.ConcordRepRequired,
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.DefensePointBarrierT2,
            DisplayName = "Point Barrier T2",
            SlotKind = SlotKind.Utility,
            CreditCost = T2ModuleTweaksV0.DefensePointBarrierCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.DefensePointBarrierTechPrereq,
            InstallTicks = T2ModuleTweaksV0.DefensePointBarrierInstallTicks,
            PowerDraw = T2ModuleTweaksV0.DefensePointBarrierPowerDraw,
            ShieldBonusFlat = T2ModuleTweaksV0.DefensePointBarrierShieldBonusFlat,
            FactionId = FactionTweaksV0.WeaversId,
            FactionRepRequired = T2ModuleTweaksV0.WeaversRepRequired,
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.DefenseDamageControlT2,
            DisplayName = "Damage Control T2",
            SlotKind = SlotKind.Utility,
            CreditCost = T2ModuleTweaksV0.DefenseDamageControlCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.DefenseDamageControlTechPrereq,
            InstallTicks = T2ModuleTweaksV0.DefenseDamageControlInstallTicks,
            PowerDraw = T2ModuleTweaksV0.DefenseDamageControlPowerDraw,
            HullBonusFlat = T2ModuleTweaksV0.DefenseDamageControlHullBonusFlat,
            FactionId = FactionTweaksV0.ConcordId,
            FactionRepRequired = T2ModuleTweaksV0.ConcordRepRequired,
        },
        new ModuleDef
        {
            ModuleId = WellKnownModuleIds.DefenseArmorWeaveT2,
            DisplayName = "Armor Weave T2",
            SlotKind = SlotKind.Utility,
            CreditCost = T2ModuleTweaksV0.DefenseArmorWeaveCreditCost,
            TechPrerequisite = T2ModuleTweaksV0.DefenseArmorWeaveTechPrereq,
            InstallTicks = T2ModuleTweaksV0.DefenseArmorWeaveInstallTicks,
            PowerDraw = T2ModuleTweaksV0.DefenseArmorWeavePowerDraw,
            HullBonusFlat = T2ModuleTweaksV0.DefenseArmorWeaveHullBonusFlat,
            FactionId = FactionTweaksV0.ChitinId,
            FactionRepRequired = T2ModuleTweaksV0.ChitinRepRequired,
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

    // GATE.S7.T2_MODULES.CATALOG.001: Faction-rep-aware install check.
    public static bool CanInstall(string moduleId, HashSet<string> unlockedTechs, Dictionary<string, int> factionRep)
    {
        var def = GetById(moduleId);
        if (def == null) return false;
        if (!string.IsNullOrEmpty(def.TechPrerequisite) && !unlockedTechs.Contains(def.TechPrerequisite))
            return false;
        if (!string.IsNullOrEmpty(def.FactionId) && def.FactionRepRequired > 0)
        {
            int rep = 0;
            factionRep?.TryGetValue(def.FactionId, out rep);
            if (rep < def.FactionRepRequired) return false;
        }
        return true;
    }
}
