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

    // DEMAND (Ideal Stock Level)
    // If Inventory < Demand, Price goes UP.
    // If Inventory > Demand, Price goes DOWN.
    [JsonInclude]
    public Dictionary<string, int> Demand { get; set; } = new();

    // PRICING CONSTANTS
    private const int BASE_PRICE = 100;
    private const float ELASTICITY = 2.0f; // Price sensitivity

    public int GetPrice(string goodId)
    {
        int stock = Inventory.GetValueOrDefault(goodId, 0);
        int demand = Demand.GetValueOrDefault(goodId, 10); // Default demand if unset
        
        if (demand <= 0) demand = 1; // Prevent div/0

        // Scarcity Ratio: 0.5 (Surplus) to 2.0 (Shortage)
        float ratio = (float)demand / (float)(stock + 1);
        
        // Linear pricing model for stability
        // High Demand (Low Stock) -> High Price
        float priceMultiplier = 1.0f + ((ratio - 1.0f) * ELASTICITY);
        
        // Clamp multiplier (0.1x to 5.0x)
        if (priceMultiplier < 0.1f) priceMultiplier = 0.1f;
        if (priceMultiplier > 5.0f) priceMultiplier = 5.0f;

        return (int)(BASE_PRICE * priceMultiplier);
    }
}