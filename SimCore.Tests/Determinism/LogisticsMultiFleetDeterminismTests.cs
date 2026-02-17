using NUnit.Framework;
using SimCore.Schemas;
using SimCore.World;
using SimCore;
using SimCore.Commands;

namespace SimCore.Tests.Determinism;

[TestFixture]
public sealed class LogisticsMultiFleetDeterminismTests
{
    private static SimKernel KernelWithTwoFleetsThreeStations()
    {
        var k = new SimKernel(seed: 123);

        var def = new WorldDefinition
        {
            WorldId = "micro_world_logi_det_001",
            Markets =
            {
                new WorldMarket { Id = "mkt_a", Inventory = new() { ["ore"] = 0 } },
                new WorldMarket { Id = "mkt_b", Inventory = new() { ["ore"] = 200 } },
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
                // Constrained capacity to force deterministic ordering/contention.
                new WorldEdge { Id = "lane_ab", FromNodeId = "stn_a", ToNodeId = "stn_b", Distance = 1.0f, TotalCapacity = 1 },
                new WorldEdge { Id = "lane_ba", FromNodeId = "stn_b", ToNodeId = "stn_a", Distance = 1.0f, TotalCapacity = 1 },
                new WorldEdge { Id = "lane_bc", FromNodeId = "stn_b", ToNodeId = "stn_c", Distance = 1.0f, TotalCapacity = 1 },
                new WorldEdge { Id = "lane_cb", FromNodeId = "stn_c", ToNodeId = "stn_b", Distance = 1.0f, TotalCapacity = 1 }
            },
            Player = new WorldPlayerStart { Credits = 0, LocationNodeId = "stn_a" }
        };

        WorldLoader.Apply(k.State, def);

        // Ensure a second fleet exists deterministically.
        // WorldLoader creates fleet_trader_1; create fleet_trader_2 at same node to induce contention.
        var s = k.State;
        s.Fleets["fleet_trader_2"] = new SimCore.Entities.Fleet
        {
            Id = "fleet_trader_2",
            OwnerId = "player",
            CurrentNodeId = "stn_a",
            State = SimCore.Entities.FleetState.Idle,
            Speed = 1.0f
        };

        // Normalize speed for fleet_trader_1 too.
        s.Fleets["fleet_trader_1"].Speed = 1.0f;

        // Create an active industry site at stn_c that consumes ore (forces repeated shortages).
        s.IndustrySites["fac_c"] = new SimCore.Entities.IndustrySite
        {
            Id = "fac_c",
            NodeId = "stn_c",
            Active = true,
            BufferDays = 1,
            Inputs = new System.Collections.Generic.Dictionary<string, int> { { "ore", 1 } }
        };

        // Force both fleets into the logistics pipeline deterministically (contention on TotalCapacity=1 lanes).
        // Both will plan: A -> B (pickup) -> C (deliver).
        var f1 = s.Fleets["fleet_trader_1"];
        var f2 = s.Fleets["fleet_trader_2"];

        Assert.That(SimCore.Systems.LogisticsSystem.PlanLogistics(s, f1, "mkt_b", "mkt_c", "ore", 5), Is.True);
        Assert.That(SimCore.Systems.LogisticsSystem.PlanLogistics(s, f2, "mkt_b", "mkt_c", "ore", 5), Is.True);

        return k;
    }

    [Test]
    public void TwoFleets_LogisticsAndLaneContention_IsDeterministic()
    {
        // Run A
        var kA = KernelWithTwoFleetsThreeStations();
        for (var i = 0; i < 200; i++) kA.Step();
        var hashA = kA.State.GetSignature();

        // Run B (fresh)
        var kB = KernelWithTwoFleetsThreeStations();
        for (var i = 0; i < 200; i++) kB.Step();
        var hashB = kB.State.GetSignature();

        Assert.That(hashB, Is.EqualTo(hashA));
    }

    [Test]
    public void TwoFleets_UiCommandInterleavings_WithSaveLoadMidSequence_IsDeterministic()
    {
        static string RunScenario()
        {
            var k = KernelWithTwoFleetsThreeStations();
            // Preconditions: ensure we are actually exercising 2 known fleets.
            Assert.That(k.State.Fleets.ContainsKey("fleet_trader_1"), Is.True);
            Assert.That(k.State.Fleets.ContainsKey("fleet_trader_2"), Is.True);

            // Let systems settle deterministically with both jobs present.
            for (var i = 0; i < 5; i++) k.Step();
            var sigSettle = k.State.GetSignature();

            // Tick T: cancel job on fleet 1 (must cause a deterministic state change).
            k.EnqueueCommand(new FleetJobCancelCommand("fleet_trader_1", "test_cancel_f1"));
            k.Step();
            var sigAfterCancel = k.State.GetSignature();
            Assert.That(sigAfterCancel, Is.Not.EqualTo(sigSettle));

            // Tick T+1: set manual override on fleet 2 (must cause a deterministic state change).
            k.EnqueueCommand(new FleetSetDestinationCommand("fleet_trader_2", "stn_c", "test_override_f2"));
            k.Step();
            var sigAfterOverride = k.State.GetSignature();
            Assert.That(sigAfterOverride, Is.Not.EqualTo(sigAfterCancel));

            // Give movement/routing a deterministic chance to observe override.
            for (var i = 0; i < 2; i++) k.Step();
            var sigAfterOverrideSettled = k.State.GetSignature();

            // Tick T+4: clear manual override on fleet 2 (must cause a deterministic state change).
            k.EnqueueCommand(new FleetSetDestinationCommand("fleet_trader_2", "", "test_clear_override_f2"));
            k.Step();
            var sigAfterClear = k.State.GetSignature();
            Assert.That(sigAfterClear, Is.Not.EqualTo(sigAfterOverrideSettled));

            // Save/load mid-sequence must be lossless at this point.
            var json = k.SaveToString();
            var k2 = new SimKernel(seed: 123);
            k2.LoadFromString(json);
            var sigAfterLoad = k2.State.GetSignature();
            Assert.That(sigAfterLoad, Is.EqualTo(sigAfterClear));

            // Continue stepping to reach a stable post-interleaving state.
            for (var i = 0; i < 100; i++) k2.Step();

            return k2.State.GetSignature();

        }

        var hashA = RunScenario();
        var hashB = RunScenario();

        Assert.That(hashB, Is.EqualTo(hashA));
    }
}
