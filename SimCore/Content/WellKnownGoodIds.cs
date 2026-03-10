namespace SimCore.Content;

// GATE.S18.TRADE_GOODS.CONTENT_OVERHAUL.001: 13-good economy per trade_goods_v0.md.
// Canonical good ID constants for use in SimCore.Gen and SimCore.Systems.
// Defined here (SimCore.Content, not scanned by ContentSubstrateIntegrationGuard) so that
// GalaxyGenerator and other systems can reference goods without triggering hardcoded-string-literal violations.
public static class WellKnownGoodIds
{
    // Tier 1 — Extraction
    public const string Fuel           = "fuel";
    public const string Ore            = "ore";
    public const string Organics       = "organics";
    public const string RareMetals     = "rare_metals";

    // Tier 2 — Processed
    public const string Metal          = "metal";
    public const string Food           = "food";
    public const string Composites     = "composites";
    public const string Electronics    = "electronics";
    public const string Munitions      = "munitions";

    // Tier 2.5 — Manufactured
    public const string Components     = "components";

    // Tier 3 — Exotic
    public const string ExoticCrystals = "exotic_crystals";
    public const string SalvagedTech   = "salvaged_tech";
    public const string ExoticMatter   = "exotic_matter";

    // GATE.S7.INSTABILITY_EFFECTS.MARKET.001: Security goods see demand skew in unstable regions.
    public static bool IsSecurityGood(string goodId) =>
        System.StringComparer.Ordinal.Equals(goodId, Fuel) ||
        System.StringComparer.Ordinal.Equals(goodId, Munitions);
}
