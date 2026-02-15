param(
    [int] $MaxGates = 200,
    [int] $MaxMissingPerGate = 50
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    $root = (& git rev-parse --show-toplevel 2>$null)
    if (-not $root) { throw "Not a git repo (git rev-parse failed). Run from inside the repo." }
    return $root.Trim()
}

function Ensure-Dir([string] $Path) {
    if (-not (Test-Path $Path)) { New-Item -ItemType Directory -Force -Path $Path | Out-Null }
}

function Read-AllTextUtf8([string] $Path) {
    if (-not (Test-Path $Path)) { throw ("File not found: {0}" -f $Path) }

    $bytes = [System.IO.File]::ReadAllBytes($Path)

    # If a UTF-8 BOM is present, strip it to avoid leaking BOM into content.
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        $trim = New-Object byte[] ($bytes.Length - 3)
        [System.Array]::Copy($bytes, 3, $trim, 0, $trim.Length)
        $bytes = $trim
    }

    $utf8Strict = New-Object System.Text.UTF8Encoding($false, $true)
    return $utf8Strict.GetString($bytes)
}

function Normalize-NewlinesLf([string] $s) {
    if ($null -eq $s) { return "" }
    $s = $s.Replace("`r`n", "`n").Replace("`r", "`n")
    return $s
}

function Ensure-SingleTrailingLf([string] $s) {
    $s = Normalize-NewlinesLf $s
    $s = $s.TrimEnd("`n")
    return ($s + "`n")
}

function Write-AllTextUtf8NoBom([string] $Path, [string] $Content) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $utf8NoBom)
}

