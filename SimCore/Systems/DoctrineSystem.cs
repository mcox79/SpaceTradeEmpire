using SimCore.Entities;

namespace SimCore.Systems;

// GATE.S7.AUTOMATION_MGMT.DOCTRINE.001: Doctrine evaluation system.
// Evaluates fleet retreat conditions and effective engagement stance.
public static class DoctrineSystem
{
    /// <summary>
    /// Returns true if the fleet's hull percentage is at or below the retreat threshold.
    /// If HullHpMax is uninitialized (-1) or zero, retreat is never triggered.
    /// </summary>
    public static bool EvaluateRetreat(Fleet fleet)
    {
        if (fleet == null) return false;
        if (fleet.HullHpMax <= 0) return false;

        int hullPct = (int)((long)fleet.HullHp * 100 / fleet.HullHpMax);
        return hullPct <= fleet.Doctrine.RetreatThresholdPct;
    }

    /// <summary>
    /// Returns the effective engagement stance from the fleet's doctrine.
    /// </summary>
    public static EngagementStance GetEffectiveStance(Fleet fleet)
    {
        if (fleet == null) return EngagementStance.Defensive;
        return fleet.Doctrine.Stance;
    }

    // GATE.S7.AUTOMATION.BUDGET_CAPS.001: Budget enforcement.

    /// <summary>
    /// Returns true if the fleet's current cycle spending would exceed the budget cap.
    /// CreditCap of 0 means unlimited.
    /// </summary>
    public static bool IsBudgetExceeded(Fleet fleet, long additionalCredits = 0)
    {
        if (fleet == null) return false;
        if (fleet.Budget.CreditCap <= 0) return false; // 0 = unlimited
        return fleet.Metrics.SpentCreditsThisCycle + additionalCredits > fleet.Budget.CreditCap;
    }

    /// <summary>
    /// Returns true if the fleet's goods spending would exceed the goods cap.
    /// GoodsCap of 0 means unlimited.
    /// </summary>
    public static bool IsGoodsBudgetExceeded(Fleet fleet, int additionalGoods = 0)
    {
        if (fleet == null) return false;
        if (fleet.Budget.GoodsCap <= 0) return false; // 0 = unlimited
        return fleet.Metrics.SpentGoodsThisCycle + additionalGoods > fleet.Budget.GoodsCap;
    }

    /// <summary>
    /// Record spending against the per-cycle budget counters.
    /// </summary>
    public static void RecordCycleSpending(Fleet fleet, long credits, int goods)
    {
        if (fleet == null) return;
        fleet.Metrics.SpentCreditsThisCycle += credits;
        fleet.Metrics.SpentGoodsThisCycle += goods;
    }

    /// <summary>
    /// Reset per-cycle spending counters (call at the start of each automation cycle).
    /// </summary>
    public static void ResetCycleSpending(Fleet fleet)
    {
        if (fleet == null) return;
        fleet.Metrics.SpentCreditsThisCycle = 0;
        fleet.Metrics.SpentGoodsThisCycle = 0;
    }
}
