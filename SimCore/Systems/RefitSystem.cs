using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Content;
using SimCore.Entities;

namespace SimCore.Systems;

// GATE.S4.UPGRADE.SYSTEM.001: Refit system — install/remove modules, validate slots.
public static class RefitSystem
{
    private sealed class Scratch
    {
        public readonly List<string> FleetIds = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();
    public sealed class RefitResult
    {
        public bool Success { get; set; }
        public string Reason { get; set; } = "";
    }

    public static RefitResult InstallModule(SimState state, string fleetId, int slotIndex, string moduleId)
    {
        if (string.IsNullOrEmpty(fleetId))
            return new RefitResult { Success = false, Reason = "empty_fleet_id" };
        if (string.IsNullOrEmpty(moduleId))
            return new RefitResult { Success = false, Reason = "empty_module_id" };

        if (!state.Fleets.TryGetValue(fleetId, out var fleet))
            return new RefitResult { Success = false, Reason = "fleet_not_found" };

        if (slotIndex < 0 || slotIndex >= fleet.Slots.Count)
            return new RefitResult { Success = false, Reason = "invalid_slot_index" };

        var slot = fleet.Slots[slotIndex];

        // GATE.T65.REFIT.SLOT_COMPAT.001: If the same module is already installed, return success (no-op).
        if (string.Equals(slot.InstalledModuleId, moduleId, StringComparison.Ordinal))
            return new RefitResult { Success = true, Reason = "already_installed" };

        var moduleDef = UpgradeContentV0.GetById(moduleId);
        if (moduleDef == null)
            return new RefitResult { Success = false, Reason = "unknown_module" };

        // Validate slot kind matches
        if (moduleDef.SlotKind != slot.SlotKind)
            return new RefitResult { Success = false, Reason = "slot_kind_mismatch" };

        // GATE.S18.SHIP_MODULES.FITTING_BUDGET.001: Validate power budget.
        int currentDraw = ComputeTotalPowerDraw(fleet);
        // Subtract power of the module being replaced (if any).
        var existingDef = UpgradeContentV0.GetById(slot.InstalledModuleId ?? "");
        if (existingDef != null) currentDraw -= existingDef.PowerDraw;
        int budget = GetPowerBudget(fleet);
        if (budget > 0 && currentDraw + moduleDef.PowerDraw > budget)
            return new RefitResult { Success = false, Reason = "power_exceeded" };

        // Validate tech prerequisite
        if (!UpgradeContentV0.CanInstall(moduleId, state.Tech.UnlockedTechIds))
            return new RefitResult { Success = false, Reason = "tech_not_unlocked" };

        // GATE.S8.T3_MODULES.DISCOVERY_GATE.001: Block station purchase of discovery-only modules.
        if (moduleDef.IsDiscoveryOnly)
            return new RefitResult { Success = false, Reason = "discovery_only" };

        // GATE.S7.T2_MODULES.FITTING.001: Validate faction reputation for T2 modules.
        if (!string.IsNullOrEmpty(moduleDef.FactionId) && moduleDef.FactionRepRequired > 0)
        {
            int rep = 0;
            state.FactionReputation.TryGetValue(moduleDef.FactionId, out rep);
            if (rep < moduleDef.FactionRepRequired)
                return new RefitResult { Success = false, Reason = "faction_rep_insufficient" };
        }

        // GATE.S7.TECH_ACCESS.LOCK.001: Check faction-locked module access.
        var techAccess = Tweaks.TechAccessTweaksV0.CheckAccess(moduleId, state.FactionReputation);
        if (techAccess.IsLocked && !techAccess.HasAccess)
            return new RefitResult { Success = false, Reason = "tech_access_locked" };

        // Deduct cost
        if (state.PlayerCredits < moduleDef.CreditCost)
            return new RefitResult { Success = false, Reason = "insufficient_credits" };

        state.PlayerCredits -= moduleDef.CreditCost;
        slot.InstalledModuleId = moduleId;
        slot.PowerDraw = moduleDef.PowerDraw;

        return new RefitResult { Success = true };
    }

