using System.Collections.Generic;
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

    // SLICE 3: DISCOVERY MARKERS
    // Seeded discovery ids that become Seen when a fleet enters this node.
    // Determinism: producers may add in any order; consumers must sort Ordinal.
    public List<string> SeededDiscoveryIds { get; set; } = new();

    // SLICE 3: SIGNAL LAYER
    // Precursor Trace: Accumulates from Fracture usage. Drives Containment.
    public float Trace { get; set; } = 0f;
}
