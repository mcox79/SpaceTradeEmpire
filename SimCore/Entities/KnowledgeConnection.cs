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
}
