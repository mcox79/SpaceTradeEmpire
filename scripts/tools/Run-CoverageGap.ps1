# Run-CoverageGap.ps1 -Bridge method + UI script coverage analysis.
# Outputs: per-bot breakdown, per-bridge-partial summary, JSON + markdown reports.
param(
    [string]$RepoRoot = (Split-Path $PSScriptRoot -Parent | Split-Path -Parent),
    [string]$OutputDir  # Optional. Default: reports/analytics/
)

$ErrorActionPreference = 'Stop'

if (-not $OutputDir) { $OutputDir = Join-Path $RepoRoot 'reports/analytics' }
if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null }

# ── 1. Extract all public V0/V1 methods from SimBridge partials ──
$bridgeDir = Join-Path $RepoRoot 'scripts/bridge'
$bridgeFiles = Get-ChildItem -Path $bridgeDir -Filter '*.cs' -File
$bridgeMethods = [ordered]@{}  # method → source file
foreach ($f in $bridgeFiles) {
    $content = Get-Content $f.FullName -Raw
    $matches = [regex]::Matches($content, 'public\s+\S+\s+(\w+V\d+)\s*\(')
    foreach ($m in $matches) {
        $name = $m.Groups[1].Value
        if (-not $bridgeMethods.Contains($name)) {
            $bridgeMethods[$name] = $f.Name
        }
    }
}
$totalMethods = $bridgeMethods.Count

# ── 2. Per-bot method call analysis ──
$botScripts = @{
    'playthrough'      = 'scripts/tests/playthrough_bot_v0.gd'
    'visual_sweep'     = 'scripts/tests/visual_sweep_bot_v0.gd'
    'first_hour'       = 'scripts/tests/test_first_hour_proof_v0.gd'
    'deep_systems'     = 'scripts/tests/test_deep_systems_v0.gd'
    'tutorial'         = 'scripts/tests/test_tutorial_proof_v0.gd'
    'experience'       = 'scripts/tests/test_fh_experience_v0.gd'
    'eval_economy'     = 'scripts/tests/test_economy_health_eval_v0.gd'
    'eval_narrative'   = 'scripts/tests/test_narrative_pacing_eval_v0.gd'
    'eval_flight'      = 'scripts/tests/test_flight_feel_eval_v0.gd'
    'eval_dread'       = 'scripts/tests/test_dread_pacing_eval_v0.gd'
    'eval_audio'       = 'scripts/tests/test_audio_atmosphere_eval_v0.gd'
    'eval_automation'  = 'scripts/tests/test_automation_eval_v0.gd'
}
# Also scan shared bot libraries
$botLibs = @(
    'scripts/tools/bot_assert.gd',
    'scripts/tests/experience_observer.gd',
    'scripts/tests/screenshot_capture.gd'
)

$perBotMethods = @{}  # bot_name → Set of method names
$allBotMethods = @{}  # method → list of bot names

foreach ($botName in $botScripts.Keys) {
    $botPath = Join-Path $RepoRoot $botScripts[$botName]
    if (-not (Test-Path $botPath)) { continue }
    $botContent = Get-Content $botPath -Raw
    $perBotMethods[$botName] = @{}
    # Match both quoted strings "MethodV0" and direct references MethodV0
    $callMatches = [regex]::Matches($botContent, '[\"\.](\w+V\d+)[\"(\s]')
    foreach ($cm in $callMatches) {
        $mName = $cm.Groups[1].Value
        if ($bridgeMethods.Contains($mName)) {
            $perBotMethods[$botName][$mName] = $true
            if (-not $allBotMethods.ContainsKey($mName)) { $allBotMethods[$mName] = @() }
            if ($botName -notin $allBotMethods[$mName]) { $allBotMethods[$mName] += $botName }
        }
    }
    # Also plain string match for methods that appear without quotes (e.g., in comments or call())
    foreach ($method in $bridgeMethods.Keys) {
        if ($botContent -match [regex]::Escape($method)) {
            if (-not $perBotMethods[$botName].ContainsKey($method)) {
                $perBotMethods[$botName][$method] = $true
                if (-not $allBotMethods.ContainsKey($method)) { $allBotMethods[$method] = @() }
                if ($botName -notin $allBotMethods[$method]) { $allBotMethods[$method] += $botName }
            }
        }
    }
}

