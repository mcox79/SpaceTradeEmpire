param(
    [string] $Targets = "",
    [int]    $MaxFileBytes = 204800,    # 200 KB
    [int]    $MaxTotalBytes = 15728640  # 15 MB
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    $root = (& git rev-parse --show-toplevel 2>$null)
    if (-not $root) { throw "Not a git repo (git rev-parse failed). Run from inside the repo." }
    return $root.Trim()
}

function Ensure-Dir([string] $Path) {
    if (-not (Test-Path $Path)) { New-Item -ItemType Directory -Force -Path $Path | Out-Null }
}

function Write-AllTextUtf8NoBom([string] $Path, [string] $Content) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $utf8NoBom)
}

function Read-TextCapped([string] $Path, [int] $CapBytes) {
    if (-not (Test-Path $Path)) { return "<<MISSING: $Path>>" }

    $fi = Get-Item $Path
    if ($fi.Length -le $CapBytes) {
        return Get-Content -LiteralPath $Path -Raw
    }

    $headBytes = [Math]::Min($CapBytes, $fi.Length)
    $fs = [System.IO.File]::OpenRead($Path)
    try {
        $buf = New-Object byte[] $headBytes
        [void] $fs.Read($buf, 0, $headBytes)
        $text = [System.Text.Encoding]::UTF8.GetString($buf)
    } finally {
        $fs.Dispose()
    }

    return "<<TRUNCATED: $Path ($($fi.Length) bytes, cap $CapBytes bytes)>>`r`n$text"
}

function Safe-Append([System.Text.StringBuilder] $Sb, [string] $Text, [ref] $TotalBytes, [int] $MaxBytes) {
    $bytes = [System.Text.Encoding]::UTF8.GetByteCount($Text)
    if (($TotalBytes.Value + $bytes) -gt $MaxBytes) {
        $Sb.AppendLine("<<TRUNCATED TOTAL OUTPUT: cap $MaxBytes bytes reached>>") | Out-Null
        return $false
    }
    $Sb.Append($Text) | Out-Null
    $TotalBytes.Value += $bytes
    return $true
}

function To-Lines($x) {
    if ($null -eq $x) { return @() }
    if ($x -is [string]) { return @($x) }
    return @($x)
}

