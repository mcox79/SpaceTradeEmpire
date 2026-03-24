using System;
using System.Collections.Generic;
using System.Linq;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Gen;

/// <summary>
/// Market initialization: inventory seeding, economy placement, industry site creation.
/// Extracted from GalaxyGenerator for maintainability (GATE.X.HYGIENE.GALAXY_GEN_SPLIT.001).
/// </summary>
public static class MarketInitGen
{
    private const int STRUCT_ZERO_STOCK = 0; // STRUCTURAL: zero inventory baseline.
    public static void InitMarkets(SimState state, List<Node> nodesList, int starCount, bool enableDistributionSinksV0)
    {
        for (int i = 0; i < nodesList.Count; i++)
        {
            var node = nodesList[i];
            var mkt = new Market { Id = node.MarketId };

            // Seed fuel/metal/ore inventory keys for deterministic price publishing.
            mkt.Inventory[WellKnownGoodIds.Fuel]  = CatalogTweaksV0.FuelInitialStock;
            mkt.Inventory[WellKnownGoodIds.Metal] = CatalogTweaksV0.MetalInitialStock;
            mkt.Inventory[WellKnownGoodIds.Ore]   = CatalogTweaksV0.OreInitialStock;

            // GATE.S18.TRADE_GOODS.GEO_DISTRIBUTION.001: Seed organics (~40%), rare_metals (~15%).
            int geoHash = (i * 7919 + 1301) % 100; // deterministic pseudo-random 0-99
            if (geoHash < CatalogTweaksV0.OrganicsNodePct)
            {
                mkt.Inventory[WellKnownGoodIds.Organics] = CatalogTweaksV0.OrganicsInitialStock;
                node.Name += " (Agri)";
            }
            if (geoHash >= (100 - CatalogTweaksV0.RareMetalsNodePct))
            {
                mkt.Inventory[WellKnownGoodIds.RareMetals] = CatalogTweaksV0.RareMetalsInitialStock;
                node.Name += " (Rare Min)";
            }

            bool isStarter = i < Math.Min(starCount, GalaxyGenerator.StarterRegionNodeCount);

            // Deterministic fuel source v0: every 6th node has a fuel well.
            bool isFuelWell = (i % 6) == 0;
            if (isFuelWell)
            {
                mkt.Inventory[WellKnownGoodIds.Fuel] = Math.Max(mkt.Inventory[WellKnownGoodIds.Fuel], 3000);

                var well = new IndustrySite
                {
                    Id = $"well_{i}",
                    NodeId = node.Id,
                    RecipeId = "",
                    Inputs = new Dictionary<string, int>(),
                    Outputs = new Dictionary<string, int> { { WellKnownGoodIds.Fuel, 5 } },
                    BufferDays = 1,
                    DegradePerDayBps = 0
                };

                state.IndustrySites.Add(well.Id, well);
                node.Name += " (Fuel Well)";
            }

            if (i % 2 == 0)
            {
                if (isStarter)
                {
                    // Even starter nodes: ore-rich mining systems. High ore stock drives
                    // ore price down, creating profitable ore→odd-node arbitrage (141 cr/unit).
                    mkt.Inventory[WellKnownGoodIds.Fuel] = 120;
                    mkt.Inventory[WellKnownGoodIds.Ore] = 500;
                    mkt.Inventory[WellKnownGoodIds.Metal] = 10;
                    // GATE.T53.BOT.MARKET_SEED.001: Seed manufactured goods at starter nodes
                    // so haven upgrade materials are available in the first hour.
                    mkt.Inventory[WellKnownGoodIds.Composites] = CatalogTweaksV0.StarterMiningComposites;
                    mkt.Inventory[WellKnownGoodIds.Electronics] = CatalogTweaksV0.StarterMiningElectronics;
                    mkt.Inventory[WellKnownGoodIds.RareMetals] =
                        Math.Max(mkt.Inventory.GetValueOrDefault(WellKnownGoodIds.RareMetals), CatalogTweaksV0.StarterMiningRareMetals);
                    // GATE.T55.ECON.FACTORY_BUFFER.001: Seed exotic crystals for rare metals refinery bootstrap.
                    mkt.Inventory[WellKnownGoodIds.ExoticCrystals] =
                        Math.Max(mkt.Inventory.GetValueOrDefault(WellKnownGoodIds.ExoticCrystals), CatalogTweaksV0.StarterExoticCrystals);
                }
                else
                {
                    // Mining nodes: ore surplus, metal/fuel deficit → NPC traders haul ore out.
                    mkt.Inventory[WellKnownGoodIds.Ore] = CatalogTweaksV0.MiningOreBase + (geoHash * CatalogTweaksV0.MiningOreVarianceMul);
                    mkt.Inventory[WellKnownGoodIds.Metal] = CatalogTweaksV0.MiningMetalBase + (geoHash % CatalogTweaksV0.MiningMetalVarianceMod);
                    mkt.Inventory[WellKnownGoodIds.Fuel] = CatalogTweaksV0.MiningFuelBase + (geoHash % CatalogTweaksV0.MiningFuelVarianceMod);
                }

                var mine = new IndustrySite
                {
                    Id = $"mine_{i}",
                    NodeId = node.Id,
                    RecipeId = WellKnownRecipeIds.ExtractOre,
                    Inputs = new Dictionary<string, int>
                    {
                        { WellKnownGoodIds.Fuel, CatalogTweaksV0.MineFuelInput },
                        { WellKnownGoodIds.Ore, STRUCT_ZERO_STOCK }
                    },
                    Outputs = new Dictionary<string, int> { { WellKnownGoodIds.Ore, CatalogTweaksV0.MineOreOutput } },
                    BufferDays = 1,
                    DegradePerDayBps = 0
                };

                mine.Inputs.Remove(WellKnownGoodIds.Ore);

                state.IndustrySites.Add(mine.Id, mine);
                node.Name += " (Mining)";
            }
            else
            {
                if (isStarter)
                {
                    mkt.Inventory[WellKnownGoodIds.Fuel] = CatalogTweaksV0.StarterRefineryFuel;
                    mkt.Inventory[WellKnownGoodIds.Ore] = STRUCT_ZERO_STOCK;
                    mkt.Inventory[WellKnownGoodIds.Metal] = CatalogTweaksV0.StarterRefineryMetal;
                    // GATE.T53.BOT.MARKET_SEED.001: Seed manufactured goods at starter nodes.
                    mkt.Inventory[WellKnownGoodIds.Composites] = CatalogTweaksV0.StarterRefineryComposites;
                    mkt.Inventory[WellKnownGoodIds.Electronics] = CatalogTweaksV0.StarterRefineryElectronics;
                    // GATE.T55.ECON.FACTORY_BUFFER.001: Seed exotic crystals for rare metals refinery bootstrap.
                    mkt.Inventory[WellKnownGoodIds.ExoticCrystals] =
                        Math.Max(mkt.Inventory.GetValueOrDefault(WellKnownGoodIds.ExoticCrystals), CatalogTweaksV0.StarterExoticCrystals);
                }
                else
                {
                    // Refinery nodes: metal surplus, ore/fuel deficit → NPC traders haul ore in.
                    mkt.Inventory[WellKnownGoodIds.Metal] = CatalogTweaksV0.RefineryMetalBase + (geoHash * CatalogTweaksV0.RefineryMetalVarianceMul);
                    mkt.Inventory[WellKnownGoodIds.Ore] = geoHash % CatalogTweaksV0.RefineryOreVarianceMod;
                    mkt.Inventory[WellKnownGoodIds.Fuel] = CatalogTweaksV0.RefineryFuelBase + (geoHash % CatalogTweaksV0.RefineryFuelVarianceMod);
                }

                var factory = new IndustrySite
                {
                    Id = $"fac_{i}",
                    NodeId = node.Id,
                    RecipeId = WellKnownRecipeIds.RefineMetal,
                    Inputs = new Dictionary<string, int>
                    {
                        { WellKnownGoodIds.Ore, CatalogTweaksV0.FactoryOreInput },
                        { WellKnownGoodIds.Fuel, CatalogTweaksV0.FactoryFuelInput }
                    },
                    Outputs = new Dictionary<string, int> { { WellKnownGoodIds.Metal, CatalogTweaksV0.FactoryMetalOutput } },
                    BufferDays = CatalogTweaksV0.FactoryBufferDays,
                    DegradePerDayBps = CatalogTweaksV0.FactoryDegradeBps
                };
                state.IndustrySites.Add(factory.Id, factory);
                node.Name += " (Refinery)";
            }

            if (enableDistributionSinksV0 && isStarter && (i % CatalogTweaksV0.SinkPlacementModulus) == CatalogTweaksV0.SinkPlacementOffset)
            {
                var metalSink = new IndustrySite
                {
                    Id = $"sink_metal_{i}",
                    NodeId = node.Id,
                    RecipeId = "",
                    Inputs = new Dictionary<string, int> { { WellKnownGoodIds.Metal, 1 } },
                    Outputs = new Dictionary<string, int>(),
                    BufferDays = 1,
                    DegradePerDayBps = 0
                };
                state.IndustrySites.Add(metalSink.Id, metalSink);
            }

            if (i % CatalogTweaksV0.MunitionsNodeModulus == CatalogTweaksV0.MunitionsNodeOffset)
            {
                var munFac = new IndustrySite
                {
                    Id = $"munfac_{i}",
                    NodeId = node.Id,
                    RecipeId = WellKnownRecipeIds.ManufactureMunitions,
                    Inputs = new Dictionary<string, int>
                    {
                        { WellKnownGoodIds.Metal, CatalogTweaksV0.MunitionsMetalInput },
                        { WellKnownGoodIds.Fuel, CatalogTweaksV0.MunitionsFuelInput }
                    },
                    Outputs = new Dictionary<string, int> { { WellKnownGoodIds.Munitions, CatalogTweaksV0.MunitionsOutput } },
                    BufferDays = CatalogTweaksV0.MunitionsBufferDays,
                    DegradePerDayBps = CatalogTweaksV0.MunitionsDegradeBps
                };
                state.IndustrySites.Add(munFac.Id, munFac);
                node.Name += " (Munitions)";
            }

            // GATE.S7.PRODUCTION.FULL_DEPLOY.001: Deploy remaining 5 recipes.

            // ProcessFood: at agri nodes (where organics were seeded).
            bool hasOrganics = geoHash < CatalogTweaksV0.OrganicsNodePct;
            if (hasOrganics && i % 2 == 1) // Odd agri nodes get food processors (avoid overlap with mines)
            {
                state.IndustrySites[$"foodproc_{i}"] = new IndustrySite
                {
                    Id = $"foodproc_{i}",
                    NodeId = node.Id,
                    RecipeId = WellKnownRecipeIds.ProcessFood,
                    Inputs = new Dictionary<string, int>
                    {
                        { WellKnownGoodIds.Organics, CatalogTweaksV0.FoodProcessorOrganicsInput },
                        { WellKnownGoodIds.Fuel, CatalogTweaksV0.FoodProcessorFuelInput }
                    },
                    Outputs = new Dictionary<string, int> { { WellKnownGoodIds.Food, CatalogTweaksV0.FoodProcessorFoodOutput } },
                    BufferDays = 2,
                    DegradePerDayBps = CatalogTweaksV0.FoodProcessorDegradeBps,
                };
            }

            // FabricateComposites: at industrial nodes.
            if (i % CatalogTweaksV0.CompositesNodeModulus == CatalogTweaksV0.CompositesNodeOffset)
            {
                state.IndustrySites[$"compfab_{i}"] = new IndustrySite
                {
                    Id = $"compfab_{i}",
                    NodeId = node.Id,
                    RecipeId = WellKnownRecipeIds.FabricateComposites,
                    Inputs = new Dictionary<string, int>
                    {
                        { WellKnownGoodIds.Metal, CatalogTweaksV0.CompositesMetalInput },
                        { WellKnownGoodIds.Organics, CatalogTweaksV0.CompositesOrganicsInput }
                    },
                    Outputs = new Dictionary<string, int> { { WellKnownGoodIds.Composites, CatalogTweaksV0.CompositesOutput } },
                    BufferDays = 2,
                    DegradePerDayBps = CatalogTweaksV0.CompositesDegradeBps,
                };
                // GATE.T53.BOT.MARKET_SEED.001: Seed initial composites + inputs so players can buy in first hour.
                mkt.Inventory[WellKnownGoodIds.Composites] =
                    Math.Max(mkt.Inventory.GetValueOrDefault(WellKnownGoodIds.Composites), CatalogTweaksV0.CompositesBootstrapStock);
                // Seed inputs so the factory can sustain production.
                mkt.Inventory[WellKnownGoodIds.Metal] =
                    Math.Max(mkt.Inventory.GetValueOrDefault(WellKnownGoodIds.Metal), CatalogTweaksV0.CompositesBootstrapStock);
                mkt.Inventory[WellKnownGoodIds.Organics] =
                    Math.Max(mkt.Inventory.GetValueOrDefault(WellKnownGoodIds.Organics), CatalogTweaksV0.CompositesBootstrapStock);
            }

            // AssembleComponents: at tech-industrial nodes.
            if (i % CatalogTweaksV0.ComponentsNodeModulus == CatalogTweaksV0.ComponentsNodeOffset)
            {
                state.IndustrySites[$"compasm_{i}"] = new IndustrySite
                {
                    Id = $"compasm_{i}",
                    NodeId = node.Id,
                    RecipeId = WellKnownRecipeIds.AssembleComponents,
                    Inputs = new Dictionary<string, int>
                    {
                        { WellKnownGoodIds.Metal, CatalogTweaksV0.ComponentsMetalInput },
                        { WellKnownGoodIds.Electronics, CatalogTweaksV0.ComponentsElectronicsInput }
                    },
                    Outputs = new Dictionary<string, int> { { WellKnownGoodIds.Components, CatalogTweaksV0.ComponentsOutput } },
                    BufferDays = 2,
                    DegradePerDayBps = CatalogTweaksV0.ComponentsDegradeBps,
                };
            }

            // AssembleElectronics: at tech nodes near fracture borders (exotic_crystals + fuel → electronics).
            if (i % CatalogTweaksV0.ElectronicsNodeModulus == CatalogTweaksV0.ElectronicsNodeOffset)
            {
                state.IndustrySites[$"elecfab_{i}"] = new IndustrySite
                {
                    Id = $"elecfab_{i}",
                    NodeId = node.Id,
                    RecipeId = WellKnownRecipeIds.AssembleElectronics,
                    Inputs = new Dictionary<string, int>
                    {
                        { WellKnownGoodIds.ExoticCrystals, CatalogTweaksV0.ElectronicsCrystalsInput },
                        { WellKnownGoodIds.Fuel, CatalogTweaksV0.ElectronicsFuelInput }
                    },
                    Outputs = new Dictionary<string, int>
                    {
                        { WellKnownGoodIds.Electronics, CatalogTweaksV0.ElectronicsOutput }
                    },
                    BufferDays = 2,
                    DegradePerDayBps = CatalogTweaksV0.ElectronicsDegradeBps,
                };
                // Seed initial Exotic Crystals stock so the factory can bootstrap.
                mkt.Inventory[WellKnownGoodIds.ExoticCrystals] =
                    Math.Max(mkt.Inventory.GetValueOrDefault(WellKnownGoodIds.ExoticCrystals), CatalogTweaksV0.ElectronicsBootstrapCrystals);
                // GATE.T53.BOT.MARKET_SEED.001: Seed initial electronics so players can buy in first hour.
                mkt.Inventory[WellKnownGoodIds.Electronics] =
                    Math.Max(mkt.Inventory.GetValueOrDefault(WellKnownGoodIds.Electronics), CatalogTweaksV0.ElectronicsBootstrapStock);
            }

            // SalvageToMetal: at salvage yards.
            if (i % CatalogTweaksV0.SalvageNodeModulus == CatalogTweaksV0.SalvageMetalNodeOffset)
            {
                state.IndustrySites[$"salvmetal_{i}"] = new IndustrySite
                {
                    Id = $"salvmetal_{i}",
                    NodeId = node.Id,
                    RecipeId = WellKnownRecipeIds.SalvageToMetal,
                    Inputs = new Dictionary<string, int>
                    {
                        { WellKnownGoodIds.SalvagedTech, CatalogTweaksV0.SalvageTechInput }
                    },
                    Outputs = new Dictionary<string, int> { { WellKnownGoodIds.Metal, CatalogTweaksV0.SalvageMetalOutput } },
                    BufferDays = 1,
                    DegradePerDayBps = 0,
                };
            }

            // SalvageToComponents: at different salvage yards.
            if (i % CatalogTweaksV0.SalvageNodeModulus == CatalogTweaksV0.SalvageComponentsNodeOffset)
            {
                state.IndustrySites[$"salvcomp_{i}"] = new IndustrySite
                {
                    Id = $"salvcomp_{i}",
                    NodeId = node.Id,
                    RecipeId = WellKnownRecipeIds.SalvageToComponents,
                    Inputs = new Dictionary<string, int>
                    {
                        { WellKnownGoodIds.SalvagedTech, CatalogTweaksV0.SalvageTechInput }
                    },
                    Outputs = new Dictionary<string, int> { { WellKnownGoodIds.Components, CatalogTweaksV0.SalvageComponentsOutput } },
                    BufferDays = 1,
                    DegradePerDayBps = 0,
                };
            }

            // GATE.T55.SUPPLY.RARE_METALS_RECIPE.001: Rare metals refinery (ore + exotic_crystals → rare_metals).
            if (i % CatalogTweaksV0.RareMetalsRefNodeModulus == CatalogTweaksV0.RareMetalsRefNodeOffset)
            {
                state.IndustrySites[$"rmref_{i}"] = new IndustrySite
                {
                    Id = $"rmref_{i}",
                    NodeId = node.Id,
                    RecipeId = WellKnownRecipeIds.RefineRareMetals,
                    Inputs = new Dictionary<string, int>
                    {
                        { WellKnownGoodIds.Ore, CatalogTweaksV0.RareMetalsRefOreInput },
                        { WellKnownGoodIds.ExoticCrystals, CatalogTweaksV0.RareMetalsRefCrystalsInput }
                    },
                    Outputs = new Dictionary<string, int> { { WellKnownGoodIds.RareMetals, CatalogTweaksV0.RareMetalsRefOutput } },
                    BufferDays = 2,
                    DegradePerDayBps = CatalogTweaksV0.RareMetalsRefDegradeBps,
                };
                // Seed initial rare_metals stock so the refinery bootstraps.
                mkt.Inventory[WellKnownGoodIds.RareMetals] =
                    Math.Max(mkt.Inventory.GetValueOrDefault(WellKnownGoodIds.RareMetals), CatalogTweaksV0.RareMetalsRefBootstrapStock);
                node.Name += " (Rare Ref)";
            }

            state.Markets.Add(node.MarketId, mkt);
        }
    }

