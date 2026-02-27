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
}

public sealed class IntelBook
{
    // Key format: marketId|goodId
    [JsonInclude] public Dictionary<string, IntelObservation> Observations { get; private set; } = new();

    // GATE.S3_6.DISCOVERY_STATE.001
    // DiscoveryStateV0 keyed by stable DiscoveryId; ordered by DiscoveryId asc for listing.
    [JsonInclude] public Dictionary<string, DiscoveryStateV0> Discoveries { get; private set; } = new();

    public static string Key(string marketId, string goodId) => marketId + "|" + goodId;
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
}
