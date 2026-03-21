using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using SimCore.Content;

namespace SimCore.Tests.Systems;

// GATE.S8.PENTAGON.DETECT.001: Pentagon break detection tests.
// GATE.S8.PENTAGON.CASCADE.001: Pentagon cascade economic effects tests.
[TestFixture]
public sealed class PentagonBreakTests
{
    private SimKernel CreateKernel(int seed = 42)
    {
        var kernel = new SimKernel(seed);
        GalaxyGenerator.Generate(kernel.State, 12, 100f);
        return kernel;
    }

    [Test]
    public void PentagonCascade_NotActive_WhenR3NotFired()
    {
        var kernel = CreateKernel();
        kernel.State.FractureUnlocked = true;
        kernel.Step();
        Assert.That(kernel.State.StoryState.PentagonCascadeActive, Is.False);
    }

    [Test]
    public void PentagonCascade_NotActive_WhenFractureNotUnlocked()
    {
        var kernel = CreateKernel();
        var ss = kernel.State.StoryState;
        ss.PentagonTradeFlags = 0x1F; // All traded
        ss.RevealedFlags |= RevelationFlags.R3_Pentagon;
        kernel.State.FractureUnlocked = false;
        kernel.Step();
        Assert.That(ss.PentagonCascadeActive, Is.False);
    }

    [Test]
    public void PentagonCascade_Activates_WhenR3FiredAndFractureUnlocked()
    {
        var kernel = CreateKernel();
        var ss = kernel.State.StoryState;
        ss.PentagonTradeFlags = 0x1F;
        ss.RevealedFlags |= RevelationFlags.R3_Pentagon;
        kernel.State.FractureUnlocked = true;

        // Must have at least one Communion node.
        kernel.State.NodeFactionId[kernel.State.PlayerLocationNodeId] = FactionTweaksV0.CommunionId;

        kernel.Step();
        Assert.That(ss.PentagonCascadeActive, Is.True);
        Assert.That(ss.PentagonCascadeTick, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void PentagonCascade_InjectsFoodIntoCommunionMarkets()
    {
        var kernel = CreateKernel();
        var state = kernel.State;
        var ss = state.StoryState;

        // Set up cascade pre-conditions.
        ss.PentagonTradeFlags = 0x1F;
        ss.RevealedFlags |= RevelationFlags.R3_Pentagon;
        state.FractureUnlocked = true;

        // Use the player's start node as a Communion node.
        var communionNode = state.PlayerLocationNodeId;
        state.NodeFactionId[communionNode] = FactionTweaksV0.CommunionId;

        // Use the existing market at this node (assigned during galaxy gen).
        var marketId = state.Nodes[communionNode].MarketId;
        if (!state.Markets.ContainsKey(marketId))
            state.Markets[marketId] = new Market { Id = marketId };
        state.Markets[marketId].Inventory[WellKnownGoodIds.Food] = 0;

        // First step activates cascade.
        kernel.Step();
        Assert.That(ss.PentagonCascadeActive, Is.True);

        // Step to a food injection tick (multiples of CascadeFoodIntervalTicks).
        // Track peak food to account for NPC consumption on the same tick.
        int targetTick = PentagonBreakTweaksV0.CascadeFoodIntervalTicks * 3;
        int peakFood = 0;
        while (state.Tick < targetTick)
        {
            kernel.Step();
            var food = state.Markets[marketId].Inventory.GetValueOrDefault(WellKnownGoodIds.Food, 0);
            if (food > peakFood) peakFood = food;
        }

        Assert.That(peakFood,
            Is.GreaterThan(0), "Communion market should have food injected by cascade at some point");
    }

    [Test]
    public void PentagonCascade_DoesNotActivateTwice()
    {
        var kernel = CreateKernel();
        var ss = kernel.State.StoryState;
        ss.PentagonTradeFlags = 0x1F;
        ss.RevealedFlags |= RevelationFlags.R3_Pentagon;
        kernel.State.FractureUnlocked = true;
        kernel.State.NodeFactionId[kernel.State.PlayerLocationNodeId] = FactionTweaksV0.CommunionId;

        kernel.Step();
        int firstTick = ss.PentagonCascadeTick;

        // Step again — cascade tick should not change.
        kernel.Step();
        Assert.That(ss.PentagonCascadeTick, Is.EqualTo(firstTick));
    }
}