# Scan bot library files too (attribute to "shared_lib")
foreach ($libRel in $botLibs) {
    $libPath = Join-Path $RepoRoot $libRel
    if (-not (Test-Path $libPath)) { continue }
    $libContent = Get-Content $libPath -Raw
    foreach ($method in $bridgeMethods.Keys) {
        if ($libContent -match [regex]::Escape($method)) {
            if (-not $allBotMethods.ContainsKey($method)) { $allBotMethods[$method] = @() }
            if ("shared_lib" -notin $allBotMethods[$method]) { $allBotMethods[$method] += "shared_lib" }
        }
    }
}

# ── 3. UI/view/core/vfx/audio call analysis ──
$uiDirs = @(
    @{ Path = 'scripts/ui';    Filter = '*.gd'; Label = 'ui_gd' },
    @{ Path = 'scripts/ui';    Filter = '*.cs'; Label = 'ui_cs' },
    @{ Path = 'scripts/view';  Filter = '*.cs'; Label = 'view_cs' },
    @{ Path = 'scripts/view';  Filter = '*.gd'; Label = 'view_gd' },
    @{ Path = 'scripts/core';  Filter = '*.gd'; Label = 'core_gd' },
    @{ Path = 'scripts/core';  Filter = '*.cs'; Label = 'core_cs' },
    @{ Path = 'scripts/vfx';   Filter = '*.gd'; Label = 'vfx_gd' },
    @{ Path = 'scripts/audio'; Filter = '*.gd'; Label = 'audio_gd' },
    @{ Path = 'scripts/camera'; Filter = '*.gd'; Label = 'camera_gd' }
)

$uiCalls = @{}       # method → $true
$uiCallSources = @{} # method → list of source labels

foreach ($dir in $uiDirs) {
    $dirPath = Join-Path $RepoRoot $dir.Path
    if (-not (Test-Path $dirPath)) { continue }
    $files = Get-ChildItem -Path $dirPath -Filter $dir.Filter -File -ErrorAction SilentlyContinue
    foreach ($f in $files) {
        $content = Get-Content $f.FullName -Raw
        foreach ($method in $bridgeMethods.Keys) {
            # Match quoted strings "MethodV0" (GDScript call pattern)
            if ($content -match """$([regex]::Escape($method))""") {
                $uiCalls[$method] = $true
                if (-not $uiCallSources.ContainsKey($method)) { $uiCallSources[$method] = @() }
                $src = "$($dir.Label):$($f.Name)"
                if ($src -notin $uiCallSources[$method]) { $uiCallSources[$method] += $src }
            }
            # Match direct C# method calls: bridge.MethodV0( or .MethodV0(
            if ($content -match "\.$([regex]::Escape($method))\s*\(") {
                $uiCalls[$method] = $true
                if (-not $uiCallSources.ContainsKey($method)) { $uiCallSources[$method] = @() }
                $src = "$($dir.Label):$($f.Name)"
                if ($src -notin $uiCallSources[$method]) { $uiCallSources[$method] += $src }
            }
        }
    }
}

# ── 4. Classify each bridge method ──
$exercised = @()   # bot-tested
$uiOnly    = @()   # called by UI but no bot
$uncalled  = @()   # neither bot nor UI

foreach ($method in ($bridgeMethods.Keys | Sort-Object)) {
    $inBot = $allBotMethods.ContainsKey($method)
    $inUi  = $uiCalls.ContainsKey($method)
    $source = $bridgeMethods[$method]
    $bots = if ($inBot) { ($allBotMethods[$method] | Sort-Object) -join ", " } else { "" }
    $uiSrcs = if ($inUi) { ($uiCallSources[$method] | Sort-Object) -join ", " } else { "" }

    $entry = [PSCustomObject]@{
        Method    = $method
        Source    = $source
        Bots      = $bots
        UiSources = $uiSrcs
        Status    = ""
    }

    if ($inBot) {
        $entry.Status = "EXERCISED"
        $exercised += $entry
    } elseif ($inUi) {
        $entry.Status = "UI_ONLY"
        $uiOnly += $entry
    } else {
        $entry.Status = "UNCALLED"
        $uncalled += $entry
    }
}

# ── 4b. Priority tier classification for untested methods ──
# CRITICAL: Methods called by UI but no bot exercises them (all UI-called, no bot)
# HIGH:     Methods with >3 UI call sites but no bot coverage
# MEDIUM:   Methods with 1-2 UI call sites, no bot
# LOW:      Methods with 0 UI calls and 0 bot calls (potentially dead)
$priorityTiers = @{
    CRITICAL = @()
    HIGH     = @()
    MEDIUM   = @()
    LOW      = @()
}

