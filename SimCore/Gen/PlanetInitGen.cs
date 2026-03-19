using System;
using System.Collections.Generic;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Gen;

// GATE.S7.PLANET.GENERATION.001: Procedural star + planet generation.
// Called from GalaxyGenerator.Generate() after MarketInitGen.InitMarkets().
// All derivation is deterministic from nodeId hashes — no RNG consumption.
public static class PlanetInitGen
{
    /// <summary>
    /// For each node, generate a star and a planet. Store in state.Stars and state.Planets.
    /// Planet properties are influenced by the star's luminosity and the planet's orbital distance.
    /// </summary>
    public static void InitPlanets(SimState state, IReadOnlyList<Node> nodesList)
    {
        state.Stars.Clear();
        state.Planets.Clear();

        // Pre-compute world classes for specialization bias.
        var worldClasses = GalaxyGenerator.GetWorldClassIdByNodeIdV0(state);

        for (int i = 0; i < nodesList.Count; i++)
        {
            var node = nodesList[i];
            var nodeId = node.Id ?? "";

            // Get world class for this node (default to FRONTIER if unknown).
            string worldClass = worldClasses.TryGetValue(nodeId, out var wc) ? wc : "FRONTIER";

            // 1. Generate star.
            var star = GenerateStar(nodeId, worldClass);
            state.Stars[nodeId] = star;

            // 2. Generate planet (influenced by star).
            var planet = GeneratePlanet(nodeId, worldClass, star);
            state.Planets[nodeId] = planet;
        }

        // 3. Seed planet industries for landable planets with specializations.
        SeedPlanetIndustries(state);
    }

    private static Star GenerateStar(string nodeId, string worldClass)
    {
        uint hash = GalaxyGenerator.Fnv1a32Utf8(nodeId + "_star_class");

        // GATE.S14.STAR.STARTER_GUARANTEE.001: Player start always gets a welcoming Sol-like star.
        StarClass starClass;
        if (nodeId == "star_0")
        {
            starClass = StarClass.ClassG;
        }
        else
        {
            // Pick star class from world-class-biased distribution.
            var dist = PlanetContentV0.StarDistribution.TryGetValue(worldClass, out var d)
                ? d : PlanetContentV0.DefaultStarDistribution;
            starClass = PickWeighted(dist, hash);
        }

        // Derive luminosity from star class range.
        uint lumHash = GalaxyGenerator.Fnv1a32Utf8(nodeId + "_star_lum");
        var (lumMin, lumMax) = GetLuminosityRange(starClass);
        int luminosity = DeriveInRange(lumHash, lumMin, lumMax);

        return new Star
        {
            NodeId = nodeId,
            Class = starClass,
            LuminosityBps = luminosity,
            DisplayName = PlanetContentV0.GetStarClassName(starClass),
        };
    }

