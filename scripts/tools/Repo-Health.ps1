<#
.SYNOPSIS
    Repo health one-command runner v0.

.DESCRIPTION
    Runs deterministic repo health checks:
    1) Connectivity scan -> docs/generated/connectivity_graph.json
    2) dotnet test -> enforces repo health scan including optional connectivity delta

.PARAMETER MintBaseline
    One-time operation: mints docs/connectivity/baseline_cross_layer_edges_v0.txt from current connectivity graph.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Repo-Health.ps1

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Repo-Health.ps1 -MintBaseline
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [switch] $MintBaseline
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Repo root is current directory (expected usage: run from repo root).
$repo = (Get-Location).Path

# 1) Connectivity scan (deterministic outputs under docs/generated)
powershell -NoProfile -ExecutionPolicy Bypass -File "scripts/tools/Scan-Connectivity.ps1" -Force -Harden
if ($LASTEXITCODE -ne 0) { throw "Scan-Connectivity failed: exit code $LASTEXITCODE" }

# 2) Repo health enforcement via tests.
if ($MintBaseline) {
    $env:STE_REPO_HEALTH_MINT_CONNECTIVITY_BASELINE = "1"
    Remove-Item Env:\STE_REPO_HEALTH_REQUIRE_CONNECTIVITY_DELTA -ErrorAction SilentlyContinue
} else {
    Remove-Item Env:\STE_REPO_HEALTH_MINT_CONNECTIVITY_BASELINE -ErrorAction SilentlyContinue
    $env:STE_REPO_HEALTH_REQUIRE_CONNECTIVITY_DELTA = "1"
}

dotnet test "SimCore.Tests/SimCore.Tests.csproj" -c Release
if ($LASTEXITCODE -ne 0) { throw "dotnet test failed: exit code $LASTEXITCODE" }

# Helpful deterministic output for baseline minting workflows.
if ($MintBaseline) {
    Write-Host "MINTED_BASELINE: docs/connectivity/baseline_cross_layer_edges_v0.txt"
}
