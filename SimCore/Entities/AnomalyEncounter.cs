using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.S6.ANOMALY.ENCOUNTER_MODEL.001: Anomaly encounter lifecycle.
public enum AnomalyEncounterStatus
{
    Pending = 0,
    Completed = 1
}

// GATE.S6.ANOMALY.ENCOUNTER_MODEL.001: Active anomaly encounter from scanned anomaly discovery.
public sealed class AnomalyEncounter
{
    [JsonInclude] public string EncounterId { get; set; } = "";
    [JsonInclude] public string NodeId { get; set; } = "";
    [JsonInclude] public string DiscoveryId { get; set; } = "";
    [JsonInclude] public string Family { get; set; } = ""; // DERELICT, RUIN, SIGNAL
    [JsonInclude] public int Difficulty { get; set; } = 1;
    [JsonInclude] public AnomalyEncounterStatus Status { get; set; } = AnomalyEncounterStatus.Pending;
    [JsonInclude] public int CreatedTick { get; set; } = 0;

    // GATE.S6.ANOMALY.REWARD_LOOT.001: Loot generated on completion.
    [JsonInclude] public Dictionary<string, int> LootItems { get; set; } = new();
    [JsonInclude] public int CreditReward { get; set; } = 0;
    [JsonInclude] public string DiscoveryLeadNodeId { get; set; } = ""; // SIGNAL: leads to new discovery
}
