using SimCore.Entities;
using System;
using System.Linq;

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

        // 2. Dispatch Idle Fleets (Simple Greedy Algo)
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
                    // Start Travel
                    fleet.DestinationNodeId = job.SourceNodeId;
                    fleet.State = FleetState.Traveling;
                    fleet.TravelProgress = 0f;
                }
                break;

            case JobStage.Loading:
                // Instant Load for Slice 2
                job.Stage = JobStage.EnRouteToTarget;
                fleet.CurrentTask = $"Hauling {job.GoodId} to {job.TargetNodeId}";
                break;

            case JobStage.EnRouteToTarget:
                if (fleet.CurrentNodeId == job.TargetNodeId && fleet.State != FleetState.Traveling)
                {
                    job.Stage = JobStage.Unloading;
                    fleet.CurrentTask = $"Unloading {job.GoodId}";
                }
                else if (fleet.DestinationNodeId != job.TargetNodeId)
                {
                    fleet.DestinationNodeId = job.TargetNodeId;
                    fleet.State = FleetState.Traveling;
                    fleet.TravelProgress = 0f;
                }
                break;

            case JobStage.Unloading:
                // Instant Unload -> Complete
                // (Here we would actually transfer cargo in Slice 3)
                if (state.Markets.TryGetValue(job.TargetNodeId, out var mkt))
                {
                    if (!mkt.Inventory.ContainsKey(job.GoodId)) mkt.Inventory[job.GoodId] = 0;
                    mkt.Inventory[job.GoodId] += job.Quantity; // Teleport delivery for verification
                }
                fleet.CurrentJob = null;
                fleet.CurrentTask = "Idle";
                break;
        }
    }

    private static void TryAssignJob(SimState state, Fleet fleet)
    {
        // Find a starving industry site
        foreach (var site in state.IndustrySites.Values)
        {
            if (!site.Active) continue;
            foreach (var input in site.Inputs)
            {
                // Check if Factory needs it
                if (!state.Markets.TryGetValue(site.NodeId, out var destMarket)) continue;
                int currentStock = destMarket.Inventory.ContainsKey(input.Key) ? destMarket.Inventory[input.Key] : 0;
                
                if (currentStock < input.Value * 5) // Simple buffer check
                {
                    // Find Source
                    var sourceMkt = state.Markets.Values
                        .FirstOrDefault(m => m.Inventory.ContainsKey(input.Key) && m.Inventory[input.Key] >= input.Value && m.Id != site.NodeId);
                    
                    if (sourceMkt != null)
                    {
                        // Assign Job
                        fleet.CurrentJob = new LogisticsJob
                        {
                            GoodId = input.Key,
                            Quantity = input.Value,
                            SourceNodeId = sourceMkt.Id,
                            TargetNodeId = site.NodeId,
                            Stage = JobStage.EnRouteToSource
                        };
                        fleet.CurrentTask = $"Fetching {input.Key} from {sourceMkt.Id}";
                        return; // One job per tick
                    }
                }
            }
        }
    }
}