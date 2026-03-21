using System.Text.Json.Serialization;

namespace SimCore.Entities;

// Tutorial phase progression — hard-gated, FO-voiced onboarding.
// 7 acts, ~30 active phases. Integer values preserved for save compatibility.
// Phases advance deterministically based on game state checks in TutorialSystem.
public enum TutorialPhase
{
    NotStarted = 0,

    // Act 1: Cold Open (Ship Computer only — no FO yet)
    Awaken = 1,               // Intro cinematic completes
    Flight_Intro = 2,         // Ship Computer: "Systems online. WASD to fly, click to set course."
    First_Dock = 3,           // Player docks at station

    // Act 2: The Crew (Maren introduced)
    Maren_Hail = 4,           // Maren hails: route analysis (2-beat)
    Maren_Settle = 5,         // Maren reads situation: warfront context, thin margins
    Market_Explain = 6,       // Maren: probability framing on margin
    Buy_Prompt = 7,           // Player must buy a surplus good
    Buy_React = 8,            // Maren validates choice

    // Act 3: The Trade Loop (3 manual trades required — pain before relief)
    Travel_Prompt = 9,        // Player must travel to another station
    Arrival_Dock = 10,        // Dock at destination (wrong-station warning if bad)
    Sell_Prompt = 11,         // Player must sell cargo
    First_Profit = 12,        // Maren: profit logged. Loops to Travel_Prompt until 3 trades done.
    FO_Selection = 13,        // Choose from 3 FO candidates (moved to Act 7, value preserved)

    // Act 4: The World (Galaxy opens up)
    World_Intro = 14,         // FO: warfront context, factions, territory (2-beat)
    Explore_Prompt = 15,      // Visit 3 systems total
    Cruise_Intro = 16,        // Ship Computer: cruise drive available (repurposed from Explore_Complete)
    Galaxy_Map_Prompt = 17,   // Galaxy map — wow moment

    // Act 5: The Threat (Combat intro — Dask cameo)
    Threat_Warning = 18,      // FO: "Scanner contact. Hostile."
    Dask_Hail = 19,           // Dask hails with combat briefing (staggered intro)
    Combat_Engage = 20,       // Defeat tutorial pirate (guaranteed win)
    Combat_Debrief = 21,      // Debrief + fuel mention (2-beat)
    Repair_Prompt = 22,       // Dock to repair hull

    // Act 6: The Upgrade (Modules — Lira cameo)
    Module_Intro = 23,        // FO: "That fight exposed a weakness."
    Module_Equip = 24,        // Equip a module (free starter granted)
    Module_React = 25,        // FO: personality reaction to upgrade
    Lira_Tease = 26,          // Lira hails about drive harmonic signature (staggered intro)

    // Act 7: The Empire + Graduation
    Automation_Intro = 27,    // FO: "What if this route ran itself?" (2-beat)
    Automation_Create = 28,   // Create a TradeCharter program
    Automation_Running = 29,  // Watch passive income (30 ticks)
    Automation_React = 30,    // FO: "One route. Imagine ten."

    // [REMOVED — save compat] Commission_Intro moved to post-tutorial progressive disclosure
    Commission_Intro = 31,    // DEAD — never entered. Kept for save compatibility.

    // New phases (reusing freed slots + extending)
    Module_Calibration_Notice = 32,  // Ship Computer: instrument calibration variance (mystery seed)
    Jump_Anomaly = 33,               // Maren (rotating): "Did you see that?" (world-is-watching seed)

    // [REMOVED — save compat] Haven/Research/Knowledge/Frontier moved to progressive disclosure
    Haven_Upgrade_Prompt = 34, // DEAD — never entered
    Haven_React = 35,          // DEAD — never entered
    Research_Intro = 36,       // DEAD — never entered
    Research_Start = 37,       // DEAD — never entered
    Research_React = 38,       // DEAD — never entered
    Knowledge_Intro = 39,      // DEAD — never entered
    Frontier_Tease = 40,       // DEAD — never entered

    // Act 7 continued: Graduation (Capstone)
    Mystery_Reveal = 41,      // Selected FO: drive mystery deepened (2-beat)
    Graduation_Summary = 42,  // Ship Computer: stats recap
    FO_Farewell = 43,         // Personality-specific farewell
    Milestone_Award = 44,     // "CAPTAIN'S COMMISSION" milestone
    Tutorial_Complete = 45    // Full game unlocked
}

// Serializable tutorial progression state.
// Null on SimState means "tutorial never started" (existing saves, tutorial skipped).
public sealed class TutorialState
{
    [JsonInclude] public TutorialPhase Phase { get; set; } = TutorialPhase.NotStarted;

    // Which FO candidate was selected (None until FO_Selection completes).
    [JsonInclude] public FirstOfficerCandidate SelectedCandidate { get; set; } = FirstOfficerCandidate.None;

    // Whether the current phase's dialogue has been shown and dismissed.
    // Set by bridge when GDScript reports dialogue advanced.
    [JsonInclude] public bool DialogueDismissed { get; set; }

    // Snapshot of goods traded at phase entry — used to detect first buy/sell.
    [JsonInclude] public int GoodsTradedAtPhaseEntry { get; set; }

    // Snapshot of nodes visited at phase entry — used to detect travel.
    [JsonInclude] public int NodesVisitedAtPhaseEntry { get; set; }

    // Stall timer: ticks since last phase change (for nudge dialogue).
    [JsonInclude] public int TicksSincePhaseChange { get; set; }

    // Multi-line dialogue sequence index within the current phase (0-based).
    // Reset to 0 on phase change. Incremented by DismissTutorialDialogueV0
    // when more sequence lines exist for the current phase.
    [JsonInclude] public int DialogueSequence { get; set; }

    // Snapshot of techs unlocked at phase entry — used to detect research progress.
    [JsonInclude] public int TechsUnlockedAtPhaseEntry { get; set; }

    // Snapshot of NPC fleets destroyed at phase entry — used to detect combat completion.
    [JsonInclude] public int NpcFleetsDestroyedAtPhaseEntry { get; set; }

    // Manual trade loop counter: incremented on each First_Profit.
    // Automation unlocks after RequiredManualTrades (3) completed trades.
    [JsonInclude] public int ManualTradesCompleted { get; set; }

    // Whether the tutorial pirate has been spawned (Act 5).
    [JsonInclude] public bool TutorialPirateSpawned { get; set; }

    // Whether the tutorial starter module has been granted (Act 6).
    [JsonInclude] public bool TutorialModuleGranted { get; set; }
}
