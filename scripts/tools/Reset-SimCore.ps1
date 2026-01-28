# SCRIPT: scripts\tools\Reset-SimCore.ps1
# PURPOSE: Full reset of SimCore architecture. Creates projects, writes fixed code, runs tests.
# TARGETS: .NET 8.0
# AUTHORS: Founder Protocol (Architecture v6)

$ErrorActionPreference = "Stop"
$root = (git rev-parse --show-toplevel).Trim()
Set-Location $root

Write-Host "--- MASTER RESET: SIMCORE ARCHITECTURE ---" -ForegroundColor Cyan

# ==========================================
# 1. INFRASTRUCTURE (Create Projects & Solution)
# ==========================================
$coreDir = Join-Path $root "SimCore"
$testDir = Join-Path $root "SimCore.Tests"

# Directories
if (-not (Test-Path $coreDir)) { New-Item -ItemType Directory -Path $coreDir -Force | Out-Null }
if (-not (Test-Path $testDir)) { New-Item -ItemType Directory -Path $testDir -Force | Out-Null }
if (-not (Test-Path "$coreDir/Entities")) { New-Item -ItemType Directory -Path "$coreDir/Entities" -Force | Out-Null }
if (-not (Test-Path "$coreDir/Gen")) { New-Item -ItemType Directory -Path "$coreDir/Gen" -Force | Out-Null }
if (-not (Test-Path "$coreDir/Commands")) { New-Item -ItemType Directory -Path "$coreDir/Commands" -Force | Out-Null }

# Projects
if (-not (Test-Path "$coreDir/SimCore.csproj")) {
    Write-Host "Creating SimCore Project..."
    dotnet new classlib -n SimCore -o $coreDir -f net8.0
}

if (-not (Test-Path "$testDir/SimCore.Tests.csproj")) {
    Write-Host "Creating Tests Project..."
    dotnet new nunit -n SimCore.Tests -o $testDir -f net8.0
    dotnet add $testDir reference $coreDir
}

# Solution
$slnPath = Join-Path $root "SpaceTradeEmpire.sln"
if (-not (Test-Path $slnPath)) {
    Write-Host "Creating Solution File..."
    dotnet new sln -n SpaceTradeEmpire
}

# Link Everything
dotnet sln $slnPath add "$coreDir/SimCore.csproj" 2>$null
dotnet sln $slnPath add "$testDir/SimCore.Tests.csproj" 2>$null

# ==========================================
# 2. CODE GENERATION (With Syntax Fixes)
# ==========================================

# A. ENTITIES
$code_Entities = @'
using System.Numerics;

namespace SimCore.Entities;

public enum NodeKind { Star, Station, Waypoint }

public class Node
{
    public string Id { get; set; } = "";
    public Vector3 Position { get; set; }
    public NodeKind Kind { get; set; }
    public string Name { get; set; } = "";
    public string MarketId { get; set; } = "";
}

public class Edge
{
    public string Id { get; set; } = "";
    public string FromNodeId { get; set; } = "";
    public string ToNodeId { get; set; } = "";
    public float Distance { get; set; }
    public int TotalCapacity { get; set; } = 10;
    public int UsedSlots { get; set; } = 0;
}
'@
[System.IO.File]::WriteAllText("$coreDir/Entities/MapEntities.cs", $code_Entities)

# B. SIMSTATE
$code_SimState = @'
using System.Text;
using System.Security.Cryptography;
using SimCore.Entities;

namespace SimCore;

public class SimState
{
    public int Tick { get; private set; }
    public Random Rng { get; private set; }
    
    public Dictionary<string, Market> Markets { get; private set; } = new();
    public Dictionary<string, Node> Nodes { get; private set; } = new();
    public Dictionary<string, Edge> Edges { get; private set; } = new();

    public long PlayerCredits { get; set; } = 1000;
    public Dictionary<string, int> PlayerCargo { get; set; } = new();
    public string PlayerLocationNodeId { get; set; } = "";

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
        sb.Append($"Tick:{Tick}|Cred:{PlayerCredits}|Loc:{PlayerLocationNodeId}|");
        
        foreach(var m in Markets.OrderBy(k => k.Key))
        {
            sb.Append($"Mkt:{m.Key}_Inv:{m.Value.Inventory}|");
        }
        
