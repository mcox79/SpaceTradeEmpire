using SimCore.Entities;
using System;
using System.Linq;

namespace SimCore.Systems;

// GATE.T18.NARRATIVE.KNOWLEDGE_GRAPH.001: Derive discovery connections from
// pre-authored definitions. Show unresolved connections as "?" to create
// a puzzle surface.
//
// Connection reveal logic:
//   Both endpoints Seen     → connection appears as "?" (something links these)
//   Both endpoints Analyzed → connection type and description revealed
public static class KnowledgeGraphSystem
{
    /// <summary>
    /// Evaluate knowledge graph connections. When both endpoints of a connection
    /// are discovered, mark the connection visible. When both are Analyzed,
    /// fully reveal the connection type and description.
    /// </summary>
    public static void Process(SimState state)
    {
        var connections = state.Intel.KnowledgeConnections;
        if (connections.Count == 0) return;

        foreach (var conn in connections)
        {
            // Skip already-revealed connections
            if (conn.IsRevealed) continue;

            // Check if both endpoints exist in the discovery state
            var sourcePhase = GetDiscoveryPhase(state, conn.SourceDiscoveryId);
            var targetPhase = GetDiscoveryPhase(state, conn.TargetDiscoveryId);

            if (sourcePhase == null || targetPhase == null) continue;

            // Both endpoints must be at least Seen for the connection to appear
            if (sourcePhase < DiscoveryPhase.Seen || targetPhase < DiscoveryPhase.Seen)
                continue;

            // Both Analyzed → fully reveal
            if (sourcePhase >= DiscoveryPhase.Analyzed && targetPhase >= DiscoveryPhase.Analyzed)
            {
                conn.IsRevealed = true;
                conn.RevealedTick = state.Tick;
            }
            // Otherwise the connection exists but shows as "?" — no state change needed,
            // the bridge layer checks both endpoint phases to determine display mode.
        }
    }

    /// <summary>
    /// Check if a connection is visible (both endpoints at least Seen).
    /// </summary>
    public static bool IsConnectionVisible(SimState state, KnowledgeConnection conn)
    {
        var sourcePhase = GetDiscoveryPhase(state, conn.SourceDiscoveryId);
        var targetPhase = GetDiscoveryPhase(state, conn.TargetDiscoveryId);

        if (sourcePhase == null || targetPhase == null) return false;
        return sourcePhase >= DiscoveryPhase.Seen && targetPhase >= DiscoveryPhase.Seen;
    }

    /// <summary>
    /// Get the count of visible but unrevealed connections ("?" connections).
    /// </summary>
    public static int GetQuestionMarkCount(SimState state)
    {
        int count = 0;
        foreach (var conn in state.Intel.KnowledgeConnections)
        {
            if (!conn.IsRevealed && IsConnectionVisible(state, conn))
                count++;
        }
        return count;
    }

    private static DiscoveryPhase? GetDiscoveryPhase(SimState state, string discoveryId)
    {
        if (string.IsNullOrEmpty(discoveryId)) return null;

        if (state.Intel.Discoveries.TryGetValue(discoveryId, out var ds))
            return ds.Phase;
        return null;
    }
}
