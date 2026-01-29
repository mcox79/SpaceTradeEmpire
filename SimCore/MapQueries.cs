namespace SimCore;

public static class MapQueries
{
    public static bool TryGetEdgeId(SimState state, string fromNodeId, string toNodeId, out string edgeId)
    {
        edgeId = "";
        if (string.IsNullOrEmpty(fromNodeId) || string.IsNullOrEmpty(toNodeId)) return false;
        if (fromNodeId == toNodeId) return false;

        foreach (var e in state.Edges.Values)
        {
            if ((e.FromNodeId == fromNodeId && e.ToNodeId == toNodeId) ||
                (e.ToNodeId == fromNodeId && e.FromNodeId == toNodeId))
            {
                edgeId = e.Id;
                return true;
            }
        }
        return false;
    }

    public static bool AreConnected(SimState state, string fromNodeId, string toNodeId)
        => TryGetEdgeId(state, fromNodeId, toNodeId, out _);
}