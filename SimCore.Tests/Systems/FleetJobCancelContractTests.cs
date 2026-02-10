using System;
using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Entities;
using SimCore.Gen;

namespace SimCore.Tests.Systems;

public class FleetJobCancelContractTests
{
    [Test]
    public void CancelJob_ClearsJobRouteAndTask_Deterministically()
    {
        const int seed = 123;

        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        // Prevent LogisticsSystem from assigning a new job after we cancel.
        foreach (var site in sim.State.IndustrySites.Values)
            site.Active = false;

        // Deterministic pick: lowest Fleet.Id
        var fleet = sim.State.Fleets.Values
            .OrderBy(f => f.Id, StringComparer.Ordinal)
            .First();

        // Precondition: craft an active job + route state (does not need to be runnable).
        fleet.CurrentJob = new LogisticsJob
        {
            GoodId = "ore",
            SourceNodeId = "N_A",
            TargetNodeId = "N_B",
            Amount = 5,
            Phase = LogisticsJobPhase.Pickup
        };

        fleet.RouteEdgeIds.Clear();
        fleet.RouteEdgeIds.Add("E_1");
        fleet.RouteEdgeIds.Add("E_2");
        fleet.RouteEdgeIndex = 1;
        fleet.FinalDestinationNodeId = "N_B";
        fleet.CurrentTask = "Fetching ore from N_A";

        // Act: enqueue command through SimKernel (canonical API in your codebase).
        sim.EnqueueCommand(new FleetJobCancelCommand(fleet.Id, "test_cancel"));
        sim.Step();

        // Assert: deterministic state transition per gate text
        Assert.That(fleet.CurrentJob, Is.Null, "Job should be cleared.");
        Assert.That(fleet.RouteEdgeIds.Count, Is.EqualTo(0), "Route edges should be cleared.");
        Assert.That(fleet.RouteEdgeIndex, Is.EqualTo(0), "Route edge index should reset.");
        Assert.That(fleet.FinalDestinationNodeId, Is.EqualTo(""), "Final destination should clear.");
        Assert.That(fleet.CurrentTask, Is.EqualTo("Idle"), "Task should update to Idle.");
    }

    [Test]
    public void CancelJob_WhenNoJob_DoesNotMutateFleetJobFields()
    {
        const int seed = 123;

        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        foreach (var site in sim.State.IndustrySites.Values)
            site.Active = false;

        var fleet = sim.State.Fleets.Values
            .OrderBy(f => f.Id, StringComparer.Ordinal)
            .First();

        // Baseline known values
        fleet.CurrentJob = null;
        fleet.RouteEdgeIds.Clear();
        fleet.RouteEdgeIndex = 0;
        fleet.FinalDestinationNodeId = "";
        fleet.CurrentTask = "Idle";

        sim.EnqueueCommand(new FleetJobCancelCommand(fleet.Id, "noop"));
        sim.Step();

        // Cannot assert world hash no-op because Step() advances simulation.
        Assert.That(fleet.CurrentJob, Is.Null);
        Assert.That(fleet.RouteEdgeIds.Count, Is.EqualTo(0));
        Assert.That(fleet.RouteEdgeIndex, Is.EqualTo(0));
        Assert.That(fleet.FinalDestinationNodeId, Is.EqualTo(""));
        Assert.That(fleet.CurrentTask, Is.EqualTo("Idle"));
    }
}
