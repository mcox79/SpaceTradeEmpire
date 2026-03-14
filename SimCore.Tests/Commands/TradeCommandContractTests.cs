using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Commands;

[TestFixture]
[Category("TradeCommandContract")]
public sealed class TradeCommandContractTests
{
	private const string MarketId = "mkt_test";
	private const string Fuel = "fuel";
	private const int InitialCredits = 1000;
	private const int InitialStock = 100;

	private static SimState MakeState()
	{
		var state = new SimState(seed: 42);
		state.PlayerCredits = InitialCredits;

		var market = new Market { Id = MarketId };
		market.Inventory[Fuel] = InitialStock;
		state.Markets[MarketId] = market;

		return state;
	}

	// ── BuyCommand ──

	[Test]
	public void BuyCommand_Success_UpdatesCreditsCargoAndMarket()
	{
		var state = MakeState();
		int pricePer = state.Markets[MarketId].GetBuyPrice(Fuel);
		int qty = 5;

		new BuyCommand(MarketId, Fuel, qty).Execute(state);

		// GATE.X.MARKET_PRICING.FEE_WIRE.001: totalCost includes 1% transaction fee.
		int grossCost = pricePer * qty;
		int fee = MarketSystem.ComputeTransactionFeeCredits(state, grossCost);
		Assert.That(state.PlayerCredits, Is.EqualTo(InitialCredits - grossCost - fee));
		Assert.That(InventoryLedger.Get(state.PlayerCargo, Fuel), Is.EqualTo(qty));
		Assert.That(InventoryLedger.Get(state.Markets[MarketId].Inventory, Fuel), Is.EqualTo(InitialStock - qty));
	}

	[Test]
	public void BuyCommand_InsufficientCredits_NoChange()
	{
		var state = MakeState();
		state.PlayerCredits = 1; // too low to buy anything at market price

		new BuyCommand(MarketId, Fuel, 5).Execute(state);

		Assert.That(state.PlayerCredits, Is.EqualTo(1));
		Assert.That(InventoryLedger.Get(state.PlayerCargo, Fuel), Is.EqualTo(0));
		Assert.That(InventoryLedger.Get(state.Markets[MarketId].Inventory, Fuel), Is.EqualTo(InitialStock));
	}

	[Test]
	public void BuyCommand_InsufficientStock_NoChange()
	{
		var state = MakeState();
		int overStock = InitialStock + 1;

		new BuyCommand(MarketId, Fuel, overStock).Execute(state);

		Assert.That(state.PlayerCredits, Is.EqualTo(InitialCredits));
		Assert.That(InventoryLedger.Get(state.Markets[MarketId].Inventory, Fuel), Is.EqualTo(InitialStock));
	}

	[Test]
	public void BuyCommand_ZeroQuantity_NoChange()
	{
		var state = MakeState();

		new BuyCommand(MarketId, Fuel, 0).Execute(state);

		Assert.That(state.PlayerCredits, Is.EqualTo(InitialCredits));
	}

	[Test]
	public void BuyCommand_BuyAll_EmptiesMarket()
	{
		var state = MakeState();
		state.PlayerCredits = 1_000_000; // enough for anything
		int qty = InitialStock;

		new BuyCommand(MarketId, Fuel, qty).Execute(state);

		Assert.That(InventoryLedger.Get(state.Markets[MarketId].Inventory, Fuel), Is.EqualTo(0));
		Assert.That(InventoryLedger.Get(state.PlayerCargo, Fuel), Is.EqualTo(qty));
	}

	[Test]
	public void BuyCommand_InvalidMarketId_NoChange()
	{
		var state = MakeState();

		new BuyCommand("nonexistent", Fuel, 1).Execute(state);

		Assert.That(state.PlayerCredits, Is.EqualTo(InitialCredits));
	}

	// ── SellCommand ──

	[Test]
	public void SellCommand_Success_UpdatesCreditsCargoAndMarket()
	{
		var state = MakeState();
		InventoryLedger.AddCargo(state.PlayerCargo, Fuel, 10);
		int pricePer = state.Markets[MarketId].GetSellPrice(Fuel);
		int qty = 5;

		new SellCommand(MarketId, Fuel, qty).Execute(state);

		// GATE.X.MARKET_PRICING.FEE_WIRE.001: totalValue has 1% fee deducted.
		int grossValue = pricePer * qty;
		int fee = MarketSystem.ComputeTransactionFeeCredits(state, grossValue);
		Assert.That(state.PlayerCredits, Is.EqualTo(InitialCredits + grossValue - fee));
		Assert.That(InventoryLedger.Get(state.PlayerCargo, Fuel), Is.EqualTo(5));
		Assert.That(InventoryLedger.Get(state.Markets[MarketId].Inventory, Fuel), Is.EqualTo(InitialStock + qty));
	}