    /// <summary>
    /// Post-processing: ensure the player's starting station has at least one good with
    /// a profitable margin (>50 cr/unit) when sold at a station within 2 hops.
    /// Must run AFTER faction market bias and player relocation.
    /// GATE.T50.ECON.ROUTE_QUALITY.001: Extended to 2-hop neighbors for topology robustness.
    /// </summary>
    public static void GuaranteeStarterArbitrageV0(SimState state)
    {
        var startId = state.PlayerLocationNodeId;
        if (string.IsNullOrEmpty(startId)) return;
        if (!state.Markets.TryGetValue(startId, out var startMarket)) return;

        // Find node IDs within 2 hops via edges.
        var neighborIds = new List<string>();
        var hop1Set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var edge in state.Edges.Values)
        {
            if (string.Equals(edge.FromNodeId, startId, StringComparison.Ordinal))
            { neighborIds.Add(edge.ToNodeId); hop1Set.Add(edge.ToNodeId); }
            else if (string.Equals(edge.ToNodeId, startId, StringComparison.Ordinal))
            { neighborIds.Add(edge.FromNodeId); hop1Set.Add(edge.FromNodeId); }
        }
        // GATE.T50.ECON.ROUTE_QUALITY.001: Add 2-hop neighbors for broader coverage.
        foreach (var hop1Id in hop1Set)
        {
            foreach (var edge in state.Edges.Values)
            {
                string? candidate = null;
                if (string.Equals(edge.FromNodeId, hop1Id, StringComparison.Ordinal))
                    candidate = edge.ToNodeId;
                else if (string.Equals(edge.ToNodeId, hop1Id, StringComparison.Ordinal))
                    candidate = edge.FromNodeId;
                if (candidate != null
                    && !string.Equals(candidate, startId, StringComparison.Ordinal)
                    && !hop1Set.Contains(candidate))
                {
                    neighborIds.Add(candidate);
                }
            }
        }

