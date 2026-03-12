using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;
using System;
using System.Linq;

namespace SimCore.Systems;

// GATE.T18.NARRATIVE.FO_SYSTEM.001: First Officer companion system.
// Manages tier progression and dialogue trigger evaluation.
// Tier advances by tick thresholds. Dialogue fires on action triggers.
// The FO is the player's emotional proxy — reacts to revelations.
public static class FirstOfficerSystem
{
    /// <summary>
    /// Per-tick processing: advance dialogue tier based on current tick.
    /// </summary>
    public static void Process(SimState state)
    {
        if (state.FirstOfficer == null) return;
        if (!state.FirstOfficer.IsPromoted) return;

        // Advance tier based on tick thresholds OR relationship score.
        // GATE.T18.CHARACTER.FO_TRIGGER_WIRING.001: Score accumulation can advance tier early.
        var fo = state.FirstOfficer;
        DialogueTier newTier = fo.Tier;

        // Tick-based advancement (original)
        if (state.Tick >= NarrativeTweaksV0.TierEndgameTick)
            newTier = DialogueTier.Endgame;
        else if (state.Tick >= NarrativeTweaksV0.TierRevelationTick)
            newTier = DialogueTier.Revelation;
        else if (state.Tick >= NarrativeTweaksV0.TierFractureTick)
            newTier = DialogueTier.Fracture;
        else if (state.Tick >= NarrativeTweaksV0.TierMidTick)
            newTier = DialogueTier.Mid;

        // Score-based advancement: active play can advance tier early
        if (fo.RelationshipScore >= NarrativeTweaksV0.ScoreTierEndgame && newTier < DialogueTier.Endgame)
            newTier = DialogueTier.Endgame;
        else if (fo.RelationshipScore >= NarrativeTweaksV0.ScoreTierRevelation && newTier < DialogueTier.Revelation)
            newTier = DialogueTier.Revelation;
        else if (fo.RelationshipScore >= NarrativeTweaksV0.ScoreTierFracture && newTier < DialogueTier.Fracture)
            newTier = DialogueTier.Fracture;
        else if (fo.RelationshipScore >= NarrativeTweaksV0.ScoreTierMid && newTier < DialogueTier.Mid)
            newTier = DialogueTier.Mid;

        if (newTier > fo.Tier)
            fo.Tier = newTier;

        // GATE.T18.CHARACTER.FO_REACT.001: Auto-detect state-based triggers each tick.
        TryAutoDetectTriggers(state);
    }

    /// <summary>
    /// Check game-state conditions and fire FO triggers when conditions are first met.
    /// Once-per-trigger semantics are enforced by TryFireTrigger (DialogueEventLog check).
    /// GATE.T18.CHARACTER.FO_TRIGGER_WIRING.001: Expanded auto-detect for all tiers.
    /// </summary>
    private static void TryAutoDetectTriggers(SimState state)
    {
        var playerFleet = state.Fleets.Values.FirstOrDefault(f =>
            string.Equals(f.OwnerId, "player", StringComparison.Ordinal));
        if (playerFleet == null) return;

        // ── EARLY TIER triggers ──

        // FIRST_WARP: player fleet has traveled to a different node from start
        if (!string.IsNullOrEmpty(playerFleet.CurrentNodeId)
            && playerFleet.State != FleetState.Traveling
            && state.Missions.CompletedMissionIds.Count == 0
            && state.Missions.EventLog.Count > 0)
        {
            TryFireTrigger(state, "FIRST_WARP");
        }

        // FIRST_NPC_MET: any AI fleet at the same node as the player
        if (!string.IsNullOrEmpty(playerFleet.CurrentNodeId))
        {
            foreach (var fleet in state.Fleets.Values)
            {
                if (string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal)) continue;
                if (string.Equals(fleet.CurrentNodeId, playerFleet.CurrentNodeId, StringComparison.Ordinal)
                    && fleet.State != FleetState.Traveling)
                {
                    TryFireTrigger(state, "FIRST_NPC_MET");
                    break;
                }
            }
        }

