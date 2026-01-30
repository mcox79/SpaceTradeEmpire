using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

public class IndustrySite
{
    public string Id { get; set; } = "";
    public string NodeId { get; set; } = "";
    
    // RECIPE DEFINITION
    public Dictionary<string, int> Inputs { get; set; } = new();
    public Dictionary<string, int> Outputs { get; set; } = new();
    
    // STATE
    public float Efficiency { get; set; } = 1.0f;
    public bool Active { get; set; } = true;
}