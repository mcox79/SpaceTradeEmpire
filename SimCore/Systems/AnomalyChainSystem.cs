using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.T48.ANOMALY.CHAIN_SYSTEM.001: Anomaly chain lifecycle — initiation, step advancement, worldgen seeding.
// Step advancement is handled by DiscoveryOutcomeSystem.TryAdvanceChains (already wired).
// This system provides initiation and worldgen seeding entry points.
public static class AnomalyChainSystem
{
    private sealed class Scratch
    {
        public readonly List<string> SortedIds = new();
        public readonly Queue<string> BfsQueue = new();
        public readonly HashSet<string> BfsVisited = new(StringComparer.Ordinal);
        public readonly Dictionary<string, int> BfsDistances = new(StringComparer.Ordinal);
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();

    /// <summary>
    /// Per-tick processing: check active chains for step completion via discovery analysis.
    /// The actual chain advancement is done in DiscoveryOutcomeSystem.TryAdvanceChains,
    /// so this method handles any additional per-tick chain logic (e.g., timeout, failure conditions).
    /// </summary>
    public static void Process(SimState state)
    {
        if (state is null) return; // STRUCTURAL: null guard
        if (state.AnomalyChains.Count == 0) return; // STRUCTURAL: empty guard

        // Chain advancement is handled by DiscoveryOutcomeSystem.TryAdvanceChains.
        // This system is a placeholder for future per-tick chain logic
        // (e.g., chain timeout, environmental triggers, chain failure on world events).
    }

    /// <summary>
    /// Initiate a new anomaly chain from a template. Places discovery sites at reachable
    /// deep-space nodes using BFS from the starter node.
    /// </summary>
    public static bool InitiateChain(SimState state, string chainId, string starterNodeId)
    {
        if (state is null || string.IsNullOrEmpty(chainId) || string.IsNullOrEmpty(starterNodeId)) return false;
        if (state.AnomalyChains.ContainsKey(chainId)) return false; // Already exists

        // Find template.
        AnomalyChainContentV0.ChainTemplate? template = null;
        foreach (var t in AnomalyChainContentV0.AllChains)
        {
            if (string.Equals(t.ChainId, chainId, StringComparison.Ordinal))
            {
                template = t;
                break;
            }
        }
        if (template is null) return false;

        // Build BFS distances from starter.
        var scratch = s_scratch.GetOrCreateValue(state);
        var distances = ComputeHopDistances(state, starterNodeId, scratch);

        // Collect used nodes.
        var usedNodes = new HashSet<string>(StringComparer.Ordinal);
        usedNodes.Add(starterNodeId);

        // Place discovery at each step.
        var chain = new AnomalyChain
        {
            ChainId = chainId,
            Status = AnomalyChainStatus.Active,
            StartedTick = state.Tick,
            StarterNodeId = starterNodeId,
            CurrentStepIndex = 0, // STRUCTURAL: start at first step
        };

        foreach (var stepTemplate in template.Steps)
        {
            var candidates = new List<string>();
            for (int d = stepTemplate.MinHopsFromStarter; d <= stepTemplate.MaxHopsFromStarter; d++)
            {
                foreach (var kv in distances)
                {
                    if (kv.Value == d && !usedNodes.Contains(kv.Key))
                        candidates.Add(kv.Key);
                }
                candidates.Sort(StringComparer.Ordinal);
                if (candidates.Count >= 3) break; // STRUCTURAL: enough candidates
            }

            // Fallback: closest available.
            if (candidates.Count == 0) // STRUCTURAL: empty guard
            {
                var sortedAll = scratch.SortedIds;
                sortedAll.Clear();
                foreach (var kv in distances) sortedAll.Add(kv.Key);
                sortedAll.Sort(StringComparer.Ordinal);
                foreach (var nid in sortedAll)
                {
                    if (!usedNodes.Contains(nid) && !string.Equals(nid, starterNodeId, StringComparison.Ordinal))
                    {
                        candidates.Add(nid);
                        break;
                    }
                }
            }

            string placedNodeId = "";
            if (candidates.Count > 0)
            {
                // Deterministic pick: use Rng if available, else first candidate.
                int idx = state.Rng is not null ? state.Rng.Next(candidates.Count) : 0; // STRUCTURAL: fallback index
                placedNodeId = candidates[idx];
                usedNodes.Add(placedNodeId);
            }

            // Create discovery at the placed node.
            string discoveryId = "";
            if (!string.IsNullOrEmpty(placedNodeId) && state.Nodes.TryGetValue(placedNodeId, out var node))
            {
                discoveryId = $"disc_v0|{stepTemplate.DiscoveryKind}|{placedNodeId}|CHAIN_{chainId}|step_{stepTemplate.StepIndex}";
                node.SeededDiscoveryIds ??= new List<string>();
                if (!node.SeededDiscoveryIds.Contains(discoveryId))
                    node.SeededDiscoveryIds.Add(discoveryId);

                // Create intel discovery entry.
                if (!state.Intel.Discoveries.ContainsKey(discoveryId))
                {
                    state.Intel.Discoveries[discoveryId] = new DiscoveryStateV0
                    {
                        DiscoveryId = discoveryId,
                        Phase = DiscoveryPhase.Seen,
                    };
                }
            }

            chain.Steps.Add(new AnomalyChainStep
            {
                StepIndex = stepTemplate.StepIndex,
                DiscoveryKind = stepTemplate.DiscoveryKind,
                MinHopsFromStarter = stepTemplate.MinHopsFromStarter,
                MaxHopsFromStarter = stepTemplate.MaxHopsFromStarter,
                NarrativeText = stepTemplate.NarrativeText,
                LeadText = stepTemplate.LeadText,
                LootOverrides = new Dictionary<string, int>(stepTemplate.LootOverrides),
                PlacedDiscoveryId = discoveryId,
                IsCompleted = false,
            });
        }

        state.AnomalyChains[chainId] = chain;
        return true;
    }