        sb.Append($"Nodes:{Nodes.Count}|Edges:{Edges.Count}|");
        if (Nodes.Count > 0)
        {
            var firstNode = Nodes.Values.OrderBy(n => n.Id).First();
            sb.Append($"FirstNode:{firstNode.Position.X:F2}|");
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

# C. ICOMMAND & KERNEL
$code_ICommand = @'
namespace SimCore;
public interface ICommand
{
    void Execute(SimState state);
}
'@
[System.IO.File]::WriteAllText("$coreDir/ICommand.cs", $code_ICommand)

$code_Kernel = @'
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
        while (_commandQueue.TryDequeue(out var cmd))
        {
            cmd.Execute(_state);
        }
        _state.AdvanceTick();
    }
}
'@
[System.IO.File]::WriteAllText("$coreDir/SimKernel.cs", $code_Kernel)

# D. GALAXY GENERATOR
$code_Gen = @'
using System.Numerics;
using SimCore.Entities;

namespace SimCore.Gen;

public static class GalaxyGenerator
{
    public static void Generate(SimState state, int starCount, float radius)
    {
        state.Nodes.Clear();
        state.Edges.Clear();
        state.Markets.Clear();

        var nodesList = new List<Node>();

        for (int i = 0; i < starCount; i++)
        {
            float x = (float)(state.Rng.NextDouble() * 2 - 1) * radius;
            float z = (float)(state.Rng.NextDouble() * 2 - 1) * radius;
            
            var node = new Node
            {
                Id = $"star_{i}",
                Name = $"System {i}",
                Position = new Vector3(x, 0, z),
                Kind = NodeKind.Star,
                MarketId = $"mkt_{i}"
            };
            
            state.Nodes.Add(node.Id, node);
            nodesList.Add(node);

            state.Markets.Add(node.MarketId, new Market 
            { 
                Id = node.MarketId, 
                BasePrice = 100, 
                Inventory = 50 + state.Rng.Next(50) 
            });
        }
        
        if (nodesList.Count == 0) return;

        state.PlayerLocationNodeId = nodesList[0].Id;

        // Connect Neighbors
        foreach (var node in nodesList)
        {
            var neighbors = nodesList
                .Where(n => n.Id != node.Id)
                .OrderBy(n => Vector3.Distance(node.Position, n.Position))
                .Take(2);

            foreach (var target in neighbors)
            {
                string edgeId = $"edge_{GetSortedId(node.Id, target.Id)}";
                
                if (!state.Edges.ContainsKey(edgeId))
                {
                    float dist = Vector3.Distance(node.Position, target.Position);
                    state.Edges.Add(edgeId, new Edge
                    {
                        Id = edgeId,
                        FromNodeId = node.Id,
                        ToNodeId = target.Id,
                        Distance = dist,
                        TotalCapacity = 5
                    });
                }
            }
        }
    }

    private static string GetSortedId(string a, string b)
    {
        return string.Compare(a, b) < 0 ? $"{a}_{b}" : $"{b}_{a}";
    }
}
'@
[System.IO.File]::WriteAllText("$coreDir/Gen/GalaxyGenerator.cs", $code_Gen)

# E. TESTS
$code_Test = @'
using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using System.Linq;

namespace SimCore.Tests;

public class GalaxyTests
{
    [Test]
    public void Generation_IsDeterministic()
    {
        var simA = new SimKernel(999);
        GalaxyGenerator.Generate(simA.State, 10, 100f);
        string hashA = simA.State.GetSignature();

        var simB = new SimKernel(999);
        GalaxyGenerator.Generate(simB.State, 10, 100f);
        string hashB = simB.State.GetSignature();

        Assert.That(hashA, Is.EqualTo(hashB));
        Assert.That(simA.State.Nodes.Count, Is.EqualTo(10));
        Assert.That(simA.State.Edges.Count, Is.GreaterThan(0));
    }

    [Test]
    public void Generation_CreatesValidMarkets()
    {
        var sim = new SimKernel(123);
        GalaxyGenerator.Generate(sim.State, 5, 50f);

        var firstNode = sim.State.Nodes.Values.First();
        Assert.That(firstNode.MarketId, Is.Not.Empty);
        Assert.That(sim.State.Markets.ContainsKey(firstNode.MarketId), Is.True);
    }
}
'@
[System.IO.File]::WriteAllText("$testDir/GalaxyTests.cs", $code_Test)

# Cleanup
Remove-Item "$coreDir/Class1.cs" -ErrorAction SilentlyContinue
Remove-Item "$testDir/UnitTest1.cs" -ErrorAction SilentlyContinue
Remove-Item "$testDir/SmokeTests.cs" -ErrorAction SilentlyContinue

# ==========================================
# 3. VERIFICATION
# ==========================================
Write-Host "`n--- RUNNING TESTS ---" -ForegroundColor Cyan
dotnet test $slnPath --nologo --verbosity normal

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSUCCESS: SimCore is Live, Linked, and Tested." -ForegroundColor Green
} else {
    Write-Host "`nFAILURE: Tests Failed." -ForegroundColor Red
}