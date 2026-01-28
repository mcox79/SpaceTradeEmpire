using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Entities;
using SimCore.Gen;

namespace SimCore.Tests;

public class MovementTests
{
    [Test]
    public void Fleet_CanTravel_BetweenConnectedNodes()
    {
        var kernel = new SimKernel(12345);
        
        // Setup: Tiny Galaxy
        GalaxyGenerator.Generate(kernel.State, 2, 50f);
        
        // Find two connected nodes
        var edge = kernel.State.Edges.Values.First();
        var startNode = edge.FromNodeId;
        var endNode = edge.ToNodeId;

        // Spawn Fleet
        var fleet = new Fleet { Id = "f1", CurrentNodeId = startNode, State = FleetState.Docked };
        kernel.State.Fleets.Add(fleet.Id, fleet);

        // COMMAND: Travel
        kernel.EnqueueCommand(new TravelCommand("f1", endNode));
        kernel.Step();

        // ASSERT: Moving
        Assert.That(fleet.State, Is.EqualTo(FleetState.Travel));
        Assert.That(fleet.DestinationNodeId, Is.EqualTo(endNode));
        Assert.That(fleet.TravelProgress, Is.GreaterThan(0f));

        // SIMULATE: Travel to completion (Speed 0.2 means ~5 ticks)
        for(int i=0; i<10; i++) kernel.Step();

        // ASSERT: Arrived
        Assert.That(fleet.State, Is.EqualTo(FleetState.Docked));
        Assert.That(fleet.CurrentNodeId, Is.EqualTo(endNode));
    }
}