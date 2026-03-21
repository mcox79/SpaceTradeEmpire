using SimCore.Entities;

namespace SimCore.Tweaks;

// GATE.T42.PLANET_SCAN.TWEAKS.001: Planet scanning tuning constants.
public static class PlanetScanTweaksV0
{
    // ── Scanner charge budget ──
    public static int BasicCharges { get; } = 2;
    public static int Mk1Charges { get; } = 3;
    public static int Mk2Charges { get; } = 4;
    public static int Mk3Charges { get; } = 5;

    public static int BasicRechargeTicks { get; } = 30;
    public static int Mk1RechargeTicks { get; } = 25;
    public static int Mk2RechargeTicks { get; } = 20;
    public static int Mk3RechargeTicks { get; } = 15;

    // Atmospheric sampling costs 1 charge + this many fuel.
    public static int AtmosphericSampleFuelCost { get; } = 1;
    // Landing scan costs 1 charge + this many fuel.
    public static int LandingScanFuelCost { get; } = 1;

    // ── Investigation ──
    public static int InvestigationMinTicks { get; } = 5;
    public static int InvestigationMaxTicks { get; } = 15;
    public static int InvestigationBonusKgConnections { get; } = 2;

    // ── Mode × Planet Type Affinity Matrix (bps, 10000 = 1.0×) ──
    // Row = ScanMode (0=MineralSurvey, 1=SignalSweep, 2=Archaeological)
    // Col = PlanetType (0=Terrestrial, 1=Ice, 2=Sand, 3=Lava, 4=Gaseous, 5=Barren)
    public static readonly int[,] AffinityMatrixBps = new int[3, 6]
    {
        // MineralSurvey:  Terr   Ice    Sand   Lava   Gas    Barren
        {                  8000, 10000, 15000, 12000,  7000, 13000 },
        // SignalSweep:    Terr   Ice    Sand   Lava   Gas    Barren
        {                  7000,  8000,  6000, 14000, 15000,  5000 },
        // Archaeological: Terr   Ice    Sand   Lava   Gas    Barren
        {                 13000, 10000, 11000,  8000,  3000, 15000 },
    };

    /// <summary>
    /// Look up affinity in basis points for a given scan mode and planet type.
    /// Returns 10000 (1.0×) if inputs are out of range.
    /// </summary>
    public static int GetAffinityBps(ScanMode mode, PlanetType planetType)
    {
        int row = (int)mode;
        int col = (int)planetType;
        if (row < 0 || row >= 3 || col < 0 || col >= 6) return 10000; // STRUCTURAL: default 1.0×
        return AffinityMatrixBps[row, col];
    }

    /// <summary>
    /// Max charges for a given scanner tier (0=Basic, 1=Mk1, 2=Mk2, 3+=Mk3).
    /// </summary>
    public static int GetMaxCharges(int scannerTier)
    {
        return scannerTier switch
        {
            0 => BasicCharges,    // STRUCTURAL: tier index
            1 => Mk1Charges,     // STRUCTURAL: tier index
            2 => Mk2Charges,     // STRUCTURAL: tier index
            _ => Mk3Charges,
        };
    }

    /// <summary>
    /// Recharge rate in ticks per charge for a given scanner tier.
    /// </summary>
    public static int GetRechargeTicks(int scannerTier)
    {
        return scannerTier switch
        {
            0 => BasicRechargeTicks,    // STRUCTURAL: tier index
            1 => Mk1RechargeTicks,     // STRUCTURAL: tier index
            2 => Mk2RechargeTicks,     // STRUCTURAL: tier index
            _ => Mk3RechargeTicks,
        };
    }

    // ── Finding category probability weights by affinity band ──
    // High affinity (>= 12000 bps): mode-primary category strongly favored.
    // Mid affinity (8000-11999 bps): balanced.
    // Low affinity (< 8000 bps): secondary categories dominate.
    public static int HighAffinityThresholdBps { get; } = 12000;
    public static int MidAffinityThresholdBps { get; } = 8000;

    // Category weights per mode at high affinity (sum to 100).
    // MineralSurvey: ResourceIntel dominant
    public static int MineralHighResourceIntel { get; } = 60;
    public static int MineralHighSignalLead { get; } = 20;
    public static int MineralHighPhysicalEvidence { get; } = 15;
    public static int MineralHighFragmentCache { get; } = 3;
    public static int MineralHighDataArchive { get; } = 2;

    // SignalSweep: SignalLead dominant
    public static int SignalHighResourceIntel { get; } = 15;
    public static int SignalHighSignalLead { get; } = 55;
    public static int SignalHighPhysicalEvidence { get; } = 20;
    public static int SignalHighFragmentCache { get; } = 5;
    public static int SignalHighDataArchive { get; } = 5;

    // Archaeological: PhysicalEvidence + DataArchive dominant
    public static int ArchHighResourceIntel { get; } = 5;
    public static int ArchHighSignalLead { get; } = 10;
    public static int ArchHighPhysicalEvidence { get; } = 40;
    public static int ArchHighFragmentCache { get; } = 10;
    public static int ArchHighDataArchive { get; } = 35;

    // Default fallback weights when mode doesn't match high/mid/low affinity bands.
    public static int DefaultResourceIntel { get; } = 50;
    public static int DefaultSignalLead { get; } = 20;
    public static int DefaultPhysicalEvidence { get; } = 20;
    public static int DefaultFragmentCache { get; } = 5;
    public static int DefaultDataArchive { get; } = 5;

    // Low-affinity primary-to-secondary transfer fraction (bps, 3000 = 30%).
    public static int LowAffinityTransferBps { get; } = 3000;

    // FragmentCache and DataArchive only from landing scans.
    // Orbital scans redistribute those weights to the top 3 categories.

    // ── Fragment affinity weights by planet type ──
    // Which AdaptationFragmentKind is favored on each planet type.
    // Values are relative weights (higher = more likely).
    public static int IceBiologicalWeight { get; } = 50;
    public static int IceOtherWeight { get; } = 10;

    public static int SandStructuralWeight { get; } = 50;
    public static int SandOtherWeight { get; } = 10;

    public static int LavaEnergeticWeight { get; } = 50;
    public static int LavaOtherWeight { get; } = 10;

    public static int GaseousCognitiveWeight { get; } = 50;
    public static int GaseousOtherWeight { get; } = 10;

    // Barren: no bias — all equal.
    public static int BarrenAnyWeight { get; } = 25;

    // Terrestrial: fragments rare (lowest base chance).
    public static int TerrestrialFragmentChanceBps { get; } = 500; // 5% base chance

    // ── Scanner tier unlock tech IDs ──
    public static string Mk1TechId { get; } = "sensors_mk1";
    public static string Mk2TechId { get; } = "deep_scan";
    public static string Mk3TechId { get; } = "advanced_sensors";
    public static string FractureScannerTechId { get; } = "fracture_scanner";

    // ── Instability gating ──
    // Fraction of deep-space seeds (hops >= 5) that get instability gates.
    public static int InstabilityGate2PercentBps { get; } = 2000; // 20%
    public static int InstabilityGate3PercentBps { get; } = 1000; // 10%
    public static int DeepSpaceHopThreshold { get; } = 5;

    // ── Mk3 dual-mode: second mode result quality penalty ──
    public static int DualModeSecondaryAffinityPenaltyBps { get; } = 3000; // -30%
}
