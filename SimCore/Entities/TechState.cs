using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.S4.TECH.CORE.001: Persisted tech/research state.
public sealed class TechState
{
    [JsonInclude] public HashSet<string> UnlockedTechIds { get; set; } = new(System.StringComparer.Ordinal);
    [JsonInclude] public string CurrentResearchTechId { get; set; } = "";
    [JsonInclude] public int ResearchProgressTicks { get; set; } = 0;
    [JsonInclude] public int ResearchTotalTicks { get; set; } = 0;
    [JsonInclude] public long ResearchCreditsSpent { get; set; } = 0;
    [JsonInclude] public List<TechEvent> EventLog { get; set; } = new();
    [JsonInclude] public long NextEventSeq { get; set; } = 1;

    [JsonIgnore]
    public bool IsResearching => !string.IsNullOrEmpty(CurrentResearchTechId);
}

// GATE.S4.TECH.CORE.001: Tech event for deterministic event log.
public sealed class TechEvent
{
    [JsonInclude] public long Seq { get; set; }
    [JsonInclude] public int Tick { get; set; }
    [JsonInclude] public string TechId { get; set; } = "";
    [JsonInclude] public string EventType { get; set; } = ""; // Started, Completed, Cancelled
}
