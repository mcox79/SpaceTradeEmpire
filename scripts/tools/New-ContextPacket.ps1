<#
.SYNOPSIS
    Generates a compact Context Packet for LLM injection.
    Writes: docs/generated/01_CONTEXT_PACKET.md

    Sections:
    - [SYSTEM HEALTH] connectivity + tests + hash snapshot presence
    - [NEXT ACTIONS] minimal deterministic next steps
    - [GIT] branch/head/baseline + status porcelain + recent commits + changed files vs baseline
    - [HOT FILES] extracted from docs/generated/02_STATUS_PACKET.txt (GIT DIFF NAME-STATUS block)
    - [FILE MAP] repo-relative paths only (capped)
    - [FOCUS FILES] optional content for matched files (capped)
#>

[CmdletBinding()]
param(
    [string[]] $Focus = @(),

    # Compatibility: some tooling calls New-ContextPacket.ps1 -Force
    [switch]   $Force,

    # Default is compact. Turn on heavy sections only when needed.
    [switch]   $IncludeFileMap,
    [switch]   $IncludeFullTweakPolicy,

    [int]      $MaxMapFiles = 350,
    [int]      $MaxHotFiles = 10,
    [int]      $MaxFocusFileBytes = 70000,
    [int]      $MaxTotalBytes = 220000,

    [int]      $AtlasChildLimit = 8,
    [int]      $GeneratedArtifactLimit = 25
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-AllTextUtf8NoBom([string] $Path, [string] $Content) {
    $enc = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $enc)
}

function Ensure-Dir([string] $Dir) {
    if (-not (Test-Path -LiteralPath $Dir)) {
        New-Item -ItemType Directory -Force -Path $Dir | Out-Null
    }
}

