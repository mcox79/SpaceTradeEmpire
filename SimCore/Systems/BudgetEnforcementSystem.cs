using SimCore.Entities;

namespace SimCore.Systems;

// GATE.S7.AUTOMATION_MGMT.BUDGET_ENFORCEMENT.001: Budget enforcement for fleet automation.
// Checks whether a proposed operation fits within the fleet's per-cycle budget caps.
public static class BudgetEnforcementSystem
{
    /// <summary>
    /// Returns true if the proposed credit and goods costs fit within the fleet's remaining budget.
    /// A cap of 0 means unlimited (no restriction on that dimension).
    /// </summary>
    public static bool CheckBudget(Fleet fleet, long creditCost, int goodsCost)
    {
        if (fleet == null) return false;

        var budget = fleet.Budget;
        var metrics = fleet.Metrics;

        // Check credit cap (0 = unlimited).
        if (budget.CreditCap > 0)
        {
            if (metrics.SpentCreditsThisCycle + creditCost > budget.CreditCap)
                return false;
        }

        // Check goods cap (0 = unlimited).
        if (budget.GoodsCap > 0)
        {
            if (metrics.SpentGoodsThisCycle + goodsCost > budget.GoodsCap)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Set the fleet's automation budget caps. 0 = unlimited.
    /// </summary>
    public static void SetBudget(Fleet fleet, long creditCap, int goodsCap)
    {
        if (fleet == null) return;
        fleet.Budget.CreditCap = creditCap;
        fleet.Budget.GoodsCap = goodsCap;
    }

    /// <summary>
    /// Returns the remaining budget (credits, goods) for this cycle.
    /// A return value of long.MaxValue / int.MaxValue means unlimited.
    /// </summary>
    public static (long credits, int goods) GetRemainingBudget(Fleet fleet)
    {
        if (fleet == null) return (0, 0);

        var budget = fleet.Budget;
        var metrics = fleet.Metrics;

        long remainingCredits = budget.CreditCap > 0
            ? budget.CreditCap - metrics.SpentCreditsThisCycle
            : long.MaxValue;

        int remainingGoods = budget.GoodsCap > 0
            ? budget.GoodsCap - metrics.SpentGoodsThisCycle
            : int.MaxValue;

        return (remainingCredits, remainingGoods);
    }
}
