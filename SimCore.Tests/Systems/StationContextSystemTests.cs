using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Collections.Generic;

namespace SimCore.Tests.Systems;

[TestFixture]
[Category("StationContextSystem")]
public sealed class StationContextSystemTests
{
    private static SimState CreateState(int seed = 42)
    {
        var state = new SimState(seed);
        state.Markets["nodeA"] = new Market
        {
            Id = "nodeA",
            Inventory = new() { ["food"] = 50, ["fuel"] = 50 }
        };
        return state;
    }

    [Test]
    public void CalmContext_WhenNoConditionsMet()
    {
        var state = CreateState();
        // All inventory at IdealStock → no shortage, no opportunity
        while (state.Tick % StationContextTweaksV0.ContextUpdateIntervalTicks != 0)
            state.AdvanceTick();

        StationContextSystem.Process(state);

        Assert.That(state.StationContexts, Is.Not.Null);
        Assert.That(state.StationContexts["nodeA"].ContextType, Is.EqualTo(StationContextType.Calm));
    }

    [Test]
    public void ShortageContext_WhenLowStock()
    {
        var state = CreateState();
        // Set food well below shortage threshold (40% of IdealStock=50 → 20)
        state.Markets["nodeA"].Inventory["food"] = 5;

        while (state.Tick % StationContextTweaksV0.ContextUpdateIntervalTicks != 0)
            state.AdvanceTick();

        StationContextSystem.Process(state);

        Assert.That(state.StationContexts["nodeA"].ContextType, Is.EqualTo(StationContextType.Shortage));
        Assert.That(state.StationContexts["nodeA"].PrimaryGoodId, Is.EqualTo("food"));
    }

    [Test]
    public void WarfrontDemand_TakesPriority()
    {
        var state = CreateState();
        // Set low stock (would be shortage) AND warfront
        state.Markets["nodeA"].Inventory["food"] = 5;
        state.Warfronts["wf1"] = new WarfrontState
        {
            Id = "wf1",
            Intensity = WarfrontIntensity.Skirmish,
            ContestedNodeIds = new List<string> { "nodeA" }
        };

        while (state.Tick % StationContextTweaksV0.ContextUpdateIntervalTicks != 0)
            state.AdvanceTick();

        StationContextSystem.Process(state);

        // Warfront demand takes priority over shortage
        Assert.That(state.StationContexts["nodeA"].ContextType,
            Is.EqualTo(StationContextType.WarfrontDemand));
        Assert.That(state.StationContexts["nodeA"].PrimaryGoodId,
            Is.EqualTo(StationContextTweaksV0.WarfrontDemandGoodId));
    }

    [Test]
    public void PeaceWarfront_DoesNotTrigger()
    {
        var state = CreateState();
        state.Warfronts["wf1"] = new WarfrontState
        {
            Id = "wf1",
            Intensity = WarfrontIntensity.Peace,
            ContestedNodeIds = new List<string> { "nodeA" }
        };

        while (state.Tick % StationContextTweaksV0.ContextUpdateIntervalTicks != 0)
            state.AdvanceTick();

        StationContextSystem.Process(state);

        Assert.That(state.StationContexts["nodeA"].ContextType,
            Is.Not.EqualTo(StationContextType.WarfrontDemand));
    }
}
