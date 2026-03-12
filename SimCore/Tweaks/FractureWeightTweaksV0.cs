namespace SimCore.Tweaks;

// GATE.T18.NARRATIVE.FRACTURE_WEIGHT.001: Fracture weight mechanic constants.
// Cargo loaded in unstable space weighs differently when brought to stable space.
// Dynamic ratios per instability phase prevent wiki-lookup optimization.
// All values in basis points (10000 = 1.0x).
public static class FractureWeightTweaksV0
{
    // Phase 1 (Shimmer): barely noticeable weight variation.
    public const int Phase1MinWeightBps = 9500;  // 0.95x
    public const int Phase1MaxWeightBps = 10500; // 1.05x

    // Phase 2 (Drift): significant weight variation — trade puzzle emerges.
    public const int Phase2MinWeightBps = 8000;  // 0.80x
    public const int Phase2MaxWeightBps = 13000; // 1.30x

    // Phase 3 (Fracture): dramatic — experienced players exploit this.
    public const int Phase3MinWeightBps = 5000;  // 0.50x
    public const int Phase3MaxWeightBps = 20000; // 2.00x

    // Phase 0 (Stable): no weight variation.
    public const int Phase0WeightBps = 10000; // 1.00x exactly

    // Instability phase thresholds (must match Node.InstabilityLevel ranges).
    // STRUCTURAL: these mirror the existing 5-phase model.
    public const int STRUCT_PhaseShimmerMin = 25;
    public const int STRUCT_PhaseDriftMin = 50;
    public const int STRUCT_PhaseFractureMin = 75;
    public const int STRUCT_PhaseVoidMin = 100;
}
