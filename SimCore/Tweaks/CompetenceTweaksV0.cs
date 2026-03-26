namespace SimCore.Tweaks;

// GATE.T57.CENTAUR.COMPETENCE_TIERS.001: Crisis-gated tier advancement thresholds.
// Per fo_trade_manager_v0.md: growth through crisis survival, not XP accumulation.
public static class CompetenceTweaksV0
{
    // Tier 1 → 2 (Novice → Competent): basic trade mastery + exposure to danger
    public static int CompetentMinTrades { get; } = 15;
    public static int CompetentMinNodes { get; } = 5;
    public static bool CompetentRequiresWarfront { get; } = true;

    // Tier 2 → 3 (Competent → Master): strategic mastery + endgame engagement
    public static int MasterMinSystems { get; } = 8;
    public static bool MasterRequiresHaven { get; } = true;
    public static bool MasterRequiresEndgameTrigger { get; } = true;

    // Confidence score adjustments per event
    public static int ConfidenceOnProfitableTrade { get; } = 2;
    public static int ConfidenceOnUnprofitableTrade { get; } = -3;
    public static int ConfidenceOnDiscovery { get; } = 3;
    public static int ConfidenceOnCombatSurvival { get; } = 5;
    public static int ConfidenceOnDeath { get; } = -10;
    public static int ConfidenceMin { get; } = 0;
    public static int ConfidenceMax { get; } = 100;
}
