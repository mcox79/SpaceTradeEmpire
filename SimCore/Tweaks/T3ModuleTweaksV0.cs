namespace SimCore.Tweaks;

// GATE.S8.T3_MODULES.CONTENT.001: T3 precursor module tuning constants.
// T3 modules are discovery-only (not purchasable at stations), require exotic_matter sustain.
public static class T3ModuleTweaksV0
{
    // Common T3 properties.
    public const string T3TechPrerequisite = "precursor_integration";
    public const int T3CreditCost = 0;  // Not purchasable — acquired via discovery
    public const int T3InstallTicks = 15;

    // ── Weapons ──
    public const int VoidLancePowerDraw = 25;
    public const int VoidLanceDamageBonusPct = 60;
    public const int VoidLanceSustainExotic = 1;

    public const int DisruptorPowerDraw = 20;
    public const int DisruptorDamageBonusPct = 45;
    public const int DisruptorSustainExotic = 1;

    public const int NullCannonPowerDraw = 30;
    public const int NullCannonDamageBonusPct = 75;
    public const int NullCannonSustainExotic = 2;
    public const int NullCannonSustainMunitions = 3;

    // ── Shields ──
    public const int PhaseShieldPowerDraw = 20;
    public const int PhaseShieldBonusFlat = 80;
    public const int PhaseShieldSustainExotic = 1;

    // ── Engines ──
    public const int DimensionalDrivePowerDraw = 22;
    public const int DimensionalDriveSpeedBonusPct = 50;
    public const int DimensionalDriveSustainExotic = 1;

    // ── Utility ──
    public const int PrecursorScannerPowerDraw = 15;
    public const int PrecursorScannerSustainExotic = 1;

    public const int TemporalStabilizerPowerDraw = 18;
    public const int TemporalStabilizerHullBonusFlat = 60;
    public const int TemporalStabilizerSustainExotic = 1;

    public const int VoidHarvesterPowerDraw = 12;
    public const int VoidHarvesterSustainExotic = 1;

    public const int ResonanceAmplifierPowerDraw = 16;
    public const int ResonanceAmplifierSustainExotic = 1;

    // Weaver Spindle Tractor — faction variant with auto-salvage.
    public const int WeaverSpindleCreditCost = 200;
    public const string WeaverSpindleTechPrereq = "sensor_suite";
    public const int WeaverSpindleInstallTicks = 8;
    public const int WeaverSpindlePowerDraw = 10;
    public const int WeaverSpindleTractorRange = 25;
    public const int WeaverSpindleFactionRepRequired = 50;
}
