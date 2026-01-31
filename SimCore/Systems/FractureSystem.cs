using SimCore.Entities;
using System;
using System.Numerics;

namespace SimCore.Systems;

public static class FractureSystem
{
    public static void Process(SimState state)
    {
        foreach (var fleet in state.Fleets.Values)
        {
            if (fleet.State != FleetState.FractureTraveling) continue;

            if (!state.Nodes.TryGetValue(fleet.CurrentNodeId, out var startNode)) continue;
            if (!state.Nodes.TryGetValue(fleet.DestinationNodeId, out var endNode)) continue;

            // Distance Calculation (Euclidean for Fracture/Void)
            float dist = Vector3.Distance(startNode.Position, endNode.Position);
            if (dist < 0.1f) dist = 0.1f;

            // Progress
            float progressStep = fleet.Speed / dist;
            fleet.TravelProgress += progressStep;

            if (fleet.TravelProgress >= 1.0f)
            {
                // Arrival Logic
                fleet.TravelProgress = 0f;
                fleet.CurrentNodeId = fleet.DestinationNodeId;
                fleet.DestinationNodeId = "";
                fleet.State = FleetState.Idle;
                
                // SLICE 3: TRACE GENERATION
                // Arriving via Fracture leaves a signature in the fabric
                endNode.Trace += 0.5f;
            }
        }
    }
}