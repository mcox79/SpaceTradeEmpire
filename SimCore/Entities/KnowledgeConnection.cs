using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.T18.NARRATIVE.KNOWLEDGE_GRAPH.001: Connection type between discoveries.
public enum KnowledgeConnectionType
{
    SameOrigin = 0,   // Two discoveries from the same ancient event
    Lead = 1,         // One discovery points to another location
    FactionLink = 2,  // Discovery connects to a known faction
    TechUnlock = 3,   // Discovery reveals a researchable technology
    LoreFragment = 4  // Discovery adds to a narrative thread
}

// GATE.T18.NARRATIVE.KNOWLEDGE_GRAPH.001: Connection between two discoveries.
// Shows as "?" when both endpoints are Seen but not Analyzed.
// Fully revealed when both endpoints are Analyzed.
public sealed class KnowledgeConnection
{
    [JsonInclude] public string ConnectionId { get; set; } = "";
    [JsonInclude] public string SourceDiscoveryId { get; set; } = "";
    [JsonInclude] public string TargetDiscoveryId { get; set; } = "";
    [JsonInclude] public KnowledgeConnectionType ConnectionType { get; set; } = KnowledgeConnectionType.SameOrigin;

    // Whether the connection has been revealed to the player.
    // False = shows as "?" (connection exists but type unknown).
    // True = connection type and description visible.
    [JsonInclude] public bool IsRevealed { get; set; }
    [JsonInclude] public int RevealedTick { get; set; }

    // Human-readable description shown when revealed.
    [JsonInclude] public string Description { get; set; } = "";

    // GATE.T57.KG.LINK_FEEDBACK.001: Player-created speculative link state.
    [JsonInclude] public KGLinkState LinkState { get; set; } = KGLinkState.None;
}

// GATE.T57.KG.LINK_FEEDBACK.001: Speculative link state machine (Obra Dinn model).
public enum KGLinkState
{
    None = 0,           // System-created or not player-linked
    Speculative = 1,    // Player proposed this link
    Plausible = 2,      // System found partial evidence
    Confirmed = 3,      // Both endpoints analyzed + system verified
    Contradicted = 4    // Evidence contradicts player theory
}

// GATE.T57.KG.PLAYER_VERBS.001: Player annotation on a discovery node in the knowledge graph.
public sealed class KGAnnotation
{
    [JsonInclude] public string DiscoveryId { get; set; } = "";
    [JsonInclude] public string Text { get; set; } = "";  // Max 50 chars
    [JsonInclude] public int CreatedTick { get; set; }
}

// GATE.T57.KG.PLAYER_VERBS.001: Pin state for a discovery node.
// Max 3 pins active at once.
public sealed class KGPin
{
    [JsonInclude] public string DiscoveryId { get; set; } = "";
    [JsonInclude] public int PinnedTick { get; set; }
}

// GATE.T57.KG.PLAYER_VERBS.001: FO flag on a discovery — asks FO to evaluate.
public sealed class KGFOFlag
{
    [JsonInclude] public string DiscoveryId { get; set; } = "";
    [JsonInclude] public int FlaggedTick { get; set; }
    [JsonInclude] public bool FOResponded { get; set; }
    [JsonInclude] public string FOResponse { get; set; } = "";
}

// GATE.T57.KG.PLAYER_VERBS.001: Player KG verb state stored in IntelBook.
public sealed class KnowledgeGraphPlayerState
{
    [JsonInclude] public List<KGPin> Pins { get; set; } = new();           // Max 3
    [JsonInclude] public List<KGAnnotation> Annotations { get; set; } = new();
    [JsonInclude] public List<KGFOFlag> FOFlags { get; set; } = new();
    // Compare pairs: "discoveryId_A|discoveryId_B" for side-by-side view.
    [JsonInclude] public List<string> ComparePairs { get; set; } = new();
}

// GATE.T58.KG.MILESTONE_ENTITY.001: 7-milestone progressive disclosure for KG verbs.
// Per ExplorationDiscovery.md R12: NOT all verbs at once. Each milestone unlocks next verb.
public enum KGMilestone
{
    Geographic = 0,     // M1: First discovery Seen → map node appears
    Pin = 1,            // M2: 3 discoveries Seen → Pin verb unlocked
    Relational = 2,     // M3: First connection revealed → connection edges visible
    Annotate = 3,       // M4: 5 discoveries + 1 Analyzed → Annotate verb unlocked
    Flag = 4,           // M5: FO promoted + 3 Analyzed → Flag-for-FO verb unlocked
    Link = 5,           // M6: 2 connections revealed → Speculative Link verb unlocked
    Compare = 6         // M7: 8 discoveries + 3 Analyzed → Compare verb unlocked
}

// GATE.T58.KG.MILESTONE_ENTITY.001: KG milestone progression state stored in IntelBook.
public sealed class KGMilestoneState
{
    // Highest milestone reached (milestones below this are also unlocked).
    [JsonInclude] public KGMilestone HighestMilestone { get; set; } = KGMilestone.Geographic;
    // Tick when each milestone was first reached.
    [JsonInclude] public Dictionary<int, int> MilestoneTicks { get; set; } = new();
    // Whether the milestone unlock notification has been consumed by the bridge.
    [JsonInclude] public int PendingMilestoneNotification { get; set; } = -1; // -1 = none
}
