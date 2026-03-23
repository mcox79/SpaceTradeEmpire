using System;
using System.Collections.Generic;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.T45.DEEP_DREAD.LATTICE_FAUNA.001: Lattice Fauna lifecycle.
// Spawns at Phase 3+ nodes when player fracture signature is detected.
// Interferes with instruments, drains fuel, increases route uncertainty.
// Avoidable by going dark (cutting fracture drive, staying still for N ticks).
// Leaves residue that attracts more fauna.
public static class LatticeFaunaSystem
{
    public static void Process(SimState state)
    {
        state.LatticeFauna ??= new List<LatticeFauna>();
        state.LatticeFaunaResidue ??= new Dictionary<string, int>(StringComparer.Ordinal);

        // Update existing fauna.
        for (int i = state.LatticeFauna.Count - 1; i >= 0; i--) // STRUCTURAL: reverse for removal
        {
            var fauna = state.LatticeFauna[i];
            switch (fauna.State)
            {
                case LatticeFaunaState.Approaching:
                    if (state.Tick >= fauna.ArrivalTick)
                    {
                        fauna.State = LatticeFaunaState.Present;
                        fauna.NodeId = state.PlayerLocationNodeId;
                    }
                    break;

                case LatticeFaunaState.Present:
                    // Check if player is going dark (not moving, no fracture drive).
                    bool isPlayerDark = IsPlayerDark(state);
                    if (isPlayerDark)
                    {
                        fauna.DarkTicksAccumulated++;
                        if (fauna.DarkTicksAccumulated >= LatticeFaunaTweaksV0.GoDarkTicks)
                        {
                            fauna.State = LatticeFaunaState.Departing;
                        }
                    }
                    else
                    {
                        fauna.DarkTicksAccumulated = 0; // STRUCTURAL: reset on activity

                        // Apply interference effects while present.
                        ApplyInterference(state);
                    }
                    break;

                case LatticeFaunaState.Departing:
                    // Leave residue and remove.
                    state.LatticeFaunaResidue[fauna.NodeId] = state.Tick + LatticeFaunaTweaksV0.ResidueDurationTicks;
                    state.LatticeFauna.RemoveAt(i);
                    break;
            }
        }

        // Prune expired residue.
        var expiredResidueKeys = new List<string>();
        foreach (var kv in state.LatticeFaunaResidue)
        {
            if (state.Tick > kv.Value) expiredResidueKeys.Add(kv.Key);
        }
        foreach (var key in expiredResidueKeys)
            state.LatticeFaunaResidue.Remove(key);

        // Spawn new fauna?
        if (state.Tick % LatticeFaunaTweaksV0.CheckIntervalTicks != 0) return; // STRUCTURAL: interval
        if (state.LatticeFauna.Count >= LatticeFaunaTweaksV0.MaxConcurrent) return;

        var playerNodeId = state.PlayerLocationNodeId;
        if (string.IsNullOrEmpty(playerNodeId)) return;
        if (!state.Nodes.TryGetValue(playerNodeId, out var node)) return;

        int phase = InstabilityTweaksV0.GetPhaseIndex(node.InstabilityLevel);
        if (phase < LatticeFaunaTweaksV0.SpawnMinPhase) return;

        // Check fracture signature (player must have used fracture drive recently).
        if (!HasFractureSignature(state)) return;

        // Residue at current node attracts more fauna (lower spawn check).
        bool hasResidue = state.LatticeFaunaResidue.ContainsKey(playerNodeId);

        // Deterministic spawn roll.
        ulong hash = Fnv1aHash($"fauna_{state.Tick}_{playerNodeId}");
        int roll = (int)(hash % 100UL); // STRUCTURAL: modulus 100
        int threshold = hasResidue ? 40 : 20; // STRUCTURAL: residue doubles spawn chance
        if (roll >= threshold) return;

        // Spawn approaching fauna.
        state.LatticeFauna.Add(new LatticeFauna
        {
            Id = $"fauna_{state.Tick}",
            NodeId = playerNodeId,
            State = LatticeFaunaState.Approaching,
            SpawnTick = state.Tick,
            ArrivalTick = state.Tick + LatticeFaunaTweaksV0.ArrivalDelayTicks,
        });
    }

    /// <summary>
    /// Returns true if player has fracture signature (used fracture drive recently).
    /// </summary>
    private static bool HasFractureSignature(SimState state)
    {
        // If player has fracture unlocked and has done fracture jumps, signature is present.
        // Signature decays after SignatureDecayTicks of no fracture activity.
        if (!state.FractureUnlocked) return false;
        // Use FractureExposureJumps > 0 as a proxy for recent fracture activity.
        return state.FractureExposureJumps > 0;
    }

    /// <summary>
    /// Returns true if player is "going dark" — not moving, not in transit.
    /// </summary>
    private static bool IsPlayerDark(SimState state)
    {
        // Player is dark if not currently traveling (no selected destination).
        return string.IsNullOrEmpty(state.PlayerSelectedDestinationNodeId);
    }

    /// <summary>
    /// Apply fauna interference: fuel drain per tick.
    /// </summary>
    private static void ApplyInterference(SimState state)
    {
        foreach (var fleet in state.Fleets.Values)
        {
            if (fleet.OwnerId != "player") continue;
            fleet.FuelCurrent = Math.Max(0, fleet.FuelCurrent - LatticeFaunaTweaksV0.FuelDrainPerTick); // STRUCTURAL: floor
            break;
        }
    }

    private static ulong Fnv1aHash(string input)
    {
        ulong hash = 14695981039346656037UL;
        foreach (char c in input) { hash ^= (byte)c; hash *= 1099511628211UL; }
        return hash;
    }
}