    // GATE.S4.UPGRADE_PIPELINE.TIMED_REFIT.001: Queue a module install with timed delay.
    // Instead of instant install, adds a RefitQueueEntry with TicksRemaining = ModuleDef.InstallTicks.
    // Credits are deducted immediately on queue; the module installs when ticks reach 0.
    public static RefitResult QueueInstall(SimState state, string fleetId, int slotIndex, string moduleId)
    {
        if (string.IsNullOrEmpty(fleetId))
            return new RefitResult { Success = false, Reason = "empty_fleet_id" };
        if (string.IsNullOrEmpty(moduleId))
            return new RefitResult { Success = false, Reason = "empty_module_id" };

        if (!state.Fleets.TryGetValue(fleetId, out var fleet))
            return new RefitResult { Success = false, Reason = "fleet_not_found" };

        if (slotIndex < 0 || slotIndex >= fleet.Slots.Count)
            return new RefitResult { Success = false, Reason = "invalid_slot_index" };

        var slot = fleet.Slots[slotIndex];
        var moduleDef = UpgradeContentV0.GetById(moduleId);
        if (moduleDef == null)
            return new RefitResult { Success = false, Reason = "unknown_module" };

        if (moduleDef.SlotKind != slot.SlotKind)
            return new RefitResult { Success = false, Reason = "slot_kind_mismatch" };

        // GATE.S18.SHIP_MODULES.FITTING_BUDGET.001: Validate power budget.
        int currentDraw = ComputeTotalPowerDraw(fleet);
        var existingDef = UpgradeContentV0.GetById(slot.InstalledModuleId ?? "");
        if (existingDef != null) currentDraw -= existingDef.PowerDraw;
        int budget = GetPowerBudget(fleet);
        if (budget > 0 && currentDraw + moduleDef.PowerDraw > budget)
            return new RefitResult { Success = false, Reason = "power_exceeded" };

        if (!UpgradeContentV0.CanInstall(moduleId, state.Tech.UnlockedTechIds))
            return new RefitResult { Success = false, Reason = "tech_not_unlocked" };

        // GATE.S8.T3_MODULES.DISCOVERY_GATE.001: Block station purchase of discovery-only modules.
        if (moduleDef.IsDiscoveryOnly)
            return new RefitResult { Success = false, Reason = "discovery_only" };

        // GATE.S7.T2_MODULES.FITTING.001: Validate faction reputation for T2 modules.
        if (!string.IsNullOrEmpty(moduleDef.FactionId) && moduleDef.FactionRepRequired > 0)
        {
            int rep = 0;
            state.FactionReputation.TryGetValue(moduleDef.FactionId, out rep);
            if (rep < moduleDef.FactionRepRequired)
                return new RefitResult { Success = false, Reason = "faction_rep_insufficient" };
        }

        // GATE.S7.TECH_ACCESS.LOCK.001: Check faction-locked module access.
        var techAccessQ = Tweaks.TechAccessTweaksV0.CheckAccess(moduleId, state.FactionReputation);
        if (techAccessQ.IsLocked && !techAccessQ.HasAccess)
            return new RefitResult { Success = false, Reason = "tech_access_locked" };

        if (state.PlayerCredits < moduleDef.CreditCost)
            return new RefitResult { Success = false, Reason = "insufficient_credits" };

        state.PlayerCredits -= moduleDef.CreditCost;

        fleet.RefitQueue.Add(new Entities.RefitQueueEntry
        {
            ModuleId = moduleId,
            SlotIndex = slotIndex,
            TicksRemaining = moduleDef.InstallTicks,
        });

        return new RefitResult { Success = true };
    }

    // GATE.S4.UPGRADE_PIPELINE.TIMED_REFIT.001: Process all fleet refit queues each tick.
    // Decrements TicksRemaining; when 0, calls InstallModule (bypassing credit check since
    // credits were already deducted at queue time).
    public static void ProcessRefitQueue(SimState state)
    {
        if (state is null) return;

        // Deterministic fleet order.
        var scratch = s_scratch.GetOrCreateValue(state);
        var fleetIds = scratch.FleetIds;
        fleetIds.Clear();
        foreach (var k in state.Fleets.Keys) fleetIds.Add(k);
        fleetIds.Sort(StringComparer.Ordinal);

        foreach (var fid in fleetIds)
        {
            var fleet = state.Fleets[fid];
            if (fleet.RefitQueue.Count == 0) continue;

            for (int i = fleet.RefitQueue.Count - 1; i >= 0; i--)
            {
                var entry = fleet.RefitQueue[i];
                entry.TicksRemaining--;

                if (entry.TicksRemaining <= 0)
                {
                    // Direct slot assignment (credits already deducted).
                    if (entry.SlotIndex >= 0 && entry.SlotIndex < fleet.Slots.Count)
                    {
                        fleet.Slots[entry.SlotIndex].InstalledModuleId = entry.ModuleId;
                        // GATE.S18.SHIP_MODULES.FITTING_BUDGET.001: Track power draw on slot.
                        var entryDef = UpgradeContentV0.GetById(entry.ModuleId);
                        fleet.Slots[entry.SlotIndex].PowerDraw = entryDef?.PowerDraw ?? 0;
                    }
                    fleet.RefitQueue.RemoveAt(i);
                }
            }
        }
    }

