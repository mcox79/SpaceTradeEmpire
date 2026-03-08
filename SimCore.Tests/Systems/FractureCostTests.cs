using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Numerics;

namespace SimCore.Tests.Systems;

// GATE.S6.FRACTURE.COST_MODEL.001
public class FractureCostTests
{
    private SimState SetupFractureState()
    {
        var state = new SimState(42);

        // Create a fleet with enough supplies and hull
        var fleet = new Fleet
        {
            Id = "player_fleet",
            OwnerId = "player",
            CurrentNodeId = "node_a",
            State = FleetState.Idle,
            Speed = 1.0f,
            TechLevel = 2,
            HullHp = 100,
            HullHpMax = 200,
            Supplies = 50
        };
        state.Fleets["player_fleet"] = fleet;

        // Create nodes
        state.Nodes["node_a"] = new Node { Id = "node_a", Position = new Vector3(0, 0, 0) };
        state.Nodes["void_1"] = new Node { Id = "void_1", Position = new Vector3(10, 0, 0), IsFractureNode = true };

        // Create void site
        state.VoidSites["void_1"] = new VoidSite { Id = "void_1" };

        return state;
    }

    [Test]
    public void FractureDeparture_DeductsFuel()
    {
        var state = SetupFractureState();
        int suppliesBefore = state.Fleets["player_fleet"].Supplies;

        new FractureTravelCommand("player_fleet", "void_1").Execute(state);

        var fleet = state.Fleets["player_fleet"];
        Assert.That(fleet.State, Is.EqualTo(FleetState.FractureTraveling));
        Assert.That(fleet.Supplies, Is.EqualTo(suppliesBefore - FractureTweaksV0.FractureFuelPerJump));
    }

    [Test]
    public void FractureDeparture_InsufficientFuel_Blocked()
    {
        var state = SetupFractureState();
        state.Fleets["player_fleet"].Supplies = FractureTweaksV0.FractureFuelPerJump - 1;

        new FractureTravelCommand("player_fleet", "void_1").Execute(state);

        Assert.That(state.Fleets["player_fleet"].State, Is.EqualTo(FleetState.Idle));
    }

    [Test]
    public void FractureArrival_AppliesHullStress()
    {
        var state = SetupFractureState();
        new FractureTravelCommand("player_fleet", "void_1").Execute(state);

        var fleet = state.Fleets["player_fleet"];
        int hullBefore = fleet.HullHp;

        // Force arrival
        fleet.TravelProgress = 0.99f;
        fleet.DestinationNodeId = "void_1";
        FractureSystem.Process(state);

        Assert.That(fleet.HullHp, Is.EqualTo(hullBefore - FractureTweaksV0.FractureHullStressPerJump));
    }

    [Test]
    public void FractureArrival_HullStress_FlooredAtOne()
    {
        var state = SetupFractureState();
        new FractureTravelCommand("player_fleet", "void_1").Execute(state);

        var fleet = state.Fleets["player_fleet"];
        fleet.HullHp = 5; // Less than stress amount
        fleet.TravelProgress = 0.99f;
        fleet.DestinationNodeId = "void_1";
        FractureSystem.Process(state);

        Assert.That(fleet.HullHp, Is.EqualTo(1)); // Floor at 1, not killed
    }

    [Test]
    public void FractureArrival_AccumulatesTrace()
    {
        var state = SetupFractureState();
        float traceBefore = state.Nodes["void_1"].Trace;

        new FractureTravelCommand("player_fleet", "void_1").Execute(state);
        var fleet = state.Fleets["player_fleet"];
        fleet.TravelProgress = 0.99f;
        fleet.DestinationNodeId = "void_1";
        FractureSystem.Process(state);

        Assert.That(state.Nodes["void_1"].Trace,
            Is.EqualTo(traceBefore + FractureTweaksV0.FractureTracePerArrival).Within(0.01f));
    }

    [Test]
    public void FractureCostConstants_Positive()
    {
        Assert.That(FractureTweaksV0.FractureFuelPerJump, Is.GreaterThan(0));
        Assert.That(FractureTweaksV0.FractureHullStressPerJump, Is.GreaterThan(0));
        Assert.That(FractureTweaksV0.FractureTracePerArrival, Is.GreaterThan(0f));
    }
}
