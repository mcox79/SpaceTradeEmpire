#nullable enable

using Godot;
using SimCore;
using SimCore.Gen;
using SimCore.Commands;
using SimCore.Intents;
using SimCore.Systems;
using SimCore.Programs;
using SimCore.Events;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge
{
    // --- Programs: bridge lifecycle + explain snapshots ---

    public string CreateAutoBuyProgram(string marketId, string goodId, int quantity, int cadenceTicks)
    {
        if (IsLoading) return "";
        if (string.IsNullOrWhiteSpace(marketId)) return "";
        if (string.IsNullOrWhiteSpace(goodId)) return "";
        if (quantity <= 0) return "";
        if (cadenceTicks <= 0) cadenceTicks = 1;

        _stateLock.EnterWriteLock();
        try
        {
            var id = _kernel.State.CreateAutoBuyProgram(marketId, goodId, quantity, cadenceTicks);
            if (!string.IsNullOrWhiteSpace(id))
            {
                // Deterministic: record under the same lock in a single sequence.
                RecordProgramEvent(
                        type: 1,
                        tick: _kernel.State.Tick,
                        programId: id,
                        marketId: marketId,
                        goodId: goodId,
                        note: $"qty={quantity} cad={cadenceTicks}t");
            }
            return id;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    public bool StartProgram(string programId)
    {
        return EnqueueProgramStatus(programId, ProgramStatus.Running);
    }

    public bool PauseProgram(string programId)
    {
        return EnqueueProgramStatus(programId, ProgramStatus.Paused);
    }

    public bool CancelProgram(string programId)
    {
        return EnqueueProgramStatus(programId, ProgramStatus.Cancelled);
    }

    private bool EnqueueProgramStatus(string programId, ProgramStatus status)
    {
        if (IsLoading) return false;
        if (string.IsNullOrWhiteSpace(programId)) return false;

        _stateLock.EnterWriteLock();
        try
        {
            _kernel.EnqueueCommand(new SetProgramStatusCommand(programId, status));
            return true;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }


    public Godot.Collections.Array GetProgramExplainSnapshot()
    {
        var arr = new Godot.Collections.Array();
        if (IsLoading) return arr;

        _stateLock.EnterReadLock();
        try
        {
            var payload = ProgramExplain.Build(_kernel.State);
            var json = ProgramExplain.ToDeterministicJson(payload);

            // Convert schema-bound JSON payload into GDScript-friendly dictionaries.
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("Programs", out var progs) || progs.ValueKind != System.Text.Json.JsonValueKind.Array)
                return arr;

            foreach (var item in progs.EnumerateArray())
            {
                var d = new Godot.Collections.Dictionary
                {
                    ["id"] = item.GetProperty("Id").GetString() ?? "",
                    ["kind"] = item.GetProperty("Kind").GetString() ?? "",
                    ["status"] = item.GetProperty("Status").GetString() ?? "",
                    ["cadence_ticks"] = item.GetProperty("CadenceTicks").GetInt32(),
                    ["next_run_tick"] = item.GetProperty("NextRunTick").GetInt32(),
                    ["last_run_tick"] = item.GetProperty("LastRunTick").GetInt32(),
                    ["market_id"] = item.GetProperty("MarketId").GetString() ?? "",
                    ["good_id"] = item.GetProperty("GoodId").GetString() ?? "",
                    ["quantity"] = item.GetProperty("Quantity").GetInt32()
                };
                arr.Add(d);
            }

            return arr;
        }
        catch
        {
            return arr;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    public Godot.Collections.Dictionary GetProgramQuote(string programId)
    {
        var d = new Godot.Collections.Dictionary();
        if (IsLoading) return d;
        if (string.IsNullOrWhiteSpace(programId)) return d;

        _stateLock.EnterReadLock();
        try
        {
            var state = _kernel.State;
            if (state.Programs is null) return d;
            if (!state.Programs.Instances.ContainsKey(programId)) return d;

            // Deterministic: request + snapshot => quote
            var snap = ProgramQuoteSnapshot.Capture(state, programId);
            var quote = ProgramQuote.BuildFromSnapshot(snap);

            d["program_id"] = quote.ProgramId;
            d["kind"] = quote.Kind;
            d["quote_tick"] = quote.QuoteTick;

            d["market_id"] = quote.MarketId;
            d["good_id"] = quote.GoodId;
            d["quantity"] = quote.Quantity;
            d["cadence_ticks"] = quote.CadenceTicks;

            d["unit_price_now"] = quote.UnitPriceNow;
            d["est_cost_or_value_per_run"] = quote.EstCostOrValuePerRun;
            d["est_runs_per_day"] = quote.EstRunsPerDay;
            d["est_daily_cost_or_value"] = quote.EstDailyCostOrValue;

            // Constraints (schema-bound booleans)
            d["market_exists"] = quote.Constraints.MarketExists;
            d["has_enough_credits_now"] = quote.Constraints.HasEnoughCreditsNow;
            d["has_enough_supply_now"] = quote.Constraints.HasEnoughSupplyNow;
            d["has_enough_cargo_now"] = quote.Constraints.HasEnoughCargoNow;

            // Risks (sorted for determinism in core; keep order)
            var risksArr = new Godot.Collections.Array();
            foreach (var r in quote.Risks)
            {
                risksArr.Add(r);
            }
            d["risks"] = risksArr;

            return d;
        }
        catch
        {
            return d;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }


    public Godot.Collections.Dictionary GetProgramOutcome(string programId)
    {
        var d = new Godot.Collections.Dictionary();
        if (IsLoading) return d;
        if (string.IsNullOrWhiteSpace(programId)) return d;

        _stateLock.EnterReadLock();
        try
        {
            var state = _kernel.State;
            if (!state.Programs.Instances.TryGetValue(programId, out var p))
                return d;

            d["program_id"] = programId;
            d["tick_now"] = state.Tick;

            d["status"] = p.Status.ToString();
            d["next_run_tick"] = p.NextRunTick;
            d["last_run_tick"] = p.LastRunTick;

            // Best-effort: ProgramExplain tracks scheduling, not execution results.
            // This outcome describes the last emission opportunity, not guaranteed fills.
            if (p.LastRunTick >= 0)
            {
                d["last_emission"] = $"BuyIntent {p.MarketId}:{p.GoodId} x{p.Quantity}";
            }
            else
            {
                d["last_emission"] = "(never)";
            }

            d["notes"] = "Outcome is scheduling/emission metadata only (not fill confirmation).";
            return d;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }


    // --- Program UI event log snapshot (Slice 3 / GATE.UI.PROGRAMS.EVENT.001) ---
    // Returns the last N program events for the given program, newest-first.
    // Determinism: filter by ProgramId Ordinal, order by Seq desc with stable tie-breakers.
    public Godot.Collections.Array GetProgramEventLogSnapshot(string programId, int maxEvents = 25)
    {
        var arr = new Godot.Collections.Array();
        if (IsLoading) return arr;
        if (string.IsNullOrWhiteSpace(programId)) return arr;
        if (maxEvents <= 0) return arr;
        if (maxEvents > 200) maxEvents = 200;

        _stateLock.EnterReadLock();
        try
        {
            if (_programEventLog.Count == 0) return arr;

            var slice = _programEventLog
                    .Where(e => string.Equals(e.ProgramId, programId, StringComparison.Ordinal))
                    .OrderByDescending(e => e.Seq)
                    .ThenByDescending(e => e.Tick)
                    .ThenByDescending(e => e.Type)
                    .Take(maxEvents)
                    .ToArray();

            foreach (var e in slice)
            {
                var d = new Godot.Collections.Dictionary
                {
                    ["version"] = e.Version,
                    ["seq"] = e.Seq,
                    ["tick"] = e.Tick,
                    ["type"] = e.Type,
                    ["program_id"] = e.ProgramId,
                    ["market_id"] = e.MarketId,
                    ["good_id"] = e.GoodId,
                    ["note"] = e.Note
                };
                arr.Add(d);
            }

            return arr;
        }
        catch
        {
            return arr;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    private void UpdateProgramEventLog_AfterStep(SimState state)
    {
        if (state is null) return;
        if (state.Programs is null) return;
        if (state.Programs.Instances is null) return;

        var tick = state.Tick;

        // Deterministic: iterate programs by id.
        foreach (var kv in state.Programs.Instances.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var p = kv.Value;
            if (p is null) continue;

            var id = p.Id ?? "";
            if (string.IsNullOrWhiteSpace(id)) continue;

            var status = p.Status.ToString();
            var lastRun = p.LastRunTick;
            var marketId = p.MarketId ?? "";
            var goodId = p.GoodId ?? "";
            var qty = p.Quantity;

            if (!_programSnapById.TryGetValue(id, out var snap))
            {
                snap = new ProgramSnap();
                _programSnapById[id] = snap;

                // First time seen: only synthesize CREATED if no event exists for this program.
                // This avoids duplicate CREATED when creation already recorded an event deterministically.
                if (!_programEventLog.Any(e => string.Equals(e.ProgramId, id, StringComparison.Ordinal)))
                {
                    RecordProgramEvent(1, tick, id, marketId, goodId, note: $"kind={p.Kind} qty={qty} (synth)");
                }

                snap.Status = status;
                snap.LastRunTick = lastRun;
                snap.MarketId = marketId;
                snap.GoodId = goodId;
                snap.Quantity = qty;
                continue;
            }

            if (!string.Equals(snap.Status, status, StringComparison.Ordinal))
            {
                RecordProgramEvent(2, tick, id, marketId, goodId, note: $"{snap.Status}->{status}");
                snap.Status = status;
            }

            // A Run event is defined as LastRunTick advancing to the current tick.
            if (lastRun == tick && snap.LastRunTick != lastRun)
            {
                RecordProgramEvent(3, tick, id, marketId, goodId, note: $"qty={qty}");
            }

            snap.LastRunTick = lastRun;
            snap.MarketId = marketId;
            snap.GoodId = goodId;
            snap.Quantity = qty;
        }

        // Cap memory (deterministic truncation from oldest).
        const int cap = 800;
        if (_programEventLog.Count > cap)
        {
            var remove = _programEventLog.Count - cap;
            if (remove > 0) _programEventLog.RemoveRange(0, remove);
        }
    }

    private void RecordProgramEvent(int type, int tick, string programId, string marketId, string goodId, string note)
    {
        // Deterministic: seq increments strictly in call order under the state write lock.
        var e = new ProgramEvent
        {
            Version = 1,
            Seq = ++_programEventSeq,
            Tick = tick,
            Type = type,
            ProgramId = programId ?? "",
            MarketId = marketId ?? "",
            GoodId = goodId ?? "",
            Note = note ?? ""
        };
        _programEventLog.Add(e);
    }
}
