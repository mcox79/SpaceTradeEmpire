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
}