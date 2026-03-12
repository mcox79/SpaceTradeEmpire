using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.T18.NARRATIVE.FO_MODEL.001: First Officer candidate types.
public enum FirstOfficerCandidate
{
    None = 0,
    Analyst = 1,
    Veteran = 2,
    Pathfinder = 3
}

// GATE.T18.NARRATIVE.FO_MODEL.001: Dialogue tier progression (tier-gated triggers).
public enum DialogueTier
{
    Early = 0,       // tick 0-300
    Mid = 1,         // tick 300-600
    Fracture = 2,    // tick 600-1000
    Revelation = 3,  // tick 1000-1500
    Endgame = 4      // tick 1500+
}

// GATE.T18.NARRATIVE.FO_MODEL.001: Fired dialogue event record.
public sealed class DialogueEvent
{
    [JsonInclude] public string TriggerToken { get; set; } = "";
    [JsonInclude] public int FiredTick { get; set; }
}

// GATE.T18.NARRATIVE.FO_MODEL.001: First Officer companion state.
// Three candidates (Analyst, Veteran, Pathfinder). Player promotes one at tick 50-150.
// Action-triggered dialogue with tier gates. Each candidate has a blind spot
// that gets exposed through play.
public sealed class FirstOfficer
{
    [JsonInclude] public FirstOfficerCandidate CandidateType { get; set; } = FirstOfficerCandidate.None;
    [JsonInclude] public bool IsPromoted { get; set; }
    [JsonInclude] public int PromotionTick { get; set; }
    [JsonInclude] public DialogueTier Tier { get; set; } = DialogueTier.Early;

    // Relationship score: accumulated through shared experiences.
    [JsonInclude] public int RelationshipScore { get; set; }

    // Whether the FO's blind spot has been exposed by game events.
    [JsonInclude] public bool BlindSpotExposed { get; set; }

    // Dialogue event log: which triggers have fired (prevents repeats).
    [JsonInclude] public List<DialogueEvent> DialogueEventLog { get; private set; } = new();

    // Current pending dialogue line (set by system, consumed by bridge). Transient.
    [JsonIgnore] public string PendingDialogueLine { get; set; } = "";
    [JsonIgnore] public string PendingTriggerToken { get; set; } = "";
}
