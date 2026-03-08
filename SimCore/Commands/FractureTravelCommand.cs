using System;
using System.Numerics;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Commands;

// GATE.S6.FRACTURE.TRAVEL_CMD.001: Initiate off-lane travel to a VoidSite.
// 10x slower than lane transit. Requires minimum TechLevel.
public sealed class FractureTravelCommand : ICommand
{
    public string FleetId { get; }
    public string VoidSiteId { get; }

    public FractureTravelCommand(string fleetId, string voidSiteId)
    {
        FleetId = fleetId ?? "";
        VoidSiteId = voidSiteId ?? "";
    }

    public void Execute(SimState state)
    {
        if (state is null) return;
        if (string.IsNullOrWhiteSpace(FleetId)) return;
        if (string.IsNullOrWhiteSpace(VoidSiteId)) return;

        if (!state.Fleets.TryGetValue(FleetId, out var fleet)) return;

        // Must not already be traveling.
        if (fleet.State == FleetState.Traveling || fleet.State == FleetState.FractureTraveling)
            return;

        // Must meet minimum tech level.
        if (fleet.TechLevel < FractureTweaksV0.MinFractureTravelTechLevel)
            return;

        // Target void site must exist.
        if (!state.VoidSites.TryGetValue(VoidSiteId, out var site))
            return;

        // Fleet must be at a node (has a position to depart from).
        if (string.IsNullOrWhiteSpace(fleet.CurrentNodeId)) return;
        if (!state.Nodes.TryGetValue(fleet.CurrentNodeId, out var node)) return;

        // GATE.S6.FRACTURE.COST_MODEL.001: Must have enough fuel.
        if (fleet.Supplies < FractureTweaksV0.FractureFuelPerJump) return;

        // GATE.S6.FRACTURE.COST_MODEL.001: Deduct fuel on departure.
        fleet.Supplies -= FractureTweaksV0.FractureFuelPerJump;

        // Initiate fracture travel.
        fleet.State = FleetState.FractureTraveling;
        fleet.FractureTargetSiteId = VoidSiteId;
        fleet.TravelProgress = 0f;
        fleet.CurrentTask = "FractureTraveling";

        // Clear any lane-based route state.
        fleet.RouteEdgeIds?.Clear();
        fleet.RouteEdgeIndex = 0;
        fleet.FinalDestinationNodeId = "";
        fleet.DestinationNodeId = "";
        fleet.CurrentEdgeId = "";
        fleet.ManualOverrideNodeId = "";
    }
}
