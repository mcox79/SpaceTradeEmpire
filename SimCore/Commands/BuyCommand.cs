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
		// Clamp to available stock.
		int qty = Math.Min(Quantity, available);
		if (qty <= 0) return;

		// GATE.X.SHIP_CLASS.CARGO_ENFORCE.001: Clamp to cargo capacity.
		var playerFleet = state.Fleets.Values.FirstOrDefault(f =>
			string.Equals(f.OwnerId, "player", StringComparison.Ordinal));
		if (playerFleet != null)
		{
			var classDef = ShipClassContentV0.GetById(playerFleet.ShipClassId);
			if (classDef != null && classDef.CargoCapacity > 0)
			{
				int currentCargo = state.PlayerCargo.Values.Sum();
				int cargoSpace = classDef.CargoCapacity - currentCargo;
				qty = Math.Min(qty, cargoSpace);
				if (qty <= 0) return;
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

		// GATE.T61.MARKET.DEPTH_MODEL.001: Depth-based price impact (buy price UP).
		int depthBps = MarketSystem.ComputeDepthImpactBps(qty, market.Depth);
		if (depthBps > 0)
			unitPrice = (int)Math.Max(1, (long)unitPrice * (10000 + depthBps) / 10000);

		// GATE.T61.MARKET.BID_ASK.001: Dynamic spread adjustment (buy price UP).
		int dynSpreadBps = MarketSystem.GetDynamicSpreadAdjustmentBps(state, MarketId);
		if (dynSpreadBps > 0)
			unitPrice = (int)Math.Max(1, (long)unitPrice * (10000 + dynSpreadBps / 2) / 10000);

		// GATE.T52.ECON.TRADE_DIVERSITY.001: Apply recent-trade margin dampening (buy price UP).
		int dampenBps = MarketSystem.GetRecentTradeDampenBps(state, MarketId, GoodId);
		unitPrice = MarketSystem.ApplyRecentTradeDampening(unitPrice, dampenBps, isBuy: true);

		// GATE.T65.ECON.ROUTE_NOVELTY.001: Apply novelty bonus (buy price DOWN for new routes).
		int noveltyBps = MarketSystem.GetRouteNoveltyBonusBps(state, MarketId, GoodId);
		unitPrice = MarketSystem.ApplyNoveltyBonus(unitPrice, noveltyBps, isBuy: true);

		// GATE.X.MARKET_PRICING.REP_WIRE.001: Apply reputation-based price modifier.
		var factionId = MarketSystem.GetControllingFactionIdForMarket(state, MarketId);
		int repBps = MarketSystem.GetRepPricingBps(state, factionId);
		unitPrice = MarketSystem.ApplyRepPricing(unitPrice, repBps);

		// GATE.X.MARKET_PRICING.AFFINITY_WIRE.001: Apply faction good affinity modifier.
		int affinityBps = MarketSystem.GetFactionGoodAffinityBps(state, factionId, GoodId);
		unitPrice = MarketSystem.ApplyFactionGoodAffinityPricing(unitPrice, affinityBps);

		if (instMultBps != 10000)
			unitPrice = (int)Math.Max(1, (long)unitPrice * instMultBps / 10000);

		// Compute effective cost per unit including tariffs and fees, then clamp qty to affordable.
		int tariffBps = MarketSystem.GetEffectiveTariffBps(state, MarketId);
		int sampleCost = unitPrice;
		sampleCost += MarketSystem.ComputeTariffCredits(sampleCost, tariffBps);
		sampleCost += MarketSystem.ComputeTransactionFeeCredits(state, sampleCost);
		// GATE.T68.ECON.PERCENTAGE_SINKS.001: Include trade tax in affordability check.
		if (FleetUpkeepTweaksV0.TradeTaxBps > 0)
			sampleCost += (int)Math.Max(1, (long)sampleCost * FleetUpkeepTweaksV0.TradeTaxBps / 10000);
		int effectiveUnitCost = Math.Max(1, sampleCost);
		qty = (int)Math.Min(qty, state.PlayerCredits / effectiveUnitCost);
		if (qty <= 0) return;

		// Recompute exact total for the clamped quantity.
		int totalCost = unitPrice * qty;

		// GATE.S7.FACTION.TARIFF_ENFORCE.001: Apply tariff surcharge (increases buy cost).
		totalCost += MarketSystem.ComputeTariffCredits(totalCost, tariffBps);

		// GATE.X.MARKET_PRICING.FEE_WIRE.001: Deduct transaction fee.
		totalCost += MarketSystem.ComputeTransactionFeeCredits(state, totalCost);

		// GATE.T68.ECON.PERCENTAGE_SINKS.001: Apply trade tax (percentage-based sink).
		if (FleetUpkeepTweaksV0.TradeTaxBps > 0)
		{
			long taxAmount = (long)totalCost * FleetUpkeepTweaksV0.TradeTaxBps / 10000;
			if (taxAmount < 1) taxAmount = 1;
			totalCost += (int)taxAmount;
		}

		// Final safety: if rounding pushed total over budget, reduce by one.
		while (qty > 0 && totalCost > state.PlayerCredits)
		{
			qty--;
			totalCost = unitPrice * qty;
			totalCost += MarketSystem.ComputeTariffCredits(totalCost, tariffBps);
			totalCost += MarketSystem.ComputeTransactionFeeCredits(state, totalCost);
			if (FleetUpkeepTweaksV0.TradeTaxBps > 0)
				totalCost += (int)Math.Max(1, (long)totalCost * FleetUpkeepTweaksV0.TradeTaxBps / 10000);
		}
		if (qty <= 0) return;

		// Update Quantity to reflect clamped amount (for transaction record).
		Quantity = qty;

		if (!InventoryLedger.TryRemoveMarket(market.Inventory, GoodId, qty)) return;

		state.PlayerCredits -= totalCost;
		InventoryLedger.AddCargo(state.PlayerCargo, GoodId, qty);

		// GATE.X.LEDGER.COST_BASIS.001: Update weighted average cost basis.
		{
			int totalQty = InventoryLedger.Get(state.PlayerCargo, GoodId); // qty after this buy
			int prevQty = totalQty - qty;
			if (prevQty < 0) prevQty = 0;
			state.PlayerCargoCostBasis.TryGetValue(GoodId, out int prevAvg);
			long totalBasis = (long)prevQty * prevAvg + (long)qty * unitPrice;
			state.PlayerCargoCostBasis[GoodId] = totalQty > 0 ? (int)(totalBasis / totalQty) : 0;
		}

		// GATE.X.LEDGER.TX_MODEL.001: Record buy transaction for audit trail.
		state.AppendTransaction(new TransactionRecord
		{
			CashDelta = -totalCost,
			GoodId = GoodId,
			Quantity = qty,
			Source = "Buy",
			NodeId = MarketId,
		});

		// GATE.T52.ECON.TRADE_DIVERSITY.001: Record trade for margin dampening.
		MarketSystem.RecordPlayerTrade(state, MarketId, GoodId);
		// GATE.T65.ECON.ROUTE_NOVELTY.001: Record trade for novelty tracking.
		MarketSystem.RecordRouteNovelty(state, MarketId, GoodId);
		// GATE.T66.ECON.MARGIN_FLOOR.001: Record station trade for fresh stock premium.
		MarketSystem.RecordStationTrade(state, MarketId);

		// GATE.T61.MARKET.DEPTH_MODEL.001: Consume market depth after trade.
		MarketSystem.ConsumeDepth(market, qty);
		// GATE.T61.MARKET.BID_ASK.001: Record trade for volatility tracking.
		MarketSystem.RecordTradeVolatility(market);
		// GATE.T61.MARKET.PRICE_SMOOTH.001: Stamp last trade tick for depth decay.
		market.LastTradeTick = state.Tick;

		// GATE.T53.BOT.TRADE_REP.001: Award faction rep for trading at their station.
		if (!string.IsNullOrEmpty(factionId))
			ReputationSystem.OnTradeAtFactionStation(state, factionId);

		// GATE.T67.PACING.STREAK_BREAKER.001: Record action type for streak tracking.
		RecordActionType(state, "buy");
		// GATE.T67.FO.SILENCE_DECISIONS.001: Increment decision counter for FO silence tracking.
		if (state.FirstOfficer != null) state.FirstOfficer.DecisionsSinceLastLine++;
	}

	// GATE.T67.PACING.STREAK_BREAKER.001: Track consecutive same-type actions.
	private static void RecordActionType(SimState state, string actionType)
	{
		if (state.PlayerStats == null) return;
		if (string.Equals(state.PlayerStats.LastActionType, actionType, StringComparison.Ordinal))
			state.PlayerStats.ConsecutiveActionStreak++;
		else
		{
			state.PlayerStats.LastActionType = actionType;
			state.PlayerStats.ConsecutiveActionStreak = 1; // STRUCTURAL: first action of new type
		}
	}
}
