using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Collections.Generic;

namespace SimCore.Tests.Systems;

// GATE.T18.NARRATIVE.FRACTURE_WEIGHT.001
[TestFixture]
public sealed class FractureWeightTests
{
    private static void AdvanceTo(SimState state, int targetTick)
    {
        while (state.Tick < targetTick)
            state.AdvanceTick();
    }

    [Test]
    public void GetInstabilityPhase_CorrectForAllPhases()
    {
        Assert.That(FractureWeightSystem.GetInstabilityPhase(0), Is.EqualTo(0));
        Assert.That(FractureWeightSystem.GetInstabilityPhase(
            FractureWeightTweaksV0.STRUCT_PhaseShimmerMin), Is.EqualTo(1));
        Assert.That(FractureWeightSystem.GetInstabilityPhase(
            FractureWeightTweaksV0.STRUCT_PhaseDriftMin), Is.EqualTo(2));
        Assert.That(FractureWeightSystem.GetInstabilityPhase(
            FractureWeightTweaksV0.STRUCT_PhaseFractureMin), Is.EqualTo(3));
        Assert.That(FractureWeightSystem.GetInstabilityPhase(
            FractureWeightTweaksV0.STRUCT_PhaseVoidMin), Is.EqualTo(4));
    }

    [Test]
    public void ComputeWeightBps_StablePhaseReturnsNoChange()
    {
        var state = new SimState(42);
        int bps = FractureWeightSystem.ComputeWeightBps(state, "fleet_1", "food", 0);
        Assert.That(bps, Is.EqualTo(FractureWeightTweaksV0.Phase0WeightBps));
    }

    [Test]
    public void ComputeWeightBps_UnstablePhaseReturnsWithinRange()
    {
        var state = new SimState(42);
        AdvanceTo(state, 100);

        int bps1 = FractureWeightSystem.ComputeWeightBps(
            state, "fleet_1", "food", FractureWeightTweaksV0.STRUCT_PhaseShimmerMin);
        Assert.That(bps1, Is.InRange(
            FractureWeightTweaksV0.Phase1MinWeightBps,
            FractureWeightTweaksV0.Phase1MaxWeightBps));

        int bps2 = FractureWeightSystem.ComputeWeightBps(
            state, "fleet_1", "food", FractureWeightTweaksV0.STRUCT_PhaseDriftMin);
        Assert.That(bps2, Is.InRange(
            FractureWeightTweaksV0.Phase2MinWeightBps,
            FractureWeightTweaksV0.Phase2MaxWeightBps));

        int bps3 = FractureWeightSystem.ComputeWeightBps(
            state, "fleet_1", "food", FractureWeightTweaksV0.STRUCT_PhaseFractureMin);
        Assert.That(bps3, Is.InRange(
            FractureWeightTweaksV0.Phase3MinWeightBps,
            FractureWeightTweaksV0.Phase3MaxWeightBps));
    }

    [Test]
    public void ComputeWeightBps_IsDeterministic()
    {
        var state = new SimState(42);
        AdvanceTo(state, 100);

        int bps1 = FractureWeightSystem.ComputeWeightBps(
            state, "fleet_1", "food", FractureWeightTweaksV0.STRUCT_PhaseShimmerMin);
        int bps2 = FractureWeightSystem.ComputeWeightBps(
            state, "fleet_1", "food", FractureWeightTweaksV0.STRUCT_PhaseShimmerMin);

        Assert.That(bps1, Is.EqualTo(bps2));
    }

    [Test]
    public void ComputeWeightBps_DifferentTickGivesDifferentResult()
    {
        var results = new HashSet<int>();
        for (int t = 0; t < 100; t++)
        {
            var state = new SimState(42);
            AdvanceTo(state, t);
            int bps = FractureWeightSystem.ComputeWeightBps(
                state, "fleet_1", "food", FractureWeightTweaksV0.STRUCT_PhaseFractureMin);
            results.Add(bps);
        }

        Assert.That(results.Count, Is.GreaterThan(1));
    }

