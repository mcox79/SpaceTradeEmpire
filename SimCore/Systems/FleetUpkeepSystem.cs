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
}
