# Run-FHBot-MultiSeed.ps1 — Sweep multiple seeds through the first-hour bot.
# Reports aggregate pass rate. Exits 1 if ANY seed fails.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot-MultiSeed.ps1
#   powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot-MultiSeed.ps1 -Seeds 42,99,1001
#   powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot-MultiSeed.ps1 -Script deep_systems
param(
    [int[]]$Seeds = @(42, 99, 1001, 31337, 77777),
    [string]$Mode = "headless",
    [string]$Script = "first_hour"  # "first_hour" or "deep_systems"
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)

# Build once
Write-Host "=== Building C# project ==="
dotnet build "$repoRoot\SimCore\SimCore.csproj" --nologo -v q
if ($LASTEXITCODE -ne 0) {
    Write-Host "BUILD FAILED"
    exit 1
}

$botScript = switch ($Script) {
    "deep_systems" { "res://scripts/tests/test_deep_systems_v0.gd" }
    default        { "res://scripts/tests/test_first_hour_proof_v0.gd" }
}

# Detect Godot
$godot = if (Test-Path "C:\Godot\Godot_v4.6-stable_mono_win64.exe") {
    "C:\Godot\Godot_v4.6-stable_mono_win64.exe"
} else {
    Get-ChildItem "C:\Users\$env:USERNAME\Downloads" -Filter "Godot_v4.6*mono*.exe" -Recurse |
        Select-Object -First 1 -ExpandProperty FullName
}
if (-not $godot) {
    Write-Host "ERROR: Godot binary not found"
    exit 1
}

$results = @{}
$totalPassed = 0
$totalFailed = 0

foreach ($seed in $Seeds) {
    Write-Host ""
    Write-Host "=== Seed $seed ==="

    $outDir = "$repoRoot\reports\first_hour"
    if (Test-Path $outDir) { Remove-Item "$outDir\*" -Force -ErrorAction SilentlyContinue }
    else { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

    $stdoutFile = "$outDir\stdout.txt"
    $stderrFile = "$outDir\stderr.txt"

    $proc = Start-Process -FilePath $godot `
        -ArgumentList "--headless", "--path", $repoRoot, "-s", $botScript, "--", "--seed=$seed" `
        -PassThru -NoNewWindow `
        -RedirectStandardOutput $stdoutFile `
        -RedirectStandardError $stderrFile

    $timeout = 90
    if (-not $proc.WaitForExit($timeout * 1000)) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        Write-Host "  TIMEOUT after ${timeout}s"
        $results[$seed] = "TIMEOUT"
        $totalFailed++
        continue
    }

    $stdout = Get-Content $stdoutFile -Raw -ErrorAction SilentlyContinue
    $passCount = ([regex]::Matches($stdout, "ASSERT_PASS")).Count
    $failCount = ([regex]::Matches($stdout, "ASSERT_FAIL")).Count

    if ($stdout -match "PASS" -and $failCount -eq 0) {
        Write-Host "  PASS ($passCount assertions)"
        $results[$seed] = "PASS"
        $totalPassed++
    } else {
        Write-Host "  FAIL ($failCount failures, $passCount passes)"
        # Print failures
        $stdout -split "`n" | Where-Object { $_ -match "ASSERT_FAIL" } | ForEach-Object { Write-Host "    $_" }
        $results[$seed] = "FAIL"
        $totalFailed++
    }
}

Write-Host ""
Write-Host "=== Multi-Seed Summary ==="
Write-Host "Script: $Script"
foreach ($seed in $Seeds) {
    Write-Host "  Seed $seed : $($results[$seed])"
}
Write-Host "SEED_SWEEP|$totalPassed/$($Seeds.Count) passed"
Write-Host ""

if ($totalFailed -gt 0) {
    Write-Host "FAIL — $totalFailed seed(s) failed"
    exit 1
} else {
    Write-Host "PASS — all $($Seeds.Count) seeds passed"
    exit 0
}
