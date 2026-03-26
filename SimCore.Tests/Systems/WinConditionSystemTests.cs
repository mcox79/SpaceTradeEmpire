using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

[TestFixture]
[Category("WinConditionSystem")]
public sealed class WinConditionSystemTests
{
    private static SimState CreateState(int seed = 42)
    {
        var state = new SimState(seed);
        state.FactionReputation["concord"] = 0;
        state.FactionReputation["weaver"] = 0;
        state.FactionReputation["communion"] = 0;
        state.FactionReputation["chitin"] = 0;
        return state;
    }

    [Test]
    public void NoChosenPath_IsNoOp()
    {
        var state = CreateState();
        state.Haven.ChosenEndgamePath = EndgamePath.None;

        WinConditionSystem.Process(state);

        Assert.That(state.EndgameProgress.CompletionPercent, Is.EqualTo(0));
        Assert.That(state.GameResultValue, Is.EqualTo(GameResult.InProgress));
    }

    [Test]
    public void GameAlreadyOver_IsNoOp()
    {
        var state = CreateState();
        state.GameResultValue = GameResult.Victory;
        state.Haven.ChosenEndgamePath = EndgamePath.Reinforce;

        WinConditionSystem.Process(state);

        // Should not re-compute progress
        Assert.That(state.EndgameProgress.CompletionPercent, Is.EqualTo(0));
    }

    [Test]
    public void Reinforce_PartialProgress()
    {
        var state = CreateState();
        state.Haven.ChosenEndgamePath = EndgamePath.Reinforce;
        state.Haven.Discovered = true;
        // Meet 1 of 4 requirements: concord rep
        state.FactionReputation["concord"] = WinRequirementsTweaksV0.ReinforceMinConcordRep;

        WinConditionSystem.Process(state);

        Assert.That(state.EndgameProgress.CompletionPercent, Is.EqualTo(25));
        Assert.That(state.GameResultValue, Is.EqualTo(GameResult.InProgress));
    }

    [Test]
    public void Reinforce_AllMet_TriggersVictory()
    {
        var state = CreateState();
        state.Haven.ChosenEndgamePath = EndgamePath.Reinforce;
        state.Haven.Discovered = true;
        state.Haven.Tier = WinRequirementsTweaksV0.ReinforceMinHavenTier;
        state.FactionReputation["concord"] = WinRequirementsTweaksV0.ReinforceMinConcordRep;
        state.FactionReputation["weaver"] = WinRequirementsTweaksV0.ReinforceMinWeaverRep;
        state.AdaptationFragments[WinRequirementsTweaksV0.ReinforceRequiredFragment] = new AdaptationFragment
        {
            FragmentId = WinRequirementsTweaksV0.ReinforceRequiredFragment,
            CollectedTick = 0
        };

        WinConditionSystem.Process(state);

        Assert.That(state.EndgameProgress.CompletionPercent, Is.EqualTo(100));
        Assert.That(state.GameResultValue, Is.EqualTo(GameResult.Victory));
    }

    [Test]
    public void Naturalize_RequiresTwoFragments()
    {
        var state = CreateState();
        state.Haven.ChosenEndgamePath = EndgamePath.Naturalize;
        state.Haven.Discovered = true;
        state.Haven.Tier = WinRequirementsTweaksV0.NaturalizeMinHavenTier;
        state.FactionReputation["communion"] = WinRequirementsTweaksV0.NaturalizeMinCommunionRep;
        // Only 1 of 2 required fragments
        state.AdaptationFragments[WinRequirementsTweaksV0.NaturalizeRequiredFragment1] = new AdaptationFragment
        {
            FragmentId = WinRequirementsTweaksV0.NaturalizeRequiredFragment1,
            CollectedTick = 0
        };

        WinConditionSystem.Process(state);

        Assert.That(state.EndgameProgress.CompletionPercent, Is.EqualTo(75));
        Assert.That(state.GameResultValue, Is.EqualTo(GameResult.InProgress));
    }
}
