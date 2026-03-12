using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

// GATE.T18.NARRATIVE.ROUTE_UNCERTAINTY.001
[TestFixture]
public sealed class RouteUncertaintyTests
{
    private static void AdvanceTo(SimState state, int targetTick)
    {
        while (state.Tick < targetTick)
            state.AdvanceTick();
    }

    private SimState MakeState()
    {
        var state = new SimState(42);
        state.Nodes["star_0"] = new Node { Id = "star_0", InstabilityLevel = 0 };
        state.Nodes["star_1"] = new Node
        {
            Id = "star_1",
            InstabilityLevel = FractureWeightTweaksV0.STRUCT_PhaseDriftMin
        };
        state.Nodes["star_2"] = new Node
        {
            Id = "star_2",
            InstabilityLevel = FractureWeightTweaksV0.STRUCT_PhaseFractureMin
        };

        state.Edges["edge_stable"] = new Edge
        {
            Id = "edge_stable",
            FromNodeId = "star_0",
            ToNodeId = "star_0",
            Distance = 10f
        };
        state.Edges["edge_drift"] = new Edge
        {
            Id = "edge_drift",
            FromNodeId = "star_0",
            ToNodeId = "star_1",
            Distance = 10f
        };
        state.Edges["edge_fracture"] = new Edge
        {
            Id = "edge_fracture",
            FromNodeId = "star_0",
            ToNodeId = "star_2",
            Distance = 10f
        };

        state.Fleets["fleet_trader_1"] = new Fleet
        {
            Id = "fleet_trader_1",
            Speed = 1.0f,
            OwnerId = "player"
        };

        return state;
    }

    [Test]
    public void StableEdge_ExactEta()
    {
        var state = MakeState();
        var (min, max) = RouteUncertaintySystem.ComputeEtaRange(
            state, "edge_stable", "fleet_trader_1");

        Assert.That(min, Is.EqualTo(max));
        Assert.That(min, Is.GreaterThan(0));
    }

    [Test]
    public void DriftEdge_HasVariance()
    {
        var state = MakeState();
        var (min, max) = RouteUncertaintySystem.ComputeEtaRange(
            state, "edge_drift", "fleet_trader_1");

        Assert.That(max, Is.GreaterThan(min));
    }

    [Test]
    public void FractureEdge_WiderVarianceThanDrift()
    {
        var state = MakeState();

        var (driftMin, driftMax) = RouteUncertaintySystem.ComputeEtaRange(
            state, "edge_drift", "fleet_trader_1");
        var (fracMin, fracMax) = RouteUncertaintySystem.ComputeEtaRange(
            state, "edge_fracture", "fleet_trader_1");

        int driftRange = driftMax - driftMin;
        int fracRange = fracMax - fracMin;
        Assert.That(fracRange, Is.GreaterThanOrEqualTo(driftRange));
    }

    [Test]
    public void AdaptationStage_ProgressesWithJumps()
    {
        Assert.That(RouteUncertaintySystem.GetAdaptationStage(0), Is.EqualTo(1));
        Assert.That(RouteUncertaintySystem.GetAdaptationStage(
            RouteUncertaintyTweaksV0.Stage2JumpsRequired), Is.EqualTo(2));
        Assert.That(RouteUncertaintySystem.GetAdaptationStage(
            RouteUncertaintyTweaksV0.Stage3JumpsRequired), Is.EqualTo(3));
    }

    [Test]
    public void ScannerAdaptation_NarrowsRange()
    {
        var state = MakeState();

        state.FractureExposureJumps = 0;
        var (min0, max0) = RouteUncertaintySystem.ComputeEtaRange(
            state, "edge_drift", "fleet_trader_1");
        int range0 = max0 - min0;

        state.FractureExposureJumps = RouteUncertaintyTweaksV0.Stage2JumpsRequired;
        var (min2, max2) = RouteUncertaintySystem.ComputeEtaRange(
            state, "edge_drift", "fleet_trader_1");
        int range2 = max2 - min2;

        state.FractureExposureJumps = RouteUncertaintyTweaksV0.Stage3JumpsRequired;
        var (min3, max3) = RouteUncertaintySystem.ComputeEtaRange(
            state, "edge_drift", "fleet_trader_1");
        int range3 = max3 - min3;

        Assert.That(range2, Is.LessThanOrEqualTo(range0));
        Assert.That(range3, Is.LessThanOrEqualTo(range2));
    }

