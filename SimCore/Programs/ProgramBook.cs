using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Programs;

/// <summary>
/// Serializable container for program instances.
/// Determinism rule: iteration must be sorted by key (Id).
/// </summary>
public sealed class ProgramBook
{
    [JsonInclude] public Dictionary<string, ProgramInstance> Instances { get; private set; } = new();
}
