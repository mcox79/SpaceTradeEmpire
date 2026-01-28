# SCRIPT: scripts\tools\Update-SimCore-Movement.ps1
# PURPOSE: Implement Fleet Entities, Travel Commands, and Movement Logic in SimCore.
# TARGETS: .NET 8.0
# AUTHORS: Founder Protocol (Architecture v6)

$ErrorActionPreference = "Stop"
$root = (git rev-parse --show-toplevel).Trim()
Set-Location $root

Write-Host "--- IMPLEMENTING SIMCORE MOVEMENT SYSTEM ---" -ForegroundColor Cyan

$coreDir = Join-Path $root "SimCore"
$testDir = Join-Path $root "SimCore.Tests"

# 1. CREATE DIRECTORIES
if (-not (Test-Path "$coreDir/Systems")) { New-Item -ItemType Directory -Path "$coreDir/Systems" -Force | Out-Null }

# 2. DEFINE FLEET ENTITY
# Replaces the old 'player.gd' monolithic approach with a clean Data Class.
$code_Fleet = @'
namespace SimCore.Entities;

public enum FleetState { Idle, Travel, Docked }

public class Fleet
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";
    
    // LOCATION STATE
    public string CurrentNodeId { get; set; } = ""; // If docked/idle
    public string DestinationNodeId { get; set; } = ""; // If traveling
    public string CurrentEdgeId { get; set; } = "";     // If traveling
    
    // TRAVEL PROGRESS
    public float TravelProgress { get; set; } = 0f; // 0.0 to 1.0
    public float Speed { get; set; } = 0.2f;        // Segments per tick
    
    public FleetState State { get; set; } = FleetState.Idle;
    
    // CARGO (Simple Dictionary for now)
    public Dictionary<string, int> Cargo { get; set; } = new();
}
'@
[System.IO.File]::WriteAllText("$coreDir/Entities/Fleet.cs", $code_Fleet)


# 3. UPDATE SIMSTATE
# We add a Dictionary to track all fleets in the universe.
$code_SimState = @'
using System.Text;
using System.Security.Cryptography;
using SimCore.Entities;

namespace SimCore;

public class SimState
{
    public int Tick { get; private set; }
    public Random Rng { get; private set; }
    
    // --- WORLD STATE ---
    public Dictionary<string, Market> Markets { get; private set; } = new();
    public Dictionary<string, Node> Nodes { get; private set; } = new();
    public Dictionary<string, Edge> Edges { get; private set; } = new();
    
    // --- ACTORS ---
    public Dictionary<string, Fleet> Fleets { get; private set; } = new();

    // --- PLAYER LEDGER ---
    public long PlayerCredits { get; set; } = 1000;

    public SimState(int seed)
    {
        Tick = 0;
        Rng = new Random(seed);
    }

    public void AdvanceTick()
    {
        Tick++;
    }

    public string GetSignature()
    {
        var sb = new StringBuilder();
        sb.Append($"Tick:{Tick}|Cred:{PlayerCredits}|");
        
        // Hash Fleets (Location is critical for sync)
        foreach(var f in Fleets.OrderBy(k => k.Key))
        {
            sb.Append($"Flt:{f.Key}_N:{f.Value.CurrentNodeId}_S:{f.Value.State}|");
        }

        // Hash Markets
        foreach(var m in Markets.OrderBy(k => k.Key))
        {
            sb.Append($"Mkt:{m.Key}_Inv:{m.Value.Inventory}|");
        }
        
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }
}

public class Market
{
    public string Id { get; set; } = "";
    public int Inventory { get; set; }
    public int BasePrice { get; set; }
    public int CurrentPrice => Math.Max(1, BasePrice + (100 - Inventory)); 
}
'@
[System.IO.File]::WriteAllText("$coreDir/SimState.cs", $code_SimState)


# 4. CREATE MOVEMENT SYSTEM
# This is the Logic that moves ships. It replaces 'fleet.move_toward' from GDScript.
$code_System = @'
using SimCore.Entities;

namespace SimCore.Systems;

public static class MovementSystem
{
    public static void Process(SimState state)
    {
        foreach (var fleet in state.Fleets.Values)
        {
            if (fleet.State != FleetState.Travel) continue;

            // Advance Progress
            fleet.TravelProgress += fleet.Speed;

            // Arrival Check
            if (fleet.TravelProgress >= 1.0f)
            {
                // ARRIVAL LOGIC
                fleet.CurrentNodeId = fleet.DestinationNodeId;
                fleet.State = FleetState.Docked; // Auto-dock on arrival for now
                fleet.TravelProgress = 0f;
                fleet.CurrentEdgeId = "";
                fleet.DestinationNodeId = "";
            }
        }
    }
}
'@
[System.IO.File]::WriteAllText("$coreDir/Systems/MovementSystem.cs", $code_System)


