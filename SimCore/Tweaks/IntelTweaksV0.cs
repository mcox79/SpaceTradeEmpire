namespace SimCore.Tweaks;

public static class IntelTweaksV0
{
    public const int ExpeditionDurationTicks = 10;
    public const int ExpeditionLeadProbability_0_100 = 80;

    // GATE.S3_6.RUMOR_INTEL_MIN.002
    // Minimum rumor leads seeded per generated world. Tweak-routed: do not introduce a literal in GalaxyGenerator.
    public const int MinRumorLeadsPerSeed = 1;

    // GATE.S7.REVEALS.WARFRONT_REVEAL.001: Progressive warfront intel tier thresholds.
    // Tier 1 (Presence): node within sensor range reveals warfront exists.
    // Tier 2 (Composition): player has visited node reveals fleet strength.
    // Tier 3 (Strategic): sustained observation (N ticks at node) reveals supply + strategic value.
    public const int WarfrontIntelTier1 = 1;   // Presence — detected at sensor range
    public const int WarfrontIntelTier2 = 2;   // Composition — after visit
    public const int WarfrontIntelTier3 = 3;   // Strategic — sustained observation
    public const int WarfrontIntelTier3ObservationTicks = 15; // Ticks at node for tier 3
}
