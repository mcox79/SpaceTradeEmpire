# FILE: scripts/tools/Export-RepoEvidence.ps1
# PURPOSE:
#   Generate deterministic evidence artifacts so gate Evidence fields can only cite real paths.
#
# OUTPUTS (under docs/generated/evidence):
#   - repo_file_index.txt            (all tracked + relevant untracked files)
#   - simcore_tests_index.txt        (all SimCore.Tests .cs files)
#   - godot_scripts_index.txt        (scripts/**/*.cs + scripts/**/*.gd)
#   - gate_evidence_grep.txt         (pattern hits for known gate anchors)
#   - gate_evidence_map.json         (machine-readable map of discovered evidence paths)
#
# USAGE:
#   powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Export-RepoEvidence.ps1 -Verbose
#
# COMPAT:
#   Windows PowerShell 5.1 (no Sort-Object -CultureInvariant)
#
# NOTES:
#   - No external dependencies. Uses git + built-in PowerShell.
#   - Deterministic ordering: ordinal string sort on normalized repo-relative paths.
#   - UTF-8 no BOM, LF newlines.

[CmdletBinding()]
param(
  [string]$RepoRoot = "",
  [string]$OutDirRelative = "docs/generated/evidence"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
  param([string]$RootHint)
  if ($RootHint -and (Test-Path $RootHint)) { return (Resolve-Path $RootHint).Path }
  $root = (& git rev-parse --show-toplevel) 2>$null
  if (-not $root) { throw "Not a git repo (git rev-parse failed). Run from inside repo or pass -RepoRoot." }
  return $root.Trim()
}

function Write-Utf8NoBomLf {
  param(
    [Parameter(Mandatory=$true)][string]$Path,
    [Parameter(Mandatory=$true)][string]$Text
  )
  $dir = Split-Path -Parent $Path
  if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }

  $normalized = $Text -replace "`r`n", "`n" -replace "`r", "`n"
  $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
  [System.IO.File]::WriteAllText($Path, $normalized, $utf8NoBom)
}

