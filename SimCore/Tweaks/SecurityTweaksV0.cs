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

    // GATE.S7.ENFORCEMENT.HEAT_ACCUM.001: Pattern-based heat accumulation.
    // High-value trade: bonus heat per 100 credits of cargo value.
    public const float HighValueHeatPerHundredCredits = 0.02f;
    public const int HighValueThresholdCredits = 500;

    // Route repetition: 3+ traversals in a window trigger bonus heat per traversal.
    public const int RepetitionThreshold = 3;
    public const float RepetitionBonusHeat = 0.15f;

    // Hostile counterparty: trading at a hostile-faction node adds heat.
    public const float HostileCounterpartyHeat = 0.30f;

    // Heat decay rate per tick (replaces MarketSystem's flat 0.05f).
    public const float HeatDecayPerTick = 0.05f;

    // Traversal count reset interval (ticks). Matches decay window.
    public const int TraversalWindowTicks = 100;

    // GATE.S7.ENFORCEMENT.CONFISCATION.001: Confiscation at high heat.
    // Heat threshold: confiscation triggers when edge heat exceeds this.
    public const float ConfiscationHeatThreshold = 2.0f;

    // Cooldown: minimum ticks between confiscation events on the same fleet.
    public const int ConfiscationCooldownTicks = 500;

    // Fine: basis points of cargo value. 1000 bps = 10%.
    public const int ConfiscationFineBps = 1500;

    // Max units confiscated per event (caps severity).
    public const int ConfiscationMaxUnits = 5;
}
