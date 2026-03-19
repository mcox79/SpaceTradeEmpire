using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S7.INSTABILITY.TICK_SYSTEM.001: Per-tick instability evolution.
// Warfront-adjacent nodes gain instability proportional to warfront intensity.
// Distant nodes stabilize slowly over time. Phase transitions happen at thresholds.
public static class InstabilitySystem
{
    private sealed class Scratch
    {
        public readonly HashSet<string> ContestedNodes = new(StringComparer.Ordinal);
        public readonly Dictionary<string, int> NodeIntensity = new(StringComparer.Ordinal);
        public readonly List<string> SortedNodeKeys = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();

    public static void Process(SimState state)
    {
        if (state.Nodes is null || state.Nodes.Count == 0) return; // STRUCTURAL: empty guard

        var scratch = s_scratch.GetOrCreateValue(state);
        // Build set of contested node IDs for fast lookup.
        var contestedNodes = scratch.ContestedNodes;
        contestedNodes.Clear();
        int maxIntensityByNode_default = 0; // STRUCTURAL: default for non-contested
        var nodeIntensity = scratch.NodeIntensity;
        nodeIntensity.Clear();

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
        var sortedNodeKeys = scratch.SortedNodeKeys;
        sortedNodeKeys.Clear();
        foreach (var k in state.Nodes.Keys) sortedNodeKeys.Add(k);
        sortedNodeKeys.Sort(StringComparer.Ordinal);
        foreach (var nodeKey in sortedNodeKeys)
        {
            var node = state.Nodes[nodeKey];

            int oldPhase = InstabilityTweaksV0.GetPhaseIndex(node.InstabilityLevel);

            if (contestedNodes.Contains(nodeKey))
            {
                // Warfront-adjacent: gain instability once per gain interval (not every tick).
                if (state.Tick > 0 && state.Tick % InstabilityTweaksV0.GainIntervalTicks == 0) // STRUCTURAL: tick guard
                {
                    int intensity = nodeIntensity.TryGetValue(nodeKey, out var i) ? i : maxIntensityByNode_default;
                    int gain = InstabilityTweaksV0.BaseGainPerInterval * intensity;
                    node.InstabilityLevel = Math.Min(node.InstabilityLevel + gain, InstabilityTweaksV0.MaxInstability);
                }
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
                    PressureTweaksV0.InstabilityPhaseMagnitude, targetRef: nodeKey);
            }
        }
    }
}
