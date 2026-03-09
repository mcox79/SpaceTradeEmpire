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

    // --- GATE.S4.MAINT_SUSTAIN.SUPPLY_REPAIR.001: Supply-based repair ---

    // BPS restored per unit of supply consumed during RepairWithSupply.
    public const int BpsPerSupplyUnit = 500;

    // Ticks between each 1-unit supply consumption for active sites.
    public const int SupplyConsumptionIntervalTicks = 10;

    // Multiplier for decay rate when SupplyLevel is 0 (integer; 2 = double speed).
    public const int NoSupplyDecayMultiplier = 2;

    // Maximum supply level (cap).
    public const int MaxSupplyLevel = 100;

    // --- GATE.S7.POWER.MOUNT_DEGRADE.001: Module condition decay ---

    // Condition decay per cycle (percentage points, 1% per cycle).
    public const int ModuleConditionDecayPct = 1;

    // Ticks between condition decay cycles.
    public const int ModuleConditionDecayCycleTicks = 360;
}
