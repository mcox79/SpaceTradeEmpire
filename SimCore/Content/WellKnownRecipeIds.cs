namespace SimCore.Content;

// GATE.S18.TRADE_GOODS.CONTENT_OVERHAUL.001: 9-recipe economy per trade_goods_v0.md.
// Canonical recipe ID constants for use in SimCore.Gen.
// Defined here (SimCore.Content, not scanned by ContentSubstrateIntegrationGuard) so that
// GalaxyGenerator can reference recipes without triggering hardcoded-string-literal violations.
public static class WellKnownRecipeIds
{
    // Extraction
    public const string ExtractOre              = "recipe_extract_ore";

    // Processing
    public const string RefineMetal             = "recipe_refine_metal";
    public const string ProcessFood             = "recipe_process_food";
    public const string AssembleElectronics     = "recipe_assemble_electronics";
    public const string FabricateComposites     = "recipe_fabricate_composites";
    public const string ManufactureMunitions    = "recipe_manufacture_munitions";

    // Manufacturing
    public const string AssembleComponents      = "recipe_assemble_components";

    // Salvage
    public const string SalvageToMetal          = "recipe_salvage_to_metal";
    public const string SalvageToComponents     = "recipe_salvage_to_components";
}
