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
  [void]$sb.Append("- max $MaxAttachments attachments per LLM session (exclusive of docs/generated/01_CONTEXT_PACKET.md)" + $nl)
  [void]$sb.Append("- Preferred NO new files unless the task requires creating one" + $nl)
  [void]$sb.Append("- If you propose reflection or optional behavior, you must justify determinism and failure safety" + $nl + $nl)

  [void]$sb.Append("Non-negotiable design laws (hard):" + $nl)
  [void]$sb.Append("- Determinism first: no timestamps%wall-clock, no global RNG, no unordered iteration; outputs must be byte-for-byte stable for same inputs." + $nl)
  [void]$sb.Append("- Stable ordering required everywhere: any list output must declare and implement deterministic sort keys with explicit tie-breakers." + $nl)
  [void]$sb.Append("- Seed is world identity: persist through save%load; tests%transcripts must surface Seed in headers and failure messages for repro." + $nl)
  [void]$sb.Append("- Single mutation pipeline: UI and commands must not mutate state directly; commands enqueue intents only; kernel intent resolution is the only mutator." + $nl)
  [void]$sb.Append("- Boundary law: UI must not reference SimCore.Entities or SimCore.Systems; all reads via SimBridge snapshots%facts helpers." + $nl)
  [void]$sb.Append("- Runtime IO law: SimCore has no System.IO runtime IO; only allowed engine IO surfaces (res://, user://) as enforced by tests." + $nl)
  [void]$sb.Append("- Schema law: events%explain chains are schema-bound (tokens%fields), no free-text reasons; stable serialization%ordering." + $nl)
  [void]$sb.Append("- Tweaks law: any balance-affecting constants must route via versioned Tweaks with deterministic defaults; tests must be able to override without code changes (or be explicitly allowlisted)." + $nl + $nl)

  [void]$sb.Append("STOP conditions (hard):" + $nl)
  [void]$sb.Append("- STOP if you cannot state deterministic ordering keys%tie-breakers for any output list%log%table (do not guess)." + $nl)
  [void]$sb.Append("- STOP if you are about to introduce a balance constant and cannot justify Tweaks vs allowlist placement." + $nl)
  [void]$sb.Append("- STOP if the change risks violating UI boundaries or the single-mutation pipeline (do not ""just try"")." + $nl)
  [void]$sb.Append("- STOP if you did not run proofs but are about to claim PASS." + $nl + $nl)

  [void]$sb.Append("Routing (deterministic):" + $nl)
  [void]$sb.Append("- If tests failing OR connectivity violations OR Validate-Gates fails: BASELINE FIX mode." + $nl)
  [void]$sb.Append("- Else if next_gate_packet says Split required: YES: SPLIT mode (add subgates, do not delete parent)." + $nl)
  [void]$sb.Append("- Else: EXECUTE mode." + $nl + $nl)

  [void]$sb.Append("Task:" + $nl)
  [void]$sb.Append("Execute exactly one gate: the Selected task in next_gate_packet." + $nl + $nl)

  [void]$sb.Append("Output required (strict):" + $nl + $nl)
  [void]$sb.Append("You MUST output exactly these sections, in this order." + $nl + $nl)

  [void]$sb.Append("A) NARRATIVE_PLAN" + $nl + $nl)

  [void]$sb.Append("A1) What we are doing" + $nl)
  [void]$sb.Append("- 3 to 6 bullets explaining the intent in plain language." + $nl)
  [void]$sb.Append("- Must reference the Selected gate id and task id." + $nl + $nl)

  [void]$sb.Append("A2) Why this is the right change" + $nl)
  [void]$sb.Append("- 2 to 5 bullets." + $nl)
  [void]$sb.Append("- Must mention determinism risks being avoided (ordering, time sources, unordered iteration, etc.) where relevant." + $nl + $nl)

  [void]$sb.Append("A3) Files we will change" + $nl)
  [void]$sb.Append("- A numbered list." + $nl)
  [void]$sb.Append("- Each item: file path + 1 sentence describing the change." + $nl + $nl)

  [void]$sb.Append("A4) Files we will NOT change" + $nl)
  [void]$sb.Append("- 2 to 6 bullets listing the attached files that will remain unedited, if any." + $nl)
  [void]$sb.Append("- If all attached files will be edited, say: ""None""." + $nl + $nl)

  [void]$sb.Append("A5) Step by step apply plan (human followable)" + $nl)
  [void]$sb.Append("- A numbered list of steps." + $nl)
  [void]$sb.Append("- Each step must correspond to exactly one PATCH block that will appear in section B." + $nl)
  [void]$sb.Append("- Each step must say:" + $nl)
  [void]$sb.Append("  - Which file" + $nl)
  [void]$sb.Append("  - What to search for (a short snippet or the ANCHOR_START line)" + $nl)
  [void]$sb.Append("  - What will be replaced conceptually (1 sentence)" + $nl)
  [void]$sb.Append("  - What success looks like (1 sentence)" + $nl + $nl)

  [void]$sb.Append("B) EXECUTION_OUTPUT" + $nl + $nl)

  [void]$sb.Append("B1) File edits (exact paths)" + $nl)
  [void]$sb.Append("Default output mode is PATCH, not full-file." + $nl + $nl)

  [void]$sb.Append("PATCH rules (must follow exactly):" + $nl)
  [void]$sb.Append("- For each edited file, output:" + $nl)
  [void]$sb.Append("  FILE: <exact repo-relative path using / separators>" + $nl)
  [void]$sb.Append("  PATCH:" + $nl)
  [void]$sb.Append("  - ANCHOR_START: <exact existing line to search for, copied verbatim>" + $nl)
  [void]$sb.Append("    REPLACE_RANGE:" + $nl)
  [void]$sb.Append('```' + $nl)
  [void]$sb.Append("<paste the exact lines to replace, copied verbatim, in its own codebox>" + $nl)
  [void]$sb.Append('```' + $nl)
  [void]$sb.Append("    WITH:" + $nl)
  [void]$sb.Append('```' + $nl)
  [void]$sb.Append("<new lines, in their own codebox>" + $nl)
  [void]$sb.Append('```' + $nl)
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

  [void]$sb.Append("Anchor verification rule (hard):" + $nl)
  [void]$sb.Append("- If you cannot quote exact ANCHOR_START and ANCHOR_END from the attachments, you MUST stop and request the file re-upload." + $nl)
  [void]$sb.Append("- Do NOT provide approximate anchors or best guess patches." + $nl + $nl)

  [void]$sb.Append("B2) Proof command(s) to run (exact commands, deterministic)" + $nl)
  [void]$sb.Append("- Provide runnable commands only (PowerShell preferred)." + $nl)
  [void]$sb.Append("- Commands must capture exit code and output deterministically." + $nl)
  [void]$sb.Append("- Proof adequacy rule: proofs must cover the gate's acceptance surface, not just compile." + $nl)
  [void]$sb.Append("- Default expectation: run the full SimCore.Tests suite in Release unless you justify a narrower filter by naming the exact contract being exercised and why full-suite is unnecessary." + $nl)
  [void]$sb.Append("- If you modify any gate ledger%queue%generated contract surface (docs/55_GATES.md, docs/56_SESSION_LOG.md, docs/gates/gates.json, docs/generated contract artifacts), include scripts/tools/Validate-Gates.ps1 in proofs." + $nl)
  [void]$sb.Append("- Keep commands minimal, but not smaller than correctness." + $nl + $nl)

  [void]$sb.Append("B3) Stop conditions" + $nl)
  [void]$sb.Append("- If proofs fail, STOP and output DIAGNOSTICS:" + $nl)
  [void]$sb.Append("  - first failure only" + $nl)
  [void]$sb.Append("  - likely cause (1 to 3 bullets)" + $nl)
  [void]$sb.Append("  - minimal next action (1 to 3 bullets)" + $nl)
  [void]$sb.Append("- Do not propose broad refactors or additional gates in DIAGNOSTICS." + $nl + $nl)

  [void]$sb.Append("Spirit of development completion check (how to answer at the end):" + $nl)
  [void]$sb.Append("- Treat the gate as complete in the spirit of development if: acceptance surface is satisfied, proofs pass, and no Non-negotiable design laws are violated." + $nl)
    [void]$sb.Append("- Do NOT re-litigate broader architecture or propose ""ideal"" refactors unless they are required by the gate." + $nl)
  [void]$sb.Append("- If additional improvements exist but are not required for the gate, list them as OUT_OF_SCOPE_IMPROVEMENTS (max 3 bullets) and still answer YES." + $nl)
  [void]$sb.Append("- If the answer is NO, it must be because of a concrete unmet acceptance condition or a violated design law, and you must name it." + $nl)
  [void]$sb.Append("- Required format:" + $nl)
  [void]$sb.Append("  SPIRIT_CHECK: YES or NO" + $nl)
  [void]$sb.Append("  REASON: 1 to 3 bullets tied to gate acceptance%design laws" + $nl)
  [void]$sb.Append("  OUT_OF_SCOPE_IMPROVEMENTS: 0 to 3 bullets (optional)" + $nl + $nl)

  [void]$sb.Append("C) CLOSEOUT_PATCH (always, no JSON) when you're done and user confirms" + $nl + $nl)
  [void]$sb.Append("SESSION_LOG_LINE" + $nl)
  [void]$sb.Append("- One line to append to docs/56_SESSION_LOG.md." + $nl)
  [void]$sb.Append("- Format must match existing file and MUST start with ""- ""." + $nl)
  [void]$sb.Append("- Required fields:" + $nl)
  [void]$sb.Append("  - Date (YYYY-MM-DD)" + $nl)
  [void]$sb.Append("  - branch" + $nl)
  [void]$sb.Append("  - gate id" + $nl)
  [void]$sb.Append("  - result token PASS or FAIL" + $nl)
  [void]$sb.Append("  - Parenthetical summary containing ALL of:" + $nl)
  [void]$sb.Append("    - Plain-English description of what the test/scenario actually exercises (not just the gate title)" + $nl)
  [void]$sb.Append("    - Specific proof values or assertions that passed (e.g. exact tokens, hash match, counts)" + $nl)
  [void]$sb.Append("    - Save/load coverage if applicable" + $nl)
  [void]$sb.Append("    - Determinism properties (e.g. no timestamps, exits nonzero on drift)" + $nl)
  [void]$sb.Append("    - Total test count at time of close (e.g. 175/175 tests pass)" + $nl)
  [void]$sb.Append("    - Any generated artifact paths produced as evidence" + $nl)
  [void]$sb.Append("  - Evidence: semicolon-separated repo-relative paths" + $nl)
  [void]$sb.Append("- Model the line after existing entries in docs/56_SESSION_LOG.md before writing." + $nl + $nl)

  [void]$sb.Append("GATES_55_UPDATE" + $nl)
  [void]$sb.Append("- Either ""NO_CHANGE"" or:" + $nl)
  [void]$sb.Append("  - Find gate '<gate_id>' in docs/55_GATES.md and set status to DONE or IN_PROGRESS." + $nl)
  [void]$sb.Append("  - If DONE, include one closure proof phrase (max 180 chars)." + $nl + $nl)

  [void]$sb.Append("QUEUE_EDIT" + $nl)
  [void]$sb.Append("- Delete task_id '<task_id>' from docs/gates/gates.json tasks[]." + $nl)
  [void]$sb.Append("- Ensure pending_completion is null after closeout." + $nl + $nl)

  [void]$sb.Append("Prohibited:" + $nl)
  [void]$sb.Append("- JSON output" + $nl)
  [void]$sb.Append("- planning additional gates" + $nl)
  [void]$sb.Append("- requesting broad extra context" + $nl)
  [void]$sb.Append("- exceeding attachment cap" + $nl)
  [void]$sb.Append("- rewriting canonical docs unless the task intent explicitly requires changes there" + $nl)
  [void]$sb.Append("- claiming you verified anything not in attachments" + $nl + $nl)

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
