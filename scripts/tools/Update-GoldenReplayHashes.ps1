# scripts/tools/Update-GoldenReplayHashes.ps1
# PowerShell 5.1
# Updates docs/generated/snapshots/golden_replay_hashes.txt based on GoldenReplay test output.

[CmdletBinding()]
param(
	[string]$RepoRoot = "",
	[string]$TestProjectRel = "SimCore.Tests\SimCore.Tests.csproj",
	[string]$GoldenRel = "docs\generated\snapshots\golden_replay_hashes.txt",
	[switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Derive repo root robustly (works even if $PSScriptRoot is empty)
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
	$scriptPath = $MyInvocation.MyCommand.Path
	if ([string]::IsNullOrWhiteSpace($scriptPath)) {
		throw "Cannot determine script path to derive RepoRoot. Please pass -RepoRoot explicitly."
	}
	$scriptDir = Split-Path -Parent $scriptPath
	$RepoRoot = (Resolve-Path (Join-Path $scriptDir "..\..")).Path
}
else {
	$RepoRoot = (Resolve-Path $RepoRoot).Path
}

function Get-Utf8NoBomEncoding {
	return New-Object System.Text.UTF8Encoding($false)
}

function Write-AllTextUtf8NoBom([string]$Path, [string]$Content) {
	$enc = Get-Utf8NoBomEncoding
	[System.IO.File]::WriteAllText($Path, $Content, $enc)
}

function Ensure-ParentDir([string]$Path) {
	$dir = [System.IO.Path]::GetDirectoryName($Path)
	if (-not [string]::IsNullOrWhiteSpace($dir) -and -not (Test-Path $dir)) {
		New-Item -ItemType Directory -Force -Path $dir | Out-Null
	}
}

function Extract-Hash([string[]]$Lines, [string]$Prefix) {
	# Matches: "Genesis Hash: <64hex>" / "Final Hash A: <64hex>" / "Final Hash B: <64hex>"
	foreach ($l in $Lines) {
		if ($l -match ("^\s*" + [Regex]::Escape($Prefix) + "\s*:\s*([0-9A-Fa-f]{64})\s*$")) {
			return $Matches[1].ToUpperInvariant()
		}
	}
	throw "Could not find hash line with prefix '$Prefix' (expected 64 hex chars)."
}

$repoRootFull = (Resolve-Path $RepoRoot).Path
$testProj = Join-Path $repoRootFull $TestProjectRel
$goldenPath = Join-Path $repoRootFull $GoldenRel

if (-not (Test-Path $testProj)) {
	throw "Test project not found: $testProj"
}

function Quote-Arg([string]$a) {
	if ($null -eq $a) { return '""' }
	# Quote if it contains whitespace or quotes or semicolons (logger uses ;)
	if ($a -match '[\s";]') {
		# Escape embedded quotes for Windows command-line parsing
		$escaped = $a -replace '"', '\"'
		return '"' + $escaped + '"'
	}
	return $a
}

$filter = "FullyQualifiedName~GoldenReplay"
$cmd = @("dotnet", "test", $testProj, "--filter", $filter, "--logger", "console;verbosity=minimal")
if ($NoBuild) { $cmd += "--no-build" }

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $cmd[0]

# Quote args so paths with spaces stay intact
$argTokens = @()
for ($i = 1; $i -lt $cmd.Count; $i++) {
	$argTokens += (Quote-Arg $cmd[$i])
}
$psi.Arguments = ($argTokens -join " ")

Write-Host ("Running: " + $psi.FileName + " " + $psi.Arguments)

$psi.WorkingDirectory = $repoRootFull
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true

$p = New-Object System.Diagnostics.Process
$p.StartInfo = $psi
[void]$p.Start()

$stdout = $p.StandardOutput.ReadToEnd()
$stderr = $p.StandardError.ReadToEnd()
$p.WaitForExit()

if ($p.ExitCode -ne 0) {
	Write-Host $stdout
	Write-Host $stderr
	throw "dotnet test failed with exit code $($p.ExitCode)"
}

$lines = $stdout -split "`r?`n"

$genesis = Extract-Hash -Lines $lines -Prefix "Genesis Hash"
$finalA = Extract-Hash -Lines $lines -Prefix "Final Hash A"
$finalB = Extract-Hash -Lines $lines -Prefix "Final Hash B"

if ($finalA -ne $finalB) {
	throw "Final Hash A != Final Hash B. A=$finalA B=$finalB"
}

Ensure-ParentDir $goldenPath

# Canonical, minimal file format matching your current file:
# Genesis=<64hex>
# Final=<64hex>
$content = @()
$content += "Genesis=$genesis"
$content += "Final=$finalA"
$content += ""
Write-AllTextUtf8NoBom -Path $goldenPath -Content ($content -join "`n")

Write-Host "Updated: $goldenPath"
Write-Host "Genesis: $genesis"
Write-Host "Final:   $finalA"
