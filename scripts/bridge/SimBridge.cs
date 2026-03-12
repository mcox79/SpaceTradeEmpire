#nullable enable

using Godot;
using SimCore;
using SimCore.Content;
using SimCore.Gen;
using SimCore.Commands;
using SimCore.Intents;
using SimCore.Systems;
using SimCore.Programs;
using SimCore.Events;
using System;
using System.Collections.Generic;
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
    private Godot.Collections.Dictionary _cachedPlayerStateV0 = new Godot.Collections.Dictionary();

    // Cached logistics snapshot (nonblocking UI readout).
    // If the read lock is busy, we return the last captured snapshot instead of stalling a frame.
    private Godot.Collections.Dictionary _cachedLogisticsSnapshot = new Godot.Collections.Dictionary();
    private string _cachedLogisticsSnapshotKey = "";

    // Cached dashboard snapshot (nonblocking UI readout).
    // If the read lock is busy, we return the last captured dashboard snapshot instead of stalling a frame.
    private Godot.Collections.Dictionary _cachedDashboardSnapshot = new Godot.Collections.Dictionary();


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
        catch (Exception ex)
        {
            GD.PrintErr($"[SimBridge] TryParseQuickSaveV2 failed: {ex.GetType().Name}: {ex.Message}");
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

        // GATE.S10.EMPIRE.SHELL.001: Spawn EmpireDashboard into the scene tree.
        // Wrap in a CanvasLayer so it renders above the 3D viewport.
        if (!Engine.IsEditorHint())
        {
            var layer = new CanvasLayer();
            layer.Name = "EmpireDashboardLayer";
            layer.Layer = 100;
            var dashboard = new SpaceTradeEmpire.Ui.EmpireDashboard();
            dashboard.Name = "EmpireDashboard";
            layer.AddChild(dashboard);
            GetTree().Root.CallDeferred(Node.MethodName.AddChild, layer);
        }
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
                // GATE.S4.CATALOG.MARKET_BIND.001: inject registry so generator validates seeded goods.
                var _reg = ContentRegistryLoader.LoadFromJsonOrThrow(ContentRegistryLoader.DefaultRegistryJsonV0);
                GalaxyGenerator.Generate(_kernel.State, StarCount, 200f,
                    new GalaxyGenerator.GalaxyGenOptions { Registry = _reg });
                EnsurePlayerFleetV0(_kernel.State);
            }
            finally
            {
                _stateLock.ExitWriteLock();
            }
        }
    }

    // Creates the canonical player fleet if absent (e.g. fresh generated world with no save file).
    // Mirrors the fleet created by WorldLoader for loaded worlds.
    private static void EnsurePlayerFleetV0(SimState state)
    {
        const string playerFleetId = "fleet_trader_1";
        if (state.Fleets.ContainsKey(playerFleetId)) return;

        state.Fleets[playerFleetId] = new SimCore.Entities.Fleet
        {
            Id = playerFleetId,
            OwnerId = "player",
            CurrentNodeId = state.PlayerLocationNodeId ?? "",
            DestinationNodeId = "",
            CurrentEdgeId = "",
            State = SimCore.Entities.FleetState.Idle,
            TravelProgress = 0f,
            Speed = 0.5f,
            CurrentTask = "Idle",
            CurrentJob = null,
            FuelCapacity = SimCore.Content.ShipClassContentV0.GetById("corvette")?.BaseFuelCapacity ?? SimCore.Tweaks.SustainTweaksV0.DefaultFuelCapacity,
            FuelCurrent = SimCore.Content.ShipClassContentV0.GetById("corvette")?.BaseFuelCapacity ?? SimCore.Tweaks.SustainTweaksV0.DefaultFuelCapacity,
            // SLICE 4: Standard hero ship slots (GATE.S4.MODULE_MODEL.SLOTS.001)
            // Ordered by SlotId Ordinal asc: cargo < engine < utility < weapon.
            Slots = new List<SimCore.Entities.ModuleSlot>
            {
                new() { SlotId = "slot_cargo_0",   SlotKind = SimCore.Entities.SlotKind.Cargo },
                new() { SlotId = "slot_engine_0",  SlotKind = SimCore.Entities.SlotKind.Engine },
                new() { SlotId = "slot_utility_0", SlotKind = SimCore.Entities.SlotKind.Utility },
                new() { SlotId = "slot_weapon_0",  SlotKind = SimCore.Entities.SlotKind.Weapon },
            }
        };
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
                    ["fleet_count"] = n.FleetCount,
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
                    ["edge_id"] = lg.EdgeId ?? "",
                    ["neighbor_display_name"] = lg.NeighborDisplayName ?? ""
                });
            }

            // GATE.S5.COMBAT_PLAYABLE.ENCOUNTER_TRIGGER.001
            var fleets = new Godot.Collections.Array();
            for (int i = 0; i < snap.Fleets.Count; i++)
            {
                var f = snap.Fleets[i];
                fleets.Add(new Godot.Collections.Dictionary
                {
                    ["fleet_id"] = f.FleetId ?? "",
                    ["owner_id"] = f.OwnerId ?? ""
                });
            }

            d = new Godot.Collections.Dictionary
            {
                ["station"] = station,
                ["discovery_sites"] = sites,
                ["lane_gate"] = laneGate,
                ["fleets"] = fleets
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

    // Headless-test helper: stop the sim thread so the process can exit promptly
    // after SceneTree.quit(). Call this from GDScript test scripts before quit().
    public void StopSimV0()
    {
        StopSimulation();
    }

    // Returns the current player ship state name from GameManager (GameShell-only query).
    // Used by headless tests to prove the state machine surface without direct node access.
    public string GetPlayerShipStateNameV0()
    {
        var gm = GetNodeOrNull<Node>("/root/GameManager");
        if (gm == null) return "UNKNOWN";
        return gm.Call("get_player_ship_state_name_v0").AsString();
    }

    // Returns {credits, cargo_count, current_node_id, ship_state_token} from player state.
    // Nonblocking: returns last cached snapshot if read lock is unavailable.
    public Godot.Collections.Dictionary GetPlayerStateV0()
    {
        TryExecuteSafeRead(state =>
        {
            int cargoCount = 0;
            foreach (var v in state.PlayerCargo.Values)
                cargoCount += v;
            var nodeName = state.PlayerLocationNodeId ?? "";
            if (!string.IsNullOrEmpty(state.PlayerLocationNodeId)
                && state.Nodes.TryGetValue(state.PlayerLocationNodeId, out var pNode))
                nodeName = pNode.Name ?? state.PlayerLocationNodeId ?? "";
            var d = new Godot.Collections.Dictionary
            {
                ["credits"] = state.PlayerCredits,
                ["cargo_count"] = cargoCount,
                ["current_node_id"] = state.PlayerLocationNodeId ?? "",
                ["node_name"] = nodeName
            };
            lock (_snapshotLock)
            {
                _cachedPlayerStateV0 = d;
            }
        }, 0);
        lock (_snapshotLock)
        {
            var result = _cachedPlayerStateV0.Duplicate();
            result["ship_state_token"] = GetPlayerShipStateNameV0();
            return result;
        }
    }

    // GATE.S7.MAIN_MENU.CAPTAIN_NAME.001: Set captain name on the live state.
    // Called by main_menu.gd before transitioning to the game scene.
    public void SetCaptainNameV0(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        // Clamp to 32 chars to match UI constraint.
        if (name.Length > 32) name = name.Substring(0, 32);
        _stateLock.EnterWriteLock();
        try
        {
            if (_kernel?.State != null)
                _kernel.State.CaptainName = name;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    // GATE.S7.MAIN_MENU.CAPTAIN_NAME.001: Query captain name for UI display.
    public string GetCaptainNameV0()
    {
        string result = "Commander";
        TryExecuteSafeRead(state =>
        {
            result = state.CaptainName ?? "Commander";
        }, 0);
        return result;
    }

    public string GetNodeDisplayNameV0(string nodeId)
    {
        string name = nodeId ?? "";
        TryExecuteSafeRead(state =>
        {
            if (state.Nodes.TryGetValue(nodeId ?? "", out var node))
                name = node.Name ?? nodeId ?? "";
        }, 0);
        return name;
    }

    // Returns hero ship slot loadout ordered by slot_id Ordinal asc.
    // Each entry: {slot_id (string), slot_kind (string), installed_module_id (string or "")}.
    // Nonblocking: returns empty array if read lock is unavailable.
    public Godot.Collections.Array GetHeroShipLoadoutV0()
    {
        var slots = new Godot.Collections.Array();
        TryExecuteSafeRead(state =>
        {
            if (!state.Fleets.TryGetValue("fleet_trader_1", out var fleet)) return;
            foreach (var s in fleet.Slots.OrderBy(s => s.SlotId, StringComparer.Ordinal))
            {
                slots.Add(new Godot.Collections.Dictionary
                {
                    ["slot_id"] = s.SlotId,
                    ["slot_kind"] = s.SlotKind.ToString(),
                    ["installed_module_id"] = s.InstalledModuleId ?? ""
                });
            }
        }, 0);
        return slots;
    }

    // Returns player cargo as [{good_id, qty}] ordered by good_id asc, qty > 0 only.
    // Reads from state.PlayerCargo (the hero ship's personal inventory, updated by TradeCommand).
    // Nonblocking: returns empty array if read lock is unavailable.
    public Godot.Collections.Array GetPlayerCargoV0()
    {
        var result = new Godot.Collections.Array();
        TryExecuteSafeRead(state =>
        {
            foreach (var kv in state.PlayerCargo.Where(kv => kv.Value > 0).OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                result.Add(new Godot.Collections.Dictionary
                {
                    ["good_id"] = kv.Key,
                    ["qty"] = kv.Value
                });
            }
        }, 0);
        return result;
    }

    // Returns the current SimCore tick index. Thread-safe read via state lock.
    // Used by headless tests to assert tick advance after TravelCommand dispatch.
    public int GetSimTickV0()
    {
        int tick = -1;
        TryExecuteSafeRead(state => { tick = state.Tick; });
        return tick;
    }

    // GDScript-callable wrapper: dispatches a TravelCommand for the given fleet to the target node.
    // Equivalent to EnqueueCommand(new TravelCommand(fleetId, targetNodeId)) from C#.
    public void DispatchTravelCommandV0(string fleetId, string targetNodeId)
    {
        EnqueueCommand(new TravelCommand(fleetId, targetNodeId));
    }

    // GDScript-callable wrapper: dispatches a PlayerArriveCommand to update the hero ship location in SimState.
    // Called by game_manager.on_lane_arrival_v0 after the hero ship completes a lane transit.
    // Blocks until the sim thread processes the command so callers can read updated state immediately.
    public void DispatchPlayerArriveV0(string targetNodeId)
    {
        int tickBefore = GetSimTickV0();
        EnqueueCommand(new PlayerArriveCommand(targetNodeId));
        WaitForTickAdvance(tickBefore, 200);
    }

    // GATE.S6.REVEAL.SCAN_CMD.001: Dispatch a scan/analyze action on a discovery.
    public void DispatchScanDiscoveryV0(string discoveryId)
    {
        EnqueueCommand(new ScanDiscoveryCommand(discoveryId));
    }

    // GATE.S1.HERO_SHIP_LOOP.PLAYER_TRADE.001
    // Returns market listings for the given node. Each entry: { good_id, buy_price, sell_price, quantity }.
    public Godot.Collections.Array GetPlayerMarketViewV0(string nodeId)
    {
        nodeId ??= "";
        var result = new Godot.Collections.Array();
        ExecuteSafeRead(state =>
        {
            if (!state.Markets.TryGetValue(nodeId, out var market)) return;
            var goods = market.Inventory.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
            foreach (var goodId in goods)
            {
                result.Add(new Godot.Collections.Dictionary
                {
                    ["good_id"] = goodId,
                    ["buy_price"] = market.GetBuyPrice(goodId),
                    ["sell_price"] = market.GetSellPrice(goodId),
                    ["quantity"] = market.Inventory.TryGetValue(goodId, out var qty) ? qty : 0
                });
            }
        });
        return result;
    }

    // GATE.S4.CATALOG.EPIC_CLOSE.001
    // Returns all good IDs registered in the content catalog, sorted Ordinal.
    // This is a catalog registry query — does not reflect market stock or node economic profile.
    // Use this to verify catalog completeness. Use GetPlayerMarketViewV0 to see what a specific
    // node currently has in stock. Long-term, market view will be driven by node profiles, not inventory keys.
    public Godot.Collections.Array GetCatalogGoodsV0()
    {
        var result = new Godot.Collections.Array();
        var reg = ContentRegistryLoader.LoadFromJsonOrThrow(ContentRegistryLoader.DefaultRegistryJsonV0);
        foreach (var good in reg.Goods)
            result.Add(good.Id);
        return result;
    }

    // GATE.S4.INDU_STRUCT.PLAYABLE_VIEW.001
    // GATE.S7.PRODUCTION.BRIDGE_READOUT.001: Enhanced with recipe_name, inputs (display names), output display names.
    // Returns array of {site_id, recipe_id, recipe_name, efficiency_pct, health_pct, inputs, outputs} per site.
    public Godot.Collections.Array GetNodeIndustryV0(string nodeId)
    {
        var result = new Godot.Collections.Array();
        if (string.IsNullOrEmpty(nodeId)) return result;

        // Load content registry once for display name lookups.
        var reg = SimCore.Content.ContentRegistryLoader.LoadFromJsonOrThrow(
            SimCore.Content.ContentRegistryLoader.DefaultRegistryJsonV0);
        var goodNames = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var g in reg.Goods)
            goodNames[g.Id] = string.IsNullOrEmpty(g.DisplayName) ? g.Id : g.DisplayName;
        var recipeMap = new System.Collections.Generic.Dictionary<string, SimCore.Content.ContentRegistryLoader.RecipeDefV0>(StringComparer.Ordinal);
        foreach (var r in reg.Recipes)
            recipeMap[r.Id] = r;

        TryExecuteSafeRead(state =>
        {
            // Deterministic ordering: sorted by site key
            var keys = new System.Collections.Generic.List<string>();
            foreach (var kv in state.IndustrySites)
            {
                if (string.Equals(kv.Value.NodeId, nodeId, StringComparison.Ordinal))
                    keys.Add(kv.Key);
            }
            keys.Sort(StringComparer.Ordinal);

            foreach (var key in keys)
            {
                var site = state.IndustrySites[key];
                var dict = new Godot.Collections.Dictionary();
                dict["site_id"] = site.Id;
                dict["recipe_id"] = site.RecipeId ?? "";
                dict["efficiency_pct"] = (int)(site.Efficiency * 100);
                dict["health_pct"] = site.HealthBps / 100;

                // Recipe display name from content registry.
                string recipeName = site.RecipeId ?? "";
                if (!string.IsNullOrEmpty(site.RecipeId) && recipeMap.TryGetValue(site.RecipeId, out var recipeDef))
                    recipeName = string.IsNullOrEmpty(recipeDef.DisplayName) ? site.RecipeId : recipeDef.DisplayName;
                dict["recipe_name"] = recipeName;

                // Inputs from recipe definition with display names.
                var inputs = new Godot.Collections.Array();
                if (!string.IsNullOrEmpty(site.RecipeId) && recipeMap.TryGetValue(site.RecipeId, out var rDef))
                {
                    foreach (var inp in rDef.Inputs)
                    {
                        goodNames.TryGetValue(inp.GoodId, out var gName);
                        inputs.Add((gName ?? inp.GoodId) + ":" + inp.Qty);
                    }
                }
                dict["inputs"] = inputs;

                var outputs = new Godot.Collections.Array();
                var outKeys = new System.Collections.Generic.List<string>(site.Outputs.Keys);
                outKeys.Sort(StringComparer.Ordinal);
                foreach (var ok in outKeys)
                {
                    goodNames.TryGetValue(ok, out var gName);
                    outputs.Add((gName ?? ok) + ":" + site.Outputs[ok]);
                }
                dict["outputs"] = outputs;

                result.Add(dict);
            }
        });

        return result;
    }

    // GATE.S4.INDU_STRUCT.SHORTFALL_LOG.001
    // Returns shortfall events since the given tick (inclusive).
    public Godot.Collections.Array GetIndustryEventsV0(int sinceTick)
    {
        var result = new Godot.Collections.Array();

        TryExecuteSafeRead(state =>
        {
            foreach (var evt in state.ShortfallEventLog)
            {
                if (evt.Tick < sinceTick) continue;
                var dict = new Godot.Collections.Dictionary();
                dict["seq"] = evt.Seq;
                dict["tick"] = evt.Tick;
                dict["site_id"] = evt.SiteId;
                dict["recipe_id"] = evt.RecipeId;
                dict["missing_good_id"] = evt.MissingGoodId;
                dict["required_qty"] = evt.RequiredQty;
                dict["available_qty"] = evt.AvailableQty;
                dict["efficiency_bps"] = evt.EfficiencyBps;
                result.Add(dict);
            }
        });

        return result;
    }

    // Dispatches a TradeCommand (buy or sell) for the player at the given market node.
    // Blocks until the sim thread processes the command (up to 200ms) so callers
    // can read updated state immediately without a timer-based race.
    public void DispatchPlayerTradeV0(string nodeId, string goodId, int qty, bool isBuy)
    {
        int tickBefore = GetSimTickV0();
        var type = isBuy ? TradeType.Buy : TradeType.Sell;
        EnqueueCommand(new TradeCommand("player", nodeId, goodId, qty, type));
        WaitForTickAdvance(tickBefore, 200);
    }

    // GATE.S4.MODULE_MODEL.EQUIP.001
    // Dispatches EquipModuleCommand to install moduleId into slotId on the hero ship.
    // Pass moduleId="" to unequip. Non-blocking; takes effect on the next sim tick.
    public void DispatchEquipModuleV0(string slotId, string moduleId)
    {
        EnqueueCommand(new EquipModuleCommand("fleet_trader_1", slotId, moduleId));
    }

    // Returns the current FleetState name for the given fleet ID, or "UNKNOWN" if not found.
    // Used by headless tests to confirm fleet moved to Traveling after TravelCommand dispatch.
    public string GetFleetStateV0(string fleetId)
    {
        string result = "UNKNOWN";
        TryExecuteSafeRead(state =>
        {
            if (state.Fleets.TryGetValue(fleetId, out var f))
                result = f.State.ToString();
        });
        return result;
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

    // GATE.S1.DISCOVERY_INTERACT.SCAN.001: Advance discovery phase (Seen→Scanned or Scanned→Analyzed).
    public Godot.Collections.Dictionary AdvanceDiscoveryPhaseV0(string discoveryId)
    {
        var result = new Godot.Collections.Dictionary();
        result["ok"] = false;
        result["phase_token"] = "UNKNOWN";
        result["reason"] = "";

        if (string.IsNullOrEmpty(discoveryId) || IsLoading)
        {
            result["reason"] = "invalid";
            return result;
        }

        _stateLock.EnterWriteLock();
        try
        {
            var state = _kernel.State;

            // Try scan first (Seen → Scanned).
            var scanReason = SimCore.Systems.IntelSystem.GetScanReasonCode(state, discoveryId);
            if (scanReason == SimCore.Entities.DiscoveryReasonCode.Ok)
            {
                SimCore.Systems.IntelSystem.ApplyScan(state, "fleet_trader_1", discoveryId);
                result["ok"] = true;
                result["phase_token"] = "SCANNED";
                return result;
            }

            // Try analyze (Scanned → Analyzed).
            var analyzeReason = SimCore.Systems.IntelSystem.GetAnalyzeReasonCode(state, discoveryId);
            if (analyzeReason == SimCore.Entities.DiscoveryReasonCode.Ok)
            {
                SimCore.Systems.IntelSystem.ApplyAnalyze(state, "fleet_trader_1", discoveryId);
                result["ok"] = true;
                result["phase_token"] = "ANALYZED";
                return result;
            }

            result["reason"] = scanReason.ToString();
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        return result;
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

    // GATE.S7.MAIN_MENU.AUTO_SAVE.001: Auto-save to dedicated slot 0 (quicksave_auto.json).
    // Saves to auto-save path without disturbing the active manual save slot.
    public void AutoSaveV0()
    {
        var prevPath = _savePathAbs;
        _savePathAbs = ProjectSettings.GlobalizePath("user://quicksave_auto.json");
        _saveRequested = true;
        // Restore previous path after flagging save. ExecuteSave picks up _savePathAbs atomically
        // on the sim thread, so we defer restore by one frame to ensure it's read.
        CallDeferred(nameof(_RestoreAutoSavePath), prevPath);
    }

    private void _RestoreAutoSavePath(string path)
    {
        _savePathAbs = path;
    }

    // GATE.S1.SAVE_UI.SLOTS.001: save slot support (3 slots).
    public void SetActiveSaveSlotV0(int slot)
    {
        if (slot < 1 || slot > 3) slot = 1;
        _savePathAbs = ProjectSettings.GlobalizePath($"user://quicksave_{slot}.json");
    }

    public Godot.Collections.Dictionary GetSaveSlotMetadataV0(int slot)
    {
        var result = new Godot.Collections.Dictionary();
        result["slot"] = slot;
        result["exists"] = false;
        result["timestamp"] = "";
        result["credits"] = 0;
        result["system_name"] = "";

        if (slot < 1 || slot > 3) return result;

        var path = ProjectSettings.GlobalizePath($"user://quicksave_{slot}.json");
        if (!File.Exists(path)) return result;

        result["exists"] = true;
        try
        {
            result["timestamp"] = File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm");

            var text = File.ReadAllText(path);
            if (TryParseQuickSaveV2(text, out var qs))
            {
                var root = qs.Kernel;
                if (root.TryGetProperty("Fleets", out var fleets) &&
                    fleets.TryGetProperty("fleet_trader_1", out var pf))
                {
                    if (pf.TryGetProperty("Credits", out var c))
                        result["credits"] = c.GetInt32();
                    if (pf.TryGetProperty("CurrentNodeId", out var n))
                        result["system_name"] = n.GetString() ?? "";
                }
            }
        }
        catch { /* best-effort metadata extraction */ }

        return result;
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

    // GATE.S12.UX_POLISH.DISPLAY_NAMES.001: Convert snake_case IDs to readable "Title Case" display names.
    // Examples: "fuel" → "Fuel", "hull_plating" → "Hull Plating", "star_0" → "Star 0"
    public static string FormatDisplayNameV0(string id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        var parts = id.Split('_');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0 && char.IsLetter(parts[i][0]))
            {
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            }
        }
        return string.Join(" ", parts);
    }

    // ── GATE.S15.FEEL.JUMP_EVENT_TOAST.001: Jump event queries for toast notifications ──

    private Godot.Collections.Array _cachedJumpEventsV0 = new Godot.Collections.Array();

    /// <summary>
    /// Returns all jump events recorded in state.JumpEvents as a Godot Array of dicts.
    /// Each dict keys: event_id, kind (string "salvage"/"signal"/"turbulence"/"none"),
    ///   fleet_id, edge_id, node_id, tick, good_id, quantity, hull_damage.
    /// Nonblocking: returns last cached array if read lock is unavailable.
    /// </summary>
    public Godot.Collections.Array GetJumpEventsV0()
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            foreach (var evt in state.JumpEvents)
            {
                string kindStr = evt.Kind switch
                {
                    SimCore.Entities.JumpEventKind.Salvage     => "salvage",
                    SimCore.Entities.JumpEventKind.Signal      => "signal",
                    SimCore.Entities.JumpEventKind.Turbulence  => "turbulence",
                    _                                          => "none",
                };
                var d = new Godot.Collections.Dictionary
                {
                    ["event_id"]   = evt.EventId,
                    ["kind"]       = kindStr,
                    ["fleet_id"]   = evt.FleetId,
                    ["edge_id"]    = evt.EdgeId,
                    ["node_id"]    = evt.NodeId,
                    ["tick"]       = evt.Tick,
                    ["good_id"]    = evt.GoodId,
                    ["quantity"]   = evt.Quantity,
                    ["hull_damage"] = evt.HullDamage,
                };
                arr.Add(d);
            }
            lock (_snapshotLock) { _cachedJumpEventsV0 = arr; }
        }, 0);

        lock (_snapshotLock) { return _cachedJumpEventsV0; }
    }
}
