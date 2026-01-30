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
                    fleet.DestinationNodeId = job.SourceNodeId;
                    fleet.State = FleetState.Traveling;
                    fleet.TravelProgress = 0f;
                    fleet.CurrentTask = $"Fetching {job.GoodId}";
                }
                break;

            case JobStage.Loading:
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
                // Resolve Market correctly via Node lookup
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

    private static void TryAssignJob(SimState state, Fleet fleet)
    {
        // Iterate Industry Sites to find demand
        foreach (var site in state.IndustrySites.Values)
        {
            if (!site.Active) continue;
            
            // FIX: Look up the Node first, THEN the Market
            if (!state.Nodes.TryGetValue(site.NodeId, out var siteNode)) continue;
            if (!state.Markets.TryGetValue(siteNode.MarketId, out var destMarket)) continue;

            foreach (var input in site.Inputs)
            {
                int currentStock = destMarket.Inventory.ContainsKey(input.Key) ? destMarket.Inventory[input.Key] : 0;
                
                // If Factory is starving (buffer < 5x batch)
                if (currentStock < input.Value * 5)
                {
                    // Find Source: Iterate ALL Markets
                    foreach (var sourceMkt in state.Markets.Values)
                    {
                        // Don't buy from yourself
                        if (sourceMkt.Id == destMarket.Id) continue;

                        if (sourceMkt.Inventory.ContainsKey(input.Key) && sourceMkt.Inventory[input.Key] >= input.Value)
                        {
                            // Found a seller! Assign Job.
                            // We need the Node ID for the Source Market. 
                            // (In Slice 2 generator, NodeId and MarketId are linked, but let's reverse lookup or assumes known)
                            // Hack for Slice 2: We need the NODE ID of the source market to travel to.
                            // We can find the node that points to this market.
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