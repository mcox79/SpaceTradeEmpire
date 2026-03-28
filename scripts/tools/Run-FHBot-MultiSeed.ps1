# Run-FHBot-MultiSeed.ps1 — Sweep multiple seeds through the first-hour bot.
# Reports aggregate pass rate. Exits 1 if ANY seed fails.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot-MultiSeed.ps1
#   powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot-MultiSeed.ps1 -Seeds 42,99,1001
#   powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot-MultiSeed.ps1 -Script deep_systems
param(
    [string[]]$Seeds = @("42", "99", "1001", "31337", "77777"),
    [string]$Mode = "headless",
    [string]$Script = "first_hour"  # "first_hour" or "deep_systems"
)

# Robust seed parsing: handles -Seeds "42,99,1001" (single comma-separated string)
# as well as -Seeds 42,99,1001 (PS array) and -Seeds @(42,99) (explicit array).
$parsedSeeds = @()
foreach ($s in $Seeds) {
    foreach ($part in ($s -split ',')) {
        $trimmed = $part.Trim()
        if ($trimmed -match '^\d+$') {
            $parsedSeeds += [int]$trimmed
        }
    }
}
if ($parsedSeeds.Count -eq 0) {
    Write-Host "ERROR: No valid seeds parsed from: $Seeds"
    exit 1
}
$Seeds = $parsedSeeds
Write-Host "Seeds: $($Seeds -join ', ') ($($Seeds.Count) total)"

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
    "deep_systems"   { "res://scripts/tests/test_deep_systems_v0.gd" }
    "tutorial"       { "res://scripts/tests/test_tutorial_proof_v0.gd" }
    "chaos_tutorial" { "res://scripts/tests/test_chaos_tutorial_v0.gd" }
    default          { "res://scripts/tests/test_first_hour_proof_v0.gd" }
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

    # Use script-specific output dir to avoid overwriting other bots' results
    $outDir = "$repoRoot\reports\first_hour\$Script"
    if ($seed -eq $Seeds[0]) {
        # Only clean on first seed of this run
        if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force -ErrorAction SilentlyContinue }
    }
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null

    $stdoutFile = "$outDir\stdout_seed_$seed.txt"
    $stderrFile = "$outDir\stderr_seed_$seed.txt"

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
    # Detect assertion prefix based on script type
    $assertPrefix = switch ($Script) {
        "deep_systems"   { "DS1" }
        "tutorial"       { "TUT" }
        "chaos_tutorial" { "CHAOS" }
        default          { "FH1" }
    }
    $passCount = ([regex]::Matches($stdout, "$assertPrefix\|ASSERT_PASS")).Count
    $failCount = ([regex]::Matches($stdout, "$assertPrefix\|ASSERT_FAIL")).Count

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
# === Cross-Seed Variance Report (per-metric comparison table) ===
Write-Host ""
Write-Host "=== Cross-Seed Variance Table ==="

# Parse per-seed metrics from captured stdout
$perSeedGoals = @{}       # seed → hashtable of goal_name → score
$perSeedPassCounts = @{}  # seed → int
$perSeedFailCounts = @{}  # seed → int
$perSeedFlags = @{}       # seed → list of flag strings
$perSeedFps = @{}         # seed → hashtable (min, avg, max)
$perSeedEvents = @{}      # seed → list of event strings
$perSeedDeadZones = @{}   # seed → list of dead zone strings

foreach ($seed in $Seeds) {
    $stdout = $seedStdouts[$seed]
    if (-not $stdout) { continue }

    # Assertion counts
    $perSeedPassCounts[$seed] = ([regex]::Matches($stdout, "$assertPrefix\|ASSERT_PASS")).Count
    $perSeedFailCounts[$seed] = ([regex]::Matches($stdout, "$assertPrefix\|ASSERT_FAIL")).Count

    # Goal scores from GOAL lines: FH1|GOAL|name=value or EXP|GOAL|name=value
    $perSeedGoals[$seed] = @{}
    $goalMatches = [regex]::Matches($stdout, "(?:$assertPrefix|EXP)\|GOAL\|(\w+)=(\d+)")
    foreach ($gm in $goalMatches) {
        $perSeedGoals[$seed][$gm.Groups[1].Value] = [int]$gm.Groups[2].Value
    }
    # Also capture from REPORT|SCORES line
    if ($stdout -match "REPORT\|SCORES\|(.+)") {
        $scoreStr = $Matches[1]
        $scoreStr -split '\s+' | ForEach-Object {
            if ($_ -match '^(\w+)=(\d+)$') {
                $perSeedGoals[$seed][$Matches[1]] = [int]$Matches[2]
            }
        }
    }

    # Flags: FH1|FLAG|xxx or EXP|FLAG|xxx
    $perSeedFlags[$seed] = @()
    $flagMatches = [regex]::Matches($stdout, "(?:$assertPrefix|EXP)\|FLAG\|(.+)")
    foreach ($fm in $flagMatches) {
        $perSeedFlags[$seed] += $fm.Groups[1].Value.Trim()
    }

    # FPS data: EXP|PERF|fps_min=X|fps_avg=Y|fps_max=Z (or similar)
    $perSeedFps[$seed] = @{}
    if ($stdout -match "EXP\|PERF\|fps_min=([\d\.]+)") { $perSeedFps[$seed]["min"] = $Matches[1] }
    if ($stdout -match "EXP\|PERF\|fps_avg=([\d\.]+)") { $perSeedFps[$seed]["avg"] = $Matches[1] }
    if ($stdout -match "EXP\|PERF\|fps_max=([\d\.]+)") { $perSeedFps[$seed]["max"] = $Matches[1] }
    # Also try single-line format: EXP|PERF|min=X|avg=Y|max=Z
    if ($stdout -match "EXP\|PERF\|min=([\d\.]+)\|avg=([\d\.]+)\|max=([\d\.]+)") {
        $perSeedFps[$seed]["min"] = $Matches[1]
        $perSeedFps[$seed]["avg"] = $Matches[2]
        $perSeedFps[$seed]["max"] = $Matches[3]
    }

    # Decision events: EXP|EVENT|xxx
    $perSeedEvents[$seed] = @()
    $eventMatches = [regex]::Matches($stdout, "EXP\|EVENT\|(.+)")
    foreach ($em in $eventMatches) {
        $perSeedEvents[$seed] += $em.Groups[1].Value.Trim()
    }

    # Dead zones: EXP|DEAD_ZONE|xxx
    $perSeedDeadZones[$seed] = @()
    $dzMatches = [regex]::Matches($stdout, "EXP\|DEAD_ZONE\|(.+)")
    foreach ($dz in $dzMatches) {
        $perSeedDeadZones[$seed] += $dz.Groups[1].Value.Trim()
    }
}