    private static Planet GeneratePlanet(string nodeId, string worldClass, Star star)
    {
        // 1. Pick planet type from world-class-biased distribution.
        // GATE.S14.STAR.STARTER_GUARANTEE.001: Start system always gets Terrestrial
        // (habitable zone, moon-capable, best visual impression).
        uint typeHash = GalaxyGenerator.Fnv1a32Utf8(nodeId + "_planet_type");
        PlanetType planetType;
        if (nodeId == "star_0")
        {
            planetType = PlanetType.Terrestrial;
        }
        else
        {
            var typeDist = PlanetContentV0.TypeDistribution.TryGetValue(worldClass, out var td)
                ? td : PlanetContentV0.DefaultTypeDistribution;
            planetType = PickWeighted(typeDist, typeHash);
        }

        // 2. Derive physical properties from type ranges.
        uint gravHash = GalaxyGenerator.Fnv1a32Utf8(nodeId + "_planet_grav");
        uint atmoHash = GalaxyGenerator.Fnv1a32Utf8(nodeId + "_planet_atmo");
        uint tempHash = GalaxyGenerator.Fnv1a32Utf8(nodeId + "_planet_temp");
        uint orbitHash = GalaxyGenerator.Fnv1a32Utf8(nodeId + "_planet_orbit");

        var (gravMin, gravMax) = GetGravityRange(planetType);
        var (atmoMin, atmoMax) = GetAtmoRange(planetType);
        var (tempMin, tempMax) = GetTempRange(planetType);

        int gravity = DeriveInRange(gravHash, gravMin, gravMax);
        int atmosphere = DeriveInRange(atmoHash, atmoMin, atmoMax);
        int baseTemp = DeriveInRange(tempHash, tempMin, tempMax);

        // 3. Apply star luminosity modifier to temperature.
        // Higher luminosity → hotter planets. Baseline is 10000 (Sol-like).
        int lumDelta = star.LuminosityBps - PlanetTweaksV0.ClassGLuminosityMin;
        int tempFromLum = (lumDelta * PlanetTweaksV0.TempPerThousandLuminosity) / PlanetTweaksV0.ClassGLuminosityMin;

        // 4. Apply orbit distance modifier to temperature.
        // Farther from star → cooler.
        int orbitDistance = DeriveInRange(orbitHash,
            PlanetTweaksV0.OrbitDistanceMinU, PlanetTweaksV0.OrbitDistanceMaxU);
        int orbitDelta = orbitDistance - PlanetTweaksV0.BaselineOrbitU;
        int tempFromOrbit = -(orbitDelta * PlanetTweaksV0.TempPerOrbitUnitBps);

        int temperature = Math.Clamp(baseTemp + tempFromLum + tempFromOrbit, 0, PlanetTweaksV0.LavaTempMax);

        // 5. Compute landability from gravity + atmosphere.
        bool baseLandable = planetType != PlanetType.Gaseous
            && gravity >= PlanetTweaksV0.SafeGravityMin
            && gravity <= PlanetTweaksV0.SafeGravityMax
            && atmosphere >= PlanetTweaksV0.SafeAtmoMin
            && atmosphere <= PlanetTweaksV0.SafeAtmoMax;

        int techTier = 0;
        if (planetType == PlanetType.Gaseous)
        {
            baseLandable = false;
            techTier = -1; // Never landable
        }
        else if (planetType == PlanetType.Lava || planetType == PlanetType.Barren)
        {
            if (!baseLandable)
            {
                // Harsh environment — tech-gated landing
                baseLandable = true; // Landable WITH tech
                techTier = PlanetTweaksV0.HarshLandingTechTier;
            }
        }

        // 6. Pick specialization from type + world class bias.
        uint specHash = GalaxyGenerator.Fnv1a32Utf8(nodeId + "_planet_spec");
        var specialization = PickSpecialization(planetType, worldClass, specHash);

        // 7. Generate display name.
        uint nameHash = GalaxyGenerator.Fnv1a32Utf8(nodeId + "_planet_name");
        string displayName = GenerateDisplayName(planetType, nameHash);

        return new Planet
        {
            NodeId = nodeId,
            Type = planetType,
            GravityBps = gravity,
            AtmosphereBps = atmosphere,
            TemperatureBps = temperature,
            Landable = baseLandable,
            LandingTechTier = techTier,
            Specialization = specialization,
            DisplayName = displayName,
        };
    }

    // ── Planet economy seeding ──

    // GATE.S7.PLANET.ECONOMY.001: Seed industry sites for landable planets with specializations.
    // Only landable planets (techTier >= 0) get industries — non-landable (gaseous) do not.
    // Industries are additive: they join the node's existing station-based industries.
    private static void SeedPlanetIndustries(SimState state)
    {
        foreach (var planet in state.Planets.Values)
        {
            if (planet.Specialization == PlanetSpecialization.None) continue;
            if (!planet.Landable) continue;
            if (planet.LandingTechTier < 0) continue; // Gas giants: never landable

            var nodeId = planet.NodeId ?? "";
            // Markets keyed by nodeId (== node.MarketId in StarNetworkGen).
            if (!state.Markets.TryGetValue(nodeId, out var market)) continue;

            var site = CreateSpecializedSite(nodeId, planet.Specialization);
            if (site is null) continue;

            state.IndustrySites[site.Id] = site;

            // Seed initial inventory for the planet's output goods.
            SeedSpecializationInventory(market, planet.Specialization);
        }
    }

