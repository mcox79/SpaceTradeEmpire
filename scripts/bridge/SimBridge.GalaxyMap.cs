#nullable enable

using Godot;
using SimCore;
using SimCore.Systems;
using System;

namespace SpaceTradeEmpire.Bridge;

// GATE.S7.GALAXY_MAP_V2.QUERIES.001: Galaxy map overlay query methods.
public partial class SimBridge
{
    // ── GetFactionTerritoryOverlayV0 ──
    /// <summary>
    /// Returns Dictionary: key=system_id (string), value=Dictionary with
    /// "controlling_faction" (string) and "influence_pct" (float 0-1).
    /// Nonblocking read.
    /// </summary>
    public Godot.Collections.Dictionary GetFactionTerritoryOverlayV0()
    {
        var result = new Godot.Collections.Dictionary();

        TryExecuteSafeRead(state =>
        {
            foreach (var kv in state.NodeFactionId)
            {
                var nodeId = kv.Key;
                var factionId = kv.Value;
                if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(factionId))
                    continue;

                // Influence percentage: use reputation as a proxy normalized to 0-1.
                // Rep range [-100, 100] mapped to influence [0.0, 1.0].
                float influencePct = 0.5f;
                if (state.FactionReputation.TryGetValue(factionId, out var rep))
                {
                    influencePct = Math.Clamp((rep + 100f) / 200f, 0f, 1f);
                }

                result[nodeId] = new Godot.Collections.Dictionary
                {
                    ["controlling_faction"] = factionId,
                    ["influence_pct"] = influencePct,
                };
            }
        }, 0);