	[Test]
	public void SellCommand_InsufficientCargo_NoChange()
	{
		var state = MakeState();
		// Player has no fuel cargo

		new SellCommand(MarketId, Fuel, 5).Execute(state);

		Assert.That(state.PlayerCredits, Is.EqualTo(InitialCredits));
		Assert.That(InventoryLedger.Get(state.Markets[MarketId].Inventory, Fuel), Is.EqualTo(InitialStock));
	}

	[Test]
	public void SellCommand_ZeroQuantity_NoChange()
	{
		var state = MakeState();
		InventoryLedger.AddCargo(state.PlayerCargo, Fuel, 10);

		new SellCommand(MarketId, Fuel, 0).Execute(state);

		Assert.That(InventoryLedger.Get(state.PlayerCargo, Fuel), Is.EqualTo(10));
	}

	[Test]
	public void SellCommand_SellAll_EmptiesCargo()
	{
		var state = MakeState();
		InventoryLedger.AddCargo(state.PlayerCargo, Fuel, 10);

		new SellCommand(MarketId, Fuel, 10).Execute(state);

		Assert.That(InventoryLedger.Get(state.PlayerCargo, Fuel), Is.EqualTo(0));
		Assert.That(state.PlayerCargo.ContainsKey(Fuel), Is.False, "Cargo key removed at zero");
	}

	// ── TradeCommand ──

	[Test]
	public void TradeCommand_Buy_UpdatesCreditsCargoAndMarket()
	{
		var state = MakeState();
		int pricePer = state.Markets[MarketId].GetBuyPrice(Fuel);
		int qty = 3;

		new TradeCommand("player", MarketId, Fuel, qty, TradeType.Buy).Execute(state);

		// TradeCommand delegates to BuyCommand which includes transaction fee.
		int grossCost = pricePer * qty;
		int fee = MarketSystem.ComputeTransactionFeeCredits(state, grossCost);
		Assert.That(state.PlayerCredits, Is.EqualTo(InitialCredits - grossCost - fee));
		Assert.That(InventoryLedger.Get(state.PlayerCargo, Fuel), Is.EqualTo(qty));
		Assert.That(InventoryLedger.Get(state.Markets[MarketId].Inventory, Fuel), Is.EqualTo(InitialStock - qty));
	}

	[Test]
	public void TradeCommand_Sell_UpdatesCreditsCargoAndMarket()
	{
		var state = MakeState();
		InventoryLedger.AddCargo(state.PlayerCargo, Fuel, 10);
		int pricePer = state.Markets[MarketId].GetSellPrice(Fuel);
		int qty = 4;

		new TradeCommand("player", MarketId, Fuel, qty, TradeType.Sell).Execute(state);

		// TradeCommand delegates to SellCommand which includes transaction fee.
		int grossValue = pricePer * qty;
		int fee = MarketSystem.ComputeTransactionFeeCredits(state, grossValue);
		Assert.That(state.PlayerCredits, Is.EqualTo(InitialCredits + grossValue - fee));
		Assert.That(InventoryLedger.Get(state.PlayerCargo, Fuel), Is.EqualTo(6));
		Assert.That(InventoryLedger.Get(state.Markets[MarketId].Inventory, Fuel), Is.EqualTo(InitialStock + qty));
	}

	[Test]
	public void TradeCommand_Buy_InsufficientCredits_NoChange()
	{
		var state = MakeState();
		state.PlayerCredits = 0;

		new TradeCommand("player", MarketId, Fuel, 5, TradeType.Buy).Execute(state);

		Assert.That(state.PlayerCredits, Is.EqualTo(0));
		Assert.That(InventoryLedger.Get(state.PlayerCargo, Fuel), Is.EqualTo(0));
	}

	[Test]
	public void TradeCommand_Sell_InsufficientCargo_NoChange()
	{
		var state = MakeState();

		new TradeCommand("player", MarketId, Fuel, 5, TradeType.Sell).Execute(state);

		Assert.That(state.PlayerCredits, Is.EqualTo(InitialCredits));
	}

	[Test]
	public void TradeCommand_ZeroQuantity_NoChange()
	{
		var state = MakeState();

		new TradeCommand("player", MarketId, Fuel, 0, TradeType.Buy).Execute(state);

		Assert.That(state.PlayerCredits, Is.EqualTo(InitialCredits));
	}
}
