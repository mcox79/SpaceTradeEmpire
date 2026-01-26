param(
[switch]$StagedOnly = $true
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Fail([string]$msg) {
Write-Host $msg -ForegroundColor Red
exit 1
}

function Get-RepoRoot {
$root = (& git rev-parse --show-toplevel 2>$null)
if (-not $root) { throw "Not in a git repository." }
return $root.Trim()
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
# Use -z and join output to avoid PowerShell newline/chunk quirks.
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
return ,$gd
}

function Check-GdFile([string]$repoRoot, [string]$relPath) {
$full = Join-Path $repoRoot $relPath
if (-not (Test-Path -LiteralPath $full)) { return @("MISSING: $relPath") }

$bytes = [System.IO.File]::ReadAllBytes($full)
$errs = New-Object System.Collections.Generic.List[string]

if (Has-Utf8Bom $bytes) { $errs.Add("BOM: $relPath") }

$text = [System.Text.Encoding]::UTF8.GetString($bytes)
if (Contains-ZeroWidth $text) { $errs.Add("ZERO-WIDTH: $relPath") }

$lines = $text -split "`r?`n", -1
for ($i = 0; $i -lt $lines.Length; $i++) {
$ln = $lines[$i]

if ($ln -match '^( +)\S') {
$errs.Add(("{0}:{1}: leading spaces indentation (tabs-only policy)" -f $relPath, ($i+1)))
}

if ($ln -match "^\t+ +\S") {
$errs.Add(("{0}:{1}: mixed indent (tabs then spaces)" -f $relPath, ($i+1)))
}

if ($ln -match "[ \t]+$") {
$errs.Add(("{0}:{1}: trailing whitespace" -f $relPath, ($i+1)))
}
}

return $errs
}

$repoRoot = Get-RepoRoot
Set-Location $repoRoot

$files = @()
if ($StagedOnly) { $files = @(Get-StagedGdFiles) }

if ($null -eq $files -or $files.Length -eq 0) {
Write-Host "OK: no staged .gd files"
exit 0
}

$allErrs = New-Object System.Collections.Generic.List[string]
foreach ($f in $files) {
$errs = Check-GdFile -repoRoot $repoRoot -relPath $f
foreach ($e in $errs) { $allErrs.Add($e) }
}

if ($allErrs.Count -gt 0) {
Write-Host "FATAL: indentation/whitespace policy violations detected:" -ForegroundColor Red
$allErrs | Sort-Object | Get-Unique | ForEach-Object { Write-Host $_ -ForegroundColor Red }
Fail "Fix the above issues, then re-stage and commit."
}

Write-Host "OK: staged .gd files pass tabs-only policy"
exit 0