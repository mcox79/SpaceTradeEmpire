param(
[switch]$AllowDocs,
[switch]$AllowTooling,
[switch]$AllowAddons,
[int]$MaxFiles = 5
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Fail($msg) {
Write-Host $msg
exit 1
}

function Ensure-Dir($path) {
if (-not (Test-Path $path)) { New-Item -ItemType Directory -Path $path | Out-Null }
}

function Get-RepoRoot() {
$root = (& git rev-parse --show-toplevel 2>$null)
if (-not $root) { Fail "ERROR: Not in a git repository." }
return $root.Trim()
}

function Normalize-RelPath($repoRoot, $path) {
$full = [System.IO.Path]::GetFullPath($path)
$root = [System.IO.Path]::GetFullPath($repoRoot)
if (-not $full.StartsWith($root)) { return $path }
return $full.Substring($root.Length).TrimStart('\','/')
}

function Is-ForbiddenPath($relPath) {
$p = $relPath.Replace('\','/')

if (-not $AllowDocs) {
if ($p -ieq "_PROJECT_CONTEXT.md") { return $true }
}

if (-not $AllowAddons) {
if ($p.StartsWith("addons/")) { return $true }
}

if (-not $AllowTooling) {
if ($p.EndsWith(".ps1")) { return $true }
if ($p.StartsWith("tools/")) { return $true }
if ($p.StartsWith("scripts/tools/")) { return $true }
}

# Generated artifacts should never be patched
if ($p.StartsWith("_scratch/") -or $p.StartsWith("._scratch/")) { return $true }

return $false
}

$repoRoot = Get-RepoRoot
Set-Location $repoRoot

Ensure-Dir "_scratch"
$patchPath = Join-Path $repoRoot "_scratch\patch.diff"

$patchText = Get-Clipboard -Raw
if (-not $patchText) { Fail "ERROR: Clipboard is empty." }

# Strip common markdown fences if present
$patchText = $patchText -replace "(?m)^\s*```(?:diff)?\s*$", ""
$patchText = $patchText -replace "(?m)^\s*```\s*$", ""

# Basic sanity check: require a diff marker somewhere (multiline)
if ($patchText -notmatch "(?m)^\s*(diff --git|---\s|\*\*\* Begin Patch)") {
	Fail "ERROR: Clipboard does not look like a unified diff patch."
}

Set-Content -Path $patchPath -Value $patchText -Encoding UTF8

Write-Host ">>> PATCH: git apply --check"
& git apply --check $patchPath
if ($LASTEXITCODE -ne 0) { Fail "ERROR: git apply --check failed." }

Write-Host ">>> PATCH: applying"
& git apply $patchPath
if ($LASTEXITCODE -ne 0) { Fail "ERROR: git apply failed." }

# Change budget and forbidden paths gate
$changed = (& git diff --name-only).Trim() | Where-Object { $_ -ne "" }
$changedCount = @($changed).Count

if ($changedCount -gt $MaxFiles) {
Fail ("ERROR: Change budget exceeded. Touched {0} files, max is {1}. Split the patch or raise -MaxFiles." -f $changedCount, $MaxFiles)
}

$badPaths = @()
foreach ($f in $changed) {
$rel = Normalize-RelPath $repoRoot $f
if (Is-ForbiddenPath $rel) { $badPaths += $rel }
}

if ($badPaths.Count -gt 0) {
Write-Host "ERROR: Patch touched forbidden paths:"
$badPaths | ForEach-Object { Write-Host (" - " + $_) }
Fail "Aborting. Revert with git reset --hard (or restore via your rollback workflow)."
}

# Indentation gate (only meaningful if .gd changed, but cheap to run always)
if (Test-Path ".\Check-GDScriptIndent.ps1") {
Write-Host ">>> GATE: tabs-only indentation"
& powershell -NoProfile -ExecutionPolicy Bypass -File ".\Check-GDScriptIndent.ps1"
if ($LASTEXITCODE -ne 0) { Fail "ERROR: Tabs-only indentation gate failed." }
} else {
Write-Host "WARN: Check-GDScriptIndent.ps1 not found at repo root. Skipping indentation gate."
}

Write-Host ">>> SUMMARY: git diff --stat"
& git diff --stat

Write-Host "OK: Patch applied and gates passed."
exit 0