    [Test]
    public void Process_AdjustsCargoOnStableArrival()
    {
        var state = new SimState(42);
        AdvanceTo(state, 100);

        state.Nodes["stable"] = new Node { Id = "stable", InstabilityLevel = 0 };

        var fleet = new Fleet
        {
            Id = "fleet_trader_1",
            OwnerId = "player",
            CurrentNodeId = "stable"
        };
        fleet.Cargo["food"] = 100;
        fleet.CargoOriginPhase["food"] = FractureWeightTweaksV0.STRUCT_PhaseShimmerMin;
        state.Fleets["fleet_trader_1"] = fleet;

        state.ArrivalsThisTick.Add(("fleet_trader_1", "edge_1", "stable"));

        FractureWeightSystem.Process(state);

        int newQty = fleet.GetCargoUnits("food");
        Assert.That(newQty, Is.GreaterThanOrEqualTo(1));
        Assert.That(fleet.CargoOriginPhase.ContainsKey("food"), Is.False);
    }

    [Test]
    public void Process_NoAdjustmentAtUnstableDestination()
    {
        var state = new SimState(42);
        AdvanceTo(state, 100);

        state.Nodes["unstable"] = new Node
        {
            Id = "unstable",
            InstabilityLevel = FractureWeightTweaksV0.STRUCT_PhaseShimmerMin
        };

        var fleet = new Fleet
        {
            Id = "fleet_trader_1",
            OwnerId = "player",
            CurrentNodeId = "unstable"
        };
        fleet.Cargo["food"] = 100;
        fleet.CargoOriginPhase["food"] = FractureWeightTweaksV0.STRUCT_PhaseDriftMin;
        state.Fleets["fleet_trader_1"] = fleet;

        state.ArrivalsThisTick.Add(("fleet_trader_1", "edge_1", "unstable"));

        FractureWeightSystem.Process(state);

        Assert.That(fleet.GetCargoUnits("food"), Is.EqualTo(100));
        Assert.That(fleet.CargoOriginPhase.ContainsKey("food"), Is.True);
    }

    [Test]
    public void Process_EmptyArrivals_NoException()
    {
        var state = new SimState(42);
        FractureWeightSystem.Process(state);
        Assert.Pass();
    }

    [Test]
    public void RecordCargoOrigin_SetsPhase()
    {
        var state = new SimState(42);
        state.Nodes["star_0"] = new Node
        {
            Id = "star_0",
            InstabilityLevel = FractureWeightTweaksV0.STRUCT_PhaseDriftMin
        };

        var fleet = new Fleet
        {
            Id = "fleet_1",
            CurrentNodeId = "star_0"
        };
        state.Fleets["fleet_1"] = fleet;

        FractureWeightSystem.RecordCargoOrigin(state, fleet, "food");
        Assert.That(fleet.CargoOriginPhase["food"], Is.EqualTo(2));
    }

    [Test]
    public void RecordCargoOrigin_StableDoesNotSet()
    {
        var state = new SimState(42);
        state.Nodes["star_0"] = new Node { Id = "star_0", InstabilityLevel = 0 };

        var fleet = new Fleet
        {
            Id = "fleet_1",
            CurrentNodeId = "star_0"
        };
        state.Fleets["fleet_1"] = fleet;

        FractureWeightSystem.RecordCargoOrigin(state, fleet, "food");
        Assert.That(fleet.CargoOriginPhase.ContainsKey("food"), Is.False);
    }

