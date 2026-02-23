using System.Text;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using SimCore.Entities;
using System.Linq;
using System.Collections.Generic;
using System;
using SimCore.Intents;
using SimCore.Programs;

namespace SimCore;

public static class RiskModelV0
{
    public const int BpsDenom = 10000;

    public const double ScalarDefault = 1.0;
    public const double ScalarMin = 0.0;
    public const double ScalarMax = 10.0;

    public const int TotalBpsCap = 500;

    // Base bps per band (delay, loss, inspection)
    public const int Band0DelayBps = 4;
    public const int Band0LossBps = 0;
    public const int Band0InspBps = 1;

    public const int Band1DelayBps = 15;
    public const int Band1LossBps = 1;
    public const int Band1InspBps = 4;

    public const int Band2DelayBps = 40;
    public const int Band2LossBps = 5;
    public const int Band2InspBps = 15;

    public const int Band3DelayBps = 70;
    public const int Band3LossBps = 15;
    public const int Band3InspBps = 35;

    // Outcome ranges (min + modulo)
    public const int DelayMinTicks = 1;
    public const ulong DelayMod = 3UL;

    public const int LossMinUnits = 1;
    public const ulong LossMod = 2UL;

    public const int InspMinTicks = 1;
    public const ulong InspMod = 4UL;

    public const int OutcomeHashXor = unchecked((int)0x51C0FFEE);

    // Hash constants (FNV1a 64)
    public const ulong FnvOffset = 14695981039346656037UL;
    public const ulong FnvPrime = 1099511628211UL;

    // RoutePlanner risk band thresholds
    public const int RiskBand0Max = 500;
    public const int RiskBand1Max = 1500;
    public const int RiskBand2Max = 3000;

