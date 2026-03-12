using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.T18.NARRATIVE.WAR_FACES.001: Narrative NPC kinds (three war faces).
public enum NarrativeNpcKind
{
    Regular = 0,      // NPC trader on overlapping route; vanishes when war reaches home
    Stationmaster = 1, // Named NPC at frequented station; 8-10 ambient lines
    Enemy = 2          // Valorin patrol captain; interdicts then reappears at Communion station
}

// GATE.T18.NARRATIVE.WAR_FACES.001: Named story NPC for war faces system.
// Three NPCs that give the wars a human face through the trade loop.
public sealed class NarrativeNpc
{
    [JsonInclude] public string NpcId { get; set; } = "";
    [JsonInclude] public NarrativeNpcKind Kind { get; set; } = NarrativeNpcKind.Regular;
    [JsonInclude] public string Name { get; set; } = "";

    // Current location node.
    [JsonInclude] public string NodeId { get; set; } = "";
    // Home node (Regular: war reaching here triggers disappearance).
    [JsonInclude] public string HomeNodeId { get; set; } = "";
    // Faction affiliation.
    [JsonInclude] public string FactionId { get; set; } = "";

    // Dialogue state: index of next line to show (for Stationmaster progression).
    [JsonInclude] public int DialogueState { get; set; }
    // Triggers already fired (prevents repeats). Keyed by trigger token.
    [JsonInclude] public List<string> FiredTriggers { get; private set; } = new();

    // Alive state. When false, NPC has been removed from the world.
    [JsonInclude] public bool IsAlive { get; set; } = true;
    [JsonInclude] public int VanishTick { get; set; }
    [JsonInclude] public string VanishReason { get; set; } = "";

    // Enemy-specific: whether interdiction has occurred.
    [JsonInclude] public bool HasInterdicted { get; set; }
    // Enemy-specific: whether the Communion station encounter has occurred.
    [JsonInclude] public bool CommunionEncounterAvailable { get; set; }
}
