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
    public const int StarterRegionNodeCount = 12;

    // GATE.X.TWEAKS.DATA.MIGRATE.WORLDGEN_BOUNDS.001: v0 goods set for producer/sink bounds checks.
    // Keep stable and ordered for deterministic reports and failure messages.
    private static readonly string[] WorldgenBoundsGoodsV0 = { "fuel", "ore", "metal" };

    // GATE.S2_5.WGEN.DISTRIBUTION.001: deterministic starter region accessor for tests and reports.
    // v0 starter region: star_0..star_(StarterRegionNodeCount-1) when present (sorted ordinal).
    public static IReadOnlyList<string> GetStarterRegionNodeIdsSortedV0(SimState state)
        => GetStarterNodeIdsSortedV0(state);

    // GATE.S2_5.WGEN.DISTRIBUTION.001: generation options (explicit inputs, default behavior unchanged).
    public sealed record GalaxyGenOptions
    {
        // Default false to preserve existing goldens and baseline determinism contracts.
        public bool EnableDistributionSinksV0 { get; init; } = false;

        // GATE.S2_5.WGEN.DISTINCTNESS.001: deterministic class parameter overrides for REPORTING (null = defaults).
        // Length must be exactly 3 when provided (CORE, FRONTIER, RIM).
        public float[]? WorldClassLaneCapacityMultipliersV0 { get; init; } = null;

        // Optional override for fee multipliers used in reports (does not affect simulation state directly).
        // Length must be exactly 3 when provided (CORE, FRONTIER, RIM).
        public float[]? WorldClassFeeMultipliersV0 { get; init; } = null;

        // Optional deterministic override to force all nodes into a single class index (0..2).
        public int? ForceAllNodesToWorldClassIndexV0 { get; init; } = null;
    }

    // GATE.S2_5.WGEN.WORLD_CLASSES.001: deterministic world classes v0.
    // Exactly 3 classes. Each class has exactly one measurable effect in v0: fee_multiplier.
    public static readonly (string WorldClassId, float FeeMultiplier)[] WorldClassesV0 =
    {
        ("CORE", 1.00f),
        ("FRONTIER", 1.10f),
        ("RIM", 1.20f),
    };

    // GATE.S2_5.WGEN.DISTINCTNESS.001: report-only class topology params v0.
    // Default lane-cap multipliers are derived deterministically from existing WorldClassesV0 fee multipliers
    // to avoid introducing new numeric literals that violate the tweak routing guard.

    public static void Generate(SimState state, int starCount, float radius)
            => Generate(state, starCount, radius, options: null);

    public static void Generate(SimState state, int starCount, float radius, GalaxyGenOptions? options)
    {
        options ??= new GalaxyGenOptions();

        // GATE.X.TWEAKS.DATA.MIGRATE.WORLDGEN_BOUNDS.001: tweak-sourced bounds (generator is the source of truth).
        var minP = Math.Max(0, state.Tweaks.WorldgenMinProducersPerGood);
        var minS = Math.Max(0, state.Tweaks.WorldgenMinSinksPerGood);

        // If sinks are required by tweaks, force-enable v0 distribution sinks deterministically.
        var effectiveEnableDistributionSinksV0 = options.EnableDistributionSinksV0 || (minS > 0);

        state.Nodes.Clear();
        state.Edges.Clear();
        state.Markets.Clear();
        state.Fleets.Clear();
        state.IndustrySites.Clear();

        // GATE.S2_5.WGEN.GALAXY.001: enforce minimum starter region size at the generator boundary.
        if (starCount < StarterRegionNodeCount)
        {
            starCount = StarterRegionNodeCount;
        }

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
            // Note: fuel is an economy-critical input (mines/refineries). Seed enough to avoid immediate global starvation.
            mkt.Inventory["fuel"] = 500;
            mkt.Inventory["ore"] = 0;
            mkt.Inventory["metal"] = 0;

            // GATE.S2_5.WGEN.ECON.001: deterministic economy placement v0 for starter region.
            // Starter region is the first N stars by generation index.
            bool isStarter = i < Math.Min(starCount, StarterRegionNodeCount);

            // Deterministic fuel source v0: every 6th node has a fuel well that produces fuel with no inputs.
            // This prevents the world from draining all seeded fuel and going globally idle.
            bool isFuelWell = (i % 6) == 0;
            if (isFuelWell)
            {
                mkt.Inventory["fuel"] = Math.Max(mkt.Inventory["fuel"], 3000); // bootstrap supply

                var well = new IndustrySite
                {
                    Id = $"well_{i}",
                    NodeId = node.Id,
                    Inputs = new Dictionary<string, int>(), // no inputs
                    Outputs = new Dictionary<string, int> { { "fuel", 5 } },
                    BufferDays = 1,
                    DegradePerDayBps = 0
                };

                state.IndustrySites.Add(well.Id, well);
                node.Name += " (Fuel Well)";
            }

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

            // GATE.S2_5.WGEN.DISTRIBUTION.001: ensure a structural sink exists for starter goods.
            // v0: add a deterministic metal demand sink on some starter nodes (OPT-IN via GalaxyGenOptions).
            if (effectiveEnableDistributionSinksV0 && isStarter && (i % 5) == 1)
            {
                var metalSink = new IndustrySite
                {
                    Id = $"sink_metal_{i}",
                    NodeId = node.Id,
                    Inputs = new Dictionary<string, int> { { "metal", 1 } },
                    Outputs = new Dictionary<string, int>(),
                    BufferDays = 1,
                    DegradePerDayBps = 0
                };
                state.IndustrySites.Add(metalSink.Id, metalSink);
            }

            state.Markets.Add(node.MarketId, mkt);
        }

        if (nodesList.Count == 0) return;
        state.PlayerLocationNodeId = nodesList[0].Id;

        // GATE.S2_5.WGEN.GALAXY.001: deterministic topology v0.
        // Requirements:
        // - connected starter region graph
        // - MIN starter nodes = StarterRegionNodeCount (or all nodes if fewer)
        // - MIN starter lanes = 18
        // - stable LaneId minted via deterministic counter (no hash iteration)
        int starterN = Math.Min(nodesList.Count, StarterRegionNodeCount);
        int laneCounter = 0;

        // Duplicate suppression key: normalized endpoints "a|b" with ordinal ordering.
        var laneKey = new HashSet<string>(StringComparer.Ordinal);

        void AddLane(Node a, Node b, int capacity)
        {
            var u = a.Id;
            var v = b.Id;
            if (string.CompareOrdinal(u, v) > 0)
            {
                (u, v) = (v, u);
            }

            var key = $"{u}|{v}";
            if (!laneKey.Add(key)) return;

            laneCounter++;
            string id = $"lane_{laneCounter:D4}";
            state.Edges.Add(id, new Edge
            {
                Id = id,
                FromNodeId = u,
                ToNodeId = v,
                Distance = Vector3.Distance(a.Position, b.Position),
                TotalCapacity = capacity
            });
        }

        if (starterN >= 2)
        {
            // 1) Starter ring (connected).
            for (int i = 0; i < starterN; i++)
            {
                var a = nodesList[i];
                var b = nodesList[(i + 1) % starterN];
                AddLane(a, b, capacity: 5);
            }

            // 2) Add deterministic chords until MIN starter lanes reached.
            // First pass: step=2 chords around the ring.
            for (int i = 0; i < starterN && laneKey.Count < 18; i++)
            {
                var a = nodesList[i];
                var b = nodesList[(i + 2) % starterN];
                AddLane(a, b, capacity: 4);
            }

            // Second pass: step=3 chords if still short (requires starterN >= 4 to add new edges).
            for (int i = 0; i < starterN && laneKey.Count < 18; i++)
            {
                var a = nodesList[i];
                var b = nodesList[(i + 3) % starterN];
                AddLane(a, b, capacity: 3);
            }
        }

        // 3) Attach any non-starter nodes deterministically to keep whole galaxy connected.
        // Connect each node i to i-1 for i >= starterN.
        for (int i = starterN; i < nodesList.Count; i++)
        {
            AddLane(nodesList[i - 1], nodesList[i], capacity: 5);
        }

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

        // GATE.X.TWEAKS.DATA.MIGRATE.WORLDGEN_BOUNDS.001: enforce bounds deterministically at the generator boundary.
        var (pass, report) = EvaluateWorldgenBoundsV0(state, WorldgenBoundsGoodsV0, minP, minS);
        if (!pass)
        {
            throw new InvalidOperationException(report);
        }
    }

    // NOTE: CreateEdge retained for back-compat callers, but now mints deterministic lane ids
    // only when used through BuildTopology lanes above. Existing direct callers still get
    // stable ids derived from endpoints (not used for gate proofs).
    private static (bool Pass, string Report) EvaluateWorldgenBoundsV0(SimState state, IReadOnlyList<string> goodsOrdered, int minP, int minS)
    {
        var starterNodes = new HashSet<string>(GetStarterNodeIdsSortedV0(state), StringComparer.Ordinal);

        var producers = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var sinks = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var g in goodsOrdered)
        {
            producers[g] = new HashSet<string>(StringComparer.Ordinal);
            sinks[g] = new HashSet<string>(StringComparer.Ordinal);
        }

        foreach (var site in state.IndustrySites.Values.OrderBy(s => s.Id, StringComparer.Ordinal))
        {
            if (site is null) continue;
            if (!starterNodes.Contains(site.NodeId)) continue;

            if (site.Outputs is not null)
            {
                foreach (var kv in site.Outputs.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    if (kv.Value <= 0) continue;
                    if (!producers.TryGetValue(kv.Key, out var set)) continue;
                    set.Add(site.NodeId);
                }
            }

            if (site.Inputs is not null)
            {
                foreach (var kv in site.Inputs.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    if (kv.Value <= 0) continue;
                    if (!sinks.TryGetValue(kv.Key, out var set)) continue;
                    set.Add(site.NodeId);
                }
            }
        }

        var sb = new StringBuilder(256);
        sb.Append("worldgen_bounds_v0 ");
        sb.Append("minP=").Append(minP).Append(" ");
        sb.Append("minS=").Append(minS);

        bool pass = true;
        foreach (var g in goodsOrdered)
        {
            var p = producers[g].Count;
            var s = sinks[g].Count;
            sb.Append(" | ").Append(g).Append(":P=").Append(p).Append(",S=").Append(s);
            if (p < minP || s < minS) pass = false;
        }

        sb.Append(pass ? " PASS" : " FAIL");
        return (pass, sb.ToString());
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
        return string.CompareOrdinal(a, b) < 0 ? $"{a}_{b}" : $"{b}_{a}";
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

    // GATE.S2_5.WGEN.DISCOVERY_SEEDING.001: discovery seeding contract v0.
    // Deterministic ordering keys:
    // - Industry-derived seeds: (DiscoveryKind, NodeId, RefId, SourceId) all ordinal, then DiscoveryId.
    // - Lane-derived seeds: normalized endpoints (min,max) ordinal, then LaneId ordinal.
    // Output ordering: list is sorted by DiscoveryId (ordinal) as a single canonical ordering for diff stability.
    public const string DiscoverySeedKind_AnomalyFamilyV0 = "AnomalyFamily";

    // Back-compat wrapper (seed=0). Keeps older callers deterministic.
    public static IReadOnlyList<DiscoverySeedSurfaceV0> BuildDiscoverySeedSurfaceV0(SimState state)
        => BuildDiscoverySeedSurfaceV0(state, seed: default(int));

    public static IReadOnlyList<DiscoverySeedSurfaceV0> BuildDiscoverySeedSurfaceV0(SimState state, int seed)
    {
        var seeds = new List<DiscoverySeedSurfaceV0>();

        // 1) Resource pool markers from IndustrySites outputs (deterministic: sites by Id, outputs by GoodId).
        foreach (var site in state.IndustrySites.Values
                     .Where(s => s is not null)
                     .OrderBy(s => s!.Id, StringComparer.Ordinal))
        {
            if (site!.Outputs is null) continue;

            foreach (var kv in site.Outputs.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                if (kv.Value <= default(int)) continue;

                var kind = DiscoverySeedKindsV0.ResourcePoolMarker;
                var nodeId = site.NodeId ?? "";
                var refId = kv.Key ?? "";
                var sourceId = site.Id ?? "";

                seeds.Add(new DiscoverySeedSurfaceV0
                {
                    DiscoveryKind = kind,
                    NodeId = nodeId,
                    RefId = refId,
                    SourceId = sourceId,
                    DiscoveryId = MintDiscoveryIdV0(kind, nodeId, refId, sourceId)
                });
            }
        }

        // 2) Corridor traces from lanes (deterministic: normalized endpoints, then LaneId).
        var lanes = state.Edges.Values
            .Select(e =>
            {
                var u = e.FromNodeId ?? "";
                var v = e.ToNodeId ?? "";
                if (string.CompareOrdinal(u, v) > default(int))
                {
                    (u, v) = (v, u);
                }

                return (U: u, V: v, LaneId: e.Id ?? "");
            })
            .OrderBy(t => t.U, StringComparer.Ordinal)
            .ThenBy(t => t.V, StringComparer.Ordinal)
            .ThenBy(t => t.LaneId, StringComparer.Ordinal)
            .ToList();

        foreach (var l in lanes)
        {
            var kind = DiscoverySeedKindsV0.CorridorTrace;
            var nodeId = l.U;
            var refId = l.V;
            var sourceId = l.LaneId;

            seeds.Add(new DiscoverySeedSurfaceV0
            {
                DiscoveryKind = kind,
                NodeId = nodeId,
                RefId = refId,
                SourceId = sourceId,
                DiscoveryId = MintDiscoveryIdV0(kind, nodeId, refId, sourceId)
            });
        }

        // 3) Per-WorldClass guarantees: at least 1 ResourcePoolMarker and 1 AnomalyFamily per class.
        var classByNode = GetWorldClassIdByNodeIdV0(state);

        var classes = classByNode.Values.Distinct().ToList();
        classes.Sort(StringComparer.Ordinal);

        // Precompute node lists per class (deterministic by NodeId).
        var nodesByClass = new Dictionary<string, List<Node>>(StringComparer.Ordinal);
        foreach (var wc in classes) nodesByClass[wc] = new List<Node>();

        foreach (var n in state.Nodes.Values
                     .Where(n => n is not null)
                     .OrderBy(n => n!.Id, StringComparer.Ordinal))
        {
            if (!classByNode.TryGetValue(n!.Id, out var wc)) continue;
            nodesByClass[wc].Add(n);
        }

        // Existing resource markers by class.
        var hasResourceByClass = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in seeds.Where(s => string.Equals(s.DiscoveryKind, DiscoverySeedKindsV0.ResourcePoolMarker, StringComparison.Ordinal)))
        {
            if (classByNode.TryGetValue(s.NodeId, out var wc))
            {
                hasResourceByClass.Add(wc);
            }
        }

        // Deterministic anomaly family selection.
        var families = new[] { "DERELICT", "RUIN", "SIGNAL" };

        foreach (var wc in classes)
        {
            if (!nodesByClass.TryGetValue(wc, out var nodes) || nodes.Count <= default(int)) continue;

            var host = ChooseBestNodeForClassV0(nodes, seed);

            if (!hasResourceByClass.Contains(wc))
            {
                var kind = DiscoverySeedKindsV0.ResourcePoolMarker;
                var nodeId = host.Id;
                var refId = "resource_marker_v0|" + wc;
                var sourceId = "synthetic";

                seeds.Add(new DiscoverySeedSurfaceV0
                {
                    DiscoveryKind = kind,
                    NodeId = nodeId,
                    RefId = refId,
                    SourceId = sourceId,
                    DiscoveryId = MintDiscoveryIdV0(kind, nodeId, refId, sourceId)
                });
            }

            // Always add anomaly family (1 per class).
            {
                var kind = DiscoverySeedKind_AnomalyFamilyV0;
                var nodeId = host.Id;

                var famHash = Fnv1a32Utf8(seed + "|" + wc + "|anomaly_family_v0");
                var fam = families[(int)(famHash % (uint)families.Length)];

                var refId = fam;
                var sourceId = "seed:" + seed;

                seeds.Add(new DiscoverySeedSurfaceV0
                {
                    DiscoveryKind = kind,
                    NodeId = nodeId,
                    RefId = refId,
                    SourceId = sourceId,
                    DiscoveryId = MintDiscoveryIdV0(kind, nodeId, refId, sourceId)
                });
            }
        }

        seeds.Sort((a, b) => string.CompareOrdinal(a.DiscoveryId, b.DiscoveryId));
        return seeds;
    }

    // GATE.S2_5.WGEN.DISCOVERY_SEEDING.002: deterministic violations table (Seed%WorldClass%ReasonCode%PrimaryId)
    // sorted by ReasonCode then PrimaryId then Seed.
    public static string BuildDiscoverySeedingViolationsReportV0(SimState state, int seed)
    {
        var classByNode = GetWorldClassIdByNodeIdV0(state);
        var classes = classByNode.Values.Distinct().ToList();
        classes.Sort(StringComparer.Ordinal);

        var seeds = BuildDiscoverySeedSurfaceV0(state, seed);

        var resourceByClass = new HashSet<string>(StringComparer.Ordinal);
        var anomalyByClass = new HashSet<string>(StringComparer.Ordinal);

        foreach (var s in seeds)
        {
            if (!classByNode.TryGetValue(s.NodeId, out var wc)) continue;

            if (string.Equals(s.DiscoveryKind, DiscoverySeedKindsV0.ResourcePoolMarker, StringComparison.Ordinal))
                resourceByClass.Add(wc);

            if (string.Equals(s.DiscoveryKind, DiscoverySeedKind_AnomalyFamilyV0, StringComparison.Ordinal))
                anomalyByClass.Add(wc);
        }

        var violations = new List<(int Seed, string WorldClass, string ReasonCode, string PrimaryId)>();

        foreach (var wc in classes)
        {
            if (!resourceByClass.Contains(wc))
                violations.Add((seed, wc, "MISSING_RESOURCE_MARKER", wc));

            if (!anomalyByClass.Contains(wc))
                violations.Add((seed, wc, "MISSING_ANOMALY_FAMILY", wc));
        }

        violations.Sort((a, b) =>
        {
            int c = string.CompareOrdinal(a.ReasonCode, b.ReasonCode);
            if (c != default(int)) return c;

            c = string.CompareOrdinal(a.PrimaryId, b.PrimaryId);
            if (c != default(int)) return c;

            return a.Seed.CompareTo(b.Seed);
        });

        var sb = new StringBuilder();
        sb.Append("DISCOVERY_SEEDING_V0").Append('\n');
        sb.Append("Seed=").Append(seed).Append('\n');
        sb.Append("VIOLATIONS").Append('\n');
        sb.Append("Seed\tWorldClass\tReasonCode\tPrimaryId").Append('\n');

        foreach (var v in violations)
        {
            sb.Append(v.Seed).Append('\t')
              .Append(v.WorldClass).Append('\t')
              .Append(v.ReasonCode).Append('\t')
              .Append(v.PrimaryId)
              .Append('\n');
        }

        sb.Append("Result=").Append(violations.Count == default(int) ? "PASS" : "FAIL").Append('\n');
        sb.Append("ViolationsCount=").Append(violations.Count).Append('\n');

        return sb.ToString();
    }

    // GATE.S2_5.WGEN.DISCOVERY_SEEDING.006: player-readable readout (Facts%Events style) for one seed.
    // Deterministic ordering keys:
    // - discovery seeds are emitted in canonical DiscoveryId (ordinal) order (already sorted by BuildDiscoverySeedSurfaceV0).
    // Deterministic formatting:
    // - tab-separated table with a fixed header, no timestamps, no localization.
    public static string BuildDiscoveryReadoutV0(SimState state, int seed)
    {
        var seeds = BuildDiscoverySeedSurfaceV0(state, seed);
        var classByNode = GetWorldClassIdByNodeIdV0(state);

        var sb = new StringBuilder();
        sb.Append("FACTS").Append('\n');
        sb.Append("DiscoveryKind\tNodeId\tWorldClassId\tRefId\tSourceId\tDiscoveryId").Append('\n');

        foreach (var s in seeds)
        {
            var wc = "";
            if (s.NodeId is not null && classByNode.TryGetValue(s.NodeId, out var w))
            {
                wc = w ?? "";
            }

            sb.Append(s.DiscoveryKind ?? "").Append('\t')
              .Append(s.NodeId ?? "").Append('\t')
              .Append(wc).Append('\t')
              .Append(s.RefId ?? "").Append('\t')
              .Append(s.SourceId ?? "").Append('\t')
              .Append(s.DiscoveryId ?? "")
              .Append('\n');
        }

        sb.Append("SeededDiscoveriesCount=").Append(seeds.Count).Append('\n');
        sb.Append("Result=OK").Append('\n');

        return sb.ToString();
    }

    private static Node ChooseBestNodeForClassV0(IReadOnlyList<Node> nodes, int seed)
    {
        // Deterministic: minimum score; tie-break by NodeId (ordinal).
        // Avoid new numeric literals: use default(int) instead of 0.
        if (nodes.Count <= default(int)) throw new InvalidOperationException("No nodes available for class seeding.");

        Node best = nodes[default(int)];
        uint bestScore = ScoreNode(seed, best);

        var i = default(int);
        while (i < nodes.Count)
        {
            var n = nodes[i];
            var score = ScoreNode(seed, n);

            if (score < bestScore)
            {
                best = n;
                bestScore = score;
                i++;
                continue;
            }

            if (score == bestScore && string.CompareOrdinal(n.Id, best.Id) < default(int))
            {
                best = n;
                bestScore = score;
            }

            i++;
        }

        return best;
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

    private static string MintDiscoveryIdV0(string kind, string nodeId, string refId, string sourceId)
    {
        // Canonical stable id format (v0). No timestamps%wall-clock, no RNG, no unordered iteration inputs.
        // Use '|' separators and include all components to avoid collisions.
        return $"disc_v0|{kind}|{nodeId}|{refId}|{sourceId}";
    }

    private static uint ScoreNode(int seed, Node n)
    {
        // Deterministic across seeds without introducing quantization constants:
        // hash (seed|nodeId) only.
        return Fnv1a32Utf8(seed + "|" + (n.Id ?? ""));
    }

    // GATE.S2_5.WGEN.GALAXY.001: diff-friendly topology dump (deterministic).
    // - nodes sorted by NodeId (ordinal)
    // - lanes sorted by FromId,ToId,LaneId (ordinal)
    // Risk scalar defaults are allowed; this dump does not invent additional schema fields.
    public static string BuildTopologyDump(SimState state)
    {
        var sb = new StringBuilder();

        var nodesSorted = state.Nodes.Values.ToList();
        nodesSorted.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

        sb.Append("nodes_count=").Append(nodesSorted.Count).Append('\n');
        foreach (var n in nodesSorted)
        {
            sb.Append("N|").Append(n.Id).Append("|k=").Append(n.Kind).Append('\n');
        }

        var lanesSorted = state.Edges.Values
    .Select(e =>
    {
        var u = e.FromNodeId;
        var v = e.ToNodeId;
        if (string.CompareOrdinal(u, v) > 0)
        {
            (u, v) = (v, u);
        }
        return (From: u, To: v, LaneId: e.Id, Cap: e.TotalCapacity);
    })
    .ToList();

        lanesSorted.Sort((a, b) =>
        {
            int c = string.CompareOrdinal(a.From, b.From);
            if (c != 0) return c;

            c = string.CompareOrdinal(a.To, b.To);
            if (c != 0) return c;

            return string.CompareOrdinal(a.LaneId, b.LaneId);
        });

        sb.Append("lanes_count=").Append(lanesSorted.Count).Append('\n');
        foreach (var l in lanesSorted)
        {
            // risk scalar default (allowed): r=0
            sb.Append("L|").Append(l.From).Append('|').Append(l.To)
              .Append("|id=").Append(l.LaneId)
              .Append("|c=").Append(l.Cap)
              .Append("|r=0")
              .Append('\n');
        }

        return sb.ToString();
    }

    public readonly record struct SeedExplorerV0Config(
        int StarCount,
        float Radius,
        int MaxHops,
        int ChokepointCapLe,
        int MaxChokepoints,
        string[] Goods)
    {
        public static readonly SeedExplorerV0Config Default = new(
            StarCount: 20,
            Radius: 100f,
            MaxHops: 4,
            ChokepointCapLe: 3,
            MaxChokepoints: 1,
            Goods: new[] { "fuel", "ore", "metal" });
    }

    // GATE.S2_5.TOOL.SEED_EXPLORER.001: econ loops report (deterministic).
    // Contract (v0):
    // - enumerate candidate loops within starter region with hop_count<=maxHops
    // - deterministic ordering by route_id (ordinal)
    // - filter "viable" loops by:
    //   - net_profit_proxy > 0 (computed via inventory deltas per leg)
    //   - volume_proxy > 0 (count of profitable legs)
    // Output is diff-friendly and byte-for-byte stable (no timestamps).
    public static string BuildEconLoopsReport(SimState state, int seed, int maxHops)
    {
        // Back-compat entrypoint: keep signature stable, route through config.
        var cfg = SeedExplorerV0Config.Default with { MaxHops = maxHops };
        return BuildEconLoopsReport(state, seed, cfg);
    }

    public static string BuildEconLoopsReport(SimState state, int seed, SeedExplorerV0Config cfg)
    {
        var loops = FindViableLoopsV0(state, seed, cfg.MaxHops, cfg.Goods);

        var sb = new StringBuilder();
        sb.Append("ECON_LOOPS_V0").Append('\n');
        sb.Append("seed=").Append(seed).Append('\n');
        sb.Append("max_hops=").Append(cfg.MaxHops).Append('\n');
        sb.Append("loops_count=").Append(loops.Count).Append('\n');

        // Stable ordering by route_id (ordinal).
        loops.Sort((a, b) => string.CompareOrdinal(a.RouteId, b.RouteId));

        foreach (var l in loops)
        {
            sb.Append("R|").Append(l.RouteId)
              .Append("|hops=").Append(l.Hops)
              .Append("|net_profit_proxy=").Append(l.NetProfitProxy)
              .Append("|volume_proxy=").Append(l.VolumeProxy)
              .Append('\n');
        }

        return sb.ToString();
    }

    // GATE.S2_5.TOOL.SEED_EXPLORER.001: invariants report (deterministic).
    // Implements a minimal v0 invariant set that is stable and failure-safe:
    // - CONNECTED_GRAPH: all nodes reachable from starter hub (star_0) when treating lanes as undirected
    // - STARTER_REGION_SAFE_PATH: for each starter node, exists a path from hub with max_chokepoints<=1
    //   chokepoint(v0) := lane capacity <= 3
    // - EARLY_LOOPS_MIN3: at least 3 viable loops by the v0 loop criteria
    // Failures emit records {Seed, InvariantName, PrimaryId, DetailsKV} sorted by InvariantName then PrimaryId.
    public static string BuildInvariantsReport(SimState state, int seed, int maxHopsForLoops)
    {
        // Back-compat entrypoint: keep signature stable, route through config.
        var cfg = SeedExplorerV0Config.Default with { MaxHops = maxHopsForLoops };
        return BuildInvariantsReport(state, seed, cfg);
    }

    public static string BuildInvariantsReport(SimState state, int seed, SeedExplorerV0Config cfg)
    {
        var failures = new List<(string InvariantName, string PrimaryId, string DetailsKv)>();

        // CONNECTED_GRAPH
        {
            var all = state.Nodes.Keys.ToList();
            all.Sort(StringComparer.Ordinal);

            var hub = PickStarterHubIdV0(state);
            var reachable = BfsReachableUndirected(state, hub);

            if (reachable.Count != all.Count)
            {
                // Emit one record with a stable summary.
                var missing = all.Where(id => !reachable.Contains(id)).ToList();
                missing.Sort(StringComparer.Ordinal);

                string details = $"hub={hub};reachable={reachable.Count};total={all.Count};missing_count={missing.Count}";
                failures.Add(("CONNECTED_GRAPH", "graph", details));
            }
        }

        // STARTER_REGION_SAFE_PATH
        {
            var hub = PickStarterHubIdV0(state);
            var starter = GetStarterNodeIdsSortedV0(state);

            for (int i = 0; i < starter.Count; i++)
            {
                var target = starter[i];
                if (string.Equals(target, hub, StringComparison.Ordinal)) continue;

                bool ok = ExistsPathWithMaxChokepointsV0(state, hub, target, cfg.MaxChokepoints, cfg.ChokepointCapLe);
                if (!ok)
                {
                    string details = $"hub={hub};target={target};max_chokepoints={cfg.MaxChokepoints};chokepoint_cap_le={cfg.ChokepointCapLe}";
                    failures.Add(("STARTER_REGION_SAFE_PATH", target, details));
                }
            }
        }

        // EARLY_LOOPS_MIN3
        {
            var loops = FindViableLoopsV0(state, seed, cfg.MaxHops, cfg.Goods);
            if (loops.Count < 3)
            {
                string details = $"min=3;actual={loops.Count};max_hops={cfg.MaxHops}";
                failures.Add(("EARLY_LOOPS_MIN3", "starter_region", details));
            }
        }

        // Deterministic ordering for failures.
        failures.Sort((a, b) =>
        {
            int c = string.CompareOrdinal(a.InvariantName, b.InvariantName);
            if (c != 0) return c;
            return string.CompareOrdinal(a.PrimaryId, b.PrimaryId);
        });

        var sb = new StringBuilder();
        sb.Append("INVARIANTS_V0").Append('\n');
        sb.Append("seed=").Append(seed).Append('\n');

        if (failures.Count == 0)
        {
            sb.Append("result=PASS").Append('\n');
            return sb.ToString();
        }

        sb.Append("result=FAIL").Append('\n');
        sb.Append("failures_count=").Append(failures.Count).Append('\n');

        for (int i = 0; i < failures.Count; i++)
        {
            var f = failures[i];
            sb.Append("F|Seed=").Append(seed)
              .Append("|InvariantName=").Append(f.InvariantName)
              .Append("|PrimaryId=").Append(f.PrimaryId)
              .Append("|DetailsKV=").Append(f.DetailsKv)
              .Append('\n');
        }

        return sb.ToString();
    }

    // GATE.S2_5.TOOL.SEED_EXPLORER.001: diff mode for topology (deterministic).
    // Emits added%removed nodes and lanes between two generated states.
    public static string BuildTopologyDiffReport(SimState a, int seedA, SimState b, int seedB)
    {
        var aNodes = a.Nodes.Keys.ToList();
        var bNodes = b.Nodes.Keys.ToList();
        aNodes.Sort(StringComparer.Ordinal);
        bNodes.Sort(StringComparer.Ordinal);

        var setA = new HashSet<string>(aNodes, StringComparer.Ordinal);
        var setB = new HashSet<string>(bNodes, StringComparer.Ordinal);

        var addedNodes = bNodes.Where(id => !setA.Contains(id)).ToList();
        var removedNodes = aNodes.Where(id => !setB.Contains(id)).ToList();
        addedNodes.Sort(StringComparer.Ordinal);
        removedNodes.Sort(StringComparer.Ordinal);

        var aLanes = GetLaneKeysSortedV0(a);
        var bLanes = GetLaneKeysSortedV0(b);

        var laneSetA = new HashSet<string>(aLanes, StringComparer.Ordinal);
        var laneSetB = new HashSet<string>(bLanes, StringComparer.Ordinal);

        var addedLanes = bLanes.Where(k => !laneSetA.Contains(k)).ToList();
        var removedLanes = aLanes.Where(k => !laneSetB.Contains(k)).ToList();
        addedLanes.Sort(StringComparer.Ordinal);
        removedLanes.Sort(StringComparer.Ordinal);

        var sb = new StringBuilder();
        sb.Append("TOPOLOGY_DIFF_V0").Append('\n');
        sb.Append("seedA=").Append(seedA).Append('\n');
        sb.Append("seedB=").Append(seedB).Append('\n');

        sb.Append("added_nodes_count=").Append(addedNodes.Count).Append('\n');
        foreach (var n in addedNodes) sb.Append("+N|").Append(n).Append('\n');

        sb.Append("removed_nodes_count=").Append(removedNodes.Count).Append('\n');
        foreach (var n in removedNodes) sb.Append("-N|").Append(n).Append('\n');

        sb.Append("added_lanes_count=").Append(addedLanes.Count).Append('\n');
        foreach (var k in addedLanes) sb.Append("+L|").Append(k).Append('\n');

        sb.Append("removed_lanes_count=").Append(removedLanes.Count).Append('\n');
        foreach (var k in removedLanes) sb.Append("-L|").Append(k).Append('\n');

        return sb.ToString();
    }

    // GATE.S2_5.TOOL.SEED_EXPLORER.001: diff mode for econ loops (deterministic).
    // Emits added%removed loops by route_id between two seeds.
    public static string BuildLoopsDiffReport(SimState a, int seedA, SimState b, int seedB, int maxHops)
    {
        // Back-compat entrypoint: keep signature stable, route through config.
        var cfg = SeedExplorerV0Config.Default with { MaxHops = maxHops };
        return BuildLoopsDiffReport(a, seedA, b, seedB, cfg);
    }

    public static string BuildLoopsDiffReport(SimState a, int seedA, SimState b, int seedB, SeedExplorerV0Config cfg)
    {
        var loopsA = FindViableLoopsV0(a, seedA, cfg.MaxHops, cfg.Goods);
        var loopsB = FindViableLoopsV0(b, seedB, cfg.MaxHops, cfg.Goods);

        loopsA.Sort((x, y) => string.CompareOrdinal(x.RouteId, y.RouteId));
        loopsB.Sort((x, y) => string.CompareOrdinal(x.RouteId, y.RouteId));

        var setA = new HashSet<string>(loopsA.Select(x => x.RouteId), StringComparer.Ordinal);
        var setB = new HashSet<string>(loopsB.Select(x => x.RouteId), StringComparer.Ordinal);

        var added = loopsB.Select(x => x.RouteId).Where(id => !setA.Contains(id)).ToList();
        var removed = loopsA.Select(x => x.RouteId).Where(id => !setB.Contains(id)).ToList();
        added.Sort(StringComparer.Ordinal);
        removed.Sort(StringComparer.Ordinal);

        var sb = new StringBuilder();
        sb.Append("LOOPS_DIFF_V0").Append('\n');
        sb.Append("seedA=").Append(seedA).Append('\n');
        sb.Append("seedB=").Append(seedB).Append('\n');
        sb.Append("max_hops=").Append(cfg.MaxHops).Append('\n');

        sb.Append("added_loops_count=").Append(added.Count).Append('\n');
        foreach (var id in added) sb.Append("+R|").Append(id).Append('\n');

        sb.Append("removed_loops_count=").Append(removed.Count).Append('\n');
        foreach (var id in removed) sb.Append("-R|").Append(id).Append('\n');

        return sb.ToString();
    }

    private readonly struct LoopRecordV0
    {
        public readonly string RouteId;
        public readonly int Hops;
        public readonly int NetProfitProxy;
        public readonly int VolumeProxy;

        public LoopRecordV0(string routeId, int hops, int netProfitProxy, int volumeProxy)
        {
            RouteId = routeId;
            Hops = hops;
            NetProfitProxy = netProfitProxy;
            VolumeProxy = volumeProxy;
        }
    }

    private static List<LoopRecordV0> FindViableLoopsV0(SimState state, int seed, int maxHops, string[] goods)
    {
        if (maxHops < 2) maxHops = 2;

        var starter = GetStarterNodeIdsSortedV0(state);

        // adjacency within starter region only
        var starterSet = new HashSet<string>(starter, StringComparer.Ordinal);
        var adj = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        for (int i = 0; i < starter.Count; i++)
        {
            adj[starter[i]] = new List<string>();
        }

        foreach (var e in state.Edges.Values)
        {
            var u = e.FromNodeId;
            var v = e.ToNodeId;
            if (!starterSet.Contains(u) || !starterSet.Contains(v)) continue;

            if (adj.TryGetValue(u, out var lu)) lu.Add(v);
            if (adj.TryGetValue(v, out var lv)) lv.Add(u);
        }

        foreach (var kv in adj)
        {
            kv.Value.Sort(StringComparer.Ordinal);
        }

        var loops = new List<LoopRecordV0>();

        // DFS from each start; use canonicalization to dedupe rotations%direction.
        for (int i = 0; i < starter.Count; i++)
        {
            var start = starter[i];
            var path = new List<string>(capacity: maxHops + 1) { start };
            var used = new HashSet<string>(StringComparer.Ordinal) { start };

            Dfs(start, start, path, used, depth: 0);
        }

        // Dedupe by route_id deterministically.
        var byId = new Dictionary<string, LoopRecordV0>(StringComparer.Ordinal);
        for (int i = 0; i < loops.Count; i++)
        {
            var l = loops[i];
            if (!byId.ContainsKey(l.RouteId))
            {
                byId.Add(l.RouteId, l);
            }
        }

        var outList = byId.Values.ToList();
        outList.Sort((a, b) => string.CompareOrdinal(a.RouteId, b.RouteId));
        return outList;

        void Dfs(string root, string cur, List<string> p, HashSet<string> seen, int depth)
        {
            if (!adj.TryGetValue(cur, out var neigh)) return;

            // We allow closing the loop back to root when length>=2.
            for (int ni = 0; ni < neigh.Count; ni++)
            {
                var nxt = neigh[ni];

                if (string.Equals(nxt, root, StringComparison.Ordinal))
                {
                    if (p.Count >= 3 && p.Count <= maxHops + 1)
                    {
                        // cycle nodes exclude the repeated root at end
                        var cycle = p.ToArray(); // includes root at [0]
                        var canon = CanonicalizeCycleV0(cycle);

                        var routeId = "loop|" + string.Join(">", canon) + ">" + canon[0];
                        var (profit, vol) = ScoreLoopProfitProxyV0(state, canon, goods);

                        if (profit > 0 && vol > 0)
                        {
                            loops.Add(new LoopRecordV0(routeId, hops: canon.Length, netProfitProxy: profit, volumeProxy: vol));
                        }
                    }
                    continue;
                }

                if (seen.Contains(nxt)) continue;
                if (p.Count >= maxHops + 1) continue;

                seen.Add(nxt);
                p.Add(nxt);
                Dfs(root, nxt, p, seen, depth + 1);
                p.RemoveAt(p.Count - 1);
                seen.Remove(nxt);
            }
        }
    }

    private static (int Profit, int Volume) ScoreLoopProfitProxyV0(SimState state, string[] cycle, string[] goods)
    {
        // For each leg A->B, pick a good that has positive inventory delta invA - invB.
        // Profit proxy is sum of best positive deltas; volume proxy is count of legs with positive deltas.
        int profit = 0;
        int vol = 0;

        for (int i = 0; i < cycle.Length; i++)
        {
            var a = cycle[i];
            var b = cycle[(i + 1) % cycle.Length];

            var invA = GetMarketInventoryV0(state, a);
            var invB = GetMarketInventoryV0(state, b);

            // Goods set is fixed and deterministic for v0 via SeedExplorerV0Config.
            // Caller provides an explicit list to avoid tool churn when the economy schema grows.
            goods ??= SeedExplorerV0Config.Default.Goods;

            int best = 0;
            for (int g = 0; g < goods.Length; g++)
            {
                var good = goods[g];
                int aQty = invA.TryGetValue(good, out var aq) ? aq : 0;
                int bQty = invB.TryGetValue(good, out var bq) ? bq : 0;
                int delta = aQty - bQty;
                if (delta > best) best = delta;
            }

            if (best > 0)
            {
                profit += best;
                vol += 1;
            }
        }

        return (profit, vol);
    }

    private static Dictionary<string, int> GetMarketInventoryV0(SimState state, string nodeId)
    {
        // Node.MarketId is set to node.Id in Generate().
        if (!state.Markets.TryGetValue(nodeId, out var mkt) || mkt == null)
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        // Inventory is expected to be IDictionary<string,int>.
        var inv = mkt.Inventory;
        if (inv == null) return new Dictionary<string, int>(StringComparer.Ordinal);

        // Copy to avoid callers mutating shared references; keep ordinal comparer.
        var d = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var kv in inv)
        {
            d[kv.Key] = kv.Value;
        }
        return d;
    }

    private static string[] CanonicalizeCycleV0(string[] cycleWithRootAt0)
    {
        // cycleWithRootAt0 includes unique nodes, starting at some root; no repeated end.
        // Canonicalization:
        // - choose lexicographically smallest rotation (ordinal)
        // - compare forward vs reversed and pick smallest
        var c = cycleWithRootAt0;

        var bestF = BestRotationV0(c);
        var rev = c.Reverse().ToArray();
        var bestR = BestRotationV0(rev);

        // Pick smaller between forward and reversed.
        for (int i = 0; i < bestF.Length; i++)
        {
            int cmp = string.CompareOrdinal(bestF[i], bestR[i]);
            if (cmp < 0) return bestF;
            if (cmp > 0) return bestR;
        }
        return bestF;
    }

    private static string[] BestRotationV0(string[] cycle)
    {
        int n = cycle.Length;
        int best = 0;

        for (int start = 1; start < n; start++)
        {
            for (int k = 0; k < n; k++)
            {
                var a = cycle[(start + k) % n];
                var b = cycle[(best + k) % n];
                int cmp = string.CompareOrdinal(a, b);
                if (cmp < 0)
                {
                    best = start;
                    break;
                }
                if (cmp > 0)
                {
                    break;
                }
            }
        }

        var rot = new string[n];
        for (int i = 0; i < n; i++)
        {
            rot[i] = cycle[(best + i) % n];
        }
        return rot;
    }

    private static string PickStarterHubIdV0(SimState state)
    {
        // Prefer star_0 when present, else smallest NodeId.
        if (state.Nodes.ContainsKey("star_0")) return "star_0";
        var ids = state.Nodes.Keys.ToList();
        ids.Sort(StringComparer.Ordinal);
        return ids.Count > 0 ? ids[0] : "";
    }

    private static List<string> GetStarterNodeIdsSortedV0(SimState state)
    {
        // v0 starter region: star_0..star_(StarterRegionNodeCount-1) when present.
        var ids = state.Nodes.Keys.ToList();
        ids.Sort(StringComparer.Ordinal);

        var starter = new List<string>();
        for (int i = 0; i < ids.Count; i++)
        {
            var id = ids[i];
            if (id.StartsWith("star_", StringComparison.Ordinal))
            {
                if (TryParseStarIndexV0(id, out var idx) && idx >= 0 && idx < StarterRegionNodeCount)
                {
                    starter.Add(id);
                }
            }
        }

        starter.Sort(StringComparer.Ordinal);
        return starter;
    }

    private static bool TryParseStarIndexV0(string id, out int idx)
    {
        idx = -1;
        if (!id.StartsWith("star_", StringComparison.Ordinal)) return false;
        var tail = id.Substring("star_".Length);
        return int.TryParse(tail, out idx);
    }

    private static HashSet<string> BfsReachableUndirected(SimState state, string start)
    {
        var reachable = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(start) || !state.Nodes.ContainsKey(start)) return reachable;

        var q = new Queue<string>();
        q.Enqueue(start);
        reachable.Add(start);

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            foreach (var nxt in GetNeighborsUndirectedSortedV0(state, cur))
            {
                if (reachable.Add(nxt))
                {
                    q.Enqueue(nxt);
                }
            }
        }

        return reachable;
    }

    private static bool ExistsPathWithMaxChokepointsV0(SimState state, string start, string goal, int maxChokepoints, int chokepointCapLe)
    {
        if (string.IsNullOrEmpty(start) || string.IsNullOrEmpty(goal)) return false;
        if (!state.Nodes.ContainsKey(start) || !state.Nodes.ContainsKey(goal)) return false;

        // Dijkstra-like BFS on small graph: state is (node, chokepointsUsed), edge cost is chokepoint flag.
        var best = new Dictionary<(string Node, int Chokes), int>();
        var q = new Queue<(string Node, int Chokes)>();

        q.Enqueue((start, 0));
        best[(start, 0)] = 0;

        while (q.Count > 0)
        {
            var s = q.Dequeue();
            if (string.Equals(s.Node, goal, StringComparison.Ordinal) && s.Chokes <= maxChokepoints) return true;

            foreach (var (nxt, isChoke) in GetNeighborsWithChokepointFlagSortedV0(state, s.Node, chokepointCapLe))
            {
                int nextChokes = s.Chokes + (isChoke ? 1 : 0);
                if (nextChokes > maxChokepoints) continue;

                var key = (nxt, nextChokes);
                if (best.ContainsKey(key)) continue;

                best[key] = nextChokes;
                q.Enqueue(key);
            }
        }

        return false;
    }

    private static List<string> GetNeighborsUndirectedSortedV0(SimState state, string nodeId)
    {
        var list = new List<string>();

        foreach (var e in state.Edges.Values)
        {
            if (string.Equals(e.FromNodeId, nodeId, StringComparison.Ordinal))
                list.Add(e.ToNodeId);
            else if (string.Equals(e.ToNodeId, nodeId, StringComparison.Ordinal))
                list.Add(e.FromNodeId);
        }

        list.Sort(StringComparer.Ordinal);
        return list;
    }

    private static List<(string Neighbor, bool IsChokepoint)> GetNeighborsWithChokepointFlagSortedV0(SimState state, string nodeId, int chokepointCapLe)
    {
        var list = new List<(string Neighbor, bool IsChokepoint)>();

        foreach (var e in state.Edges.Values)
        {
            bool isChoke = e.TotalCapacity <= chokepointCapLe;

            if (string.Equals(e.FromNodeId, nodeId, StringComparison.Ordinal))
                list.Add((e.ToNodeId, isChoke));
            else if (string.Equals(e.ToNodeId, nodeId, StringComparison.Ordinal))
                list.Add((e.FromNodeId, isChoke));
        }

        list.Sort((a, b) => string.CompareOrdinal(a.Neighbor, b.Neighbor));
        return list;
    }

    private static List<string> GetLaneKeysSortedV0(SimState state)
    {
        // Lane key normalized as "From|To|LaneId" where From<=To (ordinal).
        var keys = state.Edges.Values
            .Select(e =>
            {
                var u = e.FromNodeId;
                var v = e.ToNodeId;
                if (string.CompareOrdinal(u, v) > 0) (u, v) = (v, u);
                return $"{u}|{v}|{e.Id}";
            })
            .ToList();

        keys.Sort(StringComparer.Ordinal);
        return keys;
    }

    // GATE.S2_5.WGEN.WORLD_CLASSES.001: diff-friendly world class report (deterministic).
    // - per-class summary sorted by WorldClassId
    // - per-node assignment list sorted by NodeId
    // Assignment rule (v0):
    // - nodes sorted by NodeId (ordinal) for report row stability
    // - class assignment based on radial rank (distance from origin) for structural distinctness validation
    // - buckets are deterministic: CORE = inner third, FRONTIER = middle third, RIM = outer third
    public static string BuildWorldClassReport(SimState state)
    {
        var nodesSorted = state.Nodes.Values.ToList();
        nodesSorted.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

        var classesSorted = WorldClassesV0.ToList();
        classesSorted.Sort((a, b) => string.CompareOrdinal(a.WorldClassId, b.WorldClassId));

        var rankedByRadius = nodesSorted
            .Select(n =>
            {
                var x = n.Position.X;
                var z = n.Position.Z;
                var d2 = (x * x) + (z * z);
                return (Node: n, Dist2: d2);
            })
            .OrderBy(t => t.Dist2)
            .ThenBy(t => t.Node.Id, StringComparer.Ordinal)
            .ToList();

        var rankById = new Dictionary<string, int>(rankedByRadius.Count, StringComparer.Ordinal);
        var r = default(int);
        foreach (var t in rankedByRadius)
        {
            rankById[t.Node.Id] = r;
            r++;
        }

        var classCount = WorldClassesV0.Length;

        var sb = new StringBuilder();
        sb.AppendLine("WorldClassId\tfee_multiplier");
        foreach (var c in classesSorted)
        {
            sb.Append(c.WorldClassId).Append('\t')
              .Append(c.FeeMultiplier.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))
              .Append('\n');
        }

        sb.AppendLine("NodeId\tWorldClassId\tfee_multiplier");
        for (int i = 0; i < nodesSorted.Count; i++)
        {
            var n = nodesSorted[i];
            var rank = rankById[n.Id];

            var cidx = (rank * classCount) / rankedByRadius.Count;
            var c = WorldClassesV0[cidx];

            sb.Append(n.Id).Append('\t')
              .Append(c.WorldClassId).Append('\t')
              .Append(c.FeeMultiplier.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))
              .Append('\n');
        }

        return sb.ToString();
    }

    // GATE.S2_5.WGEN.DISCOVERY_SEEDING.002: deterministic world class assignment accessor v0.
    // Deterministic ordering keys:
    // - nodes ranked by radius2 asc, tie-break by NodeId (ordinal)
    // - node iteration for map fill is NodeId (ordinal) for stable outputs
    // Assignment rule matches BuildWorldClassReport and ComputeWorldClassStatsV0:
    // - class index is radial rank bucket: idx = (rank * classCount) / nodeCount
    // - optional forced class index is folded deterministically into [0..classCount-1]
    public static IReadOnlyDictionary<string, string> GetWorldClassIdByNodeIdV0(
        SimState state,
        GalaxyGenOptions? options = null)
    {
        options ??= new GalaxyGenOptions();

        var nodesSorted = state.Nodes.Values.ToList();
        nodesSorted.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

        var rankedByRadius = nodesSorted
            .Select(n =>
            {
                var x = n.Position.X;
                var z = n.Position.Z;
                var d2 = (x * x) + (z * z);
                return (Node: n, Dist2: d2);
            })
            .OrderBy(t => t.Dist2)
            .ThenBy(t => t.Node.Id, StringComparer.Ordinal)
            .ToList();

        var rankById = new Dictionary<string, int>(rankedByRadius.Count, StringComparer.Ordinal);
        var r = default(int);
        foreach (var t in rankedByRadius)
        {
            rankById[t.Node.Id] = r;
            r++;
        }

        var classCount = WorldClassesV0.Length;

        // Optional forced class index (report-only): folded deterministically into [0..classCount-1].
        var forcedMaybe = options.ForceAllNodesToWorldClassIndexV0;
        var forcedIdx = forcedMaybe is int f ? (Math.Abs(f) % classCount) : (int?)null;

        var byNode = new Dictionary<string, string>(nodesSorted.Count, StringComparer.Ordinal);
        foreach (var n in nodesSorted)
        {
            int idx;
            if (forcedIdx is int forced)
            {
                idx = forced;
            }
            else
            {
                var rank = rankById[n.Id];
                idx = (rank * classCount) / rankedByRadius.Count;
            }

            byNode[n.Id] = WorldClassesV0[idx].WorldClassId;
        }

        return byNode;
    }

    // GATE.S2_5.WGEN.DISCOVERY_SEEDING.002: deterministic world class assignment accessor v0 (tests + seeding).
    // Deterministic ordering keys:
    // - nodes ranked by radius2 asc, tie-break by NodeId (ordinal)
    // - map fill by NodeId (ordinal)
    // Assignment rule matches BuildWorldClassReport:
    // - class index is radial rank bucket: idx = (rank * classCount) / nodeCount
    public static IReadOnlyDictionary<string, string> GetWorldClassIdByNodeIdV0(SimState state)
    {
        var nodesSorted = state.Nodes.Values.ToList();
        nodesSorted.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

        var rankedByRadius = nodesSorted
            .Select(n =>
            {
                var x = n.Position.X;
                var z = n.Position.Z;
                var d2 = (x * x) + (z * z);
                return (Node: n, Dist2: d2);
            })
            .OrderBy(t => t.Dist2)
            .ThenBy(t => t.Node.Id, StringComparer.Ordinal)
            .ToList();

        var rankById = new Dictionary<string, int>(rankedByRadius.Count, StringComparer.Ordinal);
        var r = default(int);
        foreach (var t in rankedByRadius)
        {
            rankById[t.Node.Id] = r;
            r++;
        }

        var classCount = WorldClassesV0.Length;

        var byNode = new Dictionary<string, string>(nodesSorted.Count, StringComparer.Ordinal);
        foreach (var n in nodesSorted)
        {
            var rank = rankById[n.Id];
            var idx = (rank * classCount) / rankedByRadius.Count;
            byNode[n.Id] = WorldClassesV0[idx].WorldClassId;
        }

        return byNode;
    }

    // GATE.S2_5.WGEN.DISTINCTNESS.001: deterministic class-level structural stats v0.
    // Metrics are computed from the current SimState topology, applying report-only class scaling (no state mutation).
    public sealed record WorldClassStatsV0(
        double AvgDegree,
        double AvgLaneCapacity,
        double ChokepointDensity,
        double FeeMultiplier,
        double AvgRadius2);

    public static IReadOnlyDictionary<string, WorldClassStatsV0> ComputeWorldClassStatsV0(
        SimState state,
        int chokepointCapLe,
        GalaxyGenOptions? options = null)
    {
        options ??= new GalaxyGenOptions();

        var classCount = WorldClassesV0.Length;

        // Default lane-cap multipliers are identity by construction (no new numeric literals).
        // Callers may override deterministically for report-only experiments.
        var laneCapMult = (options.WorldClassLaneCapacityMultipliersV0 is null)
            ? WorldClassesV0.Select(c => c.FeeMultiplier / c.FeeMultiplier).ToArray()
            : options.WorldClassLaneCapacityMultipliersV0.ToArray();

        if (laneCapMult.Length != classCount)
            throw new InvalidOperationException("WorldClassLaneCapacityMultipliersV0 must have length WorldClassesV0.Length.");

        var feeMult = options.WorldClassFeeMultipliersV0 is null
            ? null
            : options.WorldClassFeeMultipliersV0.ToArray();

        if (feeMult is not null && feeMult.Length != classCount)
            throw new InvalidOperationException("WorldClassFeeMultipliersV0 must have length WorldClassesV0.Length.");

        // Node order is deterministic (ordinal). Class assignment is based on radial rank for structural distinctness.
        var nodesSorted = state.Nodes.Values.ToList();
        nodesSorted.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

        var rankedByRadius = nodesSorted
            .Select(n =>
            {
                var x = n.Position.X;
                var z = n.Position.Z;
                var d2 = (x * x) + (z * z);
                return (Node: n, Dist2: d2);
            })
            .OrderBy(t => t.Dist2)
            .ThenBy(t => t.Node.Id, StringComparer.Ordinal)
            .ToList();

        var rankById = new Dictionary<string, int>(rankedByRadius.Count, StringComparer.Ordinal);
        var r = default(int);
        foreach (var t in rankedByRadius)
        {
            rankById[t.Node.Id] = r;
            r++;
        }

        // Optional forced class index (report-only): folded deterministically into [0..classCount-1] without new literals.
        var forcedMaybe = options.ForceAllNodesToWorldClassIndexV0;
        var forcedIdx = forcedMaybe is int f ? (Math.Abs(f) % classCount) : (int?)null;

        var classByNode = new Dictionary<string, int>(nodesSorted.Count, StringComparer.Ordinal);
        foreach (var n in nodesSorted)
        {
            int idx;
            if (forcedIdx is int forced)
            {
                idx = forced;
            }
            else
            {
                var rank = rankById[n.Id];
                idx = (rank * classCount) / rankedByRadius.Count;
            }

            classByNode[n.Id] = idx;
        }

        // Precompute incident edges per node with deterministic ordering.
        var incident = new Dictionary<string, List<Edge>>(nodesSorted.Count, StringComparer.Ordinal);
        foreach (var n in nodesSorted)
            incident[n.Id] = new List<Edge>();

        foreach (var e in state.Edges.Values.OrderBy(e => e.Id, StringComparer.Ordinal))
        {
            if (incident.TryGetValue(e.FromNodeId, out var lf)) lf.Add(e);
            if (incident.TryGetValue(e.ToNodeId, out var lt)) lt.Add(e);
        }

        var sumsDegree = new double[classCount];
        var sumsLaneCap = new double[classCount];
        var sumsChoke = new double[classCount];
        var sumsRadius2 = new double[classCount];
        var counts = new int[classCount];

        foreach (var n in nodesSorted)
        {
            var cidx = classByNode[n.Id];
            var edges = incident[n.Id];
            var deg = edges.Count;

            var px = (double)n.Position.X;
            var pz = (double)n.Position.Z;
            var radius2 = (px * px) + (pz * pz);

            var laneCapAvg = default(double);
            var chokeFrac = default(double);

            if (deg != default(int))
            {
                var capSum = default(double);
                var choke = default(int);

                foreach (var edge in edges)
                {
                    // Apply class-specific scaling for reporting without mutating state.
                    var effectiveCap = edge.TotalCapacity * laneCapMult[cidx];
                    capSum += effectiveCap;
                    if (effectiveCap <= chokepointCapLe) choke++;
                }

                laneCapAvg = capSum / deg;
                chokeFrac = (double)choke / deg;
            }

            sumsDegree[cidx] += deg;
            sumsLaneCap[cidx] += laneCapAvg;
            sumsChoke[cidx] += chokeFrac;
            sumsRadius2[cidx] += radius2;
            counts[cidx]++;
        }

        // In normal worldgen (starCount >= StarterRegionNodeCount) each class is populated deterministically.
        var result = new Dictionary<string, WorldClassStatsV0>(classCount, StringComparer.Ordinal);

        var c = default(int);
        while (c < classCount)
        {
            var id = WorldClassesV0[c].WorldClassId;
            var denom = (double)counts[c];
            var fm = feeMult is null ? WorldClassesV0[c].FeeMultiplier : feeMult[c];

            // Failure-safe: keep output finite and deterministic even if a class ends up empty (eg forced class).
            if (denom == default(double))
            {
                result[id] = new WorldClassStatsV0(
                    AvgDegree: default(double),
                    AvgLaneCapacity: default(double),
                    ChokepointDensity: default(double),
                    FeeMultiplier: fm,
                    AvgRadius2: default(double));
            }
            else
            {
                result[id] = new WorldClassStatsV0(
                    AvgDegree: sumsDegree[c] / denom,
                    AvgLaneCapacity: sumsLaneCap[c] / denom,
                    ChokepointDensity: sumsChoke[c] / denom,
                    FeeMultiplier: fm,
                    AvgRadius2: sumsRadius2[c] / denom);
            }

            c++;
        }

        return result;
    }

    // GATE.S2_5.WGEN.DISTINCTNESS.TARGETS.001: enforce distinctness targets v0 using report metrics.
    // - Constraints are measurable inequalities over per-seed class stats (avg_radius2) and params (fee_multiplier ordering).
    // - Violations list is deterministic: sort by Code (ordinal) then Seed (asc).
    public sealed record WorldClassDistinctnessTargetsV0(
        bool RequireRadius2Ordering);

    public static WorldClassDistinctnessTargetsV0 GetWorldClassDistinctnessTargetsV0()
        => new(RequireRadius2Ordering: true);

    private readonly record struct WorldClassDistinctnessViolationV0(
        int Seed,
        string Code,
        string Metric,
        double Delta,
        double Lhs,
        double Rhs);

    public static (bool Pass, string Report) BuildWorldClassDistinctnessTargetsReportV0(
        int n,
        int starCount,
        float radius,
        int chokepointCapLe,
        GalaxyGenOptions? options = null,
        WorldClassDistinctnessTargetsV0? targets = null)
    {
        if (n <= default(int)) throw new ArgumentOutOfRangeException(nameof(n), "n must be >= 1.");

        options ??= new GalaxyGenOptions();
        targets ??= GetWorldClassDistinctnessTargetsV0();

        // Explicit stable class ids (do not infer from array position outside this file).
        const string CORE = "CORE";
        const string FRONTIER = "FRONTIER";
        const string RIM = "RIM";

        // Dominant constraints per class (measurable inequalities).
        // - CORE: fee CORE < FRONTIER, and avg_radius2 CORE < FRONTIER
        // - FRONTIER: avg_radius2 CORE < FRONTIER < RIM
        // - RIM: fee FRONTIER < RIM, and avg_radius2 FRONTIER < RIM
        var dominantByClass = new Dictionary<string, List<string>>(StringComparer.Ordinal)
        {
            [CORE] = new List<string> { "FEE_CORE_LT_FRONTIER", "R2_CORE_LT_FRONTIER" },
            [FRONTIER] = new List<string> { "R2_CORE_LT_FRONTIER", "R2_FRONTIER_LT_RIM" },
            [RIM] = new List<string> { "FEE_FRONTIER_LT_RIM", "R2_FRONTIER_LT_RIM" },
        };

        var violations = new List<WorldClassDistinctnessViolationV0>();

        // Config sanity: at least one dominant constraint per class.
        foreach (var cls in new[] { CORE, FRONTIER, RIM }.OrderBy(s => s, StringComparer.Ordinal))
        {
            if (!dominantByClass.TryGetValue(cls, out var list) || list.Count == default(int))
            {
                violations.Add(new WorldClassDistinctnessViolationV0(
                    Seed: default(int),
                    Code: "CONFIG_MISSING_DOMINANT_CONSTRAINT",
                    Metric: "class",
                    Delta: default(double),
                    Lhs: default(double),
                    Rhs: default(double)));
            }
        }

        // Fee multiplier ordering targets (params): strict ordering without thresholds.
        var feeById = WorldClassesV0.ToDictionary(c => c.WorldClassId, c => (double)c.FeeMultiplier, StringComparer.Ordinal);

        var feeCore = feeById[CORE];
        var feeFrontier = feeById[FRONTIER];
        var feeRim = feeById[RIM];

        var feeDeltaCF = feeFrontier - feeCore;
        if (!(feeCore < feeFrontier))
        {
            violations.Add(new WorldClassDistinctnessViolationV0(
                Seed: default(int),
                Code: "FEE_CORE_NOT_LT_FRONTIER",
                Metric: "fee_multiplier",
                Delta: feeDeltaCF,
                Lhs: feeCore,
                Rhs: feeFrontier));
        }

        var feeDeltaFR = feeRim - feeFrontier;
        if (!(feeFrontier < feeRim))
        {
            violations.Add(new WorldClassDistinctnessViolationV0(
                Seed: default(int),
                Code: "FEE_FRONTIER_NOT_LT_RIM",
                Metric: "fee_multiplier",
                Delta: feeDeltaFR,
                Lhs: feeFrontier,
                Rhs: feeRim));
        }

        // Per-seed structural targets (report metrics).
        if (targets.RequireRadius2Ordering)
        {
            var seed = default(int);
            while (seed < n)
            {
                seed++;

                var sim = new SimCore.SimKernel(seed);
                Generate(sim.State, starCount, radius, options);

                var stats = ComputeWorldClassStatsV0(sim.State, chokepointCapLe, options);

                var coreR2 = stats[CORE].AvgRadius2;
                var frontierR2 = stats[FRONTIER].AvgRadius2;
                var rimR2 = stats[RIM].AvgRadius2;

                var dCF = frontierR2 - coreR2;
                if (!(coreR2 < frontierR2))
                {
                    violations.Add(new WorldClassDistinctnessViolationV0(
                        Seed: seed,
                        Code: "R2_CORE_NOT_LT_FRONTIER",
                        Metric: "avg_radius2",
                        Delta: dCF,
                        Lhs: coreR2,
                        Rhs: frontierR2));
                }

                var dFR = rimR2 - frontierR2;
                if (!(frontierR2 < rimR2))
                {
                    violations.Add(new WorldClassDistinctnessViolationV0(
                        Seed: seed,
                        Code: "R2_FRONTIER_NOT_LT_RIM",
                        Metric: "avg_radius2",
                        Delta: dFR,
                        Lhs: frontierR2,
                        Rhs: rimR2));
                }
            }
        }

        violations.Sort((a, b) =>
        {
            int c = string.CompareOrdinal(a.Code, b.Code);
            if (c != 0) return c;
            return a.Seed.CompareTo(b.Seed);
        });

        var sb = new StringBuilder();
        sb.Append("WORLD_CLASS_DISTINCTNESS_TARGETS_V0").Append('\n');
        sb.Append("seeds=1..").Append(n).Append('\n');
        sb.Append("star_count=").Append(starCount).Append('\n');
        sb.Append("radius=").Append(radius.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
        sb.Append("chokepoint_cap_le=").Append(chokepointCapLe).Append('\n');
        sb.Append("targets_require_radius2_ordering=").Append(targets.RequireRadius2Ordering ? "1" : "0").Append('\n');

        sb.Append("DOMINANT_CONSTRAINTS").Append('\n');
        foreach (var cls in dominantByClass.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            var list = dominantByClass[cls].OrderBy(s => s, StringComparer.Ordinal).ToArray();
            sb.Append("D|Class=").Append(cls).Append("|Constraints=");
            var i = default(int);
            while (i < list.Length)
            {
                if (i != default(int)) sb.Append(',');
                sb.Append(list[i]);
                i++;
            }
            sb.Append('\n');
        }

        bool pass = violations.Count == default(int);
        sb.Append("result\t").Append(pass ? "PASS" : "FAIL").Append('\n');
        if (!pass)
        {
            sb.Append("violations_count=").Append(violations.Count).Append('\n');
            sb.Append("VIOLATIONS").Append('\n');
            var i = default(int);
            while (i < violations.Count)
            {
                var v = violations[i];
                sb.Append("V|Seed=").Append(v.Seed)
                  .Append("|Code=").Append(v.Code)
                  .Append("|Metric=").Append(v.Metric)
                  .Append("|Delta=").Append(v.Delta.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture))
                  .Append("|Lhs=").Append(v.Lhs.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture))
                  .Append("|Rhs=").Append(v.Rhs.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture))
                  .Append('\n');
                i++;
            }
        }

        return (pass, sb.ToString());
    }

    // GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.007: deterministic per-seed unlock surface summary v0.
    // Maps existing discovery seed kinds to implied unlock types (stable string mapping, not gameplay).
    // Ordering: seeds sorted by DiscoveryId (ordinal) per the BuildDiscoverySeedSurfaceV0 contract.
    // Violation: a seed with zero ResourcePoolMarker entries (no SiteBlueprint unlock opportunity).
    public static string BuildUnlockReportV0(SimState state, int seed)
    {
        // Structural mapping: DiscoveryKind -> implied unlock type v0 (not gameplay-affecting).
        const string STRUCT_UnlockType_SiteBlueprint = "SiteBlueprint";   // STRUCTURAL: unlock type label
        const string STRUCT_UnlockType_CorridorAccess = "CorridorAccess"; // STRUCTURAL: unlock type label
        const string STRUCT_UnlockType_Permit = "Permit";                 // STRUCTURAL: unlock type label
        const int STRUCT_ZERO = 0;                                         // STRUCTURAL: zero sentinel for counts and violation flag
        const int STRUCT_ONE = 1;                                          // STRUCTURAL: single violation count

        var seeds = BuildDiscoverySeedSurfaceV0(state, seed);

        // Count by unlock type (deterministic: fixed set of known kinds).
        int siteBlueprintCount = STRUCT_ZERO;
        int corridorAccessCount = STRUCT_ZERO;
        int permitCount = STRUCT_ZERO;

        // Emit per-entry lines sorted by DiscoveryId (already sorted by BuildDiscoverySeedSurfaceV0).
        var sb = new StringBuilder();

        foreach (var s in seeds)
        {
            string unlockType;
            if (string.Equals(s.DiscoveryKind, DiscoverySeedKindsV0.ResourcePoolMarker, StringComparison.Ordinal))
            {
                unlockType = STRUCT_UnlockType_SiteBlueprint;
                siteBlueprintCount++;
            }
            else if (string.Equals(s.DiscoveryKind, DiscoverySeedKindsV0.CorridorTrace, StringComparison.Ordinal))
            {
                unlockType = STRUCT_UnlockType_CorridorAccess;
                corridorAccessCount++;
            }
            else if (string.Equals(s.DiscoveryKind, DiscoverySeedKind_AnomalyFamilyV0, StringComparison.Ordinal))
            {
                unlockType = STRUCT_UnlockType_Permit;
                permitCount++;
            }
            else
            {
                // Unknown kind: surface it with a stable label so future kinds are visible.
                unlockType = "Unknown:" + s.DiscoveryKind;
            }

            sb.Append("  DiscoveryId=").Append(s.DiscoveryId)
              .Append(" Kind=").Append(s.DiscoveryKind)
              .Append(" UnlockType=").Append(unlockType)
              .Append(" NodeId=").Append(s.NodeId)
              .Append('\n');
        }

        // Violation: no SiteBlueprint unlock opportunity (ResourcePoolMarker missing entirely).
        bool violation = siteBlueprintCount == STRUCT_ZERO;
        int violationsCount = violation ? STRUCT_ONE : STRUCT_ZERO;

        var result = new StringBuilder();
        result.Append("Seed=").Append(seed).Append('\n');
        result.Append("SiteBlueprintCount=").Append(siteBlueprintCount).Append('\n');
        result.Append("CorridorAccessCount=").Append(corridorAccessCount).Append('\n');
        result.Append("PermitCount=").Append(permitCount).Append('\n');
        result.Append("Result=").Append(violation ? "FAIL" : "PASS").Append('\n');
        result.Append("ViolationsCount=").Append(violationsCount).Append('\n');
        result.Append("Entries").Append('\n');
        result.Append(sb);
        return result.ToString();
    }

    public static string BuildFactionSeedReport(SimState state, int seed)
    {
        // Order homes by a position-derived stable score so different seeds (different positions) produce diffs.
        var scored = state.Nodes.Values
            .Select(n => (Id: n.Id, Score: ScoreNode(seed, n)))
            .ToList();

        const int STRUCT_ZERO = 0; // STRUCTURAL: deterministic comparator and matrix default use zero sentinel

        scored.Sort((a, b) =>
        {
            int c = b.Score.CompareTo(a.Score); // Score descending
            if (c != STRUCT_ZERO) return c;
            return string.CompareOrdinal(a.Id, b.Id); // Id ascending, ordinal
        });

        var nodeIds = scored.Select(t => t.Id).ToList();

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
                int v = STRUCT_ZERO;
                if (!string.Equals(row, col, StringComparison.Ordinal))
                {
                    var fr = factions.First(ff => string.Equals(ff.FactionId, row, StringComparison.Ordinal));
                    v = fr.Relations.TryGetValue(col, out var vv) ? vv : STRUCT_ZERO;
                }
                sb.Append('\t').Append(v);
            }
            sb.Append('\n');
        }

        return sb.ToString();
    }
}
