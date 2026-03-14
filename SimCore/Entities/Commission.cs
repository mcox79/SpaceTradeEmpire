using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.S7.FACTION_COMMISSION.ENTITY.001: Player's active faction commission.
// Starsector-model: passive rep gain with employer, passive rep loss with rivals, stipend payment.
public sealed class Commission
{
    [JsonInclude] public string FactionId { get; set; } = "";
    [JsonInclude] public int StartTick { get; set; }
    [JsonInclude] public int StipendCreditsPerCycle { get; set; }
}
