using System.Text.Json.Serialization;

namespace SimCore.Entities;

public enum MarketGoodViewKind
{
    LocalTruth = 0,
    RemoteIntel = 1
}

public enum InventoryBand
{
    Unknown = 0,
    VeryLow = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    VeryHigh = 5
}

public readonly struct MarketGoodView
{
    public MarketGoodViewKind Kind { get; init; }
    public int ExactInventoryQty { get; init; } // only meaningful for LocalTruth
    public InventoryBand InventoryBand { get; init; } // only meaningful for RemoteIntel
    public int AgeTicks { get; init; } // 0 if local, -1 if unknown
}

public sealed class IntelObservation
{
    [JsonInclude] public int ObservedTick { get; set; } = 0;
    [JsonInclude] public int ObservedInventoryQty { get; set; } = 0;
    // GATE.S10.TRADE_INTEL.MODEL.001: Price intel for trade route discovery.
    [JsonInclude] public int ObservedBuyPrice { get; set; } = 0;
    [JsonInclude] public int ObservedSellPrice { get; set; } = 0;
    [JsonInclude] public int ObservedMidPrice { get; set; } = 0;
}

// GATE.S11.GAME_FEEL.PRICE_HISTORY.001: Time-series price snapshot for trend display.
public sealed class PriceSnapshot
{
    [JsonInclude] public string NodeId { get; set; } = "";
    [JsonInclude] public string GoodId { get; set; } = "";
    [JsonInclude] public int BuyPrice { get; set; }
    [JsonInclude] public int SellPrice { get; set; }
    [JsonInclude] public long Tick { get; set; }
}

public sealed class IntelBook
{
    // Key format: marketId|goodId
    [JsonInclude] public Dictionary<string, IntelObservation> Observations { get; private set; } = new();

    // GATE.S3_6.DISCOVERY_STATE.001
    // DiscoveryStateV0 keyed by stable DiscoveryId; ordered by DiscoveryId asc for listing.
    [JsonInclude] public Dictionary<string, DiscoveryStateV0> Discoveries { get; private set; } = new();

    // GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.001
    // UnlockContractV0 keyed by stable UnlockId; ordered by UnlockId asc for listing.
    [JsonInclude] public Dictionary<string, UnlockContractV0> Unlocks { get; private set; } = new();

    // GATE.S3_6.RUMOR_INTEL_MIN.001
    // RumorLead keyed by stable LeadId (format: LEAD.<zero-padded-4-digit>); ordered by LeadId asc for listing.
    [JsonInclude] public Dictionary<string, RumorLead> RumorLeads { get; private set; } = new();

    // GATE.S10.TRADE_INTEL.ROUTE_ENTITY.001: Discovered trade routes keyed by RouteId (sourceNode|destNode|goodId).
    [JsonInclude] public Dictionary<string, TradeRouteIntel> TradeRoutes { get; private set; } = new();

    // GATE.S11.GAME_FEEL.PRICE_HISTORY.001: Time-series price history for trend charts.
    [JsonInclude] public List<PriceSnapshot> PriceHistory { get; private set; } = new();

    public static string Key(string marketId, string goodId) => marketId + "|" + goodId;
    public static string RouteKey(string sourceNodeId, string destNodeId, string goodId) => sourceNodeId + "|" + destNodeId + "|" + goodId;
}

// GATE.S10.TRADE_INTEL.ROUTE_ENTITY.001: Trade route status lifecycle.
public enum TradeRouteStatus
{
    Discovered = 0,
    Active = 1,
    Stale = 2,
    Unprofitable = 3
}

// GATE.S10.TRADE_INTEL.ROUTE_ENTITY.001: Discovered trade route with profitability estimate.
public sealed class TradeRouteIntel
{
    [JsonInclude] public string RouteId { get; set; } = "";
    [JsonInclude] public string SourceNodeId { get; set; } = "";
    [JsonInclude] public string DestNodeId { get; set; } = "";
    [JsonInclude] public string GoodId { get; set; } = "";
    [JsonInclude] public int EstimatedProfitPerUnit { get; set; } = 0;
    [JsonInclude] public int DiscoveredTick { get; set; } = 0;
    [JsonInclude] public int LastValidatedTick { get; set; } = 0;
    [JsonInclude] public TradeRouteStatus Status { get; set; } = TradeRouteStatus.Discovered;
    // GATE.S7.NARRATIVE_DELIVERY.ENTITY.001: Flavor text for narrative display.
    [JsonInclude] public string FlavorText { get; set; } = "";
}

