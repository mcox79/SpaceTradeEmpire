using System;
using System.Collections.Generic;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S4.TECH.SYSTEM.001: Research system — start, tick progress, complete.
// GATE.S8.RESEARCH_SUSTAIN.SYSTEM.001: Production-sustained research (goods consumption).
public static class ResearchSystem
{
    public sealed class StartResult
    {
        public bool Success { get; set; }
        public string Reason { get; set; } = "";
    }

    // GATE.S8.RESEARCH_SUSTAIN.SYSTEM.001: Overload with nodeId for goods-sustained research.
    public static StartResult StartResearch(SimState state, string techId, string nodeId)
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

        // Validate nodeId has a market if specified (needed for goods consumption).
        // If nodeId is empty, research proceeds as credit-only (legacy/fallback behavior).
        if (!string.IsNullOrEmpty(nodeId) && !state.Markets.ContainsKey(nodeId))
            return new StartResult { Success = false, Reason = "invalid_research_node" };

        state.Tech.CurrentResearchTechId = techId;
        state.Tech.ResearchProgressTicks = 0;
        state.Tech.ResearchTotalTicks = def.ResearchTicks;
        state.Tech.ResearchCreditsSpent = 0;
        state.Tech.ResearchNodeId = nodeId ?? "";
        state.Tech.SustainAccumulatorTicks = 0;
        state.Tech.StallTicks = 0;
        state.Tech.StallReason = "";

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

    // Backwards-compatible overload (nodeId defaults to empty — no goods consumption).
    public static StartResult StartResearch(SimState state, string techId)
        => StartResearch(state, techId, "");

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

        // 1. Credit cost per tick (reduced, still exists).
        int tickCost = ResearchTweaksV0.CreditCostPerTickBase * def.Tier;
        if (state.PlayerCredits < tickCost)
        {
            state.Tech.StallTicks++;
            state.Tech.StallReason = "insufficient_credits";
            return;
        }

        // 2. GATE.S8.RESEARCH_SUSTAIN.SYSTEM.001: Sustain cycle — consume goods periodically.
        if (def.SustainInputs.Count > 0
            && !string.IsNullOrEmpty(state.Tech.ResearchNodeId)
            && state.Markets.TryGetValue(state.Tech.ResearchNodeId, out var market))
        {
            state.Tech.SustainAccumulatorTicks++;
            int interval = def.SustainIntervalTicks > 0
                ? def.SustainIntervalTicks
                : ResearchTweaksV0.DefaultSustainIntervalTicks;

            if (state.Tech.SustainAccumulatorTicks >= interval)
            {
                // Check all inputs are available — sort keys for determinism.
                var inputKeys = new List<string>(def.SustainInputs.Keys);
                inputKeys.Sort(StringComparer.Ordinal);

                foreach (var goodId in inputKeys)
                {
                    int required = def.SustainInputs[goodId];
                    int available = InventoryLedger.Get(market.Inventory, goodId);
                    if (available < required)
                    {
                        state.Tech.StallTicks++;
                        state.Tech.StallReason = "missing_good:" + goodId;
                        // All-or-nothing: don't consume anything partial.
                        return;
                    }
                }

                // Consume all inputs.
                foreach (var goodId in inputKeys)
                {
                    int qty = def.SustainInputs[goodId];
                    InventoryLedger.TryRemoveMarket(market.Inventory, goodId, qty);
                }

                state.Tech.SustainAccumulatorTicks = 0;
            }
        }

        // 3. Deduct credits and advance progress.
        state.PlayerCredits -= tickCost;
        state.Tech.ResearchCreditsSpent += tickCost;
        state.Tech.StallTicks = 0;
        state.Tech.StallReason = "";
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
        // GATE.S12.PROGRESSION.STATS.001: Track techs unlocked.
        if (state.PlayerStats != null)
            state.PlayerStats.TechsUnlocked = state.Tech.UnlockedTechIds.Count;

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
        state.Tech.ResearchNodeId = "";
        state.Tech.SustainAccumulatorTicks = 0;
        state.Tech.StallTicks = 0;
        state.Tech.StallReason = "";
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
        state.Tech.ResearchNodeId = "";
        state.Tech.SustainAccumulatorTicks = 0;
        state.Tech.StallTicks = 0;
        state.Tech.StallReason = "";
    }

    // GATE.S8.HAVEN.RESEARCH_LAB.001: Made public for HavenResearchLabSystem reuse.
    public static void ApplyUnlockEffect(SimState state, string effect)
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
