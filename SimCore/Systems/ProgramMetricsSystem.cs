using SimCore.Entities;

namespace SimCore.Systems;

// GATE.S7.AUTOMATION_MGMT.PROGRAM_METRICS.001: Records program cycle outcomes to fleet metrics.
public static class ProgramMetricsSystem
{
    /// <summary>
    /// Record a successful program cycle: increments CyclesRun, accumulates goods and credits.
    /// Resets consecutive failure counter on success.
    /// </summary>
    public static void RecordCycleSuccess(Fleet fleet, int goodsMoved, long credits, int tick, long expense = 0)
    {
        if (fleet == null) return;
        fleet.Metrics.CyclesRun++;
        fleet.Metrics.GoodsMoved += goodsMoved;
        fleet.Metrics.CreditsEarned += credits;
        fleet.Metrics.LastActiveTick = tick;
        // Success resets consecutive failure tracking.
        fleet.Metrics.ConsecutiveFailures = 0;
        fleet.Metrics.LastFailureReason = ProgramFailureReason.None;
        // GATE.S7.AUTOMATION.PERF_TRACKING.001: Extended metrics.
        fleet.Metrics.TotalExpense += expense;
        fleet.Metrics.TradesCompleted++;
    }

    /// <summary>
    /// Record a failed program cycle: increments Failures counter.
    /// GATE.S7.AUTOMATION.FAILURE_REASONS.001: Now accepts failure reason code.
    /// </summary>
    public static void RecordCycleFailure(Fleet fleet, int tick, ProgramFailureReason reason = ProgramFailureReason.None)
    {
        if (fleet == null) return;
        fleet.Metrics.Failures++;
        fleet.Metrics.LastActiveTick = tick;
        // GATE.S7.AUTOMATION.FAILURE_REASONS.001: Record failure reason.
        if (reason != ProgramFailureReason.None)
        {
            fleet.Metrics.LastFailureReason = reason;
            fleet.Metrics.ConsecutiveFailures++;
        }
    }

    /// <summary>
    /// GATE.S7.AUTOMATION.PERF_TRACKING.001: Compute net profit from accumulated metrics.
    /// </summary>
    public static long GetNetProfit(Fleet fleet)
    {
        if (fleet == null) return 0;
        return fleet.Metrics.CreditsEarned - fleet.Metrics.TotalExpense;
    }
}
