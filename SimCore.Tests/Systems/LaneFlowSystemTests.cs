using NUnit.Framework;
using SimCore;
using SimCore.Schemas;
using SimCore.Systems;
using SimCore.World;

namespace SimCore.Tests.Systems;

[TestFixture]
public sealed class LaneFlowSystemTests
{
    private static SimKernel KernelWithWorld001()
    {
        var k = new SimKernel(seed: 123);

        var def = ScenarioHarnessV0.MicroWorld001();
        WorldLoader.Apply(k.State, def);

        return k;
    }

    [Test]
    public void Enqueue_Debits_Source_Immediately_And_Arrives_After_CeilDistance_Ticks()
    {
        var k = KernelWithWorld001();
        var s = k.State;

        Assert.That(s.Tick, Is.EqualTo(0));

        var ok = LaneFlowSystem.TryEnqueueTransfer(
            s,
            fromNodeId: "stn_a",
            toNodeId: "stn_b",
            goodId: "ore",
            quantity: 4,
            transferId: "xfer_001");

        Assert.That(ok, Is.True);

        Assert.That(s.Markets["mkt_a"].Inventory["ore"], Is.EqualTo(6));
        Assert.That(s.Markets["mkt_b"].Inventory["ore"], Is.EqualTo(1));

        // Step 1: tick advances from 0 to 1 at the end of Step().
        // LaneFlowSystem.Process runs BEFORE AdvanceTick, so arrivals for tick 1 are not processed yet.
        k.Step();
        Assert.That(s.Markets["mkt_b"].Inventory["ore"], Is.EqualTo(1));

        // Step 2: at the start of this step, Tick == 1, so Process can deliver arrivals scheduled for tick 1.
        k.Step();
        Assert.That(s.Markets["mkt_b"].Inventory["ore"], Is.EqualTo(5));
    }

    [Test]
    public void Multiple_Arrivals_Same_Tick_Process_In_Stable_Order()
    {
        var k = KernelWithWorld001();
        var s = k.State;

        var ok1 = LaneFlowSystem.TryEnqueueTransfer(s, "stn_a", "stn_b", "food", 1, "xfer_010");
        var ok2 = LaneFlowSystem.TryEnqueueTransfer(s, "stn_a", "stn_b", "food", 1, "xfer_002");
        var ok3 = LaneFlowSystem.TryEnqueueTransfer(s, "stn_a", "stn_b", "food", 1, "xfer_100");

        Assert.That(ok1 && ok2 && ok3, Is.True);

        // Same reasoning as above: need 2 steps for a 1-tick delay to be processed.
        k.Step();
        Assert.That(s.Markets["mkt_b"].Inventory["food"], Is.EqualTo(12));

        k.Step();
        Assert.That(s.Markets["mkt_b"].Inventory["food"], Is.EqualTo(15));
        Assert.That(s.InFlightTransfers.Count, Is.EqualTo(0));
    }

    [Test]
    public void LaneCapacity_Bounds_Delivery_And_Queues_Overflow_Deterministically_With_Report()
    {
        var k = KernelWithWorld001();
        var s = k.State;

        // lane_ab has TotalCapacity = 5.
        // Enqueue 5 food first (should fully deliver), then 5 ore (should be entirely queued to next tick).
        var ok1 = LaneFlowSystem.TryEnqueueTransfer(s, "stn_a", "stn_b", "food", 3, "xfer_001"); // consumes remaining food=3
        Assert.That(ok1, Is.True);

        // Top up ore transfers so we exceed capacity across the chokepoint.
        var ok2 = LaneFlowSystem.TryEnqueueTransfer(s, "stn_a", "stn_b", "ore", 5, "xfer_002");
        Assert.That(ok2, Is.True);

        // After enqueue: source debited immediately.
        Assert.That(s.Markets["mkt_a"].Inventory["food"], Is.EqualTo(0));
        Assert.That(s.Markets["mkt_a"].Inventory["ore"], Is.EqualTo(5));

        // Step 1: Tick 0 -> 1 (arrivals for tick 1 are not processed yet).
        k.Step();
        Assert.That(s.Markets["mkt_b"].Inventory["food"], Is.EqualTo(12));
        Assert.That(s.Markets["mkt_b"].Inventory["ore"], Is.EqualTo(1));

        // Step 2: Tick == 1 at start; capacity is enforced now.
        // Total due volume is 3+5=8 > capacity(5). By deterministic ordering, food xfer_001 arrives before ore xfer_002.
        k.Step();

        // Food (3) must have arrived; remaining capacity 2 delivers partial ore (2), leaving 3 queued.
        Assert.That(s.Markets["mkt_b"].Inventory["food"], Is.EqualTo(15));
        Assert.That(s.Markets["mkt_b"].Inventory["ore"], Is.EqualTo(3));

        var reportTick1 = LaneFlowSystem.GetLastLaneUtilizationReport(s);
        Assert.That(reportTick1, Does.Contain("LANE_UTILIZATION_REPORT_V0\n"));
        Assert.That(reportTick1, Does.Contain("tick=1\n"));
        Assert.That(reportTick1, Does.Contain("lane_id|delivered|capacity|queued\n"));
        Assert.That(reportTick1, Does.Contain("lane_ab|5|5|3\n"));

        // Step 3: queued remainder delivers next tick (up to capacity).
        k.Step();
        Assert.That(s.Markets["mkt_b"].Inventory["ore"], Is.EqualTo(6));

        var reportTick2 = LaneFlowSystem.GetLastLaneUtilizationReport(s);
        Assert.That(reportTick2, Does.Contain("tick=2\n"));
        Assert.That(reportTick2, Does.Contain("lane_ab|3|5|0\n"));
    }

