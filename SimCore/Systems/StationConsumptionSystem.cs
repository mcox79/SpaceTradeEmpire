using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

/// <summary>
/// Station population consumption: every station node consumes food and fuel each
/// cadence tick, representing crew life-support. This creates permanent demand sinks
/// that prevent surplus goods from collapsing prices galaxy-wide.
/// Runs after IndustrySystem so production happens before consumption.
/// </summary>
public static class StationConsumptionSystem
{
    private sealed class Scratch
    {
        public readonly List<string> MarketKeys = new();
    }

    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();

    public static void Process(SimState state)
    {
        if (state is null) return;

        int cadence = StationConsumptionTweaksV0.CadenceTicks;
        if (cadence <= 0) return;
        if (state.Tick == 0) return; // Skip worldgen tick.
        if (state.Tick % cadence != 0) return;

        int foodQty = StationConsumptionTweaksV0.FoodPerTick * cadence;
        int fuelQty = StationConsumptionTweaksV0.FuelPerTick * cadence;
        if (foodQty <= 0 && fuelQty <= 0) return;

        var scratch = s_scratch.GetOrCreateValue(state);
        var keys = scratch.MarketKeys;
        keys.Clear();
        foreach (var k in state.Markets.Keys) keys.Add(k);
        keys.Sort(StringComparer.Ordinal);

        // Haven is the player's private starbase — not a populated station.
        var havenMarketId = state.Haven?.MarketId;

        foreach (var marketId in keys)
        {
            var market = state.Markets[marketId];
            if (market is null) continue;

            // Skip Haven market (player-owned base, no ambient population).
            if (!string.IsNullOrEmpty(havenMarketId) &&
                string.Equals(marketId, havenMarketId, StringComparison.Ordinal))
                continue;

            // Consume food (preserve zero key for market display).
            if (foodQty > 0)
            {
                int have = InventoryLedger.Get(market.Inventory, WellKnownGoodIds.Food);
                if (have > 0)
                {
                    int consume = Math.Min(have, foodQty);
                    InventoryLedger.TryRemoveMarket(market.Inventory, WellKnownGoodIds.Food, consume);
                }
                else
                {
                    if (!market.Inventory.ContainsKey(WellKnownGoodIds.Food))
                        market.Inventory[WellKnownGoodIds.Food] = 0;
                }
            }

            // Consume fuel (preserve zero key for market display).
            if (fuelQty > 0)
            {
                int have = InventoryLedger.Get(market.Inventory, WellKnownGoodIds.Fuel);
                if (have > 0)
                {
                    int consume = Math.Min(have, fuelQty);
                    InventoryLedger.TryRemoveMarket(market.Inventory, WellKnownGoodIds.Fuel, consume);
                }
                else
                {
                    if (!market.Inventory.ContainsKey(WellKnownGoodIds.Fuel))
                        market.Inventory[WellKnownGoodIds.Fuel] = 0;
                }
            }
        }
    }
}
