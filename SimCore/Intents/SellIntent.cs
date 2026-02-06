using SimCore.Commands;

namespace SimCore.Intents;

public sealed class SellIntent : IIntent
{
	public string Kind => "SELL";

	public string MarketId { get; }
	public string GoodId { get; }
	public int Quantity { get; }

	public SellIntent(string marketId, string goodId, int quantity)
	{
		MarketId = marketId ?? "";
		GoodId = goodId ?? "";
		Quantity = quantity;
	}

	public void Apply(SimState state)
	{
		var cmd = new SellCommand(MarketId, GoodId, Quantity);
		cmd.Execute(state);
	}
}