# Build seed column headers
$seedHeaders = ($Seeds | ForEach-Object { "seed_$_" }) -join "|"
Write-Host "SEED_VARIANCE|metric|$seedHeaders|range|verdict"

# Helper: emit a variance row
function Write-VarianceRow {
    param([string]$MetricName, [hashtable]$SeedValues, [int[]]$SeedList, [double]$HighVarianceThreshold)
    $vals = @()
    $colStrs = @()
    foreach ($s in $SeedList) {
        if ($SeedValues.ContainsKey($s)) {
            $v = $SeedValues[$s]
            $vals += [double]$v
            $colStrs += "$v"
        } else {
            $colStrs += "-"
        }
    }
    if ($vals.Count -ge 2) {
        $mn = ($vals | Measure-Object -Minimum).Minimum
        $mx = ($vals | Measure-Object -Maximum).Maximum
        $range = $mx - $mn
        $verdict = if ($range -gt $HighVarianceThreshold) { "HIGH_VARIANCE" } else { "OK" }
        $colStr = $colStrs -join "|"
        Write-Host "SEED_VARIANCE|$MetricName|$colStr|$range|$verdict"
    } else {
        $colStr = $colStrs -join "|"
        Write-Host "SEED_VARIANCE|$MetricName|$colStr|-|INSUFFICIENT_DATA"
    }
}

# Assertion pass counts
$assertPassVals = @{}
foreach ($s in $Seeds) { if ($perSeedPassCounts.ContainsKey($s)) { $assertPassVals[$s] = $perSeedPassCounts[$s] } }
Write-VarianceRow -MetricName "assert_pass" -SeedValues $assertPassVals -SeedList $Seeds -HighVarianceThreshold 10

# Assertion fail counts
$assertFailVals = @{}
$anyFail = $false
foreach ($s in $Seeds) {
    if ($perSeedFailCounts.ContainsKey($s)) {
        $assertFailVals[$s] = $perSeedFailCounts[$s]
        if ($perSeedFailCounts[$s] -gt 0) { $anyFail = $true }
    }
}
$failColStrs = @()
foreach ($s in $Seeds) {
    if ($assertFailVals.ContainsKey($s)) { $failColStrs += "$($assertFailVals[$s])" } else { $failColStrs += "-" }
}
$failVerdict = if ($anyFail) { "CRITICAL" } else { "OK" }
$failColStr = $failColStrs -join "|"
$failVals = @($assertFailVals.Values)
$failRange = if ($failVals.Count -ge 2) { ($failVals | Measure-Object -Maximum).Maximum - ($failVals | Measure-Object -Minimum).Minimum } else { 0 }
Write-Host "SEED_VARIANCE|assert_fail|$failColStr|$failRange|$failVerdict"

# Goal scores — one row per goal metric
$allGoalKeys = @{}
foreach ($s in $Seeds) {
    if ($perSeedGoals.ContainsKey($s)) {
        foreach ($k in $perSeedGoals[$s].Keys) { $allGoalKeys[$k] = $true }
    }
}
foreach ($gk in ($allGoalKeys.Keys | Sort-Object)) {
    $gVals = @{}
    foreach ($s in $Seeds) {
        if ($perSeedGoals.ContainsKey($s) -and $perSeedGoals[$s].ContainsKey($gk)) {
            $gVals[$s] = $perSeedGoals[$s][$gk]
        }
    }
    Write-VarianceRow -MetricName "goal_$gk" -SeedValues $gVals -SeedList $Seeds -HighVarianceThreshold 2
}

