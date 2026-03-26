namespace SimCore.Tweaks;

/// <summary>
/// GATE.S7.T2_MODULES.CATALOG.001: T2 module numeric constants.
/// All gameplay-affecting values for T2 faction-gated modules.
/// T2 modules have higher stats than Mk2 modules and require faction reputation to purchase.
/// </summary>
public static class T2ModuleTweaksV0
{
    // ── Faction rep requirement (shared across all T2 modules) ──
    public const int FactionRepRequired = 25; // Friendly tier threshold

    // ── Weapon: Railgun T2 (Kinetic, Valorin) ──
    public const int RailgunCreditCost      = 250;
    public const int RailgunInstallTicks    = 8;
    public const int RailgunPowerDraw       = 18;
    public const int RailgunDamageBonusPct  = 45;
    public const int RailgunBaseDamage      = 20;
    public const int RailgunSustainMunitions = 3;

    // ── Weapon: Plasma T2 (Energy, Communion) ──
    public const int PlasmaCreditCost       = 280;
    public const int PlasmaInstallTicks     = 9;
    public const int PlasmaPowerDraw        = 22;
    public const int PlasmaDamageBonusPct   = 40;
    public const int PlasmaBaseDamage       = 18;
    public const int PlasmaSustainMunitions = 2;
    public const int PlasmaSustainFuel      = 2;

    // ── Shield: Matrix T2 (Concord) ──
    public const int ShieldMatrixCreditCost     = 220;
    public const int ShieldMatrixInstallTicks   = 10;
    public const int ShieldMatrixPowerDraw      = 16;
    public const int ShieldMatrixShieldBonusFlat = 60;

    // ── Engine: Fusion T2 (Weavers) ──
    public const int FusionEngineCreditCost     = 260;
    public const int FusionEngineInstallTicks   = 14;
    public const int FusionEnginePowerDraw      = 20;
    public const int FusionEngineSpeedBonusPct  = 50;

    // ── Scanner: Deep T2 (Chitin) ──
    public const int DeepScannerCreditCost      = 200;
    public const int DeepScannerInstallTicks    = 7;
    public const int DeepScannerPowerDraw       = 8;

    // ── Point Defense T2 (Concord) ──
    public const int PointDefenseCreditCost     = 240;
    public const int PointDefenseInstallTicks   = 8;
    public const int PointDefensePowerDraw      = 14;
    public const int PointDefenseDamageBonusPct = 30;
    public const int PointDefenseBaseDamage     = 14;
    public const int PointDefenseSustainMunitions = 2;

    // ── Hull: Nanite T2 (Weavers) ──
    public const int HullNaniteCreditCost       = 230;
    public const int HullNaniteInstallTicks     = 10;
    public const int HullNanitePowerDraw        = 10;
    public const int HullNaniteHullBonusFlat    = 70;

    // ── Tech prerequisites (string IDs, not numeric) ──
    public const string RailgunTechPrereq       = "advanced_kinetics";
    public const string PlasmaTechPrereq        = "plasma_containment";
    public const string ShieldMatrixTechPrereq  = "matrix_shielding";
    public const string FusionEngineTechPrereq  = "fusion_propulsion";
    public const string DeepScannerTechPrereq   = "deep_scan_array";
    public const string PointDefenseTechPrereq  = "point_defense_grid";
    public const string HullNaniteTechPrereq    = "nanite_reinforcement";

    // ════════════════════════════════════════════════════════════════════════
    // GATE.S7.T2_MODULES.EXPANSION.001: 19 additional T2 modules
    // ════════════════════════════════════════════════════════════════════════

    // ── Per-faction rep requirements (varied 25-45) ──
    public const int ConcordRepRequired   = 30;
    public const int ChitinRepRequired    = 35;
    public const int WeaversRepRequired   = 30;
    public const int ValorinRepRequired   = 40;
    public const int CommunionRepRequired = 35;

    // ── Weapon: Autocannon T2 (Kinetic, rapid fire — Valorin) ──
    public const int AutocannonCreditCost       = 230;
    public const int AutocannonInstallTicks     = 7;
    public const int AutocannonPowerDraw        = 16;
    public const int AutocannonDamageBonusPct   = 35;
    public const int AutocannonBaseDamage       = 16;
    public const int AutocannonSustainMunitions = 3;
    public const string AutocannonTechPrereq    = "rapid_fire_systems";