    public const int BandLow = 0;
    public const int BandMed = 1;
    public const int BandHigh = 2;
    public const int BandExtreme = 3;
}
public class SimState
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

    // Logistics event stream (Slice 3 / GATE.LOGI.EVENT.001)
    [JsonInclude] public long NextLogisticsEventSeq { get; set; } = 1;

    // Non-serialized emission counter used to preserve within-fleet ordering prior to tick-final Seq assignment.
    [JsonInclude] public long NextLogisticsEmitOrder { get; set; } = 1;

    [JsonInclude] public List<SimCore.Events.LogisticsEvents.Event> LogisticsEventLog { get; private set; } = new();

    // Reusable buffers for per-tick deterministic logistics event finalization.
    // Private so they are not serialized and do not affect determinism across fresh runs.
    private readonly List<int> _logiFinalizeIdx = new();
    private readonly List<int> _logiFinalizeDest = new();
    private SimCore.Events.LogisticsEvents.Event[] _logiFinalizeTemp = Array.Empty<SimCore.Events.LogisticsEvents.Event>();

    // Security incident event stream (Slice 3 / GATE.S3.RISK_MODEL.001)
    [JsonInclude] public long NextSecurityEventSeq { get; set; } = 1;

    // Non-serialized emission counter used to preserve within-edge ordering prior to tick-final Seq assignment.
    [JsonInclude] public long NextSecurityEmitOrder { get; set; } = 1;

    [JsonInclude] public List<SimCore.Events.SecurityEvents.Event> SecurityEventLog { get; private set; } = new();

    // Reusable buffers for per-tick deterministic security event finalization.
    // Private so they are not serialized and do not affect determinism across fresh runs.
    private readonly List<int> _secFinalizeIdx = new();
    private readonly List<int> _secFinalizeDest = new();
    private SimCore.Events.SecurityEvents.Event[] _secFinalizeTemp = Array.Empty<SimCore.Events.SecurityEvents.Event>();

    // Fleet event stream (Slice 3 / GATE.S3.FLEET.ROLES.001)
    [JsonInclude] public long NextFleetEventSeq { get; set; } = 1;

    // Non-serialized emission counter used to preserve within-fleet ordering prior to tick-final Seq assignment.
    [JsonInclude] public long NextFleetEmitOrder { get; set; } = 1;

    [JsonInclude] public List<SimCore.Events.FleetEvents.Event> FleetEventLog { get; private set; } = new();

    // Reusable buffers for per-tick deterministic fleet event finalization.
    // Private so they are not serialized and do not affect determinism across fresh runs.
    private readonly List<int> _fleetFinalizeIdx = new();
    private readonly List<int> _fleetFinalizeDest = new();
    private SimCore.Events.FleetEvents.Event[] _fleetFinalizeTemp = Array.Empty<SimCore.Events.FleetEvents.Event>();

    public void EmitLogisticsEvent(SimCore.Events.LogisticsEvents.Event e)
    {
        if (e is null) return;

        // Buffer event and finalize Seq at end of tick using deterministic ordering rules.
        var emitOrder = NextLogisticsEmitOrder;
        NextLogisticsEmitOrder = checked(NextLogisticsEmitOrder + 1);

        e.Version = SimCore.Events.LogisticsEvents.EventsVersion;
        e.Seq = 0; // assigned during tick finalization
        e.EmitOrder = emitOrder;
        e.Tick = Tick;

        LogisticsEventLog ??= new List<SimCore.Events.LogisticsEvents.Event>();
        LogisticsEventLog.Add(e);
    }

    public void EmitSecurityEvent(SimCore.Events.SecurityEvents.Event e)
    {
        if (e is null) return;

        // Buffer event and finalize Seq at end of tick using deterministic ordering rules.
        var emitOrder = NextSecurityEmitOrder;
        NextSecurityEmitOrder = checked(NextSecurityEmitOrder + 1);

        e.Version = SimCore.Events.SecurityEvents.EventsVersion;
        e.Seq = 0; // assigned during tick finalization
        e.EmitOrder = emitOrder;
        e.Tick = Tick;

        SecurityEventLog ??= new List<SimCore.Events.SecurityEvents.Event>();
        SecurityEventLog.Add(e);
    }

    public void EmitFleetEvent(SimCore.Events.FleetEvents.Event e)
    {
        if (e is null) return;

        // Buffer event and finalize Seq at end of tick using deterministic ordering rules.
        var emitOrder = NextFleetEmitOrder;
        NextFleetEmitOrder = checked(NextFleetEmitOrder + 1);

        e.Version = SimCore.Events.FleetEvents.EventsVersion;
        e.Seq = 0; // assigned during tick finalization
        e.EmitOrder = emitOrder;
        e.Tick = Tick;

        FleetEventLog ??= new List<SimCore.Events.FleetEvents.Event>();
        FleetEventLog.Add(e);
    }

    // GATE.S4.INDU.MIN_LOOP.001
    // Deterministic industry event emission (Seq assigned immediately; ordering is defined by deterministic system iteration order).
    public void EmitIndustryEvent(string note)
    {
        if (note is null) note = "";
        var seq = NextIndustryEventSeq;
        NextIndustryEventSeq = checked(NextIndustryEventSeq + 1);

        IndustryEventLog ??= new List<string>();
        IndustryEventLog.Add($"I{seq} tick={Tick} {note}");
    }

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

    public SimState(int seed)
    {
        InitialSeed = seed;
        Tick = 0;
        Rng = new Random(seed);

        // GATE.X.TWEAKS.DATA.001
        // Stable defaults for all runs unless explicitly overridden deterministically.
        Tweaks = TweakConfigV0.CreateDefaults();
        TweaksHash = ComputeTweaksHashHex(Tweaks);
    }

    // GATE.X.TWEAKS.DATA.001
    // Deterministic tweak loading:
    // - If overrideJson is provided, it wins.
    // - Else if tweakConfigPath is provided and exists, load it.
    // - Else keep stable defaults.
    public void LoadTweaksFromJsonOverride(string? overrideJson)
    {
        if (!string.IsNullOrWhiteSpace(overrideJson))
        {
            var parsed = TweakConfigV0.ParseJsonOrDefaults(overrideJson);
            Tweaks = parsed;
            TweaksHash = ComputeTweaksHashHex(Tweaks);
            return;
        }

        // Keep defaults, but ensure hash is non-empty and stable.
        Tweaks ??= TweakConfigV0.CreateDefaults();
        TweaksHash = ComputeTweaksHashHex(Tweaks);
    }

    private static string ComputeTweaksHashHex(TweakConfigV0 cfg)
    {
        var canonical = cfg.ToCanonicalJson();
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes);
    }

    [JsonConstructor]
    public SimState() { }

    public void AdvanceTick()
    {
        FinalizeFleetEventsForTick();
        FinalizeLogisticsEventsForTick();
        FinalizeSecurityEventsForTick();
        Tick++;
    }

    private void FinalizeFleetEventsForTick()
    {
        if (FleetEventLog is null) return;
        if (FleetEventLog.Count == 0) return;

        // Reuse buffers to avoid per-tick allocations.
        _fleetFinalizeIdx.Clear();

        // Gather indices of events emitted this tick that have not yet been assigned a Seq.
        for (var i = 0; i < FleetEventLog.Count; i++)
        {
            var e = FleetEventLog[i];
            if (e is null) continue;
            if (e.Tick != Tick) continue;
            if (e.Seq != 0) continue;
            _fleetFinalizeIdx.Add(i);
        }

        if (_fleetFinalizeIdx.Count == 0) return;

        // Sort indices in deterministic event order without LINQ allocations.
        _fleetFinalizeIdx.Sort((ai, bi) =>
        {
            var a = FleetEventLog[ai];
            var b = FleetEventLog[bi];

            int c;
            c = string.CompareOrdinal(a.FleetId ?? "", b.FleetId ?? ""); if (c != 0) return c;
            c = a.EmitOrder.CompareTo(b.EmitOrder); if (c != 0) return c;
            c = ((int)a.Type).CompareTo((int)b.Type); if (c != 0) return c;
            c = string.CompareOrdinal(a.ChosenRouteId ?? "", b.ChosenRouteId ?? ""); if (c != 0) return c;
            c = a.Role.CompareTo(b.Role); if (c != 0) return c;
            c = a.ProfitScore.CompareTo(b.ProfitScore); if (c != 0) return c;
            c = a.CapacityScore.CompareTo(b.CapacityScore); if (c != 0) return c;
            c = a.RiskScore.CompareTo(b.RiskScore); if (c != 0) return c;
            c = string.CompareOrdinal(a.Note ?? "", b.Note ?? ""); if (c != 0) return c;

            // Absolute final tiebreak: index to keep ordering deterministic even if all keys match.
            return ai.CompareTo(bi);
        });

        // Assign Seq in deterministic order (idx is now sorted by event order).
        for (var j = 0; j < _fleetFinalizeIdx.Count; j++)
        {
            var e = FleetEventLog[_fleetFinalizeIdx[j]];
            var seq = NextFleetEventSeq;
            NextFleetEventSeq = checked(NextFleetEventSeq + 1);
            e.Seq = seq;
        }

        // Reorder the log in-place for this tick only, so list order matches deterministic order.
        // Write the sorted events back into the same set of slots in ascending index order.
        _fleetFinalizeDest.Clear();
        _fleetFinalizeDest.AddRange(_fleetFinalizeIdx);
        _fleetFinalizeDest.Sort();

        if (_fleetFinalizeTemp.Length < _fleetFinalizeIdx.Count)
            _fleetFinalizeTemp = new SimCore.Events.FleetEvents.Event[_fleetFinalizeIdx.Count];

        for (var j = 0; j < _fleetFinalizeIdx.Count; j++)
            _fleetFinalizeTemp[j] = FleetEventLog[_fleetFinalizeIdx[j]];

        for (var j = 0; j < _fleetFinalizeDest.Count; j++)
            FleetEventLog[_fleetFinalizeDest[j]] = _fleetFinalizeTemp[j];
    }

    private void FinalizeLogisticsEventsForTick()
    {
        if (LogisticsEventLog is null) return;
        if (LogisticsEventLog.Count == 0) return;

        // Reuse buffers to avoid per-tick allocations.
        _logiFinalizeIdx.Clear();

        // Gather indices of events emitted this tick that have not yet been assigned a Seq.
        for (var i = 0; i < LogisticsEventLog.Count; i++)
        {
            var e = LogisticsEventLog[i];
            if (e is null) continue;
            if (e.Tick != Tick) continue;
            if (e.Seq != 0) continue;
            _logiFinalizeIdx.Add(i);
        }

        if (_logiFinalizeIdx.Count == 0) return;

        // Sort indices in deterministic event order without LINQ allocations.
        _logiFinalizeIdx.Sort((ai, bi) =>
        {
            var a = LogisticsEventLog[ai];
            var b = LogisticsEventLog[bi];

            int c;
            c = string.CompareOrdinal(a.FleetId ?? "", b.FleetId ?? ""); if (c != 0) return c;
            c = a.EmitOrder.CompareTo(b.EmitOrder); if (c != 0) return c;
            c = ((int)a.Type).CompareTo((int)b.Type); if (c != 0) return c;
            c = string.CompareOrdinal(a.GoodId ?? "", b.GoodId ?? ""); if (c != 0) return c;
            c = string.CompareOrdinal(a.SourceNodeId ?? "", b.SourceNodeId ?? ""); if (c != 0) return c;
            c = string.CompareOrdinal(a.TargetNodeId ?? "", b.TargetNodeId ?? ""); if (c != 0) return c;
            c = a.Amount.CompareTo(b.Amount); if (c != 0) return c;
            c = string.CompareOrdinal(a.Note ?? "", b.Note ?? ""); if (c != 0) return c;

            // Absolute final tiebreak: index to keep ordering deterministic even if all keys match.
            return ai.CompareTo(bi);
        });

        // Assign Seq in deterministic order (idx is now sorted by event order).
        for (var j = 0; j < _logiFinalizeIdx.Count; j++)
        {
            var e = LogisticsEventLog[_logiFinalizeIdx[j]];
            var seq = NextLogisticsEventSeq;
            NextLogisticsEventSeq = checked(NextLogisticsEventSeq + 1);
            e.Seq = seq;
        }

        // Reorder the log in-place for this tick only, so list order matches deterministic order.
        // Write the sorted events back into the same set of slots in ascending index order.
        _logiFinalizeDest.Clear();
        _logiFinalizeDest.AddRange(_logiFinalizeIdx);
        _logiFinalizeDest.Sort();

        if (_logiFinalizeTemp.Length < _logiFinalizeIdx.Count)
            _logiFinalizeTemp = new SimCore.Events.LogisticsEvents.Event[_logiFinalizeIdx.Count];

        for (var j = 0; j < _logiFinalizeIdx.Count; j++)
            _logiFinalizeTemp[j] = LogisticsEventLog[_logiFinalizeIdx[j]];

        for (var j = 0; j < _logiFinalizeDest.Count; j++)
            LogisticsEventLog[_logiFinalizeDest[j]] = _logiFinalizeTemp[j];
    }

    private void FinalizeSecurityEventsForTick()
    {
        if (SecurityEventLog is null) return;
        if (SecurityEventLog.Count == 0) return;

        _secFinalizeIdx.Clear();

        for (var i = 0; i < SecurityEventLog.Count; i++)
        {
            var e = SecurityEventLog[i];
            if (e is null) continue;
            if (e.Tick != Tick) continue;
            if (e.Seq != 0) continue;
            _secFinalizeIdx.Add(i);
        }

        if (_secFinalizeIdx.Count == 0) return;

        _secFinalizeIdx.Sort((ai, bi) =>
        {
            var a = SecurityEventLog[ai];
            var b = SecurityEventLog[bi];

            int c;
            c = string.CompareOrdinal(a.EdgeId ?? "", b.EdgeId ?? ""); if (c != 0) return c;
            c = a.EmitOrder.CompareTo(b.EmitOrder); if (c != 0) return c;
            c = ((int)a.Type).CompareTo((int)b.Type); if (c != 0) return c;
            c = string.CompareOrdinal(a.FromNodeId ?? "", b.FromNodeId ?? ""); if (c != 0) return c;
            c = string.CompareOrdinal(a.ToNodeId ?? "", b.ToNodeId ?? ""); if (c != 0) return c;
            c = a.RiskBand.CompareTo(b.RiskBand); if (c != 0) return c;
            c = a.DelayTicks.CompareTo(b.DelayTicks); if (c != 0) return c;
            c = a.LossUnits.CompareTo(b.LossUnits); if (c != 0) return c;
            c = a.InspectionTicks.CompareTo(b.InspectionTicks); if (c != 0) return c;
            c = string.CompareOrdinal(a.CauseChain ?? "", b.CauseChain ?? ""); if (c != 0) return c;
            c = string.CompareOrdinal(a.Note ?? "", b.Note ?? ""); if (c != 0) return c;

            return ai.CompareTo(bi);
        });

        for (var j = 0; j < _secFinalizeIdx.Count; j++)
        {
            var e = SecurityEventLog[_secFinalizeIdx[j]];
            var seq = NextSecurityEventSeq;
            NextSecurityEventSeq = checked(NextSecurityEventSeq + 1);
            e.Seq = seq;
        }

        _secFinalizeDest.Clear();
        _secFinalizeDest.AddRange(_secFinalizeIdx);
        _secFinalizeDest.Sort();

        if (_secFinalizeTemp.Length < _secFinalizeIdx.Count)
            _secFinalizeTemp = new SimCore.Events.SecurityEvents.Event[_secFinalizeIdx.Count];

        for (var j = 0; j < _secFinalizeIdx.Count; j++)
            _secFinalizeTemp[j] = SecurityEventLog[_secFinalizeIdx[j]];

        for (var j = 0; j < _secFinalizeDest.Count; j++)
            SecurityEventLog[_secFinalizeDest[j]] = _secFinalizeTemp[j];
    }

    public void HydrateAfterLoad()
    {
        Rng = new Random(InitialSeed + Tick);
        Programs ??= new ProgramBook();

        // IMPORTANT: IntentEnvelope.Intent is JsonIgnore (not persisted).
        // After load, any PendingIntents would have null Intent and silently do nothing.
        // Until GATE.SAVE.001 defines intent persistence, we discard pending intents explicitly.
        PendingIntents ??= new List<IntentEnvelope>();
        PendingIntents.Clear();

        LogisticsEventLog ??= new List<SimCore.Events.LogisticsEvents.Event>();
        SecurityEventLog ??= new List<SimCore.Events.SecurityEvents.Event>();
        FleetEventLog ??= new List<SimCore.Events.FleetEvents.Event>();

        // GATE.S4.INDU.MIN_LOOP.001
        IndustryBuilds ??= new Dictionary<string, IndustryBuildState>(StringComparer.Ordinal);
        IndustryEventLog ??= new List<string>();

        LogisticsReservations ??= new Dictionary<string, SimCore.Entities.LogisticsReservation>(StringComparer.Ordinal);

        InvalidateRoutePlannerCaches();
    }

    public void InvalidateRoutePlannerCaches()
    {
        _routeOutgoingBuilt = false;
        _routeOutgoingByFromNode = null;
    }

    public Dictionary<string, List<Edge>> GetOutgoingEdgesByFromNodeDeterministic()
    {
        if (_routeOutgoingBuilt && _routeOutgoingByFromNode is not null)
            return _routeOutgoingByFromNode;

        var outgoing = new Dictionary<string, List<Edge>>(StringComparer.Ordinal);

        foreach (var e in Edges.Values)
        {
            if (e is null) continue;

            var from = e.FromNodeId ?? "";
            if (from.Length == 0) continue;

            if (!outgoing.TryGetValue(from, out var list))
            {
                list = new List<Edge>(capacity: 4);
                outgoing[from] = list;
            }

            list.Add(e);
        }

        foreach (var kv in outgoing)
        {
            kv.Value.Sort((a, b) => string.CompareOrdinal(a.Id ?? "", b.Id ?? ""));
        }

        _routeOutgoingByFromNode = outgoing;
        _routeOutgoingBuilt = true;
        return outgoing;
    }

    /// <summary>
    /// Deterministic entrypoint for systems to enqueue intents.
    /// Mirrors SimKernel's wrap behavior (Seq, tick, kind).
    /// </summary>
    public void EnqueueIntent(IIntent intent)
    {
        if (intent is null) return;

        var seq = NextIntentSeq;
        NextIntentSeq = checked(NextIntentSeq + 1);

        PendingIntents.Add(new IntentEnvelope
        {
            Seq = seq,
            CreatedTick = Tick,
            Kind = intent.Kind,
            Intent = intent
        });
    }

    // Slice 3 / GATE.LOGI.RESERVE.001
    // Reservation helpers (deterministic).
    public int GetTotalReservedRemaining(string marketId, string goodId)
    {
        if (LogisticsReservations is null) return 0;
        if (string.IsNullOrWhiteSpace(marketId)) return 0;
        if (string.IsNullOrWhiteSpace(goodId)) return 0;

        var sum = 0;
        foreach (var r in LogisticsReservations.Values)
        {
            if (r is null) continue;
            if (!string.Equals(r.MarketId, marketId, StringComparison.Ordinal)) continue;
            if (!string.Equals(r.GoodId, goodId, StringComparison.Ordinal)) continue;
            if (r.Remaining <= 0) continue;
            sum = checked(sum + r.Remaining);
        }
        return sum;
    }

    public int GetUnreservedAvailable(string marketId, string goodId)
    {
        if (string.IsNullOrWhiteSpace(marketId)) return 0;
        if (string.IsNullOrWhiteSpace(goodId)) return 0;

        if (!Markets.TryGetValue(marketId, out var m) || m is null) return 0;
        var inv = m.Inventory.TryGetValue(goodId, out var v) ? v : 0;
        if (inv <= 0) return 0;

        var reserved = GetTotalReservedRemaining(marketId, goodId);
        var unreserved = inv - reserved;
        return unreserved <= 0 ? 0 : unreserved;
    }

    public bool TryCreateLogisticsReservation(string marketId, string goodId, string fleetId, int requestedQty, out string reservationId, out int reservedQty)
    {
        reservationId = "";
        reservedQty = 0;

        if (string.IsNullOrWhiteSpace(marketId)) return false;
        if (string.IsNullOrWhiteSpace(goodId)) return false;
        if (string.IsNullOrWhiteSpace(fleetId)) return false;
        if (requestedQty <= 0) return false;

        // Reserve only from currently unreserved pool (does not mutate inventory).
        var unreserved = GetUnreservedAvailable(marketId, goodId);
        if (unreserved <= 0) return true; // optional: no reservation created

        var qty = Math.Min(unreserved, requestedQty);
        if (qty <= 0) return true;

        var id = $"R{NextLogisticsReservationSeq}";
        NextLogisticsReservationSeq = checked(NextLogisticsReservationSeq + 1);

        LogisticsReservations ??= new Dictionary<string, SimCore.Entities.LogisticsReservation>(StringComparer.Ordinal);
        LogisticsReservations[id] = new SimCore.Entities.LogisticsReservation
        {
            Id = id,
            MarketId = marketId,
            GoodId = goodId,
            FleetId = fleetId,
            Remaining = qty
        };

        reservationId = id;
        reservedQty = qty;
        return true;
    }

    public bool TryGetLogisticsReservation(string reservationId, out SimCore.Entities.LogisticsReservation? res)
    {
        res = null;
        if (LogisticsReservations is null) return false;
        if (string.IsNullOrWhiteSpace(reservationId)) return false;
        return LogisticsReservations.TryGetValue(reservationId, out res);
    }

    public void ConsumeLogisticsReservation(string reservationId, int consumeQty)
    {
        if (consumeQty <= 0) return;
        if (!TryGetLogisticsReservation(reservationId, out var r) || r is null) return;

        var next = r.Remaining - consumeQty;
        if (next <= 0)
        {
            LogisticsReservations.Remove(reservationId);
            return;
        }

        r.Remaining = next;
    }

    public void ReleaseLogisticsReservation(string reservationId)
    {
        if (LogisticsReservations is null) return;
        if (string.IsNullOrWhiteSpace(reservationId)) return;
        LogisticsReservations.Remove(reservationId);
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

    public string GetSignature()
    {
        var sb = new StringBuilder();
        sb.Append($"Tick:{Tick}|Cred:{PlayerCredits}|Loc:{PlayerLocationNodeId}|");

        sb.Append($"Nodes:{Nodes.Count}|Edges:{Edges.Count}|Markets:{Markets.Count}|Fleets:{Fleets.Count}|Sites:{IndustrySites.Count}|");

        foreach (var f in Fleets.OrderBy(k => k.Key))
        {
            sb.Append($"Flt:{f.Key}_N:{f.Value.CurrentNodeId}_S:{f.Value.State}_D:{f.Value.DestinationNodeId}|");

            // Include cargo deterministically (keys sorted, stable formatting).
            if (f.Value.Cargo is not null && f.Value.Cargo.Count > 0)
            {
                sb.Append("Cargo:");
                foreach (var kv in f.Value.Cargo.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                {
                    sb.Append($"{kv.Key}:{kv.Value},");
                }
                sb.Append("|");
            }
        }

        foreach (var m in Markets.OrderBy(k => k.Key))
        {
            sb.Append($"Mkt:{m.Key}|");
            foreach (var kv in m.Value.Inventory.OrderBy(i => i.Key))
            {
                sb.Append($"{kv.Key}:{kv.Value},");
            }
            sb.Append("|");
        }

        if (LogisticsReservations is not null && LogisticsReservations.Count > 0)
        {
            foreach (var kv in LogisticsReservations.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var r = kv.Value;
                sb.Append($"Res:{kv.Key}|M:{r.MarketId}|G:{r.GoodId}|F:{r.FleetId}|Rem:{r.Remaining}|");
            }
        }

        if (Programs is not null && Programs.Instances is not null && Programs.Instances.Count > 0)
        {
            foreach (var kv in Programs.Instances.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var p = kv.Value;
                sb.Append($"Prog:{p.Id}|K:{p.Kind}|S:{p.Status}|Cad:{p.CadenceTicks}|Nx:{p.NextRunTick}|Ls:{p.LastRunTick}|M:{p.MarketId}|G:{p.GoodId}|Q:{p.Quantity}|");
            }
        }

        if (Intel is not null && Intel.Observations is not null && Intel.Observations.Count > 0)
        {
            foreach (var kv in Intel.Observations.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var obs = kv.Value;
                sb.Append($"Intel:{kv.Key}@{obs.ObservedTick}={obs.ObservedInventoryQty}|");
            }
        }

        foreach (var s in IndustrySites.OrderBy(k => k.Key))
        {
            // Include tech sustainment state so determinism drift cannot hide.
            sb.Append($"Site:{s.Key}|Eff:{s.Value.Efficiency:F4}|Health:{s.Value.HealthBps}|BufD:{s.Value.BufferDays}|Rem:{s.Value.DegradeRemainder}|");
        }

        // GATE.S4.INDU.MIN_LOOP.001
        // Include persisted construction state in signature so save%load%replay drift cannot hide.
        if (IndustryBuilds is not null && IndustryBuilds.Count > 0)
        {
            foreach (var kv in IndustryBuilds.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var b = kv.Value;
                if (b is null) continue;
                sb.Append($"IB:{kv.Key}|A:{(b.Active ? 1 : 0)}|R:{b.RecipeId}|Si:{b.StageIndex}|Sn:{b.StageName}|Rem:{b.StageTicksRemaining}|Blk:{b.BlockerReason}|Act:{b.SuggestedAction}|");
            }
        }

        foreach (var n in Nodes.OrderBy(k => k.Key))
        {
            if (n.Value.Trace > 0.001f) sb.Append($"N_Tr:{n.Key}:{n.Value.Trace:F2}|");
        }
        foreach (var e in Edges.OrderBy(k => k.Key))
        {
            if (e.Value.Heat > 0.001f) sb.Append($"E_Ht:{e.Key}:{e.Value.Heat:F2}|");
        }

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }
}
