using NUnit.Framework;
using SimCore;
using SimCore.Schemas;
using SimCore.Systems;
using SimCore.Tweaks;
using SimCore.World;

namespace SimCore.Tests.Systems;

// GATE.S7.INSTABILITY_EFFECTS.LANE.001: Contract tests for instability lane delay + closure.
[TestFixture]
public sealed class InstabilityLaneTests
{
    private static SimKernel MakeKernel(int srcInstability, int dstInstability)
    {
        var k = new SimKernel(seed: 42);
        var def = ScenarioHarnessV0.MicroWorld001();
        WorldLoader.Apply(k.State, def);
        k.State.Nodes["stn_a"].InstabilityLevel = srcInstability;
        k.State.Nodes["stn_b"].InstabilityLevel = dstInstability;
        return k;
    }

    [Test]
    public void Stable_NoDelayBonus()
    {
        var k = MakeKernel(0, 0);
        var ok = LaneFlowSystem.TryEnqueueTransfer(k.State, "stn_a", "stn_b", "ore", 1, "t1");
        Assert.That(ok, Is.True);

        // distance=1.0 → ceil=1 tick delay. No instability bonus.
        var xfer = k.State.InFlightTransfers[0];
        Assert.That(xfer.ArriveTick, Is.EqualTo(1), "Stable: base delay only.");
    }

    [Test]
    public void Shimmer_AddsDelay()
    {
        var k = MakeKernel(InstabilityTweaksV0.ShimmerMin, 0);
        var ok = LaneFlowSystem.TryEnqueueTransfer(k.State, "stn_a", "stn_b", "ore", 1, "t1");
        Assert.That(ok, Is.True);

        var xfer = k.State.InFlightTransfers[0];
        // base 1 tick + max(1, 1*10/100) = 1 + 1 = 2
        Assert.That(xfer.ArriveTick, Is.EqualTo(2), "Shimmer: +10% delay (min 1 tick).");
    }

    [Test]
    public void Drift_AddsDelay()
    {
        var k = MakeKernel(InstabilityTweaksV0.DriftMin, 0);
        var ok = LaneFlowSystem.TryEnqueueTransfer(k.State, "stn_a", "stn_b", "ore", 1, "t1");
        Assert.That(ok, Is.True);

        var xfer = k.State.InFlightTransfers[0];
        // base 1 tick + max(1, 1*20/100) = 1 + 1 = 2
        Assert.That(xfer.ArriveTick, Is.EqualTo(2), "Drift: +20% delay (min 1 tick).");
    }

    [Test]
    public void Fracture_AddsLargerDelay()
    {
        var k = MakeKernel(InstabilityTweaksV0.FractureMin, 0);
        var ok = LaneFlowSystem.TryEnqueueTransfer(k.State, "stn_a", "stn_b", "ore", 1, "t1");
        Assert.That(ok, Is.True);

        var xfer = k.State.InFlightTransfers[0];
        // base 1 tick + max(1, 1*40/100) = 1 + 1 = 2
        Assert.That(xfer.ArriveTick, Is.EqualTo(2), "Fracture: +40% delay (min 1 tick).");
    }

    [Test]
    public void Void_Source_LaneSevered()
    {
        var k = MakeKernel(InstabilityTweaksV0.VoidMin, 0);
        var ok = LaneFlowSystem.TryEnqueueTransfer(k.State, "stn_a", "stn_b", "ore", 1, "t1");

        Assert.That(ok, Is.False, "Void source: lane should be severed.");
        Assert.That(k.State.InFlightTransfers, Is.Empty, "No transfer should be enqueued.");
        // Inventory should be unchanged (no debit).
        Assert.That(k.State.Markets["mkt_a"].Inventory["ore"], Is.EqualTo(10));
    }

    [Test]
    public void Void_Destination_LaneSevered()
    {
        var k = MakeKernel(0, InstabilityTweaksV0.VoidMin);
        var ok = LaneFlowSystem.TryEnqueueTransfer(k.State, "stn_a", "stn_b", "ore", 1, "t1");

        Assert.That(ok, Is.False, "Void destination: lane should be severed.");
        Assert.That(k.State.Markets["mkt_a"].Inventory["ore"], Is.EqualTo(10));
    }

    [Test]
    public void DelayUsesMaxPhaseOfBothEndpoints()
    {
        // Source Shimmer (phase 1), dest Fracture (phase 3) → should use Fracture delay.
        var k = MakeKernel(InstabilityTweaksV0.ShimmerMin, InstabilityTweaksV0.FractureMin);
        var ok = LaneFlowSystem.TryEnqueueTransfer(k.State, "stn_a", "stn_b", "ore", 1, "t1");
        Assert.That(ok, Is.True);

        var xfer = k.State.InFlightTransfers[0];
        // Fracture (+40%) applied: base 1 + max(1, 1*40/100) = 2
        Assert.That(xfer.ArriveTick, Is.EqualTo(2), "Should use max phase (Fracture) delay.");
    }
}
