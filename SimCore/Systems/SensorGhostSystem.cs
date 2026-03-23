using System;
using System.Collections.Generic;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.T45.DEEP_DREAD.SENSOR_GHOSTS.001: Phantom fleet contacts at Phase 2+ nodes.
// Deterministic: hash(tick, nodeId) decides spawn. Ghosts appear on sensors as real
// fleet contacts, then vanish after 3-8 ticks. The player can't tell if a contact
// is real until it disappears — or doesn't.
public static class SensorGhostSystem
{
    private static readonly string[] GhostTypes = { "trader", "patrol", "unknown" };

    public static void Process(SimState state)
    {
        // Initialize ghost list if needed.
        state.SensorGhosts ??= new List<SensorGhost>();

        // Expire old ghosts.
        for (int i = state.SensorGhosts.Count - 1; i >= 0; i--) // STRUCTURAL: reverse iteration for removal
        {
            if (state.Tick >= state.SensorGhosts[i].ExpiryTick)
                state.SensorGhosts.RemoveAt(i);
        }

        // Only spawn on check interval.
        if (state.Tick % DeepDreadTweaksV0.GhostCheckIntervalTicks != 0) return; // STRUCTURAL: interval guard

        // Check player's current node phase.
        var playerNodeId = state.PlayerLocationNodeId;
        if (string.IsNullOrEmpty(playerNodeId)) return;
        if (!state.Nodes.TryGetValue(playerNodeId, out var node)) return;

        int phase = InstabilityTweaksV0.GetPhaseIndex(node.InstabilityLevel);
        if (phase < DeepDreadTweaksV0.GhostMinPhase) return;

        // Already at max concurrent ghosts?
        if (state.SensorGhosts.Count >= DeepDreadTweaksV0.GhostMaxConcurrent) return;

        // Deterministic spawn roll via FNV hash.
        ulong hash = Fnv1aHash($"ghost_{state.Tick}_{playerNodeId}");
        int roll = (int)(hash % (ulong)DeepDreadTweaksV0.GhostSpawnModulus);
        int threshold = DeepDreadTweaksV0.GhostSpawnBaseThreshold * (phase - 1); // Phase 2=15, 3=30, 4=45
        if (roll >= threshold) return;

        // Spawn a ghost at an adjacent node.
        var adjNodes = new List<string>();
        foreach (var edge in state.Edges.Values)
        {
            string adj = "";
            if (edge.FromNodeId == playerNodeId) adj = edge.ToNodeId;
            else if (edge.ToNodeId == playerNodeId) adj = edge.FromNodeId;
            if (adj.Length > 0) adjNodes.Add(adj); // STRUCTURAL: empty string guard
        }
        if (adjNodes.Count == 0) return; // STRUCTURAL: no neighbors

        adjNodes.Sort(StringComparer.Ordinal);
        string ghostNodeId = adjNodes[(int)(hash / (ulong)DeepDreadTweaksV0.GhostSpawnModulus % (ulong)adjNodes.Count)];

        // Deterministic lifetime.
        int lifetime = DeepDreadTweaksV0.GhostMinLifetimeTicks
            + (int)((hash >> 8) % (ulong)(DeepDreadTweaksV0.GhostMaxLifetimeTicks - DeepDreadTweaksV0.GhostMinLifetimeTicks + 1)); // STRUCTURAL: bit shift for second hash value

        // Deterministic fleet type.
        string ghostType = GhostTypes[(int)((hash >> 16) % (ulong)GhostTypes.Length)]; // STRUCTURAL: third hash value

        state.SensorGhosts.Add(new SensorGhost
        {
            Id = $"ghost_{state.Tick}_{ghostNodeId}",
            NodeId = ghostNodeId,
            ApparentFleetType = ghostType,
            SpawnTick = state.Tick,
            ExpiryTick = state.Tick + lifetime,
        });
    }

    private static ulong Fnv1aHash(string input)
    {
        ulong hash = 14695981039346656037UL;
        foreach (char c in input) { hash ^= (byte)c; hash *= 1099511628211UL; }
        return hash;
    }
}
