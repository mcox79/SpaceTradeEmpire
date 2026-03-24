<#
.SYNOPSIS
  Quick audit: C# test suite + optimize scan + coverage gap + AI linting in <90s.
  For CI-like pre-commit validation.

.PARAMETER Scope
  Subdirectory to limit optimize scan to (relative to repo root).
  Default: entire repo
#>
param(
    [string]$Scope = ""
)

$ErrorActionPreference = "Continue"
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$exitCode = 0
$startTime = Get-Date

Write-Host "=== AUDIT QUICK START ==="
Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host ""

# ── Step 1: C# Build ──────────────────────────────────────────────
Write-Host "--- Step 1: Build ---"
$buildOutput = & dotnet build (Join-Path $repoRoot "Space Trade Empire.csproj") --nologo -v q 2>&1
$buildExitCode = $LASTEXITCODE
if ($buildExitCode -ne 0) {
    Write-Host "BUILD FAILED (exit $buildExitCode)"
    $buildOutput | ForEach-Object { Write-Host "  $_" }
    $exitCode = 2
} else {
    Write-Host "BUILD OK"
}
Write-Host ""

# ── Step 2: C# Test Suite ─────────────────────────────────────────
Write-Host "--- Step 2: Test Suite ---"
$testOutput = & dotnet test (Join-Path $repoRoot "SimCore.Tests/SimCore.Tests.csproj") -c Release --nologo -v q 2>&1
$testExitCode = $LASTEXITCODE
$testSummary = ($testOutput | Select-String "Passed!|Failed!")
if ($testExitCode -ne 0) {
    Write-Host "TESTS FAILED (exit $testExitCode)"
    $testOutput | Where-Object { $_ -match "Failed|Error" } | ForEach-Object { Write-Host "  $_" }
    $exitCode = [math]::Max($exitCode, 2)
} else {
    Write-Host "TESTS OK -- $testSummary"
}
Write-Host ""

# ── Step 3: Optimize Scan (Pass 1) ────────────────────────────────
Write-Host "--- Step 3: Optimize Scan ---"
$scanArgs = @("-ExecutionPolicy", "Bypass", "-File", (Join-Path $repoRoot "scripts/tools/Run-OptimizeScan.ps1"))
if ($Scope) { $scanArgs += @("-Scope", $Scope) }
$scanOutput = & powershell @scanArgs 2>&1
$scanSummary = ($scanOutput | Select-String "OPTSCAN\|SUMMARY")
$scanTotal = ($scanOutput | Select-String "OPTSCAN\|TOTAL")
$critLine = ($scanOutput | Select-String "CRITICAL=(\d+)")
$critCount = 0
if ($critLine) {
    $critCount = [int]($critLine.Matches[0].Groups[1].Value)
}
Write-Host "$scanSummary"
Write-Host "$scanTotal"
if ($critCount -gt 0) {
    Write-Host "CRITICAL findings: $critCount"
    $exitCode = [math]::Max($exitCode, 1)
}
Write-Host ""

# ── Step 4: Coverage Gap ──────────────────────────────────────────
Write-Host "--- Step 4: Coverage Gap ---"
$covOutput = & powershell -ExecutionPolicy Bypass -File (Join-Path $repoRoot "scripts/tools/Run-CoverageGap.ps1") 2>&1
$covSummary = ($covOutput | Select-String "UNCALLED")
Write-Host ($covOutput | Select-String "Total bridge methods")
Write-Host ($covOutput | Select-String "EXERCISED")
Write-Host ($covOutput | Select-String "UI_ONLY")
Write-Host $covSummary
Write-Host ""

# ── Step 5: Semgrep Architecture Lint ────────────────────────────
Write-Host "--- Step 5: Semgrep Architecture Lint ---"
$semgrepAvailable = $null -ne (Get-Command semgrep -ErrorAction SilentlyContinue)
if ($semgrepAvailable) {
    $semgrepConfig = Join-Path $repoRoot ".semgrep.yml"
    $semgrepTarget = Join-Path $repoRoot "SimCore"
    $semgrepOutput = & semgrep --config $semgrepConfig --quiet $semgrepTarget 2>&1
    $semgrepExit = $LASTEXITCODE
    if ($semgrepExit -ne 0) {
        $errorCount = ($semgrepOutput | Measure-Object -Line).Lines
        Write-Host "SEMGREP: $errorCount architecture violations found"
        $exitCode = [math]::Max($exitCode, 1)
    } else {
        Write-Host "SEMGREP OK -- no architecture violations"
    }
} else {
    Write-Host "SEMGREP SKIPPED -- not installed (pip install semgrep)"
}
Write-Host ""

# ── Step 6: GDScript Lint ────────────────────────────────────────
Write-Host "--- Step 6: GDScript Lint ---"
$gdlintAvailable = $null -ne (Get-Command gdlint -ErrorAction SilentlyContinue)
if ($gdlintAvailable) {
    $gdlintTarget = Join-Path $repoRoot "scripts"
    $gdlintOutput = & gdlint $gdlintTarget 2>&1
    $gdlintExit = $LASTEXITCODE
    if ($gdlintExit -ne 0) {
        $issueCount = ($gdlintOutput | Measure-Object -Line).Lines
        Write-Host "GDLINT: $issueCount issues found"
    } else {
        Write-Host "GDLINT OK"
    }
} else {
    Write-Host "GDLINT SKIPPED -- not installed (pip install gdtoolkit)"
}
Write-Host ""

# ── Summary ───────────────────────────────────────────────────────
$elapsed = (Get-Date) - $startTime
Write-Host "=== AUDIT QUICK COMPLETE ==="
Write-Host "Duration: $([math]::Round($elapsed.TotalSeconds, 1))s"
Write-Host "Exit code: $exitCode (0=clean, 1=warnings, 2=critical)"
exit $exitCode
