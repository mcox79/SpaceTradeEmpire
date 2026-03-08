using System;
using System.Linq;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S7.WARFRONT.DEMAND_SHOCK.001: Wartime demand multipliers.
// Factions at war consume goods at elevated rates, draining market inventory
// at contested nodes to simulate wartime scarcity.
public static class WarfrontDemandSystem
{
    public static void Process(SimState state)
    {
        if (state.Warfronts is null || state.Warfronts.Count == 0) return;

        foreach (var wf in state.Warfronts.Values.OrderBy(w => w.Id, StringComparer.Ordinal))
        {
            if (wf.Intensity == WarfrontIntensity.Peace) continue;

            int intensity = (int)wf.Intensity;

            // For each contested node, consume war goods from market.
            foreach (var nodeId in wf.ContestedNodeIds)
            {
                if (!state.Nodes.TryGetValue(nodeId, out var node)) continue;
                if (string.IsNullOrEmpty(node.MarketId)) continue;
                if (!state.Markets.TryGetValue(node.MarketId, out var market)) continue;

                // War consumption: drain goods proportional to intensity.
                ConsumeWarGood(market, WellKnownGoodIds.Munitions, intensity, WarfrontTweaksV0.MunitionsDemandMultiplierPct);
                ConsumeWarGood(market, WellKnownGoodIds.Composites, intensity, WarfrontTweaksV0.CompositesDemandMultiplierPct);
                ConsumeWarGood(market, WellKnownGoodIds.Fuel, intensity, WarfrontTweaksV0.FuelDemandMultiplierPct);
            }
        }
    }

    // Consume qty = (multiplierPct - 100) * intensity / (TotalWarIntensity * 100) per tick.
    // At TotalWar (intensity=4), full multiplier applies.
    // Uses integer arithmetic for determinism.
    private static void ConsumeWarGood(Market market, string goodId, int intensity, int multiplierPct)
    {
        if (!market.Inventory.TryGetValue(goodId, out var current)) return;
        if (current <= 0) return;

        // Extra demand = base * (mult - 100) * intensity / (4 * 100)
        // Since we don't know "base", consume a fixed amount scaled by intensity.
        // STRUCTURAL: 1 unit per intensity level above peace, scaled by multiplier tier.
        int drain = (multiplierPct - WarfrontTweaksV0.DefaultDemandMultiplierPct) * intensity / (WarfrontTweaksV0.TotalWarIntensity * WarfrontTweaksV0.DefaultDemandMultiplierPct);
        if (drain <= 0) return;

        market.Inventory[goodId] = Math.Max(0, current - drain);
    }
}
