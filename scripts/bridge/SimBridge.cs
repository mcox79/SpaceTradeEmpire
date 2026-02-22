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

    // UI snapshots must not stall the main thread waiting for sim write locks.
    // If the read lock is busy (sim stepping), return cached values instead of blocking a frame.
    private readonly object _snapshotLock = new object();
    private long _cachedPlayerCredits = 0;
    private string _cachedPlayerLocation = "";
    private Godot.Collections.Dictionary _cachedPlayerCargo = new Godot.Collections.Dictionary();

    // Cached logistics snapshot (nonblocking UI readout).
    // If the read lock is busy, we return the last captured snapshot instead of stalling a frame.
    private Godot.Collections.Dictionary _cachedLogisticsSnapshot = new Godot.Collections.Dictionary();
    private string _cachedLogisticsSnapshotKey = "";

    // Cached dashboard snapshot (nonblocking UI readout).
    // If the read lock is busy, we return the last captured dashboard snapshot instead of stalling a frame.
    private Godot.Collections.Dictionary _cachedDashboardSnapshot = new Godot.Collections.Dictionary();

    // UI state persisted in quicksave (GATE.S3.UI.DASH.001)
    // Determinism: store as simple scalars with stable defaults.
    private int _uiStationViewIndex = 0;           // 0=Market%Traffic, 1=Logistics, 2=Sustainment, 3=Dash
    private int _uiDashboardLastSnapshotTick = -1; // last captured snapshot tick for dashboard metrics

    // UI selection state persisted in quicksave (GATE.UI.PLAY.TRADELOOP.SAVELOAD.001)
    // Determinism: store as simple scalar with stable default.
    private string _uiSelectedFleetId = "";

    // Cached program event log (deterministic, newest-first snapshots for UI).
    private sealed class ProgramEvent
    {
        public int Version = 1;
        public long Seq = 0;
        public int Tick = 0;
        public int Type = 0; // 1=Created, 2=StatusChanged, 3=Ran
        public string ProgramId = "";
        public string MarketId = "";
        public string GoodId = "";
        public string Note = "";
    }

    private sealed class ProgramSnap
    {
        public string Status = "";
        public int LastRunTick = -1;
        public string MarketId = "";
        public string GoodId = "";
        public int Quantity = 0;
    }

    private long _programEventSeq = 0;
    private readonly System.Collections.Generic.List<ProgramEvent> _programEventLog = new System.Collections.Generic.List<ProgramEvent>(512);
    private readonly System.Collections.Generic.Dictionary<string, ProgramSnap> _programSnapById = new System.Collections.Generic.Dictionary<string, ProgramSnap>(StringComparer.Ordinal);

    private sealed class ProgramSnapEntry
    {
        public string ProgramId { get; set; } = "";
        public ProgramSnap Snap { get; set; } = new ProgramSnap();
    }

    private sealed class ProgramEventLogSave
    {
        public int Version { get; set; } = 1;
        public long Seq { get; set; } = 0;
        public ProgramEvent[] Events { get; set; } = Array.Empty<ProgramEvent>();

        // Deterministic serialization: array sorted by ProgramId (Ordinal), not a dictionary.
        public ProgramSnapEntry[] Snaps { get; set; } = Array.Empty<ProgramSnapEntry>();
    }

    private sealed class UiStateSave
    {
        public int Version { get; set; } = 1;

        public int StationViewIndex { get; set; } = 0;
        public int DashboardLastSnapshotTick { get; set; } = -1;

        // Selected fleet in FleetMenu (empty means "no selection")
        public string SelectedFleetId { get; set; } = "";
    }

    private sealed class QuickSaveV2
    {
        public string Format { get; set; } = "STE_QUICKSAVE_V2";
        public JsonElement Kernel { get; set; }
        public ProgramEventLogSave ProgramEventLog { get; set; } = new ProgramEventLogSave();

        // UI state persistence (selected tab/view + last dashboard snapshot tick)
        public UiStateSave UiState { get; set; } = new UiStateSave();
    }

    private static JsonSerializerOptions CreateDeterministicJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        };
    }

    private static JsonElement ParseJsonElement(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static bool TryParseQuickSaveV2(string text, out QuickSaveV2? qs)
    {
        qs = null;
        try
        {
            qs = JsonSerializer.Deserialize<QuickSaveV2>(text, CreateDeterministicJsonOptions());
            if (qs == null) return false;
            if (!string.Equals(qs.Format, "STE_QUICKSAVE_V2", StringComparison.Ordinal)) return false;
            return true;
        }
        catch
        {
            qs = null;
            return false;
        }
    }

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
                    UpdateProgramEventLog_AfterStep(_kernel.State);
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

    // Nonblocking variant for UI: avoids frame hitches by not waiting behind sim write locks.
    // Returns false if the lock could not be acquired within timeoutMs.
    public bool TryExecuteSafeRead(Action<SimState> action, int timeoutMs = 0)
    {
        if (IsLoading) return false;
        if (action == null) return false;

        if (!_stateLock.TryEnterReadLock(timeoutMs))
        {
            return false;
        }

        try
        {
            action(_kernel.State);
            return true;
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

        // Defensive: if caller is already inside a safe-read (or write) lock on this thread,
        // do NOT attempt to re-enter the lock (LockRecursionPolicy.NoRecursion).
        if (_stateLock.IsReadLockHeld || _stateLock.IsWriteLockHeld)
        {
            return GetIntelAgeTicks_NoLock(_kernel.State, marketId, goodId);
        }

        var age = -1;
        TryExecuteSafeRead(state =>
        {
            age = GetIntelAgeTicks_NoLock(state, marketId, goodId);
        });
        return age;
    }

    // IMPORTANT: caller must already be inside TryExecuteSafeRead.
    // This method must not acquire _stateLock or call other locking bridge methods.
    public int GetIntelAgeTicks_NoLock(SimCore.SimState state, string marketId, string goodId)
    {
        var view = IntelSystem.GetMarketGoodView(state, marketId, goodId);
        return view.AgeTicks;
    }

    public Godot.Collections.Array GetSustainmentSnapshot(string marketId)
    {
        if (IsLoading) return new Godot.Collections.Array();
        if (string.IsNullOrWhiteSpace(marketId)) return new Godot.Collections.Array();

        // Defensive: if caller is already inside a safe-read (or write) lock on this thread,
        // do NOT attempt to re-enter the lock (LockRecursionPolicy.NoRecursion).
        if (_stateLock.IsReadLockHeld || _stateLock.IsWriteLockHeld)
        {
            return GetSustainmentSnapshot_NoLock(_kernel.State, marketId);
        }

        _stateLock.EnterReadLock();
        try
        {
            return GetSustainmentSnapshot_NoLock(_kernel.State, marketId);
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    // IMPORTANT: caller must already be inside TryExecuteSafeRead.
    // Do NOT acquire _stateLock or call other locking bridge methods.
    public Godot.Collections.Array GetSustainmentSnapshot_NoLock(SimCore.SimState state, string marketId)
    {
        var arr = new Godot.Collections.Array();
        if (state == null) return arr;
        if (string.IsNullOrWhiteSpace(marketId)) return arr;

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
                ["fail_band"] = s.FailBand,
            };

            var inputsArr = new Godot.Collections.Array();
            foreach (var i in s.Inputs)
            {
                var id = new Godot.Collections.Dictionary
                {
                    ["good_id"] = i.GoodId,

                    ["have_units"] = i.HaveUnits,
                    ["per_tick_required"] = i.PerTickRequired,
                    ["buffer_target_units"] = i.BufferTargetUnits,

                    ["coverage_ticks"] = i.CoverageTicks,
                    ["coverage_days"] = i.CoverageDays,
                    ["coverage_band"] = i.CoverageBand,
                    ["buffer_margin"] = i.BufferMargin,
                };
                inputsArr.Add(id);
            }

            d["inputs"] = inputsArr;
            arr.Add(d);
        }

        return arr;
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

        // Never block the main thread behind sim stepping.
        if (_stateLock.TryEnterReadLock(0))
        {
            try
            {
                var credits = _kernel.State.PlayerCredits;
                var location = _kernel.State.PlayerLocationNodeId ?? "";

                var cargo = new Godot.Collections.Dictionary();
                foreach (var kv in _kernel.State.PlayerCargo)
                {
                    cargo[kv.Key] = kv.Value;
                }

                dict["credits"] = credits;
                dict["location"] = location;
                dict["cargo"] = cargo;

                // Update cache for the next time we can't acquire the lock.
                lock (_snapshotLock)
                {
                    _cachedPlayerCredits = credits;
                    _cachedPlayerLocation = location;
                    _cachedPlayerCargo = cargo; // cargo is a fresh dictionary, safe to reuse as cache
                }

                return dict;
            }
            finally
            {
                _stateLock.ExitReadLock();
            }
        }

        // Lock busy: return cached snapshot (best-effort, nonblocking).
        lock (_snapshotLock)
        {
            dict["credits"] = _cachedPlayerCredits;
            dict["location"] = _cachedPlayerLocation;
            dict["cargo"] = _cachedPlayerCargo;
            return dict;
        }
    }

    // --- Station logistics snapshot (GATE.UI.LOGISTICS.001) ---
    // Minimal readout via SimBridge facts: active jobs + buffer deficits (shortages) + bottlenecks.
    // Determinism:
    // - jobs ordered by FleetId Ordinal
    // - shortages ordered by deficit desc, then GoodId Ordinal, then SiteId Ordinal
    // Failure safety:
    // - never blocks UI thread; returns cached snapshot when sim holds write lock
    public Godot.Collections.Dictionary GetLogisticsStationSnapshot(string nodeOrMarketId, int maxItems = 8)
    {
        var dict = new Godot.Collections.Dictionary();
        if (IsLoading) return dict;

        if (string.IsNullOrWhiteSpace(nodeOrMarketId))
        {
            dict["status"] = "NO_KEY";
            return dict;
        }

        if (maxItems <= 0) maxItems = 1;
        if (maxItems > 50) maxItems = 50;

        // Never block the main thread behind sim stepping.
        if (_stateLock.TryEnterReadLock(0))
        {
            try
            {
                var state = _kernel.State;

                var marketId = ResolveMarketIdFromNodeOrMarket(state, nodeOrMarketId);
                if (string.IsNullOrWhiteSpace(marketId) || !state.Markets.ContainsKey(marketId))
                {
                    dict["status"] = "NO_MARKET";
                    dict["key"] = nodeOrMarketId;
                    dict["market_id"] = marketId ?? "";
                }
                else
                {
                    dict["status"] = "OK";
                    dict["key"] = nodeOrMarketId;
                    dict["market_id"] = marketId;
                }

                var jobsArr = new Godot.Collections.Array();
                var shortagesArr = new Godot.Collections.Array();

                // Jobs touching this market (source or target), deterministic by FleetId.
                foreach (var fleet in state.Fleets.Values.OrderBy(f => f.Id, StringComparer.Ordinal))
                {
                    var job = fleet.CurrentJob;
                    if (job is null) continue;

                    var srcMarketId = ResolveMarketIdFromNodeOrMarket(state, job.SourceNodeId ?? "");
                    var dstMarketId = ResolveMarketIdFromNodeOrMarket(state, job.TargetNodeId ?? "");

                    if (!string.IsNullOrWhiteSpace(marketId) &&
                        !string.Equals(srcMarketId, marketId, StringComparison.Ordinal) &&
                        !string.Equals(dstMarketId, marketId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    int remaining;
                    if (job.Phase == SimCore.Entities.LogisticsJobPhase.Pickup)
                    {
                        remaining = Math.Max(0, job.Amount - job.PickedUpAmount);
                    }
                    else
                    {
                        remaining = Math.Max(0, job.PickedUpAmount);
                    }

                    var jd = new Godot.Collections.Dictionary
                    {
                        ["fleet_id"] = fleet.Id ?? "",
                        ["phase"] = job.Phase.ToString(),
                        ["good_id"] = job.GoodId ?? "",
                        ["amount"] = job.Amount,
                        ["picked_up_amount"] = job.PickedUpAmount,
                        ["remaining"] = remaining,
                        ["source_node_id"] = job.SourceNodeId ?? "",
                        ["target_node_id"] = job.TargetNodeId ?? "",
                        ["source_market_id"] = srcMarketId ?? "",
                        ["target_market_id"] = dstMarketId ?? ""
                    };

                    jobsArr.Add(jd);
                }

                // Buffer deficits for industry sites at this market (shortages), plus bottlenecks as top deficits.
                if (!string.IsNullOrWhiteSpace(marketId) &&
                    state.Markets.TryGetValue(marketId, out var market))
                {
                    var deficits = new System.Collections.Generic.List<(string SiteId, string GoodId, int Target, int Current, int Deficit)>();

                    foreach (var site in state.IndustrySites.Values.OrderBy(s => s.Id, StringComparer.Ordinal))
                    {
                        if (!site.Active) continue;
                        if (string.IsNullOrWhiteSpace(site.NodeId)) continue;

                        var siteMarketId = ResolveMarketIdFromNodeOrMarket(state, site.NodeId);
                        if (!string.Equals(siteMarketId, marketId, StringComparison.Ordinal)) continue;

                        foreach (var input in site.Inputs.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                        {
                            var goodId = input.Key;
                            var perTick = input.Value;
                            if (string.IsNullOrWhiteSpace(goodId)) continue;
                            if (perTick <= 0) continue;

                            var target = IndustrySystem.ComputeBufferTargetUnits(site, goodId);
                            var current = market.Inventory.TryGetValue(goodId, out var curUnits) ? curUnits : 0;
                            var deficit = target - current;
                            if (deficit <= 0) continue;

                            deficits.Add((site.Id ?? "", goodId, target, current, deficit));
                        }
                    }

                    var ordered = deficits
                        .OrderByDescending(x => x.Deficit)
                        .ThenBy(x => x.GoodId, StringComparer.Ordinal)
                        .ThenBy(x => x.SiteId, StringComparer.Ordinal)
                        .Take(maxItems)
                        .ToArray();

                    foreach (var d in ordered)
                    {
                        shortagesArr.Add(new Godot.Collections.Dictionary
                        {
                            ["site_id"] = d.SiteId,
                            ["good_id"] = d.GoodId,
                            ["current"] = d.Current,
                            ["target"] = d.Target,
                            ["deficit"] = d.Deficit
                        });
                    }

                    dict["shortage_count"] = deficits.Count;
                }
                else
                {
                    dict["shortage_count"] = 0;
                }

                dict["jobs"] = jobsArr;
                dict["job_count"] = jobsArr.Count;
                dict["shortages"] = shortagesArr;
                dict["bottleneck_count"] = shortagesArr.Count;

                // Station incident timeline (GATE.UI.LOGISTICS.EVENT.001)
                // Determinism: order by Seq desc, then Tick desc, then Type desc, then FleetId Ordinal.
                // Failure safety: snapshot is lock-scoped and never blocks UI thread beyond TryEnterReadLock(0).
                var eventsArr = new Godot.Collections.Array();
                if (!string.IsNullOrWhiteSpace(marketId) &&
                    state.LogisticsEventLog != null &&
                    state.LogisticsEventLog.Count > 0)
                {
                    var maxEvents = Math.Min(200, Math.Max(1, maxItems) * 6);

                    var slice = state.LogisticsEventLog
                        .Where(e =>
                            string.Equals(e.SourceMarketId, marketId, StringComparison.Ordinal) ||
                            string.Equals(e.TargetMarketId, marketId, StringComparison.Ordinal))
                        .OrderByDescending(e => e.Seq)
                        .ThenByDescending(e => e.Tick)
                        .ThenByDescending(e => (int)e.Type)
                        .ThenBy(e => e.FleetId, StringComparer.Ordinal)
                        .Take(maxEvents)
                        .ToArray();

                    foreach (var e in slice)
                    {
                        eventsArr.Add(new Godot.Collections.Dictionary
                        {
                            ["version"] = e.Version,
                            ["seq"] = e.Seq,
                            ["tick"] = e.Tick,
                            ["type"] = (int)e.Type,

                            ["fleet_id"] = e.FleetId ?? "",
                            ["good_id"] = e.GoodId ?? "",
                            ["amount"] = e.Amount,

                            ["source_node_id"] = e.SourceNodeId ?? "",
                            ["target_node_id"] = e.TargetNodeId ?? "",
                            ["source_market_id"] = e.SourceMarketId ?? "",
                            ["target_market_id"] = e.TargetMarketId ?? "",

                            ["note"] = e.Note ?? ""
                        });
                    }
                }

                dict["events"] = eventsArr;
                dict["event_count"] = eventsArr.Count;

                // Cache for the next time we can't acquire the lock.
                lock (_snapshotLock)
                {
                    _cachedLogisticsSnapshotKey = nodeOrMarketId;
                    _cachedLogisticsSnapshot = dict;
                }

                return dict;

            }
            finally
            {
                _stateLock.ExitReadLock();
            }
        }

        // Lock busy: return cached snapshot (best-effort, nonblocking).
        lock (_snapshotLock)
        {
            // If key changed, still return cached snapshot rather than blocking.
            return _cachedLogisticsSnapshot;
        }
    }

    // --- Dashboards v0 (GATE.S3.UI.DASH.001) ---
    // UI exposes deterministic metrics from the last snapshot tick:
    // - total_shipments: count of active fleet jobs
    // - avg_delay_ticks: avg (snapshot_tick - last_job_event_tick) over active jobs, best-effort
    // - top3_bottleneck_lanes: derived from logistics event notes containing LaneCapacity markers
    // - top3_profit_loops: best 2-hop A>B>A loop proxies from market prices (ties lex)
    // Failure safety: never blocks UI thread; returns cached snapshot when sim holds write lock.
    public Godot.Collections.Dictionary GetDashboardSnapshot(int topN = 3)
    {
        if (topN <= 0) topN = 1;
        if (topN > 10) topN = 10;

        var dict = new Godot.Collections.Dictionary();
        if (IsLoading) return dict;

        if (_stateLock.TryEnterReadLock(0))
        {
            try
            {
                var state = _kernel.State;
                var snapTick = state.Tick;

                // total_shipments = active jobs (deterministic by definition, no ordering).
                var fleets = state.Fleets.Values
                    .OrderBy(f => f.Id, StringComparer.Ordinal)
                    .ToArray();

                var activeJobs = fleets.Where(f => f.CurrentJob != null).ToArray();
                dict["snapshot_tick"] = snapTick;
                dict["total_shipments"] = activeJobs.Length;

                // avg_delay_ticks: best-effort, based on last event tick for that fleet (pickup/dropoff issued),
                // falling back to 0 if no event is found.
                long delaySum = 0;
                int delayCount = 0;

                if (state.LogisticsEventLog != null && state.LogisticsEventLog.Count > 0)
                {
                    // Build a last-event-tick map for fleets with deterministic tie break:
                    // pick max Tick, then max Seq for the fleet.
                    var lastTickByFleet = new System.Collections.Generic.Dictionary<string, (int Tick, long Seq)>(StringComparer.Ordinal);

                    foreach (var e in state.LogisticsEventLog)
                    {
                        var fid = e.FleetId ?? "";
                        if (string.IsNullOrWhiteSpace(fid)) continue;

                        // Only consider job lifecycle events as delay anchors (name-based, stable).
                        var typeName = e.Type.ToString();
                        if (!(typeName.Contains("Issued", StringComparison.Ordinal) ||
                              typeName.Contains("Queued", StringComparison.Ordinal) ||
                              typeName.Contains("Pickup", StringComparison.Ordinal) ||
                              typeName.Contains("Dropoff", StringComparison.Ordinal)))
                        {
                            continue;
                        }

                        var tick = e.Tick;
                        var seq = e.Seq;

                        if (lastTickByFleet.TryGetValue(fid, out var cur))
                        {
                            if (tick > cur.Tick || (tick == cur.Tick && seq > cur.Seq))
                                lastTickByFleet[fid] = (tick, seq);
                        }
                        else
                        {
                            lastTickByFleet[fid] = (tick, seq);
                        }
                    }

                    foreach (var f in activeJobs)
                    {
                        var fid = f.Id ?? "";
                        if (string.IsNullOrWhiteSpace(fid)) continue;

                        if (lastTickByFleet.TryGetValue(fid, out var lt))
                        {
                            var d = Math.Max(0, snapTick - lt.Tick);
                            delaySum += d;
                            delayCount++;
                        }
                    }
                }

                var avgDelay = (delayCount <= 0) ? 0 : (int)(delaySum / delayCount);
                dict["avg_delay_ticks"] = avgDelay;

                // top3_bottleneck_lanes from event note markers on this snapshot tick.
                // We look for notes containing "Reason=LaneCapacity" or "LaneCapacity".
                var laneCounts = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);

                if (state.LogisticsEventLog != null && state.LogisticsEventLog.Count > 0)
                {
                    foreach (var e in state.LogisticsEventLog)
                    {
                        if (e.Tick != snapTick) continue;

                        var note = e.Note ?? "";
                        if (string.IsNullOrWhiteSpace(note)) continue;

                        if (!(note.Contains("LaneCapacity", StringComparison.Ordinal) || note.Contains("Reason=LaneCapacity", StringComparison.Ordinal)))
                            continue;

                        // Parse lane id from common patterns: "LaneId=<id>" or "lane=<id>"
                        string laneId = "";
                        var idx = note.IndexOf("LaneId=", StringComparison.Ordinal);
                        if (idx >= 0)
                        {
                            var start = idx + "LaneId=".Length;
                            var end = note.IndexOfAny(new[] { ' ', ';', ',', '\t' }, start);
                            laneId = (end >= 0) ? note.Substring(start, end - start) : note.Substring(start);
                        }
                        else
                        {
                            idx = note.IndexOf("lane=", StringComparison.Ordinal);
                            if (idx >= 0)
                            {
                                var start = idx + "lane=".Length;
                                var end = note.IndexOfAny(new[] { ' ', ';', ',', '\t' }, start);
                                laneId = (end >= 0) ? note.Substring(start, end - start) : note.Substring(start);
                            }
                        }

                        laneId = laneId?.Trim() ?? "";
                        if (string.IsNullOrWhiteSpace(laneId)) continue;

                        laneCounts.TryGetValue(laneId, out var c);
                        laneCounts[laneId] = c + 1;
                    }
                }

                var topLanes = laneCounts
                    .OrderByDescending(kv => kv.Value)
                    .ThenBy(kv => kv.Key, StringComparer.Ordinal)
                    .Take(topN)
                    .ToArray();

                var lanesArr = new Godot.Collections.Array();
                foreach (var kv in topLanes)
                {
                    lanesArr.Add(new Godot.Collections.Dictionary
                    {
                        ["lane_id"] = kv.Key,
                        ["count"] = kv.Value
                    });
                }
                dict["top3_bottleneck_lanes"] = lanesArr;

                // top3_profit_loops proxy: best 2-hop A>B>A where each leg uses the best positive price diff good.
                // Determinism: markets sorted, goods sorted, tie-break goods lex, then route_id lex.
                var markets = state.Markets.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();

                (string GoodId, int Profit) BestLeg(string from, string to)
                {
                    if (!state.Markets.TryGetValue(from, out var mFrom)) return ("", 0);
                    if (!state.Markets.TryGetValue(to, out var mTo)) return ("", 0);

                    string bestGood = "";
                    int bestProfit = 0;

                    foreach (var good in mFrom.Inventory.Keys.OrderBy(k => k, StringComparer.Ordinal))
                    {
                        var pFrom = mFrom.GetPrice(good);
                        var pTo = mTo.GetPrice(good);
                        var profit = pTo - pFrom;
                        if (profit <= 0) continue;

                        if (profit > bestProfit)
                        {
                            bestProfit = profit;
                            bestGood = good;
                        }
                        else if (profit == bestProfit && profit > 0 && string.CompareOrdinal(good, bestGood) < 0)
                        {
                            bestGood = good;
                        }
                    }

                    return (bestGood, bestProfit);
                }

                var loops = new System.Collections.Generic.List<(string RouteId, string A, string B, string GoodAB, string GoodBA, int NetProfit)>(64);

                for (int i = 0; i < markets.Length; i++)
                {
                    for (int j = 0; j < markets.Length; j++)
                    {
                        if (i == j) continue;

                        var a = markets[i];
                        var b = markets[j];

                        var leg1 = BestLeg(a, b);
                        var leg2 = BestLeg(b, a);

                        if (leg1.Profit <= 0 || leg2.Profit <= 0) continue;

                        var net = leg1.Profit + leg2.Profit;
                        var routeId = $"{a}>{b}>{a}";

                        loops.Add((routeId, a, b, leg1.GoodId, leg2.GoodId, net));
                    }
                }

                var topLoops = loops
                    .OrderByDescending(x => x.NetProfit)
                    .ThenBy(x => x.RouteId, StringComparer.Ordinal)
                    .Take(topN)
                    .ToArray();

                var loopsArr = new Godot.Collections.Array();
                foreach (var l in topLoops)
                {
                    loopsArr.Add(new Godot.Collections.Dictionary
                    {
                        ["route_id"] = l.RouteId,
                        ["from_market_id"] = l.A,
                        ["to_market_id"] = l.B,
                        ["good_ab"] = l.GoodAB,
                        ["good_ba"] = l.GoodBA,
                        ["net_profit_proxy"] = l.NetProfit
                    });
                }
                dict["top3_profit_loops"] = loopsArr;

                // Persist last snapshot tick for save%load.
                _uiDashboardLastSnapshotTick = snapTick;

                // Cache for the next time we can't acquire the lock.
                lock (_snapshotLock)
                {
                    _cachedDashboardSnapshot = dict;
                }

                return dict;
            }
            finally
            {
                _stateLock.ExitReadLock();
            }
        }

        lock (_snapshotLock)
        {
            return _cachedDashboardSnapshot;
        }
    }

    public int GetUiStationViewIndex() => _uiStationViewIndex;

    public void SetUiStationViewIndex(int idx)
    {
        _uiStationViewIndex = Math.Clamp(idx, 0, 3);
    }

    public int GetUiDashboardLastSnapshotTick() => _uiDashboardLastSnapshotTick;

    public string GetUiSelectedFleetId() => _uiSelectedFleetId;

    public void SetUiSelectedFleetId(string fleetId)
    {
        _uiSelectedFleetId = fleetId ?? "";
    }

    private static string ResolveMarketIdFromNodeOrMarket(SimState state, string nodeOrMarketId)
    {
        if (state is null) return "";
        if (string.IsNullOrWhiteSpace(nodeOrMarketId)) return "";

        if (state.Markets.ContainsKey(nodeOrMarketId)) return nodeOrMarketId;

        if (state.Nodes.TryGetValue(nodeOrMarketId, out var node))
        {
            if (!string.IsNullOrWhiteSpace(node.MarketId) && state.Markets.ContainsKey(node.MarketId))
            {
                return node.MarketId;
            }
        }

        return "";
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
            string kernelJson;
            QuickSaveV2 qs;
            try
            {
                kernelJson = _kernel.SaveToString();

                var snaps = _programSnapById
                    .OrderBy(k => k.Key, StringComparer.Ordinal)
                    .Select(k => new ProgramSnapEntry { ProgramId = k.Key, Snap = k.Value })
                    .ToArray();

                var logSave = new ProgramEventLogSave
                {
                    Version = 1,
                    Seq = _programEventSeq,
                    Events = _programEventLog.ToArray(),
                    Snaps = snaps
                };

                // Persist UI state deterministically as scalar fields.
                var ui = new UiStateSave
                {
                    Version = 1,
                    StationViewIndex = _uiStationViewIndex,
                    DashboardLastSnapshotTick = _uiDashboardLastSnapshotTick,
                    SelectedFleetId = _uiSelectedFleetId ?? ""
                };

                qs = new QuickSaveV2
                {
                    Format = "STE_QUICKSAVE_V2",
                    Kernel = ParseJsonElement(kernelJson),
                    ProgramEventLog = logSave,
                    UiState = ui
                };
            }
            finally
            {
                _stateLock.ExitReadLock();
            }

            var json = JsonSerializer.Serialize(qs, CreateDeterministicJsonOptions());
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
            var text = File.ReadAllText(_savePathAbs);

            var isV2 = TryParseQuickSaveV2(text, out var qs);

            _stateLock.EnterWriteLock();
            try
            {
                if (isV2 && qs != null)
                {
                    var kernelText = qs.Kernel.GetRawText();
                    _kernel.LoadFromString(kernelText);

                    // Restore program event log (deterministic content, preserved order).
                    _programEventSeq = qs.ProgramEventLog.Seq;

                    _programEventLog.Clear();
                    if (qs.ProgramEventLog.Events != null && qs.ProgramEventLog.Events.Length > 0)
                    {
                        _programEventLog.AddRange(qs.ProgramEventLog.Events);
                    }

                    _programSnapById.Clear();
                    if (qs.ProgramEventLog.Snaps != null)
                    {
                        foreach (var se in qs.ProgramEventLog.Snaps)
                        {
                            if (se == null) continue;
                            if (string.IsNullOrWhiteSpace(se.ProgramId)) continue;
                            if (se.Snap == null) continue;
                            _programSnapById[se.ProgramId] = se.Snap;
                        }
                    }

                    // Restore UI state (safe defaults if missing / older saves).
                    if (qs.UiState != null)
                    {
                        _uiStationViewIndex = Math.Clamp(qs.UiState.StationViewIndex, 0, 3);
                        _uiDashboardLastSnapshotTick = qs.UiState.DashboardLastSnapshotTick;
                        _uiSelectedFleetId = qs.UiState.SelectedFleetId ?? "";
                    }
                    else
                    {
                        _uiStationViewIndex = 0;
                        _uiDashboardLastSnapshotTick = -1;
                        _uiSelectedFleetId = "";
                    }
                }
                else
                {
                    // Backward compatible: old format is kernel JSON only.
                    _kernel.LoadFromString(text);

                    _programEventSeq = 0;
                    _programEventLog.Clear();
                    _programSnapById.Clear();

                    _uiStationViewIndex = 0;
                    _uiDashboardLastSnapshotTick = -1;
                    _uiSelectedFleetId = "";
                }
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