foreach ($method in ($bridgeMethods.Keys | Sort-Object)) {
    $inBot = $allBotMethods.ContainsKey($method)
    if ($inBot) { continue }

    $inUi = $uiCalls.ContainsKey($method)
    $source = $bridgeMethods[$method]
    $uiCallCount = 0
    if ($uiCallSources.ContainsKey($method)) {
        $uiCallCount = $uiCallSources[$method].Count
    }

    $tierEntry = [PSCustomObject]@{
        Method      = $method
        Source      = $source
        UiCallCount = $uiCallCount
        UiSources   = if ($uiCallSources.ContainsKey($method)) { ($uiCallSources[$method] | Sort-Object) -join ", " } else { "" }
        Tier        = ""
    }

    if ($inUi -and $uiCallCount -gt 3) {
        $tierEntry.Tier = "HIGH"
        $priorityTiers.HIGH += $tierEntry
    } elseif ($inUi) {
        $tierEntry.Tier = "MEDIUM"
        $priorityTiers.MEDIUM += $tierEntry
    } else {
        $tierEntry.Tier = "LOW"
        $priorityTiers.LOW += $tierEntry
    }
}

# CRITICAL = union of HIGH + MEDIUM (any UI-called method with no bot)
$priorityTiers.CRITICAL = @($priorityTiers.HIGH) + @($priorityTiers.MEDIUM)

# ── 5. Per-bridge-partial summary ──
$partialSummary = [ordered]@{}
foreach ($method in $bridgeMethods.Keys) {
    $src = $bridgeMethods[$method]
    if (-not $partialSummary.Contains($src)) {
        $partialSummary[$src] = @{ Total = 0; Exercised = 0; UiOnly = 0; Uncalled = 0 }
    }
    $partialSummary[$src].Total++
    if ($allBotMethods.ContainsKey($method)) { $partialSummary[$src].Exercised++ }
    elseif ($uiCalls.ContainsKey($method)) { $partialSummary[$src].UiOnly++ }
    else { $partialSummary[$src].Uncalled++ }
}

# ── 6. Per-bot summary ──
$perBotSummary = [ordered]@{}
foreach ($botName in ($perBotMethods.Keys | Sort-Object)) {
    $perBotSummary[$botName] = $perBotMethods[$botName].Count
}

# ── 7. UI script coverage ──
$uiDir = Join-Path $RepoRoot 'scripts/ui'
$uiScripts = Get-ChildItem -Path $uiDir -Filter '*.gd' -File -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name

# Collect all bot + bot-lib file content for UI script name matching
$allBotContent = ""
$botDir = Join-Path $RepoRoot 'scripts/tests'
$botFiles = Get-ChildItem -Path $botDir -Filter '*.gd' -File -ErrorAction SilentlyContinue
foreach ($f in $botFiles) { $allBotContent += Get-Content $f.FullName -Raw }
foreach ($libRel in $botLibs) {
    $libPath = Join-Path $RepoRoot $libRel
    if (Test-Path $libPath) { $allBotContent += Get-Content $libPath -Raw }
}

# Classify UI scripts by first-hour criticality
$criticalUiScripts = @(
    'hud', 'hero_trade_menu', 'combat_hud', 'galaxy_intro_overlay',
    'fo_selection_overlay', 'intro_sequence', 'loss_screen', 'victory_screen',
    'warp_transit_hud', 'scanner_hud_panel', 'market_ui', 'station_menu'
)

$uiTestedScripts = @()
$uiUntestedCritical = @()
$uiUntestedOther = @()
foreach ($ui in ($uiScripts | Sort-Object)) {
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($ui)
    $referenced = $allBotContent -match [regex]::Escape($baseName)
    if ($referenced) {
        $uiTestedScripts += $ui
    } else {
        $isCritical = $criticalUiScripts -contains $baseName
        if ($isCritical) {
            $uiUntestedCritical += $ui
        } else {
            $uiUntestedOther += $ui
        }
    }
}

# ── 8. Historical delta (compare with previous report) ──
$prevReportPath = Join-Path $OutputDir 'coverage_report.json'
$prevData = $null
$delta = $null
if (Test-Path $prevReportPath) {
    try {
        $prevData = Get-Content $prevReportPath -Raw | ConvertFrom-Json
        $delta = @{
            prev_total       = $prevData.total_methods
            prev_exercised   = $prevData.exercised
            prev_pct         = $prevData.coverage_pct
            curr_total       = $totalMethods
            curr_exercised   = $exercised.Count
            curr_pct         = 0
            delta_methods    = $totalMethods - $prevData.total_methods
            delta_exercised  = $exercised.Count - $prevData.exercised
        }
    } catch { $delta = $null }
}

