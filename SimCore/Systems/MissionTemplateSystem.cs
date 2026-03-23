using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.T48.TEMPLATE.SCHEMA.001: Mission template instantiation and step completion engine.
// Processes active template missions and checks step completion conditions.
public static class MissionTemplateSystem
{
    private sealed class Scratch
    {
        public readonly List<string> SortedIds = new();
        public readonly List<string> TempGoods = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();

    public static void Process(SimState state)
    {
        if (state is null) return; // STRUCTURAL: null guard
        if (state.ActiveTemplateMissionIds is null || state.ActiveTemplateMissionIds.Count == 0) return; // STRUCTURAL: empty guard

        // GATE.T48.TEMPLATE.TWIST_ENGINE.001: Periodically evaluate twist injection.
        if (MissionTemplateTweaksV0.TwistCheckIntervalTicks <= 0) return; // STRUCTURAL: disabled guard
        if (state.Tick % MissionTemplateTweaksV0.TwistCheckIntervalTicks != 0) return; // STRUCTURAL: interval check

        var scratch = s_scratch.GetOrCreateValue(state);
        var sortedIds = scratch.SortedIds;
        sortedIds.Clear();
        foreach (var id in state.ActiveTemplateMissionIds) sortedIds.Add(id);
        sortedIds.Sort(StringComparer.Ordinal);

        foreach (var missionId in sortedIds)
        {
            if (string.IsNullOrEmpty(state.Missions?.ActiveMissionId)) continue;
            if (!string.Equals(state.Missions.ActiveMissionId, missionId, StringComparison.Ordinal)) continue;
            if (state.Missions.CurrentStepIndex >= state.Missions.ActiveSteps.Count) continue;

            // Check twist injection: mid-mission complication based on world state.
            EvaluateTwistInjection(state, missionId);
        }
    }

    // GATE.T48.TEMPLATE.TWIST_ENGINE.001: Mid-mission twist injection from live world state.
    private static void EvaluateTwistInjection(SimState state, string missionId)
    {
        if (state.Rng is null) return; // STRUCTURAL: RNG guard
        if (state.TemplateMissionTwistCount >= MissionTemplateTweaksV0.MaxTwistsPerMission) return;

        // Find the template for this mission.
        string templateId = ExtractTemplateId(missionId);
        var template = FindTemplate(templateId);
        if (template is null || template.TwistSlotDefs.Count == 0) return; // STRUCTURAL: no twists defined

        // Calculate twist probability bonus from world state.
        int bonusBps = 0; // STRUCTURAL: init

        // Instability at player node increases twist chance.
        string playerNode = state.PlayerLocationNodeId ?? "";
        if (!string.IsNullOrEmpty(playerNode) && state.Nodes.TryGetValue(playerNode, out var node))
        {
            bonusBps += node.InstabilityLevel * MissionTemplateTweaksV0.InstabilityTwistBonusBps;
        }

        // Warfront proximity increases twist chance.
        if (!string.IsNullOrEmpty(playerNode) && state.Warfronts is not null)
        {
            foreach (var wf in state.Warfronts.Values)
            {
                if (wf.ContestedNodeIds is not null && wf.ContestedNodeIds.Contains(playerNode))
                {
                    bonusBps += MissionTemplateTweaksV0.WarfrontProximityBonusBps;
                    break;
                }
            }
        }

        // Roll for each twist slot.
        foreach (var twist in template.TwistSlotDefs)
        {
            if (state.TemplateMissionTwistCount >= MissionTemplateTweaksV0.MaxTwistsPerMission) break;

            int threshold = twist.WeightBps + bonusBps;
            int roll = state.Rng.Next(MissionTemplateTweaksV0.TwistWeightSumBps);
            if (roll < threshold)
            {
                state.TemplateMissionTwistCount++;
            }
        }
    }

    // Extract template ID from mission ID format "TMPL_{templateId}_{tick}".
    private static string ExtractTemplateId(string missionId)
    {
        if (string.IsNullOrEmpty(missionId) || !missionId.StartsWith("TMPL_", StringComparison.Ordinal))
            return "";
        int lastUnderscore = missionId.LastIndexOf('_');
        int prefixLen = 5; // STRUCTURAL: "TMPL_" length
        if (lastUnderscore <= prefixLen) return ""; // STRUCTURAL: guard against malformed ID
        return missionId.Substring(prefixLen, lastUnderscore - prefixLen);
    }

