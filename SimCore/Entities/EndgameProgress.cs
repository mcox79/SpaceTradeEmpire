using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.S8.WIN.PROGRESS_TRACK.001: Per-tick endgame progress snapshot for UI display.
public class EndgameProgress
{
    // Overall completion percentage (0-100).
    [JsonInclude] public int CompletionPercent { get; set; } = 0;

    // Per-requirement progress items.
    [JsonInclude] public bool HavenTierMet { get; set; }
    [JsonInclude] public int HavenTierCurrent { get; set; }
    [JsonInclude] public int HavenTierRequired { get; set; }

    [JsonInclude] public bool FactionRep1Met { get; set; }
    [JsonInclude] public string FactionRep1Id { get; set; } = "";
    [JsonInclude] public int FactionRep1Current { get; set; }
    [JsonInclude] public int FactionRep1Required { get; set; }

    [JsonInclude] public bool FactionRep2Met { get; set; }
    [JsonInclude] public string FactionRep2Id { get; set; } = "";
    [JsonInclude] public int FactionRep2Current { get; set; }
    [JsonInclude] public int FactionRep2Required { get; set; }

    [JsonInclude] public bool Fragment1Met { get; set; }
    [JsonInclude] public string Fragment1Id { get; set; } = "";

    [JsonInclude] public bool Fragment2Met { get; set; }
    [JsonInclude] public string Fragment2Id { get; set; } = "";

    // Renegotiate-specific: revelations.
    [JsonInclude] public int RevelationsCurrent { get; set; }
    [JsonInclude] public int RevelationsRequired { get; set; }
    [JsonInclude] public bool RevelationsMet { get; set; }
}
