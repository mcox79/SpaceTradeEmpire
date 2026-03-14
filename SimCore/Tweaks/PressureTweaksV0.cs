namespace SimCore.Tweaks;

// GATE.X.PRESSURE.SYSTEM.001: Pressure pacing constants (integers only for determinism).
public static class PressureTweaksV0
{
    // Basis points thresholds for tier transitions (accumulated pressure).
    public const int StrainedThresholdBps = 2000;  // 20%
    public const int UnstableThresholdBps = 4000;  // 40%
    public const int CriticalThresholdBps = 7000;  // 70%
    public const int CollapsedThresholdBps = 9000;  // 90%

    // Max pressure tier jump per enforcement window (1 = max-one-jump).
    public const int MaxTierJumpPerWindow = 1;

    // Enforcement window size in ticks.
    public const int EnforcementWindowTicks = 50;

    // Intervention budget: max alerts per window.
    public const int MaxAlertsPerWindowNormal = 3;
    public const int MaxAlertsPerWindowCrisis = 5;

    // Pressure decay per tick (bps) when no new deltas.
    public const int NaturalDecayBps = 10;

    // Max accumulated pressure bps.
    public const int MaxAccumulatedBps = 10000;

    // Crisis tier threshold (Critical or above).
    public const int CrisisTierMin = 3; // PressureTier.Critical

    // GATE.X.PRESSURE.ENFORCE.001: Consequence magnitudes.
    // Crisis fee increase (BPS: 2000 = +20% market fee).
    public const int CrisisFeeIncreaseBps = 2000;

    // Collapse piracy escalation — pressure injected into piracy domain.
    public const int CollapsePiracyEscalationMagnitude = 500;

    // GATE.X.PRESSURE_INJECT.*.001: Source system injection magnitudes (bps).
    // Warfront demand: injected when supply deliveries shift intensity down.
    public const int WarfrontShiftMagnitude = 300;
    // Instability: injected when a node crosses a phase threshold (0→1, 1→2, etc).
    public const int InstabilityPhaseMagnitude = 200;
    // Market blocked: injected when instability closes a market (Void phase).
    public const int MarketBlockedMagnitude = 400;
    // Sustain starvation: injected when a fleet's module is disabled by supply shortfall.
    public const int SustainStarvationMagnitude = 250;
}
