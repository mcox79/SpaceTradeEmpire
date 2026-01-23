param(
  [Parameter(Mandatory=$true)][string]$Path
)

$tokens = $null
$errs = $null
[System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path $Path), [ref]$tokens, [ref]$errs) | Out-Null

if ($errs.Count -gt 0) {
  Write-Host "FAIL: PowerShell parse check: $Path"
  $errs | ForEach-Object {
    Write-Host ("{0}:{1}:{2} {3}" -f $Path, $_.Extent.StartLineNumber, $_.Extent.StartColumnNumber, $_.Message)
  }
  exit 1
}

Write-Host "OK: PowerShell parse check: $Path"
exit 0