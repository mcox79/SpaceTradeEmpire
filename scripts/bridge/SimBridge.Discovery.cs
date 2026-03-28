#nullable enable

using Godot;
using SimCore;
using SimCore.Entities;
using SimCore.Programs;
using SimCore.Systems;
using SimCore.Tweaks;
using System;
using System.Linq;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge
{
    // --- Discovery-as-Automation bridge methods (GATE.T41.DISCOVERY.BRIDGE.001) ---

    // Cached snapshots (nonblocking UI readout).
    private Godot.Collections.Array _cachedDiscoveryTradeIntelV0 = new();
    private Godot.Collections.Array _cachedActiveChainsV0 = new();
    private Godot.Collections.Dictionary _cachedSurveyProgramStatusV0 = new();

    // GATE.T41.DISCOVERY.BRIDGE.001: Get trade routes derived from discoveries.
    public Godot.Collections.Array GetDiscoveryTradeIntelV0()
    {
        TryExecuteSafeRead(state =>
        {
            var result = new Godot.Collections.Array();
            if (state.Intel?.TradeRoutes == null) { lock (_snapshotLock) { _cachedDiscoveryTradeIntelV0 = result; } return; }

            foreach (var kv in state.Intel.TradeRoutes.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var route = kv.Value;
                if (string.IsNullOrEmpty(route.SourceDiscoveryId)) continue;

                var entry = new Godot.Collections.Dictionary
                {
                    ["route_id"] = route.RouteId,
                    ["source_node_id"] = route.SourceNodeId,
                    ["dest_node_id"] = route.DestNodeId,
                    ["good_id"] = route.GoodId,
                    ["estimated_profit"] = route.EstimatedProfitPerUnit,
                    ["discovered_tick"] = route.DiscoveredTick,
                    ["last_validated_tick"] = route.LastValidatedTick,
                    ["status"] = route.Status.ToString(),
                    ["source_discovery_id"] = route.SourceDiscoveryId,
                    ["flavor_text"] = route.FlavorText,
                    // GATE.T57.CENTAUR.CONFIDENCE_LANG.001: Confidence score + personality text.
                    ["confidence_score"] = route.ConfidenceScore,
                    ["confidence_text"] = route.ConfidenceText,
                    ["proven_trade_count"] = route.ProvenTradeCount
                };
                result.Add(entry);
            }

            lock (_snapshotLock) { _cachedDiscoveryTradeIntelV0 = result; }
        });

        lock (_snapshotLock) { return _cachedDiscoveryTradeIntelV0?.Duplicate(true) ?? new Godot.Collections.Array(); }
    }

    // GATE.T41.DISCOVERY.BRIDGE.001: Get active anomaly chains.
    public Godot.Collections.Array GetActiveChainsV0()
    {
        TryExecuteSafeRead(state =>
        {
            var result = new Godot.Collections.Array();
            if (state.AnomalyChains == null) { lock (_snapshotLock) { _cachedActiveChainsV0 = result; } return; }

            foreach (var kv in state.AnomalyChains.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var chain = kv.Value;
                var entry = new Godot.Collections.Dictionary
                {
                    ["chain_id"] = chain.ChainId,
                    ["status"] = chain.Status.ToString(),
                    ["current_step"] = chain.CurrentStepIndex,
                    ["total_steps"] = chain.Steps.Count,
                    ["started_tick"] = chain.StartedTick,
                    ["starter_node_id"] = chain.StarterNodeId
                };

                // Current step narrative (if active).
                if (chain.Status == AnomalyChainStatus.Active && chain.CurrentStepIndex < chain.Steps.Count)
                {
                    var step = chain.Steps[chain.CurrentStepIndex];
                    entry["current_step_kind"] = step.DiscoveryKind;
                    entry["current_step_narrative"] = step.NarrativeText;
                    entry["current_step_lead"] = step.LeadText;
                }

                result.Add(entry);
            }

            lock (_snapshotLock) { _cachedActiveChainsV0 = result; }
        });

        lock (_snapshotLock) { return _cachedActiveChainsV0?.Duplicate(true) ?? new Godot.Collections.Array(); }
    }

    // GATE.T41.DISCOVERY.BRIDGE.001: Get progress for a specific chain.
    public Godot.Collections.Dictionary GetChainProgressV0(string chainId)
    {
        var result = new Godot.Collections.Dictionary();

        TryExecuteSafeRead(state =>
        {
            if (state.AnomalyChains == null || !state.AnomalyChains.TryGetValue(chainId ?? "", out var chain))
            {
                result["found"] = false;
                return;
            }

            result["found"] = true;
            result["chain_id"] = chain.ChainId;
            result["status"] = chain.Status.ToString();
            result["current_step"] = chain.CurrentStepIndex;
            result["total_steps"] = chain.Steps.Count;

            var steps = new Godot.Collections.Array();
            foreach (var step in chain.Steps)
            {
                steps.Add(new Godot.Collections.Dictionary
                {
                    ["step_index"] = step.StepIndex,
                    ["kind"] = step.DiscoveryKind,
                    ["is_completed"] = step.IsCompleted,
                    ["narrative"] = step.NarrativeText,
                    ["lead"] = step.LeadText,
                    ["placed_discovery_id"] = step.PlacedDiscoveryId
                });
            }
            result["steps"] = steps;
        });

        return result;
    }

    // GATE.T41.DISCOVERY.BRIDGE.001: Create a survey program.
    public string CreateSurveyProgramV0(string family, string homeNodeId, int rangeHops, int cadenceTicks)
    {
        string programId = "";
        try
        {
            _stateLock.EnterWriteLock();
            try
            {
                var state = _kernel.State;
                programId = state.CreateSurveyProgramV0(family ?? "", homeNodeId ?? "", rangeHops, cadenceTicks);
            }
            finally { _stateLock.ExitWriteLock(); }
        }
        catch (Exception ex) { GD.PrintErr($"CreateSurveyProgramV0 error: {ex.Message}"); }
        return programId;
    }

    // GATE.T41.DISCOVERY.BRIDGE.001: Get survey program status.
    public Godot.Collections.Dictionary GetSurveyProgramStatusV0()
    {
        TryExecuteSafeRead(state =>
        {
            var result = new Godot.Collections.Dictionary();
            var programs = new Godot.Collections.Array();

            if (state.Programs?.Instances != null)
            {
                foreach (var kv in state.Programs.Instances.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    var p = kv.Value;
                    if (!string.Equals(p.Kind, ProgramKind.SurveyV0, StringComparison.Ordinal)) continue;

                    programs.Add(new Godot.Collections.Dictionary
                    {
                        ["id"] = p.Id,
                        ["family"] = p.SurveyFamily,
                        ["home_node_id"] = p.SiteId,
                        ["range_hops"] = p.SurveyRangeHops,
                        ["status"] = p.Status.ToString(),
                        ["cadence_ticks"] = p.CadenceTicks,
                        ["last_run_tick"] = p.LastRunTick,
                        ["next_run_tick"] = p.NextRunTick
                    });
                }
            }

            result["programs"] = programs;
            result["count"] = programs.Count;

            lock (_snapshotLock) { _cachedSurveyProgramStatusV0 = result; }
        });

        lock (_snapshotLock) { return _cachedSurveyProgramStatusV0?.Duplicate(true) ?? new Godot.Collections.Dictionary(); }
    }

    // GATE.T62.PIPELINE.SURVEY_BRIDGE.001: Get survey results — intel generated by survey programs.
    public Godot.Collections.Array GetSurveyResultsV0()
    {
        var result = new Godot.Collections.Array();
        TryExecuteSafeRead(state =>
        {
            if (state.Intel?.EconomicIntels == null) return;

            // Survey-generated intels have source discovery IDs from survey scans.
            foreach (var kv in state.Intel.EconomicIntels.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var intel = kv.Value;
                if (intel == null) continue;
                // Survey intels are identified by source discoveries starting with "SURVEY_".
                if (!intel.SourceDiscoveryId.StartsWith("SURVEY_", StringComparison.Ordinal)
                    && !intel.SourceDiscoveryId.StartsWith("survey_", StringComparison.Ordinal))
                    continue;

                int age = state.Tick - intel.CreatedTick;
                int freshnessPct = intel.FreshnessMaxTicks > 0
                    ? Math.Max(0, 100 - (age * 100 / intel.FreshnessMaxTicks))
                    : 100;

                result.Add(new Godot.Collections.Dictionary
                {
                    ["intel_id"] = intel.IntelId,
                    ["type"] = intel.Type.ToString(),
                    ["node_id"] = intel.NodeId,
                    ["good_id"] = intel.GoodId,
                    ["estimated_value"] = intel.EstimatedValue,
                    ["freshness_pct"] = freshnessPct,
                    ["source_discovery_id"] = intel.SourceDiscoveryId,
                });
            }
        });
        return result;
    }

    // GATE.T41.DISCOVERY.BRIDGE.001: Check if survey program is unlocked for a family.
    public bool IsSurveyUnlockedV0(string family)
    {
        bool unlocked = false;
        TryExecuteSafeRead(state =>
        {
            int count = DiscoveryOutcomeSystem.GetManualScanCountByFamily(state, family ?? "");
            unlocked = count >= SurveyProgramTweaksV0.ManualScanGateCount;
        });
        return unlocked;
    }

    // GATE.T57.FEEL.AUDIO_SIGS.001: Get the latest discovery audio cue from fleet events.
    // Returns the AudioCue string ("AnomalyPing", "ScanProcess", "DiscoveryReveal", "InsightChime", "ScanFailed", or "").
    private string _cachedLatestAudioCue = "";

    public string GetLatestDiscoveryAudioCueV0()
    {
        TryExecuteSafeRead(state =>
        {
            string latest = "";
            if (state.FleetEventLog != null)
            {
                for (int i = state.FleetEventLog.Count - 1; i >= 0; i--)
                {
                    var evt = state.FleetEventLog[i];
                    if (evt == null) continue;
                    if (!string.IsNullOrEmpty(evt.AudioCue))
                    {
                        latest = evt.AudioCue;
                        break;
                    }
                }
            }
            lock (_snapshotLock) { _cachedLatestAudioCue = latest; }
        });
        lock (_snapshotLock) { return _cachedLatestAudioCue ?? ""; }
    }

    // GATE.T57.FEEL.DISCOVERY_FAILURE.001: Get failure status for a specific discovery.
    public Godot.Collections.Dictionary GetDiscoveryFailureStatusV0(string discoveryId)
    {
        var result = new Godot.Collections.Dictionary();
        TryExecuteSafeRead(state =>
        {
            if (state.Intel?.Discoveries == null || !state.Intel.Discoveries.TryGetValue(discoveryId ?? "", out var disc))
            {
                result["found"] = false;
                return;
            }
            result["found"] = true;
            result["last_failure_tick"] = disc.LastFailureTick;
            result["last_failure_type"] = disc.LastFailureType.ToString();
            result["failure_count"] = disc.FailureCount;
            result["on_cooldown"] = disc.LastFailureTick >= 0
                && (state.Tick - disc.LastFailureTick) < SimCore.Tweaks.DiscoveryFailureTweaksV0.FailureCooldownTicks;
        });
        return result;
    }

    // GATE.T57.PIPELINE.ECONOMIC_INTEL.001: Get economic intels.
    private Godot.Collections.Array _cachedEconomicIntelsV0 = new();

    public Godot.Collections.Array GetEconomicIntelsV0()
    {
        TryExecuteSafeRead(state =>
        {
            var result = new Godot.Collections.Array();
            if (state.Intel?.EconomicIntels == null)
            {
                lock (_snapshotLock) { _cachedEconomicIntelsV0 = result; }
                return;
            }

            foreach (var kv in state.Intel.EconomicIntels.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var intel = kv.Value;
                // GATE.T62.PIPELINE.INTEL_UI.001: Add freshness + margin confidence indicators.
                int age = state.Tick - intel.CreatedTick;
                int freshnessPct = intel.FreshnessMaxTicks > 0
                    ? Math.Max(0, 100 - (age * 100 / intel.FreshnessMaxTicks))
                    : 100; // STRUCTURAL: fracture = always 100%
                int marginConfBps = DiscoveryOutcomeSystem.ComputeMarginConfidenceBps(state.Tick, intel);
                string freshnessLabel = freshnessPct > 66 ? "Fresh" : (freshnessPct > 33 ? "Aging" : (freshnessPct > 0 ? "Stale" : "Expired"));

                result.Add(new Godot.Collections.Dictionary
                {
                    ["intel_id"] = intel.IntelId,
                    ["type"] = intel.Type.ToString(),
                    ["source_discovery_id"] = intel.SourceDiscoveryId,
                    ["node_id"] = intel.NodeId,
                    ["good_id"] = intel.GoodId,
                    ["estimated_value"] = intel.EstimatedValue,
                    ["created_tick"] = intel.CreatedTick,
                    ["freshness_max_ticks"] = intel.FreshnessMaxTicks,
                    ["distance_band"] = intel.DistanceBand,
                    ["flavor_text"] = intel.FlavorText,
                    ["fo_commentary"] = intel.FoCommentary,
                    ["freshness_pct"] = freshnessPct,
                    ["freshness_label"] = freshnessLabel,
                    ["margin_confidence_bps"] = marginConfBps,
                    ["ticks_remaining"] = intel.FreshnessMaxTicks > 0 ? Math.Max(0, intel.FreshnessMaxTicks - age) : -1,
                });
            }

            lock (_snapshotLock) { _cachedEconomicIntelsV0 = result; }
        });

        lock (_snapshotLock) { return _cachedEconomicIntelsV0?.Duplicate(true) ?? new Godot.Collections.Array(); }
    }

    // GATE.T57.PIPELINE.BRIDGE.001: Get a single economic intel by discovery ID.
    public Godot.Collections.Dictionary GetEconomicIntelByDiscoveryV0(string discoveryId)
    {
        var result = new Godot.Collections.Dictionary();
        TryExecuteSafeRead(state =>
        {
            if (state.Intel?.EconomicIntels == null)
            {
                result["found"] = false;
                return;
            }

            string key = "ECON_" + (discoveryId ?? "");
            if (!state.Intel.EconomicIntels.TryGetValue(key, out var intel))
            {
                result["found"] = false;
                return;
            }

            result["found"] = true;
            result["intel_id"] = intel.IntelId;
            result["type"] = intel.Type.ToString();
            result["node_id"] = intel.NodeId;
            result["good_id"] = intel.GoodId;
            result["estimated_value"] = intel.EstimatedValue;
            result["created_tick"] = intel.CreatedTick;
            result["freshness_max_ticks"] = intel.FreshnessMaxTicks;
            result["distance_band"] = intel.DistanceBand;
            result["flavor_text"] = intel.FlavorText;
            result["fo_commentary"] = intel.FoCommentary;
        });
        return result;
    }

    // GATE.T57.PIPELINE.BRIDGE.001: Get chain intel entries for a specific chain.
    public Godot.Collections.Array GetChainIntelV0(string chainId)
    {
        var result = new Godot.Collections.Array();
        TryExecuteSafeRead(state =>
        {
            if (state.Intel?.EconomicIntels == null) return;

            string prefix = "ECON_CHAIN_" + (chainId ?? "") + "_";
            foreach (var kv in state.Intel.EconomicIntels.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                if (!kv.Key.StartsWith(prefix, StringComparison.Ordinal)) continue;
                var intel = kv.Value;
                result.Add(new Godot.Collections.Dictionary
                {
                    ["intel_id"] = intel.IntelId,
                    ["type"] = intel.Type.ToString(),
                    ["node_id"] = intel.NodeId,
                    ["estimated_value"] = intel.EstimatedValue,
                    ["created_tick"] = intel.CreatedTick,
                    ["flavor_text"] = intel.FlavorText,
                    ["fo_commentary"] = intel.FoCommentary
                });
            }
        });
        return result;
    }

    // ── GATE.T57.KG.BRIDGE.001: Knowledge Graph bridge methods ──

    public bool PinDiscoveryV0(string discoveryId)
    {
        bool ok = false;
        try
        {
            _stateLock.EnterWriteLock();
            try { ok = KnowledgeGraphSystem.PinDiscovery(_kernel.State, discoveryId ?? ""); }
            finally { _stateLock.ExitWriteLock(); }
        }
        catch (Exception ex) { GD.PrintErr($"PinDiscoveryV0 error: {ex.Message}"); }
        return ok;
    }

    public bool UnpinDiscoveryV0(string discoveryId)
    {
        bool ok = false;
        try
        {
            _stateLock.EnterWriteLock();
            try { ok = KnowledgeGraphSystem.UnpinDiscovery(_kernel.State, discoveryId ?? ""); }
            finally { _stateLock.ExitWriteLock(); }
        }
        catch (Exception ex) { GD.PrintErr($"UnpinDiscoveryV0 error: {ex.Message}"); }
        return ok;
    }

    public bool AnnotateDiscoveryV0(string discoveryId, string text)
    {
        bool ok = false;
        try
        {
            _stateLock.EnterWriteLock();
            try { ok = KnowledgeGraphSystem.AnnotateDiscovery(_kernel.State, discoveryId ?? "", text ?? ""); }
            finally { _stateLock.ExitWriteLock(); }
        }
        catch (Exception ex) { GD.PrintErr($"AnnotateDiscoveryV0 error: {ex.Message}"); }
        return ok;
    }

    public bool LinkSpeculativeV0(string sourceDiscoveryId, string targetDiscoveryId)
    {
        bool ok = false;
        try
        {
            _stateLock.EnterWriteLock();
            try { ok = KnowledgeGraphSystem.CreateSpeculativeLink(_kernel.State, sourceDiscoveryId ?? "", targetDiscoveryId ?? ""); }
            finally { _stateLock.ExitWriteLock(); }
        }
        catch (Exception ex) { GD.PrintErr($"LinkSpeculativeV0 error: {ex.Message}"); }
        return ok;
    }

    public bool FlagForFOV0(string discoveryId)
    {
        bool ok = false;
        try
        {
            _stateLock.EnterWriteLock();
            try { ok = KnowledgeGraphSystem.FlagForFO(_kernel.State, discoveryId ?? ""); }
            finally { _stateLock.ExitWriteLock(); }
        }
        catch (Exception ex) { GD.PrintErr($"FlagForFOV0 error: {ex.Message}"); }
        return ok;
    }

    public bool CompareDiscoveriesV0(string discoveryIdA, string discoveryIdB)
    {
        bool ok = false;
        try
        {
            _stateLock.EnterWriteLock();
            try { ok = KnowledgeGraphSystem.CompareDiscoveries(_kernel.State, discoveryIdA ?? "", discoveryIdB ?? ""); }
            finally { _stateLock.ExitWriteLock(); }
        }
        catch (Exception ex) { GD.PrintErr($"CompareDiscoveriesV0 error: {ex.Message}"); }
        return ok;
    }

    // GATE.T57.KG.BRIDGE.001: Get KG dual-mode data (geographic + relational positions).
    public Godot.Collections.Dictionary GetKGDualModeV0()
    {
        var result = new Godot.Collections.Dictionary();
        TryExecuteSafeRead(state =>
        {
            if (state.Intel == null)
            {
                result["discoveries"] = new Godot.Collections.Array();
                result["connections"] = new Godot.Collections.Array();
                result["pins"] = new Godot.Collections.Array();
                result["annotations"] = new Godot.Collections.Array();
                return;
            }

            // Discoveries with positions.
            var discoveries = new Godot.Collections.Array();
            if (state.Intel.Discoveries != null)
            {
                foreach (var kv in state.Intel.Discoveries.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    var disc = kv.Value;
                    if (disc == null) continue;

                    // Find node for geographic position.
                    string nodeId = "";
                    float posX = 0, posY = 0, posZ = 0;
                    foreach (var nodeKvp in state.Nodes)
                    {
                        var node = nodeKvp.Value;
                        if (node?.SeededDiscoveryIds == null) continue;
                        if (node.SeededDiscoveryIds.Contains(disc.DiscoveryId))
                        {
                            nodeId = node.Id ?? "";
                            posX = node.Position.X;
                            posY = node.Position.Y;
                            posZ = node.Position.Z;
                            break;
                        }
                    }

                    discoveries.Add(new Godot.Collections.Dictionary
                    {
                        ["discovery_id"] = disc.DiscoveryId,
                        ["phase"] = disc.Phase.ToString(),
                        ["node_id"] = nodeId,
                        ["pos_x"] = posX,
                        ["pos_y"] = posY,
                        ["pos_z"] = posZ,
                        ["flavor_text"] = disc.FlavorText
                    });
                }
            }
            result["discoveries"] = discoveries;

            // Connections with link state.
            var connections = new Godot.Collections.Array();
            foreach (var conn in state.Intel.KnowledgeConnections)
            {
                connections.Add(new Godot.Collections.Dictionary
                {
                    ["connection_id"] = conn.ConnectionId,
                    ["source_discovery_id"] = conn.SourceDiscoveryId,
                    ["target_discovery_id"] = conn.TargetDiscoveryId,
                    ["type"] = conn.ConnectionType.ToString(),
                    ["is_revealed"] = conn.IsRevealed,
                    ["link_state"] = conn.LinkState.ToString(),
                    ["description"] = conn.Description
                });
            }
            result["connections"] = connections;

            // Player state: pins, annotations.
            var pins = new Godot.Collections.Array();
            var annotations = new Godot.Collections.Array();
            if (state.Intel.KGPlayerState != null)
            {
                foreach (var p in state.Intel.KGPlayerState.Pins)
                    pins.Add(new Godot.Collections.Dictionary { ["discovery_id"] = p.DiscoveryId, ["pinned_tick"] = p.PinnedTick });
                foreach (var a in state.Intel.KGPlayerState.Annotations)
                    annotations.Add(new Godot.Collections.Dictionary { ["discovery_id"] = a.DiscoveryId, ["text"] = a.Text });
            }
            result["pins"] = pins;
            result["annotations"] = annotations;
        });
        return result;
    }

    // GATE.T41.DISCOVERY.BRIDGE.001: Get discovery sites revealed by instability.
    public Godot.Collections.Array GetInstabilityRevealedSitesV0()
    {
        var result = new Godot.Collections.Array();
        TryExecuteSafeRead(state =>
        {
            if (state.Intel?.Discoveries == null) return;

            foreach (var kv in state.Intel.Discoveries.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var disc = kv.Value;
                if (disc == null || disc.InstabilityGate <= 0) continue;

                // Check if the local node's instability meets the gate.
                string nodeId = "";
                foreach (var nodeKvp in state.Nodes)
                {
                    var node = nodeKvp.Value;
                    if (node?.SeededDiscoveryIds == null) continue;
                    if (node.SeededDiscoveryIds.Contains(disc.DiscoveryId))
                    {
                        nodeId = node.Id ?? "";
                        break;
                    }
                }

                int localInstability = 0;
                if (!string.IsNullOrEmpty(nodeId) && state.Nodes.TryGetValue(nodeId, out var n))
                    localInstability = n.InstabilityLevel;

                bool isRevealed = localInstability >= disc.InstabilityGate;

                result.Add(new Godot.Collections.Dictionary
                {
                    ["discovery_id"] = disc.DiscoveryId,
                    ["instability_gate"] = disc.InstabilityGate,
                    ["node_id"] = nodeId,
                    ["local_instability"] = localInstability,
                    ["is_revealed"] = isRevealed,
                    ["phase"] = disc.Phase.ToString(),
                    ["flavor_text"] = disc.FlavorText
                });
            }
        });
        return result;
    }

    // ── GATE.T58.KG.MILESTONE_BRIDGE.001: KG progressive disclosure milestone queries ──

    private Godot.Collections.Dictionary _cachedKGMilestoneV0 = new();

    /// <summary>Get current KG milestone state and available verbs.</summary>
    public Godot.Collections.Dictionary GetKGMilestoneV0()
    {
        TryExecuteSafeRead(state =>
        {
            var result = new Godot.Collections.Dictionary();
            var milestones = state.Intel?.KGMilestones;
            if (milestones == null)
            {
                result["highest_milestone"] = "Geographic";
                result["pending_notification"] = -1;
                lock (_snapshotLock) { _cachedKGMilestoneV0 = result; }
                return;
            }

            result["highest_milestone"] = milestones.HighestMilestone.ToString();
            result["highest_milestone_int"] = (int)milestones.HighestMilestone;
            result["pending_notification"] = milestones.PendingMilestoneNotification;

            // Available verbs based on milestone.
            int level = (int)milestones.HighestMilestone;
            result["can_pin"] = level >= 1;       // M2: Pin
            result["can_annotate"] = level >= 3;   // M4: Annotate
            result["can_flag"] = level >= 4;       // M5: Flag
            result["can_link"] = level >= 5;       // M6: Link
            result["can_compare"] = level >= 6;    // M7: Compare

            // Milestone ticks for UI timeline.
            var ticks = new Godot.Collections.Dictionary();
            foreach (var kv in milestones.MilestoneTicks)
                ticks[kv.Key.ToString()] = kv.Value;
            result["milestone_ticks"] = ticks;

            lock (_snapshotLock) { _cachedKGMilestoneV0 = result; }
        });

        lock (_snapshotLock) { return _cachedKGMilestoneV0?.Duplicate(true) ?? new Godot.Collections.Dictionary(); }
    }

    /// <summary>Consume the milestone notification (mark as displayed).</summary>
    public bool ConsumeKGMilestoneNotificationV0()
    {
        bool success = false;
        try
        {
            _stateLock.EnterWriteLock();
            try
            {
                var milestones = _kernel.State.Intel?.KGMilestones;
                if (milestones != null && milestones.PendingMilestoneNotification >= 0)
                {
                    milestones.PendingMilestoneNotification = -1;
                    success = true;
                }
            }
            finally { _stateLock.ExitWriteLock(); }
        }
        catch (Exception ex) { GD.PrintErr($"ConsumeKGMilestoneNotificationV0 error: {ex.Message}"); }
        return success;
    }
}
