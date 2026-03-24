using System;
using System.Collections.Generic;
using System.Linq;
using SimCore.Content;
using SimCore.Entities;

namespace SimCore.Systems;

// GATE.S1.MISSION.SYSTEM.001: Mission trigger evaluation and step advancement.
// Content definitions live in SimCore/Content/MissionContentV0.cs per Tweak Routing Policy.
public static class MissionSystem
{
    public static IReadOnlyList<MissionDef> GetAllMissionDefs() => MissionContentV0.AllMissions;

    public static MissionDef? GetMissionDef(string missionId)
    {
        if (string.IsNullOrWhiteSpace(missionId)) return null;
        foreach (var def in MissionContentV0.AllMissions)
        {
            if (string.Equals(def.MissionId, missionId, StringComparison.Ordinal))
                return def;
        }
        return null;
    }

    /// <summary>
    /// Returns missions available for acceptance: not completed and prerequisites met.
    /// </summary>
    public static List<MissionDef> GetAvailableMissions(SimState state)
    {
        if (state is null) return new List<MissionDef>();
        var completed = state.Missions?.CompletedMissionIds ?? new List<string>();
        var result = new List<MissionDef>();

        foreach (var def in MissionContentV0.AllMissions)
        {
            if (completed.Contains(def.MissionId)) continue;
            if (!string.IsNullOrEmpty(state.Missions?.ActiveMissionId) &&
                string.Equals(state.Missions.ActiveMissionId, def.MissionId, StringComparison.Ordinal))
                continue;

            bool prereqsMet = true;
            foreach (var prereq in def.Prerequisites)
            {
                if (!completed.Contains(prereq)) { prereqsMet = false; break; }
            }
            if (!prereqsMet) continue;

            // GATE.S7.REPUTATION.CONTRACTS.001: Check faction reputation requirement.
            if (def.RequiredRepTier >= 0 && !string.IsNullOrEmpty(def.FactionId))
            {
                var playerTier = ReputationSystem.GetRepTier(state, def.FactionId);
                // RepTier enum: Allied=0 < Friendly=1 < Neutral=2 < Hostile=3 < Enemy=4.
                // Lower value = better tier. Player tier must be <= required tier.
                if ((int)playerTier > def.RequiredRepTier) continue;
            }

            result.Add(def);
        }

        return result;
    }

    /// <summary>
    /// Accept a mission: resolve binding tokens, populate ActiveSteps.
    /// Returns false if mission not found, already active, or already completed.
    /// </summary>
    public static bool AcceptMission(SimState state, string missionId)
    {
        if (state is null) return false;
        var def = GetMissionDef(missionId);
        if (def is null) return false;

        state.Missions ??= new MissionState();

        // Cannot accept if one is already active.
        if (!string.IsNullOrEmpty(state.Missions.ActiveMissionId)) return false;

        // Cannot accept if already completed.
        if (state.Missions.CompletedMissionIds.Contains(missionId)) return false;

        // Check prerequisites.
        foreach (var prereq in def.Prerequisites)
        {
            if (!state.Missions.CompletedMissionIds.Contains(prereq)) return false;
        }

        // Resolve binding tokens.
        var resolvedTargets = ResolveBindingTokens(state);

        // Populate active steps with resolved concrete values.
        state.Missions.ActiveMissionId = missionId;
        state.Missions.CurrentStepIndex = 0;
        state.Missions.ActiveSteps.Clear();

        foreach (var stepDef in def.Steps)
        {
            var activeStep = new MissionActiveStep
            {
                StepIndex = stepDef.StepIndex,
                ObjectiveText = stepDef.ObjectiveText,
                TriggerType = stepDef.TriggerType,
                TargetNodeId = ResolveToken(stepDef.TargetNodeId, resolvedTargets),
                TargetGoodId = ResolveToken(stepDef.TargetGoodId, resolvedTargets),
                TargetQuantity = stepDef.TargetQuantity,
                Completed = false,
                // GATE.S9.MISSION_EVOL.TRIGGERS.001: Copy extended trigger fields.
                TargetFactionId = stepDef.TargetFactionId ?? "",
                TargetTechId = stepDef.TargetTechId ?? "",
                DeadlineTick = stepDef.DeadlineTicks > 0 ? state.Tick + stepDef.DeadlineTicks : 0,
                // GATE.S9.MISSION_EVOL.BRANCHING.001: Copy choice options.
                ChoiceOptions = stepDef.ChoiceOptions ?? new List<MissionChoiceOption>(),
            };
            state.Missions.ActiveSteps.Add(activeStep);
        }

        // GATE.S9.MISSION_EVOL.FAILURE.001: Set mission deadline.
        if (def.DeadlineTicks > 0)
            state.Missions.MissionDeadlineTick = state.Tick + def.DeadlineTicks;
        else
            state.Missions.MissionDeadlineTick = 0;

        EmitEvent(state, missionId, "Accepted");
        return true;
    }

