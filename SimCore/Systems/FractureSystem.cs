using SimCore.Entities;
using System;
using System.Linq;
using System.Numerics;

namespace SimCore.Systems;

public static class FractureSystem
{
    // Deterministic ordering contract:
    // - Primary key: Fleet.Id (dictionary key)
    // - Sort: StringComparer.Ordinal
    // - Filter: only fleets in FractureTraveling state
    public static string[] GetFractureFleetProcessOrder(SimState state)
    {
        return state.Fleets.Keys
            .Where(id => state.Fleets[id].State == FleetState.FractureTraveling)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
    }

    public static void Process(SimState state)
    {
        var orderedFleetIds = GetFractureFleetProcessOrder(state);

        foreach (var fleetId in orderedFleetIds)
        {
            var fleet = state.Fleets[fleetId];

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
