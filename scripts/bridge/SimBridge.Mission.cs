#nullable enable

using Godot;
using SimCore;
using SimCore.Content;
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

    // GATE.S9.SYSTEMIC.BRIDGE.001: Systemic mission offer queries and acceptance.

    /// <summary>
    /// Returns the active systemic mission offers from world state.
    /// Each entry: {offer_id, trigger_type, node_id, good_id, created_tick, expiry_tick}.
    /// Nonblocking: returns cached if read lock unavailable.
    /// </summary>
    public Godot.Collections.Array GetSystemicOffersV0()
    {
        var result = new Godot.Collections.Array();
        if (IsLoading) return result;

        TryExecuteSafeRead(state =>
        {
            var offers = state.SystemicOffers;
            if (offers is null || offers.Count == 0) return;

            foreach (var o in offers)
            {
                result.Add(new Godot.Collections.Dictionary
                {
                    ["offer_id"] = o.OfferId ?? "",
                    ["trigger_type"] = o.TriggerType.ToString(),
                    ["node_id"] = o.NodeId ?? "",
                    ["good_id"] = o.GoodId ?? "",
                    ["created_tick"] = o.CreatedTick,
                    ["expiry_tick"] = o.ExpiryTick,
                });
            }
        });

        return result;
    }

    /// <summary>
    /// Accept a systemic mission offer by ID. Delegates to
    /// SystemicMissionSystem.AcceptSystemicMission which builds a proper MissionDef
    /// from the offer and activates it. Write lock required.
    /// Returns true if accepted successfully.
    /// </summary>
    public bool AcceptSystemicMissionV0(string offerId)
    {
        if (string.IsNullOrWhiteSpace(offerId)) return false;
        if (IsLoading) return false;

        bool result = false;

        _stateLock.EnterWriteLock();
        try
        {
            result = SystemicMissionSystem.AcceptSystemicMission(_kernel.State, offerId);
            if (result)
                MissionSystem.Process(_kernel.State);
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        return result;
    }

    // GATE.T48.TEMPLATE.CONTEXT_SURFACE.001: Contextual template opportunities at a station.

    /// <summary>
    /// Returns up to 2 contextual template mission opportunities at the given node.
    /// Each entry: {template_id, display_name, archetype, situation_description}.
    /// Nonblocking: returns empty array if read lock unavailable.
    /// </summary>
    public Godot.Collections.Array GetContextualTemplatesV0(string nodeId)
    {
        var arr = new Godot.Collections.Array();
        if (string.IsNullOrEmpty(nodeId)) return arr;
        if (IsLoading) return arr;

        TryExecuteSafeRead(state =>
        {
            var templates = StationContextSystem.GetContextualTemplates(state, nodeId);
            foreach (var (templateId, displayName, archetype, situationDesc) in templates)
            {
                arr.Add(new Godot.Collections.Dictionary
                {
                    ["template_id"] = templateId,
                    ["display_name"] = displayName,
                    ["archetype"] = archetype.ToString(),
                    ["situation_description"] = situationDesc,
                });
            }
        });

        return arr;
    }

    /// <summary>
    /// Accept a contextual template mission by template ID.
    /// Returns {success (bool), mission_id (string)}.
    /// Write lock required.
    /// </summary>
    public Godot.Collections.Dictionary AcceptContextualTemplateV0(string templateId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["success"] = false,
            ["mission_id"] = "",
        };
        if (string.IsNullOrWhiteSpace(templateId)) return result;
        if (IsLoading) return result;

        _stateLock.EnterWriteLock();
        try
        {
            string missionId = MissionTemplateSystem.InstantiateTemplate(_kernel.State, templateId);
            if (!string.IsNullOrEmpty(missionId))
            {
                result["success"] = true;
                result["mission_id"] = missionId;
                // Immediately process so step triggers evaluate.
                MissionSystem.Process(_kernel.State);
            }
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        return result;
    }

    // GATE.X.UI_POLISH.QUEST_TRACKER.001: Lightweight active mission summary for HUD tracker.
    private Godot.Collections.Dictionary _cachedActiveMissionSummaryV0 = new Godot.Collections.Dictionary();

    /// <summary>
    /// Returns a compact active mission summary for the HUD quest tracker widget.
    /// {has_mission, mission_name, step_text, step_index, total_steps, progress}.
    /// Nonblocking: returns cached if read lock unavailable.
    /// </summary>
    public Godot.Collections.Dictionary GetActiveMissionSummaryV0()
    {
        TryExecuteSafeRead(state =>
        {
            var d = new Godot.Collections.Dictionary();
            var missions = state.Missions;

            if (missions is null || string.IsNullOrEmpty(missions.ActiveMissionId))
            {
                d["has_mission"] = false;
                d["mission_name"] = "";
                d["step_text"] = "";
                d["step_index"] = 0;
                d["total_steps"] = 0;
                d["progress"] = 0.0f;
            }
            else
            {
                d["has_mission"] = true;
                var def = MissionSystem.GetMissionDef(missions.ActiveMissionId);
                d["mission_name"] = def?.Title ?? missions.ActiveMissionId;
                d["total_steps"] = missions.ActiveSteps.Count;
                d["step_index"] = missions.CurrentStepIndex;

                if (missions.CurrentStepIndex >= 0 && missions.CurrentStepIndex < missions.ActiveSteps.Count)
                {
                    d["step_text"] = missions.ActiveSteps[missions.CurrentStepIndex].ObjectiveText;
                }
                else
                {
                    d["step_text"] = "";
                }

                d["progress"] = missions.ActiveSteps.Count > 0
                    ? (float)missions.CurrentStepIndex / missions.ActiveSteps.Count
                    : 0.0f;
            }

            _cachedActiveMissionSummaryV0 = d;
        });

        return _cachedActiveMissionSummaryV0;
    }

    // GATE.X.UI_POLISH.MISSION_JOURNAL.001: Abandon the active mission.
    public bool AbandonMissionV0()
    {
        if (IsLoading) return false;

        bool result = false;

        _stateLock.EnterWriteLock();
        try
        {
            result = MissionSystem.AbandonMission(_kernel.State);
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        return result;
    }

    // GATE.T52.MISSION.BRIDGE_M7M8.001: Diplomacy and smuggling mission-specific bridge queries.

    /// <summary>
    /// Returns diplomacy-specific data for an active template mission.
    /// {target_faction, rep_reward_tier, treaty_type, destination_node}.
    /// Returns empty Dictionary if mission is not a Diplomacy archetype.
    /// Nonblocking: returns cached if read lock unavailable.
    /// </summary>
    private Godot.Collections.Dictionary _cachedDiplomacyMissionDataV0 = new Godot.Collections.Dictionary();

    public Godot.Collections.Dictionary GetDiplomacyMissionDataV0(string missionId)
    {
        if (string.IsNullOrEmpty(missionId)) return new Godot.Collections.Dictionary();
        if (IsLoading) return _cachedDiplomacyMissionDataV0;

        TryExecuteSafeRead(state =>
        {
            var d = new Godot.Collections.Dictionary();

            // Find the template for this mission ID (format: TMPL_{templateId}_{tick}).
            var template = FindTemplatForMission(missionId);
            if (template is null || template.Archetype != MissionTemplateContentV0.Archetype.Diplomacy)
                return; // Not a diplomacy mission — leave cached as empty.

            var missions = state.Missions;
            if (missions is null || !string.Equals(missions.ActiveMissionId, missionId, StringComparison.Ordinal))
                return; // Not the active mission.

            // target_faction: from template FactionId, or resolved from player's current node faction.
            string targetFaction = template.FactionId;
            if (string.IsNullOrEmpty(targetFaction))
            {
                string playerNode = state.PlayerLocationNodeId ?? "";
                if (!string.IsNullOrEmpty(playerNode) && state.NodeFactionId.TryGetValue(playerNode, out var nf))
                    targetFaction = nf;
            }
            d["target_faction"] = targetFaction ?? "";

            // rep_reward_tier: template's RequiredRepTier indicates the tier threshold; higher = better reward.
            // Map: -1 (no req) → 1, 0 → 3, 1 → 2, 2 → 1.
            int repRewardTier = template.RequiredRepTier switch
            {
                0 => 3,
                1 => 2,
                2 => 1,
                _ => 1,
            };
            d["rep_reward_tier"] = repRewardTier;

            // treaty_type: derived from template ID keyword.
            string treatyType = "trade_agreement"; // default
            string tid = template.TemplateId;
            if (tid.Contains("ceasefire", StringComparison.OrdinalIgnoreCase))
                treatyType = "ceasefire";
            else if (tid.Contains("alliance", StringComparison.OrdinalIgnoreCase))
                treatyType = "alliance";
            else if (tid.Contains("envoy", StringComparison.OrdinalIgnoreCase) ||
                     tid.Contains("treaty", StringComparison.OrdinalIgnoreCase))
                treatyType = "trade_agreement";
            d["treaty_type"] = treatyType;

            // destination_node: current step's target node ID.
            string destNode = "";
            if (missions.CurrentStepIndex >= 0 && missions.CurrentStepIndex < missions.ActiveSteps.Count)
            {
                destNode = missions.ActiveSteps[missions.CurrentStepIndex].TargetNodeId ?? "";
            }
            d["destination_node"] = destNode;

            _cachedDiplomacyMissionDataV0 = d;
        });

        return _cachedDiplomacyMissionDataV0;
    }

    /// <summary>
    /// Returns smuggling-specific data for an active template mission.
    /// {contraband_good, trace_risk_bps, detection_chance_pct, blockade_active}.
    /// Returns empty Dictionary if mission is not a Smuggling archetype.
    /// Nonblocking: returns cached if read lock unavailable.
    /// </summary>
    private Godot.Collections.Dictionary _cachedSmugglingMissionDataV0 = new Godot.Collections.Dictionary();

    public Godot.Collections.Dictionary GetSmugglingMissionDataV0(string missionId)
    {
        if (string.IsNullOrEmpty(missionId)) return new Godot.Collections.Dictionary();
        if (IsLoading) return _cachedSmugglingMissionDataV0;

        TryExecuteSafeRead(state =>
        {
            var d = new Godot.Collections.Dictionary();

            // Find the template for this mission ID.
            var template = FindTemplatForMission(missionId);
            if (template is null || template.Archetype != MissionTemplateContentV0.Archetype.Smuggling)
                return; // Not a smuggling mission.

            var missions = state.Missions;
            if (missions is null || !string.Equals(missions.ActiveMissionId, missionId, StringComparison.Ordinal))
                return; // Not the active mission.

            // contraband_good: current step's target good, or first step with a good.
            string contrabandGood = "";
            if (missions.CurrentStepIndex >= 0 && missions.CurrentStepIndex < missions.ActiveSteps.Count)
            {
                contrabandGood = missions.ActiveSteps[missions.CurrentStepIndex].TargetGoodId ?? "";
            }
            if (string.IsNullOrEmpty(contrabandGood))
            {
                // Fallback: scan all steps for the first good.
                foreach (var step in missions.ActiveSteps)
                {
                    if (!string.IsNullOrEmpty(step.TargetGoodId))
                    {
                        contrabandGood = step.TargetGoodId;
                        break;
                    }
                }
            }
            d["contraband_good"] = contrabandGood;

            // trace_risk_bps: sum of twist slot weights (higher twists = more risky run).
            int traceRiskBps = 0;
            foreach (var twist in template.TwistSlotDefs)
            {
                traceRiskBps += twist.WeightBps;
            }
            d["trace_risk_bps"] = traceRiskBps;

            // detection_chance_pct: base from twist blockade weight, scaled by node instability.
            int detectionPct = 0;
            foreach (var twist in template.TwistSlotDefs)
            {
                if (string.Equals(twist.TwistType, "blockade", StringComparison.Ordinal))
                {
                    detectionPct = twist.WeightBps / 100; // bps to percent
                    break;
                }
            }
            // Scale by current node instability (0-10 range adds 0-10% extra).
            string playerNode = state.PlayerLocationNodeId ?? "";
            if (!string.IsNullOrEmpty(playerNode) && state.Nodes.TryGetValue(playerNode, out var pNode))
            {
                detectionPct += pNode.InstabilityLevel;
            }
            detectionPct = Math.Clamp(detectionPct, 0, 100);
            d["detection_chance_pct"] = detectionPct;

            // blockade_active: check if any step target node is in a warfront's contested nodes.
            bool blockadeActive = false;
            if (state.Warfronts is not null)
            {
                foreach (var step in missions.ActiveSteps)
                {
                    string targetNode = step.TargetNodeId ?? "";
                    if (string.IsNullOrEmpty(targetNode)) continue;

                    foreach (var wf in state.Warfronts.Values)
                    {
                        if (wf.ContestedNodeIds is not null && wf.ContestedNodeIds.Contains(targetNode))
                        {
                            blockadeActive = true;
                            break;
                        }
                    }
                    if (blockadeActive) break;
                }
            }
            d["blockade_active"] = blockadeActive;

            _cachedSmugglingMissionDataV0 = d;
        });

        return _cachedSmugglingMissionDataV0;
    }

    /// <summary>
    /// Helper: extract template ID from mission ID (format "TMPL_{templateId}_{tick}")
    /// and look up the template definition.
    /// </summary>
    private static MissionTemplateContentV0.MissionTemplateDef? FindTemplatForMission(string missionId)
    {
        if (string.IsNullOrEmpty(missionId) || !missionId.StartsWith("TMPL_", StringComparison.Ordinal))
            return null;

        int lastUnderscore = missionId.LastIndexOf('_');
        int prefixLen = 5; // "TMPL_" length
        if (lastUnderscore <= prefixLen) return null;

        string templateId = missionId.Substring(prefixLen, lastUnderscore - prefixLen);

        foreach (var t in MissionTemplateContentV0.AllTemplates)
        {
            if (string.Equals(t.TemplateId, templateId, StringComparison.Ordinal))
                return t;
        }
        return null;
    }
}
