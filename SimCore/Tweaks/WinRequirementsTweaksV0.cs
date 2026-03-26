namespace SimCore.Tweaks;

// GATE.S8.WIN.GAME_RESULT.001: Win condition requirements per endgame path.
public static class WinRequirementsTweaksV0
{
    // --- Reinforce path (Concord/Weaver stability) ---
    public const int ReinforceMinConcordRep = 75;
    public const int ReinforceMinWeaverRep = 50;
    public const Entities.HavenTier ReinforceMinHavenTier = Entities.HavenTier.Expanded; // Tier 4
    public const string ReinforceRequiredFragment = "frag_str_04"; // Lattice Shard — structural resonance

    // --- Naturalize path (Communion acceptance) ---
    public const int NaturalizeMinCommunionRep = 75;
    public const Entities.HavenTier NaturalizeMinHavenTier = Entities.HavenTier.Expanded; // Tier 4
    public const string NaturalizeRequiredFragment1 = "frag_str_03"; // Phase Anchor — stability tolerance
    public const string NaturalizeRequiredFragment2 = "frag_str_02"; // Compression Seed — geometric suspension

    // --- Renegotiate path (dialogue with instability) ---
    public const int RenegotiateMinCommunionRep = 50;
    public const Entities.HavenTier RenegotiateMinHavenTier = Entities.HavenTier.Expanded; // Tier 4
    public const string RenegotiateRequiredFragment = "frag_cog_04"; // Oracle Fragment — dialogue protocol
    public const int RenegotiateRequiredRevelations = 5; // All 5 revelations

    // --- Loss thresholds ---
    public const long BankruptcyCreditsThreshold = -500;
    // Bankruptcy requires credits below threshold AND no fleet cargo value to recover.
    public const int BankruptcyMinCargoValueToSurvive = 100;
}
