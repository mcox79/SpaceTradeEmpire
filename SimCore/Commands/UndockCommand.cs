using SimCore.Entities;

namespace SimCore.Commands;

public class UndockCommand : ICommand
{
    public string FleetId { get; set; }

    public UndockCommand(string fleetId)
    {
        FleetId = fleetId;
    }

    public void Execute(SimState state)
    {
        if (state.Fleets.TryGetValue(FleetId, out var fleet))
        {
            // Transition from Docked -> Idle
            if (fleet.State == FleetState.Docked)
            {
                fleet.State = FleetState.Idle;
            }
        }
    }
}