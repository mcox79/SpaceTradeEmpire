Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepoRoot {
	$root = (& git rev-parse --show-toplevel 2>$null)
	if (-not $root) { throw "Not in a git repository." }
	return $root.Trim()
}

function Ensure-Dir {
	param([Parameter(Mandatory = $true)][string]$Path)
	if (-not (Test-Path -LiteralPath $Path)) {
		[void](New-Item -ItemType Directory -Force -Path $Path)
	}
}

function Write-Utf8NoBomFile {
	param(
		[Parameter(Mandatory = $true)][string]$Path,
		[Parameter(Mandatory = $true)][string]$Content,
		[switch]$EnsureTrailingNewline
	)
	$enc = New-Object System.Text.UTF8Encoding($false)
	$text = $Content
	if ($EnsureTrailingNewline -and -not $text.EndsWith("`n")) { $text += "`n" }
	[System.IO.File]::WriteAllText($Path, $text, $enc)
}

$repoRoot = Get-RepoRoot
Set-Location $repoRoot
[Environment]::CurrentDirectory = $repoRoot

$githooksDir = Join-Path $repoRoot ".githooks"
Ensure-Dir -Path $githooksDir

# -------------------------------------------------------------------
# pre-commit (sh) delegates to repo-tracked pre-commit.cmd
# -------------------------------------------------------------------
$preCommitShPath = Join-Path $githooksDir "pre-commit"
$preCommitCmdPath = Join-Path $githooksDir "pre-commit.cmd"

$preCommitSh = @"
#!/usr/bin/env sh
set -eu

# Git for Windows typically executes hooks under sh; call the .cmd via cmd.exe.
# Use a relative path from repo root to avoid MSYS path issues.
cmd.exe /q /c .githooks\\pre-commit.cmd
"@

$preCommitCmd = @"
@echo off
setlocal

rem Repo-tracked pre-commit hook (Windows)
rem Runs scripts\check_tabs.ps1 from repo root using pwsh if available, else Windows PowerShell.

for /f "delims=" %%R in ('git rev-parse --show-toplevel') do set "REPOROOT=%%R"
cd /d "%REPOROOT%" || exit /b 1

where pwsh >nul 2>&1
if not errorlevel 1 goto HAVE_PWSH

where powershell >nul 2>&1
if not errorlevel 1 goto HAVE_POWERSHELL

echo FATAL: neither pwsh nor powershell found in PATH 1>&2
exit /b 1

:HAVE_PWSH
pwsh -NoProfile -File scripts\check_tabs.ps1 -Exit
exit /b %errorlevel%

:HAVE_POWERSHELL
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check_tabs.ps1 -Exit
exit /b %errorlevel%
"@

Write-Utf8NoBomFile -Path $preCommitShPath -Content $preCommitSh -EnsureTrailingNewline
Write-Utf8NoBomFile -Path $preCommitCmdPath -Content $preCommitCmd -EnsureTrailingNewline

# Best effort: mark sh hook executable in the index (may be ignored on Windows but helps elsewhere)
try { & git update-index --chmod=+x ".githooks/pre-commit" 2>$null | Out-Null } catch { }

# -------------------------------------------------------------------
# pre-push (sh) delegates to repo-tracked pre-push.cmd
# For now, it is a placeholder that does nothing.
# -------------------------------------------------------------------
$prePushShPath = Join-Path $githooksDir "pre-push"
$prePushCmdPath = Join-Path $githooksDir "pre-push.cmd"

$prePushSh = @"
#!/usr/bin/env sh
set -eu

cmd.exe /q /c .githooks\\pre-push.cmd
"@

$prePushCmd = @"
@echo off
setlocal

rem Repo-tracked pre-push hook (Windows)
rem Placeholder gate: do nothing for now.
exit /b 0
"@

Write-Utf8NoBomFile -Path $prePushShPath -Content $prePushSh -EnsureTrailingNewline
Write-Utf8NoBomFile -Path $prePushCmdPath -Content $prePushCmd -EnsureTrailingNewline

try { & git update-index --chmod=+x ".githooks/pre-push" 2>$null | Out-Null } catch { }

# -------------------------------------------------------------------
# Activate repo-tracked hooks
# -------------------------------------------------------------------
& git config core.hooksPath .githooks
if ($LASTEXITCODE -ne 0) { throw "git config core.hooksPath failed." }

Write-Host "OK: Installed repo-tracked hooks in .githooks and set core.hooksPath=.githooks"
Write-Host "Next: test with 'git commit --allow-empty -m ""hook test""' and then 'git push' to confirm pre-push runs."
