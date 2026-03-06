using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

// GATE.S16.NPC_ALIVE.DELAY_ENFORCE.001
[TestFixture]
public sealed class MovementDelayTests
{
    private static SimState CreateTravelingFleetState()
    {
        var state = new SimState(42);

        var nodeA = new Node { Id = "star_a", Name = "Star A" };
        var nodeB = new Node { Id = "star_b", Name = "Star B" };
        state.Nodes["star_a"] = nodeA;
        state.Nodes["star_b"] = nodeB;

        var edge = new Edge
        {
            Id = "edge_a_b",
            FromNodeId = "star_a",
            ToNodeId = "star_b",
            Distance = 10f,
            TotalCapacity = 5
        };
        state.Edges["edge_a_b"] = edge;

        var fleet = new Fleet
        {
            Id = "npc_trader_1",
            OwnerId = "faction_a",
            Role = FleetRole.Trader,
            Speed = 1.0f,
            CurrentNodeId = "star_a",
            CurrentEdgeId = "edge_a_b",
            DestinationNodeId = "star_b",
            State = FleetState.Traveling,
            TravelProgress = 0.5f,
            DelayTicksRemaining = 0
        };
        state.Fleets["npc_trader_1"] = fleet;

        return state;
    }

    [Test]
    public void DelayedFleet_DoesNotAdvance()
    {
        var state = CreateTravelingFleetState();
        state.Fleets["npc_trader_1"].DelayTicksRemaining = 3;
        float progressBefore = state.Fleets["npc_trader_1"].TravelProgress;

        MovementSystem.Process(state);

        Assert.That(state.Fleets["npc_trader_1"].TravelProgress, Is.EqualTo(progressBefore));
        Assert.That(state.Fleets["npc_trader_1"].DelayTicksRemaining, Is.EqualTo(2));
    }

    [Test]
    public void DelayedFleet_ResumesAfterCountdown()
    {
        var state = CreateTravelingFleetState();
        state.Fleets["npc_trader_1"].DelayTicksRemaining = 1;
        float progressBefore = state.Fleets["npc_trader_1"].TravelProgress;

        // Tick 1: delay decrements to 0, no movement.
        MovementSystem.Process(state);
        Assert.That(state.Fleets["npc_trader_1"].TravelProgress, Is.EqualTo(progressBefore));
        Assert.That(state.Fleets["npc_trader_1"].DelayTicksRemaining, Is.EqualTo(0));

        // Tick 2: delay is 0, movement resumes.
        MovementSystem.Process(state);
        Assert.That(state.Fleets["npc_trader_1"].TravelProgress, Is.GreaterThan(progressBefore));
    }

    [Test]
    public void NonDelayedFleet_MovesNormally()
    {
        var state = CreateTravelingFleetState();
        float progressBefore = state.Fleets["npc_trader_1"].TravelProgress;

        MovementSystem.Process(state);

        Assert.That(state.Fleets["npc_trader_1"].TravelProgress, Is.GreaterThan(progressBefore));
    }
}