    [Test]
    public void Process_NpcFleetArrival_Ignored()
    {
        var state = new SimState(42);
        AdvanceTo(state, 100);

        state.Nodes["stable"] = new Node { Id = "stable", InstabilityLevel = 0 };
        var npcFleet = new Fleet
        {
            Id = "npc_fleet", OwnerId = "npc_1", CurrentNodeId = "stable"
        };
        npcFleet.Cargo["food"] = 100;
        npcFleet.CargoOriginPhase["food"] = FractureWeightTweaksV0.STRUCT_PhaseFractureMin;
        state.Fleets["npc_fleet"] = npcFleet;

        state.ArrivalsThisTick.Add(("npc_fleet", "edge_1", "stable"));
        FractureWeightSystem.Process(state);

        Assert.That(npcFleet.GetCargoUnits("food"), Is.EqualTo(100));
    }

    [Test]
    public void Process_MinOneQuantityClamp()
    {
        var state = new SimState(42);
        AdvanceTo(state, 100);

        state.Nodes["stable"] = new Node { Id = "stable", InstabilityLevel = 0 };
        var fleet = new Fleet
        {
            Id = "fleet_trader_1", OwnerId = "player", CurrentNodeId = "stable"
        };
        // qty=1 with Phase 3 min weight (5000 bps) → 1*5000/10000=0 → clamped to 1
        fleet.Cargo["food"] = 1;
        fleet.CargoOriginPhase["food"] = FractureWeightTweaksV0.STRUCT_PhaseFractureMin;
        state.Fleets["fleet_trader_1"] = fleet;

        state.ArrivalsThisTick.Add(("fleet_trader_1", "edge_1", "stable"));
        FractureWeightSystem.Process(state);

        Assert.That(fleet.GetCargoUnits("food"), Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void Process_MultipleGoodsDifferentPhases()
    {
        var state = new SimState(42);
        AdvanceTo(state, 100);

        state.Nodes["stable"] = new Node { Id = "stable", InstabilityLevel = 0 };
        var fleet = new Fleet
        {
            Id = "fleet_trader_1", OwnerId = "player", CurrentNodeId = "stable"
        };
        fleet.Cargo["food"] = 100;
        fleet.Cargo["ore"] = 100;
        fleet.CargoOriginPhase["food"] = FractureWeightTweaksV0.STRUCT_PhaseShimmerMin;
        fleet.CargoOriginPhase["ore"] = FractureWeightTweaksV0.STRUCT_PhaseFractureMin;
        state.Fleets["fleet_trader_1"] = fleet;

        state.ArrivalsThisTick.Add(("fleet_trader_1", "edge_1", "stable"));
        FractureWeightSystem.Process(state);

        // Both should have been adjusted and origin phases cleared
        Assert.That(fleet.CargoOriginPhase.ContainsKey("food"), Is.False);
        Assert.That(fleet.CargoOriginPhase.ContainsKey("ore"), Is.False);
        Assert.That(fleet.GetCargoUnits("food"), Is.GreaterThanOrEqualTo(1));
        Assert.That(fleet.GetCargoUnits("ore"), Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void Process_MissingFleet_NoException()
    {
        var state = new SimState(42);
        state.ArrivalsThisTick.Add(("nonexistent", "edge_1", "star_0"));
        FractureWeightSystem.Process(state);
        Assert.Pass();
    }

    [Test]
    public void GetInstabilityPhase_BoundaryValues()
    {
        // Just below each threshold
        Assert.That(FractureWeightSystem.GetInstabilityPhase(
            FractureWeightTweaksV0.STRUCT_PhaseShimmerMin - 1), Is.EqualTo(0));
        Assert.That(FractureWeightSystem.GetInstabilityPhase(
            FractureWeightTweaksV0.STRUCT_PhaseDriftMin - 1), Is.EqualTo(1));
        Assert.That(FractureWeightSystem.GetInstabilityPhase(
            FractureWeightTweaksV0.STRUCT_PhaseFractureMin - 1), Is.EqualTo(2));
        Assert.That(FractureWeightSystem.GetInstabilityPhase(
            FractureWeightTweaksV0.STRUCT_PhaseVoidMin - 1), Is.EqualTo(3));
    }
}
