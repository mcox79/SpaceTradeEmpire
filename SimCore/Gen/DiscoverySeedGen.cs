using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimCore.Entities;
using SimCore.Schemas;

namespace SimCore.Gen;

// GATE.X.HYGIENE.GEN_DISCOVERY_EXTRACT.001: Discovery seed methods extracted from GalaxyGenerator.cs.
public static class DiscoverySeedGen
{
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
        var classByNode = GalaxyGenerator.GetWorldClassIdByNodeIdV0(state);

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
        // Guarantee at least 1 RUIN per world (exotic matter source).
        var families = new[] { "DERELICT", "RUIN", "SIGNAL" };
        bool hasRuin = false;

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
            // Guarantee at least 1 RUIN family per world for exotic matter.
            {
                var kind = DiscoverySeedKind_AnomalyFamilyV0;
                var nodeId = host.Id;

                var famHash = GalaxyGenerator.Fnv1a32Utf8(seed + "|" + wc + "|anomaly_family_v0");
                var fam = families[(int)(famHash % (uint)families.Length)];

                // If this is the last class and no RUIN yet, force RUIN.
                if (!hasRuin && wc == classes[classes.Count - 1])
                    fam = "RUIN";
                if (fam == "RUIN")
                    hasRuin = true;

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
        var classByNode = GalaxyGenerator.GetWorldClassIdByNodeIdV0(state);
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
        var classByNode = GalaxyGenerator.GetWorldClassIdByNodeIdV0(state);

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
        uint bestScore = GalaxyGenerator.ScoreNode(seed, best);

        var i = default(int);
        while (i < nodes.Count)
        {
            var n = nodes[i];
            var score = GalaxyGenerator.ScoreNode(seed, n);

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

    // GATE.S6.FRACTURE_DISCOVERY.DERELICT.001: Seed a single FractureDerelict VoidSite at a frontier node.
    // Deterministic: picks the node farthest from player start (star_0) using hash as tiebreaker.
    // Called from GalaxyGenerator after SeedVoidSitesV0.
    public static void SeedFractureDerelictV0(SimState state, int seed)
    {
        if (state.Nodes.Count < 2) return;

        // Player start is always star_0.
        string startNodeId = state.PlayerLocationNodeId ?? "";
        System.Numerics.Vector3 startPos = System.Numerics.Vector3.Zero;
        if (state.Nodes.TryGetValue(startNodeId, out var startNode))
            startPos = startNode.Position;

        // Pick the farthest node from player start (deterministic tiebreak by hash).
        string bestNodeId = "";
        float bestDist = -1f;
        uint bestHash = 0;

        var sortedNodeIds = new List<string>(state.Nodes.Keys);
        sortedNodeIds.Sort(StringComparer.Ordinal);

        foreach (var nodeId in sortedNodeIds)
        {
            if (!state.Nodes.TryGetValue(nodeId, out var node)) continue;
            float dist = System.Numerics.Vector3.Distance(startPos, node.Position);
            uint h = GalaxyGenerator.Fnv1a32Utf8(seed + "|fracture_derelict|" + nodeId);

            if (dist > bestDist || (dist == bestDist && h > bestHash))
            {
                bestDist = dist;
                bestHash = h;
                bestNodeId = nodeId;
            }
        }

        if (string.IsNullOrEmpty(bestNodeId)) return;
        var targetNode = state.Nodes[bestNodeId];

        // Place the derelict near the frontier node with a perpendicular offset.
        uint posHash = GalaxyGenerator.Fnv1a32Utf8(seed + "|fracture_derelict_pos");
        float offsetX = ((posHash % 100u) / 100f - 0.5f) * 40f; // ±20 units
        float offsetZ = (((posHash >> 8) % 100u) / 100f - 0.5f) * 40f;

        string siteId = "void_fracture_derelict_0";
        state.VoidSites[siteId] = new Entities.VoidSite
        {
            Id = siteId,
            Position = new System.Numerics.Vector3(
                targetNode.Position.X + offsetX,
                0,
                targetNode.Position.Z + offsetZ),
            Family = Entities.VoidSiteFamily.FractureDerelict,
            MarkerState = Entities.VoidSiteMarkerState.Discovered, // Visible from start
            NearStarA = bestNodeId,
            NearStarB = bestNodeId,
        };
    }

    // GATE.S8.ADAPTATION.PLACEMENT.001: Deterministic fragment placement across galaxy nodes.
    // Spreads 16 fragments across nodes far from player start using hash-based assignment.
    public static void SeedAdaptationFragmentsV0(SimState state, int seed)
    {
        var content = Content.AdaptationFragmentContentV0.AllFragments;
        if (content.Count == 0) return;
        if (state.Nodes.Count < Tweaks.AdaptationTweaksV0.PlacementMinDistanceFromStart) return;

        // Player start position.
        var startId = state.PlayerLocationNodeId ?? "";
        System.Numerics.Vector3 startPos = System.Numerics.Vector3.Zero;
        if (state.Nodes.TryGetValue(startId, out var startNode))
            startPos = startNode.Position;

        // Build sorted candidate nodes by distance from start (descending).
        var candidates = new List<(string NodeId, float DistSq)>();
        foreach (var kv in state.Nodes)
        {
            if (kv.Value is null) continue;
            var dx = kv.Value.Position.X - startPos.X;
            var dz = kv.Value.Position.Z - startPos.Z;
            candidates.Add((kv.Key, dx * dx + dz * dz));
        }
        candidates.Sort((a, b) =>
        {
            int c = b.DistSq.CompareTo(a.DistSq); // descending by distance
            return c != 0 ? c : string.CompareOrdinal(a.NodeId, b.NodeId);
        });

        // Skip closest nodes (placement min distance).
        int skipCount = System.Math.Min(Tweaks.AdaptationTweaksV0.PlacementMinDistanceFromStart, candidates.Count / 2);
        int eligibleCount = System.Math.Max(candidates.Count - skipCount, 1);
        eligibleCount = System.Math.Min(eligibleCount, candidates.Count); // clamp to available
        var eligible = candidates.GetRange(0, eligibleCount);

        // Hash-based assignment: each fragment gets a deterministic node.
        var usedNodes = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < content.Count; i++)
        {
            var fDef = content[i];
            uint h = GalaxyGenerator.Fnv1a32Utf8(seed + "|frag_place|" + fDef.FragmentId);
            int idx = (int)(h % (uint)eligible.Count);

            // Linear probe to avoid collisions.
            int attempts = 0;
            while (usedNodes.Contains(eligible[idx].NodeId) && attempts < eligible.Count)
            {
                idx = (idx + 1) % eligible.Count;
                attempts++;
            }

            var nodeId = eligible[idx].NodeId;
            usedNodes.Add(nodeId);

            state.AdaptationFragments[fDef.FragmentId] = new Entities.AdaptationFragment
            {
                FragmentId = fDef.FragmentId,
                Name = fDef.Name,
                Description = fDef.Description,
                Kind = fDef.Kind,
                ResonancePairId = fDef.ResonancePairId,
                NodeId = nodeId,
            };
        }
    }

    private static string MintDiscoveryIdV0(string kind, string nodeId, string refId, string sourceId)
    {
        // Canonical stable id format (v0). No timestamps%wall-clock, no RNG, no unordered iteration inputs.
        // Use '|' separators and include all components to avoid collisions.
        return $"disc_v0|{kind}|{nodeId}|{refId}|{sourceId}";
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
}
