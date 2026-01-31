Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (git rev-parse --show-toplevel).Trim()
$runnerPath = Join-Path $repoRoot "SimCore.Runner"
$binPath = Join-Path $runnerPath "bin"
$debugPath = Join-Path $binPath "Debug"
$netPath = Join-Path $debugPath "net8.0"
$runnerExe = Join-Path $netPath "SimCore.Runner.exe"

$scenariosDir = Join-Path $repoRoot "scenarios"
$scenario = Join-Path $scenariosDir "smoke_test.json"

if (-not (Test-Path $runnerExe)) {
    Write-Error "Runner executable not found at $runnerExe. Build solution first."
    exit 1
}

if (-not (Test-Path $scenario)) {
    Write-Error "Scenario file not found at $scenario."
    exit 1
}

Write-Host "--- EXECUTING SMOKE TEST ---"
& $runnerExe $scenario
if ($LASTEXITCODE -eq 0) {
    Write-Host "--- SMOKE TEST PASSED ---" -ForegroundColor Green
} else {
    Write-Error "--- SMOKE TEST FAILED ---"
}