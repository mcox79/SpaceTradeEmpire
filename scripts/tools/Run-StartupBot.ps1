<#
.SYNOPSIS
  Startup Flow Bot runner. Verifies Main Menu → New Voyage → Galaxy Gen →
  Welcome Overlay → Cinematic Descent → Playable State.
.EXAMPLE
  .\scripts\tools\Run-StartupBot.ps1 -Mode headless
  .\scripts\tools\Run-StartupBot.ps1 -Mode visual -Seed 42
#>
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('headless','visual')]
    [string]$Mode,
    [int]$Seed = -1,
    [int]$TimeoutSec = 0
)

$ErrorActionPreference = 'Stop'

# Source shared helpers
. (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'common.ps1')

$repoRoot = Get-RepoRoot
$godotExe = Get-GodotExe -RepoRoot $repoRoot
Push-Location $repoRoot

try {
    $defaultTimeout = @{
        'headless' = 60
        'visual'   = 90
    }
    $activeTimeout = if ($TimeoutSec -gt 0) { $TimeoutSec } else { $defaultTimeout[$Mode] }
    $outputDir = Join-Path $repoRoot 'reports/startup_flow'

    # ── Step 1: Build ──
    Write-Host '=== Building C# project ===' -ForegroundColor Cyan
    dotnet build 'Space Trade Empire.csproj' --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

    # ── Step 2: Clean output directory ──
    if (Test-Path $outputDir) {
        Get-ChildItem -Path $outputDir -File | Remove-Item -Force
        Write-Host ('Cleared ' + $outputDir) -ForegroundColor Yellow
    } else {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    $stdoutFile = Join-Path $outputDir 'stdout.txt'
    $stderrFile = Join-Path $outputDir 'stderr.txt'

    # ── Step 3: Build Godot arguments ──
    $script = 'res://scripts/tests/test_startup_flow_v0.gd'
    $godotArgs = @('--path', '.', '-s', $script)

    if ($Mode -eq 'headless') {
        $godotArgs = @('--headless') + $godotArgs
    }

    if ($Seed -ge 0) {
        $godotArgs += @('--', "--seed=$Seed")
    }

    # ── Step 4: Launch Godot ──
    $seedLabel = if ($Seed -ge 0) { " seed=$Seed" } else { '' }
    Write-Host "=== Launching Startup Flow Bot ($Mode mode$seedLabel) ===" -ForegroundColor Cyan
    Write-Host ('Godot: ' + $godotExe)
    Write-Host ('Script: ' + $script)
    Write-Host ('Timeout: ' + $activeTimeout + 's')

    $procArgs = @{
        FilePath = $godotExe
        ArgumentList = $godotArgs
        PassThru = $true
        RedirectStandardOutput = $stdoutFile
        RedirectStandardError = $stderrFile
    }
    if ($Mode -eq 'headless') {
        $procArgs['WindowStyle'] = 'Hidden'
    }
    $proc = Start-Process @procArgs

    if ($Mode -eq 'visual') {
        Start-Sleep -Milliseconds 500
        Add-Type -Name NativeMethods -Namespace Win32SU -MemberDefinition @'
            [DllImport("user32.dll")] public static extern bool SetForegroundWindow(System.IntPtr hWnd);
            [DllImport("kernel32.dll")] public static extern System.IntPtr GetConsoleWindow();
'@
        $consoleHwnd = [Win32SU.NativeMethods]::GetConsoleWindow()
        if ($consoleHwnd -ne [System.IntPtr]::Zero) {
            [Win32SU.NativeMethods]::SetForegroundWindow($consoleHwnd) | Out-Null
        }
        Write-Host 'Godot launched in background. Waiting for completion...' -ForegroundColor DarkGray
    }

    $exited = $proc.WaitForExit($activeTimeout * 1000)
    if (-not $exited) {
        Write-Host ('TIMEOUT after ' + $activeTimeout + 's -- killing process') -ForegroundColor Red
        $proc.Kill()
        $proc.WaitForExit(5000)
    }

    # ── Step 5: Parse output ──
    Write-Host ''
    Write-Host '=== Bot Output ===' -ForegroundColor Cyan
    $passLine = $null
    $failLine = $null
    $assertCount = 0
    $assertPassCount = 0
    if (Test-Path $stdoutFile) {
        Get-Content $stdoutFile | ForEach-Object {
            if ($_ -match '^SU1\|') {
                Write-Host $_
                if ($_ -match '^SU1\|PASS') { $passLine = $_ }
                if ($_ -match '^SU1\|FAIL') { $failLine = $_ }
                if ($_ -match '^SU1\|ASSERT_') { $assertCount++ }
                if ($_ -match '^SU1\|ASSERT_PASS') { $assertPassCount++ }
            }
        }
    }

    # ── Step 6: Check for errors ──
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

    # ── Step 7: Screenshots ──
    if ($Mode -eq 'visual') {
        Write-Host ''
        Write-Host '=== Screenshots ===' -ForegroundColor Cyan
        $pngs = Get-ChildItem -Path $outputDir -Filter '*.png' -ErrorAction SilentlyContinue
        if ($pngs.Count -eq 0) {
            Write-Host 'No screenshots captured.' -ForegroundColor DarkGray
        } else {
            foreach ($png in $pngs | Sort-Object Name) {
                $sizeKB = [math]::Round($png.Length / 1024, 1)
                Write-Host ('  ' + $png.Name + '  (' + $sizeKB + ' KB)')
            }
            Write-Host ''
            Write-Host ('Total: ' + $pngs.Count + ' screenshots') -ForegroundColor Green
        }
    }

    # ── Step 8: Verdict ──
    Write-Host ''
    Write-Host ('Assertions: ' + $assertPassCount + '/' + $assertCount + ' passed') -ForegroundColor Cyan
    if ($failLine) {
        Write-Host '=== FAIL ===' -ForegroundColor Red
        Write-Host $failLine
        Write-Host ''
        Write-Host ('Output directory: ' + $outputDir) -ForegroundColor Yellow
        exit 1
    } elseif ($passLine) {
        Write-Host '=== PASS ===' -ForegroundColor Green
        Write-Host ''
        Write-Host ('Output directory: ' + $outputDir) -ForegroundColor Green
        exit 0
    } else {
        Write-Host '=== NO VERDICT ===' -ForegroundColor Yellow
        Write-Host 'Bot did not produce PASS or FAIL. Check stdout.txt and stderr.txt.' -ForegroundColor Yellow
        Write-Host ''
        Write-Host ('Output directory: ' + $outputDir) -ForegroundColor Yellow
        exit 1
    }

} finally {
    Pop-Location
}
