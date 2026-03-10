using SimCore.Entities;

namespace SimCore.Systems;

// GATE.S7.AUTOMATION_MGMT.FAILURE_RECOVERY.001: Failure tracking and retry logic.
// Tracks consecutive failures per fleet and decides whether to retry or halt.
public static class FailureRecoverySystem
{
    /// <summary>Maximum consecutive failures before retry is denied.</summary>
    public const int MaxConsecutiveFailures = 3;

    /// <summary>
    /// Record a failure with a specific reason. Increments consecutive failure counter.
    /// </summary>
    public static void RecordFailure(Fleet fleet, ProgramFailureReason reason, int tick)
    {
        if (fleet == null) return;
        fleet.Metrics.ConsecutiveFailures++;
        fleet.Metrics.LastFailureReason = reason;
        fleet.Metrics.Failures++;
        fleet.Metrics.LastActiveTick = tick;
    }

    /// <summary>
    /// Returns true if the fleet should retry (fewer than MaxConsecutiveFailures consecutive failures).
    /// </summary>
    public static bool ShouldRetry(Fleet fleet)
    {
        if (fleet == null) return false;
        return fleet.Metrics.ConsecutiveFailures < MaxConsecutiveFailures;
    }

    /// <summary>
    /// Returns the most recent failure reason for the fleet.
    /// </summary>
    public static ProgramFailureReason GetLastFailureReason(Fleet fleet)
    {
        if (fleet == null) return ProgramFailureReason.None;
        return fleet.Metrics.LastFailureReason;
    }
}
