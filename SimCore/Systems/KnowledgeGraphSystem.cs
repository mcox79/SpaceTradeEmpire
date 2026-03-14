using SimCore.Content;
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
//
// GATE.S8.STORY.KG_REVELATION.001: Revelation-triggered connections are
// force-revealed when their associated revelation fires (no Analyzed requirement).
public static class KnowledgeGraphSystem
{
    /// <summary>
    /// Evaluate knowledge graph connections. When both endpoints of a connection
    /// are discovered, mark the connection visible. When both are Analyzed,
    /// fully reveal the connection type and description.
    /// Also force-reveal revelation connections when revelations fire.
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

        // GATE.S8.STORY.KG_REVELATION.001: Force-reveal revelation connections.
        ProcessRevelationConnections(state);
    }

    /// <summary>
    /// Force-reveal revelation-triggered connections when their associated revelation fires.
    /// These are standalone connections (no discovery endpoints) that appear in the
    /// knowledge web as narrative insights.
    /// </summary>
    private static void ProcessRevelationConnections(SimState state)
    {
        var ss = state.StoryState;
        if (ss == null) return;

        foreach (var rc in KnowledgeGraphContentV0.RevelationConnections)
        {
            // Skip if revelation hasn't fired yet
            if (!ss.HasRevelation(rc.RequiredRevelation)) continue;

            // Skip if already added to the knowledge graph
            bool exists = false;
            foreach (var conn in state.Intel.KnowledgeConnections)
            {
                if (string.Equals(conn.ConnectionId, rc.ConnectionId, StringComparison.Ordinal))
                {
                    exists = true;
                    break;
                }
            }
            if (exists) continue;

            // Create and immediately reveal the connection
            state.Intel.KnowledgeConnections.Add(new KnowledgeConnection
            {
                ConnectionId = rc.ConnectionId,
                SourceDiscoveryId = "", // Revelation connections have no discovery endpoints
                TargetDiscoveryId = "",
                ConnectionType = KnowledgeConnectionType.LoreFragment,
                IsRevealed = true,
                RevealedTick = state.Tick,
                Description = rc.Description
            });
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
