using System;
using NUnit.Framework;
using SimCore;
using SimCore.Schemas;
using SimCore.Systems;
using SimCore.World;

namespace SimCore.Tests.SaveLoad;

[TestFixture]
public sealed class SaveLoadLogisticsMidJobTests
{
    private static SimKernel KernelWithThreeStations()
    {
        var k = new SimKernel(seed: 123);

        var def = new WorldDefinition
        {
            WorldId = "micro_world_logi_saveload_001",
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

        // Make travel multi-tick per edge so there is a real "mid-job" window after load applies.
        k.State.Fleets["fleet_trader_1"].Speed = 0.5f;

        return k;
    }

    private static void StepUntilLoadedMidJob(SimKernel k, int maxSteps)
    {
        var s = k.State;
        var f = s.Fleets["fleet_trader_1"];

        for (var i = 0; i < maxSteps; i++)
        {
            k.Step();

            var job = f.CurrentJob;
            if (job is null) continue;

            // We must save AFTER the pickup intent has applied, because PendingIntents are not persisted.
            // Condition: phase is Deliver, pickup latch set, cargo loaded, and we are NOT yet at destination.
            if (job.Phase == SimCore.Entities.LogisticsJobPhase.Deliver &&
                job.PickupTransferIssued &&
                !job.DeliveryTransferIssued &&
                f.GetCargoUnits(job.GoodId) > 0 &&
                !string.Equals(f.CurrentNodeId, job.TargetNodeId, StringComparison.Ordinal))
            {
                return;
            }
        }

        Assert.Fail("Did not reach a mid-job loaded state within the step limit.");
    }

    [Test]
    public void SaveLoad_MidLogisticsJob_ContinuesToSameFinalHash_AsUninterrupted()
    {
        // Uninterrupted run (A)
        var simA = KernelWithThreeStations();
        var sA = simA.State;
        var fA = sA.Fleets["fleet_trader_1"];

        Assert.That(LogisticsSystem.PlanLogistics(sA, fA, "mkt_b", "mkt_c", "ore", 5), Is.True);

        StepUntilLoadedMidJob(simA, maxSteps: 200);

        // Save at mid-job (after load applied)
        var midJson = simA.SaveToString();

        // Loaded-from-save run (B)
        var simB = new SimKernel(seed: 123);
        simB.LoadFromString(midJson);

        // From this point, continue both in lockstep to avoid any mismatch in tick counts.
        // Run until job completes (or cap), then a few extra ticks for stability.
        const int cap = 200;
        var extra = 5;

        for (var i = 0; i < cap; i++)
        {
            var aHas = simA.State.Fleets["fleet_trader_1"].CurrentJob is not null;
            var bHas = simB.State.Fleets["fleet_trader_1"].CurrentJob is not null;

            if (!aHas && !bHas)
                break;

            simA.Step();
            simB.Step();
        }

        for (var i = 0; i < extra; i++)
        {
            simA.Step();
            simB.Step();
        }

        var hashA = simA.State.GetSignature();
        var hashB = simB.State.GetSignature();

        TestContext.Out.WriteLine($"Uninterrupted Hash: {hashA}");
        TestContext.Out.WriteLine($"SaveLoad     Hash: {hashB}");

        Assert.That(hashB, Is.EqualTo(hashA), "Save/load mid-job continuation drift detected.");
    }
}
