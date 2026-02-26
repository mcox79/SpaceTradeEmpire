using System;
using System.Collections.Generic;
using System.Linq;

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

    // GATE.S2_5.WGEN.DISCOVERY_SEEDING.001: discovery seeding surface v0 (schema-bound, deterministic).
    // Ordering contract: consumers must sort by DiscoveryId (ordinal) unless an explicit alternate ordering is declared.
    public List<DiscoverySeedSurfaceV0> DiscoverySeedsV0 { get; set; } = new();

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

// GATE.S2_5.WGEN.DISCOVERY_SEEDING.001: minimal discovery seeding surface v0.
// Required fields:
// - DiscoveryId: stable unique id within the world for this seed record.
// - DiscoveryKind: schema token (no free-text) describing the seed class.
// - NodeId: primary anchor location.
// - RefId: secondary anchor (good id, other node id, etc) depending on kind.
// - SourceId: originating generator id (lane id, industry site id, etc) for traceability.
// Stable ID rule v0:
// - DiscoveryId MUST be minted only from deterministic inputs (DiscoveryKind + stable primary ids)
// - No timestamps%wall-clock, no global RNG, no unordered iteration dependencies.
// - Suggested canonical format (implemented in generator): "disc_v0|<KIND>|<NodeId>|<RefId>|<SourceId>"
public sealed class DiscoverySeedSurfaceV0
{
    public string DiscoveryId { get; set; } = "";
    public string DiscoveryKind { get; set; } = "";
    public string NodeId { get; set; } = "";
    public string RefId { get; set; } = "";
    public string SourceId { get; set; } = "";
}

public static class DiscoverySeedKindsV0
{
    // Token set is intentionally small in v0; extend only with deterministic justification and updated contract tests.
    public const string ResourcePoolMarker = "RESOURCE_POOL_MARKER";
    public const string CorridorTrace = "CORRIDOR_TRACE";
}

public sealed class WorldPlayerStart
{
    public long Credits { get; set; } = 1000;
    public string LocationNodeId { get; set; } = "";
    public Dictionary<string, int> Cargo { get; set; } = new();
}

/// <summary>Deterministic scenario harness v0 for authored micro-worlds (stable ordering + diff summary).</summary>
public static class ScenarioHarnessV0
{
    public sealed class Builder
    {
        private readonly WorldDefinition _d;

        public Builder(string worldId)
        {
            _d = new WorldDefinition { WorldId = worldId };
        }

        public Builder Market(string id, params (string goodId, int qty)[] inv)
        {
            var m = new WorldMarket { Id = id };

            foreach (var (g, q) in inv)
            {
                m.Inventory[g] = q;
            }

            _d.Markets.Add(m);
            return this;
        }

        public Builder Node(string id, string kind, string name, string marketId, float[] pos)
        {
            _d.Nodes.Add(new WorldNode
            {
                Id = id,
                Kind = kind,
                Name = name,
                MarketId = marketId,
                Pos = pos
            });

            return this;
        }

        public Builder Lane(string id, string from, string to, float dist, int cap)
        {
            _d.Edges.Add(new WorldEdge
            {
                Id = id,
                FromNodeId = from,
                ToNodeId = to,
                Distance = dist,
                TotalCapacity = cap
            });

            return this;
        }

        public Builder Player(long credits, string loc)
        {
            _d.Player = new WorldPlayerStart
            {
                Credits = credits,
                LocationNodeId = loc
            };

            return this;
        }

        public WorldDefinition Build()
        {
            _d.Markets = _d.Markets.OrderBy(x => x.Id, StringComparer.Ordinal).ToList();
            _d.Nodes = _d.Nodes.OrderBy(x => x.Id, StringComparer.Ordinal).ToList();
            _d.Edges = _d.Edges.OrderBy(x => x.Id, StringComparer.Ordinal).ToList();

            foreach (var m in _d.Markets)
            {
                m.Inventory = m.Inventory
                    .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                    .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
            }

            return _d;
        }
    }

    public static Builder New(string worldId) => new Builder(worldId);

    public static WorldDefinition MicroWorld001()
    {
        return New("micro_world_001")
            .Market("mkt_a", ("ore", 10), ("food", 3))
            .Market("mkt_b", ("ore", 1), ("food", 12))
            .Node("stn_a", "Station", "Alpha Station", "mkt_a", new float[] { 0f, 0f, 0f })
            .Node("stn_b", "Station", "Beta Station", "mkt_b", new float[] { 10f, 0f, 0f })
            .Lane("lane_ab", "stn_a", "stn_b", 1.0f, 5)
            .Player(1000, "stn_a")
            .Build();
    }

    public static string ToDeterministicSummary(WorldDefinition d)
    {
        return
            "WORLD_SUMMARY_V0\n" +
            "world_id=" + d.WorldId + "\n" +
            "markets=" + string.Join(',', d.Markets.OrderBy(x => x.Id, StringComparer.Ordinal).Select(m => m.Id)) + "\n" +
            "nodes=" + string.Join(',', d.Nodes.OrderBy(x => x.Id, StringComparer.Ordinal).Select(n => n.Id)) + "\n" +
            "edges=" + string.Join(',', d.Edges.OrderBy(x => x.Id, StringComparer.Ordinal).Select(e => e.Id)) + "\n";
    }
}