function Is-SkippedPath([string] $RepoRelativePath) {
    $p = $RepoRelativePath.Replace("\", "/").ToLowerInvariant()

    # Skip common generated/binary dirs
    $skipDirs = @(
        ".git/", ".godot/", ".import/", "bin/", "obj/", ".mono/", "library/", "temp/", "artifacts/"
    )
    foreach ($d in $skipDirs) {
        if ($p.StartsWith($d) -or $p.Contains("/" + $d)) { return $true }
    }

    # Skip binary-ish extensions
    $ext = [System.IO.Path]::GetExtension($p)
    $skipExts = @(
        ".png",".jpg",".jpeg",".webp",".gif",
        ".wav",".mp3",".ogg",
        ".ttf",".otf",
        ".dll",".exe",".pdb",
        ".zip",".7z",".pdf",".bin"
    )
    if ($skipExts -contains $ext) { return $true }

    return $false
}

$repoRoot = Get-RepoRoot
Set-Location $repoRoot

$now = Get-Date
$outDir = Join-Path $repoRoot "docs\generated"
Ensure-Dir $outDir
$outPath = Join-Path $outDir "02_STATUS_PACKET.txt"
$tmpPath = $outPath + ".tmp"

if ($null -eq $Targets) { $Targets = "" }
$Targets = $Targets.Trim()

$branch = (& git rev-parse --abbrev-ref HEAD).Trim()
$head   = (& git rev-parse HEAD).Trim()

# Baseline selection: merge-base vs origin/main if available, else vs main, else HEAD~1
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

$statusPorcelain = (& git status --porcelain=v1)
$nameStatus = (& git diff --name-status $baseline..HEAD)
$nameStatusLines = To-Lines $nameStatus


# Extract changed file paths
$changed = @()
foreach ($line in $nameStatusLines) {

    if (-not $line) { continue }
    $parts = $line -split "`t"
    if ($parts.Count -ge 2) {
        $path = $parts[$parts.Count - 1]
        if (-not (Is-SkippedPath $path)) { $changed += $path }
    }
}
$changed = @($changed | Sort-Object -Unique)


# Canonical anchors to embed
$anchors = @(
    "docs/54_DEVELOPMENT_PLAN.md",
    "docs/52_DEVELOPMENT_LOCK_RECOMMENDATIONS.md",
    "docs/50_EARLY_MISSION_LADDER_AND_TRAVEL.md",
    "docs/51_ECONOMY_AND_TRADE_DESIGN_LAW.md",
    "docs/53_PROGRAMS_FLEETS_DOCTRINES_CONTROL_SURFACE.md",
    "docs/30_CONNECTIVITY_AND_INTERFACES.md",
    "docs/20_TESTING_AND_DETERMINISM.md",
    "docs/21_UNITS_AND_INVARIANTS.md",
    "docs/40_TOOLING_AND_HOOKS.md",
    "docs/10_DOCS_INDEX.md",
    "docs/10_ARCHITECTURE_INDEX.md"
)

# Tool outputs to embed if present
$toolOutputs = @(
    "docs/generated/connectivity_manifest.json",
    "docs/generated/connectivity_violations.json",
    "docs/generated/05_TEST_SUMMARY.txt"
)

# Snapshots (if present)
$snapshotDir = Join-Path $repoRoot "docs\generated\snapshots"
$snapshots = @()
if (Test-Path $snapshotDir) {
    $snapshots = Get-ChildItem -Path $snapshotDir -File -ErrorAction SilentlyContinue |
        Sort-Object FullName |
        ForEach-Object { $_.FullName }
}

$sb = New-Object System.Text.StringBuilder
$totalBytes = 0

$header = @"
STATUS PACKET
timestamp_local: $($now.ToString("yyyy-MM-dd HH:mm:ss"))
repo_root: $repoRoot
branch: $branch
head: $head
baseline: $baseline
targets: $Targets
generator: scripts/tools/New-StatusPacket.ps1
max_file_bytes: $MaxFileBytes
max_total_bytes: $MaxTotalBytes

"@
[void](Safe-Append $sb $header ([ref]$totalBytes) $MaxTotalBytes)

[void](Safe-Append $sb "===== CANONICAL ANCHORS =====`r`n" ([ref]$totalBytes) $MaxTotalBytes)
foreach ($a in $anchors) {
    $full = Join-Path $repoRoot $a
    $sec = "`r`n----- FILE: $a -----`r`n"
    if (-not (Safe-Append $sb $sec ([ref]$totalBytes) $MaxTotalBytes)) { break }
    $content = Read-TextCapped -Path $full -CapBytes $MaxFileBytes
    if (-not (Safe-Append $sb ($content + "`r`n") ([ref]$totalBytes) $MaxTotalBytes)) { break }
}

[void](Safe-Append $sb "`r`n===== GIT STATUS =====`r`n" ([ref]$totalBytes) $MaxTotalBytes)

$statusLines = @()
if ($null -ne $statusPorcelain) {
    if ($statusPorcelain -is [string]) { $statusLines = @($statusPorcelain) }
    else { $statusLines = @($statusPorcelain) }
}

if ($statusLines.Count -gt 0) {
    [void](Safe-Append $sb (($statusLines -join "`r`n") + "`r`n") ([ref]$totalBytes) $MaxTotalBytes)
} else {
    [void](Safe-Append $sb "(clean)`r`n" ([ref]$totalBytes) $MaxTotalBytes)
}

[void](Safe-Append $sb "`r`n===== GIT DIFF NAME-STATUS ($baseline..HEAD) =====`r`n" ([ref]$totalBytes) $MaxTotalBytes)

$nameStatusLines = @()
if ($null -ne $nameStatus) {
    if ($nameStatus -is [string]) { $nameStatusLines = @($nameStatus) }
    else { $nameStatusLines = @($nameStatus) }
}

if ($nameStatusLines.Count -gt 0) {
    [void](Safe-Append $sb (($nameStatusLines -join "`r`n") + "`r`n") ([ref]$totalBytes) $MaxTotalBytes)
} else {
    [void](Safe-Append $sb "(no changes)`r`n" ([ref]$totalBytes) $MaxTotalBytes)
}

[void](Safe-Append $sb "`r`n===== CHANGED FILE DETAILS (DIFFS / CONTENT) =====`r`n" ([ref]$totalBytes) $MaxTotalBytes)

$skippedLarge = New-Object System.Collections.Generic.List[string]

foreach ($path in $changed) {
    $sec = "`r`n----- PATH: $path -----`r`n"
    if (-not (Safe-Append $sb $sec ([ref]$totalBytes) $MaxTotalBytes)) { break }

    $full = Join-Path $repoRoot ($path.Replace("/", "\"))
    if (Test-Path $full) {
        $fi = Get-Item $full -ErrorAction SilentlyContinue
        if ($fi -and $fi.Length -gt $MaxFileBytes) {
            $note = "<<SKIPPED CONTENT (too large): $path ($($fi.Length) bytes, cap $MaxFileBytes)>>`r`n"
            [void](Safe-Append $sb $note ([ref]$totalBytes) $MaxTotalBytes)
            $skippedLarge.Add($path) | Out-Null
            continue
        }
    }

    $diffRaw = & git diff --unified=3 $baseline..HEAD -- $path 2>$null
	$diffLines = To-Lines $diffRaw

	if ($diffLines.Count -gt 0) {
	    $diffText = ($diffLines -join "`r`n")
	    $diffBytes = [System.Text.Encoding]::UTF8.GetByteCount($diffText)
	    if ($diffBytes -gt $MaxFileBytes) {
	        $diffText = "<<TRUNCATED DIFF: $path>>`r`n" + $diffText.Substring(0, [Math]::Min($diffText.Length, 20000))
	    }
	    if (-not (Safe-Append $sb ($diffText + "`r`n") ([ref]$totalBytes) $MaxTotalBytes)) { break }
	    continue
	}


    if (Test-Path $full) {
        if (Is-SkippedPath $path) {
            [void](Safe-Append $sb "<<SKIPPED CONTENT (binary/excluded): $path>>`r`n" ([ref]$totalBytes) $MaxTotalBytes)
            continue
        }
        $content = Read-TextCapped -Path $full -CapBytes $MaxFileBytes
        if (-not (Safe-Append $sb ($content + "`r`n") ([ref]$totalBytes) $MaxTotalBytes)) { break }
    } else {
        [void](Safe-Append $sb "<<MISSING PATH IN WORKTREE: $path>>`r`n" ([ref]$totalBytes) $MaxTotalBytes)
    }
}

[void](Safe-Append $sb "`r`n===== TOOL OUTPUTS (IF PRESENT) =====`r`n" ([ref]$totalBytes) $MaxTotalBytes)
foreach ($t in $toolOutputs) {
    $full = Join-Path $repoRoot $t
    if (-not (Test-Path $full)) { continue }
    $sec = "`r`n----- FILE: $t -----`r`n"
    if (-not (Safe-Append $sb $sec ([ref]$totalBytes) $MaxTotalBytes)) { break }
    $content = Read-TextCapped -Path $full -CapBytes $MaxFileBytes
    if (-not (Safe-Append $sb ($content + "`r`n") ([ref]$totalBytes) $MaxTotalBytes)) { break }
}

foreach ($s in $snapshots) {
    $rel = $s.Substring($repoRoot.Length).TrimStart("\")
    $sec = "`r`n----- FILE: $rel -----`r`n"
    if (-not (Safe-Append $sb $sec ([ref]$totalBytes) $MaxTotalBytes)) { break }
    $content = Read-TextCapped -Path $s -CapBytes $MaxFileBytes
    if (-not (Safe-Append $sb ($content + "`r`n") ([ref]$totalBytes) $MaxTotalBytes)) { break }
}

$footer = @"
===== PACKET SUMMARY =====
baseline: $baseline
changed_files_count: $(@($changed | Where-Object { $_ -and $_.ToString().Trim() -ne "" }).Count)


skipped_large_files_count: $($skippedLarge.Count)
skipped_large_files:
$($skippedLarge -join "`r`n")

"@
[void](Safe-Append $sb $footer ([ref]$totalBytes) $MaxTotalBytes)

Write-AllTextUtf8NoBom -Path $tmpPath -Content $sb.ToString()
Move-Item -Force -LiteralPath $tmpPath -Destination $outPath

Write-Host ("WROTE: {0}" -f $outPath)
exit 0