    // ── Weapon: Plasma Cannon T2 (Energy, high damage — Communion) ──
    public const int PlasmaCannonCreditCost       = 300;
    public const int PlasmaCannonInstallTicks     = 10;
    public const int PlasmaCannonPowerDraw        = 24;
    public const int PlasmaCannonDamageBonusPct   = 50;
    public const int PlasmaCannonBaseDamage       = 22;
    public const int PlasmaCannonSustainMunitions = 2;
    public const int PlasmaCannonSustainFuel      = 3;
    public const string PlasmaCannonTechPrereq    = "plasma_cannon_array";

    // ── Weapon: Missile Launcher T2 (Guided — Communion) ──
    public const int MissileLauncherCreditCost       = 270;
    public const int MissileLauncherInstallTicks     = 9;
    public const int MissileLauncherPowerDraw        = 15;
    public const int MissileLauncherDamageBonusPct   = 40;
    public const int MissileLauncherBaseDamage       = 17;
    public const int MissileLauncherSustainMunitions = 4;
    public const string MissileLauncherTechPrereq    = "guided_ordnance";

    // ── Weapon: Point Defense T2 (PD, anti-missile — Chitin) ──
    public const int WeaponPointDefenseCreditCost       = 220;
    public const int WeaponPointDefenseInstallTicks     = 7;
    public const int WeaponPointDefensePowerDraw        = 12;
    public const int WeaponPointDefenseDamageBonusPct   = 25;
    public const int WeaponPointDefenseBaseDamage       = 12;
    public const int WeaponPointDefenseSustainMunitions = 2;
    public const string WeaponPointDefenseTechPrereq    = "pd_interception";

    // ── Weapon: Gauss Cannon T2 (Kinetic, long range — Weavers) ──
    public const int GaussCannonCreditCost       = 290;
    public const int GaussCannonInstallTicks     = 10;
    public const int GaussCannonPowerDraw        = 20;
    public const int GaussCannonDamageBonusPct   = 45;
    public const int GaussCannonBaseDamage       = 21;
    public const int GaussCannonSustainMunitions = 3;
    public const string GaussCannonTechPrereq    = "gauss_acceleration";

    // ── Shield: Deflector T2 (balanced regen — Concord) ──
    public const int ShieldDeflectorCreditCost       = 210;
    public const int ShieldDeflectorInstallTicks     = 9;
    public const int ShieldDeflectorPowerDraw        = 14;
    public const int ShieldDeflectorShieldBonusFlat  = 50;
    public const string ShieldDeflectorTechPrereq    = "deflector_array";

    // ── Shield: Hardened T2 (high capacity, slow regen — Valorin) ──
    public const int ShieldHardenedCreditCost       = 260;
    public const int ShieldHardenedInstallTicks     = 12;
    public const int ShieldHardenedPowerDraw        = 18;
    public const int ShieldHardenedShieldBonusFlat  = 80;
    public const string ShieldHardenedTechPrereq    = "hardened_barriers";

    // ── Shield: Adaptive T2 (resistance matching — Chitin) ──
    public const int ShieldAdaptiveCreditCost       = 240;
    public const int ShieldAdaptiveInstallTicks     = 11;
    public const int ShieldAdaptivePowerDraw        = 16;
    public const int ShieldAdaptiveShieldBonusFlat  = 55;
    public const string ShieldAdaptiveTechPrereq    = "adaptive_shielding";

    // ── Engine: Ion T2 (efficient, moderate speed — Weavers) ──
    public const int EngineIonCreditCost       = 220;
    public const int EngineIonInstallTicks     = 12;
    public const int EngineIonPowerDraw        = 14;
    public const int EngineIonSpeedBonusPct    = 40;
    public const string EngineIonTechPrereq    = "ion_propulsion";

    // ── Engine: Plasma T2 (high thrust — Valorin) ──
    public const int EnginePlasmaCreditCost       = 280;
    public const int EnginePlasmaInstallTicks     = 13;
    public const int EnginePlasmaPowerDraw        = 22;
    public const int EnginePlasmaSpeedBonusPct    = 55;
    public const string EngineplasmaTechPrereq    = "plasma_thrust";

    // ── Engine: Warp T2 (fast lane transit — Communion) ──
    public const int EngineWarpCreditCost       = 300;
    public const int EngineWarpInstallTicks     = 15;
    public const int EngineWarpPowerDraw        = 25;
    public const int EngineWarpSpeedBonusPct    = 60;
    public const string EngineWarpTechPrereq    = "warp_field_dynamics";

    // ── Utility: Cargo Expander T2 (extra cargo space — Concord) ──
    public const int CargoExpanderCreditCost    = 180;
    public const int CargoExpanderInstallTicks  = 6;
    public const int CargoExpanderPowerDraw     = 5;
    public const string CargoExpanderTechPrereq = "cargo_optimization";

