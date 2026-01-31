using System.Numerics;

namespace SimCore.Entities;

public enum NodeKind { Star, Station, Waypoint }

public class Node
{
    public string Id { get; set; } = "";
    public Vector3 Position { get; set; }
    public NodeKind Kind { get; set; }
    public string Name { get; set; } = "";
    public string MarketId { get; set; } = "";

    // SLICE 3: SIGNAL LAYER
    // Precursor Trace: Accumulates from Fracture usage. Drives Containment.
    public float Trace { get; set; } = 0f;
}