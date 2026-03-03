namespace SimCore.Tweaks
{
    // GATE.S4.CATALOG.EPIC_CLOSE.001: market initialization knobs for base world generation.
    // Only goods requiring universal key seeding live here.
    // food is intentionally excluded: food is a production good requiring agricultural node profiles.
    // Do NOT add food here — seeding it universally at genesis undermines market class distinctness.
    public static class CatalogTweaksV0
    {
        public const int FuelInitialStock  = 500; // Bootstrap supply: prevents economy stall before fuel wells ramp up.
        public const int MetalInitialStock = 0;   // Key presence only; metal must be refined from ore at smelters.
        public const int OreInitialStock   = 0;   // Key presence only; ore must be mined at ore deposits.
        public const int HullPlatingInitialStock = 0; // Key presence only; hull_plating must be forged from metal.

        // GATE.S4.INDU_STRUCT.GENESIS_WIRE.001: forge site placement and parameters.
        public const int ForgeNodeModulus  = 7;   // Every Nth node gets a hull_plating forge.
        public const int ForgeNodeOffset   = 3;   // Modular offset for forge placement (i % Modulus == Offset).
        public const int ForgeMetalInput   = 5;   // Metal consumed per tick by hull_plating forge.
        public const int ForgeHullOutput   = 1;   // Hull_plating produced per tick by forge.
        public const int ForgeBufferDays   = 2;   // Input buffer days for forge sites.
        public const int ForgeDegradeBps   = 500; // 5% health loss per day at full undersupply.

        // ChainAnalysis: algorithm bounds.
        public const int ChainMaxTraceDepth = 10;
        public const int ChainMaxDepth = 3;          // Max recipe steps in a valid production chain.
        public const int ChainMaxByproducts = 1;     // Max byproducts per chain.
        public const int ChainReportBufferSize = 1024; // StringBuilder initial capacity for reports.
    }
}
