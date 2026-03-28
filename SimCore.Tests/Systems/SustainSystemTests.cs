using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Collections.Generic;

namespace SimCore.Tests.Systems;

// GATE.S7.SUSTAIN.FUEL_DEDUCT.001: Sustain system contract tests.
[TestFixture]
[Category("SustainSystem")]
public sealed class SustainSystemTests
{
    private SimState CreateMinimalState()
    {
        var state = new SimState(42);
        var nodeA = new Node { Id = "node_a", Name = "Alpha" };
        var nodeB = new Node { Id = "node_b", Name = "Beta" };
        state.Nodes["node_a"] = nodeA;
        state.Nodes["node_b"] = nodeB;

        var edge = new Edge
        {
            Id = "edge_a_b", FromNodeId = "node_a", ToNodeId = "node_b",
            Distance = 100f, TotalCapacity = 10
        };
        state.Edges["edge_a_b"] = edge;

        return state;
    }

    private Fleet CreateFleet(string id, string nodeId, int fuel)
    {
        var fleet = new Fleet
        {
            Id = id,
            OwnerId = "player",
            CurrentNodeId = nodeId,
            Speed = 0.5f,
            State = FleetState.Idle,
            FuelCapacity = 500,
            FuelCurrent = fuel,
        };
        return fleet;
    }

    [Test]
    public void MovingFleet_DeductsFuelPerTick()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleet("fleet_1", "node_a", 10);
        fleet.State = FleetState.Traveling;
        fleet.CurrentEdgeId = "edge_a_b";
        fleet.DestinationNodeId = "node_b";
        state.Fleets["fleet_1"] = fleet;

        SustainSystem.Process(state);

