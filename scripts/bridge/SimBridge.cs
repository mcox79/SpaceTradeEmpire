using Godot;
using SimCore;
using SimCore.Gen;
using SimCore.Entities;
using SimCore.Commands;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge : Godot.Node
{
    [Signal] public delegate void SimLoadedEventHandler();

    [Export] public int WorldSeed { get; set; } = 12345;
    [Export] public int StarCount { get; set; } = 20;
    [Export] public int TickDelayMs { get; set; } = 100;
    [Export] public bool ResetSaveOnBoot { get; set; } = true;

    private SimKernel _kernel = null!;
    private CancellationTokenSource _cts;
    private Task _simTask;
    private readonly ReaderWriterLockSlim _stateLock = new ReaderWriterLockSlim();

    public bool IsLoading { get; private set; } = false;
    private string SavePath => ProjectSettings.GlobalizePath("user://quicksave.json");
    private bool _saveRequested = false;
    private bool _loadRequested = false;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        
        // Cleanup old UI
        var hud = GetTree().Root.FindChild("HUD", true, false);
        if (hud != null) hud.QueueFree();
        var router = GetTree().Root.FindChild("InputModeRouter", true, false);
        if (router != null) router.QueueFree();

        if (ResetSaveOnBoot && File.Exists(SavePath))
        {
            try { File.Delete(SavePath); }
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

    private void InitializeKernel()
    {
        GD.Print("[BRIDGE] Initializing SimCore Kernel...");
        _kernel = new SimKernel(WorldSeed);

        if (File.Exists(SavePath) && !ResetSaveOnBoot)
        {
             ExecuteLoad();
        }
        else
        {
             GalaxyGenerator.Generate(_kernel.State, StarCount, 200f);
             SpawnPlayerFleet();
        }
    }

    private void SpawnPlayerFleet()
    {
        if (_kernel.State.Edges.Count > 0)
        {
            var edge = _kernel.State.Edges.Values.First();
            var fleet = new Fleet
            {
                Id = "test_ship_01",
                OwnerId = "player",
                CurrentNodeId = edge.FromNodeId,
                State = FleetState.Idle,
                Speed = 0.05f,
                Supplies = 100
            };
            _kernel.State.Fleets[fleet.Id] = fleet;
            _kernel.State.PlayerLocationNodeId = edge.FromNodeId;
        }
    }

    // --- THREADING LIFECYCLE ---

    private void StartSimulation()
    {
        if (_simTask != null) return;
        _cts = new CancellationTokenSource();
        _simTask = Task.Run(() => SimLoop(_cts.Token), _cts.Token);
        GD.Print("[BRIDGE] Simulation Thread Started.");
    }

    private void StopSimulation()
    {
        if (_cts == null) return;
        _cts.Cancel();
        try { _simTask.Wait(1000); } catch { }
        _cts.Dispose();
        _cts = null;
        _simTask = null;
        GD.Print("[BRIDGE] Simulation Thread Stopped.");
    }

    private async Task SimLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (_saveRequested) ExecuteSave();
                if (_loadRequested) ExecuteLoad();

                if (!IsLoading)
                {
                    _stateLock.EnterWriteLock();
                    try
                    {
                        _kernel.Step();
                    }
                    finally
                    {
                        _stateLock.ExitWriteLock();
                    }
                }

                await Task.Delay(TickDelayMs, token);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                GD.PrintErr($"[BRIDGE] CRITICAL SIM ERROR: {ex}");
                await Task.Delay(1000, token);
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

    // GDScript-Friendly Snapshot Accessor
    public Godot.Collections.Dictionary GetPlayerSnapshot()
    {
        var dict = new Godot.Collections.Dictionary();
        if (IsLoading) return dict;

        _stateLock.EnterReadLock();
        try
        {
            dict["credits"] = _kernel.State.PlayerCredits;
            // Convert C# Dict to Godot Dict for Cargo
            var cargo = new Godot.Collections.Dictionary();
            foreach(var kv in _kernel.State.PlayerCargo)
            {
                cargo[kv.Key] = kv.Value;
            }
            dict["cargo"] = cargo;
            dict["location"] = _kernel.State.PlayerLocationNodeId;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
        return dict;
    }

    public void EnqueueCommand(ICommand cmd)
    {
        _kernel.EnqueueCommand(cmd);
    }

    // --- INPUT HANDLING ---

    public override void _Process(double delta)
    {
        if (Input.IsKeyPressed(Key.F5) && !_saveRequested) _saveRequested = true;
        if (Input.IsKeyPressed(Key.F9) && !_loadRequested) _loadRequested = true;
    }

    // --- IO OPERATIONS ---

    private void ExecuteSave()
    {
        _stateLock.EnterReadLock();
        try
        {
            File.WriteAllText(SavePath, _kernel.SaveToString());
            GD.Print("[BRIDGE] Saved.");
        }
        catch (Exception ex) { GD.PrintErr(ex.ToString()); }
        finally
        {
             _stateLock.ExitReadLock();
            _saveRequested = false;
        }
    }

    private void ExecuteLoad()
    {
        if (!File.Exists(SavePath)) { _loadRequested = false; return; }

        IsLoading = true;
        _stateLock.EnterWriteLock();
        try
        {
            GD.Print("[BRIDGE] Loading...");
            var data = File.ReadAllText(SavePath);
            _kernel.LoadFromString(data);
            CallDeferred(nameof(NotifyLoadComplete));
        }
        catch (Exception ex) { GD.PrintErr($"[BRIDGE] Load Failed: {ex}"); }
        finally
        {
            _stateLock.ExitWriteLock();
            _loadRequested = false;
            IsLoading = false;
        }
    }

    private void NotifyLoadComplete()
    {
        EmitSignal(SignalName.SimLoaded);
        GD.Print("[BRIDGE] Load Complete.");
    }
}