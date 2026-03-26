using SimCore.Entities;
using SimCore.Tweaks;
using System;

namespace SimCore.Systems;

// GATE.S8.WIN.PATH_EVAL.001: Evaluate per-path victory conditions each tick.
// GATE.S8.WIN.PROGRESS_TRACK.001: Compute endgame progress snapshot for UI.
public static class WinConditionSystem
{
    public static void Process(SimState state)
    {
        // Only check while game is in progress with a chosen path.
        if (state.GameResultValue != GameResult.InProgress) return;
        if (state.Haven.ChosenEndgamePath == EndgamePath.None) return;

        // Compute progress snapshot (always, even if not yet victorious).
        var progress = state.Haven.ChosenEndgamePath switch
        {
            EndgamePath.Reinforce => ComputeReinforceProgress(state),
            EndgamePath.Naturalize => ComputeNaturalizeProgress(state),
            EndgamePath.Renegotiate => ComputeRenegotiateProgress(state),
            _ => new EndgameProgress()
        };
        state.EndgameProgress = progress;

        if (progress.CompletionPercent >= 100)
        {
            state.GameResultValue = GameResult.Victory;
        }
    }

    private static EndgameProgress ComputeReinforceProgress(SimState state)
    {
        var p = new EndgameProgress();
        GetFactionRep(state, "concord", out int concordRep);
        GetFactionRep(state, "weaver", out int weaverRep);

        p.FactionRep1Id = "concord";
        p.FactionRep1Current = concordRep;
        p.FactionRep1Required = WinRequirementsTweaksV0.ReinforceMinConcordRep;
        p.FactionRep1Met = concordRep >= WinRequirementsTweaksV0.ReinforceMinConcordRep;

        p.FactionRep2Id = "weaver";
        p.FactionRep2Current = weaverRep;
        p.FactionRep2Required = WinRequirementsTweaksV0.ReinforceMinWeaverRep;
        p.FactionRep2Met = weaverRep >= WinRequirementsTweaksV0.ReinforceMinWeaverRep;

        p.HavenTierCurrent = (int)state.Haven.Tier;
        p.HavenTierRequired = (int)WinRequirementsTweaksV0.ReinforceMinHavenTier;
        p.HavenTierMet = state.Haven.Tier >= WinRequirementsTweaksV0.ReinforceMinHavenTier;

        p.Fragment1Id = WinRequirementsTweaksV0.ReinforceRequiredFragment;
        p.Fragment1Met = IsFragmentCollected(state, p.Fragment1Id);

        // 4 requirements: 2 faction reps, haven tier, 1 fragment.
        int met = (p.FactionRep1Met ? 1 : 0) + (p.FactionRep2Met ? 1 : 0) +
                  (p.HavenTierMet ? 1 : 0) + (p.Fragment1Met ? 1 : 0);
        p.CompletionPercent = met * 25; // STRUCTURAL: 100/4
        return p;
    }

    private static EndgameProgress ComputeNaturalizeProgress(SimState state)
    {
        var p = new EndgameProgress();
        GetFactionRep(state, "communion", out int communionRep);

        p.FactionRep1Id = "communion";
        p.FactionRep1Current = communionRep;
        p.FactionRep1Required = WinRequirementsTweaksV0.NaturalizeMinCommunionRep;
        p.FactionRep1Met = communionRep >= WinRequirementsTweaksV0.NaturalizeMinCommunionRep;

        p.HavenTierCurrent = (int)state.Haven.Tier;
        p.HavenTierRequired = (int)WinRequirementsTweaksV0.NaturalizeMinHavenTier;
        p.HavenTierMet = state.Haven.Tier >= WinRequirementsTweaksV0.NaturalizeMinHavenTier;

        p.Fragment1Id = WinRequirementsTweaksV0.NaturalizeRequiredFragment1;
        p.Fragment1Met = IsFragmentCollected(state, p.Fragment1Id);

        p.Fragment2Id = WinRequirementsTweaksV0.NaturalizeRequiredFragment2;
        p.Fragment2Met = IsFragmentCollected(state, p.Fragment2Id);

        // 4 requirements: 1 faction rep, haven tier, 2 fragments.
        int met = (p.FactionRep1Met ? 1 : 0) + (p.HavenTierMet ? 1 : 0) +
                  (p.Fragment1Met ? 1 : 0) + (p.Fragment2Met ? 1 : 0);
        p.CompletionPercent = met * 25; // STRUCTURAL: 100/4
        return p;
    }

    private static EndgameProgress ComputeRenegotiateProgress(SimState state)
    {
        var p = new EndgameProgress();
        GetFactionRep(state, "communion", out int communionRep);

        p.FactionRep1Id = "communion";
        p.FactionRep1Current = communionRep;
        p.FactionRep1Required = WinRequirementsTweaksV0.RenegotiateMinCommunionRep;
        p.FactionRep1Met = communionRep >= WinRequirementsTweaksV0.RenegotiateMinCommunionRep;

        p.HavenTierCurrent = (int)state.Haven.Tier;
        p.HavenTierRequired = (int)WinRequirementsTweaksV0.RenegotiateMinHavenTier;
        p.HavenTierMet = state.Haven.Tier >= WinRequirementsTweaksV0.RenegotiateMinHavenTier;

        p.Fragment1Id = WinRequirementsTweaksV0.RenegotiateRequiredFragment;
        p.Fragment1Met = IsFragmentCollected(state, p.Fragment1Id);

        p.RevelationsCurrent = state.StoryState.RevelationCount;
        p.RevelationsRequired = WinRequirementsTweaksV0.RenegotiateRequiredRevelations;
        p.RevelationsMet = p.RevelationsCurrent >= p.RevelationsRequired;

        // 4 requirements: 1 faction rep, haven tier, 1 fragment, revelations.
        int met = (p.FactionRep1Met ? 1 : 0) + (p.HavenTierMet ? 1 : 0) +
                  (p.Fragment1Met ? 1 : 0) + (p.RevelationsMet ? 1 : 0);
        p.CompletionPercent = met * 25; // STRUCTURAL: 100/4
        return p;
    }

    private static bool GetFactionRep(SimState state, string factionId, out int rep)
    {
        return state.FactionReputation.TryGetValue(factionId, out rep);
    }

    private static bool IsFragmentCollected(SimState state, string fragmentId)
    {
        return state.AdaptationFragments.TryGetValue(fragmentId, out var frag) && frag.IsCollected;
    }
}