    // ── Utility: Scanner Array T2 (discovery range — Valorin) ──
    public const int ScannerArrayCreditCost    = 210;
    public const int ScannerArrayInstallTicks  = 8;
    public const int ScannerArrayPowerDraw     = 10;
    public const string ScannerArrayTechPrereq = "wide_spectrum_scan";

    // ── Utility: Repair Drone T2 (hull regen — Weavers) ──
    public const int RepairDroneCreditCost       = 250;
    public const int RepairDroneInstallTicks     = 9;
    public const int RepairDronePowerDraw        = 12;
    public const int RepairDroneHullBonusFlat    = 45;
    public const string RepairDroneTechPrereq    = "drone_repair_bay";

    // ── Utility: ECM T2 (electronic countermeasures — Chitin) ──
    public const int EcmCreditCost    = 230;
    public const int EcmInstallTicks  = 8;
    public const int EcmPowerDraw     = 11;
    public const string EcmTechPrereq = "electronic_warfare";

    // ── Defense: Hull Plating T2 (hull HP bonus — Concord) ──
    public const int DefenseHullPlatingCreditCost    = 200;
    public const int DefenseHullPlatingInstallTicks  = 9;
    public const int DefenseHullPlatingPowerDraw     = 8;
    public const int DefenseHullPlatingHullBonusFlat = 65;
    public const string DefenseHullPlatingTechPrereq = "reinforced_plating";

    // ── Defense: Point Barrier T2 (directional shield — Weavers) ──
    public const int DefensePointBarrierCreditCost       = 240;
    public const int DefensePointBarrierInstallTicks     = 10;
    public const int DefensePointBarrierPowerDraw        = 14;
    public const int DefensePointBarrierShieldBonusFlat  = 45;
    public const string DefensePointBarrierTechPrereq    = "barrier_projection";

    // ── Defense: Damage Control T2 (damage reduction — Concord) ──
    public const int DefenseDamageControlCreditCost       = 220;
    public const int DefenseDamageControlInstallTicks     = 8;
    public const int DefenseDamageControlPowerDraw        = 10;
    public const int DefenseDamageControlHullBonusFlat    = 50;
    public const string DefenseDamageControlTechPrereq    = "damage_control_grid";

    // ── Defense: Armor Weave T2 (resistance bonus — Chitin) ──
    public const int DefenseArmorWeaveCreditCost       = 230;
    public const int DefenseArmorWeaveInstallTicks     = 10;
    public const int DefenseArmorWeavePowerDraw        = 9;
    public const int DefenseArmorWeaveHullBonusFlat    = 55;
    public const string DefenseArmorWeaveTechPrereq    = "bio_armor_weave";

    // GATE.T59.SHIP.T2_MODULE_REASSIGN.001: New Communion scanner/nav modules
    // ── Communion: Shimmer Drive T2 (engine with scan bonus) ──
    public const int ShimmerDriveCreditCost      = 260;
    public const int ShimmerDriveInstallTicks    = 10;
    public const int ShimmerDrivePowerDraw       = 14;
    public const int ShimmerDriveSpeedBonusPct   = 15;
    public const int ShimmerDriveScanBonusPct    = 20;
    public const string ShimmerDriveTechPrereq   = "shimmer_propulsion";

    // ── Communion: Resonance Comm T2 (utility — scan range + fracture resist) ──
    public const int ResonanceCommCreditCost     = 240;
    public const int ResonanceCommInstallTicks   = 8;
    public const int ResonanceCommPowerDraw      = 10;
    public const int ResonanceCommScanBonusPct   = 30;
    public const string ResonanceCommTechPrereq  = "resonance_theory";

    // ── Communion: Phase-Lock Cradle T2 (utility — fracture resistance) ──
    public const int PhaseLockCreditCost         = 280;
    public const int PhaseLockInstallTicks       = 12;
    public const int PhaseLockPowerDraw          = 16;
    public const int PhaseLockScanBonusPct       = 15;
    public const int PhaseLockFractureResistBps  = 3000;
    public const string PhaseLockTechPrereq      = "phase_lock_theory";

    // GATE.T59.SHIP.T2_MODULE_REASSIGN.001: New Weavers structural utility
    // ── Weavers: Load-Bearing Strut T2 (defense — hull + armor) ──
    public const int LoadBearingCreditCost       = 250;
    public const int LoadBearingInstallTicks     = 10;
    public const int LoadBearingPowerDraw        = 8;
    public const int LoadBearingHullBonusFlat    = 40;
    public const int LoadBearingArmorBonusPct    = 15;
    public const string LoadBearingTechPrereq    = "structural_engineering";
}