    // GATE.S9.MISSION_EVOL.FAILURE.001: Abandon the active mission with reputation penalty.
    public static bool AbandonMission(SimState state)
    {
        if (state is null) return false;
        if (state.Missions is null) return false;
        if (string.IsNullOrEmpty(state.Missions.ActiveMissionId)) return false;

        var missionId = state.Missions.ActiveMissionId;
        state.Missions.FailedMissionIds.Add(missionId);
        EmitEvent(state, missionId, "Abandoned");

        // Apply rep penalty to all factions the player has reputation with.
        foreach (var factionId in state.FactionReputation.Keys.ToList())
        {
            ReputationSystem.AdjustReputation(state, factionId, Tweaks.MissionEvolutionTweaksV0.AbandonRepPenalty);
        }

        state.Missions.ActiveMissionId = "";
        state.Missions.CurrentStepIndex = 0;
        state.Missions.ActiveSteps.Clear();
        state.Missions.MissionDeadlineTick = 0;
        return true;
    }

    // GATE.S9.MISSION_EVOL.FAILURE.001: Fail the active mission (deadline exceeded).
    public static void FailMission(SimState state)
    {
        if (state is null) return;
        if (state.Missions is null) return;
        if (string.IsNullOrEmpty(state.Missions.ActiveMissionId)) return;

        var missionId = state.Missions.ActiveMissionId;
        state.Missions.FailedMissionIds.Add(missionId);
        EmitEvent(state, missionId, "Failed");

        state.Missions.ActiveMissionId = "";
        state.Missions.CurrentStepIndex = 0;
        state.Missions.ActiveSteps.Clear();
        state.Missions.MissionDeadlineTick = 0;
    }

    /// <summary>
    /// Per-tick processing: evaluate current step trigger, advance if met.
    /// </summary>
    public static void Process(SimState state)
    {
        if (state is null) return;
        if (state.Missions is null) return;
        if (string.IsNullOrEmpty(state.Missions.ActiveMissionId)) return;
        if (state.Missions.ActiveSteps.Count == 0) return;

        // GATE.S9.MISSION_EVOL.FAILURE.001: Check deadline.
        if (state.Missions.MissionDeadlineTick > 0 && state.Tick >= state.Missions.MissionDeadlineTick)
        {
            FailMission(state);
            return;
        }

        var stepIndex = state.Missions.CurrentStepIndex;
        if (stepIndex < 0 || stepIndex >= state.Missions.ActiveSteps.Count) return;

        var currentStep = state.Missions.ActiveSteps[stepIndex];
        if (currentStep.Completed) return;

        if (EvaluateTrigger(state, currentStep))
        {
            AdvanceStep(state);
        }
    }

