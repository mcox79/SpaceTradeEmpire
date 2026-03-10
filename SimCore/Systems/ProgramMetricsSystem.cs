using SimCore.Entities;

namespace SimCore.Systems;

// GATE.S7.AUTOMATION_MGMT.PROGRAM_METRICS.001: Records program cycle outcomes to fleet metrics.
public static class ProgramMetricsSystem
{
    /// <summary>
    /// Record a successful program cycle: increments CyclesRun, accumulates goods and credits.
    /// Resets consecutive failure counter on success.
    /// </summary>
    public static void RecordCycleSuccess(Fleet fleet, int goodsMoved, long credits, int tick)
    {
        if (fleet == null) return;
        fleet.Metrics.CyclesRun++;
        fleet.Metrics.GoodsMoved += goodsMoved;
        fleet.Metrics.CreditsEarned += credits;
        fleet.Metrics.LastActiveTick = tick;
        // Success resets consecutive failure tracking.
        fleet.Metrics.ConsecutiveFailures = 0;
        fleet.Metrics.LastFailureReason = ProgramFailureReason.None;
    }

    /// <summary>
    /// Record a failed program cycle: increments Failures counter.
    /// </summary>
    public static void RecordCycleFailure(Fleet fleet, int tick)
    {
        if (fleet == null) return;
        fleet.Metrics.Failures++;
        fleet.Metrics.LastActiveTick = tick;
    }
}
