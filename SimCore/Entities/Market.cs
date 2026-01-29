using System;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

public class Market
{
    public string Id { get; set; } = "";
    public int Inventory { get; set; } // Simple Int for Slice 1 compatibility
    public int BasePrice { get; set; }

    [JsonIgnore]
    public int CurrentPrice => Math.Max(1, BasePrice + (100 - Inventory));
}