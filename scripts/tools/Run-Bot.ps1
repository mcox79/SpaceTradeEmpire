<#
.SYNOPSIS
  Unified bot runner. Modes: trade, combat, stress, full.
.EXAMPLE
  .\scripts\tools\Run-Bot.ps1 -Mode trade
  .\scripts\tools\Run-Bot.ps1 -Mode combat -Cycles 100
  .\scripts\tools\Run-Bot.ps1 -Mode stress -Cycles 2000
#>
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('trade','combat','stress','full')]
    [string]$Mode,
    [int]$Cycles = 0,       # 0 = auto per mode
    [int]$TimeoutSec = 0    # 0 = auto per mode
)

$ErrorActionPreference = 'Stop'

# Source shared helpers
. (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'common.ps1')

$repoRoot = Get-RepoRoot
$godotExe = Get-GodotExe -RepoRoot $repoRoot
Push-Location $repoRoot

try {
    # ── Mode configuration ──
    $defaultCycles = @{
        'trade'  = 400
        'combat' = 200
        'stress' = 1500
        'full'   = 600
    }
    $defaultTimeout = @{
        'trade'  = 120
        'combat' = 90
        'stress' = 300
        'full'   = 180
    }

    $activeCycles = if ($Cycles -gt 0) { $Cycles } else { $defaultCycles[$Mode] }
    $activeTimeout = if ($TimeoutSec -gt 0) { $TimeoutSec } else { $defaultTimeout[$Mode] }

    $outputDir = Join-Path $repoRoot "reports/bot/$Mode"

    # ── Step 1: Build ──
    Write-Host "=== Building C# project ===" -ForegroundColor Cyan
    dotnet build 'Space Trade Empire.csproj' --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

    # ── Step 2: Clean output directory ──
    if (Test-Path $outputDir) {
        Get-ChildItem -Path $outputDir -File | Remove-Item -Force
        Write-Host ('Cleared ' + $outputDir) -ForegroundColor Yellow
    } else {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    $stdoutFile = Join-Path $outputDir 'stdout.txt'
    $stderrFile = Join-Path $outputDir 'stderr.txt'

    # ── Step 3: Build Godot arguments ──
    $script = 'res://scripts/tests/exploration_bot_v1.gd'
    $godotArgs = @('--headless', '--path', '.', '-s', $script, '--', '--mode', $Mode, '--cycles', $activeCycles)

    # ── Step 4: Launch Godot headless ──
    Write-Host "=== Launching Bot ($Mode mode, $activeCycles cycles) ===" -ForegroundColor Cyan
    Write-Host ('Godot: ' + $godotExe)
    Write-Host ('Script: ' + $script)
    Write-Host ('Timeout: ' + $activeTimeout + 's')

    $proc = Start-Process -FilePath $godotExe -ArgumentList $godotArgs `
        -PassThru -RedirectStandardOutput $stdoutFile -RedirectStandardError $stderrFile `
        -WindowStyle Hidden

    $exited = $proc.WaitForExit($activeTimeout * 1000)
    if (-not $exited) {
        Write-Host ('TIMEOUT after ' + $activeTimeout + 's -- killing process') -ForegroundColor Red
        $proc.Kill()
        $proc.WaitForExit(5000)
    }

    # ── Step 5: Parse output ──
    Write-Host ''
    Write-Host '=== Bot Output ===' -ForegroundColor Cyan
    $passLine = $null
    $failLine = $null
    if (Test-Path $stdoutFile) {
        Get-Content $stdoutFile | ForEach-Object {
            if ($_ -match '^BOT\|') {
                Write-Host $_
                if ($_ -match '^BOT\|PASS') { $passLine = $_ }
                if ($_ -match '^BOT\|FAIL') { $failLine = $_ }
            }
        }
    }

    # ── Step 6: Check for errors ──
    if (Test-Path $stderrFile) {
        $stderrContent = Get-Content $stderrFile -Raw
        if ($stderrContent -and $stderrContent.Trim().Length -gt 0) {
            $errorLines = ($stderrContent -split "`n") | Where-Object { $_ -match 'SCRIPT ERROR|Parse Error|ERROR' }
            if ($errorLines) {
                Write-Host ''
                Write-Host '=== Errors ===' -ForegroundColor Red
                $errorLines | ForEach-Object { Write-Host $_ }
            }
        }
    }

    # ── Step 7: Read JSON report if available ──
    $reportJson = Join-Path $outputDir 'report.json'
    if (Test-Path $reportJson) {
        Write-Host ''
        Write-Host '=== Report ===' -ForegroundColor Cyan
        $report = Get-Content $reportJson -Raw | ConvertFrom-Json
        Write-Host ('  Mode: ' + $report.mode)
        Write-Host ('  Cycles: ' + $report.cycles)
        Write-Host ('  Net Profit: ' + $report.net_profit)
        Write-Host ('  Buys: ' + $report.buys + '  Sells: ' + $report.sells + '  Travels: ' + $report.travels)
        Write-Host ('  Combats: ' + $report.combats + '  Kills: ' + $report.kills)
        Write-Host ('  Nodes: ' + $report.nodes_visited + '/' + $report.nodes_total)
        Write-Host ('  Flags: ' + $report.flags.Count)
    }

    # ── Step 8: Verdict ──
    Write-Host ''
    if ($failLine) {
        Write-Host '=== FAIL ===' -ForegroundColor Red
        Write-Host $failLine
        Write-Host ''
        Write-Host ('Output directory: ' + $outputDir) -ForegroundColor Yellow
        exit 1
    } elseif ($passLine) {
        Write-Host '=== PASS ===' -ForegroundColor Green
        Write-Host ''
        Write-Host ('Output directory: ' + $outputDir) -ForegroundColor Green
        exit 0
    } else {
        Write-Host '=== NO VERDICT ===' -ForegroundColor Yellow
        Write-Host 'Bot did not produce PASS or FAIL. Check stdout.txt and stderr.txt.' -ForegroundColor Yellow
        Write-Host ''
        Write-Host ('Output directory: ' + $outputDir) -ForegroundColor Yellow
        exit 1
    }

} finally {
    Pop-Location
}