$exercisedPct = if ($totalMethods -gt 0) { [math]::Round(($exercised.Count / $totalMethods) * 100, 1) } else { 0 }
if ($delta) { $delta.curr_pct = $exercisedPct; $delta.delta_pct = [math]::Round($exercisedPct - $delta.prev_pct, 1) }

# ── 9. Console output ──
Write-Host "=== Bridge Method Coverage ===" -ForegroundColor Cyan
Write-Host "Total bridge methods: $totalMethods"
Write-Host "EXERCISED (bot-tested): $($exercised.Count) ($exercisedPct%)" -ForegroundColor Green
Write-Host "UI_ONLY (no bot test):  $($uiOnly.Count)" -ForegroundColor Yellow
Write-Host "UNCALLED (dead/unwired): $($uncalled.Count)" -ForegroundColor Red
Write-Host ""

Write-Host "--- Per-Bridge Partial ---" -ForegroundColor Cyan
foreach ($partial in $partialSummary.Keys) {
    $s = $partialSummary[$partial]
    $pct = if ($s.Total -gt 0) { [math]::Round(($s.Exercised / $s.Total) * 100, 0) } else { 0 }
    $bar = "[$("*" * $s.Exercised)$("." * ($s.Total - $s.Exercised))]"
    Write-Host ("  {0,-35} {1,2}/{2,-2} ({3,3}%) {4}" -f $partial, $s.Exercised, $s.Total, $pct, $bar)
}
Write-Host ""

Write-Host "--- Per-Bot Methods Exercised ---" -ForegroundColor Cyan
foreach ($botName in $perBotSummary.Keys) {
    Write-Host ("  {0,-20} {1} methods" -f $botName, $perBotSummary[$botName])
}
Write-Host ""

if ($uiOnly.Count -gt 0) {
    Write-Host "--- UI_ONLY Methods (need bot coverage) ---" -ForegroundColor Yellow
    foreach ($m in $uiOnly) {
        Write-Host "  $($m.Method)  ($($m.Source))  → $($m.UiSources)"
    }
    Write-Host ""
}

if ($uncalled.Count -gt 0) {
    Write-Host "--- UNCALLED Methods (potentially dead) ---" -ForegroundColor Red
    foreach ($m in $uncalled) {
        Write-Host "  $($m.Method)  ($($m.Source))"
    }
    Write-Host ""
}

Write-Host "=== UI Script Coverage ===" -ForegroundColor Cyan
Write-Host "Total UI scripts: $($uiScripts.Count)"
Write-Host "Referenced by bots: $($uiTestedScripts.Count)"
Write-Host "Untested CRITICAL: $($uiUntestedCritical.Count)" -ForegroundColor Red
Write-Host "Untested other: $($uiUntestedOther.Count)" -ForegroundColor Yellow

if ($uiUntestedCritical.Count -gt 0) {
    Write-Host ""
    Write-Host "--- Untested CRITICAL UI Scripts ---" -ForegroundColor Red
    foreach ($u in $uiUntestedCritical) { Write-Host "  $u" }
}

# ── Priority Tier Console Output ──
Write-Host ""
Write-Host "=== Priority Tiers (untested methods) ===" -ForegroundColor Cyan
Write-Host "CRITICAL (UI-called, no bot): $($priorityTiers.CRITICAL.Count)" -ForegroundColor Red
Write-Host "  HIGH (>3 UI call sites):    $($priorityTiers.HIGH.Count)" -ForegroundColor Red
Write-Host "  MEDIUM (1-3 UI call sites): $($priorityTiers.MEDIUM.Count)" -ForegroundColor Yellow
Write-Host "  LOW (0 UI, 0 bot - dead?):    $($priorityTiers.LOW.Count)" -ForegroundColor DarkGray

if ($priorityTiers.HIGH.Count -gt 0) {
    Write-Host ""
    Write-Host "--- HIGH Priority (>3 UI sites, no bot) ---" -ForegroundColor Red
    foreach ($t in $priorityTiers.HIGH) {
        Write-Host ("  {0,-40} {1} sites  ({2})  {3}" -f $t.Method, $t.UiCallCount, $t.Source, $t.UiSources)
    }
}

