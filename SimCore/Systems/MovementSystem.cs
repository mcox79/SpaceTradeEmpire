using SimCore.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Systems;

public static class MovementSystem
{
    public static void Process(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));

        // Determinism: dictionary iteration order is not guaranteed.
        foreach (var fleet in state.Fleets.Values.OrderBy(f => f.Id, StringComparer.Ordinal))
        {
            // Slice 3: Route initiation and lane-by-lane travel.
            // Convention:
            // - If fleet.State is Idle/Docked and FinalDestinationNodeId is empty but DestinationNodeId is set,
            //   treat DestinationNodeId as "requested final destination" and plan a route.
            // - During route traversal, DestinationNodeId becomes "next hop" only.
            TryEnsureRoutePlanned(state, fleet);

            // If not traveling, try to start the next edge if a route exists.
            // IMPORTANT: if we successfully started Traveling, we should advance travel in the same tick.
            if (fleet.State != FleetState.Traveling)
            {
                TryStartNextEdgeIfRouted(state, fleet);

                // If we still didn't start traveling, nothing to do this tick for this fleet.
                if (fleet.State != FleetState.Traveling)
                    continue;
            }

            // Traveling: advance along current edge
            if (string.IsNullOrWhiteSpace(fleet.CurrentEdgeId))
            {
                // Corrupt/incomplete traveling state: drop to idle and let planner restart next tick.
                fleet.State = FleetState.Idle;
                fleet.CurrentTask = "Idle";
                continue;
            }

            if (!state.Edges.TryGetValue(fleet.CurrentEdgeId, out var edge))
            {
                // Fallback / Auto-Correction (kept from prior version)
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

            // Arrival handling
            if (fleet.TravelProgress >= 1.0f)
            {
                // Free the slot on arrival
                if (state.Edges.TryGetValue(fleet.CurrentEdgeId, out var arrivalEdge))
                {
                    arrivalEdge.UsedCapacity--;
                    if (arrivalEdge.UsedCapacity < 0) arrivalEdge.UsedCapacity = 0;
                }

                fleet.TravelProgress = 0f;

                // Move to the next hop node
                var arrivedNodeId = "";
                if (!string.IsNullOrWhiteSpace(fleet.DestinationNodeId))
                {
                    arrivedNodeId = fleet.DestinationNodeId;
                    fleet.CurrentNodeId = fleet.DestinationNodeId;
                }

                // GATE.S3_6.DISCOVERY_STATE.002: entering a node with seeded discovery markers marks Seen.
                // Determinism: IntelSystem sorts by DiscoveryId Ordinal before applying.
                if (!string.IsNullOrEmpty(arrivedNodeId)
                    && state.Nodes.TryGetValue(arrivedNodeId, out var arrivedNode)
                    && arrivedNode is not null
                    && arrivedNode.SeededDiscoveryIds is not null
                    && arrivedNode.SeededDiscoveryIds.Count != default)
                {
                    IntelSystem.ApplySeenFromNodeEntry(state, fleet.Id ?? "", arrivedNodeId, arrivedNode.SeededDiscoveryIds);
                }

                // Clear current edge hop state
                fleet.CurrentEdgeId = "";
                fleet.DestinationNodeId = "";

                // Mark this segment as completed
                if (fleet.RouteEdgeIds != null && fleet.RouteEdgeIds.Count > 0)
                {
                    if (fleet.RouteEdgeIndex < fleet.RouteEdgeIds.Count)
                    {
                        fleet.RouteEdgeIndex = Math.Min(fleet.RouteEdgeIndex + 1, fleet.RouteEdgeIds.Count);
                    }

                    // If route complete, clear route state
                    if (fleet.RouteEdgeIndex >= fleet.RouteEdgeIds.Count)
                    {
                        fleet.RouteEdgeIds.Clear();
                        fleet.RouteEdgeIndex = 0;
                        fleet.FinalDestinationNodeId = "";
                    }
                }

                // End of tick: do not start the next edge in the same Process() call.
                // Determinism + simplicity: one edge per tick per fleet.
                fleet.State = FleetState.Idle;
                fleet.CurrentTask = "Idle";
            }
        }
    }

    private static void TryEnsureRoutePlanned(SimState state, Fleet fleet)
    {
        // Manual override takes precedence over other destination requests.
        // While set, it overrides routing until cleared. (Slice 3 / GATE.UI.FLEET.003)
        if (!string.IsNullOrWhiteSpace(fleet.ManualOverrideNodeId)
            && fleet.State != FleetState.Traveling)
        {
            // If the current planned final differs, force a replan deterministically.
            if (!string.Equals(fleet.FinalDestinationNodeId, fleet.ManualOverrideNodeId, StringComparison.Ordinal))
            {
                fleet.RouteEdgeIds?.Clear();
                fleet.RouteEdgeIndex = 0;
                fleet.FinalDestinationNodeId = "";
                fleet.DestinationNodeId = "";
            }

            if ((fleet.RouteEdgeIds == null || fleet.RouteEdgeIds.Count == 0)
                && string.IsNullOrWhiteSpace(fleet.FinalDestinationNodeId))
            {
                fleet.FinalDestinationNodeId = fleet.ManualOverrideNodeId;
                fleet.DestinationNodeId = "";
            }
        }

        // If caller set only DestinationNodeId while idle/docked, treat it as final destination request.
        if (string.IsNullOrWhiteSpace(fleet.FinalDestinationNodeId)
            && !string.IsNullOrWhiteSpace(fleet.DestinationNodeId)
            && fleet.State != FleetState.Traveling
            && (fleet.RouteEdgeIds == null || fleet.RouteEdgeIds.Count == 0))
        {
            fleet.FinalDestinationNodeId = fleet.DestinationNodeId;
            fleet.DestinationNodeId = "";
        }

        if (fleet.RouteEdgeIds == null) fleet.RouteEdgeIds = new List<string>();

        if (fleet.RouteEdgeIds.Count > 0) return;
        if (string.IsNullOrWhiteSpace(fleet.FinalDestinationNodeId)) return;
        if (string.IsNullOrWhiteSpace(fleet.CurrentNodeId)) return;

        // Plan route deterministically.
        if (!RoutePlanner.TryPlan(state, fleet.CurrentNodeId, fleet.FinalDestinationNodeId, fleet.Speed, out var plan))
        {
            // No route: drop request.
            fleet.FinalDestinationNodeId = "";
            fleet.RouteEdgeIds.Clear();
            fleet.RouteEdgeIndex = 0;
            return;
        }

        fleet.RouteEdgeIds = plan.EdgeIds ?? new List<string>();
        fleet.RouteEdgeIndex = 0;

        // If route has no edges (same node), clear request.
        if (fleet.RouteEdgeIds.Count == 0)
        {
            fleet.FinalDestinationNodeId = "";
            fleet.RouteEdgeIndex = 0;
        }
    }

    private static void TryStartNextEdgeIfRouted(SimState state, Fleet fleet)
    {
        if (fleet.RouteEdgeIds == null || fleet.RouteEdgeIds.Count == 0) return;
        if (fleet.RouteEdgeIndex < 0 || fleet.RouteEdgeIndex >= fleet.RouteEdgeIds.Count) return;

        var nextEdgeId = fleet.RouteEdgeIds[fleet.RouteEdgeIndex];
        if (string.IsNullOrWhiteSpace(nextEdgeId)) return;

        if (!state.Edges.TryGetValue(nextEdgeId, out var edge)) return;

        // Sanity: ensure we're at the correct from node. If not, drop route (inconsistent state).
        if (!string.Equals(fleet.CurrentNodeId, edge.FromNodeId, StringComparison.Ordinal))
        {
            fleet.RouteEdgeIds.Clear();
            fleet.RouteEdgeIndex = 0;
            fleet.FinalDestinationNodeId = "";
            fleet.CurrentTask = "Idle";
            return;
        }

        // Capacity gate: deterministic because fleets are processed in sorted id order.
        if (edge.UsedCapacity >= edge.TotalCapacity)
        {
            fleet.State = FleetState.Idle;
            fleet.CurrentTask = "WaitingForLaneCapacity";
            return;
        }

        edge.UsedCapacity++;

        fleet.CurrentEdgeId = edge.Id;
        fleet.DestinationNodeId = edge.ToNodeId;
        fleet.State = FleetState.Traveling;
        fleet.CurrentTask = "Traveling";
        fleet.TravelProgress = 0f;
    }
}
