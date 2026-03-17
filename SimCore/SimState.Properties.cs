using System.Text;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using SimCore.Entities;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Globalization;
using SimCore.Intents;
using SimCore.Programs;
using SimCore.Tweaks;

namespace SimCore;

public partial class SimState
{
    // GATE.X.TWEAKS.DATA.001
    // Minimal v0 tweak config model.
    // Extend over time as Slice 2.5 / Slice 3 systems are migrated off hardcoded constants.
    public sealed class TweakConfigV0
    {
        // Schema contract v0
        // - Canonical JSON is order-fixed and append-only: new fields may only be added to the END of CanonicalFieldOrderV0
        // - Canonical JSON must be ASCII-safe and locale-independent (use invariant formatting for floating point)
        // - Hash definition is locked: SHA256(UTF-8 canonical JSON) rendered as uppercase hex
        public const int CurrentVersion = 0;

        // Canonical JSON field order (append-only). Do NOT reorder existing entries.
        public static readonly string[] CanonicalFieldOrderV0 = new[]
        {
            "version",
            "worldgen_min_producers_per_good",
            "worldgen_min_sinks_per_good",
            "default_lane_capacity_k",
            "market_fee_multiplier",
            "risk_scalar",
            "loop_viability_threshold",
            "role_risk_tolerance_default",
        };

        // Stable defaults (explicitly documented as part of the v0 contract).
        public const int DefaultWorldgenMinProducersPerGood = 1;
        public const int DefaultWorldgenMinSinksPerGood = 0;
        public const int DefaultLaneCapacityKValue = 0; // <= 0 means unlimited (matches prior LaneFlowSystem behavior for TotalCapacity <= 0)

        public const double DefaultMarketFeeMultiplier = 1.0;
        public const double DefaultRiskScalar = 1.0;
        public const double DefaultLoopViabilityThreshold = 0.0;
        public const double DefaultRoleRiskToleranceDefault = 1.0;

        public int Version { get; set; } = CurrentVersion;

        // Example knobs (placeholder v0 surface area).
        public int WorldgenMinProducersPerGood { get; set; } = DefaultWorldgenMinProducersPerGood;
        public int WorldgenMinSinksPerGood { get; set; } = DefaultWorldgenMinSinksPerGood;

        // TotalCapacity <= 0 means "unspecified" at the edge level.
        // DefaultLaneCapacityK <= 0 means "unlimited" (preserves pre-migration default behavior).
        public int DefaultLaneCapacityK { get; set; } = DefaultLaneCapacityKValue;

        public double MarketFeeMultiplier { get; set; } = DefaultMarketFeeMultiplier;
        public double RiskScalar { get; set; } = DefaultRiskScalar;
        public double LoopViabilityThreshold { get; set; } = DefaultLoopViabilityThreshold;
        public double RoleRiskToleranceDefault { get; set; } = DefaultRoleRiskToleranceDefault;

        public static TweakConfigV0 CreateDefaults() => new TweakConfigV0
        {
            Version = CurrentVersion,
            WorldgenMinProducersPerGood = DefaultWorldgenMinProducersPerGood,
            WorldgenMinSinksPerGood = DefaultWorldgenMinSinksPerGood,
            DefaultLaneCapacityK = DefaultLaneCapacityKValue,
            MarketFeeMultiplier = DefaultMarketFeeMultiplier,
            RiskScalar = DefaultRiskScalar,
            LoopViabilityThreshold = DefaultLoopViabilityThreshold,
            RoleRiskToleranceDefault = DefaultRoleRiskToleranceDefault
        };

