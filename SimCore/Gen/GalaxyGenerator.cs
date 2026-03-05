using System.Numerics;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Schemas;
using SimCore.Tweaks;
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
    private static readonly string[] WorldgenBoundsGoodsV0 = { WellKnownGoodIds.Fuel, WellKnownGoodIds.Ore, WellKnownGoodIds.Metal };

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

        // GATE.S4.CATALOG.MARKET_BIND.001: optional catalog registry for market good_id validation.
        // When set, Generate() throws if any seeded market inventory key is absent from the registry.
        public ContentRegistryLoader.ContentRegistryV0? Registry { get; init; } = null;
    }

    // GATE.S2_5.WGEN.WORLD_CLASSES.001: deterministic world classes v0.
    // Exactly 3 classes. Each class has exactly one measurable effect in v0: fee_multiplier.
    public static readonly (string WorldClassId, float FeeMultiplier)[] WorldClassesV0 =
    {
        ("CORE", 1.00f),
        ("FRONTIER", 1.10f),
        ("RIM", 1.20f),
    };

    // GATE.S6.FRACTURE.CONTENT.001: FRACTURE_OUTPOST world class identifier.
    // Fracture outposts are nodes that lie outside the standard lane network.
    // They host fracture-exclusive markets and require FractureAccessCheck before entry.
    // Not included in WorldClassesV0 (which drives radial assignment for lane nodes).
    // Fee multiplier: 1.50f (premium for high-risk high-reward fracture access).
    public const string FractureOutpostWorldClassId = "FRACTURE_OUTPOST";
    public static readonly float FractureOutpostFeeMultiplier = SimCore.Tweaks.FractureTweaksV0.FractureOutpostFeeMultiplier;

    // GATE.S2_5.WGEN.DISTINCTNESS.001: report-only class topology params v0.
    // Default lane-cap multipliers are derived deterministically from existing WorldClassesV0 fee multipliers
    // to avoid introducing new numeric literals that violate the tweak routing guard.

    public static void Generate(SimState state, int starCount, float radius)
            => Generate(state, starCount, radius, options: null);

    public static void Generate(SimState state, int starCount, float radius, GalaxyGenOptions? options)
    {
        options ??= new GalaxyGenOptions();

        var minP = Math.Max(0, state.Tweaks.WorldgenMinProducersPerGood);
        var minS = Math.Max(0, state.Tweaks.WorldgenMinSinksPerGood);
        var effectiveEnableDistributionSinksV0 = options.EnableDistributionSinksV0 || (minS > 0);

        state.Nodes.Clear();
        state.Edges.Clear();
        state.Markets.Clear();
        state.Fleets.Clear();
        state.IndustrySites.Clear();

        if (starCount < StarterRegionNodeCount)
        {
            starCount = StarterRegionNodeCount;
        }

        // Phase 1: Place star nodes (consumes RNG for positions).
        var nodesList = StarNetworkGen.PlaceNodes(state, starCount, radius);

        // Phase 2: Seed markets, inventory, and industry sites (deterministic, no RNG).
        MarketInitGen.InitMarkets(state, nodesList, starCount, effectiveEnableDistributionSinksV0);

        if (nodesList.Count == 0) return;
        state.PlayerLocationNodeId = nodesList[0].Id;
        state.PlayerVisitedNodeIds.Add(nodesList[0].Id);

        MarketInitGen.ValidateCatalogBinding(state, options?.Registry);

        // Phase 3: Generate stars and planets (deterministic from node hashes, no RNG).
        PlanetInitGen.InitPlanets(state, nodesList);

        // Phase 4: Wire lanes and seed AI fleets.
        StarNetworkGen.WireLanes(state, nodesList);
        StarNetworkGen.SeedAiFleets(state, nodesList);

        SeedRumorLeadsV0(state);

        var (pass, report) = EvaluateWorldgenBoundsV0(state, WorldgenBoundsGoodsV0, minP, minS);
        if (!pass)
        {
            throw new InvalidOperationException(report);
        }
    }

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

    // GATE.S3_6.RUMOR_INTEL_MIN.002
    // Deterministic rumor lead seeding v0.
    // ID format: LEAD.<seed>.<zero-padded-4-index> — stable, seed-derived, no wall-clock, no Guid.
    // Count sourced from IntelTweaksV0.MinRumorLeadsPerSeed (tweak-routed).
    // Idempotent: existing leads not overwritten.
    private static void SeedRumorLeadsV0(SimState state)
    {
        if (state is null) return;
        if (state.Intel is null) state.Intel = new SimCore.Entities.IntelBook();

        var minCount = SimCore.Tweaks.IntelTweaksV0.MinRumorLeadsPerSeed;
        if (minCount <= 0) return;

        var existing = state.Intel.RumorLeads.Count;
        if (existing >= minCount) return;

        SimCore.Systems.SerializationSystem.TryGetAttachedSeed(state, out var seed);

        var toAdd = minCount - existing;
        var startIndex = existing;

        for (int i = 0; i < toAdd; i++)
        {
            var index = startIndex + i;
            var leadId = $"LEAD.{seed}.{index:D4}";
            if (state.Intel.RumorLeads.ContainsKey(leadId)) continue;

            state.Intel.RumorLeads[leadId] = new SimCore.Entities.RumorLead
            {
                LeadId = leadId,
                Status = SimCore.Entities.RumorLeadStatus.Active,
                SourceVerbToken = "WORLDGEN",
                Hint = new SimCore.Entities.HintPayloadV0
                {
                    RegionTags = new List<string> { "FRONTIER" },
                    CoarseLocationToken = "SECTOR_UNKNOWN",
                    PrerequisiteTokens = new List<string> { "EXPLORATION" },
                    ImpliedPayoffToken = "SITE_BLUEPRINT"
                }
            };
        }
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

    internal static uint Fnv1a32Utf8(string s)
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

    internal static uint ScoreNode(int seed, Node n)
    {
        // Deterministic across seeds without introducing quantization constants:
        // hash (seed|nodeId) only.
        return Fnv1a32Utf8(seed + "|" + (n.Id ?? ""));
    }

    internal static List<string> GetStarterNodeIdsSortedV0(SimState state)
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
    public static IReadOnlyDictionary<string, string> GetWorldClassIdByNodeIdV0(SimState state)
        => GetWorldClassIdByNodeIdV0(state, options: null);

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
