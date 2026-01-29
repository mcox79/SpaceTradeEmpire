using SimCore.Entities;
using SimCore;

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
        if (!MapQueries.TryGetEdgeId(state, fleet.CurrentNodeId, TargetNodeId, out var edgeId)) return;

        // START TRAVEL
        fleet.State = FleetState.Travel;
        fleet.DestinationNodeId = TargetNodeId;
        fleet.CurrentEdgeId = edgeId;
        fleet.TravelProgress = 0f;
    }
}