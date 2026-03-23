using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.T51.TELEMETRY.LOCAL_STORE.001: Per-event telemetry for session analytics.
// Events are logged to TelemetryEvents list in SimState, written to disk by bridge at session end.
public sealed class TelemetryEvent
{
    [JsonInclude] public int Tick { get; set; }
    [JsonInclude] public string EventType { get; set; } = ""; // trade, combat, death, dock, mission
    [JsonInclude] public string NodeId { get; set; } = "";
    [JsonInclude] public string Detail { get; set; } = ""; // good_id, enemy_type, mission_id, etc.
    [JsonInclude] public long Credits { get; set; } // player credits at time of event
}
