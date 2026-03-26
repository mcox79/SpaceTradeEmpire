namespace SimCore.Tweaks;

// GATE.T57.CENTAUR.CONFIDENCE_LANG.001: Confidence score computation parameters.
// ConfidenceScore = base + provenBonus - ageDecay - volatilityPenalty, clamped [0,100].
public static class ConfidenceLangTweaksV0
{
    public static int BaseConfidence { get; } = 50;

    // Per proven trade on this route: +5 per trade, capped at +25 total.
    public static int PerProvenTradeBonusPts { get; } = 5;
    public static int MaxProvenTradeBonus { get; } = 25;

    // Age decay: -1 per AgePenaltyTickInterval ticks since last validated.
    public static int AgePenaltyTickInterval { get; } = 20;
    public static int MaxAgePenalty { get; } = 30;

    // Volatility: -2 per % point the actual price differs from estimated.
    public static int VolatilityPenaltyPerPct { get; } = 2;
    public static int MaxVolatilityPenalty { get; } = 20;

    // Refresh cadence: how often to recompute confidence (ticks).
    public static int RefreshCadenceTicks { get; } = 10;
}
