using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.S7.PLANET.MODEL.001: Star entity — spectral class determines planet conditions.
// One star per node, stored in SimState.Stars[nodeId].

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StarClass
{
    ClassG,  // Yellow (Sol-like), temperate habitable zone
    ClassK,  // Orange, cooler, wider habitable band
    ClassM,  // Red dwarf, cold, narrow habitable zone
    ClassF,  // White-yellow, hotter, fewer habitable worlds
    ClassA,  // White, very hot, most planets inhospitable
    ClassO,  // Blue giant, extreme radiation, almost no habitable worlds
}

public sealed class Star
{
    // Identity — matches the host node's Id.
    [JsonInclude] public string NodeId { get; set; } = "";

    // Spectral classification.
    [JsonInclude] public StarClass Class { get; set; } = StarClass.ClassG;

    // Luminosity in basis points (10000 = Sol-baseline).
    // Higher luminosity = hotter planets at same orbital distance.
    [JsonInclude] public int LuminosityBps { get; set; } = 10000;

    // Human-readable name for UI.
    [JsonInclude] public string DisplayName { get; set; } = "";
}
