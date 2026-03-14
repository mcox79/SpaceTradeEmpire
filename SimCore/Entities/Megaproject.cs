using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.S8.MEGAPROJECT.ENTITY.001: Megaproject — multi-stage construction that reshapes map rules.
public class Megaproject
{
    [JsonInclude] public string Id { get; set; } = "";
    [JsonInclude] public string TypeId { get; set; } = "";       // e.g. "fracture_anchor"
    [JsonInclude] public string NodeId { get; set; } = "";       // Construction site node
    [JsonInclude] public int Stage { get; set; } = 0;            // Current stage (0-based)
    [JsonInclude] public int MaxStages { get; set; } = 3;        // Total stages
    [JsonInclude] public int ProgressTicks { get; set; } = 0;    // Ticks spent in current stage
    [JsonInclude] public int CompletedTick { get; set; } = -1;   // Tick when completed (-1 = in progress)
    [JsonInclude] public string OwnerId { get; set; } = "";      // Player fleet ID that initiated

    // Per-stage supply tracking: goodId → quantity delivered for current stage.
    [JsonInclude] public Dictionary<string, int> SupplyDelivered { get; set; } = new();

    // Whether map rule mutation has been applied (only once on completion).
    [JsonInclude] public bool MutationApplied { get; set; } = false;

    public bool IsComplete => CompletedTick >= 0;
}
