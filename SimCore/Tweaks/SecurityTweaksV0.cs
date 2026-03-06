namespace SimCore.Tweaks;

// GATE.S5.SEC_LANES.MODEL.001: Security lane pacing constants.
public static class SecurityTweaksV0
{
    // BPS thresholds for security bands (higher = safer).
    public const int HostileBps = 1500;     // below this: hostile (red)
    public const int DangerousBps = 3000;   // below this: dangerous (orange)
    public const int ModerateBps = 5000;    // below this: moderate (yellow) — default
    public const int SafeBps = 7000;        // above this: safe (green)

    // Patrol fleet contribution to security per tick (BPS added per patrol fleet on adjacent node).
    public const int PatrolBoostBps = 200;

    // Economic heat penalty to security (BPS subtracted per heat unit).
    public const int HeatPenaltyBps = 50;

    // Natural drift toward default (BPS per tick).
    public const int DriftToDefaultBps = 10;

    // Default security level (neutral).
    public const int DefaultSecurityBps = 5000;

    // Min/max bounds.
    public const int MinSecurityBps = 0;
    public const int MaxSecurityBps = 10000;
}
