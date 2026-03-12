using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.T18.NARRATIVE.STATION_MEMORY.001: Per-station per-good delivery tracking.
// Tracks the player's trading relationship with each station.
// Key format in SimState: "nodeId|goodId"
public sealed class StationDeliveryRecord
{
    [JsonInclude] public string NodeId { get; set; } = "";
    [JsonInclude] public string GoodId { get; set; } = "";
    [JsonInclude] public int TotalDeliveries { get; set; }
    [JsonInclude] public int TotalQuantity { get; set; }
    [JsonInclude] public int FirstDeliveryTick { get; set; }
    [JsonInclude] public int LastDeliveryTick { get; set; }

    public static string Key(string nodeId, string goodId) => nodeId + "|" + goodId;
}
