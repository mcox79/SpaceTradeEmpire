using SimCore.Systems;

namespace SimCore.Commands;

public class SellCommand : ICommand
{
	public string MarketId { get; set; }
	public string GoodId { get; set; }
	public int Quantity { get; set; }

	public SellCommand(string marketId, string goodId, int quantity)
	{
		MarketId = marketId;
		GoodId = goodId;
		Quantity = quantity;
	}

	public void Execute(SimState state)
	{
		if (Quantity <= 0) return;
		if (!state.Markets.TryGetValue(MarketId, out var market)) return;

		if (InventoryLedger.Get(state.PlayerCargo, GoodId) < Quantity) return;

		int unitPrice = market.GetSellPrice(GoodId);

		int totalValue = unitPrice * Quantity;

		if (!InventoryLedger.TryRemoveCargo(state.PlayerCargo, GoodId, Quantity)) return;

		InventoryLedger.AddMarket(market.Inventory, GoodId, Quantity);
		state.PlayerCredits += totalValue;
		// GATE.S12.PROGRESSION.STATS.001: Track goods traded + credits earned.
		if (state.PlayerStats != null)
		{
			state.PlayerStats.GoodsTraded += Quantity;
			state.PlayerStats.TotalCreditsEarned += totalValue;
		}
	}
}