    /// <summary>
    /// Evaluate whether the current step's trigger condition is met.
    /// </summary>
    public static bool EvaluateTrigger(SimState state, MissionActiveStep step)
    {
        if (state is null || step is null) return false;

        switch (step.TriggerType)
        {
            case MissionTriggerType.ArriveAtNode:
                return string.Equals(state.PlayerLocationNodeId, step.TargetNodeId, StringComparison.Ordinal);

            case MissionTriggerType.HaveCargoMin:
                var have = state.PlayerCargo.TryGetValue(step.TargetGoodId, out var qty) ? qty : 0;
                return have >= step.TargetQuantity;

            case MissionTriggerType.NoCargoAtNode:
                if (!string.Equals(state.PlayerLocationNodeId, step.TargetNodeId, StringComparison.Ordinal))
                    return false;
                var cargoQty = state.PlayerCargo.TryGetValue(step.TargetGoodId, out var v) ? v : 0;
                return cargoQty <= 0;

            // GATE.S9.MISSION_EVOL.TRIGGERS.001: Phase 1 trigger types.
            case MissionTriggerType.ReputationMin:
                var rep = state.FactionReputation.TryGetValue(step.TargetFactionId, out var r) ? r : 0;
                return rep >= step.TargetQuantity;

            case MissionTriggerType.CreditsMin:
                return state.PlayerCredits >= step.TargetQuantity;

            case MissionTriggerType.TechUnlocked:
                return state.Tech?.UnlockedTechIds?.Contains(step.TargetTechId) == true;

            case MissionTriggerType.TimerExpired:
                return step.DeadlineTick > 0 && state.Tick >= step.DeadlineTick;

            default:
                return false;
        }
    }

    private static void AdvanceStep(SimState state)
    {
        var missions = state.Missions;
        var stepIndex = missions.CurrentStepIndex;
        var step = missions.ActiveSteps[stepIndex];

        step.Completed = true;
        EmitEvent(state, missions.ActiveMissionId, "StepCompleted", stepIndex);

        // GATE.S9.MISSION_EVOL.BRANCHING.001: Follow branch if choice was made.
        int nextIndex = -1;
        if (step.ChosenBranch >= 0 && step.ChosenBranch < step.ChoiceOptions.Count)
        {
            nextIndex = step.ChoiceOptions[step.ChosenBranch].TargetStepIndex;
        }

        if (nextIndex >= 0 && nextIndex < missions.ActiveSteps.Count)
        {
            missions.CurrentStepIndex = nextIndex;
        }
        else if (stepIndex + 1 >= missions.ActiveSteps.Count)
        {
            CompleteMission(state);
        }
        else
        {
            missions.CurrentStepIndex = stepIndex + 1;
        }
    }

    // GATE.S9.MISSION_EVOL.BRANCHING.001: Player selects a choice option at the current step.
    public static bool MakeChoice(SimState state, int optionIndex)
    {
        if (state?.Missions is null) return false;
        if (string.IsNullOrEmpty(state.Missions.ActiveMissionId)) return false;

        var stepIndex = state.Missions.CurrentStepIndex;
        if (stepIndex < 0 || stepIndex >= state.Missions.ActiveSteps.Count) return false;

        var step = state.Missions.ActiveSteps[stepIndex];
        if (step.TriggerType != MissionTriggerType.Choice) return false;
        if (optionIndex < 0 || optionIndex >= step.ChoiceOptions.Count) return false;

        step.ChosenBranch = optionIndex;
        AdvanceStep(state);
        return true;
    }

