namespace SimCore.Tweaks;

// GATE.T45.DEEP_DREAD.TWEAKS.001: Deep dread system tuning constants.
// Subnautica-style depth-as-dread: Thread Lattice instability phases map to
// escalating terror layers (Isolation → Phenomena → Predation → Meta-Dread).
public static class DeepDreadTweaksV0
{
    // --- Layer 1: Isolation (patrol thinning) ---
    // Hops from faction capital at which patrol density scales.
    // 0-2 = full density, 3-4 = half, 5+ = zero.
    public const int PatrolFullDensityMaxHops = 2;
    public const int PatrolHalfDensityMaxHops = 4;
    // BPS multiplier for half-density (5000 = 50%).
    public const int PatrolHalfDensityBps = 5000;

    // --- Layer 2: Phenomena (passive hull drain) ---
    // Hull drain per tick at Phase 2 (Drift) nodes. 1 HP per 50 ticks.
    public const int Phase2DrainIntervalTicks = 50;
    public const int Phase2DrainAmount = 1;
    // Hull drain per tick at Phase 3 (Fracture) nodes. 1 HP per 20 ticks.
    public const int Phase3DrainIntervalTicks = 20;
    public const int Phase3DrainAmount = 1;
    // Phase 4 (Void) = NO drain (void paradox — clarity at maximum depth).
    // Accommodation module ID that grants drain immunity.
    public const string AccommodationModuleId = "accommodation_geometry";

    // --- Sensor ghosts ---
    // Minimum instability phase for ghost spawning (2 = Drift).
    public const int GhostMinPhase = 2;
    // Ghost spawn check interval.
    public const int GhostCheckIntervalTicks = 25;
    // Ghost lifetime range (ticks).
    public const int GhostMinLifetimeTicks = 3;
    public const int GhostMaxLifetimeTicks = 8;
    // Max concurrent ghosts per state.
    public const int GhostMaxConcurrent = 3;
    // Ghost spawn probability modulus (hash % N < threshold = spawn).
    public const int GhostSpawnModulus = 100;
    // Base threshold at Phase 2. Phase 3 = 2x, Phase 4 = 3x.
    public const int GhostSpawnBaseThreshold = 15;

    // --- Information fog ---
    // Market data staleness: ticks before data ages out by distance band.
    public const int FogNearHopsMax = 3;        // 0-3 hops = always fresh
    public const int FogMidHopsMax = 5;          // 4-5 hops = stale after N ticks
    public const int FogMidStaleTicks = 100;     // ticks until mid-range data goes stale
    public const int FogDeepStaleTicks = 50;     // 6+ hops = stale faster
    // Scanner range reduction at Phase 2+ (BPS reduction per phase above 1).
    public const int ScanRangeReductionBpsPerPhase = 1500;

    // --- Exposure tracking ---
    // Ticks at Phase 2+ before milestone triggers.
    public const int ExposureMildThreshold = 20;
    public const int ExposureHeavyThreshold = 50;
    public const int ExposureAdaptedThreshold = 100;
    // Instrument disagreement narrowing factor (per 1000 exposure ticks).
    public const int DisagreementNarrowPerKExposure = 100; // 10% per 1000 ticks

    // --- Exposure-scaled drain intervals ---
    // Override drain intervals when exposure reaches mild/heavy thresholds.
    // High exposure = faster drain (the lattice "recognizes" you).
    public const int DrainIntervalMildExposure = 30;   // Phase 2: 50 → 30 ticks at mild exposure
    public const int DrainIntervalHeavyExposure = 20;  // Phase 2: 50 → 20 ticks at heavy exposure

    // --- Phase 2+ secondary stressors ---
    // Fuel burn multiplier at Phase 2+ (BPS, 10000 = 1x, 20000 = 2x).
    public const int FuelBurnMultiplierPhase2Bps = 20000;
    public const int BpsDivisor = 10000;
    // Cargo value decay: basis points of total cargo value lost per cycle tick.
    // Applied every CargoDecayCycleTicks at Phase 2+.
    public const int CargoDecayBpsPerCycle = 50;       // 0.5% per cycle
    public const int CargoDecayCycleTicks = 25;        // every 25 ticks

    // --- FO distance triggers ---
    public const int FoFarFromPatrolHops = 4;
    public const int FoCommsLostHops = 6;

    // --- Audio crossfade ---
    // Instability level thresholds for ambient audio layer crossfade.
    public const int AudioSafeMax = 24;          // below Shimmer
    public const int AudioShimmerMax = 49;        // Shimmer range
    public const int AudioDriftMax = 74;          // Drift range
    public const int AudioFractureMax = 99;       // Fracture range
    // Above 99 = Void layer
}