function Normalize-RelPath {
  param([string]$Path)
  if (-not $Path) { return $Path }
  return $Path.Replace("\","/").Trim()
}

function Sort-Ordinal {
  param([string[]]$Items)
  if (-not $Items) { return @() }
  # Deterministic in PS 5.1 for identical inputs; we normalize first to avoid platform path separators.
  return $Items | Sort-Object { $_ }
}

function Get-GitFileIndex {
  param([string]$Root)
  Push-Location $Root
  try {
    $tracked = (& git ls-files) | Where-Object { $_ -ne "" }
    $untracked = (& git ls-files --others --exclude-standard) | Where-Object { $_ -ne "" }

    $allRaw = @($tracked + $untracked) | ForEach-Object { Normalize-RelPath $_ }
    $all = Sort-Ordinal -Items $allRaw
    return $all
  }
  finally { Pop-Location }
}

function Filter-Paths {
  param(
    [string[]]$Paths,
    [string]$Regex
  )
  return $Paths | Where-Object { $_ -match $Regex }
}

function Grep-RepoText {
  param(
    [string]$Root,
    [string[]]$Paths,
    [string[]]$Patterns
  )

  $results = New-Object System.Collections.Generic.List[string]

  foreach ($rel in $Paths) {
    $relNorm = Normalize-RelPath $rel
    $abs = Join-Path $Root $relNorm
    if (-not (Test-Path $abs)) { continue }

    $ext = [System.IO.Path]::GetExtension($abs).ToLowerInvariant()
    if ($ext -in @(".png",".jpg",".jpeg",".webp",".bmp",".ico",".dll",".exe",".pdb",".zip",".7z",".rar",".mp4",".mov",".ogg",".wav",".mp3",".ttf",".otf")) { continue }

    $text = ""
    try {
      $text = Get-Content -LiteralPath $abs -Raw -ErrorAction Stop
    } catch {
      continue
    }

    foreach ($p in $Patterns) {
      if ($text -match $p) {
        $results.Add(("{0}`t{1}" -f $relNorm, $p))
      }
    }
  }

  return Sort-Ordinal -Items $results.ToArray()
}

function Build-GateEvidenceMap {
  param(
    [string[]]$TestFiles,
    [string[]]$GodotFiles,
    [string[]]$GrepHits
  )

  $map = [ordered]@{
    generated_at_utc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    indices = [ordered]@{
      simcore_tests_index = "docs/generated/evidence/simcore_tests_index.txt"
      godot_scripts_index = "docs/generated/evidence/godot_scripts_index.txt"
      grep_hits = "docs/generated/evidence/gate_evidence_grep.txt"
    }
    discovered = [ordered]@{
      simcore_tests = $TestFiles
      godot_scripts = $GodotFiles
      grep_hits = $GrepHits
    }
    suggested_evidence = [ordered]@{
      time = ($TestFiles | Where-Object { $_ -match "Time" })
      determinism = ($TestFiles | Where-Object { $_ -match "Determinism|Golden|Replay|LongRun" })
      save_load = ($TestFiles | Where-Object { $_ -match "Save|Load" })
      intents = ($TestFiles | Where-Object { $_ -match "Intent" })
      market = ($TestFiles | Where-Object { $_ -match "Market|Price" })
      lane = ($TestFiles | Where-Object { $_ -match "Lane|Flow" })
      intel = ($TestFiles | Where-Object { $_ -match "Intel" })
      invariants = ($TestFiles | Where-Object { $_ -match "Invariant|Conservation" })
      ui_bridge = ($GodotFiles | Where-Object { $_ -match "SimBridge|StationMenu" })
    }
  }

  return ($map | ConvertTo-Json -Depth 6)
}

# MAIN
$root = Resolve-RepoRoot -RootHint $RepoRoot
$outDir = Join-Path $root $OutDirRelative

Write-Verbose ("RepoRoot: {0}" -f $root)
Write-Verbose ("OutDir:   {0}" -f $outDir)

$allFiles = Get-GitFileIndex -Root $root

$simcoreTests = Sort-Ordinal -Items (Filter-Paths -Paths $allFiles -Regex '^SimCore\.Tests/.*\.cs$')
$godotScripts = Sort-Ordinal -Items (Filter-Paths -Paths $allFiles -Regex '^scripts/.*\.(cs|gd)$')

$patterns = @(
  'MarketPublishCadence',
  'IntelContract',
  'TimeContract',
  'LongRunWorldHash',
  'SaveLoadWorldHash',
  'InventoryConservation',
  'BasicStateInvariants',
  'EnqueueIntent',
  'SubmitBuyIntent',
  'SubmitSellIntent',
  'BuyIntent',
  'SellIntent'
)

$grepUniverse = Sort-Ordinal -Items @($simcoreTests + $godotScripts)
$grepHits = Grep-RepoText -Root $root -Paths $grepUniverse -Patterns $patterns

Write-Utf8NoBomLf -Path (Join-Path $outDir "repo_file_index.txt") -Text (($allFiles -join "`n") + "`n")
Write-Utf8NoBomLf -Path (Join-Path $outDir "simcore_tests_index.txt") -Text (($simcoreTests -join "`n") + "`n")
Write-Utf8NoBomLf -Path (Join-Path $outDir "godot_scripts_index.txt") -Text (($godotScripts -join "`n") + "`n")
Write-Utf8NoBomLf -Path (Join-Path $outDir "gate_evidence_grep.txt") -Text (($grepHits -join "`n") + "`n")

$mapJson = Build-GateEvidenceMap -TestFiles $simcoreTests -GodotFiles $godotScripts -GrepHits $grepHits
Write-Utf8NoBomLf -Path (Join-Path $outDir "gate_evidence_map.json") -Text ($mapJson + "`n")

Write-Host "Wrote evidence artifacts to: $OutDirRelative"
Write-Host " - repo_file_index.txt"
Write-Host " - simcore_tests_index.txt"
Write-Host " - godot_scripts_index.txt"
Write-Host " - gate_evidence_grep.txt"
Write-Host " - gate_evidence_map.json"
