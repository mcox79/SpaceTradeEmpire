using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SimCore.Entities;

namespace SimCore;

public static class MapQueries
{
    // GATE.S1.GALAXY_MAP.CONTRACT.001
    public sealed class SystemNodeSnapV0
    {
        public string NodeId { get; set; } = "";
        public string DisplayStateToken { get; set; } = ""; // HIDDEN%RUMORED%VISITED%MAPPED
        public string DisplayText { get; set; } = ""; // ""%???%Name%Name+Count (per token rules)
        public int ObjectCount { get; set; } = 0;
    }

    // GATE.S1.GALAXY_MAP.CONTRACT.001
    public sealed class LaneEdgeSnapV0
    {
        public string FromNodeId { get; set; } = "";
        public string ToNodeId { get; set; } = "";
    }

    // GATE.S1.GALAXY_MAP.CONTRACT.001
    public sealed class GalaxySnapshotV0
    {
        public List<SystemNodeSnapV0> SystemNodes { get; } = new List<SystemNodeSnapV0>();
        public List<LaneEdgeSnapV0> LaneEdges { get; } = new List<LaneEdgeSnapV0>();
        public string PlayerCurrentNodeId { get; set; } = "";
    }

    // GATE.S1.HERO_SHIP.SYSTEM_CONTRACT.001
    public sealed class StationSnapV0
    {
        public string NodeId { get; set; } = "";
        public string NodeName { get; set; } = "";
    }

    // GATE.S1.HERO_SHIP.SYSTEM_CONTRACT.001
    public sealed class DiscoverySiteSnapV0
    {
        public string SiteId { get; set; } = "";
        public string PhaseToken { get; set; } = ""; // SEEN%SCANNED%ANALYZED
    }

    // GATE.S1.HERO_SHIP.SYSTEM_CONTRACT.001
    public sealed class LaneGateSnapV0
    {
        public string NeighborNodeId { get; set; } = "";
        public string EdgeId { get; set; } = "";
    }

    // GATE.S1.HERO_SHIP.SYSTEM_CONTRACT.001
    public sealed class SystemSnapshotV0
    {
        public StationSnapV0 Station { get; set; } = new StationSnapV0();
        public List<DiscoverySiteSnapV0> DiscoverySites { get; } = new List<DiscoverySiteSnapV0>();
        public List<LaneGateSnapV0> LaneGate { get; } = new List<LaneGateSnapV0>();
    }

    private static string PhaseToToken(DiscoveryPhase phase)
    {
        return phase switch
        {
            DiscoveryPhase.Seen => "SEEN",
            DiscoveryPhase.Scanned => "SCANNED",
            DiscoveryPhase.Analyzed => "ANALYZED",
            _ => "SEEN"
        };
    }

    // GATE.S1.HERO_SHIP.SYSTEM_CONTRACT.001
    // Facts-only snapshot builder for a single system.
    // Determinism:
    // - discovery_sites ordered by SiteId (DiscoveryId) Ordinal asc
    // - lane_gate ordered by EdgeId Ordinal asc
    // - no timestamps%wall-clock%randomness
    public static SystemSnapshotV0 BuildSystemSnapshotV0(SimState state, string nodeId)
    {
        nodeId ??= "";

        var snap = new SystemSnapshotV0();

        if (state.Nodes.TryGetValue(nodeId, out var node))
        {
            snap.Station = new StationSnapV0
            {
                NodeId = node.Id ?? "",
                NodeName = node.Name ?? ""
            };

            if (node.SeededDiscoveryIds is not null && node.SeededDiscoveryIds.Count > 0)
            {
                // Determinism: dedupe and iterate ids in Ordinal order.
                var ids = node.SeededDiscoveryIds
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(s => s, StringComparer.Ordinal)
                    .ToList();

                for (int i = 0; i < ids.Count; i++)
                {
                    var id = ids[i];
                    if (!state.Intel.Discoveries.TryGetValue(id, out var disc)) continue;

                    snap.DiscoverySites.Add(new DiscoverySiteSnapV0
                    {
                        SiteId = disc.DiscoveryId ?? id,
                        PhaseToken = PhaseToToken(disc.Phase)
                    });
                }

                snap.DiscoverySites.Sort((a, b) => StringComparer.Ordinal.Compare(a.SiteId, b.SiteId));
            }
        }
        else
        {
            snap.Station = new StationSnapV0 { NodeId = "", NodeName = "" };
        }

        // Determinism: iterate incident edges in EdgeId (Ordinal) order.
        var edges = state.Edges.Values.ToList();
        edges.Sort((a, b) => StringComparer.Ordinal.Compare(a.Id, b.Id));

        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            if (string.IsNullOrEmpty(e.Id)) continue;

            if (string.Equals(e.FromNodeId, nodeId, StringComparison.Ordinal))
            {
                snap.LaneGate.Add(new LaneGateSnapV0
                {
                    NeighborNodeId = e.ToNodeId ?? "",
                    EdgeId = e.Id
                });
            }
            else if (string.Equals(e.ToNodeId, nodeId, StringComparison.Ordinal))
            {
                snap.LaneGate.Add(new LaneGateSnapV0
                {
                    NeighborNodeId = e.FromNodeId ?? "",
                    EdgeId = e.Id
                });
            }
        }

