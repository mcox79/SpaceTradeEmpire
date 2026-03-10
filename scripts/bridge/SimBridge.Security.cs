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

    // GATE.S7.ENFORCEMENT.BRIDGE.001: Edge heat query for UI display.

    /// Returns heat info for the edge between two nodes:
    /// {edge_id, heat (float), threshold_name (string), decay_rate (float)}.
    public Godot.Collections.Dictionary GetEdgeHeatV0(string fromNodeId, string toNodeId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["edge_id"] = "",
            ["heat"] = 0.0f,
            ["threshold_name"] = "safe",
            ["decay_rate"] = SimCore.Tweaks.SecurityTweaksV0.HeatDecayPerTick,
            ["confiscation_threshold"] = SimCore.Tweaks.SecurityTweaksV0.ConfiscationHeatThreshold,
        };

        TryExecuteSafeRead(state =>
        {
            foreach (var edge in state.Edges.Values)
            {
                if ((edge.FromNodeId == fromNodeId && edge.ToNodeId == toNodeId) ||
                    (edge.FromNodeId == toNodeId && edge.ToNodeId == fromNodeId))
                {
                    result["edge_id"] = edge.Id;
                    result["heat"] = edge.Heat;
                    if (edge.Heat >= SimCore.Tweaks.SecurityTweaksV0.ConfiscationHeatThreshold)
                        result["threshold_name"] = "confiscation";
                    else if (edge.Heat > 1.0f)
                        result["threshold_name"] = "elevated";
                    else if (edge.Heat > 0.5f)
                        result["threshold_name"] = "warm";
                    else
                        result["threshold_name"] = "safe";
                    return;
                }
            }
        }, 0);

        return result;
    }

    // GATE.S7.ENFORCEMENT.BRIDGE.001: Recent confiscation event history.

    /// Returns the most recent confiscation events (up to 10).
    /// Each entry: {tick, edge_id, good_id, units, fine_credits, cause}.
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetConfiscationHistoryV0()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        TryExecuteSafeRead(state =>
        {
            if (state.SecurityEventLog == null) return;
            int count = 0;
            // Iterate backwards for most recent first.
            for (int i = state.SecurityEventLog.Count - 1; i >= 0 && count < 10; i--)
            {
                var e = state.SecurityEventLog[i];
                if (e.Type != SimCore.Events.SecurityEvents.SecurityEventType.Confiscation) continue;
                var entry = new Godot.Collections.Dictionary
                {
                    ["tick"] = e.Tick,
                    ["edge_id"] = e.EdgeId,
                    ["good_id"] = e.ConfiscatedGoodId,
                    ["units"] = e.ConfiscatedUnits,
                    ["fine_credits"] = e.FineCredits,
                    ["cause"] = e.CauseChain,
                };
                result.Add(entry);
                count++;
            }
        }, 0);

        return result;
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
