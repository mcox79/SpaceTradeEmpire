#nullable enable

using Godot;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using System;
using System.Linq;

namespace SpaceTradeEmpire.Bridge;

// GATE.S1.MISSION.BRIDGE.001: SimBridge mission queries (active/list/accept).
public partial class SimBridge
{
    private Godot.Collections.Dictionary _cachedActiveMissionV0 = new Godot.Collections.Dictionary();
    private Godot.Collections.Array _cachedMissionListV0 = new Godot.Collections.Array();

    /// <summary>
    /// Returns the active mission snapshot: {mission_id, title, current_step, total_steps,
    /// objective_text, completed}. Empty mission_id means no active mission.
    /// Nonblocking: returns cached if read lock unavailable.
    /// </summary>
    public Godot.Collections.Dictionary GetActiveMissionV0()
    {
        TryExecuteSafeRead(state =>
        {
            var d = new Godot.Collections.Dictionary();
            var missions = state.Missions;

            if (missions is null || string.IsNullOrEmpty(missions.ActiveMissionId))
            {
                d["mission_id"] = "";
                d["title"] = "";
                d["current_step"] = 0;
                d["total_steps"] = 0;
                d["objective_text"] = "";
                d["completed"] = false;
            }
            else
            {
                var def = MissionSystem.GetMissionDef(missions.ActiveMissionId);
                d["mission_id"] = missions.ActiveMissionId;
                d["title"] = def?.Title ?? "";
                d["current_step"] = missions.CurrentStepIndex;
                d["total_steps"] = missions.ActiveSteps.Count;
                d["completed"] = false;

                if (missions.CurrentStepIndex >= 0 && missions.CurrentStepIndex < missions.ActiveSteps.Count)
                {
                    var step = missions.ActiveSteps[missions.CurrentStepIndex];
                    d["objective_text"] = step.ObjectiveText;
                    d["target_node_id"] = step.TargetNodeId ?? "";
                    d["target_good_id"] = step.TargetGoodId ?? "";
                    // GATE.S19.ONBOARD.MISSION_DEST.004: Resolve target node display name for waypoints.
                    var targetId = step.TargetNodeId ?? "";
                    if (!string.IsNullOrEmpty(targetId) && state.Nodes.TryGetValue(targetId, out var targetNode))
                        d["target_node_name"] = targetNode.Name ?? targetId;
                    else
                        d["target_node_name"] = targetId;
                }
                else
                {
                    d["objective_text"] = "";
                    d["target_node_id"] = "";
                    d["target_good_id"] = "";
                    d["target_node_name"] = "";
                }
            }

            _cachedActiveMissionV0 = d;
        });

        return _cachedActiveMissionV0;
    }

    /// <summary>
    /// Returns available missions (not active, not completed, prerequisites met).
    /// Each entry: {mission_id, title, description, reward}.
    /// Nonblocking: returns cached if read lock unavailable.
    /// </summary>
    public Godot.Collections.Array GetMissionListV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();

            var available = MissionSystem.GetAvailableMissions(state);
            foreach (var def in available)
            {
                var entry = new Godot.Collections.Dictionary
                {
                    ["mission_id"] = def.MissionId,
                    ["title"] = def.Title,
                    ["description"] = def.Description,
                    ["reward"] = def.CreditReward,
                };
                arr.Add(entry);
            }

            _cachedMissionListV0 = arr;
        });

        return _cachedMissionListV0;
    }

    // GATE.S9.MISSIONS.BRIDGE_EXT.001: Mission rewards preview and prerequisites detail.

    /// <summary>
    /// Returns reward preview for a mission: {mission_id, credit_reward, step_count, description}.
    /// Lets the player see what they'll get before accepting.
    /// </summary>
    public Godot.Collections.Dictionary GetMissionRewardsPreviewV0(string missionId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["mission_id"] = missionId ?? "",
            ["credit_reward"] = (long)0,
            ["step_count"] = 0,
            ["description"] = "",
            ["title"] = "",
        };

        if (string.IsNullOrEmpty(missionId)) return result;

        var def = MissionSystem.GetMissionDef(missionId);
        if (def == null) return result;

        result["credit_reward"] = def.CreditReward;
        result["step_count"] = def.Steps.Count;
        result["description"] = def.Description;
        result["title"] = def.Title;

        return result;
    }

    /// <summary>
    /// Returns prerequisite detail for a mission: {mission_id, prerequisites: [{mission_id, title, completed}]}.
    /// Shows what the player needs to complete before this mission unlocks.
    /// </summary>
    public Godot.Collections.Dictionary GetMissionPrerequisitesDetailV0(string missionId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["mission_id"] = missionId ?? "",
            ["prerequisites"] = new Godot.Collections.Array(),
            ["all_met"] = false,
        };

        if (string.IsNullOrEmpty(missionId)) return result;

        var def = MissionSystem.GetMissionDef(missionId);
        if (def == null) return result;

        var prereqs = new Godot.Collections.Array();
        bool allMet = true;

        TryExecuteSafeRead(state =>
        {
            foreach (var prereqId in def.Prerequisites)
            {
                var prereqDef = MissionSystem.GetMissionDef(prereqId);
                bool completed = state.Missions.CompletedMissionIds.Contains(prereqId);
                if (!completed) allMet = false;

                prereqs.Add(new Godot.Collections.Dictionary
                {
                    ["mission_id"] = prereqId,
                    ["title"] = prereqDef?.Title ?? prereqId,
                    ["completed"] = completed,
                });
            }
        });

        result["prerequisites"] = prereqs;
        result["all_met"] = allMet;

        return result;
    }

    /// <summary>
    /// Accept a mission by ID. Returns true if accepted.
    /// Write lock required.
    /// </summary>
    public bool AcceptMissionV0(string missionId)
    {
        if (string.IsNullOrWhiteSpace(missionId)) return false;
        if (IsLoading) return false;

        bool result = false;

        _stateLock.EnterWriteLock();
        try
        {
            result = MissionSystem.AcceptMission(_kernel.State, missionId);
            // Immediately evaluate triggers so already-met steps (e.g. "dock at station"
            // when already docked) advance without waiting for the next sim tick.
            if (result)
            {
                MissionSystem.Process(_kernel.State);
            }
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        return result;
    }
}
