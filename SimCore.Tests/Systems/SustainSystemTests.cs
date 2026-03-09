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
        };
        if (fuel > 0)
            fleet.Cargo[WellKnownGoodIds.Fuel] = fuel;
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
        Assert.That(fleet.GetCargoUnits(WellKnownGoodIds.Fuel), Is.EqualTo(expectedFuel));
    }

    [Test]
    public void IdleFleet_DoesNotConsumeFuel()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleet("fleet_1", "node_a", 10);
        fleet.State = FleetState.Idle;
        state.Fleets["fleet_1"] = fleet;

        SustainSystem.Process(state);

        Assert.That(fleet.GetCargoUnits(WellKnownGoodIds.Fuel), Is.EqualTo(10));
    }

    [Test]
    public void DockedFleet_DoesNotConsumeFuel()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleet("fleet_1", "node_a", 10);
        fleet.State = FleetState.Docked;
        state.Fleets["fleet_1"] = fleet;

        SustainSystem.Process(state);

        Assert.That(fleet.GetCargoUnits(WellKnownGoodIds.Fuel), Is.EqualTo(10));
    }

    [Test]
    public void FuelReachesZero_GoodRemovedFromCargo()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleet("fleet_1", "node_a", 1);
        fleet.State = FleetState.Traveling;
        fleet.CurrentEdgeId = "edge_a_b";
        state.Fleets["fleet_1"] = fleet;

        SustainSystem.Process(state);

        Assert.That(fleet.Cargo.ContainsKey(WellKnownGoodIds.Fuel), Is.False,
            "Fuel key should be removed when depleted to 0");
    }

    [Test]
    public void NoFuel_NoCrash_NoNegative()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleet("fleet_1", "node_a", 0);
        fleet.State = FleetState.Traveling;
        fleet.CurrentEdgeId = "edge_a_b";
        state.Fleets["fleet_1"] = fleet;

        // Should not throw, and cargo should stay empty.
        SustainSystem.Process(state);

        Assert.That(fleet.GetCargoUnits(WellKnownGoodIds.Fuel), Is.EqualTo(0));
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

        Assert.That(fleet.GetCargoUnits(WellKnownGoodIds.Fuel), Is.EqualTo(4));
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
        state.Fleets["fleet_2"] = fleet2;

        SustainSystem.Process(state);

        Assert.That(fleet1.GetCargoUnits(WellKnownGoodIds.Fuel), Is.EqualTo(9));
        Assert.That(fleet2.GetCargoUnits(WellKnownGoodIds.Fuel), Is.EqualTo(5));
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
        // Ensure cargo is non-empty so fuel shortfall check triggers.
        fleet.Cargo["ore"] = 1;
        state.Fleets["fleet_1"] = fleet;

        MovementSystem.Process(state);

        Assert.That(fleet.State, Is.EqualTo(FleetState.Idle));
        Assert.That(fleet.CurrentTask, Is.EqualTo("Immobilized:NoFuel"));
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
        // Ensure cargo is non-empty so fuel shortfall check triggers.
        fleet.Cargo["ore"] = 1;
        state.Fleets["fleet_1"] = fleet;

        MovementSystem.Process(state);
        Assert.That(fleet.CurrentTask, Is.EqualTo("Immobilized:NoFuel"));

        // Re-fuel the fleet.
        fleet.Cargo[WellKnownGoodIds.Fuel] = 10;

        MovementSystem.Process(state);
        Assert.That(fleet.State, Is.EqualTo(FleetState.Traveling));
    }

    [Test]
    public void Shortfall_ModuleDisabledOnSustainCycle_NoFuel()
    {
        var state = CreateMinimalState();
        var fleet = CreateFleet("fleet_1", "node_a", 0);
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
        fleet.Slots.Add(new ModuleSlot
        {
            SlotId = "weapon_1", SlotKind = SlotKind.Weapon,
            InstalledModuleId = "cannon", PowerDraw = 5, Disabled = true
        });
        state.Fleets["fleet_1"] = fleet;

        // Re-fuel the fleet.
        fleet.Cargo[WellKnownGoodIds.Fuel] = 10;

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
