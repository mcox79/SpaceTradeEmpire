using System;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Commands;

// GATE.S7.FRACTURE.OFFLANE_ROUTES.001: Initiate offlane fracture jump to a non-adjacent node.
// Uses FractureSystem.ComputeOfflaneRoute for validation and cost.
// Fleet transitions to FractureTraveling state; FractureSystem.Process handles tick progress.
public sealed class OfflaneJumpCommand : ICommand
{
    public string FleetId { get; }
    public string TargetNodeId { get; }

    public OfflaneJumpCommand(string fleetId, string targetNodeId)
    {
        FleetId = fleetId ?? "";
        TargetNodeId = targetNodeId ?? "";
    }

    public void Execute(SimState state)
    {
        if (state is null) return;
        if (string.IsNullOrWhiteSpace(FleetId)) return;
        if (string.IsNullOrWhiteSpace(TargetNodeId)) return;

        if (!state.Fleets.TryGetValue(FleetId, out var fleet)) return;

        // Must not already be traveling.
        if (fleet.State == FleetState.Traveling || fleet.State == FleetState.FractureTraveling)
            return;

        var fromNodeId = fleet.CurrentNodeId;
        var route = FractureSystem.ComputeOfflaneRoute(state, FleetId, fromNodeId, TargetNodeId);
        if (!route.Valid) return;

        // Deduct fuel on departure.
        fleet.FuelCurrent -= route.FuelCost;

        // Initiate fracture travel to the target node.
        fleet.State = FleetState.FractureTraveling;
        fleet.DestinationNodeId = TargetNodeId;
        fleet.TravelProgress = 0f;
        fleet.CurrentTask = "FractureTraveling";

        // Clear any lane-based route state.
        fleet.RouteEdgeIds?.Clear();
        fleet.RouteEdgeIndex = 0;
        fleet.FinalDestinationNodeId = "";
        fleet.CurrentEdgeId = "";
        fleet.ManualOverrideNodeId = "";
        fleet.FractureTargetSiteId = "";
    }
}
