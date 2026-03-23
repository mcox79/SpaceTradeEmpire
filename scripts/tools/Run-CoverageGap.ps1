# Run-CoverageGap.ps1 — Pre-compute bridge method coverage for audit Step 2.
# Greps bridge methods, bot calls, and UI calls. Outputs EXERCISED/UI_ONLY/UNCALLED per method.
param(
    [string]$RepoRoot = (Split-Path $PSScriptRoot -Parent | Split-Path -Parent)
)

$ErrorActionPreference = 'Stop'

# 1. Extract all public V0 methods from SimBridge partials.
$bridgeDir = Join-Path $RepoRoot 'scripts/bridge'
$bridgeFiles = Get-ChildItem -Path $bridgeDir -Filter '*.cs' -File
$bridgeMethods = @{}
foreach ($f in $bridgeFiles) {
    $content = Get-Content $f.FullName -Raw
    $matches = [regex]::Matches($content, 'public\s+\S+\s+(\w+V\d+)\s*\(')
    foreach ($m in $matches) {
        $name = $m.Groups[1].Value
        if (-not $bridgeMethods.ContainsKey($name)) {
            $bridgeMethods[$name] = $f.Name
        }
    }
}

# 2. Extract all V0 method calls from bot scripts.
$botDir = Join-Path $RepoRoot 'scripts/tests'
$botFiles = Get-ChildItem -Path $botDir -Filter '*.gd' -File -ErrorAction SilentlyContinue
$botCalls = @{}
foreach ($f in $botFiles) {
    $content = Get-Content $f.FullName -Raw
    $matches = [regex]::Matches($content, '"(\w+V\d+)"')
    foreach ($m in $matches) {
        $name = $m.Groups[1].Value
        $botCalls[$name] = $true
    }
}

# 3. Extract all V0 method calls from UI scripts (GDScript + C#).
$uiDir = Join-Path $RepoRoot 'scripts/ui'
$viewDir = Join-Path $RepoRoot 'scripts/view'
$coreDir = Join-Path $RepoRoot 'scripts/core'
$uiCalls = @{}

# GDScript UI files
$uiFiles = Get-ChildItem -Path $uiDir -Filter '*.gd' -File -ErrorAction SilentlyContinue
foreach ($f in $uiFiles) {
    $content = Get-Content $f.FullName -Raw
    $matches = [regex]::Matches($content, '"(\w+V\d+)"')
    foreach ($m in $matches) {
        $name = $m.Groups[1].Value
        $uiCalls[$name] = $true
    }
}

# C# view files (scripts/view/*.cs)
$viewFiles = Get-ChildItem -Path $viewDir -Filter '*.cs' -File -ErrorAction SilentlyContinue
foreach ($f in $viewFiles) {
    $content = Get-Content $f.FullName -Raw
    $matches = [regex]::Matches($content, '"(\w+V\d+)"')
    foreach ($m in $matches) {
        $name = $m.Groups[1].Value
        $uiCalls[$name] = $true
    }
}

# GDScript core files (game_manager.gd, etc.)
$coreFiles = Get-ChildItem -Path $coreDir -Filter '*.gd' -File -ErrorAction SilentlyContinue
foreach ($f in $coreFiles) {
    $content = Get-Content $f.FullName -Raw
    $matches = [regex]::Matches($content, '"(\w+V\d+)"')
    foreach ($m in $matches) {
        $name = $m.Groups[1].Value
        $uiCalls[$name] = $true
    }
}

# C# UI files (scripts/ui/*.cs — ProgramsMenu, FleetMenu, etc.)
$uiCsFiles = Get-ChildItem -Path $uiDir -Filter '*.cs' -File -ErrorAction SilentlyContinue
foreach ($f in $uiCsFiles) {
    $content = Get-Content $f.FullName -Raw
    $matches = [regex]::Matches($content, '"(\w+V\d+)"')
    foreach ($m in $matches) {
        $name = $m.Groups[1].Value
        $uiCalls[$name] = $true
    }
}

