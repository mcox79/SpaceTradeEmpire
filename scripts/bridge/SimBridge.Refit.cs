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
