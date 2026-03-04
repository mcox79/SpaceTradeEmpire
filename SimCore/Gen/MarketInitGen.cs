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
                    mkt.Inventory[WellKnownGoodIds.Fuel] = 120;
                    mkt.Inventory[WellKnownGoodIds.Ore] = 500;
                    mkt.Inventory[WellKnownGoodIds.Metal] = 10;
                }
                else
                {
                    mkt.Inventory[WellKnownGoodIds.Ore] = 500;
                }

                var mine = new IndustrySite
                {
                    Id = $"mine_{i}",
                    NodeId = node.Id,
                    RecipeId = WellKnownRecipeIds.ExtractOre,
                    Inputs = new Dictionary<string, int>
                    {
                        { WellKnownGoodIds.Fuel, 1 },
                        { WellKnownGoodIds.Ore, 0 }
                    },
                    Outputs = new Dictionary<string, int> { { WellKnownGoodIds.Ore, 5 } },
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
                    mkt.Inventory[WellKnownGoodIds.Fuel] = 10;
                    mkt.Inventory[WellKnownGoodIds.Ore] = 0;
                    mkt.Inventory[WellKnownGoodIds.Metal] = 200;
                }

                var factory = new IndustrySite
                {
                    Id = $"fac_{i}",
                    NodeId = node.Id,
                    RecipeId = WellKnownRecipeIds.RefineOreToMetal,
                    Inputs = new Dictionary<string, int>
                    {
                        { WellKnownGoodIds.Ore, 10 },
                        { WellKnownGoodIds.Fuel, 1 }
                    },
                    Outputs = new Dictionary<string, int> { { WellKnownGoodIds.Metal, 5 } },
                    BufferDays = 2,
                    DegradePerDayBps = 500
                };
                state.IndustrySites.Add(factory.Id, factory);
                node.Name += " (Refinery)";
            }

            if (enableDistributionSinksV0 && isStarter && (i % 5) == 1)
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

            if (i % CatalogTweaksV0.ForgeNodeModulus == CatalogTweaksV0.ForgeNodeOffset)
            {
                mkt.Inventory[WellKnownGoodIds.HullPlating] = CatalogTweaksV0.HullPlatingInitialStock;
                var forge = new IndustrySite
                {
                    Id = $"forge_{i}",
                    NodeId = node.Id,
                    RecipeId = WellKnownRecipeIds.ForgeHullPlating,
                    Inputs = new Dictionary<string, int> { { WellKnownGoodIds.Metal, CatalogTweaksV0.ForgeMetalInput } },
                    Outputs = new Dictionary<string, int> { { WellKnownGoodIds.HullPlating, CatalogTweaksV0.ForgeHullOutput } },
                    BufferDays = CatalogTweaksV0.ForgeBufferDays,
                    DegradePerDayBps = CatalogTweaksV0.ForgeDegradeBps
                };
                state.IndustrySites.Add(forge.Id, forge);
                node.Name += " (Forge)";
            }

            state.Markets.Add(node.MarketId, mkt);
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
