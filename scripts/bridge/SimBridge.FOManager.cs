#nullable enable

using Godot;
using SimCore.Entities;
using SimCore.Systems;
using System;
using System.Linq;

namespace SpaceTradeEmpire.Bridge;

// GATE.T58.FO.MANAGER_BRIDGE.001: FO Trade Manager bridge queries.
// Empire Health, Dock Recap, LOA, Service Record, Flip Moment, Decision Dialogue.
public partial class SimBridge
{
    // Cached snapshots.
    private Godot.Collections.Dictionary _cachedEmpireHealthV0 = new();
    private Godot.Collections.Dictionary _cachedDockRecapV0 = new();
    private Godot.Collections.Dictionary _cachedLOATableV0 = new();
    private Godot.Collections.Dictionary _cachedServiceRecordV0 = new();
    private Godot.Collections.Dictionary _cachedFlipMomentV0 = new();
    private Godot.Collections.Dictionary _cachedActiveDecisionV0 = new();

    // ── Empire Health ──

    public Godot.Collections.Dictionary GetEmpireHealthV0()
    {
        TryExecuteSafeRead(state =>
        {
            var result = new Godot.Collections.Dictionary();
            var fo = state.FirstOfficer;
            if (fo?.EmpireHealth == null)
            {
                result["status"] = "None";
                result["total_managed_routes"] = 0;
                lock (_snapshotLock) { _cachedEmpireHealthV0 = result; }
                return;
            }

            var h = fo.EmpireHealth;
            result["status"] = h.Status.ToString();
            result["previous_status"] = h.PreviousStatus.ToString();
            result["last_transition_tick"] = h.LastTransitionTick;
            result["healthy_routes"] = h.HealthyRoutes;
            result["degraded_routes"] = h.DegradedRoutes;
            result["dead_routes"] = h.DeadRoutes;
            result["total_managed_routes"] = h.TotalManagedRoutes;
            result["sustain_low"] = h.SustainLow;
            result["sustain_critical"] = h.SustainCritical;
            result["ship_lost"] = h.ShipLost;

            lock (_snapshotLock) { _cachedEmpireHealthV0 = result; }
        });

        lock (_snapshotLock) { return _cachedEmpireHealthV0?.Duplicate(true) ?? new Godot.Collections.Dictionary(); }
    }

    // ── Dock Recap ──

    public Godot.Collections.Dictionary GetDockRecapV0()
    {
        TryExecuteSafeRead(state =>
        {
            var result = new Godot.Collections.Dictionary();
            var fo = state.FirstOfficer;
            if (fo?.DockRecap == null)
            {
                result["pending"] = false;
                lock (_snapshotLock) { _cachedDockRecapV0 = result; }
                return;
            }

            var r = fo.DockRecap;
            result["pending"] = r.PendingRecap;
            result["last_dock_tick"] = r.LastDockTick;
            result["trades_completed"] = r.TradesCompletedSinceLastDock;
            result["credits_earned"] = r.CreditsEarnedSinceLastDock;
            result["most_severe_issue"] = r.MostSevereIssue;
            result["best_opportunity"] = r.BestOpportunity;

            var lines = new Godot.Collections.Array();
            foreach (var line in r.RecapLines) lines.Add(line);
            result["lines"] = lines;

            lock (_snapshotLock) { _cachedDockRecapV0 = result; }
        });

        lock (_snapshotLock) { return _cachedDockRecapV0?.Duplicate(true) ?? new Godot.Collections.Dictionary(); }
    }

    /// <summary>Consume the pending recap (mark as read). Called by UI after display.</summary>
    public bool ConsumeDockRecapV0()
    {
        bool success = false;
        try
        {
            _stateLock.EnterWriteLock();
            try
            {
                var fo = _kernel.State.FirstOfficer;
                if (fo?.DockRecap != null && fo.DockRecap.PendingRecap)
                {
                    fo.DockRecap.PendingRecap = false;
                    fo.DockRecap.RecapLines.Clear();
                    success = true;
                }
            }
            finally { _stateLock.ExitWriteLock(); }
        }
        catch (Exception ex) { GD.PrintErr($"ConsumeDockRecapV0 error: {ex.Message}"); }
        return success;
    }

    // ── LOA Table ──

