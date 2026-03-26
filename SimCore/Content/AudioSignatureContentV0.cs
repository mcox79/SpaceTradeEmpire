namespace SimCore.Content;

// GATE.T58.AUDIO.SIGNATURES.001: 11 audio signature definitions per fo_trade_manager_v0.md §Audio Vocabulary.
// Each signature has a unique cue ID used by the bridge and GDScript AudioBus.
// Layering rules: one alert-tier at a time, Route Heartbeat ducks during dialogue/combat,
// discovery sounds layer freely with heartbeat.
public static class AudioSignatureContentV0
{
    // ── Exploration signatures ──
    public const string AnomalyPing = "anomaly_ping";           // Scanner detects anomaly — sharp metallic 2-note
    public const string ScanProcess = "scan_process";           // Active scan in progress — low rhythmic pulse
    public const string DiscoveryReveal = "discovery_reveal";   // Scan completes — 3-note ascending chime (C-E-G)

    // ── Knowledge Graph signatures ──
    public const string InsightChime = "insight_chime";         // KG link confirmed (Obra Dinn batch) — glass bell
    public const string BatchInsight = "batch_insight";         // 3+ links confirmed — cascade chime (low-mid-high)
    public const string RevelationFanfare = "revelation_fanfare"; // KG revelation fires (R1/R3/R5) — deep resonance

    // ── FO Communication signatures ──
    public const string FOCommOpen = "fo_comm_open";            // FO begins speaking — soft radio crackle
    public const string FODecisionTone = "fo_decision_tone";    // FO decision requiring input — two-note query

    // ── Empire signatures ──
    public const string RouteHeartbeat = "route_heartbeat";     // Background pulse (3+ routes) — belt-watching
    public const string AlertSting = "alert_sting";             // Health Degraded/Critical — sharp descending
    public const string FlipMomentFanfare = "flip_moment_fanfare"; // Net-positive crossover — 4-note brass, once/game

    // ── Audio bus categories ──
    public const string BusExploration = "Exploration";   // AnomalyPing, ScanProcess, DiscoveryReveal
    public const string BusKnowledge = "Knowledge";       // InsightChime, BatchInsight, RevelationFanfare
    public const string BusFOComm = "FOComm";             // FOCommOpen, FODecisionTone
    public const string BusEmpire = "Empire";             // RouteHeartbeat, AlertSting, FlipMomentFanfare

    // ── Audio priority tiers ──
    // Alert: one at a time, queue don't stack (AlertSting, FlipMomentFanfare, RevelationFanfare)
    // Ambient: layers freely, ducks during alert/dialogue (RouteHeartbeat)
    // Punctual: plays immediately, layers with ambient (AnomalyPing, InsightChime, etc.)
    public static bool IsAlertTier(string cueId) =>
        cueId == AlertSting || cueId == FlipMomentFanfare || cueId == RevelationFanfare;

    public static bool IsAmbientTier(string cueId) =>
        cueId == RouteHeartbeat;

    public static string GetBus(string cueId) => cueId switch
    {
        AnomalyPing or ScanProcess or DiscoveryReveal => BusExploration,
        InsightChime or BatchInsight or RevelationFanfare => BusKnowledge,
        FOCommOpen or FODecisionTone => BusFOComm,
        RouteHeartbeat or AlertSting or FlipMomentFanfare => BusEmpire,
        _ => BusEmpire
    };
}
