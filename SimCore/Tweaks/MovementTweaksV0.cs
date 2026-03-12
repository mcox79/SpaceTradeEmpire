namespace SimCore.Tweaks;

// GATE.S8.TECH_EFFECTS.SPEED.001: Movement speed tuning.
public static class MovementTweaksV0
{
    // Speed multiplier when improved_thrusters tech is unlocked (1.2 = 20% bonus).
    public static float ImprovedThrustersMultiplier { get; } = 1.2f;

    // GATE.X.SHIP_CLASS.MASS_SPEED.001: Mass-based speed penalty.
    // Speed reduction per unit of ship class mass (0.003 = 0.3% per mass unit).
    public const float MassSpeedPenaltyPerUnit = 0.003f;

    // Minimum speed multiplier from mass penalty (floor so heavy ships can still move).
    public const float MinMassSpeedMultiplier = 0.3f;
}
