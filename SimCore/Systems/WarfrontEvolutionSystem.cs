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

            // GATE.S7.WARFRONT.ATTRITION.001: Fleet attrition at Skirmish+ intensity.
            ApplyFleetAttrition(state, wf);

            // GATE.S7.WARFRONT.OBJECTIVES.001: Process strategic objective capture.
            // GATE.S7.TERRITORY_SHIFT.RECOMPUTE.001: Pass state for territory update on capture.
            ProcessObjectives(state, wf);
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

    // GATE.S7.WARFRONT.ATTRITION.001: Apply fleet strength attrition based on intensity + supply.
    // At Skirmish+, both combatant fleets lose BaseAttritionPerTick * (intensity - 1).
    // If a combatant has no recent supply (no WarSupplyLedger entries), add UnsuppliedAttritionBonus.
    // When fleet strength hits 0, force de-escalation.
    public static void ApplyFleetAttrition(SimState state, WarfrontState wf)
    {
        if (wf is null) return;
        int intensity = (int)wf.Intensity;
        if (intensity < WarfrontTweaksV0.AttritionMinIntensity) return;

        int baseAttrition = WarfrontTweaksV0.BaseAttritionPerTick * (intensity - 1);

        // Check supply status for each combatant.
        bool aSupplied = HasRecentSupply(state, wf.Id, wf.CombatantA);
        bool bSupplied = HasRecentSupply(state, wf.Id, wf.CombatantB);

        int attritionA = baseAttrition + (aSupplied ? 0 : WarfrontTweaksV0.UnsuppliedAttritionBonus);
        int attritionB = baseAttrition + (bSupplied ? 0 : WarfrontTweaksV0.UnsuppliedAttritionBonus);

        wf.FleetStrengthA = Math.Max(0, wf.FleetStrengthA - attritionA);
        wf.FleetStrengthB = Math.Max(0, wf.FleetStrengthB - attritionB);

        // If either fleet is depleted, de-escalate.
        if (wf.FleetStrengthA <= 0 || wf.FleetStrengthB <= 0)
        {
            if (wf.Intensity > WarfrontIntensity.Tension)
                wf.Intensity = WarfrontIntensity.Tension;
        }
    }

    // GATE.S7.WARFRONT.ATTRITION.001: Restore fleet strength from supply deliveries.
    // Called from supply delivery logic to restore strength when goods are delivered.
    public static void RestoreFleetStrength(WarfrontState wf, string factionId, int amount)
    {
        if (wf is null || string.IsNullOrEmpty(factionId)) return;
        int restore = amount * WarfrontTweaksV0.SupplyRestorePerDelivery;

        if (string.Equals(factionId, wf.CombatantA, StringComparison.Ordinal))
            wf.FleetStrengthA = Math.Min(WarfrontTweaksV0.MaxFleetStrength, wf.FleetStrengthA + restore);
        else if (string.Equals(factionId, wf.CombatantB, StringComparison.Ordinal))
            wf.FleetStrengthB = Math.Min(WarfrontTweaksV0.MaxFleetStrength, wf.FleetStrengthB + restore);
    }

    // Check if a faction has any supply delivery for this warfront.
    private static bool HasRecentSupply(SimState state, string warfrontId, string factionId)
    {
        if (state.WarSupplyLedger is null) return false;
        if (!state.WarSupplyLedger.TryGetValue(warfrontId, out var goods)) return false;
        // If any good has been delivered, faction is considered supplied.
        // In practice, checking total delivery > 0 is sufficient.
        foreach (var kv in goods)
        {
            if (kv.Value > 0) return true;
        }
        return false;
    }

    // GATE.S7.WARFRONT.OBJECTIVES.001: Process strategic objective capture + factory regen.
    // Dominant faction (by fleet strength) accumulates DominanceTicks.
    // At CaptureDominanceTicks, objective is captured by that faction.
    // Factory objectives regen fleet strength for the controlling faction each tick.
    // GATE.S7.TERRITORY_SHIFT.RECOMPUTE.001: Updates NodeFactionId on capture.
    public static void ProcessObjectives(SimState state, WarfrontState wf)
    {
        if (wf is null || wf.Objectives is null || wf.Objectives.Count == 0) return;

        string dominant = GetDominantFaction(wf);

        foreach (var obj in wf.Objectives)
        {
            if (!string.IsNullOrEmpty(dominant))
            {
                if (string.Equals(obj.DominantFactionId, dominant, StringComparison.Ordinal))
                {
                    obj.DominanceTicks++;
                }
                else
                {
                    obj.DominantFactionId = dominant;
                    obj.DominanceTicks = 1;
                }

                if (obj.DominanceTicks >= WarfrontTweaksV0.CaptureDominanceTicks &&
                    !string.Equals(obj.ControllingFactionId, dominant, StringComparison.Ordinal))
                {
                    string oldFaction = obj.ControllingFactionId;
                    obj.ControllingFactionId = dominant;

                    // GATE.S7.TERRITORY_SHIFT.RECOMPUTE.001: Update node faction on capture.
                    if (state != null && !string.IsNullOrEmpty(obj.NodeId))
                    {
                        state.NodeFactionId[obj.NodeId] = dominant;

                        // GATE.S7.TERRITORY_SHIFT.REGIME_FLIP.001: Refresh regime for captured node.
                        // Worsening commits instantly via hysteresis, so recompute + commit now.
                        var newRegime = ReputationSystem.ComputeTerritoryRegime(state, obj.NodeId);
                        state.NodeRegimeCommitted[obj.NodeId] = (int)newRegime;
                        // Clear any pending hysteresis proposal — ownership just changed.
                        state.NodeRegimeProposed.Remove(obj.NodeId);
                        state.NodeRegimeProposedSinceTick.Remove(obj.NodeId);
                    }
                }
            }

            // Factory bonus: controlling faction gets fleet regen each tick.
            if (obj.Type == ObjectiveType.Factory && !string.IsNullOrEmpty(obj.ControllingFactionId))
            {
                if (string.Equals(obj.ControllingFactionId, wf.CombatantA, StringComparison.Ordinal))
                    wf.FleetStrengthA = Math.Min(WarfrontTweaksV0.MaxFleetStrength, wf.FleetStrengthA + WarfrontTweaksV0.FactoryRegenPerTick);
                else if (string.Equals(obj.ControllingFactionId, wf.CombatantB, StringComparison.Ordinal))
                    wf.FleetStrengthB = Math.Min(WarfrontTweaksV0.MaxFleetStrength, wf.FleetStrengthB + WarfrontTweaksV0.FactoryRegenPerTick);
            }
        }
    }

    // GATE.S7.WARFRONT.OBJECTIVES.001: Determine which faction is dominant by fleet strength.
    public static string GetDominantFaction(WarfrontState wf)
    {
        if (wf is null) return "";
        if (wf.FleetStrengthA > wf.FleetStrengthB) return wf.CombatantA;
        if (wf.FleetStrengthB > wf.FleetStrengthA) return wf.CombatantB;
        return ""; // Tied — no dominant faction.
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
