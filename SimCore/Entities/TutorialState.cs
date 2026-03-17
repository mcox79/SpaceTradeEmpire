using System.Text.Json.Serialization;

namespace SimCore.Entities;

// Tutorial phase progression — hard-gated, FO-voiced onboarding.
// 10 acts, 45 phases. Each phase has entry condition, FO dialogue, player action (gate), and exit condition.
// Phases advance deterministically based on game state checks in TutorialSystem.
public enum TutorialPhase
{
    NotStarted = 0,

    // Act 1: Cold Open (Ship Computer only — no FO yet)
    Awaken = 1,               // Intro cinematic completes
    Flight_Intro = 2,         // Ship Computer: "Systems online."
    First_Dock = 3,           // Player docks at station

    // Act 2: The Crew (Maren introduced)
    Maren_Hail = 4,           // Maren hails: route analysis (2-beat)
    Maren_Settle = 5,         // Maren reads situation: "Thin margins, but they exist."
    Market_Explain = 6,       // Maren explains supply/demand, FO TIP
    Buy_Prompt = 7,           // Player must buy a surplus good
    Buy_React = 8,            // Maren validates choice, flags sell target

    // Act 3: The Trade (First trade loop → FO Selection)
    Travel_Prompt = 9,        // Player must travel to another station
    Arrival_Dock = 10,        // Dock at destination (wrong-station warning if bad)
    Sell_Prompt = 11,         // Player must sell cargo
    First_Profit = 12,        // Maren: "Profit logged." → "We need help." (2-beat)
    FO_Selection = 13,        // Choose from 3 FO candidates (post-trade, with context)

    // Act 4: The World (Galaxy opens up)
    World_Intro = 14,         // FO explains factions, tariffs, territory (2-beat)
    Explore_Prompt = 15,      // Visit 3 systems total
    Explore_Complete = 16,    // Station/Intel tabs unlock
    Galaxy_Map_Prompt = 17,   // Press M — galaxy map wow moment

    // Act 5: The Threat (Combat intro — Dask cameo)
    Threat_Warning = 18,      // FO: "Scanner contact. Hostile."
    Dask_Hail = 19,           // Dask hails with combat briefing (staggered intro)
    Combat_Engage = 20,       // Defeat tutorial pirate (guaranteed win)
    Combat_Debrief = 21,      // Debrief + hull damage motivation (2-beat)
    Repair_Prompt = 22,       // Dock to repair hull

    // Act 6: The Upgrade (Modules — Lira cameo)
    Module_Intro = 23,        // FO: "That fight exposed a weakness."
    Module_Equip = 24,        // Equip a module (free starter granted)
    Module_React = 25,        // FO: "Good modules? Factions guard those."
    Lira_Tease = 26,          // Lira hails about drive resonance (staggered intro)

    // Act 7: The Empire (Automation reveal)
    Automation_Intro = 27,    // FO: "What if this route ran itself?" (2-beat)
    Automation_Create = 28,   // Create a TradeCharter program
    Automation_Running = 29,  // Watch passive income (30 ticks)
    Automation_React = 30,    // FO: "One route. Imagine ten."
    Commission_Intro = 31,    // FO explains commissions/bounties

    // Act 8: The Haven (Home base)
    Haven_Discovery = 32,     // Dock at Haven
    Haven_Tour = 33,          // FO tour: fabricator, lab, market (3-beat)
    Haven_Upgrade_Prompt = 34,// Optional: start an upgrade (soft-gated)
    Haven_React = 35,         // FO: "Haven will be here. The galaxy won't wait."

    // Act 9: The Frontier (Research + Knowledge)
    Research_Intro = 36,      // FO: "Better modules exist. Research unlocks them." (2-beat)
    Research_Start = 37,      // Start a research project
    Research_React = 38,      // FO personality-colored reaction
    Knowledge_Intro = 39,     // Knowledge Web panel introduced
    Frontier_Tease = 40,      // FO: fracture drive tease

    // Act 10: Graduation (Capstone)
    Mystery_Reveal = 41,      // Precursor mystery deepened (2-beat)
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

    // Whether the tutorial pirate has been spawned (Act 5).
    [JsonInclude] public bool TutorialPirateSpawned { get; set; }

    // Whether the tutorial starter module has been granted (Act 6).
    [JsonInclude] public bool TutorialModuleGranted { get; set; }
}
