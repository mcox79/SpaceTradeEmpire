using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

// GATE.X.LEDGER.INTEGRITY.001: Verify transaction ledger tracks all money flow.
[TestFixture]
[Category("Ledger")]
public sealed class LedgerIntegrityTests
{
    private const string MarketId = "mkt_test";
    private const string Fuel = "fuel";
    private const string Ore = "ore";
    private const int InitialCredits = 5000;
    private const int InitialStock = 200;

    private static SimState MakeState()
    {
        var state = new SimState(seed: 42);
        state.PlayerCredits = InitialCredits;

        var market = new Market { Id = MarketId };
        market.Inventory[Fuel] = InitialStock;
        market.Inventory[Ore] = InitialStock;
        state.Markets[MarketId] = market;

        return state;
    }

    [Test]
    public void BuySell_CashDeltaSum_MatchesCreditChange()
    {
        var state = MakeState();
        long startCredits = state.PlayerCredits;

        // Buy 10 fuel, sell 5 fuel, buy 3 ore, sell 2 ore.
        new BuyCommand(MarketId, Fuel, 10).Execute(state);
        new SellCommand(MarketId, Fuel, 5).Execute(state);
        new BuyCommand(MarketId, Ore, 3).Execute(state);
        new SellCommand(MarketId, Ore, 2).Execute(state);

        long endCredits = state.PlayerCredits;
        long txSum = state.TransactionLog.Sum(tx => (long)tx.CashDelta);

        Assert.That(txSum, Is.EqualTo(endCredits - startCredits),
            "Sum of transaction CashDeltas must equal net credit change");
    }

    [Test]
    public void MultipleBuySell_LedgerCountMatchesOperations()
    {
        var state = MakeState();

        new BuyCommand(MarketId, Fuel, 5).Execute(state);
        new SellCommand(MarketId, Fuel, 3).Execute(state);
        new BuyCommand(MarketId, Ore, 7).Execute(state);

        Assert.That(state.TransactionLog.Count, Is.EqualTo(3),
            "Each successful trade should produce exactly one transaction record");
    }

    [Test]
    public void FailedBuy_NoTransactionRecorded()
    {
        var state = MakeState();
        state.PlayerCredits = 0; // can't afford anything

        new BuyCommand(MarketId, Fuel, 5).Execute(state);

        Assert.That(state.TransactionLog.Count, Is.EqualTo(0),
            "Failed buy should not record a transaction");
    }

    [Test]
    public void FailedSell_NoTransactionRecorded()
    {
        var state = MakeState();
        // Player has no cargo to sell

        new SellCommand(MarketId, Fuel, 5).Execute(state);

        Assert.That(state.TransactionLog.Count, Is.EqualTo(0),
            "Failed sell should not record a transaction");
    }

    [Test]
    public void BuyTransaction_HasCorrectFields()
    {
        var state = MakeState();
        int buyPrice = state.Markets[MarketId].GetBuyPrice(Fuel);

        new BuyCommand(MarketId, Fuel, 3).Execute(state);

        Assert.That(state.TransactionLog.Count, Is.EqualTo(1));
        var tx = state.TransactionLog[0];
        // GATE.X.MARKET_PRICING.FEE_WIRE.001: CashDelta includes transaction fee.
        int grossCost = buyPrice * 3;
        int fee = MarketSystem.ComputeTransactionFeeCredits(state, grossCost);
        Assert.That(tx.CashDelta, Is.EqualTo(-(grossCost + fee)));
        Assert.That(tx.GoodId, Is.EqualTo(Fuel));
        Assert.That(tx.Quantity, Is.EqualTo(3));
        Assert.That(tx.Source, Is.EqualTo("Buy"));
        Assert.That(tx.NodeId, Is.EqualTo(MarketId));
    }

    [Test]
    public void SellTransaction_HasCorrectFields()
    {
        var state = MakeState();
        InventoryLedger.AddCargo(state.PlayerCargo, Fuel, 10);
        int sellPrice = state.Markets[MarketId].GetSellPrice(Fuel);

        new SellCommand(MarketId, Fuel, 4).Execute(state);

        Assert.That(state.TransactionLog.Count, Is.EqualTo(1));
        var tx = state.TransactionLog[0];
        // GATE.X.MARKET_PRICING.FEE_WIRE.001: CashDelta has fee deducted from sell revenue.
        int grossValue = sellPrice * 4;
        int fee = MarketSystem.ComputeTransactionFeeCredits(state, grossValue);
        Assert.That(tx.CashDelta, Is.EqualTo(grossValue - fee));
        Assert.That(tx.GoodId, Is.EqualTo(Fuel));
        Assert.That(tx.Quantity, Is.EqualTo(4));
        Assert.That(tx.Source, Is.EqualTo("Sell"));
        Assert.That(tx.NodeId, Is.EqualTo(MarketId));
    }

    [Test]
    public void ManyTrades_CashDeltaInvariantHolds()
    {
        // Stress test: many buy/sell cycles, verify invariant.
        var state = MakeState();
        state.PlayerCredits = 1_000_000;
        long startCredits = state.PlayerCredits;

        for (int i = 0; i < 50; i++)
        {
            new BuyCommand(MarketId, Fuel, 2).Execute(state);
            if (InventoryLedger.Get(state.PlayerCargo, Fuel) >= 2)
                new SellCommand(MarketId, Fuel, 2).Execute(state);
        }

        long endCredits = state.PlayerCredits;
        long txSum = state.TransactionLog.Sum(tx => (long)tx.CashDelta);

        Assert.That(txSum, Is.EqualTo(endCredits - startCredits),
            "Ledger integrity invariant: sum(CashDelta) == net credit change after many trades");
    }

    [Test]
    public void PerGood_QuantityDelta_MatchesCargo()
    {
        var state = MakeState();
        state.PlayerCredits = 100_000;

        new BuyCommand(MarketId, Fuel, 10).Execute(state);
        new SellCommand(MarketId, Fuel, 3).Execute(state);
        new BuyCommand(MarketId, Ore, 7).Execute(state);
        new SellCommand(MarketId, Ore, 2).Execute(state);

        // Compute per-good net quantity from ledger.
        var fuelBought = state.TransactionLog
            .Where(t => t.GoodId == Fuel && t.Source == "Buy")
            .Sum(t => t.Quantity);
        var fuelSold = state.TransactionLog
            .Where(t => t.GoodId == Fuel && t.Source == "Sell")
            .Sum(t => t.Quantity);
        var oreBought = state.TransactionLog
            .Where(t => t.GoodId == Ore && t.Source == "Buy")
            .Sum(t => t.Quantity);
        var oreSold = state.TransactionLog
            .Where(t => t.GoodId == Ore && t.Source == "Sell")
            .Sum(t => t.Quantity);

        Assert.That(fuelBought - fuelSold, Is.EqualTo(InventoryLedger.Get(state.PlayerCargo, Fuel)),
            "Ledger fuel net quantity must match cargo");
        Assert.That(oreBought - oreSold, Is.EqualTo(InventoryLedger.Get(state.PlayerCargo, Ore)),
            "Ledger ore net quantity must match cargo");
    }
}
