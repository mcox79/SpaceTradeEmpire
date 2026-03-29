using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.X.FLEET_UPKEEP.DRAIN.001: Per-cycle credit drain by ship class.
// GATE.X.FLEET_UPKEEP.DELINQUENCY.001: Grace period + cascading module failure.
// Player fleets pay upkeep every UpkeepCycleTicks. Docked fleets pay 50%.
public static class FleetUpkeepSystem
{
    private sealed class Scratch
    {
        public readonly List<string> SortedFleetIds = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();

    public static void Process(SimState state)
    {
        if (state is null) return; // STRUCTURAL: null guard
        if (FleetUpkeepTweaksV0.UpkeepCycleTicks <= 0) return; // STRUCTURAL: disabled guard
        if (state.Tick % FleetUpkeepTweaksV0.UpkeepCycleTicks != 0) return; // STRUCTURAL: cycle check

        // fh_14: Safety net — waive passive upkeep when credits critically low.
        if (state.PlayerCredits < FleetUpkeepTweaksV0.LowFundsThreshold) return;

        var scratch = s_scratch.GetOrCreateValue(state);
        var sortedFleetIds = scratch.SortedFleetIds;
        sortedFleetIds.Clear();
        foreach (var k in state.Fleets.Keys) sortedFleetIds.Add(k);
        sortedFleetIds.Sort(StringComparer.Ordinal);
        foreach (var fleetId in sortedFleetIds)
        {
            var fleet = state.Fleets[fleetId];
            if (!string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal)) continue;

            int baseCost = GetUpkeepForClass(fleet.ShipClassId);
            if (baseCost <= 0) continue; // STRUCTURAL: skip zero-cost

            // Docked discount: 50% when not moving and at a node.
            bool isDocked = !fleet.IsMoving && !string.IsNullOrEmpty(fleet.CurrentNodeId);
            int cost = isDocked
                ? (int)((long)baseCost * FleetUpkeepTweaksV0.DockedMultiplierBps / FleetUpkeepTweaksV0.BpsDivisor)
                : baseCost;
            if (cost <= 0) cost = 1; // STRUCT_MIN: min 1 cr upkeep

            if (state.PlayerCredits >= cost)
            {
                state.PlayerCredits -= cost;
                // GATE.X.FLEET_UPKEEP.DELINQUENCY.001: Paid — reset delinquency + re-enable.
                if (fleet.UpkeepDelinquentCycles > 0)
                {
                    fleet.UpkeepDelinquentCycles = 0; // STRUCTURAL: reset counter
                    RecoverModules(fleet);
                }
            }
            else
            {
                // Can't pay — deduct what we can and track delinquency.
                state.PlayerCredits -= cost;
                fleet.UpkeepDelinquentCycles++;

                // GATE.X.FLEET_UPKEEP.DELINQUENCY.001: After grace period, disable modules.
                if (fleet.UpkeepDelinquentCycles > FleetUpkeepTweaksV0.GracePeriodCycles)
                {
                    DisableHighestPowerModule(fleet);
                }
            }
        }
    }

    // GATE.X.FLEET_UPKEEP.DELINQUENCY.001: Disable the highest-PowerDraw enabled module.
    // Deterministic: sort by PowerDraw desc, then InstalledModuleId asc.
    private static void DisableHighestPowerModule(Fleet fleet)
    {
        if (fleet.Slots == null || fleet.Slots.Count == 0) return; // STRUCTURAL: empty guard

        ModuleSlot? target = null;
        foreach (var slot in fleet.Slots)
        {
            if (string.IsNullOrEmpty(slot.InstalledModuleId)) continue;
            if (slot.Disabled) continue;
            if (slot.PowerDraw <= 0) continue; // STRUCTURAL: skip passive

            if (target == null
                || slot.PowerDraw > target.PowerDraw
                || (slot.PowerDraw == target.PowerDraw
                    && string.CompareOrdinal(slot.InstalledModuleId, target.InstalledModuleId) < 0)) // STRUCTURAL: tie-break
            {
                target = slot;
            }
        }

        if (target != null)
            target.Disabled = true;
    }

    // GATE.X.FLEET_UPKEEP.DELINQUENCY.001: Re-enable modules disabled by delinquency.
    private static void RecoverModules(Fleet fleet)
    {
        if (fleet.Slots == null) return; // STRUCTURAL: null guard
        foreach (var slot in fleet.Slots)
        {
            if (slot.Disabled && !string.IsNullOrEmpty(slot.InstalledModuleId))
                slot.Disabled = false;
        }
    }

