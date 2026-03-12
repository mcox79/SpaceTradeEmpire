using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.S16.NPC_ALIVE.FLEET_RESPAWN.001
public sealed class NpcRespawnEntry
{
    [JsonInclude] public string FleetId { get; set; } = "";
    [JsonInclude] public string HomeNodeId { get; set; } = "";
    [JsonInclude] public int DestructionTick { get; set; }
    [JsonInclude] public FleetRole Role { get; set; } = FleetRole.Trader;
    [JsonInclude] public string OwnerId { get; set; } = "ai";
}
