# Run-AuditTwoPass.ps1 — Automated two-pass audit cycle
# Pass 1: Run audit quick + experience bot -> compile findings
# Pass 2: After fixes applied, re-run affected checks -> produce delta
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts/tools/Run-AuditTwoPass.ps1
#   powershell -ExecutionPolicy Bypass -File scripts/tools/Run-AuditTwoPass.ps1 -Mode first-hour
#   powershell -ExecutionPolicy Bypass -File scripts/tools/Run-AuditTwoPass.ps1 -Pass2Only
#   powershell -ExecutionPolicy Bypass -File scripts/tools/Run-AuditTwoPass.ps1 -Seed 99
param(
    [ValidateSet('quick','first-hour')]
    [string]$Mode = "quick",
    [int]$Seed = 42,
    [switch]$Pass2Only  # Skip pass 1, just run verification
)

$ErrorActionPreference = "Continue"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)

$twoPassDir = Join-Path $repoRoot "reports/audit/twopass"
$pass1Dir = Join-Path $twoPassDir "pass1"
$pass2Dir = Join-Path $twoPassDir "pass2"

# ── Helper: Parse audit output for key metrics ──
function Parse-AuditOutput {
    param([string]$RawOutput)
    $result = @{
        BuildOk       = $false
        TestOk        = $false
        TestSummary   = ""
        CriticalCount = 0
        Exercised     = 0
        UiOnly        = 0
        Uncalled      = 0
        TotalMethods  = 0
        CoveragePct   = 0
    }
    if ($RawOutput -match "BUILD OK") { $result.BuildOk = $true }
    if ($RawOutput -match "TESTS OK") { $result.TestOk = $true }
    if ($RawOutput -match "Passed!\s*-\s*Failed:\s*0.*Total:\s*(\d+)") {
        $result.TestSummary = $Matches[0]
    } elseif ($RawOutput -match "(Passed!.+)") {
        $result.TestSummary = $Matches[1].Trim()
    }
    if ($RawOutput -match "CRITICAL findings:\s*(\d+)") {
        $result.CriticalCount = [int]$Matches[1]
    } elseif ($RawOutput -match "CRITICAL=(\d+)") {
        $result.CriticalCount = [int]$Matches[1]
    }
    if ($RawOutput -match "COVERAGE\|TOTAL=(\d+)\|EXERCISED=(\d+)\|PCT=([\d\.]+)\|UI_ONLY=(\d+)\|UNCALLED=(\d+)") {
        $result.TotalMethods = [int]$Matches[1]
        $result.Exercised    = [int]$Matches[2]
        $result.CoveragePct  = [double]$Matches[3]
        $result.UiOnly       = [int]$Matches[4]
        $result.Uncalled     = [int]$Matches[5]
    }
    return $result
}

# ── Helper: Parse experience bot output for key metrics ──
function Parse-ExperienceOutput {
    param([string]$RawOutput)
    $result = @{
        Verdict       = "UNKNOWN"
        AssertPass    = 0
        AssertFail    = 0
        FlagCount     = 0
        Flags         = @()
        IssuesCritical= 0
        IssuesMajor   = 0
        IssuesMinor   = 0
        FpsAvg        = ""
    }
    if ($RawOutput -match "EXP\|PASS") { $result.Verdict = "PASS" }
    elseif ($RawOutput -match "EXP\|FAIL") { $result.Verdict = "FAIL" }
    # Also check FH1 prefixes for first-hour bot
    $result.AssertPass = ([regex]::Matches($RawOutput, "(?:EXP|FH1)\|ASSERT_PASS")).Count
    $result.AssertFail = ([regex]::Matches($RawOutput, "(?:EXP|FH1)\|ASSERT_FAIL")).Count
    $flagMatches = [regex]::Matches($RawOutput, "(?:EXP|FH1)\|FLAG\|(.+)")
    $result.FlagCount = $flagMatches.Count
    foreach ($fm in $flagMatches) { $result.Flags += $fm.Groups[1].Value.Trim() }
    # Issue counts
    $result.IssuesCritical = ([regex]::Matches($RawOutput, "EXP\|ISSUE\|CRITICAL")).Count
    $result.IssuesMajor   = ([regex]::Matches($RawOutput, "EXP\|ISSUE\|MAJOR")).Count
    $result.IssuesMinor   = ([regex]::Matches($RawOutput, "EXP\|ISSUE\|MINOR")).Count
    # FPS
    if ($RawOutput -match "EXP\|PERF\|.*avg=([\d\.]+)") { $result.FpsAvg = $Matches[1] }
    return $result
}