        int expectedFuel = 10 - SustainTweaksV0.FuelPerMoveTick;
        Assert.That(fleet.FuelCurrent, Is.EqualTo(expectedFuel));
    }

    [Test]
    public void IdleFleet_DoesNotConsumeFuel()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleet("fleet_1", "node_a", 10);
        fleet.State = FleetState.Idle;
        state.Fleets["fleet_1"] = fleet;

        // Idle at node → auto-refuels to capacity. Set capacity = current to prevent refuel.
        fleet.FuelCapacity = 10;
        SustainSystem.Process(state);

        Assert.That(fleet.FuelCurrent, Is.EqualTo(10));
    }

    [Test]
    public void DockedFleet_DoesNotConsumeFuel()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleet("fleet_1", "node_a", 10);
        fleet.State = FleetState.Docked;
        fleet.FuelCapacity = 10; // Prevent auto-refuel from changing value
        state.Fleets["fleet_1"] = fleet;

        SustainSystem.Process(state);

        Assert.That(fleet.FuelCurrent, Is.EqualTo(10));
    }

    [Test]
    public void FuelReachesZero_ClampsAtZero()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleet("fleet_1", "node_a", 1);
        fleet.State = FleetState.Traveling;
        fleet.CurrentEdgeId = "edge_a_b";
        state.Fleets["fleet_1"] = fleet;

        SustainSystem.Process(state);

        Assert.That(fleet.FuelCurrent, Is.EqualTo(0));
    }

    [Test]
    public void NoFuel_NoCrash_NoNegative()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleet("fleet_1", "node_a", 0);
        fleet.State = FleetState.Traveling;
        fleet.CurrentEdgeId = "edge_a_b";
        state.Fleets["fleet_1"] = fleet;

        // Should not throw, and fuel should stay at 0.
        SustainSystem.Process(state);

        Assert.That(fleet.FuelCurrent, Is.EqualTo(0));
    }

    [Test]
    public void FractureTraveling_AlsoConsumeFuel()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleet("fleet_1", "node_a", 5);
        fleet.State = FleetState.FractureTraveling;
        fleet.FractureTargetSiteId = "void_1";
        state.Fleets["fleet_1"] = fleet;

        SustainSystem.Process(state);

        Assert.That(fleet.FuelCurrent, Is.EqualTo(4));
    }

    [Test]
    public void MultipleFleets_IndependentFuelDeduction()
    {
        var state = CreateMinimalState();
        var fleet1 = CreateFleet("fleet_1", "node_a", 10);
        fleet1.State = FleetState.Traveling;
        fleet1.CurrentEdgeId = "edge_a_b";
        state.Fleets["fleet_1"] = fleet1;

        var fleet2 = CreateFleet("fleet_2", "node_a", 5);
        fleet2.State = FleetState.Idle;
        fleet2.FuelCapacity = 5; // Prevent auto-refuel
        state.Fleets["fleet_2"] = fleet2;

        SustainSystem.Process(state);

        Assert.That(fleet1.FuelCurrent, Is.EqualTo(9));
        Assert.That(fleet2.FuelCurrent, Is.EqualTo(5));
    }

    // Auto-refuel tests.

    [Test]
    public void AutoRefuel_IdleAtNode_TopsUpTank()
    {
        var state = CreateMinimalState();
        state.PlayerCredits = 5000; // Enough to cover full refuel (490 × 3 cr/unit).
        var fleet = CreateFleet("fleet_1", "node_a", 10);
        fleet.State = FleetState.Idle;
        state.Fleets["fleet_1"] = fleet;

        SustainSystem.Process(state);

        Assert.That(fleet.FuelCurrent, Is.EqualTo(fleet.FuelCapacity));
    }

    [Test]
    public void AutoRefuel_TravelingFleet_DoesNotRefuel()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleet("fleet_1", "node_a", 10);
        fleet.State = FleetState.Traveling;
        fleet.CurrentEdgeId = "edge_a_b";
        state.Fleets["fleet_1"] = fleet;

        SustainSystem.Process(state);

        Assert.That(fleet.FuelCurrent, Is.EqualTo(9)); // Only deducted, not refueled
    }

    [Test]
    public void AutoRefuel_NpcFleet_RefuelsFree()
    {
        var state = CreateMinimalState();
        var fleet = new Fleet
        {
            Id = "ai_fleet_1", OwnerId = "ai", CurrentNodeId = "node_a",
            Speed = 0.5f, State = FleetState.Idle,
            FuelCapacity = 500, FuelCurrent = 10
        };
        state.Fleets["ai_fleet_1"] = fleet;

        SustainSystem.Process(state);

        Assert.That(fleet.FuelCurrent, Is.EqualTo(500));
    }

    // GATE.S7.SUSTAIN.SHORTFALL.001: Shortfall immobilization tests.

    [Test]
    public void Shortfall_NoFuel_FleetImmobilized()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleet("fleet_1", "node_a", 0);
        fleet.State = FleetState.Idle;
        fleet.RouteEdgeIds = new List<string> { "edge_a_b" };
        fleet.RouteEdgeIndex = 0;
        // Fleet is at node_a but has no fuel — should auto-refuel first.
        // To test immobilization, remove from node so auto-refuel doesn't fire.
        fleet.CurrentNodeId = ""; // Not at a node → no auto-refuel
        state.Fleets["fleet_1"] = fleet;

        // SustainSystem won't refuel (no node). MovementSystem will check fuel.
        MovementSystem.Process(state);

        // Fleet has FuelCapacity > 0 but FuelCurrent = 0 → immobilized.
        // Note: without a CurrentNodeId, TryEnsureRoutePlanned won't start.
        // Let's use a proper setup instead.
        state.Fleets.Clear();
        var fleet2 = CreateFleet("fleet_2", "node_a", 0);
        fleet2.State = FleetState.Idle;
        fleet2.RouteEdgeIds = new List<string> { "edge_a_b" };
        fleet2.RouteEdgeIndex = 0;
        state.Fleets["fleet_2"] = fleet2;

        MovementSystem.Process(state);

        Assert.That(fleet2.State, Is.EqualTo(FleetState.Idle));
        Assert.That(fleet2.CurrentTask, Is.EqualTo("Immobilized:NoFuel"));
    }

    [Test]
    public void Shortfall_HasFuel_FleetCanMove()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleet("fleet_1", "node_a", 5);
        fleet.State = FleetState.Idle;
        fleet.RouteEdgeIds = new List<string> { "edge_a_b" };
        fleet.RouteEdgeIndex = 0;
        state.Fleets["fleet_1"] = fleet;

        MovementSystem.Process(state);

        Assert.That(fleet.State, Is.EqualTo(FleetState.Traveling));
    }

    [Test]
    public void Shortfall_Recovery_RefuelEnablesMovement()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleet("fleet_1", "node_a", 0);
        fleet.State = FleetState.Idle;
        fleet.RouteEdgeIds = new List<string> { "edge_a_b" };
        fleet.RouteEdgeIndex = 0;
        state.Fleets["fleet_1"] = fleet;

        MovementSystem.Process(state);
        Assert.That(fleet.CurrentTask, Is.EqualTo("Immobilized:NoFuel"));

        // Re-fuel the fleet.
        fleet.FuelCurrent = 10;

        MovementSystem.Process(state);
        Assert.That(fleet.State, Is.EqualTo(FleetState.Traveling));
    }

    [Test]
    public void Shortfall_ModuleDisabledOnSustainCycle_NoFuel()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleet("fleet_1", "node_a", 0);
        fleet.CurrentNodeId = ""; // No node → no auto-refuel
        fleet.Slots.Add(new ModuleSlot
        {
            SlotId = "weapon_1", SlotKind = SlotKind.Weapon,
            InstalledModuleId = "cannon", PowerDraw = 5
        });
        state.Fleets["fleet_1"] = fleet;

        // Advance tick to a sustain cycle boundary.
        while (state.Tick % SustainTweaksV0.SustainCycleTicks != 0)
            state.AdvanceTick();

        SustainSystem.Process(state);

        Assert.That(fleet.Slots[0].Disabled, Is.True, "Module should be disabled on sustain shortfall");
    }

    [Test]
    public void Shortfall_ModuleReenabledOnRecovery()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleet("fleet_1", "node_a", 0);
        fleet.CurrentNodeId = ""; // No node → no auto-refuel
        fleet.Slots.Add(new ModuleSlot
        {
            SlotId = "weapon_1", SlotKind = SlotKind.Weapon,
            InstalledModuleId = "cannon", PowerDraw = 5, Disabled = true
        });
        state.Fleets["fleet_1"] = fleet;

        // Re-fuel the fleet.
        fleet.FuelCurrent = 10;

        // Advance to sustain cycle boundary.
        while (state.Tick % SustainTweaksV0.SustainCycleTicks != 0)
            state.AdvanceTick();

        SustainSystem.Process(state);

        Assert.That(fleet.Slots[0].Disabled, Is.False, "Module should be re-enabled after re-supply");
    }

    [Test]
    public void Shortfall_ZeroPowerDraw_NotDisabled()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleet("fleet_1", "node_a", 0);
        fleet.CurrentNodeId = ""; // No node → no auto-refuel
        fleet.Slots.Add(new ModuleSlot
        {
            SlotId = "cargo_1", SlotKind = SlotKind.Cargo,
            InstalledModuleId = "cargo_hold", PowerDraw = 0
        });
        state.Fleets["fleet_1"] = fleet;

        // Advance to sustain cycle.
        while (state.Tick % SustainTweaksV0.SustainCycleTicks != 0)
            state.AdvanceTick();

        SustainSystem.Process(state);

        Assert.That(fleet.Slots[0].Disabled, Is.False, "Zero-power modules should not be disabled");
    }
}
