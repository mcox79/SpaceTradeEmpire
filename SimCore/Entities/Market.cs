using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

public class Market
{
    public string Id { get; set; } = "";
    
    // CORE STORAGE
    [JsonInclude]
    public Dictionary<string, int> Inventory { get; set; } = new();
    
    // PRICING (Stub for Slice 3)
    [JsonIgnore]
    public Dictionary<string, float> Prices { get; set; } = new();

    // LOGIC: Simple linear scarcity (Required by Buy/Sell Commands)
    public int GetPrice(string goodId)
    {
        int basePrice = 100;
        int stock = Inventory.ContainsKey(goodId) ? Inventory[goodId] : 0;
        // Linear scarcity: Less than 50 items = Price Spike
        return Math.Max(1, basePrice + (50 - stock));
    }
}