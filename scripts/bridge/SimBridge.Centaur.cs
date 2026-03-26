#nullable enable

using Godot;
using SimCore.Entities;
using SimCore.Systems;
using System;
using System.Linq;

namespace SpaceTradeEmpire.Bridge;

// GATE.T57.CENTAUR.BRIDGE.001: Centaur model bridge queries.
public partial class SimBridge
{
    // Cached snapshots.
    private Godot.Collections.Dictionary _cachedFOCompetenceTierV0 = new();
    private Godot.Collections.Array _cachedRouteConfidenceV0 = new();
    private Godot.Collections.Array _cachedFOAdaptationLogV0 = new();

    // GATE.T57.CENTAUR.BRIDGE.001: Get FO competence tier and confidence score.
    public Godot.Collections.Dictionary GetFOCompetenceTierV0()
    {
        TryExecuteSafeRead(state =>
        {
            var result = new Godot.Collections.Dictionary();
            var fo = state.FirstOfficer;
            if (fo?.Competence == null)
            {
                result["tier"] = "None";
                result["confidence_score"] = 0;
                result["player_demoted"] = false;
                result["tier_up_tick"] = 0;
                lock (_snapshotLock) { _cachedFOCompetenceTierV0 = result; }
                return;
            }

            result["tier"] = fo.Competence.Tier.ToString();
            result["confidence_score"] = fo.Competence.ConfidenceScore;
            result["player_demoted"] = fo.Competence.PlayerDemoted;
            result["tier_up_tick"] = fo.Competence.TierUpTick;
            result["candidate_type"] = fo.CandidateType.ToString();
            result["is_promoted"] = fo.IsPromoted;

            lock (_snapshotLock) { _cachedFOCompetenceTierV0 = result; }
        });

        lock (_snapshotLock) { return _cachedFOCompetenceTierV0?.Duplicate(true) ?? new Godot.Collections.Dictionary(); }
    }

    // GATE.T57.CENTAUR.BRIDGE.001: Get route confidence scores for all trade routes.
    public Godot.Collections.Array GetRouteConfidenceV0()
    {
        TryExecuteSafeRead(state =>
        {
            var result = new Godot.Collections.Array();
            if (state.Intel?.TradeRoutes == null)
            {
                lock (_snapshotLock) { _cachedRouteConfidenceV0 = result; }
                return;
            }

            foreach (var kv in state.Intel.TradeRoutes.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var route = kv.Value;
                result.Add(new Godot.Collections.Dictionary
                {
                    ["route_id"] = route.RouteId,
                    ["confidence_score"] = route.ConfidenceScore,
                    ["confidence_text"] = route.ConfidenceText,
                    ["status"] = route.Status.ToString(),
                    ["proven_trade_count"] = route.ProvenTradeCount,
                    ["good_id"] = route.GoodId,
                    ["source_node_id"] = route.SourceNodeId,
                    ["dest_node_id"] = route.DestNodeId
                });
            }

            lock (_snapshotLock) { _cachedRouteConfidenceV0 = result; }
        });

        lock (_snapshotLock) { return _cachedRouteConfidenceV0?.Duplicate(true) ?? new Godot.Collections.Array(); }
    }

    // GATE.T57.CENTAUR.BRIDGE.001: Get FO adaptation events (flagged/paused routes).
    public Godot.Collections.Array GetFOAdaptationLogV0()
    {
        TryExecuteSafeRead(state =>
        {
            var result = new Godot.Collections.Array();
            if (state.Intel?.TradeRoutes == null)
            {
                lock (_snapshotLock) { _cachedFOAdaptationLogV0 = result; }
                return;
            }

            foreach (var kv in state.Intel.TradeRoutes.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var route = kv.Value;
                if (route.Status != TradeRouteStatus.Flagged && route.Status != TradeRouteStatus.Paused)
                    continue;

                result.Add(new Godot.Collections.Dictionary
                {
                    ["route_id"] = route.RouteId,
                    ["status"] = route.Status.ToString(),
                    ["good_id"] = route.GoodId,
                    ["source_node_id"] = route.SourceNodeId,
                    ["dest_node_id"] = route.DestNodeId,
                    ["confidence_score"] = route.ConfidenceScore,
                    ["confidence_text"] = route.ConfidenceText
                });
            }

            lock (_snapshotLock) { _cachedFOAdaptationLogV0 = result; }
        });

        lock (_snapshotLock) { return _cachedFOAdaptationLogV0?.Duplicate(true) ?? new Godot.Collections.Array(); }
    }

    // GATE.T57.CENTAUR.BOREDOM_TRIGGERS.001: Demote FO competence tier (player action).
    public bool DemoteFOCompetenceV0()
    {
        bool success = false;
        try
        {
            _stateLock.EnterWriteLock();
            try
            {
                FirstOfficerSystem.DemoteFOCompetence(_kernel.State);
                success = true;
            }
            finally { _stateLock.ExitWriteLock(); }
        }
        catch (Exception ex) { GD.PrintErr($"DemoteFOCompetenceV0 error: {ex.Message}"); }
        return success;
    }
}
