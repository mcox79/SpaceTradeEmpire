using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Entities;
using System.Linq;
using System.Numerics;

namespace SimCore.Tests;

public class MovementTests
{
    [Test]
    public void Fleet_CanTravel_BetweenConnectedNodes()
    {
        var kernel = new SimKernel(12345);
        
        // 1. SETUP: Manual deterministic map (No GalaxyGenerator)
        var n1 = new Node { Id = "A", Position = new Vector3(0,0,0) };
        var n2 = new Node { Id = "B", Position = new Vector3(100,0,0) }; // Dist = 100
        kernel.State.Nodes.Add(n1.Id, n1);
        kernel.State.Nodes.Add(n2.Id, n2);

        var edge = new Edge 
        { 
            Id = "E1", 
            FromNodeId = "A", 
            ToNodeId = "B", 
            Distance = 100f, 
            TotalCapacity = 5 
        };
        kernel.State.Edges.Add(edge.Id, edge);

        // 2. SPAWN FLEET
        // Speed 10 means 10 ticks to arrive (100 / 10)
        var fleet = new Fleet 
        { 
            Id = "f1", 
            CurrentNodeId = "A", 
            State = FleetState.Idle,
            Speed = 10f, 
            Supplies = 100
        };
        kernel.State.Fleets.Add(fleet.Id, fleet);

        // 3. EXECUTE DEPARTURE
        kernel.EnqueueCommand(new TravelCommand("f1", "B"));
        kernel.Step(); // Tick 1: Command runs, Movement runs (Progress -> 10/100 = 0.1)
        
        // ASSERT: Moving
        Assert.That(fleet.State, Is.EqualTo(FleetState.Traveling));
        Assert.That(fleet.DestinationNodeId, Is.EqualTo("B"));
        Assert.That(fleet.TravelProgress, Is.EqualTo(0.1f).Within(0.001f));
        Assert.That(edge.UsedCapacity, Is.EqualTo(1));
        
        // 4. SIMULATE TRANSIT (Run remaining 9 ticks)
        for(int i=0; i<9; i++) kernel.Step();
        
        // ASSERT: Arrival
        Assert.That(fleet.State, Is.EqualTo(FleetState.Idle));
        Assert.That(fleet.CurrentNodeId, Is.EqualTo("B"));
        Assert.That(edge.UsedCapacity, Is.EqualTo(0)); // Slot freed
    }
}