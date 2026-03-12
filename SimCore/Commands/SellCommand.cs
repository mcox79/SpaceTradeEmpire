using SimCore.Content;
using SimCore.Systems;
using System;

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

		// GATE.S7.FACTION.TARIFF_ENFORCE.001: Check reputation-based access.
		if (!MarketSystem.CanTradeByReputation(state, MarketId)) return;

		if (InventoryLedger.Get(state.PlayerCargo, GoodId) < Quantity) return;

		// GATE.X.INSTAB_PRICE.WIRE.001: Block trade if market closed by instability; adjust price.
		int instMultBps = MarketSystem.GetInstabilityPriceMultiplierBps(state, MarketId, GoodId);
		if (instMultBps <= 0) return;

		int unitPrice = market.GetSellPrice(GoodId);
		if (instMultBps != 10000)
			unitPrice = (int)Math.Max(1, (long)unitPrice * instMultBps / 10000);

		int totalValue = unitPrice * Quantity;

		// GATE.S7.FACTION.TARIFF_ENFORCE.001: Apply tariff deduction (decreases sell revenue).
		int tariffBps = MarketSystem.GetEffectiveTariffBps(state, MarketId);
		totalValue -= MarketSystem.ComputeTariffCredits(totalValue, tariffBps);
		if (totalValue < 0) totalValue = 0;

		if (!InventoryLedger.TryRemoveCargo(state.PlayerCargo, GoodId, Quantity)) return;

		InventoryLedger.AddMarket(market.Inventory, GoodId, Quantity);
		state.PlayerCredits += totalValue;
		// GATE.S12.PROGRESSION.STATS.001: Track goods traded + credits earned.
		if (state.PlayerStats != null)
		{
			state.PlayerStats.GoodsTraded += Quantity;
			state.PlayerStats.TotalCreditsEarned += totalValue;
		}

		// GATE.T18.CHARACTER.FO_REACT.001: Fire FO trade triggers.
		if (totalValue > 0)
			FirstOfficerSystem.TryFireTrigger(state, "FIRST_PROFITABLE_TRADE");
		if (string.Equals(GoodId, WellKnownGoodIds.Munitions, StringComparison.Ordinal)
			|| string.Equals(GoodId, WellKnownGoodIds.Composites, StringComparison.Ordinal))
			FirstOfficerSystem.TryFireTrigger(state, "FIRST_WAR_GOODS_SALE");
	}
}
