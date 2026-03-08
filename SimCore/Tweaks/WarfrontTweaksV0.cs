namespace SimCore.Tweaks;

// GATE.S7.WARFRONT.STATE_MODEL.001: Warfront tuning constants.
public static class WarfrontTweaksV0
{
    // ── Intensity thresholds (for escalation/de-escalation) ──
    public const int PeaceIntensity = 0;
    public const int TensionIntensity = 1;
    public const int SkirmishIntensity = 2;
    public const int OpenWarIntensity = 3;
    public const int TotalWarIntensity = 4;

    // ── War surcharge on tariffs (basis points per intensity level) ──
    // GATE.S7.WARFRONT.TARIFF_SCALING.001: EffectiveTariff = BaseTariff + (WarSurchargeBps * Intensity) / 10000
    public const int WarSurchargeBpsPerIntensity = 300;  // +3% per intensity level

    // ── Neutrality tax (basis points, additive to tariff at war-zone stations) ──
    // GATE.S7.WARFRONT.NEUTRALITY_TAX.001: Applied to traders with no allegiance declaration.
    public const int NeutralityTaxBpsIntensity2 = 500;   // +5% at Skirmish
    public const int NeutralityTaxBpsIntensity3 = 1000;  // +10% at Open War
    public const int NeutralityTaxBpsIntensity4 = 1500;  // +15% at Total War

    // ── Demand shock multipliers (applied to faction consumption during war) ──
    // GATE.S7.WARFRONT.DEMAND_SHOCK.001: Multiplied against base NPC consumption.
    // Values represent multiplier * 100 (integer arithmetic for determinism).
    public const int MunitionsDemandMultiplierPct = 400;    // 4x at max intensity
    public const int CompositesDemandMultiplierPct = 250;   // 2.5x
    public const int FuelDemandMultiplierPct = 300;         // 3x
    public const int DefaultDemandMultiplierPct = 100;      // 1x (no change)

    // Demand scales linearly: effective = base + (multiplier - 100) * intensity / TotalWarIntensity
    // At intensity 4 (TotalWar), full multiplier applies.

    // ── Evolution timing (tick ranges for state transitions) ──
    // GATE.S7.WARFRONT.EVOLUTION.001: Cold war escalation window.
    public const int ColdWarEscalateMinTick = 200;
    public const int ColdWarEscalateMaxTick = 600;
    // Hot war ceasefire window.
    public const int HotWarCeasefireMinTick = 600;
    public const int HotWarCeasefireMaxTick = 1200;

    // ── Supply shift thresholds ──
    // GATE.S7.SUPPLY.WARFRONT_SHIFT.001: Cumulative supply delivery threshold to shift intensity.
    // When total deliveries (all goods combined) exceed this, defender intensity shifts by -1.
    // Threshold resets after each shift.
    public const int SupplyShiftThreshold = 500;

    // ── Contested node detection radius (BFS depth from faction borders) ──
    public const int ContestedBfsDepth = 1;
}