        public string ToCanonicalJson()
        {
            // Canonical, order-fixed JSON for stable hashing across platforms and whitespace differences.
            // Append-only ordering rule: add new fields ONLY by appending at the end (see CanonicalFieldOrderV0).
            var sb = new StringBuilder(256);
            sb.Append('{');
            sb.Append("\"version\":").Append(Version).Append(',');
            sb.Append("\"worldgen_min_producers_per_good\":").Append(WorldgenMinProducersPerGood).Append(',');
            sb.Append("\"worldgen_min_sinks_per_good\":").Append(WorldgenMinSinksPerGood).Append(',');
            sb.Append("\"default_lane_capacity_k\":").Append(DefaultLaneCapacityK).Append(',');
            sb.Append("\"market_fee_multiplier\":").Append(MarketFeeMultiplier.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"risk_scalar\":").Append(RiskScalar.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"loop_viability_threshold\":").Append(LoopViabilityThreshold.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"role_risk_tolerance_default\":").Append(RoleRiskToleranceDefault.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            sb.Append('}');
            return sb.ToString();
        }

        public string ToCanonicalHashUpperHex()
        {
            var canonical = ToCanonicalJson();
            var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonical));
            return Convert.ToHexString(hash);
        }

        public static TweakConfigV0 ParseJsonOrDefaults(string? json)
        {
            var cfg = CreateDefaults();
            if (string.IsNullOrWhiteSpace(json)) return cfg;

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != System.Text.Json.JsonValueKind.Object) return cfg;

                if (root.TryGetProperty("version", out var v) && v.ValueKind == System.Text.Json.JsonValueKind.Number)
                    cfg.Version = v.GetInt32();

                if (root.TryGetProperty("worldgen_min_producers_per_good", out var p) && p.ValueKind == System.Text.Json.JsonValueKind.Number)
                    cfg.WorldgenMinProducersPerGood = p.GetInt32();

                if (root.TryGetProperty("worldgen_min_sinks_per_good", out var s) && s.ValueKind == System.Text.Json.JsonValueKind.Number)
                    cfg.WorldgenMinSinksPerGood = s.GetInt32();

                if (root.TryGetProperty("default_lane_capacity_k", out var k) && k.ValueKind == System.Text.Json.JsonValueKind.Number)
                    cfg.DefaultLaneCapacityK = k.GetInt32();

                if (root.TryGetProperty("market_fee_multiplier", out var fee) && fee.ValueKind == System.Text.Json.JsonValueKind.Number)
                    cfg.MarketFeeMultiplier = fee.GetDouble();

                if (root.TryGetProperty("risk_scalar", out var risk) && risk.ValueKind == System.Text.Json.JsonValueKind.Number)
                    cfg.RiskScalar = risk.GetDouble();

                if (root.TryGetProperty("loop_viability_threshold", out var thr) && thr.ValueKind == System.Text.Json.JsonValueKind.Number)
                    cfg.LoopViabilityThreshold = thr.GetDouble();

                if (root.TryGetProperty("role_risk_tolerance_default", out var rrt) && rrt.ValueKind == System.Text.Json.JsonValueKind.Number)
                    cfg.RoleRiskToleranceDefault = rrt.GetDouble();