    private static void CompleteMission(SimState state)
    {
        var missions = state.Missions;
        var missionId = missions.ActiveMissionId;
        var def = GetMissionDef(missionId);

        if (def is not null)
        {
            state.PlayerCredits = checked(state.PlayerCredits + def.CreditReward);

            // GATE.S9.MISSION_EVOL.REWARDS.001: Distribute non-credit rewards.
            DistributeRewards(state, def);

            // GATE.T55.REP.MISSION_WIRE.001: Grant faction rep on mission completion.
            if (!string.IsNullOrEmpty(def.FactionId))
            {
                ReputationSystem.AdjustReputation(state, def.FactionId, Tweaks.FactionTweaksV0.MissionRepGain);
            }
        }

        missions.CompletedMissionIds.Add(missionId);
        // GATE.S12.PROGRESSION.STATS.001: Track missions completed.
        if (state.PlayerStats != null)
            state.PlayerStats.MissionsCompleted = missions.CompletedMissionIds.Count;
        EmitEvent(state, missionId, "MissionCompleted");

        missions.ActiveMissionId = "";
        missions.CurrentStepIndex = 0;
        missions.ActiveSteps.Clear();
    }

    // GATE.S9.MISSION_EVOL.REWARDS.001: Distribute non-credit mission rewards.
    private static void DistributeRewards(SimState state, MissionDef def)
    {
        if (def.Rewards is null) return;

        foreach (var reward in def.Rewards)
        {
            if (reward is null) continue;

            // Reputation reward.
            if (!string.IsNullOrEmpty(reward.ReputationFactionId) && reward.ReputationAmount != 0)
            {
                ReputationSystem.AdjustReputation(state, reward.ReputationFactionId, reward.ReputationAmount);
            }

            // Tech unlock reward.
            if (!string.IsNullOrEmpty(reward.TechUnlockId))
            {
                state.Tech?.UnlockedTechIds?.Add(reward.TechUnlockId);
            }

            // Intel lead reward: create a discovery lead at the specified node.
            if (!string.IsNullOrEmpty(reward.IntelLeadNodeId))
            {
                var leadId = $"LEAD.MISSION.{def.MissionId}.{reward.IntelLeadNodeId}";
                IntelSystem.GrantRumorLeadOnHubAnalysis(state, leadId, "");
            }
        }
    }

    private static void EmitEvent(SimState state, string missionId, string eventType, int stepIndex = -1)
    {
        var missions = state.Missions;
        var seq = missions.NextEventSeq;
        missions.NextEventSeq = checked(seq + 1);

        missions.EventLog.Add(new MissionEvent
        {
            Seq = seq,
            Tick = state.Tick,
            MissionId = missionId,
            EventType = eventType,
            StepIndex = stepIndex,
        });
    }

    // --- Binding token resolution ---

    private static Dictionary<string, string> ResolveBindingTokens(SimState state)
    {
        var resolved = new Dictionary<string, string>(StringComparer.Ordinal);

        resolved["$PLAYER_START"] = state.PlayerLocationNodeId ?? "";

        var startNode = state.PlayerLocationNodeId ?? "";
        var adjacentNode = "";
        if (!string.IsNullOrEmpty(startNode))
        {
            var edges = state.Edges.Values
                .Where(e => string.Equals(e.FromNodeId, startNode, StringComparison.Ordinal))
                .OrderBy(e => e.Id, StringComparer.Ordinal)
                .ToList();
            if (edges.Count > 0)
            {
                adjacentNode = edges[0].ToNodeId ?? "";
            }
        }
        resolved["$ADJACENT_1"] = adjacentNode;

        var marketGood = "";
        if (!string.IsNullOrEmpty(startNode) && state.Nodes.TryGetValue(startNode, out var node))
        {
            var marketId = node.MarketId ?? "";
            if (!string.IsNullOrEmpty(marketId) && state.Markets.TryGetValue(marketId, out var market))
            {
                var goodEntry = market.Inventory
                    .Where(kv => kv.Value > 0)
                    .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(goodEntry.Key))
                {
                    marketGood = goodEntry.Key;
                }
            }
        }
        resolved["$MARKET_GOOD_1"] = marketGood;

        return resolved;
    }

    private static string ResolveToken(string value, Dictionary<string, string> resolved)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.StartsWith("$", StringComparison.Ordinal) && resolved.TryGetValue(value, out var r))
            return r;
        return value;
    }
}
