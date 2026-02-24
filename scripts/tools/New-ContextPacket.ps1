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
    [int]      $MaxMapFiles = 1200,
    [int]      $MaxHotFiles = 10,
    [int]      $MaxFocusFileBytes = 70000,
    [int]      $MaxTotalBytes = 220000
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
        return Get-Content -LiteralPath $AbsPath -Raw
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

function Get-HotFilesFromStatusPacket {
    param(
        [string] $StatusPath,
        [int] $Max = 10
    )

    if (-not (Test-Path -LiteralPath $StatusPath)) { return @() }

    $raw = Get-Content -LiteralPath $StatusPath -Raw -ErrorAction SilentlyContinue
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

$TweakPolicyFile = Join-Path $Root 'docs\tweaks\TWEAK_ROUTING_POLICY.md'

$sb = New-Object System.Text.StringBuilder
$totalBytes = 0

[void](Safe-Append $sb ('## REPO CONTEXT (Generated ' + $now + ')' + $NL) ([ref]$totalBytes) $MaxTotalBytes)

# --- HARD RULES (Tweak routing / numeric literals) ---
[void](Safe-Append $sb ($NL + '### [HARD RULES]' + $NL) ([ref]$totalBytes) $MaxTotalBytes)

if (Test-Path -LiteralPath $TweakPolicyFile) {
    $txt = Read-TextCapped -AbsPath $TweakPolicyFile -CapBytes 35000
    [void](Safe-Append $sb ($txt + $NL) ([ref]$totalBytes) $MaxTotalBytes)
} else {
    [void](Safe-Append $sb ('<<MISSING: \docs\tweaks\TWEAK_ROUTING_POLICY.md (create this file to enforce tweak routing rules in every session)>>' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
}

# --- SYSTEM HEALTH ---
$ConnectFile = Join-Path $Root 'docs\generated\connectivity_violations.json'
$TestSummaryFile = Join-Path $Root 'docs\generated\05_TEST_SUMMARY.txt'
$HashSnapshotFile = Join-Path $Root 'docs\generated\snapshots\golden_replay_hashes.txt'
$TweakPolicyFile = Join-Path $Root '.\docs\tweaks\TWEAK_ROUTING_POLICY.md'

[void](Safe-Append $sb ($NL + '### [SYSTEM HEALTH]' + $NL) ([ref]$totalBytes) $MaxTotalBytes)

if (Test-Path -LiteralPath $ConnectFile) {
    try {
        $j = Get-Content -LiteralPath $ConnectFile -Raw | ConvertFrom-Json
        $cnt = 0
        if ($null -ne $j.violations) { $cnt = @($j.violations).Count }
        if ($cnt -gt 0) { [void](Safe-Append $sb ('- [CRITICAL] Connectivity Violations: ' + $cnt + $NL) ([ref]$totalBytes) $MaxTotalBytes) }
        else { [void](Safe-Append $sb ('- [OK] Connectivity: Clean' + $NL) ([ref]$totalBytes) $MaxTotalBytes) }
    } catch {
        [void](Safe-Append $sb ('- [WARN] Connectivity JSON present but could not be parsed.' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
    }
} else {
    [void](Safe-Append $sb ('- [WARN] No Connectivity Scan found (docs/generated/connectivity_violations.json).' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
}

if (Test-Path -LiteralPath $TestSummaryFile) {
    try {
        $lines = Get-Content -LiteralPath $TestSummaryFile -TotalCount 500
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
                if ($failed -gt 0) { [void](Safe-Append $sb ('- [CRITICAL] Tests: Failed (' + $failed + ' failed of ' + $total + ')' + $NL) ([ref]$totalBytes) $MaxTotalBytes) }
                else { [void](Safe-Append $sb ('- [OK] Tests: Passed (' + $passed + ' passed, ' + $skipped + ' skipped, ' + $total + ' total)' + $NL) ([ref]$totalBytes) $MaxTotalBytes) }
            } else {
                [void](Safe-Append $sb ('- [INFO] Tests: Summary present but parse failed.' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
            }
        } else {
            [void](Safe-Append $sb ('- [INFO] Tests: 05_TEST_SUMMARY.txt present (no recognizable summary line).' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
        }
    } catch {
        [void](Safe-Append $sb ('- [WARN] Tests: 05_TEST_SUMMARY.txt present but could not be parsed.' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
    }
} else {
    [void](Safe-Append $sb ('- [WARN] No Test Summary found (docs/generated/05_TEST_SUMMARY.txt).' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
}

if (Test-Path -LiteralPath $HashSnapshotFile) {
    [void](Safe-Append $sb ('- [OK] Hash Snapshot: Present (golden_replay_hashes.txt)' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
} else {
    [void](Safe-Append $sb ('- [WARN] Hash Snapshot: Not found (docs/generated/snapshots/golden_replay_hashes.txt)' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
}

# --- GIT ---
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

[void](Safe-Append $sb ($NL + '### [NEXT ACTIONS]' + $NL) ([ref]$totalBytes) $MaxTotalBytes)

if (Test-Path -LiteralPath $ConnectFile) {
    try {
        $j = Get-Content -LiteralPath $ConnectFile -Raw | ConvertFrom-Json
        $cnt = 0
        if ($null -ne $j.violations) { $cnt = @($j.violations).Count }
        if ($cnt -gt 0) { [void](Safe-Append $sb ('- Fix connectivity violations (blocking): ' + $cnt + $NL) ([ref]$totalBytes) $MaxTotalBytes) }
        else { [void](Safe-Append $sb ('- Connectivity: clean' + $NL) ([ref]$totalBytes) $MaxTotalBytes) }
    } catch {
        [void](Safe-Append $sb ('- Connectivity: could not parse violations JSON' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
    }
} else {
    [void](Safe-Append $sb ('- Run connectivity scan (no violations file found)' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
}

if (Test-Path -LiteralPath $TestSummaryFile) {
    $first = (Get-Content -LiteralPath $TestSummaryFile -TotalCount 120) -join "`n"
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
if (@($hot).Count -eq 0) {
    [void](Safe-Append $sb ('(none found; generate docs/generated/02_STATUS_PACKET.txt first)' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
} else {
    foreach ($p in $hot) {
        if (-not (Safe-Append $sb ('- ' + $p + $NL) ([ref]$totalBytes) $MaxTotalBytes)) { break }
    }
}

# --- FILE MAP ---
[void](Safe-Append $sb ($NL + '### [FILE MAP]' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
[void](Safe-Append $sb ('(repo-relative paths; excludes caches, build outputs, generated)' + $NL) ([ref]$totalBytes) $MaxTotalBytes)

$exts = @('.cs', '.tscn', '.gd', '.json', '.md')

$all = Get-ChildItem -LiteralPath $Root -Recurse -File -Force |
    ForEach-Object {
        $rel = Normalize-Rel -AbsPath $_.FullName -RootAbs $Root
        $ext = ([System.IO.Path]::GetExtension($rel)).ToLowerInvariant()
        [pscustomobject]@{ rel = $rel; ext = $ext }
    } |
    Where-Object { ($exts -contains $_.ext) -and (-not (Is-IgnoredPath $_.rel)) } |
    Sort-Object @{ Expression = { $_.rel }; Ascending = $true }

$listed = 0
foreach ($x in $all) {
    if ($listed -ge $MaxMapFiles) { break }
    if (-not (Safe-Append $sb ('- ' + $x.rel + $NL) ([ref]$totalBytes) $MaxTotalBytes)) { break }
    $listed++
}
if (@($all).Count -gt $listed) {
    [void](Safe-Append $sb ('<<TRUNCATED FILE MAP: listed ' + $listed + ' of ' + @($all).Count + '>>' + $NL) ([ref]$totalBytes) $MaxTotalBytes)
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
