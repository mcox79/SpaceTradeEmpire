using SimCore.Entities;

namespace SimCore.Commands;

// Slice 3 / GATE.UI.FLEET.002
// Cancel the active logistics job for a fleet via command (UI must not directly mutate).
public sealed class FleetJobCancelCommand : ICommand
{
    public string FleetId { get; set; } = "";
    public string Note { get; set; } = "";

    public FleetJobCancelCommand(string fleetId, string note = "")
    {
        FleetId = fleetId ?? "";
        Note = note ?? "";
    }

    public void Execute(SimState state)
    {
        if (state == null) return;
        if (string.IsNullOrWhiteSpace(FleetId)) return;

        if (!state.Fleets.TryGetValue(FleetId, out var fleet)) return;

        if (fleet.CurrentJob == null) return;

        // Clear job.
        fleet.CurrentJob = null;

        // Clear any UI manual override does not automatically clear on cancel.
        // (Player intent: cancel job, not necessarily override.)
        // If you want cancel to also clear override later, do it in tests/doctrine explicitly.

        // Clear route plan (best-effort) so the fleet is not "job routed" anymore.
        fleet.FinalDestinationNodeId = "";
        fleet.RouteEdgeIds.Clear();
        fleet.RouteEdgeIndex = 0;

        // Task surface: deterministic string.
        fleet.CurrentTask = "Idle";

        // Note: we do not forcibly change fleet.State here.
        // Canceling a job should not teleport/rewind travel; it only stops job execution.
        // If your doctrine requires forcing Idle only when not moving, encode it in tests.
    }
}
