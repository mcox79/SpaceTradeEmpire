using NUnit.Framework;
using SimCore.Schemas;
using SimCore.Systems;
using SimCore.World;

namespace SimCore.Tests.Systems;

[TestFixture]
public sealed class LogisticsPartialFulfillmentTests
{
    private static SimKernel KernelWithThreeStations(int supplierOre)
    {
        var k = new SimKernel(seed: 123);

        var def = new WorldDefinition
        {
            WorldId = "micro_world_logi_partial_001",
            Markets =
            {
                new WorldMarket { Id = "mkt_a", Inventory = new() { ["ore"] = 0 } },
                new WorldMarket { Id = "mkt_b", Inventory = new() { ["ore"] = supplierOre } },
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
    public void PartialPickup_DeliversOnlyWhatWasLoaded()
    {
        // Supplier has only 2 ore; job requests 5.
        var k = KernelWithThreeStations(supplierOre: 2);
        var s = k.State;

        var fleet = s.Fleets["fleet_trader_1"];
        fleet.Speed = 1.0f;

        Assert.That(LogisticsSystem.PlanLogistics(s, fleet, "mkt_b", "mkt_c", "ore", 5), Is.True);

        for (var i = 0; i < 20; i++)
            k.Step();

        Assert.That(fleet.CurrentJob, Is.Null, "Job should complete (partial fulfillment).");

        Assert.That(s.Markets["mkt_b"].Inventory["ore"], Is.EqualTo(0), "Supplier should be fully drained.");
        Assert.That(s.Markets["mkt_c"].Inventory["ore"], Is.EqualTo(2), "Destination should receive only what was available.");

        Assert.That(fleet.GetCargoUnits("ore"), Is.EqualTo(0), "Cargo should be empty after delivery.");
    }

    [Test]
    public void PartialPickup_IsDeterministicAcrossFreshRuns()
    {
        string RunOnce()
        {
            var k = KernelWithThreeStations(supplierOre: 2);
            var s = k.State;
            var fleet = s.Fleets["fleet_trader_1"];
            fleet.Speed = 1.0f;

            Assert.That(LogisticsSystem.PlanLogistics(s, fleet, "mkt_b", "mkt_c", "ore", 5), Is.True);

            for (var i = 0; i < 20; i++)
                k.Step();

            return s.GetSignature();
        }

        var a = RunOnce();
        var b = RunOnce();
        Assert.That(b, Is.EqualTo(a));
    }
}