    public Godot.Collections.Dictionary GetLOATableV0()
    {
        TryExecuteSafeRead(state =>
        {
            var result = new Godot.Collections.Dictionary();
            var fo = state.FirstOfficer;
            if (fo?.LOA == null)
            {
                lock (_snapshotLock) { _cachedLOATableV0 = result; }
                return;
            }

            result["route_creation"] = fo.LOA.GetLevel(LOADomain.RouteCreation);
            result["route_optimization"] = fo.LOA.GetLevel(LOADomain.RouteOptimization);
            result["sustain_logistics"] = fo.LOA.GetLevel(LOADomain.SustainLogistics);
            result["ship_purchase"] = fo.LOA.GetLevel(LOADomain.ShipPurchase);
            result["warfront_response"] = fo.LOA.GetLevel(LOADomain.WarfrontResponse);
            result["construction"] = fo.LOA.GetLevel(LOADomain.Construction);
            result["revert_count"] = fo.LOA.RevertEntries.Count;

            lock (_snapshotLock) { _cachedLOATableV0 = result; }
        });

        lock (_snapshotLock) { return _cachedLOATableV0?.Duplicate(true) ?? new Godot.Collections.Dictionary(); }
    }

    /// <summary>Set LOA level for a domain. domain: 0-5 (LOADomain enum), level: 4-7.</summary>
    public bool SetLOALevelV0(int domain, int level)
    {
        bool success = false;
        try
        {
            _stateLock.EnterWriteLock();
            try
            {
                var fo = _kernel.State.FirstOfficer;
                if (fo?.LOA != null && domain >= 0 && domain <= 5)
                {
                    fo.LOA.SetLevel((LOADomain)domain, level);
                    success = true;
                }
            }
            finally { _stateLock.ExitWriteLock(); }
        }
        catch (Exception ex) { GD.PrintErr($"SetLOALevelV0 error: {ex.Message}"); }
        return success;
    }

    // ── Service Record ──

    public Godot.Collections.Dictionary GetServiceRecordV0()
    {
        TryExecuteSafeRead(state =>
        {
            var result = new Godot.Collections.Dictionary();
            var fo = state.FirstOfficer;
            if (fo?.ServiceRecord == null)
            {
                lock (_snapshotLock) { _cachedServiceRecordV0 = result; }
                return;
            }

            var sr = fo.ServiceRecord;
            result["routes_managed"] = sr.RoutesManaged;
            result["recommendations_taken"] = sr.RecommendationsTaken;
            result["recommendations_offered"] = sr.RecommendationsOffered;
            result["profitable_recommendations"] = sr.ProfitableRecommendations;
            result["crises_handled"] = sr.CrisesHandled;
            result["worst_call_description"] = sr.WorstCallDescription;
            result["worst_call_cost"] = sr.WorstCallCost;
            result["notable_description"] = sr.NotableDescription;
            result["history_count"] = sr.History.Count;

            // Last 5 history entries.
            var history = new Godot.Collections.Array();
            int start = Math.Max(0, sr.History.Count - 5);
            for (int i = start; i < sr.History.Count; i++)
            {
                var entry = sr.History[i];
                history.Add(new Godot.Collections.Dictionary
                {
                    ["tick"] = entry.Tick,
                    ["event_type"] = entry.EventType,
                    ["description"] = entry.Description,
                    ["credit_impact"] = entry.CreditImpact,
                    ["was_successful"] = entry.WasSuccessful
                });
            }
            result["recent_history"] = history;

            lock (_snapshotLock) { _cachedServiceRecordV0 = result; }
        });

        lock (_snapshotLock) { return _cachedServiceRecordV0?.Duplicate(true) ?? new Godot.Collections.Dictionary(); }
    }

    // ── Flip Moment ──

    public Godot.Collections.Dictionary GetFlipMomentV0()
    {
        TryExecuteSafeRead(state =>
        {
            var result = new Godot.Collections.Dictionary();
            var fo = state.FirstOfficer;
            if (fo?.FlipMoment == null)
            {
                result["has_flipped"] = false;
                lock (_snapshotLock) { _cachedFlipMomentV0 = result; }
                return;
            }

            var fm = fo.FlipMoment;
            result["has_flipped"] = fm.HasFlipped;
            result["flip_tick"] = fm.FlipTick;
            result["consecutive_positive_ticks"] = fm.ConsecutivePositiveTicks;
            result["last_tick_net_revenue"] = fm.LastTickNetRevenue;
            result["flip_event_pending"] = fm.FlipEventPending;

            lock (_snapshotLock) { _cachedFlipMomentV0 = result; }
        });

        lock (_snapshotLock) { return _cachedFlipMomentV0?.Duplicate(true) ?? new Godot.Collections.Dictionary(); }
    }

