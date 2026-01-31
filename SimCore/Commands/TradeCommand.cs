using SimCore.Entities;
using System;

namespace SimCore.Commands;

public enum TradeType { Buy, Sell }

public class TradeCommand : ICommand
{
    public string PlayerId { get; set; }
    public string MarketNodeId { get; set; }
    public string GoodId { get; set; }
    public int Quantity { get; set; }
    public TradeType Type { get; set; }

    public TradeCommand(string playerId, string nodeId, string goodId, int qty, TradeType type)
    {
        PlayerId = playerId;
        MarketNodeId = nodeId;
        GoodId = goodId;
        Quantity = qty;
        Type = type;
    }

    public void Execute(SimState state)
    {
        // 1. Validation
        if (!state.Markets.TryGetValue(MarketNodeId, out var market)) return;
        
        // Get Price based on current scarcity
        int pricePerUnit = market.GetPrice(GoodId);
        int totalCost = pricePerUnit * Quantity;

        if (Type == TradeType.Buy)
        {
            // BUY: Player buys FROM Market
            if (state.PlayerCredits < totalCost) return; // Too poor
            if (market.Inventory.GetValueOrDefault(GoodId, 0) < Quantity) return; // Out of stock

            // Execute
            state.PlayerCredits -= totalCost;
            market.Inventory[GoodId] -= Quantity;
            
            if (!state.PlayerCargo.ContainsKey(GoodId)) state.PlayerCargo[GoodId] = 0;
            state.PlayerCargo[GoodId] += Quantity;
        }
        else
        {
            // SELL: Player sells TO Market
            if (state.PlayerCargo.GetValueOrDefault(GoodId, 0) < Quantity) return; // Player doesn't have it

            // Execute
            state.PlayerCredits += totalCost;
            if (!market.Inventory.ContainsKey(GoodId)) market.Inventory[GoodId] = 0;
            market.Inventory[GoodId] += Quantity;
            state.PlayerCargo[GoodId] -= Quantity;
            if (state.PlayerCargo[GoodId] <= 0) state.PlayerCargo.Remove(GoodId);
        }
    }
}