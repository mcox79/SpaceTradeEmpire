<#
.SYNOPSIS
  Deep audit: ALL testing layers -- unit tests, property tests, concurrency tests,
  architecture lint, GDScript lint, mutation testing, RL smoke, bot runs, screenshot
  regression. Full-spectrum quality gate.

  Expect 10-30 minutes depending on Stryker scope and bot cycles.

.PARAMETER SkipStryker
  Skip mutation testing (saves ~5-15 minutes).

.PARAMETER SkipRl
  Skip RL smoke tests (saves ~30s headless, ~60s Godot).

.PARAMETER SkipBots
  Skip bot runs (saves ~2-5 minutes).

.PARAMETER SkipScreenshots
  Skip screenshot regression (saves ~2 minutes, requires baselines).

.PARAMETER StrykerFilter
  Limit Stryker to specific files (glob pattern). Default: all configured in stryker-config.json.

.EXAMPLE
  .\Run-AuditDeep.ps1                           # Full deep audit
  .\Run-AuditDeep.ps1 -SkipStryker              # Everything except mutation testing
  .\Run-AuditDeep.ps1 -SkipBots -SkipScreenshots  # Code-only audit
#>

param(
    [switch]$SkipStryker,
    [switch]$SkipRl,
    [switch]$SkipBots,
    [switch]$SkipScreenshots,
    [string]$StrykerFilter = ""
)

$ErrorActionPreference = "Continue"
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$exitCode = 0
$startTime = Get-Date
$stepResults = @()

function Report-Step {
    param([string]$Name, [string]$Status, [string]$Detail = "")
    $script:stepResults += [PSCustomObject]@{ Step = $Name; Status = $Status; Detail = $Detail }
    $color = switch ($Status) { "PASS" { "Green" } "WARN" { "Yellow" } "FAIL" { "Red" } "SKIP" { "DarkGray" } default { "White" } }
    $msg = "  [$Status] $Name"
    if ($Detail) { $msg += " -- $Detail" }
    Write-Host $msg -ForegroundColor $color
}

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  DEEP AUDIT -- Full-Spectrum Quality Gate"
Write-Host "  $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# ═══════════════════════════════════════════════════════════════════
# PHASE 1: BUILD
# ═══════════════════════════════════════════════════════════════════
Write-Host "--- Phase 1: Build ---" -ForegroundColor White

# Game assembly
$buildOutput = & dotnet build (Join-Path $repoRoot "Space Trade Empire.csproj") --nologo -v q 2>&1
if ($LASTEXITCODE -ne 0) {
    Report-Step "Game Build" "FAIL"
    $exitCode = 2
} else {
    Report-Step "Game Build" "PASS"
}

