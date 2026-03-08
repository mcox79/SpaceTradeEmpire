using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Numerics;

namespace SimCore.Tests.Systems;

// GATE.S6.FRACTURE.DETECTION_REP.001
public class FractureDetectionTests
{
    private SimState SetupState()
    {
        var state = new SimState(42);
        state.Nodes["node_a"] = new Node { Id = "node_a", Position = new Vector3(0, 0, 0) };
        state.NodeFactionId["node_a"] = "faction_0";
        return state;
    }

    [Test]
    public void BelowThreshold_NoPenalty()
    {
        var state = SetupState();
        state.Nodes["node_a"].Trace = FractureTweaksV0.TraceDetectionThreshold - 0.1f;

        FractureSystem.DetectFractureUse(state);

        Assert.That(ReputationSystem.GetReputation(state, "faction_0"), Is.EqualTo(0));
    }

    [Test]
    public void AtThreshold_AfterDecay_NoPenalty()
    {
        var state = SetupState();
        // Exactly at threshold: decay brings it below before check
        state.Nodes["node_a"].Trace = FractureTweaksV0.TraceDetectionThreshold;

        FractureSystem.DetectFractureUse(state);

        // Decay happens first → 1.0 - 0.01 = 0.99 < 1.0 → no penalty
        Assert.That(ReputationSystem.GetReputation(state, "faction_0"), Is.EqualTo(0));
    }

    [Test]
    public void AboveThreshold_PenaltyApplied()
    {
        var state = SetupState();
        // Set trace high enough that after decay it's still >= threshold
        state.Nodes["node_a"].Trace = FractureTweaksV0.TraceDetectionThreshold + 0.1f;

        FractureSystem.DetectFractureUse(state);

        Assert.That(ReputationSystem.GetReputation(state, "faction_0"),
            Is.EqualTo(FractureTweaksV0.FractureDetectionRepPenalty));
    }

    [Test]
    public void NoFaction_NoPenalty()
    {
        var state = new SimState(42);
        state.Nodes["node_b"] = new Node { Id = "node_b", Position = Vector3.Zero };
        state.Nodes["node_b"].Trace = 5.0f;
        // No faction assigned to node_b

        FractureSystem.DetectFractureUse(state);

        // No faction to penalize — no crash
        Assert.Pass();
    }

    [Test]
    public void TraceDecays()
    {
        var state = SetupState();
        state.Nodes["node_a"].Trace = 0.5f; // Below threshold
        float before = state.Nodes["node_a"].Trace;

        FractureSystem.DetectFractureUse(state);

        Assert.That(state.Nodes["node_a"].Trace,
            Is.EqualTo(before - FractureTweaksV0.TraceDecayPerTick).Within(0.001f));
    }

    [Test]
    public void TraceDecay_FloorsAtZero()
    {
        var state = SetupState();
        state.Nodes["node_a"].Trace = 0.005f; // Less than decay rate

        FractureSystem.DetectFractureUse(state);

        Assert.That(state.Nodes["node_a"].Trace, Is.EqualTo(0f));
    }

    [Test]
    public void RepeatedDetection_AccumulatesPenalty()
    {
        var state = SetupState();
        state.Nodes["node_a"].Trace = 5.0f; // Well above threshold

        FractureSystem.DetectFractureUse(state);
        FractureSystem.DetectFractureUse(state);

        Assert.That(ReputationSystem.GetReputation(state, "faction_0"),
            Is.EqualTo(FractureTweaksV0.FractureDetectionRepPenalty * 2));
    }
}