    // GATE.T48.TENSION.MAINTENANCE.001: Per-tick continuous costs — fuel, wages, hull degradation.
    public static void ProcessContinuousCosts(SimState state)
    {
        if (state is null) return; // STRUCTURAL: null guard

        var scratch = s_scratch.GetOrCreateValue(state);
        var sortedFleetIds = scratch.SortedFleetIds;
        sortedFleetIds.Clear();
        foreach (var k in state.Fleets.Keys) sortedFleetIds.Add(k);
        sortedFleetIds.Sort(StringComparer.Ordinal);

        bool fuelCycle = FleetUpkeepTweaksV0.FuelBurnCycleTicks > 0
            && state.Tick % FleetUpkeepTweaksV0.FuelBurnCycleTicks == 0; // STRUCTURAL: cycle check
        bool wageCycle = FleetUpkeepTweaksV0.WageCycleTicks > 0
            && state.Tick % FleetUpkeepTweaksV0.WageCycleTicks == 0; // STRUCTURAL: cycle check
        bool hullCycle = FleetUpkeepTweaksV0.HullDegradCycleTicks > 0
            && state.Tick % FleetUpkeepTweaksV0.HullDegradCycleTicks == 0; // STRUCTURAL: cycle check

        if (!fuelCycle && !wageCycle && !hullCycle) return;

        foreach (var fleetId in sortedFleetIds)
        {
            var fleet = state.Fleets[fleetId];
            if (!string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal)) continue;

            bool isDocked = !fleet.IsMoving && !string.IsNullOrEmpty(fleet.CurrentNodeId);

            // Fuel consumption: docked ships consume no fuel.
            if (fuelCycle && !isDocked)
            {
                int fuelCost = GetFuelPerCycle(fleet.ShipClassId);
                if (fuelCost > 0)
                {
                    if (fleet.FuelCurrent >= fuelCost)
                    {
                        fleet.FuelCurrent -= fuelCost;
                        fleet.FuelDepletedFlag = false;
                    }
                    else
                    {
                        fleet.FuelCurrent = 0; // STRUCTURAL: floor at zero
                        fleet.FuelDepletedFlag = true; // Speed penalty flag
                    }
                }
            }
            else if (fuelCycle && isDocked)
            {
                // Docked: no fuel burn. Clear depletion flag (refueled at dock).
                fleet.FuelDepletedFlag = false;
            }

            // Crew wages: docked ships pay 50%.
            // fh_14: Safety net — waive wages when credits critically low.
            if (wageCycle && state.PlayerCredits >= FleetUpkeepTweaksV0.LowFundsThreshold)
            {
                int baseWage = GetWagePerCycle(fleet.ShipClassId);
                if (baseWage > 0)
                {
                    int wage = isDocked
                        ? (int)((long)baseWage * FleetUpkeepTweaksV0.DockedWageMultiplierBps / FleetUpkeepTweaksV0.BpsDivisor)
                        : baseWage;
                    if (wage <= 0) wage = 1; // STRUCT_MIN: min 1 cr wage
                    state.PlayerCredits -= wage;
                }
            }

            // Hull degradation: docked ships don't degrade.
            if (hullCycle && !isDocked)
            {
                int hullDmg = GetHullDegradPerCycle(fleet.ShipClassId);
                if (hullDmg > 0 && fleet.HullHp > 0)
                {
                    fleet.HullHp = Math.Max(1, fleet.HullHp - hullDmg); // STRUCTURAL: floor at 1 (wear can't kill)
                }
            }
        }

