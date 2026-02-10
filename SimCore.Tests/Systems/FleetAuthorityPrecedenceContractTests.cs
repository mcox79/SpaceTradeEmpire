using System;
using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Entities;
using SimCore.Gen;

namespace SimCore.Tests.Systems;

public class FleetAuthorityPrecedenceContractTests
{
    [Test]
    public void ManualOverride_CancelsActiveLogisticsJob_AndDoesNotResumeWhenCleared()
    {
        const int seed = 123;

        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        // Prevent LogisticsSystem from assigning a new job after override cancels the current one.
        foreach (var site in sim.State.IndustrySites.Values)
            site.Active = false;

        var fleet = sim.State.Fleets.Values
            .OrderBy(f => f.Id, StringComparer.Ordinal)
            .First();

        // Install an active job and some route state.
        fleet.CurrentJob = new LogisticsJob
        {
            GoodId = "ore",
            SourceNodeId = "N_A",
            TargetNodeId = "N_B",
            Amount = 5,
            Phase = LogisticsJobPhase.Pickup,
            PickupTransferIssued = true,
            DeliveryTransferIssued = false
        };

        fleet.RouteEdgeIds.Clear();
        fleet.RouteEdgeIds.Add("E_1");
        fleet.RouteEdgeIndex = 0;
        fleet.FinalDestinationNodeId = "N_A";
        fleet.CurrentTask = "Fetching ore from N_A";

        // Deterministic override target that exists and differs from current.
        var targetNodeId = sim.State.Nodes.Values
            .OrderBy(n => n.Id, StringComparer.Ordinal)
            .Select(n => n.Id)
            .First(id => !string.Equals(id, fleet.CurrentNodeId, StringComparison.Ordinal));

        // Act: issuing manual override must cancel job.
        sim.EnqueueCommand(new FleetSetDestinationCommand(fleet.Id, targetNodeId, "test_override"));
        sim.Step();

        Assert.That(fleet.ManualOverrideNodeId, Is.EqualTo(targetNodeId));
        Assert.That(fleet.CurrentJob, Is.Null, "Manual override must cancel active logistics job.");

        // Do not assert FinalDestinationNodeId immediately after the command.
        // MovementSystem may already have an in-flight request; the override doctrine is that it takes precedence
        // and should drive FinalDestinationNodeId on the next planning observation.

        // Allow MovementSystem to observe override and set/plan toward override.
        sim.Step();

        Assert.That(fleet.FinalDestinationNodeId, Is.EqualTo(targetNodeId),
            "While override is set, routing request must align to override.");

        // Clearing override must NOT resume canceled job.
        sim.EnqueueCommand(new FleetSetDestinationCommand(fleet.Id, "", "test_clear"));
        sim.Step();

        Assert.That(fleet.ManualOverrideNodeId, Is.EqualTo(""));
        Assert.That(fleet.CurrentJob, Is.Null, "Clearing override must not resume canceled jobs.");
    }
}