function Normalize-Rel([string] $AbsPath, [string] $RootAbs) {
    $full = (Resolve-Path -LiteralPath $AbsPath).Path
    $root = (Resolve-Path -LiteralPath $RootAbs).Path
    $rel = $full.Substring($root.Length).TrimStart('\','/')
    return ($rel -replace '\\','/')
}

function Is-IgnoredPath([string] $Rel) {
    $p = ($Rel -replace '\\','/').ToLowerInvariant()
$ignore = @(
    '.git/', '.vs/', '_scratch/', 'addons/', 'bin/', 'obj/', 'testresults/',
    '_archive/', '_shadow_godot_tree/', '.godot/', '.import/', '.mono/',
    'docs/generated/', 'docs/archive/'
)
    foreach ($i in $ignore) {
        if ($p.StartsWith($i) -or $p.Contains('/' + $i)) { return $true }
    }
    return $false
}

function Safe-Append([System.Text.StringBuilder] $Sb, [string] $Text, [ref] $TotalBytes, [int] $CapBytes) {
    $b = [System.Text.Encoding]::UTF8.GetByteCount($Text)
    if (($TotalBytes.Value + $b) -gt $CapBytes) {
        [void]$Sb.Append([Environment]::NewLine)
        [void]$Sb.Append('<<TRUNCATED TOTAL OUTPUT: cap reached>>')
        [void]$Sb.Append([Environment]::NewLine)
        return $false
    }
    [void]$Sb.Append($Text)
    $TotalBytes.Value += $b
    return $true
}

function Read-TextCapped([string] $AbsPath, [int] $CapBytes) {
    if (-not (Test-Path -LiteralPath $AbsPath)) { return '<<MISSING: ' + $AbsPath + '>>' }

    $fi = Get-Item -LiteralPath $AbsPath
    if ($fi.Length -le $CapBytes) {
        return Get-Content -LiteralPath $AbsPath -Raw -Encoding utf8
    }

    $headBytes = [Math]::Min($CapBytes, $fi.Length)
    $fs = [System.IO.File]::OpenRead($AbsPath)
    try {
        $buf = New-Object byte[] $headBytes
        [void] $fs.Read($buf, 0, $headBytes)
        $text = [System.Text.Encoding]::UTF8.GetString($buf)
    } finally {
        $fs.Dispose()
    }

    return '<<TRUNCATED: ' + $AbsPath + ' (' + $fi.Length + ' bytes, cap ' + $CapBytes + ' bytes)>>' + [Environment]::NewLine + $text
}

function Read-Utf8Raw([string] $AbsPath) {
    if (-not (Test-Path -LiteralPath $AbsPath)) { return $null }
    return Get-Content -LiteralPath $AbsPath -Raw -Encoding utf8
}

function Try-ReadJson([string] $AbsPath) {
    try {
        $raw = Read-Utf8Raw -AbsPath $AbsPath
        if ([string]::IsNullOrWhiteSpace($raw)) { return $null }
        return ($raw | ConvertFrom-Json)
    } catch {
        return $null
    }
}

function Get-StatusPacketMeta {
    param([string] $StatusPath)

    if (-not (Test-Path -LiteralPath $StatusPath)) { return $null }

    $raw = Get-Content -LiteralPath $StatusPath -TotalCount 80 -Encoding utf8
    $meta = [ordered]@{}

    foreach ($l in $raw) {
        if ($l -match '^head:\s*(\S+)') { $meta.head = $Matches[1] }
        if ($l -match '^baseline:\s*(\S+)') { $meta.baseline = $Matches[1] }
        if ($l -match '^timestamp_local:\s*(.+)$') { $meta.timestamp_local = $Matches[1].Trim() }
    }

    if ($meta.Count -eq 0) { return $null }
    return [pscustomobject]$meta
}

function Summarize-Dir {
    param(
        [string] $Prefix,
        [object[]] $AllFiles,
        [int] $ChildLimit = 8
    )

    $p = $Prefix.TrimEnd('/') + '/'
    $subset = @(
    $AllFiles |
        Where-Object { $_.rel.StartsWith($p, [System.StringComparison]::OrdinalIgnoreCase) }
)
    if ($subset.Count -eq 0) { return @('- ' + $Prefix + ': (none)') }

    $extCounts = @($subset | Group-Object ext | Sort-Object Name | ForEach-Object { "{0}={1}" -f $_.Name, $_.Count })
    $extText = if ($extCounts.Count -gt 0) { ($extCounts -join ', ') } else { '(no_exts)' }

    $childCounts = @(
        $subset |
            ForEach-Object {
                $rest = $_.rel.Substring($p.Length)
                $seg = $rest.Split('/')[0]
                if ([string]::IsNullOrWhiteSpace($seg)) { $seg = '(root)' }
                $seg
            } |
            Group-Object |
            Sort-Object @{ Expression = { $_.Count }; Descending = $true }, @{ Expression = { $_.Name }; Ascending = $true } |
            Select-Object -First $ChildLimit |
            ForEach-Object { "{0}={1}" -f $_.Name, $_.Count }
    )

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add(("- {0}: files={1}; {2}" -f $Prefix, $subset.Count, $extText)) | Out-Null
    if ($childCounts.Count -gt 0) {
        $lines.Add(("  children: {0}" -f ($childCounts -join ', '))) | Out-Null
    }
    return @($lines.ToArray())
}

function Format-ArtifactLine {
    param(
        [string] $Rel,
        [string] $Abs
    )

    if (-not (Test-Path -LiteralPath $Abs)) { return "- [MISSING] $Rel" }

    $fi = Get-Item -LiteralPath $Abs -ErrorAction SilentlyContinue
    $sha = $null
    try { $sha = (Get-FileHash -Algorithm SHA256 -LiteralPath $Abs).Hash } catch { $sha = '<<HASH_FAIL>>' }

    return ("- [OK] {0} bytes={1} sha256={2}" -f $Rel, $fi.Length, $sha)
}

function Get-HotFilesFromStatusPacket {
    param(
        [string] $StatusPath,
        [int] $Max = 10
    )

    if (-not (Test-Path -LiteralPath $StatusPath)) { return @() }

    $raw = Get-Content -LiteralPath $StatusPath -Raw -Encoding utf8 -ErrorAction SilentlyContinue
    if ([string]::IsNullOrWhiteSpace($raw)) { return @() }

    $lines = $raw -split "(`r`n|`n|`r)"
    if (-not $lines -or $lines.Count -eq 0) { return @() }

    $start = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^===== GIT DIFF NAME-STATUS ') { $start = $i + 1; break }
    }
    if ($start -lt 0) { return @() }

    $out = New-Object System.Collections.Generic.List[string]
    for ($i = $start; $i -lt $lines.Count; $i++) {
        $l = $lines[$i]
        if ($l -match '^===== ') { break }
        if ([string]::IsNullOrWhiteSpace($l)) { continue }

        $parts = $l -split "`t"
        if ($parts.Count -ge 2) {
            $p = $parts[$parts.Count - 1].Trim()
            if (-not [string]::IsNullOrWhiteSpace($p)) { $out.Add($p) | Out-Null }
        }

        if ($out.Count -ge $Max) { break }
    }

    return @($out.ToArray() | Sort-Object -Unique)
}

