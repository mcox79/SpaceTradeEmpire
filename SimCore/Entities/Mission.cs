using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.S1.MISSION.MODEL.001: Mission trigger types.
public enum MissionTriggerType
{
    ArriveAtNode = 0,    // PlayerLocationNodeId == TargetNodeId
    HaveCargoMin = 1,    // PlayerCargo[TargetGoodId] >= TargetQuantity
    NoCargoAtNode = 2,   // At TargetNodeId AND PlayerCargo[TargetGoodId] == 0
    // GATE.S9.MISSION_EVOL.TRIGGERS.001: Phase 1 trigger types.
    ReputationMin = 3,   // Reputation[TargetFactionId] >= TargetQuantity
    CreditsMin = 4,      // PlayerCredits >= TargetQuantity
    TechUnlocked = 5,    // Tech.UnlockedTechIds.Contains(TargetTechId)
    TimerExpired = 6,    // state.Tick >= DeadlineTick
    // GATE.S9.MISSION_EVOL.BRANCHING.001: Player choice (not auto-evaluated).
    Choice = 7,          // Presented as 2-3 options; player selects via command.
}

// GATE.S1.MISSION.MODEL.001: Static mission step template.
public sealed class MissionStepDef
{
    public int StepIndex { get; set; }
    public string ObjectiveText { get; set; } = "";
    public MissionTriggerType TriggerType { get; set; }
    // Concrete node/good IDs or binding tokens ($PLAYER_START, $ADJACENT_1, $MARKET_GOOD_1).
    public string TargetNodeId { get; set; } = "";
    public string TargetGoodId { get; set; } = "";
    public int TargetQuantity { get; set; } = 0;
    // GATE.S9.MISSION_EVOL.TRIGGERS.001: Extended trigger fields.
    public string TargetFactionId { get; set; } = "";
    public string TargetTechId { get; set; } = "";
    public int DeadlineTicks { get; set; } = 0; // Duration in ticks from mission accept for TimerExpired.
    // GATE.S9.MISSION_EVOL.BRANCHING.001: Choice options for Choice trigger type.
    public List<MissionChoiceOption> ChoiceOptions { get; set; } = new();
}

// GATE.S9.MISSION_EVOL.BRANCHING.001: A branching choice option.
public sealed class MissionChoiceOption
{
    public string Label { get; set; } = "";
    public int TargetStepIndex { get; set; } = -1; // Step to jump to when chosen.
}

// GATE.S9.MISSION_EVOL.REWARDS.001: Non-credit reward definition.
public sealed class MissionRewardDef
{
    public string ReputationFactionId { get; set; } = "";
    public int ReputationAmount { get; set; } = 0;
    public string TechUnlockId { get; set; } = "";
    public string IntelLeadNodeId { get; set; } = "";
}

// GATE.S1.MISSION.MODEL.001: Static mission definition (template).
public sealed class MissionDef
{
    public string MissionId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Prerequisites { get; set; } = new();
    public List<MissionStepDef> Steps { get; set; } = new();
    public long CreditReward { get; set; } = 0;
    // GATE.S9.MISSION_EVOL.REWARDS.001: Non-credit rewards.
    public List<MissionRewardDef> Rewards { get; set; } = new();
    // GATE.S9.MISSION_EVOL.FAILURE.001: Mission deadline in ticks from acceptance. 0 = no deadline.
    public int DeadlineTicks { get; set; } = 0;
    // GATE.S7.REPUTATION.CONTRACTS.001: Faction offering this contract. Empty = universal.
    public string FactionId { get; set; } = "";
    // GATE.S7.REPUTATION.CONTRACTS.001: Minimum rep tier to see this contract (Neutral/Friendly/Allied).
    // Uses RepTier enum values: 0=Allied, 1=Friendly, 2=Neutral, 3=Hostile, 4=Enemy.
    // Default -1 = no reputation requirement.
    public int RequiredRepTier { get; set; } = -1;
}

// GATE.S1.MISSION.MODEL.001: Runtime state for an active mission step (resolved targets).
public sealed class MissionActiveStep
{
    [JsonInclude] public int StepIndex { get; set; }
    [JsonInclude] public string ObjectiveText { get; set; } = "";
    [JsonInclude] public MissionTriggerType TriggerType { get; set; }
    [JsonInclude] public string TargetNodeId { get; set; } = "";
    [JsonInclude] public string TargetGoodId { get; set; } = "";
    [JsonInclude] public int TargetQuantity { get; set; } = 0;
    [JsonInclude] public bool Completed { get; set; }
    // GATE.S9.MISSION_EVOL.TRIGGERS.001: Extended trigger fields.
    [JsonInclude] public string TargetFactionId { get; set; } = "";
    [JsonInclude] public string TargetTechId { get; set; } = "";
    [JsonInclude] public int DeadlineTick { get; set; } = 0; // Absolute tick when TimerExpired fires.
    // GATE.S9.MISSION_EVOL.BRANCHING.001: Choice options + chosen branch.
    [JsonInclude] public List<MissionChoiceOption> ChoiceOptions { get; set; } = new();
    [JsonInclude] public int ChosenBranch { get; set; } = -1; // Index into ChoiceOptions, -1 = not chosen.
}

// GATE.S1.MISSION.MODEL.001: Persisted mission state in SimState.
public sealed class MissionState
{
    [JsonInclude] public string ActiveMissionId { get; set; } = "";
    [JsonInclude] public int CurrentStepIndex { get; set; }
    [JsonInclude] public List<string> CompletedMissionIds { get; set; } = new();
    [JsonInclude] public List<MissionActiveStep> ActiveSteps { get; set; } = new();
    [JsonInclude] public List<MissionEvent> EventLog { get; set; } = new();
    [JsonInclude] public long NextEventSeq { get; set; } = 1;
    // GATE.S9.MISSION_EVOL.FAILURE.001: Failed/abandoned mission tracking.
    [JsonInclude] public List<string> FailedMissionIds { get; set; } = new();
    [JsonInclude] public int MissionDeadlineTick { get; set; } = 0;
}

// GATE.S1.MISSION.SYSTEM.001: Mission event for deterministic event log.
public sealed class MissionEvent
{
    [JsonInclude] public long Seq { get; set; }
    [JsonInclude] public int Tick { get; set; }
    [JsonInclude] public string MissionId { get; set; } = "";
    [JsonInclude] public string EventType { get; set; } = ""; // Accepted, StepCompleted, MissionCompleted
    [JsonInclude] public int StepIndex { get; set; } = -1;
}
