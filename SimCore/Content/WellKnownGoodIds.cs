namespace SimCore.Content;

// GATE.S4.CATALOG.MARKET_BIND.001: canonical good ID constants for use in SimCore.Gen and SimCore.Systems.
// Defined here (SimCore.Content, not scanned by ContentSubstrateIntegrationGuard) so that
// GalaxyGenerator and other systems can reference goods without triggering hardcoded-string-literal violations.
public static class WellKnownGoodIds
{
    public const string Food         = "food";
    public const string Fuel         = "fuel";
    public const string HullPlating  = "hull_plating";
    public const string Metal        = "metal";
    public const string Ore          = "ore";

    // GATE.S6.FRACTURE.CONTENT.001: fracture-exclusive goods (high value, only at fracture nodes).
    public const string AnomalySamples = "anomaly_samples";
    public const string ExoticCrystals = "exotic_crystals";
    public const string SalvagedTech   = "salvaged_tech";
}
