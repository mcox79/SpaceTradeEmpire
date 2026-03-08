using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Commands;

// GATE.S6.FRACTURE.TRAVEL_CMD.001: Fracture travel command + movement tests.
public class FractureTravelTests
{
    private static SimState BuildState()
    {
        var state = new SimState(42);
        state.Nodes["star_a"] = new Node
        {
            Id = "star_a",
            Kind = NodeKind.Star,
            Position = new Vector3(0, 0, 0),
        };
        state.VoidSites["void_01"] = new VoidSite
        {
            Id = "void_01",
            Position = new Vector3(50, 0, 50),
            Family = VoidSiteFamily.AsteroidField,
            MarkerState = VoidSiteMarkerState.Discovered,
            NearStarA = "star_a",
            NearStarB = "star_a",
        };
        state.Fleets["fleet_1"] = new Fleet
        {
            Id = "fleet_1",
            OwnerId = "player",
            CurrentNodeId = "star_a",
            State = FleetState.Docked,
            Speed = 0.5f,
            TechLevel = 1, // Meets minimum.
        };
        return state;
    }

    [Test]
    public void Execute_InitiatesFractureTravel()
    {
        var state = BuildState();
        var cmd = new FractureTravelCommand("fleet_1", "void_01");
        cmd.Execute(state);

        var fleet = state.Fleets["fleet_1"];
        Assert.That(fleet.State, Is.EqualTo(FleetState.FractureTraveling));
        Assert.That(fleet.FractureTargetSiteId, Is.EqualTo("void_01"));
        Assert.That(fleet.TravelProgress, Is.EqualTo(0f));
        Assert.That(fleet.CurrentTask, Is.EqualTo("FractureTraveling"));
    }

    [Test]
    public void Execute_InsufficientTechLevel_NoOp()
    {
        var state = BuildState();
        state.Fleets["fleet_1"].TechLevel = 0; // Below minimum.

        var cmd = new FractureTravelCommand("fleet_1", "void_01");
        cmd.Execute(state);

        Assert.That(state.Fleets["fleet_1"].State, Is.EqualTo(FleetState.Docked));
    }

    [Test]
    public void Execute_AlreadyTraveling_NoOp()
    {
        var state = BuildState();
        state.Fleets["fleet_1"].State = FleetState.Traveling;

        var cmd = new FractureTravelCommand("fleet_1", "void_01");
        cmd.Execute(state);

        Assert.That(state.Fleets["fleet_1"].State, Is.EqualTo(FleetState.Traveling));
        Assert.That(state.Fleets["fleet_1"].FractureTargetSiteId, Is.EqualTo(""));
    }

    [Test]
    public void Execute_InvalidVoidSite_NoOp()
    {
        var state = BuildState();
        var cmd = new FractureTravelCommand("fleet_1", "nonexistent");
        cmd.Execute(state);

        Assert.That(state.Fleets["fleet_1"].State, Is.EqualTo(FleetState.Docked));
    }

    [Test]
    public void Execute_NoCurrentNode_NoOp()
    {
        var state = BuildState();
        state.Fleets["fleet_1"].CurrentNodeId = "";

        var cmd = new FractureTravelCommand("fleet_1", "void_01");
        cmd.Execute(state);

        Assert.That(state.Fleets["fleet_1"].State, Is.EqualTo(FleetState.Docked));
    }

    [Test]
    public void Execute_ClearsLaneRouteState()
    {
        var state = BuildState();
        var fleet = state.Fleets["fleet_1"];
        fleet.RouteEdgeIds = new List<string> { "edge_1", "edge_2" };
        fleet.RouteEdgeIndex = 1;
        fleet.FinalDestinationNodeId = "star_b";
        fleet.ManualOverrideNodeId = "star_b";

        var cmd = new FractureTravelCommand("fleet_1", "void_01");
        cmd.Execute(state);

        Assert.That(fleet.RouteEdgeIds, Is.Empty);
        Assert.That(fleet.RouteEdgeIndex, Is.EqualTo(0));
        Assert.That(fleet.FinalDestinationNodeId, Is.EqualTo(""));
        Assert.That(fleet.ManualOverrideNodeId, Is.EqualTo(""));
    }

    [Test]
    public void Movement_AdvancesSlowerThanLane()
    {
        var state = BuildState();
        new FractureTravelCommand("fleet_1", "void_01").Execute(state);

        var fleet = state.Fleets["fleet_1"];
        float progressBefore = fleet.TravelProgress;

        MovementSystem.Process(state);

        Assert.That(fleet.TravelProgress, Is.GreaterThan(progressBefore));
        Assert.That(fleet.State, Is.EqualTo(FleetState.FractureTraveling));
    }

    [Test]
    public void Movement_ArrivesAtVoidSite()
    {
        var state = BuildState();
        new FractureTravelCommand("fleet_1", "void_01").Execute(state);

        var fleet = state.Fleets["fleet_1"];

        // Run many ticks to ensure arrival.
        for (int i = 0; i < 10000; i++)
        {
            MovementSystem.Process(state);
            if (fleet.State != FleetState.FractureTraveling) break;
        }

        Assert.That(fleet.State, Is.EqualTo(FleetState.Idle));
        Assert.That(fleet.CurrentNodeId, Is.EqualTo(""));
        Assert.That(fleet.FractureTargetSiteId, Is.EqualTo("void_01"));
        Assert.That(fleet.CurrentTask, Is.EqualTo("AtVoidSite"));
        Assert.That(fleet.TravelProgress, Is.EqualTo(0f));
    }

    [Test]
    public void Movement_Deterministic()
    {
        int TicksToArrive(int seed)
        {
            var state = new SimState(seed);
            state.Nodes["star_a"] = new Node
            {
                Id = "star_a",
                Kind = NodeKind.Star,
                Position = new Vector3(0, 0, 0),
            };
            state.VoidSites["void_01"] = new VoidSite
            {
                Id = "void_01",
                Position = new Vector3(50, 0, 50),
                Family = VoidSiteFamily.AsteroidField,
                NearStarA = "star_a",
                NearStarB = "star_a",
            };
            state.Fleets["fleet_1"] = new Fleet
            {
                Id = "fleet_1",
                CurrentNodeId = "star_a",
                State = FleetState.Docked,
                Speed = 0.5f,
                TechLevel = 1,
            };

            new FractureTravelCommand("fleet_1", "void_01").Execute(state);
            int ticks = 0;
            while (state.Fleets["fleet_1"].State == FleetState.FractureTraveling && ticks < 50000)
            {
                MovementSystem.Process(state);
                ticks++;
            }
            return ticks;
        }

        int run1 = TicksToArrive(42);
        int run2 = TicksToArrive(42);
        Assert.That(run1, Is.EqualTo(run2));
        Assert.That(run1, Is.GreaterThan(0));
    }
}
