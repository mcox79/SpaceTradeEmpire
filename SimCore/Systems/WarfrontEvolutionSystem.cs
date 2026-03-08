using System;
using System.Linq;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S7.WARFRONT.EVOLUTION.001: Warfront state transitions.
// Cold wars escalate after ColdWarEscalateMinTick..ColdWarEscalateMaxTick ticks.
// Hot wars de-escalate (ceasefire) after HotWarCeasefireMinTick..HotWarCeasefireMaxTick ticks.
// Uses deterministic hash of warfront ID + tick for transition decisions.
public static class WarfrontEvolutionSystem
{
    public static void Process(SimState state)
    {
        if (state.Warfronts is null || state.Warfronts.Count == 0) return;

        foreach (var wf in state.Warfronts.Values.OrderBy(w => w.Id, StringComparer.Ordinal))
        {
            int age = state.Tick - wf.TickStarted;

            if (wf.WarType == WarType.Cold)
            {
                ProcessColdWar(wf, age, state.Tick);
            }
            else // Hot
            {
                ProcessHotWar(wf, age, state.Tick);
            }
        }
    }

    // STRUCTURAL: Cold war escalation logic.
    private static void ProcessColdWar(WarfrontState wf, int age, int currentTick)
    {
        if (age < WarfrontTweaksV0.ColdWarEscalateMinTick) return;
        if (age > WarfrontTweaksV0.ColdWarEscalateMaxTick)
        {
            // Past max window — force escalation if still at low intensity.
            if (wf.Intensity < WarfrontIntensity.Skirmish)
                wf.Intensity = WarfrontIntensity.Skirmish;
            return;
        }

        // STRUCTURAL: Deterministic escalation check using hash of ID + tick.
        uint hash = DeterministicHash(wf.Id, currentTick);
        // STRUCTURAL: 5% chance per tick in the escalation window.
        if (hash % 20 == 0) // STRUCTURAL: modulus 20 = 5% probability
        {
            if (wf.Intensity < WarfrontIntensity.TotalWar)
                wf.Intensity = (WarfrontIntensity)((int)wf.Intensity + 1); // STRUCTURAL: +1 intensity step
        }
    }

    // STRUCTURAL: Hot war ceasefire logic.
    private static void ProcessHotWar(WarfrontState wf, int age, int currentTick)
    {
        if (age < WarfrontTweaksV0.HotWarCeasefireMinTick) return;
        if (age > WarfrontTweaksV0.HotWarCeasefireMaxTick)
        {
            // Past max window — force de-escalation.
            if (wf.Intensity > WarfrontIntensity.Tension)
                wf.Intensity = WarfrontIntensity.Tension;
            return;
        }

        // STRUCTURAL: Deterministic de-escalation check.
        uint hash = DeterministicHash(wf.Id, currentTick);
        // STRUCTURAL: 3% chance per tick in the ceasefire window.
        if (hash % 33 == 0) // STRUCTURAL: modulus 33 ≈ 3% probability
        {
            if (wf.Intensity > WarfrontIntensity.Peace)
                wf.Intensity = (WarfrontIntensity)((int)wf.Intensity - 1); // STRUCTURAL: -1 intensity step
        }
    }

    // STRUCTURAL: FNV-1a hash for deterministic pseudo-random decisions.
    private static uint DeterministicHash(string id, int tick)
    {
        // STRUCTURAL: FNV-1a constants (same as GalaxyGenerator).
        uint hash = 2166136261;
        foreach (char c in id)
        {
            hash ^= (uint)c;
            hash *= 16777619;
        }
        hash ^= (uint)tick;
        hash *= 16777619;
        return hash;
    }
}