// GATE.S3_6.DISCOVERY_STATE.001
public enum DiscoveryPhase
{
    Seen = 0,
    Scanned = 1,
    Analyzed = 2
}

// GATE.S3_6.DISCOVERY_STATE.001
// Reason codes for blocked scan/analyze outcomes.
public enum DiscoveryReasonCode
{
    Ok = 0,
    NotSeen = 1,
    AlreadyAnalyzed = 2,
    OffHub = 3,
    NotScanned = 4
}

// GATE.S3_6.DISCOVERY_STATE.001
public sealed class DiscoveryStateV0
{
    [JsonInclude] public string DiscoveryId { get; set; } = "";
    [JsonInclude] public DiscoveryPhase Phase { get; set; } = DiscoveryPhase.Seen;
    // GATE.S7.NARRATIVE_DELIVERY.ENTITY.001: Flavor text for narrative display.
    [JsonInclude] public string FlavorText { get; set; } = "";
}

// GATE.S3_6.RUMOR_INTEL_MIN.001
public enum RumorLeadStatus
{
    Active = 0,
    Fulfilled = 1,
    Dismissed = 2
}

// GATE.S3_6.RUMOR_INTEL_MIN.001
// HintPayload carries schema-bound tokens only (no free text).
// RegionTags: list of stable region tag tokens (Ordinal asc).
// CoarseLocationToken: single coarse location token (e.g. "OUTER_RIM").
// PrerequisiteTokens: list of prerequisite tokens required before lead can be pursued (Ordinal asc).
// ImpliedPayoffToken: single payoff category token (e.g. "BROKER_UNLOCK" or "RESOURCE_SITE").
public sealed class HintPayloadV0
{
    [JsonInclude] public List<string> RegionTags { get; set; } = new();
    [JsonInclude] public string CoarseLocationToken { get; set; } = "";
    [JsonInclude] public List<string> PrerequisiteTokens { get; set; } = new();
    [JsonInclude] public string ImpliedPayoffToken { get; set; } = "";
}

// GATE.S3_6.RUMOR_INTEL_MIN.001
// RumorLead: schema-bound lead record.
// LeadId format: LEAD.<zero-padded-4-digit> (e.g. LEAD.0001).
// SourceVerbToken: schema-bound token for acquisition verb (e.g. "SCAN", "HUB_ANALYSIS", "EXPEDITION").
public sealed class RumorLead
{
    [JsonInclude] public string LeadId { get; set; } = "";
    [JsonInclude] public HintPayloadV0 Hint { get; set; } = new();
    [JsonInclude] public RumorLeadStatus Status { get; set; } = RumorLeadStatus.Active;
    [JsonInclude] public string SourceVerbToken { get; set; } = "";
}

// GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.001
public enum UnlockKind
{
    Permit = 0,
    Broker = 1,
    Recipe = 2,
    SiteBlueprint = 3,
    CorridorAccess = 4,
    SensorLayer = 5
}

// GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.001
// Reason codes for blocked acquisition outcomes.
public enum UnlockReasonCode
{
    Ok = 0,
    NotKnown = 1,
    AlreadyAcquired = 2,
    Blocked = 3
}

// GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.001
public sealed class UnlockContractV0
{
    [JsonInclude] public string UnlockId { get; set; } = "";
    [JsonInclude] public UnlockKind Kind { get; set; } = UnlockKind.Permit;

    // IntelBook-facing acquisition state.
    [JsonInclude] public bool IsAcquired { get; set; } = false;

    // Deterministic blocked flag. If true and not acquired, acquisition reason is UnlockReasonCode.Blocked.
    [JsonInclude] public bool IsBlocked { get; set; } = false;
}
