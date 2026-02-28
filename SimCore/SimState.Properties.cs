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

    [JsonInclude] public long PlayerCredits { get; set; } = 1000;

    [JsonInclude] public Dictionary<string, int> PlayerCargo { get; private set; } = new();
    [JsonInclude] public string PlayerLocationNodeId { get; set; } = "";
    [JsonInclude] public string PlayerSelectedDestinationNodeId { get; set; } = "";

    [JsonInclude] public IntelBook Intel { get; set; } = new();

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