        // GATE.T64.ECON.FRICTION_SINKS.001: Per-hop lane transit fee on arrival.
        // Flat credit cost each time the player completes a lane transit.
        // fh_14: Safety net — waive transit/docking fees when credits critically low.
        if (FleetUpkeepTweaksV0.LaneTransitFeeCr > 0 && state.ArrivalsThisTick.Count > 0
            && state.PlayerCredits >= FleetUpkeepTweaksV0.LowFundsThreshold)
        {
            foreach (var (fleetId, edgeId, nodeId) in state.ArrivalsThisTick)
            {
                if (!state.Fleets.TryGetValue(fleetId, out var arrFleet)) continue;
                if (!string.Equals(arrFleet.OwnerId, "player", StringComparison.Ordinal)) continue;
                state.PlayerCredits -= FleetUpkeepTweaksV0.LaneTransitFeeCr;

                // GATE.T67.ECON.SINK_UPKEEP.001: Docking fee on arrival at a station node.
                if (FleetUpkeepTweaksV0.DockingFeeCr > 0 && state.Markets.ContainsKey(nodeId))
                {
                    state.PlayerCredits -= FleetUpkeepTweaksV0.DockingFeeCr;
                }
            }
        }
    }

    public static int GetUpkeepForClass(string classId)
    {
        if (string.IsNullOrEmpty(classId)) return FleetUpkeepTweaksV0.DefaultUpkeep;
        return classId switch
        {
            "shuttle" => FleetUpkeepTweaksV0.ShuttleUpkeep,
            "corvette" => FleetUpkeepTweaksV0.CorvetteUpkeep,
            "clipper" => FleetUpkeepTweaksV0.ClipperUpkeep,
            "frigate" => FleetUpkeepTweaksV0.FrigateUpkeep,
            "hauler" => FleetUpkeepTweaksV0.HaulerUpkeep,
            "cruiser" => FleetUpkeepTweaksV0.CruiserUpkeep,
            "carrier" => FleetUpkeepTweaksV0.CarrierUpkeep,
            "dreadnought" => FleetUpkeepTweaksV0.DreadnoughtUpkeep,
            "ancient_bastion" => FleetUpkeepTweaksV0.AncientBastionUpkeep,
            "ancient_seeker" => FleetUpkeepTweaksV0.AncientSeekerUpkeep,
            "ancient_threshold" => FleetUpkeepTweaksV0.AncientThresholdUpkeep,
            _ => FleetUpkeepTweaksV0.DefaultUpkeep,
        };
    }

    public static int GetFuelPerCycle(string classId)
    {
        if (string.IsNullOrEmpty(classId)) return FleetUpkeepTweaksV0.FuelPerCycleDefault;
        return classId switch
        {
            "shuttle" => FleetUpkeepTweaksV0.FuelPerCycleShuttle,
            "corvette" => FleetUpkeepTweaksV0.FuelPerCycleCorvette,
            "clipper" => FleetUpkeepTweaksV0.FuelPerCycleClipper,
            "frigate" => FleetUpkeepTweaksV0.FuelPerCycleFrigate,
            "hauler" => FleetUpkeepTweaksV0.FuelPerCycleHauler,
            "cruiser" => FleetUpkeepTweaksV0.FuelPerCycleCruiser,
            "carrier" => FleetUpkeepTweaksV0.FuelPerCycleCarrier,
            "dreadnought" => FleetUpkeepTweaksV0.FuelPerCycleDreadnought,
            _ => FleetUpkeepTweaksV0.FuelPerCycleDefault,
        };
    }

    public static int GetWagePerCycle(string classId)
    {
        if (string.IsNullOrEmpty(classId)) return FleetUpkeepTweaksV0.WagePerCycleDefault;
        return classId switch
        {
            "shuttle" => FleetUpkeepTweaksV0.WagePerCycleShuttle,
            "corvette" => FleetUpkeepTweaksV0.WagePerCycleCorvette,
            "clipper" => FleetUpkeepTweaksV0.WagePerCycleClipper,
            "frigate" => FleetUpkeepTweaksV0.WagePerCycleFrigate,
            "hauler" => FleetUpkeepTweaksV0.WagePerCycleHauler,
            "cruiser" => FleetUpkeepTweaksV0.WagePerCycleCruiser,
            "carrier" => FleetUpkeepTweaksV0.WagePerCycleCarrier,
            "dreadnought" => FleetUpkeepTweaksV0.WagePerCycleDreadnought,
            _ => FleetUpkeepTweaksV0.WagePerCycleDefault,
        };
    }

    public static int GetHullDegradPerCycle(string classId)
    {
        if (string.IsNullOrEmpty(classId)) return FleetUpkeepTweaksV0.HullDegradPerCycleDefault;
        return classId switch
        {
            "shuttle" => FleetUpkeepTweaksV0.HullDegradPerCycleShuttle,
            "corvette" => FleetUpkeepTweaksV0.HullDegradPerCycleCorvette,
            "clipper" => FleetUpkeepTweaksV0.HullDegradPerCycleClipper,
            "frigate" => FleetUpkeepTweaksV0.HullDegradPerCycleFrigate,
            "hauler" => FleetUpkeepTweaksV0.HullDegradPerCycleHauler,
            "cruiser" => FleetUpkeepTweaksV0.HullDegradPerCycleCruiser,
            "carrier" => FleetUpkeepTweaksV0.HullDegradPerCycleCarrier,
            "dreadnought" => FleetUpkeepTweaksV0.HullDegradPerCycleDreadnought,
            _ => FleetUpkeepTweaksV0.HullDegradPerCycleDefault,
        };
    }
}
