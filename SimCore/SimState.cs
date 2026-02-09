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
    [JsonInclude] public Dictionary<string, Fleet> Fleets { get; private set; } = new();
    [JsonInclude] public Dictionary<string, IndustrySite> IndustrySites { get; private set; } = new();
    [JsonInclude] public List<SimCore.Entities.InFlightTransfer> InFlightTransfers { get; private set; } = new();

    [JsonInclude] public long NextIntentSeq { get; set; } = 1;
    [JsonInclude] public List<IntentEnvelope> PendingIntents { get; private set; } = new();

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
        FinalizeLogisticsEventsForTick();
        Tick++;
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

        // Deterministic ordering rule for same-tick events:
        // FleetId (ordinal) then EmitOrder (preserves within-fleet emission order),
        // then stable tie-breaks to avoid any ambiguity.
        var ordered = idx
            .Select(i => LogisticsEventLog[i])
            .OrderBy(e => e.FleetId ?? "", StringComparer.Ordinal)
            .ThenBy(e => e.EmitOrder)
            .ThenBy(e => (int)e.Type)
            .ThenBy(e => e.GoodId ?? "", StringComparer.Ordinal)
            .ThenBy(e => e.SourceNodeId ?? "", StringComparer.Ordinal)
            .ThenBy(e => e.TargetNodeId ?? "", StringComparer.Ordinal)
            .ThenBy(e => e.Amount)
            .ThenBy(e => e.Note ?? "", StringComparer.Ordinal)
            .ToList();

        // Assign Seq in deterministic order.
        foreach (var e in ordered)
        {
            var seq = NextLogisticsEventSeq;
            NextLogisticsEventSeq = checked(NextLogisticsEventSeq + 1);
            e.Seq = seq;
        }

        // Reorder the log in-place for this tick only, so list order matches deterministic order.
        idx.Sort();
        for (var j = 0; j < idx.Count; j++)
        {
            LogisticsEventLog[idx[j]] = ordered[j];
        }
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
