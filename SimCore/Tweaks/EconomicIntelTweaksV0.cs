namespace SimCore.Tweaks;

// GATE.T57.PIPELINE.ECONOMIC_INTEL.001: Tuning for economic intel generation and freshness.
public static class EconomicIntelTweaksV0
{
    // Base estimated value per intel type (credits).
    public static int ResourceDepositBaseValue { get; } = 80;
    public static int CargoManifestBaseValue { get; } = 60;
    public static int MarketAnomalyBaseValue { get; } = 40;
    public static int ChainIntelBaseValue { get; } = 120;
    public static int MarketRuinBaseValue { get; } = 100;

    // Freshness decay ticks by distance band (mirrors DiscoveryIntelTweaksV0).
    // Near (0-2 hops): fast decay. Mid (3-5): moderate. Deep (6+): slow. Fracture: never.
    public static int NearFreshnessTicks { get; } = 50;
    public static int MidFreshnessTicks { get; } = 150;
    public static int DeepFreshnessTicks { get; } = 400;
    public static int FractureFreshnessTicks { get; } = 0; // 0 = never decays

    // Margin buffer widening percentages (basis points) at decay thresholds.
    // Applied at 33%, 66%, 100% of freshness decay window.
    public static int MarginBufferEarlyBps { get; } = 500;   // 5% at 33% decay
    public static int MarginBufferMidBps { get; } = 1500;    // 15% at 66% decay
    public static int MarginBufferLateBps { get; } = 2500;   // 25% at 100% decay

    // Distance band hop thresholds (consistent with DiscoveryIntelTweaksV0).
    public static int NearMaxHops { get; } = 2;
    public static int MidMaxHops { get; } = 5;

    // GATE.T62.PIPELINE.INTEL_MARGIN.001: Margin confidence decay schedule (BPS = 0-10000).
    // 10000 = full confidence, 0 = no confidence (expired).
    public const int FullConfidenceBps = 10000;
    // Half-life: at 50% freshness, confidence drops to HalfLifeConfidenceBps.
    public const int HalfLifeConfidenceBps = 7000;
    // At 100% freshness consumed, confidence drops to ExpiredConfidenceBps.
    public const int ExpiredConfidenceBps = 0;
}
