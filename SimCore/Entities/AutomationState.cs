using System.Collections.Generic;

namespace SimCore.Entities;

// GATE.S7.AUTOMATION_MGMT.PROGRAM_METRICS.001: Per-fleet program cycle metrics.
public class ProgramCycleMetrics
{
    public int CyclesRun { get; set; }
    public int GoodsMoved { get; set; }
    public long CreditsEarned { get; set; }
    public int Failures { get; set; }
    public int LastActiveTick { get; set; } = -1;

    // GATE.S7.AUTOMATION_MGMT.FAILURE_RECOVERY.001: Consecutive failure tracking.
    public ProgramFailureReason LastFailureReason { get; set; } = ProgramFailureReason.None;
    public int ConsecutiveFailures { get; set; }

    // GATE.S7.AUTOMATION_MGMT.BUDGET_ENFORCEMENT.001: Per-cycle spending tracking.
    public long SpentCreditsThisCycle { get; set; }
    public int SpentGoodsThisCycle { get; set; }
}

// GATE.S7.AUTOMATION_MGMT.BUDGET_ENFORCEMENT.001: Per-fleet automation budget caps.
public class AutomationBudget
{
    /// <summary>Maximum credits allowed per cycle. 0 = unlimited.</summary>
    public long CreditCap { get; set; }
    /// <summary>Maximum goods units allowed per cycle. 0 = unlimited.</summary>
    public int GoodsCap { get; set; }
}

// GATE.S7.AUTOMATION_MGMT.FAILURE_RECOVERY.001: Failure reason enum.
public enum ProgramFailureReason
{
    None = 0,
    InsufficientFunds = 1,
    NoRoute = 2,
    TargetGone = 3,
    Timeout = 4,
    BudgetExceeded = 5
}

// GATE.S7.AUTOMATION_MGMT.PROGRAM_HISTORY.001: Single history entry for program outcome ring buffer.
public class ProgramHistoryEntry
{
    public int Tick { get; set; }
    public bool Success { get; set; }
    public int GoodsMoved { get; set; }
    public long CreditsEarned { get; set; }
    public ProgramFailureReason FailureReason { get; set; } = ProgramFailureReason.None;
}
