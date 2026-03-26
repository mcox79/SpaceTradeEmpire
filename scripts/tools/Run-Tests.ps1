<#
.SYNOPSIS
    Run SimCore test suite. Quiet on success, detailed diagnostics on failure.
.DESCRIPTION
    Wraps `dotnet test` to always show failed test names and error messages
    regardless of build verbosity. On success: summary line only. On failure:
    each failed test with its error message and stack trace.
.PARAMETER Filter
    NUnit test filter expression (e.g. "RoadmapConsistency", "Determinism").
    Multiple filters can be OR'd with pipe: "Shipyard|PowerBudget".
.PARAMETER Config
    Build configuration (default: Release).
.PARAMETER NoBuild
    Skip build step (use existing binaries).
.PARAMETER UpdateGolden
    Set STE_UPDATE_GOLDEN=1 for golden hash regeneration. The test will
    intentionally fail and print PASTE_GENESIS / PASTE_FINAL values.
.PARAMETER KillStale
    Kill stale testhost and dotnet processes before running. Use when tests
    hang due to file locks from previous interrupted runs.
.PARAMETER ShowOutput
    Show raw dotnet test output in addition to parsed TRX results.
    Useful for debugging build failures or test infrastructure issues.
.EXAMPLE
    .\scripts\tools\Run-Tests.ps1
    .\scripts\tools\Run-Tests.ps1 -Filter "Determinism"
    .\scripts\tools\Run-Tests.ps1 -Filter "RoadmapConsistency" -NoBuild
    .\scripts\tools\Run-Tests.ps1 -Filter "GoldenReplay" -UpdateGolden
    .\scripts\tools\Run-Tests.ps1 -KillStale
    .\scripts\tools\Run-Tests.ps1 -Filter "Shipyard|PowerBudget" -ShowOutput
#>
param(
    [string]$Filter = "",
    [string]$Config = "Release",
    [switch]$NoBuild,
    [switch]$UpdateGolden,
    [switch]$KillStale,
    [switch]$ShowOutput
)

$ErrorActionPreference = "Stop"
$ProjectPath = Join-Path $PSScriptRoot "..\..\SimCore.Tests\SimCore.Tests.csproj"
$TrxDir = Join-Path $PSScriptRoot "..\..\SimCore.Tests\TestResults"
$TrxFile = Join-Path $TrxDir "RunTests.trx"

# --- Kill stale processes if requested ---
if ($KillStale) {
    $killed = @()
    foreach ($procName in @("testhost", "dotnet")) {
        $procs = Get-Process -Name $procName -ErrorAction SilentlyContinue
        if ($procs) {
            $procs | Stop-Process -Force -ErrorAction SilentlyContinue
            $killed += "$procName($($procs.Count))"
        }
    }
    if ($killed.Count -gt 0) {
        Write-Host "Killed stale: $($killed -join ', ')" -ForegroundColor DarkYellow
    }
}

# --- Set environment for golden hash update ---
if ($UpdateGolden) {
    $env:STE_UPDATE_GOLDEN = "1"
    Write-Host "STE_UPDATE_GOLDEN=1 (golden hash regeneration mode)" -ForegroundColor Cyan
}

# Clean previous TRX to avoid stale reads.
if (Test-Path $TrxFile) { Remove-Item $TrxFile -Force }

# Build command args: quiet build, TRX logger for post-mortem.
# Use normal verbosity for UpdateGolden (need PASTE_ lines from TestContext.Out).
$verbosity = if ($UpdateGolden -or $ShowOutput) { "n" } else { "q" }
$testArgs = @(
    "test", $ProjectPath,
    "-c", $Config,
    "--nologo",
    "-v", $verbosity,
    "--logger", "trx;LogFileName=RunTests.trx"
)
if ($NoBuild) { $testArgs += "--no-build" }
if ($Filter -ne "") {
    $testArgs += "--filter"
    $testArgs += $Filter
}

# Run tests, capture output for diagnostics.
$rawOutput = & dotnet @testArgs 2>&1
$dotnetExitCode = $LASTEXITCODE

