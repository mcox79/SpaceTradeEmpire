using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.T57.PIPELINE.ECONOMIC_INTEL.001: Typed economic intelligence from discoveries.
// Each analyzed discovery produces an EconomicIntel describing its trade implications.
// Types map to discovery families per ExplorationDiscovery.md canonical spec.
public enum EconomicIntelType
{
    ResourceDeposit = 0,   // RESOURCE_POOL_MARKER: local resource with trade potential
    CargoManifest = 1,     // CORRIDOR_TRACE: corridor shortcut trade opportunity
    MarketAnomaly = 2,     // Generic: unusual price/supply pattern detected
    ChainIntel = 3,        // Anomaly chain step: multi-discovery trade insight
    MarketRuin = 4         // RUIN family: salvage/exotic matter economic impact
}

// GATE.T57.PIPELINE.ECONOMIC_INTEL.001: Economic intelligence produced per analyzed discovery.
// Keyed by IntelId in IntelBook.EconomicIntels.
public sealed class EconomicIntel
{
    [JsonInclude] public string IntelId { get; set; } = "";
    [JsonInclude] public EconomicIntelType Type { get; set; } = EconomicIntelType.MarketAnomaly;
    [JsonInclude] public string SourceDiscoveryId { get; set; } = "";
    [JsonInclude] public string NodeId { get; set; } = "";
    [JsonInclude] public string GoodId { get; set; } = "";
    [JsonInclude] public int EstimatedValue { get; set; }
    [JsonInclude] public int CreatedTick { get; set; }
    // Freshness decay: distance-band based. Near=50t, Mid=150t, Deep=400t, Fracture=never.
    // When FreshnessRemainingTicks reaches 0, intel is Stale.
    [JsonInclude] public int FreshnessMaxTicks { get; set; }
    [JsonInclude] public int DistanceBand { get; set; } // 0=Near, 1=Mid, 2=Deep, 3=Fracture
    [JsonInclude] public string FlavorText { get; set; } = "";
    // GATE.T57.CHAIN.CHAIN_INTEL.001: FO personality commentary (populated for ChainIntel type).
    [JsonInclude] public string FoCommentary { get; set; } = "";
}
