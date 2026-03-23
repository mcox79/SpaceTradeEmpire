using System;
using System.Collections.Generic;
using System.Linq;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

/// <summary>
/// Lattice drone combat engagement system.
/// GATE.S8.LATTICE_DRONES.COMBAT.001
/// Territorial (P2): warn first (1 tick grace). Hostile (P3): attack immediately.
/// Destroyed drones handled by NpcFleetCombatSystem respawn.
/// </summary>
public static class LatticeDroneCombatSystem
{
    // Scratch list to avoid per-tick allocation.
    private static readonly List<Fleet> _scratchDrones = new();

    public static void Process(SimState state)
    {
        if (state.Fleets is null) return; // STRUCTURAL: guard

        // Find player fleet.
        Fleet? playerFleet = null;
        foreach (var f in state.Fleets.Values)
        {
            if (string.Equals(f.OwnerId, "player", StringComparison.Ordinal))
            {
                playerFleet = f;
                break;
            }
        }
        if (playerFleet is null || playerFleet.HullHp <= 0) return; // STRUCTURAL: guard

        // Find drones at player's node — reuse scratch list.
        _scratchDrones.Clear();
        foreach (var f in state.Fleets.Values)
        {
            if (!f.IsLatticeDrone) continue;
            if (f.HullHp <= 0) continue; // STRUCTURAL: skip destroyed
            if (!string.Equals(f.CurrentNodeId, playerFleet.CurrentNodeId, StringComparison.Ordinal)) continue;
            _scratchDrones.Add(f);
        }
        var dronesAtNode = _scratchDrones;

        foreach (var drone in dronesAtNode)
        {
            // Grace period for territorial drones.
            if (drone.LatticeDroneGraceTicksRemaining > 0) // STRUCTURAL: grace check
            {
                drone.LatticeDroneGraceTicksRemaining--;
                continue;
            }

            // Engagement cooldown — prevent full strategic combat every tick.
            if (drone.LatticeDroneLastEngagementTick >= 0
                && (state.Tick - drone.LatticeDroneLastEngagementTick) < LatticeDroneTweaksV0.EngagementCooldownTicks)
            {
                continue;
            }

            // Engage: build profiles and resolve combat.
            CombatSystem.InitFleetCombatStats(drone, isPlayer: false);
            var droneProfile = CombatSystem.BuildProfile(drone);
            var playerProfile = CombatSystem.BuildProfile(playerFleet);

            var result = StrategicResolverV0.Resolve(droneProfile, playerProfile);

            drone.LatticeDroneLastEngagementTick = state.Tick;

            // Apply results to actual fleet HP.
            drone.HullHp = result.FleetAHullRemaining;
            drone.ShieldHp = Math.Max(0, drone.ShieldHpMax - (droneProfile.ShieldHp - (result.Frames.Count > 0 ? result.Frames[^1].AShieldRemaining : 0))); // STRUCTURAL: approximate

            playerFleet.HullHp = result.FleetBHullRemaining;
            if (result.Frames.Count > 0) // STRUCTURAL: frame check
            {
                playerFleet.ShieldHp = Math.Max(0, result.Frames[^1].BShieldRemaining); // STRUCTURAL: exact from frame
            }
        }
    }
}
