Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepoRoot {
	$root = (& git rev-parse --show-toplevel 2>$null)
	if (-not $root) { throw "Not in a git repository." }
	return $root.Trim()
}

function Ensure-Dir {
	param([Parameter(Mandatory=$true)][string]$Path)
	if (-not (Test-Path -LiteralPath $Path)) {
		[void](New-Item -ItemType Directory -Force -Path $Path)
	}
}

function Write-Utf8NoBomFile {
	param(
		[Parameter(Mandatory=$true)][string]$Path,
		[Parameter(Mandatory=$true)][string]$Content,
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

$preCommitShPath = Join-Path $githooksDir "pre-commit"
$preCommitCmdPath = Join-Path $githooksDir "pre-commit.cmd"

# Git for Windows runs hooks under sh. A .cmd must be launched via cmd.exe /c.
# Important: Convert MSYS path to Windows path via cygpath when available.
$preCommitSh = @"
#!/bin/sh
# Repo-tracked pre-commit hook for Git for Windows.
HOOK_DIR="`$(cd "`$(dirname "`$0")" && pwd)"
if command -v cygpath >/dev/null 2>&1; then
	HOOK_DIR_WIN="`$(cygpath -w "`$HOOK_DIR")"
else
	HOOK_DIR_WIN="`$(cd "`$HOOK_DIR" && pwd -W 2>/dev/null || echo "`$HOOK_DIR")"
fi
CMD_PATH="`$HOOK_DIR_WIN\\pre-commit.cmd"
exec cmd.exe /c "\"`$CMD_PATH\""
"@

$preCommitCmd = @"
@echo off
setlocal

rem Repo-tracked pre-commit hook (Windows)
rem Runs scripts\check_tabs.ps1 from repo root using Windows PowerShell 5.1

for /f "usebackq delims=" %%R in (`git rev-parse --show-toplevel 2^>nul`) do set "REPOROOT=%%R"
if not defined REPOROOT (
	echo FATAL: not in a git repository.
	exit /b 1
)

set "PWSH=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if not exist "%PWSH%" (
	echo FATAL: Windows PowerShell not found at %PWSH%
	exit /b 1
)

cd /d "%REPOROOT%"
"%PWSH%" -NoProfile -ExecutionPolicy Bypass -File "scripts\check_tabs.ps1"
exit /b %ERRORLEVEL%
"@

Write-Utf8NoBomFile -Path $preCommitShPath -Content $preCommitSh -EnsureTrailingNewline
Write-Utf8NoBomFile -Path $preCommitCmdPath -Content $preCommitCmd -EnsureTrailingNewline

# Ensure the sh hook is executable (best effort; on Windows it may be ignored)
try {
	& git update-index --chmod=+x ".githooks/pre-commit" 2>$null | Out-Null
} catch { }

# Set core.hooksPath to repo tracked .githooks
& git config core.hooksPath .githooks
if ($LASTEXITCODE -ne 0) { throw "git config core.hooksPath failed." }

Write-Host "OK: Installed repo-tracked hooks at .githooks and set core.hooksPath=.githooks"
Write-Host "Next: make a test commit to verify the pre-commit hook runs."
