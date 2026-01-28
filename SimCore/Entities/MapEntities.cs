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
}

public class Edge
{
    public string Id { get; set; } = "";
    public string FromNodeId { get; set; } = "";
    public string ToNodeId { get; set; } = "";
    public float Distance { get; set; }
    public int TotalCapacity { get; set; } = 10;
    public int UsedSlots { get; set; } = 0;
}