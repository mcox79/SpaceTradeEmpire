using Godot;
using SimCore;
using SimCore.Gen;
using SimCore.Entities;
using SimCore.Commands;
using System;
using System.IO;
using System.Linq;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge : Godot.Node
{
    [Export] public int WorldSeed { get; set; } = 12345;
    [Export] public int StarCount { get; set; } = 50;

    [Export] public double TickInterval { get; set; } = 0.1;

    // Dev-only: delete quicksave on boot so you can test "first run" flows
    [Export] public bool ResetSaveOnBoot { get; set; } = false;

    public SimKernel Kernel { get; private set; } = null!;

    private double _tickTimer = 0.0;

    private bool _saveHeld;
    private bool _loadHeld;

    private string SavePath => ProjectSettings.GlobalizePath("user://quicksave.json");

    public override void _Ready()
    {
        // Make sure input is sane on boot
        Input.MouseMode = Input.MouseModeEnum.Visible;

        // Optional legacy cleanup (safe even if nodes do not exist)
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
        GD.Print($"[BRIDGE] Generation Complete. Nodes: {Kernel.State.Nodes.Count} Edges: {Kernel.State.Edges.Count}");

        // Spawn a starter fleet at the first available edge origin
        if (Kernel.State.Edges.Count > 0)
        {
            var edge = Kernel.State.Edges.Values.First();

            var fleet = new Fleet
            {
                Id = "test_ship_01",
                OwnerId = "player",
                CurrentNodeId = edge.FromNodeId,
                State = FleetState.Docked,
                Speed = 0.05f
            };

            Kernel.State.Fleets[fleet.Id] = fleet;
            Kernel.State.PlayerLocationNodeId = edge.FromNodeId;

            GD.Print($"[BRIDGE] Spawned Fleet '{fleet.Id}' at '{fleet.CurrentNodeId}'.");
        }
        else
        {
            GD.PrintErr("[BRIDGE] ERROR: No edges generated. Map is broken.");
        }
    }

    public override void _Process(double delta)
    {
        HandleHotkeys();

        _tickTimer += delta;
        if (_tickTimer < TickInterval) return;

        _tickTimer = 0.0;

        try
        {
            Kernel.Step();
        }
        catch (Exception ex)
        {
            GD.PrintErr(ex.ToString());
        }
    }

    private void HandleHotkeys()
    {
        // F5 save (edge-triggered)
        var saveDown = Input.IsKeyPressed(Key.F5);
        if (saveDown && !_saveHeld)
        {
            _saveHeld = true;
            try
            {
                File.WriteAllText(SavePath, Kernel.SaveToString());
                GD.Print("[BRIDGE] Saved.");
            }
            catch (Exception ex)
            {
                GD.PrintErr(ex.ToString());
            }
        }
        if (!saveDown) _saveHeld = false;

        // F9 load (edge-triggered)
        var loadDown = Input.IsKeyPressed(Key.F9);
        if (loadDown && !_loadHeld)
        {
            _loadHeld = true;
            try
            {
                if (File.Exists(SavePath))
                {
                    var data = File.ReadAllText(SavePath);
                    Kernel.LoadFromString(data);
                    GD.Print("[BRIDGE] Loaded.");
                }
                else
                {
                    GD.Print("[BRIDGE] No save file found.");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr(ex.ToString());
            }
        }
        if (!loadDown) _loadHeld = false;
    }
}