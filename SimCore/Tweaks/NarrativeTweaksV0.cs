namespace SimCore.Tweaks;

// GATE.T18.NARRATIVE.TWEAKS.001: Gameplay constants for narrative systems.
public static class NarrativeTweaksV0
{
    // First Officer promotion window (tick range).
    public const int FOPromotionMinTick = 50;
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
}
