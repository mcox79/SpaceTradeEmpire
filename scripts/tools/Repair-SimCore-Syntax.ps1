# SCRIPT: scripts\tools\Repair-SimCore-Syntax.ps1
# PURPOSE: Overwrite SimCore C# files with CORRECT syntax (fixing the quote escaping errors).
# TARGETS: .NET 8.0
# AUTHORS: Founder Protocol (Architecture v6)

$ErrorActionPreference = "Stop"
$root = (git rev-parse --show-toplevel).Trim()
Set-Location $root

Write-Host "--- REPAIRING SIMCORE C# SYNTAX ---" -ForegroundColor Cyan
$coreDir = Join-Path $root "SimCore"

# 1. REPAIR ENTITIES (Using Literal String @' to prevent corruption)
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
[System.IO.File]::WriteAllText((Join-Path $coreDir "Entities/MapEntities.cs"), $code_Entities)
Write-Host "Fixed: Entities/MapEntities.cs" -ForegroundColor Green


# 2. REPAIR SIMSTATE
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
[System.IO.File]::WriteAllText((Join-Path $coreDir "SimState.cs"), $code_SimState)
Write-Host "Fixed: SimState.cs" -ForegroundColor Green


# 3. REPAIR GALAXY GENERATOR (The Main Offender)
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
[System.IO.File]::WriteAllText((Join-Path $coreDir "Gen/GalaxyGenerator.cs"), $code_Gen)
Write-Host "Fixed: Gen/GalaxyGenerator.cs" -ForegroundColor Green

# 4. RUN VALIDATION AGAIN
Write-Host "`n--- RE-RUNNING TESTS ---" -ForegroundColor Cyan
dotnet test (Join-Path $root "SpaceTradeEmpire.sln") --nologo --verbosity normal

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSUCCESS: Syntax Repaired. Build should now pass." -ForegroundColor Green
} else {
    Write-Host "`nFAILURE: Tests still failed. Check output." -ForegroundColor Red
}