using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

[TestFixture]
[Category("TutorialSystem")]
public sealed class TutorialSystemTests
{
    private static SimState CreateState(int seed = 42)
    {
        var state = new SimState(seed);
        state.TutorialState = new TutorialState
        {
            Phase = TutorialPhase.NotStarted
        };
        return state;
    }

    [Test]
    public void NullTutorialState_IsNoOp()
    {
        var state = new SimState(42);
        state.TutorialState = null;

        TutorialSystem.Process(state);

        Assert.That(state.TutorialState, Is.Null);
    }

    [Test]
    public void TutorialComplete_IsNoOp()
    {
        var state = CreateState();
        state.TutorialState!.Phase = TutorialPhase.Tutorial_Complete;
        int ticksBefore = state.TutorialState.TicksSincePhaseChange;

        TutorialSystem.Process(state);

        Assert.That(state.TutorialState.Phase, Is.EqualTo(TutorialPhase.Tutorial_Complete));
        Assert.That(state.TutorialState.TicksSincePhaseChange, Is.EqualTo(ticksBefore));
    }

    [Test]
    public void TicksSincePhaseChange_Increments()
    {
        var state = CreateState();
        state.TutorialState!.Phase = TutorialPhase.Buy_Prompt;
        state.TutorialState.TicksSincePhaseChange = 0;

        TutorialSystem.Process(state);

        Assert.That(state.TutorialState.TicksSincePhaseChange, Is.GreaterThan(0));
    }

    [Test]
    public void DialogueDismissed_AdvancesPhase()
    {
        var state = CreateState();
        state.TutorialState!.Phase = TutorialPhase.Awaken;
        state.TutorialState.DialogueDismissed = true;

        TutorialSystem.Process(state);

        // Should advance past Awaken
        Assert.That(state.TutorialState.Phase, Is.GreaterThan(TutorialPhase.Awaken));
        // DialogueDismissed should be reset on phase change
        Assert.That(state.TutorialState.DialogueDismissed, Is.False);
    }

    [Test]
    public void FirstProfit_IncrementsManualTrades()
    {
        var state = CreateState();
        state.TutorialState!.Phase = TutorialPhase.First_Profit;
        state.TutorialState.ManualTradesCompleted = 0;
        state.TutorialState.DialogueDismissed = true;
        // PlayerStats must show a trade happened
        state.PlayerStats.GoodsTraded = 1;
        state.TutorialState.GoodsTradedAtPhaseEntry = 0;

        TutorialSystem.Process(state);

        Assert.That(state.TutorialState.ManualTradesCompleted, Is.GreaterThanOrEqualTo(1));
    }
}
