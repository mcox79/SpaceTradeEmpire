namespace SimCore.Commands;

public class PlayerArriveCommand : ICommand
{
    public string TargetNodeId { get; set; } = "";

    public PlayerArriveCommand(string targetNodeId)
    {
        TargetNodeId = targetNodeId ?? "";
    }

    public void Execute(SimState state)
    {
        if (string.IsNullOrWhiteSpace(TargetNodeId)) return;
        if (!state.Nodes.ContainsKey(TargetNodeId)) return;
        state.PlayerLocationNodeId = TargetNodeId;
        state.PlayerVisitedNodeIds.Add(TargetNodeId);
    }
}
