using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.X.PRESSURE.MODEL.001: Pressure tier — 5-state ladder.
public enum PressureTier
{
    Normal = 0,
    Strained = 1,
    Unstable = 2,
    Critical = 3,
    Collapsed = 4,
}

// GATE.X.PRESSURE.MODEL.001: Pressure direction indicator.
public enum PressureDirection
{
    Improving = 0,
    Stable = 1,
    Worsening = 2,
}

// GATE.X.PRESSURE.MODEL.001: A single pressure delta entry.
public sealed class PressureDelta
{
    [JsonInclude] public string DomainId { get; set; } = "";
    [JsonInclude] public string ReasonCode { get; set; } = "";
    [JsonInclude] public int Magnitude { get; set; }
    [JsonInclude] public string TargetRef { get; set; } = "";
    [JsonInclude] public string SourceRef { get; set; } = "";
    [JsonInclude] public int Tick { get; set; }
}

// GATE.X.PRESSURE.MODEL.001: Per-domain pressure state.
public sealed class PressureDomainState
{
    [JsonInclude] public string DomainId { get; set; } = "";
    [JsonInclude] public PressureTier Tier { get; set; } = PressureTier.Normal;
    [JsonInclude] public PressureDirection Direction { get; set; } = PressureDirection.Stable;
    [JsonInclude] public int AccumulatedPressureBps { get; set; }
    [JsonInclude] public int LastTransitionTick { get; set; } = -1;
    [JsonInclude] public int AlertCount { get; set; }
    [JsonInclude] public int WindowStartTick { get; set; }
    // GATE.X.PRESSURE.ENFORCE.001: Track last tick consequences were applied (prevent per-tick spam).
    [JsonInclude] public int LastConsequenceTick { get; set; } = -1;
}

// GATE.X.PRESSURE.MODEL.001: Pressure state container.
public sealed class PressureStateContainer
{
    [JsonInclude] public Dictionary<string, PressureDomainState> Domains { get; set; } = new();
    [JsonInclude] public List<PressureDelta> DeltaLog { get; set; } = new();
    [JsonInclude] public List<PressureEvent> EventLog { get; set; } = new();
    [JsonInclude] public long NextEventSeq { get; set; } = 1;
}

// GATE.X.PRESSURE.MODEL.001: Pressure event for deterministic log.
public sealed class PressureEvent
{
    [JsonInclude] public long Seq { get; set; }
    [JsonInclude] public int Tick { get; set; }
    [JsonInclude] public string DomainId { get; set; } = "";
    [JsonInclude] public string EventType { get; set; } = ""; // TierChanged, Alert, DeltaApplied
    [JsonInclude] public PressureTier OldTier { get; set; }
    [JsonInclude] public PressureTier NewTier { get; set; }
    [JsonInclude] public string ReasonCode { get; set; } = "";
}
