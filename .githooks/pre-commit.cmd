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
