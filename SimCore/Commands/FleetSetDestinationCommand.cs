using SimCore.Entities;

namespace SimCore.Commands;

// Slice 3 / GATE.UI.FLEET.003 + GATE.UI.FLEET.AUTH.001
// Setting a non-empty TargetNodeId asserts ManualOverrideNodeId and cancels any active LogisticsJob.
// Setting TargetNodeId="" clears ManualOverrideNodeId and does NOT resume canceled jobs.
public sealed class FleetSetDestinationCommand : ICommand
{
    public string FleetId { get; set; } = "";
    public string TargetNodeId { get; set; } = "";
    public string Note { get; set; } = "";

    public FleetSetDestinationCommand(string fleetId, string targetNodeId, string note = "")
    {
        FleetId = fleetId ?? "";
        TargetNodeId = targetNodeId ?? "";
        Note = note ?? "";
    }

    public void Execute(SimState state)
    {
        if (state == null) return;
        if (string.IsNullOrWhiteSpace(FleetId)) return;

        if (!state.Fleets.TryGetValue(FleetId, out var fleet)) return;

        // CLEAR override
        if (string.IsNullOrWhiteSpace(TargetNodeId))
        {
            fleet.ManualOverrideNodeId = "";

            // Clearing override does not resume canceled jobs.
            // Keep route state as-is; route planner/system can decide what to do next.
            if (fleet.CurrentJob == null && !fleet.IsMoving)
            {
                fleet.CurrentTask = "Idle";
            }

            return;
        }

        // VALIDATION: target must exist
        if (!state.Nodes.ContainsKey(TargetNodeId)) return;

        // Assert override.
        fleet.ManualOverrideNodeId = TargetNodeId;

        // Force immediate, deterministic override routing: clear any prior plan/request.
        fleet.RouteEdgeIds.Clear();
        fleet.RouteEdgeIndex = 0;
        fleet.FinalDestinationNodeId = "";
        fleet.DestinationNodeId = "";

        // Authority precedence: issuing ManualOverride cancels any active LogisticsJob.
        if (fleet.CurrentJob != null)
        {
            fleet.CurrentJob = null;
        }

        // Update explain surface deterministically.
        fleet.CurrentTask = $"ManualOverride:{TargetNodeId}";
    }
}
