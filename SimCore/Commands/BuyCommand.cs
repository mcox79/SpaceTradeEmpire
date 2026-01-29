using SimCore.Entities;
using System;

namespace SimCore.Commands;

public class BuyCommand : ICommand
{
    public string MarketId { get; set; }
    public string GoodId { get; set; } // Added per Slice 1 requirements
    public int Quantity { get; set; }

    public BuyCommand(string marketId, string goodId, int quantity)
    {
        MarketId = marketId;
        GoodId = goodId;
        Quantity = quantity;
    }

    public void Execute(SimState state)
    {
        if (!state.Markets.ContainsKey(MarketId)) return;
        var market = state.Markets[MarketId];

        // 1. Check Availability
        if (!market.Inventory.ContainsKey(GoodId)) return;
        if (market.Inventory[GoodId] < Quantity) return;

        // 2. Calculate Price
        int unitPrice = market.GetPrice(GoodId);
        int totalCost = unitPrice * Quantity;

        // 3. Transaction
        if (state.PlayerCredits >= totalCost)
        {
            state.PlayerCredits -= totalCost;
            market.Inventory[GoodId] -= Quantity;
            
            if (!state.PlayerCargo.ContainsKey(GoodId)) state.PlayerCargo[GoodId] = 0;
            state.PlayerCargo[GoodId] += Quantity;
        }
    }
}