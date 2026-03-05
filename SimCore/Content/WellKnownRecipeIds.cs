namespace SimCore.Content;

// GATE.S4.INDU_STRUCT.GENESIS_WIRE.001: canonical recipe ID constants for use in SimCore.Gen.
// Defined here (SimCore.Content, not scanned by ContentSubstrateIntegrationGuard) so that
// GalaxyGenerator can reference recipes without triggering hardcoded-string-literal violations.
public static class WellKnownRecipeIds
{
    public const string ExtractOre          = "recipe_extract_ore";
    public const string RefineOreToMetal    = "recipe_refine_ore_to_metal";
    public const string ForgeHullPlating    = "recipe_forge_hull_plating";

    // GATE.S4.CATALOG.RECIPE_WAVE.001: advanced production recipes
    public const string AssembleElectronics = "recipe_assemble_electronics";
    public const string ForgeCompositeArmor = "recipe_forge_composite_armor";
    public const string SalvageRefine       = "recipe_salvage_refine";
}