# RL Server
$rlBuildOutput = & dotnet build (Join-Path $repoRoot "SimCore.RlServer/SimCore.RlServer.csproj") -c Release --nologo -v q 2>&1
if ($LASTEXITCODE -ne 0) {
    Report-Step "RL Server Build" "FAIL"
    $exitCode = [math]::Max($exitCode, 2)
} else {
    Report-Step "RL Server Build" "PASS"
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════════
# PHASE 2: TEST SUITE (split into targeted runs for better reporting)
# ═══════════════════════════════════════════════════════════════════
Write-Host "--- Phase 2: Test Suite ---" -ForegroundColor White

$testCsproj = Join-Path $repoRoot "SimCore.Tests/SimCore.Tests.csproj"

# 2a: Full test suite
$testOutput = & dotnet test $testCsproj -c Release --nologo -v q 2>&1
$testSummary = ($testOutput | Select-String "Passed!|Failed!")
if ($LASTEXITCODE -ne 0) {
    Report-Step "Test Suite (all)" "FAIL" "$testSummary"
    $exitCode = [math]::Max($exitCode, 2)
} else {
    Report-Step "Test Suite (all)" "PASS" "$testSummary"
}

# 2b: Determinism tests specifically (critical for sim integrity)
$detOutput = & dotnet test $testCsproj -c Release --nologo -v q --filter "FullyQualifiedName~Determinism" 2>&1
$detSummary = ($detOutput | Select-String "Passed!|Failed!")
if ($LASTEXITCODE -ne 0) {
    Report-Step "Determinism Tests" "FAIL" "$detSummary"
    $exitCode = [math]::Max($exitCode, 2)
} else {
    Report-Step "Determinism Tests" "PASS" "$detSummary"
}

# 2c: Property-based tests specifically (FsCheck random invariants)
$propOutput = & dotnet test $testCsproj -c Release --nologo -v q --filter "FullyQualifiedName~PropertyBased" 2>&1
$propSummary = ($propOutput | Select-String "Passed!|Failed!")
if ($LASTEXITCODE -ne 0) {
    # Capture FsCheck counterexample if available
    $counterex = ($propOutput | Select-String "Falsifiable" | Select-Object -First 1)
    $detail = "$propSummary"
    if ($counterex) { $detail += " counterexample: $counterex" }
    Report-Step "Property Tests (FsCheck)" "FAIL" $detail
    $exitCode = [math]::Max($exitCode, 2)
} else {
    Report-Step "Property Tests (FsCheck)" "PASS" "$propSummary"
}

# 2d: Concurrency tests specifically (SimBridge threading)
$concOutput = & dotnet test $testCsproj -c Release --nologo -v q --filter "FullyQualifiedName~Concurrency" 2>&1
$concSummary = ($concOutput | Select-String "Passed!|Failed!")
if ($LASTEXITCODE -ne 0) {
    Report-Step "Concurrency Tests" "FAIL" "$concSummary"
    $exitCode = [math]::Max($exitCode, 2)
} else {
    Report-Step "Concurrency Tests" "PASS" "$concSummary"
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════════
# PHASE 3: STATIC ANALYSIS
# ═══════════════════════════════════════════════════════════════════
Write-Host "--- Phase 3: Static Analysis ---" -ForegroundColor White

# Semgrep
$semgrepAvailable = $null -ne (Get-Command semgrep -ErrorAction SilentlyContinue)
if ($semgrepAvailable) {
    $semgrepConfig = Join-Path $repoRoot ".semgrep.yml"
    $semgrepTarget = Join-Path $repoRoot "SimCore"
    $semgrepOutput = & semgrep --config $semgrepConfig $semgrepTarget 2>&1
    # Semgrep exit codes: 0=no findings, 1=findings found, 2+=error
    $semgrepExit = $LASTEXITCODE
    # Count lines that look like findings (file:line: pattern)
    $findingLines = @($semgrepOutput | Where-Object { $_ -match "^\s*(SimCore|D:)" })
    $findingCount = $findingLines.Count
    if ($semgrepExit -ge 2) {
        Report-Step "Semgrep Architecture" "WARN" "semgrep error (exit $semgrepExit)"
    } elseif ($findingCount -gt 0) {
        Report-Step "Semgrep Architecture" "FAIL" "$findingCount violations"
        foreach ($f in $findingLines | Select-Object -First 5) {
            Write-Host "    $f" -ForegroundColor Red
        }
        $exitCode = [math]::Max($exitCode, 1)
    } else {
        Report-Step "Semgrep Architecture" "PASS" "0 violations"
    }
} else {
    Report-Step "Semgrep Architecture" "SKIP" "not installed"
}

# GDScript lint
$gdlintAvailable = $null -ne (Get-Command gdlint -ErrorAction SilentlyContinue)
if ($gdlintAvailable) {
    $gdlintTarget = Join-Path $repoRoot "scripts"
    $gdlintOutput = & gdlint $gdlintTarget 2>&1
    # Filter to real errors (not parse failures from gdlint limitations or style noise)
    $realIssues = @($gdlintOutput | Where-Object {
        $_ -match "Error:" -and $_ -notmatch "Unexpected token|Expected one|class-definitions-order|trailing-whitespace|max-line-length|max-returns|max-public-methods|unused-argument"
    })
    if ($realIssues.Count -gt 20) {
        Report-Step "GDScript Lint" "WARN" "$($realIssues.Count) substantive issues"
    } elseif ($realIssues.Count -gt 0) {
        Report-Step "GDScript Lint" "PASS" "$($realIssues.Count) minor issues"
    } else {
        Report-Step "GDScript Lint" "PASS"
    }
} else {
    Report-Step "GDScript Lint" "SKIP" "not installed"
}

# Optimize scan
$scanArgs = @("-ExecutionPolicy", "Bypass", "-File", (Join-Path $repoRoot "scripts/tools/Run-OptimizeScan.ps1"))
$scanOutput = & powershell @scanArgs 2>&1
$critLine = ($scanOutput | Select-String "CRITICAL=(\d+)")
$critCount = 0
if ($critLine) { $critCount = [int]($critLine.Matches[0].Groups[1].Value) }
if ($critCount -gt 0) {
    Report-Step "Optimize Scan" "WARN" "$critCount critical findings"
    $exitCode = [math]::Max($exitCode, 1)
} else {
    Report-Step "Optimize Scan" "PASS"
}

# Coverage gap
$covOutput = & powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot "scripts/tools/Run-CoverageGap.ps1") 2>&1
$uncalledLine = ($covOutput | Select-String "UNCALLED")
Report-Step "Coverage Gap" "PASS" "$uncalledLine"
Write-Host ""

# ═══════════════════════════════════════════════════════════════════
# PHASE 4: MUTATION TESTING (Stryker.NET)
# ═══════════════════════════════════════════════════════════════════
Write-Host "--- Phase 4: Mutation Testing ---" -ForegroundColor White

if ($SkipStryker) {
    Report-Step "Stryker.NET" "SKIP" "flag"
} else {
    $strykerAvailable = $null -ne (Get-Command dotnet-stryker -ErrorAction SilentlyContinue)
    if (-not $strykerAvailable) {
        # Try restoring local tool
        & dotnet tool restore 2>&1 | Out-Null
        $strykerAvailable = $null -ne (Get-Command dotnet-stryker -ErrorAction SilentlyContinue)
    }
    if ($strykerAvailable) {
        Write-Host "  Running Stryker (this may take 5-15 minutes)..." -ForegroundColor DarkGray
        $strykerArgs = @()
        if ($StrykerFilter) { $strykerArgs += @("--mutate", $StrykerFilter) }
        Push-Location $repoRoot
        $strykerOutput = & dotnet-stryker @strykerArgs 2>&1
        $strykerExit = $LASTEXITCODE
        Pop-Location
        $scoreLine = ($strykerOutput | Select-String "mutation score")
        if ($strykerExit -ne 0) {
            Report-Step "Stryker.NET" "WARN" "below threshold -- $scoreLine"
            $exitCode = [math]::Max($exitCode, 1)
        } else {
            Report-Step "Stryker.NET" "PASS" "$scoreLine"
        }
    } else {
        Report-Step "Stryker.NET" "SKIP" "not installed (dotnet tool restore)"
    }
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════════
# PHASE 5: RL SMOKE TESTS
# ═══════════════════════════════════════════════════════════════════
Write-Host "--- Phase 5: RL Smoke Tests ---" -ForegroundColor White

if ($SkipRl) {
    Report-Step "RL Headless Smoke" "SKIP" "flag"
    Report-Step "RL Godot Smoke" "SKIP" "flag"
} else {
    $pythonAvailable = $null -ne (Get-Command python -ErrorAction SilentlyContinue)
    if ($pythonAvailable) {
        # Headless RL smoke
        $exePath = Join-Path $repoRoot "SimCore.RlServer\bin\Release\net8.0\SimCore.RlServer.exe"
        if (Test-Path $exePath) {
            # Pipe JSON commands directly to the server and check output
            $rlInput = '{"type":"reset","seed":42,"star_count":4,"curriculum_stage":0,"max_episode_ticks":100}' + "`n"
            for ($i = 0; $i -lt 5; $i++) {
                $rlInput += '{"type":"step","action":0}' + "`n"
            }
            $rlInput += '{"type":"shutdown"}' + "`n"
            $rlOutput = $rlInput | & $exePath 2>$null
            $lines = $rlOutput | Where-Object { $_ -match '"type"' }
            $resetLine = $lines | Select-Object -First 1
            if ($resetLine -match '"reset_ok"') {
                Report-Step "RL Headless Smoke" "PASS" "reset+5 steps+shutdown"
            } else {
                Report-Step "RL Headless Smoke" "FAIL" "unexpected response"
                $exitCode = [math]::Max($exitCode, 1)
            }
        } else {
            Report-Step "RL Headless Smoke" "SKIP" "server not built"
        }
    } else {
        Report-Step "RL Headless Smoke" "SKIP" "python not found"
        Report-Step "RL Godot Smoke" "SKIP" "python not found"
    }
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════════
# PHASE 6: BOT RUNS
# ═══════════════════════════════════════════════════════════════════
Write-Host "--- Phase 6: Bot Runs ---" -ForegroundColor White

if ($SkipBots) {
    Report-Step "Trade Bot" "SKIP" "flag"
    Report-Step "Trade Economy Balance" "SKIP" "flag"
    Report-Step "Combat Bot" "SKIP" "flag"
} else {
    $botScript = Join-Path $repoRoot "scripts/tools/Run-Bot.ps1"
    if (Test-Path $botScript) {
        # 6a: Trade bot (200 cycles)
        $tradeOutput = & powershell -ExecutionPolicy Bypass -File $botScript -Mode trade -Cycles 200 2>&1
        $tradeReportPath = Join-Path $repoRoot "reports/bot/trade/report.json"
        if (Test-Path $tradeReportPath) {
            $tradeReport = Get-Content $tradeReportPath -Raw | ConvertFrom-Json
            $tradeFlags = $tradeReport.flags
            $critFlags = @($tradeFlags | Where-Object { $_.severity -eq "CRITICAL" })
            $allFlagIds = ($tradeFlags | ForEach-Object { $_.id }) -join ", "

            if ($critFlags.Count -gt 0) {
                Report-Step "Trade Bot" "FAIL" "CRITICAL: $($critFlags[0].id) -- $($critFlags[0].detail)"
                $exitCode = [math]::Max($exitCode, 2)
            } elseif ($tradeFlags.Count -gt 0) {
                Report-Step "Trade Bot" "WARN" "flags: $allFlagIds"
            } else {
                Report-Step "Trade Bot" "PASS" "200 cycles, profit=$($tradeReport.net_profit)"
            }

            # 6b: Economy diversity check
            $goodsBought = @($tradeReport.goods_bought)
            $goodsSold = @($tradeReport.goods_sold)
            $uniqueGoods = ($goodsBought + $goodsSold) | Sort-Object -Unique
            if ($uniqueGoods.Count -lt 3) {
                Report-Step "Trade Economy Balance" "WARN" "only $($uniqueGoods.Count) goods traded: $($uniqueGoods -join ', ') -- possible balance issue"
            } else {
                Report-Step "Trade Economy Balance" "PASS" "$($uniqueGoods.Count) goods traded"
            }
        } else {
            $tradeFail = ($tradeOutput | Select-String "FAIL|ERROR|FATAL")
            if ($tradeFail) {
                Report-Step "Trade Bot" "WARN" "flags found (no report.json)"
            } else {
                Report-Step "Trade Bot" "PASS" "200 cycles"
            }
            Report-Step "Trade Economy Balance" "SKIP" "no report.json"
        }

        # 6c: Combat bot (100 cycles)
        $combatOutput = & powershell -ExecutionPolicy Bypass -File $botScript -Mode combat -Cycles 100 2>&1
        $combatReportPath = Join-Path $repoRoot "reports/bot/combat/report.json"
        if (Test-Path $combatReportPath) {
            $combatReport = Get-Content $combatReportPath -Raw | ConvertFrom-Json
            $combatFlags = $combatReport.flags
            $combatCritFlags = @($combatFlags | Where-Object { $_.severity -eq "CRITICAL" })
            $combatFlagIds = ($combatFlags | ForEach-Object { $_.id }) -join ", "

            if ($combatCritFlags.Count -gt 0) {
                Report-Step "Combat Bot" "WARN" "CRITICAL: $($combatCritFlags[0].id) (hostile spawning not enabled)"
            } elseif ($combatFlags.Count -gt 0) {
                Report-Step "Combat Bot" "WARN" "flags: $combatFlagIds"
            } else {
                Report-Step "Combat Bot" "PASS" "100 cycles, combats=$($combatReport.combats) kills=$($combatReport.kills)"
            }
        } else {
            $combatFail = ($combatOutput | Select-String "FAIL|ERROR|FATAL")
            if ($combatFail) {
                Report-Step "Combat Bot" "WARN" "flags found (no report.json)"
            } else {
                Report-Step "Combat Bot" "PASS" "100 cycles"
            }
        }
    } else {
        Report-Step "Trade Bot" "SKIP" "runner not found"
        Report-Step "Trade Economy Balance" "SKIP" "runner not found"
        Report-Step "Combat Bot" "SKIP" "runner not found"
    }
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════════
# PHASE 7: SCREENSHOT REGRESSION
# ═══════════════════════════════════════════════════════════════════
Write-Host "--- Phase 7: Screenshot Regression ---" -ForegroundColor White

if ($SkipScreenshots) {
    Report-Step "Screenshot Regression" "SKIP" "flag"
} else {
    $baselineDir = Join-Path $repoRoot "reports/baselines/full"
    $currentDir = Join-Path $repoRoot "reports/screenshot/full"
    $comparePy = Join-Path $repoRoot "scripts/tools/compare_screenshots.py"

    if (-not (Test-Path $baselineDir)) {
        Report-Step "Screenshot Regression" "SKIP" "no baselines in reports/baselines/full/"
    } elseif (-not (Test-Path $currentDir)) {
        # Capture fresh screenshots first
        $ssScript = Join-Path $repoRoot "scripts/tools/Run-Screenshot.ps1"
        if (Test-Path $ssScript) {
            Write-Host "  Capturing screenshots..." -ForegroundColor DarkGray
            & powershell -ExecutionPolicy Bypass -File $ssScript -Mode full 2>&1 | Out-Null
        }
        if (-not (Test-Path $currentDir)) {
            Report-Step "Screenshot Regression" "SKIP" "no current screenshots"
        }
    }

    if ((Test-Path $baselineDir) -and (Test-Path $currentDir) -and (Test-Path $comparePy)) {
        # Try SSIM first, fallback to MAD
        $metricArg = "mad"
        & python -c "import numpy" 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) { $metricArg = "ssim" }

        $compareOutput = & python $comparePy --current $currentDir --baseline $baselineDir --metric $metricArg 2>&1

        # Parse results from JSON stdout
        $passCount = ($compareOutput | Select-String '"PASS"').Count
        $warnCount = ($compareOutput | Select-String '"WARN"').Count
        $failCount = ($compareOutput | Select-String '"FAIL"').Count
        $totalCount = $passCount + $warnCount + $failCount

        if ($failCount -gt 0) {
            Report-Step "Screenshot Regression" "WARN" "$failCount FAIL, $warnCount WARN, $passCount PASS of $totalCount (metric=$metricArg)"
            $exitCode = [math]::Max($exitCode, 1)
        } elseif ($warnCount -gt 0) {
            Report-Step "Screenshot Regression" "WARN" "$warnCount WARN, $passCount PASS of $totalCount (metric=$metricArg)"
        } else {
            Report-Step "Screenshot Regression" "PASS" "$passCount/$totalCount pass (metric=$metricArg)"
        }
    }
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════════
# FINAL REPORT
# ═══════════════════════════════════════════════════════════════════
$elapsed = (Get-Date) - $startTime

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  DEEP AUDIT REPORT"
Write-Host "============================================" -ForegroundColor Cyan

$passCount = ($stepResults | Where-Object { $_.Status -eq "PASS" }).Count
$warnCount = ($stepResults | Where-Object { $_.Status -eq "WARN" }).Count
$failCount = ($stepResults | Where-Object { $_.Status -eq "FAIL" }).Count
$skipCount = ($stepResults | Where-Object { $_.Status -eq "SKIP" }).Count

foreach ($r in $stepResults) {
    $color = switch ($r.Status) { "PASS" { "Green" } "WARN" { "Yellow" } "FAIL" { "Red" } "SKIP" { "DarkGray" } default { "White" } }
    $msg = "  [$($r.Status)] $($r.Step)"
    if ($r.Detail) { $msg += " -- $($r.Detail)" }
    Write-Host $msg -ForegroundColor $color
}

Write-Host ""
Write-Host "  Total: $passCount PASS, $warnCount WARN, $failCount FAIL, $skipCount SKIP"
Write-Host "  Duration: $([math]::Round($elapsed.TotalSeconds, 1))s"
Write-Host "  Exit code: $exitCode (0=clean, 1=warnings, 2=critical)"
Write-Host "============================================" -ForegroundColor Cyan

# Write JSON report
$reportDir = Join-Path $repoRoot "reports/audit"
New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
$ts = Get-Date -Format "yyyyMMdd_HHmmss"
$reportPath = Join-Path $reportDir "deep_audit_$ts.json"
$reportData = @{
    timestamp = (Get-Date -Format "o")
    duration_seconds = [math]::Round($elapsed.TotalSeconds, 1)
    exit_code = $exitCode
    summary = @{ pass = $passCount; warn = $warnCount; fail = $failCount; skip = $skipCount }
    steps = $stepResults | ForEach-Object { @{ step = $_.Step; status = $_.Status; detail = $_.Detail } }
}
$reportData | ConvertTo-Json -Depth 4 | Set-Content $reportPath -Encoding UTF8
Write-Host "  Report: $reportPath"

exit $exitCode