# Show raw output if requested or if UpdateGolden (user needs PASTE_ lines).
if ($ShowOutput -or $UpdateGolden) {
    foreach ($line in $rawOutput) {
        $lineStr = "$line"
        # Highlight PASTE_ lines for golden hash updates.
        if ($lineStr -match "PASTE_") {
            Write-Host $lineStr -ForegroundColor Cyan
        } elseif ($lineStr -match "Error|FAIL|error") {
            Write-Host $lineStr -ForegroundColor Red
        } else {
            Write-Host $lineStr
        }
    }
    Write-Host ""
}

# --- Clean up golden env var ---
if ($UpdateGolden) {
    Remove-Item Env:\STE_UPDATE_GOLDEN -ErrorAction SilentlyContinue
}

# Parse TRX for results.
if (-not (Test-Path $TrxFile)) {
    Write-Host "ERROR: TRX file not generated (dotnet exit code: $dotnetExitCode). Build may have failed." -ForegroundColor Red
    Write-Host ""
    # Show captured output if not already shown.
    if (-not $ShowOutput -and -not $UpdateGolden) {
        Write-Host "--- Raw output ---" -ForegroundColor DarkGray
        foreach ($line in $rawOutput) {
            Write-Host "$line"
        }
    }
    exit $dotnetExitCode
}

[xml]$trx = Get-Content $TrxFile
$ns = @{ t = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010" }

$allResults = @(Select-Xml -Xml $trx -XPath "//t:UnitTestResult" -Namespace $ns |
    ForEach-Object { $_.Node })

$total   = $allResults.Count
$passed  = @($allResults | Where-Object { $_.outcome -eq "Passed" }).Count
$failed  = @($allResults | Where-Object { $_.outcome -eq "Failed" }).Count
$skipped = @($allResults | Where-Object { $_.outcome -eq "NotExecuted" }).Count

# Duration from TRX timestamps.
$duration = ""
$times = Select-Xml -Xml $trx -XPath "//t:Times" -Namespace $ns | ForEach-Object { $_.Node }
if ($times) {
    $start  = [datetime]$times.start
    $finish = [datetime]$times.finish
    $span = $finish - $start
    if ($span.TotalMinutes -ge 1) {
        $duration = "{0}m {1}s" -f [int][math]::Floor($span.TotalMinutes), $span.Seconds
    } else {
        $duration = "{0:F1}s" -f $span.TotalSeconds
    }
}

if ($failed -eq 0) {
    # Success: one clean summary line.
    Write-Host "Passed! - Failed: 0, Passed: $passed, Skipped: $skipped, Total: $total, Duration: $duration" -ForegroundColor Green
    exit 0
}

# Failure: show every failed test with full diagnostics.
Write-Host ""
Write-Host "FAILED! - Failed: $failed, Passed: $passed, Skipped: $skipped, Total: $total, Duration: $duration" -ForegroundColor Red
Write-Host ""

$failedResults = @($allResults | Where-Object { $_.outcome -eq "Failed" } |
    Sort-Object { $_.testName })

foreach ($r in $failedResults) {
    $testName = $r.testName
    $dur = $r.duration

    Write-Host "  FAIL: $testName [$dur]" -ForegroundColor Red

    # Error message.
    $msgNode = Select-Xml -Xml $r -XPath ".//t:ErrorInfo/t:Message" -Namespace $ns |
        ForEach-Object { $_.Node }
    if ($msgNode) {
        $errorMsg = $msgNode.InnerText
        $errorMsg -split "`n" | ForEach-Object {
            Write-Host "        $_" -ForegroundColor Yellow
        }
    }

    # Stack trace (trimmed to first 5 frames for readability).
    $stNode = Select-Xml -Xml $r -XPath ".//t:ErrorInfo/t:StackTrace" -Namespace $ns |
        ForEach-Object { $_.Node }
    if ($stNode) {
        $traceText = $stNode.InnerText
        $frames = @($traceText -split "`n" | Where-Object { $_.Trim() -ne "" })
        $showFrames = @($frames | Select-Object -First 5)
        foreach ($frame in $showFrames) {
            Write-Host "        $($frame.Trim())" -ForegroundColor DarkGray
        }
        if ($frames.Count -gt 5) {
            Write-Host "        ... ($($frames.Count - 5) more frames)" -ForegroundColor DarkGray
        }
    }
    Write-Host ""
}

exit 1
