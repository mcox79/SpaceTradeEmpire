#nullable enable

using Godot;
using SimCore;
using System;
using System.Collections.Generic;

namespace SpaceTradeEmpire.Bridge;

// GATE.S4.NPC_INDU.BRIDGE.001: SimBridge NPC industry queries.
public partial class SimBridge
{
    /// <summary>
    /// Returns NPC industry status for all sites at a node.
    /// [{site_id, node_id, active, inputs, outputs}]
    /// </summary>
    public Godot.Collections.Array GetNodeIndustryStatusV0(string nodeId)
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
                var inputArr = new Godot.Collections.Array();
                if (site.Inputs != null)
                {
                    foreach (var inp in site.Inputs)
                    {
                        inputArr.Add(new Godot.Collections.Dictionary
                        {
                            ["good_id"] = inp.Key,
                            ["rate"] = inp.Value,
                        });
                    }
                }

                var outputArr = new Godot.Collections.Array();
                if (site.Outputs != null)
                {
                    foreach (var outp in site.Outputs)
                    {
                        outputArr.Add(new Godot.Collections.Dictionary
                        {
                            ["good_id"] = outp.Key,
                            ["rate"] = outp.Value,
                        });
                    }
                }

                result.Add(new Godot.Collections.Dictionary
                {
                    ["site_id"] = site.Id,
                    ["node_id"] = site.NodeId ?? "",
                    ["active"] = site.Active,
                    ["inputs"] = inputArr,
                    ["outputs"] = outputArr,
                });
            }
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns NPC demand pressure for goods at a node (current stock vs demand).
    /// [{good_id, current_stock, demand_level}]
    /// </summary>
    public Godot.Collections.Array GetNpcDemandV0(string nodeId)
    {
        var result = new Godot.Collections.Array();
        if (string.IsNullOrEmpty(nodeId)) return result;

        TryExecuteSafeRead(state =>
        {
            if (!state.Markets.TryGetValue(nodeId, out var market)) return;

            // Collect all goods demanded by NPC sites at this node
            var demandGoods = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var kv in state.IndustrySites)
            {
                if (!string.Equals(kv.Value.NodeId, nodeId, StringComparison.Ordinal)) continue;
                if (!kv.Value.Active || kv.Value.Inputs == null) continue;
                foreach (var inp in kv.Value.Inputs)
                {
                    if (!demandGoods.ContainsKey(inp.Key))
                        demandGoods[inp.Key] = 0;
                    demandGoods[inp.Key] += inp.Value;
                }
            }

            var sortedGoods = new List<string>(demandGoods.Keys);
            sortedGoods.Sort(StringComparer.Ordinal);

            foreach (var goodId in sortedGoods)
            {
                int stock = market.Inventory.TryGetValue(goodId, out var qty) ? qty : 0;
                result.Add(new Godot.Collections.Dictionary
                {
                    ["good_id"] = goodId,
                    ["current_stock"] = stock,
                    ["demand_level"] = demandGoods[goodId],
                });
            }
        }, 0);

        return result;
    }
}
