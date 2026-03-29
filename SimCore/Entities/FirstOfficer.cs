using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimCore.Entities;

// GATE.T18.NARRATIVE.FO_MODEL.001: First Officer candidate types.
public enum FirstOfficerCandidate
{
    None = 0,
    Analyst = 1,
    Veteran = 2,
    Pathfinder = 3
}

// GATE.T18.NARRATIVE.FO_MODEL.001: Dialogue tier progression (tier-gated triggers).
public enum DialogueTier
{
    Early = 0,       // tick 0-300
    Mid = 1,         // tick 300-600
    Fracture = 2,    // tick 600-1000
    Revelation = 3,  // tick 1000-1500
    Endgame = 4      // tick 1500+
}

// GATE.T18.NARRATIVE.FO_MODEL.001: Fired dialogue event record.
public sealed class DialogueEvent
{
    [JsonInclude] public string TriggerToken { get; set; } = "";
    [JsonInclude] public int FiredTick { get; set; }
}

// GATE.T57.CENTAUR.COMPETENCE_TIERS.001: FO competence tiers per fo_trade_manager_v0.md.
// Crisis-gated: Novice→Competent requires trades+nodes+warfront. Competent→Master requires Haven+systems+endgame.
// Player can demote (FO acknowledges and adapts). No visible XP bar — tier shown through FO personality language.
public enum FOCompetenceTier
{
    Novice = 0,      // Starting tier: basic trade suggestions, cautious recommendations
    Competent = 1,   // After ~15 trades, 5 nodes, warfront visit: confident multi-hop planning
    Master = 2       // After Haven, 8+ systems, endgame trigger: strategic automation architect
}

// GATE.T57.CENTAUR.COMPETENCE_TIERS.001: Competence state tracking for the centaur model.
// Growth is through crisis survival, not XP accumulation.
public sealed class FOCompetenceState
{
    [JsonInclude] public FOCompetenceTier Tier { get; set; } = FOCompetenceTier.Novice;
    [JsonInclude] public int TierUpTick { get; set; } // Tick when last tier-up occurred
    [JsonInclude] public bool PlayerDemoted { get; set; } // Player explicitly demoted FO
    // GATE.T57.CENTAUR.CONFIDENCE_LANG.001: Confidence score (0-100, personality-displayed).
    [JsonInclude] public int ConfidenceScore { get; set; } = 50;
}

// GATE.T18.NARRATIVE.FO_MODEL.001: First Officer companion state.
// Three candidates (Analyst, Veteran, Pathfinder). Player promotes one at tick 50-150.
// Action-triggered dialogue with tier gates. Each candidate has a blind spot
// that gets exposed through play.
public sealed class FirstOfficer
{
    [JsonInclude] public FirstOfficerCandidate CandidateType { get; set; } = FirstOfficerCandidate.None;
    [JsonInclude] public bool IsPromoted { get; set; }
    [JsonInclude] public int PromotionTick { get; set; }
    [JsonInclude] public DialogueTier Tier { get; set; } = DialogueTier.Early;

    // Relationship score: accumulated through shared experiences.
    [JsonInclude] public int RelationshipScore { get; set; }

    // Whether the FO's blind spot has been exposed by game events.
    [JsonInclude] public bool BlindSpotExposed { get; set; }

    // GATE.T57.CENTAUR.COMPETENCE_TIERS.001: FO trade competence state.
    [JsonInclude] public FOCompetenceState Competence { get; set; } = new();

    // Dialogue event log: which triggers have fired (prevents repeats).
    [JsonInclude] public List<DialogueEvent> DialogueEventLog { get; private set; } = new();

    // GATE.T58.FO.EMPIRE_HEALTH.001: Empire health diamond state.
    [JsonInclude] public EmpireHealthState EmpireHealth { get; set; } = new();

    // GATE.T58.FO.DOCK_RECAP.001: Dock recap ("While You Were Away") state.
    [JsonInclude] public DockRecapState DockRecap { get; set; } = new();

    // GATE.T58.FO.LOA_MODEL.001: Level of Autonomy table per domain.
    [JsonInclude] public LOATable LOA { get; set; } = new();

    // GATE.T58.FO.SERVICE_RECORD.001: FO service record (trust-building display).
    [JsonInclude] public FOServiceRecord ServiceRecord { get; set; } = new();

    // GATE.T58.FO.FLIP_MOMENT.001: Net-positive revenue flip moment state.
    [JsonInclude] public FlipMomentState FlipMoment { get; set; } = new();

    // GATE.T58.FO.DECISION_DIALOGUE.001: Decision dialogue queue and active state.
    [JsonInclude] public FODecision? ActiveDecision { get; set; }
    [JsonInclude] public List<FODecision> DecisionQueue { get; set; } = new();
    [JsonInclude] public List<FODecision> ResolvedDecisions { get; set; } = new();

    // Tick when FO last spoke (any trigger). Used by silence fallback to detect long gaps.
    [JsonInclude] public int LastDialogueTick { get; set; }