        // FIRST_DISCOVERY: any discovery has been found
        if (state.Intel.Discoveries.Count > 0)
        {
            TryFireTrigger(state, "FIRST_DISCOVERY");
        }

        // GATE.S19.ONBOARD.FO_TRIGGERS.003: Onboarding triggers

        // FIRST_SALE_COMPLETE: player has traded at least one good
        if (state.PlayerStats != null && state.PlayerStats.GoodsTraded > 0)
        {
            TryFireTrigger(state, "FIRST_SALE_COMPLETE");
        }

        // FIRST_COMBAT_WIN: player has destroyed at least one NPC fleet
        if (state.PlayerStats != null && state.PlayerStats.NpcFleetsDestroyed > 0)
        {
            TryFireTrigger(state, "FIRST_COMBAT_WIN");
        }

        // ARRIVAL_NEW_SYSTEM: fire when player has visited 2+ nodes (first real arrival)
        if (state.PlayerStats != null && state.PlayerStats.NodesVisited >= NarrativeTweaksV0.ArrivalNewSystemNodes)
        {
            TryFireTrigger(state, "ARRIVAL_NEW_SYSTEM");
        }

        // FIRST_DOCK_WARZONE: player fleet at a node in an active warfront
        if (!string.IsNullOrEmpty(playerFleet.CurrentNodeId)
            && playerFleet.State != FleetState.Traveling
            && IsNodeInActiveWarfront(state, playerFleet.CurrentNodeId))
        {
            TryFireTrigger(state, "FIRST_DOCK_WARZONE");
        }

        // ── MID TIER triggers ──

        // FACTION_REP_GAINED: any faction reputation above 0
        foreach (var kv in state.FactionReputation)
        {
            if (kv.Value > 0)
            {
                TryFireTrigger(state, "FACTION_REP_GAINED");
                break;
            }
        }

        // FIRST_MODULE_REFIT: player fleet has any installed modules
        if (playerFleet.Slots != null && playerFleet.Slots.Any(s => !string.IsNullOrEmpty(s.InstalledModuleId)))
        {
            TryFireTrigger(state, "FIRST_MODULE_REFIT");
        }

        // SUPPLY_CHAIN_NOTICED: player has completed enough trade missions (seen the economy)
        if (state.Missions.CompletedMissionIds.Count >= NarrativeTweaksV0.SupplyChainNoticedMissions)
        {
            TryFireTrigger(state, "SUPPLY_CHAIN_NOTICED");
        }

        // ── FRACTURE TIER triggers ──

        // FIRST_FRACTURE_JUMP: player fleet has been to a void site
        if (string.Equals(playerFleet.CurrentTask, "AtVoidSite", StringComparison.Ordinal))
        {
            TryFireTrigger(state, "FIRST_FRACTURE_JUMP");
        }

        // FRACTURE_WEIGHT_SURPRISE: player carrying cargo with fracture origin phase > 0
        if (playerFleet.CargoOriginPhase != null && playerFleet.CargoOriginPhase.Count > 0
            && playerFleet.CargoOriginPhase.Values.Any(v => v > 0))
        {
            TryFireTrigger(state, "FRACTURE_WEIGHT_SURPRISE");
        }

        // REGULAR_NPC_VANISHES: Regular NPC is no longer alive
        if (state.NarrativeNpcs.TryGetValue(WarFacesContentV0.RegularNpcId, out var regularNpc)
            && !regularNpc.IsAlive)
        {
            TryFireTrigger(state, "REGULAR_NPC_VANISHES");
        }

        // INSTRUMENT_DIVERGENCE: any node has >10% divergence between standard and fracture
        if (state.FractureExposureJumps > 0 && !string.IsNullOrEmpty(playerFleet.CurrentNodeId))
        {
            TryFireTrigger(state, "INSTRUMENT_DIVERGENCE");
        }

        // TOPOLOGY_SHIFT: any mutable edge has shifted
        foreach (var edge in state.Edges.Values)
        {
            if (edge.IsMutable && edge.MutationEpoch > 0)
            {
                TryFireTrigger(state, "TOPOLOGY_SHIFT");
                break;
            }
        }

