#nullable enable

using Godot;
using SimCore;
using SimCore.Systems;
using System;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge
{
    // ── GATE.S6.FRACTURE.BRIDGE.001: Fracture access and market queries ──

    /// <summary>
    /// Checks whether a fleet may enter a fracture node.
    /// Returns {allowed (bool), reason (string)}.
    /// Nonblocking read — returns denied with reason on lock failure.
    /// </summary>
    public Godot.Collections.Dictionary GetFractureAccessV0(string fleetId, string nodeId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["allowed"] = false,
            ["reason"] = "",
        };

        TryExecuteSafeRead(state =>
        {
            var check = FractureSystem.FractureAccessCheck(state, fleetId, nodeId);
            result["allowed"] = check.Allowed;
            result["reason"] = check.Reason;
        }, 0);

        return result;
    }

    // GATE.S6.FRACTURE.SENSOR_REVEAL.001: Sensor tech level for void site visibility during transit.
    // Level 0 = no sensors, 1 = sensor_suite, 2 = advanced_sensors.
    public int GetSensorLevelV0()
    {
        int level = 0;
        TryExecuteSafeRead(state =>
        {
            if (state.Tech.UnlockedTechIds.Contains(SimCore.Tweaks.SurveyTweaksV0.SensorSuiteTechId))
                level++;
            if (state.Tech.UnlockedTechIds.Contains(SimCore.Tweaks.SurveyTweaksV0.AdvancedSensorsTechId))
                level++;
        }, 0);
        return level;
    }

    // GATE.S6.FRACTURE.PLAYER_DISPATCH.001: Initiate fracture travel for player fleet.
    // Blocks until the sim thread processes the command so callers can read updated state.
    public void DispatchFractureTravelV0(string fleetId, string voidSiteId)
    {
        int tickBefore = GetSimTickBlocking();
        EnqueueCommand(new SimCore.Commands.FractureTravelCommand(fleetId, voidSiteId));
        WaitForTickAdvance(tickBefore, 200);
    }

    // GATE.S6.FRACTURE.UI_PANEL.001: List available void sites with costs.
    // Returns array of {id, family, marker_state, distance, fuel_cost, hull_stress, trace_risk}.
    public Godot.Collections.Array GetAvailableVoidSitesV0()
    {
        var result = new Godot.Collections.Array();

        TryExecuteSafeRead(state =>
        {
            string playerFleetId = "fleet_trader_1";
            if (!state.Fleets.TryGetValue(playerFleetId, out var fleet)) return;
            if (string.IsNullOrEmpty(fleet.CurrentNodeId)) return;
            if (!state.Nodes.TryGetValue(fleet.CurrentNodeId, out var playerNode)) return;

            var siteIds = new System.Collections.Generic.List<string>(state.VoidSites.Keys);
            siteIds.Sort(StringComparer.Ordinal);

            foreach (var siteId in siteIds)
            {
                var site = state.VoidSites[siteId];
                // Only show discovered or surveyed sites
                if (site.MarkerState == SimCore.Entities.VoidSiteMarkerState.Unknown) continue;

                float dist = System.Numerics.Vector3.Distance(playerNode.Position, site.Position);
                int fuelCost = SimCore.Tweaks.FractureTweaksV0.FractureFuelPerJump;
                int hullStress = SimCore.Tweaks.FractureTweaksV0.FractureHullStressPerJump;
                float traceRisk = SimCore.Tweaks.FractureTweaksV0.FractureTracePerArrival;

                result.Add(new Godot.Collections.Dictionary
                {
                    ["id"] = site.Id,
                    ["family"] = site.Family.ToString(),
                    ["marker_state"] = site.MarkerState.ToString(),
                    ["distance"] = dist,
                    ["fuel_cost"] = fuelCost,
                    ["hull_stress"] = hullStress,
                    ["trace_risk"] = traceRisk,
                    ["can_afford"] = fleet.FuelCurrent >= fuelCost,
                });
            }
        }, 0);

        return result;
    }

    // GATE.S6.FRACTURE_DISCOVERY.BRIDGE.001: Fracture discovery status for UI.
    // Returns {unlocked (bool), discovery_tick (int), derelict_node_id (string), analysis_progress (string)}.
    // Nonblocking read — returns defaults on lock failure.
    public Godot.Collections.Dictionary GetFractureDiscoveryStatusV0()
    {
        var result = new Godot.Collections.Dictionary
        {
            ["unlocked"] = false,
            ["discovery_tick"] = 0,
            ["derelict_node_id"] = "",
            ["analysis_progress"] = "unknown",
        };

        TryExecuteSafeRead(state =>
        {
            result["unlocked"] = state.FractureUnlocked;
            result["discovery_tick"] = state.FractureDiscoveryTick;

            // Find the FractureDerelict VoidSite and report its state.
            var siteIds = new System.Collections.Generic.List<string>(state.VoidSites.Keys);
            siteIds.Sort(StringComparer.Ordinal);

            foreach (var siteId in siteIds)
            {
                if (!state.VoidSites.TryGetValue(siteId, out var site)) continue;
                if (site.Family != SimCore.Entities.VoidSiteFamily.FractureDerelict) continue;

                result["derelict_node_id"] = site.NearStarA;
                result["analysis_progress"] = site.MarkerState.ToString();
                break;
            }
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns fracture-adjusted market pricing for a good at a node.
    /// Returns {mid (int), buy (int), sell (int), volume_cap (int), error (string)}.
    /// Uses current market inventory stock as input to FracturePricingV0.
    /// Nonblocking read — returns error dict if lock unavailable or node/market not found.
    /// </summary>
    public Godot.Collections.Dictionary GetFractureMarketV0(string nodeId, string goodId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["mid"] = 0,
            ["buy"] = 0,
            ["sell"] = 0,
            ["volume_cap"] = 0,
            ["error"] = "",
        };

        TryExecuteSafeRead(state =>
        {
            if (!state.Nodes.TryGetValue(nodeId, out var node))
            {
                result["error"] = $"node not found: {nodeId}";
                return;
            }

            if (string.IsNullOrEmpty(node.MarketId) || !state.Markets.TryGetValue(node.MarketId, out var market))
            {
                result["error"] = $"no market at node: {nodeId}";
                return;
            }

            int stock = market.Inventory.TryGetValue(goodId, out var s) ? s : 0;
            var pricing = FractureSystem.FracturePricingV0(stock);

            result["mid"] = pricing.Mid;
            result["buy"] = pricing.Buy;
            result["sell"] = pricing.Sell;
            result["volume_cap"] = pricing.VolumeCap;
        }, 0);

        return result;
    }
}