# ── Helper: Save metrics to JSON ──
function Save-Metrics {
    param([string]$OutDir, [hashtable]$Audit, [hashtable]$Experience)
    $data = @{
        timestamp  = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
        audit      = $Audit
        experience = $Experience
    }
    $jsonPath = Join-Path $OutDir "metrics.json"
    $data | ConvertTo-Json -Depth 4 | Set-Content $jsonPath -Encoding UTF8
    return $jsonPath
}

# ── Run a single pass ──
function Run-Pass {
    param([string]$PassName, [string]$PassDir)

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "=== $PassName ===" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    $passStart = Get-Date

    if (-not (Test-Path $PassDir)) { New-Item -ItemType Directory -Path $PassDir -Force | Out-Null }

    # ── Step A: Run AuditQuick ──
    Write-Host ""
    Write-Host "--- $PassName Step A: Audit Quick ---" -ForegroundColor Yellow
    $auditScript = Join-Path $repoRoot "scripts/tools/Run-AuditQuick.ps1"
    $auditRaw = & powershell -ExecutionPolicy Bypass -File $auditScript 2>&1 | Out-String
    $auditRaw | Set-Content (Join-Path $PassDir "audit_quick_output.txt") -Encoding UTF8
    $auditMetrics = Parse-AuditOutput $auditRaw

    Write-Host "  Build: $(if ($auditMetrics.BuildOk) { 'OK' } else { 'FAILED' })"
    Write-Host "  Tests: $(if ($auditMetrics.TestOk) { 'OK' } else { 'FAILED' }) $($auditMetrics.TestSummary)"
    Write-Host "  Critical findings: $($auditMetrics.CriticalCount)"
    Write-Host "  Coverage: $($auditMetrics.Exercised)/$($auditMetrics.TotalMethods) ($($auditMetrics.CoveragePct)%)"
    Write-Host "  UI-only: $($auditMetrics.UiOnly)  Uncalled: $($auditMetrics.Uncalled)"

    # ── Step B: Run Experience Bot (mode-dependent) ──
    $expMetrics = @{
        Verdict        = "SKIPPED"
        AssertPass     = 0
        AssertFail     = 0
        FlagCount      = 0
        Flags          = @()
        IssuesCritical = 0
        IssuesMajor    = 0
        IssuesMinor    = 0
        FpsAvg         = ""
    }

    if ($Mode -eq "first-hour") {
        Write-Host ""
        Write-Host "--- $PassName Step B: Experience Bot (seed=$Seed) ---" -ForegroundColor Yellow
        $expScript = Join-Path $repoRoot "scripts/tools/Run-ExperienceBot.ps1"
        if (Test-Path $expScript) {
            $expRaw = & powershell -ExecutionPolicy Bypass -File $expScript -Mode headless -Seed $Seed 2>&1 | Out-String
            $expRaw | Set-Content (Join-Path $PassDir "experience_bot_output.txt") -Encoding UTF8
            $expMetrics = Parse-ExperienceOutput $expRaw

            Write-Host "  Verdict: $($expMetrics.Verdict)"
            Write-Host "  Assertions: $($expMetrics.AssertPass) pass, $($expMetrics.AssertFail) fail"
            Write-Host "  Flags: $($expMetrics.FlagCount)"
            Write-Host "  Issues: CRIT=$($expMetrics.IssuesCritical) MAJ=$($expMetrics.IssuesMajor) MIN=$($expMetrics.IssuesMinor)"
            if ($expMetrics.FpsAvg) { Write-Host "  FPS avg: $($expMetrics.FpsAvg)" }
        } else {
            Write-Host "  SKIPPED -- Run-ExperienceBot.ps1 not found"
        }
    } else {
        Write-Host ""
        Write-Host "--- $PassName Step B: Experience Bot SKIPPED (quick mode) ---" -ForegroundColor DarkGray
    }

    # ── Save metrics ──
    $jsonPath = Save-Metrics -OutDir $PassDir -Audit $auditMetrics -Experience $expMetrics

    $passElapsed = (Get-Date) - $passStart
    Write-Host ""
    Write-Host "$PassName complete in $([math]::Round($passElapsed.TotalSeconds, 1))s"
    Write-Host "Metrics: $jsonPath"

    return @{ Audit = $auditMetrics; Experience = $expMetrics }
}

