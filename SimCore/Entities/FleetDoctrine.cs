namespace SimCore.Entities;

// GATE.S7.AUTOMATION_MGMT.DOCTRINE.001: Fleet engagement doctrine.
// Controls engagement stance, retreat threshold, and patrol radius.

public enum EngagementStance
{
    Aggressive = 0,
    Defensive = 1,
    Evasive = 2
}

public class FleetDoctrine
{
    public EngagementStance Stance { get; set; } = EngagementStance.Defensive;

    /// <summary>Hull percentage (0-100) at which the fleet should retreat.</summary>
    public int RetreatThresholdPct { get; set; } = 25;

    /// <summary>Patrol radius in distance units.</summary>
    public float PatrolRadius { get; set; } = 50.0f;
}
