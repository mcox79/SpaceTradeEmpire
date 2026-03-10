namespace SimCore.Tweaks;

// GATE.S7.INSTABILITY.PHASE_MODEL.001: Per-node instability phase thresholds.
// 5 phases: Stable(0-24), Shimmer(25-49), Drift(50-74), Fracture(75-99), Void(100+).
public static class InstabilityTweaksV0
{
    // Phase threshold lower bounds (inclusive).
    public const int StableMin = 0;
    public const int ShimmerMin = 25;
    public const int DriftMin = 50;
    public const int FractureMin = 75;
    public const int VoidMin = 100;

    // Phase names for UI display.
    public const string StableName = "Stable";
    public const string ShimmerName = "Shimmer";
    public const string DriftName = "Drift";
    public const string FractureName = "Fracture";
    public const string VoidName = "Void";

    // GATE.S7.INSTABILITY.WORLDGEN.001: Initial instability ranges by world class.
    public const int CoreInstabilityMin = 0;
    public const int CoreInstabilityMax = 10;
    public const int FrontierInstabilityMin = 10;
    public const int FrontierInstabilityMax = 30;
    public const int RimInstabilityMin = 20;
    public const int RimInstabilityMax = 50;
    public const int VoidSiteInstabilityMin = 50;
    public const int VoidSiteInstabilityMax = 80;

    // GATE.S7.INSTABILITY.TICK_SYSTEM.001: Per-tick evolution tuning.
    // Warfront-adjacent nodes gain instability; distant nodes decay.
    public const int BaseGainPerTick = 1;          // gain per tick per intensity level
    public const int DecayAmountPerInterval = 1;   // loss per decay interval
    public const int DecayIntervalTicks = 100;     // ticks between decay steps
    public const int MaxInstability = 150;

    // GATE.S7.INSTABILITY.CONSEQUENCES.001: Phase-based trade/travel effects.
    public const int ShimmerPriceJitterPct = 5;    // ±5% price jitter
    public const int DriftLaneDelayPct = 20;       // +20% lane travel delay
    public const int FractureTradeFailurePct = 10; // 10% chance trade fails
    // Void = market closure (boolean, no constant needed)

    // GATE.S7.INSTABILITY_EFFECTS.LANE.001: Phase-scaled lane delay percentages.
    // Shimmer=+10%, Drift=+20% (existing), Fracture=+40%. Void=lane severed.
    public const int ShimmerLaneDelayPct = 10;
    public const int FractureLaneDelayPct = 40;

    // GATE.S7.INSTABILITY_EFFECTS.MARKET.001: Instability price volatility.
    // Linear scale: multiplier bps = 10000 + (level * VolatilityMaxBps / MaxInstability).
    // At level 0: 1.0x. At level 150 (MaxInstability): 1.5x.
    public const int VolatilityMaxBps = 5000;

    // GATE.S7.INSTABILITY_EFFECTS.MARKET.001: Security demand skew at Drift+ (phase ≥2).
    // Fuel and munitions get additional price surcharge in unstable regions.
    // Scales with phase above Shimmer: phase 2 = 1x, phase 3 = 2x, phase 4 = 3x.
    public const int SecurityDemandSkewBps = 2000;

    /// <summary>Returns the phase name for a given instability level.</summary>
    public static string GetPhaseName(int level)
    {
        if (level >= VoidMin) return VoidName;
        if (level >= FractureMin) return FractureName;
        if (level >= DriftMin) return DriftName;
        if (level >= ShimmerMin) return ShimmerName;
        return StableName;
    }

    /// <summary>Returns the phase index (0-4) for a given instability level.</summary>
    public static int GetPhaseIndex(int level)
    {
        if (level >= VoidMin) return 4;
        if (level >= FractureMin) return 3;
        if (level >= DriftMin) return 2;
        if (level >= ShimmerMin) return 1;
        return 0;
    }

    /// <summary>Returns effect descriptions for the current phase.</summary>
    public static string[] GetPhaseEffects(int level)
    {
        var phase = GetPhaseIndex(level);
        return phase switch
        {
            0 => new[] { "Normal trade conditions", "Standard lane capacity" },
            1 => new[] { "Minor price volatility (+5%)", "Occasional sensor ghosts" },
            2 => new[] { "Trade route instability (+15%)", "Lane capacity reduced", "Discovery sites may shift" },
            3 => new[] { "Severe price spikes (+30%)", "Lane closures possible", "Hostile anomalies active" },
            4 => new[] { "Markets may fail", "Lanes severed", "Void entities present", "Fracture travel required" },
            _ => new[] { "Unknown phase" },
        };
    }
}
