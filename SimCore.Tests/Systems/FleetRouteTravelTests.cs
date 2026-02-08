using NUnit.Framework;
using SimCore.Schemas;
using SimCore.World;

namespace SimCore.Tests.Systems;

[TestFixture]
public sealed class FleetRouteTravelTests
{
    private static SimKernel KernelWithRouteWorld()
    {
        var k = new SimKernel(seed: 123);

        var def = new WorldDefinition
        {
            WorldId = "micro_world_route_001",
            Nodes =
            {
                new WorldNode { Id = "stn_a", Kind = "Station", Name = "A", MarketId = "", Pos = new float[] { 0f, 0f, 0f } },
                new WorldNode { Id = "stn_b", Kind = "Station", Name = "B", MarketId = "", Pos = new float[] { 1f, 0f, 0f } },
                new WorldNode { Id = "stn_c", Kind = "Station", Name = "C", MarketId = "", Pos = new float[] { 2f, 0f, 0f } }
            },
            Edges =
            {
                new WorldEdge { Id = "lane_ab", FromNodeId = "stn_a", ToNodeId = "stn_b", Distance = 1.0f, TotalCapacity = 5 },
                new WorldEdge { Id = "lane_bc", FromNodeId = "stn_b", ToNodeId = "stn_c", Distance = 1.0f, TotalCapacity = 5 }
            },
            Player = new WorldPlayerStart { Credits = 0, LocationNodeId = "stn_a" }
        };

        WorldLoader.Apply(k.State, def);
        return k;
    }

    [Test]
    public void FleetTravelsOneEdgePerTickFollowingPlannedRoute()
    {
        var k = KernelWithRouteWorld();
        var s = k.State;

        // WorldLoader creates a deterministic player fleet.
        var f = s.Fleets["fleet_trader_1"];

        // Make travel fast: 1 AU per tick on 1 AU edges => completes in a single MovementSystem.Process.
        f.Speed = 1.0f;

        // Request final destination using existing field (convention in MovementSystem).
        f.State = SimCore.Entities.FleetState.Idle;
        f.CurrentNodeId = "stn_a";
        f.DestinationNodeId = "stn_c";
        f.CurrentEdgeId = "";
        f.TravelProgress = 0f;

        // Tick 0 step: should traverse stn_a -> stn_b (one edge)
        k.Step();

        Assert.That(f.CurrentNodeId, Is.EqualTo("stn_b"));
        Assert.That(f.FinalDestinationNodeId, Is.EqualTo("stn_c"));
        Assert.That(f.RouteEdgeIds, Is.Not.Null);
        Assert.That(f.RouteEdgeIds.Count, Is.EqualTo(2));
        Assert.That(f.RouteEdgeIndex, Is.EqualTo(1));
        Assert.That(f.CurrentEdgeId, Is.EqualTo(""));
        Assert.That(f.DestinationNodeId, Is.EqualTo(""));

        // Tick 1 step: should traverse stn_b -> stn_c (second edge) and clear route
        k.Step();

        Assert.That(f.CurrentNodeId, Is.EqualTo("stn_c"));
        Assert.That(f.FinalDestinationNodeId, Is.EqualTo(""));
        Assert.That(f.RouteEdgeIds.Count, Is.EqualTo(0));
        Assert.That(f.RouteEdgeIndex, Is.EqualTo(0));
        Assert.That(f.CurrentEdgeId, Is.EqualTo(""));
        Assert.That(f.DestinationNodeId, Is.EqualTo(""));

        // Capacity should be released back to zero
        Assert.That(s.Edges["lane_ab"].UsedCapacity, Is.EqualTo(0));
        Assert.That(s.Edges["lane_bc"].UsedCapacity, Is.EqualTo(0));
    }
}
