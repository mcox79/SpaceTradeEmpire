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
                }
                else
                {
                    d["objective_text"] = "";
                    d["target_node_id"] = "";
                    d["target_good_id"] = "";
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
