using NUnit.Framework;
using SimCore.Schemas;
using SimCore.Systems;
using SimCore.World;

namespace SimCore.Tests.Systems;

[TestFixture]
public sealed class LogisticsJobContractTests
{
    private static SimState StateWithThreeStations()
    {
        var s = new SimState(123);

        var def = new WorldDefinition
        {
            WorldId = "micro_world_logi_job_001",
            Markets =
            {
                new WorldMarket { Id = "mkt_a", Inventory = new() { ["ore"] = 0 } },
                new WorldMarket { Id = "mkt_b", Inventory = new() { ["ore"] = 25 } },
                new WorldMarket { Id = "mkt_c", Inventory = new() { ["ore"] = 0 } }
            },
            Nodes =
            {
                new WorldNode { Id = "stn_a", Kind = "Station", Name = "A", MarketId = "mkt_a", Pos = new float[] { 0f, 0f, 0f } },
                new WorldNode { Id = "stn_b", Kind = "Station", Name = "B", MarketId = "mkt_b", Pos = new float[] { 1f, 0f, 0f } },
                new WorldNode { Id = "stn_c", Kind = "Station", Name = "C", MarketId = "mkt_c", Pos = new float[] { 2f, 0f, 0f } }
            },
            Edges =
            {
                new WorldEdge { Id = "lane_ab", FromNodeId = "stn_a", ToNodeId = "stn_b", Distance = 1.0f, TotalCapacity = 5 },
                new WorldEdge { Id = "lane_bc", FromNodeId = "stn_b", ToNodeId = "stn_c", Distance = 1.0f, TotalCapacity = 5 }
            },
            Player = new WorldPlayerStart { Credits = 0, LocationNodeId = "stn_a" }
        };

        WorldLoader.Apply(s, def);
        return s;
    }

    [Test]
    public void PlanLogistics_CreatesRouteAwareJob_Deterministically()
    {
        var s1 = StateWithThreeStations();
        var f1 = s1.Fleets["fleet_trader_1"];
        f1.Speed = 1.0f;

        var ok1 = LogisticsSystem.PlanLogistics(
            s1,
            f1,
            sourceMarketId: "mkt_b",
            destMarketId: "mkt_c",
            goodId: "ore",
            amount: 5);

        Assert.That(ok1, Is.True);
        Assert.That(f1.CurrentJob, Is.Not.Null);

        var j1 = f1.CurrentJob!;
        Assert.That(j1.Phase, Is.EqualTo(SimCore.Entities.LogisticsJobPhase.Pickup));
        Assert.That(j1.SourceNodeId, Is.EqualTo("stn_b"));
        Assert.That(j1.TargetNodeId, Is.EqualTo("stn_c"));
        Assert.That(j1.RouteToSourceEdgeIds, Is.EqualTo(new[] { "lane_ab" }));
        Assert.That(j1.RouteToTargetEdgeIds, Is.EqualTo(new[] { "lane_bc" }));

        // Re-run on a fresh state and ensure identical job route fields
        var s2 = StateWithThreeStations();
        var f2 = s2.Fleets["fleet_trader_1"];
        f2.Speed = 1.0f;

        var ok2 = LogisticsSystem.PlanLogistics(
            s2,
            f2,
            sourceMarketId: "mkt_b",
            destMarketId: "mkt_c",
            goodId: "ore",
            amount: 5);

        Assert.That(ok2, Is.True);

        var j2 = f2.CurrentJob!;
        Assert.That(j2.Phase, Is.EqualTo(j1.Phase));
        Assert.That(j2.SourceNodeId, Is.EqualTo(j1.SourceNodeId));
        Assert.That(j2.TargetNodeId, Is.EqualTo(j1.TargetNodeId));
        Assert.That(j2.RouteToSourceEdgeIds, Is.EqualTo(j1.RouteToSourceEdgeIds));
        Assert.That(j2.RouteToTargetEdgeIds, Is.EqualTo(j1.RouteToTargetEdgeIds));
    }
}
