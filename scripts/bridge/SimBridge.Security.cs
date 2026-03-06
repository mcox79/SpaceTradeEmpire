using Godot;
using System.Collections.Generic;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge : Node
{
    // GATE.S5.SEC_LANES.BRIDGE.001: Security lane bridge queries.

    /// Returns security level BPS for the edge between fromNodeId and toNodeId.
    /// Returns SecurityTweaksV0.DefaultSecurityBps (5000) if edge not found.
    public int GetLaneSecurityV0(string fromNodeId, string toNodeId)
    {
        int result = SimCore.Tweaks.SecurityTweaksV0.DefaultSecurityBps;
        TryExecuteSafeRead(state =>
        {
            foreach (var edge in state.Edges.Values)
            {
                if ((edge.FromNodeId == fromNodeId && edge.ToNodeId == toNodeId) ||
                    (edge.FromNodeId == toNodeId && edge.ToNodeId == fromNodeId))
                {
                    result = edge.SecurityLevelBps;
                    return;
                }
            }
        }, 0);
        return result;
    }

    // GATE.S5.SEC_LANES.UI.001: Security band queries for UI display.

    /// Returns the security band string for the edge between two nodes.
    /// Values: "hostile", "dangerous", "moderate", "safe".
    public string GetSecurityBandV0(string fromNodeId, string toNodeId)
    {
        int bps = GetLaneSecurityV0(fromNodeId, toNodeId);
        return SimCore.Systems.SecurityLaneSystem.GetSecurityBand(bps);
    }

    /// Returns the security band string for a node (based on average adjacent edge security).
    public string GetNodeSecurityBandV0(string nodeId)
    {
        int bps = GetNodeSecurityV0(nodeId);
        return SimCore.Systems.SecurityLaneSystem.GetSecurityBand(bps);
    }

    /// Returns average security BPS of all edges adjacent to nodeId.
    public int GetNodeSecurityV0(string nodeId)
    {
        int result = SimCore.Tweaks.SecurityTweaksV0.DefaultSecurityBps;
        TryExecuteSafeRead(state =>
        {
            int total = 0;
            int count = 0;
            foreach (var edge in state.Edges.Values)
            {
                if (edge.FromNodeId == nodeId || edge.ToNodeId == nodeId)
                {
                    total += edge.SecurityLevelBps;
                    count++;
                }
            }
            if (count > 0)
                result = total / count;
        }, 0);
        return result;
    }
}
