using SimCore.Entities;
using System;
using System.Linq;
using System.Collections.Generic;

namespace SimCore.Systems;

public static class LogisticsSystem
{
    public static void Process(SimState state)
    {
        // 1. Process Active Jobs
        foreach (var fleet in state.Fleets.Values.Where(f => f.CurrentJob != null))
        {
            ProcessJob(state, fleet);
        }

        // 2. Dispatch Idle Fleets
        foreach (var fleet in state.Fleets.Values.Where(f => f.CurrentJob == null && f.OwnerId != "player"))
        {
            TryAssignJob(state, fleet);
        }
    }

    private static void ProcessJob(SimState state, Fleet fleet)
    {
        var job = fleet.CurrentJob!;

        switch (job.Stage)
        {
            case JobStage.EnRouteToSource:
                if (fleet.CurrentNodeId == job.SourceNodeId && fleet.State != FleetState.Traveling)
                {
                    job.Stage = JobStage.Loading;
                    fleet.CurrentTask = $"Loading {job.GoodId}";
                }
                else if (fleet.DestinationNodeId != job.SourceNodeId)
                {
                    // If we are idle (arrived at a waypoint), plan next step
                    if (fleet.State == FleetState.Idle)
                        StartTravel(state, fleet, job.SourceNodeId, $"Fetching {job.GoodId}");
                }
                break;

            case JobStage.Loading:
                job.Stage = JobStage.EnRouteToTarget;
                StartTravel(state, fleet, job.TargetNodeId, $"Hauling {job.GoodId} to {job.TargetNodeId}");
                break;

            case JobStage.EnRouteToTarget:
                if (fleet.CurrentNodeId == job.TargetNodeId && fleet.State != FleetState.Traveling)
                {
                    job.Stage = JobStage.Unloading;
                    fleet.CurrentTask = $"Unloading {job.GoodId}";
                }
                else if (fleet.DestinationNodeId != job.TargetNodeId)
                {
                    // If we are idle (arrived at a waypoint), plan next step
                    if (fleet.State == FleetState.Idle)
                        StartTravel(state, fleet, job.TargetNodeId, $"Hauling {job.GoodId} to {job.TargetNodeId}");
                }
                break;

            case JobStage.Unloading:
                if (state.Nodes.TryGetValue(job.TargetNodeId, out var node) && 
                    state.Markets.TryGetValue(node.MarketId, out var mkt))
                {
                    if (!mkt.Inventory.ContainsKey(job.GoodId)) mkt.Inventory[job.GoodId] = 0;
                    mkt.Inventory[job.GoodId] += job.Quantity;
                }
                fleet.CurrentJob = null;
                fleet.CurrentTask = "Idle";
                break;
        }
    }

    private static void StartTravel(SimState state, Fleet fleet, string finalTargetId, string taskDesc)
    {
        // ROUTING: Don't just look for direct edge. Find the NEXT HOP.
        string nextStepId = GetNextStep(state, fleet.CurrentNodeId, finalTargetId);
        
        if (string.IsNullOrEmpty(nextStepId))
        {
             fleet.State = FleetState.Idle;
             fleet.CurrentTask = $"No route to {finalTargetId}";
             return;
        }

        // Now resolve the specific edge for this single hop
        if (MapQueries.TryGetEdgeId(state, fleet.CurrentNodeId, nextStepId, out string edgeId))
        {
            fleet.DestinationNodeId = nextStepId; // HOP DESTINATION, NOT FINAL
            fleet.CurrentEdgeId = edgeId;
            fleet.State = FleetState.Traveling;
            fleet.TravelProgress = 0f;
            fleet.CurrentTask = taskDesc + $" (via {nextStepId})";
            
            if (state.Edges.TryGetValue(edgeId, out var edge)) edge.UsedCapacity++;
        }
        else
        {
            fleet.State = FleetState.Idle;
            fleet.CurrentTask = "Waiting for edge...";
        }
    }

    // BFS PATHFINDER
    private static string GetNextStep(SimState state, string startId, string endId)
    {
        if (startId == endId) return startId;
        
        // Check direct connection first (Optimization)
        if (MapQueries.AreConnected(state, startId, endId)) return endId;

        var frontier = new Queue<string>();
        frontier.Enqueue(startId);
        var cameFrom = new Dictionary<string, string>();
        cameFrom[startId] = null;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current == endId) break;

            // Find neighbors
            foreach (var edge in state.Edges.Values)
            {
                string next = null;
                if (edge.FromNodeId == current) next = edge.ToNodeId;
                else if (edge.ToNodeId == current) next = edge.FromNodeId;
                
                if (next != null && !cameFrom.ContainsKey(next))
                {
                    frontier.Enqueue(next);
                    cameFrom[next] = current;
                }
            }
        }

        // Reconstruct path to find the IMMEDIATE next step
        if (!cameFrom.ContainsKey(endId)) return null; // No path

        var curr = endId;
        while (cameFrom[curr] != startId)
        {
            curr = cameFrom[curr];
        }
        return curr;
    }

    private static void TryAssignJob(SimState state, Fleet fleet)
    {
        foreach (var site in state.IndustrySites.Values)
        {
            if (!site.Active) continue;
            if (!state.Nodes.TryGetValue(site.NodeId, out var siteNode)) continue;
            if (!state.Markets.TryGetValue(siteNode.MarketId, out var destMarket)) continue;

            foreach (var input in site.Inputs)
            {
                int currentStock = destMarket.Inventory.ContainsKey(input.Key) ? destMarket.Inventory[input.Key] : 0;
                
                if (currentStock < input.Value * 5)
                {
                    foreach (var sourceMkt in state.Markets.Values)
                    {
                        if (sourceMkt.Id == destMarket.Id) continue;
                        if (sourceMkt.Inventory.ContainsKey(input.Key) && sourceMkt.Inventory[input.Key] >= input.Value)
                        {
                            var sourceNode = state.Nodes.Values.FirstOrDefault(n => n.MarketId == sourceMkt.Id);
                            if (sourceNode == null) continue;

                            fleet.CurrentJob = new LogisticsJob
                            {
                                GoodId = input.Key,
                                Quantity = input.Value,
                                SourceNodeId = sourceNode.Id,
                                TargetNodeId = site.NodeId,
                                Stage = JobStage.EnRouteToSource
                            };
                            fleet.CurrentTask = $"Fetching {input.Key} from {sourceNode.Name}";
                            return;
                        }
                    }
                }
            }
        }
    }
}