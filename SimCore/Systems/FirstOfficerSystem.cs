using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SimCore.Systems;

// GATE.T18.NARRATIVE.FO_SYSTEM.001: First Officer companion system.
// Manages tier progression and dialogue trigger evaluation.
// Tier advances by tick thresholds. Dialogue fires on action triggers.
// The FO is the player's emotional proxy — reacts to revelations.
public static class FirstOfficerSystem
{
    private sealed class Scratch
    {
        public readonly List<string> SortedKeys = new();
        public readonly HashSet<string> SignalNodes = new(StringComparer.Ordinal);
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();
    /// <summary>
    /// Per-tick processing: advance dialogue tier based on current tick.
    /// </summary>
    public static void Process(SimState state)
    {
        if (state.FirstOfficer == null) return;

        // GATE.T67.FO.DOCK_GREETING.001: Track first dock even before promotion.
        // If player docks before FO is promoted, set deferred flag for greeting after promotion.
        if (!state.FirstOfficer.IsPromoted)
        {
            Fleet? pf2 = null;
            foreach (var f in state.Fleets.Values)
            {
                if (string.Equals(f.OwnerId, "player", StringComparison.Ordinal))
                { pf2 = f; break; }
            }
            if (pf2 != null && pf2.State == FleetState.Docked)
                state.FirstOfficer.DeferredDockGreeting = true;
            return;
        }

        // GATE.T67.FO.DOCK_GREETING.001: Fire deferred dock greeting after promotion.
        if (state.FirstOfficer.DeferredDockGreeting)
        {
            state.FirstOfficer.DeferredDockGreeting = false;
            TryFireTrigger(state, "FIRST_DOCK");
        }

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

        // GATE.T57.CENTAUR.COMPETENCE_TIERS.001: Evaluate FO competence tier advancement.
        EvaluateCompetenceTier(state);

        // GATE.T57.CENTAUR.WORLD_ADAPT.001: Scan for world events and adapt routes.
        ProcessWorldAdaptation(state);

        // GATE.T57.CENTAUR.BOREDOM_TRIGGERS.001: Check boredom circuit breakers.
        ProcessBoredomTriggers(state);

        // GATE.T58.FO.LOA_MODEL.001: Clean up expired route revert entries.
        CleanupRevertEntries(state);

        // GATE.T63.FO.DOCK_GREETING.001: FIRST_DOCK fires even during tutorial — the audit
        // (fh_5 #13) found FO is silent at first dock because TryAutoDetectTriggers is suppressed.
        // FIRST_DOCK is safe to fire during tutorial because it's a one-shot trigger (DialogueEventLog)
        // and won't overlap with scripted dialogue phases (FO pending line is overwritten per-tick).
        {
            Fleet? pf = null;
            foreach (var f in state.Fleets.Values)
            {
                if (string.Equals(f.OwnerId, "player", StringComparison.Ordinal))
                { pf = f; break; }
            }
            if (pf != null && pf.State == FleetState.Docked)
                TryFireTrigger(state, "FIRST_DOCK");
        }

        // GATE.T18.CHARACTER.FO_REACT.001: Auto-detect state-based triggers each tick.
        // Suppress reactive triggers during tutorial to prevent overlap with scripted dialogue.
        if (!TutorialSystem.IsActive(state))
            TryAutoDetectTriggers(state);

        // GATE.T60.FO.PACING_HEARTBEAT.001: Ambient observations on 200-tick cadence.
        ProcessPacingHeartbeat(state);

        // GATE.T64.FO.AMBIENT_TRIGGERS.001: Condition-based ambient triggers (Hades grid pattern).
        if (!TutorialSystem.IsActive(state))
            ProcessAmbientConditionTriggers(state);

        // Silence fallback: if FO has been silent too long, fire an ambient commentary trigger.
        if (!TutorialSystem.IsActive(state))
            ProcessSilenceFallback(state);
    }

