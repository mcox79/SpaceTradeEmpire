using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

public class Market
{
    public string Id { get; set; } = "";
    
    // INVENTORY: Raw goods storage
    public Dictionary<string, int> Inventory { get; set; } = new();
    
    // INDUSTRY: Production capabilities
    public Dictionary<string, Industry> Industries { get; set; } = new();

    // PRICING MODEL
    public int GetPrice(string goodId)
    {
        // SLICE 1 STUB: Simple scarcity curve
        int basePrice = 100;
        int stock = Inventory.ContainsKey(goodId) ? Inventory[goodId] : 0;
        return Math.Max(1, basePrice + (50 - stock));
    }
}