    // GATE.T67.FO.DOCK_GREETING.001: Deferred dock greeting flag.
    // Set when player first docks before FO is promoted. Fires greeting after promotion.
    [JsonInclude] public bool DeferredDockGreeting { get; set; }

    // GATE.T67.FO.SILENCE_DECISIONS.001: Decision counter for silence tracking.
    // Incremented each time the player takes an action (buy/sell/travel/dock).
    [JsonInclude] public int DecisionsSinceLastLine { get; set; }

    // Current pending dialogue line (set by system, consumed by bridge). Transient.
    [JsonIgnore] public string PendingDialogueLine { get; set; } = "";
    [JsonIgnore] public string PendingTriggerToken { get; set; } = "";
}

// GATE.T58.FO.EMPIRE_HEALTH.001: Empire health status per fo_trade_manager_v0.md §Empire Health Indicator.
// Healthy/Degraded/Critical diamond communicated via HUD color + pulse.
public enum EmpireHealthStatus
{
    None = 0,       // No managed routes yet — icon hidden
    Healthy = 1,    // All routes profitable, sustain adequate
    Degraded = 2,   // 1+ routes below margin OR sustain stock low
    Critical = 3    // 1+ routes dead OR sustain critical OR ship lost
}

// GATE.T58.FO.EMPIRE_HEALTH.001: Aggregated empire health state.
public sealed class EmpireHealthState
{
    [JsonInclude] public EmpireHealthStatus Status { get; set; } = EmpireHealthStatus.None;
    [JsonInclude] public EmpireHealthStatus PreviousStatus { get; set; } = EmpireHealthStatus.None;
    [JsonInclude] public int LastTransitionTick { get; set; }
    [JsonInclude] public int HealthyRoutes { get; set; }
    [JsonInclude] public int DegradedRoutes { get; set; }
    [JsonInclude] public int DeadRoutes { get; set; }
    [JsonInclude] public int TotalManagedRoutes { get; set; }
    [JsonInclude] public bool SustainLow { get; set; }
    [JsonInclude] public bool SustainCritical { get; set; }
    [JsonInclude] public bool ShipLost { get; set; }
}

// GATE.T58.FO.DOCK_RECAP.001: Dock recap state per fo_trade_manager_v0.md §Dock Arrival Recap.
// Generates "While You Were Away" summary when docking after 100+ ticks.
public sealed class DockRecapState
{
    [JsonInclude] public int LastDockTick { get; set; }
    [JsonInclude] public bool PendingRecap { get; set; }
    [JsonInclude] public List<string> RecapLines { get; set; } = new();
    // Aggregated data for recap generation
    [JsonInclude] public int TradesCompletedSinceLastDock { get; set; }
    [JsonInclude] public long CreditsEarnedSinceLastDock { get; set; }
    [JsonInclude] public string MostSevereIssue { get; set; } = "";
    [JsonInclude] public string BestOpportunity { get; set; } = "";
}

// GATE.T58.FO.LOA_MODEL.001: Level of Autonomy table per fo_trade_manager_v0.md.
// Sheridan & Verplank (1978) levels 4-7 adapted for entertainment context.
public enum LOADomain
{
    RouteCreation = 0,
    RouteOptimization = 1,
    SustainLogistics = 2,
    ShipPurchase = 3,
    WarfrontResponse = 4,
    Construction = 5
}

public sealed class LOATable
{
    // Per-domain LOA level (4-7). Key = (int)LOADomain. Defaults from FOManagerTweaksV0.
    [JsonInclude] public Dictionary<int, int> DomainLevels { get; set; } = new();

    // GATE.T58.FO.LOA_MODEL.001: Route revert state for LOA 6 auto-actions.
    [JsonInclude] public List<RouteRevertEntry> RevertEntries { get; set; } = new();

    /// <summary>Get LOA level for a domain. Returns default from tweaks if not explicitly set.</summary>
    public int GetLevel(LOADomain domain)
    {
        int key = (int)domain;
        if (DomainLevels.TryGetValue(key, out int level)) return level;

        // Return defaults from tweaks.
        return domain switch
        {
            LOADomain.RouteCreation => Tweaks.FOManagerTweaksV0.LOARouteCreation,
            LOADomain.RouteOptimization => Tweaks.FOManagerTweaksV0.LOARouteOptimization,
            LOADomain.SustainLogistics => Tweaks.FOManagerTweaksV0.LOASustainLogistics,
            LOADomain.ShipPurchase => Tweaks.FOManagerTweaksV0.LOAShipPurchase,
            LOADomain.WarfrontResponse => Tweaks.FOManagerTweaksV0.LOAWarfrontResponse,
            LOADomain.Construction => Tweaks.FOManagerTweaksV0.LOAConstruction,
            _ => 5 // STRUCTURAL: safe default
        };
    }

    /// <summary>Set LOA level for a domain. Clamps to 4-7 range.</summary>
    public void SetLevel(LOADomain domain, int level)
    {
        int clamped = level < 4 ? 4 : level > 7 ? 7 : level; // STRUCTURAL: LOA range
        DomainLevels[(int)domain] = clamped;
    }
}