        // Already EdgeId-sorted by iteration order, but keep explicit sort for safety.
        snap.LaneGate.Sort((a, b) => StringComparer.Ordinal.Compare(a.EdgeId, b.EdgeId));

        return snap;
    }

    public static bool TryGetEdgeId(SimState state, string fromNodeId, string toNodeId, out string edgeId)
    {
        edgeId = "";
        if (string.IsNullOrEmpty(fromNodeId) || string.IsNullOrEmpty(toNodeId)) return false;
        if (fromNodeId == toNodeId) return false;

        // Determinism: iterate edges in EdgeId (Ordinal) order.
        foreach (var e in state.Edges.Values.OrderBy(e => e.Id, StringComparer.Ordinal))
        {
            if ((e.FromNodeId == fromNodeId && e.ToNodeId == toNodeId) ||
                (e.ToNodeId == fromNodeId && e.FromNodeId == toNodeId))
            {
                edgeId = e.Id;
                return true;
            }
        }
        return false;
    }

    public static bool AreConnected(SimState state, string fromNodeId, string toNodeId)
        => TryGetEdgeId(state, fromNodeId, toNodeId, out _);

    // GATE.S1.GALAXY_MAP.CONTRACT.001
    // Facts-only snapshot builder.
    // Determinism:
    // - system_nodes ordered by NodeId (Ordinal asc)
    // - lane_edges ordered by EdgeId (Ordinal asc)
    // - rumor-derived node ids are deduped and not order-sensitive
    // - no timestamps%wall-clock%randomness
    public static GalaxySnapshotV0 BuildGalaxySnapshotV0(SimState state)
    {
        var snap = new GalaxySnapshotV0
        {
            PlayerCurrentNodeId = state.PlayerLocationNodeId ?? ""
        };

        var rumored = new HashSet<string>(StringComparer.Ordinal);
        foreach (var lead in state.Intel.RumorLeads.Values)
        {
            if (lead.Status != RumorLeadStatus.Active) continue;

            var token = lead.Hint?.CoarseLocationToken ?? "";
            if (string.IsNullOrEmpty(token)) continue;

            // Contract: treat CoarseLocationToken as the node_id for v0.
            rumored.Add(token);
        }

        var nodes = state.Nodes.Values.ToList();
        nodes.Sort((a, b) => StringComparer.Ordinal.Compare(a.Id, b.Id));

        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            var nodeId = n.Id ?? "";

            int objectCount = 0;
            if (n.SeededDiscoveryIds is not null && n.SeededDiscoveryIds.Count > 0)
            {
                // Determinism: dedupe and test membership in a stable way.
                var ids = n.SeededDiscoveryIds
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(s => s, StringComparer.Ordinal)
                    .ToList();

                for (int k = 0; k < ids.Count; k++)
                {
                    if (state.Intel.Discoveries.ContainsKey(ids[k]))
                        objectCount++;
                }
            }

            var isMapped = objectCount > 0;
            var isVisited = !isMapped && string.Equals(nodeId, snap.PlayerCurrentNodeId, StringComparison.Ordinal);
            var isRumored = !isMapped && !isVisited && rumored.Contains(nodeId);

            string tokenOut;
            string textOut;

            if (isMapped)
            {
                tokenOut = "MAPPED";
                textOut = (n.Name ?? "") + "+" + objectCount.ToString(CultureInfo.InvariantCulture);
            }
            else if (isVisited)
            {
                tokenOut = "VISITED";
                textOut = n.Name ?? "";
            }
            else if (isRumored)
            {
                tokenOut = "RUMORED";
                textOut = "???";
            }
            else
            {
                tokenOut = "HIDDEN";
                textOut = "";
            }

            snap.SystemNodes.Add(new SystemNodeSnapV0
            {
                NodeId = nodeId,
                DisplayStateToken = tokenOut,
                DisplayText = textOut,
                ObjectCount = objectCount
            });
        }

        var edges = state.Edges.Values.ToList();
        edges.Sort((a, b) => StringComparer.Ordinal.Compare(a.Id, b.Id));

        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            snap.LaneEdges.Add(new LaneEdgeSnapV0
            {
                FromNodeId = e.FromNodeId ?? "",
                ToNodeId = e.ToNodeId ?? ""
            });
        }

        return snap;
    }
}