# Flag counts per seed
$flagCountVals = @{}
foreach ($s in $Seeds) {
    if ($perSeedFlags.ContainsKey($s)) { $flagCountVals[$s] = $perSeedFlags[$s].Count }
}
Write-VarianceRow -MetricName "flag_count" -SeedValues $flagCountVals -SeedList $Seeds -HighVarianceThreshold 3

# FPS avg (if available)
$hasFps = $false
foreach ($s in $Seeds) {
    if ($perSeedFps.ContainsKey($s) -and $perSeedFps[$s].Count -gt 0) { $hasFps = $true; break }
}
if ($hasFps) {
    $fpsAvgVals = @{}
    foreach ($s in $Seeds) {
        if ($perSeedFps.ContainsKey($s) -and $perSeedFps[$s].ContainsKey("avg")) {
            $fpsAvgVals[$s] = [double]$perSeedFps[$s]["avg"]
        }
    }
    Write-VarianceRow -MetricName "fps_avg" -SeedValues $fpsAvgVals -SeedList $Seeds -HighVarianceThreshold 15

    $fpsMinVals = @{}
    foreach ($s in $Seeds) {
        if ($perSeedFps.ContainsKey($s) -and $perSeedFps[$s].ContainsKey("min")) {
            $fpsMinVals[$s] = [double]$perSeedFps[$s]["min"]
        }
    }
    Write-VarianceRow -MetricName "fps_min" -SeedValues $fpsMinVals -SeedList $Seeds -HighVarianceThreshold 20
}

# Event counts per seed (if available)
$hasEvents = $false
foreach ($s in $Seeds) {
    if ($perSeedEvents.ContainsKey($s) -and $perSeedEvents[$s].Count -gt 0) { $hasEvents = $true; break }
}
if ($hasEvents) {
    $eventCountVals = @{}
    foreach ($s in $Seeds) {
        if ($perSeedEvents.ContainsKey($s)) { $eventCountVals[$s] = $perSeedEvents[$s].Count }
    }
    Write-VarianceRow -MetricName "event_count" -SeedValues $eventCountVals -SeedList $Seeds -HighVarianceThreshold 5
}

# Dead zone counts per seed (if available)
$hasDeadZones = $false
foreach ($s in $Seeds) {
    if ($perSeedDeadZones.ContainsKey($s) -and $perSeedDeadZones[$s].Count -gt 0) { $hasDeadZones = $true; break }
}
if ($hasDeadZones) {
    $dzCountVals = @{}
    foreach ($s in $Seeds) {
        if ($perSeedDeadZones.ContainsKey($s)) { $dzCountVals[$s] = $perSeedDeadZones[$s].Count }
    }
    Write-VarianceRow -MetricName "dead_zone_count" -SeedValues $dzCountVals -SeedList $Seeds -HighVarianceThreshold 2
    # List unique dead zones
    $allDz = @{}
    foreach ($s in $Seeds) {
        if ($perSeedDeadZones.ContainsKey($s)) {
            foreach ($dz in $perSeedDeadZones[$s]) { $allDz[$dz] = $true }
        }
    }
    if ($allDz.Count -gt 0) {
        Write-Host ""
        Write-Host "  Dead zones observed: $(($allDz.Keys | Sort-Object) -join ', ')"
    }
}

# Unique flags across all seeds
$allFlagSet = @{}
foreach ($s in $Seeds) {
    if ($perSeedFlags.ContainsKey($s)) {
        foreach ($f in $perSeedFlags[$s]) { $allFlagSet[$f] = $true }
    }
}
if ($allFlagSet.Count -gt 0) {
    Write-Host ""
    Write-Host "  Flags observed: $(($allFlagSet.Keys | Sort-Object) -join ', ')"
}

# Variance summary
$highVarCount = 0
$criticalCount = 0
# Re-check goal ranges for HIGH_VARIANCE count
foreach ($gk in ($allGoalKeys.Keys | Sort-Object)) {
    $gvs = @()
    foreach ($s in $Seeds) {
        if ($perSeedGoals.ContainsKey($s) -and $perSeedGoals[$s].ContainsKey($gk)) {
            $gvs += [double]$perSeedGoals[$s][$gk]
        }
    }
    if ($gvs.Count -ge 2) {
        $gr = ($gvs | Measure-Object -Maximum).Maximum - ($gvs | Measure-Object -Minimum).Minimum
        if ($gr -gt 2) { $highVarCount++ }
    }
}
if ($anyFail) { $criticalCount++ }

$varianceVerdict = if ($criticalCount -gt 0) { "CRITICAL" } elseif ($highVarCount -gt 0) { "NEEDS_ATTENTION" } else { "STABLE" }
Write-Host ""
Write-Host "VARIANCE_SUMMARY|high_variance_metrics=$highVarCount|critical=$criticalCount|verdict=$varianceVerdict"
Write-Host ""

if ($totalFailed -gt 0) {
    Write-Host "FAIL - $totalFailed seed(s) failed"
    exit 1
} else {
    Write-Host "PASS - all $($Seeds.Count) seeds passed"
    exit 0
}
