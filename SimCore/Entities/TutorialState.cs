using System.Text.Json.Serialization;

namespace SimCore.Entities;

// Tutorial phase progression — hard-gated, FO-voiced onboarding.
// Each phase has an entry condition, FO dialogue, player action (gate), and exit condition.
// Phases advance deterministically based on game state checks in TutorialSystem.
public enum TutorialPhase
{
    NotStarted = 0,

    // Act 1: The Crew
    FO_Selection = 1,         // Choose your First Officer
    FO_Selected_Settle = 2,   // FO personality greeting

    // Act 2: First Steps
    Flight_Intro = 3,         // FO explains controls, ship unfreezes
    Dock_Prompt = 4,          // Player must dock at station
    Docked_First = 5,         // FO explains market

    // Act 3: The Trade
    Market_Explain = 6,       // FO elaborates on supply/demand
    Buy_Prompt = 7,           // Player must buy a good
    Buy_Complete = 8,         // FO reacts to purchase
    Travel_Prompt = 9,        // Player must travel + dock at new station
    Sell_Prompt = 10,         // Player must sell goods
    Sell_Complete = 11,       // FO celebrates profit (emotional peak)

    // Act 4: The World
    Faction_Explain = 12,     // FO explains factions/territory
    Explore_Prompt = 13,      // Player must visit 3+ nodes
    Explore_Complete = 14,    // Station/Intel tabs unlock

    // Act 5: The Real Game
    Automation_Explain = 15,  // FO explains programs (the reveal)
    Automation_Prompt = 16,   // Player must create a program
    Automation_Complete = 17, // FO celebrates automation

    // Act 6: The Mystery
    Mystery_Tease = 18,       // FO hints at precursor lore
    Tutorial_Complete = 19,   // Tutorial done, regular FO triggers activate

    // Post-trade FO selection (rotating auditions): player chooses after first trade loop
    FO_Selection_PostTrade = 20
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
}
