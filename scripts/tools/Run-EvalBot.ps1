# Run-EvalBot.ps1 — Runs all 5 eval bots sequentially, captures stdout per bot.
# Exit code: 0 = all clean, 1 = any SCRIPT_ERROR found.
param(
    [string]$GodotPath = "",
    [string]$RepoRoot = (Split-Path $PSScriptRoot -Parent | Split-Path -Parent)
)

$ErrorActionPreference = 'Stop'

# Resolve Godot binary
if ($GodotPath -eq "") {
    $hostname = [System.Net.Dns]::GetHostName()
    if ($hostname -eq "Home") {
        $GodotPath = "C:\Godot\Godot_v4.6-stable_mono_win64.exe"
    } else {
        $GodotPath = "C:\Users\marsh\Downloads\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64.exe"
    }
}
if (-not (Test-Path $GodotPath)) {
    Write-Host "ERROR: Godot not found at $GodotPath"
    exit 1
}

# Ensure build is current
Write-Host "=== Building game assembly ==="
$buildResult = & dotnet build (Join-Path $RepoRoot 'Space Trade Empire.csproj') --nologo -v q 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "BUILD FAILED"
    Write-Host $buildResult
    exit 1
}
Write-Host "Build OK"
Write-Host ""

# Create output directory
$outDir = Join-Path $RepoRoot 'reports/eval'
if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

# Bot definitions
$bots = @(
    @{ Name = "economy_health";     Script = "test_economy_health_eval_v0.gd";    Prefix = "ECON_HEALTH" },
    @{ Name = "narrative_pacing";   Script = "test_narrative_pacing_eval_v0.gd";   Prefix = "NARR_PACE" },
    @{ Name = "dread_pacing";       Script = "test_dread_pacing_eval_v0.gd";       Prefix = "DREAD_PACE" },
    @{ Name = "audio_atmosphere";   Script = "test_audio_atmosphere_eval_v0.gd";   Prefix = "AUDIO_ATM" },
    @{ Name = "flight_feel";        Script = "test_flight_feel_eval_v0.gd";        Prefix = "FLIGHT_FEEL" }
)

$totalPass = 0
$totalWarn = 0
$totalFail = 0
$hasScriptError = $false

Write-Host "=== Running 5 Eval Bots ==="
Write-Host ""

foreach ($bot in $bots) {
    $scriptPath = "res://scripts/tests/$($bot.Script)"
    $outFile = Join-Path $outDir "$($bot.Name)_stdout.txt"
    $errFile = Join-Path $outDir "$($bot.Name)_stderr.txt"

    Write-Host "--- $($bot.Name) ---"

    # Delete quicksave to avoid stale state
    $quicksave = Join-Path $RepoRoot 'quicksave.json'
    if (Test-Path $quicksave) { Remove-Item $quicksave -Force }
    $userSave = Join-Path $env:APPDATA 'Godot/app_userdata/Space Trade Empire/quicksave.json'
    if (Test-Path $userSave) { Remove-Item $userSave -Force }

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $GodotPath
    $psi.Arguments = "--headless --path `"$RepoRoot`" -s `"$scriptPath`""
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.WorkingDirectory = $RepoRoot

    $proc = [System.Diagnostics.Process]::Start($psi)
    # Read stderr async to avoid deadlock (both streams can fill OS buffer)
    $stderrTask = $proc.StandardError.ReadToEndAsync()  # returns Task<string>
    $stdout = $proc.StandardOutput.ReadToEnd()
    $null = $proc.WaitForExit(120000)  # 2 min timeout

    if (-not $proc.HasExited) {
        $proc.Kill()
        Write-Host "  TIMEOUT (killed after 120s)"
    }
    $stderr = $stderrTask.GetAwaiter().GetResult()

    Set-Content -Path $outFile -Value $stdout -Encoding UTF8
    Set-Content -Path $errFile -Value $stderr -Encoding UTF8

    # Check for SCRIPT_ERROR in stderr — exclude known non-bot warnings
    $stderrLines = @(($stderr -split "`n") | Where-Object {
        $_ -match 'SCRIPT ERROR' -and
        $_ -notmatch 'music_manager\.gd' -and
        $_ -notmatch 'steam_interface\.gd' -and
        $_ -notmatch 'game_manager\.gd' -and
        $_ -notmatch 'galaxy_spawner\.gd' -and
        $_ -notmatch 'hides an autoload singleton' -and
        $_ -notmatch 'StationIdentity' -and
        $_ -notmatch 'Failed to compile depended scripts' -and
        $_ -notmatch 'Cannot infer the type of'
    })
    if ($stderrLines.Count -gt 0) {
        Write-Host "  SCRIPT_ERROR detected!"
        foreach ($line in $stderrLines) { Write-Host "    $line" }
        $hasScriptError = $true
    }

    # Parse assert counts from stdout
    $prefix = $bot.Prefix
    $passCount = ([regex]::Matches($stdout, "$prefix\|ASSERT_PASS")).Count
    $warnCount = ([regex]::Matches($stdout, "$prefix\|ASSERT_WARN")).Count
    $failCount = ([regex]::Matches($stdout, "$prefix\|ASSERT_FAIL")).Count
    $totalPass += $passCount
    $totalWarn += $warnCount
    $totalFail += $failCount

    Write-Host "  exit=$($proc.ExitCode) pass=$passCount warn=$warnCount fail=$failCount"
    Write-Host "  output: $outFile"
    Write-Host ""
}

# Summary
Write-Host "=== Eval Bot Summary ==="
Write-Host "Total: pass=$totalPass warn=$totalWarn fail=$totalFail"
Write-Host "SCRIPT_ERROR: $hasScriptError"

if ($hasScriptError -or $totalFail -gt 0) {
    Write-Host "RESULT: FAIL"
    exit 1
} else {
    Write-Host "RESULT: PASS"
    exit 0
}
