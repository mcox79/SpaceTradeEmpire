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
		if (Quantity <= 0) return;
		if (!state.Markets.TryGetValue(MarketNodeId, out var market)) return;

		int pricePerUnit = (Type == TradeType.Buy)
			? market.GetBuyPrice(GoodId)
			: market.GetSellPrice(GoodId);
	
		int totalCost = pricePerUnit * Quantity;


		if (Type == TradeType.Buy)
		{
			if (state.PlayerCredits < totalCost) return;
			if (InventoryLedger.Get(market.Inventory, GoodId) < Quantity) return;

			if (!InventoryLedger.TryRemoveMarket(market.Inventory, GoodId, Quantity)) return;

			state.PlayerCredits -= totalCost;
			InventoryLedger.AddCargo(state.PlayerCargo, GoodId, Quantity);
		}
		else
		{
			if (InventoryLedger.Get(state.PlayerCargo, GoodId) < Quantity) return;

			if (!InventoryLedger.TryRemoveCargo(state.PlayerCargo, GoodId, Quantity)) return;

			InventoryLedger.AddMarket(market.Inventory, GoodId, Quantity);
			state.PlayerCredits += totalCost;
		}
	}
}
