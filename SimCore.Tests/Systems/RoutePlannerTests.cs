using NUnit.Framework;
using SimCore.Schemas;
using SimCore.Systems;
using SimCore.World;

namespace SimCore.Tests.Systems;

[TestFixture]
public sealed class RoutePlannerTests
{
    private static SimState StateWithWorld001()
    {
        var s = new SimState(123);

        var def = new WorldDefinition
        {
            WorldId = "micro_world_001",
            Markets =
            {
                new WorldMarket { Id = "mkt_a", Inventory = new() { ["ore"] = 10, ["food"] = 3 } },
                new WorldMarket { Id = "mkt_b", Inventory = new() { ["ore"] = 1,  ["food"] = 12 } }
            },
            Nodes =
            {
                new WorldNode { Id = "stn_a", Kind = "Station", Name = "Alpha Station", MarketId = "mkt_a", Pos = new float[] { 0f, 0f, 0f } },
                new WorldNode { Id = "stn_b", Kind = "Station", Name = "Beta Station",  MarketId = "mkt_b", Pos = new float[] { 10f, 0f, 0f } }
            },
            Edges =
            {
                new WorldEdge { Id = "lane_ab", FromNodeId = "stn_a", ToNodeId = "stn_b", Distance = 1.0f, TotalCapacity = 5 }
            },
            Player = new WorldPlayerStart { Credits = 1000, LocationNodeId = "stn_a" }
        };

        WorldLoader.Apply(s, def);
        return s;
    }

    [Test]
    public void World001_PlansDirectRoute()
    {
        var s = StateWithWorld001();

        var ok = RoutePlanner.TryPlan(s, "stn_a", "stn_b", speedAuPerTick: 0.5f, out var plan);

        Assert.That(ok, Is.True);
        Assert.That(plan.FromNodeId, Is.EqualTo("stn_a"));
        Assert.That(plan.ToNodeId, Is.EqualTo("stn_b"));
        Assert.That(plan.NodeIds, Is.EqualTo(new[] { "stn_a", "stn_b" }));
        Assert.That(plan.EdgeIds, Is.EqualTo(new[] { "lane_ab" }));
        Assert.That(plan.TotalTravelTicks, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void EqualCostPaths_TieBreaksByEdgeIdOrder()
    {
        var s = new SimState(123);

        var def = new WorldDefinition
        {
            WorldId = "micro_world_route_tie",
            Nodes =
            {
                new WorldNode { Id = "stn_a", Kind = "Station", Name = "A", MarketId = "", Pos = new float[] { 0f, 0f, 0f } },
                new WorldNode { Id = "stn_b", Kind = "Station", Name = "B", MarketId = "", Pos = new float[] { 1f, 0f, 0f } },
                new WorldNode { Id = "stn_c", Kind = "Station", Name = "C", MarketId = "", Pos = new float[] { 2f, 0f, 0f } },
                new WorldNode { Id = "stn_d", Kind = "Station", Name = "D", MarketId = "", Pos = new float[] { 1f, 1f, 0f } }
            },
            // Intentionally unsorted insertion order
            Edges =
            {
                new WorldEdge { Id = "lane_dc", FromNodeId = "stn_d", ToNodeId = "stn_c", Distance = 1.0f, TotalCapacity = 5 },
                new WorldEdge { Id = "lane_ad", FromNodeId = "stn_a", ToNodeId = "stn_d", Distance = 1.0f, TotalCapacity = 5 },
                new WorldEdge { Id = "lane_bc", FromNodeId = "stn_b", ToNodeId = "stn_c", Distance = 1.0f, TotalCapacity = 5 },
                new WorldEdge { Id = "lane_ab", FromNodeId = "stn_a", ToNodeId = "stn_b", Distance = 1.0f, TotalCapacity = 5 }
            },
            Player = new WorldPlayerStart { Credits = 0, LocationNodeId = "stn_a" }
        };

        WorldLoader.Apply(s, def);

        // With speed 1 AU/tick and all distances 1, both A->B->C and A->D->C cost 2 ticks.
        // Deterministic tie-break should pick edge "lane_ab" over "lane_ad" at the first choice.
        var ok = RoutePlanner.TryPlan(s, "stn_a", "stn_c", speedAuPerTick: 1.0f, out var plan);

        Assert.That(ok, Is.True);
        Assert.That(plan.NodeIds, Is.EqualTo(new[] { "stn_a", "stn_b", "stn_c" }));
        Assert.That(plan.EdgeIds, Is.EqualTo(new[] { "lane_ab", "lane_bc" }));
        Assert.That(plan.TotalTravelTicks, Is.EqualTo(2));
    }
}
