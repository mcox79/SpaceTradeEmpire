namespace SimCore.Tweaks
{
    // GATE.S18.TRADE_GOODS.CONTENT_OVERHAUL.001: market initialization knobs for 13-good economy.
    public static class CatalogTweaksV0
    {
        public const int FuelInitialStock  = 500; // Bootstrap supply: prevents economy stall before fuel wells ramp up.
        public const int MetalInitialStock = 0;   // Key presence only; metal must be refined from ore at smelters.
        public const int OreInitialStock   = 0;   // Key presence only; ore must be mined at ore deposits.

        // GATE.S18.TRADE_GOODS.GEO_DISTRIBUTION.001: Geographic distribution knobs.
        public const int OrganicsNodePct       = 40;   // ~40% of nodes seed organics (agri-systems).
        public const int OrganicsInitialStock  = 300;  // Starting organics inventory at agri nodes.
        public const int RareMetalsNodePct     = 15;   // ~15% of nodes seed rare_metals (clustered).
        public const int RareMetalsInitialStock = 150; // Starting rare_metals inventory.

        // Munitions factory placement (replaces hull_plating forge).
        public const int MunitionsNodeModulus  = 7;   // Every Nth node gets a munitions factory.
        public const int MunitionsNodeOffset   = 3;   // Modular offset for placement (i % Modulus == Offset).
        public const int MunitionsMetalInput   = 2;   // Metal consumed per tick per trade_goods_v0.md.
        public const int MunitionsFuelInput    = 1;   // Fuel consumed per tick.
        public const int MunitionsOutput       = 3;   // Munitions produced per tick.
        public const int MunitionsBufferDays   = 2;   // Input buffer days for munitions sites.
        public const int MunitionsDegradeBps   = 500; // 5% health loss per day at full undersupply.

        // GATE.S7.PRODUCTION.FULL_DEPLOY.001: Remaining recipe placement knobs.
        // Food processor at agri nodes (organics → food).
        public const int FoodProcessorOrganicsInput = 2;
        public const int FoodProcessorFuelInput = 1;
        public const int FoodProcessorFoodOutput = 3;
        public const int FoodProcessorDegradeBps = 300;

        // Composites fabricator at industrial nodes (metal + organics → composites).
        public const int CompositesNodeModulus = 9;
        public const int CompositesNodeOffset = 5;
        public const int CompositesMetalInput = 2;
        public const int CompositesOrganicsInput = 1;
        public const int CompositesOutput = 2;
        public const int CompositesDegradeBps = 400;

        // Components assembler (metal + electronics → components).
        public const int ComponentsNodeModulus = 11;
        public const int ComponentsNodeOffset = 7;
        public const int ComponentsMetalInput = 3;
        public const int ComponentsElectronicsInput = 1;
        public const int ComponentsOutput = 2;
        public const int ComponentsDegradeBps = 500;

        // Salvage yard placement (salvaged_tech → metal or components).
        public const int SalvageNodeModulus = 13;
        public const int SalvageMetalNodeOffset = 2;
        public const int SalvageComponentsNodeOffset = 8;
        public const int SalvageTechInput = 1;
        public const int SalvageMetalOutput = 3;
        public const int SalvageComponentsOutput = 1;

        // ChainAnalysis: algorithm bounds.
        public const int ChainMaxTraceDepth = 10;
        public const int ChainMaxDepth = 4;          // Max recipe steps (fuel→ore→metal→electronics→components = 4).
        public const int ChainMaxByproducts = 1;     // Max byproducts per chain.
        public const int ChainReportBufferSize = 1024; // StringBuilder initial capacity for reports.
    }
}
