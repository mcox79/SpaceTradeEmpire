using Godot;
using System.Collections.Generic;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge : Node
{
    // GATE.S5.SEC_LANES.BRIDGE.001: Security lane bridge queries.

    /// Returns security level BPS for the edge between fromNodeId and toNodeId.
    /// Returns SecurityTweaksV0.DefaultSecurityBps (5000) if edge not found.
    public int GetLaneSecurityV0(string fromNodeId, string toNodeId)
    {
        int result = SimCore.Tweaks.SecurityTweaksV0.DefaultSecurityBps;
        TryExecuteSafeRead(state =>
        {
            foreach (var edge in state.Edges.Values)
            {
                if ((edge.FromNodeId == fromNodeId && edge.ToNodeId == toNodeId) ||
                    (edge.FromNodeId == toNodeId && edge.ToNodeId == fromNodeId))
                {
                    result = edge.SecurityLevelBps;
                    return;
                }
            }
        }, 0);
        return result;
    }

    // GATE.S5.SEC_LANES.UI.001: Security band queries for UI display.

    /// Returns the security band string for the edge between two nodes.
    /// Values: "hostile", "dangerous", "moderate", "safe".
    public string GetSecurityBandV0(string fromNodeId, string toNodeId)
    {
        int bps = GetLaneSecurityV0(fromNodeId, toNodeId);
        return SimCore.Systems.SecurityLaneSystem.GetSecurityBand(bps);
    }

    /// Returns the security band string for a node (based on average adjacent edge security).
    public string GetNodeSecurityBandV0(string nodeId)
    {
        int bps = GetNodeSecurityV0(nodeId);
        return SimCore.Systems.SecurityLaneSystem.GetSecurityBand(bps);
    }

    // GATE.S7.ENFORCEMENT.BRIDGE.001: Edge heat query for UI display.

    /// Returns heat info for the edge between two nodes:
    /// {edge_id, heat (float), threshold_name (string), decay_rate (float)}.
    public Godot.Collections.Dictionary GetEdgeHeatV0(string fromNodeId, string toNodeId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["edge_id"] = "",
            ["heat"] = 0.0f,
            ["threshold_name"] = "safe",
            ["decay_rate"] = SimCore.Tweaks.SecurityTweaksV0.HeatDecayPerTick,
            ["confiscation_threshold"] = SimCore.Tweaks.SecurityTweaksV0.ConfiscationHeatThreshold,
        };

        TryExecuteSafeRead(state =>
        {
            foreach (var edge in state.Edges.Values)
            {
                if ((edge.FromNodeId == fromNodeId && edge.ToNodeId == toNodeId) ||
                    (edge.FromNodeId == toNodeId && edge.ToNodeId == fromNodeId))
                {
                    result["edge_id"] = edge.Id;
                    result["heat"] = edge.Heat;
                    if (edge.Heat >= SimCore.Tweaks.SecurityTweaksV0.ConfiscationHeatThreshold)
                        result["threshold_name"] = "confiscation";
                    else if (edge.Heat > 1.0f)
                        result["threshold_name"] = "elevated";
                    else if (edge.Heat > 0.5f)
                        result["threshold_name"] = "warm";
                    else
                        result["threshold_name"] = "safe";
                    return;
                }
            }
        }, 0);

        return result;
    }

    // GATE.S7.ENFORCEMENT.BRIDGE.001: Recent confiscation event history.

    /// Returns the most recent confiscation events (up to 10).
    /// Each entry: {tick, edge_id, good_id, units, fine_credits, cause}.
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetConfiscationHistoryV0()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        TryExecuteSafeRead(state =>
        {
            if (state.SecurityEventLog == null) return;
            int count = 0;
            // Iterate backwards for most recent first.
            for (int i = state.SecurityEventLog.Count - 1; i >= 0 && count < 10; i--)
            {
                var e = state.SecurityEventLog[i];
                if (e.Type != SimCore.Events.SecurityEvents.SecurityEventType.Confiscation) continue;
                var entry = new Godot.Collections.Dictionary
                {
                    ["tick"] = e.Tick,
                    ["edge_id"] = e.EdgeId,
                    ["good_id"] = e.ConfiscatedGoodId,
                    ["units"] = e.ConfiscatedUnits,
                    ["fine_credits"] = e.FineCredits,
                    ["cause"] = e.CauseChain,
                };
                result.Add(entry);
                count++;
            }
        }, 0);

        return result;
    }

    /// Returns average security BPS of all edges adjacent to nodeId.
    public int GetNodeSecurityV0(string nodeId)
    {
        int result = SimCore.Tweaks.SecurityTweaksV0.DefaultSecurityBps;
        TryExecuteSafeRead(state =>
        {
            int total = 0;
            int count = 0;
            foreach (var edge in state.Edges.Values)
            {
                if (edge.FromNodeId == nodeId || edge.ToNodeId == nodeId)
                {
                    total += edge.SecurityLevelBps;
                    count++;
                }
            }
            if (count > 0)
                result = total / count;
        }, 0);
        return result;
    }

    // GATE.T61.SECURITY.THREAT_MAP.001: Threat overlay data for galaxy map.
    // Returns Dictionary: key=nodeId, value=Dictionary{threat (0-1), security_band, heat}.
    // Threat = weighted combo of inverse security + edge heat.
    public Godot.Collections.Dictionary GetThreatOverlayV0()
    {
        var result = new Godot.Collections.Dictionary();

        TryExecuteSafeRead(state =>
        {
            // Accumulate max heat and min security per node.
            var nodeData = new System.Collections.Generic.Dictionary<string, (int minSecBps, float maxHeat)>(System.StringComparer.Ordinal);

            foreach (var edge in state.Edges.Values)
            {
                foreach (var nid in new[] { edge.FromNodeId, edge.ToNodeId })
                {
                    if (string.IsNullOrEmpty(nid)) continue;
                    if (!nodeData.TryGetValue(nid, out var cur))
                        cur = (edge.SecurityLevelBps, edge.Heat);
                    else
                        cur = (System.Math.Min(cur.minSecBps, edge.SecurityLevelBps),
                               System.Math.Max(cur.maxHeat, edge.Heat));
                    nodeData[nid] = cur;
                }
            }

            float heatCap = SimCore.Tweaks.SecurityTweaksV0.ConfiscationHeatThreshold * 2.0f;

            foreach (var kv in nodeData)
            {
                // Security threat: 0 (10000 bps = perfectly safe) -> 1 (0 bps = hostile).
                float secThreat = 1.0f - System.Math.Clamp(kv.Value.minSecBps / 10000.0f, 0f, 1f);
                // Heat threat: 0..1.
                float heatThreat = heatCap > 0f ? System.Math.Clamp(kv.Value.maxHeat / heatCap, 0f, 1f) : 0f;
                // Combined: weighted 60% security, 40% heat.
                float threat = System.Math.Clamp(secThreat * 0.6f + heatThreat * 0.4f, 0f, 1f);

                var band = SimCore.Systems.SecurityLaneSystem.GetSecurityBand(kv.Value.minSecBps);

                var entry = new Godot.Collections.Dictionary
                {
                    ["threat"] = threat,
                    ["security_band"] = band,
                    ["heat"] = kv.Value.maxHeat,
                };
                result[kv.Key] = entry;
            }
        }, 0);

        return result;
    }

    // GATE.T61.SECURITY.INCIDENT_LOG.001: Incident timeline — last 20 security events.
    // Returns Array of Dictionaries: {tick, type, edge_id, node_id, good_id, units, fine_credits, cause, cargo_impact, credit_impact}.
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetIncidentLogV0()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        TryExecuteSafeRead(state =>
        {
            if (state.SecurityEventLog == null) return;
            int count = 0;
            for (int i = state.SecurityEventLog.Count - 1; i >= 0 && count < 20; i--)
            {
                var e = state.SecurityEventLog[i];
                var entry = new Godot.Collections.Dictionary
                {
                    ["tick"] = e.Tick,
                    ["type"] = e.Type.ToString(),
                    ["edge_id"] = e.EdgeId ?? "",
                    ["good_id"] = e.ConfiscatedGoodId ?? "",
                    ["units"] = e.ConfiscatedUnits,
                    ["fine_credits"] = e.FineCredits,
                    ["cause"] = e.CauseChain ?? "",
                };
                result.Add(entry);
                count++;
            }
        }, 0);

        return result;
    }

    // GATE.T61.SECURITY.INCIDENT_LOG.001: Count of unread incidents since last check.
    // Returns number of incidents with tick > lastReadTick.
    public int GetUnreadIncidentCountV0(int lastReadTick)
    {
        int count = 0;
        TryExecuteSafeRead(state =>
        {
            if (state.SecurityEventLog == null) return;
            for (int i = state.SecurityEventLog.Count - 1; i >= 0; i--)
            {
                if (state.SecurityEventLog[i].Tick <= lastReadTick) break;
                count++;
            }
        }, 0);
        return count;
    }

    // GATE.T61.SECURITY.EXPLAIN_LOSS.001: Loss explanation chain.
    // Returns detailed explanation for a specific incident: root cause, factors, countermeasures.
    public Godot.Collections.Dictionary ExplainIncidentV0(int incidentTick)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["root_cause"] = "",
            ["factors"] = new Godot.Collections.Array<string>(),
            ["countermeasures"] = new Godot.Collections.Array<string>(),
            ["fo_commentary"] = "",
        };

        TryExecuteSafeRead(state =>
        {
            if (state.SecurityEventLog == null) return;
            SimCore.Events.SecurityEvents.Event? found = null;
            foreach (var e in state.SecurityEventLog)
            {
                if (e.Tick == incidentTick) { found = e; break; }
            }
            if (found is null) return;

            var factors = new Godot.Collections.Array<string>();
            var countermeasures = new Godot.Collections.Array<string>();
            string rootCause = found.Type.ToString();

            switch (found.Type)
            {
                case SimCore.Events.SecurityEvents.SecurityEventType.Confiscation:
                    rootCause = "Cargo confiscated by patrol";
                    factors.Add("Carrying contraband/restricted goods in guarded territory");
                    factors.Add($"Edge heat at time of incident: {found.CauseChain}");
                    countermeasures.Add("Avoid restricted territory or sell contraband before entering");
                    countermeasures.Add("Create an escort program for dangerous routes");
                    countermeasures.Add("Check threat overlay (T key) before planning routes");
                    break;
                default:
                    rootCause = "Security incident: " + found.Type.ToString();
                    factors.Add("Operating in hostile or contested territory");
                    countermeasures.Add("Review route security before travel");
                    break;
            }

            // FO commentary based on personality.
            string foComment = "";
            if (state.FirstOfficer != null)
            {
                foComment = state.FirstOfficer.CandidateType switch
                {
                    SimCore.Entities.FirstOfficerCandidate.Analyst => "Statistical analysis suggests avoiding this corridor during high-heat periods.",
                    SimCore.Entities.FirstOfficerCandidate.Veteran => "I've seen this before. Stick to safe lanes until the heat dies down.",
                    SimCore.Entities.FirstOfficerCandidate.Pathfinder => "There might be an alternative route that avoids this chokepoint entirely.",
                    _ => "Consider adjusting your trade routes to avoid similar incidents."
                };
            }

            result["root_cause"] = rootCause;
            result["factors"] = factors;
            result["countermeasures"] = countermeasures;
            result["fo_commentary"] = foComment;
        }, 0);

        return result;
    }

    // GATE.T61.SECURITY.CONVOY_PLAN.001: Threat assessment per route edge.
    // Returns Array of {edge_id, from, to, security_band, heat, threat_level (0-1)}.
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetRouteSecurityAssessmentV0(
        Godot.Collections.Array<string> routeNodeIds)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        TryExecuteSafeRead(state =>
        {
            for (int i = 0; i < routeNodeIds.Count - 1; i++)
            {
                string fromId = routeNodeIds[i];
                string toId = routeNodeIds[i + 1];

                foreach (var edge in state.Edges.Values)
                {
                    bool match = (edge.FromNodeId == fromId && edge.ToNodeId == toId) ||
                                 (edge.FromNodeId == toId && edge.ToNodeId == fromId);
                    if (!match) continue;

                    float secThreat = 1.0f - System.Math.Clamp(edge.SecurityLevelBps / 10000.0f, 0f, 1f);
                    float heatThreat = edge.Heat / (SimCore.Tweaks.SecurityTweaksV0.ConfiscationHeatThreshold * 2.0f);
                    float threat = System.Math.Clamp(secThreat * 0.6f + heatThreat * 0.4f, 0f, 1f);
                    string band = SimCore.Systems.SecurityLaneSystem.GetSecurityBand(edge.SecurityLevelBps);

                    result.Add(new Godot.Collections.Dictionary
                    {
                        ["edge_id"] = edge.Id,
                        ["from"] = fromId,
                        ["to"] = toId,
                        ["security_band"] = band,
                        ["heat"] = edge.Heat,
                        ["threat_level"] = threat,
                    });
                    break;
                }
            }
        }, 0);
        return result;
    }
}