    /// <summary>
    /// Called during world generation. Pick 1-2 chains to seed based on galaxy size.
    /// Place starter discovery at a deep-space node (high instability phase).
    /// </summary>
    public static void SeedChainsAtWorldgen(SimState state)
    {
        if (state is null) return; // STRUCTURAL: null guard
        if (AnomalyChainContentV0.AllChains.Count == 0) return; // STRUCTURAL: no templates

        // Already seeded by NarrativePlacementGen.PlaceAnomalyChains — skip if chains exist.
        if (state.AnomalyChains.Count > 0) return; // STRUCTURAL: already seeded

        int chainCount = state.Nodes.Count >= AnomalyChainTweaksV0.TwoChainMinNodes
            ? AnomalyChainTweaksV0.LargeGalaxyChainCount
            : AnomalyChainTweaksV0.SmallGalaxyChainCount;
        if (chainCount > AnomalyChainContentV0.AllChains.Count)
            chainCount = AnomalyChainContentV0.AllChains.Count;

        // Find deep-space starter nodes (high instability phase).
        var starterCandidates = new List<string>();
        foreach (var kv in state.Nodes)
        {
            if (kv.Value.InstabilityLevel >= AnomalyChainTweaksV0.MinStarterInstabilityPhase)
                starterCandidates.Add(kv.Key);
        }
        starterCandidates.Sort(StringComparer.Ordinal);

        // Fallback: use any non-player node.
        if (starterCandidates.Count == 0) // STRUCTURAL: empty guard
        {
            foreach (var kv in state.Nodes)
            {
                if (!string.Equals(kv.Key, state.PlayerLocationNodeId, StringComparison.Ordinal))
                    starterCandidates.Add(kv.Key);
            }
            starterCandidates.Sort(StringComparer.Ordinal);
        }

        if (starterCandidates.Count == 0) return; // STRUCTURAL: no valid nodes

        var usedStarters = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < chainCount && i < AnomalyChainContentV0.AllChains.Count; i++)
        {
            var template = AnomalyChainContentV0.AllChains[i];

            // Pick starter node.
            string starterNode = "";
            foreach (var nid in starterCandidates)
            {
                if (!usedStarters.Contains(nid))
                {
                    starterNode = nid;
                    break;
                }
            }
            if (string.IsNullOrEmpty(starterNode)) break;

            usedStarters.Add(starterNode);
            InitiateChain(state, template.ChainId, starterNode);
        }
    }

    private static Dictionary<string, int> ComputeHopDistances(SimState state, string startNode, Scratch scratch)
    {
        var distances = scratch.BfsDistances;
        distances.Clear();
        var queue = scratch.BfsQueue;
        queue.Clear();
        var visited = scratch.BfsVisited;
        visited.Clear();

        queue.Enqueue(startNode);
        visited.Add(startNode);
        distances[startNode] = 0; // STRUCTURAL: start distance

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int currentDist = distances[current];
            if (currentDist >= AnomalyChainTweaksV0.MaxBfsDepth) continue;

            foreach (var edge in state.Edges.Values)
            {
                string neighbor = "";
                if (string.Equals(edge.FromNodeId, current, StringComparison.Ordinal))
                    neighbor = edge.ToNodeId;
                else if (string.Equals(edge.ToNodeId, current, StringComparison.Ordinal))
                    neighbor = edge.FromNodeId;
                if (string.IsNullOrEmpty(neighbor)) continue;

                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    distances[neighbor] = currentDist + 1; // STRUCTURAL: increment
                    queue.Enqueue(neighbor);
                }
            }
        }

        return distances;
    }

    // Type alias for content chain template.
    private sealed class ChainTemplate { } // UNUSED: direct AnomalyChainContentV0.ChainTemplate used instead
}
