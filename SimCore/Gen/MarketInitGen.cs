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

            state.Markets.Add(node.MarketId, mkt);
        }
    }

    /// <summary>
    /// Post-processing: ensure the player's starting station has at least one good with
    /// a profitable margin (>50 cr/unit) when sold at an adjacent station.
    /// Must run AFTER faction market bias and player relocation.
    /// </summary>
    public static void GuaranteeStarterArbitrageV0(SimState state)
    {
        var startId = state.PlayerLocationNodeId;
        if (string.IsNullOrEmpty(startId)) return;
        if (!state.Markets.TryGetValue(startId, out var startMarket)) return;

        // Find adjacent node IDs via edges.
        var neighborIds = new List<string>();
        foreach (var edge in state.Edges.Values)
        {
            if (string.Equals(edge.FromNodeId, startId, StringComparison.Ordinal))
                neighborIds.Add(edge.ToNodeId);
            else if (string.Equals(edge.ToNodeId, startId, StringComparison.Ordinal))
                neighborIds.Add(edge.FromNodeId);
        }

        int minTargetMargin = Tweaks.MarketTweaksV0.MinStarterMargin;

        // Find the cheapest good at the starting market — this is what new players buy.
        string cheapestGood = "";
        int cheapestPrice = int.MaxValue;
        foreach (var goodId in startMarket.Inventory.Keys)
        {
            int buyPrice = startMarket.GetBuyPrice(goodId);
            if (buyPrice < cheapestPrice)
            {
                cheapestPrice = buyPrice;
                cheapestGood = goodId;
            }
        }

        if (string.IsNullOrEmpty(cheapestGood)) return;

        // Guarantee profitable margin for the cheapest good at EVERY neighbor.
        // A new player will pick the cheapest good and travel to any neighbor.
        foreach (var nid in neighborIds)
        {
            if (!state.Markets.TryGetValue(nid, out var neighborMarket)) continue;

            // Ensure the good exists at the neighbor.
            if (!neighborMarket.Inventory.ContainsKey(cheapestGood))
                neighborMarket.Inventory[cheapestGood] = Market.IdealStock;

            int buyAtStart = startMarket.GetBuyPrice(cheapestGood);
            int sellAtNeighbor = neighborMarket.GetSellPrice(cheapestGood);
            int margin = sellAtNeighbor - buyAtStart;

            if (margin >= minTargetMargin) continue; // Already profitable here.

            // Push start stock high (low buy price) and neighbor stock low (high sell price).
            startMarket.Inventory[cheapestGood] = Math.Max(
                startMarket.Inventory.GetValueOrDefault(cheapestGood),
                Tweaks.MarketTweaksV0.StarterHighStock);
            neighborMarket.Inventory[cheapestGood] = Math.Min(
                neighborMarket.Inventory.GetValueOrDefault(cheapestGood),
                Tweaks.MarketTweaksV0.StarterLowStock);

            // Post-validate: if still below target, force extreme stock levels.
            int newBuy = startMarket.GetBuyPrice(cheapestGood);
            int newSell = neighborMarket.GetSellPrice(cheapestGood);
            if (newSell - newBuy < minTargetMargin)
            {
                startMarket.Inventory[cheapestGood] = Tweaks.MarketTweaksV0.StarterHighStock * 2;
                neighborMarket.Inventory[cheapestGood] = 1;
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
