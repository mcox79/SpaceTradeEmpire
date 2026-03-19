using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S8.THREAT.SUPPLY_SHOCK.001: Warfront disrupts production chains.
public static class SupplyShockSystem
{
    private sealed class Scratch
    {
        public readonly Dictionary<string, WarfrontIntensity> NodeIntensity = new(StringComparer.Ordinal);
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();

    // Per-tick: reduce IndustrySite efficiency at contested warfront nodes.
    public static void Process(SimState state)
    {
        if (state.Warfronts == null || state.Warfronts.Count == 0) return;

        var scratch = s_scratch.GetOrCreateValue(state);
        // Build lookup of contested node → max intensity.
        var nodeIntensity = scratch.NodeIntensity;
        nodeIntensity.Clear();
        foreach (var wf in state.Warfronts.Values)
        {
            if (wf.Intensity < WarfrontIntensity.Skirmish) continue;
            foreach (var nodeId in wf.ContestedNodeIds)
            {
                if (!nodeIntensity.TryGetValue(nodeId, out var existing) || wf.Intensity > existing)
                    nodeIntensity[nodeId] = wf.Intensity;
            }
        }

        if (nodeIntensity.Count == 0) return;

        // Apply efficiency penalty to industry sites at contested nodes.
        foreach (var kv in state.IndustrySites)
        {
            var site = kv.Value;
            if (site == null) continue;

            if (nodeIntensity.TryGetValue(site.NodeId, out var intensity))
            {
                int reductionPct = intensity >= WarfrontIntensity.OpenWar
                    ? ThreatTweaksV0.BattleOutputReductionPct
                    : ThreatTweaksV0.SkirmishOutputReductionPct;

                float factor = Math.Max(0f, (100 - reductionPct) / 100f);
                site.Efficiency = Math.Min(site.Efficiency, factor);
            }
        }
    }
}
