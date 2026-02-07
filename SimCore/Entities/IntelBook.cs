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

	public static string Key(string marketId, string goodId) => marketId + "|" + goodId;
}
