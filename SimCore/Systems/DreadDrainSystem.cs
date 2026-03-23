using System;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.T45.DEEP_DREAD.PASSIVE_DRAIN.001: Phase-based passive hull drain.
// Players at Phase 2+ nodes take slow hull damage from lattice degradation.
// Phase 4 (Void) = zero drain (void paradox — clarity at maximum depth).
// Accommodation module grants immunity.
public static class DreadDrainSystem
{
    public static void Process(SimState state)
    {
        if (state.Fleets is null) return;

        // Only affect player fleet at their current node.
        var playerNodeId = state.PlayerLocationNodeId;
        if (string.IsNullOrEmpty(playerNodeId)) return;
        if (!state.Nodes.TryGetValue(playerNodeId, out var node)) return;

        int phase = InstabilityTweaksV0.GetPhaseIndex(node.InstabilityLevel);
        if (phase < 2) return;  // Stable/Shimmer = no drain
        if (phase >= 4) return; // Void paradox = no drain

        // Determine drain interval based on phase.
        int interval = phase == 2
            ? DeepDreadTweaksV0.Phase2DrainIntervalTicks
            : DeepDreadTweaksV0.Phase3DrainIntervalTicks;
        int amount = phase == 2
            ? DeepDreadTweaksV0.Phase2DrainAmount
            : DeepDreadTweaksV0.Phase3DrainAmount;

        if (interval <= 0 || state.Tick % interval != 0) return; // STRUCTURAL: interval guard

        // Find player fleet.
        foreach (var fleet in state.Fleets.Values)
        {
            if (fleet.OwnerId != "player") continue;
            if (fleet.HullHp <= 0) continue; // STRUCTURAL: already dead

            // Check for accommodation module immunity.
            bool hasAccommodation = false;
            foreach (var slot in fleet.Slots)
            {
                if (string.Equals(slot.InstalledModuleId, DeepDreadTweaksV0.AccommodationModuleId, StringComparison.Ordinal))
                {
                    hasAccommodation = true;
                    break;
                }
            }
            if (hasAccommodation) continue;

            fleet.HullHp = Math.Max(0, fleet.HullHp - amount); // STRUCTURAL: floor at zero
            break; // Only one player fleet.
        }
    }
}
