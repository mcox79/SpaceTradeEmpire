<#
.SYNOPSIS
  Validates docs/gates/gates.json for:
  - basic schema shape (MVP)
  - freeze rules vs baseline git ref (default: HEAD~1)

  Does NOT reconcile docs/55_GATES.md in MVP.
#>

[CmdletBinding()]
param(
  [string] $RepoRoot = "",
  [string] $RegistryRelPath = "docs/gates/gates.json",
  [string] $SchemaRelPath   = "docs/gates/gates.schema.json",
  [string] $BaselineRef     = "HEAD~1"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepoRootLocal {
  $root = (& git rev-parse --show-toplevel 2>$null)
  if (-not $root) { throw "Not in a git repo (git rev-parse failed)." }
  return $root.Trim()
}

function Read-JsonFile([string] $AbsPath) {
  if (-not (Test-Path -LiteralPath $AbsPath)) { throw "Missing file: $AbsPath" }
  $raw = Get-Content -LiteralPath $AbsPath -Raw -Encoding UTF8
  if ([string]::IsNullOrWhiteSpace($raw)) { throw "Empty JSON: $AbsPath" }

  try {
    return (ConvertFrom-Json -InputObject $raw)
  } catch {
    $msg = $_.Exception.Message
    throw "Invalid JSON in file: $AbsPath :: $msg"
  }
}

function Try-Read-JsonFromGit([string] $GitRef, [string] $RelPath) {
  try {
    $p = ($RelPath -replace "\\","/").TrimStart("/")

    # Native command output is often string[]; normalize to a single string deterministically.
    $lines = @(& git show "$GitRef`:$p" 2>$null)
    if (-not $lines -or $lines.Count -eq 0) { return $null }

    $raw = ($lines -join "`n")
    if ([string]::IsNullOrWhiteSpace($raw)) { return $null }

    return (ConvertFrom-Json -InputObject $raw)
  } catch {
    return $null
  }
}

function Assert([bool] $Cond, [string] $Message) {
  if (-not $Cond) { throw $Message }
}

function Is-ValidGateId([string] $s) {
  if ([string]::IsNullOrWhiteSpace($s)) { return $false }

  $t = $s.Trim()
  $t = $t -replace [char]0x00A0, ' '  # NBSP to space
  $t = $t.Trim()

  # Must be: GATE.<MID>.<NNN>
  if (-not $t.StartsWith("GATE.")) { return $false }

  $parts = $t.Split([char]'.')
  if ($parts.Length -lt 3) { return $false }
  if ($parts[0] -ne "GATE") { return $false }

  # Last token must be NNN
  $num = [string]$parts[$parts.Length - 1]

  # Middle tokens are 1..N segments between GATE and NNN
  $midParts = @()
  for ($i = 1; $i -le ($parts.Length - 2); $i++) { $midParts += [string]$parts[$i] }

  # Must have at least 1 middle segment
  if ($midParts.Count -lt 1) { return $false }

  # Each middle segment: only A-Z 0-9 _
  foreach ($seg in $midParts) {
    if ([string]::IsNullOrWhiteSpace($seg)) { return $false }
    foreach ($ch in $seg.ToCharArray()) {
      $code = [int][char]$ch
      $ok =
        (($code -ge 65) -and ($code -le 90)) -or      # A-Z
        (($code -ge 48) -and ($code -le 57)) -or      # 0-9
        ($code -eq 95)                                # _
      if (-not $ok) { return $false }
    }
  }

  # NNN: exactly 3 digits
  if ($num.Length -ne 3) { return $false }
  foreach ($ch in $num.ToCharArray()) {
    $code = [int][char]$ch
    if (-not (($code -ge 48) -and ($code -le 57))) { return $false }
  }

  return $true
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) { $RepoRoot = Get-RepoRootLocal }
Push-Location $RepoRoot
try {
  $regPath = Join-Path $RepoRoot $RegistryRelPath
  $schemaPath = Join-Path $RepoRoot $SchemaRelPath

  # Canonical registry path for baseline comparisons (freeze rules)
  $canonicalRegistryRel = "docs/gates/gates.json"

$reg = Read-JsonFile $regPath
$null = Read-JsonFile $schemaPath  # MVP: presence + parses

# gates.json supports two shapes:
# - Legacy registry (schema_version=1, gates=[...])
# - Queue contract v2.2 (queue_contract_version=2.2, tasks=[...])

$hasSchemaVersion = ($null -ne $reg.PSObject.Properties["schema_version"])
$hasQueueContract = ($null -ne $reg.PSObject.Properties["queue_contract_version"])

if ($hasQueueContract) {
  Assert (([string]$reg.queue_contract_version) -eq "2.2") "gates.json: queue_contract_version must be 2.2"
  Assert ($null -ne $reg.PSObject.Properties["tasks"]) "gates.json: missing tasks array"
  $gates = @($reg.tasks)
  $isQueueV22 = $true
} else {
  Assert ($hasSchemaVersion) "gates.json: missing schema_version (legacy) or queue_contract_version (v2.2)"
  Assert ($reg.schema_version -eq 1) "gates.json: schema_version must be 1"
  Assert ($null -ne $reg.PSObject.Properties["gates"]) "gates.json: missing gates array"
  $gates = @($reg.gates)
  $isQueueV22 = $false
}

  Assert ($gates.Count -le 50) "gates.json: max 50 gates allowed (MVP)"

$seen = @{}

if ($isQueueV22) {

  foreach ($t in $gates) {
    # Required identifiers
    Assert ($null -ne $t.task_id) "task missing task_id"
    Assert ($null -ne $t.gate_id) "task missing gate_id"

    $taskId = ([string]$t.task_id).Trim()
    $gateId = ([string]$t.gate_id).Trim()

    Assert (-not [string]::IsNullOrWhiteSpace($taskId)) "task task_id empty"
    Assert (-not [string]::IsNullOrWhiteSpace($gateId)) "task gate_id empty"
    Assert (Is-ValidGateId $gateId) "task ${taskId}: invalid gate_id $gateId"

    # Uniqueness by task_id
    Assert (-not $seen.ContainsKey($taskId)) "duplicate task_id: $taskId"
    $seen[$taskId] = $true

    # Minimal required fields for executability
    Assert ($null -ne $t.PSObject.Properties["status"]) "task ${taskId}: missing status"
    Assert (@("TODO","IN_PROGRESS","BLOCKED","DONE") -contains $t.status) "task ${taskId}: invalid status $($t.status)"

    Assert ($null -ne $t.PSObject.Properties["evidence_paths"]) "task ${taskId}: missing evidence_paths"
    $ev = @($t.evidence_paths)
    Assert ($ev.Count -ge 2 -and $ev.Count -le 6) "task ${taskId}: evidence_paths must be 2..6"

    Assert ($null -ne $t.PSObject.Properties["constraints"]) "task ${taskId}: missing constraints"
    $cs = @($t.constraints)
    Assert ($cs.Count -ge 1 -and $cs.Count -le 16) "task ${taskId}: constraints must be 1..16"

    Assert ($null -ne $t.PSObject.Properties["completion_hint"]) "task ${taskId}: missing completion_hint"
    $ch = @($t.completion_hint)
    Assert ($ch.Count -ge 1 -and $ch.Count -le 16) "task ${taskId}: completion_hint must be 1..16"

    Assert ($null -ne $t.PSObject.Properties["escalation_rules"]) "task ${taskId}: missing escalation_rules"
    $er = @($t.escalation_rules)
    Assert ($er.Count -ge 1 -and $er.Count -le 6) "task ${taskId}: escalation_rules must be 1..6"

    # Exactly one DEFAULT escalation rule (per schema intent)
    $defaultCount = 0
    foreach ($r in $er) {
    if ($null -ne $r -and $null -ne $r.PSObject.Properties["when"]) {
        if (([string]$r.when).Trim() -eq "DEFAULT") { $defaultCount++ }
    }
    }
    Assert ($defaultCount -eq 1) "task ${taskId}: escalation_rules must contain exactly one when=DEFAULT"
  }

} else {

  foreach ($g in $gates) {
    Assert ($null -ne $g.id) "gate missing id"

    $idRaw = [string]$g.id
    $id = $idRaw.Trim()

    # Normalize common invisible characters defensively
    $id = $id -replace [char]0x00A0, ' '  # NBSP to space
    $id = $id.Trim()

    Assert (Is-ValidGateId $id) "invalid gate id: $id"
    Assert (-not $seen.ContainsKey($id)) "duplicate gate id: $id"
    $seen[$id] = $true

    # Write normalized id back for downstream checks
    $g.id = $id

    Assert (-not [string]::IsNullOrWhiteSpace($g.title)) "gate $($g.id): title required"
    Assert (@("TODO","IN_PROGRESS","BLOCKED","DONE") -contains $g.status) "gate $($g.id): invalid status $($g.status)"
    Assert (@("S1","S1_5","S2","S3","S4","SUSTAINMENT","META") -contains $g.scope) "gate $($g.id): invalid scope $($g.scope)"

    Assert ($null -ne $g.freeze) "gate $($g.id): freeze object required"
    Assert ($g.freeze.id_immutable -eq $true) "gate $($g.id): freeze.id_immutable must be true"
    Assert ($g.freeze.title_immutable -eq $true) "gate $($g.id): freeze.title_immutable must be true"
    Assert ($g.freeze.created_utc_immutable -eq $true) "gate $($g.id): freeze.created_utc_immutable must be true"
    Assert ($g.freeze.no_delete -eq $true) "gate $($g.id): freeze.no_delete must be true"

    Assert ($null -ne $g.evidence) "gate $($g.id): evidence array required"
    foreach ($e in @($g.evidence)) {
      Assert ($null -ne $e.kind) "gate $($g.id): evidence missing kind"
      Assert ($null -ne $e.path) "gate $($g.id): evidence missing path"
      Assert (@("doc","test","code","generated","command") -contains $e.kind) "gate $($g.id): evidence kind invalid $($e.kind)"
      Assert (-not [string]::IsNullOrWhiteSpace($e.path)) "gate $($g.id): evidence path empty"
    }
  }

}

  # Freeze rules vs baseline (best-effort)
  $baseline = Try-Read-JsonFromGit $BaselineRef $canonicalRegistryRel
if (-not $isQueueV22 -and $null -ne $baseline -and $null -ne $baseline.gates) {
    $old = @($baseline.gates)
    $oldMap = @{}
    foreach ($og in $old) { $oldMap[$og.id] = $og }

    foreach ($oldId in $oldMap.Keys) {
      Assert ($seen.ContainsKey($oldId)) "freeze violation: gate deleted: $oldId"
      $ng = ($gates | Where-Object { $_.id -eq $oldId } | Select-Object -First 1)
      $og = $oldMap[$oldId]

      Assert ($ng.title -eq $og.title) "freeze violation: title changed for $oldId"
      if ($null -ne $og.created_utc -and $null -ne $ng.created_utc) {
        Assert ($ng.created_utc -eq $og.created_utc) "freeze violation: created_utc changed for $oldId"
      }
    }
  }

  Write-Host "Validate-Gates: OK ($($gates.Count) gates)" -ForegroundColor Green
  exit 0
}
finally {
  Pop-Location
}
