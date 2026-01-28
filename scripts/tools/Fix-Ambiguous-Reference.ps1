# SCRIPT: scripts\tools\Fix-Ambiguous-Reference.ps1
# PURPOSE: Fix "Ambiguous Node" error by specifying Godot.Node inheritance.
# TARGETS: Godot 4.x .NET
# AUTHORS: Founder Protocol (Architecture v6)

$ErrorActionPreference = "Stop"
$root = (git rev-parse --show-toplevel).Trim()
Set-Location $root

Write-Host "--- REPAIRING NAMESPACE COLLISION ---" -ForegroundColor Cyan

$bridgeDir = Join-Path $root "scripts/bridge"

# 1. OVERWRITE SIMBRIDGE.CS
# The fix is on line 10: "public partial class SimBridge : Godot.Node"
$code_Bridge = @'
using Godot;
using SimCore;
using SimCore.Gen;
using SimCore.Entities;
using SimCore.Commands;
using System.Linq;

namespace SpaceTradeEmpire.Bridge;

// FIX: Explicitly inherit from Godot.Node to avoid conflict with SimCore.Entities.Node
public partial class SimBridge : Godot.Node
{
    public SimKernel Kernel { get; private set; }
    
    [Export] public int WorldSeed { get; set; } = 12345;
    [Export] public int StarCount { get; set; } = 20;
    
    // Ticking Logic
    private double _tickTimer = 0.0;
    [Export] public double TickInterval { get; set; } = 0.1; // 10 ticks/sec

    public override void _Ready()
    {
        GD.Print("[BRIDGE] Initializing SimCore Kernel...");
        Kernel = new SimKernel(WorldSeed);
        
        GD.Print($"[BRIDGE] Generating Galaxy...");
        GalaxyGenerator.Generate(Kernel.State, StarCount, 200f);
        
        // --- SMOKE TEST: SPAWN A FLEET ---
        if (Kernel.State.Edges.Count > 0)
        {
            // Find a valid edge to travel
            var edge = Kernel.State.Edges.Values.First();
            
            var fleet = new Fleet 
            { 
                Id = "test_ship_01", 
                OwnerId = "player",
                CurrentNodeId = edge.FromNodeId,
                State = FleetState.Docked,
                Speed = 0.05f // Slower visual speed
            };
            
            // Inject into State (God Mode)
            Kernel.State.Fleets.Add(fleet.Id, fleet);
            GD.Print($"[BRIDGE] Spawned Test Fleet: {fleet.Id} at {fleet.CurrentNodeId}");
            
            // Command it to move
            Kernel.EnqueueCommand(new TravelCommand(fleet.Id, edge.ToNodeId));
            GD.Print($"[BRIDGE] Commanded Travel to: {edge.ToNodeId}");
        }
    }

    public override void _Process(double delta)
    {
        // Run the SimCore loop based on timer
        _tickTimer += delta;
        if (_tickTimer >= TickInterval)
        {
            _tickTimer = 0.0;
            Kernel.Step();
        }
    }
}
'@
[System.IO.File]::WriteAllText((Join-Path $bridgeDir "SimBridge.cs"), $code_Bridge)

Write-Host "`nSUCCESS: Namespace collision resolved." -ForegroundColor Green
Write-Host "You may now Build and Play." -ForegroundColor Yellow