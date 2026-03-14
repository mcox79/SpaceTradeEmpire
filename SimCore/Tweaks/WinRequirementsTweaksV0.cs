namespace SimCore.Tweaks;

// GATE.S8.WIN.GAME_RESULT.001: Win condition requirements per endgame path.
public static class WinRequirementsTweaksV0
{
    // --- Reinforce path (Concord/Weaver stability) ---
    public const int ReinforceMinConcordRep = 75;
    public const int ReinforceMinWeaverRep = 50;
    public const Entities.HavenTier ReinforceMinHavenTier = Entities.HavenTier.Expanded; // Tier 4
    public const string ReinforceRequiredFragment = "lattice_reading"; // Fragment #5

    // --- Naturalize path (Communion acceptance) ---
    public const int NaturalizeMinCommunionRep = 75;
    public const Entities.HavenTier NaturalizeMinHavenTier = Entities.HavenTier.Expanded; // Tier 4
    public const string NaturalizeRequiredFragment1 = "phase_tolerance"; // Fragment #7
    public const string NaturalizeRequiredFragment2 = "geometric_suspension"; // Fragment #8

    // --- Renegotiate path (dialogue with instability) ---
    public const int RenegotiateMinCommunionRep = 50;
    public const Entities.HavenTier RenegotiateMinHavenTier = Entities.HavenTier.Expanded; // Tier 4
    public const string RenegotiateRequiredFragment = "dialogue_protocol"; // Fragment #12
    public const int RenegotiateRequiredRevelations = 5; // All 5 revelations

    // --- Loss thresholds ---
    public const long BankruptcyCreditsThreshold = -500;
    // Bankruptcy requires credits below threshold AND no fleet cargo value to recover.
    public const int BankruptcyMinCargoValueToSurvive = 100;
}
