param(
  [switch]$StagedOnly = $true
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "tools\check_tabs_lib.ps1")

$result = Invoke-CheckTabs -StagedOnly:$StagedOnly
$rc = [int]$result["ExitCode"]
$lines = $result["Lines"]
foreach ($line in $lines) {
  if ($rc -ne 0) {
    Write-Host $line -ForegroundColor Red
  } else {
    Write-Host $line
  }
}
$global:LASTEXITCODE = $rc
exit $rc
