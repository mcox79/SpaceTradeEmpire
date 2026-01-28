# SCRIPT: scripts\tools\Bootstrap-SimCore.ps1
# PURPOSE: Initialize the SimCore C# architecture, test harness, and smoke test.
# TARGETS: .NET 8.0, NUnit
# AUTHORS: Founder Protocol (Architecture v6)

$ErrorActionPreference = "Stop"
$root = (git rev-parse --show-toplevel).Trim()
Set-Location $root

Write-Host "--- BOOTSTRAPPING SIMCORE ARCHITECTURE ---" -ForegroundColor Cyan

# 1. Create Directories
$coreDir  = Join-Path $root "SimCore"
$testDir  = Join-Path $root "SimCore.Tests"
$items    = @($coreDir, $testDir)

foreach ($path in $items) {
    if (-not (Test-Path $path)) {
        New-Item -ItemType Directory -Path $path -Force | Out-Null
        Write-Host "Created: $path" -ForegroundColor Green
    }
}

# 2. Generate Projects (if missing)
if (-not (Test-Path (Join-Path $coreDir "SimCore.csproj"))) {
    Write-Host "Generating SimCore Class Library..."
    dotnet new classlib -n SimCore -o $coreDir -f net8.0
}

if (-not (Test-Path (Join-Path $testDir "SimCore.Tests.csproj"))) {
    Write-Host "Generating SimCore.Tests NUnit Project..."
    dotnet new nunit -n SimCore.Tests -o $testDir -f net8.0
    # Link Test -> Core
    dotnet add $testDir reference $coreDir
}

# 3. Create Solution (if missing) and Add Projects
$slnPath = Join-Path $root "SpaceTradeEmpire.sln"
if (-not (Test-Path $slnPath)) {
    Write-Host "Creating Solution File..."
    dotnet new sln -n SpaceTradeEmpire
}

# Always try to add (idempotent-ish) to ensure they are tracked
dotnet sln $slnPath add (Join-Path $coreDir "SimCore.csproj") 2>$null
dotnet sln $slnPath add (Join-Path $testDir "SimCore.Tests.csproj") 2>$null

# 4. WRITE THE C# FILES (Atomic Write Pattern)

# --- FILE: SimCore/SimState.cs ---
# The Single Authoritative State Object
$code_SimState = @"
using System.Text;
using System.Security.Cryptography;

namespace SimCore;

public class SimState
{
    public int Tick { get; private set; }
    public Random Rng { get; private set; } // Deterministic RNG
    public Dictionary<string, Market> Markets { get; private set; } = new();
    
    // Ledger: We use 'long' for currency to prevent floating point drift
    public long PlayerCredits { get; set; } = 1000;
    public Dictionary<string, int> PlayerCargo { get; set; } = new();

    public SimState(int seed)
    {
        Tick = 0;
        Rng = new Random(seed);
    }

    public void AdvanceTick()
    {
        Tick++;
    }

