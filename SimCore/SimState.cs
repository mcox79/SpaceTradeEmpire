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

public class SimState
{
    [JsonInclude] public int Tick { get; private set; }
    [JsonInclude] public int InitialSeed { get; private set; }
    [JsonIgnore] public Random? Rng { get; private set; }

    [JsonInclude] public Dictionary<string, Market> Markets { get; private set; } = new();
    [JsonInclude] public Dictionary<string, Node> Nodes { get; private set; } = new();
    [JsonInclude] public Dictionary<string, Edge> Edges { get; private set; } = new();

    // RoutePlanner adjacency cache (FromNodeId -> edges sorted by edge id).
    // Non-serialized: rebuilt deterministically as needed.
    [JsonIgnore] private Dictionary<string, List<Edge>>? _routeOutgoingByFromNode;
    [JsonIgnore] private bool _routeOutgoingBuilt;

    [JsonInclude] public Dictionary<string, Fleet> Fleets { get; private set; } = new();

    [JsonInclude] public Dictionary<string, IndustrySite> IndustrySites { get; private set; } = new();
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

    // Fleet event stream (Slice 3 / GATE.S3.FLEET.ROLES.001)
    [JsonInclude] public long NextFleetEventSeq { get; set; } = 1;

    // Non-serialized emission counter used to preserve within-fleet ordering prior to tick-final Seq assignment.
    [JsonInclude] public long NextFleetEmitOrder { get; set; } = 1;

    [JsonInclude] public List<SimCore.Events.FleetEvents.Event> FleetEventLog { get; private set; } = new();

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

    public SimState(int seed)
    {
        InitialSeed = seed;
        Tick = 0;
        Rng = new Random(seed);
    }

    [JsonConstructor]
    public SimState() { }

    public void AdvanceTick()
    {
        FinalizeFleetEventsForTick();
        FinalizeLogisticsEventsForTick();
        Tick++;
    }

    private void FinalizeFleetEventsForTick()
    {
        if (FleetEventLog is null) return;
        if (FleetEventLog.Count == 0) return;

        // Gather indices of events emitted this tick that have not yet been assigned a Seq.
        var idx = new List<int>();
        for (var i = 0; i < FleetEventLog.Count; i++)
        {
            var e = FleetEventLog[i];
            if (e is null) continue;
            if (e.Tick != Tick) continue;
            if (e.Seq != 0) continue;
            idx.Add(i);
        }

        if (idx.Count == 0) return;

        // Sort indices in deterministic event order without LINQ allocations.
        idx.Sort((ai, bi) =>
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
        for (var j = 0; j < idx.Count; j++)
        {
            var e = FleetEventLog[idx[j]];
            var seq = NextFleetEventSeq;
            NextFleetEventSeq = checked(NextFleetEventSeq + 1);
            e.Seq = seq;
        }

        // Reorder the log in-place for this tick only, so list order matches deterministic order.
        // Write the sorted events back into the same set of slots in ascending index order.
        var dest = new List<int>(idx);
        dest.Sort();

        var temp = new SimCore.Events.FleetEvents.Event[idx.Count];
        for (var j = 0; j < idx.Count; j++) temp[j] = FleetEventLog[idx[j]];
        for (var j = 0; j < dest.Count; j++) FleetEventLog[dest[j]] = temp[j];
    }

    private void FinalizeLogisticsEventsForTick()
    {
        if (LogisticsEventLog is null) return;
        if (LogisticsEventLog.Count == 0) return;

        // Gather indices of events emitted this tick that have not yet been assigned a Seq.
        var idx = new List<int>();
        for (var i = 0; i < LogisticsEventLog.Count; i++)
        {
            var e = LogisticsEventLog[i];
            if (e is null) continue;
            if (e.Tick != Tick) continue;
            if (e.Seq != 0) continue;
            idx.Add(i);
        }

        if (idx.Count == 0) return;

        // Sort indices in deterministic event order without LINQ allocations.
        idx.Sort((ai, bi) =>
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
        for (var j = 0; j < idx.Count; j++)
        {
            var e = LogisticsEventLog[idx[j]];
            var seq = NextLogisticsEventSeq;
            NextLogisticsEventSeq = checked(NextLogisticsEventSeq + 1);
            e.Seq = seq;
        }

        // Reorder the log in-place for this tick only, so list order matches deterministic order.
        // Write the sorted events back into the same set of slots in ascending index order.
        var dest = new List<int>(idx);
        dest.Sort();

        var temp = new SimCore.Events.LogisticsEvents.Event[idx.Count];
        for (var j = 0; j < idx.Count; j++) temp[j] = LogisticsEventLog[idx[j]];
        for (var j = 0; j < dest.Count; j++) LogisticsEventLog[dest[j]] = temp[j];
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
        FleetEventLog ??= new List<SimCore.Events.FleetEvents.Event>();

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