        return result;
    }

    // ── GetFleetPositionsOverlayV0 ──
    /// <summary>
    /// Returns Dictionary: key=system_id, value=Array of fleet info dicts
    /// (fleet_id, role, faction). Nonblocking read.
    /// </summary>
    public Godot.Collections.Dictionary GetFleetPositionsOverlayV0()
    {
        var result = new Godot.Collections.Dictionary();

        TryExecuteSafeRead(state =>
        {
            foreach (var fleet in state.Fleets.Values)
            {
                if (string.IsNullOrEmpty(fleet.CurrentNodeId))
                    continue;

                var nodeId = fleet.CurrentNodeId;

                Godot.Collections.Array fleetArray;
                if (result.ContainsKey(nodeId))
                {
                    fleetArray = (Godot.Collections.Array)result[nodeId];
                }
                else
                {
                    fleetArray = new Godot.Collections.Array();
                    result[nodeId] = fleetArray;
                }

                fleetArray.Add(new Godot.Collections.Dictionary
                {
                    ["fleet_id"] = fleet.Id ?? "",
                    ["role"] = fleet.Role.ToString(),
                    ["faction"] = fleet.OwnerId ?? "",
                });
            }
        }, 0);

        return result;
    }

    // ── GetHeatOverlayV0 ──
    /// <summary>
    /// Returns Dictionary: key=system_id, value=float heat level (0.0-1.0).
    /// Heat is the max Edge.Heat on edges adjacent to each node, normalized.
    /// Nonblocking read.
    /// </summary>
    public Godot.Collections.Dictionary GetHeatOverlayV0()
    {
        var result = new Godot.Collections.Dictionary();

        TryExecuteSafeRead(state =>
        {
            float heatCap = SimCore.Tweaks.SecurityTweaksV0.ConfiscationHeatThreshold * 2.0f;

            // Accumulate max heat per node from adjacent edges.
            var maxHeatByNode = new System.Collections.Generic.Dictionary<string, float>(StringComparer.Ordinal);

            foreach (var edge in state.Edges.Values)
            {
                float heat = edge.Heat;
                if (heat <= 0f) continue;

                if (!string.IsNullOrEmpty(edge.FromNodeId))
                {
                    if (!maxHeatByNode.TryGetValue(edge.FromNodeId, out var existingFrom) || heat > existingFrom)
                        maxHeatByNode[edge.FromNodeId] = heat;
                }
                if (!string.IsNullOrEmpty(edge.ToNodeId))
                {
                    if (!maxHeatByNode.TryGetValue(edge.ToNodeId, out var existingTo) || heat > existingTo)
                        maxHeatByNode[edge.ToNodeId] = heat;
                }
            }

            foreach (var kv in maxHeatByNode)
            {
                float normalized = heatCap > 0f ? Math.Clamp(kv.Value / heatCap, 0f, 1f) : 0f;
                result[kv.Key] = normalized;
            }
        }, 0);

        return result;
    }

    // ── GetExplorationOverlayV0 ──
    // GATE.S7.GALAXY_MAP_V2.EXPLORATION_OVL.001
    /// <summary>
    /// Returns Dictionary: key=system_id (string), value=status string
    /// ("unvisited" / "visited" / "mapped" / "anomaly").
    /// A node is "anomaly" if it has an active anomaly encounter,
    /// "mapped" if any of its seeded discoveries are in Intel.Discoveries,
    /// "visited" if in PlayerVisitedNodeIds, else "unvisited".
    /// Nonblocking read.
    /// </summary>
    public Godot.Collections.Dictionary GetExplorationOverlayV0()
    {
        var result = new Godot.Collections.Dictionary();

        TryExecuteSafeRead(state =>
        {
            // Collect node IDs that have active anomaly encounters.
            var anomalyNodes = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var enc in state.AnomalyEncounters.Values)
            {
                if (!string.IsNullOrEmpty(enc.NodeId))
                    anomalyNodes.Add(enc.NodeId);
            }

            foreach (var node in state.Nodes.Values)
            {
                var nodeId = node.Id ?? "";
                if (string.IsNullOrEmpty(nodeId)) continue;

                string status;

                // Priority: anomaly > mapped > visited > unvisited.
                if (anomalyNodes.Contains(nodeId))
                {
                    status = "anomaly";
                }
                else
                {
                    // Check "mapped": node has discovered objects (same logic as MapQueries).
                    bool isMapped = false;
                    if (node.SeededDiscoveryIds is not null
                        && node.SeededDiscoveryIds.Count > 0
                        && state.Intel?.Discoveries != null)
                    {
                        for (int i = 0; i < node.SeededDiscoveryIds.Count; i++)
                        {
                            var did = node.SeededDiscoveryIds[i];
                            if (!string.IsNullOrEmpty(did) && state.Intel.Discoveries.ContainsKey(did))
                            {
                                isMapped = true;
                                break;
                            }
                        }
                    }

                    if (isMapped)
                        status = "mapped";
                    else if (state.PlayerVisitedNodeIds.Contains(nodeId))
                        status = "visited";
                    else
                        status = "unvisited";
                }

                result[nodeId] = status;
            }
        }, 0);

        return result;
    }

    // ── GetRoutePathV0 ──
    /// <summary>
    /// Returns Dictionary with "path" (Array of system_ids in order) and
    /// "travel_time" (int ticks). Uses RoutePlanner from the player's current location.
    /// Nonblocking read.
    /// </summary>
    public Godot.Collections.Dictionary GetRoutePathV0(string destNodeId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["path"] = new Godot.Collections.Array(),
            ["travel_time"] = 0,
        };

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(destNodeId)) return;

            string fromNodeId = state.PlayerLocationNodeId ?? "";
            if (string.IsNullOrEmpty(fromNodeId)) return;

            // Get player fleet speed for route calculation.
            float speed = 0.5f; // default
            if (state.Fleets.TryGetValue("fleet_trader_1", out var playerFleet))
            {
                speed = playerFleet.Speed > 0f ? playerFleet.Speed : 0.5f;
            }

            if (RoutePlanner.TryPlan(state, fromNodeId, destNodeId, speed, out var plan))
            {
                var pathArray = new Godot.Collections.Array();
                foreach (var nodeId in plan.NodeIds)
                {
                    pathArray.Add(nodeId);
                }
                result["path"] = pathArray;
                result["travel_time"] = plan.TotalTravelTicks;
            }
        }, 0);

        return result;
    }

    // ── GetDiscoveryPhaseMarkersV0 ──
    // GATE.S6.UI_DISCOVERY.PHASE_MARKERS.001
    /// <summary>
    /// Returns Array of Dictionaries, one per known discovery site:
    ///   "node_id" (string), "discovery_id" (string), "phase" (string: "seen"/"scanned"/"analyzed"),
    ///   "pos_x"/"pos_y"/"pos_z" (float — node galactic position).
    /// Only includes discoveries the player has found (present in Intel.Discoveries).
    /// Nonblocking read.
    /// </summary>
    public Godot.Collections.Array GetDiscoveryPhaseMarkersV0()
    {
        var result = new Godot.Collections.Array();

        TryExecuteSafeRead(state =>
        {
            if (state.Intel?.Discoveries == null) return;

            foreach (var node in state.Nodes.Values)
            {
                if (node.SeededDiscoveryIds is null || node.SeededDiscoveryIds.Count == 0)
                    continue;

                var nodeId = node.Id ?? "";
                if (string.IsNullOrEmpty(nodeId)) continue;

                for (int i = 0; i < node.SeededDiscoveryIds.Count; i++)
                {
                    var did = node.SeededDiscoveryIds[i];
                    if (string.IsNullOrEmpty(did)) continue;

                    if (!state.Intel.Discoveries.TryGetValue(did, out var disc))
                        continue;

                    string phaseStr = disc.Phase switch
                    {
                        SimCore.Entities.DiscoveryPhase.Scanned => "scanned",
                        SimCore.Entities.DiscoveryPhase.Analyzed => "analyzed",
                        _ => "seen",
                    };

                    result.Add(new Godot.Collections.Dictionary
                    {
                        ["node_id"] = nodeId,
                        ["discovery_id"] = disc.DiscoveryId ?? did,
                        ["phase"] = phaseStr,
                        ["pos_x"] = node.Position.X,
                        ["pos_y"] = node.Position.Y,
                        ["pos_z"] = node.Position.Z,
                    });
                }
            }
        }, 0);

        return result;
    }

    // ── GetSystemSearchV0 ──
    /// <summary>
    /// Returns Array of Dictionaries with "system_id" and "name" for systems
    /// matching the query (case-insensitive contains match).
    /// Nonblocking read.
    /// </summary>
    public Godot.Collections.Array GetSystemSearchV0(string query)
    {
        var result = new Godot.Collections.Array();

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(query)) return;

            foreach (var node in state.Nodes.Values)
            {
                string name = node.Name ?? "";
                if (string.IsNullOrEmpty(name)) continue;

                if (name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.Add(new Godot.Collections.Dictionary
                    {
                        ["system_id"] = node.Id ?? "",
                        ["name"] = name,
                    });
                }
            }
        }, 0);

        return result;
    }
}
