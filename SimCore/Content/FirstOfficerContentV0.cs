using SimCore.Entities;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Content;

// GATE.T18.NARRATIVE.FO_CONTENT.001: First Officer dialogue content.
// Three candidates x 5 dialogue tiers. Each candidate has a personality,
// a blind spot, and an endgame lean. ~26 triggers x 3 candidates = 78 lines.
//
// Dialogue triggers fire when the player does something — not on a timer.
// The FO reacts to player ACTIONS, making the relationship feel earned.
//
// Key design rule: If the player chooses the endgame path the FO DOESN'T
// lean toward, the FO disagrees — not angrily, but with the weight of
// 20 hours of shared experience.
//
// GATE.T52.NARR.CONTENT_EXTRACT.001: All dialogue text externalized to
// SimCore/Content/Data/fo_dialogue_v0.json (embedded resource).
public static class FirstOfficerContentV0
{
    public sealed class CandidateProfile
    {
        public FirstOfficerCandidate Type { get; init; }
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public string BlindSpot { get; init; } = "";
        public string EndgameLean { get; init; } = "";  // Reinforce, Naturalize, Renegotiate
    }

    public sealed class DialogueLine
    {
        public string TriggerToken { get; init; } = "";
        public FirstOfficerCandidate CandidateType { get; init; }
        public DialogueTier MinTier { get; init; }
        public string Text { get; init; } = "";
        public int RelationshipDelta { get; init; }
    }

    public static readonly IReadOnlyList<CandidateProfile> Candidates;

    // All dialogue lines, keyed by trigger token and candidate type.
    // Lines fire once (logged in DialogueEventLog to prevent repeats).
    public static readonly IReadOnlyList<DialogueLine> AllLines;

    // ── JSON DTO types for deserialization ──

    private sealed class JsonRoot
    {
        [JsonPropertyName("version")] public int Version { get; set; }
        [JsonPropertyName("candidate_profiles")] public List<JsonCandidateProfile> CandidateProfiles { get; set; } = new();
        [JsonPropertyName("dialogue_lines")] public List<JsonDialogueLine> DialogueLines { get; set; } = new();
    }

    private sealed class JsonCandidateProfile
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("description")] public string Description { get; set; } = "";
        [JsonPropertyName("blind_spot")] public string BlindSpot { get; set; } = "";
        [JsonPropertyName("endgame_lean")] public string EndgameLean { get; set; } = "";
    }

    private sealed class JsonDialogueLine
    {
        [JsonPropertyName("trigger")] public string Trigger { get; set; } = "";
        [JsonPropertyName("candidate")] public string Candidate { get; set; } = "";
        [JsonPropertyName("tier")] public string Tier { get; set; } = "";
        [JsonPropertyName("text")] public string Text { get; set; } = "";
        [JsonPropertyName("relationship_delta")] public int RelationshipDelta { get; set; }
    }

    // ── Static initializer: load from embedded JSON ──

    static FirstOfficerContentV0()
    {
        var root = DialogueJsonLoader.Load<JsonRoot>("fo_dialogue_v0.json");

        var profiles = new List<CandidateProfile>();
        foreach (var j in root.CandidateProfiles)
        {
            profiles.Add(new CandidateProfile
            {
                Type = Enum.Parse<FirstOfficerCandidate>(j.Type),
                Name = j.Name,
                Description = j.Description,
                BlindSpot = j.BlindSpot,
                EndgameLean = j.EndgameLean,
            });
        }
        Candidates = profiles;

        var lines = new List<DialogueLine>();
        foreach (var j in root.DialogueLines)
        {
            lines.Add(new DialogueLine
            {
                TriggerToken = j.Trigger,
                CandidateType = Enum.Parse<FirstOfficerCandidate>(j.Candidate),
                MinTier = Enum.Parse<DialogueTier>(j.Tier),
                Text = j.Text,
                RelationshipDelta = j.RelationshipDelta,
            });
        }
        AllLines = lines;
    }

    /// <summary>
    /// Get the dialogue line for a specific trigger and candidate type.
    /// Returns null if no line exists for this combination.
    /// </summary>
    public static DialogueLine? GetLine(string triggerToken, FirstOfficerCandidate candidateType)
    {
        foreach (var line in AllLines)
        {
            if (line.CandidateType == candidateType &&
                string.Equals(line.TriggerToken, triggerToken, System.StringComparison.Ordinal))
            {
                return line;
            }
        }
        return null;
    }
}
