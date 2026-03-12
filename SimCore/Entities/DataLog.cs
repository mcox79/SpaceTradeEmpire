using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.T18.NARRATIVE.DATALOG_MODEL.001: Ancient scientist conversation thread identifiers.
public enum DataLogThread
{
    Containment = 0,
    Lattice = 1,
    Departure = 2,
    Accommodation = 3,
    Warning = 4,
    EconTopology = 5
}

// GATE.T18.NARRATIVE.DATALOG_MODEL.001: Single entry in a data log conversation.
public sealed class DataLogEntry
{
    [JsonInclude] public int EntryIndex { get; set; }
    [JsonInclude] public string Speaker { get; set; } = "";
    [JsonInclude] public string Text { get; set; } = "";
    // True if this line is personal/mundane (not about the central argument).
    [JsonInclude] public bool IsPersonal { get; set; }
}

// GATE.T18.NARRATIVE.DATALOG_MODEL.001: Ancient data log found at discovery sites.
// Conversations between thread-builder scientists. Found at ruins and derelicts.
// Player assembles the story out of order across multiple threads.
public sealed class DataLog
{
    [JsonInclude] public string LogId { get; set; } = "";
    [JsonInclude] public DataLogThread Thread { get; set; } = DataLogThread.Containment;
    [JsonInclude] public List<string> Speakers { get; private set; } = new();
    [JsonInclude] public List<DataLogEntry> Entries { get; private set; } = new();

    // Revelation tier (1-3): which act this log unlocks understanding for.
    [JsonInclude] public int RevelationTier { get; set; } = 1;

    // Adaptation Fragment IDs this log connects to.
    [JsonInclude] public List<string> ConnectedFragmentIds { get; private set; } = new();

    // Mechanical hook token: coordinate hint, trade intel, calibration data, or resonance location.
    [JsonInclude] public string MechanicalHook { get; set; } = "";

    // Node where this log is placed (set during world gen).
    [JsonInclude] public string LocationNodeId { get; set; } = "";

    // Player discovery state.
    [JsonInclude] public bool IsDiscovered { get; set; }
    [JsonInclude] public int DiscoveredTick { get; set; }
}