    /// <summary>
    /// Instantiate a mission from a template: resolve variable slots from world state,
    /// pick twist slots using state.Rng, create Mission entity.
    /// Returns the mission ID if successful, empty string otherwise.
    /// </summary>
    public static string InstantiateTemplate(SimState state, string templateId)
    {
        if (state is null || string.IsNullOrEmpty(templateId)) return "";
        if (state.ActiveTemplateMissionIds.Count >= MissionTemplateTweaksV0.MaxActiveTemplateMissions) return "";

        var template = FindTemplate(templateId);
        if (template is null) return "";

        // Check rep requirement.
        if (template.RequiredRepTier >= 0 && !string.IsNullOrEmpty(template.FactionId))
        {
            var playerTier = ReputationSystem.GetRepTier(state, template.FactionId);
            if ((int)playerTier > template.RequiredRepTier) return "";
        }

        // Resolve variable slots.
        var resolvedVars = ResolveVariables(state);

        // Pick twists using RNG.
        int activeTwistCount = 0; // STRUCTURAL: init counter
        if (state.Rng is not null && template.TwistSlotDefs.Count > 0)
        {
            foreach (var twist in template.TwistSlotDefs)
            {
                int roll = state.Rng.Next(MissionTemplateTweaksV0.TwistWeightSumBps);
                if (roll < twist.WeightBps)
                    activeTwistCount++;
            }
        }

        // Build mission steps with resolved variables.
        var steps = new List<MissionActiveStep>();
        foreach (var stepDef in template.Steps)
        {
            string targetNode = "";
            string targetGood = "";
            foreach (var slot in stepDef.VariableSlots)
            {
                if (resolvedVars.TryGetValue(slot, out var resolved))
                {
                    if (slot.Contains("NODE", StringComparison.Ordinal))
                        targetNode = resolved;
                    else if (slot.Contains("GOOD", StringComparison.Ordinal))
                        targetGood = resolved;
                }
            }

            var triggerType = MapObjectiveToTrigger(stepDef.Objective);
            steps.Add(new MissionActiveStep
            {
                StepIndex = stepDef.StepIndex,
                ObjectiveText = $"{stepDef.Objective}: {stepDef.CompletionCondition}",
                TriggerType = triggerType,
                TargetNodeId = targetNode,
                TargetGoodId = targetGood,
                TargetQuantity = 1, // STRUCTURAL: default quantity
            });
        }

        // Calculate reward.
        int reward = template.Reward.BaseCredits;
        if (activeTwistCount > 0)
        {
            long bonus = (long)reward * activeTwistCount * template.Reward.PerTwistBonusBps / MissionTemplateTweaksV0.BpsDivisor;
            reward += (int)bonus;
        }

        // Create mission ID.
        string missionId = $"TMPL_{templateId}_{state.Tick}";

        // Set as active mission via MissionState.
        state.Missions ??= new MissionState();
        if (!string.IsNullOrEmpty(state.Missions.ActiveMissionId)) return ""; // Already has an active mission

        state.Missions.ActiveMissionId = missionId;
        state.Missions.CurrentStepIndex = 0; // STRUCTURAL: start at first step
        state.Missions.ActiveSteps.Clear();
        state.Missions.ActiveSteps.AddRange(steps);

        state.ActiveTemplateMissionIds.Add(missionId);

        return missionId;
    }

    private static MissionTemplateContentV0.MissionTemplateDef? FindTemplate(string templateId)
    {
        foreach (var t in MissionTemplateContentV0.AllTemplates)
        {
            if (string.Equals(t.TemplateId, templateId, StringComparison.Ordinal))
                return t;
        }
        return null;
    }

    /// <summary>
    /// Resolve variable slots from current world state at player's location.
    /// $GOOD_1 = highest-demand good at node, $TARGET_NODE = adjacent node with best price,
    /// $FACTION_1 = dominant faction at node.
    /// </summary>
    private static Dictionary<string, string> ResolveVariables(SimState state)
    {
        var vars = new Dictionary<string, string>(StringComparer.Ordinal);
        string playerNode = state.PlayerLocationNodeId ?? "";

        // $GOOD_1: highest-demand good at current node.
        if (!string.IsNullOrEmpty(playerNode) && state.Markets.TryGetValue(playerNode, out var market))
        {
            string bestGood = "";
            int lowestStock = int.MaxValue;
            foreach (var kv in market.Inventory)
            {
                if (kv.Value < lowestStock || (kv.Value == lowestStock && string.CompareOrdinal(kv.Key, bestGood) < 0)) // STRUCTURAL: tie-break
                {
                    lowestStock = kv.Value;
                    bestGood = kv.Key;
                }
            }
            if (!string.IsNullOrEmpty(bestGood))
                vars["$GOOD_1"] = bestGood;
        }

        // $TARGET_NODE: adjacent node with best price for $GOOD_1.
        if (vars.TryGetValue("$GOOD_1", out var goodForTarget) && !string.IsNullOrEmpty(playerNode))
        {
            string bestNode = "";
            int bestPrice = 0; // STRUCTURAL: init
            foreach (var edge in state.Edges.Values)
            {
                string adjNode = "";
                if (string.Equals(edge.FromNodeId, playerNode, StringComparison.Ordinal))
                    adjNode = edge.ToNodeId;
                else if (string.Equals(edge.ToNodeId, playerNode, StringComparison.Ordinal))
                    adjNode = edge.FromNodeId;
                if (string.IsNullOrEmpty(adjNode)) continue;

                if (state.Markets.TryGetValue(adjNode, out var adjMarket))
                {
                    int price = adjMarket.GetPrice(goodForTarget);
                    if (price > bestPrice || (price == bestPrice && string.CompareOrdinal(adjNode, bestNode) < 0)) // STRUCTURAL: tie-break
                    {
                        bestPrice = price;
                        bestNode = adjNode;
                    }
                }
            }
            if (!string.IsNullOrEmpty(bestNode))
                vars["$TARGET_NODE"] = bestNode;
        }

        // $FACTION_1: dominant faction at current node.
        if (!string.IsNullOrEmpty(playerNode) && state.NodeFactionId.TryGetValue(playerNode, out var factionId))
        {
            vars["$FACTION_1"] = factionId;
        }

        return vars;
    }

    private static MissionTriggerType MapObjectiveToTrigger(MissionTemplateContentV0.ObjectiveType obj)
    {
        return obj switch
        {
            MissionTemplateContentV0.ObjectiveType.Deliver => MissionTriggerType.HaveCargoMin,
            MissionTemplateContentV0.ObjectiveType.Visit => MissionTriggerType.ArriveAtNode,
            MissionTemplateContentV0.ObjectiveType.Scan => MissionTriggerType.ArriveAtNode,
            MissionTemplateContentV0.ObjectiveType.Destroy => MissionTriggerType.ArriveAtNode,
            MissionTemplateContentV0.ObjectiveType.Escort => MissionTriggerType.ArriveAtNode,
            _ => MissionTriggerType.ArriveAtNode,
        };
    }
}
