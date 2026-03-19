using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S7.WARFRONT.DEMAND_SHOCK.001: Wartime demand multipliers.
// Factions at war consume goods at elevated rates, draining market inventory
// at contested nodes to simulate wartime scarcity.
// GATE.S7.SUPPLY.DELIVERY_LEDGER.001: Track cumulative supply deliveries per warfront+good.
public static class WarfrontDemandSystem
{
    private sealed class Scratch
    {
        public readonly List<string> SortedWarfrontIds = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();

    public static void Process(SimState state)
    {
        if (state.Warfronts is null || state.Warfronts.Count == 0) return;

        var scratch = s_scratch.GetOrCreateValue(state);
        var sortedIds = scratch.SortedWarfrontIds;
        sortedIds.Clear();
        foreach (var k in state.Warfronts.Keys) sortedIds.Add(k);
        sortedIds.Sort(StringComparer.Ordinal);
        foreach (var wfId in sortedIds)
        {
            var wf = state.Warfronts[wfId];
            if (wf.Intensity == WarfrontIntensity.Peace) continue;

            int intensity = (int)wf.Intensity;

            // For each contested node, consume war goods from market.
            foreach (var nodeId in wf.ContestedNodeIds)
            {
                if (!state.Nodes.TryGetValue(nodeId, out var node)) continue;
                if (string.IsNullOrEmpty(node.MarketId)) continue;
                if (!state.Markets.TryGetValue(node.MarketId, out var market)) continue;

                // War consumption: drain goods proportional to intensity.
                // GATE.S7.SUPPLY.DELIVERY_LEDGER.001: Record consumed amounts in supply ledger.
                RecordAndConsumeWarGood(state, wf.Id, market, WellKnownGoodIds.Munitions, intensity, WarfrontTweaksV0.MunitionsDemandMultiplierPct);
                RecordAndConsumeWarGood(state, wf.Id, market, WellKnownGoodIds.Composites, intensity, WarfrontTweaksV0.CompositesDemandMultiplierPct);
                RecordAndConsumeWarGood(state, wf.Id, market, WellKnownGoodIds.Fuel, intensity, WarfrontTweaksV0.FuelDemandMultiplierPct);
            }

            // GATE.S7.SUPPLY.WARFRONT_SHIFT.001: Check if supply deliveries exceed shift threshold.
            CheckSupplyShift(state, wf);
        }
    }

    // GATE.S7.SUPPLY.WARFRONT_SHIFT.001: When total deliveries exceed threshold,
    // defender intensity drops by 1 (supply effort reduces war pressure).
    private static void CheckSupplyShift(SimState state, WarfrontState wf)
    {
        if (!state.WarSupplyLedger.TryGetValue(wf.Id, out var goodLedger)) return;

        int totalDeliveries = 0; // STRUCTURAL: accumulator init
        foreach (var kv in goodLedger)
            totalDeliveries += kv.Value;

        if (totalDeliveries >= WarfrontTweaksV0.SupplyShiftThreshold)
        {
            // Shift intensity down by 1 (supplies are meeting demand).
            if (wf.Intensity > WarfrontIntensity.Peace)
            {
                int oldIntensity = (int)wf.Intensity;
                wf.Intensity = (WarfrontIntensity)(oldIntensity - 1); // STRUCTURAL: step -1

                // GATE.X.PRESSURE_INJECT.WARFRONT.001: Inject pressure on intensity shift.
                PressureSystem.InjectDelta(state, "warfront_demand", "intensity_shift",
                    PressureTweaksV0.WarfrontShiftMagnitude, targetRef: wf.Id);
            }

            // Reset ledger for this warfront.
            goodLedger.Clear();
        }
    }

    // Consume qty = (multiplierPct - 100) * intensity / (TotalWarIntensity * 100) per tick.
    // At TotalWar (intensity=4), full multiplier applies.
    // Uses integer arithmetic for determinism.
    // GATE.S7.SUPPLY.DELIVERY_LEDGER.001: Also records consumed amount in WarSupplyLedger.
    private static void RecordAndConsumeWarGood(SimState state, string warfrontId, Market market, string goodId, int intensity, int multiplierPct)
    {
        if (!market.Inventory.TryGetValue(goodId, out var current)) return;
        if (current <= 0) return;

        // Extra demand = base * (mult - 100) * intensity / (4 * 100)
        // Since we don't know "base", consume a fixed amount scaled by intensity.
        // STRUCTURAL: 1 unit per intensity level above peace, scaled by multiplier tier.
        int drain = (multiplierPct - WarfrontTweaksV0.DefaultDemandMultiplierPct) * intensity / (WarfrontTweaksV0.TotalWarIntensity * WarfrontTweaksV0.DefaultDemandMultiplierPct);
        if (drain <= 0) return;

        int actualDrain = Math.Min(drain, current);
        market.Inventory[goodId] = current - actualDrain;

        // Record in supply ledger.
        if (actualDrain > 0)
        {
            if (!state.WarSupplyLedger.TryGetValue(warfrontId, out var goodLedger))
            {
                goodLedger = new Dictionary<string, int>(StringComparer.Ordinal);
                state.WarSupplyLedger[warfrontId] = goodLedger;
            }
            goodLedger.TryGetValue(goodId, out var prev);
            goodLedger[goodId] = prev + actualDrain;
        }
    }
}
