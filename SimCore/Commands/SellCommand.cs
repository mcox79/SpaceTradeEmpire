using SimCore.Content;
using SimCore.Systems;
using SimCore.Tweaks;
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
		if (Quantity <= 0) { System.Console.Error.WriteLine($"DEBUG_SELL_CMD|REJECT|qty<=0 market={MarketId} good={GoodId} qty={Quantity}"); return; }
		if (!state.Markets.TryGetValue(MarketId, out var market)) { System.Console.Error.WriteLine($"DEBUG_SELL_CMD|REJECT|market_not_found market={MarketId} good={GoodId}"); return; }

		// GATE.S7.FACTION.TARIFF_ENFORCE.001: Check reputation-based access.
		if (!MarketSystem.CanTradeByReputation(state, MarketId)) { System.Console.Error.WriteLine($"DEBUG_SELL_CMD|REJECT|reputation_blocked market={MarketId} good={GoodId}"); return; }

		if (InventoryLedger.Get(state.PlayerCargo, GoodId) < Quantity) { System.Console.Error.WriteLine($"DEBUG_SELL_CMD|REJECT|insufficient_cargo market={MarketId} good={GoodId} qty={Quantity} have={InventoryLedger.Get(state.PlayerCargo, GoodId)}"); return; }

		// GATE.X.INSTAB_PRICE.WIRE.001: Block trade if market closed by instability; adjust price.
		int instMultBps = MarketSystem.GetInstabilityPriceMultiplierBps(state, MarketId, GoodId);
		if (instMultBps <= 0)
		{
			System.Console.Error.WriteLine($"DEBUG_SELL_CMD|REJECT|market_closed_instability market={MarketId} good={GoodId}");
			// GATE.X.PRESSURE_INJECT.MARKET.001: Market closed by instability — inject pressure.
			PressureSystem.InjectDelta(state, "trade_disruption", "market_blocked",
				PressureTweaksV0.MarketBlockedMagnitude, targetRef: MarketId);
			return;
		}

		int unitPrice = market.GetSellPrice(GoodId);

		// GATE.T52.ECON.TRADE_DIVERSITY.001: Apply recent-trade margin dampening (sell price DOWN).
		int dampenBps = MarketSystem.GetRecentTradeDampenBps(state, MarketId, GoodId);
		unitPrice = MarketSystem.ApplyRecentTradeDampening(unitPrice, dampenBps, isBuy: false);

		// GATE.X.MARKET_PRICING.REP_WIRE.001: Apply reputation-based price modifier.
		var factionId = MarketSystem.GetControllingFactionIdForMarket(state, MarketId);
		int repBps = MarketSystem.GetRepPricingBps(state, factionId);
		unitPrice = MarketSystem.ApplyRepPricing(unitPrice, repBps);

		// GATE.X.MARKET_PRICING.AFFINITY_WIRE.001: Apply faction good affinity modifier.
		int affinityBps = MarketSystem.GetFactionGoodAffinityBps(state, factionId, GoodId);
		unitPrice = MarketSystem.ApplyFactionGoodAffinityPricing(unitPrice, affinityBps);

		if (instMultBps != 10000)
			unitPrice = (int)Math.Max(1, (long)unitPrice * instMultBps / 10000);

		int totalValue = unitPrice * Quantity;

		// GATE.S7.FACTION.TARIFF_ENFORCE.001: Apply tariff deduction (decreases sell revenue).
		int tariffBps = MarketSystem.GetEffectiveTariffBps(state, MarketId);
		totalValue -= MarketSystem.ComputeTariffCredits(totalValue, tariffBps);
		if (totalValue < 0) totalValue = 0;

		// GATE.X.MARKET_PRICING.FEE_WIRE.001: Deduct transaction fee from sell revenue.
		totalValue -= MarketSystem.ComputeTransactionFeeCredits(state, totalValue);
		if (totalValue < 0) totalValue = 0;

		if (!InventoryLedger.TryRemoveCargo(state.PlayerCargo, GoodId, Quantity)) return;

		InventoryLedger.AddMarket(market.Inventory, GoodId, Quantity);
		state.PlayerCredits += totalValue;

		// GATE.X.LEDGER.COST_BASIS.001: Compute realized profit on sell.
		state.PlayerCargoCostBasis.TryGetValue(GoodId, out int costBasis);
		int profitDelta = totalValue - costBasis * Quantity;
		// Clean up cost basis if no cargo remains.
		if (InventoryLedger.Get(state.PlayerCargo, GoodId) <= 0)
			state.PlayerCargoCostBasis.Remove(GoodId);

		// GATE.X.LEDGER.TX_MODEL.001: Record sell transaction for audit trail.
		state.AppendTransaction(new TransactionRecord
		{
			CashDelta = totalValue,
			GoodId = GoodId,
			Quantity = Quantity,
			Source = "Sell",
			NodeId = MarketId,
			ProfitDelta = profitDelta,
		});

		// GATE.T52.ECON.TRADE_DIVERSITY.001: Record trade for margin dampening.
		MarketSystem.RecordPlayerTrade(state, MarketId, GoodId);

		// GATE.S12.PROGRESSION.STATS.001: Track goods traded + credits earned.
		if (state.PlayerStats != null)
		{
			state.PlayerStats.GoodsTraded += Quantity;
			state.PlayerStats.TotalCreditsEarned += totalValue;
		}

		// GATE.T53.BOT.TRADE_REP.001: Award faction rep for trading at their station.
		if (!string.IsNullOrEmpty(factionId))
			ReputationSystem.OnTradeAtFactionStation(state, factionId);

		// GATE.T18.CHARACTER.FO_REACT.001: Fire FO trade triggers.
		if (totalValue > 0)
			FirstOfficerSystem.TryFireTrigger(state, "FIRST_PROFITABLE_TRADE");
		if (string.Equals(GoodId, WellKnownGoodIds.Munitions, StringComparison.Ordinal)
			|| string.Equals(GoodId, WellKnownGoodIds.Composites, StringComparison.Ordinal))
			FirstOfficerSystem.TryFireTrigger(state, "FIRST_WAR_GOODS_SALE");
	}
}
