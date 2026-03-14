using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S8.HAVEN.RESEARCH_LAB.001: Haven research lab — parallel research slots gated by tier.
// Tier 2 = 1 slot, Tier 3 = 2 slots, Tier 4+ = 3 slots.
// Each slot can research one tech independently of the main research queue.
public static class HavenResearchLabSystem
{
    public static int GetMaxSlots(HavenTier tier)
    {
        if (tier >= HavenTier.Expanded) return HavenTweaksV0.ResearchSlotsTier4;
        if (tier >= HavenTier.Operational) return HavenTweaksV0.ResearchSlotsTier3;
        if (tier >= HavenTier.Inhabited) return HavenTweaksV0.ResearchSlotsTier2;
        return 0; // STRUCTURAL: no research lab before Tier 2
    }

    public static void Process(SimState state)
    {
        var haven = state.Haven;
        if (haven == null || !haven.Discovered) return;

        int maxSlots = GetMaxSlots(haven.Tier);
        if (maxSlots <= 0) return;

        // Trim excess slots if tier downgraded (shouldn't happen, defensive).
        while (haven.ResearchLabSlots.Count > maxSlots)
        {
            haven.ResearchLabSlots.RemoveAt(haven.ResearchLabSlots.Count - 1); // STRUCTURAL: last
        }

        foreach (var slot in haven.ResearchLabSlots)
        {
            if (!slot.IsActive) continue;

            var def = TechContentV0.GetById(slot.TechId);
            if (def == null)
            {
                // Content removed — cancel.
                ClearSlot(slot);
                continue;
            }

            // Credit cost per tick.
            int tickCost = HavenTweaksV0.ResearchLabCreditPerTick * def.Tier;
            if (state.PlayerCredits < tickCost)
            {
                slot.StallTicks++;
                slot.StallReason = "insufficient_credits";
                continue;
            }

            state.PlayerCredits -= tickCost;
            slot.StallTicks = 0; // STRUCTURAL: reset
            slot.StallReason = "";
            slot.ProgressTicks++;

            if (slot.ProgressTicks >= slot.TotalTicks)
            {
                CompleteSlotResearch(state, slot);
            }
        }
    }

    public sealed class StartResult
    {
        public bool Success { get; set; }
        public string Reason { get; set; } = "";
    }

    public static StartResult StartSlotResearch(SimState state, string techId, int slotIndex)
    {
        if (string.IsNullOrEmpty(techId))
            return new StartResult { Success = false, Reason = "empty_tech_id" };

        var haven = state.Haven;
        if (haven == null || !haven.Discovered)
            return new StartResult { Success = false, Reason = "haven_not_available" };

        int maxSlots = GetMaxSlots(haven.Tier);
        if (maxSlots <= 0)
            return new StartResult { Success = false, Reason = "research_lab_locked" };

        if (slotIndex < 0 || slotIndex >= maxSlots)
            return new StartResult { Success = false, Reason = "invalid_slot_index" };

        var def = TechContentV0.GetById(techId);
        if (def == null)
            return new StartResult { Success = false, Reason = "unknown_tech" };

        if (state.Tech.UnlockedTechIds.Contains(techId))
            return new StartResult { Success = false, Reason = "already_unlocked" };

        if (!TechContentV0.PrerequisitesMet(techId, state.Tech.UnlockedTechIds))
            return new StartResult { Success = false, Reason = "prerequisites_not_met" };

        // Check not already researching this tech in main queue or another slot.
        if (state.Tech.CurrentResearchTechId == techId)
            return new StartResult { Success = false, Reason = "already_researching_main" };

        foreach (var existing in haven.ResearchLabSlots)
        {
            if (existing.TechId == techId)
                return new StartResult { Success = false, Reason = "already_researching_slot" };
        }

        // Ensure slot list is large enough.
        while (haven.ResearchLabSlots.Count <= slotIndex)
        {
            haven.ResearchLabSlots.Add(new HavenResearchSlot { SlotIndex = haven.ResearchLabSlots.Count });
        }

        var slot = haven.ResearchLabSlots[slotIndex];
        if (slot.IsActive)
            return new StartResult { Success = false, Reason = "slot_occupied" };

        slot.TechId = techId;
        slot.ProgressTicks = 0;
        slot.TotalTicks = def.ResearchTicks;
        slot.StallTicks = 0;
        slot.StallReason = "";

        return new StartResult { Success = true };
    }

    public static void CancelSlotResearch(SimState state, int slotIndex)
    {
        var haven = state.Haven;
        if (haven == null || !haven.Discovered) return;
        if (slotIndex < 0 || slotIndex >= haven.ResearchLabSlots.Count) return;

        ClearSlot(haven.ResearchLabSlots[slotIndex]);
    }

    private static void CompleteSlotResearch(SimState state, HavenResearchSlot slot)
    {
        var techId = slot.TechId;
        if (string.IsNullOrEmpty(techId)) return;

        state.Tech.UnlockedTechIds.Add(techId);

        if (state.PlayerStats != null)
            state.PlayerStats.TechsUnlocked = state.Tech.UnlockedTechIds.Count;

        // Apply unlock effects (same as main research).
        var def = TechContentV0.GetById(techId);
        if (def != null)
        {
            foreach (var effect in def.UnlockEffects)
            {
                ResearchSystem.ApplyUnlockEffect(state, effect);
            }
        }

        // Log event in main tech event log for consistency.
        state.Tech.EventLog.Add(new TechEvent
        {
            Seq = state.Tech.NextEventSeq++,
            Tick = state.Tick,
            TechId = techId,
            EventType = "Completed"
        });

        ClearSlot(slot);
    }

    private static void ClearSlot(HavenResearchSlot slot)
    {
        slot.TechId = "";
        slot.ProgressTicks = 0;
        slot.TotalTicks = 0;
        slot.StallTicks = 0;
        slot.StallReason = "";
    }
}
