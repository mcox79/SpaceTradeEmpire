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
dotnet build "$repoRoot\Space Trade Empire.csproj" --nologo -v q
if ($LASTEXITCODE -ne 0) {
    Write-Host "BUILD FAILED"
    exit 1
}

$botScript = switch ($Script) {
    "deep_systems" { "res://scripts/tests/test_deep_systems_v0.gd" }
    "tutorial"     { "res://scripts/tests/test_tutorial_proof_v0.gd" }
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
# Seed variance tracking
$creditGrowths = @()
$minHulls = @()
$visitedCounts = @()
$seedStdouts = @{}
# Goal score aggregation
$goalScores = @{}  # key = "g1".."s_progress", value = array of ints

foreach ($seed in $Seeds) {
    Write-Host ""
    Write-Host "=== Seed $seed ==="

    # Delete stale quicksave to prevent inter-seed contamination
    $quicksave = Join-Path $env:APPDATA "Godot\app_userdata\Space Trade Empire\quicksave.json"
    if (Test-Path $quicksave) {
        Remove-Item $quicksave -Force -ErrorAction SilentlyContinue
        Write-Host "  Deleted stale quicksave"
    }

    # Recreate output dir to guarantee clean stdout/stderr files
    $outDir = "$repoRoot\reports\first_hour"
    if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force -ErrorAction SilentlyContinue }
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null

    $stdoutFile = "$outDir\stdout.txt"
    $stderrFile = "$outDir\stderr.txt"

    $proc = Start-Process -FilePath $godot `
        -ArgumentList "--headless", "--path", "`"$repoRoot`"", "-s", $botScript, "--", "--seed=$seed" `
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
    $passCount = ([regex]::Matches($stdout, "FH1\|ASSERT_PASS")).Count
    $failCount = ([regex]::Matches($stdout, "FH1\|ASSERT_FAIL")).Count

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

    # Extract pacing metrics for seed variance report
    $seedStdouts[$seed] = $stdout
    if ($stdout -match "EXPERIENCE\|flow_credit_growth=([\d\.\-]+)") {
        $creditGrowths += [double]$Matches[1]
    }
    if ($stdout -match "EXPERIENCE\|tension_min_hull=(\d+)") {
        $minHulls += [int]$Matches[1]
    }
    if ($stdout -match "SUMMARY\|visited=(\d+)") {
        $visitedCounts += [int]$Matches[1]
    }
    # Extract goal scores from report card
    if ($stdout -match "REPORT\|SCORES\|(.+)") {
        $scoreStr = $Matches[1]
        $scoreStr -split '\s+' | ForEach-Object {
            if ($_ -match '^(\w+)=(\d+)$') {
                $key = $Matches[1]
                $val = [int]$Matches[2]
                if (-not $goalScores.ContainsKey($key)) { $goalScores[$key] = @() }
                $goalScores[$key] += $val
            }
        }
    }
}

Write-Host ""
Write-Host "=== Multi-Seed Summary ==="
Write-Host "Script: $Script"
foreach ($seed in $Seeds) {
    Write-Host "  Seed $seed : $($results[$seed])"
}
Write-Host "SEED_SWEEP|$totalPassed/$($Seeds.Count) passed"

# === Seed Variance Report ===
function Get-StdDev($arr) {
    if ($arr.Count -lt 2) { return 0.0 }
    $mean = ($arr | Measure-Object -Average).Average
    $sumSq = 0.0
    foreach ($v in $arr) { $sumSq += ($v - $mean) * ($v - $mean) }
    return [math]::Sqrt($sumSq / ($arr.Count - 1))
}

Write-Host ""
Write-Host "=== Seed Variance Report ==="
if ($creditGrowths.Count -ge 2) {
    $cgMean = ($creditGrowths | Measure-Object -Average).Average
    $cgStd = Get-StdDev $creditGrowths
    Write-Host ("SEED_VARIANCE|credit_growth mean={0:F2} stdev={1:F2}" -f $cgMean, $cgStd)
    if ($cgStd -gt 2.0) { Write-Host "  WARNING: High credit growth variance across seeds" }
} else {
    Write-Host "SEED_VARIANCE|credit_growth insufficient_data ($($creditGrowths.Count) samples)"
}
if ($minHulls.Count -ge 2) {
    $mhMean = ($minHulls | Measure-Object -Average).Average
    $mhStd = Get-StdDev $minHulls
    Write-Host ("SEED_VARIANCE|min_hull mean={0:F1} stdev={1:F1}" -f $mhMean, $mhStd)
    if ($mhStd -gt 30) { Write-Host "  WARNING: High hull variance across seeds" }
} else {
    Write-Host "SEED_VARIANCE|min_hull insufficient_data ($($minHulls.Count) samples)"
}
if ($visitedCounts.Count -ge 2) {
    $vcMean = ($visitedCounts | Measure-Object -Average).Average
    $vcStd = Get-StdDev $visitedCounts
    Write-Host ("SEED_VARIANCE|visited mean={0:F1} stdev={1:F1}" -f $vcMean, $vcStd)
} else {
    Write-Host "SEED_VARIANCE|visited insufficient_data ($($visitedCounts.Count) samples)"
}
# === Goal Score Aggregation ===
if ($goalScores.Count -gt 0) {
    Write-Host ""
    Write-Host "=== Goal Score Aggregation ==="
    $goalLabels = @{
        "g1" = "Alive"; "g2" = "Teaches"; "g3" = "FO"; "g4" = "Profit"; "g5" = "Depth"
        "s_combat" = "Combat Feel"; "s_economy" = "Systemic Econ"; "s_faction" = "Faction"
        "s_mission" = "Mission"; "s_progress" = "Progression"
        "s_heat" = "Heat System"; "s_boot" = "Boot Experience"; "s_disclosure" = "Disclosure"
        "s_overlay" = "Overlay Health"
    }
    foreach ($key in @("g1","g2","g3","g4","g5","s_combat","s_economy","s_faction","s_mission","s_progress","s_heat","s_boot","s_disclosure","s_overlay")) {
        if ($goalScores.ContainsKey($key) -and $goalScores[$key].Count -ge 1) {
            $arr = $goalScores[$key]
            $mn = ($arr | Measure-Object -Minimum).Minimum
            $mx = ($arr | Measure-Object -Maximum).Maximum
            $avg = ($arr | Measure-Object -Average).Average
            $label = if ($goalLabels.ContainsKey($key)) { $goalLabels[$key] } else { $key }
            Write-Host ("  {0,-16} min={1} max={2} avg={3:F1}" -f $label, $mn, $mx, $avg)
        }
    }
}
Write-Host ""

if ($totalFailed -gt 0) {
    Write-Host "FAIL - $totalFailed seed(s) failed"
    exit 1
} else {
    Write-Host "PASS - all $($Seeds.Count) seeds passed"
    exit 0
}
