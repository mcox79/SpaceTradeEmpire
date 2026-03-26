namespace SimCore.Tweaks;

// GATE.T57.FEEL.DISCOVERY_FAILURE.001: Tuning for discovery failure states.
// 6 failure types + partial success. Probabilities in basis points (10000 = 100%).
public static class DiscoveryFailureTweaksV0
{
    // Failure probability per scan attempt (basis points). Higher instability = higher chance.
    public static int BaseFailureChanceBps { get; } = 500;     // 5% base
    public static int PerInstabilityBonusBps { get; } = 200;   // +2% per instability level
    public static int MaxFailureChanceBps { get; } = 3000;     // 30% cap

    // Partial success: reduced reward but scan still advances.
    public static int PartialSuccessChanceBps { get; } = 1500; // 15% chance of partial (instead of full failure)
    public static int PartialSuccessRewardPct { get; } = 50;   // 50% of normal reward on partial

    // Failure type weights (relative, must sum to 100).
    public static int ScanInterferenceWeight { get; } = 25;
    public static int HazardAbortWeight { get; } = 20;
    public static int IntelSpoilageWeight { get; } = 15;
    public static int ChainDeadEndWeight { get; } = 15;
    public static int ContestedDiscoveryWeight { get; } = 15;
    public static int FalsePositiveWeight { get; } = 10;

    // Cooldown ticks after failure before retry is allowed.
    public static int FailureCooldownTicks { get; } = 30;
}