function Normalize-RelPath([string] $p) {
    if (-not $p) { return "" }
    $p = $p.Trim()
    $p = $p.Replace("\", "/")
    if ($p.StartsWith("./")) { $p = $p.Substring(2) }
    return $p
}

function Extract-Paths([string] $evidenceCell) {
    if (-not $evidenceCell) { return @() }

    # Evidence cell uses separators and may contain line breaks.
    $s = $evidenceCell.Replace("`r", " ").Replace("`n", " ")

    # Extract repo-like paths conservatively.
    $rx = [regex]'(?<!\w)(docs|SimCore|SimCore\.Tests|scripts|scenes)\/[A-Za-z0-9_\-\.\/]+\.[A-Za-z0-9]+'
    $m = $rx.Matches($s)

    $paths = New-Object System.Collections.Generic.List[string]
    foreach ($x in $m) {
        $paths.Add((Normalize-RelPath $x.Value)) | Out-Null
    }

    return [string[]]@($paths | Sort-Object -Unique)
}

function Bucket-Path([string] $p) {
    $pp = $p.ToLowerInvariant()
    if ($pp.StartsWith("simcore.tests/")) { return "Tests" }
    if ($pp.StartsWith("simcore/")) { return "SimCore" }
    if ($pp.StartsWith("scripts/bridge/")) { return "Bridge" }
    if ($pp.StartsWith("scripts/ui/") -or $pp.StartsWith("scenes/")) { return "UI" }
    return "Other"
}

function Parse-SliceTables([string] $devPlanPath, [int] $maxGates) {
    # Parse gates from B5..B8 markdown tables:
    # | Gate ID | Gate | Status | Evidence |
    $text = Read-AllTextUtf8 $devPlanPath

    $sections = @("### B5.", "### B6.", "### B7.", "### B8.")
    $rows = New-Object System.Collections.Generic.List[object]

    $stats = [ordered]@{
        sections_expected                = $sections.Count
        sections_found                   = 0
        tables_found                     = 0
        table_rows_seen                  = 0
        gate_rows_parsed                 = 0
        gate_rows_skipped_too_few_cols   = 0
        gate_rows_skipped_header_sep     = 0
        gate_rows_skipped_not_row        = 0
        gate_rows_skipped_empty_gateid   = 0
    }

    foreach ($sec in $sections) {
        $idx = $text.IndexOf($sec, [System.StringComparison]::Ordinal)
        if ($idx -lt 0) { continue }
        $stats.sections_found += 1

        # Find start of table header after section header
        $sub = $text.Substring($idx)
        $tableStart = $sub.IndexOf("| Gate ID | Gate | Status | Evidence |", [System.StringComparison]::Ordinal)
        if ($tableStart -lt 0) { continue }
        $stats.tables_found += 1

        $sub2 = $sub.Substring($tableStart)

        # Stop at next "### B" or end
        $stop = $sub2.IndexOf("### B", 1, [System.StringComparison]::Ordinal)
        if ($stop -gt 0) { $sub2 = $sub2.Substring(0, $stop) }

        $sub2 = Normalize-NewlinesLf $sub2
        $lines = $sub2 -split "`n"

        foreach ($line in $lines) {
            $l = $line.Trim()

            if (-not $l.StartsWith("|")) {
                $stats.gate_rows_skipped_not_row += 1
                continue
            }

            if ($l -match '^\|\s*---') {
                $stats.gate_rows_skipped_header_sep += 1
                continue
            }

            if ($l -match '^\|\s*Gate ID\s*\|') {
                $stats.gate_rows_skipped_header_sep += 1
                continue
            }

            $stats.table_rows_seen += 1

            # naive split by |; markdown tables here are simple
            $parts = $l.Trim('|') -split '\|'
            if ($parts.Count -lt 4) {
                $stats.gate_rows_skipped_too_few_cols += 1
                continue
            }

            $gateId = $parts[0].Trim()
            $gate   = $parts[1].Trim()
            $status = $parts[2].Trim()
            $evid   = ($parts[3..($parts.Count-1)] -join "|").Trim()

            if (-not $gateId) {
                $stats.gate_rows_skipped_empty_gateid += 1
                continue
            }

            $rows.Add([pscustomobject]@{
                GateId      = $gateId
                Gate        = $gate
                Status      = $status
                EvidenceRaw = $evid
            }) | Out-Null

            $stats.gate_rows_parsed += 1
        }
    }

    $finalRows = @($rows | Sort-Object GateId | Select-Object -First $maxGates)

    return [pscustomobject]@{
        Rows  = $finalRows
        Stats = $stats
    }
}

$repoRoot = Get-RepoRoot
Set-Location $repoRoot

$ledgerRel = "docs/55_GATES.md"
$ledger = Join-Path $repoRoot ($ledgerRel.Replace("/", "\"))

$outDir = Join-Path $repoRoot "docs\generated"
Ensure-Dir $outDir
$outPath = Join-Path $outDir "gate_closure_delta.md"
$tmpPath = $outPath + ".tmp"

$parsed = Parse-SliceTables -devPlanPath $ledger -maxGates $MaxGates
$gates = @($parsed.Rows)
$parseStats = $parsed.Stats
if ($parseStats.tables_found -eq 0 -or $parseStats.gate_rows_parsed -eq 0) {
    throw ("Parse confidence LOW: tables_found={0}, gate_rows_parsed={1}. Refuse to write output." -f $parseStats.tables_found, $parseStats.gate_rows_parsed)
}

# Determine missing evidence paths for each gate
$gateRows = New-Object System.Collections.Generic.List[object]

$evidence_paths_extracted_total = 0
$gates_with_zero_evidence_paths = 0
$missing_paths_total = 0

foreach ($g in $gates) {
    $paths = @((Extract-Paths $g.EvidenceRaw))
    $evidence_paths_extracted_total += $paths.Count
    if ($paths.Count -eq 0) { $gates_with_zero_evidence_paths += 1 }

    $missing = New-Object System.Collections.Generic.List[string]
    foreach ($p in $paths) {
        $full = Join-Path $repoRoot ($p.Replace("/", "\"))
        if (-not (Test-Path $full)) { $missing.Add($p) | Out-Null }
    }
    $missing_paths_total += $missing.Count

    $gateRows.Add([pscustomobject]@{
        GateId        = $g.GateId
        Gate          = $g.Gate
        Status        = $g.Status
        EvidencePaths = @($paths)
        MissingPaths  = @(@($missing) | Select-Object -First $MaxMissingPerGate)
    }) | Out-Null
}

# Top blockers: missing paths on non-DONE gates, ranked by how many non-DONE gates reference them
$todo = @($gateRows | Where-Object { $_.Status -ne "DONE" })
$missingCounts = @{}
foreach ($tg in $todo) {
    foreach ($mp in $tg.MissingPaths) {
        if (-not $missingCounts.ContainsKey($mp)) { $missingCounts[$mp] = 0 }
        $missingCounts[$mp] += 1
    }
}

$topMissing = @(
    $missingCounts.GetEnumerator() |
    Sort-Object -Property Value -Descending |
    Select-Object -First 20
)

$sb = New-Object System.Text.StringBuilder

$sb.AppendLine("# Gate Closure Delta") | Out-Null
$sb.AppendLine("") | Out-Null

# NOTE: intentionally no generated_at_local line (reduces noise; enables deterministic output)

$sb.AppendLine(("source: {0}" -f $devPlanRel)) | Out-Null
$sb.AppendLine(("scope: B5..B8")) | Out-Null
$sb.AppendLine("") | Out-Null

$sb.AppendLine("## Parse stats") | Out-Null
$sb.AppendLine(("sections_expected: {0}" -f $parseStats.sections_expected)) | Out-Null
$sb.AppendLine(("sections_found: {0}" -f $parseStats.sections_found)) | Out-Null
$sb.AppendLine(("tables_found: {0}" -f $parseStats.tables_found)) | Out-Null
$sb.AppendLine(("table_rows_seen: {0}" -f $parseStats.table_rows_seen)) | Out-Null
$sb.AppendLine(("gate_rows_parsed: {0}" -f $parseStats.gate_rows_parsed)) | Out-Null
$sb.AppendLine(("gate_rows_skipped_too_few_cols: {0}" -f $parseStats.gate_rows_skipped_too_few_cols)) | Out-Null
$sb.AppendLine(("gate_rows_skipped_header_sep: {0}" -f $parseStats.gate_rows_skipped_header_sep)) | Out-Null
$sb.AppendLine(("gate_rows_skipped_not_row: {0}" -f $parseStats.gate_rows_skipped_not_row)) | Out-Null
$sb.AppendLine(("gate_rows_skipped_empty_gateid: {0}" -f $parseStats.gate_rows_skipped_empty_gateid)) | Out-Null
$sb.AppendLine(("gates_selected_capped: {0}" -f $gates.Count)) | Out-Null
$sb.AppendLine(("todos_found: {0}" -f $todo.Count)) | Out-Null
$sb.AppendLine(("evidence_paths_extracted_total: {0}" -f $evidence_paths_extracted_total)) | Out-Null
$sb.AppendLine(("gates_with_zero_evidence_paths: {0}" -f $gates_with_zero_evidence_paths)) | Out-Null
$sb.AppendLine(("missing_paths_total: {0}" -f $missing_paths_total)) | Out-Null

# A single explicit confidence line that is easy to grep for.
$confidence = "OK"
if ($parseStats.tables_found -eq 0 -or $parseStats.gate_rows_parsed -eq 0) { $confidence = "LOW" }
$sb.AppendLine(("parse_confidence: {0}" -f $confidence)) | Out-Null
$sb.AppendLine("") | Out-Null

$sb.AppendLine("## Top gate closure blockers") | Out-Null
if ($topMissing.Count -eq 0) {
    $sb.AppendLine("(none)") | Out-Null
} else {
    foreach ($kv in $topMissing) {
        $sb.AppendLine(("* {0} (referenced by {1} TODO gate(s))" -f $kv.Key, $kv.Value)) | Out-Null
    }
}
$sb.AppendLine("") | Out-Null

$sb.AppendLine("## Gates") | Out-Null
foreach ($gr in $gateRows) {
    $sb.AppendLine(("### {0} [{1}]" -f $gr.GateId, $gr.Status)) | Out-Null
    $sb.AppendLine($gr.Gate) | Out-Null
    $sb.AppendLine("") | Out-Null

    $sb.AppendLine("Evidence:") | Out-Null
    $eps = @($gr.EvidencePaths)
    if ($eps.Count -eq 0) {
        $sb.AppendLine("* (none parsed)") | Out-Null
    } else {
        foreach ($p in $eps) { $sb.AppendLine(("* {0}" -f $p)) | Out-Null }
    }
    $sb.AppendLine("") | Out-Null

    if ($gr.Status -eq "DONE") {
        $sb.AppendLine("Missing:") | Out-Null
        $sb.AppendLine("(none)") | Out-Null
        $sb.AppendLine("") | Out-Null
        continue
    }

    $sb.AppendLine("Missing:") | Out-Null
    if ($gr.MissingPaths.Count -eq 0) {
        $sb.AppendLine("(none detected from evidence paths)") | Out-Null
        $sb.AppendLine("") | Out-Null
        continue
    }

    $buckets = @{
        "SimCore" = New-Object System.Collections.Generic.List[string]
        "Tests"   = New-Object System.Collections.Generic.List[string]
        "Bridge"  = New-Object System.Collections.Generic.List[string]
        "UI"      = New-Object System.Collections.Generic.List[string]
        "Other"   = New-Object System.Collections.Generic.List[string]
    }

    foreach ($mp in $gr.MissingPaths) {
        $b = Bucket-Path $mp
        $buckets[$b].Add($mp) | Out-Null
    }

    foreach ($k in @("SimCore","Tests","Bridge","UI","Other")) {
        if ($buckets[$k].Count -eq 0) { continue }
        $sb.AppendLine(("* {0}:" -f $k)) | Out-Null
        foreach ($p in @($buckets[$k] | Sort-Object)) {
            $sb.AppendLine(("  - [ ] {0}" -f $p)) | Out-Null
        }
    }

    $sb.AppendLine("") | Out-Null
}

# Enforce deterministic newline + exactly one trailing newline
$content = Ensure-SingleTrailingLf ($sb.ToString())

Write-AllTextUtf8NoBom -Path $tmpPath -Content $content
Move-Item -Force -LiteralPath $tmpPath -Destination $outPath

Write-Host ("WROTE: {0}" -f $outPath)
exit 0
