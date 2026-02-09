using NUnit.Framework;
using SimCore.Schemas;
using SimCore.Systems;
using SimCore.World;
using SimCore.Entities;
using SimCore.Intents;

namespace SimCore.Tests.Systems;

[TestFixture]
public sealed class LogisticsReservationContractTests
{
    private static SimKernel KernelWithSupplierAndTwoDestinations(int supplierOre)
    {
        var k = new SimKernel(seed: 123);

        var def = new WorldDefinition
        {
            WorldId = "micro_world_logi_reserve_001",
            Markets =
            {
                new WorldMarket { Id = "mkt_start", Inventory = new() { ["ore"] = 0 } },
                new WorldMarket { Id = "mkt_sup",   Inventory = new() { ["ore"] = supplierOre } },
                new WorldMarket { Id = "mkt_d1",    Inventory = new() { ["ore"] = 0 } },
                new WorldMarket { Id = "mkt_d2",    Inventory = new() { ["ore"] = 0 } }
            },
            Nodes =
            {
                new WorldNode { Id = "stn_start", Kind = "Station", Name = "START", MarketId = "mkt_start", Pos = new float[] { 0f, 0f, 0f } },
                new WorldNode { Id = "stn_sup",   Kind = "Station", Name = "SUP",   MarketId = "mkt_sup",   Pos = new float[] { 1f, 0f, 0f } },
                new WorldNode { Id = "stn_d1",    Kind = "Station", Name = "D1",    MarketId = "mkt_d1",    Pos = new float[] { 2f, 0f, 0f } },
                new WorldNode { Id = "stn_d2",    Kind = "Station", Name = "D2",    MarketId = "mkt_d2",    Pos = new float[] { 3f, 0f, 0f } }
            },
            Edges =
            {
                new WorldEdge { Id = "lane_start_sup", FromNodeId = "stn_start", ToNodeId = "stn_sup", Distance = 1.0f, TotalCapacity = 5 },
                new WorldEdge { Id = "lane_sup_d1",    FromNodeId = "stn_sup",   ToNodeId = "stn_d1",  Distance = 1.0f, TotalCapacity = 5 },
                new WorldEdge { Id = "lane_sup_d2",    FromNodeId = "stn_sup",   ToNodeId = "stn_d2",  Distance = 1.0f, TotalCapacity = 5 }
            },
            Player = new WorldPlayerStart { Credits = 0, LocationNodeId = "stn_start" }
        };

        WorldLoader.Apply(k.State, def);

        // Add a second logistics fleet deterministically
        if (!k.State.Fleets.ContainsKey("fleet_trader_2"))
        {
            k.State.Fleets["fleet_trader_2"] = new Fleet
            {
                Id = "fleet_trader_2",
                OwnerId = "player",
                CurrentNodeId = "stn_start",
                DestinationNodeId = "",
                CurrentEdgeId = "",
                State = FleetState.Docked,
                TravelProgress = 0f,
                Speed = 1.0f,
                CurrentTask = "Docked",
                CurrentJob = null,
                Supplies = 100
            };
        }

        // Add a third "thief" fleet used to attempt a competing load.
        if (!k.State.Fleets.ContainsKey("fleet_thief"))
        {
            k.State.Fleets["fleet_thief"] = new Fleet
            {
                Id = "fleet_thief",
                OwnerId = "npc",
                CurrentNodeId = "stn_sup",
                DestinationNodeId = "",
                CurrentEdgeId = "",
                State = FleetState.Docked,
                TravelProgress = 0f,
                Speed = 1.0f,
                CurrentTask = "Docked",
                CurrentJob = null,
                Supplies = 100
            };
        }

        // Make travel single-tick per edge.
        k.State.Fleets["fleet_trader_1"].Speed = 1.0f;
        k.State.Fleets["fleet_trader_2"].Speed = 1.0f;

        return k;
    }

    [Test]
    public void Reservation_ProtectsSupplierInventory_FromNonOwnerLoads()
    {
        // Supplier has 6 ore. Two jobs request 5 each.
        // Reservations should allocate 5 to fleet_trader_1 and 1 to fleet_trader_2 (order by fleet id).
        var k = KernelWithSupplierAndTwoDestinations(supplierOre: 6);
        var s = k.State;

        var f1 = s.Fleets["fleet_trader_1"];
        var f2 = s.Fleets["fleet_trader_2"];
        var thief = s.Fleets["fleet_thief"];

        Assert.That(LogisticsSystem.PlanLogistics(s, f1, "mkt_sup", "mkt_d1", "ore", 5), Is.True);
        Assert.That(LogisticsSystem.PlanLogistics(s, f2, "mkt_sup", "mkt_d2", "ore", 5), Is.True);

        // Competing load attempt should see only unreserved inventory (0) and load nothing.
        s.EnqueueIntent(new LoadCargoIntent(thief.Id, "mkt_sup", "ore", 999));
        IntentSystem.Process(s);

        Assert.That(thief.GetCargoUnits("ore"), Is.EqualTo(0), "Non-owner load must not consume reserved inventory.");

        // Run steps until both jobs complete.
        for (var i = 0; i < 40; i++)
            k.Step();

        Assert.That(f1.CurrentJob, Is.Null);
        Assert.That(f2.CurrentJob, Is.Null);

        Assert.That(s.Markets["mkt_sup"].Inventory.GetValueOrDefault("ore", 0), Is.EqualTo(0), "Supplier should be drained.");
        Assert.That(s.Markets["mkt_d1"].Inventory.GetValueOrDefault("ore", 0), Is.EqualTo(5), "Dest1 receives 5.");
        Assert.That(s.Markets["mkt_d2"].Inventory.GetValueOrDefault("ore", 0), Is.EqualTo(1), "Dest2 receives remaining 1.");

        Assert.That(f1.GetCargoUnits("ore"), Is.EqualTo(0));
        Assert.That(f2.GetCargoUnits("ore"), Is.EqualTo(0));

        Assert.That(s.LogisticsReservations.Count, Is.EqualTo(0), "Reservations should be released/consumed by end.");
    }

    [Test]
    public void Reservation_Behavior_IsDeterministic_AcrossFreshRuns()
    {
        string RunOnce()
        {
            var k = KernelWithSupplierAndTwoDestinations(supplierOre: 6);
            var s = k.State;

            var f1 = s.Fleets["fleet_trader_1"];
            var f2 = s.Fleets["fleet_trader_2"];
            var thief = s.Fleets["fleet_thief"];

            Assert.That(LogisticsSystem.PlanLogistics(s, f1, "mkt_sup", "mkt_d1", "ore", 5), Is.True);
            Assert.That(LogisticsSystem.PlanLogistics(s, f2, "mkt_sup", "mkt_d2", "ore", 5), Is.True);

            s.EnqueueIntent(new LoadCargoIntent(thief.Id, "mkt_sup", "ore", 999));
            IntentSystem.Process(s);

            for (var i = 0; i < 40; i++)
                k.Step();

            return s.GetSignature();
        }

        var a = RunOnce();
        var b = RunOnce();
        Assert.That(b, Is.EqualTo(a));
    }
}
