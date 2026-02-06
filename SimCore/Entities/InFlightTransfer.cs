using System.Text.Json.Serialization;

namespace SimCore.Entities;

/// <summary>
/// Deterministic, tick-scheduled transfer of goods across a lane.
/// Source inventory is debited at enqueue-time; destination is credited on ArriveTick.
/// </summary>
public sealed class InFlightTransfer
{
	[JsonInclude] public string Id { get; set; } = "";

	[JsonInclude] public string EdgeId { get; set; } = "";

	[JsonInclude] public string FromNodeId { get; set; } = "";
	[JsonInclude] public string ToNodeId { get; set; } = "";

	[JsonInclude] public string FromMarketId { get; set; } = "";
	[JsonInclude] public string ToMarketId { get; set; } = "";

	[JsonInclude] public string GoodId { get; set; } = "";
	[JsonInclude] public int Quantity { get; set; } = 0;

	[JsonInclude] public int DepartTick { get; set; } = 0;
	[JsonInclude] public int ArriveTick { get; set; } = 0;
}
