using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

public class Market
{
    public string Id { get; set; } = "";
    
    // REFACTOR: Multi-good inventory
    public Dictionary<string, int> Inventory { get; set; } = new();

    // REFACTOR: Price is now a function of specific good supply
    public int GetPrice(string goodId)
    {
        // SLICE 1 STUB: Simple supply/demand curve
        int basePrice = 100;
        int stock = Inventory.ContainsKey(goodId) ? Inventory[goodId] : 0;
        
        // Simple linear scarcity: Less than 50 items = Price Spike
        return Math.Max(1, basePrice + (50 - stock));
    }
}