if ($priorityTiers.MEDIUM.Count -gt 0) {
    Write-Host ""
    Write-Host "--- MEDIUM Priority (1-3 UI sites, no bot) ---" -ForegroundColor Yellow
    foreach ($t in $priorityTiers.MEDIUM) {
        Write-Host ("  {0,-40} {1} sites  ({2})  {3}" -f $t.Method, $t.UiCallCount, $t.Source, $t.UiSources)
    }
}

if ($priorityTiers.LOW.Count -gt 0) {
    Write-Host ""
    Write-Host "--- LOW Priority (0 UI, 0 bot - potentially dead) ---" -ForegroundColor DarkGray
    foreach ($t in $priorityTiers.LOW) {
        Write-Host ("  {0,-40} ({1})" -f $t.Method, $t.Source)
    }
}

Write-Host ""
Write-Host "PRIORITY|CRITICAL=$($priorityTiers.CRITICAL.Count)|HIGH=$($priorityTiers.HIGH.Count)|MEDIUM=$($priorityTiers.MEDIUM.Count)|LOW=$($priorityTiers.LOW.Count)"

if ($delta) {
    Write-Host ""
    Write-Host "=== Delta vs Previous Run ===" -ForegroundColor Cyan
    $sign = if ($delta.delta_pct -ge 0) { "+" } else { "" }
    Write-Host "  Methods: $($delta.prev_total) → $($delta.curr_total) ($($sign)$($delta.delta_methods))"
    Write-Host "  Exercised: $($delta.prev_exercised) → $($delta.curr_exercised) ($($sign)$($delta.delta_exercised))"
    Write-Host "  Coverage: $($delta.prev_pct)% → $($delta.curr_pct)% ($($sign)$($delta.delta_pct)%)"
}

# Structured machine output
Write-Host ""
Write-Host "COVERAGE|TOTAL=$totalMethods|EXERCISED=$($exercised.Count)|PCT=$exercisedPct|UI_ONLY=$($uiOnly.Count)|UNCALLED=$($uncalled.Count)"

# ── 10. Write JSON report ──
$jsonReport = @{
    timestamp       = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
    total_methods   = $totalMethods
    exercised       = $exercised.Count
    ui_only         = $uiOnly.Count
    uncalled_count  = $uncalled.Count
    coverage_pct    = $exercisedPct
    per_partial     = @{}
    per_bot         = @{}
    methods         = @{
        exercised = @($exercised | ForEach-Object { @{ method = $_.Method; source = $_.Source; bots = $_.Bots } })
        ui_only   = @($uiOnly   | ForEach-Object { @{ method = $_.Method; source = $_.Source; ui_sources = $_.UiSources } })
        uncalled  = @($uncalled  | ForEach-Object { @{ method = $_.Method; source = $_.Source } })
    }
    ui_scripts      = @{
        total             = $uiScripts.Count
        tested            = $uiTestedScripts.Count
        untested_critical = @($uiUntestedCritical)
        untested_other    = @($uiUntestedOther)
    }
    priority_tiers  = @{
        critical_count = $priorityTiers.CRITICAL.Count
        high_count     = $priorityTiers.HIGH.Count
        medium_count   = $priorityTiers.MEDIUM.Count
        low_count      = $priorityTiers.LOW.Count
        high           = @($priorityTiers.HIGH | ForEach-Object { @{ method = $_.Method; source = $_.Source; ui_call_count = $_.UiCallCount; ui_sources = $_.UiSources } })
        medium         = @($priorityTiers.MEDIUM | ForEach-Object { @{ method = $_.Method; source = $_.Source; ui_call_count = $_.UiCallCount; ui_sources = $_.UiSources } })
        low            = @($priorityTiers.LOW | ForEach-Object { @{ method = $_.Method; source = $_.Source } })
    }
}
if ($delta) { $jsonReport.delta = $delta }

foreach ($p in $partialSummary.Keys) {
    $s = $partialSummary[$p]
    $jsonReport.per_partial[$p] = @{ total = $s.Total; exercised = $s.Exercised; ui_only = $s.UiOnly; uncalled = $s.Uncalled }
}
foreach ($b in $perBotSummary.Keys) {
    $jsonReport.per_bot[$b] = $perBotSummary[$b]
}