// GATE.T58.FO.LOA_MODEL.001: Stored previous route config for revert within time window.
public sealed class RouteRevertEntry
{
    [JsonInclude] public string RouteId { get; set; } = "";
    [JsonInclude] public int ActionTick { get; set; }
    [JsonInclude] public string PreviousSourceNodeId { get; set; } = "";
    [JsonInclude] public string PreviousDestNodeId { get; set; } = "";
    [JsonInclude] public string PreviousGoodId { get; set; } = "";
}

// GATE.T58.FO.SERVICE_RECORD.001: FO service record per fo_trade_manager_v0.md §FO Service Record.
// Trust-building display: factual, clinical, includes worst call.
public sealed class FOServiceRecord
{
    [JsonInclude] public int RoutesManaged { get; set; }
    [JsonInclude] public int RecommendationsTaken { get; set; }
    [JsonInclude] public int RecommendationsOffered { get; set; }
    [JsonInclude] public int ProfitableRecommendations { get; set; }
    [JsonInclude] public int CrisesHandled { get; set; }
    [JsonInclude] public List<ServiceRecordEntry> History { get; set; } = new();
    // Worst call tracking
    [JsonInclude] public string WorstCallDescription { get; set; } = "";
    [JsonInclude] public long WorstCallCost { get; set; }
    // Notable achievement
    [JsonInclude] public string NotableDescription { get; set; } = "";
}

// GATE.T58.FO.SERVICE_RECORD.001: Single service record event.
public sealed class ServiceRecordEntry
{
    [JsonInclude] public int Tick { get; set; }
    [JsonInclude] public string EventType { get; set; } = ""; // "recommendation", "crisis", "reroute", "notable"
    [JsonInclude] public string Description { get; set; } = "";
    [JsonInclude] public long CreditImpact { get; set; }
    [JsonInclude] public bool WasSuccessful { get; set; }
}

// GATE.T58.FO.FLIP_MOMENT.001: Flip moment state per fo_trade_manager_v0.md §The Flip Moment.
// Detects net-negative → net-positive revenue transition.
public sealed class FlipMomentState
{
    [JsonInclude] public bool HasFlipped { get; set; }           // True once, never resets
    [JsonInclude] public int FlipTick { get; set; }              // Tick when flip occurred
    [JsonInclude] public int ConsecutivePositiveTicks { get; set; } // Counter for sustained positive
    [JsonInclude] public long LastTickNetRevenue { get; set; }   // Most recent tick's net revenue
    [JsonInclude] public bool FlipEventPending { get; set; }     // Bridge consumes this for VFX/audio
}

// GATE.T58.FO.DECISION_DIALOGUE.001: Decision dialogue types.
public enum DecisionType
{
    Crisis = 0,         // Warfront/embargo escalation
    FleetDisposition = 1, // Ship purchase/assignment
    Construction = 2,    // Construction project proposal
    RouteProposal = 3,  // New route or reroute recommendation
    Warfront = 4        // Warfront engagement choice
}

// GATE.T58.FO.DECISION_DIALOGUE.001: Decision status lifecycle.
public enum DecisionStatus
{
    Queued = 0,
    AwaitingPlayer = 1,
    Resolved = 2,
    Expired = 3
}

// GATE.T58.FO.DECISION_DIALOGUE.001: Single option within a decision dialogue.
public sealed class DecisionOption
{
    [JsonInclude] public string Label { get; set; } = "";          // Short action text
    [JsonInclude] public string Description { get; set; } = "";    // Consequence description
    [JsonInclude] public long CreditImpact { get; set; }           // Estimated credit effect
    [JsonInclude] public int RiskLevel { get; set; }               // 0=safe, 1=moderate, 2=high
    [JsonInclude] public int ExplorationValue { get; set; }        // Exploration opportunity score
    [JsonInclude] public string ConsequenceColor { get; set; } = ""; // "green", "amber", "red"
}

// GATE.T58.FO.DECISION_DIALOGUE.001: Complete decision dialogue.
// Structure: Situation → Stakes → Options (Rule 3).
public sealed class FODecision
{
    [JsonInclude] public string DecisionId { get; set; } = "";
    [JsonInclude] public DecisionType Type { get; set; } = DecisionType.Crisis;
    [JsonInclude] public DecisionStatus Status { get; set; } = DecisionStatus.Queued;
    [JsonInclude] public int Severity { get; set; }                // Higher = more urgent (queue priority)
    [JsonInclude] public string Situation { get; set; } = "";      // Rule 3: context first
    [JsonInclude] public string Stakes { get; set; } = "";         // Rule 3: what's at risk
    [JsonInclude] public List<DecisionOption> Options { get; set; } = new(); // Rule 2: all visible
    [JsonInclude] public int RecommendedOptionIndex { get; set; } = -1; // Rule 1: FO recommendation
    [JsonInclude] public int SelectedOptionIndex { get; set; } = -1;
    [JsonInclude] public int PresentedTick { get; set; }
    [JsonInclude] public int ResolvedTick { get; set; }
}
