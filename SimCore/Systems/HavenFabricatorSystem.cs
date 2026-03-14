using System;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S8.HAVEN.FABRICATOR.001: T3 module fabrication at Haven (tier >= Expanded).
public static class HavenFabricatorSystem
{
    public sealed class FabricateResult
    {
        public bool Success { get; set; }
        public string Reason { get; set; } = "";
    }

    // Start fabricating a T3 module. Requires Haven tier >= Expanded,
    // exotic matter + exotic crystals in Haven market, and no active fabrication.
    public static FabricateResult StartFabrication(SimState state, string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId))
            return new FabricateResult { Success = false, Reason = "empty_module_id" };

        var haven = state.Haven;
        if (haven == null || !haven.Discovered)
            return new FabricateResult { Success = false, Reason = "haven_not_available" };

        if (haven.Tier < HavenTier.Expanded)
            return new FabricateResult { Success = false, Reason = "tier_too_low" };

        // Only one fabrication at a time.
        if (!string.IsNullOrEmpty(haven.FabricatingModuleId))
            return new FabricateResult { Success = false, Reason = "fabrication_in_progress" };

        // Check exotic matter cost.
        if (state.PlayerCredits < HavenTweaksV0.FabricateExoticMatterCost)
            return new FabricateResult { Success = false, Reason = "insufficient_exotic_matter" };

        // Deduct cost and start fabrication.
        state.PlayerCredits -= HavenTweaksV0.FabricateExoticMatterCost;
        haven.FabricatingModuleId = moduleId;
        haven.FabricationTicksRemaining = HavenTweaksV0.FabricateDurationTicks;

        return new FabricateResult { Success = true };
    }

    // Per-tick processing: advance fabrication timer and complete when done.
    public static void Process(SimState state)
    {
        var haven = state.Haven;
        if (haven == null || string.IsNullOrEmpty(haven.FabricatingModuleId))
            return;

        if (haven.FabricationTicksRemaining > 0)
        {
            haven.FabricationTicksRemaining--;
            if (haven.FabricationTicksRemaining <= 0)
            {
                // Fabrication complete — add to completed list.
                haven.CompletedFabricationIds ??= new System.Collections.Generic.List<string>();
                haven.CompletedFabricationIds.Add(haven.FabricatingModuleId);
                haven.FabricatingModuleId = null;
                haven.FabricationTicksRemaining = 0;
            }
        }
    }
}
