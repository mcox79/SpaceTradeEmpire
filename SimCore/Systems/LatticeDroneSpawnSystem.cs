using System;
using System.Collections.Generic;
using System.Linq;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

/// <summary>
/// Spawns/despawns lattice drones based on instability phase per node.
/// GATE.S8.LATTICE_DRONES.SPAWN.001
/// P0-1: no spawn. P2: territorial (spawn near void sites). P3: hostile (spawn at lanes). P4: absent (despawn).
/// </summary>
public static class LatticeDroneSpawnSystem
{
    public static void Process(SimState state)
    {
        if (state.Nodes is null || state.Nodes.Count == 0) return; // STRUCTURAL: guard

        // Only check spawns periodically.
        if (state.Tick % LatticeDroneTweaksV0.SpawnCheckIntervalTicks != 0) return; // STRUCTURAL: tick guard

        // Collect drone fleet IDs for cleanup.
        var dronesByNode = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var fleet in state.Fleets.Values)
        {
            if (!fleet.IsLatticeDrone) continue;
            var nodeId = fleet.CurrentNodeId ?? "";
            if (!dronesByNode.TryGetValue(nodeId, out var list))
            {
                list = new List<string>();
                dronesByNode[nodeId] = list;
            }
            list.Add(fleet.Id);
        }

        foreach (var kv in state.Nodes.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var node = kv.Value;
            int phase = InstabilityTweaksV0.GetPhaseIndex(node.InstabilityLevel);
            dronesByNode.TryGetValue(kv.Key, out var existingDrones);
            int droneCount = existingDrones?.Count ?? 0; // STRUCTURAL: null-safe count

            if (phase >= LatticeDroneTweaksV0.DespawnPhaseMax || phase < LatticeDroneTweaksV0.SpawnPhaseMin)
            {
                // Despawn all drones at this node.
                if (existingDrones != null)
                {
                    foreach (var droneId in existingDrones)
                        state.Fleets.Remove(droneId);
                }
            }
            else if (phase >= LatticeDroneTweaksV0.SpawnPhaseMin && droneCount < LatticeDroneTweaksV0.MaxDronesPerNode)
            {
                // Spawn a drone.
                bool isHostile = phase >= LatticeDroneTweaksV0.HostilePhaseMin;
                SpawnDrone(state, kv.Key, isHostile);
            }
        }
    }

    private static void SpawnDrone(SimState state, string nodeId, bool hostile)
    {
        string droneId = $"{LatticeDroneTweaksV0.DroneFleetIdPrefix}{nodeId}_{state.Tick}";

        var drone = new Fleet
        {
            Id = droneId,
            OwnerId = "lattice",
            CurrentNodeId = nodeId,
            State = FleetState.Idle,
            ShipClassId = LatticeDroneTweaksV0.DroneShipClassId,
            IsLatticeDrone = true,
            LatticeDroneSpawnTick = state.Tick,
            LatticeDroneGraceTicksRemaining = hostile ? 0 : LatticeDroneTweaksV0.WarningGraceTicks, // STRUCTURAL: 0=immediate, N=warn first
            HullHp = LatticeDroneTweaksV0.DroneHullHp,
            HullHpMax = LatticeDroneTweaksV0.DroneHullHp,
            ShieldHp = LatticeDroneTweaksV0.DroneShieldHp,
            ShieldHpMax = LatticeDroneTweaksV0.DroneShieldHp,
            BattleStations = BattleStationsState.BattleReady,
        };
        drone.Slots.Add(new ModuleSlot
        {
            SlotId = "w1",
            SlotKind = SlotKind.Weapon,
            InstalledModuleId = LatticeDroneTweaksV0.DroneWeaponModuleId,
        });

        state.Fleets[droneId] = drone;
    }

    /// <summary>Count active lattice drones at a node.</summary>
    public static int CountDronesAtNode(SimState state, string nodeId)
    {
        int count = 0; // STRUCTURAL: counter init
        foreach (var fleet in state.Fleets.Values)
        {
            if (fleet.IsLatticeDrone && string.Equals(fleet.CurrentNodeId, nodeId, StringComparison.Ordinal))
                count++;
        }
        return count;
    }
}
