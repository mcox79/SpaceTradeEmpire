using SimCore.Content;
using SimCore.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Gen;

// GATE.T18.NARRATIVE.LOG_PLACEMENT.001: Deterministic placement of data logs,
// Kepler chain pieces, and narrative NPC seeds during world generation.
// Uses state.Rng for seed-deterministic placement.
public static class NarrativePlacementGen
{
    /// <summary>
    /// Place all data logs into the world. Called after discovery seeding.
    /// Uses BFS hop distance from player start to match RevelationTier to distance.
    /// </summary>
    public static void PlaceDataLogs(SimState state)
    {
        var logs = DataLogContentV0.AllLogs;
        if (logs.Count == 0) return;

        string startNode = state.PlayerLocationNodeId ?? "";
        if (string.IsNullOrEmpty(startNode)) return;

        var hopDistances = ComputeHopDistances(state, startNode);
        var nodesByDistance = GroupNodesByDistance(hopDistances);

        // Sort logs by revelation tier for deterministic placement order
        var sortedLogs = logs.OrderBy(l => l.RevelationTier)
                             .ThenBy(l => l.LogId, StringComparer.Ordinal)
                             .ToList();

        var usedNodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var logDef in sortedLogs)
        {
            string? nodeId = PickNodeForLog(state, logDef, nodesByDistance, usedNodes, hopDistances);
            if (nodeId == null) continue;

            usedNodes.Add(nodeId);

            // Create a copy for placement (don't mutate the static content)
            var placed = new DataLog
            {
                LogId = logDef.LogId,
                Thread = logDef.Thread,
                RevelationTier = logDef.RevelationTier,
                MechanicalHook = logDef.MechanicalHook,
                LocationNodeId = nodeId,
                IsDiscovered = false,
                DiscoveredTick = 0
            };
            foreach (var s in logDef.Speakers) placed.Speakers.Add(s);
            foreach (var e in logDef.Entries)
            {
                placed.Entries.Add(new DataLogEntry
                {
                    EntryIndex = e.EntryIndex,
                    Speaker = e.Speaker,
                    Text = e.Text,
                    IsPersonal = e.IsPersonal
                });
            }

            state.DataLogs[logDef.LogId] = placed;
        }
    }

    /// <summary>
    /// Place Kepler chain pieces into the world. Called after PlaceDataLogs.
    /// Chain pieces are placed at progressive distances from starter.
    /// </summary>
    public static void PlaceKeplerChain(SimState state)
    {
        var pieces = KeplerChainContentV0.AllPieces;
        if (pieces.Count == 0) return;

        string startNode = state.PlayerLocationNodeId ?? "";
        if (string.IsNullOrEmpty(startNode)) return;

        var hopDistances = ComputeHopDistances(state, startNode);
        var nodesByDistance = GroupNodesByDistance(hopDistances);
        var usedNodes = new HashSet<string>(StringComparer.Ordinal);

        // Don't place Kepler on nodes already used by data logs
        foreach (var kv in state.DataLogs)
        {
            if (!string.IsNullOrEmpty(kv.Value.LocationNodeId))
                usedNodes.Add(kv.Value.LocationNodeId);
        }

        foreach (var piece in pieces)
        {
            string? nodeId = PickNodeForKeplerPiece(
                state, piece, nodesByDistance, usedNodes, hopDistances);
            if (nodeId == null) continue;

            usedNodes.Add(nodeId);

            // Store placement as a discovery-like entry keyed by piece ID
            // The actual discovery seed integration happens in Phase 7
            // For now, store the placement mapping in DataLogs with a kepler prefix
            var keplerLog = new DataLog
            {
                LogId = piece.PieceId,
                Thread = DataLogThread.Warning, // Kepler chain is cross-thread
                RevelationTier = piece.SequenceIndex + 1,
                MechanicalHook = piece.DiscoveryKind,
                LocationNodeId = nodeId,
                IsDiscovered = false,
                DiscoveredTick = 0
            };
            keplerLog.Entries.Add(new DataLogEntry
            {
                EntryIndex = 0,
                Speaker = "",
                Text = piece.Description,
                IsPersonal = false
            });

            state.DataLogs[piece.PieceId] = keplerLog;
        }
    }

    // ── BFS utilities ────────────────────────────────────────────

    /// <summary>
    /// Compute BFS hop distances from a source node to all reachable nodes.
    /// </summary>
    internal static Dictionary<string, int> ComputeHopDistances(SimState state, string sourceNodeId)
    {
        var distances = new Dictionary<string, int>(StringComparer.Ordinal);
        var queue = new Queue<string>();

        distances[sourceNodeId] = 0;
        queue.Enqueue(sourceNodeId);

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();
            int currentDist = distances[current];

            foreach (var edge in state.Edges.Values)
            {
                string? neighbor = null;
                if (string.Equals(edge.FromNodeId, current, StringComparison.Ordinal))
                    neighbor = edge.ToNodeId;
                else if (string.Equals(edge.ToNodeId, current, StringComparison.Ordinal))
                    neighbor = edge.FromNodeId;

                if (neighbor != null && !distances.ContainsKey(neighbor))
                {
                    distances[neighbor] = currentDist + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return distances;
    }

    private static Dictionary<int, List<string>> GroupNodesByDistance(
        Dictionary<string, int> hopDistances)
    {
        var groups = new Dictionary<int, List<string>>();
        foreach (var kv in hopDistances.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            if (!groups.TryGetValue(kv.Value, out var list))
            {
                list = new List<string>();
                groups[kv.Value] = list;
            }
            list.Add(kv.Key);
        }
        return groups;
    }

    private static string? PickNodeForLog(
        SimState state, DataLog log,
        Dictionary<int, List<string>> nodesByDistance,
        HashSet<string> usedNodes,
        Dictionary<string, int> hopDistances)
    {
        // Map revelation tier to target hop distance range
        int minHops, maxHops;
        switch (log.RevelationTier)
        {
            case 1: minHops = 1; maxHops = 3; break;
            case 2: minHops = 2; maxHops = 5; break;
            case 3: minHops = 4; maxHops = 99; break;
            default: minHops = 1; maxHops = 99; break;
        }

        // Fixed landmark placement for specific logs
        string? fixedNode = TryFixedLandmark(state, log.LogId, hopDistances, usedNodes);
        if (fixedNode != null) return fixedNode;

        // Collect candidate nodes in the distance range
        var candidates = new List<string>();
        for (int d = minHops; d <= maxHops; d++)
        {
            if (!nodesByDistance.TryGetValue(d, out var nodesAtDist)) continue;
            foreach (var nid in nodesAtDist)
            {
                if (!usedNodes.Contains(nid))
                    candidates.Add(nid);
            }
            if (candidates.Count >= 3) break; // enough candidates
        }

        if (candidates.Count == 0)
        {
            // Fallback: any unused node (skip starter — player begins there)
            string starter = state.PlayerLocationNodeId ?? "";
            foreach (var kv in hopDistances.OrderBy(k => k.Value).ThenBy(k => k.Key, StringComparer.Ordinal))
            {
                if (!usedNodes.Contains(kv.Key) && kv.Key != starter)
                {
                    candidates.Add(kv.Key);
                    break;
                }
            }
        }

        if (candidates.Count == 0) return null;

        // Deterministic selection using hash(logId)
        ulong h = HashString(log.LogId);
        int idx = (int)(h % (ulong)candidates.Count);
        return candidates[idx];
    }

    private static string? TryFixedLandmark(
        SimState state, string logId,
        Dictionary<string, int> hopDistances,
        HashSet<string> usedNodes)
    {
        // Fixed landmark placement for specific first-encounter logs
        return logId switch
        {
            // LOG.CONTAIN.001 → starter-adjacent (1-hop)
            "LOG.CONTAIN.001" => FindNodeAtDistance(hopDistances, usedNodes, 1, logId),
            // LOG.ECON.001 → warfront border node
            "LOG.ECON.001" => FindWarfrontNode(state, hopDistances, usedNodes, logId),
            // LOG.ACCOM.001 → discovery-rich node
            "LOG.ACCOM.001" => FindDiscoveryRichNode(state, hopDistances, usedNodes, logId),
            // LOG.WARN.001 → deep frontier (far from start)
            "LOG.WARN.001" => FindDeepFrontierNode(hopDistances, usedNodes, logId),
            // LOG.DEPART.001 → highest instability
            "LOG.DEPART.001" => FindHighInstabilityNode(state, hopDistances, usedNodes, logId),
            _ => null
        };
    }

    private static string? FindNodeAtDistance(
        Dictionary<string, int> hopDistances,
        HashSet<string> usedNodes, int targetDist, string seed)
    {
        var candidates = hopDistances
            .Where(kv => kv.Value == targetDist && !usedNodes.Contains(kv.Key))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => kv.Key)
            .ToList();

        if (candidates.Count == 0) return null;
        ulong h = HashString(seed);
        return candidates[(int)(h % (ulong)candidates.Count)];
    }

    private static string? FindWarfrontNode(
        SimState state, Dictionary<string, int> hopDistances,
        HashSet<string> usedNodes, string seed)
    {
        // Find nodes at faction borders (warfront-adjacent)
        var candidates = new List<string>();
        foreach (var wf in state.Warfronts.Values)
        {
            foreach (var nodeId in state.Nodes.Keys)
            {
                if (usedNodes.Contains(nodeId)) continue;
                if (!state.NodeFactionId.TryGetValue(nodeId, out var fid)) continue;
                if (fid == wf.CombatantA || fid == wf.CombatantB)
                {
                    if (hopDistances.ContainsKey(nodeId))
                        candidates.Add(nodeId);
                }
            }
        }

        if (candidates.Count == 0) return null;
        candidates.Sort(StringComparer.Ordinal);
        ulong h = HashString(seed);
        return candidates[(int)(h % (ulong)candidates.Count)];
    }

    private static string? FindDiscoveryRichNode(
        SimState state, Dictionary<string, int> hopDistances,
        HashSet<string> usedNodes, string seed)
    {
        // Node with most discovery sites
        var discCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var kv in state.Intel.Discoveries)
        {
            string nodeId = ExtractNodeIdFromDiscoveryId(kv.Key);
            if (!string.IsNullOrEmpty(nodeId) && !usedNodes.Contains(nodeId))
            {
                discCounts.TryGetValue(nodeId, out int c);
                discCounts[nodeId] = c + 1;
            }
        }

        if (discCounts.Count == 0) return null;
        int maxCount = discCounts.Values.Max();
        var candidates = discCounts
            .Where(kv => kv.Value == maxCount)
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => kv.Key)
            .ToList();

        ulong h = HashString(seed);
        return candidates[(int)(h % (ulong)candidates.Count)];
    }

    private static string? FindDeepFrontierNode(
        Dictionary<string, int> hopDistances,
        HashSet<string> usedNodes, string seed)
    {
        // Furthest reachable node from start
        int maxDist = hopDistances.Values.Max();
        var candidates = hopDistances
            .Where(kv => kv.Value >= maxDist - 1 && !usedNodes.Contains(kv.Key))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => kv.Key)
            .ToList();

        if (candidates.Count == 0) return null;
        ulong h = HashString(seed);
        return candidates[(int)(h % (ulong)candidates.Count)];
    }

    private static string? FindHighInstabilityNode(
        SimState state, Dictionary<string, int> hopDistances,
        HashSet<string> usedNodes, string seed)
    {
        int maxInst = 0;
        foreach (var node in state.Nodes.Values)
        {
            if (!usedNodes.Contains(node.Id) && node.InstabilityLevel > maxInst)
                maxInst = node.InstabilityLevel;
        }

        var candidates = state.Nodes.Values
            .Where(n => n.InstabilityLevel >= maxInst && !usedNodes.Contains(n.Id))
            .OrderBy(n => n.Id, StringComparer.Ordinal)
            .Select(n => n.Id)
            .ToList();

        if (candidates.Count == 0) return null;
        ulong h = HashString(seed);
        return candidates[(int)(h % (ulong)candidates.Count)];
    }

    private static string? PickNodeForKeplerPiece(
        SimState state, KeplerChainContentV0.ChainPiece piece,
        Dictionary<int, List<string>> nodesByDistance,
        HashSet<string> usedNodes,
        Dictionary<string, int> hopDistances)
    {
        var candidates = new List<string>();
        for (int d = piece.MinHopsFromStarter; d <= piece.MaxHopsFromStarter; d++)
        {
            if (!nodesByDistance.TryGetValue(d, out var nodesAtDist)) continue;
            foreach (var nid in nodesAtDist)
            {
                if (!usedNodes.Contains(nid))
                    candidates.Add(nid);
            }
            if (candidates.Count >= 3) break;
        }

        if (candidates.Count == 0)
        {
            // Fallback: closest available node
            foreach (var kv in hopDistances.OrderBy(k => k.Value).ThenBy(k => k.Key, StringComparer.Ordinal))
            {
                if (!usedNodes.Contains(kv.Key))
                {
                    candidates.Add(kv.Key);
                    break;
                }
            }
        }

        if (candidates.Count == 0) return null;

        ulong h = HashString(piece.PieceId);
        int idx = (int)(h % (ulong)candidates.Count);
        return candidates[idx];
    }

    /// <summary>
    /// Extract nodeId from a discovery ID. Format: "disc_v0|KIND|NodeId|RefId|SourceId"
    /// </summary>
    private static string ExtractNodeIdFromDiscoveryId(string discoveryId)
    {
        if (string.IsNullOrEmpty(discoveryId)) return "";
        var parts = discoveryId.Split('|');
        return parts.Length >= 3 ? parts[2] : "";
    }

    // FNV-1a 64-bit hash for deterministic placement
    private static ulong HashString(string s)
    {
        ulong h = 14695981039346656037UL;
        foreach (char c in s)
        {
            h ^= c;
            h *= 1099511628211UL;
        }
        return h;
    }

    // GATE.T18.KG_SEED.PROXIMITY.001: Generate procedural proximity + faction link connections.
    // Proximity: data logs within BFS ≤2 hops get SameOrigin connections.
    // Faction links: logs at faction-owned nodes get FactionLink connections.
    public static void GenerateProceduralConnections(SimState state)
    {
        if (state?.Intel == null) return;

        // Collect placed log node locations
        var logNodes = new Dictionary<string, string>(StringComparer.Ordinal); // logId → nodeId
        foreach (var kv in state.DataLogs)
        {
            if (!string.IsNullOrEmpty(kv.Value.LocationNodeId))
                logNodes[kv.Key] = kv.Value.LocationNodeId;
        }
        if (logNodes.Count == 0) return; // STRUCTURAL: empty guard

        // Build adjacency from edges for BFS proximity check
        var adj = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var edge in state.Edges.Values)
        {
            if (!adj.TryGetValue(edge.FromNodeId, out var fromList))
            {
                fromList = new List<string>();
                adj[edge.FromNodeId] = fromList;
            }
            fromList.Add(edge.ToNodeId);

            if (!adj.TryGetValue(edge.ToNodeId, out var toList))
            {
                toList = new List<string>();
                adj[edge.ToNodeId] = toList;
            }
            toList.Add(edge.FromNodeId);
        }

        // Existing connection IDs to avoid duplicates
        var existingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var conn in state.Intel.KnowledgeConnections)
            existingIds.Add(conn.ConnectionId);

        // Proximity connections: pairs of logs whose nodes are ≤2 hops apart
        var logIds = logNodes.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        int proxCount = 0; // STRUCTURAL: counter
        for (int i = 0; i < logIds.Count; i++)
        {
            string nodeA = logNodes[logIds[i]];
            var reachable = GetNodesWithinHops(adj, nodeA, 2);

            for (int j = i + 1; j < logIds.Count; j++)
            {
                string nodeB = logNodes[logIds[j]];
                if (!reachable.Contains(nodeB)) continue;

                string connId = $"KC.PROX.{logIds[i]}_{logIds[j]}";
                if (existingIds.Contains(connId)) continue;

                existingIds.Add(connId);
                state.Intel.KnowledgeConnections.Add(new KnowledgeConnection
                {
                    ConnectionId = connId,
                    SourceDiscoveryId = logIds[i],
                    TargetDiscoveryId = logIds[j],
                    ConnectionType = KnowledgeConnectionType.SameOrigin,
                    Description = "These sites are in close proximity.",
                    IsRevealed = false,
                });
                proxCount++;
            }
        }

        // Faction link connections: logs placed at faction-owned nodes
        foreach (var logId in logIds)
        {
            string nodeId = logNodes[logId];
            if (!state.NodeFactionId.TryGetValue(nodeId, out var factionId)) continue;
            if (string.IsNullOrEmpty(factionId)) continue;

            string connId = $"KC.FACTION.{logId}_{factionId}";
            if (existingIds.Contains(connId)) continue;

            existingIds.Add(connId);
            state.Intel.KnowledgeConnections.Add(new KnowledgeConnection
            {
                ConnectionId = connId,
                SourceDiscoveryId = logId,
                TargetDiscoveryId = factionId,
                ConnectionType = KnowledgeConnectionType.FactionLink,
                Description = $"Located in {factionId} territory.",
                IsRevealed = false,
            });
        }
    }

    // BFS: return all nodes within maxHops of source.
    private static HashSet<string> GetNodesWithinHops(
        Dictionary<string, List<string>> adj, string source, int maxHops)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<(string nodeId, int depth)>();
        visited.Add(source);
        queue.Enqueue((source, 0)); // STRUCTURAL: BFS start at 0

        while (queue.Count > 0) // STRUCTURAL: empty guard
        {
            var (nodeId, depth) = queue.Dequeue();
            if (depth >= maxHops) continue;

            if (adj.TryGetValue(nodeId, out var neighbors))
            {
                foreach (var nb in neighbors)
                {
                    if (visited.Add(nb))
                        queue.Enqueue((nb, depth + 1));
                }
            }
        }

        return visited;
    }

    // GATE.T18.KG_SEED.RESOLVE.001: Resolve knowledge graph templates into KnowledgeConnection entities.
    // Runs after data logs and Kepler chain are placed. Replaces pattern tokens with actual IDs.
    public static void ResolveKnowledgeGraphTemplates(SimState state)
    {
        if (state?.Intel == null) return;

        foreach (var template in KnowledgeGraphContentV0.AllTemplates)
        {
            string sourceId = ResolvePatternToken(template.SourcePattern);
            string targetId = ResolvePatternToken(template.TargetPattern);

            // Only create connection if both endpoints exist in state data
            if (!state.DataLogs.ContainsKey(sourceId) || !state.DataLogs.ContainsKey(targetId))
                continue; // STRUCTURAL: skip if either endpoint is missing

            state.Intel.KnowledgeConnections.Add(new KnowledgeConnection
            {
                ConnectionId = template.TemplateId,
                SourceDiscoveryId = sourceId,
                TargetDiscoveryId = targetId,
                ConnectionType = template.ConnectionType,
                Description = template.Description,
                IsRevealed = false,
            });
        }
    }

    // Resolve a pattern token to an actual discovery/log ID.
    // $KEPLER_N → "KEPLER.00N", $LOG.X.Y → "LOG.X.Y" (strip $).
    private static string ResolvePatternToken(string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return pattern;
        if (!pattern.StartsWith("$", StringComparison.Ordinal)) return pattern;

        string token = pattern.Substring(1); // STRUCTURAL: strip $ prefix

        // $KEPLER_1 through $KEPLER_6 → KEPLER.001 through KEPLER.006
        if (token.StartsWith("KEPLER_", StringComparison.Ordinal) && token.Length > 7) // STRUCTURAL: prefix length
        {
            string num = token.Substring(7); // STRUCTURAL: after "KEPLER_"
            if (int.TryParse(num, out int idx))
                return $"KEPLER.{idx:D3}";
        }

        // $LOG.CONTAIN.003 → LOG.CONTAIN.003
        return token;
    }
}
