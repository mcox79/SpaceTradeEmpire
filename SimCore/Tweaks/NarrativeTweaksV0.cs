namespace SimCore.Tweaks;

// GATE.T18.NARRATIVE.TWEAKS.001: Gameplay constants for narrative systems.
public static class NarrativeTweaksV0
{
    // GATE.T41.PACING.MOMENT_SPACING.001: First Officer promotion window (tick range).
    // fh_4: Companion moment fires too late (tick 60), compressing moments 2-4 into 30 ticks.
    // Reduced min from 50→25 so FO promotes at first dock (~tick 25-30), creating 30+ decision
    // gap before Danger (~tick 60) and 60+ gap before Power (~tick 120+).
    public const int FOPromotionMinTick = 25;
    public const int FOPromotionMaxTick = 150;

    // Dialogue tier tick thresholds.
    public const int TierMidTick = 300;
    public const int TierFractureTick = 600;
    public const int TierRevelationTick = 1000;
    public const int TierEndgameTick = 1500;

    // GATE.T18.CHARACTER.FO_TRIGGER_WIRING.001: Score-based tier advancement thresholds.
    // Active play can advance tiers earlier than tick thresholds.
    public const int ScoreTierMid = 8;
    public const int ScoreTierFracture = 18;
    public const int ScoreTierRevelation = 30;
    public const int ScoreTierEndgame = 45;

    // GATE.T18.CHARACTER.FO_PROMO.001: Score threshold for early promotion window.
    public const int FOPromotionScoreThreshold = 5;

    // War consequence delay before downstream effect resolves.
    public const int WarConsequenceDelayTicks = 100;

    // Maximum station memory records tracked (prevents unbounded growth).
    public const int StationMemoryMaxRecords = 500;

    // Stationmaster: minimum deliveries before "you're reliable" line.
    public const int StationmasterReliableThreshold = 5;

    // Regular NPC: minimum overlapping route nodes with player.
    public const int RegularOverlapMinNodes = 2;

    // Regular NPC: ticks after war reaches home before vanish bulletin appears.
    public const int RegularVanishDelayTicks = 20;

    // GATE.T18.CHARACTER.FO_TRIGGER_WIRING.001: Trigger thresholds.
    // Completed missions needed for SUPPLY_CHAIN_NOTICED trigger.
    public const int SupplyChainNoticedMissions = 3;
    // Revealed knowledge connections needed for KNOWLEDGE_WEB_INSIGHT trigger.
    public const int KnowledgeWebInsightConnections = 3;
    // GATE.T42.PLANET_SCAN.FO.001: Scans with same mode needed for PATTERN_RECOGNIZED trigger.
    public const int PatternRecognizedScanCount = 5;
    // Valorin reputation threshold for Enemy recontextualization variant.
    public const int EnemyRecontextValorinRepThreshold = 20;

    // GATE.S19.ONBOARD.FO_TRIGGERS.003: Nodes visited threshold for ARRIVAL_NEW_SYSTEM trigger.
    public const int ArrivalNewSystemNodes = 2;

    // Knowledge web seed: number of discoveries auto-advanced to Analyzed near player start.
    public const int KnowledgeWebSeedCount = 8;

    // COSTS_MOUNTING trigger: minimum credits earned before FO comments on operating costs.
    public const int CostsMountingCreditsEarned = 50;
    // COSTS_MOUNTING trigger: minimum nodes visited before FO comments on operating costs.
    public const int CostsMountingNodesVisited = 3;

    // GATE.T41.FO.SILENCE_FALLBACK.001 + GATE.T66.FO.SILENCE_FILL.001: Decision-count silence cap.
    // GATE.T66: Reduced from 50→30 ticks. fh_8: max_silence=261 despite T60+T64 content.
    // Hades pattern: FO bucket always has at least one eligible item (recycling tokens).
    // fh_14: Reduced 30→15 ticks. Headless bots showed 135-282 decision silence gaps.
    // With 15 ticks and cadence 5, FO checks 3x per threshold window.
    public const int SilenceFallbackThresholdTicks = 15;
    // Maximum number of silence break triggers (cycling SILENCE_BREAK_1..N).
    // GATE.T68.FO.AMBIENT_EXPAND.001: Expanded from 10→20 to use full content library.
    public const int SilenceBreakMaxCount = 20;
    // Cadence for checking silence fallback (every N ticks).
    // fh_14: Reduced 10→5 for more responsive silence detection.
    public const int SilenceFallbackCheckCadence = 5;

