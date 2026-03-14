using SimCore.Content;
using SimCore.Systems;
using SimCore.Tweaks;
using System;
using System.Linq;

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

		// GATE.X.SHIP_CLASS.CARGO_ENFORCE.001: Reject buy if it would exceed cargo capacity.
		var playerFleet = state.Fleets.Values.FirstOrDefault(f =>
			string.Equals(f.OwnerId, "player", StringComparison.Ordinal));
		if (playerFleet != null)
		{
			var classDef = ShipClassContentV0.GetById(playerFleet.ShipClassId);
			if (classDef != null && classDef.CargoCapacity > 0)
			{
				int currentCargo = state.PlayerCargo.Values.Sum();
				if (currentCargo + Quantity > classDef.CargoCapacity)
					return;
			}
		}

		// GATE.X.INSTAB_PRICE.WIRE.001: Block trade if market closed by instability; adjust price.
		int instMultBps = MarketSystem.GetInstabilityPriceMultiplierBps(state, MarketId, GoodId);
		if (instMultBps <= 0)
		{
			// GATE.X.PRESSURE_INJECT.MARKET.001: Market closed by instability — inject pressure.
			PressureSystem.InjectDelta(state, "trade_disruption", "market_blocked",
				PressureTweaksV0.MarketBlockedMagnitude, targetRef: MarketId);
			return;
		}

		int unitPrice = market.GetBuyPrice(GoodId);

		// GATE.X.MARKET_PRICING.REP_WIRE.001: Apply reputation-based price modifier.
		var factionId = MarketSystem.GetControllingFactionIdForMarket(state, MarketId);
		int repBps = MarketSystem.GetRepPricingBps(state, factionId);
		unitPrice = MarketSystem.ApplyRepPricing(unitPrice, repBps);

		if (instMultBps != 10000)
			unitPrice = (int)Math.Max(1, (long)unitPrice * instMultBps / 10000);
		int totalCost = unitPrice * Quantity;

		// GATE.S7.FACTION.TARIFF_ENFORCE.001: Apply tariff surcharge (increases buy cost).
		int tariffBps = MarketSystem.GetEffectiveTariffBps(state, MarketId);
		totalCost += MarketSystem.ComputeTariffCredits(totalCost, tariffBps);

		// GATE.X.MARKET_PRICING.FEE_WIRE.001: Deduct transaction fee.
		totalCost += MarketSystem.ComputeTransactionFeeCredits(state, totalCost);

		if (state.PlayerCredits < totalCost) return;

		if (!InventoryLedger.TryRemoveMarket(market.Inventory, GoodId, Quantity)) return;

		state.PlayerCredits -= totalCost;
		InventoryLedger.AddCargo(state.PlayerCargo, GoodId, Quantity);

		// GATE.X.LEDGER.COST_BASIS.001: Update weighted average cost basis.
		{
			int totalQty = InventoryLedger.Get(state.PlayerCargo, GoodId); // qty after this buy
			int prevQty = totalQty - Quantity;
			if (prevQty < 0) prevQty = 0;
			state.PlayerCargoCostBasis.TryGetValue(GoodId, out int prevAvg);
			long totalBasis = (long)prevQty * prevAvg + (long)Quantity * unitPrice;
			state.PlayerCargoCostBasis[GoodId] = totalQty > 0 ? (int)(totalBasis / totalQty) : 0;
		}

		// GATE.X.LEDGER.TX_MODEL.001: Record buy transaction for audit trail.
		state.AppendTransaction(new TransactionRecord
		{
			CashDelta = -totalCost,
			GoodId = GoodId,
			Quantity = Quantity,
			Source = "Buy",
			NodeId = MarketId,
		});
	}
}