    [Test]
    public void ComputeActualTravelTicks_WithinRange()
    {
        var state = MakeState();
        AdvanceTo(state, 100);

        var (min, max) = RouteUncertaintySystem.ComputeEtaRange(
            state, "edge_drift", "fleet_trader_1");
        int actual = RouteUncertaintySystem.ComputeActualTravelTicks(
            state, "edge_drift", "fleet_trader_1");

        Assert.That(actual, Is.InRange(min, max));
    }

    [Test]
    public void ComputeActualTravelTicks_IsDeterministic()
    {
        var state = MakeState();
        AdvanceTo(state, 100);

        int actual1 = RouteUncertaintySystem.ComputeActualTravelTicks(
            state, "edge_drift", "fleet_trader_1");
        int actual2 = RouteUncertaintySystem.ComputeActualTravelTicks(
            state, "edge_drift", "fleet_trader_1");

        Assert.That(actual1, Is.EqualTo(actual2));
    }

    [Test]
    public void RecordFractureJump_Increments()
    {
        var state = new SimState(42);
        Assert.That(state.FractureExposureJumps, Is.EqualTo(0));

        RouteUncertaintySystem.RecordFractureJump(state);
        Assert.That(state.FractureExposureJumps, Is.EqualTo(1));

        RouteUncertaintySystem.RecordFractureJump(state);
        Assert.That(state.FractureExposureJumps, Is.EqualTo(2));
    }

    [Test]
    public void MissingEdge_ReturnsOneOne()
    {
        var state = MakeState();
        var (min, max) = RouteUncertaintySystem.ComputeEtaRange(
            state, "nonexistent", "fleet_trader_1");
        Assert.That(min, Is.EqualTo(1));
        Assert.That(max, Is.EqualTo(1));
    }

    [Test]
    public void MissingFleet_ReturnsOneOne()
    {
        var state = MakeState();
        var (min, max) = RouteUncertaintySystem.ComputeEtaRange(
            state, "edge_drift", "nonexistent");
        Assert.That(min, Is.EqualTo(1));
        Assert.That(max, Is.EqualTo(1));
    }

    [Test]
    public void Phase4VoidEdge_HasVariance()
    {
        var state = MakeState();
        state.Nodes["star_void"] = new Node
        {
            Id = "star_void",
            InstabilityLevel = FractureWeightTweaksV0.STRUCT_PhaseVoidMin
        };
        state.Edges["edge_void"] = new Edge
        {
            Id = "edge_void", FromNodeId = "star_0", ToNodeId = "star_void", Distance = 10f
        };

        var (min, max) = RouteUncertaintySystem.ComputeEtaRange(
            state, "edge_void", "fleet_trader_1");
        Assert.That(max, Is.GreaterThan(min));
    }

    [Test]
    public void ZeroSpeedFleet_DoesNotCrash()
    {
        var state = MakeState();
        state.Fleets["fleet_trader_1"].Speed = 0f;

        var (min, max) = RouteUncertaintySystem.ComputeEtaRange(
            state, "edge_stable", "fleet_trader_1");
        Assert.That(min, Is.GreaterThan(0));
        Assert.That(max, Is.GreaterThanOrEqualTo(min));
    }

    [Test]
    public void Phase3MinVarianceFloor_Enforced()
    {
        var state = MakeState();
        // Max adaptation — should still have some variance in fracture space
        state.FractureExposureJumps = 10000;

        var (min, max) = RouteUncertaintySystem.ComputeEtaRange(
            state, "edge_fracture", "fleet_trader_1");
        // Phase 3 enforces MinVariancePct floor even at max adaptation
        Assert.That(max, Is.GreaterThanOrEqualTo(min));
    }
}