        // ── REVELATION TIER triggers ──

        // KNOWLEDGE_WEB_INSIGHT: knowledge graph has 3+ revealed connections
        int revealedCount = 0;
        foreach (var conn in state.Intel.KnowledgeConnections)
        {
            if (conn.IsRevealed) revealedCount++;
        }
        if (revealedCount >= NarrativeTweaksV0.KnowledgeWebInsightConnections)
        {
            TryFireTrigger(state, "KNOWLEDGE_WEB_INSIGHT");
        }
    }

    private static bool IsNodeInActiveWarfront(SimState state, string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return false;
        if (!state.NodeFactionId.TryGetValue(nodeId, out var fid)) return false;

        foreach (var wf in state.Warfronts.Values)
        {
            if ((string.Equals(fid, wf.CombatantA, StringComparison.Ordinal) ||
                 string.Equals(fid, wf.CombatantB, StringComparison.Ordinal))
                && wf.Intensity > 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Try to fire a dialogue trigger. Returns the dialogue text if the trigger
    /// fires, or empty string if it doesn't (wrong tier, already fired, no line).
    /// Called by game systems when trigger conditions are met.
    /// </summary>
    public static string TryFireTrigger(SimState state, string triggerToken)
    {
        if (state.FirstOfficer == null) return "";
        if (!state.FirstOfficer.IsPromoted) return "";

        var fo = state.FirstOfficer;

        // Check if already fired
        foreach (var evt in fo.DialogueEventLog)
        {
            if (string.Equals(evt.TriggerToken, triggerToken, StringComparison.Ordinal))
                return "";
        }

        // Look up the line
        var line = FirstOfficerContentV0.GetLine(triggerToken, fo.CandidateType);
        if (line == null) return "";

        // Check tier gate
        if (fo.Tier < line.MinTier) return "";

        // Fire the trigger
        fo.DialogueEventLog.Add(new DialogueEvent
        {
            TriggerToken = triggerToken,
            FiredTick = state.Tick
        });

        fo.RelationshipScore += line.RelationshipDelta;
        // GATE.S19.ONBOARD.FO_DYNAMIC.007: Replace dynamic tokens in dialogue text.
        fo.PendingDialogueLine = ReplaceDynamicTokens(state, line.Text);
        fo.PendingTriggerToken = triggerToken;

        // Check for blind spot exposure
        if (string.Equals(triggerToken, "BLINDSPOT_EXPOSED", StringComparison.Ordinal))
            fo.BlindSpotExposed = true;

        return fo.PendingDialogueLine;
    }

    /// <summary>
    /// Promote a candidate to First Officer. Called by player command.
    /// </summary>
    public static bool PromoteCandidate(SimState state, FirstOfficerCandidate candidateType)
    {
        if (candidateType == FirstOfficerCandidate.None) return false;
        if (state.FirstOfficer != null && state.FirstOfficer.IsPromoted) return false;

        state.FirstOfficer = new FirstOfficer
        {
            CandidateType = candidateType,
            IsPromoted = true,
            PromotionTick = state.Tick,
            Tier = DialogueTier.Early,
            RelationshipScore = 0,
            BlindSpotExposed = false
        };

        return true;
    }

    /// <summary>
    /// Consume the pending dialogue line (bridge calls this after displaying).
    /// </summary>
    public static string ConsumePendingDialogue(SimState state)
    {
        if (state.FirstOfficer == null) return "";
        string line = state.FirstOfficer.PendingDialogueLine;
        state.FirstOfficer.PendingDialogueLine = "";
        state.FirstOfficer.PendingTriggerToken = "";
        return line;
    }

    /// <summary>
    /// Check if the FO is in the promotion window (not yet promoted).
    /// GATE.T18.CHARACTER.FO_PROMO.001: Window opens by tick range OR score threshold.
    /// </summary>
    public static bool IsInPromotionWindow(SimState state)
    {
        if (state.FirstOfficer != null && state.FirstOfficer.IsPromoted) return false;

        // Tick-based window (original)
        bool inTickWindow = state.Tick >= NarrativeTweaksV0.FOPromotionMinTick &&
                            state.Tick <= NarrativeTweaksV0.FOPromotionMaxTick;

        // Score-based window: active players can promote earlier
        bool hasScoreThreshold = state.FirstOfficer != null &&
                                 state.FirstOfficer.RelationshipScore >= NarrativeTweaksV0.FOPromotionScoreThreshold;

        return inTickWindow || hasScoreThreshold;
    }

    /// <summary>
    /// Get the endgame lean for a candidate type.
    /// GATE.T18.CHARACTER.FO_PROMO.001: Used by UI to show candidate info during promotion.
    /// </summary>
    public static string GetCandidateEndgameLean(FirstOfficerCandidate candidateType)
    {
        foreach (var c in FirstOfficerContentV0.Candidates)
        {
            if (c.Type == candidateType)
                return c.EndgameLean;
        }
        return "";
    }

    /// <summary>
    /// Get the blind spot text for a candidate type.
    /// GATE.T18.CHARACTER.FO_PROMO.001: Exposed in UI after BlindSpotExposed flag is set.
    /// </summary>
    public static string GetCandidateBlindSpot(FirstOfficerCandidate candidateType)
    {
        foreach (var c in FirstOfficerContentV0.Candidates)
        {
            if (c.Type == candidateType)
                return c.BlindSpot;
        }
        return "";
    }

    /// <summary>
    /// GATE.S19.ONBOARD.FO_DYNAMIC.007: Replace dynamic tokens ({STATION}, {GOOD}, {DEST}, {CREDITS})
    /// in dialogue text with context-sensitive values from current game state.
    /// </summary>
    internal static string ReplaceDynamicTokens(SimState state, string text)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains('{')) return text;

        var playerFleet = state.Fleets.Values.FirstOrDefault(f =>
            string.Equals(f.OwnerId, "player", StringComparison.Ordinal));

        string nodeId = playerFleet?.CurrentNodeId ?? state.PlayerLocationNodeId ?? "";

        // {STATION}: current node display name
        if (text.Contains("{STATION}"))
        {
            string stationName = nodeId;
            if (state.Nodes.TryGetValue(nodeId, out var node))
                stationName = !string.IsNullOrEmpty(node.Name) ? node.Name : node.Id;
            text = text.Replace("{STATION}", stationName);
        }

        // {GOOD}: first available good at current market (deterministic: Ordinal sort)
        if (text.Contains("{GOOD}"))
        {
            string goodName = "cargo";
            if (state.Markets.TryGetValue(nodeId, out var market) && market.Inventory != null)
            {
                foreach (var kv in market.Inventory.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    if (kv.Value > 0) { goodName = kv.Key; break; }
                }
            }
            text = text.Replace("{GOOD}", goodName);
        }

        // {DEST}: adjacent node name (first neighbor, deterministic: Ordinal sort on edge Id)
        if (text.Contains("{DEST}"))
        {
            string destName = "the next system";
            foreach (var edge in state.Edges.Values.OrderBy(e => e.Id, StringComparer.Ordinal))
            {
                if (string.Equals(edge.FromNodeId, nodeId, StringComparison.Ordinal))
                {
                    if (state.Nodes.TryGetValue(edge.ToNodeId, out var destNode))
                        destName = !string.IsNullOrEmpty(destNode.Name) ? destNode.Name : destNode.Id;
                    break;
                }
                if (string.Equals(edge.ToNodeId, nodeId, StringComparison.Ordinal))
                {
                    if (state.Nodes.TryGetValue(edge.FromNodeId, out var destNode))
                        destName = !string.IsNullOrEmpty(destNode.Name) ? destNode.Name : destNode.Id;
                    break;
                }
            }
            text = text.Replace("{DEST}", destName);
        }

        // {CREDITS}: player credit balance
        if (text.Contains("{CREDITS}"))
        {
            text = text.Replace("{CREDITS}", state.PlayerCredits.ToString());
        }

        return text;
    }
}
