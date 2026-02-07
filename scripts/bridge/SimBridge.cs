#nullable enable

using Godot;
using SimCore;
using SimCore.Gen;
using SimCore.Commands;
using SimCore.Intents;
using SimCore.Systems;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge : Node
{
        [Signal] public delegate void SimLoadedEventHandler();

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
                // Emit load complete on main thread.
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
                                // Loading is exclusive.
                                if (_loadRequested)
                                {
                                        _loadRequested = false;
                                        ExecuteLoad();
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

                        var sites = SustainmentReport.BuildForNode(state, marketId);

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
                                        ["time_to_failure_days"] = s.TimeToFailureDays
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
                                                ["buffer_margin"] = inp.BufferMargin
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
