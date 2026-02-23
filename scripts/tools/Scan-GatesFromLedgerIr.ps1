param(
  [Parameter(Mandatory=$true)][string]$IrPath,
  [Parameter(Mandatory=$true)][string]$SchemaPath,
  [Parameter(Mandatory=$true)][string]$ContextPacketPath,
  [Parameter(Mandatory=$true)][string]$OutAppendPath,
  [string]$OutFullPath = "docs/generated/gates_queue_full.json",
  [Parameter(Mandatory=$true)][string]$OutReportPath,
  [ValidateSet("APPEND","FULL")][string]$Mode = "APPEND",
  [int]$QueueCap = 0,
  [switch]$EnableRepairs,
  [string]$RepairPolicyVersion = "R1"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-FailAndExit([int]$code, [string[]]$lines) {
  $content = ($lines -join "`n") + "`n"
  $dir = Split-Path -Parent $OutReportPath
  if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
  [System.IO.File]::WriteAllText($OutReportPath, $content, [System.Text.UTF8Encoding]::new($false))
  Write-Error $content
  exit $code
}

function Read-JsonPossiblyFenced([string]$path) {
  $raw = Get-Content -LiteralPath $path -Raw -Encoding UTF8

  # Prefer ```json fenced block if present. Fail if multiple json fences exist.
  $jsonFence = [regex]::Matches($raw, '(?s)```json\s*(\{.*?\})\s*```')
  if ($jsonFence.Count -gt 1) { throw "Multiple ```json fences found in $path" }

  $jsonText = $null
  $usedFence = $false

  if ($jsonFence.Count -eq 1) {
    $jsonText = $jsonFence[0].Groups[1].Value
    $usedFence = $true
  } else {
    # Fallback: allow raw JSON (no fence)
    $jsonText = $raw
  }

  try {
    return ($jsonText | ConvertFrom-Json)
  } catch {
    try {
      Add-Type -AssemblyName System.Web.Extensions
      $ser = New-Object System.Web.Script.Serialization.JavaScriptSerializer
      $ser.RecursionLimit = 2000
      return $ser.DeserializeObject($jsonText)
    } catch {
      if ($usedFence) { throw "Failed to parse fenced JSON in $path" }
      throw "Failed to parse JSON in $path"
    }
  }
}

function Read-ContextPacket([string]$path) {
  $lines = Get-Content -LiteralPath $path -Encoding UTF8
  $head = $null
  $fileMap = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::Ordinal)
  $inFileMap = $false

  for ($i=0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]

    if (-not $head) {
      if ($line -match '^\s*head:\s*([0-9a-f]{40})\s*$') { $head = $Matches[1] }
    }

    if ($line -match '^\s*###\s*\[FILE MAP\]\s*$') { $inFileMap = $true; continue }
    if ($inFileMap) {
      if ($line -match '^\s*###\s*\[') { break } # next section
      if ($line -match '^\s*-\s+(.+?)\s*$') {
        $p = $Matches[1].Trim().Replace('\','/')

        # Normalize harmless prefixes
        if ($p.StartsWith("./")) { $p = $p.Substring(2) }
        $p = $p.Trim()

        if ($p.Length -gt 0) { [void]$fileMap.Add($p) }
      }
    }
  }

  if (-not $head) { throw "Context packet missing head hash" }
  if ($fileMap.Count -lt 1) { throw "Context packet file map not found or empty" }

  return [pscustomobject]@{ Head = $head; FileMap = $fileMap }
}

function Try-Substitute-MissingEvidence([string]$missingPath, [System.Collections.Generic.HashSet[string]]$fileMap) {
  if (-not $EnableRepairs) { return $null }
  # Explicit deterministic substitutions for known bad Stage 1 emissions
  $subs = @{
    "SimCore.Tests/Programs/ProgramManualOverrideContractTests.cs" = @(
      "SimCore.Tests/Programs/ProgramContractTests.cs",
      "SimCore.Tests/Programs/ProgramExecutionIntegrationTests.cs",
      "SimCore.Tests/Programs/DefaultDoctrineContractTests.cs",
      "SimCore.Tests/Programs/FleetBindingContractTests.cs"
    )
  }

  if ($subs.ContainsKey($missingPath)) {
    foreach ($cand in $subs[$missingPath]) {
      if ($fileMap.Contains($cand)) { return $cand }
    }
    return $null
  }

  return $null
}

