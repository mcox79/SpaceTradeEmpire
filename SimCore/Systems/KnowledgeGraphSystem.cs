using SimCore.Content;
using SimCore.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
    private sealed class Scratch
    {
        public readonly HashSet<string> ExistingConnectionIds = new(StringComparer.Ordinal);
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();
    /// <summary>
    /// Evaluate knowledge graph connections. When both endpoints of a connection
    /// are discovered, mark the connection visible. When both are Analyzed,
    /// fully reveal the connection type and description.
    /// Also force-reveal revelation connections when revelations fire.
    /// </summary>
    public static void Process(SimState state)
    {
        // GATE.T58.KG.MILESTONE_ENTITY.001: Always evaluate milestones (even with 0 connections).
        ProcessMilestones(state);

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

        // GATE.T57.KG.LINK_FEEDBACK.001: Advance speculative link states.
        ProcessLinkStateMachine(state);
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

        var scratch = s_scratch.GetOrCreateValue(state);
        var existingIds = scratch.ExistingConnectionIds;
        existingIds.Clear();
        foreach (var conn in state.Intel.KnowledgeConnections)
            existingIds.Add(conn.ConnectionId);

        foreach (var rc in KnowledgeGraphContentV0.RevelationConnections)
        {
            // Skip if revelation hasn't fired yet
            if (!ss.HasRevelation(rc.RequiredRevelation)) continue;

            // Skip if already added to the knowledge graph
            if (existingIds.Contains(rc.ConnectionId)) continue;

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

    // ── GATE.T57.KG.PLAYER_VERBS.001: Player KG verb implementations ──

    /// <summary>Pin a discovery node (max 3 pins). Returns false if at max or already pinned.</summary>
    public static bool PinDiscovery(SimState state, string discoveryId)
    {
        if (state?.Intel?.KGPlayerState is null || string.IsNullOrEmpty(discoveryId)) return false;
        var pins = state.Intel.KGPlayerState.Pins;

        // Check if already pinned.
        foreach (var p in pins)
            if (string.Equals(p.DiscoveryId, discoveryId, StringComparison.Ordinal)) return false;

        if (pins.Count >= 3) return false; // STRUCTURAL: max 3 pins

        pins.Add(new Entities.KGPin { DiscoveryId = discoveryId, PinnedTick = state.Tick });
        return true;
    }

    /// <summary>Unpin a discovery node.</summary>
    public static bool UnpinDiscovery(SimState state, string discoveryId)
    {
        if (state?.Intel?.KGPlayerState is null || string.IsNullOrEmpty(discoveryId)) return false;
        var pins = state.Intel.KGPlayerState.Pins;
        for (int i = 0; i < pins.Count; i++)
        {
            if (string.Equals(pins[i].DiscoveryId, discoveryId, StringComparison.Ordinal))
            {
                pins.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    /// <summary>Annotate a discovery node (max 50 chars). Overwrites existing annotation.</summary>
    public static bool AnnotateDiscovery(SimState state, string discoveryId, string text)
    {
        if (state?.Intel?.KGPlayerState is null || string.IsNullOrEmpty(discoveryId)) return false;
        if (string.IsNullOrEmpty(text)) return false;
        if (text.Length > 50) text = text.Substring(0, 50); // STRUCTURAL: max 50 chars

        var annotations = state.Intel.KGPlayerState.Annotations;

        // Overwrite existing annotation for this discovery.
        for (int i = 0; i < annotations.Count; i++)
        {
            if (string.Equals(annotations[i].DiscoveryId, discoveryId, StringComparison.Ordinal))
            {
                annotations[i].Text = text;
                annotations[i].CreatedTick = state.Tick;
                return true;
            }
        }

        annotations.Add(new Entities.KGAnnotation
        {
            DiscoveryId = discoveryId,
            Text = text,
            CreatedTick = state.Tick
        });
        return true;
    }

    /// <summary>Create a speculative link between two discoveries (player hypothesis).</summary>
    public static bool CreateSpeculativeLink(SimState state, string sourceDiscoveryId, string targetDiscoveryId)
    {
        if (state?.Intel?.KnowledgeConnections is null) return false;
        if (string.IsNullOrEmpty(sourceDiscoveryId) || string.IsNullOrEmpty(targetDiscoveryId)) return false;
        if (string.Equals(sourceDiscoveryId, targetDiscoveryId, StringComparison.Ordinal)) return false;

        // Check if this link already exists.
        foreach (var conn in state.Intel.KnowledgeConnections)
        {
            if ((string.Equals(conn.SourceDiscoveryId, sourceDiscoveryId, StringComparison.Ordinal)
                && string.Equals(conn.TargetDiscoveryId, targetDiscoveryId, StringComparison.Ordinal))
                || (string.Equals(conn.SourceDiscoveryId, targetDiscoveryId, StringComparison.Ordinal)
                && string.Equals(conn.TargetDiscoveryId, sourceDiscoveryId, StringComparison.Ordinal)))
            {
                return false; // Already linked
            }
        }

        string connId = $"KC.PLAYER.{sourceDiscoveryId}_{targetDiscoveryId}";
        state.Intel.KnowledgeConnections.Add(new Entities.KnowledgeConnection
        {
            ConnectionId = connId,
            SourceDiscoveryId = sourceDiscoveryId,
            TargetDiscoveryId = targetDiscoveryId,
            ConnectionType = Entities.KnowledgeConnectionType.Lead,
            IsRevealed = true,
            RevealedTick = state.Tick,
            Description = "Player hypothesis: these discoveries may be connected.",
            LinkState = Entities.KGLinkState.Speculative
        });

        // GATE.T57.FEEL.AUDIO_CARD_HOOKS.001: Emit InsightChime audio cue on KG link creation.
        state.EmitFleetEvent(new SimCore.Events.FleetEvents.Event
        {
            Type = SimCore.Events.FleetEvents.FleetEventType.DiscoverySeen,
            DiscoveryId = sourceDiscoveryId,
            NodeId = "",
            AudioCue = "InsightChime"
        });
        return true;
    }

    /// <summary>Flag a discovery for FO evaluation.</summary>
    public static bool FlagForFO(SimState state, string discoveryId)
    {
        if (state?.Intel?.KGPlayerState is null || string.IsNullOrEmpty(discoveryId)) return false;
        var flags = state.Intel.KGPlayerState.FOFlags;

        // Check if already flagged.
        foreach (var f in flags)
            if (string.Equals(f.DiscoveryId, discoveryId, StringComparison.Ordinal) && !f.FOResponded) return false;

        flags.Add(new Entities.KGFOFlag
        {
            DiscoveryId = discoveryId,
            FlaggedTick = state.Tick
        });

        // Fire FO trigger for flagged discovery.
        FirstOfficerSystem.TryFireTrigger(state, "KG_PLAYER_FLAG");
        return true;
    }

    /// <summary>Compare two discoveries side by side.</summary>
    public static bool CompareDiscoveries(SimState state, string discoveryIdA, string discoveryIdB)
    {
        if (state?.Intel?.KGPlayerState is null) return false;
        if (string.IsNullOrEmpty(discoveryIdA) || string.IsNullOrEmpty(discoveryIdB)) return false;

        string pair = string.CompareOrdinal(discoveryIdA, discoveryIdB) <= 0
            ? discoveryIdA + "|" + discoveryIdB
            : discoveryIdB + "|" + discoveryIdA;

        if (state.Intel.KGPlayerState.ComparePairs.Contains(pair)) return false;
        state.Intel.KGPlayerState.ComparePairs.Add(pair);
        return true;
    }

    private static DiscoveryPhase? GetDiscoveryPhase(SimState state, string discoveryId)
    {
        if (string.IsNullOrEmpty(discoveryId)) return null;

        if (state.Intel.Discoveries.TryGetValue(discoveryId, out var ds))
            return ds.Phase;

        // Data logs are stored separately from discoveries but can be
        // endpoints of knowledge connections. Treat discovered logs as
        // Analyzed (the player has read the full conversation).
        if (state.DataLogs.TryGetValue(discoveryId, out var dl))
            return dl.IsDiscovered ? DiscoveryPhase.Analyzed : null;

        return null;
    }

    // GATE.T57.KG.LINK_FEEDBACK.001: Speculative link state machine.
    // States: Speculative → Plausible → Confirmed → Contradicted.
    // Plausible: shared attributes between endpoints.
    // Confirmed: chain/revelation proof or trade proof.
    // Contradicted: evidence disproves — never deletes, dims display.
    // Also: 3-Confirmed-link Insight bonus.
    private static void ProcessLinkStateMachine(SimState state)
    {
        if (state.Intel?.KnowledgeConnections is null) return;

        int confirmedCount = 0; // STRUCTURAL: insight bonus counter

        for (int i = 0; i < state.Intel.KnowledgeConnections.Count; i++)
        {
            var conn = state.Intel.KnowledgeConnections[i];
            if (conn.LinkState == Entities.KGLinkState.None || conn.LinkState == Entities.KGLinkState.Confirmed)
            {
                if (conn.LinkState == Entities.KGLinkState.Confirmed) confirmedCount++;
                continue;
            }
            if (conn.LinkState == Entities.KGLinkState.Contradicted) continue;

            // Only process player-created speculative/plausible links.
            if (!conn.ConnectionId.StartsWith("KC.PLAYER.", StringComparison.Ordinal)) continue;

            var srcPhase = GetDiscoveryPhase(state, conn.SourceDiscoveryId);
            var tgtPhase = GetDiscoveryPhase(state, conn.TargetDiscoveryId);

            // Contradicted: one endpoint not found or doesn't exist (data mismatch).
            if (srcPhase == null || tgtPhase == null)
            {
                conn.LinkState = Entities.KGLinkState.Contradicted;
                continue;
            }

            // Check for confirmation evidence.
            bool confirmed = false;

            // Confirmation path 1: Both endpoints Analyzed → confirmed.
            if (srcPhase >= DiscoveryPhase.Analyzed && tgtPhase >= DiscoveryPhase.Analyzed)
            {
                // Check if a chain connection exists between these endpoints (strongest evidence).
                confirmed = HasChainOrRevelationProof(state, conn.SourceDiscoveryId, conn.TargetDiscoveryId);
            }

            // Confirmation path 2: A non-player connection also links these endpoints → confirmed.
            if (!confirmed)
            {
                foreach (var other in state.Intel.KnowledgeConnections)
                {
                    if (other == conn) continue;
                    if (other.ConnectionId.StartsWith("KC.PLAYER.", StringComparison.Ordinal)) continue;
                    if ((string.Equals(other.SourceDiscoveryId, conn.SourceDiscoveryId, StringComparison.Ordinal)
                        && string.Equals(other.TargetDiscoveryId, conn.TargetDiscoveryId, StringComparison.Ordinal))
                        || (string.Equals(other.SourceDiscoveryId, conn.TargetDiscoveryId, StringComparison.Ordinal)
                        && string.Equals(other.TargetDiscoveryId, conn.SourceDiscoveryId, StringComparison.Ordinal)))
                    {
                        if (other.IsRevealed)
                        {
                            confirmed = true;
                            break;
                        }
                    }
                }
            }

            if (confirmed)
            {
                conn.LinkState = Entities.KGLinkState.Confirmed;
                confirmedCount++;
                continue;
            }

            // Plausible: shared discovery kind between endpoints.
            if (conn.LinkState == Entities.KGLinkState.Speculative)
            {
                bool plausible = HasSharedAttributes(state, conn.SourceDiscoveryId, conn.TargetDiscoveryId);
                if (plausible)
                    conn.LinkState = Entities.KGLinkState.Plausible;
            }
        }

        // 3-link Insight bonus: when 3+ links are Confirmed, fire FO trigger.
        if (confirmedCount >= 3) // STRUCTURAL: insight threshold
        {
            FirstOfficerSystem.TryFireTrigger(state, "KG_INSIGHT_BONUS");
        }
    }

    private static bool HasChainOrRevelationProof(SimState state, string discA, string discB)
    {
        // Check anomaly chains for a step linking these discoveries.
        if (state.AnomalyChains is not null)
        {
            foreach (var chain in state.AnomalyChains.Values)
            {
                if (chain is null || chain.Steps is null) continue;
                bool hasA = false, hasB = false;
                foreach (var step in chain.Steps)
                {
                    if (string.Equals(step.PlacedDiscoveryId, discA, StringComparison.Ordinal)) hasA = true;
                    if (string.Equals(step.PlacedDiscoveryId, discB, StringComparison.Ordinal)) hasB = true;
                }
                if (hasA && hasB) return true;
            }
        }
        return false;
    }

    private static bool HasSharedAttributes(SimState state, string discA, string discB)
    {
        // Shared attributes = same discovery kind (parsed from discoveryId).
        string kindA = DiscoveryOutcomeSystem.ParseDiscoveryKind(discA);
        string kindB = DiscoveryOutcomeSystem.ParseDiscoveryKind(discB);

        if (!string.IsNullOrEmpty(kindA) && string.Equals(kindA, kindB, StringComparison.Ordinal))
            return true;

        // Shared node proximity: both discoveries in same node or adjacent.
        // (Simplified: check same node via discoveryId format disc_v0|KIND|NodeId|...)
        string nodeA = ExtractNodeFromDiscoveryId(discA);
        string nodeB = ExtractNodeFromDiscoveryId(discB);
        if (!string.IsNullOrEmpty(nodeA) && string.Equals(nodeA, nodeB, StringComparison.Ordinal))
            return true;

        return false;
    }

    private static string ExtractNodeFromDiscoveryId(string discoveryId)
    {
        if (string.IsNullOrEmpty(discoveryId)) return "";
        var parts = discoveryId.Split('|');
        return parts.Length >= 3 ? parts[2] : "";
    }

    // GATE.T58.KG.MILESTONE_ENTITY.001: 7-milestone progressive disclosure evaluation.
    // Per ExplorationDiscovery.md R12: Geographic→Pin→Relational→Annotate→Flag→Link→Compare.
    private static void ProcessMilestones(SimState state)
    {
        var milestones = state.Intel.KGMilestones;
        if (milestones is null) return;

        int currentHighest = (int)milestones.HighestMilestone;

        // Count discoveries and connections for milestone thresholds.
        int seenCount = 0;
        int analyzedCount = 0;
        foreach (var disc in state.Intel.Discoveries.Values)
        {
            if (disc.Phase >= Entities.DiscoveryPhase.Seen) seenCount++;
            if (disc.Phase >= Entities.DiscoveryPhase.Analyzed) analyzedCount++;
        }

        int revealedConnections = 0;
        foreach (var conn in state.Intel.KnowledgeConnections)
        {
            if (conn.IsRevealed) revealedConnections++;
        }

        bool foPromoted = state.FirstOfficer?.IsPromoted == true;

        // M1: Geographic — first discovery Seen (always unlocked if any discovery exists).
        // Already the default HighestMilestone.

        // M2: Pin — 3 discoveries Seen.
        if (currentHighest < 1 && seenCount >= 3) // STRUCTURAL: M2 threshold
        {
            AdvanceMilestone(milestones, Entities.KGMilestone.Pin, state.Tick);
            currentHighest = 1;
        }

        // M3: Relational — first connection revealed.
        if (currentHighest < 2 && revealedConnections >= 1) // STRUCTURAL: M3 threshold
        {
            AdvanceMilestone(milestones, Entities.KGMilestone.Relational, state.Tick);
            currentHighest = 2;
        }

        // M4: Annotate — 5 discoveries + 1 Analyzed.
        if (currentHighest < 3 && seenCount >= 5 && analyzedCount >= 1) // STRUCTURAL: M4 thresholds
        {
            AdvanceMilestone(milestones, Entities.KGMilestone.Annotate, state.Tick);
            currentHighest = 3;
        }

        // M5: Flag — FO promoted + 3 Analyzed.
        if (currentHighest < 4 && foPromoted && analyzedCount >= 3) // STRUCTURAL: M5 thresholds
        {
            AdvanceMilestone(milestones, Entities.KGMilestone.Flag, state.Tick);
            currentHighest = 4;
        }

        // M6: Link — 2 connections revealed.
        if (currentHighest < 5 && revealedConnections >= 2) // STRUCTURAL: M6 threshold
        {
            AdvanceMilestone(milestones, Entities.KGMilestone.Link, state.Tick);
            currentHighest = 5;
        }

        // M7: Compare — 8 discoveries + 3 Analyzed.
        if (currentHighest < 6 && seenCount >= 8 && analyzedCount >= 3) // STRUCTURAL: M7 thresholds
        {
            AdvanceMilestone(milestones, Entities.KGMilestone.Compare, state.Tick);
        }
    }

    private static void AdvanceMilestone(Entities.KGMilestoneState milestones, Entities.KGMilestone milestone, int tick)
    {
        milestones.HighestMilestone = milestone;
        int key = (int)milestone;
        if (!milestones.MilestoneTicks.ContainsKey(key))
            milestones.MilestoneTicks[key] = tick;
        milestones.PendingMilestoneNotification = key;
    }
}
