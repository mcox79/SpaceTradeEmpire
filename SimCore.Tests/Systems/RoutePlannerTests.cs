using NUnit.Framework;
using SimCore.Events;
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

        var def = ScenarioHarnessV0.MicroWorld001();
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
        Assert.That(plan.RouteId, Is.EqualTo("stn_a>stn_b"));
    }

    [Test]
    public void MultiRouteCandidates_OrdersByHopsThenRiskThenRouteId()
    {
        var s = new SimState(123);

        var def = new WorldDefinition
        {
            WorldId = "micro_world_multi_routes",
            Nodes =
            {
                new WorldNode { Id = "stn_a", Kind = "Station", Name = "A", MarketId = "", Pos = new float[] { 0f, 0f, 0f } },
                new WorldNode { Id = "stn_b", Kind = "Station", Name = "B", MarketId = "", Pos = new float[] { 1f, 0f, 0f } },
                new WorldNode { Id = "stn_c", Kind = "Station", Name = "C", MarketId = "", Pos = new float[] { 1f, 1f, 0f } },
                new WorldNode { Id = "stn_d", Kind = "Station", Name = "D", MarketId = "", Pos = new float[] { 2f, 0f, 0f } }
            },
            // Two equal-hop routes A->B->D (lower risk) and A->C->D (higher risk via longer distances)
            Edges =
            {
                new WorldEdge { Id = "lane_ab", FromNodeId = "stn_a", ToNodeId = "stn_b", Distance = 1.0f, TotalCapacity = 5 },
                new WorldEdge { Id = "lane_bd", FromNodeId = "stn_b", ToNodeId = "stn_d", Distance = 1.0f, TotalCapacity = 5 },
                new WorldEdge { Id = "lane_ac", FromNodeId = "stn_a", ToNodeId = "stn_c", Distance = 2.0f, TotalCapacity = 5 },
                new WorldEdge { Id = "lane_cd", FromNodeId = "stn_c", ToNodeId = "stn_d", Distance = 2.0f, TotalCapacity = 5 }
            },
            Player = new WorldPlayerStart { Credits = 0, LocationNodeId = "stn_a" }
        };

        WorldLoader.Apply(s, def);

        var ok = RoutePlanner.TryPlanChoice(s, "stn_a", "stn_d", speedAuPerTick: 1.0f, maxCandidates: 8, out var choice);

        Assert.That(ok, Is.True);
        Assert.That(choice.CandidateCount, Is.GreaterThanOrEqualTo(2));
        Assert.That(choice.TieBreakReason, Is.EqualTo("RISK"));
        Assert.That(choice.ChosenRouteId, Is.EqualTo("stn_a>stn_b>stn_d"));
        Assert.That(choice.ChosenPlan.EdgeIds, Is.EqualTo(new[] { "lane_ab", "lane_bd" }));

        // Deterministic candidate ordering check.
        Assert.That(choice.Candidates[0].RouteId, Is.EqualTo("stn_a>stn_b>stn_d"));
        Assert.That(choice.Candidates[1].RouteId, Is.EqualTo("stn_a>stn_c>stn_d"));
    }

    [Test]
    public void EqualHopEqualRisk_TieBreaksByRouteId()
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

        var ok = RoutePlanner.TryPlanChoice(s, "stn_a", "stn_c", speedAuPerTick: 1.0f, maxCandidates: 8, out var choice);

        Assert.That(ok, Is.True);
        Assert.That(choice.CandidateCount, Is.GreaterThanOrEqualTo(2));
        Assert.That(choice.TieBreakReason, Is.EqualTo("ROUTE_ID"));
        Assert.That(choice.ChosenPlan.NodeIds, Is.EqualTo(new[] { "stn_a", "stn_b", "stn_c" }));
        Assert.That(choice.ChosenPlan.EdgeIds, Is.EqualTo(new[] { "lane_ab", "lane_bc" }));
        Assert.That(choice.ChosenPlan.TotalTravelTicks, Is.EqualTo(2));
    }

    [Test]
    public void RouteChosenEvent_IsSchemaBound()
    {
        var payload = LogisticsEvents.BuildPayload(0, new[]
        {
            new LogisticsEvents.Event
            {
                Type = LogisticsEvents.LogisticsEventType.RouteChosen,
                FleetId = "fleet_1",
                GoodId = "ore",
                Amount = 1,
                SourceNodeId = "stn_a",
                TargetNodeId = "stn_c",
                SourceMarketId = "mkt_a",
                TargetMarketId = "mkt_c",
                OriginId = "stn_a",
                DestId = "stn_c",
                ChosenRouteId = "stn_a>stn_b>stn_c",
                CandidateCount = 2,
                TieBreakReason = "ROUTE_ID",
                Note = "test"
            }
        });

        var json = LogisticsEvents.ToDeterministicJson(payload);
        LogisticsEvents.ValidateJsonIsSchemaBound(json);
    }
}
