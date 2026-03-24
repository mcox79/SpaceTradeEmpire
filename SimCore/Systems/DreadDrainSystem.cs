using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.T45.DEEP_DREAD.PASSIVE_DRAIN.001: Phase-based passive hull drain.
// GATE.T52.DREAD.EXPOSURE_SCALING.001: Exposure-scaled drain intervals.
// GATE.T52.DREAD.SECONDARY_STRESS.001: Phase 2+ secondary stressors (fuel burn + cargo decay).
// Players at Phase 2+ nodes take slow hull damage from lattice degradation.
// Phase 4 (Void) = zero drain (void paradox — clarity at maximum depth).
// Accommodation module grants immunity.
public static class DreadDrainSystem
{
    private sealed class Scratch
    {
        public readonly List<string> SortedCargoKeys = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();

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

        // GATE.T52.DREAD.EXPOSURE_SCALING.001: Scale drain interval by exposure level.
        int baseInterval = phase == 2
            ? DeepDreadTweaksV0.Phase2DrainIntervalTicks
            : DeepDreadTweaksV0.Phase3DrainIntervalTicks;
        int interval = ScaleIntervalByExposure(baseInterval, state.DeepExposure);

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

    /// <summary>
    /// GATE.T52.DREAD.EXPOSURE_SCALING.001: Reduce drain interval at higher exposure.
    /// Mild exposure (20+) → DrainIntervalMildExposure, Heavy (50+) → DrainIntervalHeavyExposure.
    /// Only reduces; never increases above base interval.
    /// </summary>
    internal static int ScaleIntervalByExposure(int baseInterval, int deepExposure)
    {
        int scaled = baseInterval;
        if (deepExposure >= DeepDreadTweaksV0.ExposureHeavyThreshold)
            scaled = Math.Min(scaled, DeepDreadTweaksV0.DrainIntervalHeavyExposure);
        else if (deepExposure >= DeepDreadTweaksV0.ExposureMildThreshold)
            scaled = Math.Min(scaled, DeepDreadTweaksV0.DrainIntervalMildExposure);
        return scaled;
    }

    /// <summary>
    /// GATE.T52.DREAD.SECONDARY_STRESS.001: Phase 2+ secondary stressors.
    /// - Fuel burn multiplier: fleet fuel consumption doubled at Phase 2+.
    /// - Cargo decay: slow credit loss from cargo value degradation.
    /// Called from SimKernel tick pipeline alongside Process().
    /// </summary>
    public static void ProcessSecondaryStressors(SimState state)
    {
        if (state is null) return; // STRUCTURAL: null guard

        var playerNodeId = state.PlayerLocationNodeId;
        if (string.IsNullOrEmpty(playerNodeId)) return;
        if (!state.Nodes.TryGetValue(playerNodeId, out var node)) return;

        int phase = InstabilityTweaksV0.GetPhaseIndex(node.InstabilityLevel);
        if (phase < 2) return;  // Stable/Shimmer = no stressors
        if (phase >= 4) return; // Void paradox = no stressors

        // --- Fuel burn multiplier ---
        // Apply extra fuel drain to player fleets (on top of FleetUpkeepSystem's normal burn).
        // We drain extra fuel = (multiplier - 1x) worth every FuelBurnCycleTicks.
        bool fuelCycle = FleetUpkeepTweaksV0.FuelBurnCycleTicks > 0
            && state.Tick % FleetUpkeepTweaksV0.FuelBurnCycleTicks == 0; // STRUCTURAL: same cycle as upkeep
        if (fuelCycle && state.Fleets is not null)
        {
            foreach (var fleet in state.Fleets.Values)
            {
                if (!string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal)) continue;
                bool isDocked = !fleet.IsMoving && !string.IsNullOrEmpty(fleet.CurrentNodeId);
                if (isDocked) continue; // STRUCTURAL: docked ships exempt

                int baseFuel = FleetUpkeepSystem.GetFuelPerCycle(fleet.ShipClassId);
                if (baseFuel <= 0) continue;

                // Extra fuel = base * (multiplier - 10000) / 10000.
                int extraFuel = (int)((long)baseFuel * (DeepDreadTweaksV0.FuelBurnMultiplierPhase2Bps - DeepDreadTweaksV0.BpsDivisor) / DeepDreadTweaksV0.BpsDivisor);
                if (extraFuel <= 0) continue; // STRUCTURAL: no extra if multiplier <= 1x

                if (fleet.FuelCurrent >= extraFuel)
                {
                    fleet.FuelCurrent -= extraFuel;
                }
                else
                {
                    fleet.FuelCurrent = 0; // STRUCTURAL: floor at zero
                    fleet.FuelDepletedFlag = true;
                }
            }
        }

        // --- Cargo decay ---
        // Slow credit loss: reduce player credits proportional to total cargo value.
        bool decayCycle = DeepDreadTweaksV0.CargoDecayCycleTicks > 0
            && state.Tick % DeepDreadTweaksV0.CargoDecayCycleTicks == 0; // STRUCTURAL: cycle check
        if (decayCycle && state.PlayerCargo is not null && state.PlayerCargo.Count > 0)
        {
            // Total cargo value = sum of (quantity * cost basis) for each good.
            long totalValue = 0;
            var scratch = s_scratch.GetOrCreateValue(state);
            var sortedKeys = scratch.SortedCargoKeys;
            sortedKeys.Clear();
            foreach (var k in state.PlayerCargo.Keys) sortedKeys.Add(k);
            sortedKeys.Sort(StringComparer.Ordinal); // STRUCTURAL: deterministic order

            foreach (var goodId in sortedKeys)
            {
                int qty = state.PlayerCargo[goodId];
                if (qty <= 0) continue;
                int costBasis = 0;
                if (state.PlayerCargoCostBasis is not null)
                    state.PlayerCargoCostBasis.TryGetValue(goodId, out costBasis);
                if (costBasis <= 0) costBasis = 1; // STRUCTURAL: min 1 credit per unit fallback
                totalValue += (long)qty * costBasis;
            }

            if (totalValue > 0)
            {
                // Decay = totalValue * CargoDecayBpsPerCycle / 10000, min 1 if any cargo.
                long decay = totalValue * DeepDreadTweaksV0.CargoDecayBpsPerCycle / DeepDreadTweaksV0.BpsDivisor;
                if (decay <= 0) decay = 1; // STRUCTURAL: min 1 credit loss if cargo exists
                state.PlayerCredits -= decay;
            }
        }
    }
}
