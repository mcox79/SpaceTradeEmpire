using SimCore.Entities;
using SimCore.Tweaks;
using System.Linq;

namespace SimCore.Systems;

// Hard-gated tutorial state machine. Evaluates gate conditions each tick
// and advances TutorialPhase when satisfied. All dialogue presentation
// is handled by GDScript (tutorial_director.gd) via SimBridge.
public static class TutorialSystem
{
    /// <summary>
    /// Per-tick processing: check gate conditions and advance phase.
    /// </summary>
    public static void Process(SimState state)
    {
        if (state.TutorialState == null) return;
        var ts = state.TutorialState;
        if (ts.Phase == TutorialPhase.Tutorial_Complete) return;

        ts.TicksSincePhaseChange++;

        switch (ts.Phase)
        {
            case TutorialPhase.NotStarted:
                // Waiting for GDScript to start tutorial via bridge call.
                break;

            case TutorialPhase.FO_Selection:
                // Rotating auditions: hails shown → dialogue dismissed → skip to Flight_Intro.
                // Selection happens later at FO_Selection_PostTrade (phase 20).
                // Legacy save guard: if candidate already selected, advance directly.
                if (ts.SelectedCandidate != FirstOfficerCandidate.None)
                    AdvanceTo(state, ts, TutorialPhase.FO_Selected_Settle);
                else if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Flight_Intro);
                break;

            case TutorialPhase.FO_Selected_Settle:
                // Gate: dialogue dismissed by player.
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Flight_Intro);
                break;

            case TutorialPhase.Flight_Intro:
                // Gate: dialogue dismissed (controls explained).
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Dock_Prompt);
                break;

            case TutorialPhase.Dock_Prompt:
                // Gate: player docked (has_docked = nodesVisited > 0 || goodsTraded > 0).
                // In practice, docking fires on_proximity_dock_entered which is GDScript.
                // We detect it via PlayerStats changes or PlayerLocationNodeId changes.
                // The bridge will call AdvanceTutorialPhaseV0 when dock event fires.
                break;

            case TutorialPhase.Docked_First:
                // Gate: dialogue dismissed.
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Market_Explain);
                break;

            case TutorialPhase.Market_Explain:
                // Gate: dialogue dismissed.
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Buy_Prompt);
                break;

            case TutorialPhase.Buy_Prompt:
                // Gate: player bought something (cargo count increased).
                if (state.PlayerCargo.Values.Sum() > 0)
                    AdvanceTo(state, ts, TutorialPhase.Buy_Complete);
                break;

            case TutorialPhase.Buy_Complete:
                // Gate: dialogue dismissed.
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Travel_Prompt);
                break;

            case TutorialPhase.Travel_Prompt:
                // Gate: nodesVisited increased AND player arrived at a new node.
                if (state.PlayerStats != null
                    && state.PlayerStats.NodesVisited > ts.NodesVisitedAtPhaseEntry)
                {
                    // Bridge will also advance on dock event at new station.
                    AdvanceTo(state, ts, TutorialPhase.Sell_Prompt);
                }
                break;

            case TutorialPhase.Sell_Prompt:
                // Gate: goods traded increased (player sold).
                if (state.PlayerStats != null
                    && state.PlayerStats.GoodsTraded > ts.GoodsTradedAtPhaseEntry)
                {
                    AdvanceTo(state, ts, TutorialPhase.Sell_Complete);
                }
                break;

            case TutorialPhase.Sell_Complete:
                // Gate: dialogue dismissed (emotional peak acknowledged).
                // If no FO selected yet (rotating auditions), go to post-trade selection.
                if (ts.DialogueDismissed)
                {
                    if (ts.SelectedCandidate == FirstOfficerCandidate.None)
                        AdvanceTo(state, ts, TutorialPhase.FO_Selection_PostTrade);
                    else
                        AdvanceTo(state, ts, TutorialPhase.Faction_Explain);
                }
                break;

            case TutorialPhase.FO_Selection_PostTrade:
                // Gate: FO candidate selected (player chose from enriched overlay).
                if (ts.SelectedCandidate != FirstOfficerCandidate.None)
                    AdvanceTo(state, ts, TutorialPhase.FO_Selected_Settle);
                break;

            case TutorialPhase.Faction_Explain:
                // Gate: dialogue dismissed.
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Explore_Prompt);
                break;

            case TutorialPhase.Explore_Prompt:
                // Gate: 3+ nodes visited.
                if (state.PlayerStats != null
                    && state.PlayerStats.NodesVisited >= TutorialTweaksV0.ExploreCompleteNodes)
                {
                    AdvanceTo(state, ts, TutorialPhase.Explore_Complete);
                }
                break;

            case TutorialPhase.Explore_Complete:
                // Gate: dialogue dismissed.
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Automation_Explain);
                break;

            case TutorialPhase.Automation_Explain:
                // Gate: dialogue dismissed.
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Automation_Prompt);
                break;

            case TutorialPhase.Automation_Prompt:
                // Gate: player has at least one active program.
                if (state.Programs != null && state.Programs.Instances.Count > 0)
                    AdvanceTo(state, ts, TutorialPhase.Automation_Complete);
                break;

            case TutorialPhase.Automation_Complete:
                // Gate: dialogue dismissed.
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Mystery_Tease);
                break;

            case TutorialPhase.Mystery_Tease:
                // Gate: dialogue dismissed.
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Tutorial_Complete);
                break;
        }
    }

    /// <summary>
    /// Advance to a new phase, resetting per-phase tracking state.
    /// </summary>
    private static void AdvanceTo(SimState state, TutorialState ts, TutorialPhase next)
    {
        ts.Phase = next;
        ts.DialogueDismissed = false;
        ts.DialogueSequence = 0;
        ts.TicksSincePhaseChange = 0;

        // Snapshot current stats for delta detection in next phase.
        if (state.PlayerStats != null)
        {
            ts.GoodsTradedAtPhaseEntry = state.PlayerStats.GoodsTraded;
            ts.NodesVisitedAtPhaseEntry = state.PlayerStats.NodesVisited;
        }
    }

    /// <summary>
    /// Check if tutorial is active (not complete and not null).
    /// Used by FirstOfficerSystem to suppress reactive triggers during tutorial.
    /// </summary>
    public static bool IsActive(SimState state)
    {
        return state.TutorialState != null
            && state.TutorialState.Phase != TutorialPhase.Tutorial_Complete;
    }
}
