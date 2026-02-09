using System;
using NUnit.Framework;
using SimCore.Programs;
using SimCore.Schemas;
using SimCore.Systems;
using SimCore.World;

namespace SimCore.Tests.Systems;

[TestFixture]
public sealed class LogisticsJobExecutionIntegrationTests
{
    private static SimState StateWithThreeStationsAndOre()
    {
        var s = new SimState(123);

        var def = new WorldDefinition
        {
            WorldId = "micro_world_logi_exec_001",
            Markets =
            {
                new WorldMarket { Id = "mkt_a", Inventory = new() { ["ore"] = 0 } },
                new WorldMarket { Id = "mkt_b", Inventory = new() { ["ore"] = 10 } },
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

        // Make travel single-tick per edge
        s.Fleets["fleet_trader_1"].Speed = 1.0f;

        return s;
    }

    private static void StepLikeKernel(SimState s)
    {
        // Mirror SimKernel.Step ordering (excluding command queue, which this test does not use).
        LaneFlowSystem.Process(s);
        ProgramSystem.Process(s);
        IntentSystem.Process(s);
        MovementSystem.Process(s);
        LogisticsSystem.Process(s);
        IndustrySystem.Process(s);
        s.AdvanceTick();
    }

    [Test]
    public void LogisticsJob_ExecutesPickupAndDelivery_ExactlyOnce()
    {
        var s = StateWithThreeStationsAndOre();
        var f = s.Fleets["fleet_trader_1"];

        // Plan: source mkt_b (stn_b) -> dest mkt_c (stn_c), 5 ore
        var ok = LogisticsSystem.PlanLogistics(
            s,
            f,
            sourceMarketId: "mkt_b",
            destMarketId: "mkt_c",
            goodId: "ore",
            amount: 5);

        Assert.That(ok, Is.True);
        Assert.That(f.CurrentJob, Is.Not.Null);

        // Run enough steps for: travel to source, enqueue load; load; travel to dest, enqueue unload; unload
        for (var i = 0; i < 6; i++)
            StepLikeKernel(s);

        Assert.That(f.CurrentJob, Is.Null, "Job should be complete.");

        var src = s.Markets["mkt_b"];
        var dst = s.Markets["mkt_c"];

        Assert.That(src.Inventory.GetValueOrDefault("ore", 0), Is.EqualTo(5), "Source should have decreased by 5.");
        Assert.That(dst.Inventory.GetValueOrDefault("ore", 0), Is.EqualTo(5), "Destination should have increased by 5.");
        Assert.That(f.GetCargoUnits("ore"), Is.EqualTo(0), "Fleet cargo should be empty after delivery.");

        // Extra steps should not change totals (guards against double-issue when idle)
        for (var i = 0; i < 6; i++)
            StepLikeKernel(s);

        Assert.That(src.Inventory.GetValueOrDefault("ore", 0), Is.EqualTo(5));
        Assert.That(dst.Inventory.GetValueOrDefault("ore", 0), Is.EqualTo(5));
        Assert.That(f.GetCargoUnits("ore"), Is.EqualTo(0));
    }
}
