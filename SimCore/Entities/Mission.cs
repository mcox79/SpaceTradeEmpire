using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.S1.MISSION.MODEL.001: Mission trigger types.
public enum MissionTriggerType
{
    ArriveAtNode = 0,    // PlayerLocationNodeId == TargetNodeId
    HaveCargoMin = 1,    // PlayerCargo[TargetGoodId] >= TargetQuantity
    NoCargoAtNode = 2,   // At TargetNodeId AND PlayerCargo[TargetGoodId] == 0
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
