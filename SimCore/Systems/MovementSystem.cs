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

            // Edge Lookup
            if (!state.Edges.TryGetValue(fleet.CurrentEdgeId, out var edge))
            {
                // Fallback / Auto-Correction
                fleet.TravelProgress += fleet.Speed * 0.1f;
            }
            else
            {
                // Distance-based progress
                float dist = edge.Distance > 0 ? edge.Distance : 1f;
                float step = fleet.Speed / dist;
                fleet.TravelProgress += step;
                
                // SLICE 3: HEAT GENERATION
                if (fleet.CurrentJob != null && fleet.CurrentJob.Amount > 0)
                {
                    MarketSystem.RegisterTraffic(state, edge.Id, fleet.CurrentJob.Amount);
                }
            }

            // Arrival
            if (fleet.TravelProgress >= 1.0f)
            {
                // CRITICAL FIX: Free the slot on arrival
                if (state.Edges.TryGetValue(fleet.CurrentEdgeId, out var arrivalEdge))
                {
                    arrivalEdge.UsedCapacity--;
                    if (arrivalEdge.UsedCapacity < 0) arrivalEdge.UsedCapacity = 0;
                }

                fleet.TravelProgress = 0f;
                fleet.CurrentNodeId = fleet.DestinationNodeId;
                fleet.DestinationNodeId = "";
                fleet.CurrentEdgeId = "";
                fleet.State = FleetState.Idle;
            }
        }
    }
}