    // GATE.T62.SHIP.MODULE_REASSIGN.001: Transfer compatible modules from source to target fleet.
    // Iterates source slots in order; for each installed module, finds first empty target slot
    // with matching SlotKind that fits within power budget. Modules that don't fit stay on source.
    public static int TransferModules(Fleet source, Fleet target)
    {
        if (source == null || target == null) return 0; // STRUCTURAL: null guard

        int transferred = 0; // STRUCTURAL: counter
        int targetPowerUsed = ComputeTotalPowerDraw(target);
        int targetPowerBudget = GetPowerBudget(target);

        for (int si = 0; si < source.Slots.Count; si++)
        {
            var srcSlot = source.Slots[si];
            if (string.IsNullOrEmpty(srcSlot.InstalledModuleId)) continue;

            var moduleDef = UpgradeContentV0.GetById(srcSlot.InstalledModuleId);
            if (moduleDef == null) continue;

            // Check power budget on target.
            if (targetPowerBudget > 0 && targetPowerUsed + moduleDef.PowerDraw > targetPowerBudget)
                continue;

            // Find first empty target slot with matching SlotKind.
            int targetSlotIdx = -1; // STRUCTURAL: sentinel
            for (int ti = 0; ti < target.Slots.Count; ti++)
            {
                if (string.IsNullOrEmpty(target.Slots[ti].InstalledModuleId)
                    && target.Slots[ti].SlotKind == srcSlot.SlotKind)
                {
                    targetSlotIdx = ti;
                    break;
                }
            }
            if (targetSlotIdx < 0) continue;

            // Transfer: install on target, remove from source.
            target.Slots[targetSlotIdx].InstalledModuleId = srcSlot.InstalledModuleId;
            target.Slots[targetSlotIdx].PowerDraw = moduleDef.PowerDraw;
            srcSlot.InstalledModuleId = null;
            srcSlot.PowerDraw = 0;
            targetPowerUsed += moduleDef.PowerDraw;
            transferred++;
        }

        return transferred;
    }

    public static RefitResult RemoveModule(SimState state, string fleetId, int slotIndex)
    {
        if (!state.Fleets.TryGetValue(fleetId, out var fleet))
            return new RefitResult { Success = false, Reason = "fleet_not_found" };

        if (slotIndex < 0 || slotIndex >= fleet.Slots.Count)
            return new RefitResult { Success = false, Reason = "invalid_slot_index" };

        var slot = fleet.Slots[slotIndex];
        if (string.IsNullOrEmpty(slot.InstalledModuleId))
            return new RefitResult { Success = false, Reason = "slot_empty" };

        slot.InstalledModuleId = null;
        slot.PowerDraw = 0;
        return new RefitResult { Success = true };
    }

    /// <summary>
    /// Computes total stat bonuses from all installed modules on a fleet.
    /// </summary>
    public static ModuleStatBonuses ComputeBonuses(Fleet fleet)
    {
        var bonuses = new ModuleStatBonuses();
        foreach (var slot in fleet.Slots)
        {
            if (string.IsNullOrEmpty(slot.InstalledModuleId)) continue;
            var def = UpgradeContentV0.GetById(slot.InstalledModuleId);
            if (def == null) continue;

            bonuses.SpeedBonusPct += def.SpeedBonusPct;
            bonuses.ShieldBonusFlat += def.ShieldBonusFlat;
            bonuses.HullBonusFlat += def.HullBonusFlat;
            bonuses.DamageBonusPct += def.DamageBonusPct;
        }
        return bonuses;
    }

    // GATE.S18.SHIP_MODULES.FITTING_BUDGET.001: Compute total power draw of installed modules.
    public static int ComputeTotalPowerDraw(Fleet fleet)
    {
        int total = 0;
        foreach (var slot in fleet.Slots)
        {
            if (string.IsNullOrEmpty(slot.InstalledModuleId)) continue;
            var def = UpgradeContentV0.GetById(slot.InstalledModuleId);
            if (def != null) total += def.PowerDraw;
        }
        return total;
    }

    // GATE.S18.SHIP_MODULES.FITTING_BUDGET.001: Get power budget for a fleet's ship class.
    public static int GetPowerBudget(Fleet fleet)
    {
        var classDef = ShipClassContentV0.GetById(fleet.ShipClassId);
        return classDef?.BasePower ?? 0;
    }

    public sealed class ModuleStatBonuses
    {
        public int SpeedBonusPct { get; set; }
        public int ShieldBonusFlat { get; set; }
        public int HullBonusFlat { get; set; }
        public int DamageBonusPct { get; set; }
    }
}