    // fh_14: Reduced from 15→10. VFY bot streak=10, experience bot streak=86.
    // 10 consecutive same actions is enough for FO to comment on the pattern.
    // Hades injects at ~10-15 repetitions; 10 feels more responsive.
    public const int MonotoneStreakThreshold = 10;
    // GATE.T68.FO.AMBIENT_EXPAND.001: Expanded from 6→10 for more variety.
    public const int MonotoneStreakMaxCount = 10;

    // fh_14: Reduced from 20→15. With FO commentary at 10, market perturbation at 15
    // provides a two-stage escalation: first FO warns, then market shifts if player persists.
    public const int EventInterruptStreakThreshold = 15;
    // Price shift magnitude in bps applied to the good being traded monotonously.
    // 4000 bps = 40% price shift — enough to make the route clearly suboptimal.
    public const int EventInterruptPriceShiftBps = 4000;

    // GATE.T67.FO.SILENCE_DECISIONS.001: Decision-based silence fallback.
    // After N player decisions with no FO line, fire an ambient observation.
    // Research: Valve Response System uses 15-20s hard floor; adapted for decision count.
    // fh_14: Reduced 25→15. Now that more commands increment the counter
    // (undock, combat, equip, scan), 15 decisions is ~3-4 trade cycles — appropriate pace.
    public const int SilenceDecisionThreshold = 15;

    // GATE.T64.FO.COMBAT_REACTION.001: Delayed FO reaction after combat win.
    // Fires 10 ticks after kill, cycling through COMBAT_REACTION_1..N.
    public const int CombatReactionDelayTicks = 10;
    public const int CombatReactionMaxCount = 5;

    // GATE.T41.FO.AMBIENT_CADENCE.001: Proactive FO triggers every 30-50 decisions post-d60.
    // Per fo_trade_manager_v0.md Law 3 + NarrativeDesign.md Principle 8: FO reacts to player
    // patterns (route repetition, high-margin arrival, milestones). fh_4 found 3 dead zones
    // totaling 479 decisions without positive feedback. Reduced cadence from 200→40 ticks,
    // silence min from 80→25, increased obs count from 8→20.
    public const int HeartbeatCadenceTicks = 40;
    public const int HeartbeatSilenceMinTicks = 25;
    // GATE.T68.FO.AMBIENT_EXPAND.001: Expanded from 12→24 for deeper content coverage.
    public const int AmbientObsMaxCount = 24;

    // GATE.T64.FO.AMBIENT_TRIGGERS.001: Condition-based ambient triggers (Hades grid pattern).
    // These fire on specific gameplay conditions, cycling through 3 variants per type.
    // GATE.T66.PACING.DEAD_ZONE_INJECT.001: Reduced cadence from 50→25 ticks.
    // Subnautica pattern: force micro-events when >15 decisions since last signal.
    // fh_8: 3 dead zones (d480-720), 240-decision plateaus. Need faster injection.
    public const int AmbientCondCheckTicks = 25;
    public const int AmbientCondSilenceMinTicks = 15;
    // GATE.T66: Raised per-type limit from 3→6 for longer sessions.
    public const int AmbientCondMaxPerType = 6;
    // MARKET_OPPORTUNITY: minimum margin (cr) at nearby node to trigger.
    public const int MarketOpportunityMinMargin = 50;
    // REWARD_MILESTONE: credit thresholds.
    public const int RewardMilestone1 = 1000;
    public const int RewardMilestone2 = 5000;
    public const int RewardMilestone3 = 10000;
}
