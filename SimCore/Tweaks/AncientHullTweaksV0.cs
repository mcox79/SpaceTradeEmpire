namespace SimCore.Tweaks;

// GATE.S8.ANCIENT_HULLS.CONTENT.001: Ancient ship hull stat constants.
public static class AncientHullTweaksV0
{
    // ── Bastion (tank) — heavy armor, high hull, low speed ──
    public const int BastionSlotCount = 8;
    public const int BastionBasePower = 90;
    public const int BastionCargoCapacity = 40;
    public const int BastionMass = 110;
    public const int BastionScanRange = 60;
    public const int BastionArmorFore = 60;
    public const int BastionArmorPort = 50;
    public const int BastionArmorStbd = 50;
    public const int BastionArmorAft = 40;
    public const int BastionCoreHull = 180;
    public const int BastionBaseShield = 70;
    public const int BastionBaseFuelCapacity = 400;

    // ── Seeker (explorer) — high scan, good speed, moderate combat ──
    public const int SeekerSlotCount = 7;
    public const int SeekerBasePower = 70;
    public const int SeekerCargoCapacity = 55;
    public const int SeekerMass = 50;
    public const int SeekerScanRange = 150;
    public const int SeekerArmorFore = 25;
    public const int SeekerArmorPort = 20;
    public const int SeekerArmorStbd = 20;
    public const int SeekerArmorAft = 25;
    public const int SeekerCoreHull = 70;
    public const int SeekerBaseShield = 40;
    public const int SeekerBaseFuelCapacity = 1200;

    // ── Threshold (fracture specialist) — balanced, fracture bonuses implicit ──
    public const int ThresholdSlotCount = 9;
    public const int ThresholdBasePower = 85;
    public const int ThresholdCargoCapacity = 45;
    public const int ThresholdMass = 75;
    public const int ThresholdScanRange = 100;
    public const int ThresholdArmorFore = 35;
    public const int ThresholdArmorPort = 35;
    public const int ThresholdArmorStbd = 35;
    public const int ThresholdArmorAft = 30;
    public const int ThresholdCoreHull = 120;
    public const int ThresholdBaseShield = 60;
    public const int ThresholdBaseFuelCapacity = 800;

    // Hull restoration at Haven (Tier 3+).
    public const int RestoreMinHavenTier = 3;
}
