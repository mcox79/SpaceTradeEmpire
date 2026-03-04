using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

// GATE.S3.RISK_SINKS.DELAY_MODEL.001: Contract tests for player-visible travel delay queries.
[TestFixture]
[Category("RiskDelay")]
public sealed class RiskDelayTests
{
    /// <summary>
    /// Creates a minimal SimState with a single edge of the specified distance.
    /// Distance controls the risk band via EdgeRiskScoreV0 (milli-AU thresholds).
    /// </summary>
    private static SimState CreateStateWithEdge(string edgeId, float distance)
    {
        var state = new SimState(42);

        var nodeA = new Node { Id = "node_a" };
        var nodeB = new Node { Id = "node_b" };
        state.Nodes["node_a"] = nodeA;
        state.Nodes["node_b"] = nodeB;

        var edge = new Edge
        {
            Id = edgeId,
            FromNodeId = "node_a",
            ToNodeId = "node_b",
            Distance = distance,
        };
        state.Edges[edgeId] = edge;

        return state;
    }

    [Test]
    public void GetRiskBpsForEdge_ReturnsCorrectBps_BandLow()
    {
        // Distance 0.3 AU = 300 milli-AU < 500 threshold => BandLow (band 0)
        var state = CreateStateWithEdge("lane_short", 0.3f);
        var (delayBps, lossBps, inspBps) = RiskSystem.GetRiskBpsForEdge(state, "lane_short");

        // Band 0 values at default scalar 1.0
        Assert.That(delayBps, Is.EqualTo(RiskModelV0.Band0DelayBps));
        Assert.That(lossBps, Is.EqualTo(RiskModelV0.Band0LossBps));
        Assert.That(inspBps, Is.EqualTo(RiskModelV0.Band0InspBps));
    }

    [Test]
    public void GetRiskBpsForEdge_ReturnsCorrectBps_BandMed()
    {
        // Distance 1.0 AU = 1000 milli-AU, 500..1500 => BandMed (band 1)
        var state = CreateStateWithEdge("lane_med", 1.0f);
        var (delayBps, lossBps, inspBps) = RiskSystem.GetRiskBpsForEdge(state, "lane_med");

        Assert.That(delayBps, Is.EqualTo(RiskModelV0.Band1DelayBps));
        Assert.That(lossBps, Is.EqualTo(RiskModelV0.Band1LossBps));
        Assert.That(inspBps, Is.EqualTo(RiskModelV0.Band1InspBps));
    }

    [Test]
    public void GetRiskBpsForEdge_ReturnsCorrectBps_BandHigh()
    {
        // Distance 2.0 AU = 2000 milli-AU, 1500..3000 => BandHigh (band 2)
        var state = CreateStateWithEdge("lane_long", 2.0f);
        var (delayBps, lossBps, inspBps) = RiskSystem.GetRiskBpsForEdge(state, "lane_long");

        Assert.That(delayBps, Is.EqualTo(RiskModelV0.Band2DelayBps));
        Assert.That(lossBps, Is.EqualTo(RiskModelV0.Band2LossBps));
        Assert.That(inspBps, Is.EqualTo(RiskModelV0.Band2InspBps));
    }

    [Test]
    public void GetRiskBpsForEdge_UnknownEdge_ReturnsZero()
    {
        var state = CreateStateWithEdge("lane_a", 1.0f);
        var (delayBps, lossBps, inspBps) = RiskSystem.GetRiskBpsForEdge(state, "nonexistent");

        Assert.That(delayBps, Is.EqualTo(0));
        Assert.That(lossBps, Is.EqualTo(0));
        Assert.That(inspBps, Is.EqualTo(0));
    }

    [Test]
    public void DelayTicksRemaining_DefaultsToZero()
    {
        var fleet = new Fleet { Id = "test_fleet" };
        Assert.That(fleet.DelayTicksRemaining, Is.EqualTo(0));
    }

    [Test]
    public void GetDelayTicksRemaining_ReturnsFleetValue()
    {
        var state = new SimState(42);
        var fleet = new Fleet { Id = "fleet_trader_1", DelayTicksRemaining = 7 };
        state.Fleets["fleet_trader_1"] = fleet;

        var result = RiskSystem.GetDelayTicksRemaining(state, "fleet_trader_1");
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void GetDelayTicksRemaining_UnknownFleet_ReturnsZero()
    {
        var state = new SimState(42);
        var result = RiskSystem.GetDelayTicksRemaining(state, "no_such_fleet");
        Assert.That(result, Is.EqualTo(0));
    }
}
