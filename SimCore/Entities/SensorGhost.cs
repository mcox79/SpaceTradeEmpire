using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.T45.DEEP_DREAD.SENSOR_GHOSTS.001: Phantom fleet contact.
// Generated deterministically at Phase 2+ nodes. Appears on sensors
// as a real fleet, then vanishes after a few ticks. Epistemic horror.
public class SensorGhost
{
    [JsonInclude] public string Id { get; set; } = "";
    [JsonInclude] public string NodeId { get; set; } = "";
    [JsonInclude] public string ApparentFleetType { get; set; } = ""; // "trader", "patrol", "unknown"
    [JsonInclude] public int SpawnTick { get; set; }
    [JsonInclude] public int ExpiryTick { get; set; }
}
