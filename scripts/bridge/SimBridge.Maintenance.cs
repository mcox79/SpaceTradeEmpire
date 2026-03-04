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

// GATE.S4.MAINT.BRIDGE.001: SimBridge maintenance queries — health, repair cost, repair intent.
public partial class SimBridge
{
    /// <summary>
    /// Returns maintenance info for all industry sites at a node.
    /// [{site_id, recipe_id, health_pct, efficiency_pct, repair_cost, needs_repair}]
    /// </summary>
    public Godot.Collections.Array GetNodeMaintenanceV0(string nodeId)
    {
        var result = new Godot.Collections.Array();
        if (string.IsNullOrEmpty(nodeId)) return result;

        TryExecuteSafeRead(state =>
        {
            var keys = new List<string>();
            foreach (var kv in state.IndustrySites)
            {
                if (string.Equals(kv.Value.NodeId, nodeId, StringComparison.Ordinal))
                    keys.Add(kv.Key);
            }
            keys.Sort(StringComparer.Ordinal);

            foreach (var key in keys)
            {
                var site = state.IndustrySites[key];
                int repairCost = MaintenanceSystem.GetRepairCost(site);
                result.Add(new Godot.Collections.Dictionary
                {
                    ["site_id"] = site.Id,
                    ["recipe_id"] = site.RecipeId ?? "",
                    ["health_pct"] = site.HealthBps / 100,
                    ["efficiency_pct"] = (int)(site.Efficiency * 100),
                    ["repair_cost"] = repairCost,
                    ["needs_repair"] = site.HealthBps < SimCore.Tweaks.MaintenanceTweaksV0.MaxHealthBps,
                });
            }
        }, 0);

        return result;
    }

    /// <summary>
    /// Repairs a site to full health. Returns {success, reason, credits_cost, bps_restored}.
    /// </summary>
    public Godot.Collections.Dictionary RepairSiteV0(string siteId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["success"] = false,
            ["reason"] = "",
            ["credits_cost"] = 0,
            ["bps_restored"] = 0,
        };
        _stateLock.EnterWriteLock();
        try
        {
            var r = MaintenanceSystem.RepairSite(_kernel.State, siteId);
            result["success"] = r.Success;
            result["reason"] = r.Reason;
            result["credits_cost"] = r.CreditsCost;
            result["bps_restored"] = r.BpsRestored;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
        return result;
    }
}
