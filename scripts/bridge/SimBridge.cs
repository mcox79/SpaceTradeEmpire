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

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge : Node
{
    [Signal] public delegate void SimLoadedEventHandler();
    [Signal] public delegate void SaveCompletedEventHandler();

    private volatile bool _emitSaveCompletePending = false;
    private int _saveEpoch = 0;
    private int _loadEpoch = 0;

    public int GetSaveEpoch() => Volatile.Read(ref _saveEpoch);
    public int GetLoadEpoch() => Volatile.Read(ref _loadEpoch);

    [Export] public int WorldSeed { get; set; } = 12345;
    [Export] public int StarCount { get; set; } = 20;

    // Sim loop timing. If 0, runs as fast as possible.
    [Export] public int TickDelayMs { get; set; } = 100;

    // If true, deletes the quicksave on boot (runtime only).
    [Export] public bool ResetSaveOnBoot { get; set; } = true;

    private SimKernel _kernel = null!;
    private CancellationTokenSource? _cts;
    private Task? _simTask;

    private readonly ReaderWriterLockSlim _stateLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

    private string _savePathAbs = "";
    private volatile bool _saveRequested = false;
    private volatile bool _loadRequested = false;

    private int _isLoading = 0;
    private volatile bool _emitLoadCompletePending = false;

    public bool IsLoading
    {
        get => Volatile.Read(ref _isLoading) != 0;
        private set => Volatile.Write(ref _isLoading, value ? 1 : 0);
    }

    public override void _Ready()
    {
        // Autoloads run during editor startup. Do not execute runtime logic in the editor.
        if (Engine.IsEditorHint())
        {
            return;
        }

        Input.MouseMode = Input.MouseModeEnum.Visible;

        // Compute save path once on main thread. Do NOT call Godot API from worker thread.
        _savePathAbs = ProjectSettings.GlobalizePath("user://quicksave.json");

        if (ResetSaveOnBoot && File.Exists(_savePathAbs))
        {
            try { File.Delete(_savePathAbs); }
            catch (Exception ex) { GD.PrintErr(ex.ToString()); }
        }

        InitializeKernel();
        StartSimulation();
    }

    public override void _ExitTree()
    {
        StopSimulation();
        _stateLock.Dispose();
    }

    public override void _Process(double delta)
    {
        // Emit save/load complete on main thread.
        if (_emitSaveCompletePending)
        {
            _emitSaveCompletePending = false;
            EmitSignal(SignalName.SaveCompleted);
        }
        if (_emitLoadCompletePending)
        {
            _emitLoadCompletePending = false;
            EmitSignal(SignalName.SimLoaded);
        }
    }

    private void InitializeKernel()
    {
        GD.Print("[BRIDGE] Initializing SimCore Kernel...");
        _kernel = new SimKernel(WorldSeed);

        if (File.Exists(_savePathAbs) && !ResetSaveOnBoot)
        {
            RequestLoad();
        }
        else
        {
            _stateLock.EnterWriteLock();
            try
            {
                GalaxyGenerator.Generate(_kernel.State, StarCount, 200f);
            }
            finally
            {
                _stateLock.ExitWriteLock();
            }
        }
    }

    private void StartSimulation()
    {
        if (_simTask != null && !_simTask.IsCompleted) return;

        _cts = new CancellationTokenSource();
        _simTask = Task.Run(() => SimLoop(_cts.Token), _cts.Token);

        GD.Print("[BRIDGE] Simulation Thread Started.");
    }

    private void StopSimulation()
    {
        try
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }

            // Best-effort join.
            if (_simTask != null && !_simTask.IsCompleted)
            {
                _simTask.Wait(1000);
            }
        }
        catch
        {
            // Suppress shutdown exceptions.
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _simTask = null;
        }
    }

    private async Task SimLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                // Loading is exclusive. If we load, do NOT step this iteration.
                // Otherwise the post-load state advances by one tick and breaks save/load preservation.
                if (_loadRequested)
                {
                    _loadRequested = false;
                    ExecuteLoad();

                    if (TickDelayMs > 0)
                    {
                        await Task.Delay(TickDelayMs, token);
                    }
                    else
                    {
                        await Task.Yield();
                    }

                    continue;
                }

                // Saving can occur between steps.
                if (_saveRequested)
                {
                    _saveRequested = false;
                    ExecuteSave();
                }

                _stateLock.EnterWriteLock();
                try
                {
                    _kernel.Step();
                }
                finally
                {
                    _stateLock.ExitWriteLock();
                }

                if (TickDelayMs > 0)
                {
                    await Task.Delay(TickDelayMs, token);
                }
                else
                {
                    await Task.Yield();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[BRIDGE] CRITICAL SIM ERROR: {ex}");
                await Task.Delay(250, token);
            }
        }
    }

    // --- PUBLIC API (Thread-Safe) ---

    public void ExecuteSafeRead(Action<SimState> action)
    {
        if (IsLoading) return;

        _stateLock.EnterReadLock();
        try
        {
            action(_kernel.State);
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    public void EnqueueCommand(ICommand cmd)
    {
        if (IsLoading) return;

        _stateLock.EnterWriteLock();
        try
        {
            _kernel.EnqueueCommand(cmd);
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    public void EnqueueIntent(IIntent intent)
    {
        if (IsLoading) return;

        _stateLock.EnterWriteLock();
        try
        {
            _kernel.EnqueueIntent(intent);
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    // --- Fleet UI commands (Slice 3 / GATE.UI.FLEET.002, GATE.UI.FLEET.003) ---

    // Best-effort: block until the sim thread advances at least one tick so an immediate UI Refresh()
    // reflects the deterministic post-command state (job cleared, route cleared, task updated).
    private bool WaitForTickAdvance(int tickBefore, int timeoutMs)
    {
        if (tickBefore < 0) return false;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            int now;
            _stateLock.EnterReadLock();
            try
            {
                now = _kernel.State.Tick;
            }
            finally
            {
                _stateLock.ExitReadLock();
            }

            if (now > tickBefore) return true;
            Thread.Sleep(1);
        }

        return false;
    }

    public bool CancelFleetJob(string fleetId, string note = "")
    {
        if (IsLoading) return false;
        if (string.IsNullOrWhiteSpace(fleetId)) return false;

        int tickBefore;
        _stateLock.EnterReadLock();
        try
        {
            tickBefore = _kernel.State.Tick;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }

        _stateLock.EnterWriteLock();
        try
        {
            _kernel.EnqueueCommand(new SimCore.Commands.FleetJobCancelCommand(fleetId, note));
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        var timeoutMs = Math.Max(250, (TickDelayMs * 3) + 50);
        WaitForTickAdvance(tickBefore, timeoutMs);
        return true;
    }


    // targetNodeId = "" clears manual override
    public bool SetFleetDestination(string fleetId, string targetNodeId, string note = "")
    {
        if (IsLoading) return false;
        if (string.IsNullOrWhiteSpace(fleetId)) return false;

        _stateLock.EnterWriteLock();
        try
        {
            _kernel.EnqueueCommand(new SimCore.Commands.FleetSetDestinationCommand(fleetId, targetNodeId ?? "", note));
            return true;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    // --- Intents: UI.002 contract (buy/sell generates intents) ---

    public void SubmitBuyIntent(string marketId, string goodId, int quantity)
    {
        if (IsLoading) return;
        if (string.IsNullOrWhiteSpace(marketId)) return;
        if (string.IsNullOrWhiteSpace(goodId)) return;
        if (quantity <= 0) return;

        EnqueueIntent(new BuyIntent(marketId, goodId, quantity));
    }

    public void SubmitSellIntent(string marketId, string goodId, int quantity)
    {
        if (IsLoading) return;
        if (string.IsNullOrWhiteSpace(marketId)) return;
        if (string.IsNullOrWhiteSpace(goodId)) return;
        if (quantity <= 0) return;

        EnqueueIntent(new SellIntent(marketId, goodId, quantity));
    }

    // Legacy GDScript API used by ActiveStation.gd (non-blocking intent submit with pre-checks)
    public bool TryBuyCargo(string marketId, string goodId, int quantity)
    {
        if (IsLoading) return false;
        if (string.IsNullOrWhiteSpace(marketId)) return false;
        if (string.IsNullOrWhiteSpace(goodId)) return false;
        if (quantity <= 0) return false;

        _stateLock.EnterReadLock();
        try
        {
            var state = _kernel.State;
            if (!state.Markets.TryGetValue(marketId, out var market)) return false;

            var price = market.GetPrice(goodId);
            if (price <= 0) return false;

            var total = (long)price * (long)quantity;
            if (state.PlayerCredits < total) return false;

            var supply = market.Inventory.TryGetValue(goodId, out var v) ? v : 0;
            if (supply < quantity) return false;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }

        SubmitBuyIntent(marketId, goodId, quantity);
        return true;
    }

    public bool TrySellCargo(string marketId, string goodId, int quantity)
    {
        if (IsLoading) return false;
        if (string.IsNullOrWhiteSpace(marketId)) return false;
        if (string.IsNullOrWhiteSpace(goodId)) return false;
        if (quantity <= 0) return false;

        _stateLock.EnterReadLock();
        try
        {
            var state = _kernel.State;

            var have = state.PlayerCargo.TryGetValue(goodId, out var v) ? v : 0;
            if (have < quantity) return false;

            if (!state.Markets.ContainsKey(marketId)) return false;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }

        SubmitSellIntent(marketId, goodId, quantity);
        return true;
    }

    // --- Read APIs for UI.001 (inventory, price, intel age) ---

    public int GetMarketPrice(string marketId, string goodId)
    {
        if (IsLoading) return 0;
        if (string.IsNullOrWhiteSpace(marketId)) return 0;
        if (string.IsNullOrWhiteSpace(goodId)) return 0;

        _stateLock.EnterReadLock();
        try
        {
            var state = _kernel.State;
            if (!state.Markets.TryGetValue(marketId, out var market)) return 0;
            return market.GetPrice(goodId);
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    public int GetIntelAgeTicks(string marketId, string goodId)
    {
        if (IsLoading) return -1;
        if (string.IsNullOrWhiteSpace(marketId)) return -1;
        if (string.IsNullOrWhiteSpace(goodId)) return -1;

        _stateLock.EnterReadLock();
        try
        {
            var state = _kernel.State;
            var view = IntelSystem.GetMarketGoodView(state, marketId, goodId);
            return view.AgeTicks;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    public Godot.Collections.Array GetSustainmentSnapshot(string marketId)
    {
        var arr = new Godot.Collections.Array();
        if (IsLoading) return arr;
        if (string.IsNullOrWhiteSpace(marketId)) return arr;

        _stateLock.EnterReadLock();
        try
        {
            var state = _kernel.State;

            var sites = SustainmentSnapshot.BuildForNode(state, marketId);

            foreach (var s in sites)
            {
                var d = new Godot.Collections.Dictionary
                {
                    ["site_id"] = s.SiteId,
                    ["node_id"] = s.NodeId,
                    ["health_bps"] = s.HealthBps,
                    ["eff_bps_now"] = s.EffBpsNow,
                    ["degrade_per_day_bps"] = s.DegradePerDayBps,
                    ["worst_buffer_margin"] = s.WorstBufferMargin,

                    ["time_to_starve_ticks"] = s.TimeToStarveTicks,
                    ["time_to_starve_days"] = s.TimeToStarveDays,
                    ["time_to_failure_ticks"] = s.TimeToFailureTicks,
                    ["time_to_failure_days"] = s.TimeToFailureDays,

                    ["starve_band"] = s.StarveBand,
                    ["fail_band"] = s.FailBand
                };

                var inputsArr = new Godot.Collections.Array();
                foreach (var inp in s.Inputs)
                {
                    inputsArr.Add(new Godot.Collections.Dictionary
                    {
                        ["good_id"] = inp.GoodId,
                        ["have_units"] = inp.HaveUnits,
                        ["per_tick_required"] = inp.PerTickRequired,
                        ["buffer_target_units"] = inp.BufferTargetUnits,

                        ["coverage_ticks"] = inp.CoverageTicks,
                        ["coverage_days"] = inp.CoverageDays,
                        ["buffer_margin"] = inp.BufferMargin,

                        ["coverage_band"] = inp.CoverageBand
                    });
                }

                d["inputs"] = inputsArr;
                arr.Add(d);
            }

            return arr;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }



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
            return _kernel.State.CreateAutoBuyProgram(marketId, goodId, quantity, cadenceTicks);
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

    public Godot.Collections.Array GetFleetExplainSnapshot()
    {
        var arr = new Godot.Collections.Array();
        if (IsLoading) return arr;

        _stateLock.EnterReadLock();
        try
        {
            var state = _kernel.State;

            // Deterministic ordering: Fleet.Id Ordinal
            var fleets = state.Fleets.Values
                    .OrderBy(f => f.Id, StringComparer.Ordinal)
                    .ToArray();

            foreach (var f in fleets)
            {
                var d = new Godot.Collections.Dictionary
                {
                    ["id"] = f.Id,
                    ["current_node_id"] = f.CurrentNodeId,
                    ["state"] = f.State.ToString(),
                    ["task"] = f.CurrentTask,

                    // Authority surface required by Slice 3 UI/play capstones
                    ["active_controller"] = f.ActiveController.ToString(),
                    ["program_id"] = f.ProgramId ?? "",
                    ["manual_override_node_id"] = f.ManualOverrideNodeId ?? "",

                    // Destination surfaces (stable strings)
                    ["destination_node_id"] = f.DestinationNodeId ?? "",
                    ["final_destination_node_id"] = f.FinalDestinationNodeId ?? "",

                    // Route progress required by GATE.UI.FLEET.001
                    ["route_edge_index"] = f.RouteEdgeIndex,
                    ["route_edge_total"] = (f.RouteEdgeIds != null) ? f.RouteEdgeIds.Count : 0,
                    ["route_progress"] = $"{f.RouteEdgeIndex}/{((f.RouteEdgeIds != null) ? f.RouteEdgeIds.Count : 0)}"
                };

                // Cargo summary required by GATE.UI.FLEET.001
                if (f.Cargo != null && f.Cargo.Count > 0)
                {
                    var parts = f.Cargo
                            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                            .Select(kv => $"{kv.Key}:{kv.Value}")
                            .ToArray();
                    d["cargo_summary"] = string.Join(", ", parts);
                }
                else
                {
                    d["cargo_summary"] = "(empty)";
                }

                // Job fields required by GATE.UI.FLEET.001
                if (f.CurrentJob != null)
                {
                    var j = f.CurrentJob;
                    d["job_phase"] = j.Phase.ToString();
                    d["job_good_id"] = j.GoodId ?? "";
                    d["job_amount"] = j.Amount;
                    d["job_picked_up_amount"] = j.PickedUpAmount;

                    // "remaining" for UI: while picking up, remaining = Amount - PickedUpAmount (best effort),
                    // while delivering, remaining = PickedUpAmount (amount to deliver).
                    int remaining;
                    if (j.Phase == SimCore.Entities.LogisticsJobPhase.Pickup)
                    {
                        remaining = Math.Max(0, j.Amount - j.PickedUpAmount);
                    }
                    else
                    {
                        remaining = Math.Max(0, j.PickedUpAmount);
                    }
                    d["job_remaining"] = remaining;
                }
                else
                {
                    d["job_phase"] = "";
                    d["job_good_id"] = "";
                    d["job_amount"] = 0;
                    d["job_picked_up_amount"] = 0;
                    d["job_remaining"] = 0;
                }

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


    // --- Fleet UI event log snapshot (Slice 3 / GATE.UI.FLEET.EVENT.001) ---
    // Returns the last N schema-bound logistics events for the given fleet, newest-first.
    // Determinism: filter by FleetId Ordinal, order by Seq desc with stable tie-breakers.
    public Godot.Collections.Array GetFleetEventLogSnapshot(string fleetId, int maxEvents = 25)
    {
        var arr = new Godot.Collections.Array();
        if (IsLoading) return arr;
        if (string.IsNullOrWhiteSpace(fleetId)) return arr;
        if (maxEvents <= 0) return arr;
        if (maxEvents > 200) maxEvents = 200;

        _stateLock.EnterReadLock();
        try
        {
            var events = _kernel.State.LogisticsEventLog;
            if (events == null || events.Count == 0) return arr;

            var slice = events
                    .Where(e => string.Equals(e.FleetId, fleetId, StringComparison.Ordinal))
                    .OrderByDescending(e => e.Seq)
                    .ThenByDescending(e => e.Tick)
                    .ThenByDescending(e => (int)e.Type)
                    .Take(maxEvents)
                    .ToArray();

            foreach (var e in slice)
            {
                var d = new Godot.Collections.Dictionary
                {
                    ["version"] = e.Version,
                    ["seq"] = e.Seq,
                    ["tick"] = e.Tick,
                    ["type"] = (int)e.Type,

                    ["fleet_id"] = e.FleetId,
                    ["good_id"] = e.GoodId,
                    ["amount"] = e.Amount,

                    ["source_node_id"] = e.SourceNodeId,
                    ["target_node_id"] = e.TargetNodeId,
                    ["source_market_id"] = e.SourceMarketId,
                    ["target_market_id"] = e.TargetMarketId,

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

    public string GetFleetPlayabilityTranscript(int maxEventsPerFleet = 10)
    {
        if (IsLoading) return "";
        if (maxEventsPerFleet < 0) maxEventsPerFleet = 0;
        if (maxEventsPerFleet > 200) maxEventsPerFleet = 200;

        _stateLock.EnterReadLock();
        try
        {
            var state = _kernel.State;

            var lines = new System.Collections.Generic.List<string>(256);
            lines.Add($"seed={WorldSeed} star_count={StarCount} tick={state.Tick}");

            // Deterministic ordering: Fleet.Id Ordinal
            var fleets = state.Fleets.Values
                    .OrderBy(f => f.Id, StringComparer.Ordinal)
                    .ToArray();

            foreach (var f in fleets)
            {
                var ctrl = f.ActiveController.ToString();
                var overrideTarget = f.ManualOverrideNodeId ?? "";
                var jobPhase = (f.CurrentJob != null) ? f.CurrentJob.Phase.ToString() : "";
                var jobGood = (f.CurrentJob != null) ? (f.CurrentJob.GoodId ?? "") : "";
                var jobAmt = (f.CurrentJob != null) ? f.CurrentJob.Amount : 0;
                var jobPicked = (f.CurrentJob != null) ? f.CurrentJob.PickedUpAmount : 0;

                lines.Add($"fleet={f.Id} node={f.CurrentNodeId} state={f.State} ctrl={ctrl} override={overrideTarget} task={f.CurrentTask} job_phase={jobPhase} job_good={jobGood} job_amt={jobAmt} job_picked={jobPicked} route={f.RouteEdgeIndex}/{((f.RouteEdgeIds != null) ? f.RouteEdgeIds.Count : 0)}");

                if (f.Cargo != null && f.Cargo.Count > 0)
                {
                    var cargoParts = f.Cargo
                            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                            .Select(kv => $"{kv.Key}:{kv.Value}")
                            .ToArray();
                    lines.Add($"  cargo={string.Join(",", cargoParts)}");
                }
                else
                {
                    lines.Add("  cargo=(empty)");
                }

                if (maxEventsPerFleet > 0 && state.LogisticsEventLog != null && state.LogisticsEventLog.Count > 0)
                {
                    var slice = state.LogisticsEventLog
                            .Where(e => string.Equals(e.FleetId, f.Id, StringComparison.Ordinal))
                            .OrderByDescending(e => e.Seq)
                            .ThenByDescending(e => e.Tick)
                            .ThenByDescending(e => (int)e.Type)
                            .Take(maxEventsPerFleet)
                            .ToArray();

                    foreach (var e in slice)
                    {
                        lines.Add($"  ev seq={e.Seq} tick={e.Tick} type={(int)e.Type} src_node={e.SourceNodeId} dst_node={e.TargetNodeId} src_mkt={e.SourceMarketId} dst_mkt={e.TargetMarketId} good={e.GoodId} amt={e.Amount} note={e.Note}");
                    }
                }
            }

            var transcript = string.Join("\n", lines);
            var hash = Fnv1a64(transcript);
            return $"hash64={hash:X16}\n" + transcript;
        }
        catch
        {
            return "";
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    // GDScript-friendly snapshot accessor
    public Godot.Collections.Dictionary GetPlayerSnapshot()

    {
        var dict = new Godot.Collections.Dictionary();
        if (IsLoading) return dict;

        _stateLock.EnterReadLock();
        try
        {
            dict["credits"] = _kernel.State.PlayerCredits;
            dict["location"] = _kernel.State.PlayerLocationNodeId;

            var cargo = new Godot.Collections.Dictionary();
            foreach (var kv in _kernel.State.PlayerCargo)
            {
                cargo[kv.Key] = kv.Value;
            }
            dict["cargo"] = cargo;

            return dict;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    public string GetMarketExplainTranscript(string marketId)
    {
        if (IsLoading) return "";
        if (string.IsNullOrWhiteSpace(marketId)) return "";
        _stateLock.EnterReadLock();
        try
        {
            var state = _kernel.State;

            var lines = new System.Collections.Generic.List<string>(256);
            lines.Add("[EXPLAIN] v1");
            lines.Add($"tick={state.Tick}");
            lines.Add($"market_id={marketId}");
            lines.Add($"player_credits={state.PlayerCredits}");
            lines.Add($"player_location={state.PlayerLocationNodeId}");

            if (state.Markets.TryGetValue(marketId, out var market))
            {
                lines.Add("market_inventory:");
                foreach (var kv in market.Inventory.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    lines.Add($"  {kv.Key}={kv.Value}");
                }
            }
            else
            {
                lines.Add("market_inventory: (missing)");
            }

            lines.Add("player_cargo:");
            foreach (var kv in state.PlayerCargo.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                lines.Add($"  {kv.Key}={kv.Value}");
            }

            lines.Add("programs:");
            if (state.Programs is null)
            {
                lines.Add("  (none)");
            }
            else
            {
                var programs = state.Programs.Instances.Values
                    .Where(p => string.Equals(p.MarketId, marketId, StringComparison.Ordinal))
                    .OrderBy(p => p.Id, StringComparer.Ordinal)
                    .ToArray();

                if (programs.Length == 0)
                {
                    lines.Add("  (none)");
                }
                else
                {
                    foreach (var p in programs)
                    {
                        var snap = ProgramQuoteSnapshot.Capture(state, p.Id);
                        var quote = ProgramQuote.BuildFromSnapshot(snap);

                        lines.Add($"  {quote.ProgramId} kind={quote.Kind} status={p.Status} good={quote.GoodId} qty={quote.Quantity} cad={quote.CadenceTicks}");
                        lines.Add($"    constraints: market_exists={quote.Constraints.MarketExists} credits_now={quote.Constraints.HasEnoughCreditsNow} supply_now={quote.Constraints.HasEnoughSupplyNow} cargo_now={quote.Constraints.HasEnoughCargoNow}");

                        if (quote.Risks.Count > 0)
                        {
                            lines.Add("    risks:");
                            foreach (var r in quote.Risks)
                            {
                                lines.Add($"      {r}");
                            }
                        }
                    }
                }
            }

            lines.Add("sustainment:");
            var sites = SustainmentSnapshot.BuildForNode(state, marketId)
                .OrderBy(s => s.SiteId, StringComparer.Ordinal)
                .ToArray();

            if (sites.Length == 0)
            {
                lines.Add("  (none)");
            }
            else
            {
                foreach (var s in sites)
                {
                    lines.Add($"  {s.SiteId} node={s.NodeId} health_bps={s.HealthBps} eff_bps={s.EffBpsNow} margin={s.WorstBufferMargin:0.00} starve={s.StarveBand} fail={s.FailBand}");
                    foreach (var inp in s.Inputs.OrderBy(i => i.GoodId, StringComparer.Ordinal))
                    {
                        lines.Add($"    {inp.GoodId} have={inp.HaveUnits} req_per_tick={inp.PerTickRequired} target={inp.BufferTargetUnits} cover={inp.CoverageBand} margin={inp.BufferMargin:0.00}");
                    }
                }
            }

            var transcript = string.Join("\n", lines);
            var hash = Fnv1a64(transcript);
            return $"hash64={hash:X16}\n" + transcript;
        }
        catch
        {
            return "";
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    private static ulong Fnv1a64(string s)
    {
        unchecked
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong h = offset;
            for (int i = 0; i < s.Length; i++)
            {
                h ^= (byte)s[i];
                h *= prime;
            }
            return h;
        }
    }

    public void RequestSave()
    {
        _saveRequested = true;
    }

    public void RequestLoad()
    {
        _loadRequested = true;
    }

    private void ExecuteSave()
    {
        try
        {
            _stateLock.EnterReadLock();
            string json;
            try
            {
                json = _kernel.SaveToString();
            }
            finally
            {
                _stateLock.ExitReadLock();
            }
            File.WriteAllText(_savePathAbs, json);

            Interlocked.Increment(ref _saveEpoch);
            _emitSaveCompletePending = true;

            GD.Print($"[BRIDGE] Saved: {_savePathAbs}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[BRIDGE] Save failed: {ex}");
        }
    }

    private void ExecuteLoad()
    {
        if (!File.Exists(_savePathAbs))
        {
            GD.Print("[BRIDGE] Load requested but no save exists.");
            return;
        }
        IsLoading = true;
        try
        {
            var json = File.ReadAllText(_savePathAbs);

            _stateLock.EnterWriteLock();
            try
            {
                _kernel.LoadFromString(json);
            }
            finally
            {
                _stateLock.ExitWriteLock();
            }

            Interlocked.Increment(ref _loadEpoch);
            _emitLoadCompletePending = true;

            GD.Print("[BRIDGE] Load complete.");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[BRIDGE] Load failed: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
