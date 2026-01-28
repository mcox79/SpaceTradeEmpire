# SCRIPT: scripts\tools\Setup-Godot-Bridge.ps1
# PURPOSE: Link Godot to SimCore and create the Visual Bridge.
# TARGETS: Godot 4.x .NET
# AUTHORS: Founder Protocol (Architecture v6)

$ErrorActionPreference = "Stop"
$root = (git rev-parse --show-toplevel).Trim()
Set-Location $root

Write-Host "--- ESTABLISHING GODOT-SIMCORE BRIDGE ---" -ForegroundColor Cyan

# 1. FIND GODOT PROJECT FILE
# We look for the game's main csproj (excluding SimCore/Tests)
$godotProject = Get-ChildItem -Path $root -Filter "*.csproj" | 
    Where-Object { $_.Name -notlike "SimCore*" } | 
    Select-Object -First 1

if (-not $godotProject) {
    Write-Warning "No Godot .csproj found in root!"
    Write-Warning "Please open Godot, create a C# script, and Build to generate the project file."
    throw "Godot Project File Missing"
}

Write-Host "Found Godot Project: $($godotProject.Name)" -ForegroundColor Green

# 2. ADD REFERENCE
# This allows Godot C# scripts to use 'using SimCore;'
dotnet add $godotProject.FullName reference (Join-Path $root "SimCore/SimCore.csproj")

# 3. CREATE DIRECTORIES
$bridgeDir = Join-Path $root "scripts/bridge"
$viewDir   = Join-Path $root "scripts/view"
if (-not (Test-Path $bridgeDir)) { New-Item -ItemType Directory -Path $bridgeDir -Force | Out-Null }
if (-not (Test-Path $viewDir)) { New-Item -ItemType Directory -Path $viewDir -Force | Out-Null }

# 4. WRITE SimBridge.cs
# This is the "Singleton" access point for the simulation.
$code_Bridge = @"
using Godot;
using SimCore;
using SimCore.Gen;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge : Node
{
    // THE KERNEL: The single instance of the C# Simulation
    public SimKernel Kernel { get; private set; }
    
    // Config
    [Export] public int WorldSeed { get; set; } = 12345;
    [Export] public int StarCount { get; set; } = 20;
    [Export] public float GalaxyRadius { get; set; } = 200f;

    public override void _Ready()
    {
        GD.Print(""[BRIDGE] Initializing SimCore Kernel..."");
        
        // 1. Init Brain
        Kernel = new SimKernel(WorldSeed);
        
        // 2. Generate World (Slice 1 Topology)
        GD.Print($""[BRIDGE] Generating Galaxy: {StarCount} Stars"");
        GalaxyGenerator.Generate(Kernel.State, StarCount, GalaxyRadius);
        
        GD.Print($""[BRIDGE] Ready. Signature: {Kernel.State.GetSignature()}"");
    }

    public override void _Process(double delta)
    {
        // SLICE 1: Manual stepping or slow stepping only.
        // In future slices, this might run a threaded job.
    }

    // Helper for GDScript/Console access
    public void RunDayTick()
    {
        Kernel.Step();
        GD.Print($""[BRIDGE] Day Tick Complete. Tick: {Kernel.State.Tick}"");
    }
}
"@
[System.IO.File]::WriteAllText((Join-Path $bridgeDir "SimBridge.cs"), $code_Bridge)


# 5. WRITE GalaxyView.cs
# Replaces 'galaxy_spawner.gd'. Visualizes SimCore data.
$code_View = @"
using Godot;
using SimCore.Entities;
using SpaceTradeEmpire.Bridge;
using System.Collections.Generic;

namespace SpaceTradeEmpire.View;

public partial class GalaxyView : Node3D
{
    private SimBridge _bridge;
    
    // visual cache
    private Dictionary<string, Node3D> _nodeVisuals = new();

    public override void _Ready()
    {
        // 1. Find Bridge
        _bridge = GetNode<SimBridge>(""/root/SimBridge"");
        if (_bridge == null)
        {
            GD.PrintErr(""[VIEW] SimBridge not found at /root/SimBridge"");
            return;
        }

        // 2. Wait for Sim Init (simplest way: wait one frame or check null)
        // For Slice 1, Bridge inits in _Ready, so we are safe if Autoload order is correct.
        DrawGalaxy();
    }

    private void DrawGalaxy()
    {
        var state = _bridge.Kernel.State;
        
        // DRAW STARS
        var starMesh = new SphereMesh { Radius = 1.0f, Height = 2.0f };
        var starMat = new StandardMaterial3D { AlbedoColor = new Color(0, 0.6f, 1.0f), EmissionEnabled = true, Emission = new Color(0, 0.6f, 1.0f), EmissionEnergyMultiplier = 2.0f };
        starMesh.Material = starMat;

        foreach (var kvp in state.Nodes)
        {
            var nodeData = kvp.Value;
            var instance = new MeshInstance3D
            {
                Mesh = starMesh,
                Name = nodeData.Id
            };
            
            // CONVERT: System.Numerics.Vector3 -> Godot.Vector3
            instance.Position = new Vector3(nodeData.Position.X, nodeData.Position.Y, nodeData.Position.Z);
            
            AddChild(instance);
            _nodeVisuals[nodeData.Id] = instance;
            
            // Label
            var label = new Label3D
            {
                Text = nodeData.Name,
                Position = new Vector3(0, 2.5f, 0),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                FontSize = 24
            };
            instance.AddChild(label);
        }

        // DRAW EDGES
        var edgeMat = new StandardMaterial3D { AlbedoColor = new Color(0.2f, 0.2f, 0.2f) };
        
        foreach (var kvp in state.Edges)
        {
            var edge = kvp.Value;
            if (!state.Nodes.ContainsKey(edge.FromNodeId) || !state.Nodes.ContainsKey(edge.ToNodeId)) continue;

            var start = state.Nodes[edge.FromNodeId];
            var end = state.Nodes[edge.ToNodeId];
            
            var p1 = new Vector3(start.Position.X, start.Position.Y, start.Position.Z);
            var p2 = new Vector3(end.Position.X, end.Position.Y, end.Position.Z);

            DrawLine(p1, p2, edgeMat);
        }
    }

    private void DrawLine(Vector3 start, Vector3 end, Material mat)
    {
        var mid = (start + end) / 2f;
        var dist = start.DistanceTo(end);
        
        var mesh = new CylinderMesh
        {
            TopRadius = 0.1f,
            BottomRadius = 0.1f,
            Height = dist,
            Material = mat
        };

        var instance = new MeshInstance3D { Mesh = mesh, Position = mid };
        
        // Orient cylinder
        instance.LookAtFromPosition(mid, end, Vector3.Up);
        instance.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2f);
        
        AddChild(instance);
    }
}
"@
[System.IO.File]::WriteAllText((Join-Path $viewDir "GalaxyView.cs"), $code_View)

Write-Host "`nSUCCESS: Bridge and View scripts created." -ForegroundColor Green
Write-Host "NEXT STEPS (Manual Godot Setup):" -ForegroundColor Yellow
Write-Host "1. Open Godot."
Write-Host "2. Build the Solution (Click 'Build' in top right)."
Write-Host "3. Project Settings > Autoload > Add 'scripts/bridge/SimBridge.cs', name it 'SimBridge'."
Write-Host "4. Open 'scenes/main.tscn' (or your prototype scene)."
Write-Host "5. Delete the old 'GalaxySpawner' node."
Write-Host "6. Add a new Node3D, name it 'GalaxyView', and attach 'scripts/view/GalaxyView.cs'."
Write-Host "7. Play the scene. You should see blue stars generated by C# code!"