using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.T48.TELEMETRY.SESSION_WRITER.001: Economy health snapshot for dev telemetry.
public sealed class TelemetrySnapshot
{
    [JsonInclude] public int Tick { get; set; }
    [JsonInclude] public int ActiveNpcTradeRoutes { get; set; }
    [JsonInclude] public int AvgNpcIdleTicks { get; set; }
    [JsonInclude] public int GoodsVelocity { get; set; }
    [JsonInclude] public int PriceVarianceAvg { get; set; }
    [JsonInclude] public int StockoutCount { get; set; }
    [JsonInclude] public long CreditInflation { get; set; }
    [JsonInclude] public long PlayerCredits { get; set; }
    [JsonInclude] public int PlayerNodesVisited { get; set; }
}
