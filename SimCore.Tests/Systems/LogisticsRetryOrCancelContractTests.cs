using NUnit.Framework;
using SimCore.Schemas;
using SimCore.Systems;
using SimCore.World;

namespace SimCore.Tests.Systems;

[TestFixture]
public sealed class LogisticsRetryOrCancelContractTests
{
    private static SimKernel KernelWithThreeStations(int supplierOre)
    {
        var k = new SimKernel(seed: 123);

        var def = new WorldDefinition
        {
            WorldId = "micro_world_logi_retry_001",
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
    public void RetryUpToNZeroPickupObservations_ThenCancel()
    {
        var k = KernelWithThreeStations(supplierOre: 0);
        var s = k.State;

        var fleet = s.Fleets["fleet_trader_1"];
        fleet.Speed = 1.0f;

        Assert.That(LogisticsSystem.PlanLogistics(s, fleet, "mkt_b", "mkt_c", "ore", 5), Is.True);

        var sawFirstZeroObservation = false;
        var sawSecondZeroObservation = false;

        // Step until the job cancels, but assert it does NOT cancel on the first 0 observation.
        for (var i = 0; i < 400; i++)
        {
            k.Step();

            var job = fleet.CurrentJob;
            if (job != null)
            {
                if (!sawFirstZeroObservation && job.ZeroPickupObservations >= 1)
                {
                    sawFirstZeroObservation = true;
                    Assert.That(fleet.CurrentJob, Is.Not.Null, "Job must not cancel on first 0 pickup observation.");
                }

                if (!sawSecondZeroObservation && job.ZeroPickupObservations >= 2)
                {
                    sawSecondZeroObservation = true;
                }
            }
            else
            {
                break; // canceled or completed
            }
        }

        Assert.That(sawFirstZeroObservation, Is.True, "Test never observed a 0 pickup attempt; setup may have changed.");
        Assert.That(sawSecondZeroObservation, Is.True, "Job canceled too early; expected at least 2 failed pickup observations before cancel.");
        Assert.That(fleet.CurrentJob, Is.Null, "Job should eventually cancel when supplier stays empty.");

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

    [Test]
    public void IfSupplierRefillsBeforeN_JobRetriesAndCompletes()
    {
        var k = KernelWithThreeStations(supplierOre: 0);
        var s = k.State;

        var fleet = s.Fleets["fleet_trader_1"];
        fleet.Speed = 1.0f;

        Assert.That(LogisticsSystem.PlanLogistics(s, fleet, "mkt_b", "mkt_c", "ore", 5), Is.True);

        var refilled = false;
        var sawDeliverPhase = false;

        // Step until:
        // - we observe the job enter Deliver phase (proof pickup succeeded), AND
        // - destination inventory reflects the unload (which can occur on a later tick than job clear)
        for (var i = 0; i < 2000; i++)
        {
            k.Step();

            var job = fleet.CurrentJob;

            if (!refilled && job != null && job.ZeroPickupObservations >= 1)
            {
                // Deterministic intervention within the test: supplier gets stock after first failed observation.
                s.Markets["mkt_b"].Inventory["ore"] = 5;
                refilled = true;
            }

            if (job != null && job.Phase == Entities.LogisticsJobPhase.Deliver)
            {
                sawDeliverPhase = true;
            }

            // Unload intent can apply after the job is cleared from the fleet, so use market state as completion signal.
            if (refilled && s.Markets["mkt_c"].Inventory["ore"] == 5)
            {
                break;
            }
        }

        Assert.That(refilled, Is.True, "Test did not trigger refill timing; setup may have changed.");
        Assert.That(sawDeliverPhase, Is.True, "Job never entered Deliver phase after refill (pickup may have still failed).");
        Assert.That(s.Markets["mkt_c"].Inventory["ore"], Is.EqualTo(5), "Destination should receive the full requested amount after refill.");
        Assert.That(s.Markets["mkt_b"].Inventory["ore"], Is.EqualTo(0), "Supplier should be drained by the successful pickup.");
        Assert.That(fleet.GetCargoUnits("ore"), Is.EqualTo(0), "Cargo should be empty after delivery.");
    }
}
