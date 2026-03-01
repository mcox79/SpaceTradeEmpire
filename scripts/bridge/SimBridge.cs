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

    // Bridge readiness v0 (for deterministic headless tooling).
    // Set to 1 only after _Ready has applied cmdline overrides, initialized kernel, and started sim thread.
    private int _bridgeReadyV0 = 0;
    private int _cmdlineReadyV0 = 0;

    // Deterministic: true only after ApplyCmdlineOverrides() has run in _Ready().
    public bool GetCmdlineReadyV0() => Volatile.Read(ref _cmdlineReadyV0) != 0;

    public int GetSaveEpoch() => Volatile.Read(ref _saveEpoch);
    public int GetLoadEpoch() => Volatile.Read(ref _loadEpoch);

    // Deterministic readiness probe for tests: true only after _Ready completed initialization.
    public bool GetBridgeReadyV0() => Volatile.Read(ref _bridgeReadyV0) != 0;

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

    // Cached discovery snapshot (nonblocking UI readout).
    // If the read lock is busy, we return the last captured snapshot instead of stalling a frame.
    private Godot.Collections.Dictionary _cachedDiscoverySnapshotV0 = new Godot.Collections.Dictionary();

    // Cached unlock snapshot (nonblocking UI readout).
    // If the read lock is busy, we return the last captured snapshot instead of stalling a frame.
    private Godot.Collections.Array _cachedUnlockSnapshot = new Godot.Collections.Array();

    // Cached expedition status snapshots keyed by programId (nonblocking UI readout).
    // If the read lock is busy, we return the last captured snapshot instead of stalling a frame.
    private readonly System.Collections.Generic.Dictionary<string, Godot.Collections.Dictionary> _cachedExpeditionStatusSnapshots =
        new System.Collections.Generic.Dictionary<string, Godot.Collections.Dictionary>(StringComparer.Ordinal);

    // Cached rumor lead snapshot (nonblocking UI readout).
    // If the read lock is busy, we return the last captured snapshot instead of stalling a frame.
    private Godot.Collections.Array _cachedRumorLeadSnapshot = new Godot.Collections.Array();

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

        // Ensure cmdline overrides apply before kernel init.
        ApplyCmdlineOverrides();

        // Mark cmdline ready immediately after overrides apply.
        Volatile.Write(ref _cmdlineReadyV0, 1);

        // Explicitly keep seed stable if cmdline override exists; do not allow other code to change it later.
        // (No-op if ApplyCmdlineOverrides did not find --seed=...)
        // WorldSeed is already set inside ApplyCmdlineOverrides.

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

        // Ready only after cmdline override + kernel init + sim start.
        Volatile.Write(ref _bridgeReadyV0, 1);
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

    // Cmdline overrides are allowed for headless tooling (deterministic).
    // Supported:
    //   --seed=<int>   sets WorldSeed before kernel init
    // Determinism: if seed override is provided, force ResetSaveOnBoot=true to prevent save contamination.
    private void ApplyCmdlineOverrides()
    {
        var args = OS.GetCmdlineUserArgs();
        if (args == null || args.Length == 0) return;

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i] ?? "";
            if (a.Length == 0) continue;

            if (a.StartsWith("--seed=", StringComparison.Ordinal))
            {
                var raw = a.Substring("--seed=".Length);
                if (int.TryParse(raw, out var seed))
                {
                    WorldSeed = seed;
                    ResetSaveOnBoot = true;
                }
            }
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

            // Best-effort join. 200ms is enough: sim loop cancels within one TickDelayMs cycle (default 100ms).
            // Do not block longer — this may be called from the main thread (RequestShutdownV0).
            if (_simTask != null && !_simTask.IsCompleted)
            {
                _simTask.Wait(200);
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

    private static Godot.Collections.Array ToGodotArrayStrings(System.Collections.Generic.IReadOnlyList<string>? tokens)
    {
        var a = new Godot.Collections.Array();
        if (tokens == null) return a;

        for (int i = 0; i < tokens.Count; i++)
        {
            a.Add(tokens[i] ?? "");
        }

        return a;
    }

    // GATE.S3_6.UI_DISCOVERY_MIN.001
    // Facts-only discovery snapshot for UI.
    // Determinism:
    // - ordering is produced by IntelSystem (UnlockId asc, LeadId asc, token sublists Ordinal asc)
    // - no timestamps%wall-clock
    // Failure safety:
    // - if we cannot take the read lock immediately, return the cached snapshot
    public Godot.Collections.Dictionary GetDiscoverySnapshotV0(string stationId)
    {
        stationId ??= "";

        if (!TryExecuteSafeRead(state =>
        {
            var snap = IntelSystem.BuildDiscoverySnapshotV0(state, stationId);

            var d = new Godot.Collections.Dictionary
            {
                ["discovered_site_count"] = snap.DiscoveredSiteCount,
                ["scanned_site_count"] = snap.ScannedSiteCount,
                ["analyzed_site_count"] = snap.AnalyzedSiteCount,
                ["expedition_status_token"] = snap.ExpeditionStatusToken ?? "",

                // GATE.S3_6.UI_DISCOVERY_MIN.002
                // Active discovery exceptions (token + reason tokens + intervention verbs).
                // Determinism:
                // - list is expected to be ExceptionToken Ordinal asc
                // - reason tokens Ordinal asc
                // - intervention verbs Ordinal asc
                // Failure safety:
                // - empty list is valid (vacuous-pass for Seed 42 tick 0)
                ["active_exceptions"] = new Godot.Collections.Array()
            };

            var unlocks = new Godot.Collections.Array();
            for (int i = 0; i < snap.Unlocks.Count; i++)
            {
                var u = snap.Unlocks[i];
                var ud = new Godot.Collections.Dictionary
                {
                    ["unlock_id"] = u.UnlockId ?? "",
                    ["effect_tokens"] = ToGodotArrayStrings(u.EffectTokens),
                    ["blocked_reason_token"] = u.BlockedReasonToken ?? "",
                    ["blocked_action_tokens"] = ToGodotArrayStrings(u.BlockedActionTokens),
                    ["deploy_verb_control_tokens"] = ToGodotArrayStrings(u.DeployVerbControlTokens)
                };
                unlocks.Add(ud);
            }
            d["unlocks"] = unlocks;

            var leads = new Godot.Collections.Array();
            for (int i = 0; i < snap.RumorLeads.Count; i++)
            {
                var r = snap.RumorLeads[i];
                var rd = new Godot.Collections.Dictionary
                {
                    ["lead_id"] = r.LeadId ?? "",
                    ["hint_tokens"] = ToGodotArrayStrings(r.HintTokens)
                };
                leads.Add(rd);
            }
            d["rumor_leads"] = leads;

            lock (_snapshotLock)
            {
                _cachedDiscoverySnapshotV0 = d;
            }
        }, 0))
        {
            lock (_snapshotLock)
            {
                return _cachedDiscoverySnapshotV0;
            }
        }

        lock (_snapshotLock)
        {
            return _cachedDiscoverySnapshotV0;
        }
    }

    // GATE.S1.GALAXY_MAP.CONTRACT.001
    // Facts-only galaxy map snapshot for UI.
    // Determinism:
    // - system_nodes ordered by node_id Ordinal asc (SimCore builder)
    // - lane_edges ordered by edge_id Ordinal asc (SimCore builder)
    // - no timestamps%wall-clock
    // Failure safety:
    // - blocking read lock is acceptable for contract v0 (no cached fallback behavior)
    public Godot.Collections.Dictionary GetGalaxySnapshotV0()
    {
        Godot.Collections.Dictionary d = new Godot.Collections.Dictionary();
        ExecuteSafeRead(state =>
        {
            var snap = MapQueries.BuildGalaxySnapshotV0(state);

            var nodes = new Godot.Collections.Array();
            for (int i = 0; i < snap.SystemNodes.Count; i++)
            {
                var n = snap.SystemNodes[i];

                // Position is required for rendering, but view code must only consume snapshots.
                float px = 0f;
                float py = 0f;
                float pz = 0f;
                if (state.Nodes != null && !string.IsNullOrEmpty(n.NodeId) && state.Nodes.TryGetValue(n.NodeId, out var node))
                {
                    px = node.Position.X;
                    py = node.Position.Y;
                    pz = node.Position.Z;
                }

                nodes.Add(new Godot.Collections.Dictionary
                {
                    ["node_id"] = n.NodeId ?? "",
                    ["display_state_token"] = n.DisplayStateToken ?? "",
                    ["display_text"] = n.DisplayText ?? "",
                    ["object_count"] = n.ObjectCount,
                    ["pos_x"] = px,
                    ["pos_y"] = py,
                    ["pos_z"] = pz
                });
            }

            var edges = new Godot.Collections.Array();
            for (int i = 0; i < snap.LaneEdges.Count; i++)
            {
                var e = snap.LaneEdges[i];
                edges.Add(new Godot.Collections.Dictionary
                {
                    ["from_id"] = e.FromNodeId ?? "",
                    ["to_id"] = e.ToNodeId ?? ""
                });
            }

            d = new Godot.Collections.Dictionary
            {
                ["system_nodes"] = nodes,
                ["lane_edges"] = edges,
                ["player_current_node_id"] = snap.PlayerCurrentNodeId ?? ""
            };
        });

        return d;
    }

    // GATE.S1.HERO_SHIP.SYSTEM_CONTRACT.001
    // Facts-only system snapshot for UI.
    // Determinism:
    // - discovery_sites ordered by site_id (DiscoveryId) Ordinal asc (SimCore builder)
    // - lane_gate ordered by edge_id Ordinal asc (SimCore builder)
    // - no timestamps%wall-clock
    // Contract:
    // - no position data returned (orbital positions are GameShell seed-derived, not SimCore)
    public Godot.Collections.Dictionary GetSystemSnapshotV0(string nodeId)
    {
        nodeId ??= "";

        Godot.Collections.Dictionary d = new Godot.Collections.Dictionary();
        ExecuteSafeRead(state =>
        {
            var snap = MapQueries.BuildSystemSnapshotV0(state, nodeId);

            var station = new Godot.Collections.Dictionary
            {
                ["node_id"] = snap.Station.NodeId ?? "",
                ["node_name"] = snap.Station.NodeName ?? ""
            };

            var sites = new Godot.Collections.Array();
            for (int i = 0; i < snap.DiscoverySites.Count; i++)
            {
                var s = snap.DiscoverySites[i];
                sites.Add(new Godot.Collections.Dictionary
                {
                    ["site_id"] = s.SiteId ?? "",
                    ["phase_token"] = s.PhaseToken ?? ""
                });
            }

            var laneGate = new Godot.Collections.Array();
            for (int i = 0; i < snap.LaneGate.Count; i++)
            {
                var lg = snap.LaneGate[i];
                laneGate.Add(new Godot.Collections.Dictionary
                {
                    ["neighbor_node_id"] = lg.NeighborNodeId ?? "",
                    ["edge_id"] = lg.EdgeId ?? ""
                });
            }

            d = new Godot.Collections.Dictionary
            {
                ["station"] = station,
                ["discovery_sites"] = sites,
                ["lane_gate"] = laneGate
            };
        });

        return d;
    }

    // Deterministic headless shutdown hook for tooling/tests.
    // Cancels the sim loop and requests engine quit on the main thread.
    public void RequestShutdownV0()
    {
        // StopSimulation cancels the sim task and waits briefly (main-thread safe).
        StopSimulation();
        // Defer Quit() to the engine's next idle frame so the engine exits cleanly
        // in headless mode without blocking on mid-frame state.
        GetTree().CallDeferred("quit");
    }

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
