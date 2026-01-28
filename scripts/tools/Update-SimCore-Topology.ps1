# SCRIPT: scripts\tools\Update-SimCore-Topology.ps1
# PURPOSE: Inject Topology (Nodes/Edges) and Galaxy Generation into SimCore.
# TARGETS: .NET 8.0
# AUTHORS: Founder Protocol (Architecture v6)

$ErrorActionPreference = "Stop"
$root = (git rev-parse --show-toplevel).Trim()
Set-Location $root

Write-Host "--- PORTING TOPOLOGY TO SIMCORE ---" -ForegroundColor Cyan

$coreDir = Join-Path $root "SimCore"
$testDir = Join-Path $root "SimCore.Tests"

# 1. ENSURE DIRECTORIES EXIST
$dirs = @(
    (Join-Path $coreDir "Entities"),
    (Join-Path $coreDir "Gen")
)
foreach ($d in $dirs) {
    if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
}

# 2. DEFINE ENTITIES (Section 3.1: Core Entities)
# We use System.Numerics for vectors to avoid Godot dependencies.
$code_Entities = @"
using System.Numerics;

namespace SimCore.Entities;

public enum NodeKind { Star, Station, Waypoint }

public class Node
{
    public string Id { get; set; } = "";
    public Vector3 Position { get; set; }
    public NodeKind Kind { get; set; }
    public string Name { get; set; } = "";
    
    // Logic: What market lives here?
    public string MarketId { get; set; } = "";
}

public class Edge
{
    public string Id { get; set; } = "";
    public string FromNodeId { get; set; } = "";
    public string ToNodeId { get; set; } = "";
    public float Distance { get; set; }
    
    // ARCHITECTURE v6: SLOT CAPACITY
    // This replaces 'Fuel' as the constraint.
    public int TotalCapacity { get; set; } = 10;
    public int UsedSlots { get; set; } = 0;
}
"@
[System.IO.File]::WriteAllText((Join-Path $coreDir "Entities/MapEntities.cs"), $code_Entities)


# 3. UPGRADE SIMSTATE
# We overwrite SimState to include the new Maps.
$code_SimState = @"
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

    // --- PLAYER STATE ---
    public long PlayerCredits { get; set; } = 1000;
    public Dictionary<string, int> PlayerCargo { get; set; } = new();
    public string PlayerLocationNodeId { get; set; } = ""; // Logic Position

    public SimState(int seed)
    {
        Tick = 0;
        Rng = new Random(seed);
    }

    public void AdvanceTick()
    {
        Tick++;
        // Future: Reset daily slot capacity here
    }

    public string GetSignature()
    {
        var sb = new StringBuilder();
        sb.Append($"Tick:{Tick}|Cred:{PlayerCredits}|Loc:{PlayerLocationNodeId}|");
        
        // Hash Markets
        foreach(var m in Markets.OrderBy(k => k.Key))
        {
            sb.Append($"Mkt:{m.Key}_Inv:{m.Value.Inventory}|");
        }
        
        // Hash Topology (Prove Generation Determinism)
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
"@
[System.IO.File]::WriteAllText((Join-Path $coreDir "SimState.cs"), $code_SimState)


# 4. PORT GALAXY GENERATOR
# Logic ported from galaxy_generator.gd
$code_Gen = @"
using System.Numerics;
using SimCore.Entities;

namespace SimCore.Gen;

public static class GalaxyGenerator
{
    public static void Generate(SimState state, int starCount, float radius)
    {
        state.Nodes.Clear();
        state.Edges.Clear();
        state.Markets.Clear(); // Clear old data

        var nodesList = new List<Node>();

        // 1. SCATTER STARS
        for (int i = 0; i < starCount; i++)
        {
            // Simple random scatter
            float x = (float)(state.Rng.NextDouble() * 2 - 1) * radius;
            float z = (float)(state.Rng.NextDouble() * 2 - 1) * radius;
            
            var node = new Node
            {
                Id = $""star_{i}"",
                Name = $""System {i}"",
                Position = new Vector3(x, 0, z),
                Kind = NodeKind.Star,
                MarketId = $""mkt_{i}""
            };
            
            state.Nodes.Add(node.Id, node);
            nodesList.Add(node);

            // Add a Market to every star for Slice 1
            state.Markets.Add(node.MarketId, new Market 
            { 
                Id = node.MarketId, 
                BasePrice = 100, 
                Inventory = 50 + state.Rng.Next(50) 
            });
        }
        
        if (nodesList.Count == 0) return;

        // Set Player Start
        state.PlayerLocationNodeId = nodesList[0].Id;

        // 2. CONNECT NEIGHBORS (Simple MST-ish or Distance connect)
        // Connect each star to its 2 closest neighbors
        int edgeCount = 0;
        foreach (var node in nodesList)
        {
            var neighbors = nodesList
                .Where(n => n.Id != node.Id)
                .OrderBy(n => Vector3.Distance(node.Position, n.Position))
                .Take(2);

            foreach (var target in neighbors)
            {
                // Undirected Graph: Check if connection exists
                string edgeId = $""edge_{GetSortedId(node.Id, target.Id)}"";
                
                if (!state.Edges.ContainsKey(edgeId))
                {
                    float dist = Vector3.Distance(node.Position, target.Position);
                    state.Edges.Add(edgeId, new Edge
                    {
                        Id = edgeId,
                        FromNodeId = node.Id,
                        ToNodeId = target.Id,
                        Distance = dist,
                        TotalCapacity = 5 // Initial slot limit
                    });
                }
            }
        }
    }

    private static string GetSortedId(string a, string b)
    {
        return string.Compare(a, b) < 0 ? $""{a}_{b}"" : $""{b}_{a}"";
    }
}
"@
[System.IO.File]::WriteAllText((Join-Path $coreDir "Gen/GalaxyGenerator.cs"), $code_Gen)


# 5. ADD TOPOLOGY TEST
$code_Test = @"
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
"@
[System.IO.File]::WriteAllText((Join-Path $testDir "GalaxyTests.cs"), $code_Test)


# 6. EXECUTE TESTS
Write-Host "`n--- VERIFYING TOPOLOGY INTEGRATION ---" -ForegroundColor Cyan
dotnet test (Join-Path $root "SpaceTradeEmpire.sln") --nologo --verbosity normal

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSUCCESS: Galaxy Topology Ported to SimCore." -ForegroundColor Green
    Write-Host "SimCore now owns the Map." -ForegroundColor Gray
} else {
    Write-Host "`nFAILURE: Tests failed." -ForegroundColor Red
    exit 1
}