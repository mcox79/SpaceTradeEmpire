#nullable enable

using Godot;
using SimCore;
using SimCore.Content;
using System;

namespace SpaceTradeEmpire.Bridge;

// GATE.S7.AUTOMATION_MGMT.BRIDGE_QUERIES.001: Automation management query contracts.
public partial class SimBridge
{
    // ── GetProgramPerformanceV0 ──
    /// <summary>
    /// Returns fleet program performance metrics including cycle counts,
    /// goods moved, credits earned, budget info, and recent history.
    /// </summary>
    public Godot.Collections.Dictionary GetProgramPerformanceV0(string fleetId)
    {
        var result = new Godot.Collections.Dictionary();
        if (string.IsNullOrEmpty(fleetId)) return result;

        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue(fleetId, out var fleet)) return;

            var m = fleet.Metrics;
            result["fleet_id"] = fleetId;
            result["cycles_run"] = m.CyclesRun;
            result["goods_moved"] = m.GoodsMoved;
            result["credits_earned"] = m.CreditsEarned;
            result["failures"] = m.Failures;
            result["last_active_tick"] = m.LastActiveTick;
            result["spent_credits_this_cycle"] = m.SpentCreditsThisCycle;
            result["spent_goods_this_cycle"] = m.SpentGoodsThisCycle;

            // GATE.S7.AUTOMATION.PERF_TRACKING.001: Extended metrics.
            result["total_expense"] = m.TotalExpense;
            result["trades_completed"] = m.TradesCompleted;
            result["ticks_active"] = m.TicksActive;
            result["net_profit"] = m.CreditsEarned - m.TotalExpense;
            result["consecutive_failures"] = m.ConsecutiveFailures;
            result["last_failure_reason"] = m.LastFailureReason.ToString();

            // Budget info
            var b = fleet.Budget;
            result["budget_credit_cap"] = b.CreditCap;
            result["budget_goods_cap"] = b.GoodsCap;