function Normalize-RepoPath([string]$p) {
  if ($null -eq $p) { return $null }
  $s = $p.Trim().Replace('\','/')
  if ($s -match '^[A-Za-z]:/' ) { return $null } # absolute windows path not allowed
  if ($s.StartsWith("/")) { return $null }
  return $s
}

function Is-TestPath([string]$p) {
  if ($p.StartsWith("SimCore.Tests/")) { return $true }
  if ($p -match '(^|/)tests(/|$)') { return $true }
  if ($p -match 'Tests\.cs$') { return $true }
  return $false
}

function Get-BucketKey([string]$p) {
  if (-not $p) { return "Other" }
  if ($p.StartsWith("docs/generated/")) { return "DataGenerated" }
  if ($p.StartsWith("SimCore.Tests/TestData/")) { return "DataGenerated" }
  if ($p.StartsWith("SimCore.Tests/")) { return "Tests" }
  if ($p.StartsWith("SimCore/")) { return "SimCore" }
  if ($p.StartsWith("scripts/tools/")) { return "DocsTooling" }
  if ($p.StartsWith("docs/")) { return "DocsTooling" }
  if ($p.StartsWith("scripts/") -or $p.StartsWith("scenes/")) { return "UI" }
  return "Other"
}

function Get-BucketCount([object[]]$evidencePaths) {
  $set = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::Ordinal)
  foreach ($p0 in @($evidencePaths)) {
    $p = Normalize-RepoPath ($p0 + "")
    if ($p) { [void]$set.Add((Get-BucketKey $p)) }
  }
  return $set.Count
}

function Status-RankTask([string]$st) {
  $s = ($st + "").Trim()
  if ($s -eq "IN_PROGRESS") { return 0 }
  if ($s -eq "TODO") { return 1 }
  return 9
}

function Select-Anchor([string[]]$evidence, [string]$preferredAnchor) {
  if ($preferredAnchor) {
    $pa = Normalize-RepoPath $preferredAnchor
    if ($pa -and ($evidence -contains $pa)) { return $pa }
  }
  foreach ($p in $evidence) { if (Is-TestPath $p) { return $p } }
  return $evidence[0]
}

function Trim-Evidence([string[]]$evidence, [string]$anchor) {
  # anchor first
  $rest = @()
  foreach ($p in $evidence) { if ($p -ne $anchor) { $rest += $p } }

  if ($evidence.Count -le 6) { return @($anchor) + $rest }

  # priority: tests, production, tooling/docs
  $tests = @()
  $prod  = @()
  $other = @()

  foreach ($p in $rest) {
    if (Is-TestPath $p) { $tests += $p; continue }
    if ($p.StartsWith("SimCore/") -or $p.StartsWith("scripts/") -or $p.StartsWith("scenes/")) { $prod += $p; continue }
    $other += $p
  }

  $tests = $tests | Sort-Object
  $prod  = $prod  | Sort-Object
  $other = $other | Sort-Object

  $kept = @($anchor)
  foreach ($p in ($tests + $prod + $other)) {
    if ($kept.Count -ge 6) { break }
    $kept += $p
  }
  return $kept
}

function Sha256Bytes([string]$s) {
  $sha = [System.Security.Cryptography.SHA256]::Create()
  try {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($s)
    return $sha.ComputeHash($bytes)
  } finally { $sha.Dispose() }
}

function Get-GitCommitIsoUtc([string]$head) {
  $iso = (& git show -s --format=%cI $head 2>$null)
  if (-not $iso) { throw "Unable to read git commit time for head=$head" }
  return ($iso.Trim())
}

function Get-RootQueueOrderingConst($schema) {
  $qo = $schema.properties.queue_ordering
  if (-not $qo) { throw "Schema missing properties.queue_ordering" }
  if (-not $qo.const) { throw "Schema missing properties.queue_ordering.const" }
  return (($qo.const + "").Trim())
}

function Mint-TaskId([string]$gateId, [string]$candidateKey, [string]$anchor, [hashtable]$used) {
  $fp = "{0}|{1}|{2}" -f $gateId, $candidateKey, $anchor
  $h = Sha256Bytes $fp
  # uint32 from first 4 bytes, big endian
  $n0 = ([uint32]$h[0] -shl 24) -bor ([uint32]$h[1] -shl 16) -bor ([uint32]$h[2] -shl 8) -bor ([uint32]$h[3])
  $n = [int]($n0 % 1000)

  for ($i=0; $i -lt 1000; $i++) {
    $id = "{0}.T{1:000}" -f $gateId, $n
    if (-not $used.ContainsKey($id)) { $used[$id] = $true; return $id }
    $n = ($n + 1) % 1000
  }
  throw "Unable to allocate unique task_id for gate_id=$gateId"
}

