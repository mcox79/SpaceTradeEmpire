#nullable enable

using Godot;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;
using System;

namespace SpaceTradeEmpire.Bridge;

// GATE.T18.NARRATIVE.BRIDGE.001: Narrative layer bridge queries.
// Exposes First Officer, Data Logs, Knowledge Graph, Station Memory,
// War Consequences, Narrative NPCs, Fracture Mechanics, and Instrument
// Disagreement state to the Godot UI layer.
public partial class SimBridge
{
    // ── First Officer ──────────────────────────────────────────────

    /// <summary>
    /// Returns FO state: {promoted (bool), type (string), name (string),
    /// tier (string), score (int), blind_spot_exposed (bool), in_promotion_window (bool)}.
    /// </summary>
    public Godot.Collections.Dictionary GetFirstOfficerStateV0()
    {
        var result = new Godot.Collections.Dictionary
        {
            ["promoted"] = false,
            ["type"] = "None",
            ["name"] = "",
            ["tier"] = "Early",
            ["score"] = 0,
            ["blind_spot_exposed"] = false,
            ["in_promotion_window"] = false,
        };

        TryExecuteSafeRead(state =>
        {
            result["in_promotion_window"] = FirstOfficerSystem.IsInPromotionWindow(state);

            if (state.FirstOfficer == null) return;
            var fo = state.FirstOfficer;

            result["promoted"] = fo.IsPromoted;
            result["type"] = fo.CandidateType.ToString();
            result["archetype"] = fo.CandidateType.ToString();
            result["tier"] = fo.Tier.ToString();
            result["score"] = fo.RelationshipScore;
            result["blind_spot_exposed"] = fo.BlindSpotExposed;
            result["dialogue_count"] = fo.DialogueEventLog?.Count ?? 0;
            result["pending_text"] = fo.PendingDialogueLine ?? "";

            // Resolve name from content
            foreach (var c in FirstOfficerContentV0.Candidates)
            {
                if (c.Type == fo.CandidateType)
                {
                    result["name"] = c.Name;
                    break;
                }
            }
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns available FO candidates: [{type (string), name, description}].
    /// </summary>
    public Godot.Collections.Array GetFirstOfficerCandidatesV0()
    {
        var result = new Godot.Collections.Array();

        foreach (var c in FirstOfficerContentV0.Candidates)
        {
            result.Add(new Godot.Collections.Dictionary
            {
                ["type"] = c.Type.ToString(),
                ["name"] = c.Name,
                ["description"] = c.Description,
            });
        }

        return result;
    }

    /// <summary>
    /// Returns current triggered FO dialogue line (or ""). Consumes the pending line.
    /// </summary>
    public string GetFirstOfficerDialogueV0()
    {
        string line = "";

        TryExecuteSafeRead(state =>
        {
            line = FirstOfficerSystem.ConsumePendingDialogue(state);
        }, 0);

        return line;
    }

    /// <summary>
    /// Promote a candidate to First Officer. Returns true on success.
    /// </summary>
    public bool PromoteFirstOfficerV0(string candidateType)
    {
        bool success = false;

        if (!Enum.TryParse<FirstOfficerCandidate>(candidateType, out var parsed))
            return false;

        _stateLock.EnterWriteLock();
        try
        {
            success = FirstOfficerSystem.PromoteCandidate(_kernel.State, parsed);
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        return success;
    }

    // ── Data Logs ──────────────────────────────────────────────────

    /// <summary>
    /// Returns all discovered data logs: [{log_id, thread, speakers (Array), tier, is_new (bool)}].
    /// </summary>
    public Godot.Collections.Array GetDiscoveredDataLogsV0()
    {
        var result = new Godot.Collections.Array();

        TryExecuteSafeRead(state =>
        {
            foreach (var kv in state.DataLogs)
            {
                var log = kv.Value;
                if (!log.IsDiscovered) continue;

                var speakers = new Godot.Collections.Array();
                foreach (var s in log.Speakers) speakers.Add(s);

                result.Add(new Godot.Collections.Dictionary
                {
                    ["log_id"] = log.LogId,
                    ["thread"] = log.Thread.ToString(),
                    ["speakers"] = speakers,
                    ["tier"] = log.RevelationTier,
                    ["mechanical_hook"] = log.MechanicalHook,
                    ["location_node"] = log.LocationNodeId,
                });
            }
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns full detail of a data log: {log_id, thread, tier, mechanical_hook,
    /// entries: [{speaker, text, is_personal}]}.
    /// </summary>
    public Godot.Collections.Dictionary GetDataLogDetailV0(string logId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["log_id"] = logId ?? "",
            ["found"] = false,
            ["thread"] = "",
            ["tier"] = 0,
            ["mechanical_hook"] = "",
            ["entries"] = new Godot.Collections.Array(),
        };

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(logId)) return;
            if (!state.DataLogs.TryGetValue(logId, out var log)) return;

            result["found"] = true;
            result["thread"] = log.Thread.ToString();
            result["tier"] = log.RevelationTier;
            result["mechanical_hook"] = log.MechanicalHook;

            var entries = new Godot.Collections.Array();
            foreach (var entry in log.Entries)
            {
                entries.Add(new Godot.Collections.Dictionary
                {
                    ["speaker"] = entry.Speaker,
                    ["text"] = entry.Text,
                    ["is_personal"] = entry.IsPersonal,
                });
            }
            result["entries"] = entries;
        }, 0);

        return result;
    }

    // ── Knowledge Graph ────────────────────────────────────────────

    /// <summary>
    /// Returns all knowledge connections: [{connection_id, source_id, target_id,
    /// type (string), revealed (bool), visible (bool), description}].
    /// </summary>
    public Godot.Collections.Array GetKnowledgeGraphV0()
    {
        var result = new Godot.Collections.Array();

        TryExecuteSafeRead(state =>
        {
            foreach (var conn in state.Intel.KnowledgeConnections)
            {
                bool visible = KnowledgeGraphSystem.IsConnectionVisible(state, conn);
                if (!visible) continue;

                result.Add(new Godot.Collections.Dictionary
                {
                    ["connection_id"] = conn.ConnectionId,
                    ["source_id"] = conn.SourceDiscoveryId,
                    ["target_id"] = conn.TargetDiscoveryId,
                    ["type"] = conn.IsRevealed ? conn.ConnectionType.ToString() : "Unknown",
                    ["revealed"] = conn.IsRevealed,
                    ["visible"] = true,
                    ["description"] = conn.IsRevealed ? conn.Description : "?",
                });
            }
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns knowledge graph stats: {total, revealed, question_marks}.
    /// </summary>
    public Godot.Collections.Dictionary GetKnowledgeGraphStatsV0()
    {
        var result = new Godot.Collections.Dictionary
        {
            ["total"] = 0,
            ["revealed"] = 0,
            ["question_marks"] = 0,
        };

        TryExecuteSafeRead(state =>
        {
            int total = state.Intel.KnowledgeConnections.Count;
            int revealed = 0;
            int questionMarks = 0;

            foreach (var conn in state.Intel.KnowledgeConnections)
            {
                if (conn.IsRevealed) revealed++;
                else if (KnowledgeGraphSystem.IsConnectionVisible(state, conn))
                    questionMarks++;
            }

            result["total"] = total;
            result["revealed"] = revealed;
            result["question_marks"] = questionMarks;
        }, 0);

        return result;
    }

    // ── Station Memory ─────────────────────────────────────────────

    /// <summary>
    /// Returns station memory for a node: {goods: [{good_id, deliveries, quantity}]}.
    /// </summary>
    public Godot.Collections.Dictionary GetStationMemoryV0(string nodeId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["node_id"] = nodeId ?? "",
            ["goods"] = new Godot.Collections.Array(),
        };

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(nodeId)) return;

            var goods = new Godot.Collections.Array();
            foreach (var kv in state.StationMemory)
            {
                var record = kv.Value;
                if (!string.Equals(record.NodeId, nodeId, StringComparison.Ordinal)) continue;

                goods.Add(new Godot.Collections.Dictionary
                {
                    ["good_id"] = record.GoodId,
                    ["deliveries"] = record.TotalDeliveries,
                    ["quantity"] = record.TotalQuantity,
                    ["first_tick"] = record.FirstDeliveryTick,
                    ["last_tick"] = record.LastDeliveryTick,
                });
            }
            result["goods"] = goods;
        }, 0);

        return result;
    }

    // ── War Consequences ───────────────────────────────────────────

    /// <summary>
    /// Returns active (unresolved) war consequences:
    /// [{id, manifest_text, consequence_text, ticks_remaining, good_id, quantity}].
    /// </summary>
    public Godot.Collections.Array GetActiveWarConsequencesV0()
    {
        var result = new Godot.Collections.Array();

        TryExecuteSafeRead(state =>
        {
            foreach (var kv in state.WarConsequences)
            {
                var wc = kv.Value;

                int ticksRemaining = wc.IsResolved ? 0
                    : Math.Max(0, wc.DelayTicks - (state.Tick - wc.CreatedTick));

                result.Add(new Godot.Collections.Dictionary
                {
                    ["id"] = wc.Id,
                    ["manifest_text"] = wc.ManifestText,
                    ["consequence_text"] = wc.IsResolved ? wc.ConsequenceText : "",
                    ["is_resolved"] = wc.IsResolved,
                    ["ticks_remaining"] = ticksRemaining,
                    ["good_id"] = wc.GoodId,
                    ["quantity"] = wc.Quantity,
                    ["source_node"] = wc.SourceNodeId,
                });
            }
        }, 0);

        return result;
    }

    // ── Narrative NPCs ─────────────────────────────────────────────

    /// <summary>
    /// Returns narrative NPCs at a given node:
    /// [{npc_id, name, kind (string), dialogue (string), is_alive}].
    /// </summary>
    public Godot.Collections.Array GetNarrativeNpcsAtNodeV0(string nodeId)
    {
        var result = new Godot.Collections.Array();

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(nodeId)) return;

            foreach (var kv in state.NarrativeNpcs)
            {
                var npc = kv.Value;
                if (!npc.IsAlive) continue;
                if (!string.Equals(npc.NodeId, nodeId, StringComparison.Ordinal)) continue;

                string dialogue = "";
                if (npc.Kind == NarrativeNpcKind.Regular)
                    dialogue = NarrativeNpcSystem.GetRegularMention(state, nodeId);

                result.Add(new Godot.Collections.Dictionary
                {
                    ["npc_id"] = npc.NpcId,
                    ["name"] = npc.Name,
                    ["kind"] = npc.Kind.ToString(),
                    ["dialogue"] = dialogue,
                    ["is_alive"] = npc.IsAlive,
                    ["faction"] = npc.FactionId,
                });
            }
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns all narrative NPCs regardless of location:
    /// [{npc_id, name, kind, node_id, is_alive, vanish_reason}].
    /// </summary>
    public Godot.Collections.Array GetAllNarrativeNpcsV0()
    {
        var result = new Godot.Collections.Array();

        TryExecuteSafeRead(state =>
        {
            foreach (var kv in state.NarrativeNpcs)
            {
                var npc = kv.Value;
                result.Add(new Godot.Collections.Dictionary
                {
                    ["npc_id"] = npc.NpcId,
                    ["name"] = npc.Name,
                    ["kind"] = npc.Kind.ToString(),
                    ["node_id"] = npc.NodeId,
                    ["is_alive"] = npc.IsAlive,
                    ["vanish_reason"] = npc.VanishReason,
                    ["faction"] = npc.FactionId,
                });
            }
        }, 0);

        return result;
    }

    // ── Fracture Mechanics ─────────────────────────────────────────

    /// <summary>
    /// Returns cargo fracture weight info for the player fleet:
    /// [{good_id, current_qty, origin_phase, weight_bps}].
    /// </summary>
    public Godot.Collections.Array GetCargoFractureWeightV0()
    {
        var result = new Godot.Collections.Array();

        TryExecuteSafeRead(state =>
        {
            string playerFleetId = "fleet_trader_1";
            if (!state.Fleets.TryGetValue(playerFleetId, out var fleet)) return;

            foreach (var kv in fleet.CargoOriginPhase)
            {
                string goodId = kv.Key;
                int originPhase = kv.Value;
                int currentQty = fleet.GetCargoUnits(goodId);
                if (currentQty <= 0) continue;

                int weightBps = FractureWeightSystem.ComputeWeightBps(
                    state, playerFleetId, goodId, originPhase);

                result.Add(new Godot.Collections.Dictionary
                {
                    ["good_id"] = goodId,
                    ["current_qty"] = currentQty,
                    ["origin_phase"] = originPhase,
                    ["weight_bps"] = weightBps,
                });
            }
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns ETA range for a route edge: {min, max, uncertainty_pct, adaptation_stage}.
    /// </summary>
    public Godot.Collections.Dictionary GetRouteEtaRangeV0(string edgeId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["edge_id"] = edgeId ?? "",
            ["min"] = 0,
            ["max"] = 0,
            ["uncertainty_pct"] = 0,
            ["adaptation_stage"] = 1,
        };

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(edgeId)) return;
            string playerFleetId = "fleet_trader_1";

            var (minTicks, maxTicks) = RouteUncertaintySystem.ComputeEtaRange(
                state, edgeId, playerFleetId);

            result["min"] = minTicks;
            result["max"] = maxTicks;
            result["adaptation_stage"] = RouteUncertaintySystem.GetAdaptationStage(
                state.FractureExposureJumps);

            if (maxTicks > 0 && minTicks < maxTicks)
            {
                int range = maxTicks - minTicks;
                int mid = (maxTicks + minTicks) / 2;
                result["uncertainty_pct"] = mid > 0 ? range * 100 / mid : 0;
            }
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns dual instrument readings for a good at a node:
    /// {standard_price, fracture_price, divergence_pct}.
    /// </summary>
    public Godot.Collections.Dictionary GetDualReadingsV0(string nodeId, string goodId)
    {
        var result = new Godot.Collections.Dictionary
        {
            ["node_id"] = nodeId ?? "",
            ["good_id"] = goodId ?? "",
            ["standard_price"] = 0,
            ["fracture_price"] = 0,
            ["divergence_pct"] = 0,
        };

        TryExecuteSafeRead(state =>
        {
            if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(goodId)) return;

            int stdPrice = InstrumentDisagreementSystem.ComputeStandardPriceReading(
                state, nodeId, goodId);
            int fracPrice = InstrumentDisagreementSystem.ComputeFracturePriceReading(
                state, nodeId, goodId);

            result["standard_price"] = stdPrice;
            result["fracture_price"] = fracPrice;

            if (stdPrice > 0)
            {
                int diff = Math.Abs(stdPrice - fracPrice);
                result["divergence_pct"] = diff * 100 / stdPrice;
            }
        }, 0);

        return result;
    }

    // ── Active Leads ──────────────────────────────────────────────

    /// <summary>
    /// GATE.S6.UI_DISCOVERY.ACTIVE_LEADS.001: Returns up to 3 active rumor leads
    /// from IntelBook for HUD display: [{lead_id, source_verb, location_token, payoff_token, node_name}].
    /// </summary>
    public Godot.Collections.Array GetActiveLeadsV0()
    {
        var result = new Godot.Collections.Array();

        TryExecuteSafeRead(state =>
        {
            if (state.Intel?.RumorLeads is null) return;

            int count = 0;
            foreach (var kv in state.Intel.RumorLeads)
            {
                if (count >= 3) break;
                var lead = kv.Value;
                if (lead.Status != RumorLeadStatus.Active) continue;

                string locationToken = lead.Hint?.CoarseLocationToken ?? "";
                string nodeName = locationToken;
                // Resolve node display name if location token is a node ID.
                if (!string.IsNullOrEmpty(locationToken) && state.Nodes.TryGetValue(locationToken, out var node))
                    nodeName = node.Name ?? locationToken;

                result.Add(new Godot.Collections.Dictionary
                {
                    ["lead_id"] = lead.LeadId,
                    ["source_verb"] = lead.SourceVerbToken ?? "",
                    ["location_token"] = locationToken,
                    ["payoff_token"] = lead.Hint?.ImpliedPayoffToken ?? "",
                    ["node_name"] = nodeName,
                });
                count++;
            }
        }, 0);

        return result;
    }

    /// <summary>
    /// Returns mutable edges that have shifted: [{edge_id, from, to, mutation_epoch}].
    /// </summary>
    public Godot.Collections.Array GetMutableEdgesV0()
    {
        var result = new Godot.Collections.Array();

        TryExecuteSafeRead(state =>
        {
            foreach (var kv in state.Edges)
            {
                var edge = kv.Value;
                if (!edge.IsMutable) continue;

                result.Add(new Godot.Collections.Dictionary
                {
                    ["edge_id"] = edge.Id,
                    ["from"] = edge.FromNodeId,
                    ["to"] = edge.ToNodeId,
                    ["mutation_epoch"] = edge.MutationEpoch,
                    ["is_shifted"] = edge.MutationEpoch > 0,
                });
            }
        }, 0);

        return result;
    }
}
