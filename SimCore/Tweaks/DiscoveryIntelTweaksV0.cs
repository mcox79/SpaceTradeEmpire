namespace SimCore.Tweaks;

// GATE.T41.DISCOVERY_INTEL.TWEAKS.001: Decay rates and thresholds for discovery-derived trade routes.
public static class DiscoveryIntelTweaksV0
{
    // Decay ticks by distance band (how long a discovery-derived route stays fresh).
    public const int NearDecayTicks = 50;
    public const int MidDecayTicks = 150;
    public const int DeepDecayTicks = 400;
    public const int FractureDecayTicks = 0; // 0 = never decays (fracture routes are rare and precious)

    // Hop thresholds for distance bands (from player start).
    public const int NearMaxHops = 2;
    public const int MidMaxHops = 5;
    // Deep = hops > MidMaxHops

    // Minimum estimated profit for a discovery to generate a trade route.
    public const int DiscoveryRouteMinProfit = 5;

    // High-value stale trigger: routes above this profit generate FO dialogue when going stale.
    public const int HighValueStaleThreshold = 30;
}