# VFX scripts (may call bridge for dread state etc.)
$vfxDir = Join-Path $RepoRoot 'scripts/vfx'
$vfxFiles = Get-ChildItem -Path $vfxDir -Filter '*.gd' -File -ErrorAction SilentlyContinue
foreach ($f in $vfxFiles) {
    $content = Get-Content $f.FullName -Raw
    $matches = [regex]::Matches($content, '"(\w+V\d+)"')
    foreach ($m in $matches) {
        $name = $m.Groups[1].Value
        $uiCalls[$name] = $true
    }
}

# Audio scripts (scripts/audio/*.gd — may call bridge for dread/music state)
$audioDir = Join-Path $RepoRoot 'scripts/audio'
$audioFiles = Get-ChildItem -Path $audioDir -Filter '*.gd' -File -ErrorAction SilentlyContinue
foreach ($f in $audioFiles) {
    $content = Get-Content $f.FullName -Raw
    $matches = [regex]::Matches($content, '"(\w+V\d+)"')
    foreach ($m in $matches) {
        $name = $m.Groups[1].Value
        $uiCalls[$name] = $true
    }
}

# 4. Classify each bridge method.
$exercised = @()
$uiOnly = @()
$uncalled = @()

foreach ($method in ($bridgeMethods.Keys | Sort-Object)) {
    $inBot = $botCalls.ContainsKey($method)
    $inUi = $uiCalls.ContainsKey($method)
    if ($inBot) {
        $exercised += [PSCustomObject]@{ Method=$method; Source=$bridgeMethods[$method]; BotCovered=$true; UiCovered=$inUi }
    } elseif ($inUi) {
        $uiOnly += [PSCustomObject]@{ Method=$method; Source=$bridgeMethods[$method]; BotCovered=$false; UiCovered=$true }
    } else {
        $uncalled += [PSCustomObject]@{ Method=$method; Source=$bridgeMethods[$method]; BotCovered=$false; UiCovered=$false }
    }
}

$total = $bridgeMethods.Count
$exercisedPct = if ($total -gt 0) { [math]::Round(($exercised.Count / $total) * 100, 1) } else { 0 }

# 5. Output summary.
Write-Host "=== Bridge Method Coverage ==="
Write-Host "Total bridge methods: $total"
Write-Host "EXERCISED (bot-tested): $($exercised.Count) ($exercisedPct%)"
Write-Host "UI_ONLY (no bot test):  $($uiOnly.Count)"
Write-Host "UNCALLED (dead/unwired): $($uncalled.Count)"
Write-Host ""

if ($uiOnly.Count -gt 0) {
    Write-Host "--- UI_ONLY Methods (need bot coverage) ---"
    foreach ($m in $uiOnly) {
        Write-Host "  $($m.Method)  ($($m.Source))"
    }
    Write-Host ""
}

if ($uncalled.Count -gt 0) {
    Write-Host "--- UNCALLED Methods (potentially dead) ---"
    foreach ($m in $uncalled) {
        Write-Host "  $($m.Method)  ($($m.Source))"
    }
    Write-Host ""
}

# 6. Also scan UI scripts for screenshot/bot coverage.
$uiScripts = Get-ChildItem -Path $uiDir -Filter '*.gd' -File | Select-Object -ExpandProperty BaseName
$botContent = ""
foreach ($f in $botFiles) {
    $botContent += Get-Content $f.FullName -Raw
}
$untestedUi = @()
foreach ($ui in ($uiScripts | Sort-Object)) {
    if ($botContent -notmatch [regex]::Escape($ui)) {
        $untestedUi += $ui
    }
}

Write-Host "=== UI Script Coverage ==="
Write-Host "Total UI scripts: $($uiScripts.Count)"
Write-Host "Referenced by bots: $($uiScripts.Count - $untestedUi.Count)"
Write-Host "Not referenced: $($untestedUi.Count)"
if ($untestedUi.Count -gt 0) {
    Write-Host ""
    Write-Host "--- Untested UI Scripts ---"
    foreach ($u in $untestedUi) {
        Write-Host "  $u.gd"
    }
}
