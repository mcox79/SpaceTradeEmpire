Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepoRoot {
  $root = (& git rev-parse --show-toplevel 2>$null)
  if (-not $root) { throw "Not in a git repository." }
  return $root.Trim()
}

$repoRoot = Get-RepoRoot
Set-Location $repoRoot
[Environment]::CurrentDirectory = $repoRoot

$gitDir = Join-Path $repoRoot ".git"
if (-not (Test-Path -LiteralPath $gitDir)) { throw "Missing .git directory at repo root." }

$hooksDir = Join-Path $gitDir "hooks"
if (-not (Test-Path -LiteralPath $hooksDir)) { New-Item -ItemType Directory -Path $hooksDir | Out-Null }

# pre-commit (sh) delegates to pre-commit.cmd because Git for Windows runs hooks under sh
$shHookPath  = Join-Path $hooksDir "pre-commit"
$cmdHookPath = Join-Path $hooksDir "pre-commit.cmd"

$shLines = @(
  "#!/usr/bin/env sh"
  "set -eu"
  "cmd.exe /c .git\\\\hooks\\\\pre-commit.cmd"
)

$cmdLines = @(
  "@echo off"
  "setlocal"
  ""
  "for /f ""delims="" %%G in ('git rev-parse --show-toplevel') do set ""REPO=%%G"""
  "cd /d ""%REPO%"" || exit /b 1"
  ""
  "where pwsh >nul 2>&1"
  "if not errorlevel 1 goto HAVE_PWSH"
  ""
  "where powershell >nul 2>&1"
  "if not errorlevel 1 goto HAVE_POWERSHELL"
  ""
  "echo FATAL: neither pwsh nor powershell found in PATH 1>&2"
  "exit /b 1"
  ""
  ":HAVE_PWSH"
  "pwsh -NoProfile -File scripts\\check_tabs.ps1"
  "exit /b %errorlevel%"
  ""
  ":HAVE_POWERSHELL"
  "powershell -NoProfile -ExecutionPolicy Bypass -File scripts\\check_tabs.ps1"
  "exit /b %errorlevel%"
)

$encNoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($shHookPath,  ($shLines  -join "`n")  + "`n",  $encNoBom)
[System.IO.File]::WriteAllText($cmdHookPath, ($cmdLines -join "`r`n") + "`r`n", $encNoBom)

# Ensure sh hook is LF-only and starts with shebang; Git for Windows will still run it
Write-Host ("INSTALLED: {0}" -f $shHookPath)
Write-Host ("INSTALLED: {0}" -f $cmdHookPath)
