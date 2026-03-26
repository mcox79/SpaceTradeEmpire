using SimCore.Entities;
using SimCore.Tweaks;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SimCore.Systems;

// GATE.T18.NARRATIVE.TOPOLOGY_SHIFT.001: In Phase 3+ space, edge connections
// mutate on player arrival. Deterministic from hash(edgeId, tick, nodeId).
// Never orphans a node (always preserves connectivity). Routes are unreliable —
// the player navigates by topology, not memory.
public static class TopologyShiftSystem
{
    private sealed class Scratch
    {
        public readonly List<Edge> MutableEdges = new();
        public readonly Dictionary<string, int> EdgeCountByNode = new(StringComparer.Ordinal);
        public readonly List<string> Candidates = new();
        public readonly HashSet<string> DirectNeighbors = new(StringComparer.Ordinal);
        public readonly List<string> UniqueCandidates = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();
    /// <summary>
    /// On player arrival at a Phase 3+ node, mutate eligible mutable edges.
    /// </summary>
    public static void Process(SimState state)
    {
        if (state.ArrivalsThisTick.Count == 0) return;

        foreach (var (fleetId, edgeId, nodeId) in state.ArrivalsThisTick)
        {
            if (!state.Fleets.TryGetValue(fleetId, out var fleet)) continue;
            if (fleet.OwnerId != "player") continue; // only player arrival triggers shift
            if (!state.Nodes.TryGetValue(nodeId, out var node)) continue;

            // Only Phase 3+ (Fracture and above) triggers topology mutation
            if (node.InstabilityLevel < TopologyShiftTweaksV0.STRUCT_MinPhaseForMutation) continue;
            if (!node.MutableTopology) continue;

            ApplyTopologyShift(state, nodeId);
        }
    }

    /// <summary>
    /// Mutate edges at a node. Deterministic, connectivity-preserving.
    /// </summary>
    public static void ApplyTopologyShift(SimState state, string nodeId)
    {
        var scratch = s_scratch.GetOrCreateValue(state);

        // Find all mutable edges touching this node
        var mutableEdges = scratch.MutableEdges;
        mutableEdges.Clear();
        foreach (var kv in state.Edges)
        {
            var edge = kv.Value;
            if (!edge.IsMutable) continue;
            if (edge.FromNodeId == nodeId || edge.ToNodeId == nodeId)
                mutableEdges.Add(edge);
        }

        if (mutableEdges.Count == 0) return;

        // Sort for determinism
        mutableEdges.Sort(Edge.DeterministicComparer);

        // Pre-compute edge counts per node (avoids O(n²) per mutable edge).
        var edgeCounts = scratch.EdgeCountByNode;
        edgeCounts.Clear();
        foreach (var kv in state.Edges)
        {
            var e = kv.Value;
            edgeCounts[e.FromNodeId] = edgeCounts.GetValueOrDefault(e.FromNodeId) + 1;
            edgeCounts[e.ToNodeId] = edgeCounts.GetValueOrDefault(e.ToNodeId) + 1;
        }

        int mutationsApplied = 0;

        foreach (var edge in mutableEdges)
        {
            if (mutationsApplied >= TopologyShiftTweaksV0.MaxMutationsPerArrival) break;

            // Check mutation probability
            ulong h = DeterministicHash(edge.Id, state.Tick, nodeId);
            int roll = (int)(h % 10000);
            if (roll >= TopologyShiftTweaksV0.MutationProbabilityBps) continue;

            // Check connectivity preservation — don't remove if it would orphan either endpoint
            string farNodeId = edge.FromNodeId == nodeId ? edge.ToNodeId : edge.FromNodeId;
            // Edge count minus 1 (excluding this edge) must be >= minimum.
            int farCount = edgeCounts.GetValueOrDefault(farNodeId) - 1;
            int arrivalCount = edgeCounts.GetValueOrDefault(nodeId) - 1;
            if (farCount < TopologyShiftTweaksV0.STRUCT_MinEdgesPerNode ||
                arrivalCount < TopologyShiftTweaksV0.STRUCT_MinEdgesPerNode)
                continue;

            // Mutate: rewire the far endpoint to a different neighbor
            string? newTargetId = PickNewTarget(state, nodeId, farNodeId, edge.Id, scratch);
            if (newTargetId == null) continue;

            // Update edge counts for the rewire.
            edgeCounts[farNodeId] = edgeCounts.GetValueOrDefault(farNodeId) - 1;
            edgeCounts[newTargetId] = edgeCounts.GetValueOrDefault(newTargetId) + 1;

            // Rewire the edge
            if (edge.FromNodeId == nodeId)
                edge.ToNodeId = newTargetId;
            else
                edge.FromNodeId = newTargetId;

            edge.MutationEpoch++;
            mutationsApplied++;
        }

        if (mutationsApplied > 0)
        {
            state.InvalidateRoutePlannerCaches();
        }
    }

    /// <summary>
    /// Pick a new target node for the mutated edge. Must be:
    /// - A node within 2 hops of the arrival node
    /// - Not already directly connected to the arrival node by another edge
    /// - Not the current far node
    /// Returns null if no valid target exists.
    /// </summary>
    private static string? PickNewTarget(
        SimState state, string arrivalNodeId, string currentFarNodeId, string mutatingEdgeId,
        Scratch scratch)
    {
        // Find all nodes within 2 hops of the arrival node
        var candidates = scratch.Candidates;
        candidates.Clear();
        var directNeighbors = scratch.DirectNeighbors;
        directNeighbors.Clear();

        foreach (var kv in state.Edges)
        {
            var e = kv.Value;
            if (e.FromNodeId == arrivalNodeId) directNeighbors.Add(e.ToNodeId);
            if (e.ToNodeId == arrivalNodeId) directNeighbors.Add(e.FromNodeId);
        }

        // Look for 2-hop neighbors not already directly connected
        foreach (var neighbor in directNeighbors)
        {
            foreach (var kv in state.Edges)
            {
                var e = kv.Value;
                string? twoHopNode = null;
                if (e.FromNodeId == neighbor && e.ToNodeId != arrivalNodeId)
                    twoHopNode = e.ToNodeId;
                else if (e.ToNodeId == neighbor && e.FromNodeId != arrivalNodeId)
                    twoHopNode = e.FromNodeId;

                if (twoHopNode != null &&
                    twoHopNode != currentFarNodeId &&
                    !directNeighbors.Contains(twoHopNode) &&
                    twoHopNode != arrivalNodeId)
                {
                    candidates.Add(twoHopNode);
                }
            }
        }

        if (candidates.Count == 0) return null;

        // Sort for determinism, pick via hash
        candidates.Sort(StringComparer.Ordinal);
        // Remove duplicates
        var unique = scratch.UniqueCandidates;
        unique.Clear();
        string? prev = null;
        foreach (var c in candidates)
        {
            if (c != prev) unique.Add(c);
            prev = c;
        }

        if (unique.Count == 0) return null;

        ulong h = DeterministicHash(mutatingEdgeId, state.Tick + 1, arrivalNodeId);
        int idx = (int)(h % (ulong)unique.Count);
        return unique[idx];
    }

    private static ulong DeterministicHash(string edgeId, int tick, string nodeId)
    {
        ulong h = 14695981039346656037UL;
        foreach (char c in edgeId)
        {
            h ^= c;
            h *= 1099511628211UL;
        }
        h ^= (uint)tick;
        h *= 1099511628211UL;
        foreach (char c in nodeId)
        {
            h ^= c;
            h *= 1099511628211UL;
        }
        return h;
    }
}
