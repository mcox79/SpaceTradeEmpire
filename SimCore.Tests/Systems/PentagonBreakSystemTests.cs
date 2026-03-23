using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

[TestFixture]
[Category("PentagonBreakSystem")]
public sealed class PentagonBreakSystemTests
{
    private static SimState CreateState(int seed = 42)
    {
        var state = new SimState(seed);
        state.StoryState = new StoryState();
        state.Nodes["nodeC"] = new Node { Id = "nodeC", MarketId = "mkt_C" };
        state.Markets["mkt_C"] = new Market
        {
            Id = "mkt_C",
            Inventory = new() { [WellKnownGoodIds.Food] = 10 }
        };
        state.NodeFactionId["nodeC"] = FactionTweaksV0.CommunionId;
        return state;
    }

    [Test]
    public void CascadeActivates_WhenAllConditionsMet()
    {
        var state = CreateState();
        state.StoryState.RevealedFlags = RevelationFlags.R3_Pentagon;
        state.FractureUnlocked = true;

        PentagonBreakSystem.Process(state);

        Assert.That(state.StoryState.PentagonCascadeActive, Is.True);
        Assert.That(state.StoryState.PentagonCascadeTick, Is.EqualTo(state.Tick));
    }

    [Test]
    public void CascadeDoesNotActivate_WithoutR3()
    {
        var state = CreateState();
        state.StoryState.RevealedFlags = RevelationFlags.None;
        state.FractureUnlocked = true;

        PentagonBreakSystem.Process(state);

        Assert.That(state.StoryState.PentagonCascadeActive, Is.False);
    }

    [Test]
    public void CascadeDoesNotActivate_WithoutFracture()
    {
        var state = CreateState();
        state.StoryState.RevealedFlags = RevelationFlags.R3_Pentagon;
        state.FractureUnlocked = false;

        PentagonBreakSystem.Process(state);

        Assert.That(state.StoryState.PentagonCascadeActive, Is.False);
    }

    [Test]
    public void CascadeFoodInjection_AtInterval()
    {
        var state = CreateState();
        state.StoryState.PentagonCascadeActive = true;
        int foodBefore = state.Markets["mkt_C"].Inventory[WellKnownGoodIds.Food];

        // Advance to cascade interval
        while (state.Tick % PentagonBreakTweaksV0.CascadeFoodIntervalTicks != 0)
            state.AdvanceTick();

        PentagonBreakSystem.Process(state);

        Assert.That(state.Markets["mkt_C"].Inventory[WellKnownGoodIds.Food],
            Is.EqualTo(foodBefore + PentagonBreakTweaksV0.CommunionFoodSelfProductionQty));
    }
}