        int minTargetMargin = Tweaks.MarketTweaksV0.MinStarterMargin;

        // Tutorial Travel_Prompt gate requires visiting an UNVISITED node.
        // Pre-visited nodes (star_0 + starter) cannot satisfy this gate, so the
        // profitable route MUST exist at an unvisited neighbor — otherwise the
        // player's first trade is guaranteed unprofitable.
        var unvisitedNeighborIds = neighborIds
            .Where(nid => !state.PlayerVisitedNodeIds.Contains(nid))
            .ToList();

        // Guarantee profitable margin for EVERY good at the starter station
        // at the BEST adjacent neighbor.  The tutorial may recommend any good,
        // so we need at least one neighbor where each good is profitable.
        // Prioritize UNVISITED neighbors so the tutorial travel gate is satisfiable.
        foreach (var goodId in startMarket.Inventory.Keys.ToList())
        {
            int buyAtStart = startMarket.GetBuyPrice(goodId);

            // Find the best UNVISITED neighbor sell price for this good.
            // Fall back to any neighbor if no unvisited ones exist.
            var candidateNeighbors = unvisitedNeighborIds.Count > 0
                ? unvisitedNeighborIds : neighborIds;
            int bestMargin = int.MinValue;
            string bestNid = "";
            foreach (var nid in candidateNeighbors)
            {
                if (!state.Markets.TryGetValue(nid, out var nm)) continue;
                if (!nm.Inventory.ContainsKey(goodId)) continue;
                int sell = nm.GetSellPrice(goodId);
                int margin = sell - buyAtStart;
                if (margin > bestMargin)
                {
                    bestMargin = margin;
                    bestNid = nid;
                }
            }

            if (bestMargin >= minTargetMargin) continue; // Already profitable somewhere.

            // Pick the best neighbor (or first unvisited) and force profitability.
            if (string.IsNullOrEmpty(bestNid) && candidateNeighbors.Count > 0)
                bestNid = candidateNeighbors[0];
            if (string.IsNullOrEmpty(bestNid)) continue;

            if (!state.Markets.TryGetValue(bestNid, out var neighborMarket)) continue;

            // Ensure the good exists at the neighbor.
            if (!neighborMarket.Inventory.ContainsKey(goodId))
                neighborMarket.Inventory[goodId] = Market.IdealStock;

            // Push start stock high (low buy price) and neighbor stock low (high sell price).
            startMarket.Inventory[goodId] = Math.Max(
                startMarket.Inventory.GetValueOrDefault(goodId),
                Tweaks.MarketTweaksV0.StarterHighStock);
            neighborMarket.Inventory[goodId] = Math.Min(
                neighborMarket.Inventory.GetValueOrDefault(goodId),
                Tweaks.MarketTweaksV0.StarterLowStock);

            // Post-validate: if still below target, force extreme stock levels.
            int newBuy = startMarket.GetBuyPrice(goodId);
            int newSell = neighborMarket.GetSellPrice(goodId);
            if (newSell - newBuy < minTargetMargin)
            {
                startMarket.Inventory[goodId] = Tweaks.MarketTweaksV0.StarterHighStock * 2;
                neighborMarket.Inventory[goodId] = 1;
            }
        }

