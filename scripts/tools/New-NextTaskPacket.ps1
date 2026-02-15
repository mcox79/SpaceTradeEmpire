[CmdletBinding()]
param(
  [string] $RepoRoot = "",
  [string] $QueueRelPath = "docs/gates/gates.json",
  [int]    $MaxCandidates = 25,
  [int]    $MaxAttachments = 6,
  [string] $OutRelPath = "docs/generated/next_task_packet.md"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepoRootLocal {
  $root = (& git rev-parse --show-toplevel 2>$null)
  if (-not $root) { throw "Not in a git repo (git rev-parse failed)." }
  return $root.Trim()
}

function Ensure-Dir([string] $Dir) {
  if (-not (Test-Path -LiteralPath $Dir)) { New-Item -ItemType Directory -Force -Path $Dir | Out-Null }
}

function Write-AtomicUtf8NoBom([string] $Path, [string] $Content) {
  $dir = Split-Path -Parent $Path
  Ensure-Dir $dir
  $tmp = $Path + ".tmp"
  $enc = New-Object System.Text.UTF8Encoding($false)
  [System.IO.File]::WriteAllText($tmp, $Content, $enc)
  Move-Item -Force -LiteralPath $tmp -Destination $Path
}

function Read-JsonFile([string] $AbsPath) {
  if (-not (Test-Path -LiteralPath $AbsPath)) { throw "Missing file: $AbsPath" }
  $raw = Get-Content -LiteralPath $AbsPath -Raw -Encoding UTF8
  return ($raw | ConvertFrom-Json)
}

function Normalize-Rel([string] $p) {
  return (($p -replace "\\","/").TrimStart("/"))
}

function Get-OptProp([object] $obj, [string] $name) {
  if ($null -eq $obj) { return $null }
  $p = $obj.PSObject.Properties.Match($name) | Select-Object -First 1
  if ($null -eq $p) { return $null }
  return $p.Value
}

function Get-PropArrayCount([object] $obj, [string] $name, [int] $defaultValue) {
  $v = Get-OptProp $obj $name
  if ($null -eq $v) { return $defaultValue }
  return @($v).Count
}

function Get-HasNonEmptyArray([object] $obj, [string] $name) {
  $v = Get-OptProp $obj $name
  if ($null -eq $v) { return $false }
  $n = @($v | Where-Object { (($_ + "").Trim()) }).Count
  return ($n -gt 0)
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) { $RepoRoot = Get-RepoRootLocal }
Push-Location $RepoRoot
try {
  $repo = $RepoRoot
  $queueAbs = Join-Path $repo $QueueRelPath
  $q = Read-JsonFile $queueAbs

  $queueVer = (Get-OptProp $q "queue_contract_version") + ""
  $queueOrd = (Get-OptProp $q "queue_ordering") + ""
  if ($queueVer -ne "2.2") { throw "Queue contract mismatch: queue_contract_version='$queueVer' (expected '2.2')" }
  if ($queueOrd -ne "MULTIKEY_V1") { throw "Queue ordering mismatch: queue_ordering='$queueOrd' (expected 'MULTIKEY_V1')" }

  if ($null -ne (Get-OptProp $q "pending_completion")) {
    throw "Queue has pending_completion set. Perform Step D closeout first."
  }

  $tasks = @($q.tasks)
  if ($tasks.Count -eq 0) { throw "No tasks in queue." }

  # Ranking policy:
  # 1) IN_PROGRESS before TODO
  # 2) fewer evidence_paths
  # 3) fewer buckets
  # 4) not blocked
  # 5) tie-break lex (gate_id, task_id)
  $ranked = @($tasks) |
    Sort-Object `
      @{ Expression = { $st = (($_.status + "").Trim()); if ($st -eq "IN_PROGRESS") { 0 } else { 1 } }; Ascending = $true }, `
      @{ Expression = { Get-PropArrayCount $_ "evidence_paths" 999 }; Ascending = $true }, `
      @{ Expression = { Get-PropArrayCount $_ "buckets" 0 }; Ascending = $true }, `
      @{ Expression = { if (Get-HasNonEmptyArray $_ "blocked_by") { 1 } else { 0 } }; Ascending = $true }, `
      @{ Expression = { (($_.gate_id + "").Trim()) }; Ascending = $true }, `
      @{ Expression = { (($_.task_id + "").Trim()) }; Ascending = $true } |
    Select-Object -First $MaxCandidates

  $selected = $ranked[0]

  # Attachment shortlist: evidence_paths that exist and not under docs/generated or docs/gates
  $attach = New-Object System.Collections.Generic.List[string]
  $eligible = @()
  foreach ($p0 in @($selected.evidence_paths)) {
    $p = Normalize-Rel ([string]$p0)
    if ([string]::IsNullOrWhiteSpace($p)) { continue }
    if ($p.StartsWith("docs/generated/")) { continue }
    if ($p.StartsWith("docs/gates/")) { continue }
    $abs = Join-Path $repo ($p -replace "/","\\")
    if (-not (Test-Path -LiteralPath $abs)) { continue }
    $eligible += $p
  }
  $eligible = @($eligible | Sort-Object)
  foreach ($p in $eligible) {
    if (-not $attach.Contains($p)) { $attach.Add($p) | Out-Null }
    if ($attach.Count -ge $MaxAttachments) { break }
  }

  $nl = [Environment]::NewLine
  $head = (& git rev-parse HEAD).Trim()

  $sb = New-Object System.Text.StringBuilder
  [void]$sb.Append("# Next Task Packet (HEAD $head)$nl$nl")

  [void]$sb.Append("Ranking policy (deterministic):$nl")
  [void]$sb.Append("1) status IN_PROGRESS before TODO$nl")
  [void]$sb.Append("2) fewer evidence_paths$nl")
  [void]$sb.Append("3) fewer buckets$nl")
  [void]$sb.Append("4) not blocked$nl")
  [void]$sb.Append("5) tie-break lex (gate_id, task_id)$nl$nl")

  [void]$sb.Append("## Ranked candidates (top $($ranked.Count))$nl")
  $i = 1
  foreach ($t in $ranked) {
    $ep = Get-PropArrayCount $t "evidence_paths" 0
    $bk = Get-PropArrayCount $t "buckets" 0
    $bl = Get-PropArrayCount $t "blocked_by" 0
    [void]$sb.Append("$i. $($t.task_id) [$($t.status)] (gate $($t.gate_id)) evidence=$ep buckets=$bk blocked=$bl$nl")
    $i++
  }
  [void]$sb.Append($nl)

  [void]$sb.Append("## Selected$nl")
  [void]$sb.Append("- task_id: $($selected.task_id)$nl")
  [void]$sb.Append("- gate_id: $($selected.gate_id)$nl")
  [void]$sb.Append("- status: $($selected.status)$nl")
  if ($null -ne (Get-OptProp $selected "title")) { [void]$sb.Append("- title: $($selected.title)$nl") }
  [void]$sb.Append($nl)

  [void]$sb.Append("Intent:$nl")
  if ($null -ne (Get-OptProp $selected "intent")) { [void]$sb.Append("> $($selected.intent)$nl") } else { [void]$sb.Append("> (missing)$nl") }
  [void]$sb.Append($nl)

  [void]$sb.Append("Evidence (from queue):$nl")
  foreach ($p0 in @($selected.evidence_paths)) {
    $p = Normalize-Rel ([string]$p0)
    if (-not [string]::IsNullOrWhiteSpace($p)) { [void]$sb.Append("- $p$nl") }
  }
  [void]$sb.Append($nl)

  $constraints = Get-OptProp $selected "constraints"
  if ($null -ne $constraints -and @($constraints).Count -gt 0) {
    [void]$sb.Append("Constraints:$nl")
    foreach ($x in @($constraints)) {
      $s = ("" + $x).Trim()
      if ($s) { [void]$sb.Append("- $s$nl") }
    }
    [void]$sb.Append($nl)
  }

  $hints = Get-OptProp $selected "completion_hint"
  if ($null -ne $hints -and @($hints).Count -gt 0) {
    [void]$sb.Append("Completion hints:$nl")
    foreach ($x in @($hints)) {
      $s = ("" + $x).Trim()
      if ($s) { [void]$sb.Append("- $s$nl") }
    }
    [void]$sb.Append($nl)
  }

  [void]$sb.Append("Attachment shortlist (cap $MaxAttachments, excludes docs/generated/01_CONTEXT_PACKET.md):$nl")
  if ($attach.Count -eq 0) { [void]$sb.Append("- <<none proposed>>$nl") }
  else { foreach ($p in $attach) { [void]$sb.Append("- $p$nl") } }
  [void]$sb.Append($nl)

  $outAbs = Join-Path $repo $OutRelPath
  Write-AtomicUtf8NoBom $outAbs ($sb.ToString())

  Write-Host "New-NextTaskPacket: OK ($OutRelPath)"
}
finally {
  Pop-Location
}