# ── Delta comparison ──
function Show-Delta {
    param([hashtable]$P1, [hashtable]$P2)

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Magenta
    Write-Host "=== DELTA: Pass 1 vs Pass 2 ===" -ForegroundColor Magenta
    Write-Host "========================================" -ForegroundColor Magenta
    Write-Host ""

    $a1 = $P1.Audit
    $a2 = $P2.Audit
    $e1 = $P1.Experience
    $e2 = $P2.Experience

    Write-Host "DELTA|metric|pass1|pass2|change|verdict"

    # Build
    $buildChange = if ($a1.BuildOk -eq $a2.BuildOk) { "=" } elseif ($a2.BuildOk) { "FIXED" } else { "REGRESSED" }
    $buildVerdict = if ($a2.BuildOk) { "OK" } else { "FAIL" }
    Write-Host "DELTA|build|$($a1.BuildOk)|$($a2.BuildOk)|$buildChange|$buildVerdict"

    # Tests
    $testChange = if ($a1.TestOk -eq $a2.TestOk) { "=" } elseif ($a2.TestOk) { "FIXED" } else { "REGRESSED" }
    $testVerdict = if ($a2.TestOk) { "OK" } else { "FAIL" }
    Write-Host "DELTA|tests|$($a1.TestOk)|$($a2.TestOk)|$testChange|$testVerdict"

    # Critical findings
    $critDelta = $a2.CriticalCount - $a1.CriticalCount
    $critSign = if ($critDelta -gt 0) { "+$critDelta" } elseif ($critDelta -lt 0) { "$critDelta" } else { "0" }
    $critVerdict = if ($a2.CriticalCount -eq 0) { "CLEAN" } elseif ($critDelta -lt 0) { "IMPROVED" } elseif ($critDelta -gt 0) { "REGRESSED" } else { "UNCHANGED" }
    Write-Host "DELTA|critical_findings|$($a1.CriticalCount)|$($a2.CriticalCount)|$critSign|$critVerdict"

    # Coverage
    $covDelta = [math]::Round($a2.CoveragePct - $a1.CoveragePct, 1)
    $covSign = if ($covDelta -gt 0) { "+$covDelta" } elseif ($covDelta -lt 0) { "$covDelta" } else { "0" }
    $covVerdict = if ($covDelta -gt 0) { "IMPROVED" } elseif ($covDelta -lt 0) { "REGRESSED" } else { "UNCHANGED" }
    Write-Host "DELTA|coverage_pct|$($a1.CoveragePct)|$($a2.CoveragePct)|$covSign|$covVerdict"

    # Total methods
    $methDelta = $a2.TotalMethods - $a1.TotalMethods
    $methSign = if ($methDelta -gt 0) { "+$methDelta" } elseif ($methDelta -lt 0) { "$methDelta" } else { "0" }
    Write-Host "DELTA|total_methods|$($a1.TotalMethods)|$($a2.TotalMethods)|$methSign|INFO"

    # Exercised
    $exDelta = $a2.Exercised - $a1.Exercised
    $exSign = if ($exDelta -gt 0) { "+$exDelta" } elseif ($exDelta -lt 0) { "$exDelta" } else { "0" }
    $exVerdict = if ($exDelta -gt 0) { "IMPROVED" } elseif ($exDelta -lt 0) { "REGRESSED" } else { "UNCHANGED" }
    Write-Host "DELTA|exercised|$($a1.Exercised)|$($a2.Exercised)|$exSign|$exVerdict"

    # UI-only
    $uiDelta = $a2.UiOnly - $a1.UiOnly
    $uiSign = if ($uiDelta -gt 0) { "+$uiDelta" } elseif ($uiDelta -lt 0) { "$uiDelta" } else { "0" }
    $uiVerdict = if ($uiDelta -lt 0) { "IMPROVED" } elseif ($uiDelta -gt 0) { "REGRESSED" } else { "UNCHANGED" }
    Write-Host "DELTA|ui_only|$($a1.UiOnly)|$($a2.UiOnly)|$uiSign|$uiVerdict"

    # Uncalled
    $ucDelta = $a2.Uncalled - $a1.Uncalled
    $ucSign = if ($ucDelta -gt 0) { "+$ucDelta" } elseif ($ucDelta -lt 0) { "$ucDelta" } else { "0" }
    $ucVerdict = if ($ucDelta -lt 0) { "IMPROVED" } elseif ($ucDelta -gt 0) { "REGRESSED" } else { "UNCHANGED" }
    Write-Host "DELTA|uncalled|$($a1.Uncalled)|$($a2.Uncalled)|$ucSign|$ucVerdict"

    # Experience bot metrics (if available)
    if ($e1.Verdict -ne "SKIPPED" -or $e2.Verdict -ne "SKIPPED") {
        Write-Host ""
        Write-Host "--- Experience Bot Delta ---" -ForegroundColor Cyan

        $expVerdChange = if ($e1.Verdict -eq $e2.Verdict) { "=" } elseif ($e2.Verdict -eq "PASS") { "FIXED" } else { "REGRESSED" }
        Write-Host "DELTA|exp_verdict|$($e1.Verdict)|$($e2.Verdict)|$expVerdChange|$(if ($e2.Verdict -eq 'PASS') { 'OK' } else { 'FAIL' })"

        $apDelta = $e2.AssertPass - $e1.AssertPass
        $apSign = if ($apDelta -gt 0) { "+$apDelta" } elseif ($apDelta -lt 0) { "$apDelta" } else { "0" }
        Write-Host "DELTA|exp_assert_pass|$($e1.AssertPass)|$($e2.AssertPass)|$apSign|INFO"

        $afDelta = $e2.AssertFail - $e1.AssertFail
        $afSign = if ($afDelta -gt 0) { "+$afDelta" } elseif ($afDelta -lt 0) { "$afDelta" } else { "0" }
        $afVerdict = if ($e2.AssertFail -eq 0) { "CLEAN" } elseif ($afDelta -lt 0) { "IMPROVED" } elseif ($afDelta -gt 0) { "REGRESSED" } else { "UNCHANGED" }
        Write-Host "DELTA|exp_assert_fail|$($e1.AssertFail)|$($e2.AssertFail)|$afSign|$afVerdict"

        $flagDelta = $e2.FlagCount - $e1.FlagCount
        $flagSign = if ($flagDelta -gt 0) { "+$flagDelta" } elseif ($flagDelta -lt 0) { "$flagDelta" } else { "0" }
        $flagVerdict = if ($flagDelta -lt 0) { "IMPROVED" } elseif ($flagDelta -gt 0) { "REGRESSED" } else { "UNCHANGED" }
        Write-Host "DELTA|exp_flags|$($e1.FlagCount)|$($e2.FlagCount)|$flagSign|$flagVerdict"

        if ($e1.FpsAvg -and $e2.FpsAvg) {
            $fps1 = [double]$e1.FpsAvg
            $fps2 = [double]$e2.FpsAvg
            $fpsDelta = [math]::Round($fps2 - $fps1, 1)
            $fpsSign = if ($fpsDelta -gt 0) { "+$fpsDelta" } elseif ($fpsDelta -lt 0) { "$fpsDelta" } else { "0" }
            $fpsVerdict = if ($fpsDelta -ge 0) { "OK" } else { "REGRESSED" }
            Write-Host "DELTA|exp_fps_avg|$($e1.FpsAvg)|$($e2.FpsAvg)|$fpsSign|$fpsVerdict"
        }

        $issDelta = ($e2.IssuesCritical + $e2.IssuesMajor + $e2.IssuesMinor) - ($e1.IssuesCritical + $e1.IssuesMajor + $e1.IssuesMinor)
        $issSign = if ($issDelta -gt 0) { "+$issDelta" } elseif ($issDelta -lt 0) { "$issDelta" } else { "0" }
        $issVerdict = if ($issDelta -lt 0) { "IMPROVED" } elseif ($issDelta -gt 0) { "REGRESSED" } else { "UNCHANGED" }
        Write-Host "DELTA|exp_issues_total|$($e1.IssuesCritical + $e1.IssuesMajor + $e1.IssuesMinor)|$($e2.IssuesCritical + $e2.IssuesMajor + $e2.IssuesMinor)|$issSign|$issVerdict"
    }

    # Overall verdict
    $improved = 0
    $regressed = 0
    if (-not $a1.BuildOk -and $a2.BuildOk) { $improved++ }
    if ($a1.BuildOk -and -not $a2.BuildOk) { $regressed++ }
    if (-not $a1.TestOk -and $a2.TestOk) { $improved++ }
    if ($a1.TestOk -and -not $a2.TestOk) { $regressed++ }
    if ($critDelta -lt 0) { $improved++ }
    if ($critDelta -gt 0) { $regressed++ }
    if ($covDelta -gt 0) { $improved++ }
    if ($covDelta -lt 0) { $regressed++ }
    if ($e1.AssertFail -gt 0 -and $e2.AssertFail -eq 0) { $improved++ }
    if ($e1.AssertFail -eq 0 -and $e2.AssertFail -gt 0) { $regressed++ }

    Write-Host ""
    $overallVerdict = if ($regressed -gt 0) { "REGRESSIONS_FOUND" } elseif ($improved -gt 0) { "IMPROVED" } else { "NO_CHANGE" }
    $overallColor = switch ($overallVerdict) { "REGRESSIONS_FOUND" { "Red" } "IMPROVED" { "Green" } default { "Yellow" } }
    Write-Host "TWOPASS_VERDICT|improved=$improved|regressed=$regressed|verdict=$overallVerdict" -ForegroundColor $overallColor

    # Save delta report
    $deltaData = @{
        timestamp = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
        improved  = $improved
        regressed = $regressed
        verdict   = $overallVerdict
    }
    $deltaPath = Join-Path $twoPassDir "delta.json"
    $deltaData | ConvertTo-Json -Depth 4 | Set-Content $deltaPath -Encoding UTF8
    Write-Host "Delta report: $deltaPath"
}

