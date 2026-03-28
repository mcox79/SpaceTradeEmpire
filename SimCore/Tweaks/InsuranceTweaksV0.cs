namespace SimCore.Tweaks;

// GATE.T62.LOSS.INSURANCE_MODEL.001: Fleet insurance tuning constants.
public static class InsuranceTweaksV0
{
    // Premium rate in basis points of ship purchase price per cycle.
    // 200 bps = 2% of ship value per premium cycle.
    public const int PremiumRateBps = 200;
    public const int BpsDivisor = 10000;

    // Premium deduction interval in ticks (every 100 ticks = ~1 game-day cycle).
    public const int PremiumIntervalTicks = 100;

    // Insurance payout as percentage of ship value in basis points.
    // 7000 bps = 70% payout on destruction.
    public const int PayoutRateBps = 7000;

    // Deductible: flat credit cost subtracted from payout.
    public const int DeductibleCredits = 200;

    // Minimum credits for insurance to auto-enroll. Below this, skip premium deduction.
    public const int MinCreditsForPremium = 100;

    // Grace period: ticks after purchase before first premium is due.
    public const int GracePeriodTicks = 50;

    // GATE.T62.LOSS.REPLACEMENT_FLOW.001: Respawn tuning.
    // Minimum hull HP on respawn (floor for all ship classes).
    public const int RespawnMinHull = 10;
    // Hull restored = max(RespawnMinHull, CoreHull / RespawnHullDivisor).
    public const int RespawnHullDivisor = 4;
}
