using SimCore.Commands;

namespace SimCore.Intents;

public sealed class BuyIntent : IIntent
{
	public string Kind => "BUY";

	public string MarketId { get; }
	public string GoodId { get; }
	public int Quantity { get; }

	public BuyIntent(string marketId, string goodId, int quantity)
	{
		MarketId = marketId ?? "";
		GoodId = goodId ?? "";
		Quantity = quantity;
	}

	public void Apply(SimState state)
	{
		var cmd = new BuyCommand(MarketId, GoodId, Quantity);
		cmd.Execute(state);
	}
}
