using SimCore.Entities;
using SimCore.Tweaks;
using System;

namespace SimCore.Systems;

// Hard-gated tutorial state machine. Evaluates gate conditions each tick
// and advances TutorialPhase when satisfied. All dialogue presentation
// is handled by GDScript (tutorial_director.gd) via SimBridge.
// 7 acts, ~30 active phases. Acts 1-7 replace the original 10-act tutorial.
// Trade loop: 3 manual trades required before automation unlock.
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

            // == Act 1: Cold Open ==========================================
            case TutorialPhase.Awaken:
                // Gate: dialogue dismissed (intro cinematic acknowledged).
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Flight_Intro);
                break;

            case TutorialPhase.Flight_Intro:
                // Gate: dialogue dismissed (Ship Computer message read).
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.First_Dock);
                break;

            case TutorialPhase.First_Dock:
                // Gate: player docked (bridge NotifyTutorialDockV0 fires).
                // Advancement handled by bridge → Module_Calibration_Notice.
                break;

            // == Act 2: The Crew ==========================================
            case TutorialPhase.Module_Calibration_Notice:
                // Gate: dialogue dismissed (Ship Computer calibration notice).
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Maren_Hail);
                break;

            case TutorialPhase.Maren_Hail:
                // Gate: dialogue dismissed (2-beat sequence via DialogueSequence).
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Maren_Settle);
                break;

            case TutorialPhase.Maren_Settle:
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
                {
                    int cargoTotal = 0;
                    foreach (var v in state.PlayerCargo.Values) cargoTotal += v;
                    if (cargoTotal > 0)
                        AdvanceTo(state, ts, TutorialPhase.Buy_React);
                }
                break;

            case TutorialPhase.Buy_React:
                // Gate: dialogue dismissed. First time → cruise drive intro. Loop → skip to travel.
                if (ts.DialogueDismissed)
                {
                    var nextAfterBuy = ts.ManualTradesCompleted > 0
                        ? TutorialPhase.Travel_Prompt
                        : TutorialPhase.Cruise_Intro;
                    AdvanceTo(state, ts, nextAfterBuy);
                }
                break;

            // == Act 3: The Trade Loop ====================================
            case TutorialPhase.Cruise_Intro:
                // Gate: dialogue dismissed (Ship Computer cruise notice).
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Travel_Prompt);
                break;

            case TutorialPhase.Travel_Prompt:
                // Gate: nodesVisited increased (traveled to new system).
                if (state.PlayerStats != null
                    && state.PlayerStats.NodesVisited > ts.NodesVisitedAtPhaseEntry)
                {
                    // First trade: go through Jump_Anomaly. Subsequent: straight to Arrival_Dock.
                    var nextPhase = ts.ManualTradesCompleted == 0
                        ? TutorialPhase.Jump_Anomaly
                        : TutorialPhase.Arrival_Dock;
                    AdvanceTo(state, ts, nextPhase);
                }
                break;

            case TutorialPhase.Jump_Anomaly:
                // Gate: dialogue dismissed (Maren: "Did you see that?").
                // Bridge handles transition from travel → here on first trade.
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Arrival_Dock);
                break;

            case TutorialPhase.Arrival_Dock:
                // Gate: player docked at new station (bridge NotifyTutorialDockV0).
                // Advancement handled by bridge.
                break;

            case TutorialPhase.Sell_Prompt:
                // Gate: goods traded increased (player sold).
                if (state.PlayerStats != null
                    && state.PlayerStats.GoodsTraded > ts.GoodsTradedAtPhaseEntry)
                {
                    AdvanceTo(state, ts, TutorialPhase.First_Profit);
                }
                break;

            case TutorialPhase.First_Profit:
                // Gate: dialogue dismissed. Increment trade count and loop or advance.
                if (ts.DialogueDismissed)
                {
                    ts.ManualTradesCompleted++;
                    if (ts.ManualTradesCompleted >= TutorialTweaksV0.RequiredManualTrades)
                    {
                        // 3 trades done → open the world.
                        AdvanceTo(state, ts, TutorialPhase.World_Intro);
                    }
                    else
                    {
                        // Loop back for another trade run.
                        AdvanceTo(state, ts, TutorialPhase.Travel_Prompt);
                    }
                }
                break;

            // == Act 4: The World =========================================
            case TutorialPhase.World_Intro:
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Explore_Prompt);
                break;

            case TutorialPhase.Explore_Prompt:
                if (state.PlayerStats != null
                    && state.PlayerStats.NodesVisited >= TutorialTweaksV0.ExploreCompleteNodes)
                {
                    AdvanceTo(state, ts, TutorialPhase.Galaxy_Map_Prompt);
                }
                break;

            case TutorialPhase.Galaxy_Map_Prompt:
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Threat_Warning);
                break;

            // == Act 5: The Threat ========================================
            case TutorialPhase.Threat_Warning:
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Dask_Hail);
                break;

            case TutorialPhase.Dask_Hail:
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Combat_Engage);
                break;

            case TutorialPhase.Combat_Engage:
                // Gate: combat completed (NPC fleets destroyed increased).
                if (state.PlayerStats != null
                    && state.PlayerStats.NpcFleetsDestroyed > ts.NpcFleetsDestroyedAtPhaseEntry)
                {
                    AdvanceTo(state, ts, TutorialPhase.Combat_Debrief);
                }
                break;

            case TutorialPhase.Combat_Debrief:
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Repair_Prompt);
                break;

            case TutorialPhase.Repair_Prompt:
                // Gate: hull restored (player fleet hull >= max hull).
                {
                    Fleet? playerFleet = null;
                    foreach (var f in state.Fleets.Values)
                    {
                        if (string.Equals(f.OwnerId, "player", StringComparison.Ordinal))
                        { playerFleet = f; break; }
                    }
                    if (playerFleet != null && playerFleet.HullHp >= playerFleet.HullHpMax)
                        AdvanceTo(state, ts, TutorialPhase.Module_Intro);
                }
                break;

            // == Act 6: The Upgrade =======================================
            case TutorialPhase.Module_Intro:
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Module_Equip);
                break;

            case TutorialPhase.Module_Equip:
                // Gate: player has equipped at least 1 module.
                {
                    Fleet? playerFleet2 = null;
                    foreach (var f in state.Fleets.Values)
                    {
                        if (string.Equals(f.OwnerId, "player", StringComparison.Ordinal))
                        { playerFleet2 = f; break; }
                    }
                    if (playerFleet2 != null && playerFleet2.Slots != null)
                    {
                        bool hasModule = false;
                        foreach (var s in playerFleet2.Slots)
                        {
                            if (!string.IsNullOrEmpty(s.InstalledModuleId))
                            { hasModule = true; break; }
                        }
                        if (hasModule)
                            AdvanceTo(state, ts, TutorialPhase.Module_React);
                    }
                }
                break;

            case TutorialPhase.Module_React:
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Lira_Tease);
                break;

            case TutorialPhase.Lira_Tease:
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Automation_Intro);
                break;

            // == Act 7: The Empire + Graduation ===========================
            case TutorialPhase.Automation_Intro:
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Automation_Create);
                break;

            case TutorialPhase.Automation_Create:
                if (state.Programs != null && state.Programs.Instances.Count > 0)
                    AdvanceTo(state, ts, TutorialPhase.Automation_Running);
                break;

            case TutorialPhase.Automation_Running:
                if (ts.TicksSincePhaseChange >= TutorialTweaksV0.AutomationWaitTicks)
                    AdvanceTo(state, ts, TutorialPhase.Automation_React);
                break;

            case TutorialPhase.Automation_React:
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.FO_Selection);
                break;

            case TutorialPhase.FO_Selection:
                // Gate: FO candidate selected (player picks after meeting all 3 FOs).
                if (ts.SelectedCandidate != FirstOfficerCandidate.None)
                    AdvanceTo(state, ts, TutorialPhase.Mystery_Reveal);
                break;

            // -- Graduation --
            case TutorialPhase.Mystery_Reveal:
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Graduation_Summary);
                break;

            case TutorialPhase.Graduation_Summary:
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.FO_Farewell);
                break;

            case TutorialPhase.FO_Farewell:
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Milestone_Award);
                break;

            case TutorialPhase.Milestone_Award:
                if (ts.DialogueDismissed)
                    AdvanceTo(state, ts, TutorialPhase.Tutorial_Complete);
                break;

            // Dead phases (31, 34-40): never entered in new flow, no-op.
            case TutorialPhase.Commission_Intro:
            case TutorialPhase.Haven_Upgrade_Prompt:
            case TutorialPhase.Haven_React:
            case TutorialPhase.Research_Intro:
            case TutorialPhase.Research_Start:
            case TutorialPhase.Research_React:
            case TutorialPhase.Knowledge_Intro:
            case TutorialPhase.Frontier_Tease:
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
            ts.NpcFleetsDestroyedAtPhaseEntry = state.PlayerStats.NpcFleetsDestroyed;
            ts.TechsUnlockedAtPhaseEntry = state.PlayerStats.TechsUnlocked;
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
