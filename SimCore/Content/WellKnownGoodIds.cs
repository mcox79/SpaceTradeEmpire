namespace SimCore.Content;

// GATE.S4.CATALOG.MARKET_BIND.001: canonical good ID constants for use in SimCore.Gen and SimCore.Systems.
// Defined here (SimCore.Content, not scanned by ContentSubstrateIntegrationGuard) so that
// GalaxyGenerator and other systems can reference goods without triggering hardcoded-string-literal violations.
public static class WellKnownGoodIds
{
    public const string Food  = "food";
    public const string Fuel  = "fuel";
    public const string Metal = "metal";
    public const string Ore   = "ore";
}
