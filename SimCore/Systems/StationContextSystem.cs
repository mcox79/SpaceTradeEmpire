using System;
using System.Collections.Generic;
using System.Linq;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S9.SYSTEMIC.STATION_CONTEXT.001: Per-station economic context.
// Classifies each market into one of: SHORTAGE, OPPORTUNITY, WARFRONT_DEMAND, CALM.

public enum StationContextType
{
    Calm = 0,
    Shortage = 1,
    Opportunity = 2,
    WarfrontDemand = 3
}

public sealed class StationContext
{
    public string NodeId { get; set; } = "";
    public StationContextType ContextType { get; set; } = StationContextType.Calm;
    public string PrimaryGoodId { get; set; } = "";
    public int LastUpdateTick { get; set; }
}

public static class StationContextSystem
{
    /// <summary>
    /// Process all markets and update station contexts in SimState.
    /// Called from SimKernel.Step at the configured interval.
    /// </summary>
    public static void Process(SimState state)
    {
        if (state.Tick % StationContextTweaksV0.ContextUpdateIntervalTicks != 0) return;

        state.StationContexts ??= new Dictionary<string, StationContext>(StringComparer.Ordinal);

        foreach (var kvp in state.Markets)
        {
            var market = kvp.Value;
            var ctx = ComputeContext(market, state);
            ctx.NodeId = kvp.Key;
            ctx.LastUpdateTick = state.Tick;
            state.StationContexts[kvp.Key] = ctx;
        }
    }

    /// <summary>
    /// Compute the economic context for a single market.
    /// Priority: WarfrontDemand > Shortage > Opportunity > Calm.
    /// </summary>
    public static StationContext ComputeContext(Market market, SimState state)
    {
        var ctx = new StationContext();

        // Check for warfront demand (highest priority).
        if (state.Warfronts != null)
        {
            foreach (var wf in state.Warfronts.Values)
            {
                if (wf.Intensity >= WarfrontIntensity.Skirmish &&
                    wf.ContestedNodeIds != null &&
                    wf.ContestedNodeIds.Contains(market.Id))
                {
                    ctx.ContextType = StationContextType.WarfrontDemand;
                    ctx.PrimaryGoodId = StationContextTweaksV0.WarfrontDemandGoodId;
                    return ctx;
                }
            }
        }

        // Check for shortage (any good below threshold).
        string shortageGood = "";
        int worstShortageRatio = StationContextTweaksV0.ShortageThresholdPct;
        foreach (var inv in market.Inventory)
        {
            if (inv.Value <= 0) continue;
            int ratioPct = inv.Value * 100 / Market.IdealStock; // STRUCTURAL: 100 = pct calc
            if (ratioPct < worstShortageRatio)
            {
                worstShortageRatio = ratioPct;
                shortageGood = inv.Key;
            }
        }
        if (!string.IsNullOrEmpty(shortageGood))
        {
            ctx.ContextType = StationContextType.Shortage;
            ctx.PrimaryGoodId = shortageGood;
            return ctx;
        }

        // Check for opportunity (any good priced well above base).
        string oppGood = "";
        int bestPremium = StationContextTweaksV0.OpportunityPremiumPct;
        foreach (var inv in market.Inventory)
        {
            int sellPrice = market.GetSellPrice(inv.Key);
            int premiumPct = (sellPrice - Market.BasePrice) * 100 / Market.BasePrice; // STRUCTURAL: 100 = pct calc
            if (premiumPct > bestPremium)
            {
                bestPremium = premiumPct;
                oppGood = inv.Key;
            }
        }
        if (!string.IsNullOrEmpty(oppGood))
        {
            ctx.ContextType = StationContextType.Opportunity;
            ctx.PrimaryGoodId = oppGood;
            return ctx;
        }

        ctx.ContextType = StationContextType.Calm;
        return ctx;
    }
}
