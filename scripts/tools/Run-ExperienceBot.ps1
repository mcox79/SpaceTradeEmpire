<#
.SYNOPSIS
  First-Hour Experience Bot runner. Plays the game autonomously for ~60
  game-minutes and scores metrics across 12 dimensions, detects issues,
  and produces ranked prescriptions with file references.

.EXAMPLE
  .\scripts\tools\Run-ExperienceBot.ps1 -Mode headless
  .\scripts\tools\Run-ExperienceBot.ps1 -Mode visual -Seed 42
  .\scripts\tools\Run-ExperienceBot.ps1 -Mode headless -Sweep
  .\scripts\tools\Run-ExperienceBot.ps1 -Mode headless -Sweep -AllArchetypes
  .\scripts\tools\Run-ExperienceBot.ps1 -Mode headless -Archetype explorer -Seed 42
#>
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('headless','visual')]
    [string]$Mode,
    [int]$Seed = -1,
    [ValidateSet('balanced','trader','explorer','fighter')]
    [string]$Archetype = 'balanced',
    [switch]$Sweep,            # Run 5 seeds
    [switch]$AllArchetypes,    # Run all 4 archetypes (with -Sweep)
    [int]$TimeoutSec = 0       # 0 = auto
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

    # ── Determine run matrix ──
    $defaultTimeout = @{ 'headless' = 120; 'visual' = 360 }
    $activeTimeout = if ($TimeoutSec -gt 0) { $TimeoutSec } else { $defaultTimeout[$Mode] }

    $seeds = @()
    $archetypes = @()

    if ($Sweep) {
        $seeds = @(42, 99, 1001, 31337, 77777)
    } elseif ($Seed -ge 0) {
        $seeds = @($Seed)
    } else {
        $seeds = @(42)
    }

    if ($AllArchetypes) {
        $archetypes = @('balanced', 'trader', 'explorer', 'fighter')
    } else {
        $archetypes = @($Archetype)
    }

    $script = 'res://scripts/tests/test_fh_experience_v0.gd'
    $outputBase = Join-Path $repoRoot 'reports/experience'
    $allResults = @()
    $allIssues = @()
    $failCount = 0

    foreach ($arch in $archetypes) {
        foreach ($s in $seeds) {
            $runLabel = "$arch/seed_$s"
            $outputDir = Join-Path $outputBase $runLabel
            if (Test-Path $outputDir) {
                Get-ChildItem -Path $outputDir -File | Remove-Item -Force
            } else {
                New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
            }

            $stdoutFile = Join-Path $outputDir 'stdout.txt'
            $stderrFile = Join-Path $outputDir 'stderr.txt'

            # Build Godot arguments
            $godotArgs = @('--path', '.', '-s', $script, '--', "--seed=$s", "--archetype=$arch")
            if ($Mode -eq 'headless') {
                $godotArgs = @('--headless') + $godotArgs
            }

            Write-Host ''
            Write-Host "=== Run: $runLabel ($Mode) ===" -ForegroundColor Cyan

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
                    Add-Type -Name NativeMethods -Namespace Win32EXP -MemberDefinition @'
                        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(System.IntPtr hWnd);
                        [DllImport("kernel32.dll")] public static extern System.IntPtr GetConsoleWindow();
'@ -ErrorAction SilentlyContinue
                } catch { }
                $consoleHwnd = [Win32EXP.NativeMethods]::GetConsoleWindow()
                if ($consoleHwnd -ne [System.IntPtr]::Zero) {
                    [Win32EXP.NativeMethods]::SetForegroundWindow($consoleHwnd) | Out-Null
                }
            }

            $exited = $proc.WaitForExit($activeTimeout * 1000)
            if (-not $exited) {
                Write-Host ('TIMEOUT after ' + $activeTimeout + 's -- killing') -ForegroundColor Red
                $proc.Kill()
                $proc.WaitForExit(5000)
            }

            # ── Parse output ──
            $passLine = $null
            $failLine = $null
            $scoreLines = @()
            $issueLines = @()
            $issueSummaryLine = $null
            if (Test-Path $stdoutFile) {
                Get-Content $stdoutFile | ForEach-Object {
                    if ($_ -match '^EXP\|') {
                        if ($_ -match 'SCORE\|') { $scoreLines += $_ }
                        if ($_ -match '^EXP\|ISSUE\|') { $issueLines += $_ }
                        if ($_ -match '^EXP\|ISSUE_SUMMARY\|') { $issueSummaryLine = $_ }
                        if ($_ -match '^EXP\|PASS') { $passLine = $_ }
                        if ($_ -match '^EXP\|FAIL') { $failLine = $_ }
                    }
                }
            }

            # ── Display scores ──
            foreach ($sl in $scoreLines) {
                $text = $sl -replace '^EXP\|SCORE\|', '  '
                Write-Host $text
            }

            # ── Display issues (color-coded by severity) ──
            if ($issueLines.Count -gt 0) {
                Write-Host ''
                Write-Host "  --- Issues ($($issueLines.Count)) ---" -ForegroundColor Yellow
                foreach ($il in $issueLines) {
                    # Format: EXP|ISSUE|SEVERITY|CATEGORY|description|fix=...|file=...
                    $parts = ($il -replace '^EXP\|ISSUE\|', '') -split '\|'
                    $severity = if ($parts.Count -ge 1) { $parts[0] } else { '?' }
                    $category = if ($parts.Count -ge 2) { $parts[1] } else { '?' }
                    $desc     = if ($parts.Count -ge 3) { $parts[2] } else { '' }
                    $fix      = if ($parts.Count -ge 4) { $parts[3] -replace '^fix=', '' } else { '' }
                    $file     = if ($parts.Count -ge 5) { $parts[4] -replace '^file=', '' } else { '' }

                    $sevColor = switch ($severity) {
                        'CRITICAL' { 'Red' }
                        'MAJOR'    { 'Yellow' }
                        'MINOR'    { 'DarkYellow' }
                        default    { 'Gray' }
                    }
                    $sevIcon = switch ($severity) {
                        'CRITICAL' { '[!!!]' }
                        'MAJOR'    { '[!! ]' }
                        'MINOR'    { '[!  ]' }
                        default    { '[?  ]' }
                    }

                    Write-Host "  $sevIcon " -ForegroundColor $sevColor -NoNewline
                    Write-Host "${category}: " -ForegroundColor White -NoNewline
                    Write-Host $desc -ForegroundColor $sevColor
                    if ($fix) {
                        Write-Host "        Rx: $fix" -ForegroundColor DarkCyan
                    }
                    if ($file) {
                        Write-Host "        File: $file" -ForegroundColor DarkGray
                    }

                    # Collect for aggregate
                    $allIssues += [PSCustomObject]@{
                        Run      = $runLabel
                        Severity = $severity
                        Category = $category
                        Desc     = $desc
                        Fix      = $fix
                        File     = $file
                    }
                }
            }

            # ── Check errors ──
            if (Test-Path $stderrFile) {
                $stderrContent = Get-Content $stderrFile -Raw
                if ($stderrContent -and $stderrContent.Trim().Length -gt 0) {
                    $errorLines = ($stderrContent -split "`n") | Where-Object { $_ -match 'SCRIPT ERROR|Parse Error|ERROR' }
                    if ($errorLines) {
                        Write-Host '  Errors:' -ForegroundColor Red
                        $errorLines | ForEach-Object { Write-Host "    $_" }
                    }
                }
            }

            # ── Screenshots (visual mode) ──
            if ($Mode -eq 'visual') {
                $defaultScreenDir = Join-Path $outputBase 'screenshots'
                if (Test-Path $defaultScreenDir) {
                    $pngs = Get-ChildItem -Path $defaultScreenDir -Filter '*.png' -ErrorAction SilentlyContinue
                    if ($pngs.Count -gt 0) {
                        Write-Host "  Screenshots: $($pngs.Count) captured" -ForegroundColor Green
                    }
                }
            }

            # ── JSON report ──
            $reportJson = Join-Path $outputDir 'report.json'
            if (Test-Path $reportJson) {
                Write-Host "  Report: $reportJson" -ForegroundColor DarkCyan
            }

            # ── Verdict ──
            $verdict = 'UNKNOWN'
            if ($failLine) { $verdict = 'FAIL'; $failCount++ }
            elseif ($passLine) { $verdict = 'PASS' }
            else { $verdict = 'NO_VERDICT'; $failCount++ }

            $color = switch ($verdict) { 'PASS' { 'Green' } 'FAIL' { 'Red' } default { 'Yellow' } }
            Write-Host "  Verdict: $verdict" -ForegroundColor $color

            $runIssueCount = $issueLines.Count
            $allResults += [PSCustomObject]@{
                Archetype = $arch
                Seed      = $s
                Verdict   = $verdict
                Issues    = $runIssueCount
                Output    = $outputDir
            }
        }
    }

    # ── Aggregate Report ──
    Write-Host ''
    Write-Host '=== Results ===' -ForegroundColor Magenta
    Write-Host ''
    $allResults | Format-Table Archetype, Seed, Verdict, Issues -AutoSize | Out-String | Write-Host
    $passCount = @($allResults | Where-Object { $_.Verdict -eq 'PASS' }).Count
    $totalCount = @($allResults).Count
    Write-Host "$passCount/$totalCount PASS" -ForegroundColor $(if ($failCount -eq 0) { 'Green' } else { 'Yellow' })

    # ── Aggregate Issue Summary (cross-seed/archetype) ──
    if (@($allIssues).Count -gt 0) {
        Write-Host ''
        Write-Host '=== Issue Summary (all runs) ===' -ForegroundColor Yellow
        Write-Host ''

        # Deduplicate by description, count occurrences
        $grouped = @{}
        foreach ($issue in $allIssues) {
            $key = "$($issue.Severity)|$($issue.Category)|$($issue.Desc)"
            if (-not $grouped.ContainsKey($key)) {
                $grouped[$key] = [PSCustomObject]@{
                    Severity = $issue.Severity
                    Category = $issue.Category
                    Desc     = $issue.Desc
                    Fix      = $issue.Fix
                    File     = $issue.File
                    Count    = 1
                    Runs     = @($issue.Run)
                }
            } else {
                $grouped[$key].Count++
                $grouped[$key].Runs += $issue.Run
            }
        }

        # Sort by severity then count
        $sevOrder = @{ 'CRITICAL' = 0; 'MAJOR' = 1; 'MINOR' = 2 }
        $sorted = $grouped.Values | Sort-Object @{Expression={$sevOrder[$_.Severity]}}, @{Expression={$_.Count};Descending=$true}

        $critTotal = ($sorted | Where-Object { $_.Severity -eq 'CRITICAL' }).Count
        $majTotal  = ($sorted | Where-Object { $_.Severity -eq 'MAJOR' }).Count
        $minTotal  = ($sorted | Where-Object { $_.Severity -eq 'MINOR' }).Count

        Write-Host "  CRITICAL: $critTotal  MAJOR: $majTotal  MINOR: $minTotal" -ForegroundColor $(if ($critTotal -gt 0) { 'Red' } elseif ($majTotal -gt 0) { 'Yellow' } else { 'Green' })
        Write-Host ''

        $idx = 1
        foreach ($issue in $sorted) {
            $sevColor = switch ($issue.Severity) {
                'CRITICAL' { 'Red' }
                'MAJOR'    { 'Yellow' }
                default    { 'DarkYellow' }
            }
            $freq = if ($totalCount -gt 1) { " ($($issue.Count)/$($totalCount) runs)" } else { '' }
            Write-Host "  $idx. [$($issue.Severity)] $($issue.Category): $($issue.Desc)$freq" -ForegroundColor $sevColor
            Write-Host "     Rx: $($issue.Fix)" -ForegroundColor DarkCyan
            Write-Host "     File: $($issue.File)" -ForegroundColor DarkGray
            $idx++
        }
    } else {
        Write-Host ''
        Write-Host '=== No issues detected ===' -ForegroundColor Green
    }

    Write-Host ''
    Write-Host ('Reports: ' + $outputBase) -ForegroundColor Cyan
    exit $(if ($failCount -eq 0) { 0 } else { 1 })

} finally {
    Pop-Location
}
