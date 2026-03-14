using SimCore.Entities;

namespace SimCore.Tweaks;

// GATE.S8.HAVEN.ENDGAME_PATHS.001: Endgame path tuning constants.
public static class EndgameTweaksV0
{
    // Minimum Haven tier to choose an endgame path.
    public const HavenTier MinTierForChoice = HavenTier.Expanded; // Tier 4

    // Per-tick reputation drift per path (applied to the path's aligned faction).
    public const int ReinforceRepDriftPerInterval = 1;     // Concord-aligned stability
    public const int NaturalizeRepDriftPerInterval = 1;    // Communion-aligned acceptance
    public const int RenegotiateRepDriftPerInterval = -1;  // Costs factional trust

    // Interval (ticks) between path-specific rep drift applications.
    public const int PathDriftIntervalTicks = 100;

    // Accommodation thread progress per qualifying action.
    public const int AccommodationProgressPerAction = 5;

    // Max accommodation progress per thread.
    public const int AccommodationMaxProgress = 100;

    // Communion Representative spawn conditions.
    public const HavenTier CommunionRepMinTier = HavenTier.Operational; // Tier 3
    public const int CommunionRepMinFactionRep = 0; // Neutral or better
    public const int CommunionRepDialogueTierMax = 3;
}