# 5. CREATE TRAVEL COMMAND
# The "Input" that tells the sim to start a journey.
$code_Cmd = @'
using SimCore.Entities;

namespace SimCore.Commands;

public class TravelCommand : ICommand
{
    public string FleetId { get; set; }
    public string TargetNodeId { get; set; }

    public TravelCommand(string fleetId, string targetNodeId)
    {
        FleetId = fleetId;
        TargetNodeId = targetNodeId;
    }

    public void Execute(SimState state)
    {
        if (!state.Fleets.ContainsKey(FleetId)) return;
        var fleet = state.Fleets[FleetId];
        
        // RULE: Cannot move if already moving
        if (fleet.State == FleetState.Travel) return;

        // RULE: Must be connected
        // Simple linear search for Slice 1. Optimization later.
        string edgeId = "";
        foreach(var e in state.Edges.Values)
        {
            if ((e.FromNodeId == fleet.CurrentNodeId && e.ToNodeId == TargetNodeId) ||
                (e.ToNodeId == fleet.CurrentNodeId && e.FromNodeId == TargetNodeId))
            {
                edgeId = e.Id;
                break;
            }
        }

        if (string.IsNullOrEmpty(edgeId)) return; // No route

        // START TRAVEL
        fleet.State = FleetState.Travel;
        fleet.DestinationNodeId = TargetNodeId;
        fleet.CurrentEdgeId = edgeId;
        fleet.TravelProgress = 0f;
    }
}
'@
[System.IO.File]::WriteAllText("$coreDir/Commands/TravelCommand.cs", $code_Cmd)


# 6. UPDATE KERNEL
# Wire the new system into the main loop.
$code_Kernel = @'
using SimCore.Commands;
using SimCore.Systems;

namespace SimCore;

public class SimKernel
{
    private SimState _state;
    private Queue<ICommand> _commandQueue = new();

    public SimState State => _state; 

    public SimKernel(int seed)
    {
        _state = new SimState(seed);
    }

    public void EnqueueCommand(ICommand cmd)
    {
        _commandQueue.Enqueue(cmd);
    }

    public void Step()
    {
        // 1. Process Input
        while (_commandQueue.TryDequeue(out var cmd))
        {
            cmd.Execute(_state);
        }

        // 2. Systems Execution [cite: 1442]
        MovementSystem.Process(_state);

        // 3. Tick Advance
        _state.AdvanceTick();
    }
}
'@
[System.IO.File]::WriteAllText("$coreDir/SimKernel.cs", $code_Kernel)


# 7. ADD MOVEMENT TEST
# Prove it works before we touch Godot.
$code_Test = @'
using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Entities;
using SimCore.Gen;

namespace SimCore.Tests;

public class MovementTests
{
    [Test]
    public void Fleet_CanTravel_BetweenConnectedNodes()
    {
        var kernel = new SimKernel(12345);
        
        // Setup: Tiny Galaxy
        GalaxyGenerator.Generate(kernel.State, 2, 50f);
        
        // Find two connected nodes
        var edge = kernel.State.Edges.Values.First();
        var startNode = edge.FromNodeId;
        var endNode = edge.ToNodeId;

        // Spawn Fleet
        var fleet = new Fleet { Id = "f1", CurrentNodeId = startNode, State = FleetState.Docked };
        kernel.State.Fleets.Add(fleet.Id, fleet);

        // COMMAND: Travel
        kernel.EnqueueCommand(new TravelCommand("f1", endNode));
        kernel.Step();

        // ASSERT: Moving
        Assert.That(fleet.State, Is.EqualTo(FleetState.Travel));
        Assert.That(fleet.DestinationNodeId, Is.EqualTo(endNode));
        Assert.That(fleet.TravelProgress, Is.GreaterThan(0f));

        // SIMULATE: Travel to completion (Speed 0.2 means ~5 ticks)
        for(int i=0; i<10; i++) kernel.Step();

        // ASSERT: Arrived
        Assert.That(fleet.State, Is.EqualTo(FleetState.Docked));
        Assert.That(fleet.CurrentNodeId, Is.EqualTo(endNode));
    }
}
'@
[System.IO.File]::WriteAllText("$testDir/MovementTests.cs", $code_Test)

# 8. VERIFY
Write-Host "`n--- VERIFYING MOVEMENT LOGIC ---" -ForegroundColor Cyan
dotnet test $root/SpaceTradeEmpire.sln --nologo --verbosity normal

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSUCCESS: Movement System implemented and verified." -ForegroundColor Green
} else {
    Write-Host "`nFAILURE: Tests failed." -ForegroundColor Red
}