# SCRIPT: scripts\tools\Setup-Bridge.ps1
# PURPOSE: Link Godot to SimCore and create the Visual Bridge scripts.
# TARGETS: Godot 4.x .NET
# AUTHORS: Founder Protocol (Architecture v6)

$ErrorActionPreference = "Stop"
$root = (git rev-parse --show-toplevel).Trim()
Set-Location $root

Write-Host "--- ESTABLISHING GODOT-SIMCORE BRIDGE ---" -ForegroundColor Cyan

# 1. FIND GODOT PROJECT FILE
# We look for the game's main csproj (excluding SimCore)
$godotProject = Get-ChildItem -Path $root -Filter "*.csproj" | 
    Where-Object { $_.Name -notlike "SimCore*" } | 
    Select-Object -First 1

if (-not $godotProject) {
    Write-Warning "CRITICAL: No Godot .csproj found in root!"
    Write-Warning "Action Required: Open Godot, click the 'Build' (Hammer) icon in top right."
    throw "Godot Project File Missing"
}

Write-Host "Found Godot Project: $($godotProject.Name)" -ForegroundColor Green

# 2. ADD REFERENCE (The Link)
# This allows Godot scripts to say "using SimCore;"
dotnet add $godotProject.FullName reference (Join-Path $root "SimCore/SimCore.csproj")

# 3. CREATE DIRECTORIES
$bridgeDir = Join-Path $root "scripts/bridge"
$viewDir   = Join-Path $root "scripts/view"
if (-not (Test-Path $bridgeDir)) { New-Item -ItemType Directory -Path $bridgeDir -Force | Out-Null }
if (-not (Test-Path $viewDir)) { New-Item -ItemType Directory -Path $viewDir -Force | Out-Null }

# 4. WRITE SimBridge.cs (The Singleton)
$code_Bridge = @'
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
        GD.Print("[BRIDGE] Initializing SimCore Kernel...");
        
        // 1. Init Brain
        Kernel = new SimKernel(WorldSeed);
        
        // 2. Generate World (Slice 1 Topology)
        GD.Print($"[BRIDGE] Generating Galaxy: {StarCount} Stars");
        GalaxyGenerator.Generate(Kernel.State, StarCount, GalaxyRadius);
        
        GD.Print($"[BRIDGE] Ready. Signature: {Kernel.State.GetSignature()}");
    }

    public override void _Process(double delta)
    {
        // SLICE 1: No automatic ticking yet.
        // We will drive this manually or via UI buttons later.
    }
}
'@
[System.IO.File]::WriteAllText((Join-Path $bridgeDir "SimBridge.cs"), $code_Bridge)

# 5. WRITE GalaxyView.cs (The Renderer)
$code_View = @'
using Godot;
using SimCore.Entities;
using SpaceTradeEmpire.Bridge;
using System.Collections.Generic;

namespace SpaceTradeEmpire.View;

public partial class GalaxyView : Node3D
{
    private SimBridge _bridge;
    private Dictionary<string, Node3D> _nodeVisuals = new();

    public override void _Ready()
    {
        // 1. Find Bridge
        _bridge = GetNode<SimBridge>("/root/SimBridge");
        if (_bridge == null)
        {
            GD.PrintErr("[VIEW] SimBridge not found at /root/SimBridge");
            return;
        }

        // 2. Draw
        DrawGalaxy();
    }

    private void DrawGalaxy()
    {
        var state = _bridge.Kernel.State;
        
        // --- DRAW STARS ---
        var starMesh = new SphereMesh { Radius = 1.0f, Height = 2.0f };
        var starMat = new StandardMaterial3D { 
            AlbedoColor = new Color(0, 0.6f, 1.0f), 
            EmissionEnabled = true, 
            Emission = new Color(0, 0.6f, 1.0f), 
            EmissionEnergyMultiplier = 2.0f 
        };
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

        // --- DRAW EDGES ---
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
        instance.LookAtFromPosition(mid, end, Vector3.Up);
        instance.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2f);
        AddChild(instance);
    }
}
'@
[System.IO.File]::WriteAllText((Join-Path $viewDir "GalaxyView.cs"), $code_View)

Write-Host "`nSUCCESS: Bridge scripts created." -ForegroundColor Green
Write-Host "Proceed to Manual Godot Setup." -ForegroundColor Yellow