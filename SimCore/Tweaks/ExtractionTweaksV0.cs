namespace SimCore.Tweaks;

/// <summary>
/// GATE.EXTRACT.BUILD_CREATES_INDUSTRY.001: Extraction station tweaks.
/// GATE.EXTRACT.FRACTURE_PROGRAM.001: Fracture extraction program tweaks.
/// </summary>
public static class ExtractionTweaksV0
{
    // Extraction station output per tick (units of the mapped good).
    public const int ExtractionOutputPerTick = 2;

    // Fracture extraction program: per-cycle fuel cost.
    public const int FractureExtractionFuelCost = 3;

    // Fracture extraction program: per-cycle exotic_crystals cost.
    public const int FractureExtractionCrystalCost = 3;

    // Fracture extraction program: per-cycle exotic_matter yield (min).
    public const int FractureExtractionYieldMin = 5;

    // Fracture extraction program: per-cycle exotic_matter yield (max).
    public const int FractureExtractionYieldMax = 8;

    // Fracture extraction program: per-cycle DeepExposure increment.
    public const int FractureExtractionExposurePerCycle = 1;

    // Fracture extraction program: default cadence in ticks.
    public const int FractureExtractionCadenceTicks = 60;
}
