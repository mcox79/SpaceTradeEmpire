param(
	[switch]$Force = $false,
	[ValidateSet("OFF","WARN","FAIL")][string]$EpicMismatchMode = "OFF"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-Utf8NoBom {
	return New-Object System.Text.UTF8Encoding($false)
}

function Assert-NoUtf8Bom {
	param(
		[Parameter(Mandatory=$true)][string]$Path
	)
	$bytes = [System.IO.File]::ReadAllBytes($Path)
	if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
		throw ("UTF-8 BOM detected in file: " + $Path)
	}
}

function Write-AtomicUtf8NoBom {
	param(
		[Parameter(Mandatory=$true)][string]$Path,
		[Parameter(Mandatory=$true)][string[]]$Lines
	)

	$dir = Split-Path -Parent $Path
	if (-not (Test-Path -LiteralPath $dir)) {
		$null = New-Item -ItemType Directory -Path $dir -Force
	}

	$tmp = $Path + ".tmp"
	$content = ($Lines -join "`n") + "`n"
	[System.IO.File]::WriteAllText($tmp, $content, (Get-Utf8NoBom))
	Move-Item -Force -LiteralPath $tmp -Destination $Path
	Assert-NoUtf8Bom -Path $Path
}

function Read-AllLines-Deterministic {
	param(
		[Parameter(Mandatory=$true)][string]$Path
	)
	if (-not (Test-Path -LiteralPath $Path)) {
		throw ("Missing required file: " + $Path)
	}
	# ReadAllLines preserves line boundaries deterministically. We do not accept BOM.
	Assert-NoUtf8Bom -Path $Path
	return [System.IO.File]::ReadAllLines($Path)
}

function Parse-SessionLog-PassGateIds {
	param(
		[Parameter(Mandatory=$true)][string]$SessionLogPath
	)

	$lines = Read-AllLines-Deterministic -Path $SessionLogPath
	$set = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::Ordinal)

	foreach ($line in $lines) {
		# Capture "<gate_id> PASS" in one deterministic match to avoid $Matches reuse issues.
		$m = [Regex]::Match($line, '\b(GATE\.[A-Z0-9_\.]+)\b\s+PASS\b')
		if ($m.Success) {
			$gateId = $m.Groups[1].Value
			if ($gateId.Length -gt 0) {
				$null = $set.Add($gateId)
			}
		}
	}

	return $set
}

function Parse-Ledger-Gates {
	param(
		[Parameter(Mandatory=$true)][string]$LedgerPath
	)

	$lines = Read-AllLines-Deterministic -Path $LedgerPath

	# Parse markdown tables: any row containing "| GATE." is considered a gate row.
	# Columns are: Gate ID | Status | Gate | Evidence
	$records = @()
	foreach ($line in $lines) {
		if ($line -notmatch '^\s*\|\s*GATE\.') { continue }

		$cols = $line.Split('|')
		if ($cols.Length -lt 6) { continue }

		$gateId = $cols[1].Trim()
		$status = $cols[2].Trim()
		$gateText = $cols[3].Trim()
		$evidence = $cols[4].Trim()

		if ($gateId -notmatch '^GATE\.[A-Z0-9_\.]+$') { continue }

		$records += [PSCustomObject]@{
			gate_id = $gateId
			status = $status
			gate = $gateText
			evidence = $evidence
		}
	}

	return $records
}

function Sort-Strings-Ordinal {
	param([Parameter(Mandatory=$true)][string[]]$Items)
	$arr = @($Items)
	[System.Array]::Sort($arr, [System.StringComparer]::Ordinal)
	return $arr
}

function Try-Extract-CanonicalEpicSection {
	param([Parameter(Mandatory=$true)][object]$Lines)

	# Coerce scalar or array into a deterministic array of strings.
	$arr = @($Lines)

	# Accept either header:
	# 1) "## A0. Canonical Epic Bullets (authoritative for scanning and next-gate selection)"
	# 2) "## Canonical Epic Bullets (authoritative for scanning and next-gate selection)"
	$headerRx = '^\s*##\s+(A0\.\s+)?Canonical Epic Bullets\s+\(authoritative for scanning and next-gate selection\)\s*$'

	$start = -1
	for ($i = 0; $i -lt $arr.Length; $i++) {
		$line = [string]$arr[$i]
		if ($line -and ($line -match $headerRx)) { $start = $i + 1; break }
	}
	if ($start -lt 0) { return $null }

	# End at the next subsection header (### ...) or next major lettered section (## B. / ## C. ...).
	# This avoids terminating early on "## Rule: ..." which you currently have inside the canonical block.
	$end = $arr.Length
	for ($j = $start; $j -lt $arr.Length; $j++) {
		$line = [string]$arr[$j]
		if ($line -match '^\s*###\s+') { $end = $j; break }
		if ($line -match '^\s*##\s+[A-Z]\.\s+') { $end = $j; break }
	}

	return ,($arr[$start..($end-1)])
}