    private static IndustrySite? CreateSpecializedSite(string nodeId, PlanetSpecialization spec)
    {
        return spec switch
        {
            PlanetSpecialization.Agriculture => new IndustrySite
            {
                Id = $"planet_farm_{nodeId}",
                NodeId = nodeId,
                RecipeId = "",
                Inputs = new Dictionary<string, int>(),
                Outputs = new Dictionary<string, int>
                {
                    { Content.WellKnownGoodIds.Food, PlanetTweaksV0.AgricultureFoodOutput }
                },
                BufferDays = PlanetTweaksV0.PlanetIndustryBufferDays,
                DegradePerDayBps = 0, // Natural source: no degradation (no inputs to be undersupplied on).
            },
            PlanetSpecialization.Mining => new IndustrySite
            {
                Id = $"planet_mine_{nodeId}",
                NodeId = nodeId,
                RecipeId = Content.WellKnownRecipeIds.ExtractOre,
                Inputs = new Dictionary<string, int>
                {
                    { Content.WellKnownGoodIds.Fuel, PlanetTweaksV0.MiningFuelInput }
                },
                Outputs = new Dictionary<string, int>
                {
                    { Content.WellKnownGoodIds.Ore, PlanetTweaksV0.MiningOreOutput }
                },
                BufferDays = PlanetTweaksV0.PlanetIndustryBufferDays,
                DegradePerDayBps = PlanetTweaksV0.PlanetIndustryDegradeBps,
            },
            PlanetSpecialization.Manufacturing => new IndustrySite
            {
                Id = $"planet_factory_{nodeId}",
                NodeId = nodeId,
                RecipeId = Content.WellKnownRecipeIds.RefineMetal,
                Inputs = new Dictionary<string, int>
                {
                    { Content.WellKnownGoodIds.Ore, PlanetTweaksV0.ManufacturingOreInput },
                    { Content.WellKnownGoodIds.Fuel, PlanetTweaksV0.ManufacturingFuelInput }
                },
                Outputs = new Dictionary<string, int>
                {
                    { Content.WellKnownGoodIds.Metal, PlanetTweaksV0.ManufacturingMetalOutput }
                },
                BufferDays = PlanetTweaksV0.PlanetIndustryBufferDays,
                DegradePerDayBps = PlanetTweaksV0.PlanetIndustryDegradeBps,
            },
            PlanetSpecialization.HighTech => new IndustrySite
            {
                Id = $"planet_lab_{nodeId}",
                NodeId = nodeId,
                RecipeId = Content.WellKnownRecipeIds.AssembleElectronics,
                Inputs = new Dictionary<string, int>
                {
                    { Content.WellKnownGoodIds.ExoticCrystals, PlanetTweaksV0.HighTechCrystalInput },
                    { Content.WellKnownGoodIds.Fuel, PlanetTweaksV0.HighTechFuelInput }
                },
                Outputs = new Dictionary<string, int>
                {
                    { Content.WellKnownGoodIds.Electronics, PlanetTweaksV0.HighTechElectronicsOutput }
                },
                BufferDays = PlanetTweaksV0.PlanetIndustryBufferDays,
                DegradePerDayBps = PlanetTweaksV0.PlanetIndustryDegradeBps,
            },
            PlanetSpecialization.FuelExtraction => new IndustrySite
            {
                Id = $"planet_fuel_{nodeId}",
                NodeId = nodeId,
                RecipeId = "",
                Inputs = new Dictionary<string, int>(),
                Outputs = new Dictionary<string, int>
                {
                    { Content.WellKnownGoodIds.Fuel, PlanetTweaksV0.FuelExtractionFuelOutput }
                },
                BufferDays = PlanetTweaksV0.PlanetIndustryBufferDays,
                DegradePerDayBps = 0, // Natural source: no degradation.
            },
            _ => null,
        };
    }

    private static void SeedSpecializationInventory(Market market, PlanetSpecialization spec)
    {
        switch (spec)
        {
            case PlanetSpecialization.Agriculture:
                if (!market.Inventory.ContainsKey(Content.WellKnownGoodIds.Food))
                    market.Inventory[Content.WellKnownGoodIds.Food] = 0;
                market.Inventory[Content.WellKnownGoodIds.Food] =
                    Math.Max(market.Inventory[Content.WellKnownGoodIds.Food], PlanetTweaksV0.PlanetInitialFoodStock);
                break;
            case PlanetSpecialization.Mining:
                // Ore already seeded by MarketInitGen.
                break;
            case PlanetSpecialization.Manufacturing:
                // Metal already seeded by MarketInitGen.
                break;
            case PlanetSpecialization.HighTech:
                if (!market.Inventory.ContainsKey(Content.WellKnownGoodIds.Electronics))
                    market.Inventory[Content.WellKnownGoodIds.Electronics] = 0;
                market.Inventory[Content.WellKnownGoodIds.Electronics] =
                    Math.Max(market.Inventory[Content.WellKnownGoodIds.Electronics], PlanetTweaksV0.PlanetInitialElectronicsStock);
                break;
            case PlanetSpecialization.FuelExtraction:
                // Fuel already seeded by MarketInitGen.
                break;
        }
    }

