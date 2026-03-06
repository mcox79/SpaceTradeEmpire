namespace SimCore.Tweaks;

// GATE.S14.NPC_ALIVE.FLEET_SEED.001: AI fleet role distribution and speed tuning.
public static class FleetSeedTweaksV0
{
    // Role distribution bucket size (rng hash % BucketSize).
    public static int BucketSize { get; } = 100;

    // Bucket thresholds: [0, TraderThreshold) = Trader, [TraderThreshold, HaulerThreshold) = Hauler, rest = Patrol.
    public static int TraderThreshold { get; } = 60;
    public static int HaulerThreshold { get; } = 85;

    // Fleet speeds by role.
    public static float TraderSpeed { get; } = 0.8f;
    public static float HaulerSpeed { get; } = 0.7f;
    public static float PatrolSpeed { get; } = 1.0f;
}
