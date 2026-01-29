using Godot;
using SimCore;
using SimCore.Gen;
using SimCore.Entities;
using SimCore.Commands;
using SimCore.Systems; // Added for MovementSystem
using System;
using System.IO;
using System.Linq;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge : Godot.Node
{
    [Signal] public delegate void SimLoadedEventHandler();

    [Export] public int WorldSeed { get; set; } = 12345;
    [Export] public int StarCount { get; set; } = 20; // Reduced for Slice 1 clarity
    [Export] public double TickInterval { get; set; } = 0.1; // 10 ticks/sec
    [Export] public bool ResetSaveOnBoot { get; set; } = false;

    public SimKernel Kernel { get; private set; } = null!;

    private double _tickTimer = 0.0;
    private bool _saveHeld;
    private bool _loadHeld;
    
    public bool IsLoading { get; private set; } = false;
    private string SavePath => ProjectSettings.GlobalizePath("user://quicksave.json");

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

        GD.Print("[BRIDGE] Initializing SimCore Kernel...");
        Kernel = new SimKernel(WorldSeed);

        GD.Print($"[BRIDGE] Generating Galaxy ({StarCount} stars)...");
        GalaxyGenerator.Generate(Kernel.State, StarCount, 200f);
        
        // SPAWN PLAYER FLEET
        if (Kernel.State.Edges.Count > 0)
        {
            var edge = Kernel.State.Edges.Values.First();
            var fleet = new Fleet
            {
                Id = "test_ship_01",
                OwnerId = "player",
                CurrentNodeId = edge.FromNodeId,
                State = FleetState.Idle, // FIXED: Was Docked
                Speed = 0.05f,           // Slower visual speed
                Supplies = 100
            };
            Kernel.State.Fleets[fleet.Id] = fleet;
            Kernel.State.PlayerLocationNodeId = edge.FromNodeId;
            GD.Print($"[BRIDGE] Spawned Fleet '{fleet.Id}' at '{fleet.CurrentNodeId}'.");
        }
    }

    public override void _Process(double delta)
    {
        HandleHotkeys();
        if (IsLoading) return;

        _tickTimer += delta;
        if (_tickTimer < TickInterval) return;

        _tickTimer = 0.0;
        try
        {
            if (Kernel != null) Kernel.Step();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[BRIDGE] Step Error: {ex}");
        }
    }

    private void HandleHotkeys()
    {
        var saveDown = Input.IsKeyPressed(Key.F5);
        if (saveDown && !_saveHeld) { _saveHeld = true; PerformSave(); }
        if (!saveDown) _saveHeld = false;

        var loadDown = Input.IsKeyPressed(Key.F9);
        if (loadDown && !_loadHeld) { _loadHeld = true; CallDeferred(nameof(PerformLoad)); }
        if (!loadDown) _loadHeld = false;
    }

    private void PerformSave()
    {
        try
        {
            File.WriteAllText(SavePath, Kernel.SaveToString());
            GD.Print("[BRIDGE] Saved.");
        }
        catch (Exception ex) { GD.PrintErr(ex.ToString()); }
    }

    private void PerformLoad()
    {
        if (!File.Exists(SavePath)) return;

        IsLoading = true;
        GD.Print("[BRIDGE] Loading...");
        try
        {
            var data = File.ReadAllText(SavePath);
            Kernel.LoadFromString(data);
            EmitSignal(SignalName.SimLoaded);
            GD.Print("[BRIDGE] Load Complete.");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[BRIDGE] Load Failed: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}