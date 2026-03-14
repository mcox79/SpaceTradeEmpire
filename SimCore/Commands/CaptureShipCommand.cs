using System;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Commands;

// GATE.S5.LOSS_RECOVERY.CAPTURE.001: Capture a disabled NPC ship.
public sealed class CaptureShipCommand : ICommand
{
    public string TargetFleetId { get; }

    public CaptureShipCommand(string targetFleetId)
    {
        TargetFleetId = targetFleetId ?? "";
    }

    public void Execute(SimState state)
    {
        if (state is null || string.IsNullOrEmpty(TargetFleetId)) return;

        // Validate target exists and is disabled
        if (!state.Fleets.TryGetValue(TargetFleetId, out var target)) return;
        if (string.Equals(target.OwnerId, "player", StringComparison.Ordinal)) return;
        if (target.HullHpMax <= 0) return;

        // Must be below 10% hull
        int captureThreshold = Math.Max(1, target.HullHpMax / 10); // STRUCTURAL: 10% threshold
        if (target.HullHp > captureThreshold) return;

        // Player must be at same node
        var playerFleet = FindPlayerFleet(state);
        if (playerFleet == null) return;
        if (!string.Equals(playerFleet.CurrentNodeId, target.CurrentNodeId, StringComparison.Ordinal)) return;

        // Haven must be Tier 3+ (Operational — has hangar bay)
        if (state.Haven == null || !state.Haven.Discovered) return;
        if ((int)state.Haven.Tier < 3) return; // STRUCTURAL: tier 3 minimum

        // Check hangar capacity
        if (!HavenHangarSystem.CanStore(state, "capture_placeholder")) // Just checking capacity
        {
            // Try alternate check: count stored ships vs max bays
            int maxBays = HavenUpgradeSystem.GetMaxHangarBays(state.Haven.Tier);
            int maxStored = maxBays - 1; // STRUCTURAL: reserve one bay for active ship
            if (maxStored <= 0 || state.Haven.StoredShipIds.Count >= maxStored) return;
        }

        // Create captured fleet from NPC ship class
        var capturedFleetId = $"captured_{TargetFleetId}_{state.Tick}";
        var capturedFleet = new Fleet
        {
            Id = capturedFleetId,
            OwnerId = "player",
            ShipClassId = target.ShipClassId,
            CurrentNodeId = state.Haven.NodeId,
            State = FleetState.Idle,
            HullHp = captureThreshold, // Captured at threshold HP
            HullHpMax = target.HullHpMax,
            ShieldHp = 0,
            ShieldHpMax = target.ShieldHpMax,
            Speed = target.Speed,
            IsStored = true,
            FuelCapacity = target.FuelCapacity,
            FuelCurrent = 0, // Captured ships have no fuel
        };

        state.Fleets[capturedFleetId] = capturedFleet;
        state.Haven.StoredShipIds.Add(capturedFleetId);

        // Remove original NPC fleet (no respawn — captured, not destroyed)
        state.Fleets.Remove(TargetFleetId);

        // Remove from respawn queue if present
        state.NpcRespawnQueue.RemoveAll(e =>
            string.Equals(e.FleetId, TargetFleetId, StringComparison.Ordinal));

        // Player stat tracking
        if (state.PlayerStats != null)
            state.PlayerStats.NpcFleetsDestroyed++; // Counts as a "defeat" for stats
    }

    private static Fleet? FindPlayerFleet(SimState state)
    {
        foreach (var fleet in state.Fleets.Values)
        {
            if (string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal) && !fleet.IsStored)
                return fleet;
        }
        return null;
    }
}