# ── 11. Write markdown report ──
$md = @()
$md += "# Bridge Method & UI Coverage Report"
$md += "**Generated**: $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
$md += ""
$md += "## Summary"
$md += "| Metric | Value |"
$md += "|--------|-------|"
$md += "| Total bridge methods | $totalMethods |"
$md += "| EXERCISED (bot-tested) | $($exercised.Count) ($exercisedPct%) |"
$md += "| UI_ONLY (no bot test) | $($uiOnly.Count) |"
$md += "| UNCALLED (dead/unwired) | $($uncalled.Count) |"
$md += "| UI scripts total | $($uiScripts.Count) |"
$md += "| UI scripts tested | $($uiTestedScripts.Count) |"
$md += "| UI scripts untested (critical) | $($uiUntestedCritical.Count) |"
$md += ""

if ($delta) {
    $sign = if ($delta.delta_pct -ge 0) { "+" } else { "" }
    $md += "## Delta vs Previous Run"
    $md += "| Metric | Before | After | Delta |"
    $md += "|--------|--------|-------|-------|"
    $md += "| Methods | $($delta.prev_total) | $($delta.curr_total) | $($sign)$($delta.delta_methods) |"
    $md += "| Exercised | $($delta.prev_exercised) | $($delta.curr_exercised) | $($sign)$($delta.delta_exercised) |"
    $md += "| Coverage | $($delta.prev_pct)% | $($delta.curr_pct)% | $($sign)$($delta.delta_pct)% |"
    $md += ""
}

$md += "## Per-Bridge Partial"
$md += "| Partial | Exercised | UI Only | Uncalled | Total | Coverage |"
$md += "|---------|-----------|---------|----------|-------|----------|"
foreach ($p in $partialSummary.Keys) {
    $s = $partialSummary[$p]
    $pct = if ($s.Total -gt 0) { [math]::Round(($s.Exercised / $s.Total) * 100, 0) } else { 0 }
    $md += "| $p | $($s.Exercised) | $($s.UiOnly) | $($s.Uncalled) | $($s.Total) | $pct% |"
}
$md += ""

$md += "## Per-Bot Coverage"
$md += "| Bot | Methods Exercised |"
$md += "|-----|------------------|"
foreach ($b in $perBotSummary.Keys) {
    $md += "| $b | $($perBotSummary[$b]) |"
}
$md += ""

$md += "## EXERCISED Methods ($($exercised.Count))"
$md += "| Method | Source | Tested By |"
$md += "|--------|--------|-----------|"
foreach ($m in $exercised) {
    $md += "| ``$($m.Method)`` | $($m.Source) | $($m.Bots) |"
}
$md += ""

if ($uiOnly.Count -gt 0) {
    $md += "## UI_ONLY Methods -Need Bot Coverage ($($uiOnly.Count))"
    $md += "| Method | Source | Called By |"
    $md += "|--------|--------|----------|"
    foreach ($m in $uiOnly) {
        $md += "| ``$($m.Method)`` | $($m.Source) | $($m.UiSources) |"
    }
    $md += ""
}

if ($uncalled.Count -gt 0) {
    $md += "## UNCALLED Methods -Potentially Dead ($($uncalled.Count))"
    $md += "| Method | Source |"
    $md += "|--------|--------|"
    foreach ($m in $uncalled) {
        $md += "| ``$($m.Method)`` | $($m.Source) |"
    }
    $md += ""
}

$md += "## Priority Tiers (untested methods)"
$md += "| Tier | Count | Description |"
$md += "|------|-------|-------------|"
$md += "| CRITICAL (all UI-called) | $($priorityTiers.CRITICAL.Count) | Called by UI but no bot exercises them |"
$md += "| HIGH | $($priorityTiers.HIGH.Count) | >3 UI call sites, no bot coverage |"
$md += "| MEDIUM | $($priorityTiers.MEDIUM.Count) | 1-3 UI call sites, no bot coverage |"
$md += "| LOW | $($priorityTiers.LOW.Count) | 0 UI calls, 0 bot calls (potentially dead) |"
$md += ""

if ($priorityTiers.HIGH.Count -gt 0) {
    $md += "### HIGH Priority ($($priorityTiers.HIGH.Count))"
    $md += "| Method | Source | UI Sites | Called By |"
    $md += "|--------|--------|----------|----------|"
    foreach ($t in $priorityTiers.HIGH) {
        $md += "| ``$($t.Method)`` | $($t.Source) | $($t.UiCallCount) | $($t.UiSources) |"
    }
    $md += ""
}

if ($priorityTiers.MEDIUM.Count -gt 0) {
    $md += "### MEDIUM Priority ($($priorityTiers.MEDIUM.Count))"
    $md += "| Method | Source | UI Sites | Called By |"
    $md += "|--------|--------|----------|----------|"
    foreach ($t in $priorityTiers.MEDIUM) {
        $md += "| ``$($t.Method)`` | $($t.Source) | $($t.UiCallCount) | $($t.UiSources) |"
    }
    $md += ""
}

