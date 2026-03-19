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

# ─── OUTPUT ───────────────────────────────────────────────────────────

# Filter out test files from findings
$findings = @($findings | Where-Object { $_.File -notlike "*Tests*" -and $_.File -notlike "*test_*" })

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
