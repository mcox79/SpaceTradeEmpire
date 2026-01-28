using SimCore.Entities;

namespace SimCore.Systems;

public static class MovementSystem
{
    public static void Process(SimState state)
    {
        foreach (var fleet in state.Fleets.Values)
        {
            if (fleet.State != FleetState.Travel) continue;

            // Advance Progress
            fleet.TravelProgress += fleet.Speed;

            // Arrival Check
            if (fleet.TravelProgress >= 1.0f)
            {
                // ARRIVAL LOGIC
                fleet.CurrentNodeId = fleet.DestinationNodeId;
                fleet.State = FleetState.Docked; // Auto-dock on arrival for now
                fleet.TravelProgress = 0f;
                fleet.CurrentEdgeId = "";
                fleet.DestinationNodeId = "";
            }
        }
    }
}