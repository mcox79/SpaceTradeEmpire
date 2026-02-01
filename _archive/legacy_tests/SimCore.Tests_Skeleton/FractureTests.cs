using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using System.Numerics;

namespace SimCore.Tests;

public class FractureTests
{
    [Test]
    public void Fracture_Movement_AccumulatesTrace_OnArrival()
    {
        var state = new SimState(456);
        
        // Setup Nodes (10 units apart)
        var n1 = new Node { Id = "n1", Position = new Vector3(0, 0, 0), Trace = 0f };
        var n2 = new Node { Id = "n2", Position = new Vector3(10, 0, 0), Trace = 0f };
        state.Nodes.Add("n1", n1);
        state.Nodes.Add("n2", n2);

        // Setup Fleet (Speed 5, should take 2 ticks)
        var fleet = new Fleet 
        { 
            Id = "f1", 
            CurrentNodeId = "n1", 
            DestinationNodeId = "n2", 
            State = FleetState.FractureTraveling,
            Speed = 5.0f
        };
        state.Fleets.Add("f1", fleet);

        // Tick 1: Progress = 0.5
        FractureSystem.Process(state);
        Assert.That(fleet.State, Is.EqualTo(FleetState.FractureTraveling));
        Assert.That(fleet.TravelProgress, Is.EqualTo(0.5f).Within(0.01f));
        Assert.That(state.Nodes["n2"].Trace, Is.EqualTo(0f));

        // Tick 2: Arrival
        FractureSystem.Process(state);
        Assert.That(fleet.State, Is.EqualTo(FleetState.Idle));
        Assert.That(fleet.CurrentNodeId, Is.EqualTo("n2"));
        
        // Assert Trace Generation
        Assert.That(state.Nodes["n2"].Trace, Is.GreaterThan(0f));
    }
}