using SimCore.Systems;

namespace SimCore.Commands;

// GATE.S8.MEGAPROJECT.SYSTEM.001: Start a megaproject at a node.
public sealed class StartMegaprojectCommand : ICommand
{
    public string TypeId { get; set; }
    public string NodeId { get; set; }
    public string FleetId { get; set; }

    public StartMegaprojectCommand(string typeId, string nodeId, string fleetId)
    {
        TypeId = typeId;
        NodeId = nodeId;
        FleetId = fleetId;
    }

    public void Execute(SimState state)
    {
        MegaprojectSystem.StartMegaproject(state, TypeId, NodeId, FleetId);
    }
}
