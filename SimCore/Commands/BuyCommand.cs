using SimCore.Systems;

namespace SimCore.Commands;

public class BuyCommand : ICommand
{
	public string MarketId { get; set; }
	public string GoodId { get; set; }
	public int Quantity { get; set; }

	public BuyCommand(string marketId, string goodId, int quantity)
	{
		MarketId = marketId;
		GoodId = goodId;
		Quantity = quantity;
	}

	public void Execute(SimState state)
	{
		if (Quantity <= 0) return;
		if (!state.Markets.TryGetValue(MarketId, out var market)) return;

		// GATE.S7.FACTION.TARIFF_ENFORCE.001: Check reputation-based access.
		if (!MarketSystem.CanTradeByReputation(state, MarketId)) return;

		var available = InventoryLedger.Get(market.Inventory, GoodId);
		if (available < Quantity) return;

		int unitPrice = market.GetBuyPrice(GoodId);
		int totalCost = unitPrice * Quantity;

		// GATE.S7.FACTION.TARIFF_ENFORCE.001: Apply tariff surcharge (increases buy cost).
		int tariffBps = MarketSystem.GetEffectiveTariffBps(state, MarketId);
		totalCost += MarketSystem.ComputeTariffCredits(totalCost, tariffBps);

		if (state.PlayerCredits < totalCost) return;

		if (!InventoryLedger.TryRemoveMarket(market.Inventory, GoodId, Quantity)) return;

		state.PlayerCredits -= totalCost;
		InventoryLedger.AddCargo(state.PlayerCargo, GoodId, Quantity);
	}
}
