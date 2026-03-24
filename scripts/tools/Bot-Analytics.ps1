<#
.SYNOPSIS
    Bot Analytics — parses bot output files and produces coverage/analysis reports.
.PARAMETER BotType
    Bot type: playthrough, rl, trade, combat, stress, full
.PARAMETER ReportDir
    Directory containing bot output files (default: reports/bot/<BotType>/)
.PARAMETER OutputDir
    Directory for analysis output (default: reports/analytics/)
#>
param(
    [Parameter(Mandatory)][ValidateSet("playthrough","rl","trade","combat","stress","full")]
    [string]$BotType,
    [string]$ReportDir,
    [string]$OutputDir = "reports/analytics"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Push-Location $repoRoot

if (-not $ReportDir) { $ReportDir = "reports/bot/$BotType" }
if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null }

$summary = @{
    bot_type    = $BotType
    timestamp   = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
    phases      = @()
    actions     = @{}
    flags       = @()
    metrics     = @{}
}

$stdoutFile = Join-Path $ReportDir "stdout.txt"
if (-not (Test-Path $stdoutFile)) {
    Write-Host "No stdout.txt found in $ReportDir"
    Pop-Location
    exit 0
}

$lines = Get-Content $stdoutFile -ErrorAction SilentlyContinue

switch ($BotType) {
    "playthrough" {
        $prefix = "PLAY|"
        $phases = @()
        $flags = @()
        $result = "UNKNOWN"
        $credits = 0
        $trades = 0
        $visited = 0

        foreach ($line in $lines) {
            if ($line -notmatch "^PLAY\|") { continue }
            $parts = $line.Substring($prefix.Length).Split("|")

            if ($parts[0] -eq "PHASE") { $phases += $parts[1] }
            if ($parts[0] -eq "RESULT") { $result = $parts[1] }
            if ($parts[0] -eq "SUMMARY") {
                foreach ($kv in $parts) {
                    if ($kv -match "credits=(\d+)") { $credits = [int]$Matches[1] }
                    if ($kv -match "trades=(\d+)") { $trades = [int]$Matches[1] }
                    if ($kv -match "visited=(\d+)") { $visited = [int]$Matches[1] }
                    if ($kv -match "flags=(.+)") {
                        $flagStr = $Matches[1]
                        if ($flagStr -ne "NONE") { $flags = $flagStr.Split("|") }
                    }
                }
            }
            # Track phase transitions from log lines
            foreach ($ph in @("TUTORIAL","TRADE","EXPLORE","HAVEN","UPGRADE","RESEARCH","EQUIP","ENDGAME","VICTORY")) {
                if ($parts[0] -eq $ph -and $ph -notin $phases) { $phases += $ph }
            }
        }

        $expectedPhases = @("TUTORIAL","TRADE","EXPLORE","HAVEN","UPGRADE","RESEARCH","EQUIP","ENDGAME","VICTORY")
        $phaseCoverage = 0
        foreach ($ep in $expectedPhases) {
            if ($ep -in $phases) { $phaseCoverage++ }
        }
        $coveragePct = [math]::Round(($phaseCoverage / $expectedPhases.Count) * 100, 1)

        $summary.phases = $phases
        $summary.flags = $flags
        $summary.metrics = @{
            result          = $result
            credits         = $credits
            trades          = $trades
            nodes_visited   = $visited
            phase_coverage  = "$phaseCoverage/$($expectedPhases.Count) ($coveragePct%)"
            victory_reached = ($result -eq "PASS")
        }

        Write-Host "=== Playthrough Bot Analytics ==="
        Write-Host "Result: $result"
        Write-Host "Phase coverage: $phaseCoverage/$($expectedPhases.Count) ($coveragePct%)"
        Write-Host "Phases reached: $($phases -join ' -> ')"
        Write-Host "Credits: $credits | Trades: $trades | Nodes: $visited"
        if ($flags.Count -gt 0) { Write-Host "Flags: $($flags -join ', ')" }
    }

    "rl" {
        $prefix = "RLAG|"
        $actionCounts = @{}
        $rewards = @()
        $episodes = 0

        foreach ($line in $lines) {
            if ($line -notmatch "^RLAG\|") { continue }
            $parts = $line.Substring($prefix.Length).Split("|")

            if ($parts[0] -eq "STEP") {
                foreach ($kv in $parts) {
                    if ($kv -match "action=(.+)") {
                        $act = $Matches[1]
                        $category = ($act -split "_")[0]
                        if (-not $actionCounts.ContainsKey($category)) { $actionCounts[$category] = 0 }
                        $actionCounts[$category]++
                    }
                }
            }
            if ($parts[0] -eq "EPISODE_END") { $episodes++ }
        }

        $totalActions = ($actionCounts.Values | Measure-Object -Sum).Sum
        $summary.actions = $actionCounts
        $summary.metrics = @{
            episodes      = $episodes
            total_actions = $totalActions
            action_categories = $actionCounts.Count
        }

        Write-Host "=== RL Bot Analytics ==="
        Write-Host "Episodes: $episodes | Total actions: $totalActions"
        Write-Host "Action categories used: $($actionCounts.Count)"
        foreach ($k in ($actionCounts.Keys | Sort-Object)) {
            $count = $actionCounts[$k]
            $pct = if ($totalActions -gt 0) { [math]::Round(($count / $totalActions) * 100, 1) } else { 0 }
            Write-Host "  $k : $count ($pct%)"
        }
    }

    default {
        # Trade/combat/stress/full bots
        $actionLabels = @{}
        $flagList = @()

        foreach ($line in $lines) {
            if ($line -match "^(TBOT|CBOT|SBOT|FBOT)\|") {
                $parts = $line.Split("|")
                foreach ($p in $parts) {
                    if ($p -match "^(TRADE|BUY|SELL|COMBAT|TRAVEL|WARP)") {
                        $cat = $Matches[1]
                        if (-not $actionLabels.ContainsKey($cat)) { $actionLabels[$cat] = 0 }
                        $actionLabels[$cat]++
                    }
                    if ($p -match "^(TRADE_NO_EFFECT|NEVER_|NET_LOSS|STUCK_|PRICE_|ECONOMY_|CREDIT_)") {
                        $flagList += $p
                    }
                }
            }
        }

        $summary.actions = $actionLabels
        $summary.flags = $flagList
        $summary.metrics = @{
            action_categories = $actionLabels.Count
            flag_count        = $flagList.Count
        }

        Write-Host "=== $BotType Bot Analytics ==="
        Write-Host "Actions: $($actionLabels.Count) categories"
        foreach ($k in ($actionLabels.Keys | Sort-Object)) {
            Write-Host "  $k : $($actionLabels[$k])"
        }
        if ($flagList.Count -gt 0) { Write-Host "Flags: $($flagList -join ', ')" }
    }
}

# Write JSON summary
$jsonPath = Join-Path $OutputDir "${BotType}_summary.json"
$summary | ConvertTo-Json -Depth 4 | Set-Content $jsonPath -Encoding UTF8
Write-Host "`nSummary written to: $jsonPath"

# Write text report
$reportPath = Join-Path $OutputDir "${BotType}_report.txt"
$reportLines = @(
    "Bot Analytics Report - $BotType",
    "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
    "Source: $ReportDir",
    "",
    "Metrics:"
)
foreach ($k in ($summary.metrics.Keys | Sort-Object)) {
    $reportLines += "  $k = $($summary.metrics[$k])"
}
if ($summary.flags.Count -gt 0) {
    $reportLines += ""
    $reportLines += "Flags:"
    foreach ($f in $summary.flags) { $reportLines += "  - $f" }
}
if ($summary.actions.Count -gt 0) {
    $reportLines += ""
    $reportLines += "Action Distribution:"
    foreach ($k in ($summary.actions.Keys | Sort-Object)) {
        $reportLines += "  $k = $($summary.actions[$k])"
    }
}
$reportLines | Set-Content $reportPath -Encoding UTF8
Write-Host "Report written to: $reportPath"

Pop-Location
exit 0
