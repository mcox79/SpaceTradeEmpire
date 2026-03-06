using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.S15.FEEL.JUMP_EVENT_SYS.001: Random events during lane transit.
public enum JumpEventKind
{
    None = 0,
    Salvage = 1,    // Found cargo floating in lane
    Signal = 2,     // Detected anomaly signal (discovery lead)
    Turbulence = 3  // Hull damage from lane instability
}

// GATE.S15.FEEL.JUMP_EVENT_SYS.001
public sealed class JumpEvent
{
    [JsonInclude] public string EventId { get; set; } = "";
    [JsonInclude] public JumpEventKind Kind { get; set; } = JumpEventKind.None;
    [JsonInclude] public string FleetId { get; set; } = "";
    [JsonInclude] public string EdgeId { get; set; } = "";
    [JsonInclude] public string NodeId { get; set; } = ""; // Arrival node
    [JsonInclude] public int Tick { get; set; } = 0;
    [JsonInclude] public string GoodId { get; set; } = "";  // Salvage: what was found
    [JsonInclude] public int Quantity { get; set; } = 0;     // Salvage: how much
    [JsonInclude] public int HullDamage { get; set; } = 0;   // Turbulence: damage dealt
}
