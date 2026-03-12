using SimCore.Entities;
using SimCore.Tweaks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Systems;

// GATE.T18.NARRATIVE.TOPOLOGY_SHIFT.001: In Phase 3+ space, edge connections
// mutate on player arrival. Deterministic from hash(edgeId, tick, nodeId).
// Never orphans a node (always preserves connectivity). Routes are unreliable —
// the player navigates by topology, not memory.
public static class TopologyShiftSystem
{
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
        // Find all mutable edges touching this node
        var mutableEdges = new List<Edge>();
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

        int mutationsApplied = 0;

        foreach (var edge in mutableEdges)
        {
            if (mutationsApplied >= TopologyShiftTweaksV0.MaxMutationsPerArrival) break;

            // Check mutation probability
            ulong h = DeterministicHash(edge.Id, state.Tick, nodeId);
            int roll = (int)(h % 10000);
            if (roll >= TopologyShiftTweaksV0.MutationProbabilityBps) continue;

            // Check connectivity preservation — don't remove if it would orphan either endpoint
            if (!CanSafelyMutate(state, edge, nodeId)) continue;

            // Mutate: rewire the far endpoint to a different neighbor
            string farNodeId = edge.FromNodeId == nodeId ? edge.ToNodeId : edge.FromNodeId;
            string? newTargetId = PickNewTarget(state, nodeId, farNodeId, edge.Id);
            if (newTargetId == null) continue;

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
    /// Check that removing/rewiring this edge won't leave either endpoint
    /// with fewer than STRUCT_MinEdgesPerNode connections.
    /// </summary>
    private static bool CanSafelyMutate(SimState state, Edge edge, string arrivalNodeId)
    {
        string farNodeId = edge.FromNodeId == arrivalNodeId ? edge.ToNodeId : edge.FromNodeId;

        // Count edges for the far node (excluding this edge)
        int farNodeEdgeCount = 0;
        foreach (var kv in state.Edges)
        {
            if (kv.Key == edge.Id) continue;
            var e = kv.Value;
            if (e.FromNodeId == farNodeId || e.ToNodeId == farNodeId)
                farNodeEdgeCount++;
        }

        // Count edges for the arrival node (excluding this edge)
        int arrivalNodeEdgeCount = 0;
        foreach (var kv in state.Edges)
        {
            if (kv.Key == edge.Id) continue;
            var e = kv.Value;
            if (e.FromNodeId == arrivalNodeId || e.ToNodeId == arrivalNodeId)
                arrivalNodeEdgeCount++;
        }

        return farNodeEdgeCount >= TopologyShiftTweaksV0.STRUCT_MinEdgesPerNode &&
               arrivalNodeEdgeCount >= TopologyShiftTweaksV0.STRUCT_MinEdgesPerNode;
    }

    /// <summary>
    /// Pick a new target node for the mutated edge. Must be:
    /// - A node adjacent to the arrival node (preserves local connectivity)
    /// - Not already directly connected to the arrival node by another edge
    /// - Not the current far node
    /// Returns null if no valid target exists.
    /// </summary>
    private static string? PickNewTarget(
        SimState state, string arrivalNodeId, string currentFarNodeId, string mutatingEdgeId)
    {
        // Find all nodes within 2 hops of the arrival node
        var candidates = new List<string>();
        var directNeighbors = new HashSet<string>(StringComparer.Ordinal);

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
        var unique = new List<string>();
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
