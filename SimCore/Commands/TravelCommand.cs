using SimCore.Entities;

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
        
        // RULE: Cannot move if already moving
        if (fleet.State == FleetState.Travel) return;

        // RULE: Must be connected
        // Simple linear search for Slice 1. Optimization later.
        string edgeId = "";
        foreach(var e in state.Edges.Values)
        {
            if ((e.FromNodeId == fleet.CurrentNodeId && e.ToNodeId == TargetNodeId) ||
                (e.ToNodeId == fleet.CurrentNodeId && e.FromNodeId == TargetNodeId))
            {
                edgeId = e.Id;
                break;
            }
        }

        if (string.IsNullOrEmpty(edgeId)) return; // No route

        // START TRAVEL
        fleet.State = FleetState.Travel;
        fleet.DestinationNodeId = TargetNodeId;
        fleet.CurrentEdgeId = edgeId;
        fleet.TravelProgress = 0f;
    }
}