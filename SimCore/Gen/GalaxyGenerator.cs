using System.Numerics;
using SimCore.Entities;
using SimCore.Schemas;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Text;

namespace SimCore.Gen;

public static class GalaxyGenerator
{
    // GATE.S2_5.WGEN.ECON.001: deterministic starter region size (first N stars by generation index).
    public const int StarterRegionNodeCount = 8;

    public static void Generate(SimState state, int starCount, float radius)
    {
        state.Nodes.Clear();
        state.Edges.Clear();
        state.Markets.Clear();
        state.Fleets.Clear();
        state.IndustrySites.Clear();

        var nodesList = new List<Node>();
        var rng = state.Rng ?? throw new InvalidOperationException("SimState.Rng is null.");

        for (int i = 0; i < starCount; i++)
        {
            float x = (float)(rng.NextDouble() * 2 - 1) * radius;
            float z = (float)(rng.NextDouble() * 2 - 1) * radius;

            var node = new Node
            {
                Id = $"star_{i}",
                Name = $"System {i}",
                Position = new Vector3(x, 0, z),
                Kind = NodeKind.Star,
                MarketId = $"star_{i}"
            };
            state.Nodes.Add(node.Id, node);
            nodesList.Add(node);

            var mkt = new Market { Id = node.MarketId };

            // Always ensure keys exist for deterministic price publishing and inventory semantics.
            mkt.Inventory["fuel"] = 100;
            mkt.Inventory["ore"] = 0;
            mkt.Inventory["metal"] = 0;

            // GATE.S2_5.WGEN.ECON.001: deterministic economy placement v0 for starter region.
            // Starter region is the first N stars by generation index.
            bool isStarter = i < Math.Min(starCount, StarterRegionNodeCount);

            if (i % 2 == 0)
            {
                // MINE: supplies ore; starter region also supplies fuel and demands metal (demand sink).
                if (isStarter)
                {
                    mkt.Inventory["fuel"] = 120;  // supply
                    mkt.Inventory["ore"] = 500;   // supply
                    mkt.Inventory["metal"] = 10;  // demand sink (scarce)
                }
                else
                {
                    mkt.Inventory["ore"] = 500;
                }

                var mine = new IndustrySite
                {
                    Id = $"mine_{i}",
                    NodeId = node.Id,
                    Inputs = new Dictionary<string, int>
                    {
                        { "fuel", 1 },
                        { "ore", 0 } // keep ore key present in Inputs map? no, Inputs should be meaningful only
                    },
                    Outputs = new Dictionary<string, int> { { "ore", 5 } },
                    BufferDays = 1,
                    DegradePerDayBps = 0
                };

                // Remove the dummy input so upkeep inputs are real only
                mine.Inputs.Remove("ore");

                state.IndustrySites.Add(mine.Id, mine);
                node.Name += " (Mining)";
            }
            else
            {
                // REFINERY: consumes ore + fuel, produces metal; starter region demands ore+fuel and supplies metal.
                if (isStarter)
                {
                    mkt.Inventory["fuel"] = 10;   // demand sink (scarce)
                    mkt.Inventory["ore"] = 0;     // demand sink (scarce)
                    mkt.Inventory["metal"] = 200; // supply
                }

                var factory = new IndustrySite
                {
                    Id = $"fac_{i}",
                    NodeId = node.Id,
                    Inputs = new Dictionary<string, int>
                    {
                        { "ore", 10 },
                        { "fuel", 1 }
                    },
                    Outputs = new Dictionary<string, int> { { "metal", 5 } },
                    BufferDays = 2,
                    DegradePerDayBps = 500 // 5% health per day at full deficit
                };
                state.IndustrySites.Add(factory.Id, factory);
                node.Name += " (Refinery)";
            }

            state.Markets.Add(node.MarketId, mkt);
        }

        if (nodesList.Count == 0) return;
        state.PlayerLocationNodeId = nodesList[0].Id;

        for (int i = 0; i < nodesList.Count - 1; i++)
        {
            CreateEdge(state, nodesList[i], nodesList[i + 1]);
        }
        CreateEdge(state, nodesList[nodesList.Count - 1], nodesList[0]);

        foreach (var node in nodesList)
        {
            var fleet = new Fleet
            {
                Id = $"ai_fleet_{node.Id}",
                OwnerId = "ai",
                CurrentNodeId = node.Id,
                Speed = 0.8f,
                State = FleetState.Idle,
                Supplies = 100
            };
            state.Fleets.Add(fleet.Id, fleet);
        }
    }

    private static void CreateEdge(SimState state, Node a, Node b)
    {
        string id = $"edge_{GetSortedId(a.Id, b.Id)}";
        if (!state.Edges.ContainsKey(id))
        {
            state.Edges.Add(id, new Edge
            {
                Id = id,
                FromNodeId = a.Id,
                ToNodeId = b.Id,
                Distance = Vector3.Distance(a.Position, b.Position),
                TotalCapacity = 5
            });
        }
    }

    private static string GetSortedId(string a, string b)
    {
        return string.Compare(a, b) < 0 ? $"{a}_{b}" : $"{b}_{a}";
    }