        // Second pass: ensure buy prices at start are low enough for ≥3 unit purchases.
        // Even with guaranteed margin, high buy prices limit quantity and credit growth.
        int maxBuyPrice = Tweaks.MarketTweaksV0.MaxStarterBuyPrice;
        int buyFloorStock = Tweaks.MarketTweaksV0.StarterBuyFloorStock;
        foreach (var goodId in startMarket.Inventory.Keys.ToList())
        {
            int buyPrice = startMarket.GetBuyPrice(goodId);
            if (buyPrice > maxBuyPrice)
            {
                startMarket.Inventory[goodId] = Math.Max(
                    startMarket.Inventory.GetValueOrDefault(goodId),
                    buyFloorStock);
            }
        }
    }

    public static void ValidateCatalogBinding(SimState state, ContentRegistryLoader.ContentRegistryV0? registry)
    {
        if (registry is not { } catalogReg) return;

        var catalogGoodIds = new HashSet<string>(catalogReg.Goods.Select(g => g.Id), StringComparer.Ordinal);
        foreach (var mkt in state.Markets.Values)
        {
            foreach (var goodId in mkt.Inventory.Keys)
            {
                if (!catalogGoodIds.Contains(goodId))
                    throw new InvalidOperationException(
                        $"Market {mkt.Id} seeded good '{goodId}' is absent from the content registry.");
            }
        }
    }
}
