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

        // Non-starter node stock variance: creates NPC trade routes galaxy-wide.
        // Wide variance within same node type ensures profitable trades even between
        // two mining or two refinery nodes (overcomes the 10% bid-ask spread).
        // Mining nodes (even): ore surplus, metal/fuel deficit.
        public const int MiningOreBase = 200;
        public const int MiningOreVarianceMul = 5;      // ore = Base + geoHash * Mul (200-695)
        public const int MiningMetalBase = 2;
        public const int MiningMetalVarianceMod = 35;   // metal = Base + geoHash % Mod (2-36)
        public const int MiningFuelBase = 30;
        public const int MiningFuelVarianceMod = 120;   // fuel = Base + geoHash % Mod (30-149)
        // Refinery nodes (odd): metal surplus, ore/fuel deficit.
        public const int RefineryMetalBase = 100;
        public const int RefineryMetalVarianceMul = 3;   // metal = Base + geoHash * Mul (100-397)
        public const int RefineryOreVarianceMod = 40;    // ore = geoHash % Mod (0-39)
        public const int RefineryFuelBase = 10;
        public const int RefineryFuelVarianceMod = 80;   // fuel = Base + geoHash % Mod (10-89)

        // Munitions factory placement (replaces hull_plating forge).
        public const int MunitionsNodeModulus  = 7;   // Every Nth node gets a munitions factory.
        public const int MunitionsNodeOffset   = 3;   // Modular offset for placement (i % Modulus == Offset).
        public const int MunitionsMetalInput   = 2;   // Metal consumed per tick per trade_goods_v0.md.
        public const int MunitionsFuelInput    = 1;   // Fuel consumed per tick.
        public const int MunitionsOutput       = 3;   // Munitions produced per tick.
        public const int MunitionsBufferDays   = 0;   // No pre-buffering; consume inputs as they arrive.
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

        // Electronics fabricator (exotic_crystals + fuel → electronics).
        public const int ElectronicsNodeModulus = 8;
        public const int ElectronicsNodeOffset = 4;
        public const int ElectronicsCrystalsInput = 1;
        public const int ElectronicsFuelInput = 1;
        public const int ElectronicsOutput = 2;
        public const int ElectronicsDegradeBps = 400;
        public const int ElectronicsBootstrapCrystals = 50;
        public const int ElectronicsBootstrapStock = 30;

        // Composites bootstrap stock at fabricator nodes.
        public const int CompositesBootstrapStock = 30;

        // Salvage yard placement (salvaged_tech → metal or components).
        public const int SalvageNodeModulus = 13;
        public const int SalvageMetalNodeOffset = 2;
        public const int SalvageComponentsNodeOffset = 8;
        public const int SalvageTechInput = 1;
        public const int SalvageMetalOutput = 3;
        public const int SalvageComponentsOutput = 1;

        // Mine recipe: fuel → ore extraction.
        public const int MineOreOutput = 5;            // Ore produced per tick at mine sites.
        public const int MineFuelInput = 1;            // Fuel consumed per tick at mine sites.

        // Refinery recipe: ore + fuel → metal.
        public const int FactoryOreInput = 10;         // Ore consumed per tick at refineries.
        public const int FactoryMetalOutput = 5;       // Metal produced per tick at refineries.
        public const int FactoryFuelInput = 1;         // Fuel consumed per tick at refineries.
        public const int FactoryBufferDays = 2;        // GATE.T55.ECON.FACTORY_BUFFER.001: 2-day buffer for production stability.
        public const int FactoryDegradeBps = 500;      // 5% health loss per day at full undersupply.

        // GATE.T53.BOT.MARKET_SEED.001: Starter mining node manufactured-goods stock.
        public const int StarterMiningComposites = 25;
        public const int StarterMiningElectronics = 25;
        public const int StarterMiningRareMetals = 25;

        // Starter refinery node stock (odd starter nodes).
        public const int StarterRefineryFuel = 10;     // Low fuel drives traders to haul fuel in.
        public const int StarterRefineryMetal = 200;   // High metal drives traders to haul metal out.

        // GATE.T53.BOT.MARKET_SEED.001: Starter refinery node manufactured-goods stock.
        public const int StarterRefineryComposites = 20;
        public const int StarterRefineryElectronics = 20;

        // GATE.T55.ECON.FACTORY_BUFFER.001: Exotic crystals seed at starter nodes for recipe bootstrap.
        public const int StarterExoticCrystals = 15;

        // Distribution sink placement (starter region).
        public const int SinkPlacementModulus = 5;     // Every Nth starter node gets a metal sink.
        public const int SinkPlacementOffset = 1;      // Modular offset for sink placement.

        // GATE.T55.SUPPLY.RARE_METALS_RECIPE.001: Rare metals refinery placement.
        public const int RareMetalsRefNodeModulus = 10;
        public const int RareMetalsRefNodeOffset = 3;
        public const int RareMetalsRefOreInput = 5;
        public const int RareMetalsRefCrystalsInput = 1;
        public const int RareMetalsRefOutput = 2;
        public const int RareMetalsRefDegradeBps = 400;
        public const int RareMetalsRefBootstrapStock = 15;

        // GATE.T56.FIX.RARE_METALS_DRAIN.001: Rare metals mine at "(Rare Min)" nodes.
        // Without local production, seeded stock drains to zero via NPC haulers.
        public const int RareMetalsMineOutput = 3;     // rare_metals produced per tick.
        public const int RareMetalsMineFuelInput = 1;  // fuel consumed per tick.

        // ChainAnalysis: algorithm bounds.
        public const int ChainMaxTraceDepth = 10;
        public const int ChainMaxDepth = 4;          // Max recipe steps (fuel→ore→metal→electronics→components = 4).
        public const int ChainMaxByproducts = 1;     // Max byproducts per chain.
        public const int ChainReportBufferSize = 1024; // StringBuilder initial capacity for reports.
    }
}
