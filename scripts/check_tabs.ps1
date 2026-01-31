param(
	[switch]$Exit
)

# scripts/check_tabs.ps1
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

function Get-RepoRoot {
	$root = (& git rev-parse --show-toplevel 2>$null)
	if (-not $root) { throw "Not in a git repository (git rev-parse --show-toplevel failed)." }
	return $root.Trim()
}

$repoRoot = Get-RepoRoot
Set-Location $repoRoot
[Environment]::CurrentDirectory = $repoRoot

$libPath = Join-Path $repoRoot "scripts\tools\check_tabs_lib.ps1"
if (-not (Test-Path -LiteralPath $libPath)) {
	throw "Missing dependency: scripts\tools\check_tabs_lib.ps1"
}

. $libPath

if (-not (Get-Command Invoke-CheckTabs -ErrorAction SilentlyContinue)) {
	throw "check_tabs_lib.ps1 did not define Invoke-CheckTabs."
}

$result = Invoke-CheckTabs -StagedOnly

foreach ($line in $result.Lines) {
	Write-Host $line
}

$code = 1
try { $code = [int]$result.ExitCode } catch { $code = 1 }

# Always set LASTEXITCODE for callers
$global:LASTEXITCODE = $code

# Only terminate the process if explicitly requested (hooks/CI)
if ($Exit) {
	exit $code
}

# Interactive: do not emit an extra "0"/"1" line, just return control
return
