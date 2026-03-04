using System;
using System.Collections.Generic;
using System.Linq;
using SimCore.Content;
using SimCore.Entities;

namespace SimCore.Systems;

// GATE.S4.UPGRADE.SYSTEM.001: Refit system — install/remove modules, validate slots.
public static class RefitSystem
{
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
        var moduleDef = UpgradeContentV0.GetById(moduleId);
        if (moduleDef == null)
            return new RefitResult { Success = false, Reason = "unknown_module" };

        // Validate slot kind matches
        if (moduleDef.SlotKind != slot.SlotKind)
            return new RefitResult { Success = false, Reason = "slot_kind_mismatch" };

        // Validate tech prerequisite
        if (!UpgradeContentV0.CanInstall(moduleId, state.Tech.UnlockedTechIds))
            return new RefitResult { Success = false, Reason = "tech_not_unlocked" };

        // Deduct cost
        if (state.PlayerCredits < moduleDef.CreditCost)
            return new RefitResult { Success = false, Reason = "insufficient_credits" };

        state.PlayerCredits -= moduleDef.CreditCost;
        slot.InstalledModuleId = moduleId;

        return new RefitResult { Success = true };
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

    public sealed class ModuleStatBonuses
    {
        public int SpeedBonusPct { get; set; }
        public int ShieldBonusFlat { get; set; }
        public int HullBonusFlat { get; set; }
        public int DamageBonusPct { get; set; }
    }
}
