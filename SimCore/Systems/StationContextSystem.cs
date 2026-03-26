using System;
using System.Collections.Generic;
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
    // GATE.T48.TEMPLATE.CONTEXT_SURFACE.001: Return up to 2 matching templates for a station.
    // Matches based on archetype (Supply if shortage, Combat if warfront nearby, etc.),
    // player reputation meets template requirements, and template not already active.
    public static List<(string templateId, string displayName, Content.MissionTemplateContentV0.Archetype archetype, string situationDesc)>
        GetContextualTemplates(SimState state, string nodeId)
    {
        var result = new List<(string, string, Content.MissionTemplateContentV0.Archetype, string)>();
        if (state is null || string.IsNullOrEmpty(nodeId)) return result;

        // Determine station context type.
        StationContextType ctxType = StationContextType.Calm;
        string primaryGood = "";
        if (state.StationContexts != null && state.StationContexts.TryGetValue(nodeId, out var ctx))
        {
            ctxType = ctx.ContextType;
            primaryGood = ctx.PrimaryGoodId;
        }

        // Map context type to preferred archetype(s).
        var preferredArchetypes = new List<Content.MissionTemplateContentV0.Archetype>();
        switch (ctxType)
        {
            case StationContextType.Shortage:
                preferredArchetypes.Add(Content.MissionTemplateContentV0.Archetype.Supply);
                preferredArchetypes.Add(Content.MissionTemplateContentV0.Archetype.Explore);
                break;
            case StationContextType.WarfrontDemand:
                preferredArchetypes.Add(Content.MissionTemplateContentV0.Archetype.Combat);
                preferredArchetypes.Add(Content.MissionTemplateContentV0.Archetype.Supply);
                break;
            case StationContextType.Opportunity:
                preferredArchetypes.Add(Content.MissionTemplateContentV0.Archetype.Supply);
                preferredArchetypes.Add(Content.MissionTemplateContentV0.Archetype.Politics);
                break;
            default: // Calm
                preferredArchetypes.Add(Content.MissionTemplateContentV0.Archetype.Explore);
                preferredArchetypes.Add(Content.MissionTemplateContentV0.Archetype.Politics);
                break;
        }

        foreach (var template in Content.MissionTemplateContentV0.AllTemplates)
        {
            if (result.Count >= 2) break; // STRUCTURAL: max 2 results

            // Archetype must match context.
            if (!preferredArchetypes.Contains(template.Archetype)) continue;

            // Template not already active.
            if (state.ActiveTemplateMissionIds != null)
            {
                bool alreadyActive = false;
                foreach (var id in state.ActiveTemplateMissionIds)
                {
                    if (id.Contains(template.TemplateId, StringComparison.Ordinal))
                    {
                        alreadyActive = true;
                        break;
                    }
                }
                if (alreadyActive) continue;
            }

            // Rep requirement check.
            if (template.RequiredRepTier >= 0 && !string.IsNullOrEmpty(template.FactionId))
            {
                var playerTier = ReputationSystem.GetRepTier(state, template.FactionId);
                if ((int)playerTier > template.RequiredRepTier) continue;
            }

            // Build situation description based on context.
            string situationDesc = ctxType switch
            {
                StationContextType.Shortage => $"Supply shortage of {primaryGood} at this station",
                StationContextType.WarfrontDemand => "Warfront activity detected nearby",
                StationContextType.Opportunity => $"Market opportunity for {primaryGood} here",
                _ => "Quiet sector — good time for exploration",
            };

            result.Add((template.TemplateId, template.DisplayName, template.Archetype, situationDesc));
        }

        return result;
    }

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

        // Single-pass: check shortage and opportunity simultaneously.
        string shortageGood = "";
        int worstShortageRatio = StationContextTweaksV0.ShortageThresholdPct;
        string oppGood = "";
        int bestPremium = StationContextTweaksV0.OpportunityPremiumPct;
        foreach (var inv in market.Inventory)
        {
            // Shortage check.
            if (inv.Value > 0)
            {
                int ratioPct = inv.Value * 100 / Market.IdealStock; // STRUCTURAL: 100 = pct calc
                if (ratioPct < worstShortageRatio)
                {
                    worstShortageRatio = ratioPct;
                    shortageGood = inv.Key;
                }
            }

            // Opportunity check.
            int sellPrice = market.GetSellPrice(inv.Key);
            int premiumPct = (sellPrice - Market.BasePrice) * 100 / Market.BasePrice; // STRUCTURAL: 100 = pct calc
            if (premiumPct > bestPremium)
            {
                bestPremium = premiumPct;
                oppGood = inv.Key;
            }
        }

        // Priority: Shortage > Opportunity > Calm.
        if (!string.IsNullOrEmpty(shortageGood))
        {
            ctx.ContextType = StationContextType.Shortage;
            ctx.PrimaryGoodId = shortageGood;
            return ctx;
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
