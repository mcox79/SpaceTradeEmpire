using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.S9.SYSTEMIC.TRIGGER_ENGINE.001: Trigger type for procedural mission generation.
public enum SystemicTriggerType
{
    WarDemand = 0,      // Warfront goods shortage at contested nodes
    PriceSpike = 1,     // Good price > threshold × base price
    SupplyShortage = 2, // Production deficit near instability
}

// GATE.S9.SYSTEMIC.TRIGGER_ENGINE.001: A procedural mission offer generated from world-state.
public sealed class SystemicMissionOffer
{
    [JsonInclude] public string OfferId { get; set; } = "";
    [JsonInclude] public SystemicTriggerType TriggerType { get; set; }
    [JsonInclude] public string NodeId { get; set; } = "";
    [JsonInclude] public string GoodId { get; set; } = "";
    [JsonInclude] public int CreatedTick { get; set; }
    [JsonInclude] public int ExpiryTick { get; set; }
}
