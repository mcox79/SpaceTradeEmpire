using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Commands;
using SimCore.Systems;
using System.Collections.Generic;

namespace SimCore.Tests;

public class TravelTests
{
    private SimState _state;
    private Fleet _fleet;
    private Node _nodeA;
    private Node _nodeB;
    private Edge _edge;

    [SetUp]
    public void Setup()
    {
        _state = new SimState(123);
        
        _nodeA = new Node { Id = "A" };
        _nodeB = new Node { Id = "B" };
        _state.Nodes.Add("A", _nodeA);
        _state.Nodes.Add("B", _nodeB);

        _edge = new Edge { Id = "A_B", FromNodeId = "A", ToNodeId = "B", Distance = 10f, TotalCapacity = 2 };
        _state.Edges.Add("A_B", _edge);

        _fleet = new Fleet { Id = "F1", CurrentNodeId = "A", Speed = 5f, Supplies = 100 };
        _state.Fleets.Add("F1", _fleet);
    }

    [Test]
    public void Travel_UpdatesState_AndConsumesSlot()
    {
        var cmd = new TravelCommand("F1", "B");
        cmd.Execute(_state);

        Assert.That(_fleet.State, Is.EqualTo(FleetState.Traveling));
        Assert.That(_fleet.CurrentEdgeId, Is.EqualTo("A_B"));
        Assert.That(_edge.UsedCapacity, Is.EqualTo(1));
    }

    [Test]
    public void MovementSystem_AdvancesProgress_AndArrives()
    {
        var cmd = new TravelCommand("F1", "B");
        cmd.Execute(_state);

        // Tick 1: Progress = 5 speed / 10 dist = 0.5
        MovementSystem.Process(_state);
        Assert.That(_fleet.TravelProgress, Is.EqualTo(0.5f));
        Assert.That(_fleet.State, Is.EqualTo(FleetState.Traveling));

        // Tick 2: Progress = 1.0 -> Arrival
        MovementSystem.Process(_state);
        Assert.That(_fleet.State, Is.EqualTo(FleetState.Idle));
        Assert.That(_fleet.CurrentNodeId, Is.EqualTo("B"));
        Assert.That(_edge.UsedCapacity, Is.EqualTo(0)); // Slot freed
    }
}