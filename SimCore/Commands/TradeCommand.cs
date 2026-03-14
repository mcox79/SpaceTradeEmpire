using SimCore.Systems;

namespace SimCore.Commands;

public enum TradeType { Buy, Sell }

public class TradeCommand : ICommand
{
	public string PlayerId { get; set; }
	public string MarketNodeId { get; set; }
	public string GoodId { get; set; }
	public int Quantity { get; set; }
	public TradeType Type { get; set; }

	public TradeCommand(string playerId, string nodeId, string goodId, int qty, TradeType type)
	{
		PlayerId = playerId;
		MarketNodeId = nodeId;
		GoodId = goodId;
		Quantity = qty;
		Type = type;
	}

	public void Execute(SimState state)
	{
		// Delegate to BuyCommand/SellCommand which handle tariffs, fees,
		// instability pricing, reputation modifiers, cost basis, and
		// transaction ledger recording.
		if (Type == TradeType.Buy)
		{
			new BuyCommand(MarketNodeId, GoodId, Quantity).Execute(state);
		}
		else
		{
			new SellCommand(MarketNodeId, GoodId, Quantity).Execute(state);
		}
	}
}
