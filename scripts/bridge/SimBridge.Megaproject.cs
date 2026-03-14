using Godot;
using System;
using System.Linq;
using SimCore.Systems;
using SimCore.Content;

namespace SpaceTradeEmpire.Bridge;

// GATE.S8.MEGAPROJECT.BRIDGE.001: Bridge queries + commands for megaprojects.
public partial class SimBridge
{
    /// <summary>
    /// Get all megaprojects as an array of dictionaries.
    /// Each: {id, type_id, node_id, stage, max_stages, progress_ticks, ticks_per_stage, completed, mutation_applied}.
    /// </summary>
    public Godot.Collections.Array GetMegaprojectsV0()
    {
        var result = new Godot.Collections.Array();
        TryExecuteSafeRead(state =>
        {
            foreach (var kv in state.Megaprojects.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var mp = kv.Value;
                var def = MegaprojectContentV0.GetByTypeId(mp.TypeId);
                var dict = new Godot.Collections.Dictionary
                {
                    ["id"] = mp.Id,
                    ["type_id"] = mp.TypeId,
                    ["name"] = def?.Name ?? mp.TypeId,
                    ["node_id"] = mp.NodeId,
                    ["stage"] = mp.Stage,
                    ["max_stages"] = mp.MaxStages,
                    ["progress_ticks"] = mp.ProgressTicks,
                    ["ticks_per_stage"] = def?.TicksPerStage ?? 0,
                    ["completed"] = mp.IsComplete,
                    ["mutation_applied"] = mp.MutationApplied,
                };
                result.Add(dict);
            }
        }, 0);
        return result;
    }

    /// <summary>
    /// Get detailed megaproject info including per-stage supply requirements and delivery status.
    /// </summary>
    public Godot.Collections.Dictionary GetMegaprojectDetailV0(string megaprojectId)
    {
        var result = new Godot.Collections.Dictionary();
        TryExecuteSafeRead(state =>
        {
            if (!state.Megaprojects.TryGetValue(megaprojectId, out var mp)) return;
            var def = MegaprojectContentV0.GetByTypeId(mp.TypeId);
            if (def == null) return;

            result["id"] = mp.Id;
            result["type_id"] = mp.TypeId;
            result["name"] = def.Name;
            result["description"] = def.Description;
            result["node_id"] = mp.NodeId;
            result["stage"] = mp.Stage;
            result["max_stages"] = mp.MaxStages;
            result["progress_ticks"] = mp.ProgressTicks;
            result["ticks_per_stage"] = def.TicksPerStage;
            result["completed"] = mp.IsComplete;
            result["completed_tick"] = mp.CompletedTick;
            result["credit_cost"] = def.CreditCost;

            // Supply requirements per stage.
            var supply = new Godot.Collections.Array();
            foreach (var req in def.SupplyPerStage.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                int delivered = mp.SupplyDelivered.TryGetValue(req.Key, out var d) ? d : 0;
                supply.Add(new Godot.Collections.Dictionary
                {
                    ["good_id"] = req.Key,
                    ["required"] = req.Value,
                    ["delivered"] = delivered,
                });
            }
            result["supply"] = supply;

            // Is stage fully supplied?
            result["stage_supplied"] = MegaprojectSystem.IsStageSupplied(mp, def);
        }, 0);
        return result;
    }

    /// <summary>
    /// Get available megaproject types as an array of {type_id, name, description, stages, credit_cost, supply}.
    /// </summary>
    public Godot.Collections.Array GetMegaprojectTypesV0()
    {
        var result = new Godot.Collections.Array();
        foreach (var def in MegaprojectContentV0.All)
        {
            var supply = new Godot.Collections.Array();
            foreach (var req in def.SupplyPerStage.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                supply.Add(new Godot.Collections.Dictionary
                {
                    ["good_id"] = req.Key,
                    ["quantity"] = req.Value,
                });
            }
            result.Add(new Godot.Collections.Dictionary
            {
                ["type_id"] = def.TypeId,
                ["name"] = def.Name,
                ["description"] = def.Description,
                ["stages"] = def.Stages,
                ["credit_cost"] = def.CreditCost,
                ["min_faction_rep"] = def.MinFactionRep,
                ["supply"] = supply,
            });
        }
        return result;
    }

    /// <summary>
    /// Start a megaproject at the given node. Returns {success, reason, megaproject_id}.
    /// </summary>
    public Godot.Collections.Dictionary StartMegaprojectV0(string typeId, string nodeId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["success"] = false,
            ["reason"] = "",
            ["megaproject_id"] = "",
        };
        _stateLock.EnterWriteLock();
        try
        {
            var r = MegaprojectSystem.StartMegaproject(_kernel.State, typeId, nodeId, "fleet_trader_1");
            result["success"] = r.Success;
            result["reason"] = r.Reason;
            result["megaproject_id"] = r.MegaprojectId;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
        return result;
    }

    /// <summary>
    /// Deliver goods from player cargo to a megaproject.
    /// </summary>
    public bool DeliverMegaprojectSupplyV0(string megaprojectId, string goodId, int quantity)
    {
        bool success = false;
        _stateLock.EnterWriteLock();
        try
        {
            success = MegaprojectSystem.DeliverSupply(_kernel.State, megaprojectId, goodId, quantity);
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
        return success;
    }
}
