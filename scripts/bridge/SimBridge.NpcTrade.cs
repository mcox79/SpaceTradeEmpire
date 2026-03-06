using Godot;
using Godot.Collections;
using System.Collections.Generic;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge : Node
{
    // GATE.S5.NPC_TRADE.BRIDGE.001: NPC trade bridge queries.

    /// Returns array of dictionaries with active NPC trade routes.
    /// Each dict: { fleet_id, source_node_id, dest_node_id, good_id, qty }
    public Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> GetNpcTradeRoutesV0()
    {
        var routes = new Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>>();
        TryExecuteSafeRead(state =>
        {
            var fleetIds = new List<string>(state.Fleets.Keys);
            fleetIds.Sort(System.StringComparer.Ordinal);
            foreach (var fid in fleetIds)
            {
                var fleet = state.Fleets[fid];
                if (fleet.OwnerId == "player") continue;
                if (fleet.Role != SimCore.Entities.FleetRole.Trader) continue;
                if (string.IsNullOrEmpty(fleet.FinalDestinationNodeId) &&
                    string.IsNullOrEmpty(fleet.DestinationNodeId)) continue;

                var dict = new Godot.Collections.Dictionary<string, Variant>();
                dict["fleet_id"] = fid;
                dict["source_node_id"] = fleet.CurrentNodeId ?? "";
                dict["dest_node_id"] = fleet.FinalDestinationNodeId ?? fleet.DestinationNodeId ?? "";

                // Find primary cargo
                string primaryGood = "";
                int primaryQty = 0;
                foreach (var kv in fleet.Cargo)
                {
                    if (kv.Value > primaryQty)
                    {
                        primaryGood = kv.Key;
                        primaryQty = kv.Value;
                    }
                }
                dict["good_id"] = primaryGood;
                dict["qty"] = primaryQty;
                routes.Add(dict);
            }
        }, 0);
        return routes;
    }

    // GATE.S11.GAME_FEEL.NPC_ROUTE_VIS.001: Patrol fleet routes for galaxy map visualization.
    /// Returns array of dictionaries with active NPC patrol routes.
    /// Each dict: { fleet_id, source_node_id, dest_node_id }
    public Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> GetNpcPatrolRoutesV0()
    {
        var routes = new Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>>();
        TryExecuteSafeRead(state =>
        {
            var fleetIds = new List<string>(state.Fleets.Keys);
            fleetIds.Sort(System.StringComparer.Ordinal);
            foreach (var fid in fleetIds)
            {
                var fleet = state.Fleets[fid];
                if (fleet.OwnerId == "player") continue;
                if (fleet.Role != SimCore.Entities.FleetRole.Patrol) continue;
                if (string.IsNullOrEmpty(fleet.FinalDestinationNodeId) &&
                    string.IsNullOrEmpty(fleet.DestinationNodeId)) continue;

                var dict = new Godot.Collections.Dictionary<string, Variant>();
                dict["fleet_id"] = fid;
                dict["source_node_id"] = fleet.CurrentNodeId ?? "";
                dict["dest_node_id"] = fleet.FinalDestinationNodeId ?? fleet.DestinationNodeId ?? "";
                routes.Add(dict);
            }
        }, 0);
        return routes;
    }

    /// Returns NPC trade volume (total cargo units in NPC fleets) at or destined for nodeId.
    public int GetNpcTradeActivityV0(string nodeId)
    {
        int volume = 0;
        TryExecuteSafeRead(state =>
        {
            foreach (var fleet in state.Fleets.Values)
            {
                if (fleet.OwnerId == "player") continue;
                if (fleet.Role != SimCore.Entities.FleetRole.Trader) continue;
                if (fleet.CurrentNodeId != nodeId &&
                    fleet.DestinationNodeId != nodeId &&
                    fleet.FinalDestinationNodeId != nodeId) continue;

                foreach (var kv in fleet.Cargo)
                    volume += kv.Value;
            }
        }, 0);
        return volume;
    }
}