                return cfg;
            }
            catch
            {
                // Failure-safe determinism: invalid JSON falls back to stable defaults.
                return cfg;
            }
        }
    }

    [JsonInclude] public int Tick { get; private set; }
    [JsonInclude] public int InitialSeed { get; private set; }
    [JsonIgnore] public Random? Rng { get; private set; }

    // GATE.S3_5.CONTENT_SUBSTRATE.003
    // Content pack identity bound into world identity and persisted through save%load.
    // pack_id%pack_version must be stable and surfaced in failures for repro.
    [JsonInclude] public string ContentPackIdV0 { get; set; } = "";
    [JsonInclude] public int ContentPackVersionV0 { get; set; } = 0;

    // GATE.X.CONTENT_SUBSTRATE.001
    // Surfaced deterministic content registry identity (no timestamps).
    // Version is part of the registry payload; Digest is SHA256 over canonical text.
    // Stored in save%load so replay and transcripts can reference exact content identity.
    [JsonInclude] public int ContentRegistryVersionV0 { get; set; } = 0;
    [JsonInclude] public string ContentRegistryDigestV0 { get; set; } = "";

    // GATE.S7.MAIN_MENU.NEW_VOYAGE.001: Difficulty preset chosen at world creation.
    // Persisted so difficulty multipliers are consistent across save/load.
    // Default Normal preserves backward compatibility with existing worlds.
    [JsonInclude] public DifficultyPreset Difficulty { get; set; } = DifficultyPreset.Normal;

    // GATE.S7.MAIN_MENU.CAPTAIN_NAME.001: Captain name chosen at voyage creation.
    // Persisted for narrative display. Not gameplay-affecting; excluded from GetSignature().
    [JsonInclude] public string CaptainName { get; set; } = "Commander";

    // GATE.X.TWEAKS.DATA.001
    // Versioned tweak config loaded deterministically (defaults or JSON override).
    // NOTE: kept out of save%load and golden hashing until a dedicated transcript surface consumes it.
    [JsonIgnore] public TweakConfigV0 Tweaks { get; private set; } = TweakConfigV0.CreateDefaults();
    [JsonIgnore] public string TweaksHash { get; private set; } = "";

    // GATE.X.TWEAKS.DATA.TRANSCRIPT.001
    // Deterministic transcript surface (no timestamps, fixed formatting).
    // Intended to be emitted at tick 0 by transcript producers.
    public string GetDeterministicTranscriptTick0Line()
    {
        // Keep formatting stable: ASCII-safe keys, no locale-dependent formatting.
        return $"tick=0 tweaks_version={Tweaks.Version} tweaks_hash={TweaksHash}";
    }

    [JsonInclude] public Dictionary<string, Market> Markets { get; private set; } = new();
    [JsonInclude] public Dictionary<string, Node> Nodes { get; private set; } = new();
    [JsonInclude] public Dictionary<string, Edge> Edges { get; private set; } = new();

    // RoutePlanner adjacency cache (FromNodeId -> edges sorted by edge id).
    // Non-serialized: rebuilt deterministically as needed.
    [JsonIgnore] private Dictionary<string, List<Edge>>? _routeOutgoingByFromNode;
    [JsonIgnore] private bool _routeOutgoingBuilt;

    [JsonInclude] public Dictionary<string, Fleet> Fleets { get; private set; } = new();

    [JsonInclude] public Dictionary<string, IndustrySite> IndustrySites { get; private set; } = new();

    // GATE.S4.INDU.MIN_LOOP.001
    // Persisted industry construction pipeline state (deterministic, save%load stable).
    [JsonInclude] public Dictionary<string, IndustryBuildState> IndustryBuilds { get; private set; } = new(StringComparer.Ordinal);

    // GATE.S4.INDU.MIN_LOOP.001
    // Deterministic industry event stream for save%load%replay comparison.
    [JsonInclude] public long NextIndustryEventSeq { get; set; } = 1;
    [JsonInclude] public List<string> IndustryEventLog { get; private set; } = new();

    // GATE.S4.INDU_STRUCT.SHORTFALL_LOG.001
    // Persistent typed shortfall event log for UI explain surfaces and chain analysis.
    [JsonInclude] public long NextShortfallEventSeq { get; set; } = 1;
    [JsonInclude] public List<SimCore.Events.IndustryEvents.ShortfallEvent> ShortfallEventLog { get; private set; } = new();

    [JsonInclude] public List<SimCore.Entities.InFlightTransfer> InFlightTransfers { get; private set; } = new();

    [JsonInclude] public long NextIntentSeq { get; set; } = 1;
    [JsonInclude] public List<IntentEnvelope> PendingIntents { get; private set; } = new();

    // Slice 3 / GATE.LOGI.RES.001
    // Deterministic logistics job id sequence (J1, J2, ...)
    [JsonInclude] public long NextLogisticsJobSeq { get; set; } = 1;

    // Slice 3 / GATE.LOGI.RESERVE.001
    // Virtual reservations protecting market inventory for logistics jobs.
    [JsonInclude] public long NextLogisticsReservationSeq { get; set; } = 1;
    [JsonInclude] public Dictionary<string, SimCore.Entities.LogisticsReservation> LogisticsReservations { get; private set; } = new();

    // Programs (Slice 2 foundation)
    [JsonInclude] public long NextProgramSeq { get; set; } = 1;
    [JsonInclude] public ProgramBook Programs { get; set; } = new();

    // GATE.S8.WIN.GAME_RESULT.001: Current game result state.
    [JsonInclude] public GameResult GameResultValue { get; set; } = GameResult.InProgress;

    // GATE.S8.WIN.PROGRESS_TRACK.001: Current endgame progress snapshot (updated each tick).
    [JsonIgnore] public EndgameProgress EndgameProgress { get; set; } = new();

    [JsonInclude] public long PlayerCredits { get; set; } = 1000;

    [JsonInclude] public Dictionary<string, int> PlayerCargo { get; private set; } = new();
    // GATE.X.LEDGER.COST_BASIS.001: Weighted average buy price per cargo good (credits per unit).
    [JsonInclude] public Dictionary<string, int> PlayerCargoCostBasis { get; set; } = new();
    [JsonInclude] public string PlayerLocationNodeId { get; set; } = "";
    [JsonInclude] public string PlayerSelectedDestinationNodeId { get; set; } = "";
    [JsonInclude] public HashSet<string> PlayerVisitedNodeIds { get; private set; } = new(StringComparer.Ordinal);

    [JsonInclude] public IntelBook Intel { get; set; } = new();

    // GATE.S1.MISSION.MODEL.001: Persisted mission state.
    [JsonInclude] public MissionState Missions { get; set; } = new();

    // GATE.S4.TECH.CORE.001: Persisted tech/research state.
    [JsonInclude] public TechState Tech { get; set; } = new();

    // GATE.S4.CONSTR_PROG.MODEL.001: Persisted construction state.
    [JsonInclude] public ConstructionState Construction { get; set; } = new();

    // GATE.X.PRESSURE.MODEL.001: Persisted pressure state.
    [JsonInclude] public PressureStateContainer Pressure { get; set; } = new();

    // GATE.S7.PLANET.MODEL.001: Persisted planet + star state (keyed by nodeId).
    [JsonInclude] public Dictionary<string, Planet> Planets { get; private set; } = new(StringComparer.Ordinal);
    [JsonInclude] public Dictionary<string, Star> Stars { get; private set; } = new(StringComparer.Ordinal);

    // GATE.S12.PROGRESSION.STATS.001: Player progression statistics.
    [JsonInclude] public PlayerStats PlayerStats { get; set; } = new();

    // GATE.S7.FACTION_COMMISSION.ENTITY.001: Player's active faction commission (null = none).
    [JsonInclude] public Commission? ActiveCommission { get; set; }

    // GATE.S7.FACTION.REPUTATION_SYS.001: Player standing per faction [-100,100].
    [JsonInclude] public Dictionary<string, int> FactionReputation { get; private set; } = new(StringComparer.Ordinal);

    // GATE.S7.FACTION.TARIFF_ENFORCE.001: Node-to-faction mapping (nodeId -> factionId).
    [JsonInclude] public Dictionary<string, string> NodeFactionId { get; private set; } = new(StringComparer.Ordinal);
    // GATE.S7.FACTION.TARIFF_ENFORCE.001: Faction base tariff rates (factionId -> rate 0.0-1.0).
    [JsonInclude] public Dictionary<string, float> FactionTariffRates { get; private set; } = new(StringComparer.Ordinal);
    // GATE.S7.FACTION.BRIDGE_QUERIES.001: Faction trade policy (factionId -> TradePolicy int).
    [JsonInclude] public Dictionary<string, int> FactionTradePolicy { get; private set; } = new(StringComparer.Ordinal);
    // GATE.S7.FACTION.BRIDGE_QUERIES.001: Faction aggression level (factionId -> 0=peaceful,1=defensive,2=hostile).
    [JsonInclude] public Dictionary<string, int> FactionAggressionLevel { get; private set; } = new(StringComparer.Ordinal);

    // GATE.S7.FACTION_COMMISSION.INFAMY.001: Infamy per faction. Caps max achievable RepTier.
    [JsonInclude] public Dictionary<string, int> InfamyByFaction { get; private set; } = new(StringComparer.Ordinal);

    // GATE.S7.TERRITORY.HYSTERESIS.001: Committed territory regime per node (int cast of TerritoryRegime).
    [JsonInclude] public Dictionary<string, int> NodeRegimeCommitted { get; private set; } = new(StringComparer.Ordinal);
    // GATE.S7.TERRITORY.HYSTERESIS.001: Proposed improvement regime per node (int cast of TerritoryRegime).
    [JsonInclude] public Dictionary<string, int> NodeRegimeProposed { get; private set; } = new(StringComparer.Ordinal);
    // GATE.S7.TERRITORY.HYSTERESIS.001: Tick when proposed regime was first set per node.
    [JsonInclude] public Dictionary<string, int> NodeRegimeProposedSinceTick { get; private set; } = new(StringComparer.Ordinal);

    // GATE.S7.WARFRONT.STATE_MODEL.001: Active warfronts keyed by warfront ID.
    [JsonInclude] public Dictionary<string, WarfrontState> Warfronts { get; private set; } = new(StringComparer.Ordinal);

    // GATE.S7.SUPPLY.DELIVERY_LEDGER.001: Cumulative war supply deliveries per warfront+good.
    // Outer key = warfrontId, inner key = goodId, value = cumulative units consumed.
    [JsonInclude] public Dictionary<string, Dictionary<string, int>> WarSupplyLedger { get; private set; } = new(StringComparer.Ordinal);

    // GATE.S7.TERRITORY.EMBARGO_MODEL.001: Active embargoes keyed by embargo ID.
    [JsonInclude] public List<EmbargoState> Embargoes { get; private set; } = new();

    // GATE.S7.DIPLOMACY.FRAMEWORK.001: Active diplomatic acts keyed by act ID.
    [JsonInclude] public Dictionary<string, Entities.DiplomaticAct> DiplomaticActs { get; private set; } = new(StringComparer.Ordinal);
    [JsonInclude] public long NextDiplomaticActSeq { get; set; } = 1;

    // GATE.T18.NARRATIVE.ENTITIES.001: Data logs keyed by LogId.
    [JsonInclude] public Dictionary<string, Entities.DataLog> DataLogs { get; private set; } = new(StringComparer.Ordinal);

    // GATE.T18.NARRATIVE.ENTITIES.001: Station delivery tracking keyed by "nodeId|goodId".
    [JsonInclude] public Dictionary<string, Entities.StationDeliveryRecord> StationMemory { get; private set; } = new(StringComparer.Ordinal);

    // GATE.T18.NARRATIVE.ENTITIES.001: War consequences keyed by Id.
    [JsonInclude] public Dictionary<string, Entities.WarConsequence> WarConsequences { get; private set; } = new(StringComparer.Ordinal);

    // GATE.T18.NARRATIVE.ENTITIES.001: Named story NPCs keyed by NpcId.
    [JsonInclude] public Dictionary<string, Entities.NarrativeNpc> NarrativeNpcs { get; private set; } = new(StringComparer.Ordinal);

    // GATE.T18.NARRATIVE.ENTITIES.001: First Officer companion state (null until candidate chosen).
    [JsonInclude] public Entities.FirstOfficer? FirstOfficer { get; set; }

    // GATE.S8.HAVEN.ENTITY.001: Haven starbase state.
    [JsonInclude] public Entities.HavenStarbase Haven { get; set; } = new();

    // GATE.S8.MEGAPROJECT.ENTITY.001: Active megaprojects keyed by Id.
    [JsonInclude] public Dictionary<string, Entities.Megaproject> Megaprojects { get; private set; } = new(StringComparer.Ordinal);
    [JsonInclude] public long NextMegaprojectSeq { get; set; } = 1;

    // GATE.S8.MEGAPROJECT.MAP_RULES.001: Sensor Pylon node IDs for scan range extension.
    [JsonInclude] public HashSet<string> SensorPylonNodes { get; private set; } = new(StringComparer.Ordinal);

    // GATE.S8.ADAPTATION.ENTITY.001: Adaptation fragments keyed by FragmentId.
    [JsonInclude] public Dictionary<string, Entities.AdaptationFragment> AdaptationFragments { get; private set; } = new(StringComparer.Ordinal);

    // GATE.S8.STORY_STATE.ENTITY.001: Story progression state (5 Recontextualizations).
    [JsonInclude] public Entities.StoryState StoryState { get; set; } = new();

    // Tutorial state machine (null for existing saves / tutorial skipped).
    [JsonInclude] public Entities.TutorialState? TutorialState { get; set; }

    // GATE.S9.SYSTEMIC.STATION_CONTEXT.001: Per-station economic context cache.
    [JsonInclude] public Dictionary<string, Systems.StationContext> StationContexts { get; set; } = new(StringComparer.Ordinal);

    // GATE.T18.NARRATIVE.ROUTE_UNCERTAINTY.001: Cumulative fracture jumps for scanner adaptation.
    [JsonInclude] public int FractureExposureJumps { get; set; } = 0;

    // GATE.S6.FRACTURE_DISCOVERY.MODEL.001: Fracture system unlock flag (discovery-gated).
    [JsonInclude] public bool FractureUnlocked { get; set; }
    // GATE.S6.FRACTURE_DISCOVERY.MODEL.001: Tick when fracture was discovered/unlocked.
    [JsonInclude] public int FractureDiscoveryTick { get; set; }

    // GATE.S6.FRACTURE.VOID_SITES.001: Void discovery sites between systems.
    [JsonInclude] public Dictionary<string, VoidSite> VoidSites { get; private set; } = new(StringComparer.Ordinal);

    // GATE.S6.ANOMALY.ENCOUNTER_MODEL.001: Active anomaly encounters keyed by EncounterId.
    [JsonInclude] public Dictionary<string, AnomalyEncounter> AnomalyEncounters { get; private set; } = new(StringComparer.Ordinal);
    [JsonInclude] public long NextAnomalyEncounterSeq { get; set; } = 1;

    // GATE.S9.SYSTEMIC.TRIGGER_ENGINE.001: Active systemic mission offers from world-state triggers.
    [JsonInclude] public List<SystemicMissionOffer> SystemicOffers { get; private set; } = new();

    // GATE.S15.FEEL.JUMP_EVENT_SYS.001: Recent jump events for UI display.
    [JsonInclude] public List<JumpEvent> JumpEvents { get; private set; } = new();
    [JsonInclude] public long NextJumpEventSeq { get; set; } = 1;

    // GATE.S5.LOOT.DROP_SYSTEM.001: Active loot drops keyed by drop ID.
    [JsonInclude] public Dictionary<string, Entities.LootDrop> LootDrops { get; private set; } = new(StringComparer.Ordinal);

    // GATE.S5.COMBAT_LOCAL.COMBAT_LOG.001: last N combat logs (newest first, max 10).
    [JsonInclude] public List<Systems.CombatSystem.CombatLog> CombatLogs { get; private set; } = new();

    // GATE.S5.COMBAT_LOCAL.BRIDGE_COMBAT.001: transient combat state (not persisted).
    [JsonIgnore] public bool InCombat { get; set; }
    [JsonIgnore] public string? CombatOpponentId { get; set; }

    // GATE.S15.FEEL.JUMP_EVENT_SYS.001: Transient per-tick fleet arrival records for JumpEventSystem.
    // Cleared at start of each tick. Populated by MovementSystem on lane arrival.
    [JsonIgnore] public List<(string FleetId, string EdgeId, string NodeId)> ArrivalsThisTick { get; } = new();

    // GATE.S16.NPC_ALIVE.FLEET_DESTROY.001: Transient per-tick destroyed NPC fleet IDs.
    // Cleared at start of each tick. Populated by NpcFleetCombatSystem.
    [JsonIgnore] public List<string> NpcFleetsDestroyedThisTick { get; } = new();

    // GATE.S16.NPC_ALIVE.FLEET_RESPAWN.001: Persisted pending respawn queue.
    // Each entry: (FleetId, HomeNodeId, DestructionTick). Processed by NpcFleetCombatSystem.
    [JsonInclude] public List<NpcRespawnEntry> NpcRespawnQueue { get; set; } = new();

    // GATE.S3_6.EXPLOITATION_PACKAGES.002: exploitation package ledger event log.
    // Persisted. Append-only. Schema-bound tokens: TradePnL, InventoryLoaded, InventoryUnloaded,
    // Produced, BudgetExhausted, NoExportRoute. Ordering: deterministic (tick-order of Apply calls).
    [JsonInclude] public List<string> ExploitationEventLog { get; private set; } = new();

    // GATE.S3_6.EXPEDITION_PROGRAMS.001: transient per-tick expedition intent result surface (not persisted).
    // Cleared by the kernel before each tick; written by ExpeditionIntentV0.Apply.
    [JsonIgnore] public string? LastExpeditionRejectReason { get; set; }
    [JsonIgnore] public string? LastExpeditionAcceptedLeadId { get; set; }
    [JsonIgnore] public string? LastExpeditionAcceptedKind { get; set; }

    // GATE.S4.INDU.MIN_LOOP.001
    // Failure-safe accessor: always returns a non-null build state entry.
    public IndustryBuildState GetOrCreateIndustryBuildState(string siteId)
    {
        if (string.IsNullOrWhiteSpace(siteId)) siteId = "";

        IndustryBuilds ??= new Dictionary<string, IndustryBuildState>(StringComparer.Ordinal);
        if (!IndustryBuilds.TryGetValue(siteId, out var st) || st is null)
        {
            st = new IndustryBuildState();
            IndustryBuilds[siteId] = st;
        }
        return st;
    }

    // Persisted POCO for industry construction v0.
    public sealed class IndustryBuildState
    {
        [JsonInclude] public bool Active { get; set; } = true;
        [JsonInclude] public string RecipeId { get; set; } = "";
        [JsonInclude] public int StageIndex { get; set; } = 0;
        [JsonInclude] public string StageName { get; set; } = "";
        [JsonInclude] public int StageTicksRemaining { get; set; } = 0;
        [JsonInclude] public string BlockerReason { get; set; } = "";
        [JsonInclude] public string SuggestedAction { get; set; } = "";
    }

    /// <summary>
    /// Creates a deterministic program id and adds the instance to the book.
    /// </summary>
    public string CreateAutoBuyProgram(string marketId, string goodId, int quantity, int cadenceTicks)
    {
        var id = $"P{NextProgramSeq}";
        NextProgramSeq = checked(NextProgramSeq + 1);

        var p = new ProgramInstance
        {
            Id = id,
            Kind = ProgramKind.AutoBuy,
            Status = ProgramStatus.Paused,
            CreatedTick = Tick,
            CadenceTicks = cadenceTicks <= 0 ? 1 : cadenceTicks,
            NextRunTick = Tick,
            LastRunTick = -1,
            MarketId = marketId ?? "",
            GoodId = goodId ?? "",
            Quantity = quantity
        };

        Programs ??= new ProgramBook();
        Programs.Instances[id] = p;
        return id;
    }

    /// <summary>
    /// Creates a deterministic program id and adds the instance to the book.
    /// </summary>
    public string CreateAutoSellProgram(string marketId, string goodId, int quantity, int cadenceTicks)
    {
        var id = $"P{NextProgramSeq}";
        NextProgramSeq = checked(NextProgramSeq + 1);

        var p = new ProgramInstance
        {
            Id = id,
            Kind = ProgramKind.AutoSell,
            Status = ProgramStatus.Paused,
            CreatedTick = Tick,
            CadenceTicks = cadenceTicks <= 0 ? 1 : cadenceTicks,
            NextRunTick = Tick,
            LastRunTick = -1,
            MarketId = marketId ?? "",
            GoodId = goodId ?? "",
            Quantity = quantity
        };

        Programs ??= new ProgramBook();
        Programs.Instances[id] = p;
        return id;
    }

    // GATE.S4.CONSTR_PROG.001: construction programs v0
    // Deterministic helper to create a construction program bound to an industry site.
    public string CreateConstrCapModuleProgramV0(string siteId, int cadenceTicks)
    {
        var id = $"P{NextProgramSeq}";
        NextProgramSeq = checked(NextProgramSeq + 1);

        var p = new ProgramInstance
        {
            Id = id,
            Kind = ProgramKind.ConstrCapModuleV0,
            Status = ProgramStatus.Paused,
            CreatedTick = Tick,
            CadenceTicks = cadenceTicks <= 0 ? 1 : cadenceTicks,
            NextRunTick = Tick,
            LastRunTick = -1,
            SiteId = siteId ?? "",
            // MarketId%GoodId%Quantity are unused by this kind; stage inputs are derived from IndustryTweaksV0 and site binding.
            MarketId = "",
            GoodId = "",
            Quantity = 0
        };

        Programs ??= new ProgramBook();
        Programs.Instances[id] = p;
        return id;
    }

    // GATE.S3_6.EXPEDITION_PROGRAMS.002: expedition program v0 factory.
    public string CreateExpeditionProgramV0(string leadId, string fleetId, int cadenceTicks)
    {
        var id = $"P{NextProgramSeq}";
        NextProgramSeq = checked(NextProgramSeq + 1);

        var p = new ProgramInstance
        {
            Id = id,
            Kind = ProgramKind.ExpeditionV0,
            Status = ProgramStatus.Paused,
            CreatedTick = Tick,
            CadenceTicks = cadenceTicks <= 0 ? 1 : cadenceTicks,
            NextRunTick = Tick,
            LastRunTick = -1,
            FleetId = fleetId ?? "",
            ExpeditionSiteId = leadId ?? "",
            ExpeditionTicksRemaining = 0,
            SiteId = "",
            MarketId = "",
            GoodId = "",
            Quantity = 0
        };

        Programs ??= new ProgramBook();
        Programs.Instances[id] = p;
        return id;
    }

    // GATE.S3_6.EXPLOITATION_PACKAGES.002: TradeCharter program v0 factory.
    public string CreateTradeCharterV0Program(
        string sourceMarketId, string destMarketId,
        string buyGoodId, string sellGoodId, int cadenceTicks)
    {
        var id = $"P{NextProgramSeq}";
        NextProgramSeq = checked(NextProgramSeq + 1);

        var p = new ProgramInstance
        {
            Id = id,
            Kind = ProgramKind.TradeCharterV0,
            Status = ProgramStatus.Paused,
            CreatedTick = Tick,
            CadenceTicks = cadenceTicks <= 0 ? 1 : cadenceTicks,
            NextRunTick = Tick,
            LastRunTick = -1,
            SourceMarketId = sourceMarketId ?? "",
            MarketId = destMarketId ?? "",
            GoodId = buyGoodId ?? "",
            SellGoodId = sellGoodId ?? "",
            Quantity = 0
        };

        Programs ??= new ProgramBook();
        Programs.Instances[id] = p;
        return id;
    }

    // GATE.S3_6.EXPLOITATION_PACKAGES.002: ResourceTap program v0 factory.
    public string CreateResourceTapV0Program(
        string sourceMarketId, string extractGoodId, int cadenceTicks)
    {
        var id = $"P{NextProgramSeq}";
        NextProgramSeq = checked(NextProgramSeq + 1);

        var p = new ProgramInstance
        {
            Id = id,
            Kind = ProgramKind.ResourceTapV0,
            Status = ProgramStatus.Paused,
            CreatedTick = Tick,
            CadenceTicks = cadenceTicks <= 0 ? 1 : cadenceTicks,
            NextRunTick = Tick,
            LastRunTick = -1,
            SourceMarketId = sourceMarketId ?? "",
            GoodId = extractGoodId ?? "",
            MarketId = "",
            SellGoodId = "",
            Quantity = 0
        };

        Programs ??= new ProgramBook();
        Programs.Instances[id] = p;
        return id;
    }
}
