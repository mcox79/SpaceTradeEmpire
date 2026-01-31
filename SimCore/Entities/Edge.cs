using System.Numerics;

namespace SimCore.Entities;

public class Edge
{
    public string Id { get; set; } = "";
    public string FromNodeId { get; set; } = "";
    public string ToNodeId { get; set; } = "";
    public float Distance { get; set; }
    
    // ARCHITECTURE: "Lane travel is constrained by Slot Capacity"
    public int TotalCapacity { get; set; } = 5;
    public int UsedCapacity { get; set; } = 0;

    // SLICE 3: SIGNAL LAYER
    // Economic Heat: Driven by throughput/value. Drives Piracy.
    public float Heat { get; set; } = 0f;
}