using System;
using System.Linq;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S7.SUSTAIN.FUEL_DEDUCT.001: Fleet fuel + module sustain deduction per tick.
// GATE.S7.SUSTAIN.ECONOMY_WIRE.001: NPC fleet fuel consumption at reduced rate.
// GATE.S7.SUSTAIN.SHORTFALL.001: Module disable on sustain shortfall, recovery on re-supply.
public static class SustainSystem
{
    /// <summary>
    /// Deducts fuel from moving fleets and sustain goods from equipped modules on cycle boundaries.
    /// Called once per tick after MovementSystem.
    /// NPC fleets consume fuel at NpcFuelRateMultiplier (0.5 = every 2nd tick).
    /// On sustain cycle boundaries, checks module sustain requirements and disables/enables accordingly.
    /// </summary>
    public static void Process(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));

        // NPC fuel interval: deduct once every N ticks where N = 1/NpcFuelRateMultiplier.
        int npcFuelInterval = SustainTweaksV0.NpcFuelRateMultiplier > 0f
            ? Math.Max(1, (int)(1.0f / SustainTweaksV0.NpcFuelRateMultiplier))
            : 0;

        bool isSustainCycle = SustainTweaksV0.SustainCycleTicks > 0
            && state.Tick % SustainTweaksV0.SustainCycleTicks == 0;

        foreach (var fleet in state.Fleets.Values.OrderBy(f => f.Id, StringComparer.Ordinal))
        {
            // Fuel deduction: moving fleets burn fuel each tick.
            if (fleet.IsMoving)
            {
                bool isNpc = fleet.OwnerId != "player";

                // NPC fleets burn at reduced rate: only deduct on interval ticks.
                if (isNpc && (npcFuelInterval <= 0 || state.Tick % npcFuelInterval != 0))
                {
                    // Still process sustain cycle for NPC fleets even if not deducting fuel.
                    if (isSustainCycle)
                        ProcessModuleSustain(fleet);
                    continue;
                }

                int fuelCost = SustainTweaksV0.FuelPerMoveTick;
                int currentFuel = fleet.GetCargoUnits(WellKnownGoodIds.Fuel);
                if (currentFuel > 0)
                {
                    int deducted = Math.Min(fuelCost, currentFuel);
                    fleet.Cargo[WellKnownGoodIds.Fuel] = currentFuel - deducted;
                    if (fleet.Cargo[WellKnownGoodIds.Fuel] <= 0)
                        fleet.Cargo.Remove(WellKnownGoodIds.Fuel);
                }
            }

            // GATE.S7.SUSTAIN.SHORTFALL.001: Module sustain check on cycle boundary.
            if (isSustainCycle)
                ProcessModuleSustain(fleet);
        }
    }

    /// <summary>
    /// On sustain cycle: if fleet has no fuel, disable all modules with PowerDraw > 0.
    /// If fleet has fuel, re-enable modules that were disabled by sustain shortfall
    /// (but NOT modules disabled by PowerBudgetSystem — those have PowerDraw > BasePower).
    /// </summary>
    private static void ProcessModuleSustain(Fleet fleet)
    {
        if (fleet.Slots == null || fleet.Slots.Count == 0) return;

        bool hasFuel = fleet.GetCargoUnits(WellKnownGoodIds.Fuel) > 0;

        foreach (var slot in fleet.Slots)
        {
            if (string.IsNullOrEmpty(slot.InstalledModuleId)) continue;

            if (!hasFuel && slot.PowerDraw > 0)
            {
                // Shortfall: disable module.
                slot.Disabled = true;
            }
            else if (hasFuel && slot.Disabled && slot.PowerDraw > 0)
            {
                // Recovery: re-enable (PowerBudgetSystem will re-disable if over budget).
                slot.Disabled = false;
            }
        }
    }
}
