using System;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S4.TECH.SYSTEM.001: Research system — start, tick progress, complete.
public static class ResearchSystem
{
    public sealed class StartResult
    {
        public bool Success { get; set; }
        public string Reason { get; set; } = "";
    }

    public static StartResult StartResearch(SimState state, string techId)
    {
        if (string.IsNullOrEmpty(techId))
            return new StartResult { Success = false, Reason = "empty_tech_id" };

        var def = TechContentV0.GetById(techId);
        if (def == null)
            return new StartResult { Success = false, Reason = "unknown_tech" };

        if (state.Tech.UnlockedTechIds.Contains(techId))
            return new StartResult { Success = false, Reason = "already_unlocked" };

        if (state.Tech.IsResearching)
            return new StartResult { Success = false, Reason = "already_researching" };

        if (!TechContentV0.PrerequisitesMet(techId, state.Tech.UnlockedTechIds))
            return new StartResult { Success = false, Reason = "prerequisites_not_met" };

        // GATE.S4.TECH_INDUSTRIALIZE.TIER_SCALING.001: tier gating
        if (def.Tier > state.Tech.TechLevel + 1)
            return new StartResult { Success = false, Reason = "tier_locked" };

        state.Tech.CurrentResearchTechId = techId;
        state.Tech.ResearchProgressTicks = 0;
        state.Tech.ResearchTotalTicks = def.ResearchTicks;
        state.Tech.ResearchCreditsSpent = 0;

        // Log event
        state.Tech.EventLog.Add(new TechEvent
        {
            Seq = state.Tech.NextEventSeq++,
            Tick = state.Tick,
            TechId = techId,
            EventType = "Started"
        });

        return new StartResult { Success = true };
    }

    public static void ProcessResearch(SimState state)
    {
        if (!state.Tech.IsResearching) return;

        var techId = state.Tech.CurrentResearchTechId;
        var def = TechContentV0.GetById(techId);
        if (def == null)
        {
            // Content removed — cancel research
            CancelResearch(state);
            return;
        }

        // GATE.S4.TECH_INDUSTRIALIZE.TIER_SCALING.001: cost scales with tier
        int tickCost = ResearchTweaksV0.CreditCostPerTickBase * def.Tier;
        if (state.PlayerCredits < tickCost)
        {
            // Not enough credits — stall (don't progress but don't cancel)
            return;
        }

        state.PlayerCredits -= tickCost;
        state.Tech.ResearchCreditsSpent += tickCost;
        state.Tech.ResearchProgressTicks += ResearchTweaksV0.ProgressPerTick;

        if (state.Tech.ResearchProgressTicks >= state.Tech.ResearchTotalTicks)
        {
            CompleteResearch(state);
        }
    }

    public static void CompleteResearch(SimState state)
    {
        var techId = state.Tech.CurrentResearchTechId;
        if (string.IsNullOrEmpty(techId)) return;

        state.Tech.UnlockedTechIds.Add(techId);

        // Apply unlock effects
        var def = TechContentV0.GetById(techId);
        if (def != null)
        {
            foreach (var effect in def.UnlockEffects)
            {
                ApplyUnlockEffect(state, effect);
            }
        }

        // Log event
        state.Tech.EventLog.Add(new TechEvent
        {
            Seq = state.Tech.NextEventSeq++,
            Tick = state.Tick,
            TechId = techId,
            EventType = "Completed"
        });

        // Clear research state
        state.Tech.CurrentResearchTechId = "";
        state.Tech.ResearchProgressTicks = 0;
        state.Tech.ResearchTotalTicks = 0;
    }

    public static void CancelResearch(SimState state)
    {
        var techId = state.Tech.CurrentResearchTechId;
        if (string.IsNullOrEmpty(techId)) return;

        state.Tech.EventLog.Add(new TechEvent
        {
            Seq = state.Tech.NextEventSeq++,
            Tick = state.Tick,
            TechId = techId,
            EventType = "Cancelled"
        });

        state.Tech.CurrentResearchTechId = "";
        state.Tech.ResearchProgressTicks = 0;
        state.Tech.ResearchTotalTicks = 0;
    }

    private static void ApplyUnlockEffect(SimState state, string effect)
    {
        if (effect == "tech_level_increase_1")
        {
            // GATE.S4.TECH_INDUSTRIALIZE.TIER_SCALING.001: increment TechState.TechLevel
            state.Tech.TechLevel += ResearchTweaksV0.TechLevelPerFractureDrive;

            if (state.Fleets.TryGetValue("fleet_trader_1", out var fleet))
            {
                fleet.TechLevel += ResearchTweaksV0.TechLevelPerFractureDrive;
            }
        }
        // Other effects (speed_bonus_20pct, production_efficiency_10pct, etc.)
        // are resolved at point-of-use by checking Tech.UnlockedTechIds.
    }
}
