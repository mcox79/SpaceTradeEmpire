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
}
