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

public partial class SimState
{
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

    public void HydrateAfterLoad()
    {
        // Rng is [JsonIgnore] and only used by GalaxyGenerator during one-time world creation.
        // It is intentionally left null after load — post-load calls to GalaxyGenerator.Generate()
        // will throw via the ?? guard there, making misuse explicit rather than silent.
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
        // GATE.S3_6.EXPLOITATION_PACKAGES.002
        ExploitationEventLog ??= new List<string>();

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
}
