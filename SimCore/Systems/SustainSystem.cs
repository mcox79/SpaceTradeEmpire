using System;
using System.Collections.Generic;
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
    /// Auto-refuels fleets that are idle/docked at a node.
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
            // Fuel deduction: moving fleets burn fuel each tick from their dedicated tank.
            if (fleet.IsMoving)
            {
                bool isNpc = fleet.OwnerId != "player";

                // NPC fleets burn at reduced rate: only deduct on interval ticks.
                if (isNpc && (npcFuelInterval <= 0 || state.Tick % npcFuelInterval != 0))
                {
                    // Still process sustain cycle for NPC fleets even if not deducting fuel.
                    if (isSustainCycle)
                        ProcessModuleSustain(fleet, fleet.Cargo);
                    continue;
                }

                int fuelCost = SustainTweaksV0.FuelPerMoveTick;
                if (fleet.FuelCurrent > 0)
                {
                    int deducted = Math.Min(fuelCost, fleet.FuelCurrent);
                    fleet.FuelCurrent -= deducted;
                }
            }

            // Auto-refuel: any fleet at a node (idle/docked) gets topped up.
            if (!fleet.IsMoving
                && fleet.FuelCurrent < fleet.FuelCapacity
                && !string.IsNullOrEmpty(fleet.CurrentNodeId))
            {
                bool isPlayer = string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal);
                if (isPlayer && SustainTweaksV0.RefuelCreditCostPerUnit > 0)
                {
                    int deficit = fleet.FuelCapacity - fleet.FuelCurrent;
                    int affordable = (int)(state.PlayerCredits / SustainTweaksV0.RefuelCreditCostPerUnit);
                    int refuelAmount = Math.Min(deficit, affordable);
                    if (refuelAmount > 0)
                    {
                        fleet.FuelCurrent += refuelAmount;
                        state.PlayerCredits -= refuelAmount * SustainTweaksV0.RefuelCreditCostPerUnit;
                    }
                }
                else
                {
                    // Free refuel (player when cost=0, or NPC fleets).
                    fleet.FuelCurrent = fleet.FuelCapacity;
                }
            }

            // GATE.S7.SUSTAIN.SHORTFALL.001: Module sustain check on cycle boundary.
            if (isSustainCycle)
            {
                bool isPlayer = string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal);
                var cargo = isPlayer ? state.PlayerCargo : fleet.Cargo;
                ProcessModuleSustain(fleet, cargo);
            }
        }
    }

    /// <summary>
    /// On sustain cycle: if fleet has no fuel, disable all modules with PowerDraw > 0.
    /// If fleet has fuel, check SustainInputs goods and deduct from cargo. Disable on shortfall.
    /// GATE.X.MODULE_SUSTAIN.DEDUCT.001: Per-module good consumption during sustain cycle.
    /// </summary>
    private static void ProcessModuleSustain(Fleet fleet, Dictionary<string, int> cargo)
    {
        if (fleet.Slots == null || fleet.Slots.Count == 0) return;

        bool hasFuel = fleet.FuelCurrent > 0;

        foreach (var slot in fleet.Slots)
        {
            if (string.IsNullOrEmpty(slot.InstalledModuleId)) continue;

            if (!hasFuel && slot.PowerDraw > 0)
            {
                // Fuel shortfall: disable module.
                slot.Disabled = true;
                continue;
            }

            // GATE.X.MODULE_SUSTAIN.DEDUCT.001: Check and consume SustainInputs goods.
            if (hasFuel && slot.PowerDraw > 0)
            {
                var moduleDef = UpgradeContentV0.GetById(slot.InstalledModuleId);
                if (moduleDef != null && moduleDef.SustainInputs.Count > 0)
                {
                    // Check if all required goods are available (deterministic: sort keys).
                    bool canSustain = true;
                    var keys = new List<string>(moduleDef.SustainInputs.Keys);
                    keys.Sort(StringComparer.Ordinal);

                    foreach (var goodId in keys)
                    {
                        int required = moduleDef.SustainInputs[goodId];
                        int available = InventoryLedger.Get(cargo, goodId);
                        if (available < required)
                        {
                            canSustain = false;
                            break;
                        }
                    }

                    if (canSustain)
                    {
                        // Deduct sustain goods from cargo.
                        foreach (var goodId in keys)
                        {
                            int required = moduleDef.SustainInputs[goodId];
                            InventoryLedger.TryRemoveCargo(cargo, goodId, required);
                        }
                        // Re-enable if was disabled by sustain shortfall.
                        if (slot.Disabled) slot.Disabled = false;
                    }
                    else
                    {
                        // Sustain shortfall: disable module.
                        slot.Disabled = true;
                    }
                }
                else if (slot.Disabled)
                {
                    // No sustain requirements, has fuel: re-enable.
                    slot.Disabled = false;
                }
            }
        }
    }
}
