#nullable enable

using Godot;
using SimCore;
using SimCore.Content;
using SimCore.Systems;
using System;
using System.Collections.Generic;

namespace SpaceTradeEmpire.Bridge;

// GATE.S4.CONSTR_PROG.BRIDGE.001: SimBridge construction queries + start intent.
public partial class SimBridge
{
    private Godot.Collections.Array _cachedConstructionProjectsV0 = new Godot.Collections.Array();

    /// <summary>
    /// Returns all active construction projects.
    /// [{project_id, project_def_id, node_id, current_step, total_steps, step_progress_ticks, ticks_per_step, completed, progress_pct}]
    /// </summary>
    public Godot.Collections.Array GetConstructionProjectsV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            var keys = new List<string>(state.Construction.Projects.Keys);
            keys.Sort(StringComparer.Ordinal);

            foreach (var key in keys)
            {
                var p = state.Construction.Projects[key];
                var totalTicks = p.TotalSteps * p.TicksPerStep;
                var elapsedTicks = (p.CurrentStep * p.TicksPerStep) + p.StepProgressTicks;
                var d = new Godot.Collections.Dictionary
                {
                    ["project_id"] = p.ProjectId,
                    ["project_def_id"] = p.ProjectDefId,
                    ["node_id"] = p.NodeId,
                    ["current_step"] = p.CurrentStep,
                    ["total_steps"] = p.TotalSteps,
                    ["step_progress_ticks"] = p.StepProgressTicks,
                    ["ticks_per_step"] = p.TicksPerStep,
                    ["completed"] = p.Completed,
                    ["progress_pct"] = totalTicks > 0 ? (elapsedTicks * 100) / totalTicks : 0,
                };
                arr.Add(d);
            }
            lock (_snapshotLock) { _cachedConstructionProjectsV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedConstructionProjectsV0; }
    }

    /// <summary>
    /// Returns progress for a specific project.
    /// {project_id, current_step, total_steps, step_progress_ticks, ticks_per_step, completed, progress_pct}
    /// </summary>
    public Godot.Collections.Dictionary GetConstructionProgressV0(string projectId)
    {
        var result = new Godot.Collections.Dictionary();
        if (string.IsNullOrEmpty(projectId)) return result;

        TryExecuteSafeRead(state =>
        {
            if (!state.Construction.Projects.TryGetValue(projectId, out var p)) return;
            var totalTicks = p.TotalSteps * p.TicksPerStep;
            var elapsedTicks = (p.CurrentStep * p.TicksPerStep) + p.StepProgressTicks;
            result["project_id"] = p.ProjectId;
            result["current_step"] = p.CurrentStep;
            result["total_steps"] = p.TotalSteps;
            result["step_progress_ticks"] = p.StepProgressTicks;
            result["ticks_per_step"] = p.TicksPerStep;
            result["completed"] = p.Completed;
            result["progress_pct"] = totalTicks > 0 ? (elapsedTicks * 100) / totalTicks : 0;
        });

        return result;
    }

    /// <summary>
    /// Returns why a construction project can't be started. Empty string = can start.
    /// </summary>
    public string GetConstructionBlockReasonV0(string projectDefId, string nodeId)
    {
        string reason = "";
        TryExecuteSafeRead(state =>
        {
            reason = ConstructionSystem.GetBlockReason(state, projectDefId, nodeId);
        });
        return reason;
    }

    /// <summary>
    /// Returns available construction project definitions.
    /// [{project_def_id, display_name, total_steps, ticks_per_step, credit_cost_per_step, prerequisites_met}]
    /// </summary>
    public Godot.Collections.Array GetAvailableConstructionDefsV0()
    {
        var arr = new Godot.Collections.Array();
        TryExecuteSafeRead(state =>
        {
            foreach (var def in ConstructionContentV0.AllProjects)
            {
                var d = new Godot.Collections.Dictionary
                {
                    ["project_def_id"] = def.ProjectDefId,
                    ["display_name"] = def.DisplayName,
                    ["total_steps"] = def.TotalSteps,
                    ["ticks_per_step"] = def.TicksPerStep,
                    ["credit_cost_per_step"] = def.CreditCostPerStep,
                    ["prerequisites_met"] = ConstructionContentV0.PrerequisitesMet(def.ProjectDefId, state.Tech.UnlockedTechIds),
                };
                arr.Add(d);
            }
        }, 0);
        return arr;
    }

    /// <summary>
    /// Starts a construction project. Returns {success, reason, project_id}.
    /// </summary>
    public Godot.Collections.Dictionary StartConstructionV0(string projectDefId, string nodeId)
    {
        var result = new Godot.Collections.Dictionary { ["success"] = false, ["reason"] = "", ["project_id"] = "" };
        _stateLock.EnterWriteLock();
        try
        {
            var r = ConstructionSystem.StartConstruction(_kernel.State, projectDefId, nodeId);
            result["success"] = r.Success;
            result["reason"] = r.Reason;
            result["project_id"] = r.ProjectId;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
        return result;
    }
}