function Parse-Canonical-Epics {
	param(
		[Parameter(Mandatory=$true)][object]$EpicsLines
	)

	# Coerce scalar or array into a deterministic array of strings.
	$lines = @($EpicsLines)

	$rx = '^\s*-\s+(EPIC\.[A-Z0-9_\.]+)\s+\[(TODO|IN_PROGRESS|DONE|DEFERRED)\]:.*\(\s*gates:\s*([^)]+)\s*\)\s*$'

	$records = @()
	foreach ($lineObj in $lines) {
		$line = [string]$lineObj
		if ([string]::IsNullOrWhiteSpace($line)) { continue }

		$m = [Regex]::Match($line, $rx)
		if (-not $m.Success) { continue }

		$epicId = $m.Groups[1].Value.Trim()
		$declared = $m.Groups[2].Value.Trim()
		$selRaw = $m.Groups[3].Value.Trim()

		$selectors = @()
		foreach ($p in ($selRaw -split ',')) {
			$t = $p.Trim()
			if ($t.Length -gt 0) { $selectors += $t }
		}

		$records += [PSCustomObject]@{
			epic_id = $epicId
			declared_status = $declared
			selectors = $selectors
		}
	}

	return $records
}

function Expand-Epic-Selectors {
	param(
		[Parameter(Mandatory=$true)][string[]]$Selectors,
		[Parameter(Mandatory=$true)][string[]]$AllGateIds,
		[Parameter(Mandatory=$true)][hashtable]$StatusMap
	)

	$matched = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::Ordinal)
	$unknownExact = @()

	foreach ($sel in $Selectors) {
		if ($sel.Contains("*")) {
			foreach ($gid in $AllGateIds) {
				if ($gid -clike $sel) { $null = $matched.Add($gid) }
			}
		} else {
			if ($StatusMap.ContainsKey($sel)) {
				$null = $matched.Add($sel)
			} else {
				$unknownExact += $sel
			}
		}
	}

	$matchedList = Sort-Strings-Ordinal -Items @($matched)
	return [PSCustomObject]@{
		matched = $matchedList
		unknown_exact = $unknownExact
	}
}

function Compute-Epic-Status {
	param(
		[Parameter(Mandatory=$true)][string[]]$MatchedGateIds,
		[Parameter(Mandatory=$true)][hashtable]$StatusMap
	)

	if ($MatchedGateIds.Count -eq 0) { return "IN_PROGRESS" }

	$allDone = $true
	$allTodo = $true

	foreach ($gid in $MatchedGateIds) {
		$st = [string]$StatusMap[$gid]
		if ($st -ne "DONE") { $allDone = $false }
		if ($st -ne "TODO") { $allTodo = $false }
	}

	if ($allDone) { return "DONE" }
	if ($allTodo) { return "TODO" }
	return "IN_PROGRESS"
}

function Summarize-GateList {
	param([Parameter(Mandatory=$true)][string[]]$GateIds)

	# Keep mismatch lines bounded but deterministic.
	if ($GateIds.Count -le 20) {
		return ($GateIds -join ",")
	}
	$head = $GateIds[0..19] -join ","
	$more = $GateIds.Count - 20
	return ($head + ",...(+${more})")
}

function Read-Allowlist {
	param(
		[Parameter(Mandatory=$true)][string]$AllowlistPath
	)

	$set = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::Ordinal)

	if (-not (Test-Path -LiteralPath $AllowlistPath)) {
		# Allow empty allowlist by default.
		return $set
	}

	$lines = Read-AllLines-Deterministic -Path $AllowlistPath

	foreach ($line in $lines) {
		$t = $line.Trim()
		if ($t.Length -eq 0) { continue }
		if ($t.StartsWith("#")) { continue }
		if ($t -match '^GATE\.[A-Z0-9_\.]+$') {
			$null = $set.Add($t)
		}
	}

	return $set
}

function Severity-Rank {
	param([Parameter(Mandatory=$true)][string]$Severity)
	switch ($Severity) {
		'HARD_FAIL' { return 0 }
		'WARN' { return 1 }
		default { return 9 }
	}
}

