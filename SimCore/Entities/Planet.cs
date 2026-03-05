using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.S7.PLANET.MODEL.001: Planet entity — physical properties + specialization.
// One planet per node, stored in SimState.Planets[nodeId].

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlanetType
{
    Terrestrial,   // Habitable, balanced economy
    Ice,           // Cold, fuel extraction + rare materials
    Sand,          // Desert, mining-focused
    Lava,          // Volcanic, heavy industry (tech-gated landing)
    Gaseous,       // Gas giant, NEVER landable (no surface)
    Barren,        // No atmosphere, mining-only (tech-gated landing)
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlanetSpecialization
{
    None,
    Agriculture,    // Food production
    Mining,         // Ore + rare ore extraction
    Manufacturing,  // Metal refining, hull plating forging
    HighTech,       // Electronics, composite armor
    FuelExtraction, // Fuel wells, gas processing
}

public sealed class Planet
{
    // Identity — matches the host node's Id.
    [JsonInclude] public string NodeId { get; set; } = "";

    // Planet classification.
    [JsonInclude] public PlanetType Type { get; set; } = PlanetType.Terrestrial;

    // Physical properties in basis points (0-10000).
    // 5000 = Earth-normal for gravity, breathable for atmosphere, temperate for temperature.
    [JsonInclude] public int GravityBps { get; set; } = 5000;
    [JsonInclude] public int AtmosphereBps { get; set; } = 5000;
    [JsonInclude] public int TemperatureBps { get; set; } = 5000;

    // Derived from gravity + atmosphere (computed at generation time).
    [JsonInclude] public bool Landable { get; set; } = false;

    // Minimum tech tier required to land. 0 = no tech needed, 1+ = requires planetary_landing_mk1 etc.
    [JsonInclude] public int LandingTechTier { get; set; } = 0;

    // Economic role of this planet.
    [JsonInclude] public PlanetSpecialization Specialization { get; set; } = PlanetSpecialization.None;

    // Human-readable name for UI.
    [JsonInclude] public string DisplayName { get; set; } = "";
}