    // GATE.S2_5.WGEN.FACTION.001: deterministic faction seeding v0.
    // Output format is intentionally diff-friendly: a table sorted by FactionId plus a relations matrix
    // with rows%cols sorted by FactionId. Avoid unordered iteration by sorting ids explicitly.
    public static IReadOnlyList<WorldFaction> SeedFactionsFromNodesSorted(IReadOnlyList<string> nodeIdsSorted)
    {
        if (nodeIdsSorted.Count == 0) throw new InvalidOperationException("No nodes available for faction seeding.");

        var roles = new[] { "Trader", "Miner", "Pirate" };
        var fids = new[] { "faction_0", "faction_1", "faction_2" };

        int idx0 = 0;
        int idx1 = nodeIdsSorted.Count / 2;
        int idx2 = nodeIdsSorted.Count - 1;

        var home = new[] { nodeIdsSorted[idx0], nodeIdsSorted[idx1], nodeIdsSorted[idx2] };

        // Ensure uniqueness for tiny worlds deterministically (advance to next available sorted node id).
        var used = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < home.Length; i++)
        {
            if (used.Add(home[i])) continue;

            for (int j = 0; j < nodeIdsSorted.Count; j++)
            {
                if (used.Add(nodeIdsSorted[j]))
                {
                    home[i] = nodeIdsSorted[j];
                    break;
                }
            }
        }

        // Canonical relations pattern (values in {-1,0,+1}). Keep explicit 0 entries for stable diffs.
        var rel = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal)
        {
            ["faction_0"] = new Dictionary<string, int>(StringComparer.Ordinal) { ["faction_1"] = +1, ["faction_2"] = -1 },
            ["faction_1"] = new Dictionary<string, int>(StringComparer.Ordinal) { ["faction_0"] = +1, ["faction_2"] = 0 },
            ["faction_2"] = new Dictionary<string, int>(StringComparer.Ordinal) { ["faction_0"] = -1, ["faction_1"] = 0 },
        };

        var factions = new List<WorldFaction>(capacity: 3);
        for (int i = 0; i < 3; i++)
        {
            var fid = fids[i];
            var rmap = rel[fid];

            factions.Add(new WorldFaction
            {
                FactionId = fid,
                HomeNodeId = home[i],
                RoleTag = roles[i],
                Relations = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    // Explicitly include all non-self entries, including zeros.
                    ["faction_0"] = fid == "faction_0" ? 0 : rmap.GetValueOrDefault("faction_0", 0),
                    ["faction_1"] = fid == "faction_1" ? 0 : rmap.GetValueOrDefault("faction_1", 0),
                    ["faction_2"] = fid == "faction_2" ? 0 : rmap.GetValueOrDefault("faction_2", 0),
                }
            });

            // Remove self key to keep semantics "Relations[OtherFactionId]" while still keeping stable diffs.
            factions[i].Relations.Remove(fid);
        }

        // Ensure stable order by FactionId.
        factions.Sort((a, b) => string.CompareOrdinal(a.FactionId, b.FactionId));
        return factions;
    }

    private static uint Fnv1a32Utf8(string s)
    {
        unchecked
        {
            uint h = 2166136261;
            var bytes = Encoding.UTF8.GetBytes(s);
            for (int i = 0; i < bytes.Length; i++)
            {
                h ^= bytes[i];
                h *= 16777619;
            }
            return h;
        }
    }

    private static int Quantize1e3(float v) => (int)MathF.Round(v * 1000f);

    private static uint ScoreNode(int seed, Node n)
    {
        var p = n.Position;
        var qx = Quantize1e3(p.X);
        var qz = Quantize1e3(p.Z);
        return Fnv1a32Utf8($"{seed}|{n.Id}|{qx}|{qz}");
    }

    public static string BuildFactionSeedReport(SimState state, int seed)
    {
        // Order homes by a position-derived stable score so different seeds (different positions) produce diffs.
        var nodeIds = state.Nodes.Values
            .Select(n => (Id: n.Id, Score: ScoreNode(seed, n)))
                            .OrderByDescending(t => t.Score)
                            .ThenBy(t => t.Id, StringComparer.Ordinal)
                            .Select(t => t.Id)
                            .ToList();

        var factions = SeedFactionsFromNodesSorted(nodeIds);

        var fids = factions.Select(f => f.FactionId).OrderBy(id => id, StringComparer.Ordinal).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("FactionId\tHomeNodeId\tRoleTag");
        foreach (var f in factions)
        {
            sb.Append(f.FactionId).Append('\t')
              .Append(f.HomeNodeId).Append('\t')
              .Append(f.RoleTag).Append('\n');
        }

        sb.Append("Matrix\t");
        sb.Append(string.Join('\t', fids));
        sb.Append('\n');

        foreach (var row in fids)
        {
            sb.Append(row);
            foreach (var col in fids)
            {
                int v = 0;
                if (!string.Equals(row, col, StringComparison.Ordinal))
                {
                    var fr = factions.First(ff => string.Equals(ff.FactionId, row, StringComparison.Ordinal));
                    v = fr.Relations.TryGetValue(col, out var vv) ? vv : 0;
                }
                sb.Append('\t').Append(v);
            }
            sb.Append('\n');
        }

        return sb.ToString();
    }
}
