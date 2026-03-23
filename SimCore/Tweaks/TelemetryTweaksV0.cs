namespace SimCore.Tweaks;

// GATE.T48.TELEMETRY.SESSION_WRITER.001: Telemetry snapshot tuning constants.
public static class TelemetryTweaksV0
{
    // Snapshot every N ticks.
    public const int SnapshotIntervalTicks = 100;

    // Maximum snapshots to retain (ring buffer).
    public const int MaxSnapshots = 50;

    // GATE.T51.TELEMETRY.LOCAL_STORE.001: Maximum events to retain per session.
    public const int MaxEvents = 500;
}