    /// <summary>Consume the flip event (mark as displayed). Called by UI after VFX/audio.</summary>
    public bool ConsumeFlipEventV0()
    {
        bool success = false;
        try
        {
            _stateLock.EnterWriteLock();
            try
            {
                var fo = _kernel.State.FirstOfficer;
                if (fo?.FlipMoment != null && fo.FlipMoment.FlipEventPending)
                {
                    fo.FlipMoment.FlipEventPending = false;
                    success = true;
                }
            }
            finally { _stateLock.ExitWriteLock(); }
        }
        catch (Exception ex) { GD.PrintErr($"ConsumeFlipEventV0 error: {ex.Message}"); }
        return success;
    }

    // ── Decision Dialogue ──

    public Godot.Collections.Dictionary GetActiveDecisionV0()
    {
        TryExecuteSafeRead(state =>
        {
            var result = new Godot.Collections.Dictionary();
            var fo = state.FirstOfficer;
            if (fo?.ActiveDecision == null)
            {
                result["has_decision"] = false;
                lock (_snapshotLock) { _cachedActiveDecisionV0 = result; }
                return;
            }

            var d = fo.ActiveDecision;
            result["has_decision"] = true;
            result["decision_id"] = d.DecisionId;
            result["type"] = d.Type.ToString();
            result["severity"] = d.Severity;
            result["situation"] = d.Situation;
            result["stakes"] = d.Stakes;
            result["recommended_index"] = d.RecommendedOptionIndex;
            result["presented_tick"] = d.PresentedTick;
            result["queue_size"] = fo.DecisionQueue.Count;

            var options = new Godot.Collections.Array();
            for (int i = 0; i < d.Options.Count; i++)
            {
                var opt = d.Options[i];
                options.Add(new Godot.Collections.Dictionary
                {
                    ["label"] = opt.Label,
                    ["description"] = opt.Description,
                    ["credit_impact"] = opt.CreditImpact,
                    ["risk_level"] = opt.RiskLevel,
                    ["exploration_value"] = opt.ExplorationValue,
                    ["consequence_color"] = opt.ConsequenceColor,
                    ["is_recommended"] = (i == d.RecommendedOptionIndex)
                });
            }
            result["options"] = options;

            lock (_snapshotLock) { _cachedActiveDecisionV0 = result; }
        });

        lock (_snapshotLock) { return _cachedActiveDecisionV0?.Duplicate(true) ?? new Godot.Collections.Dictionary(); }
    }

    // ── FO State (GATE.T65.FO.DOCK_WIRE.001) ──

    private Godot.Collections.Dictionary _cachedFOStateV0 = new();

    /// <summary>
    /// Returns FO dialogue state for bot verification. Exposes total dialogue lines,
    /// promotion status, pending line, and relationship score.
    /// </summary>
    public Godot.Collections.Dictionary GetFOStateV0()
    {
        TryExecuteSafeRead(state =>
        {
            var result = new Godot.Collections.Dictionary();
            var fo = state.FirstOfficer;
            if (fo == null)
            {
                result["is_promoted"] = false;
                result["total_lines"] = 0;
                result["pending_line"] = "";
                result["candidate_type"] = "";
                result["relationship_score"] = 0;
                result["last_dialogue_tick"] = 0;
                lock (_snapshotLock) { _cachedFOStateV0 = result; }
                return;
            }

            result["is_promoted"] = fo.IsPromoted;
            result["total_lines"] = fo.DialogueEventLog.Count;
            result["pending_line"] = fo.PendingDialogueLine ?? "";
            result["candidate_type"] = fo.CandidateType.ToString();
            result["relationship_score"] = fo.RelationshipScore;
            result["last_dialogue_tick"] = fo.LastDialogueTick;

            lock (_snapshotLock) { _cachedFOStateV0 = result; }
        });

        lock (_snapshotLock) { return _cachedFOStateV0?.Duplicate(true) ?? new Godot.Collections.Dictionary(); }
    }

    /// <summary>Player selects a decision option. Returns true if resolved.</summary>
    public bool ResolveDecisionV0(int optionIndex)
    {
        bool success = false;
        try
        {
            _stateLock.EnterWriteLock();
            try
            {
                success = DecisionDialogueSystem.ResolveDecision(_kernel.State, optionIndex);
            }
            finally { _stateLock.ExitWriteLock(); }
        }
        catch (Exception ex) { GD.PrintErr($"ResolveDecisionV0 error: {ex.Message}"); }
        return success;
    }
}