    // GOLDEN REPLAY HASH: Essential for proving determinism
    public string GetSignature()
    {
        var sb = new StringBuilder();
        sb.Append($"Tick:{Tick}|Cred:{PlayerCredits}|");
        
        // Sort keys to ensure deterministic ordering
        foreach(var m in Markets.OrderBy(k => k.Key))
        {
            sb.Append($"Mkt:{m.Key}_Inv:{m.Value.Inventory}_Price:{m.Value.CurrentPrice}|");
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
    
    // Basic Placeholder Pricing Model
    public int CurrentPrice => Math.Max(1, BasePrice + (100 - Inventory)); 
}
"@
[System.IO.File]::WriteAllText((Join-Path $coreDir "SimState.cs"), $code_SimState)


# --- FILE: SimCore/ICommand.cs ---
# The Input Contract
$code_ICommand = @"
namespace SimCore;

public interface ICommand
{
    void Execute(SimState state);
}
"@
[System.IO.File]::WriteAllText((Join-Path $coreDir "ICommand.cs"), $code_ICommand)


# --- FILE: SimCore/Commands.cs ---
# Slice 1: The Trucker (Buy/Sell)
$code_Commands = @"
namespace SimCore.Commands;

public class BuyCommand : ICommand
{
    public string MarketId { get; set; }
    public int Quantity { get; set; }

    public BuyCommand(string marketId, int quantity)
    {
        MarketId = marketId;
        Quantity = quantity;
    }

    public void Execute(SimState state)
    {
        if (!state.Markets.ContainsKey(MarketId)) return;
        var market = state.Markets[MarketId];

        int cost = market.CurrentPrice * Quantity;
        
        // Validation (The ""Rules"")
        if (state.PlayerCredits >= cost && market.Inventory >= Quantity)
        {
            // Mutate State
            state.PlayerCredits -= cost;
            market.Inventory -= Quantity;
            
            // Add to cargo
            if (!state.PlayerCargo.ContainsKey(MarketId)) state.PlayerCargo[MarketId] = 0;
            state.PlayerCargo[MarketId] += Quantity;
        }
    }
}
"@
[System.IO.File]::WriteAllText((Join-Path $coreDir "Commands.cs"), $code_Commands)


# --- FILE: SimCore/SimKernel.cs ---
# The Brain that steps time
$code_Kernel = @"
using SimCore.Commands;

namespace SimCore;

public class SimKernel
{
    private SimState _state;
    private Queue<ICommand> _commandQueue = new();

    public SimState State => _state; // Read-only access for Shell

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

        // 2. Simulate World (The ""Day Tick"")
        // In full impl, this would iterate markets, production, decay signals
        _state.AdvanceTick();
    }
}
"@
[System.IO.File]::WriteAllText((Join-Path $coreDir "SimKernel.cs"), $code_Kernel)


# --- FILE: SimCore.Tests/SmokeTests.cs ---
# The Validator: Determinism & Basic Logic
$code_Tests = @"
using NUnit.Framework;
using SimCore;
using SimCore.Commands;

namespace SimCore.Tests;

public class SmokeTests
{
    [Test]
    public void Determinism_GoldenReplay_ProducesIdenticalSignatures()
    {
        // SETUP: Two identical universes
        var simA = new SimKernel(12345);
        var simB = new SimKernel(12345);

        // SETUP MARKET
        void Setup(SimKernel k) 
        {
            k.State.Markets.Add(""m1"", new Market { Id = ""m1"", BasePrice = 10, Inventory = 100 });
        }
        Setup(simA);
        Setup(simB);

        // ACTION: Same command sequence
        var cmd = new BuyCommand(""m1"", 5);
        simA.EnqueueCommand(cmd);
        simB.EnqueueCommand(cmd);

        // STEP
        simA.Step();
        simB.Step();

        // ASSERT: Bitwise identical state
        Assert.That(simA.State.GetSignature(), Is.EqualTo(simB.State.GetSignature()));
        
        // ASSERT: Logic correctness
        Assert.That(simA.State.PlayerCredits, Is.LessThan(1000));
        Assert.That(simA.State.Markets[""m1""].Inventory, Is.EqualTo(95));
    }
}
"@
[System.IO.File]::WriteAllText((Join-Path $testDir "SmokeTests.cs"), $code_Tests)

# 5. REMOVE DEFAULT GARBAGE
$garbage = Join-Path $coreDir "Class1.cs"
if (Test-Path $garbage) { Remove-Item $garbage }
$garbage = Join-Path $testDir "UnitTest1.cs"
if (Test-Path $garbage) { Remove-Item $garbage }


# 6. RUN VALIDATION
Write-Host "`n--- EXECUTING VALIDATION GATE ---" -ForegroundColor Cyan
dotnet test $slnPath --nologo --verbosity normal

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSUCCESS: SimCore Scaffolding Deployed and Verified." -ForegroundColor Green
    Write-Host "Next Step: Open 'SpaceTradeEmpire.sln' in your IDE." -ForegroundColor Gray
} else {
    Write-Host "`nFAILURE: Tests failed. Check output above." -ForegroundColor Red
    exit 1
}