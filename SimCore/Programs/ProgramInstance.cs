using System.Text.Json.Serialization;

namespace SimCore.Programs;

/// <summary>
/// Serializable runtime program instance.
/// Determinism rule: the Id is assigned from SimState.NextProgramSeq and used for ordering.
/// </summary>
public sealed class ProgramInstance
{
    [JsonInclude] public string Id { get; set; } = "";
    [JsonInclude] public string Kind { get; set; } = "";
    [JsonInclude] public string FleetId { get; set; } = ""; // Optional: when set, program is scoped to a fleet.
    [JsonInclude] public ProgramStatus Status { get; set; } = ProgramStatus.Paused;

    [JsonInclude] public int CreatedTick { get; set; } = 0;

    // Scheduling
    [JsonInclude] public int CadenceTicks { get; set; } = 60;
    [JsonInclude] public int NextRunTick { get; set; } = 0;
    [JsonInclude] public int LastRunTick { get; set; } = -1;

    // Parameters (for AUTO_BUY)
    [JsonInclude] public string MarketId { get; set; } = "";
    [JsonInclude] public string GoodId { get; set; } = "";
    [JsonInclude] public int Quantity { get; set; } = 0;

    public bool IsRunnableAt(int tick)
    {
        if (Status != ProgramStatus.Running) return false;
        return tick >= NextRunTick;
    }
}
