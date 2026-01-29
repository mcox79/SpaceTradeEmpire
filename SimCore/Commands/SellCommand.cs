using SimCore.Entities;
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
        if (!state.Markets.ContainsKey(MarketId)) return;
        var market = state.Markets[MarketId];

        // 1. Validate Player Has Cargo
        if (!state.PlayerCargo.ContainsKey(GoodId)) return;
        if (state.PlayerCargo[GoodId] < Quantity) return;

        // 2. Calculate Price (Spot Price)
        // In a real economy, we'd use a Bid/Ask spread, but for Slice 1 we use Spot.
        int unitPrice = market.GetPrice(GoodId); 
        int totalValue = unitPrice * Quantity;

        // 3. Execute Transaction
        state.PlayerCargo[GoodId] -= Quantity;
        if (state.PlayerCargo[GoodId] <= 0) state.PlayerCargo.Remove(GoodId);
        
        market.Inventory[GoodId] += Quantity; // Supply increases, Price will drop next tick
        state.PlayerCredits += totalValue;
    }
}