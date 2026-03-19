<#
.SYNOPSIS
  Visual screenshot capture runner. Supports 7 modes: first-hour (default), full, transit, video, eval, regression, scenario.
  For headless-only verification, use Run-FHBot.ps1 instead.
.EXAMPLE
  .\scripts\tools\Run-Screenshot.ps1 -Mode first-hour
  .\scripts\tools\Run-Screenshot.ps1 -Mode full
  .\scripts\tools\Run-Screenshot.ps1 -Mode video
  .\scripts\tools\Run-Screenshot.ps1 -Mode regression
  .\scripts\tools\Run-Screenshot.ps1 -Mode scenario -Script res://scripts/tests/my_custom_bot.gd -Prefix CUST
#>
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('first-hour','full','transit','video','eval','regression','scenario')]
    [string]$Mode,
    [string]$Script = '',       # Custom script path (scenario mode only)
    [string]$Prefix = 'SCEN',   # Output prefix filter (scenario mode only)
    [int]$TimeoutSec = 0        # 0 = auto per mode
)

$ErrorActionPreference = 'Stop'

# Source shared helpers
. (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'common.ps1')

$repoRoot = Get-RepoRoot
$godotExe = Get-GodotExe -RepoRoot $repoRoot
Push-Location $repoRoot

try {
    # ── Mode configuration ──
    $modeConfig = @{
        'first-hour' = @{
            Script = 'res://scripts/tests/test_first_hour_proof_v0.gd'
            Timeout = 120
            Prefix = 'FH1'
            NativeDir = 'reports/first_hour'
            CopyFrom = 'reports/first_hour'
        }
        'full' = @{
            Script = 'res://scripts/tests/visual_sweep_bot_v0.gd'
            Timeout = 120
            Prefix = 'VSWP'
            NativeDir = 'reports/visual_eval'
            CopyFrom = 'reports/visual_eval'
        }
        'transit' = @{
            Script = 'res://scripts/tests/lane_transfer_diag_bot.gd'
            Timeout = 120
            Prefix = 'LTDG'
            NativeDir = 'reports/lane_transfer_diag'
            CopyFrom = 'reports/lane_transfer_diag'
        }
        'video' = @{
            Script = 'res://scripts/tests/visual_sweep_bot_v0.gd'
            Timeout = 120
            Prefix = 'VSWP'
            NativeDir = 'reports/screenshot/video'
            CopyFrom = 'reports/visual_eval'
        }
        'eval' = @{
            Script = 'res://scripts/tests/visual_sweep_bot_v0.gd'
            Timeout = 120
            Prefix = 'VSWP'
            NativeDir = 'reports/visual_eval'
            CopyFrom = 'reports/visual_eval'
        }
        'regression' = @{
            Script = 'res://scripts/tests/visual_sweep_bot_v0.gd'
            Timeout = 120
            Prefix = 'VSWP'
            NativeDir = 'reports/visual_eval'
            CopyFrom = 'reports/visual_eval'
        }
    }

    # ── Scenario mode: user-supplied script ──
    if ($Mode -eq 'scenario') {
        if ([string]::IsNullOrWhiteSpace($Script)) {
            throw 'scenario mode requires -Script parameter (e.g. -Script res://scripts/tests/my_bot.gd)'
        }
        # Derive a clean output dir name from the script filename
        $scriptStem = [System.IO.Path]::GetFileNameWithoutExtension($Script.Replace('res://', ''))
        $modeConfig['scenario'] = @{
            Script = $Script
            Timeout = 120
            Prefix = $Prefix
            NativeDir = "reports/screenshot/scenario_$scriptStem"
            CopyFrom = $null
        }
    }

    $cfg = $modeConfig[$Mode]
    if ($TimeoutSec -gt 0) { $cfg.Timeout = $TimeoutSec }

    $outputDir = Join-Path $repoRoot "reports/screenshot/$Mode"

    # ── Step 1: Build ──
    Write-Host "=== Building C# project ===" -ForegroundColor Cyan
    dotnet build 'Space Trade Empire.csproj' --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

    # ── Step 2: Clean output directories ──
    # Clean the bot's native output dir
    $nativeDir = Join-Path $repoRoot $cfg.NativeDir
    if (Test-Path $nativeDir) {
        Get-ChildItem -Path $nativeDir -File | Remove-Item -Force
        Write-Host ('Cleared ' + $nativeDir) -ForegroundColor Yellow
    } else {
        New-Item -ItemType Directory -Path $nativeDir -Force | Out-Null
    }

    # Clean the unified output dir (if different from native)
    if ($outputDir -ne $nativeDir) {
        if (Test-Path $outputDir) {
            Get-ChildItem -Path $outputDir -File | Remove-Item -Force
        } else {
            New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
        }
    }

    # ── Step 2b: Retention cleanup — remove old video/audio from OTHER mode dirs ──
    # Keep current + previous session (for pixel comparison), delete older.
    $screenshotRoot = Join-Path $repoRoot 'reports/screenshot'
    if (Test-Path $screenshotRoot) {
        $modeDirs = Get-ChildItem -Path $screenshotRoot -Directory -ErrorAction SilentlyContinue
        foreach ($mDir in $modeDirs) {
            if ($mDir.FullName -eq $outputDir) { continue }  # Skip current mode dir
            # Clean video files (.avi, .mp4) older than 1 day
            $oldVideos = Get-ChildItem -Path $mDir.FullName -Include '*.avi','*.mp4' -Recurse -ErrorAction SilentlyContinue |
                Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-1) }
            foreach ($v in $oldVideos) {
                Remove-Item -Path $v.FullName -Force
                Write-Host ('Retention cleanup: removed ' + $v.Name) -ForegroundColor DarkGray
            }
            # Clean audio files (.wav, .ogg, .mp3) older than 1 day
            $oldAudio = Get-ChildItem -Path $mDir.FullName -Include '*.wav','*.ogg','*.mp3' -Recurse -ErrorAction SilentlyContinue |
                Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-1) }
            foreach ($a in $oldAudio) {
                Remove-Item -Path $a.FullName -Force
                Write-Host ('Retention cleanup: removed ' + $a.Name) -ForegroundColor DarkGray
            }
            # Clean old PNGs (2+ days old) from non-baseline dirs — keep last session for comparison
            if ($mDir.Name -ne 'baselines') {
                $oldPngs = Get-ChildItem -Path $mDir.FullName -Filter '*.png' -ErrorAction SilentlyContinue |
                    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-2) }
                foreach ($p in $oldPngs) {
                    Remove-Item -Path $p.FullName -Force
                    Write-Host ('Retention cleanup: removed ' + $p.Name) -ForegroundColor DarkGray
                }
            }
        }
    }

    $stdoutFile = Join-Path $outputDir 'stdout.txt'
    $stderrFile = Join-Path $outputDir 'stderr.txt'

    # ── Step 3: Build Godot arguments ──
    $godotArgs = @('--path', '.', '--resolution', '1920x1080', '-s', $cfg.Script)

    if ($Mode -eq 'video') {
        $videoPath = Join-Path $outputDir 'sweep.avi'
        $godotArgs += @('--write-movie', $videoPath, '--fixed-fps', '30')
        Write-Host "Video output: $videoPath" -ForegroundColor Cyan
    }

    # ── Step 4: Launch Godot ──
    Write-Host "=== Launching Screenshot Bot ($Mode mode) ===" -ForegroundColor Cyan
    Write-Host ('Godot: ' + $godotExe)
    Write-Host ('Script: ' + $cfg.Script)
    Write-Host ('Timeout: ' + $cfg.Timeout + 's')

    $proc = Start-Process -FilePath $godotExe -ArgumentList $godotArgs `
        -PassThru -RedirectStandardOutput $stdoutFile -RedirectStandardError $stderrFile

    # Push Godot window behind the current window
    Start-Sleep -Milliseconds 500
    Add-Type -Name NativeMethods -Namespace Win32 -MemberDefinition @'
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(System.IntPtr hWnd);
        [DllImport("kernel32.dll")] public static extern System.IntPtr GetConsoleWindow();
'@
    $consoleHwnd = [Win32.NativeMethods]::GetConsoleWindow()
    if ($consoleHwnd -ne [System.IntPtr]::Zero) {
        [Win32.NativeMethods]::SetForegroundWindow($consoleHwnd) | Out-Null
    }
    Write-Host 'Godot launched in background. Waiting for completion...' -ForegroundColor DarkGray

    $exited = $proc.WaitForExit($cfg.Timeout * 1000)
    if (-not $exited) {
        Write-Host ('TIMEOUT after ' + $cfg.Timeout + 's -- killing process') -ForegroundColor Red
        $proc.Kill()
        $proc.WaitForExit(5000)
    }

    # ── Step 5: Parse output ──
    Write-Host ''
    Write-Host '=== Bot Output ===' -ForegroundColor Cyan
    if (Test-Path $stdoutFile) {
        Get-Content $stdoutFile | ForEach-Object {
            if ($_ -match ('^' + $cfg.Prefix)) { Write-Host $_ }
        }
    }

    if (Test-Path $stderrFile) {
        $stderrContent = Get-Content $stderrFile -Raw
        if ($stderrContent -and $stderrContent.Trim().Length -gt 0) {
            $errorLines = ($stderrContent -split "`n") | Where-Object { $_ -match 'SCRIPT ERROR|Parse Error|ERROR' }
            if ($errorLines) {
                Write-Host ''
                Write-Host '=== Errors ===' -ForegroundColor Red
                $errorLines | ForEach-Object { Write-Host $_ }
            }
        }
    }

    # ── Step 6: Copy output to unified dir (if needed) ──
    if ($cfg.CopyFrom -and $outputDir -ne (Join-Path $repoRoot $cfg.CopyFrom)) {
        $sourceDir = Join-Path $repoRoot $cfg.CopyFrom
        if (Test-Path $sourceDir) {
            $sourceFiles = @(Get-ChildItem -Path $sourceDir -File -ErrorAction SilentlyContinue)
            if ($sourceFiles.Count -gt 0) {
                Copy-Item -Path (Join-Path $sourceDir '*') -Destination $outputDir -Force
                Write-Host "Copied $($sourceFiles.Count) files to $outputDir" -ForegroundColor DarkGray
            }
        }
    }

    # ── Step 7: Report ──
    Write-Host ''
    Write-Host '=== Screenshots ===' -ForegroundColor Cyan
    $pngs = @(Get-ChildItem -Path $outputDir -Filter '*.png' -ErrorAction SilentlyContinue)
    if ($pngs.Count -eq 0) {
        # Check native dir as fallback
        $pngs = @(Get-ChildItem -Path $nativeDir -Filter '*.png' -ErrorAction SilentlyContinue)
    }

    if ($pngs.Count -eq 0) {
        Write-Host 'No screenshots captured. Check stderr for errors.' -ForegroundColor Red
    } else {
        foreach ($png in $pngs | Sort-Object Name) {
            $sizeKB = [math]::Round($png.Length / 1024, 1)
            Write-Host ('  ' + $png.Name + '  (' + $sizeKB + ' KB)')
        }
        Write-Host ''
        Write-Host ('Total: ' + $pngs.Count + ' screenshots') -ForegroundColor Green
    }

    # Summary JSON
    $summaryPaths = @(
        (Join-Path $outputDir 'summary.json'),
        (Join-Path $nativeDir 'summary.json')
    )
    foreach ($sp in $summaryPaths) {
        if (Test-Path $sp) {
            Write-Host ''
            Write-Host '=== Summary ===' -ForegroundColor Cyan
            $summary = Get-Content $sp -Raw | ConvertFrom-Json
            Write-Host ('  Snapshots: ' + $summary.snapshot_count)
            if ($summary.PSObject.Properties['critical_failures']) {
                Write-Host ('  Critical failures: ' + $summary.critical_failures)
            }
            break
        }
    }

    # ── Step 8: Mode-specific post-run ──
    if ($Mode -eq 'video') {
        $aviPath = Join-Path $outputDir 'sweep.avi'
        if (Test-Path $aviPath) {
            $aviSizeMB = [math]::Round((Get-Item $aviPath).Length / 1MB, 1)
            Write-Host ''
            Write-Host "=== Video Captured ===" -ForegroundColor Green
            Write-Host "  File: $aviPath"
            Write-Host "  Size: $aviSizeMB MB"
        } else {
            # Check if --write-movie created files elsewhere
            Write-Host ''
            Write-Host 'Video file not found at expected path.' -ForegroundColor Yellow
            Write-Host 'Note: --write-movie with -s scripts may require testing.' -ForegroundColor Yellow
        }
    }

    if ($Mode -eq 'regression') {
        Write-Host ''
        Write-Host '=== Regression Comparison ===' -ForegroundColor Cyan
        $baselineDir = Join-Path $repoRoot 'reports/baselines/full'
        if (-not (Test-Path $baselineDir)) {
            Write-Host 'No baselines found. Run with -Mode full first, then copy screenshots to reports/baselines/full/' -ForegroundColor Yellow
        } else {
            $compareScript = Join-Path $repoRoot 'scripts/tools/compare_screenshots.py'
            $compareResult = & python $compareScript --current $outputDir --baseline $baselineDir 2>&1
            $compareResult | ForEach-Object { Write-Host $_ }
        }
    }

    Write-Host ''
    Write-Host ('Output directory: ' + $outputDir) -ForegroundColor Green

    # Write eval_prompt.txt for eval mode
    if ($Mode -eq 'eval') {
        $pngList = ''
        if ($pngs) {
            foreach ($png in $pngs | Sort-Object Name) {
                $pngList += ('  - ' + $png.FullName + "`n")
            }
        }
        $evalGuide = Join-Path $repoRoot 'scripts/tools/visual_eval_guide.md'
        $promptText = @"
VISUAL EVALUATION REQUEST

Read the evaluation guide at: $evalGuide
Then read and evaluate each screenshot below using that guide.

Screenshots to evaluate:
$pngList
Also read the summary report: $(Join-Path $outputDir 'summary.json')

Follow the rating template from the guide for each screenshot,
then provide the overall synthesis (top 3 strengths, top 3 issues, priority fix).
"@
        $evalPromptPath = Join-Path $outputDir 'eval_prompt.txt'
        Set-Content -Path $evalPromptPath -Value $promptText -Encoding UTF8
        Write-Host ''
        Write-Host '=== Ready for Evaluation ===' -ForegroundColor Cyan
        Write-Host 'Claude will now read and evaluate the screenshots.' -ForegroundColor Green
    }

} finally {
    Pop-Location
}
