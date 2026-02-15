[CmdletBinding()]
param(
  [string] $RepoRoot = "",
  [string] $FinalizePath = "",
  [string] $QueueRelPath = "docs/gates/gates.json",
  [string] $SessionLogRelPath = "docs/56_SESSION_LOG.md",
  [string] $OutRelPath = "docs/generated/phase4_closeout_patch.md",
  [switch] $AllowHeuristicFinalizePick
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

function Get-OptProp([object] $obj, [string] $name) {
  if ($null -eq $obj) { return $null }
  $p = $obj.PSObject.Properties.Match($name) | Select-Object -First 1
  if ($null -eq $p) { return $null }
  return $p.Value
}

function Normalize-NextAction([string] $s) {
  $x = ($s + "").Trim()
  if ([string]::IsNullOrWhiteSpace($x)) { return @($null, "missing next_action") }

  $allowed = @("STEP_A","STEP_B","STEP_D","STOP")

  if ($allowed -contains $x) { return @($x, $null) }

  # Strict normalization: allow "STEP_D|STOP" or "STEP_D,STOP" but only if STEP_D is present.
  $parts = @($x -split "[\|\s,]+" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
  $parts = @($parts | ForEach-Object { ($_ + "").Trim().ToUpperInvariant() } | Select-Object -Unique)

  if (($parts -contains "STEP_D") -and $parts.Count -ge 1) {
    return @("STEP_D", "normalized next_action from '$x' to 'STEP_D' (schema requires single enum value)")
  }

  return @($null, "invalid next_action '$x' (expected one of: STEP_A, STEP_B, STEP_D, STOP)")
}

function Pick-FinalizePathHeuristic([string] $RepoRootAbs) {
  $gen = Join-Path $RepoRootAbs "docs/generated"
  if (-not (Test-Path -LiteralPath $gen)) { return @($null, "docs/generated does not exist") }

  $candidates = @()
  $patterns = @("*finalize*.json","*completion*.json","*pending_completion*.json","*closeout*.json")
  foreach ($pat in $patterns) {
    $candidates += Get-ChildItem -LiteralPath $gen -Recurse -File -Filter $pat -ErrorAction SilentlyContinue
  }

  $candidates = @($candidates | Sort-Object LastWriteTime -Descending)
  if ($candidates.Count -eq 0) { return @($null, "no finalize-like json files found under docs/generated") }

  # If more than one, refuse unless caller explicitly allows heuristic pick.
  if ($candidates.Count -gt 1) {
    $top = @($candidates | Select-Object -First 8 FullName,LastWriteTime,Length)
    $msg = "multiple finalize-like json files found; pass -FinalizePath explicitly. Top candidates:`n" + ($top | ForEach-Object { "  - $($_.FullName) ($($_.LastWriteTime))" } | Out-String)
    return @($null, $msg.TrimEnd())
  }

  return @($candidates[0].FullName, $null)
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) { $RepoRoot = Get-RepoRootLocal }
Push-Location $RepoRoot
try {
  $repo = $RepoRoot

  if ([string]::IsNullOrWhiteSpace($FinalizePath)) {
    if (-not $AllowHeuristicFinalizePick) {
      throw "FinalizePath not provided. Re-run with -FinalizePath <path> or add -AllowHeuristicFinalizePick to pick from docs/generated/*finalize*.json."
    }
    $pick = Pick-FinalizePathHeuristic $repo
    $FinalizePath = $pick[0]
    $why = $pick[1]
    if ([string]::IsNullOrWhiteSpace($FinalizePath)) { throw $why }
  }

  $finalAbs = $FinalizePath
  if (-not [System.IO.Path]::IsPathRooted($finalAbs)) {
    $finalAbs = Join-Path $repo $FinalizePath
  }
  $final = Read-JsonFile $finalAbs

  $pc = Get-OptProp $final "pending_completion"
  if ($null -eq $pc) { throw "Finalize JSON missing pending_completion object: $finalAbs" }

  $gateId = (Get-OptProp $pc "gate_id") + ""
  $taskId = (Get-OptProp $pc "task_id") + ""
  $result = (Get-OptProp $pc "result") + ""
  $completedUtc = (Get-OptProp $pc "completed_utc") + ""
  $branch = (Get-OptProp $pc "branch") + ""
  $proofHint = (Get-OptProp $pc "proof_hint") + ""
  $deltaSummary = Get-OptProp $pc "delta_summary"
  $nextActionRaw = (Get-OptProp $pc "next_action") + ""

  if ([string]::IsNullOrWhiteSpace($gateId)) { throw "Finalize pending_completion.gate_id is missing" }
  if ([string]::IsNullOrWhiteSpace($taskId)) { throw "Finalize pending_completion.task_id is missing" }
  if ([string]::IsNullOrWhiteSpace($result)) { throw "Finalize pending_completion.result is missing" }
  if ([string]::IsNullOrWhiteSpace($completedUtc)) { throw "Finalize pending_completion.completed_utc is missing" }
  if ([string]::IsNullOrWhiteSpace($branch)) { throw "Finalize pending_completion.branch is missing" }
  if ([string]::IsNullOrWhiteSpace($proofHint)) { throw "Finalize pending_completion.proof_hint is missing" }

  $na = Normalize-NextAction $nextActionRaw
  $nextAction = $na[0]
  $nextActionWarn = $na[1]
  if ([string]::IsNullOrWhiteSpace($nextAction)) { throw $nextActionWarn }

  $queueAbs = Join-Path $repo $QueueRelPath
  $queue = Read-JsonFile $queueAbs

  $queueVer = (Get-OptProp $queue "queue_contract_version") + ""
  $queueOrd = (Get-OptProp $queue "queue_ordering") + ""
  if ($queueVer -ne "2.2") { throw "Queue contract mismatch: queue_contract_version='$queueVer' (expected '2.2')" }
  if ($queueOrd -ne "MULTIKEY_V1") { throw "Queue ordering mismatch: queue_ordering='$queueOrd' (expected 'MULTIKEY_V1')" }

  $queuePc = Get-OptProp $queue "pending_completion"
  if ($null -ne $queuePc) {
    $qid = (Get-OptProp $queuePc "task_id") + ""
    throw "Queue has pending_completion set (task_id='$qid'). Resolve it before generating another closeout patch."
  }

  $tasks = @($queue.tasks)
  $match = @($tasks | Where-Object { (($_.task_id + "").Trim()) -eq $taskId })
  if ($match.Count -ne 1) {
    $n = $match.Count
    throw "Task '$taskId' not found uniquely in queue.tasks (found=$n). Refuse to generate patch."
  }

  # Session log line (match existing style: "- YYYY-MM-DD, branch, GATE... RESULT (...) Evidence: ...")
  $date = ""
  try { $date = ([DateTime]::Parse($completedUtc)).ToString("yyyy-MM-dd") } catch { $date = ($completedUtc + "").Substring(0,10) }

  $shortProof = $proofHint.Trim()
  if ($shortProof.Length -gt 180) { $shortProof = $shortProof.Substring(0,177) + "..." }

  $evidence = @()
  $proofPaths = Get-OptProp $pc "proof_paths"
  if ($null -ne $proofPaths -and @($proofPaths).Count -gt 0) {
    foreach ($p in @($proofPaths)) {
      $s = ("" + $p).Trim()
      if ($s) { $evidence += $s }
    }
  }

  # If no proof_paths, attempt to pull obvious repo paths from delta_summary lines
  if ($evidence.Count -eq 0 -and $null -ne $deltaSummary -and @($deltaSummary).Count -gt 0) {
    $re = [regex]::new("(?<![A-Za-z0-9._/\-])(scripts/[A-Za-z0-9._/\-]+|SimCore/[A-Za-z0-9._/\-]+|addons/[A-Za-z0-9._/\-]+|docs/[A-Za-z0-9._/\-]+)")
    foreach ($line in @($deltaSummary)) {
      $t = ("" + $line)
      foreach ($m in $re.Matches($t)) {
        $evidence += ($m.Groups[1].Value)
      }
    }
    $evidence = @($evidence | Select-Object -Unique)
    if ($evidence.Count -gt 6) { $evidence = @($evidence | Select-Object -First 6) }
  }

  $evTail = ""
  if ($evidence.Count -gt 0) {
    $evTail = " Evidence: " + ($evidence -join "; ")
  }

  $logLine = "- $date, $branch, $gateId $result ($shortProof).$evTail"

  $nl = [Environment]::NewLine
  $sb = New-Object System.Text.StringBuilder
  [void]$sb.Append("# Step D Closeout Patch$nl$nl")
  [void]$sb.Append("Source finalize: $FinalizePath$nl$nl")

  if (-not [string]::IsNullOrWhiteSpace($nextActionWarn)) {
    [void]$sb.Append("WARNING:$nl- $nextActionWarn$nl$nl")
  }

  [void]$sb.Append("## Completion summary$nl")
  [void]$sb.Append("- gate_id: $gateId$nl")
  [void]$sb.Append("- task_id: $taskId$nl")
  [void]$sb.Append("- result: $result$nl")
  [void]$sb.Append("- completed_utc: $completedUtc$nl")
  [void]$sb.Append("- branch: $branch$nl")
  [void]$sb.Append("- next_action: $nextAction$nl$nl")

  [void]$sb.Append("## Queue edits (manual, controlled)$nl")
  [void]$sb.Append("In docs/gates/gates.json:$nl")
  [void]$sb.Append("1) Delete the task object with task_id '$taskId' from tasks[].$nl")
  [void]$sb.Append("2) Ensure pending_completion remains null after closeout (do not start a new task if pending_completion is set).$nl$nl")

  [void]$sb.Append("## Session log append (copy/paste exactly one line)$nl")
  [void]$sb.Append("Append to docs/56_SESSION_LOG.md:$nl$nl")
  [void]$sb.Append("```text$nl$logLine$nl```$nl$nl")

  [void]$sb.Append("## docs/55_GATES.md update$nl")
  [void]$sb.Append("Find gate '$gateId' in docs/55_GATES.md.$nl")
  [void]$sb.Append("- If this completion makes the entire gate DONE, set status to DONE and add/adjust closure proof as you see fit.$nl")
  [void]$sb.Append("- Otherwise set status to IN_PROGRESS (or leave as TODO if you do not track partials there).$nl$nl")

  [void]$sb.Append("## Delta summary (from finalize)$nl")
  if ($null -ne $deltaSummary -and @($deltaSummary).Count -gt 0) {
    foreach ($x in @($deltaSummary)) {
      $s = ("" + $x).Trim()
      if ($s) { [void]$sb.Append("- $s$nl") }
    }
  } else {
    [void]$sb.Append("- (none)$nl")
  }
  [void]$sb.Append($nl)

  $outAbs = Join-Path $repo $OutRelPath
  Write-AtomicUtf8NoBom $outAbs ($sb.ToString())

  Write-Host "New-StepDCloseoutPatch: OK ($OutRelPath)"
}
finally {
  Pop-Location
}
