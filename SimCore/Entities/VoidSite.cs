using System.Numerics;

namespace SimCore.Entities;

// GATE.S6.FRACTURE.VOID_SITES.001: Discovery locations in deep space between systems.
public enum VoidSiteFamily
{
    AsteroidField,
    NebulaRemnant,
    AbandonedStation,
    AnomalyRift,
    ResourceDeposit,
}

public enum VoidSiteMarkerState
{
    Unknown,     // Not yet discovered by player
    Discovered,  // Player has sensor-detected this site
    Surveyed,    // Player has placed a survey marker
}

public class VoidSite
{
    public string Id { get; set; } = "";
    public Vector3 Position { get; set; }
    public VoidSiteFamily Family { get; set; }
    public VoidSiteMarkerState MarkerState { get; set; } = VoidSiteMarkerState.Unknown;

    // Estimated resources (populated when surveyed, accuracy depends on sensor tech).
    public int EstimatedResourceValue { get; set; }

    // The two star nodes this void site lies between (for deterministic seeding).
    public string NearStarA { get; set; } = "";
    public string NearStarB { get; set; } = "";
}
