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

    // GATE.S6.FRACTURE.ACCESS_MODEL.001: Fracture node access properties.
    // IsFractureNode: true if this node requires fracture access checks.
    // FractureTier: minimum tech tier required to enter (0 = any, 1+ = tiered).
    public bool IsFractureNode { get; set; } = false;
    public int FractureTier { get; set; } = 0;

    // GATE.S7.INSTABILITY.PHASE_MODEL.001: Per-node instability level (0-100+).
    // 5 phases: Stable(0-24), Shimmer(25-49), Drift(50-74), Fracture(75-99), Void(100+).
    public int InstabilityLevel { get; set; } = 0;

    // GATE.T18.NARRATIVE.TOPOLOGY_SHIFT.001: True if edges at this node can mutate in Phase 3+.
    public bool MutableTopology { get; set; } = false;
}
