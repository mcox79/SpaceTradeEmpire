using System;
using SimCore.Entities;

namespace SimCore.Systems;

// GATE.S8.HAVEN.HANGAR.001: Haven hangar — multi-ship storage and swapping.
public static class HavenHangarSystem
{
    // Check if a fleet can be stored in the Haven hangar.
    public static bool CanStore(SimState state, string fleetId)
    {
        var haven = state.Haven;
        if (haven == null || !haven.Discovered) return false;

        int maxBays = HavenUpgradeSystem.GetMaxHangarBays(haven.Tier);
        // StoredShipIds count must be less than maxBays - 1 (one bay for active ship).
        int maxStored = maxBays - 1;
        if (maxStored <= 0) return false;
        if (haven.StoredShipIds.Count >= maxStored) return false;

        // Fleet must exist and not already be stored.
        if (!state.Fleets.TryGetValue(fleetId, out var fleet)) return false;
        if (fleet.IsStored) return false;

        return true;
    }

    // Store a fleet in the Haven hangar.
    public static bool StoreFleet(SimState state, string fleetId)
    {
        if (!CanStore(state, fleetId)) return false;

        var fleet = state.Fleets[fleetId];
        fleet.IsStored = true;
        fleet.State = FleetState.Idle;
        state.Haven.StoredShipIds.Add(fleetId);
        return true;
    }

    // Deploy a stored fleet (swap with active fleet at Haven).
    public static bool SwapShip(SimState state, string activeFleetId, string storedFleetId)
    {
        var haven = state.Haven;
        if (haven == null || !haven.Discovered) return false;

        if (!state.Fleets.TryGetValue(activeFleetId, out var activeFleet)) return false;
        if (!state.Fleets.TryGetValue(storedFleetId, out var storedFleet)) return false;

        // Active fleet must be at Haven node.
        if (!string.Equals(activeFleet.CurrentNodeId, haven.NodeId, StringComparison.Ordinal)) return false;

        // Stored fleet must actually be stored.
        if (!storedFleet.IsStored) return false;
        if (!haven.StoredShipIds.Contains(storedFleetId)) return false;

        // Swap: store the active fleet, deploy the stored one.
        activeFleet.IsStored = true;
        activeFleet.State = FleetState.Idle;
        haven.StoredShipIds.Remove(storedFleetId);
        haven.StoredShipIds.Add(activeFleetId);

        storedFleet.IsStored = false;
        storedFleet.State = FleetState.Docked;
        storedFleet.CurrentNodeId = haven.NodeId;
        storedFleet.DestinationNodeId = "";
        storedFleet.CurrentEdgeId = "";

        return true;
    }
}
