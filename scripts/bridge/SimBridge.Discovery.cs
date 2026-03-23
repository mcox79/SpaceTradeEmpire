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
                    ["flavor_text"] = route.FlavorText
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
}
