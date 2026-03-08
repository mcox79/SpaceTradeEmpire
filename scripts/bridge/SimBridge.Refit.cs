#nullable enable

using Godot;
using SimCore;
using SimCore.Content;
using SimCore.Gen;
using SimCore.Commands;
using SimCore.Intents;
using SimCore.Systems;
using SimCore.Programs;
using SimCore.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace SpaceTradeEmpire.Bridge;

// GATE.S4.UPGRADE.BRIDGE.001: SimBridge.Refit partial — module queries + install/remove intents.
public partial class SimBridge
{
    private Godot.Collections.Array _cachedAvailableModulesV0 = new Godot.Collections.Array();
    private Godot.Collections.Array _cachedPlayerFleetSlotsV0 = new Godot.Collections.Array();

    /// <summary>
    /// Returns player fleet slot layout: [{slot_kind, installed_module_id}]
    /// </summary>
    public Godot.Collections.Array GetPlayerFleetSlotsV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            if (state.Fleets.TryGetValue("fleet_trader_1", out var fleet))
            {
                foreach (var slot in fleet.Slots)
                {
                    arr.Add(new Godot.Collections.Dictionary
                    {
                        ["slot_kind"] = slot.SlotKind.ToString(),
                        ["installed_module_id"] = slot.InstalledModuleId ?? "",
                    });
                }
            }
            lock (_snapshotLock) { _cachedPlayerFleetSlotsV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedPlayerFleetSlotsV0; }
    }

    /// <summary>
    /// Returns modules available for installation (filtered by tech).
    /// [{module_id, display_name, slot_kind, credit_cost, can_install, speed_bonus_pct, shield_bonus_flat, hull_bonus_flat, damage_bonus_pct}]
    /// </summary>
    public Godot.Collections.Array GetAvailableModulesV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            foreach (var mod in UpgradeContentV0.AllModules)
            {
                arr.Add(new Godot.Collections.Dictionary
                {
                    ["module_id"] = mod.ModuleId,
                    ["display_name"] = mod.DisplayName,
                    ["slot_kind"] = mod.SlotKind.ToString(),
                    ["credit_cost"] = mod.CreditCost,
                    ["can_install"] = UpgradeContentV0.CanInstall(mod.ModuleId, state.Tech.UnlockedTechIds),
                    ["speed_bonus_pct"] = mod.SpeedBonusPct,
                    ["shield_bonus_flat"] = mod.ShieldBonusFlat,
                    ["hull_bonus_flat"] = mod.HullBonusFlat,
                    ["damage_bonus_pct"] = mod.DamageBonusPct,
                });
            }
            lock (_snapshotLock) { _cachedAvailableModulesV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedAvailableModulesV0; }
    }

    // GATE.S4.UPGRADE_PIPELINE.BRIDGE_QUEUE.001
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetRefitQueueV0(string fleetId)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue(fleetId, out var fleet)) return;
            foreach (var entry in fleet.RefitQueue)
            {
                var row = new Godot.Collections.Dictionary();
                row["module_id"] = entry.ModuleId;
                row["slot_index"] = entry.SlotIndex;
                row["ticks_remaining"] = entry.TicksRemaining;
                var moduleDef = UpgradeContentV0.GetById(entry.ModuleId);
                row["display_name"] = moduleDef?.DisplayName ?? entry.ModuleId;
                result.Add(row);
            }
        });
        return result;
    }

    public Godot.Collections.Dictionary GetRefitProgressV0(string fleetId)
    {
        var d = new Godot.Collections.Dictionary { ["queue_size"] = 0 };
        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue(fleetId, out var fleet)) return;
            d["queue_size"] = fleet.RefitQueue.Count;
            if (fleet.RefitQueue.Count > 0)
            {
                var first = fleet.RefitQueue[0];
                var moduleDef = UpgradeContentV0.GetById(first.ModuleId);
                d["current_module"] = moduleDef?.DisplayName ?? first.ModuleId;
                d["ticks_remaining"] = first.TicksRemaining;
                d["install_ticks"] = moduleDef?.InstallTicks ?? 5;
            }
        });
        return d;
    }

    // GATE.S4.UI_INDU.WHY_BLOCKED.001
    public string GetRefitBlockReasonV0(string fleetId, string moduleId)
    {
        var moduleDef = UpgradeContentV0.GetById(moduleId);
        if (moduleDef == null) return "unknown_module";
        string reason = "";
        TryExecuteSafeRead(state =>
        {
            if (!UpgradeContentV0.CanInstall(moduleId, state.Tech.UnlockedTechIds))
            {
                reason = "missing_tech"; return;
            }
            if (!state.Fleets.ContainsKey(fleetId)) { reason = "unknown_fleet"; return; }
            if (state.PlayerCredits < moduleDef.CreditCost) { reason = "insufficient_credits:" + moduleDef.CreditCost; return; }
        });
        return reason;
    }

    // GATE.S18.EMPIRE_DASH.SHIP_TAB.001: Ship fitting summary for dock UI.
    // Returns {ship_class, power_used, power_max, slot_count, slots_filled,
    //          zone_fore, zone_fore_max, zone_port, zone_port_max,
    //          zone_stbd, zone_stbd_max, zone_aft, zone_aft_max,
    //          hull, hull_max, shield, shield_max}
    public Godot.Collections.Dictionary GetPlayerShipFittingV0()
    {
        var result = new Godot.Collections.Dictionary();
        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue("fleet_trader_1", out var fleet)) return;
            var classDef = SimCore.Content.ShipClassContentV0.GetById(fleet.ShipClassId);
            result["ship_class"] = classDef?.DisplayName ?? fleet.ShipClassId;
            result["ship_class_id"] = fleet.ShipClassId;
            result["power_used"] = SimCore.Systems.RefitSystem.ComputeTotalPowerDraw(fleet);
            result["power_max"] = SimCore.Systems.RefitSystem.GetPowerBudget(fleet);
            result["slot_count"] = fleet.Slots.Count;
            int filled = 0;
            foreach (var s in fleet.Slots) { if (!string.IsNullOrEmpty(s.InstalledModuleId)) filled++; }
            result["slots_filled"] = filled;
            result["zone_fore"] = fleet.ZoneArmorHp[(int)SimCore.Entities.ZoneFacing.Fore];
            result["zone_fore_max"] = fleet.ZoneArmorHpMax[(int)SimCore.Entities.ZoneFacing.Fore];
            result["zone_port"] = fleet.ZoneArmorHp[(int)SimCore.Entities.ZoneFacing.Port];
            result["zone_port_max"] = fleet.ZoneArmorHpMax[(int)SimCore.Entities.ZoneFacing.Port];
            result["zone_stbd"] = fleet.ZoneArmorHp[(int)SimCore.Entities.ZoneFacing.Starboard];
            result["zone_stbd_max"] = fleet.ZoneArmorHpMax[(int)SimCore.Entities.ZoneFacing.Starboard];
            result["zone_aft"] = fleet.ZoneArmorHp[(int)SimCore.Entities.ZoneFacing.Aft];
            result["zone_aft_max"] = fleet.ZoneArmorHpMax[(int)SimCore.Entities.ZoneFacing.Aft];
            result["hull"] = fleet.HullHp;
            result["hull_max"] = fleet.HullHpMax;
            result["shield"] = fleet.ShieldHp;
            result["shield_max"] = fleet.ShieldHpMax;
        }, 0);
        return result;
    }

    /// <summary>
    /// Installs a module into a fleet slot. Returns {success, reason}.
    /// </summary>
    public Godot.Collections.Dictionary InstallModuleV0(string fleetId, int slotIndex, string moduleId)
    {
        var result = new Godot.Collections.Dictionary { ["success"] = false, ["reason"] = "" };
        _stateLock.EnterWriteLock();
        try
        {
            var r = RefitSystem.InstallModule(_kernel.State, fleetId, slotIndex, moduleId);
            result["success"] = r.Success;
            result["reason"] = r.Reason;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
        return result;
    }

    /// <summary>
    /// Removes a module from a fleet slot. Returns {success, reason}.
    /// </summary>
    public Godot.Collections.Dictionary RemoveModuleV0(string fleetId, int slotIndex)
    {
        var result = new Godot.Collections.Dictionary { ["success"] = false, ["reason"] = "" };
        _stateLock.EnterWriteLock();
        try
        {
            var r = RefitSystem.RemoveModule(_kernel.State, fleetId, slotIndex);
            result["success"] = r.Success;
            result["reason"] = r.Reason;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
        return result;
    }
}
