<#
.SYNOPSIS
    Run the playthrough bot to completion (VICTORY/DONE) or timeout.

.DESCRIPTION
    Builds C# assemblies, launches Godot headless with playthrough_bot_v0.gd,
    captures output, and parses PLAY| prefixed lines for phase transitions.
    Exit 0 if bot reached VICTORY or DONE phase, 1 otherwise.

.PARAMETER MaxTicks
    Maximum ticks before the bot times out (default 5000).

.PARAMETER TimeoutSec
    Wall-clock timeout in seconds (default 300).

.EXAMPLE
    .\scripts\tools\Run-Playthrough.ps1
    .\scripts\tools\Run-Playthrough.ps1 -MaxTicks 10000 -TimeoutSec 600
#>
param(
    [int]$MaxTicks = 5000,
    [int]$TimeoutSec = 300
)

$ErrorActionPreference = 'Stop'

# Source shared helpers
. (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'common.ps1')

$repoRoot = Get-RepoRoot
$godotExe = Get-GodotExe -RepoRoot $repoRoot
Push-Location $repoRoot

try {
    # ── Step 1: Build ──
    Write-Host "=== Building C# project ===" -ForegroundColor Cyan
    dotnet build 'Space Trade Empire.csproj' --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

    # ── Step 2: Prepare output directory ──
    $outputDir = Join-Path $repoRoot "reports/bot/playthrough"
    if (Test-Path $outputDir) {
        Get-ChildItem -Path $outputDir -File | Remove-Item -Force
        Write-Host ('Cleared ' + $outputDir) -ForegroundColor Yellow
    } else {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    $stdoutFile = Join-Path $outputDir 'stdout.txt'
    $stderrFile = Join-Path $outputDir 'stderr.txt'
    $summaryFile = Join-Path $outputDir 'summary.txt'

    # ── Step 3: Build Godot arguments ──
    $script = 'res://scripts/tests/playthrough_bot_v0.gd'
    $godotArgs = @('--headless', '--path', '.', '-s', $script, '--', '--max-ticks', $MaxTicks)

    # ── Step 4: Launch Godot headless ──
    Write-Host "=== Launching Playthrough Bot (max-ticks=$MaxTicks) ===" -ForegroundColor Cyan
    Write-Host ('Godot: ' + $godotExe)
    Write-Host ('Script: ' + $script)
    Write-Host ('Timeout: ' + $TimeoutSec + 's')

    $proc = Start-Process -FilePath $godotExe -ArgumentList $godotArgs `
        -PassThru -RedirectStandardOutput $stdoutFile -RedirectStandardError $stderrFile `
        -WindowStyle Hidden

    $exited = $proc.WaitForExit($TimeoutSec * 1000)
    if (-not $exited) {
        Write-Host ('TIMEOUT after ' + $TimeoutSec + 's -- killing process') -ForegroundColor Red
        $proc.Kill()
        $proc.WaitForExit(5000)
    }

    # ── Step 5: Parse PLAY| lines for phase transitions ──
    Write-Host ''
    Write-Host '=== Playthrough Output ===' -ForegroundColor Cyan

    $phases = @()
    $lastPhase = ''
    $victoryReached = $false

    if (Test-Path $stdoutFile) {
        Get-Content $stdoutFile | ForEach-Object {
            if ($_ -match '^PLAY\|') {
                Write-Host $_
                $phases += $_
                if ($_ -match 'PHASE[=:](\S+)') {
                    $lastPhase = $Matches[1]
                }
                if ($_ -match 'VICTORY|DONE') {
                    $victoryReached = $true
                }
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

    # ── Step 7: Write summary report ──
    $summary = @(
        "Playthrough Bot Summary"
        "======================"
        "Date:       $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
        "MaxTicks:   $MaxTicks"
        "Timeout:    ${TimeoutSec}s"
        "Timed Out:  $(-not $exited)"
        "Phases:     $($phases.Count)"
        "Last Phase: $lastPhase"
        "Victory:    $victoryReached"
        ""
        "Phase Transitions:"
    )
    $summary += $phases
    ($summary) -join "`r`n" | Set-Content -Path $summaryFile -Encoding UTF8
    Write-Host ''
    Write-Host ('Summary written to: ' + $summaryFile) -ForegroundColor Cyan

    # ── Step 8: Verdict ──
    Write-Host ''
    if ($victoryReached) {
        Write-Host '=== PASS (VICTORY/DONE reached) ===' -ForegroundColor Green
        Write-Host ('Output directory: ' + $outputDir) -ForegroundColor Green
        exit 0
    } else {
        Write-Host '=== FAIL (VICTORY/DONE not reached) ===' -ForegroundColor Red
        Write-Host ('Last phase: ' + $lastPhase) -ForegroundColor Yellow
        Write-Host ('Output directory: ' + $outputDir) -ForegroundColor Yellow
        exit 1
    }

} finally {
    Pop-Location
}