    [Test]
    public void CapacityScarcity_MultiTransfer_Overflow_IsStableAcrossTicks_And_PerTransferRemaindersAreDeterministic()
    {
        var k = KernelWithWorld001();
        var s = k.State;

        // lane_ab capacity = 5, delay = 1 tick.
        // Use ore only: mkt_a starts with 10 ore, so we can enqueue total 10 across 3 transfers.
        // Stable order at delivery is by (ArriveTick, EdgeId, Id) so ids define deterministic precedence.
        Assert.That(LaneFlowSystem.TryEnqueueTransfer(s, "stn_a", "stn_b", "ore", 4, "xfer_002"), Is.True);
        Assert.That(LaneFlowSystem.TryEnqueueTransfer(s, "stn_a", "stn_b", "ore", 4, "xfer_010"), Is.True);
        Assert.That(LaneFlowSystem.TryEnqueueTransfer(s, "stn_a", "stn_b", "ore", 2, "xfer_100"), Is.True);

        // Source debited immediately: 10 - (4+4+2) = 0.
        Assert.That(s.Markets["mkt_a"].Inventory["ore"], Is.EqualTo(0));
        Assert.That(s.Markets["mkt_b"].Inventory["ore"], Is.EqualTo(1));

        // Step 1: Tick 0 -> 1 (arrivals for tick 1 are not processed yet).
        k.Step();
        Assert.That(s.Tick, Is.EqualTo(1));
        Assert.That(s.Markets["mkt_b"].Inventory["ore"], Is.EqualTo(1));

        // Step 2: Process tick 1 arrivals with capacity=5.
        // Expected: deliver xfer_002 fully (4), then xfer_010 partially (1), queue remainder (3) and xfer_100 (2).
        k.Step();
        Assert.That(s.Tick, Is.EqualTo(2));
        Assert.That(s.Markets["mkt_b"].Inventory["ore"], Is.EqualTo(6)); // 1 + 5 delivered

        // Validate per-transfer remainders are deterministic.
        // Only xfer_010 (3) and xfer_100 (2) should remain in-flight.
        Assert.That(s.InFlightTransfers.Count, Is.EqualTo(2));

        var t010 = s.InFlightTransfers.Single(x => x.Id == "xfer_010");
        var t100 = s.InFlightTransfers.Single(x => x.Id == "xfer_100");

        Assert.That(t010.EdgeId, Is.EqualTo("lane_ab"));
        Assert.That(t100.EdgeId, Is.EqualTo("lane_ab"));

        Assert.That(t010.Quantity, Is.EqualTo(3));
        Assert.That(t100.Quantity, Is.EqualTo(2));

        // Both should be queued to the next tick after being overflowed at tick=1.
        Assert.That(t010.ArriveTick, Is.EqualTo(2));
        Assert.That(t100.ArriveTick, Is.EqualTo(2));

        var report1 = LaneFlowSystem.GetLastLaneUtilizationReport(s);
        Assert.That(report1, Does.Contain("LANE_UTILIZATION_REPORT_V0\n"));
        Assert.That(report1, Does.Contain("tick=1\n"));
        Assert.That(report1, Does.Contain("lane_id|delivered|capacity|queued\n"));
        Assert.That(report1, Does.Contain("lane_ab|5|5|5\n")); // queued total remaining on lane after tick1: 3+2

        // Step 3: tick 2 processing delivers remaining 5 (bounded by capacity, but exactly equals remainder here).
        k.Step();
        Assert.That(s.Tick, Is.EqualTo(3));
        Assert.That(s.Markets["mkt_b"].Inventory["ore"], Is.EqualTo(11)); // 6 + 5 delivered
        Assert.That(s.InFlightTransfers.Count, Is.EqualTo(0));

        var report2 = LaneFlowSystem.GetLastLaneUtilizationReport(s);
        Assert.That(report2, Does.Contain("tick=2\n"));
        Assert.That(report2, Does.Contain("lane_ab|5|5|0\n"));
    }
}
