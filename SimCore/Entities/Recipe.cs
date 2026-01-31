using System.Collections.Generic;

namespace SimCore.Entities;

public class Recipe
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int DurationTicks { get; set; } = 10;
    
    public Dictionary<string, int> Inputs { get; set; } = new();
    public Dictionary<string, int> Outputs { get; set; } = new();
}