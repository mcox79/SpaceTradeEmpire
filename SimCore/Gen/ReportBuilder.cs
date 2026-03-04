using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimCore.Content;
using SimCore.Entities;

namespace SimCore.Gen;

// GATE.X.HYGIENE.GEN_REPORT_EXTRACT.001: Report methods extracted from GalaxyGenerator.cs.
public static class ReportBuilder
{
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
            Goods: new[] { WellKnownGoodIds.Fuel, WellKnownGoodIds.Ore, WellKnownGoodIds.Metal });
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
            var starter = GalaxyGenerator.GetStarterNodeIdsSortedV0(state);

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

        var classesSorted = GalaxyGenerator.WorldClassesV0.ToList();
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

        var classCount = GalaxyGenerator.WorldClassesV0.Length;

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
            var c = GalaxyGenerator.WorldClassesV0[cidx];

            sb.Append(n.Id).Append('\t')
              .Append(c.WorldClassId).Append('\t')
              .Append(c.FeeMultiplier.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))
              .Append('\n');
        }

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

        var starter = GalaxyGenerator.GetStarterNodeIdsSortedV0(state);

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
}