    /// <summary>
    /// Check game-state conditions and fire FO triggers when conditions are first met.
    /// Once-per-trigger semantics are enforced by TryFireTrigger (DialogueEventLog check).
    /// GATE.T18.CHARACTER.FO_TRIGGER_WIRING.001: Expanded auto-detect for all tiers.
    /// </summary>
    private static void TryAutoDetectTriggers(SimState state)
    {
        Fleet? playerFleet = null;
        foreach (var f in state.Fleets.Values)
        {
            if (string.Equals(f.OwnerId, "player", StringComparison.Ordinal))
            { playerFleet = f; break; }
        }
        if (playerFleet == null) return;

        // ── EARLY TIER triggers ──

        // FIRST_DOCK: player fleet docked at a station for the first time
        if (playerFleet.State == FleetState.Docked)
        {
            TryFireTrigger(state, "FIRST_DOCK");
        }

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

        // GATE.T64.FO.COMBAT_REACTION.001: Delayed FO reaction after every combat win.
        // Fires CombatReactionDelayTicks after the kill, cycling COMBAT_REACTION_1..N.
        if (state.PlayerStats != null && state.PlayerStats.LastCombatWinTick >= 0)
        {
            int sinceCombat = state.Tick - state.PlayerStats.LastCombatWinTick;
            if (sinceCombat == NarrativeTweaksV0.CombatReactionDelayTicks)
            {
                int idx = (state.PlayerStats.NpcFleetsDestroyed % NarrativeTweaksV0.CombatReactionMaxCount) + 1; // STRUCTURAL: 1-based cycling
                TryFireTrigger(state, $"COMBAT_REACTION_{idx}");
            }
        }

        // GATE.T60.SPIN.TUTORIAL.001: Hint to use battle stations when hostile nearby but spin never used.
        if (playerFleet.BattleStations == BattleStationsState.StandDown
            && playerFleet.State != FleetState.Docked
            && !string.IsNullOrEmpty(playerFleet.CurrentNodeId))
        {
            bool hasHostile = false;
            foreach (var f in state.Fleets.Values)
            {
                if (string.Equals(f.OwnerId, "player", StringComparison.Ordinal)) continue;
                if (f.HullHp <= 0) continue;
                if (!string.Equals(f.CurrentNodeId, playerFleet.CurrentNodeId, StringComparison.Ordinal)) continue;
                if (string.Equals(f.OwnerId, Tweaks.FactionTweaksV0.PirateId, StringComparison.Ordinal))
                { hasHostile = true; break; }
            }
            if (hasHostile)
                TryFireTrigger(state, "BATTLE_STATIONS_HINT");
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

        // FIRST_INDUSTRY_SEEN: player fleet docked at a node with IndustrySites
        if (!string.IsNullOrEmpty(playerFleet.CurrentNodeId)
            && playerFleet.State != FleetState.Traveling)
        {
            foreach (var site in state.IndustrySites.Values)
            {
                if (string.Equals(site.NodeId, playerFleet.CurrentNodeId, StringComparison.Ordinal))
                {
                    TryFireTrigger(state, "FIRST_INDUSTRY_SEEN");
                    break;
                }
            }
        }

        // COSTS_MOUNTING: FO comments on operating costs after player has traded enough
        // to notice the credit drain. One-shot, fires once per playthrough.
        if (state.PlayerStats != null
            && state.PlayerStats.TotalCreditsEarned >= NarrativeTweaksV0.CostsMountingCreditsEarned
            && state.PlayerStats.NodesVisited >= NarrativeTweaksV0.CostsMountingNodesVisited)
        {
            TryFireTrigger(state, "COSTS_MOUNTING");
        }

        // GATE.T63.PACING.MID_EXPLORE_BEAT.001: ALL_NODES_EXPLORED — breaks 153-decision reward desert.
        // When player has visited every node, FO delivers galaxy assessment + hints at deeper systems.
        if (state.PlayerStats != null
            && state.Nodes.Count > 0
            && state.PlayerStats.NodesVisited >= state.Nodes.Count)
        {
            TryFireTrigger(state, "ALL_NODES_EXPLORED");
        }

        // GATE.T67.PACING.STREAK_BREAKER.001: Monotone action streak → FO observation.
        // After 15+ consecutive same-type actions, FO breaks the monotony with a comment.
        if (state.PlayerStats != null
            && state.PlayerStats.ConsecutiveActionStreak >= NarrativeTweaksV0.MonotoneStreakThreshold
            && state.PlayerStats.ConsecutiveActionStreak % NarrativeTweaksV0.MonotoneStreakThreshold == 0) // STRUCTURAL: fire every Nth
        {
            int streakIdx = (state.PlayerStats.ConsecutiveActionStreak / NarrativeTweaksV0.MonotoneStreakThreshold
                % NarrativeTweaksV0.MonotoneStreakMaxCount) + 1; // STRUCTURAL: 1-based cycling
            TryFireTrigger(state, $"MONOTONE_STREAK_{streakIdx}");
        }

        // GATE.T68.PACING.EVENT_INTERRUPTS.001: Market perturbation at high monotone streaks.
        // After 20+ consecutive same actions, inject dampening to break the economic loop.
        MarketSystem.InjectMonotoneInterrupt(state);

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
        bool hasModule = false;
        if (playerFleet.Slots != null)
        {
            foreach (var s in playerFleet.Slots)
            {
                if (!string.IsNullOrEmpty(s.InstalledModuleId)) { hasModule = true; break; }
            }
        }
        if (hasModule)
        {
            TryFireTrigger(state, "FIRST_MODULE_REFIT");
        }

        // GATE.T60.DISC.SENSOR_NOTIFY.001: Notify when sensor_suite research completes.
        if (state.Tech.UnlockedTechIds.Contains(Tweaks.SurveyTweaksV0.SensorSuiteTechId))
        {
            TryFireTrigger(state, "SENSOR_SUITE_ONLINE");
        }

        // SUPPLY_CHAIN_NOTICED: player has completed enough trade missions (seen the economy)
        if (state.Missions.CompletedMissionIds.Count >= NarrativeTweaksV0.SupplyChainNoticedMissions)
        {
            TryFireTrigger(state, "SUPPLY_CHAIN_NOTICED");
        }

        // GATE.T63.PACING.LATE_ESCALATION.001: LATE_INSTABILITY_WARNING — FO warns of growing warfront tension.
        // Fires once when any warfront reaches Skirmish+ after the late escalation tick threshold.
        if (state.Tick >= Tweaks.WarfrontTweaksV0.LateEscalationStartTick
            && state.Warfronts != null)
        {
            foreach (var wf in state.Warfronts.Values)
            {
                if ((int)wf.Intensity >= (int)WarfrontIntensity.Skirmish)
                {
                    TryFireTrigger(state, "LATE_INSTABILITY_WARNING");
                    break;
                }
            }
        }

        // ── FRACTURE TIER triggers ──

        // FIRST_FRACTURE_JUMP: player fleet has been to a void site
        if (string.Equals(playerFleet.CurrentTask, "AtVoidSite", StringComparison.Ordinal))
        {
            TryFireTrigger(state, "FIRST_FRACTURE_JUMP");
        }

        // FRACTURE_WEIGHT_SURPRISE: player carrying cargo with fracture origin phase > 0
        if (playerFleet.CargoOriginPhase != null && playerFleet.CargoOriginPhase.Count > 0
            && HasPositiveOriginPhase(playerFleet))
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

        // ── GATE.T41 Discovery-as-Automation triggers ──

        // FIRST_TRADE_ROUTE_DISCOVERED: any discovery-derived trade route exists
        if (state.Intel.TradeRoutes.Count > 0)
        {
            foreach (var route in state.Intel.TradeRoutes.Values)
            {
                if (!string.IsNullOrEmpty(route.SourceDiscoveryId))
                {
                    TryFireTrigger(state, "FIRST_TRADE_ROUTE_DISCOVERED");
                    break;
                }
            }
        }

        // SURVEY_AUTOMATION_SUGGESTED: player has manually scanned 3+ discoveries of any family
        {
            int maxScans = 0;
            foreach (var disc in state.Intel.Discoveries.Values)
            {
                if (disc is null || disc.Phase < DiscoveryPhase.Scanned) continue;
                maxScans++;
            }
            if (maxScans >= Tweaks.SurveyProgramTweaksV0.ManualScanGateCount)
            {
                TryFireTrigger(state, "SURVEY_AUTOMATION_SUGGESTED");
            }
        }

        // CHAIN_LINK_DISCOVERED: any anomaly chain has CurrentStepIndex > 0 (step completed)
        if (state.AnomalyChains != null)
        {
            foreach (var chain in state.AnomalyChains.Values)
            {
                if (chain.CurrentStepIndex > 0 && chain.Status == AnomalyChainStatus.Active)
                {
                    TryFireTrigger(state, "CHAIN_LINK_DISCOVERED");
                    break;
                }
            }
        }

        // CHAIN_COMPLETED: any anomaly chain is Completed
        if (state.AnomalyChains != null)
        {
            foreach (var chain in state.AnomalyChains.Values)
            {
                if (chain.Status == AnomalyChainStatus.Completed)
                {
                    TryFireTrigger(state, "CHAIN_COMPLETED");
                    break;
                }
            }
        }

        // ── GATE.T62.PIPELINE.FO_INTEL_TRIGGER.001: Discovery opportunity trigger ──
        // Fires when fresh EconomicIntel exists with estimated margin > threshold.
        if (state.Intel.EconomicIntels.Count > 0)
        {
            foreach (var intel in state.Intel.EconomicIntels.Values)
            {
                if (intel is null) continue;
                int age = state.Tick - intel.CreatedTick;
                if (age < 0) continue; // STRUCTURAL: skip invalid
                // Fresh = within first third of max freshness window.
                int freshThreshold = intel.FreshnessMaxTicks / 3; // STRUCTURAL: /3 early decay band
                if (freshThreshold <= 0) freshThreshold = 1; // STRUCTURAL: minimum 1
                if (age <= freshThreshold && intel.EstimatedValue >= Tweaks.EconomicIntelTweaksV0.MarketAnomalyBaseValue)
                {
                    TryFireTrigger(state, "DISCOVERY_OPPORTUNITY");
                    break;
                }
            }
        }

        // ── GATE.T42 Planet Scan triggers ──

        // FIRST_PLANET_SURVEYED: player has performed at least one planet scan
        if (state.PlanetScanResults.Count > 0)
        {
            TryFireTrigger(state, "FIRST_PLANET_SURVEYED");
        }

        // SCAN_MODE_MISMATCH: most recent scan had low affinity (< 8000 bps)
        if (state.PlanetScanResults.Count > 0)
        {
            // Check the most recent scan result.
            string latestScanId = $"SCAN_{state.NextPlanetScanSeq - 1}";
            if (state.PlanetScanResults.TryGetValue(latestScanId, out var latestScan)
                && latestScan.AffinityBps < PlanetScanTweaksV0.MidAffinityThresholdBps)
            {
                TryFireTrigger(state, "SCAN_MODE_MISMATCH");
            }
        }

        // PATTERN_RECOGNIZED: 5+ scans completed with any single mode
        {
            int mineralCount = 0, signalCount = 0, archCount = 0;
            foreach (var scan in state.PlanetScanResults.Values)
            {
                switch (scan.Mode)
                {
                    case ScanMode.MineralSurvey: mineralCount++; break;
                    case ScanMode.SignalSweep: signalCount++; break;
                    case ScanMode.Archaeological: archCount++; break;
                }
            }
            if (mineralCount >= NarrativeTweaksV0.PatternRecognizedScanCount
                || signalCount >= NarrativeTweaksV0.PatternRecognizedScanCount
                || archCount >= NarrativeTweaksV0.PatternRecognizedScanCount)
            {
                TryFireTrigger(state, "PATTERN_RECOGNIZED");
            }
        }

        // RARE_FIND: any landing scan produced FragmentCache, or Physical Evidence with investigation
        foreach (var scan in state.PlanetScanResults.Values)
        {
            if (scan.Phase == ScanPhase.Landing || scan.Phase == ScanPhase.AtmosphericSample)
            {
                if (scan.Category == FindingCategory.FragmentCache
                    || (scan.Category == FindingCategory.PhysicalEvidence && scan.InvestigationAvailable))
                {
                    TryFireTrigger(state, "RARE_FIND");
                    break;
                }
            }
        }

        // SIGNAL_TRIANGULATED: 2+ signal leads from different nodes targeting the same area
        // (simplified: 2+ Signal Lead scan results from different nodes)
        {
            var scratch = s_scratch.GetOrCreateValue(state);
            scratch.SignalNodes.Clear();
            foreach (var scan in state.PlanetScanResults.Values)
            {
                if (scan.Category == FindingCategory.SignalLead)
                    scratch.SignalNodes.Add(scan.NodeId);
            }
            if (scratch.SignalNodes.Count >= 2) // STRUCTURAL: triangulation threshold
            {
                TryFireTrigger(state, "SIGNAL_TRIANGULATED");
            }
        }

        // LORE_DISCOVERY: any scan produced a DataArchive finding
        foreach (var scan in state.PlanetScanResults.Values)
        {
            if (scan.Category == FindingCategory.DataArchive)
            {
                TryFireTrigger(state, "LORE_DISCOVERY");
                break;
            }
        }

        // TRADE_INTEL_STALE: any high-value discovery-derived route has gone Stale
        if (state.Intel.TradeRoutes.Count > 0)
        {
            foreach (var route in state.Intel.TradeRoutes.Values)
            {
                if (!string.IsNullOrEmpty(route.SourceDiscoveryId)
                    && route.Status == TradeRouteStatus.Stale
                    && route.EstimatedProfitPerUnit >= Tweaks.DiscoveryIntelTweaksV0.HighValueStaleThreshold)
                {
                    TryFireTrigger(state, "TRADE_INTEL_STALE");
                    break;
                }
            }
        }

        // ── GATE.T45 Deep Dread triggers (8 triggers) ──

        // Compute min hop distance from nearest faction home (measures isolation).
        int minHopsFromCiv = int.MaxValue;
        if (!string.IsNullOrEmpty(playerFleet.CurrentNodeId) && state.FactionHomeNodes != null)
        {
            foreach (var kv in state.FactionHomeNodes)
            {
                int hops = NpcTradeSystem.ComputeHopsFromFactionHome(state, kv.Key, playerFleet.CurrentNodeId);
                if (hops < minHopsFromCiv) minHopsFromCiv = hops;
            }
        }

        // FAR_FROM_PATROL: player is far from any faction patrol routes
        if (minHopsFromCiv >= DeepDreadTweaksV0.FoFarFromPatrolHops)
        {
            TryFireTrigger(state, "FAR_FROM_PATROL");
        }

        // COMMS_LOST: player is beyond reliable comms range
        if (minHopsFromCiv >= DeepDreadTweaksV0.FoCommsLostHops)
        {
            TryFireTrigger(state, "COMMS_LOST");
        }

        // Phase-based triggers at player node.
        if (!string.IsNullOrEmpty(playerFleet.CurrentNodeId)
            && state.Nodes.TryGetValue(playerFleet.CurrentNodeId, out var playerNode))
        {
            int phase = InstabilityTweaksV0.GetPhaseIndex(playerNode.InstabilityLevel);

            // LATTICE_THIN: player at Phase 2+ node (lattice degradation visible)
            if (phase >= 2) // STRUCTURAL: Phase 2+ threshold
            {
                TryFireTrigger(state, "LATTICE_THIN");
            }

            // VOID_ENTRY: player at Phase 4 node (the void paradox)
            if (phase >= 4) // STRUCTURAL: Phase 4 threshold
            {
                TryFireTrigger(state, "VOID_ENTRY");
            }
        }

        // SENSOR_GHOST_SEEN: any sensor ghost currently active
        if (state.SensorGhosts != null && state.SensorGhosts.Count > 0)
        {
            TryFireTrigger(state, "SENSOR_GHOST_SEEN");
        }

        // FAUNA_DETECTED: any lattice fauna present at player's node
        if (state.LatticeFauna != null)
        {
            foreach (var fauna in state.LatticeFauna)
            {
                if (fauna.State == LatticeFaunaState.Present
                    && string.Equals(fauna.NodeId, playerFleet.CurrentNodeId, StringComparison.Ordinal))
                {
                    TryFireTrigger(state, "FAUNA_DETECTED");
                    break;
                }
            }
        }

        // DEEP_EXPOSURE_MILD: accumulated deep exposure reaches mild threshold
        if (state.DeepExposure >= DeepDreadTweaksV0.ExposureMildThreshold)
        {
            TryFireTrigger(state, "DEEP_EXPOSURE_MILD");
        }

        // DEEP_EXPOSURE_HEAVY: accumulated deep exposure reaches heavy threshold
        if (state.DeepExposure >= DeepDreadTweaksV0.ExposureHeavyThreshold)
        {
            TryFireTrigger(state, "DEEP_EXPOSURE_HEAVY");
        }
    }

    // GATE.T57.CENTAUR.COMPETENCE_TIERS.001: Crisis-gated FO competence tier advancement.
    // Novice→Competent: 15+ trades, 5+ nodes visited, seen a warfront.
    // Competent→Master: 8+ systems, Haven discovered, endgame trigger reached.
    private static void EvaluateCompetenceTier(SimState state)
    {
        var fo = state.FirstOfficer;
        if (fo?.Competence is null) return;
        if (fo.Competence.PlayerDemoted) return; // Respect player demotion

        var comp = fo.Competence;
        var stats = state.PlayerStats;
        if (stats is null) return;

        if (comp.Tier == Entities.FOCompetenceTier.Novice)
        {
            bool hasTrades = stats.GoodsTraded >= CompetenceTweaksV0.CompetentMinTrades;
            bool hasNodes = stats.NodesVisited >= CompetenceTweaksV0.CompetentMinNodes;
            bool hasWarfront = false;
            if (CompetenceTweaksV0.CompetentRequiresWarfront)
            {
                foreach (var evt in fo.DialogueEventLog)
                {
                    if (string.Equals(evt.TriggerToken, "FIRST_DOCK_WARZONE", StringComparison.Ordinal))
                    { hasWarfront = true; break; }
                }
            }
            else
            {
                hasWarfront = true;
            }

            if (hasTrades && hasNodes && hasWarfront)
            {
                comp.Tier = Entities.FOCompetenceTier.Competent;
                comp.TierUpTick = state.Tick;
                TryFireTrigger(state, "FO_COMPETENCE_TIER_UP");
            }
        }
        else if (comp.Tier == Entities.FOCompetenceTier.Competent)
        {
            bool hasSystems = state.PlayerVisitedNodeIds.Count >= CompetenceTweaksV0.MasterMinSystems;
            bool hasHaven = !CompetenceTweaksV0.MasterRequiresHaven || (state.Haven != null && state.Haven.Discovered);
            bool hasEndgame = !CompetenceTweaksV0.MasterRequiresEndgameTrigger
                || fo.Tier >= Entities.DialogueTier.Endgame;

            if (hasSystems && hasHaven && hasEndgame)
            {
                comp.Tier = Entities.FOCompetenceTier.Master;
                comp.TierUpTick = state.Tick;
                TryFireTrigger(state, "FO_COMPETENCE_TIER_UP");
            }
        }
    }

    // GATE.T57.CENTAUR.COMPETENCE_TIERS.001: Allow player to demote FO (accessible from bridge).
    public static void DemoteFOCompetence(SimState state)
    {
        if (state?.FirstOfficer?.Competence is null) return;
        var comp = state.FirstOfficer.Competence;
        if (comp.Tier > Entities.FOCompetenceTier.Novice)
        {
            comp.Tier = (Entities.FOCompetenceTier)((int)comp.Tier - 1); // STRUCTURAL: decrement by 1
            comp.PlayerDemoted = true;
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

        fo.LastDialogueTick = state.Tick;
        fo.DecisionsSinceLastLine = 0; // GATE.T67.FO.SILENCE_DECISIONS.001: Reset decision counter.
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

        // GATE.T66.FO.DOCK_RELIABILITY.001: Retroactively fire FIRST_DOCK if player is docked.
        // TryFireTrigger requires IsPromoted=true, so FIRST_DOCK misses at actual first dock
        // (FO not yet selected). Fire it now so the FO greets the player immediately.
        Fleet? playerFleet = null;
        foreach (var f in state.Fleets.Values)
        {
            if (string.Equals(f.OwnerId, "player", StringComparison.Ordinal))
            { playerFleet = f; break; }
        }
        if (playerFleet != null && playerFleet.State == FleetState.Docked)
            TryFireTrigger(state, "FIRST_DOCK");

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

        Fleet? playerFleet = null;
        foreach (var f in state.Fleets.Values)
        {
            if (string.Equals(f.OwnerId, "player", StringComparison.Ordinal))
            { playerFleet = f; break; }
        }

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
                var scratch = s_scratch.GetOrCreateValue(state);
                var sortedGoods = scratch.SortedKeys;
                sortedGoods.Clear();
                foreach (var k in market.Inventory.Keys) sortedGoods.Add(k);
                sortedGoods.Sort(StringComparer.Ordinal);
                foreach (var goodKey in sortedGoods)
                {
                    if (market.Inventory.TryGetValue(goodKey, out var qty) && qty > 0) { goodName = goodKey; break; }
                }
            }
            text = text.Replace("{GOOD}", goodName);
        }

        // {DEST}: adjacent node name (first neighbor, deterministic: Ordinal sort on edge Id)
        if (text.Contains("{DEST}"))
        {
            string destName = "the next system";
            var scratchEdge = s_scratch.GetOrCreateValue(state);
            var sortedEdgeIds = scratchEdge.SortedKeys;
            sortedEdgeIds.Clear();
            foreach (var k in state.Edges.Keys) sortedEdgeIds.Add(k);
            sortedEdgeIds.Sort(StringComparer.Ordinal);
            foreach (var edgeId in sortedEdgeIds)
            {
                var edge = state.Edges[edgeId];
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

    private static bool HasPositiveOriginPhase(Fleet fleet)
    {
        if (fleet.CargoOriginPhase == null) return false;
        foreach (var v in fleet.CargoOriginPhase.Values)
        {
            if (v > 0) return true;
        }
        return false;
    }

    // GATE.T57.CENTAUR.WORLD_ADAPT.001: Scan trade routes for 5 world event types.
    // Adapts by flagging or pausing routes. FO never wrong — galaxy is unpredictable.
    private static void ProcessWorldAdaptation(SimState state)
    {
        if (state.Intel?.TradeRoutes is null || state.Intel.TradeRoutes.Count == 0) return;
        if (state.Tick % Tweaks.WorldAdaptTweaksV0.AdaptCadenceTicks != 0) return;

        foreach (var kvp in state.Intel.TradeRoutes)
        {
            var route = kvp.Value;
            if (route is null) continue;
            if (route.Status == TradeRouteStatus.Unprofitable) continue;

            var eventType = DetectWorldEvent(state, route);

            if (eventType == WorldAdaptEventType.None)
            {
                // If previously flagged/paused and conditions cleared, restore to Active.
                if (route.Status == TradeRouteStatus.Flagged || route.Status == TradeRouteStatus.Paused)
                    route.Status = TradeRouteStatus.Active;
                continue;
            }

            // Apply adaptation based on event type.
            var action = MapEventToAction(eventType);
            switch (action)
            {
                case AdaptationAction.FlagRoute:
                    if (route.Status != TradeRouteStatus.Paused)
                        route.Status = TradeRouteStatus.Flagged;
                    break;
                case AdaptationAction.PauseRoute:
                    route.Status = TradeRouteStatus.Paused;
                    break;
                case AdaptationAction.WidenSearch:
                case AdaptationAction.Reroute:
                    route.Status = TradeRouteStatus.Flagged;
                    break;
            }

            // Escalate flag → pause after threshold ticks.
            if (route.Status == TradeRouteStatus.Flagged)
            {
                int ageSinceValidated = state.Tick - route.LastValidatedTick;
                if (ageSinceValidated > Tweaks.WorldAdaptTweaksV0.FlagToPauseEscalationTicks)
                    route.Status = TradeRouteStatus.Paused;
            }
        }
    }

    // GATE.T57.CENTAUR.WORLD_ADAPT.001: Detect which (if any) world event affects this route.
    // Priority order: FactionConflict > TariffImposed > MarketShift > PlayerOverlap > IntelAged.
    private static WorldAdaptEventType DetectWorldEvent(SimState state, TradeRouteIntel route)
    {
        // 1. FactionConflict: route's good is under an active embargo.
        if (state.Embargoes is not null)
        {
            foreach (var embargo in state.Embargoes)
            {
                if (embargo is null) continue;
                if (string.Equals(embargo.GoodId, route.GoodId, StringComparison.Ordinal))
                    return WorldAdaptEventType.FactionConflict;
            }
        }

        // 2. TariffImposed: route endpoints are in a warfront zone (active conflict = implicit tariff).
        if (state.Warfronts is not null && state.Warfronts.Count > 0)
        {
            foreach (var wf in state.Warfronts.Values)
            {
                if (wf is null || wf.ContestedNodeIds is null) continue;
                if (wf.ContestedNodeIds.Contains(route.SourceNodeId)
                    || wf.ContestedNodeIds.Contains(route.DestNodeId))
                    return WorldAdaptEventType.TariffImposed;
            }
        }

        // 3. MarketShift: actual profit deviates significantly from estimated.
        if (route.EstimatedProfitPerUnit > 0)
        {
            int actualProfit = 0; // STRUCTURAL: default
            if (state.Markets.TryGetValue(route.SourceNodeId, out var srcMkt)
                && state.Markets.TryGetValue(route.DestNodeId, out var dstMkt))
            {
                int buy = srcMkt.GetPrice(route.GoodId);
                int sell = dstMkt.GetPrice(route.GoodId);
                if (buy > 0 && sell > 0) actualProfit = sell - buy;
            }
            int diff = route.EstimatedProfitPerUnit - actualProfit;
            if (diff < 0) diff = -diff;
            int deviationPct = (diff * 100) / route.EstimatedProfitPerUnit; // STRUCTURAL: pct calc
            if (deviationPct >= Tweaks.WorldAdaptTweaksV0.MarketShiftThresholdPct)
                return WorldAdaptEventType.MarketShift;
        }

        // 4. PlayerOverlap: NPC margin compression on this route.
        {
            int compressionBps = NpcTradeSystem.GetNpcMarginCompressionBps(state, route.SourceNodeId, route.DestNodeId, route.GoodId);
            if (compressionBps >= Tweaks.WorldAdaptTweaksV0.PlayerOverlapCompressionBps)
                return WorldAdaptEventType.PlayerOverlap;
        }

        // 5. IntelAged: route intel is old relative to freshness window.
        int routeAge = state.Tick - route.LastValidatedTick;
        if (route.ConfidenceScore > 0) // STRUCTURAL: only check if confidence was computed
        {
            // Use EconomicIntel freshness if available, else default 200 ticks.
            int freshnessWindow = 200; // STRUCTURAL: default window
            int ageThreshold = (freshnessWindow * Tweaks.WorldAdaptTweaksV0.IntelAgedThresholdPct) / 100; // STRUCTURAL: pct calc
            if (routeAge > ageThreshold)
                return WorldAdaptEventType.IntelAged;
        }

        return WorldAdaptEventType.None;
    }

    // GATE.T57.CENTAUR.WORLD_ADAPT.001: Map world event type to adaptation action.
    private static AdaptationAction MapEventToAction(WorldAdaptEventType eventType)
    {
        return eventType switch
        {
            WorldAdaptEventType.TariffImposed => AdaptationAction.PauseRoute,
            WorldAdaptEventType.FactionConflict => AdaptationAction.PauseRoute,
            WorldAdaptEventType.MarketShift => AdaptationAction.FlagRoute,
            WorldAdaptEventType.PlayerOverlap => AdaptationAction.WidenSearch,
            WorldAdaptEventType.IntelAged => AdaptationAction.FlagRoute,
            _ => AdaptationAction.None
        };
    }

    // GATE.T58.FO.LOA_MODEL.001: Remove expired route revert entries past the revert window.
    private static void CleanupRevertEntries(SimState state)
    {
        var loa = state.FirstOfficer?.LOA;
        if (loa is null || loa.RevertEntries.Count == 0) return;

        int expireThreshold = state.Tick - Tweaks.FOManagerTweaksV0.RouteRevertWindowTicks;
        for (int i = loa.RevertEntries.Count - 1; i >= 0; i--) // STRUCTURAL: reverse iterate for removal
        {
            if (loa.RevertEntries[i].ActionTick < expireThreshold)
                loa.RevertEntries.RemoveAt(i);
        }
    }

    // Silence fallback: fire ambient commentary when FO has been silent too long.
    // Uses numbered SILENCE_BREAK_N tokens (one-shot each). Ensures the companion
    // never disappears for 100+ ticks even when no state-based triggers are reachable.
    private static void ProcessSilenceFallback(SimState state)
    {
        if (state.Tick % NarrativeTweaksV0.SilenceFallbackCheckCadence != 0) return;
        var fo = state.FirstOfficer;
        if (fo is null || !fo.IsPromoted) return;

        // Initialize LastDialogueTick to PromotionTick if never spoken.
        if (fo.LastDialogueTick <= 0)
            fo.LastDialogueTick = fo.PromotionTick;

        int silenceDuration = state.Tick - fo.LastDialogueTick;
        bool tickSilent = silenceDuration >= NarrativeTweaksV0.SilenceFallbackThresholdTicks;
        // GATE.T67.FO.SILENCE_DECISIONS.001: Decision-based silence — fire if player has made
        // 25+ decisions without hearing from FO, regardless of tick count.
        bool decisionSilent = fo.DecisionsSinceLastLine >= NarrativeTweaksV0.SilenceDecisionThreshold;
        if (!tickSilent && !decisionSilent) return;

        // GATE.T66.FO.SILENCE_FILL.001: Find the next unfired silence break token.
        // Hades priority-bucket pattern: cycle through tokens, recycling when all consumed.
        for (int n = 1; n <= NarrativeTweaksV0.SilenceBreakMaxCount; n++)
        {
            string token = $"SILENCE_BREAK_{n}";
            bool alreadyFired = false;
            foreach (var evt in fo.DialogueEventLog)
            {
                if (string.Equals(evt.TriggerToken, token, StringComparison.Ordinal))
                { alreadyFired = true; break; }
            }
            if (!alreadyFired)
            {
                TryFireTrigger(state, token);
                return; // Fire at most one per check.
            }
        }

        // GATE.T66.FO.SILENCE_FILL.001: All tokens consumed — recycle by clearing
        // silence break entries from the dialogue log. Tokens fire again in sequence.
        // Subnautica pattern: background timer forces micro-events when silent too long.
        fo.DialogueEventLog.RemoveAll(evt =>
            evt.TriggerToken != null && evt.TriggerToken.StartsWith("SILENCE_BREAK_", StringComparison.Ordinal));
        // Also recycle ambient observations for longer sessions.
        fo.DialogueEventLog.RemoveAll(evt =>
            evt.TriggerToken != null && evt.TriggerToken.StartsWith("AMBIENT_OBS_", StringComparison.Ordinal));
        // Fire the first recycled token immediately.
        TryFireTrigger(state, "SILENCE_BREAK_1");
    }

    // GATE.T60.FO.PACING_HEARTBEAT.001: Ambient observations on 200-tick cadence.
    // Unlike SILENCE_BREAK (one-shot), these cycle through AMBIENT_OBS_1..N and
    // provide market trends, faction activity, and route tips. Fire only if FO
    // has been silent for HeartbeatSilenceMinTicks.
    private static void ProcessPacingHeartbeat(SimState state)
    {
        if (state.Tick % NarrativeTweaksV0.HeartbeatCadenceTicks != 0) return;
        var fo = state.FirstOfficer;
        if (fo is null || !fo.IsPromoted) return;

        int silenceDuration = state.Tick - fo.LastDialogueTick;
        if (silenceDuration < NarrativeTweaksV0.HeartbeatSilenceMinTicks) return;

        // Cycle through AMBIENT_OBS_1..N based on tick.
        int index = (state.Tick / NarrativeTweaksV0.HeartbeatCadenceTicks) % NarrativeTweaksV0.AmbientObsMaxCount + 1; // STRUCTURAL: 1-based
        string token = $"AMBIENT_OBS_{index}";

        // Ambient observations are one-shot like other triggers (logged in DialogueEventLog).
        TryFireTrigger(state, token);
    }

    // GATE.T64.FO.AMBIENT_TRIGGERS.001: Condition-based ambient triggers.
    // Three types: MARKET_OPPORTUNITY, TERRITORY_ENTRY, REWARD_MILESTONE.
    // Each cycles through 3 variants (one-shot per variant). Fire at most 1 per check.
    private static void ProcessAmbientConditionTriggers(SimState state)
    {
        if (NarrativeTweaksV0.AmbientCondCheckTicks <= 0) return;
        if (state.Tick % NarrativeTweaksV0.AmbientCondCheckTicks != 0) return;
        var fo = state.FirstOfficer;
        if (fo is null || !fo.IsPromoted) return;

        int silenceDuration = state.Tick - fo.LastDialogueTick;
        if (silenceDuration < NarrativeTweaksV0.AmbientCondSilenceMinTicks) return;

        // 1. REWARD_MILESTONE: credit thresholds (deterministic, easy to check).
        if (state.PlayerStats != null)
        {
            long earned = state.PlayerStats.TotalCreditsEarned;
            if (earned >= NarrativeTweaksV0.RewardMilestone3)
                { if (TryFireTrigger(state, "REWARD_MILESTONE_3").Length > 0) return; }
            else if (earned >= NarrativeTweaksV0.RewardMilestone2)
                { if (TryFireTrigger(state, "REWARD_MILESTONE_2").Length > 0) return; }
            else if (earned >= NarrativeTweaksV0.RewardMilestone1)
                { if (TryFireTrigger(state, "REWARD_MILESTONE_1").Length > 0) return; }
        }

        // 2. TERRITORY_ENTRY: fire when player is at a node and has arrivals this tick.
        // Cycle through variants based on visit count.
        if (state.ArrivalsThisTick.Count > 0 && state.PlayerStats != null)
        {
            int visitIdx = (state.PlayerStats.NodesVisited % NarrativeTweaksV0.AmbientCondMaxPerType) + 1; // STRUCTURAL: 1-based
            if (TryFireTrigger(state, $"TERRITORY_ENTRY_{visitIdx}").Length > 0) return;
        }

        // 3. MARKET_OPPORTUNITY: check if any good at current market has high margin at neighbor.
        if (state.Fleets.TryGetValue("fleet_trader_1", out var pf)
            && !string.IsNullOrEmpty(pf.CurrentNodeId)
            && state.Markets.TryGetValue(pf.CurrentNodeId, out var curMkt))
        {
            bool found = false;
            foreach (var edge in state.Edges.Values)
            {
                string? neighborId = null;
                if (string.Equals(edge.FromNodeId, pf.CurrentNodeId, StringComparison.Ordinal))
                    neighborId = edge.ToNodeId;
                else if (string.Equals(edge.ToNodeId, pf.CurrentNodeId, StringComparison.Ordinal))
                    neighborId = edge.FromNodeId;
                if (neighborId == null) continue;
                if (!state.Markets.TryGetValue(neighborId, out var nMkt)) continue;

                foreach (var goodId in curMkt.Inventory.Keys)
                {
                    if (!nMkt.Inventory.ContainsKey(goodId)) continue;
                    int buyHere = curMkt.GetBuyPrice(goodId);
                    int sellThere = nMkt.GetSellPrice(goodId);
                    if (sellThere - buyHere >= NarrativeTweaksV0.MarketOpportunityMinMargin)
                    { found = true; break; }
                }
                if (found) break;
            }
            if (found)
            {
                int mktIdx = (state.Tick / NarrativeTweaksV0.AmbientCondCheckTicks) % NarrativeTweaksV0.AmbientCondMaxPerType + 1; // STRUCTURAL: 1-based
                TryFireTrigger(state, $"MARKET_OPPORTUNITY_{mktIdx}");
            }
        }
    }

    // GATE.T57.CENTAUR.BOREDOM_TRIGGERS.001: 5 circuit breakers to nudge stagnating players.
    // Each fires a unique FO trigger token. Once-per-trigger semantics via DialogueEventLog.
    private static void ProcessBoredomTriggers(SimState state)
    {
        if (state.Tick % Tweaks.BoredomTriggerTweaksV0.BoredomCheckCadenceTicks != 0) return;
        if (state.FirstOfficer is null || !state.FirstOfficer.IsPromoted) return;

        // 1. No discovery for N ticks.
        int lastDiscoveryTick = FindLastDiscoveryTick(state);
        if (lastDiscoveryTick >= 0 && (state.Tick - lastDiscoveryTick) > Tweaks.BoredomTriggerTweaksV0.NoDiscoveryThresholdTicks)
            TryFireTrigger(state, "BOREDOM_NO_DISCOVERY");

        // 2. Margin compression on 3+ routes.
        int compressedCount = 0; // STRUCTURAL: counter start
        if (state.Intel?.TradeRoutes is not null)
        {
            foreach (var kvp in state.Intel.TradeRoutes)
            {
                var route = kvp.Value;
                if (route is null || route.Status == TradeRouteStatus.Unprofitable) continue;
                int bps = NpcTradeSystem.GetNpcMarginCompressionBps(state, route.SourceNodeId, route.DestNodeId, route.GoodId);
                if (bps >= Tweaks.BoredomTriggerTweaksV0.CompressedRouteBpsThreshold)
                    compressedCount++;
            }
        }
        if (compressedCount >= Tweaks.BoredomTriggerTweaksV0.CompressedRoutesThreshold)
            TryFireTrigger(state, "BOREDOM_MARGIN_COMPRESSED");

        // 3. Sustain/passive programs dominating the program book (ratio of types).
        if (state.Programs?.Instances is not null && state.Programs.Instances.Count > 0)
        {
            int totalProgs = 0; // STRUCTURAL: counter start
            int sustainProgs = 0; // STRUCTURAL: counter start
            foreach (var prog in state.Programs.Instances.Values)
            {
                if (prog is null || prog.Status != Programs.ProgramStatus.Running) continue;
                totalProgs++;
                if (string.Equals(prog.Kind, "RESOURCE_TAP_V0", StringComparison.Ordinal)
                    || string.Equals(prog.Kind, "AUTO_BUY", StringComparison.Ordinal))
                    sustainProgs++;
            }
            if (totalProgs > 0)
            {
                int sustainPct = (sustainProgs * 100) / totalProgs; // STRUCTURAL: pct calc
                if (sustainPct > Tweaks.BoredomTriggerTweaksV0.SustainRevenueThresholdPct)
                    TryFireTrigger(state, "BOREDOM_SUSTAIN_DOMINANT");
            }
        }

        // 4. Chain intel breadcrumb stale (active chain with next step not pursued).
        if (state.AnomalyChains is not null)
        {
            foreach (var chain in state.AnomalyChains.Values)
            {
                if (chain is null || chain.Status != AnomalyChainStatus.Active) continue;
                if (chain.CurrentStepIndex >= chain.Steps.Count) continue;
                // The chain hasn't advanced — check how long since last step.
                int chainAge = state.Tick - chain.StartedTick;
                if (chainAge > Tweaks.BoredomTriggerTweaksV0.ChainIntelStaleThresholdTicks)
                {
                    TryFireTrigger(state, "BOREDOM_CHAIN_STALE");
                    break;
                }
            }
        }

        // 5. Long time since last revelation (any Analyzed discovery).
        int lastAnalyzedTick = FindLastAnalyzedDiscoveryTick(state);
        if (lastAnalyzedTick >= 0 && (state.Tick - lastAnalyzedTick) > Tweaks.BoredomTriggerTweaksV0.SinceRevelationThresholdTicks)
            TryFireTrigger(state, "BOREDOM_NO_REVELATION");
    }

    private static int FindLastDiscoveryTick(SimState state)
    {
        int latest = -1; // STRUCTURAL: sentinel
        if (state.Intel?.Discoveries is null) return latest;
        foreach (var disc in state.Intel.Discoveries.Values)
        {
            if (disc is null) continue;
            // Use CreatedTick if available — discovery phase progression implies tick.
            // Approximate: any Seen discovery means a discovery happened. Use current tick as proxy.
            if (disc.Phase >= DiscoveryPhase.Seen)
            {
                // No CreatedTick field on DiscoveryStateV0, so scan EconomicIntels for timing.
                if (state.Intel.EconomicIntels is not null)
                {
                    string intelKey = "ECON_" + disc.DiscoveryId;
                    if (state.Intel.EconomicIntels.TryGetValue(intelKey, out var econ) && econ.CreatedTick > latest)
                        latest = econ.CreatedTick;
                }
            }
        }
        return latest;
    }

    private static int FindLastAnalyzedDiscoveryTick(SimState state)
    {
        int latest = -1; // STRUCTURAL: sentinel
        if (state.Intel?.EconomicIntels is null) return latest;
        foreach (var econ in state.Intel.EconomicIntels.Values)
        {
            if (econ is null) continue;
            if (econ.CreatedTick > latest)
                latest = econ.CreatedTick;
        }
        return latest;
    }
}
