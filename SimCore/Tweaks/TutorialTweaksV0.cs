namespace SimCore.Tweaks;

// Gameplay constants for the tutorial system.
// All balance-relevant numerics live here (TweakRoutingGuard compliant).
public static class TutorialTweaksV0
{
    // Stall nudge: ticks before FO reminds the player what to do.
    public const int StallNudgeTicks = 60;

    // Minimum nodes visited to unlock Station/Intel tabs (mirrors onboarding state).
    public const int ExploreCompleteNodes = 3;

    // Typewriter speed: characters per second (0 = instant for headless).
    public const int TypewriterCharsPerSecond = 40;
}
