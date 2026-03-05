namespace SimCore.Tweaks;

// GATE.S7.PLANET.MODEL.001: Planet generation and landability constants (integers only for determinism).
public static class PlanetTweaksV0
{
    // ── Physical property ranges per planet type (basis points) ──

    // Terrestrial
    public const int TerrestrialGravityMin = 3000;
    public const int TerrestrialGravityMax = 7000;
    public const int TerrestrialAtmoMin = 3000;
    public const int TerrestrialAtmoMax = 7000;
    public const int TerrestrialTempMin = 3500;
    public const int TerrestrialTempMax = 6500;

    // Ice
    public const int IceGravityMin = 2000;
    public const int IceGravityMax = 5000;
    public const int IceAtmoMin = 1000;
    public const int IceAtmoMax = 4000;
    public const int IceTempMin = 500;
    public const int IceTempMax = 2500;

    // Sand (desert)
    public const int SandGravityMin = 4000;
    public const int SandGravityMax = 8000;
    public const int SandAtmoMin = 500;
    public const int SandAtmoMax = 3000;
    public const int SandTempMin = 6000;
    public const int SandTempMax = 8500;

    // Lava (volcanic)
    public const int LavaGravityMin = 5000;
    public const int LavaGravityMax = 9000;
    public const int LavaAtmoMin = 2000;
    public const int LavaAtmoMax = 6000;
    public const int LavaTempMin = 8000;
    public const int LavaTempMax = 9500;

    // Gaseous (gas giant)
    public const int GaseousGravityMin = 8000;
    public const int GaseousGravityMax = 10000;
    public const int GaseousAtmoMin = 8000;
    public const int GaseousAtmoMax = 10000;
    public const int GaseousTempMin = 2000;
    public const int GaseousTempMax = 7000;

    // Barren (no atmosphere)
    public const int BarrenGravityMin = 1000;
    public const int BarrenGravityMax = 4000;
    public const int BarrenAtmoMin = 0;
    public const int BarrenAtmoMax = 500;
    public const int BarrenTempMin = 1000;
    public const int BarrenTempMax = 3000;

    // ── Landability thresholds ──

    // Gravity range for safe unassisted landing (basis points).
    public const int SafeGravityMin = 1000;
    public const int SafeGravityMax = 8000;

    // Atmosphere range for safe unassisted landing (basis points).
    public const int SafeAtmoMin = 500;
    public const int SafeAtmoMax = 8000;

    // Tech tier required for harsh-environment landing.
    public const int HarshLandingTechTier = 1;

    // ── Type distribution weights (out of 100) ──
    // Used by PlanetInitGen to weight planet type selection per world class.
    // CORE: favors Terrestrial, HighTech
    // FRONTIER: balanced mix
    // RIM: favors harsh environments (Ice, Sand, Barren)

    // Planet type count for enum indexing.
    public const int PlanetTypeCount = 6;

    // ── Economy ──

    // Industry output multiplier for planet-specific sites (bps, 10000 = 1.0x baseline).
    public const int PlanetIndustryOutputMultiplierBps = 10000;

    // Degradation rate for planet industry sites (bps per tick).
    public const int PlanetIndustryDegradeBps = 200;

    // ── Planet industry output/input rates per specialization ──

    // Agriculture: food farm (natural source, no inputs — conservative rate to avoid flooding).
    public const int AgricultureFoodOutput = 2;

    // Mining: deep mine (enhanced ore extraction).
    public const int MiningOreOutput = 8;
    public const int MiningFuelInput = 2;

    // Manufacturing: factory (ore + fuel → metal).
    public const int ManufacturingOreInput = 5;
    public const int ManufacturingFuelInput = 1;
    public const int ManufacturingMetalOutput = 2;

    // HighTech: electronics lab (exotic_crystals + fuel → electronics).
    public const int HighTechCrystalInput = 1;
    public const int HighTechFuelInput = 1;
    public const int HighTechElectronicsOutput = 1;

    // FuelExtraction: gas processor (natural source, no inputs — conservative rate).
    public const int FuelExtractionFuelOutput = 3;

    // Buffer days for planet industry sites.
    public const int PlanetIndustryBufferDays = 2;

    // Initial inventory seed for planet specialty goods.
    public const int PlanetInitialFoodStock = 200;
    public const int PlanetInitialElectronicsStock = 50;

    // ── Star class constants ──

    // Star class count for enum indexing.
    public const int StarClassCount = 6;

    // Luminosity ranges per star class (basis points, 10000 = Sol).
    public const int ClassGLuminosityMin = 8000;
    public const int ClassGLuminosityMax = 12000;
    public const int ClassKLuminosityMin = 4000;
    public const int ClassKLuminosityMax = 8000;
    public const int ClassMLuminosityMin = 1000;
    public const int ClassMLuminosityMax = 4000;
    public const int ClassFLuminosityMin = 12000;
    public const int ClassFLuminosityMax = 18000;
    public const int ClassALuminosityMin = 18000;
    public const int ClassALuminosityMax = 30000;
    public const int ClassOLuminosityMin = 30000;
    public const int ClassOLuminosityMax = 50000;

    // Temperature modifier per 1000 luminosity bps above/below Sol baseline (10000).
    // Higher luminosity = hotter planet temperatures.
    // Applied as: tempBps += (luminosity - 10000) * TempPerThousandLuminosity / 1000
    public const int TempPerThousandLuminosity = 300;

    // Planet orbit distance range (sim units) — determines temperature modifier.
    // Closer orbits = hotter, farther = cooler.
    public const int OrbitDistanceMinU = 15;
    public const int OrbitDistanceMaxU = 50;
    // Temperature modifier per unit of orbit distance beyond baseline (25u).
    // Farther from star = cooler.
    public const int BaselineOrbitU = 25;
    public const int TempPerOrbitUnitBps = 80;
}
