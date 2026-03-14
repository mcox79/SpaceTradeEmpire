using System;
using System.Collections.Generic;
using System.Linq;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S7.INSTABILITY.TICK_SYSTEM.001: Per-tick instability evolution.
// Warfront-adjacent nodes gain instability proportional to warfront intensity.
// Distant nodes stabilize slowly over time. Phase transitions happen at thresholds.
public static class InstabilitySystem
{
    public static void Process(SimState state)
    {
        if (state.Nodes is null || state.Nodes.Count == 0) return; // STRUCTURAL: empty guard

        // Build set of contested node IDs for fast lookup.
        var contestedNodes = new HashSet<string>(StringComparer.Ordinal);
        int maxIntensityByNode_default = 0; // STRUCTURAL: default for non-contested
        var nodeIntensity = new Dictionary<string, int>(StringComparer.Ordinal);

        if (state.Warfronts is not null)
        {
            foreach (var wf in state.Warfronts.Values)
            {
                if (wf.Intensity == WarfrontIntensity.Peace) continue;
                int intensity = (int)wf.Intensity;

                foreach (var nodeId in wf.ContestedNodeIds)
                {
                    contestedNodes.Add(nodeId);
                    nodeIntensity.TryGetValue(nodeId, out var existing);
                    if (intensity > existing)
                        nodeIntensity[nodeId] = intensity;
                }
            }
        }

        // Process all nodes deterministically.
        foreach (var kv in state.Nodes.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var node = kv.Value;

            int oldPhase = InstabilityTweaksV0.GetPhaseIndex(node.InstabilityLevel);

            if (contestedNodes.Contains(kv.Key))
            {
                // Warfront-adjacent: gain instability.
                int intensity = nodeIntensity.TryGetValue(kv.Key, out var i) ? i : maxIntensityByNode_default;
                int gain = InstabilityTweaksV0.BaseGainPerTick * intensity;
                node.InstabilityLevel = Math.Min(node.InstabilityLevel + gain, InstabilityTweaksV0.MaxInstability);
            }
            else if (node.InstabilityLevel > 0) // STRUCTURAL: skip stable nodes
            {
                // Distant: decay toward 0, once per decay interval.
                if (state.Tick > 0 && state.Tick % InstabilityTweaksV0.DecayIntervalTicks == 0) // STRUCTURAL: tick guard
                {
                    node.InstabilityLevel = Math.Max(0, node.InstabilityLevel - InstabilityTweaksV0.DecayAmountPerInterval); // STRUCTURAL: floor at zero
                }
            }

            // GATE.X.PRESSURE_INJECT.INSTABILITY.001: Inject pressure on phase transition.
            int newPhase = InstabilityTweaksV0.GetPhaseIndex(node.InstabilityLevel);
            if (newPhase != oldPhase)
            {
                PressureSystem.InjectDelta(state, "instability", "phase_transition",
                    PressureTweaksV0.InstabilityPhaseMagnitude, targetRef: kv.Key);
            }
        }
    }
}
