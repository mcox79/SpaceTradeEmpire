using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Linq;

namespace SimCore.Tests.Systems;

// GATE.T18.NARRATIVE.WAR_CONSEQUENCE.001
[TestFixture]
public sealed class WarConsequenceTests
{
    private static void AdvanceTo(SimState state, int targetTick)
    {
        while (state.Tick < targetTick)
            state.AdvanceTick();
    }

    private SimState MakeWarState()
    {
        var state = new SimState(42);
        state.Nodes["star_0"] = new Node { Id = "star_0" };
        state.Nodes["star_1"] = new Node { Id = "star_1" };
        state.NodeFactionId["star_0"] = "Valorin";
        state.NodeFactionId["star_1"] = "Communion";

        state.Warfronts["wf_1"] = new WarfrontState
        {
            Id = "wf_1",
            CombatantA = "Valorin",
            CombatantB = "Communion",
            Intensity = WarfrontIntensity.Skirmish
        };
        return state;
    }

    [Test]
    public void CheckAndCreate_CreatesConsequenceForWarGoods()
    {
        var state = MakeWarState();
        AdvanceTo(state, 100);

        WarConsequenceSystem.CheckAndCreateConsequence(
            state, "star_0", WellKnownGoodIds.Munitions, 50);

        Assert.That(state.WarConsequences, Has.Count.EqualTo(1));
        var wc = state.WarConsequences.Values.First();
        Assert.That(wc.GoodId, Is.EqualTo(WellKnownGoodIds.Munitions));
        Assert.That(wc.Quantity, Is.EqualTo(50));
        Assert.That(wc.ManifestText, Is.Not.Empty);
        Assert.That(wc.IsResolved, Is.False);
        Assert.That(wc.DelayTicks, Is.EqualTo(NarrativeTweaksV0.WarConsequenceDelayTicks));
    }

    [Test]
    public void CheckAndCreate_IgnoresNonWarGoods()
    {
        var state = MakeWarState();
        WarConsequenceSystem.CheckAndCreateConsequence(state, "star_0", "food", 50);
        Assert.That(state.WarConsequences, Is.Empty);
    }

    [Test]
    public void CheckAndCreate_IgnoresNonWarfrontNode()
    {
        var state = new SimState(42);
        state.Nodes["star_0"] = new Node { Id = "star_0" };
        state.NodeFactionId["star_0"] = "Neutral";

        WarConsequenceSystem.CheckAndCreateConsequence(
            state, "star_0", WellKnownGoodIds.Munitions, 50);
        Assert.That(state.WarConsequences, Is.Empty);
    }

    [Test]
    public void Process_ResolvesAfterDelay()
    {
        var state = MakeWarState();
        AdvanceTo(state, 100);

        WarConsequenceSystem.CheckAndCreateConsequence(
            state, "star_0", WellKnownGoodIds.Munitions, 50);
        var wc = state.WarConsequences.Values.First();
        int createdTick = state.Tick;

        // Not resolved yet
        AdvanceTo(state, createdTick + NarrativeTweaksV0.WarConsequenceDelayTicks - 1);
        WarConsequenceSystem.Process(state);
        Assert.That(wc.IsResolved, Is.False);

        // Now resolved
        AdvanceTo(state, createdTick + NarrativeTweaksV0.WarConsequenceDelayTicks);
        WarConsequenceSystem.Process(state);
        Assert.That(wc.IsResolved, Is.True);
        Assert.That(wc.ConsequenceText, Is.Not.Empty);
        Assert.That(wc.ResolvedTick, Is.EqualTo(state.Tick));
    }

    [Test]
    public void Process_AlreadyResolvedSkipped()
    {
        var state = MakeWarState();
        AdvanceTo(state, 100);

        WarConsequenceSystem.CheckAndCreateConsequence(
            state, "star_0", WellKnownGoodIds.Composites, 30);

        int createdTick = state.Tick;
        AdvanceTo(state, createdTick + NarrativeTweaksV0.WarConsequenceDelayTicks);
        WarConsequenceSystem.Process(state);

        var wc = state.WarConsequences.Values.First();
        int resolvedTick = wc.ResolvedTick;

        AdvanceTo(state, resolvedTick + 100);
        WarConsequenceSystem.Process(state);
        Assert.That(wc.ResolvedTick, Is.EqualTo(resolvedTick));
    }

    [Test]
    public void Process_EmptyConsequences_NoException()
    {
        var state = new SimState(42);
        WarConsequenceSystem.Process(state);
        Assert.Pass();
    }

    [Test]
    public void AllWarGoods_CreateConsequences()
    {
        var state = MakeWarState();
        AdvanceTo(state, 100);

        WarConsequenceSystem.CheckAndCreateConsequence(
            state, "star_0", WellKnownGoodIds.Munitions, 10);
        state.AdvanceTick();
        WarConsequenceSystem.CheckAndCreateConsequence(
            state, "star_0", WellKnownGoodIds.Composites, 10);
        state.AdvanceTick();
        WarConsequenceSystem.CheckAndCreateConsequence(
            state, "star_0", WellKnownGoodIds.Fuel, 10);

        Assert.That(state.WarConsequences, Has.Count.EqualTo(3));
    }

    [Test]
    public void CheckAndCreate_ZeroQuantity_Ignored()
    {
        var state = MakeWarState();
        WarConsequenceSystem.CheckAndCreateConsequence(
            state, "star_0", WellKnownGoodIds.Munitions, 0);
        Assert.That(state.WarConsequences, Is.Empty);
    }

    [Test]
    public void CheckAndCreate_NegativeQuantity_Ignored()
    {
        var state = MakeWarState();
        WarConsequenceSystem.CheckAndCreateConsequence(
            state, "star_0", WellKnownGoodIds.Munitions, -5);
        Assert.That(state.WarConsequences, Is.Empty);
    }

    [Test]
    public void CheckAndCreate_DuplicateIdOverwrites()
    {
        var state = MakeWarState();
        AdvanceTo(state, 100);

        // Same tick + node + good → same consequence ID → overwrites
        WarConsequenceSystem.CheckAndCreateConsequence(
            state, "star_0", WellKnownGoodIds.Munitions, 50);
        WarConsequenceSystem.CheckAndCreateConsequence(
            state, "star_0", WellKnownGoodIds.Munitions, 75);

        Assert.That(state.WarConsequences, Has.Count.EqualTo(1));
        var wc = state.WarConsequences.Values.First();
        Assert.That(wc.Quantity, Is.EqualTo(75));
    }

    [Test]
    public void Process_MultipleConsequences_MixedResolution()
    {
        var state = MakeWarState();
        AdvanceTo(state, 100);

        WarConsequenceSystem.CheckAndCreateConsequence(
            state, "star_0", WellKnownGoodIds.Munitions, 10);

        AdvanceTo(state, 110);
        WarConsequenceSystem.CheckAndCreateConsequence(
            state, "star_0", WellKnownGoodIds.Composites, 20);

        // Advance past first delay but not second
        AdvanceTo(state, 100 + NarrativeTweaksV0.WarConsequenceDelayTicks);
        WarConsequenceSystem.Process(state);

        int resolved = state.WarConsequences.Values.Count(w => w.IsResolved);
        int pending = state.WarConsequences.Values.Count(w => !w.IsResolved);
        Assert.That(resolved, Is.EqualTo(1));
        Assert.That(pending, Is.EqualTo(1));
    }
}
