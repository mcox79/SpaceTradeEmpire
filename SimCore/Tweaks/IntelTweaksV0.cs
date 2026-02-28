namespace SimCore.Tweaks;

public static class IntelTweaksV0
{
    public const int ExpeditionDurationTicks = 10;
    public const int ExpeditionLeadProbability_0_100 = 80;

    // GATE.S3_6.RUMOR_INTEL_MIN.002
    // Minimum rumor leads seeded per generated world. Tweak-routed: do not introduce a literal in GalaxyGenerator.
    public const int MinRumorLeadsPerSeed = 1;
}
