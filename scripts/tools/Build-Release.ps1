<#
.SYNOPSIS
    Builds the Space Trade Empire C#/.NET 8 project in Release configuration
    and prints instructions for running the Godot export.

.DESCRIPTION
    1. Runs dotnet build in Release mode
    2. Creates the build/ output directory if missing
    3. Prints Godot export instructions

.EXAMPLE
    powershell -File scripts/tools/Build-Release.ps1

.NOTES
    GATE.T46.BUILD.RELEASE_TEST.001 — Release pipeline verification (2026-03-22)

    Results:
    - dotnet build "Space Trade Empire.csproj" -c Release: PASS (0 errors, 6 pre-existing addon warnings)
    - dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release: PASS (1454/1454)
      Note: BalanceLock baseline was stale (AutoSaveTweaksV0 added without baseline update).
            Deleted and regenerated docs/tweaks/balance_baseline_v0.json before final test run.

    Godot export templates: NOT INSTALLED
      - C:\Users\marsh\AppData\Roaming\Godot\export_templates\ exists but is empty.
      - Godot PCK/EXE export is BLOCKED until templates are installed.
      - To install: open Godot editor -> Editor -> Manage Export Templates -> Download.
        Or download godot-4.6-stable-export-templates.tpz from https://godotengine.org/download/archive/4.6-stable/
        and install via Editor -> Manage Export Templates -> Install from File.
      - Once installed, export command is:
          C:\Godot\Godot_v4.6-stable_mono_win64.exe --headless --path <project_root> --export-release "Windows Desktop"
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path))
$csproj = Join-Path $projectRoot 'Space Trade Empire.csproj'
$buildDir = Join-Path $projectRoot 'build'

Write-Host "=== Build-Release ===" -ForegroundColor Cyan
Write-Host "Project: $csproj"
Write-Host ""

# Step 1: dotnet build Release
Write-Host "[1/2] Building C#/.NET 8 project (Release)..." -ForegroundColor Yellow
dotnet build $csproj -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: dotnet build failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}
Write-Host "Build succeeded." -ForegroundColor Green
Write-Host ""

# Step 2: Ensure build/ output directory exists
if (-not (Test-Path $buildDir)) {
    New-Item -ItemType Directory -Path $buildDir | Out-Null
    Write-Host "[2/2] Created output directory: $buildDir" -ForegroundColor Yellow
} else {
    Write-Host "[2/2] Output directory exists: $buildDir" -ForegroundColor Yellow
}
Write-Host ""

# Step 3: Print Godot export instructions
Write-Host "=== Next Steps ===" -ForegroundColor Cyan
Write-Host @"
To export the game, ensure Godot export templates are installed, then run:

  C:\Godot\Godot_v4.6-stable_mono_win64.exe --headless --path "$projectRoot" --export-release "Windows Desktop"

This will produce:
  $buildDir\SpaceTradeEmpire.exe

Export templates can be downloaded from:
  https://godotengine.org/download/archive/4.6-stable/

Or installed via Editor > Manage Export Templates in the Godot editor.
"@

exit 0
