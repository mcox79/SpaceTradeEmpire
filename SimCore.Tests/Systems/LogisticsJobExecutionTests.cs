using NUnit.Framework;
using SimCore.Schemas;
using SimCore.World;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

[TestFixture]
public sealed class LogisticsJobExecutionTests
{
    private static SimKernel KernelWithThreeStations()
    {
        var k = new SimKernel(seed: 123);

        var def = new WorldDefinition
        {
            WorldId = "micro_world_logi_exec_001",
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

        WorldLoader.Apply(k.State, def);
        return k;
    }

    [Test]
    public void PlannedJob_ExecutesPickupAndDelivery_ExactlyOnce()
    {
        var k = KernelWithThreeStations();
        var s = k.State;

        var fleet = s.Fleets["fleet_trader_1"];
        fleet.Speed = 1.0f;

        // Plan: pick up ore at stn_b (mkt_b) and deliver to stn_c (mkt_c)
        var ok = LogisticsSystem.PlanLogistics(
            s,
            fleet,
            sourceMarketId: "mkt_b",
            destMarketId: "mkt_c",
            goodId: "ore",
            amount: 5);

        Assert.That(ok, Is.True);
        Assert.That(fleet.CurrentJob, Is.Not.Null);

        // Step enough ticks to traverse A->B, pickup, B->C, deliver, and clear job.
        // With Speed=1 and Distance=1, each edge should complete in 1 tick of travel progress.
        for (var i = 0; i < 10; i++)
            k.Step();

        Assert.That(fleet.CurrentJob, Is.Null, "Job should complete and clear.");

        // Inventory should have moved: mkt_b reduced by 5, mkt_c increased by 5.
        var b = s.Markets["mkt_b"].Inventory["ore"];
        var c = s.Markets["mkt_c"].Inventory["ore"];

        Assert.That(b, Is.EqualTo(20));
        Assert.That(c, Is.EqualTo(5));

        // Cargo should not retain the shipped goods after delivery.
        Assert.That(fleet.GetCargoUnits("ore"), Is.EqualTo(0));
    }

    [Test]
    public void Execution_IsDeterministicAcrossFreshRuns()
    {
        var k1 = KernelWithThreeStations();
        var s1 = k1.State;
        var f1 = s1.Fleets["fleet_trader_1"];
        f1.Speed = 1.0f;

        Assert.That(LogisticsSystem.PlanLogistics(s1, f1, "mkt_b", "mkt_c", "ore", 5), Is.True);

        for (var i = 0; i < 10; i++)
            k1.Step();

        var b1 = s1.Markets["mkt_b"].Inventory["ore"];
        var c1 = s1.Markets["mkt_c"].Inventory["ore"];
        var n1 = f1.CurrentNodeId;

        var k2 = KernelWithThreeStations();
        var s2 = k2.State;
        var f2 = s2.Fleets["fleet_trader_1"];
        f2.Speed = 1.0f;

        Assert.That(LogisticsSystem.PlanLogistics(s2, f2, "mkt_b", "mkt_c", "ore", 5), Is.True);

        for (var i = 0; i < 10; i++)
            k2.Step();

        var b2 = s2.Markets["mkt_b"].Inventory["ore"];
        var c2 = s2.Markets["mkt_c"].Inventory["ore"];
        var n2 = f2.CurrentNodeId;

        Assert.That(b2, Is.EqualTo(b1));
        Assert.That(c2, Is.EqualTo(c1));
        Assert.That(n2, Is.EqualTo(n1));
        Assert.That(f2.GetCargoUnits("ore"), Is.EqualTo(f1.GetCargoUnits("ore")));
    }
}
