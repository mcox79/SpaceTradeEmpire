using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Content;
using SimCore.Entities;

namespace SimCore.Systems;

// GATE.S7.POWER.BUDGET_ENFORCE.001: Enforces power draw vs ship class BasePower each tick.
// Over-budget fleets have lowest-priority (last slot) modules disabled until within budget.
// Under-budget fleets have previously disabled modules re-enabled if they fit.
public static class PowerBudgetSystem
{
    private sealed class Scratch
    {
        public readonly List<string> SortedFleetIds = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();

    public static void Process(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));

        var scratch = s_scratch.GetOrCreateValue(state);
        var sortedFleetIds = scratch.SortedFleetIds;
        sortedFleetIds.Clear();
        foreach (var k in state.Fleets.Keys) sortedFleetIds.Add(k);
        sortedFleetIds.Sort(StringComparer.Ordinal);
        foreach (var fleetId in sortedFleetIds)
        {
            var fleet = state.Fleets[fleetId];
            if (fleet.Slots.Count == 0) continue;

            var classDef = ShipClassContentV0.GetById(fleet.ShipClassId);
            int budget = classDef?.BasePower ?? 0;
            if (budget <= 0) continue; // No power budget = no enforcement.

            // Phase 1: Try re-enabling disabled modules (forward order = highest priority first).
            int totalDraw = ComputeActivePowerDraw(fleet);
            for (int i = 0; i < fleet.Slots.Count; i++)
            {
                var slot = fleet.Slots[i];
                if (!slot.Disabled || string.IsNullOrEmpty(slot.InstalledModuleId)) continue;
                if (totalDraw + slot.PowerDraw <= budget)
                {
                    slot.Disabled = false;
                    totalDraw += slot.PowerDraw;
                }
            }

            // Phase 2: Disable modules in reverse order (lowest priority) if over budget.
            totalDraw = ComputeActivePowerDraw(fleet);
            if (totalDraw <= budget) continue;

            for (int i = fleet.Slots.Count - 1; i >= 0 && totalDraw > budget; i--)
            {
                var slot = fleet.Slots[i];
                if (slot.Disabled || string.IsNullOrEmpty(slot.InstalledModuleId)) continue;
                slot.Disabled = true;
                totalDraw -= slot.PowerDraw;
            }
        }
    }

    /// <summary>
    /// Computes power draw of non-disabled installed modules only.
    /// </summary>
    public static int ComputeActivePowerDraw(Fleet fleet)
    {
        int total = 0;
        foreach (var slot in fleet.Slots)
        {
            if (slot.Disabled || string.IsNullOrEmpty(slot.InstalledModuleId)) continue;
            total += slot.PowerDraw;
        }
        return total;
    }
}
