using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Schemas;
using SimCore.Systems;
using SimCore.Tweaks;
using SimCore.World;
using System.Collections.Generic;

namespace SimCore.Tests.Systems;

// GATE.S7.ENFORCEMENT.HEAT_ACCUM.001: Contract tests for pattern-based heat accumulation.
[TestFixture]
public sealed class SecurityHeatAccumTests
{
    private static SimState MakeState()
    {
        var state = new SimState();
        state.Nodes["n1"] = new Node { Id = "n1", MarketId = "m1" };
        state.Nodes["n2"] = new Node { Id = "n2", MarketId = "m2" };
        state.Markets["m1"] = new Market { Id = "m1" };
        state.Markets["m2"] = new Market { Id = "m2" };
        state.Edges["e1"] = new Edge { Id = "e1", FromNodeId = "n1", ToNodeId = "n2", Distance = 1f };
        return state;
    }

    [Test]
    public void BaseTraffic_AddsHeat()
    {
        var state = MakeState();
        SecurityLaneSystem.RegisterPatternedTraffic(state, "e1", 10, 0, null);

        Assert.That(state.Edges["e1"].Heat, Is.GreaterThan(0f), "Base traffic should add heat.");
        // 10 * 0.01 = 0.1
        Assert.That(state.Edges["e1"].Heat, Is.EqualTo(0.1f).Within(0.001f));
    }

    [Test]
    public void HighValueTrade_AddsBonusHeat()
    {
        var state = MakeState();

        // Below threshold: no bonus.
        SecurityLaneSystem.RegisterPatternedTraffic(state, "e1", 1, 400, null);
        float lowValueHeat = state.Edges["e1"].Heat;

        state.Edges["e1"].Heat = 0;

        // Above threshold: bonus applied.
        SecurityLaneSystem.RegisterPatternedTraffic(state, "e1", 1, 1000, null);
        float highValueHeat = state.Edges["e1"].Heat;

        Assert.That(highValueHeat, Is.GreaterThan(lowValueHeat),
            "High-value trade should add more heat than low-value.");
    }

    [Test]
    public void RouteRepetition_ThresholdTriggersBonus()
    {
        var state = MakeState();

        // First two traversals: below threshold.
        SecurityLaneSystem.RegisterPatternedTraffic(state, "e1", 1, 0, null);
        SecurityLaneSystem.RegisterPatternedTraffic(state, "e1", 1, 0, null);
        float heatBefore = state.Edges["e1"].Heat;

        // Third traversal: crosses threshold.
        SecurityLaneSystem.RegisterPatternedTraffic(state, "e1", 1, 0, null);
        float heatAfter = state.Edges["e1"].Heat;

        // Heat per base call = 0.01. After 3rd call, should add bonus 0.15.
        float expectedBonus = SecurityTweaksV0.RepetitionBonusHeat;
        float delta = heatAfter - heatBefore;

        Assert.That(delta, Is.GreaterThan(0.01f + expectedBonus * 0.5f),
            "Third traversal should include repetition bonus heat.");
        Assert.That(state.Edges["e1"].TraversalCount, Is.EqualTo(3));
    }

    [Test]
    public void HostileCounterparty_AddsBonusHeat()
    {
        var state = MakeState();
        state.NodeFactionId["n2"] = "faction_hostile";
        // Set rep below trade-blocked threshold.
        state.FactionReputation["faction_hostile"] = FactionTweaksV0.TradeBlockedRepThreshold - 10;

        SecurityLaneSystem.RegisterPatternedTraffic(state, "e1", 1, 0, "n2");
        float hostileHeat = state.Edges["e1"].Heat;

        var state2 = MakeState();
        SecurityLaneSystem.RegisterPatternedTraffic(state2, "e1", 1, 0, "n2");
        float neutralHeat = state2.Edges["e1"].Heat;

        Assert.That(hostileHeat, Is.GreaterThan(neutralHeat),
            "Trading at hostile faction node should add extra heat.");
    }

    [Test]
    public void HeatDecay_ReducesOverTime()
    {
        var state = MakeState();
        state.Edges["e1"].Heat = 1.0f;

        SecurityLaneSystem.ProcessSecurityLanes(state);

        Assert.That(state.Edges["e1"].Heat, Is.LessThan(1.0f),
            "Heat should decay each tick.");
        Assert.That(state.Edges["e1"].Heat,
            Is.EqualTo(1.0f - SecurityTweaksV0.HeatDecayPerTick).Within(0.001f));
    }

    [Test]
    public void TraversalCount_ResetsAtWindowBoundary()
    {
        // Use MicroWorld001 with SimKernel to advance Tick properly.
        var kernel = new SimKernel(seed: 42);
        WorldLoader.Apply(kernel.State, ScenarioHarnessV0.MicroWorld001());
        kernel.State.Edges["lane_ab"].TraversalCount = 5;

        // ProcessSecurityLanes runs before AdvanceTick, so we need one extra step
        // to have Tick == TraversalWindowTicks when DecayHeat runs.
        for (int i = 0; i <= SecurityTweaksV0.TraversalWindowTicks; i++)
            kernel.Step();

        Assert.That(kernel.State.Edges["lane_ab"].TraversalCount, Is.EqualTo(0),
            "Traversal count should reset at window boundary.");
    }

    [Test]
    public void PatternedTraffic_HigherThanFlatRate()
    {
        var state = MakeState();
        // Patterned: high value, repeated, hostile.
        state.NodeFactionId["n2"] = "faction_hostile";
        state.FactionReputation["faction_hostile"] = FactionTweaksV0.TradeBlockedRepThreshold - 10;

        for (int i = 0; i < 4; i++)
            SecurityLaneSystem.RegisterPatternedTraffic(state, "e1", 5, 1000, "n2");
        float patternedHeat = state.Edges["e1"].Heat;

        // Flat rate: same volume but no value/repetition/hostile signals.
        var state2 = MakeState();
        for (int i = 0; i < 4; i++)
            MarketSystem.RegisterTraffic(state2, "e1", 5);
        float flatHeat = state2.Edges["e1"].Heat;

        Assert.That(patternedHeat, Is.GreaterThan(flatHeat),
            "Pattern-based accumulation should produce more heat than flat rate.");
    }
}
