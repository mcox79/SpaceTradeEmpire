using System.Collections.Generic;

namespace SimCore.Schemas;

/// <summary>
/// Minimal, deterministic world description intended for authored micro-worlds and tests.
/// Kept intentionally small so Slice 1 can build on a stable base without touching galaxy generation.
/// </summary>
public sealed class WorldDefinition
{
    public string WorldId { get; set; } = "micro_world";

    public List<WorldMarket> Markets { get; set; } = new();
    public List<WorldNode> Nodes { get; set; } = new();
    public List<WorldEdge> Edges { get; set; } = new();
    public List<WorldFaction> Factions { get; set; } = new();

    // Optional world class definitions (v0). Each class has exactly one measurable effect: FeeMultiplier.
    public List<WorldClassDefinition> WorldClasses { get; set; } = new();

    public WorldPlayerStart? Player { get; set; }
}

public sealed class WorldClassDefinition
{
    public string WorldClassId { get; set; } = "";
    public float FeeMultiplier { get; set; } = 1.0f;
}

public sealed class WorldMarket
{
    public string Id { get; set; } = "";
    public Dictionary<string, int> Inventory { get; set; } = new();
}

public sealed class WorldNode
{
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "Station"; // Star | Station | Waypoint
    public string Name { get; set; } = "";

    // Position is [x,y,z] in sim-space units. Stored as array for JSON simplicity.
    public float[] Pos { get; set; } = new float[] { 0f, 0f, 0f };

    // Optional link to a market (for stations).
    public string MarketId { get; set; } = "";

    // Optional deterministic world class tag (v0). Exactly one per node when assigned by generators.
    public string WorldClassId { get; set; } = "";
}

public sealed class WorldEdge
{
    public string Id { get; set; } = "";
    public string FromNodeId { get; set; } = "";
    public string ToNodeId { get; set; } = "";
    public float Distance { get; set; } = 0f;

    public int TotalCapacity { get; set; } = 5;
}

public sealed class WorldFaction
{
    public string FactionId { get; set; } = "";
    public string HomeNodeId { get; set; } = "";
    public string RoleTag { get; set; } = "";

    // Relations[OtherFactionId] in {-1,0,+1}. Keep explicit 0 entries for stable diffs.
    public Dictionary<string, int> Relations { get; set; } = new();
}

public sealed class WorldPlayerStart
{
    public long Credits { get; set; } = 1000;
    public string LocationNodeId { get; set; } = "";
    public Dictionary<string, int> Cargo { get; set; } = new();
}