if ($priorityTiers.LOW.Count -gt 0) {
    $md += "### LOW Priority ($($priorityTiers.LOW.Count))"
    $md += "| Method | Source |"
    $md += "|--------|--------|"
    foreach ($t in $priorityTiers.LOW) {
        $md += "| ``$($t.Method)`` | $($t.Source) |"
    }
    $md += ""
}

if ($uiUntestedCritical.Count -gt 0) {
    $md += "## Untested CRITICAL UI Scripts"
    $md += "These first-hour-relevant UI scripts are not exercised by any bot:"
    $md += ""
    foreach ($u in $uiUntestedCritical) { $md += "- ``$u``" }
    $md += ""
}

if ($uiUntestedOther.Count -gt 0) {
    $md += "## Untested Other UI Scripts ($($uiUntestedOther.Count))"
    foreach ($u in $uiUntestedOther) { $md += "- ``$u``" }
    $md += ""
}

# ── 12. Program Type Coverage ──
$programTypes = [ordered]@{
    'TradeCharter'       = 'CreateTradeCharterProgram'
    'AutoBuy'            = 'CreateAutoBuyProgram'
    'AutoSell'           = 'CreateAutoSellProgram'
    'ResourceTap'        = 'CreateResourceTapProgram'
    'Expedition'         = 'CreateExpeditionProgram'
    'Escort'             = 'CreateEscortProgramV0'
    'Patrol'             = 'CreatePatrolProgramV0'
    'Survey'             = 'CreateSurveyProgramV0'
    'FractureExtraction' = 'CreateFractureExtractionProgram'
    'ConstrCapModule'    = 'CreateConstrCapModuleProgram'
}

$botGdFiles = Get-ChildItem -Path (Join-Path $RepoRoot 'scripts/tests') -Filter '*.gd' -File -ErrorAction SilentlyContinue
$programCoverage = @()
$programExercisedCount = 0

foreach ($pType in $programTypes.Keys) {
    $createMethod = $programTypes[$pType]
    $exercisedBy = @()
    foreach ($gd in $botGdFiles) {
        $gdContent = Get-Content $gd.FullName -Raw
        if ($gdContent -match [regex]::Escape($createMethod)) {
            $exercisedBy += $gd.Name
        }
    }
    $status = if ($exercisedBy.Count -gt 0) { "EXERCISED" } else { "GAP" }
    if ($exercisedBy.Count -gt 0) { $programExercisedCount++ }
    $programCoverage += [PSCustomObject]@{
        Type           = $pType
        CreateMethod   = $createMethod
        ExercisedBy    = ($exercisedBy -join ", ")
        Status         = $status
    }
    Write-Host ("PROGRAM_COVERAGE|{0}|{1}|{2}" -f $pType, $createMethod, (($exercisedBy -join ", ") + ""))
}

$programGapCount = $programTypes.Count - $programExercisedCount
Write-Host ""
Write-Host "=== Program Type Coverage ===" -ForegroundColor Cyan
Write-Host "Exercised: $programExercisedCount / $($programTypes.Count)" -ForegroundColor $(if ($programExercisedCount -eq $programTypes.Count) { "Green" } else { "Yellow" })
Write-Host "Gaps: $programGapCount" -ForegroundColor $(if ($programGapCount -eq 0) { "Green" } else { "Red" })
Write-Host ""
foreach ($pc in $programCoverage) {
    $color = if ($pc.Status -eq "EXERCISED") { "Green" } else { "Red" }
    $bots = if ($pc.ExercisedBy) { $pc.ExercisedBy } else { "(none)" }
    Write-Host ("  {0,-20} {1,-40} {2,-10} {3}" -f $pc.Type, $pc.CreateMethod, $pc.Status, $bots) -ForegroundColor $color
}
Write-Host ""
Write-Host "PROGRAM_COVERAGE_SUMMARY|exercised=$programExercisedCount/$($programTypes.Count)|gap=$programGapCount"

# ── 13. Action Lifecycle Coverage ──
$lifecycleMethods = [ordered]@{
    'StartProgram'              = 'Start'
    'PauseProgram'              = 'Pause'
    'CancelProgram'             = 'Cancel'
    'GetProgramPostmortemV0'    = 'Postmortem'
    'GetProgramPerformanceV0'   = 'Performance'
}

