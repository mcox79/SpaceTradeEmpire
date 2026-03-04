namespace SimCore.Tweaks;

// GATE.S4.TECH.CORE.001: Research pacing constants (integers only for determinism).
public static class ResearchTweaksV0
{
    // Credit cost per research tick (multiplied by tech CreditCost / ResearchTicks).
    public const int CreditCostPerTickBase = 5;

    // Max concurrent research projects (v0: always 1).
    public const int MaxConcurrentResearch = 1;

    // Progress ticks added per sim tick while researching.
    public const int ProgressPerTick = 1;

    // Tech level increase per fracture_drive unlock.
    public const int TechLevelPerFractureDrive = 1;
}