function Is-Executable-CommandLine([string]$s) {
  if (-not $s) { return $false }
  $t = $s.Trim()
  if ($t -eq "") { return $false }
  if ($t -match '^(dotnet|pwsh|powershell|git)\s+') { return $true }
  if ($t -match '^(cmd\.exe|bash)\s+') { return $true }
  if ($t -match '^\.\\') { return $true }
  return $false
}

function Split-ToMaxLen {
  param(
    [Parameter(Mandatory=$true)][string]$Text,
    [int]$MaxLen = 160
  )

  $t = ($Text -replace '\s+', ' ').Trim()
  if ($t.Length -le $MaxLen) { return ,$t }

  $out = New-Object System.Collections.Generic.List[string]
  $start = 0
  while ($start -lt $t.Length) {
    $take = [Math]::Min($MaxLen, $t.Length - $start)
    $chunk = $t.Substring($start, $take)

    $lastSpace = $chunk.LastIndexOf(' ')
    if ($lastSpace -gt 40 -and ($start + $take) -lt $t.Length) {
      $chunk = $chunk.Substring(0, $lastSpace)
      $take = $chunk.Length
    }

    $out.Add($chunk.Trim())
    $start += $take
    while ($start -lt $t.Length -and $t[$start] -eq ' ') { $start++ }
  }

  return $out.ToArray()
}