    // ── Helpers ──

    private static PlanetSpecialization PickSpecialization(PlanetType type, string worldClass, uint hash)
    {
        if (!PlanetContentV0.SpecializationAffinity.TryGetValue(type, out var affinities))
            return PlanetSpecialization.None;

        // Build weighted list with world class bonus.
        var weighted = new List<(PlanetSpecialization Spec, int Weight)>(affinities.Count);
        foreach (var (spec, weight) in affinities)
        {
            int bonus = PlanetContentV0.GetWorldClassBonus(worldClass, spec);
            weighted.Add((spec, weight + bonus));
        }

        return PickWeighted(weighted, hash);
    }

    private static T PickWeighted<T>(IReadOnlyList<(T Item, int Weight)> options, uint hash)
    {
        int totalWeight = 0;
        for (int i = 0; i < options.Count; i++)
            totalWeight += options[i].Weight;

        if (totalWeight <= 0) return options[0].Item;

        int roll = (int)(hash % (uint)totalWeight);
        int cumulative = 0;
        for (int i = 0; i < options.Count; i++)
        {
            cumulative += options[i].Weight;
            if (roll < cumulative)
                return options[i].Item;
        }
        return options[options.Count - 1].Item;
    }

    private static int DeriveInRange(uint hash, int min, int max)
    {
        if (max <= min) return min;
        return min + (int)(hash % (uint)(max - min + 1));
    }

    private static string GenerateDisplayName(PlanetType type, uint hash)
    {
        // Thematic name pools per planet type — avoids "Terrestrial World Delta-5" dev feel.
        string[] terrestrialNames = { "Haven", "Verdance", "Prospect", "Meridian", "Solace",
                                       "Crestfall", "Beacon", "Ashford", "Windreach", "Thornfield",
                                       "Glenmore", "Copperleaf", "Millhaven", "Ironwick", "Dawnrise" };
        string[] iceNames = { "Frostholm", "Borealis", "Glacier Peak", "Rimward", "Crystalis",
                               "Snowdrift", "Permafrost", "Iceveil", "Wintermere", "Hailstone",
                               "Shardfall", "Coldreach", "Pale Summit", "Neverspring", "Tundrine" };
        string[] sandNames = { "Dusthaven", "Scorchwind", "Red Mesa", "Sandrift", "Aridspire",
                                "Dunebreak", "Sunstone", "Mirage Flat", "Amber Reach", "Bleachrock",
                                "Caldera", "Oasis Point", "Siltspur", "Drymouth", "Cinderfield" };
        string[] lavaNames = { "Magmaris", "Ashfall", "Emberpeak", "Crucible", "Pyrrhus",
                                "Cindervault", "Forge Mouth", "Brimstone", "Sulphur Rift", "Hearthcore",
                                "Obsidian Deep", "Smoldervein", "Flamecrest", "Ignatius", "Scoria" };
        string[] gasNames = { "Tempest", "Titan's Eye", "Stormveil", "Chromos", "Aurealis",
                               "Zephyria", "Cloudrift", "Thundermaw", "Bellows", "Maelstrom",
                               "Gasgiant Minor", "Iridium Deep", "Cyclonia", "Vortessa", "Nimbus" };
        string[] barrenNames = { "Dustrock", "Graymere", "Hollow Ridge", "Craterfield", "Ashpit",
                                  "Bleakstone", "Dead Reach", "Scrapyard", "Rubble Point", "Ironflat",
                                  "Pockmark", "Slagheap", "Wastrel", "Gritstone", "Desolace" };

        string[] pool = type switch
        {
            PlanetType.Terrestrial => terrestrialNames,
            PlanetType.Ice => iceNames,
            PlanetType.Sand => sandNames,
            PlanetType.Lava => lavaNames,
            PlanetType.Gaseous => gasNames,
            PlanetType.Barren => barrenNames,
            _ => terrestrialNames,
        };

        int idx = (int)(hash % (uint)pool.Length);
        // Optional numeral suffix for uniqueness (only if hash collision likely).
        int numeral = (int)((hash / (uint)pool.Length) % 9) + 1;
        return numeral <= 1 ? pool[idx] : $"{pool[idx]} {numeral}";
    }

