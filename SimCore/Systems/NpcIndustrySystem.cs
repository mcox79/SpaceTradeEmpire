using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S4.NPC_INDU.DEMAND.001: NPC industry — demand generation + production reactions.
public static class NpcIndustrySystem
{
    private sealed class Scratch
    {
        public readonly List<string> SiteIds = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();
    // GATE.S4.NPC_INDU.DEMAND.001: Process NPC industry per tick.
    // NPC industry sites consume inputs from local markets, creating demand pressure.
    public static void ProcessNpcIndustry(SimState state)
    {
        if (state == null) return;

        // Only process every N ticks (performance + realism: NPCs don't react instantly)
        if (state.Tick % NpcIndustryTweaksV0.ProcessIntervalTicks != 0) return;

        // Iterate industry sites in deterministic order
        var scratch = s_scratch.GetOrCreateValue(state);
        var siteIds = scratch.SiteIds;
        siteIds.Clear();
        foreach (var k in state.IndustrySites.Keys) siteIds.Add(k);
        siteIds.Sort(StringComparer.Ordinal);

        foreach (var siteId in siteIds)
        {
            var site = state.IndustrySites[siteId];
            if (site == null || !site.Active) continue;

            var nodeId = site.NodeId ?? "";
            if (string.IsNullOrEmpty(nodeId)) continue;

            if (!state.Markets.TryGetValue(nodeId, out var market)) continue;

            ProcessSiteDemand(state, site, market);
        }
    }

    // GATE.S4.NPC_INDU.DEMAND.001: NPC sites consume inputs from local market.
    // This creates demand pressure — lower inventory drives prices up via the inventory-based pricing model.
    private static void ProcessSiteDemand(SimState state, IndustrySite site, Market market)
    {
        if (site.Inputs == null || site.Inputs.Count == 0) return;

        // For each input good, consume a small amount from the local market
        foreach (var kv in site.Inputs)
        {
            var goodId = kv.Key ?? "";
            if (string.IsNullOrEmpty(goodId)) continue;

            int currentStock = market.Inventory.TryGetValue(goodId, out var qty) ? qty : 0;
            if (currentStock <= 0) continue;

            // Consume up to NpcDemandConsumptionUnits per cycle
            int consumed = Math.Min(currentStock, NpcIndustryTweaksV0.NpcDemandConsumptionUnits);
            market.Inventory[goodId] = currentStock - consumed;
        }
    }

    // GATE.S4.NPC_INDU.REACTION.001: NPC production reacts to shortfalls.
    // When output goods stock is low, NPC sites boost production (add to inventory).
    public static void ProcessNpcReaction(SimState state)
    {
        if (state == null) return;

        // Only process every N ticks (slower than demand)
        if (state.Tick % NpcIndustryTweaksV0.ReactionIntervalTicks != 0) return;

        var scratch2 = s_scratch.GetOrCreateValue(state);
        var siteIds2 = scratch2.SiteIds;
        siteIds2.Clear();
        foreach (var k in state.IndustrySites.Keys) siteIds2.Add(k);
        siteIds2.Sort(StringComparer.Ordinal);

        foreach (var siteId in siteIds2)
        {
            var site = state.IndustrySites[siteId];
            if (site == null || !site.Active) continue;

            var nodeId = site.NodeId ?? "";
            if (string.IsNullOrEmpty(nodeId)) continue;

            if (!state.Markets.TryGetValue(nodeId, out var market)) continue;

            if (site.Outputs == null || site.Outputs.Count == 0) continue;

            foreach (var kv in site.Outputs)
            {
                var goodId = kv.Key ?? "";
                if (string.IsNullOrEmpty(goodId)) continue;

                int currentStock = market.Inventory.TryGetValue(goodId, out var qty) ? qty : 0;

                // If stock is low, NPC industry produces more
                if (currentStock < NpcIndustryTweaksV0.LowStockThreshold)
                {
                    int productionBoost = NpcIndustryTweaksV0.ReactionProductionBoost;
                    int newStock = currentStock + productionBoost;
                    market.Inventory[goodId] = newStock;
                }
            }
        }
    }
}
