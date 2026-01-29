using SimCore.Entities;
using System;

namespace SimCore.Systems;

public static class MovementSystem
{
    public static void Process(SimState state)
    {
        foreach (var fleet in state.Fleets.Values)
        {
            if (fleet.State != FleetState.Traveling) continue;

            if (!state.Edges.ContainsKey(fleet.CurrentEdgeId))
            {
                // Error recovery: snap back to node
                fleet.State = FleetState.Idle;
                continue;
            }

            var edge = state.Edges[fleet.CurrentEdgeId];

            // 1. CONSUME SUPPLIES (Operating Cost)
            // Architecture: "Ships pay OperatingCost... not Fuel"
            // For Slice 1: 1 unit per tick. If empty, speed drops.
            if (fleet.Supplies > 0)
            {
                fleet.Supplies--;
            }
            else
            {
                // Penalty: Move at 10% speed if starving
                // Note: Real implementation would morale shock here.
            }

            // 2. ADVANCE
            // Progress = Speed / Distance
            float effectiveSpeed = (fleet.Supplies > 0) ? fleet.Speed : (fleet.Speed * 0.1f);
            float progressStep = effectiveSpeed / Math.Max(1f, edge.Distance);
            
            fleet.TravelProgress += progressStep;

            // 3. ARRIVAL
            if (fleet.TravelProgress >= 1.0f)
            {
                fleet.TravelProgress = 0f;
                fleet.State = FleetState.Idle;
                fleet.CurrentNodeId = fleet.DestinationNodeId;
                fleet.DestinationNodeId = "";
                fleet.CurrentEdgeId = "";
                
                // Free the slot
                edge.UsedCapacity--;
            }
        }
    }
}