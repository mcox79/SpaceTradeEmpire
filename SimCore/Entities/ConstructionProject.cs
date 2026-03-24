using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.S4.CONSTR_PROG.MODEL.001: Construction project types.
public enum ConstructionType
{
    Depot = 0,
    Shipyard = 1,
    Refinery = 2,
    ScienceCenter = 3,
    Extraction = 4,
}

// GATE.S4.CONSTR_PROG.MODEL.001: Static construction project definition.
public sealed class ConstructionDef
{
    public string ProjectDefId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public ConstructionType Type { get; set; }
    public int TotalSteps { get; set; } = 1;
    public int TicksPerStep { get; set; } = 10;
    public int CreditCostPerStep { get; set; } = 100;
    public List<string> Prerequisites { get; set; } = new(); // tech IDs required
    public string Description { get; set; } = "";
}

// GATE.S4.CONSTR_PROG.MODEL.001: Active construction project instance.
public sealed class ConstructionProject
{
    [JsonInclude] public string ProjectId { get; set; } = "";
    [JsonInclude] public string ProjectDefId { get; set; } = "";
    [JsonInclude] public string NodeId { get; set; } = "";
    [JsonInclude] public int CurrentStep { get; set; }
    [JsonInclude] public int StepProgressTicks { get; set; }
    [JsonInclude] public int TotalSteps { get; set; }
    [JsonInclude] public int TicksPerStep { get; set; }
    [JsonInclude] public bool Completed { get; set; }
    [JsonInclude] public int StartedTick { get; set; }
    [JsonInclude] public int CompletedTick { get; set; } = -1;
}

// GATE.S4.CONSTR_PROG.MODEL.001: Construction state container.
public sealed class ConstructionState
{
    [JsonInclude] public Dictionary<string, ConstructionProject> Projects { get; set; } = new();
    [JsonInclude] public List<ConstructionEvent> EventLog { get; set; } = new();
    [JsonInclude] public long NextEventSeq { get; set; } = 1;
    [JsonInclude] public long NextProjectSeq { get; set; } = 1;
}

// GATE.S4.CONSTR_PROG.MODEL.001: Construction event for deterministic log.
public sealed class ConstructionEvent
{
    [JsonInclude] public long Seq { get; set; }
    [JsonInclude] public int Tick { get; set; }
    [JsonInclude] public string ProjectId { get; set; } = "";
    [JsonInclude] public string ProjectDefId { get; set; } = "";
    [JsonInclude] public string EventType { get; set; } = ""; // Started, StepCompleted, Completed, Cancelled, Stalled
    [JsonInclude] public int StepIndex { get; set; } = -1;
}