            // History (last 10 entries, newest first)
            var historyArr = new Godot.Collections.Array();
            var history = fleet.History;
            int start = Math.Max(0, history.Count - 10);
            for (int i = history.Count - 1; i >= start; i--)
            {
                var h = history[i];
                historyArr.Add(new Godot.Collections.Dictionary
                {
                    ["tick"] = h.Tick,
                    ["success"] = h.Success,
                    ["goods_moved"] = h.GoodsMoved,
                    ["credits_earned"] = h.CreditsEarned,
                    ["failure_reason"] = h.FailureReason.ToString(),
                });
            }
            result["history"] = historyArr;
        }, 0);

        return result;
    }

    // ── GetProgramFailureReasonsV0 ──
    /// <summary>
    /// Returns fleet failure tracking data: total failures, consecutive count,
    /// last failure reason, and a breakdown by failure reason from history.
    /// </summary>
    public Godot.Collections.Dictionary GetProgramFailureReasonsV0(string fleetId)
    {
        var result = new Godot.Collections.Dictionary();
        if (string.IsNullOrEmpty(fleetId)) return result;

        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue(fleetId, out var fleet)) return;

            var m = fleet.Metrics;
            result["fleet_id"] = fleetId;
            result["total_failures"] = m.Failures;
            result["consecutive_failures"] = m.ConsecutiveFailures;
            result["last_failure_reason"] = m.LastFailureReason.ToString();

            // Breakdown by reason from history
            var breakdown = new Godot.Collections.Dictionary();
            foreach (var h in fleet.History)
            {
                if (!h.Success)
                {
                    var reason = h.FailureReason.ToString();
                    if (breakdown.ContainsKey(reason))
                        breakdown[reason] = (int)breakdown[reason] + 1;
                    else
                        breakdown[reason] = 1;
                }
            }
            result["failure_breakdown"] = breakdown;
        }, 0);

        return result;
    }

    // ── GetDoctrineSettingsV0 ──
    /// <summary>
    /// Returns fleet engagement doctrine settings: stance, retreat threshold,
    /// and patrol radius.
    /// </summary>
    public Godot.Collections.Dictionary GetDoctrineSettingsV0(string fleetId)
    {
        var result = new Godot.Collections.Dictionary();
        if (string.IsNullOrEmpty(fleetId)) return result;

        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue(fleetId, out var fleet)) return;

            var d = fleet.Doctrine;
            result["fleet_id"] = fleetId;
            result["stance"] = d.Stance.ToString();
            result["stance_value"] = (int)d.Stance;
            result["retreat_threshold_pct"] = d.RetreatThresholdPct;
            result["patrol_radius"] = d.PatrolRadius;
        }, 0);

        return result;
    }

    // ── SetDoctrineV0 ──
    // GATE.S7.AUTOMATION_MGMT.BRIDGE_WRITES.001: Write fleet doctrine settings.
    /// <summary>
    /// Sets fleet engagement doctrine: stance (Aggressive/Defensive/Evasive),
    /// retreat threshold (0-100 hull %), and patrol radius.
    /// </summary>
    public bool SetDoctrineV0(string fleetId, string stance, int retreatThreshold, int patrolRadius)
    {
        if (IsLoading) return false;
        if (string.IsNullOrEmpty(fleetId)) return false;

        // Map stance string to enum. Default to Defensive if unrecognized.
        SimCore.Entities.EngagementStance parsedStance;
        if (!System.Enum.TryParse<SimCore.Entities.EngagementStance>(stance, ignoreCase: true, out parsedStance))
            parsedStance = SimCore.Entities.EngagementStance.Defensive;

        // Clamp retreat threshold to valid range.
        retreatThreshold = Math.Clamp(retreatThreshold, 0, 100);

        // Clamp patrol radius to non-negative.
        if (patrolRadius < 0) patrolRadius = 0;

        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            if (!state.Fleets.TryGetValue(fleetId, out var fleet)) return false;

            fleet.Doctrine.Stance = parsedStance;
            fleet.Doctrine.RetreatThresholdPct = retreatThreshold;
            fleet.Doctrine.PatrolRadius = (float)patrolRadius;
            return true;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    // ── SetBudgetCapsV0 ──
    // GATE.S7.AUTOMATION_MGMT.BRIDGE_WRITES.001: Write automation budget caps.
    /// <summary>
    /// Sets per-fleet automation budget caps: max credits and goods per cycle.
    /// A value of 0 means unlimited.
    /// </summary>
    public bool SetBudgetCapsV0(string fleetId, int creditCap, int goodsCap)
    {
        if (IsLoading) return false;
        if (string.IsNullOrEmpty(fleetId)) return false;

        // Clamp to non-negative.
        if (creditCap < 0) creditCap = 0;
        if (goodsCap < 0) goodsCap = 0;

        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;
            if (!state.Fleets.TryGetValue(fleetId, out var fleet)) return false;

            fleet.Budget.CreditCap = (long)creditCap;
            fleet.Budget.GoodsCap = goodsCap;
            return true;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    // ── GetProgramTemplatesV0 ──
    // GATE.S7.AUTOMATION.TEMPLATES_UI.001: Query program preset templates from content registry.
    /// <summary>
    /// Returns an array of preset program template dictionaries.
    /// Each entry has: template_id, display_name, description, program_kind,
    /// default_cadence_ticks.
    /// </summary>
    public Godot.Collections.Array GetProgramTemplatesV0()
    {
        var arr = new Godot.Collections.Array();

        // Read from content registry (static data, no lock needed)
        foreach (var template in SimCore.Content.ProgramTemplateContentV0.AllTemplates)
        {
            arr.Add(new Godot.Collections.Dictionary
            {
                ["template_id"] = template.TemplateId,
                ["display_name"] = template.DisplayName,
                ["description"] = template.Description,
                ["program_kind"] = template.ProgramKind,
                ["default_cadence_ticks"] = template.DefaultCadenceTicks,
            });
        }

        return arr;
    }
}
