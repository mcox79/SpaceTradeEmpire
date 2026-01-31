Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepoRoot {
  $root = (& git rev-parse --show-toplevel 2>$null)
  if (-not $root) { throw "Not in a git repository." }
  $root.Trim()
}

function Has-Utf8Bom([byte[]]$b) {
  return ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
}

function Contains-ZeroWidth([string]$s) {
  return ($s.IndexOf([char]0xFEFF) -ge 0 -or
          $s.IndexOf([char]0x200B) -ge 0 -or
          $s.IndexOf([char]0x200C) -ge 0 -or
          $s.IndexOf([char]0x200D) -ge 0 -or
          $s.IndexOf([char]0x2060) -ge 0)
}

function Get-StagedGdFiles {
  $rawOut = & git diff --cached --name-only -z --diff-filter=ACMR
  if ($LASTEXITCODE -ne 0) { throw "git diff --cached failed." }

  $text = ""
  if ($null -ne $rawOut) {
    if ($rawOut -is [array]) { $text = ($rawOut -join "") } else { $text = [string]$rawOut }
  }
  if ([string]::IsNullOrEmpty($text)) { return @() }

  $parts = $text -split "`0"
  $gd = @()
  foreach ($p in $parts) {
    if ([string]::IsNullOrWhiteSpace($p)) { continue }
    if ($p -like "addons/*") { continue }
    if ($p -like "_scratch/*" -or $p -like "._scratch/*") { continue }
    if ($p.ToLowerInvariant().EndsWith(".gd")) { $gd += $p }
  }
  return @($gd)
}

function Get-StagedBlobText {
  param([Parameter(Mandatory=$true)][string]$RepoRelPath)
  $spec = ":" + $RepoRelPath
  $out = & git show $spec 2>$null
  if ($LASTEXITCODE -ne 0) { throw "Failed to read staged content for: $RepoRelPath" }
  return ($out | Out-String)
}

function Split-Lines([string]$text) {
  $norm = $text -replace "`r`n", "`n"
  $norm = $norm -replace "`r", "`n"
  return ($norm -split "`n", -1)
}

function Check-GdFile([string]$repoRoot, [string]$relPath) {
  $errs = New-Object System.Collections.Generic.List[string]

  $text = Get-StagedBlobText -RepoRelPath $relPath

  # Detect UTF-8 BOM as leading U+FEFF once decoded.
  if ($text.Length -gt 0 -and $text[0] -eq [char]0xFEFF) { $errs.Add("BOM: $relPath") }

  if (Contains-ZeroWidth $text) { $errs.Add("ZERO-WIDTH: $relPath") }

  $lines = Split-Lines $text
  for ($i = 0; $i -lt $lines.Length; $i++) {
    $ln = [string]$lines[$i]

    if ($ln -match '^( +)') {
      $errs.Add(("{0}:{1}: leading spaces indentation (tabs-only policy)" -f $relPath, ($i+1)))
    }

    if ($ln -match '^\t+ +') {
      $errs.Add(("{0}:{1}: mixed indent (tabs then spaces)" -f $relPath, ($i+1)))
    }

    if ($ln -match "[ \t]+$") {
      $errs.Add(("{0}:{1}: trailing whitespace" -f $relPath, ($i+1)))
    }
  }

  return $errs
}

function Invoke-CheckTabs {
  param([switch]$StagedOnly = $true)
  $repoRoot = Get-RepoRoot
  Set-Location $repoRoot
  [Environment]::CurrentDirectory = $repoRoot

  $files = @()
  if ($StagedOnly) { $files = @(Get-StagedGdFiles) }

  if ($null -eq $files -or $files.Length -eq 0) {
    return @{ ExitCode = 0; Lines = @("OK: no staged .gd files") }
  }

  $allErrs = New-Object System.Collections.Generic.List[string]
  foreach ($f in $files) {
    $errs = Check-GdFile -repoRoot $repoRoot -relPath $f
    foreach ($e in $errs) { $allErrs.Add($e) }
  }

  if ($allErrs.Count -gt 0) {
    $out = New-Object System.Collections.Generic.List[string]
    $out.Add("FATAL: indentation/whitespace policy violations detected:")
    foreach ($e in ($allErrs | Sort-Object | Get-Unique)) { $out.Add($e) }
    $out.Add("Fix the above issues, then re-stage and commit.")
    return @{ ExitCode = 1; Lines = $out }
  }

  return @{ ExitCode = 0; Lines = @("OK: staged .gd files pass tabs-only policy") }
}
