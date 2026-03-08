#nullable enable

using Godot;
using SimCore;
using SimCore.Entities;
using System;
using System.Linq;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge
{
    // ── GATE.S7.WARFRONT.BRIDGE.001: Warfront state queries ──

    /// <summary>
    /// Returns all active warfronts as an array of dicts.
    /// Each: {id, combatant_a, combatant_b, intensity (int 0-4), intensity_label (string),
    ///        war_type (string Hot/Cold), tick_started (int), contested_count (int)}.
    /// Nonblocking read.
    /// </summary>
    public Godot.Collections.Array GetWarfrontsV0()
    {
        var result = new Godot.Collections.Array();

        TryExecuteSafeRead(state =>
        {
            if (state.Warfronts is null || state.Warfronts.Count == 0) return;

            foreach (var wf in state.Warfronts.Values.OrderBy(w => w.Id, StringComparer.Ordinal))
            {
                var intensityLabel = wf.Intensity switch
                {
                    WarfrontIntensity.Peace => "Peace",
                    WarfrontIntensity.Tension => "Tension",
                    WarfrontIntensity.Skirmish => "Skirmish",
                    WarfrontIntensity.OpenWar => "Open War",
                    WarfrontIntensity.TotalWar => "Total War",
                    _ => "Unknown",
                };

                result.Add(new Godot.Collections.Dictionary
                {
                    ["id"] = wf.Id,
                    ["combatant_a"] = wf.CombatantA,
                    ["combatant_b"] = wf.CombatantB,
                    ["intensity"] = (int)wf.Intensity,
                    ["intensity_label"] = intensityLabel,
                    ["war_type"] = wf.WarType == WarType.Hot ? "Hot" : "Cold",
                    ["tick_started"] = wf.TickStarted,
                    ["contested_count"] = wf.ContestedNodeIds.Count,
                });
            }
        }, 0);

        return result;
    }

    // ── GATE.S7.SUPPLY.BRIDGE.001: War supply delivery queries ──

    /// <summary>
    /// Returns war supply delivery info for a warfront.
    /// Returns {warfront_id, deliveries (Dictionary: goodId → int), shift_threshold (int), shift_progress_pct (int 0-100)}.
    /// Nonblocking read.
    /// </summary>
    public Godot.Collections.Dictionary GetWarSupplyV0(string warfrontId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["warfront_id"] = warfrontId ?? "",
            ["deliveries"] = new Godot.Collections.Dictionary(),
            ["shift_threshold"] = SimCore.Tweaks.WarfrontTweaksV0.SupplyShiftThreshold,
            ["shift_progress_pct"] = 0,
        };

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(warfrontId)) return;
            if (!state.WarSupplyLedger.TryGetValue(warfrontId, out var goodLedger)) return;

            var deliveries = new Godot.Collections.Dictionary();
            int totalDeliveries = 0;
            foreach (var kv in goodLedger)
            {
                deliveries[kv.Key] = kv.Value;
                totalDeliveries += kv.Value;
            }
            result["deliveries"] = deliveries;

            int threshold = SimCore.Tweaks.WarfrontTweaksV0.SupplyShiftThreshold;
            int pct = threshold > 0 ? Math.Min(totalDeliveries * 100 / threshold, 100) : 0;
            result["shift_progress_pct"] = pct;
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns the max warfront intensity affecting a specific node (0 = peace).
    /// Used by GalaxyView for visual war-zone indicators.
    /// </summary>
    public int GetNodeWarIntensityV0(string nodeId)
    {
        int maxIntensity = 0;

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(nodeId)) return;
            if (state.Warfronts is null) return;

            foreach (var wf in state.Warfronts.Values)
            {
                if (wf.ContestedNodeIds.Contains(nodeId))
                {
                    int intensity = (int)wf.Intensity;
                    if (intensity > maxIntensity)
                        maxIntensity = intensity;
                }
            }
        }, 0);

        return maxIntensity;
    }
}
