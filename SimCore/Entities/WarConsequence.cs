using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.T18.NARRATIVE.WAR_CONSEQUENCE.001: War consequence kinds.
public enum WarConsequenceKind
{
    SupplyDelivered = 0,
    CounteroffensiveDamage = 1,
    CivilianCasualties = 2
}

// GATE.T18.NARRATIVE.WAR_CONSEQUENCE.001: Delayed war feedback entity.
// Created when player delivers war goods to warfront nodes.
// After delay ticks, resolves with a toast showing downstream effect.
public sealed class WarConsequence
{
    [JsonInclude] public string Id { get; set; } = "";
    [JsonInclude] public WarConsequenceKind Kind { get; set; } = WarConsequenceKind.SupplyDelivered;
    [JsonInclude] public string SourceNodeId { get; set; } = "";
    [JsonInclude] public string TargetNodeId { get; set; } = "";
    [JsonInclude] public string GoodId { get; set; } = "";
    [JsonInclude] public int Quantity { get; set; }
    [JsonInclude] public int CreatedTick { get; set; }
    [JsonInclude] public int DelayTicks { get; set; }

    // Immediate manifest text: "Warfront garrison strength: restored"
    [JsonInclude] public string ManifestText { get; set; } = "";

    // Delayed consequence text: "Settlement destroyed by counteroffensive using delivered munitions"
    [JsonInclude] public string ConsequenceText { get; set; } = "";

    [JsonInclude] public bool IsResolved { get; set; }
    [JsonInclude] public int ResolvedTick { get; set; }
}