function Add-Finding {
	param(
		[Parameter(Mandatory=$true)][ref]$Findings,
		[Parameter(Mandatory=$true)][string]$Severity,
		[Parameter(Mandatory=$true)][string]$Kind,
		[Parameter(Mandatory=$true)][string]$GateId,
		[Parameter(Mandatory=$true)][string]$Details
	)

	$Findings.Value += [PSCustomObject]@{
		severity = $Severity
		kind = $Kind
		gate_id = $GateId
		details = $Details
	}
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$ledgerPath = Join-Path $repoRoot "docs/55_GATES.md"
$sessionLogPath = Join-Path $repoRoot "docs/56_SESSION_LOG.md"
$queuePath = Join-Path $repoRoot "docs/gates/gates.json"
$allowlistPath = Join-Path $repoRoot "docs/roadmap/legacy_done_without_pass_allowlist_v0.txt"
$outPath = Join-Path $repoRoot "docs/generated/roadmap_mismatches_v0.txt"
$epicsPath = Join-Path $repoRoot "docs/54_EPICS.md"

$ledger = Parse-Ledger-Gates -LedgerPath $ledgerPath
$passSet = Parse-SessionLog-PassGateIds -SessionLogPath $sessionLogPath
$allowSet = Read-Allowlist -AllowlistPath $allowlistPath
if ($null -eq $allowSet) {
	$allowSet = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::Ordinal)
}

$findings = @()

# Duplicate gate ids in ledger
$seen = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::Ordinal)
foreach ($r in $ledger) {
	if ($seen.Contains($r.gate_id)) {
		Add-Finding -Findings ([ref]$findings) -Severity "HARD_FAIL" -Kind "DUPLICATE_GATE_ID" -GateId $r.gate_id -Details "Gate id appears more than once in docs/55_GATES.md"
	} else {
		$null = $seen.Add($r.gate_id)
	}
}

# Build status map for fast lookup
$statusMap = @{}
$evidenceMap = @{}
foreach ($r in $ledger) {
	if (-not $statusMap.ContainsKey($r.gate_id)) {
		$statusMap[$r.gate_id] = $r.status
		$evidenceMap[$r.gate_id] = $r.evidence
	}
}

# EPICS canonical scan (optional, backward compatible)
if ($EpicMismatchMode -ne "OFF") {
	$epicSeverity = if ($EpicMismatchMode -eq "FAIL") { "HARD_FAIL" } else { "WARN" }

$epicsLinesAll = $null
if (Test-Path -LiteralPath $epicsPath) {
	# Force collection so single-line files do not collapse to a scalar string.
	$epicsLinesAll = @(Read-AllLines-Deterministic -Path $epicsPath)
}

	if ($null -ne $epicsLinesAll) {
		$canonical = Try-Extract-CanonicalEpicSection -Lines $epicsLinesAll
		if ($null -ne $canonical) {
$epics = Parse-Canonical-Epics -EpicsLines $canonical

if ($epics.Count -eq 0) {
	Add-Finding -Findings ([ref]$findings) -Severity $epicSeverity -Kind "EPIC_CANONICAL_NO_MATCHES" -GateId "EPIC.CANONICAL" -Details "Canonical Epic Bullets section found, but 0 epic bullets matched the required format. Ensure lines start with '- EPIC.' and include '[STATUS]:' and '(gates: ...)' on the same line."
}

# Duplicate epic ids inside canonical section is always a hard failure.
$seenEpic = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::Ordinal)
foreach ($e in $epics) {
	if ($seenEpic.Contains($e.epic_id)) {
		Add-Finding -Findings ([ref]$findings) -Severity "HARD_FAIL" -Kind "DUPLICATE_EPIC_ID" -GateId $e.epic_id -Details "Epic id appears more than once in Canonical Epic Bullets section"
	} else {
		$null = $seenEpic.Add($e.epic_id)
	}
}

			$allGateIds = Sort-Strings-Ordinal -Items @($statusMap.Keys)

			foreach ($e in $epics) {
				$exp = Expand-Epic-Selectors -Selectors $e.selectors -AllGateIds $allGateIds -StatusMap $statusMap

				$unknown = @($exp.unknown_exact)
				$matched = @($exp.matched)

				if ($unknown.Count -gt 0) {
					Add-Finding -Findings ([ref]$findings) -Severity $epicSeverity -Kind "EPIC_UNKNOWN_GATE_REF" -GateId $e.epic_id -Details ("Unknown gate id(s) referenced: " + (($unknown | Sort-Object) -join ","))
				}

				if ($matched.Count -eq 0) {
					Add-Finding -Findings ([ref]$findings) -Severity $epicSeverity -Kind "EPIC_SELECTOR_EMPTY" -GateId $e.epic_id -Details ("Selectors matched 0 gates: " + ($e.selectors -join ","))
					continue
				}

				$computed = Compute-Epic-Status -MatchedGateIds $matched -StatusMap $statusMap
				if ($computed -ne $e.declared_status) {
					$gateList = Summarize-GateList -GateIds $matched
					Add-Finding -Findings ([ref]$findings) -Severity $epicSeverity -Kind "EPIC_STATUS_MISMATCH" -GateId $e.epic_id -Details ("declared=" + $e.declared_status + " computed=" + $computed + " matched_gates=" + $gateList)
				}
			}
		}
	}
}

