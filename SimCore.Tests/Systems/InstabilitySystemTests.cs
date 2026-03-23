using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Collections.Generic;

namespace SimCore.Tests.Systems;

[TestFixture]
[Category("InstabilitySystem")]
public sealed class InstabilitySystemTests
{
    private static SimState CreateState(int seed = 42)
    {
        var state = new SimState(seed);
        state.Nodes["nodeA"] = new Node { Id = "nodeA", InstabilityLevel = 0 };
        state.Nodes["nodeB"] = new Node { Id = "nodeB", InstabilityLevel = 30 };
        return state;
    }

    [Test]
    public void ContestedNode_GainsInstability()
    {
        var state = CreateState();
        state.Warfronts["wf1"] = new WarfrontState
        {
            Id = "wf1",
            Intensity = WarfrontIntensity.Skirmish,
            ContestedNodeIds = new List<string> { "nodeA" }
        };

        int levelBefore = state.Nodes["nodeA"].InstabilityLevel;

        // Advance to gain interval
        while (state.Tick == 0 || state.Tick % InstabilityTweaksV0.GainIntervalTicks != 0)
            state.AdvanceTick();

        InstabilitySystem.Process(state);

        Assert.That(state.Nodes["nodeA"].InstabilityLevel, Is.GreaterThan(levelBefore));
    }

    [Test]
    public void NonContestedUnstableNode_Decays()
    {
        var state = CreateState();
        // nodeB has instability 30, no warfronts — dict is already empty

        // Advance to decay interval
        while (state.Tick == 0 || state.Tick % InstabilityTweaksV0.DecayIntervalTicks != 0)
            state.AdvanceTick();

        InstabilitySystem.Process(state);

        Assert.That(state.Nodes["nodeB"].InstabilityLevel, Is.LessThan(30));
    }

    [Test]
    public void StableNode_DoesNotDecayBelowZero()
    {
        var state = CreateState();
        state.Nodes["nodeA"].InstabilityLevel = 0;

        while (state.Tick == 0 || state.Tick % InstabilityTweaksV0.DecayIntervalTicks != 0)
            state.AdvanceTick();

        InstabilitySystem.Process(state);

        Assert.That(state.Nodes["nodeA"].InstabilityLevel, Is.EqualTo(0));
    }

    [Test]
    public void GainClampsAtMax()
    {
        var state = CreateState();
        state.Nodes["nodeA"].InstabilityLevel = InstabilityTweaksV0.MaxInstability - 1;
        state.Warfronts["wf1"] = new WarfrontState
        {
            Id = "wf1",
            Intensity = WarfrontIntensity.TotalWar,
            ContestedNodeIds = new List<string> { "nodeA" }
        };

        while (state.Tick == 0 || state.Tick % InstabilityTweaksV0.GainIntervalTicks != 0)
            state.AdvanceTick();

        InstabilitySystem.Process(state);

        Assert.That(state.Nodes["nodeA"].InstabilityLevel,
            Is.LessThanOrEqualTo(InstabilityTweaksV0.MaxInstability));
    }

    [Test]
    public void Tick0_NoGainOrDecay()
    {
        var state = CreateState();
        state.Warfronts["wf1"] = new WarfrontState
        {
            Id = "wf1",
            Intensity = WarfrontIntensity.Skirmish,
            ContestedNodeIds = new List<string> { "nodeA" }
        };
        // At tick 0
        InstabilitySystem.Process(state);

        Assert.That(state.Nodes["nodeA"].InstabilityLevel, Is.EqualTo(0));
    }
}
