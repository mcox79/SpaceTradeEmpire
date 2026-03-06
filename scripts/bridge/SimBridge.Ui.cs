#nullable enable

using Godot;
using SimCore;
using SimCore.Gen;
using SimCore.Commands;
using SimCore.Intents;
using SimCore.Systems;
using SimCore.Programs;
using SimCore.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge
{
    public int GetUiStationViewIndex() => _uiStationViewIndex;

    public void SetUiStationViewIndex(int idx)
    {
        _uiStationViewIndex = Math.Clamp(idx, 0, 3);
    }

    public int GetUiDashboardLastSnapshotTick() => _uiDashboardLastSnapshotTick;

    public string GetUiSelectedFleetId() => _uiSelectedFleetId;

    public void SetUiSelectedFleetId(string fleetId)
    {
        _uiSelectedFleetId = fleetId ?? "";
    }

    // GATE.S15.FEEL.FACTION_LABELS.001: Faction territory snapshot for galaxy map overlay.
    // Returns Array of dicts: {faction_id, role_tag, home_node_id, controlled_node_ids}.
    // Derives faction home nodes deterministically from sorted node IDs (same algorithm as
    // GalaxyGenerator.SeedFactionsFromNodesSorted). Controlled nodes use BFS depth<=3.
    // Nonblocking: returns last cached snapshot if read lock is unavailable.
    private Godot.Collections.Array _cachedFactionMapV0 = new Godot.Collections.Array();

    public Godot.Collections.Array GetFactionMapV0()
    {
        TryExecuteSafeRead(state =>
        {
            if (state.Nodes == null || state.Nodes.Count == 0) return;

            // Build sorted node ID list — mirrors GalaxyGenerator.SeedFactionsFromNodesSorted input.
            var sortedNodeIds = state.Nodes.Keys
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();

            if (sortedNodeIds.Count == 0) return;

            // Replicate home-node seeding (indices 0, mid, last).
            int idx0 = 0;
            int idx1 = sortedNodeIds.Count / 2;
            int idx2 = sortedNodeIds.Count - 1;
            var home = new[] { sortedNodeIds[idx0], sortedNodeIds[idx1], sortedNodeIds[idx2] };

            // Deduplicate exactly as SeedFactionsFromNodesSorted does.
            var used = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < home.Length; i++)
            {
                if (used.Add(home[i])) continue;
                foreach (var nid in sortedNodeIds)
                {
                    if (used.Add(nid)) { home[i] = nid; break; }
                }
            }

            var roles = new[] { "Trader", "Miner", "Pirate" };
            var fids  = new[] { "faction_0", "faction_1", "faction_2" };

            // Build adjacency for BFS (undirected — edges are one-way in the model but travel is
            // bidirectional, so we include both directions).
            var adj = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            if (state.Edges != null)
            {
                foreach (var e in state.Edges.Values)
                {
                    if (e == null) continue;
                    var from = e.FromNodeId ?? "";
                    var to   = e.ToNodeId ?? "";
                    if (from.Length == 0 || to.Length == 0) continue;
                    if (!adj.ContainsKey(from)) adj[from] = new List<string>();
                    if (!adj.ContainsKey(to))   adj[to]   = new List<string>();
                    adj[from].Add(to);
                    adj[to].Add(from);
                }
            }

            // BFS each faction up to depth 3; closer faction wins on contested nodes.
            var claims = new Dictionary<string, (string FactionId, int Depth)>(StringComparer.Ordinal);
            const int maxDepth = 3;

            for (int fi = 0; fi < 3; fi++)
            {
                var fid = fids[fi];
                var homeId = home[fi];
                if (string.IsNullOrEmpty(homeId)) continue;

                var queue = new Queue<(string NodeId, int Depth)>();
                queue.Enqueue((homeId, 0));
                var visited = new HashSet<string>(StringComparer.Ordinal) { homeId };

                while (queue.Count > 0)
                {
                    var (nodeId, depth) = queue.Dequeue();

                    if (!claims.TryGetValue(nodeId, out var existing) ||
                        depth < existing.Depth ||
                        (depth == existing.Depth && string.CompareOrdinal(fid, existing.FactionId) < 0))
                    {
                        claims[nodeId] = (fid, depth);
                    }

                    if (depth >= maxDepth) continue;

                    if (adj.TryGetValue(nodeId, out var neighbors))
                    {
                        foreach (var nb in neighbors)
                        {
                            if (visited.Add(nb))
                                queue.Enqueue((nb, depth + 1));
                        }
                    }
                }
            }

            var result = new Godot.Collections.Array();
            for (int fi = 0; fi < 3; fi++)
            {
                var fid    = fids[fi];
                var homeId = home[fi];
                var role   = roles[fi];

                var controlled = new Godot.Collections.Array();
                foreach (var kv in claims.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                {
                    if (string.Equals(kv.Value.FactionId, fid, StringComparison.Ordinal))
                        controlled.Add(kv.Key);
                }

                result.Add(new Godot.Collections.Dictionary
                {
                    ["faction_id"]          = fid,
                    ["role_tag"]            = role,
                    ["home_node_id"]        = homeId,
                    ["controlled_node_ids"] = controlled,
                });
            }

            lock (_snapshotLock)
            {
                _cachedFactionMapV0 = result;
            }
        }, 0);

        lock (_snapshotLock)
        {
            return (Godot.Collections.Array)_cachedFactionMapV0.Duplicate();
        }
    }
}
