using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.T41.ANOMALY_CHAIN.MODEL.001: Multi-site anomaly chain state.

public enum AnomalyChainStatus
{
    Active = 0,
    Completed = 1,
    Failed = 2
}

// A single step in an anomaly chain template.
public sealed class AnomalyChainStep
{
    [JsonInclude] public int StepIndex { get; set; }
    [JsonInclude] public string DiscoveryKind { get; set; } = "";
    [JsonInclude] public int MinHopsFromStarter { get; set; }
    [JsonInclude] public int MaxHopsFromStarter { get; set; }
    [JsonInclude] public string NarrativeText { get; set; } = "";
    [JsonInclude] public string LeadText { get; set; } = "";
    [JsonInclude] public Dictionary<string, int> LootOverrides { get; set; } = new();
    [JsonInclude] public string PlacedDiscoveryId { get; set; } = "";
    [JsonInclude] public bool IsCompleted { get; set; }
}

// Runtime anomaly chain instance (persisted in SimState.AnomalyChains).
public sealed class AnomalyChain
{
    [JsonInclude] public string ChainId { get; set; } = "";
    [JsonInclude] public List<AnomalyChainStep> Steps { get; set; } = new();
    [JsonInclude] public int CurrentStepIndex { get; set; }
    [JsonInclude] public AnomalyChainStatus Status { get; set; } = AnomalyChainStatus.Active;
    [JsonInclude] public int StartedTick { get; set; }
    [JsonInclude] public string StarterNodeId { get; set; } = "";
}
