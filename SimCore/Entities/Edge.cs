using System;
using System.Collections.Generic;
using System.Numerics;

namespace SimCore.Entities;

public class Edge
{
    // Deterministic ordering contract for corridor-like items:
    // - Primary: FromNodeId (Ordinal)
    // - Secondary: ToNodeId (Ordinal)
    // - Tertiary: Id (Ordinal)
    public static readonly IComparer<Edge> DeterministicComparer = Comparer<Edge>.Create((a, b) =>
    {
        if (ReferenceEquals(a, b)) return 0;
        if (a is null) return -1;
        if (b is null) return 1;

        int c1 = StringComparer.Ordinal.Compare(a.FromNodeId, b.FromNodeId);
        if (c1 != 0) return c1;

        int c2 = StringComparer.Ordinal.Compare(a.ToNodeId, b.ToNodeId);
        if (c2 != 0) return c2;

        return StringComparer.Ordinal.Compare(a.Id, b.Id);
    });

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
