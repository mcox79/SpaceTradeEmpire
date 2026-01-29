using SimCore.Entities;
using System;
using System.Linq;

namespace SimCore.Commands;

public class TravelCommand : ICommand
{
    public string FleetId { get; set; }
    public string TargetNodeId { get; set; }

    public TravelCommand(string fleetId, string targetNodeId)
    {
        FleetId = fleetId;
        TargetNodeId = targetNodeId;
    }

    public void Execute(SimState state)
    {
        if (!state.Fleets.ContainsKey(FleetId)) return;
        var fleet = state.Fleets[FleetId];

        // 1. VALIDATION: Must be Idle
        if (fleet.State != FleetState.Idle) return;

        // 2. VALIDATION: Find connecting edge
        var edge = state.Edges.Values.FirstOrDefault(e => 
            (e.FromNodeId == fleet.CurrentNodeId && e.ToNodeId == TargetNodeId) ||
            (e.FromNodeId == TargetNodeId && e.ToNodeId == fleet.CurrentNodeId)
        );

        if (edge == null) return; // No direct link

        // 3. ARCHITECTURE: Check Slot Capacity (Simplified for Slice 1)
        if (edge.UsedCapacity >= edge.TotalCapacity) return; 

        // 4. COMMIT TO TRAVEL
        fleet.State = FleetState.Traveling;
        fleet.CurrentEdgeId = edge.Id;
        fleet.DestinationNodeId = TargetNodeId;
        fleet.TravelProgress = 0f;
        
        // Occupy Slot
        edge.UsedCapacity++;
    }
}