namespace SimCore.Content;

// GATE.S4.INDU_STRUCT.GENESIS_WIRE.001: canonical recipe ID constants for use in SimCore.Gen.
// Defined here (SimCore.Content, not scanned by ContentSubstrateIntegrationGuard) so that
// GalaxyGenerator can reference recipes without triggering hardcoded-string-literal violations.
public static class WellKnownRecipeIds
{
    public const string ExtractOre          = "recipe_extract_ore";
    public const string RefineOreToMetal    = "recipe_refine_ore_to_metal";
    public const string ForgeHullPlating    = "recipe_forge_hull_plating";
}