function Choose-CompletionCommandLine([string[]]$EvidencePaths) {
  $hasCsTests = $false
  $hasGd = $false

  foreach ($p0 in @($EvidencePaths)) {
    $p = ($p0 + "").Trim().Replace('\','/')
    if ($p -match '^(SimCore\.Tests/|GameShell\.Tests/)') { $hasCsTests = $true }
    if ($p -match '\.gd$') { $hasGd = $true }
  }

  if ($hasCsTests -and (Test-Path -LiteralPath "SimCore.Tests/SimCore.Tests.csproj")) {
    return "dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release"
  }

  if ($hasGd -and (Test-Path -LiteralPath "scripts/tools/Validate-GodotScript.ps1")) {
    return "powershell -ExecutionPolicy Bypass -File scripts/tools/Validate-GodotScript.ps1"
  }

  if (Test-Path -LiteralPath "scripts/tools/Validate-Gates.ps1") {
    return "powershell -ExecutionPolicy Bypass -File scripts/tools/Validate-Gates.ps1"
  }

  if (Test-Path -LiteralPath "SimCore.Tests/SimCore.Tests.csproj") {
    return "dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release"
  }

  if (Test-Path -LiteralPath "scripts/tools/Validate-GodotScript.ps1") {
    return "powershell -ExecutionPolicy Bypass -File scripts/tools/Validate-GodotScript.ps1"
  }

  return "powershell -NoProfile -Command ""Write-Error 'No repo validator command available for this task'; exit 1"""
}

function Infer-CompletionHint([string]$acceptanceText, [string[]]$evidence) {
  # Rule A: always include an explicit runnable command line as completion_hint[0]
  $cmd = Choose-CompletionCommandLine $evidence

  $out = New-Object System.Collections.Generic.List[string]
  foreach ($p in (Split-ToMaxLen -Text $cmd -MaxLen 160)) {
    if ($p) { $out.Add($p) }
  }

  # Append acceptance text (optional), but split to <=160 and cap deterministically
  if (-not [string]::IsNullOrWhiteSpace($acceptanceText)) {
    $lines = $acceptanceText -split "`r?`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }
    $joined = ($lines -join " / ")

    foreach ($p in (Split-ToMaxLen -Text $joined -MaxLen 160)) {
      if ($p) { $out.Add($p) }
      if ($out.Count -ge 8) { break } # deterministic cap
    }
  }

  # Rule B: hard guarantee every item <=160 (defensive)
  $final = New-Object System.Collections.Generic.List[string]
  foreach ($l in $out) {
    foreach ($p in (Split-ToMaxLen -Text ($l + "") -MaxLen 160)) {
      if ($p) { $final.Add($p) }
    }
  }

  return ,$final.ToArray()
}

function Trunc([string]$s, [int]$max) {
  if ($null -eq $s) { return "" }
  $t = $s.Trim()
  if ($t.Length -le $max) { return $t }
  return $t.Substring(0, $max)
}

function Get-PropOrNull($obj, [string]$name) {
  if ($null -eq $obj) { return $null }
  if ($obj.PSObject.Properties.Name -contains $name) { return $obj.$name }
  return $null
}

function Get-PropOrDefault($obj, [string]$name, $default) {
  $v = Get-PropOrNull $obj $name
  if ($null -eq $v) { return $default }
  return $v
}

# ------------------ Load inputs ------------------

if (-not (Test-Path -LiteralPath $IrPath)) { Write-FailAndExit 3 @("FAIL_PARSE", "missing_ir_path=$IrPath") }
if (-not (Test-Path -LiteralPath $SchemaPath)) { Write-FailAndExit 3 @("FAIL_PARSE", "missing_schema_path=$SchemaPath") }
if (-not (Test-Path -LiteralPath $ContextPacketPath)) { Write-FailAndExit 3 @("FAIL_PARSE", "missing_context_packet_path=$ContextPacketPath") }

$ir = Read-JsonPossiblyFenced $IrPath
$schema = Read-JsonPossiblyFenced $SchemaPath
$ctx = Read-ContextPacket $ContextPacketPath
$queueOrderingConst = Get-RootQueueOrderingConst $schema

# IR compatibility shim: support either "head" or "context_head"
$irHead = $null
if ($ir.PSObject.Properties.Name -contains 'head') {
    $irHead = [string]$ir.head
} elseif ($ir.PSObject.Properties.Name -contains 'context_head') {
    $irHead = [string]$ir.context_head
}

if ([string]::IsNullOrWhiteSpace($irHead)) {
    throw "IR missing required head field (expected top-level 'head' or 'context_head')."
}

if ($irHead -ne $ctx.Head) {
    throw "IR/context HEAD mismatch. IR=$irHead CONTEXT=$($ctx.Head)"
}

$irCapRaw = Get-PropOrNull $ir "cap"
$irOrdering = Get-PropOrNull $ir "ordering"

$lines = @()
$lines += "PASS_FAIL: PENDING"
$lines += "head_ir: $irHead"
$lines += "head_ctx: $($ctx.Head)"
$lines += "file_map_count: $($ctx.FileMap.Count)"
$lines += ("cap_ir: " + ($(if ($null -eq $irCapRaw) { "(missing)" } else { ($irCapRaw | ConvertTo-Json -Compress) })))
$lines += ("ordering_ir: " + ($(if ([string]::IsNullOrWhiteSpace(($irOrdering + ""))) { "(missing)" } else { (($irOrdering + "").Trim()) })))
$lines += "ordering_schema_const: $queueOrderingConst"
$lines += ("ordering_stage2: " + $queueOrderingConst)
$lines += ""

$rp = "STRICT"
if ($EnableRepairs) { $rp = "REPAIR" }
$lines += ("repair_policy: " + $rp)
$lines += ("repair_policy_version: " + $RepairPolicyVersion)
$lines += "repair_rules_count: 1"
$lines += ""

if ($irHead -ne $ctx.Head) {
  Write-FailAndExit 3 ($lines + @("FAIL_PARSE", "reason: IR head does not match context packet head"))
}
# IR compatibility shim:
# - Stage1 ledger IR shape: eligible_gates[] or gates[]
# - Prebuilt queue shape: tasks[] (already a queue)
$eligibleGatesArr = $null
$prebuiltTasksArr = $null

if ($ir.PSObject.Properties.Name -contains 'eligible_gates') {
  $eligibleGatesArr = @($ir.eligible_gates)
} elseif ($ir.PSObject.Properties.Name -contains 'gates') {
  $eligibleGatesArr = @($ir.gates)
} elseif ($ir.PSObject.Properties.Name -contains 'tasks') {
  $prebuiltTasksArr = @($ir.tasks)
}

if ($null -ne $prebuiltTasksArr) {
  # Prebuilt tasks passthrough: write report PASS and write outputs deterministically.
  $lines += "mode_detected: PREBUILT_TASKS"
  $lines[0] = "PASS_FAIL: PASS"

  $repDir = Split-Path -Parent $OutReportPath
  if ($repDir -and -not (Test-Path $repDir)) { New-Item -ItemType Directory -Path $repDir | Out-Null }
  [System.IO.File]::WriteAllText($OutReportPath, (($lines -join "`n") + "`n"), [System.Text.UTF8Encoding]::new($false))

  # Append JSON output: tasks only
  $appDir = Split-Path -Parent $OutAppendPath
  if ($appDir -and -not (Test-Path $appDir)) { New-Item -ItemType Directory -Path $appDir | Out-Null }
  $jsonOut = ($prebuiltTasksArr | ConvertTo-Json -Depth 50)
  [System.IO.File]::WriteAllText($OutAppendPath, ($jsonOut + "`n"), [System.Text.UTF8Encoding]::new($false))

  if ($Mode -eq "FULL") {
    $genUtc = Get-GitCommitIsoUtc $ctx.Head
    $full = [ordered]@{
      queue_contract_version = "2.2"
      queue_ordering = $queueOrderingConst
      generated_utc = $genUtc
      queue_intent = ("Stage 2 passthrough (IR already contains tasks) at HEAD " + $ctx.Head)
      tasks = $prebuiltTasksArr
    }
    $fullJson = ($full | ConvertTo-Json -Depth 80)
    $fullDir = Split-Path -Parent $OutFullPath
    if ($fullDir -and -not (Test-Path $fullDir)) { New-Item -ItemType Directory -Path $fullDir | Out-Null }
    [System.IO.File]::WriteAllText($OutFullPath, ($fullJson + "`n"), [System.Text.UTF8Encoding]::new($false))
  }

  exit 0
}

if ($null -eq $eligibleGatesArr -or @($eligibleGatesArr).Count -lt 1) {
  Write-FailAndExit 3 ($lines + @(
    "FAIL_PARSE",
    "reason: IR missing input list (expected eligible_gates[], gates[], or tasks[])"
  ))
}

# schema introspection
$taskDef = $schema.'$defs'.task
if (-not $taskDef) {
  Write-FailAndExit 1 ($lines + @("FAIL_SCHEMA", "reason: schema missing $defs.task"))
}

$taskRequired = @()
if ($taskDef.required) { $taskRequired = @($taskDef.required) }

$taskProps = @{}
if ($taskDef.properties) {
  foreach ($p in $taskDef.properties.PSObject.Properties) { $taskProps[$p.Name] = $true }
}

$lines += "task_required: " + ($taskRequired -join ", ")
$lines += "task_prop_count: " + ($taskProps.Keys.Count)
$lines += ""

# ------------------ Candidate processing ------------------

function Get-IrGateStatus($g) {
  if ($null -eq $g) { return "" }

  if ($g.PSObject.Properties.Name -contains 'gate_status') { return (($g.gate_status + "").Trim()) }
  if ($g.PSObject.Properties.Name -contains 'status')      { return (($g.status + "").Trim()) }

  return ""
}

function Get-IrGateTitle($g) {
  if ($null -eq $g) { return "" }

  if ($g.PSObject.Properties.Name -contains 'gate_title') { return (($g.gate_title + "").Trim()) }
  if ($g.PSObject.Properties.Name -contains 'title')      { return (($g.title + "").Trim()) }

  return ""
}

function Get-IrAcceptanceText($g) {
  if ($null -eq $g) { return "" }

  if ($g.PSObject.Properties.Name -contains 'acceptance_text') { return (($g.acceptance_text + "").Trim()) }

  # Stage1 IR fallbacks (common in your IR)
  if ($g.PSObject.Properties.Name -contains 'completion_hint') {
    $xs = @($g.completion_hint) | ForEach-Object { ("" + $_).Trim() } | Where-Object { $_ }
    if ($xs.Count -gt 0) { return ($xs -join "; ") }
  }
  if ($g.PSObject.Properties.Name -contains 'constraints') {
    $xs = @($g.constraints) | ForEach-Object { ("" + $_).Trim() } | Where-Object { $_ }
    if ($xs.Count -gt 0) { return ($xs -join "; ") }
  }

  return ""
}

function Get-IrEvidenceUniverse($g) {
  if ($null -eq $g) { return @() }

  # Stage1 IR variants
  if ($g.PSObject.Properties.Name -contains 'evidence_universe') { return @($g.evidence_universe) }

  # Your current IR shape
  if ($g.PSObject.Properties.Name -contains 'evidence_paths') { return @($g.evidence_paths) }

  # Older shapes sometimes used evidence[] objects with .path
  if ($g.PSObject.Properties.Name -contains 'evidence') {
    $out = @()
    foreach ($e in @($g.evidence)) {
      $p = Get-PropOrNull $e "path"
      if ($null -ne $p) { $out += ([string]$p) }
    }
    return $out
  }

  return @()
}

function Status-Rank([string]$st) {
  $s = ($st + "").Trim()
  if ($s -eq "IN_PROGRESS") { return 0 }
  if ($s -eq "TODO") { return 1 }
  return 2
}

$all = @($eligibleGatesArr)
$candidatesTotal = $all.Count

# Only TODO/IN_PROGRESS are candidates; Stage 1 may already apply this, but Stage 2 verifies.
$eligible = @()
foreach ($g in $all) {
  $st = Get-IrGateStatus $g
  if ($st -eq "TODO" -or $st -eq "IN_PROGRESS") { $eligible += $g }
}

$eligibleTotal = $eligible.Count
$cap = 0
if ($QueueCap -gt 0) {
  $cap = $QueueCap
} else {
  $irCap = Get-PropOrNull $ir "cap"

  if ($irCap -is [int] -and $irCap -gt 0) {
    $cap = [int]$irCap
  } elseif ($irCap -ne $null) {
    $maxActive = Get-PropOrNull $irCap "max_active_gates"
    if ($maxActive -is [int] -and $maxActive -gt 0) {
      $cap = [int]$maxActive
    }
  }

  if ($cap -le 0) { $cap = $eligibleTotal }
}

# Stage 2 recomputes ORDER_V1 deterministically: status rank then gate_id then candidate key
# candidate key uses gate_title fallback gate_id (same fallback used later)
$eligibleSorted = $eligible | Sort-Object `
  @{ Expression = { Status-Rank (Get-IrGateStatus $_) }; Ascending = $true }, `
  @{ Expression = { (($_.gate_id + "").Trim()) }; Ascending = $true }, `
  @{ Expression = { $t = Get-IrGateTitle $_; if ($t) { $t } else { (($_.gate_id + "").Trim()) } }; Ascending = $true }

# Compare IR order vs Stage 2 order on gate_id sequence (report only)
$irSeq = @($eligible | ForEach-Object { (($_.gate_id + "").Trim()) })
$s2Seq = @($eligibleSorted | ForEach-Object { (($_.gate_id + "").Trim()) })
$orderingDisagreement = 0
for ($i=0; $i -lt [Math]::Min($irSeq.Count, $s2Seq.Count); $i++) {
  if ($irSeq[$i] -ne $s2Seq[$i]) { $orderingDisagreement++; break }
}

$selected = $eligibleSorted

$usedIds = @{}
$tasks = @()
$dropped = @()
$repairsApplied = @()

$notQueueable = @{
  INSUFFICIENT_EVIDENCE = 0
  MISSING_FROM_FILEMAP = 0
  TOO_MUCH_EVIDENCE = 0
  OTHER = 0
  MISSING_ON_DISK = 0
}

$dropped = @()

foreach ($g in $selected) {
  $gateId = ($g.gate_id + "").Trim()
  $gateTitle = Get-IrGateTitle $g
  $gateStatus = Get-IrGateStatus $g
  $accept = Get-IrAcceptanceText $g

  $candKey = $gateTitle
  if (-not $candKey) { $candKey = $gateId }

  # evidence universe (IR shape compatibility)
  $e0 = @()
  foreach ($p in @(Get-IrEvidenceUniverse $g)) {
    $np = Normalize-RepoPath ($p + "")
    if ($np) { $e0 += $np }
  }

  # de-dup preserve order
  $seen = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::Ordinal)
  $e1 = @()
  foreach ($p in $e0) {
    if (-not $seen.Contains($p)) { [void]$seen.Add($p); $e1 += $p }
  }

# filter to file map, but attempt deterministic repair for known missing paths
$missing = @()
$e2 = @()
$repairs = @()

foreach ($p in $e1) {
  # Context packet FILE MAP excludes docs/generated/** by design, so do not require membership there.
  # Still enforce on-disk existence later via MISSING_ON_DISK.
  if ($p.StartsWith("docs/generated/")) {
    $e2 += $p
    continue
  }

  if ($ctx.FileMap.Contains($p)) {
    $e2 += $p
    continue
  }

  $sub = Try-Substitute-MissingEvidence $p $ctx.FileMap
  if ($sub) {
    $e2 += $sub
    $repairs += ("{0} TO {1}" -f $p, $sub)
    $repairsApplied += ("gate_id={0} {1}" -f $gateId, ("{0}=>{1}" -f $p, $sub))
  } else {
    $missing += $p
  }
}

# de-dup again after substitutions
$seen2 = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::Ordinal)
$e2d = @()
foreach ($p in $e2) {
  if (-not $seen2.Contains($p)) { [void]$seen2.Add($p); $e2d += $p }
}
$e2 = $e2d

if ($missing.Count -gt 0) {
  $notQueueable.MISSING_FROM_FILEMAP++
  $dropped += [pscustomobject]@{
    gate_id = $gateId
    reason = "MISSING_FROM_FILEMAP"
    missing_paths = ($missing -join "; ")
  }
  continue
}

# Optional hardening: ensure NON-generated evidence paths actually exist on disk.
# docs/generated/** may be missing because the gate may be responsible for creating it.
$missingOnDisk = @()
foreach ($p in $e2) {
  if ($p.StartsWith("docs/generated/")) { continue }
  if (-not (Test-Path -LiteralPath $p)) { $missingOnDisk += $p }
}
if ($missingOnDisk.Count -gt 0) {
  $notQueueable.MISSING_ON_DISK++
  $dropped += [pscustomobject]@{
    gate_id = $gateId
    reason = "MISSING_ON_DISK"
    missing_paths = ($missingOnDisk -join "; ")
  }
  continue
}

if ($e2.Count -lt 2) {
  $notQueueable.INSUFFICIENT_EVIDENCE++
  $dropped += [pscustomobject]@{
    gate_id = $gateId
    reason = "INSUFFICIENT_EVIDENCE"
    missing_paths = ""
  }
  continue
}

  $anchor = Select-Anchor $e2 $null
  $eFinal = Trim-Evidence $e2 $anchor

if ($eFinal.Count -gt 6) {
  $notQueueable.TOO_MUCH_EVIDENCE++
  $dropped += [pscustomobject]@{
    gate_id = $gateId
    reason = "TOO_MUCH_EVIDENCE"
    missing_paths = ""
  }
  continue
}

  $taskId = Mint-TaskId $gateId $candKey $anchor $usedIds

  # Build a task object using schema-known keys only.
  $task = [ordered]@{}

  # Helper to set a key if it exists in schema
  function SetIfAllowed([string]$k, $v) {
    if ($taskProps.ContainsKey($k)) { $task[$k] = $v }
  }

  # Populate common keys (only if allowed)
  SetIfAllowed "gate_id" $gateId
  SetIfAllowed "task_id" $taskId
  SetIfAllowed "status" $gateStatus

# Preserve full text for LLM and human clarity
$fullGateTitle = (($gateTitle + "") -replace '\s+', ' ').Trim()

$title = ("{0}: {1}" -f $gateId, $fullGateTitle).Trim()
SetIfAllowed "title" $title

$intent = $fullGateTitle
if (-not $intent) { $intent = "Execute $gateId task using evidence paths." }
SetIfAllowed "intent" $intent

  SetIfAllowed "evidence_paths" $eFinal

  # Conservative constraints
  $constraints = @(
    "Single-session scope: stay within evidence and expected touch paths.",
    "If evidence is missing or tests fail, escalate via DEFAULT."
  )
  SetIfAllowed "constraints" $constraints

  $hints = Infer-CompletionHint $accept $eFinal
  SetIfAllowed "completion_hint" $hints

  # task_preflight provides traceability without extra schema keys
  if ($repairs.Count -gt 0) {
  # add deterministic repair notes for visibility
  $repairLine = "evidence_repairs=" + ($repairs -join ";")
} else {
  $repairLine = ""
}
$tp = @(
  ("anchor=" + $anchor),
  ("candidate_key_sha256=" + ([BitConverter]::ToString((Sha256Bytes $candKey)).Replace("-","").ToLowerInvariant().Substring(0,12)))
)
if ($repairLine -ne "") { $tp += $repairLine }
SetIfAllowed "task_preflight" $tp

  # escalation_rules required by schema: each item needs when + route, optional note
  if ($taskProps.ContainsKey("escalation_rules")) {
    $task["escalation_rules"] = @(
      [ordered]@{
        when = "DEFAULT"
        route = "STOP"
        note = "Escalate if blocked or scope expands."
      }
    )
  }

  # Validate required keys are present
  $missingReq = @()
  foreach ($rk in $taskRequired) { if (-not $task.Contains($rk)) { $missingReq += $rk } }
  if ($missingReq.Count -gt 0) {
    Write-FailAndExit 1 ($lines + @(
      "FAIL_SCHEMA",
      ("reason: task missing required keys for gate_id=$gateId task_id=$taskId"),
      ("missing_required_keys: " + ($missingReq -join ", ")),
      ("present_keys: " + (($task.Keys) -join ", "))
    ))
  }

  # Reject any extra keys (should not happen because we SetIfAllowed)
  foreach ($k in $task.Keys) {
    if (-not $taskProps.ContainsKey($k)) {
      Write-FailAndExit 1 ($lines + @("FAIL_SCHEMA","reason: task has unexpected key: $k"))
    }
  }

  # Evidence constraints hard-check
  if ($task["evidence_paths"].Count -lt 2 -or $task["evidence_paths"].Count -gt 6) {
    Write-FailAndExit 2 ($lines + @("FAIL_EVIDENCE","reason: evidence_paths count out of range for task_id=$taskId"))
  }

  $tasks += [pscustomobject]$task
}

# MULTIKEY_V1 task ordering (stable)
$tasksSorted = $tasks | Sort-Object `
  @{ Expression = { Status-RankTask $_.status }; Ascending = $true }, `
  @{ Expression = { @($_.evidence_paths).Count }; Ascending = $true }, `
  @{ Expression = { Get-BucketCount @($_.evidence_paths) }; Ascending = $true }, `
  @{ Expression = { ($_.gate_id + "") }; Ascending = $true }, `
  @{ Expression = { ($_.task_id + "") }; Ascending = $true }

# Apply cap after sorting (cap=0 means no cap)
# Sort-Object can return a scalar when only 1 task exists; normalize to array for .Count under StrictMode
$tasksSortedArr = @($tasksSorted)

if ($cap -gt 0 -and $tasksSortedArr.Count -gt $cap) {
  $tasks = @($tasksSortedArr | Select-Object -First $cap)
  $droppedByCap = ($tasksSortedArr.Count - $cap)
} else {
  $tasks = @($tasksSortedArr)
  $droppedByCap = 0
}

$queuedTotal = $tasks.Count

$lines += "candidates_total: $candidatesTotal"
$lines += "eligible_total: $eligibleTotal"
$lines += "cap_used: $cap"
$lines += "queued_total: $queuedTotal"
$lines += "ordering_disagreement_count: $orderingDisagreement"
$lines += "dropped_by_cap: $droppedByCap"
$lines += ""
$lines += "not_queueable:"
$lines += "  INSUFFICIENT_EVIDENCE: $($notQueueable.INSUFFICIENT_EVIDENCE)"
$lines += "  MISSING_FROM_FILEMAP: $($notQueueable.MISSING_FROM_FILEMAP)"
$lines += "  MISSING_ON_DISK: $($notQueueable.MISSING_ON_DISK)"
$lines += "  TOO_MUCH_EVIDENCE: $($notQueueable.TOO_MUCH_EVIDENCE)"
$lines += "  OTHER: $($notQueueable.OTHER)"
$lines += ""

$lines += "dropped_detail:"
if ($dropped.Count -eq 0) {
  $lines += "  (none)"
} else {
  foreach ($d in $dropped) {
    $lines += ("  - gate_id={0} reason={1} missing={2}" -f $d.gate_id, $d.reason, $d.missing_paths)
  }
}

$lines += ""

$lines += "repairs_applied:"
if ($repairsApplied.Count -eq 0) {
  $lines += "  (none)"
} else {
  foreach ($r in ($repairsApplied | Sort-Object)) {
    $lines += "  - $r"
  }
}

$lines += ""

# If we got here, PASS
$lines[0] = "PASS_FAIL: PASS"

# Write report
$repDir = Split-Path -Parent $OutReportPath
if ($repDir -and -not (Test-Path $repDir)) { New-Item -ItemType Directory -Path $repDir | Out-Null }
[System.IO.File]::WriteAllText($OutReportPath, (($lines -join "`n") + "`n"), [System.Text.UTF8Encoding]::new($false))

# Write append JSON only on PASS
$appDir = Split-Path -Parent $OutAppendPath
if ($appDir -and -not (Test-Path $appDir)) { New-Item -ItemType Directory -Path $appDir | Out-Null }

# Stable JSON emission
$jsonOut = ($tasks | ConvertTo-Json -Depth 50)
[System.IO.File]::WriteAllText($OutAppendPath, ($jsonOut + "`n"), [System.Text.UTF8Encoding]::new($false))

if ($Mode -eq "FULL") {
  $genUtc = Get-GitCommitIsoUtc $ctx.Head

  $contractVersion = "2.2"
  $queueIntent = "Stage 2 queue built from Stage 1 IR and repo truth at HEAD " + $ctx.Head

  $full = [ordered]@{
    queue_contract_version = $contractVersion
    queue_ordering = $queueOrderingConst
    generated_utc = $genUtc
    queue_intent = $queueIntent
    tasks = $tasks
  }

  $fullJson = ($full | ConvertTo-Json -Depth 80)
  $fullDir = Split-Path -Parent $OutFullPath
  if ($fullDir -and -not (Test-Path $fullDir)) { New-Item -ItemType Directory -Path $fullDir | Out-Null }
  [System.IO.File]::WriteAllText($OutFullPath, ($fullJson + "`n"), [System.Text.UTF8Encoding]::new($false))
}

exit 0
