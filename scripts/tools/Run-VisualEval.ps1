<#
.SYNOPSIS
  Runs the Visual Sweep Bot windowed, captures screenshots at 16 game states.
.EXAMPLE
  .\scripts\tools\Run-VisualEval.ps1
  .\scripts\tools\Run-VisualEval.ps1 -GodotPath "C:\Godot\Godot_v4.6-stable_mono_win64.exe"
#>
param(
    [string]$GodotPath = 'C:\Godot\Godot_v4.6-stable_mono_win64.exe',
    [int]$TimeoutSec = 120
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path))
Push-Location $repoRoot

try {
    Write-Host '=== Building C# project ===' -ForegroundColor Cyan
    dotnet build 'Space Trade Empire.csproj' --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

    $outputDir = Join-Path (Join-Path $repoRoot 'reports') 'visual_eval'
    if (Test-Path $outputDir) {
        Get-ChildItem -Path $outputDir | Remove-Item -Recurse -Force
        Write-Host ('Cleared ' + $outputDir) -ForegroundColor Yellow
    } else {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    $stdoutFile = Join-Path $outputDir 'stdout.txt'
    $stderrFile = Join-Path $outputDir 'stderr.txt'

    Write-Host '=== Launching Visual Sweep Bot ===' -ForegroundColor Cyan
    Write-Host ('Godot: ' + $GodotPath)
    Write-Host ('Timeout: ' + $TimeoutSec + 's')

    $proc = Start-Process -FilePath $GodotPath -ArgumentList @(
        '--path', '.', '-s', 'res://scripts/tests/visual_sweep_bot_v0.gd'
    ) -PassThru -RedirectStandardOutput $stdoutFile -RedirectStandardError $stderrFile

    # Push Godot window behind the current window so it doesn't steal focus.
    # Godot still renders when occluded (just not when minimized).
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

    $exited = $proc.WaitForExit($TimeoutSec * 1000)
    if (-not $exited) {
        Write-Host ('TIMEOUT after ' + $TimeoutSec + 's -- killing process') -ForegroundColor Red
        $proc.Kill()
        $proc.WaitForExit(5000)
    }

    Write-Host ''
    Write-Host '=== Bot Output ===' -ForegroundColor Cyan
    if (Test-Path $stdoutFile) {
        Get-Content $stdoutFile | ForEach-Object {
            if ($_ -match '^VSWP') { Write-Host $_ }
        }
    }

    if (Test-Path $stderrFile) {
        $stderrContent = Get-Content $stderrFile -Raw
        if ($stderrContent -and $stderrContent.Trim().Length -gt 0) {
            Write-Host ''
            Write-Host '=== Stderr ===' -ForegroundColor Yellow
            Write-Host $stderrContent
        }
    }

    Write-Host ''
    Write-Host '=== Screenshots ===' -ForegroundColor Cyan
    $pngs = Get-ChildItem -Path $outputDir -Filter '*.png' -ErrorAction SilentlyContinue
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

    $summaryPath = Join-Path $outputDir 'summary.json'
    if (Test-Path $summaryPath) {
        Write-Host ''
        Write-Host '=== Summary ===' -ForegroundColor Cyan
        $summary = Get-Content $summaryPath -Raw | ConvertFrom-Json
        Write-Host ('  Snapshots: ' + $summary.snapshot_count)
        Write-Host ('  Critical failures: ' + $summary.critical_failures)
    }

    Write-Host ''
    Write-Host ('Output directory: ' + $outputDir) -ForegroundColor Green
    Write-Host 'Screenshots will be auto-cleaned on next run.' -ForegroundColor DarkGray

    # Write evaluation prompt file for Claude Code to pick up
    $evalPromptPath = Join-Path $outputDir 'eval_prompt.txt'
    $pngList = ''
    if ($pngs) {
        foreach ($png in $pngs | Sort-Object Name) {
            $pngList += ('  - ' + $png.FullName + "`n")
        }
    }
    $evalGuide = Join-Path (Join-Path (Join-Path $repoRoot 'scripts') 'tools') 'visual_eval_guide.md'
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
    Set-Content -Path $evalPromptPath -Value $promptText -Encoding UTF8
    Write-Host ''
    Write-Host '=== Ready for Evaluation ===' -ForegroundColor Cyan
    Write-Host 'Ask Claude: "evaluate the visual sweep screenshots"' -ForegroundColor Green

} finally {
    Pop-Location
}
