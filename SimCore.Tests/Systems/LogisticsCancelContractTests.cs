using NUnit.Framework;
using SimCore.Schemas;
using SimCore.Systems;
using SimCore.World;

namespace SimCore.Tests.Systems;

[TestFixture]
public sealed class LogisticsCancelContractTests
{
    private static SimKernel KernelWithThreeStations(int supplierOre)
    {
        var k = new SimKernel(seed: 123);

        var def = new WorldDefinition
        {
            WorldId = "micro_world_logi_cancel_001",
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
    public void Cancel_WhenPickupLoadsZero_ClearsFleetStateDeterministically()
    {
        // Supplier has 0 ore; job requests 5; pickup intent clamps to 0; job must cancel.
        var k = KernelWithThreeStations(supplierOre: 0);
        var s = k.State;

        var fleet = s.Fleets["fleet_trader_1"];
        fleet.Speed = 1.0f;

        Assert.That(LogisticsSystem.PlanLogistics(s, fleet, "mkt_b", "mkt_c", "ore", 5), Is.True);

        for (var i = 0; i < 20; i++)
            k.Step();

        Assert.That(fleet.CurrentJob, Is.Null, "Job should be canceled and cleared.");
        Assert.That(fleet.State, Is.EqualTo(Entities.FleetState.Idle));
        Assert.That(fleet.RouteEdgeIds.Count, Is.EqualTo(0));
        Assert.That(fleet.RouteEdgeIndex, Is.EqualTo(0));
        Assert.That(fleet.DestinationNodeId, Is.EqualTo(""));
        Assert.That(fleet.FinalDestinationNodeId, Is.EqualTo(""));
        Assert.That(fleet.CurrentTask, Is.EqualTo("Idle"));

        Assert.That(s.Markets["mkt_b"].Inventory["ore"], Is.EqualTo(0));
        Assert.That(s.Markets["mkt_c"].Inventory["ore"], Is.EqualTo(0));
        Assert.That(fleet.GetCargoUnits("ore"), Is.EqualTo(0));
    }
}
