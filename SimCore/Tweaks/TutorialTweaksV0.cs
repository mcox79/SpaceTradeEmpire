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

    // Act 5: Tutorial pirate stats (guaranteed-win encounter).
    public const int CombatTutorialPirateHull = 30;
    public const int CombatTutorialPirateShields = 0;

    // Act 6: Module granted to player after combat tutorial.
    public const string ModuleGrantId = "mod_basic_laser";

    // Act 7: Ticks to wait while automation runs before advancing.
    public const int AutomationWaitTicks = 30;

    // Manual trades required before automation unlock (pain before relief).
    public const int RequiredManualTrades = 3;

    // [DEPRECATED] Act 8 removed from tutorial — Haven is progressive disclosure.
    public const int HavenUpgradeStallTicks = 60;
}