$lifecycleCoverage = @()
$lifecycleExercisedCount = 0

foreach ($lcMethod in $lifecycleMethods.Keys) {
    $lcLabel = $lifecycleMethods[$lcMethod]
    $exercisedBy = @()
    foreach ($gd in $botGdFiles) {
        $gdContent = Get-Content $gd.FullName -Raw
        if ($gdContent -match [regex]::Escape($lcMethod)) {
            $exercisedBy += $gd.Name
        }
    }
    $status = if ($exercisedBy.Count -gt 0) { "EXERCISED" } else { "GAP" }
    if ($exercisedBy.Count -gt 0) { $lifecycleExercisedCount++ }
    $lifecycleCoverage += [PSCustomObject]@{
        Method      = $lcMethod
        Label       = $lcLabel
        ExercisedBy = ($exercisedBy -join ", ")
        Status      = $status
    }
}

$lifecycleGapCount = $lifecycleMethods.Count - $lifecycleExercisedCount
Write-Host ""
Write-Host "=== Action Lifecycle Coverage ===" -ForegroundColor Cyan
Write-Host "Exercised: $lifecycleExercisedCount / $($lifecycleMethods.Count)" -ForegroundColor $(if ($lifecycleExercisedCount -eq $lifecycleMethods.Count) { "Green" } else { "Yellow" })
Write-Host "Gaps: $lifecycleGapCount" -ForegroundColor $(if ($lifecycleGapCount -eq 0) { "Green" } else { "Red" })
Write-Host ""
foreach ($lc in $lifecycleCoverage) {
    $color = if ($lc.Status -eq "EXERCISED") { "Green" } else { "Red" }
    $bots = if ($lc.ExercisedBy) { $lc.ExercisedBy } else { "(none)" }
    Write-Host ("  {0,-35} {1,-15} {2,-10} {3}" -f $lc.Method, $lc.Label, $lc.Status, $bots) -ForegroundColor $color
}
Write-Host ""
Write-Host "LIFECYCLE_COVERAGE_SUMMARY|exercised=$lifecycleExercisedCount/$($lifecycleMethods.Count)|gap=$lifecycleGapCount"

# Add to JSON report
$jsonReport.program_coverage = @{
    total           = $programTypes.Count
    exercised       = $programExercisedCount
    gap             = $programGapCount
    types           = @($programCoverage | ForEach-Object { @{ type = $_.Type; create_method = $_.CreateMethod; exercised_by = $_.ExercisedBy; status = $_.Status } })
}
$jsonReport.lifecycle_coverage = @{
    total           = $lifecycleMethods.Count
    exercised       = $lifecycleExercisedCount
    gap             = $lifecycleGapCount
    methods         = @($lifecycleCoverage | ForEach-Object { @{ method = $_.Method; label = $_.Label; exercised_by = $_.ExercisedBy; status = $_.Status } })
}

# Add to markdown report
$md += "## Program Type Coverage"
$md += "| Type | Creation Method | Status | Exercised By |"
$md += "|------|-----------------|--------|-------------|"
foreach ($pc in $programCoverage) {
    $bots = if ($pc.ExercisedBy) { $pc.ExercisedBy } else { "*(none)*" }
    $md += "| $($pc.Type) | ``$($pc.CreateMethod)`` | $($pc.Status) | $bots |"
}
$md += ""
$md += "**Summary**: $programExercisedCount / $($programTypes.Count) program types exercised, $programGapCount gaps"
$md += ""

$md += "## Action Lifecycle Coverage"
$md += "| Method | Label | Status | Exercised By |"
$md += "|--------|-------|--------|-------------|"
foreach ($lc in $lifecycleCoverage) {
    $bots = if ($lc.ExercisedBy) { $lc.ExercisedBy } else { "*(none)*" }
    $md += "| ``$($lc.Method)`` | $($lc.Label) | $($lc.Status) | $bots |"
}
$md += ""
$md += "**Summary**: $lifecycleExercisedCount / $($lifecycleMethods.Count) lifecycle methods exercised, $lifecycleGapCount gaps"
$md += ""

# Rewrite JSON with new sections included
$jsonPath = Join-Path $OutputDir "coverage_report.json"
$jsonReport | ConvertTo-Json -Depth 6 | Set-Content $jsonPath -Encoding UTF8

$mdPath = Join-Path $OutputDir "coverage_report.md"
($md -join "`n") | Set-Content $mdPath -Encoding UTF8
Write-Host "JSON report: $jsonPath"
Write-Host "Markdown report: $mdPath"
