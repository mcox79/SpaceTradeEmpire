<#
.SYNOPSIS
  Issue Verification Bot runner. Runs targeted probes to confirm/refute audit findings.
  Emits VFY|VERIFY lines for each probe: CONFIRMED, UNCONFIRMED, or SKIP.

.EXAMPLE
  .\scripts\tools\Run-VerifyIssues.ps1 -Mode headless
  .\scripts\tools\Run-VerifyIssues.ps1 -Mode visual -Seed 42
  .\scripts\tools\Run-VerifyIssues.ps1 -Mode headless -Seed 42 -TimeoutSec 120
#>
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('headless','visual')]
    [string]$Mode,
    [int]$Seed = 42,
    [int]$TimeoutSec = 0
)

$ErrorActionPreference = 'Stop'

# Source shared helpers
. (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'common.ps1')

$repoRoot = Get-RepoRoot
$godotExe = Get-GodotExe -RepoRoot $repoRoot
Push-Location $repoRoot

try {
    # ── Build ──
    Write-Host '=== Building C# project ===' -ForegroundColor Cyan
    dotnet build 'Space Trade Empire.csproj' --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

    # ── Config ──
    $defaultTimeout = @{ 'headless' = 120; 'visual' = 180 }
    $activeTimeout = if ($TimeoutSec -gt 0) { $TimeoutSec } else { $defaultTimeout[$Mode] }

    $script = 'res://scripts/tests/test_verify_issues_v0.gd'
    $outputDir = Join-Path $repoRoot 'reports/verification'

    if (Test-Path $outputDir) {
        Get-ChildItem -Path $outputDir -File | Remove-Item -Force
    } else {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    $stdoutFile = Join-Path $outputDir 'stdout.txt'
    $stderrFile = Join-Path $outputDir 'stderr.txt'

    # ── Godot args ──
    $godotArgs = @('--path', '.', '-s', $script, '--', "--seed=$Seed")
    if ($Mode -eq 'headless') {
        $godotArgs = @('--headless') + $godotArgs
    } else {
        $godotArgs = @('--resolution', '1920x1080') + $godotArgs
    }

    # ── Launch ──
    Write-Host "=== Verification Bot ($Mode, seed=$Seed) ===" -ForegroundColor Cyan
    Write-Host "Timeout: ${activeTimeout}s"

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

    # Push Godot behind console in visual mode
    if ($Mode -eq 'visual') {
        Start-Sleep -Milliseconds 500
        try {
            Add-Type -Name NativeMethods -Namespace Win32VFY -MemberDefinition @'
                [DllImport("user32.dll")] public static extern bool SetForegroundWindow(System.IntPtr hWnd);
                [DllImport("kernel32.dll")] public static extern System.IntPtr GetConsoleWindow();
'@ -ErrorAction SilentlyContinue
        } catch { }
        $consoleHwnd = [Win32VFY.NativeMethods]::GetConsoleWindow()
        if ($consoleHwnd -ne [System.IntPtr]::Zero) {
            [Win32VFY.NativeMethods]::SetForegroundWindow($consoleHwnd) | Out-Null
        }
    }

    $exited = $proc.WaitForExit($activeTimeout * 1000)
    if (-not $exited) {
        Write-Host "TIMEOUT after ${activeTimeout}s -- killing" -ForegroundColor Red
        $proc.Kill()
        $proc.WaitForExit(5000)
    }

    # ── Parse output ──
    Write-Host ''
    Write-Host '=== Verification Results ===' -ForegroundColor Cyan

    $confirmed = 0
    $unconfirmed = 0
    $skipped = 0
    $verifyLines = @()

    if (Test-Path $stdoutFile) {
        Get-Content $stdoutFile | ForEach-Object {
            if ($_ -match '^VFY\|VERIFY\|') {
                $parts = ($_ -replace '^VFY\|VERIFY\|', '') -split '\|'
                $probe  = if ($parts.Count -ge 1) { $parts[0] } else { '?' }
                $status = if ($parts.Count -ge 2) { $parts[1] } else { '?' }
                $evidence = if ($parts.Count -ge 3) { $parts[2] } else { '' }

                $color = switch ($status) {
                    'CONFIRMED'   { 'Green' }
                    'UNCONFIRMED' { 'Red' }
                    'SKIP'        { 'DarkGray' }
                    default       { 'Yellow' }
                }
                $icon = switch ($status) {
                    'CONFIRMED'   { '[OK]' }
                    'UNCONFIRMED' { '[!!]' }
                    'SKIP'        { '[--]' }
                    default       { '[??]' }
                }

                Write-Host "  $icon " -ForegroundColor $color -NoNewline
                Write-Host "${probe}: " -ForegroundColor White -NoNewline
                Write-Host $evidence -ForegroundColor $color

                switch ($status) {
                    'CONFIRMED'   { $confirmed++ }
                    'UNCONFIRMED' { $unconfirmed++ }
                    'SKIP'        { $skipped++ }
                }

                $verifyLines += [PSCustomObject]@{
                    Probe    = $probe
                    Status   = $status
                    Evidence = $evidence
                }
            }
        }
    }

    # ── Check errors ──
    if (Test-Path $stderrFile) {
        $stderrContent = Get-Content $stderrFile -Raw
        if ($stderrContent -and $stderrContent.Trim().Length -gt 0) {
            $errorLines = ($stderrContent -split "`n") | Where-Object { $_ -match 'SCRIPT ERROR|Parse Error|ERROR' }
            if ($errorLines) {
                Write-Host ''
                Write-Host '=== Errors ===' -ForegroundColor Red
                $errorLines | ForEach-Object { Write-Host "  $_" }
            }
        }
    }

    # ── Summary ──
    $total = $confirmed + $unconfirmed + $skipped
    Write-Host ''
    Write-Host "=== Summary ===" -ForegroundColor Magenta
    Write-Host "  Confirmed:   $confirmed" -ForegroundColor Green
    Write-Host "  Unconfirmed: $unconfirmed" -ForegroundColor $(if ($unconfirmed -gt 0) { 'Red' } else { 'Green' })
    Write-Host "  Skipped:     $skipped" -ForegroundColor DarkGray
    Write-Host "  Total:       $total"

    # ── Screenshots ──
    $pngs = @(Get-ChildItem -Path $outputDir -Filter '*.png' -ErrorAction SilentlyContinue)
    if ($pngs.Count -gt 0) {
        Write-Host ''
        Write-Host "=== Screenshots ($($pngs.Count)) ===" -ForegroundColor Cyan
        foreach ($png in $pngs | Sort-Object Name) {
            $sizeKB = [math]::Round($png.Length / 1024, 1)
            Write-Host "  $($png.Name)  ($sizeKB KB)"
        }
    }

    Write-Host ''
    Write-Host "Output: $outputDir" -ForegroundColor Green

    # ── Verdict ──
    $exitCode = 0
    if (Test-Path $stdoutFile) {
        $failLine = Get-Content $stdoutFile | Where-Object { $_ -match '^VFY\|FAIL' }
        if ($failLine) { $exitCode = 1 }
    }

    exit $exitCode

} finally {
    Pop-Location
}
