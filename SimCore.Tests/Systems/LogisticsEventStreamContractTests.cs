using NUnit.Framework;
using SimCore.Events;
using SimCore.Schemas;
using SimCore.Systems;
using SimCore.World;

namespace SimCore.Tests.Systems;

[TestFixture]
public sealed class LogisticsEventStreamContractTests
{
    private static SimKernel KernelWithThreeStations()
    {
        var k = new SimKernel(seed: 123);

        var def = new WorldDefinition
        {
            WorldId = "micro_world_logi_events_001",
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

        // Make travel single-tick per edge for stable stepping.
        k.State.Fleets["fleet_trader_1"].Speed = 1.0f;

        return k;
    }

    private static string RunAndGetEventJson()
    {
        var k = KernelWithThreeStations();
        var s = k.State;
        var f = s.Fleets["fleet_trader_1"];

        Assert.That(LogisticsSystem.PlanLogistics(s, f, "mkt_b", "mkt_c", "ore", 5), Is.True);

        // Enough ticks to: arrive source, issue pickup, switch phase, arrive dest, issue delivery, clear job.
        for (var i = 0; i < 10; i++) k.Step();

        var payload = LogisticsEvents.BuildPayload(s.Tick, s.LogisticsEventLog);
        var json = LogisticsEvents.ToDeterministicJson(payload);

        // Must be schema-bound
        LogisticsEvents.ValidateJsonIsSchemaBound(json);

        // Must contain at least the core lifecycle events
        Assert.That(s.LogisticsEventLog.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(s.LogisticsEventLog[0].Seq, Is.GreaterThan(0));

        return json;
    }

    [Test]
    public void LogisticsEvents_AreSchemaBound_AndDeterministicAcrossFreshRuns()
    {
        var a = RunAndGetEventJson();
        var b = RunAndGetEventJson();

        Assert.That(b, Is.EqualTo(a));
    }
}
