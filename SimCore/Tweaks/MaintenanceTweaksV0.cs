namespace SimCore.Tweaks;

// GATE.S4.MAINT.CORE.001: Maintenance/degradation pacing constants (integers only for determinism).
public static class MaintenanceTweaksV0
{
    // Default degradation rate in basis points per tick (100 bps = 1%).
    public const int DefaultDegradePerTickBps = 10;

    // Critical health threshold in basis points. Below this, efficiency drops.
    public const int CriticalHealthBps = 5000; // 50%

    // Efficiency penalty per 1000 bps below critical (linear scaling).
    // At 0 health: penalty = CriticalHealthBps / 1000 * PenaltyPer1000Bps = 5 * 10 = 50% efficiency loss.
    public const int EfficiencyPenaltyPer1000BpsBelowCritical = 10; // pct points

    // Repair cost in credits per 1000 bps restored.
    public const int RepairCostPer1000Bps = 5;

    // Maximum health in basis points.
    public const int MaxHealthBps = 10000;

    // Minimum health floor (clamped, cannot go below).
    public const int MinHealthBps = 0;

    // Minimum degrade rate below which no decay occurs.
    public const int MinDegradeThresholdBps = 0;

    // Basis-point bucket size for cost/penalty calculations.
    public const int BpsBucketSize = 1000;

    // Minimum repair cost when any repair is needed.
    public const int MinRepairCost = 1;

    // Full efficiency percentage (integer).
    public const int FullEfficiencyPct = 100;
}
