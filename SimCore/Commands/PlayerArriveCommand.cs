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
        bool isNew = state.PlayerVisitedNodeIds.Add(TargetNodeId);
        // GATE.S12.PROGRESSION.STATS.001: Track nodes visited.
        if (isNew && state.PlayerStats != null)
            state.PlayerStats.NodesVisited = state.PlayerVisitedNodeIds.Count;
    }
}
