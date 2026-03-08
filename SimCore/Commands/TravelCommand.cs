using SimCore.Entities;
using SimCore.Tweaks;
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

        // 4. TRANSIT COST: Player fleets pay credits scaled by lane congestion.
        if (fleet.OwnerId == "player")
        {
            int cost = ComputeTransitCost(edge);
            if (cost > 0)
            {
                if (state.PlayerCredits < cost) return; // Cannot afford — UI should pre-validate.
                state.PlayerCredits -= cost;
            }
        }

        // 5. COMMIT TO TRAVEL
        fleet.State = FleetState.Traveling;
        fleet.CurrentEdgeId = edge.Id;
        fleet.DestinationNodeId = TargetNodeId;
        fleet.TravelProgress = 0f;

        // Occupy Slot
        edge.UsedCapacity++;
    }

    /// <summary>
    /// Transit cost = BaseCreditCost + congestion surcharge.
    /// Congestion = UsedCapacity / TotalCapacity (0.0–1.0).
    /// Surcharge = congestion * MaxCongestionSurcharge.
    /// Busy lanes cost more; empty lanes cost the base rate.
    /// </summary>
    public static int ComputeTransitCost(Edge edge)
    {
        int baseCost = TransitTweaksV0.BaseCreditCost;
        float congestion = 0f;
        if (edge.TotalCapacity > 0)
            congestion = (float)edge.UsedCapacity / edge.TotalCapacity;
        int surcharge = (int)(congestion * TransitTweaksV0.MaxCongestionSurcharge);
        return baseCost + surcharge;
    }
}
