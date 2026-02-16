[CmdletBinding()]
param(
  [string] $RepoRoot = "",
  [int]    $MaxAttachments = 6
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

function Read-Text([string] $AbsPath) {
  if (-not (Test-Path -LiteralPath $AbsPath)) { return "" }
  return (Get-Content -LiteralPath $AbsPath -Raw -Encoding UTF8)
}

function Normalize-Rel([string] $p) {
  return (($p -replace "\\","/").Trim()).TrimStart("/")
}

function Extract-HeadFromNextGatePacketFirstLine([string] $nextText) {
  if ([string]::IsNullOrWhiteSpace($nextText)) { return "" }

  $firstLine = $nextText -split "(`r`n|`n|`r)" | Select-Object -First 1
  $firstLine = ($firstLine + "").Trim()

  # Expected: "# Next Gate Packet (HEAD <sha>)"
  if ($firstLine -match '\(HEAD\s+([0-9a-fA-F]{40})\)') {
    return ($matches[1] + "").ToLowerInvariant()
  }

  return ""
}

function Extract-HeadFromContextPacket([string] $contextText) {
  if ([string]::IsNullOrWhiteSpace($contextText)) { return "" }

  # Look for: "head: <sha>" (per your ledger IR prompt contract)
  $m = [regex]::Match($contextText, '(?im)^\s*head\s*:\s*([0-9a-fA-F]{40})\s*$')
  if ($m.Success) { return ($m.Groups[1].Value + "").ToLowerInvariant() }

  # Fallback: any "(HEAD <sha>)" marker
  $m2 = [regex]::Match($contextText, '\(HEAD\s+([0-9a-fA-F]{40})\)')
  if ($m2.Success) { return ($m2.Groups[1].Value + "").ToLowerInvariant() }

  return ""
}

function Truncate-NextGatePacketToSelectedOnly([string] $nextText) {
  if ([string]::IsNullOrWhiteSpace($nextText)) { return "" }

  $lines = @($nextText -split "(`r`n|`n|`r)")
  $out = New-Object System.Collections.Generic.List[string]
  $stop = $false

  foreach ($ln in $lines) {
    if ($ln -match '^\s*##\s+Other active gates\b') { $stop = $true }
    if ($stop) { break }
    $out.Add($ln) | Out-Null
  }

  return ($out -join [Environment]::NewLine)
}

function Read-AttachmentsFile([string] $absPath) {
  $txt = Read-Text $absPath
  if ([string]::IsNullOrWhiteSpace($txt)) { return @() }

  return @(
    ($txt -split "(`r`n|`n|`r)") |
    ForEach-Object { Normalize-Rel ($_ + "") } |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
  )
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) { $RepoRoot = Get-RepoRootLocal }
Push-Location $RepoRoot
try {
  $genDir = Join-Path $RepoRoot "docs/generated"
  Ensure-Dir $genDir

  $contextRel = "docs/generated/01_CONTEXT_PACKET.md"
  $nextRel    = "docs/generated/next_gate_packet.md"
  $attachRel  = "docs/generated/llm_attachments.txt"
  $outRel     = "docs/generated/llm_prompt.md"

  $contextAbs = Join-Path $RepoRoot $contextRel
  $nextAbs    = Join-Path $RepoRoot $nextRel
  $attachAbs  = Join-Path $RepoRoot $attachRel
  $outAbs     = Join-Path $RepoRoot $outRel

  if (-not (Test-Path -LiteralPath $nextAbs)) {
    throw "Missing $nextRel. Run New-NextGatePacket.ps1 (devtool next) first."
  }
  if (-not (Test-Path -LiteralPath $contextAbs)) {
    throw "Missing $contextRel. Run New-ContextPacket.ps1 first."
  }

  $headGit = ((& git rev-parse HEAD).Trim()).ToLowerInvariant()

  $nextTextFull = Read-Text $nextAbs
  $nextHead = Extract-HeadFromNextGatePacketFirstLine $nextTextFull
  if ([string]::IsNullOrWhiteSpace($nextHead)) {
    throw "Cannot extract HEAD from $nextRel first line. Expected '(HEAD <40-hex>)'. Regenerate next gate packet."
  }

  $contextText = Read-Text $contextAbs
  $contextHead = Extract-HeadFromContextPacket $contextText
  if ([string]::IsNullOrWhiteSpace($contextHead)) {
    throw "Cannot extract HEAD from $contextRel. Expected line 'head: <40-hex>' (or '(HEAD <40-hex>)'). Regenerate context packet."
  }

  if ($headGit -ne $nextHead -or $headGit -ne $contextHead) {
    throw "HEAD mismatch: regenerate next gate packet and context packet (git=$headGit next=$nextHead context=$contextHead)"
  }

  # Attachments: read existing llm_attachments.txt, normalize + dedupe, enforce cap excluding context packet
  $existingAttach = @()
  if (Test-Path -LiteralPath $attachAbs) {
    $existingAttach = Read-AttachmentsFile $attachAbs
  }

  $dedup = New-Object System.Collections.Generic.List[string]
  foreach ($l in $existingAttach) {
    $p = Normalize-Rel $l
    if ([string]::IsNullOrWhiteSpace($p)) { continue }
    if ($p -eq $contextRel) { continue }
    if (-not $dedup.Contains($p)) { $dedup.Add($p) | Out-Null }
  }

  $final = New-Object System.Collections.Generic.List[string]
  $count = 0
  for ($i = 0; $i -lt $dedup.Count; $i++) {
    if ($count -ge $MaxAttachments) { break }
    $final.Add($dedup[$i]) | Out-Null
    $count++
  }

  # Rewrite llm_attachments.txt deterministically (context excluded)
  $sbA = New-Object System.Text.StringBuilder
  foreach ($p in $final) { [void]$sbA.Append($p + [Environment]::NewLine) }
  Write-AtomicUtf8NoBom $attachAbs ($sbA.ToString())

  $nextTextSelectedOnly = Truncate-NextGatePacketToSelectedOnly $nextTextFull

  # Build full Phase 3 template in llm_prompt.md
  $nl = [Environment]::NewLine
  $sb = New-Object System.Text.StringBuilder

  [void]$sb.Append("PHASE 3: LLM EXECUTION (HEAD $headGit)" + $nl + $nl)

  [void]$sb.Append("INSTRUCTIONS" + $nl)
  [void]$sb.Append("1) Attach ONLY the files listed under ATTACHMENTS." + $nl)
  [void]$sb.Append("2) Paste the prompt below into the LLM." + $nl)
  [void]$sb.Append("3) Execute exactly as instructed. Do not add extra files." + $nl + $nl)

  [void]$sb.Append("ATTACHMENTS (ONLY)" + $nl)
  [void]$sb.Append("- $contextRel" + $nl)
  foreach ($p in $final) { [void]$sb.Append("- $p" + $nl) }
  [void]$sb.Append($nl)

  [void]$sb.Append("----- BEGIN PROMPT -----" + $nl)
  [void]$sb.Append("# LLM Prompt (HEAD $headGit)" + $nl + $nl)

  [void]$sb.Append("Hard guardrails:" + $nl)
  [void]$sb.Append("- gate ids immutable once merged" + $nl)
  [void]$sb.Append("- no deletions by default (except deleting the completed task from docs/gates/gates.json during Step D)" + $nl)
  [void]$sb.Append("- planning commits separate from execution commits" + $nl)
  [void]$sb.Append("- max $MaxAttachments attachments per LLM session (exclusive of docs/generated/01_CONTEXT_PACKET.md)" + $nl + $nl)

  [void]$sb.Append("Routing (deterministic):" + $nl)
  [void]$sb.Append("- If tests failing OR connectivity violations OR Validate-Gates fails: BASELINE FIX mode." + $nl)
  [void]$sb.Append("- Else if next_gate_packet says Split required: YES: SPLIT mode (add subgates, do not delete parent)." + $nl)
  [void]$sb.Append("- Else: EXECUTE mode." + $nl + $nl)

  [void]$sb.Append("Task:" + $nl)
  [void]$sb.Append("Execute exactly one gate: the Selected task in next_gate_packet." + $nl + $nl)

  [void]$sb.Append("Output required (strict):" + $nl + $nl)

  [void]$sb.Append("A) EXECUTION_OUTPUT" + $nl + $nl)

  [void]$sb.Append("1) File edits (exact paths)" + $nl)
  [void]$sb.Append("Default output mode is PATCH, not full-file." + $nl + $nl)

  [void]$sb.Append("PATCH rules (must follow exactly):" + $nl)
  [void]$sb.Append("- For each edited file, output:" + $nl)
  [void]$sb.Append("  FILE: <exact repo-relative path using / separators>" + $nl)
  [void]$sb.Append("  PATCH:" + $nl)
  [void]$sb.Append("  - ANCHOR_START: <exact existing line to search for, copied verbatim>" + $nl)
  [void]$sb.Append("    REPLACE_RANGE:" + $nl)
  [void]$sb.Append("    <paste the exact lines to replace, copied verbatim>" + $nl)
  [void]$sb.Append("    WITH:" + $nl)
  [void]$sb.Append("    <new lines>" + $nl)
  [void]$sb.Append("  - (repeat per edit block)" + $nl)
  [void]$sb.Append("  ANCHOR_END: <exact existing line to search for, copied verbatim>" + $nl + $nl)

  [void]$sb.Append("- Every PATCH block must include BOTH an ANCHOR_START and an ANCHOR_END that exist exactly once in the current file." + $nl)
  [void]$sb.Append("- Keep patches minimal: do not reformat unrelated lines, do not reorder sections, do not rewrite entire documents unless necessary." + $nl)
  [void]$sb.Append("- FULL FILE output is allowed ONLY when:" + $nl)
  [void]$sb.Append("  a) creating a new file, OR" + $nl)
  [void]$sb.Append("  b) the file is <= 120 lines, OR" + $nl)
  [void]$sb.Append("  c) the change genuinely touches most of the file." + $nl)
  [void]$sb.Append("  If FULL FILE is used, explicitly label it:" + $nl)
  [void]$sb.Append("    FILE: <path>" + $nl)
  [void]$sb.Append("    FULL_CONTENTS:" + $nl)
  [void]$sb.Append("    <entire file>" + $nl + $nl)

  [void]$sb.Append("2) Proof command(s) to run (exact commands, deterministic)" + $nl)
  [void]$sb.Append("- Provide runnable commands only (PowerShell preferred)." + $nl)
  [void]$sb.Append("- Commands must capture exit code and output deterministically." + $nl + $nl)

  [void]$sb.Append("3) If proofs fail, stop and output DIAGNOSTICS" + $nl)
  [void]$sb.Append("- First failure only" + $nl)
  [void]$sb.Append("- Likely cause" + $nl)
  [void]$sb.Append("- Minimal next action" + $nl + $nl)

  [void]$sb.Append("B) CLOSEOUT_PATCH (always, no JSON)" + $nl)
  [void]$sb.Append("SESSION_LOG_LINE" + $nl)
  [void]$sb.Append("- One line to append to docs/56_SESSION_LOG.md (must start with ""- "")." + $nl + $nl)

  [void]$sb.Append("GATES_55_UPDATE" + $nl)
  [void]$sb.Append("- Either ""NO_CHANGE"" or:" + $nl)
  [void]$sb.Append("  - Find gate '<gate_id>' in docs/55_GATES.md and set status to DONE or IN_PROGRESS." + $nl)
  [void]$sb.Append("  - If DONE, include one closure proof phrase (max 180 chars)." + $nl + $nl)

  [void]$sb.Append("QUEUE_EDIT" + $nl)
  [void]$sb.Append("- Delete task_id '<task_id>' from docs/gates/gates.json tasks[]." + $nl)
  [void]$sb.Append("- Ensure pending_completion is null after closeout." + $nl + $nl)

  [void]$sb.Append("Prohibited:" + $nl)
  [void]$sb.Append("- JSON output (including finalize json)" + $nl)
  [void]$sb.Append("- planning additional gates" + $nl)
  [void]$sb.Append("- requesting broad extra context" + $nl)
  [void]$sb.Append("- exceeding attachment cap" + $nl)
  [void]$sb.Append("- rewriting canonical docs unless the task intent explicitly requires changes there" + $nl + $nl)

  [void]$sb.Append("## Attachments (in order)" + $nl)
  [void]$sb.Append("- $contextRel" + $nl)
  foreach ($p in $final) { [void]$sb.Append("- $p" + $nl) }
  [void]$sb.Append($nl)

  [void]$sb.Append("## Next Gate Packet (Selected only)" + $nl)
  [void]$sb.Append($nextTextSelectedOnly + $nl)
  [void]$sb.Append("----- END PROMPT -----" + $nl)

  Write-AtomicUtf8NoBom $outAbs ($sb.ToString())

  Write-Host "New-LlmPrompt: OK (head=$headGit attachments excluding context=$count)"
}
finally {
  Pop-Location
}
