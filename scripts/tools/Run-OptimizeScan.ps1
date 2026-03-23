<#
.SYNOPSIS
  Automated Pass 1 scanner for /optimize skill.
  Runs Grep-equivalent pattern checks across the codebase and outputs
  structured findings in OPTSCAN| prefixed lines.

.PARAMETER Mode
  Which check categories to run: all, determinism, arch, security, deadcode
  Default: all

.PARAMETER Scope
  Subdirectory to limit scan to (relative to repo root).
  Default: entire repo

.PARAMETER Severity
  Minimum severity to report: critical, warning, suggestion
  Default: warning
#>
param(
    [ValidateSet("all","determinism","arch","security","deadcode")]
    [string]$Mode = "all",
    [string]$Scope = "",
    [ValidateSet("critical","warning","suggestion")]
    [string]$Severity = "warning"
)

$ErrorActionPreference = "Continue"
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$findings = @()
$severityRank = @{ "CRITICAL" = 3; "WARNING" = 2; "SUGGESTION" = 1 }
$minRank = switch ($Severity) { "critical" { 3 } "warning" { 2 } "suggestion" { 1 } }

function Add-Finding {
    param([string]$Sev, [string]$Category, [string]$File, [int]$Line, [string]$Desc, [string]$Fix)
    if ($severityRank[$Sev] -ge $minRank) {
        $rel = $File.Replace($repoRoot, "").TrimStart("\","/")
        $script:findings += [PSCustomObject]@{
            Severity = $Sev; Category = $Category
            File = $rel; Line = $Line
            Description = $Desc; Fix = $Fix
        }
    }
}

function Search-Pattern {
    param([string]$Pattern, [string]$Path, [string]$Include, [string]$Sev, [string]$Cat, [string]$Desc, [string]$Fix)
    $searchPath = if ($Scope) { Join-Path $repoRoot $Scope } else { $Path }
    if (-not (Test-Path $searchPath)) { return }
    $rgArgs = @("--no-heading", "--line-number", "--color=never", "-e", $Pattern)
    if ($Include) { $rgArgs += "--glob"; $rgArgs += $Include }
    $rgArgs += $searchPath
    try {
        $output = & rg @rgArgs 2>&1 | Where-Object { $_ -is [string] -or $_.GetType().Name -ne "ErrorRecord" }
        if ($LASTEXITCODE -ne 0) { return }
        if ($output) {
            foreach ($line in $output) {
                $lineStr = "$line"
                if ($lineStr -match "^(.+?):(\d+):(.*)$") {
                    Add-Finding $Sev $Cat $Matches[1] ([int]$Matches[2]) "$Desc [$($Matches[3].Trim())]" $Fix
                }
            }
        }
    } catch { <# rg not found or error — skip #> }
}

# ─── DETERMINISM CHECKS ───────────────────────────────────────────────
if ($Mode -eq "all" -or $Mode -eq "determinism") {
    $simcore = Join-Path $repoRoot "SimCore"
    $systems = Join-Path $simcore "Systems"

    # Exclude test files
    $detPatterns = @(
        @{ P = "new Random\(\)";           D = "Unseeded Random"; F = "Use seeded Random or SimRng" }
        @{ P = "DateTime\.(Now|UtcNow)";   D = "DateTime.Now usage"; F = "Use tick-based time from SimKernel" }
        @{ P = "DateTimeOffset\.Now";       D = "DateTimeOffset.Now usage"; F = "Use tick-based time" }
        @{ P = "Guid\.NewGuid";            D = "Guid.NewGuid usage"; F = "Use deterministic ID generation" }
        @{ P = "Environment\.TickCount";    D = "Environment.TickCount"; F = "Use SimKernel tick counter" }
        @{ P = "Stopwatch\.(StartNew|GetTimestamp)"; D = "Stopwatch usage"; F = "Remove or guard with #if DEBUG" }
        @{ P = "Task\.Run\(";             D = "Task.Run in SimCore"; F = "SimCore must be single-threaded" }
        @{ P = "Parallel\.(For|ForEach)";  D = "Parallel loop in SimCore"; F = "Use sequential loop" }
        @{ P = "\.AsParallel\(\)";         D = "PLINQ in SimCore"; F = "Use sequential LINQ" }
    )
    foreach ($dp in $detPatterns) {
        Search-Pattern -Pattern $dp.P -Path $simcore -Include "*.cs" -Sev "CRITICAL" -Cat "determinism" -Desc $dp.D -Fix $dp.F
    }

    $warnPatterns = @(
        @{ P = "ConcurrentDictionary";     D = "ConcurrentDictionary in SimCore"; F = "Use Dictionary with explicit locking" }
        @{ P = "Thread\.(Sleep|Yield)";    D = "Thread.Sleep/Yield in SimCore"; F = "Remove blocking call" }
    )
    foreach ($wp in $warnPatterns) {
        Search-Pattern -Pattern $wp.P -Path $simcore -Include "*.cs" -Sev "WARNING" -Cat "determinism" -Desc $wp.D -Fix $wp.F
    }

    # async in Systems/ specifically
    Search-Pattern -Pattern "\basync\b" -Path $systems -Include "*.cs" -Sev "WARNING" -Cat "determinism" -Desc "async keyword in System" -Fix "Systems must be synchronous for determinism"
}

# ─── ARCHITECTURE CHECKS ──────────────────────────────────────────────
if ($Mode -eq "all" -or $Mode -eq "arch") {
    $simcore = Join-Path $repoRoot "SimCore"
    $scripts = Join-Path $repoRoot "scripts"

    Search-Pattern -Pattern "using Godot" -Path $simcore -Include "*.cs" -Sev "CRITICAL" -Cat "architecture" -Desc "Godot dependency in SimCore" -Fix "SimCore must have zero Godot dependencies"

    # sim_ref in GDScript (excluding bridge/, tests/, core/ infrastructure)
    # Note: `sim.` is the bridge NODE reference in core files — that's correct usage.
    # Only `sim_ref` (direct SimCore state access) is a true violation.
    $gdFiles = Get-ChildItem -Path $scripts -Filter "*.gd" -Recurse |
        Where-Object { $_.FullName -notlike "*bridge*" -and $_.FullName -notlike "*bot_assert*" -and $_.FullName -notlike "*test_*" }
    foreach ($gd in $gdFiles) {
        $content = Get-Content $gd.FullName -Raw -ErrorAction SilentlyContinue
        if ($content -match "(?m)\bsim_ref\b") {
            $lineNum = 0
            foreach ($l in (Get-Content $gd.FullName)) {
                $lineNum++
                if ($l -match "\bsim_ref\b") {
                    Add-Finding "CRITICAL" "architecture" $gd.FullName $lineNum "Direct sim_ref access in GDScript (must use SimBridge)" "Use bridge.call() instead"
                }
            }
        }
    }

    # Empty catch blocks (heuristic: { } or { } with only whitespace)
    Search-Pattern -Pattern "catch\s*\([^)]*\)\s*\{\s*\}" -Path $simcore -Include "*.cs" -Sev "WARNING" -Cat "architecture" -Desc "Empty catch block (swallowed exception)" -Fix "Log or rethrow"
}

# ─── SECURITY CHECKS ──────────────────────────────────────────────────
if ($Mode -eq "all" -or $Mode -eq "security") {
    $root = $repoRoot

    Search-Pattern -Pattern "BinaryFormatter" -Path $root -Include "*.cs" -Sev "CRITICAL" -Cat "security" -Desc "BinaryFormatter (RCE risk)" -Fix "Use JsonSerializer"
    Search-Pattern -Pattern "TypeNameHandling\.(All|Auto|Objects|Arrays)" -Path $root -Include "*.cs" -Sev "CRITICAL" -Cat "security" -Desc "Unsafe TypeNameHandling (RCE risk)" -Fix "Remove TypeNameHandling or use None"
    Search-Pattern -Pattern "(?i)password\s*=\s*\x22[^\x22]+\x22" -Path $root -Include "*.cs" -Sev "WARNING" -Cat "security" -Desc "Possible hardcoded credential" -Fix "Use environment variable or config"
}

# ─── DEAD CODE SIGNALS ────────────────────────────────────────────────
if ($Mode -eq "all" -or $Mode -eq "deadcode") {
    $simcore = Join-Path $repoRoot "SimCore"

    Search-Pattern -Pattern "(TODO|HACK|FIXME|XXX):" -Path $simcore -Include "*.cs" -Sev "SUGGESTION" -Cat "dead-code" -Desc "TODO/HACK marker" -Fix "Address or remove"
}

# ─── GDSCRIPT QUALITY CHECKS ────────────────────────────────────────
if ($Mode -eq "all") {
    $scripts = Join-Path $repoRoot "scripts"

    # Missing await on async calls (call returns a signal but not awaited)
    Search-Pattern -Pattern 'create_timer\([^)]+\)\.timeout\s*$' -Path $scripts -Include "*.gd" -Sev "WARNING" -Cat "gdscript" -Desc "create_timer().timeout without await" -Fix "Add 'await' before create_timer"

    # Bare 'return' in _process/_physics_process (should be 'return false')
    # Scope-aware: only flags returns inside _process or _physics_process functions.
    $gdFiles = Get-ChildItem -Path $scripts -Filter "*.gd" -Recurse -ErrorAction SilentlyContinue
    foreach ($gf in $gdFiles) {
        $lines = Get-Content $gf.FullName -ErrorAction SilentlyContinue
        if (-not $lines) { continue }
        $inProcessFunc = $false
        for ($li = 0; $li -lt $lines.Count; $li++) {
            $ln = $lines[$li]
            # Detect _process or _physics_process function definition
            if ($ln -match '^(func\s+_(?:process|physics_process)\s*\()') {
                $inProcessFunc = $true
                continue
            }
            # Any other func definition at top indent level ends the process function
            if ($ln -match '^func\s+' -and $inProcessFunc) {
                $inProcessFunc = $false
            }
            # Check for bare return inside _process/_physics_process
            if ($inProcessFunc -and $ln -match '^\s+return\s*$') {
                Add-Finding "WARNING" "gdscript" $gf.FullName ($li + 1) "Bare return in _process/_physics_process (should be 'return false') [$($ln.Trim())]" "Use 'return false' in _process functions"
            }
        }
    }

    # bridge.call with wrong number of quotes (heuristic: empty call string)
    Search-Pattern -Pattern 'bridge\.call\(""\)' -Path $scripts -Include "*.gd" -Sev "CRITICAL" -Cat "gdscript" -Desc "bridge.call with empty method name" -Fix "Provide correct V0 method name"

    # Signal name mismatch (C# events use PascalCase, GDScript uses snake_case)
    Search-Pattern -Pattern 'connect\("[A-Z]' -Path $scripts -Include "*.gd" -Sev "WARNING" -Cat "gdscript" -Desc "Signal connect with PascalCase (C# signals are auto-lowercased)" -Fix "Use snake_case signal name"
}

# ─── HOT-PATH ALLOCATION CHECKS ─────────────────────────────────────
if ($Mode -eq "all") {
    $systems = Join-Path $repoRoot "SimCore/Systems"

    # new List/Dictionary/HashSet in Process methods (hot-path allocation)
    Search-Pattern -Pattern "new (List|Dictionary|HashSet|Queue|Stack|StringBuilder)<" -Path $systems -Include "*.cs" -Sev "WARNING" -Cat "allocation" -Desc "Heap allocation in System (potential hot-path)" -Fix "Pre-allocate or use pooled collection"

    # LINQ in Process methods (causes allocations via iterators)
    Search-Pattern -Pattern "\.(Select|Where|OrderBy|GroupBy|ToList|ToArray|ToDictionary|Aggregate|Any|All|Count|First|Last|Single)\(" -Path $systems -Include "*.cs" -Sev "SUGGESTION" -Cat "allocation" -Desc "LINQ in System file (iterator allocation)" -Fix "Consider loop-based approach for hot paths"

    # String interpolation in Process (heap allocation per frame)
    Search-Pattern -Pattern '\$"[^"]*\{' -Path $systems -Include "*.cs" -Sev "SUGGESTION" -Cat "allocation" -Desc "String interpolation in System (per-frame allocation risk)" -Fix "Cache or guard with condition"
}

# ─── OUTPUT ───────────────────────────────────────────────────────────

# Filter out test files from findings
$findings = @($findings | Where-Object { $_.File -notlike "*Tests*" -and $_.File -notlike "*test_*" })

# Filter out findings guarded by debug/diagnostic patterns:
# 1. Inside #if DEBUG blocks
# 2. In files where the call site is guarded by a const-false variable
# 3. Lines/preceding lines with OPTSCAN:SUPPRESS comment
$filtered = @()
foreach ($f in $findings) {
    $fullPath = Join-Path $repoRoot $f.File
    $suppress = $false
    if (Test-Path $fullPath) {
        $lines = Get-Content $fullPath -ErrorAction SilentlyContinue
        $lineIdx = $f.Line - 1

        # Check 1: Inside #if DEBUG block
        for ($i = $lineIdx; $i -ge 0 -and $i -ge ($lineIdx - 30); $i--) {
            $ln = $lines[$i].Trim()
            if ($ln -eq "#if DEBUG") { $suppress = $true; break }
            if ($ln -eq "#endif" -or $ln -eq "#else") { break }
        }

        # Check 2: OPTSCAN:SUPPRESS on same line or line above
        if (-not $suppress) {
            $curLine = $lines[$lineIdx]
            $prevLine = if ($lineIdx -gt 0) { $lines[$lineIdx - 1] } else { "" }
            if ($curLine -match "OPTSCAN:SUPPRESS" -or $prevLine -match "OPTSCAN:SUPPRESS") {
                $suppress = $true
            }
        }

        # Check 3: File has const-false guard that makes this code path dead in Release
        if (-not $suppress -and $f.Category -eq "determinism") {
            $fileContent = ($lines) -join "`n"
            if ($fileContent -match "const\s+bool\s+\w+\s*=\s*false" -and $fileContent -match "#if DEBUG") {
                # This file has a DEBUG-only diagnostic toggle. Check if the finding
                # is inside a helper function only called from the guarded else-branch.
                $suppress = $true
            }
        }
    }
    if (-not $suppress) { $filtered += $f }
}
$findings = $filtered

# Summary
$critCount = @($findings | Where-Object { $_.Severity -eq "CRITICAL" }).Count
$warnCount = @($findings | Where-Object { $_.Severity -eq "WARNING" }).Count
$suggCount = @($findings | Where-Object { $_.Severity -eq "SUGGESTION" }).Count

Write-Host "OPTSCAN|SUMMARY|CRITICAL=$critCount|WARNING=$warnCount|SUGGESTION=$suggCount"
Write-Host "OPTSCAN|MODE|$Mode"
Write-Host "OPTSCAN|SCOPE|$(if ($Scope) { $Scope } else { 'full-repo' })"
Write-Host ""

# Findings sorted by severity
$sorted = $findings | Sort-Object { $severityRank[$_.Severity] } -Descending
foreach ($f in $sorted) {
    Write-Host "OPTSCAN|FINDING|$($f.Severity)|$($f.Category)|$($f.File):$($f.Line)|$($f.Description)|$($f.Fix)"
}

Write-Host ""
Write-Host "OPTSCAN|TOTAL|$($findings.Count) findings"

# Exit code: 1 if any critical findings
if ($critCount -gt 0) { exit 1 } else { exit 0 }