# PASS entries imply DONE in ledger
foreach ($gateId in ($passSet | Sort-Object)) {
	if (-not $statusMap.ContainsKey($gateId)) {
		Add-Finding -Findings ([ref]$findings) -Severity "HARD_FAIL" -Kind "PASS_UNKNOWN_GATE" -GateId $gateId -Details "PASS entry exists but gate id not found in docs/55_GATES.md"
		continue
	}
	$st = $statusMap[$gateId]
	if ($st -ne "DONE") {
		Add-Finding -Findings ([ref]$findings) -Severity "HARD_FAIL" -Kind "PASS_IMPLIES_NOT_DONE" -GateId $gateId -Details ("PASS entry exists but ledger status is '" + $st + "'")
	}
}

# DONE gates must not have TBD evidence
foreach ($r in $ledger) {
	if ($r.status -ne "DONE") { continue }
	$ev = $r.evidence
	if ($null -eq $ev) { $ev = "" }

	if ($ev -match '\bTBD\b') {
		Add-Finding -Findings ([ref]$findings) -Severity "HARD_FAIL" -Kind "DONE_EVIDENCE_TBD" -GateId $r.gate_id -Details "Ledger status DONE but Evidence contains TBD"
	}
}

# DONE without PASS is legacy HARD_FAIL unless allowlisted
foreach ($r in $ledger) {
	if ($r.status -ne "DONE") { continue }
	if ($passSet.Contains($r.gate_id)) { continue }
	if ($allowSet.Contains($r.gate_id)) { continue }
	Add-Finding -Findings ([ref]$findings) -Severity "HARD_FAIL" -Kind "DONE_WITHOUT_PASS" -GateId $r.gate_id -Details "Ledger status DONE but no PASS entry in docs/56_SESSION_LOG.md and not allowlisted"
}

# Active queue must not contain DONE gates (by ledger)
Assert-NoUtf8Bom -Path $queuePath
$queueJson = Get-Content -LiteralPath $queuePath -Raw | ConvertFrom-Json
foreach ($t in $queueJson.tasks) {
	$gid = [string]$t.gate_id
	if ($statusMap.ContainsKey($gid) -and $statusMap[$gid] -eq "DONE") {
		Add-Finding -Findings ([ref]$findings) -Severity "HARD_FAIL" -Kind "QUEUE_CONTAINS_DONE_GATE" -GateId $gid -Details ("Task '" + $t.task_id + "' is in queue but gate is DONE in ledger")
	}
}

# Deterministic sort: severity rank, then kind ordinal, then gate_id ordinal
$sorted = $findings | Sort-Object `
	@{ Expression = { Severity-Rank $_.severity }; Ascending = $true }, `
	@{ Expression = { $_.kind }; Ascending = $true }, `
	@{ Expression = { $_.gate_id }; Ascending = $true }

# Emit report lines: "SEVERITY|KIND|GATE_ID|DETAILS"
$outLines = @()
foreach ($f in $sorted) {
	$details = $f.details -replace "`r", "" -replace "`n", " "
	$outLines += ($f.severity + "|" + $f.kind + "|" + $f.gate_id + "|" + $details)
}

# Ensure writer never receives an empty array (writer currently rejects empty).
$outLines = @($outLines)
if ($outLines.Count -eq 0) { $outLines = @("OK") }

Write-AtomicUtf8NoBom -Path $outPath -Lines $outLines

$hardFailCount = 0
foreach ($f in $sorted) {
	if ($f.severity -eq "HARD_FAIL") { $hardFailCount++ }
}

if ($hardFailCount -gt 0) {
	exit 2
}

exit 0
