using System;
using System.Linq;
using NUnit.Framework;
using SimCore.Gen;
using SimCore.Intents;
using SimCore.Programs;
using SimCore.Systems;

namespace SimCore.Tests.Sustainment;

[TestFixture]
public sealed class LogisticsMutationPipelineContractTests
{
    [Test]
    public void LogisticsSystem_Process_DoesNotDirectlyMutate_MarketInventory_Or_FleetCargo()
    {
        // This test enforces GATE.LOGI.MUT.001 at the system boundary:
        // LogisticsSystem may enqueue intents and mutate job/route/task state,
        // but MUST NOT directly mutate market inventory or fleet cargo dictionaries.
        const int seed = 123;

        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        // Deterministic fleet + market + good selection
        var fleet = sim.State.Fleets.Values
            .OrderBy(f => f.Id, StringComparer.Ordinal)
            .First();

        var market = sim.State.Markets.Values
            .OrderBy(m => m.Id, StringComparer.Ordinal)
            .First();

        var goodId = market.Inventory.Keys
            .OrderBy(k => k, StringComparer.Ordinal)
            .First();

        // Force known, non-trivial starting values
        market.Inventory[goodId] = 7;
        fleet.Cargo.Clear();
        fleet.Cargo[goodId] = 3;

        // Snapshot pre-state (only the mutation surfaces we are guarding)
        var marketBefore = market.Inventory.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
        var cargoBefore = fleet.Cargo.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

        // Run LogisticsSystem in isolation (NOT the full kernel step).
        // Any inventory/cargo mutation here is a contract violation.
        LogisticsSystem.Process(sim.State);

        // Assert market inventory unchanged
        Assert.That(market.Inventory.Count, Is.EqualTo(marketBefore.Count));
        foreach (var kv in marketBefore.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            Assert.That(market.Inventory.ContainsKey(kv.Key), Is.True, $"Market missing key '{kv.Key}' after LogisticsSystem.Process().");
            Assert.That(market.Inventory[kv.Key], Is.EqualTo(kv.Value), $"Market inventory mutated for '{kv.Key}' by LogisticsSystem.Process().");
        }

        // Assert fleet cargo unchanged
        Assert.That(fleet.Cargo.Count, Is.EqualTo(cargoBefore.Count));
        foreach (var kv in cargoBefore.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            Assert.That(fleet.Cargo.ContainsKey(kv.Key), Is.True, $"Cargo missing key '{kv.Key}' after LogisticsSystem.Process().");
            Assert.That(fleet.Cargo[kv.Key], Is.EqualTo(kv.Value), $"Fleet cargo mutated for '{kv.Key}' by LogisticsSystem.Process().");
        }
    }

    [Test]
    public void LogisticsSystem_Process_MayEnqueueIntents_But_MutationsOccurOnlyWhenIntentSystemProcesses()
    {
        // This test demonstrates the intended pipeline:
        // - LogisticsSystem.Process() can enqueue intents
        // - Only IntentSystem.Process() should cause inventory/cargo mutations
        var s = LogisticsJobExecutionState();

        var fleet = s.Fleets["fleet_trader_1"];
        var src = s.Markets["mkt_b"];
        var dst = s.Markets["mkt_c"];

        // Preconditions
        Assert.That(src.Inventory.GetValueOrDefault("ore", 0), Is.EqualTo(10));
        Assert.That(dst.Inventory.GetValueOrDefault("ore", 0), Is.EqualTo(0));
        Assert.That(fleet.GetCargoUnits("ore"), Is.EqualTo(0));

        // Plan and step until we are at source node and LogisticsSystem has had a chance to enqueue load.
        var ok = LogisticsSystem.PlanLogistics(s, fleet, "mkt_b", "mkt_c", "ore", 5);
        Assert.That(ok, Is.True);

        // Tick 1: movement+logistics runs, should enqueue load (but NOT execute it yet).
        StepLikeKernelWithoutIntentExecution(s);

        // After LogisticsSystem.Process(), the intent should be in queue; inventory/cargo should be unchanged.
        Assert.That(src.Inventory.GetValueOrDefault("ore", 0), Is.EqualTo(10));
        Assert.That(dst.Inventory.GetValueOrDefault("ore", 0), Is.EqualTo(0));
        Assert.That(fleet.GetCargoUnits("ore"), Is.EqualTo(0));

        // Now execute intents: this is where mutation is allowed/expected.
        IntentSystem.Process(s);

        Assert.That(src.Inventory.GetValueOrDefault("ore", 0), Is.EqualTo(5));
        Assert.That(fleet.GetCargoUnits("ore"), Is.EqualTo(5));
    }

    private static SimCore.SimState LogisticsJobExecutionState()
    {
        var s = new SimCore.SimState(123);

        var def = new SimCore.Schemas.WorldDefinition
        {
            WorldId = "micro_world_logi_mut_001",
            Markets =
            {
                new SimCore.Schemas.WorldMarket { Id = "mkt_a", Inventory = new() { ["ore"] = 0 } },
                new SimCore.Schemas.WorldMarket { Id = "mkt_b", Inventory = new() { ["ore"] = 10 } },
                new SimCore.Schemas.WorldMarket { Id = "mkt_c", Inventory = new() { ["ore"] = 0 } }
            },
            Nodes =
            {
                new SimCore.Schemas.WorldNode { Id = "stn_a", Kind = "Station", Name = "A", MarketId = "mkt_a", Pos = new float[] { 0f, 0f, 0f } },
                new SimCore.Schemas.WorldNode { Id = "stn_b", Kind = "Station", Name = "B", MarketId = "mkt_b", Pos = new float[] { 1f, 0f, 0f } },
                new SimCore.Schemas.WorldNode { Id = "stn_c", Kind = "Station", Name = "C", MarketId = "mkt_c", Pos = new float[] { 2f, 0f, 0f } }
            },
            Edges =
            {
                new SimCore.Schemas.WorldEdge { Id = "lane_ab", FromNodeId = "stn_a", ToNodeId = "stn_b", Distance = 1.0f, TotalCapacity = 5 },
                new SimCore.Schemas.WorldEdge { Id = "lane_bc", FromNodeId = "stn_b", ToNodeId = "stn_c", Distance = 1.0f, TotalCapacity = 5 }
            },
            Player = new SimCore.Schemas.WorldPlayerStart { Credits = 0, LocationNodeId = "stn_a" }
        };

        SimCore.World.WorldLoader.Apply(s, def);

        // Single-tick lane traversal
        s.Fleets["fleet_trader_1"].Speed = 1.0f;

        return s;
    }

    private static void StepLikeKernelWithoutIntentExecution(SimCore.SimState s)
    {
        // Mirror kernel ordering BUT skip IntentSystem.Process() on purpose
        // to prove mutations do not happen during LogisticsSystem.Process().
        LaneFlowSystem.Process(s);
        SimCore.Programs.ProgramSystem.Process(s);


        // SKIP: IntentSystem.Process(s);

        MovementSystem.Process(s);
        LogisticsSystem.Process(s);
        IndustrySystem.Process(s);
        s.AdvanceTick();
    }
}
