using SimCore.Systems;

namespace SimCore.Commands;

// GATE.S8.HAVEN.HANGAR.001: Player swaps active ship with stored ship at Haven.
public class SwapShipCommand : ICommand
{
    public string ActiveFleetId { get; set; }
    public string StoredFleetId { get; set; }

    public SwapShipCommand(string activeFleetId, string storedFleetId)
    {
        ActiveFleetId = activeFleetId;
        StoredFleetId = storedFleetId;
    }

    public void Execute(SimState state)
    {
        HavenHangarSystem.SwapShip(state, ActiveFleetId, StoredFleetId);
    }
}