# ── Main ──
$startTime = Get-Date
Write-Host "=== TWO-PASS AUDIT ===" -ForegroundColor Cyan
Write-Host "Mode: $Mode  Seed: $Seed  Pass2Only: $Pass2Only"
Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

if (-not $Pass2Only) {
    # ── Pass 1 ──
    $p1Results = Run-Pass -PassName "PASS 1 (Baseline)" -PassDir $pass1Dir

    Write-Host ""
    Write-Host "=== Pass 1 Complete ===" -ForegroundColor Green
    Write-Host "Apply fixes, then re-run with -Pass2Only to compare."
    Write-Host "  powershell -ExecutionPolicy Bypass -File scripts/tools/Run-AuditTwoPass.ps1 -Mode $Mode -Seed $Seed -Pass2Only"
    Write-Host ""
} else {
    # Load pass 1 metrics from file
    $p1MetricsPath = Join-Path $pass1Dir "metrics.json"
    if (-not (Test-Path $p1MetricsPath)) {
        Write-Host "ERROR: No pass 1 metrics found at $p1MetricsPath" -ForegroundColor Red
        Write-Host "Run without -Pass2Only first to establish baseline."
        exit 1
    }
    $p1Data = Get-Content $p1MetricsPath -Raw | ConvertFrom-Json
    $p1Results = @{
        Audit = @{
            BuildOk       = [bool]$p1Data.audit.BuildOk
            TestOk        = [bool]$p1Data.audit.TestOk
            TestSummary   = [string]$p1Data.audit.TestSummary
            CriticalCount = [int]$p1Data.audit.CriticalCount
            Exercised     = [int]$p1Data.audit.Exercised
            UiOnly        = [int]$p1Data.audit.UiOnly
            Uncalled      = [int]$p1Data.audit.Uncalled
            TotalMethods  = [int]$p1Data.audit.TotalMethods
            CoveragePct   = [double]$p1Data.audit.CoveragePct
        }
        Experience = @{
            Verdict        = [string]$p1Data.experience.Verdict
            AssertPass     = [int]$p1Data.experience.AssertPass
            AssertFail     = [int]$p1Data.experience.AssertFail
            FlagCount      = [int]$p1Data.experience.FlagCount
            Flags          = @()
            IssuesCritical = [int]$p1Data.experience.IssuesCritical
            IssuesMajor    = [int]$p1Data.experience.IssuesMajor
            IssuesMinor    = [int]$p1Data.experience.IssuesMinor
            FpsAvg         = [string]$p1Data.experience.FpsAvg
        }
    }
    if ($p1Data.experience.Flags) {
        $p1Results.Experience.Flags = @($p1Data.experience.Flags)
    }
}

if ($Pass2Only) {
    # ── Pass 2 ──
    $p2Results = Run-Pass -PassName "PASS 2 (Verification)" -PassDir $pass2Dir

    # ── Delta ──
    Show-Delta -P1 $p1Results -P2 $p2Results
}

$totalElapsed = (Get-Date) - $startTime
Write-Host ""
Write-Host "=== TWO-PASS AUDIT COMPLETE ==="
Write-Host "Total duration: $([math]::Round($totalElapsed.TotalSeconds, 1))s"
Write-Host "Reports: $twoPassDir"
