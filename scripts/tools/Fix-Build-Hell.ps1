# SCRIPT: scripts\tools\Fix-Build-Hell.ps1
# PURPOSE: Clean build artifacts, exclude SimCore source from Godot, and restore dependencies.
# TARGETS: .NET 8.0
# AUTHORS: Founder Protocol (Architecture v6)

$ErrorActionPreference = "Stop"
$root = (git rev-parse --show-toplevel).Trim()
Set-Location $root

Write-Host "--- FIXING BUILD CONFIGURATION ---" -ForegroundColor Cyan

# 1. CLEANUP GHOST FILES
# We delete obj/bin folders to remove any 'Duplicate Attribute' artifacts.
Write-Host "Step 1: Cleaning build artifacts..." -ForegroundColor Yellow
$dirsToClean = @(
    "bin", "obj", ".godot", 
    "SimCore/bin", "SimCore/obj", 
    "SimCore.Tests/bin", "SimCore.Tests/obj"
)
foreach ($d in $dirsToClean) {
    if (Test-Path $d) { Remove-Item -Path $d -Recurse -Force -ErrorAction SilentlyContinue }
}

# 2. PATCH GODOT CSPROJ (The Critical Fix)
# We must tell Godot NOT to compile SimCore source files, because we reference the DLL instead.
Write-Host "Step 2: Patching Godot Project file..." -ForegroundColor Yellow
$godotProject = Get-ChildItem -Path $root -Filter "*.csproj" | Where-Object { $_.Name -notlike "SimCore*" } | Select-Object -First 1

if ($godotProject) {
    [xml]$xml = Get-Content $godotProject.FullName
    $ns = $xml.Project.NamespaceURI
    
    # Check if we already have the exclusions
    $hasExclusion = $xml.Project.ItemGroup.Compile | Where-Object { $_.Remove -eq "SimCore\**" }
    
    if (-not $hasExclusion) {
        # Create new ItemGroup for exclusions
        $itemGroup = $xml.CreateElement("ItemGroup", $ns)
        
        # Exclude SimCore Source
        $compileRemove = $xml.CreateElement("Compile", $ns)
        $compileRemove.SetAttribute("Remove", "SimCore\**")
        $itemGroup.AppendChild($compileRemove) | Out-Null
        
        # Exclude Tests Source
        $testRemove = $xml.CreateElement("Compile", $ns)
        $testRemove.SetAttribute("Remove", "SimCore.Tests\**")
        $itemGroup.AppendChild($testRemove) | Out-Null

        $xml.Project.AppendChild($itemGroup) | Out-Null
        
        $xml.Save($godotProject.FullName)
        Write-Host "Fixed: Added <Compile Remove> tags to $($godotProject.Name)" -ForegroundColor Green
    } else {
        Write-Host "Skipped: Project already has exclusions." -ForegroundColor Gray
    }
}

# 3. REWRITE C# FILES (Using Literal Strings to prevent Syntax Errors)
Write-Host "Step 3: Repairing C# Syntax..." -ForegroundColor Yellow
$coreDir = Join-Path $root "SimCore"

# ENTITIES
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

# GALAXY GENERATOR (Fixed ID Syntax)
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

# 4. RESTORE & TEST
Write-Host "Step 4: Restoring Dependencies..." -ForegroundColor Yellow
dotnet restore

Write-Host "Step 5: Verifying SimCore..." -ForegroundColor Yellow
dotnet test (Join-Path $root "SpaceTradeEmpire.sln") --nologo --verbosity minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSUCCESS: Architecture Repaired." -ForegroundColor Green
    Write-Host "You may now open Godot and Build." -ForegroundColor Gray
} else {
    Write-Host "`nFAILURE: SimCore tests failed. Check output." -ForegroundColor Red
}