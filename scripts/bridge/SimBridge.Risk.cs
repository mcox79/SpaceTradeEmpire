#nullable enable

using Godot;
using SimCore;
using System;

namespace SpaceTradeEmpire.Bridge;

// GATE.S3.RISK_SINKS.BRIDGE.001: Risk delay queries for GameShell.
public partial class SimBridge
{
    // GATE.S3.RISK_SINKS.BRIDGE.001
    /// <summary>
    /// Returns {delayed (bool), ticks_remaining (int)} for the given fleet.
    /// Nonblocking: returns empty dict if read lock is unavailable.
    /// </summary>
    public Godot.Collections.Dictionary GetDelayStatusV0(string fleetId)
    {
        var d = new Godot.Collections.Dictionary
        {
            ["delayed"] = false,
            ["ticks_remaining"] = 0
        };
        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue(fleetId, out var fleet))
                return;
            d["delayed"] = fleet.DelayTicksRemaining > 0;
            d["ticks_remaining"] = fleet.DelayTicksRemaining;
        });
        return d;
    }

    // GATE.S3.RISK_SINKS.BRIDGE.001
    /// <summary>
    /// Returns {base_ticks (int), delay_ticks (int), total_ticks (int)} travel ETA for the given fleet.
    /// base_ticks is derived from TravelProgress (estimated ticks to destination).
    /// delay_ticks is the risk-event delay remaining.
    /// Nonblocking: returns zeros if read lock is unavailable.
    /// </summary>
    public Godot.Collections.Dictionary GetTravelEtaV0(string fleetId, string targetNodeId)
    {
        var d = new Godot.Collections.Dictionary
        {
            ["base_ticks"] = 0,
            ["delay_ticks"] = 0,
            ["total_ticks"] = 0
        };
        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue(fleetId, out var fleet))
                return;

            int baseTicks = 0;
            int delayTicks = fleet.DelayTicksRemaining;

            // If fleet is traveling, estimate remaining ticks from TravelProgress.
            // Speed is AU per tick; progress is 0..1. Remaining = (1 - progress) / speed.
            if (fleet.IsMoving && fleet.Speed > 0f)
            {
                float remaining = 1.0f - fleet.TravelProgress;
                baseTicks = (int)Math.Ceiling(remaining / fleet.Speed);
            }

            d["base_ticks"] = baseTicks;
            d["delay_ticks"] = delayTicks;
            d["total_ticks"] = baseTicks + delayTicks;
        });
        return d;
    }

    // GATE.S7.RISK_METER_UI.BRIDGE.001: Aggregated risk meters for HUD display.
    /// <summary>
    /// Returns {heat (float 0-1), influence (float 0-1), trace (float 0-1),
    ///          heat_threshold (string), influence_threshold (string), trace_threshold (string)}.
    /// Heat: max Edge.Heat near player, normalized by ConfiscationHeatThreshold * 2.
    /// Influence: inverse faction reputation at player node (high rep = low influence meter).
    /// Trace: Node.Trace at player node, normalized by TraceDetectionThreshold * 2.
    /// </summary>
    public Godot.Collections.Dictionary GetRiskMetersV0()
    {
        var d = new Godot.Collections.Dictionary
        {
            ["heat"] = 0.0f,
            ["influence"] = 0.0f,
            ["trace"] = 0.0f,
            ["heat_threshold"] = "Calm",
            ["influence_threshold"] = "Calm",
            ["trace_threshold"] = "Calm",
        };
        TryExecuteSafeRead(state =>
        {
            const string playerFleetId = "fleet_trader_1";
            if (!state.Fleets.TryGetValue(playerFleetId, out var fleet))
                return;

            string nodeId = fleet.CurrentNodeId;
            if (string.IsNullOrEmpty(nodeId))
                return;

            // Heat: max Edge.Heat on edges adjacent to player node.
            float maxHeat = 0f;
            float heatCap = SimCore.Tweaks.SecurityTweaksV0.ConfiscationHeatThreshold * 2.0f;
            foreach (var edge in state.Edges.Values)
            {
                if (edge.FromNodeId == nodeId || edge.ToNodeId == nodeId)
                {
                    if (edge.Heat > maxHeat) maxHeat = edge.Heat;
                }
            }
            float heatNorm = heatCap > 0f ? Math.Clamp(maxHeat / heatCap, 0f, 1f) : 0f;

            // Influence: inverse of faction reputation at player's node.
            // Rep range [-100, 100] mapped to influence [1.0, 0.0].
            float influenceNorm = 0.5f; // default: neutral
            if (state.NodeFactionId.TryGetValue(nodeId, out var controllingFaction) && !string.IsNullOrEmpty(controllingFaction))
            {
                int rep = 0;
                state.FactionReputation.TryGetValue(controllingFaction, out rep);
                // Map [-100, 100] -> [1.0, 0.0]: influence = (100 - rep) / 200
                influenceNorm = Math.Clamp((100f - rep) / 200f, 0f, 1f);
            }

            // Trace: node.Trace normalized by detection threshold * 2.
            float traceNorm = 0f;
            if (state.Nodes.TryGetValue(nodeId, out var traceNode))
            {
                float traceCap = SimCore.Tweaks.FractureTweaksV0.TraceDetectionThreshold * 2.0f;
                traceNorm = traceCap > 0f ? Math.Clamp(traceNode.Trace / traceCap, 0f, 1f) : 0f;
            }

            d["heat"] = heatNorm;
            d["influence"] = influenceNorm;
            d["trace"] = traceNorm;
            d["heat_threshold"] = ThresholdName(heatNorm);
            d["influence_threshold"] = ThresholdName(influenceNorm);
            d["trace_threshold"] = ThresholdName(traceNorm);
        });
        return d;
    }

    /// Maps a 0-1 normalized value to a human-readable threshold name.
    private static string ThresholdName(float value)
    {
        if (value >= 0.8f) return "Critical";
        if (value >= 0.6f) return "High";
        if (value >= 0.4f) return "Elevated";
        if (value >= 0.2f) return "Noticed";
        return "Calm";
    }
}