    // ── Property range lookups ──

    private static (int Min, int Max) GetGravityRange(PlanetType type) => type switch
    {
        PlanetType.Terrestrial => (PlanetTweaksV0.TerrestrialGravityMin, PlanetTweaksV0.TerrestrialGravityMax),
        PlanetType.Ice => (PlanetTweaksV0.IceGravityMin, PlanetTweaksV0.IceGravityMax),
        PlanetType.Sand => (PlanetTweaksV0.SandGravityMin, PlanetTweaksV0.SandGravityMax),
        PlanetType.Lava => (PlanetTweaksV0.LavaGravityMin, PlanetTweaksV0.LavaGravityMax),
        PlanetType.Gaseous => (PlanetTweaksV0.GaseousGravityMin, PlanetTweaksV0.GaseousGravityMax),
        PlanetType.Barren => (PlanetTweaksV0.BarrenGravityMin, PlanetTweaksV0.BarrenGravityMax),
        _ => (PlanetTweaksV0.TerrestrialGravityMin, PlanetTweaksV0.TerrestrialGravityMax),
    };

    private static (int Min, int Max) GetAtmoRange(PlanetType type) => type switch
    {
        PlanetType.Terrestrial => (PlanetTweaksV0.TerrestrialAtmoMin, PlanetTweaksV0.TerrestrialAtmoMax),
        PlanetType.Ice => (PlanetTweaksV0.IceAtmoMin, PlanetTweaksV0.IceAtmoMax),
        PlanetType.Sand => (PlanetTweaksV0.SandAtmoMin, PlanetTweaksV0.SandAtmoMax),
        PlanetType.Lava => (PlanetTweaksV0.LavaAtmoMin, PlanetTweaksV0.LavaAtmoMax),
        PlanetType.Gaseous => (PlanetTweaksV0.GaseousAtmoMin, PlanetTweaksV0.GaseousAtmoMax),
        PlanetType.Barren => (PlanetTweaksV0.BarrenAtmoMin, PlanetTweaksV0.BarrenAtmoMax),
        _ => (PlanetTweaksV0.TerrestrialAtmoMin, PlanetTweaksV0.TerrestrialAtmoMax),
    };

    private static (int Min, int Max) GetTempRange(PlanetType type) => type switch
    {
        PlanetType.Terrestrial => (PlanetTweaksV0.TerrestrialTempMin, PlanetTweaksV0.TerrestrialTempMax),
        PlanetType.Ice => (PlanetTweaksV0.IceTempMin, PlanetTweaksV0.IceTempMax),
        PlanetType.Sand => (PlanetTweaksV0.SandTempMin, PlanetTweaksV0.SandTempMax),
        PlanetType.Lava => (PlanetTweaksV0.LavaTempMin, PlanetTweaksV0.LavaTempMax),
        PlanetType.Gaseous => (PlanetTweaksV0.GaseousTempMin, PlanetTweaksV0.GaseousTempMax),
        PlanetType.Barren => (PlanetTweaksV0.BarrenTempMin, PlanetTweaksV0.BarrenTempMax),
        _ => (PlanetTweaksV0.TerrestrialTempMin, PlanetTweaksV0.TerrestrialTempMax),
    };

    private static (int Min, int Max) GetLuminosityRange(StarClass cls) => cls switch
    {
        StarClass.ClassG => (PlanetTweaksV0.ClassGLuminosityMin, PlanetTweaksV0.ClassGLuminosityMax),
        StarClass.ClassK => (PlanetTweaksV0.ClassKLuminosityMin, PlanetTweaksV0.ClassKLuminosityMax),
        StarClass.ClassM => (PlanetTweaksV0.ClassMLuminosityMin, PlanetTweaksV0.ClassMLuminosityMax),
        StarClass.ClassF => (PlanetTweaksV0.ClassFLuminosityMin, PlanetTweaksV0.ClassFLuminosityMax),
        StarClass.ClassA => (PlanetTweaksV0.ClassALuminosityMin, PlanetTweaksV0.ClassALuminosityMax),
        StarClass.ClassO => (PlanetTweaksV0.ClassOLuminosityMin, PlanetTweaksV0.ClassOLuminosityMax),
        _ => (PlanetTweaksV0.ClassGLuminosityMin, PlanetTweaksV0.ClassGLuminosityMax),
    };
}