$Root = (Get-Location).Path

$outDir = Join-Path $Root 'docs\generated'
Ensure-Dir $outDir
$outPath = Join-Path $outDir '01_CONTEXT_PACKET.md'
$tmpPath = $outPath + '.tmp'

$NL = [Environment]::NewLine
$now = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'

# --- GIT IDENTITY (must exist before any $branch/$head/$baseline usage) ---
$branch = (& git rev-parse --abbrev-ref HEAD).Trim()
$head = (& git rev-parse HEAD).Trim()

$baseline = $null
try {
    & git rev-parse --verify origin/main 1>$null 2>$null
    $baseline = (& git merge-base HEAD origin/main).Trim()
} catch {
    try {
        & git rev-parse --verify main 1>$null 2>$null
        $baseline = (& git merge-base HEAD main).Trim()
    } catch {
        try { $baseline = (& git rev-parse HEAD~1).Trim() } catch { $baseline = $head }
    }
}

$TweakPolicyFile = Join-Path $Root 'docs\tweaks\TWEAK_ROUTING_POLICY.md'

$sb = New-Object System.Text.StringBuilder
$totalBytes = 0

[void](Safe-Append $sb ('## REPO CONTEXT' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
[void](Safe-Append $sb ($NL + '### [IDENTITY]' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
[void](Safe-Append $sb ('branch: ' + $branch + $NL + 'head: ' + $head + $NL + 'baseline: ' + $baseline + $NL) ([ref]$totalBytes) $MaxTotalBytes)

# --- HARD RULES (Tweak routing / numeric literals) ---
[void](Safe-Append $sb ($NL + '### [HARD RULES]' + $NL) ([ref]$totalBytes) $MaxTotalBytes)

if (Test-Path -LiteralPath $TweakPolicyFile) {
    [void](Safe-Append $sb ('policy_source: docs/tweaks/TWEAK_ROUTING_POLICY.md' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
    [void](Safe-Append $sb ('note: pass -IncludeFullTweakPolicy to embed more of this file' + $NL) ([ref]$totalBytes) $MaxTotalBytes)

    $cap = 14000
    if ($IncludeFullTweakPolicy) { $cap = 100000 }

    $txt = Read-TextCapped -AbsPath $TweakPolicyFile -CapBytes $cap
    [void](Safe-Append $sb ($txt + $NL) ([ref]$totalBytes) $MaxTotalBytes)
} else {
    [void](Safe-Append $sb ('<<MISSING: \docs\tweaks\TWEAK_ROUTING_POLICY.md (create this file to enforce tweak routing rules in every session)>>' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
}

# --- FILE DISCOVERY (used by atlas, optional file map, focus files) ---
$exts = @('.cs', '.tscn', '.gd', '.json', '.md', '.ps1')

$all = Get-ChildItem -LiteralPath $Root -Recurse -File -Force |
    ForEach-Object {
        $rel = Normalize-Rel -AbsPath $_.FullName -RootAbs $Root
        $ext = ([System.IO.Path]::GetExtension($rel)).ToLowerInvariant()
        [pscustomobject]@{ rel = $rel; ext = $ext }
    } |
    Where-Object { ($exts -contains $_.ext) -and (-not (Is-IgnoredPath $_.rel)) } |
    Sort-Object @{ Expression = { $_.rel }; Ascending = $true }

# --- WORKFLOW QUICKREF ---
[void](Safe-Append $sb ($NL + '### [WORKFLOW QUICKREF]' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
[void](Safe-Append $sb ('- Validate gates: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Validate-Gates.ps1' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
[void](Safe-Append $sb ('- Status packet: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/New-StatusPacket.ps1' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
[void](Safe-Append $sb ('- Repo health: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Repo-Health.ps1' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
[void](Safe-Append $sb ('- Tests (canonical): dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release' + $NL) ([ref]$totalBytes) $MaxTotalBytes)

# --- CANONICAL DOCS / ENTRYPOINTS ---
[void](Safe-Append $sb ($NL + '### [CANONICAL DOCS AND ENTRYPOINTS]' + $NL) ([ref]$totalBytes) $MaxTotalBytes)

$canon = @(
    'docs/00_READ_FIRST_LLM_CONTRACT.md',
    'docs/10_ARCHITECTURE_INDEX.md',
    'docs/10_KERNEL_INDEX.md',
    'docs/20_TESTING_AND_DETERMINISM.md',
    'docs/21_90_TERMS_UNITS_IDS.md',
    'docs/30_CONNECTIVITY_AND_INTERFACES.md',
    'docs/40_TOOLING_AND_HOOKS.md',
    'docs/54_EPICS.md',
    'docs/57_RUNBOOK.md',
    'docs/gates/gates.json',
    'docs/gates/gates.schema.json',
    'scripts/tools/Validate-Gates.ps1',
    'scripts/tools/New-StatusPacket.ps1',
    'scripts/tools/Repo-Health.ps1'
)

foreach ($c in $canon) {
    $abs = Join-Path $Root ($c -replace '/', '\')
    $ln = Format-ArtifactLine -Rel $c -Abs $abs
    if (-not (Safe-Append $sb ($ln + $NL) ([ref]$totalBytes) $MaxTotalBytes)) { break }
}

# --- REPO ATLAS ---
[void](Safe-Append $sb ($NL + '### [REPO ATLAS]' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
[void](Safe-Append $sb ('(high-level map; excludes generated output; compact by default)' + $NL) ([ref]$totalBytes) $MaxTotalBytes)

$atlasRoots = @('SimCore', 'SimCore.Tests', 'SimCore.Runner', 'scripts', 'scenes', 'docs')
foreach ($r in $atlasRoots) {
    $lines = Summarize-Dir -Prefix $r -AllFiles $all -ChildLimit $AtlasChildLimit
    foreach ($ln in $lines) {
        if (-not (Safe-Append $sb ($ln + $NL) ([ref]$totalBytes) $MaxTotalBytes)) { break }
    }
}

# --- SYSTEM HEALTH ---
$ConnectFile = Join-Path $Root 'docs\generated\connectivity_violations.json'
$TestSummaryFile = Join-Path $Root 'docs\generated\05_TEST_SUMMARY.txt'
$HashSnapshotFile = Join-Path $Root 'docs\generated\snapshots\golden_replay_hashes.txt'
$statusPacketPath = Join-Path $Root 'docs\generated\02_STATUS_PACKET.txt'

[void](Safe-Append $sb ($NL + '### [SYSTEM HEALTH]' + $NL) ([ref]$totalBytes) $MaxTotalBytes)

# Status Packet staleness check
$sp = Get-StatusPacketMeta -StatusPath $statusPacketPath
if ($null -eq $sp) {
    [void](Safe-Append $sb ('- [WARN] Status packet missing: docs/generated/02_STATUS_PACKET.txt' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
} else {
    if ($sp.head -and $sp.head -ne $head) {
        [void](Safe-Append $sb ('- [WARN] Status packet stale vs HEAD: status_head=' + $sp.head + ' current_head=' + $head + $NL) ([ref]$totalBytes) $MaxTotalBytes)
    } else {
        [void](Safe-Append $sb ('- [OK] Status packet: head=' + $sp.head + $NL) ([ref]$totalBytes) $MaxTotalBytes)
    }
}

# Connectivity
if (Test-Path -LiteralPath $ConnectFile) {
    $j = Try-ReadJson -AbsPath $ConnectFile
    if ($null -eq $j) {
        [void](Safe-Append $sb ('- [WARN] Connectivity: present but JSON parse failed' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
    } else {
        $errs = 0
        $warns = 0
        if ($j.counts) {
            if ($null -ne $j.counts.errors) { $errs = [int]$j.counts.errors }
            if ($null -ne $j.counts.warnings) { $warns = [int]$j.counts.warnings }
        }
        $vcount = if ($null -ne $j.violations) { @($j.violations).Count } else { 0 }

        if ($errs -gt 0) {
            [void](Safe-Append $sb ('- [CRITICAL] Connectivity: errors=' + $errs + ' warnings=' + $warns + ' violations=' + $vcount + $NL) ([ref]$totalBytes) $MaxTotalBytes)
        } elseif ($warns -gt 0) {
            [void](Safe-Append $sb ('- [WARN] Connectivity: errors=' + $errs + ' warnings=' + $warns + ' violations=' + $vcount + $NL) ([ref]$totalBytes) $MaxTotalBytes)
        } else {
            [void](Safe-Append $sb ('- [OK] Connectivity: clean' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
        }

        if ($j.rules) {
            $r = @($j.rules | Sort-Object id | Select-Object -First 8)
            if ($r.Count -gt 0) {
                [void](Safe-Append $sb ('  rules:' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
                foreach ($rr in $r) {
                    $desc = ''
                    if ($rr.description) { $desc = $rr.description.ToString() }
                    if ($desc.Length -gt 120) { $desc = $desc.Substring(0,120) + '...' }
                    [void](Safe-Append $sb ('  - ' + $rr.id + ' severity=' + $rr.severity + ' : ' + $desc + $NL) ([ref]$totalBytes) $MaxTotalBytes)
                }
            }
        }
    }
} else {
    [void](Safe-Append $sb ('- [WARN] Connectivity scan missing: docs/generated/connectivity_violations.json' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
}

# Tests
$testCfg = 'UNKNOWN'
if (Test-Path -LiteralPath $TestSummaryFile) {
    try {
        $lines = Get-Content -LiteralPath $TestSummaryFile -TotalCount 700 -Encoding utf8

        $cfg = 'UNKNOWN'
        foreach ($l in $lines) {
            if ($l -match '\\bin\\Release\\') { $cfg = 'Release'; break }
            if ($l -match '\\bin\\Debug\\') { $cfg = 'Debug' }
        }
        $testCfg = $cfg

        $summary = $null
        foreach ($l in $lines) {
            if ($l -match '^\s*Passed!\s*-\s*Failed:\s*\d+,\s*Passed:\s*\d+,\s*Skipped:\s*\d+,\s*Total:\s*\d+') { $summary = $l; break }
        }

        if ($summary) {
            $m = [regex]::Match($summary, 'Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)')
            if ($m.Success) {
                $failed = [int]$m.Groups[1].Value
                $passed = [int]$m.Groups[2].Value
                $skipped = [int]$m.Groups[3].Value
                $total = [int]$m.Groups[4].Value
                if ($failed -gt 0) {
                    [void](Safe-Append $sb ('- [CRITICAL] Tests: failed=' + $failed + ' total=' + $total + ' config=' + $cfg + $NL) ([ref]$totalBytes) $MaxTotalBytes)
                } else {
                    [void](Safe-Append $sb ('- [OK] Tests: passed=' + $passed + ' skipped=' + $skipped + ' total=' + $total + ' config=' + $cfg + $NL) ([ref]$totalBytes) $MaxTotalBytes)
                }
            } else {
                [void](Safe-Append $sb ('- [INFO] Tests: summary present but parse failed; config=' + $cfg + $NL) ([ref]$totalBytes) $MaxTotalBytes)
            }
        } else {
            [void](Safe-Append $sb ('- [INFO] Tests: 05_TEST_SUMMARY.txt present; config=' + $cfg + ' (no recognizable summary line)' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
        }

        if ($cfg -eq 'Debug') {
            [void](Safe-Append $sb ('  note: Debug build detected; canonical health uses Release' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
        }
    } catch {
        [void](Safe-Append $sb ('- [WARN] Tests: 05_TEST_SUMMARY.txt present but could not be parsed' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
    }
} else {
    [void](Safe-Append $sb ('- [WARN] Tests summary missing: docs/generated/05_TEST_SUMMARY.txt' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
}

# Hash snapshot presence
if (Test-Path -LiteralPath $HashSnapshotFile) {
    [void](Safe-Append $sb ('- [OK] Hash Snapshot: Present (docs/generated/snapshots/golden_replay_hashes.txt)' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
} else {
    [void](Safe-Append $sb ('- [WARN] Hash Snapshot: Not found (docs/generated/snapshots/golden_replay_hashes.txt)' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
}

# --- GENERATED ARTIFACTS ---
[void](Safe-Append $sb ($NL + '### [GENERATED ARTIFACTS]' + $NL) ([ref]$totalBytes) $MaxTotalBytes)

$artifacts = @(
    'docs/generated/02_STATUS_PACKET.txt',
    'docs/generated/connectivity_manifest.json',
    'docs/generated/connectivity_violations.json',
    'docs/generated/05_TEST_SUMMARY.txt',
    'docs/generated/snapshots/golden_replay_hashes.txt'
)

foreach ($a in $artifacts) {
    $abs = Join-Path $Root ($a -replace '/', '\')
    $ln = Format-ArtifactLine -Rel $a -Abs $abs
    if (-not (Safe-Append $sb ($ln + $NL) ([ref]$totalBytes) $MaxTotalBytes)) { break }
}

$genDir = Join-Path $Root 'docs\generated'
if (Test-Path -LiteralPath $genDir) {
    $others = Get-ChildItem -LiteralPath $genDir -File -Recurse -Force -ErrorAction SilentlyContinue |
        Where-Object {
            $rel = Normalize-Rel -AbsPath $_.FullName -RootAbs $Root
            ($rel -notin $artifacts) -and ($_.Length -le 1048576)
        } |
        Sort-Object FullName |
        Select-Object -First $GeneratedArtifactLimit

    if ($others.Count -gt 0) {
        [void](Safe-Append $sb ('(other generated files, capped)' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
        foreach ($o in $others) {
            $rel = Normalize-Rel -AbsPath $o.FullName -RootAbs $Root
            $sha = $null
            try { $sha = (Get-FileHash -Algorithm SHA256 -LiteralPath $o.FullName).Hash } catch { $sha = '<<HASH_FAIL>>' }
            if (-not (Safe-Append $sb ('- ' + $rel + ' bytes=' + $o.Length + ' sha256=' + $sha + $NL) ([ref]$totalBytes) $MaxTotalBytes)) { break }
        }
    }
}

[void](Safe-Append $sb ($NL + '### [NEXT ACTIONS]' + $NL) ([ref]$totalBytes) $MaxTotalBytes)

if ($null -ne $sp -and $sp.head -and $sp.head -ne $head) {
    [void](Safe-Append $sb ('- Status packet stale vs HEAD: run scripts/tools/New-StatusPacket.ps1 then rerun this context packet' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
}

if ($testCfg -eq 'Debug') {
    [void](Safe-Append $sb ('- Tests were run in Debug: rerun dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
}

if (Test-Path -LiteralPath $ConnectFile) {
    $j = Try-ReadJson -AbsPath $ConnectFile
    if ($null -eq $j) {
        [void](Safe-Append $sb ('- Connectivity: could not parse violations JSON' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
    } else {
        $cnt = 0
        if ($null -ne $j.violations) { $cnt = @($j.violations).Count }
        if ($cnt -gt 0) { [void](Safe-Append $sb ('- Fix connectivity violations (blocking): ' + $cnt + $NL) ([ref]$totalBytes) $MaxTotalBytes) }
        else { [void](Safe-Append $sb ('- Connectivity: clean' + $NL) ([ref]$totalBytes) $MaxTotalBytes) }
    }
} else {
    [void](Safe-Append $sb ('- Run connectivity scan (no violations file found)' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
}

if (Test-Path -LiteralPath $TestSummaryFile) {
    $first = (Get-Content -LiteralPath $TestSummaryFile -TotalCount 120 -Encoding utf8) -join "`n"
    if ($first -match 'Failed:\s*0') { [void](Safe-Append $sb ('- Tests: passed' + $NL) ([ref]$totalBytes) $MaxTotalBytes) }
    else { [void](Safe-Append $sb ('- Tests: check 05_TEST_SUMMARY.txt (possible failures)' + $NL) ([ref]$totalBytes) $MaxTotalBytes) }
} else {
    [void](Safe-Append $sb ('- Tests: missing 05_TEST_SUMMARY.txt (run Verify Logic)' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
}

[void](Safe-Append $sb ('- Run gate review using docs/55_LLM_GATE_REVIEW_PROMPT.md against the attached artifacts' + $NL) ([ref]$totalBytes) $MaxTotalBytes)

[void](Safe-Append $sb ($NL + '### [GIT]' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
[void](Safe-Append $sb ('branch: ' + $branch + $NL + 'head: ' + $head + $NL + 'baseline: ' + $baseline + $NL) ([ref]$totalBytes) $MaxTotalBytes)

$porcelain = (& git status --porcelain=v1)
[void](Safe-Append $sb ($NL + 'status_porcelain:' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
if ($porcelain) { [void](Safe-Append $sb (($porcelain -join $NL) + $NL) ([ref]$totalBytes) $MaxTotalBytes) }
else { [void](Safe-Append $sb ('(clean)' + $NL) ([ref]$totalBytes) $MaxTotalBytes) }

$recent = (& git log -n 8 --pretty=format:'%h %s')
if ($recent) {
    [void](Safe-Append $sb ($NL + 'recent_commits:' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
    [void](Safe-Append $sb (($recent -join $NL) + $NL) ([ref]$totalBytes) $MaxTotalBytes)
}

$changed = (& git diff --name-only ($baseline + '..HEAD'))
if ($changed) {
    [void](Safe-Append $sb ($NL + 'changed_files_vs_baseline:' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
    [void](Safe-Append $sb (($changed -join $NL) + $NL) ([ref]$totalBytes) $MaxTotalBytes)
} else {
    [void](Safe-Append $sb ($NL + 'changed_files_vs_baseline:' + $NL + '(none)' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
}

# --- HOT FILES ---
[void](Safe-Append $sb ($NL + '### [HOT FILES]' + $NL) ([ref]$totalBytes) $MaxTotalBytes)

$statusPacketPath = Join-Path $Root 'docs\generated\02_STATUS_PACKET.txt'
$hot = Get-HotFilesFromStatusPacket -StatusPath $statusPacketPath -Max $MaxHotFiles
if (-not (Test-Path -LiteralPath $statusPacketPath)) {
    [void](Safe-Append $sb ('(status packet missing; generate docs/generated/02_STATUS_PACKET.txt to populate hot files)' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
} elseif (@($hot).Count -eq 0) {
    [void](Safe-Append $sb ('(none)' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
} else {
    foreach ($p in $hot) {
        if (-not (Safe-Append $sb ('- ' + $p + $NL) ([ref]$totalBytes) $MaxTotalBytes)) { break }
    }
}

# --- FILE MAP ---
[void](Safe-Append $sb ($NL + '### [FILE MAP]' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
[void](Safe-Append $sb ('(compact by default; pass -IncludeFileMap to include paths)' + $NL) ([ref]$totalBytes) $MaxTotalBytes)

if (-not $IncludeFileMap) {
    [void](Safe-Append $sb ('(skipped)' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
} else {
    $listed = 0
    foreach ($x in $all) {
        if ($listed -ge $MaxMapFiles) { break }
        if (-not (Safe-Append $sb ('- ' + $x.rel + $NL) ([ref]$totalBytes) $MaxTotalBytes)) { break }
        $listed++
    }
    if (@($all).Count -gt $listed) {
        [void](Safe-Append $sb ('<<TRUNCATED FILE MAP: listed ' + $listed + ' of ' + @($all).Count + '>>' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
    }
}

# --- FOCUS FILES ---
[void](Safe-Append $sb ($NL + '### [FOCUS FILES]' + $NL) ([ref]$totalBytes) $MaxTotalBytes)

$focusFrags = @()
foreach ($f in $Focus) {
    if (-not [string]::IsNullOrWhiteSpace($f)) { $focusFrags += $f.Trim() }
}

if ($focusFrags.Count -eq 0) {
    [void](Safe-Append $sb ('(none specified; pass -Focus ''path_fragment'')' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
} else {
    $matches = New-Object System.Collections.Generic.List[string]
    foreach ($x in $all) {
        foreach ($frag in $focusFrags) {
            if ($x.rel -like ('*' + $frag + '*')) { $matches.Add($x.rel) | Out-Null; break }
        }
    }

    $uniq = @($matches.ToArray() | Sort-Object -Unique)
    if ($uniq.Count -eq 0) {
        [void](Safe-Append $sb ('(no matches)' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
    } else {
        foreach ($rel in $uniq) {
            [void](Safe-Append $sb ($NL + '#### [FOCUS] ' + $rel + $NL) ([ref]$totalBytes) $MaxTotalBytes)

            $abs = Join-Path $Root ($rel -replace '/', '\')
            $txt = Read-TextCapped -AbsPath $abs -CapBytes $MaxFocusFileBytes

            $ext = ([System.IO.Path]::GetExtension($rel)).ToLowerInvariant().TrimStart('.')
            if ($ext -eq 'cs') { $ext = 'csharp' }
            if ([string]::IsNullOrWhiteSpace($ext)) { $ext = 'text' }

            [void](Safe-Append $sb ('```' + $ext + $NL) ([ref]$totalBytes) $MaxTotalBytes)
            [void](Safe-Append $sb ($txt + $NL) ([ref]$totalBytes) $MaxTotalBytes)
            [void](Safe-Append $sb ('```' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
        }
    }
}

Write-AllTextUtf8NoBom -Path $tmpPath -Content $sb.ToString()
Move-Item -Force -LiteralPath $tmpPath -Destination $outPath
Write-Host ('WROTE: ' + $outPath)
exit 0
