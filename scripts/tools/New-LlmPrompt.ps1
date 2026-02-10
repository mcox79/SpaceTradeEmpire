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
  return (($p -replace "\\","/").TrimStart("/"))
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) { $RepoRoot = Get-RepoRootLocal }
Push-Location $RepoRoot
try {
  $genDir = Join-Path $RepoRoot "docs/generated"
  Ensure-Dir $genDir

  $contextRel = "docs/generated/01_CONTEXT_PACKET.md"
  $nextRel = "docs/generated/next_gate_packet.md"
  $attachRel = "docs/generated/llm_attachments.txt"

  $nextAbs = Join-Path $RepoRoot $nextRel
  if (-not (Test-Path -LiteralPath $nextAbs)) {
    throw "Missing $nextRel. Run New-NextGatePacket.ps1 (devtool next) first."
  }

  $existing = Read-Text (Join-Path $RepoRoot $attachRel)
  $lines = @()
  if (-not [string]::IsNullOrWhiteSpace($existing)) {
    $lines = @($existing -split "(`r`n|`n|`r)" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
  }

  $dedup = New-Object System.Collections.Generic.List[string]

  foreach ($l in $lines) {
    $p = Normalize-Rel $l
    if ([string]::IsNullOrWhiteSpace($p)) { continue }
    if ($p -eq $contextRel) { continue } # context always excluded from cap list
    if (-not $dedup.Contains($p)) { $dedup.Add($p) | Out-Null }
  }

  $final = New-Object System.Collections.Generic.List[string]

  $count = 0
  for ($i = 0; $i -lt $dedup.Count; $i++) {
    if ($count -ge $MaxAttachments) { break }
    $final.Add($dedup[$i]) | Out-Null
    $count++
  }

  $sbA = New-Object System.Text.StringBuilder
  foreach ($p in $final) { [void]$sbA.Append($p + [Environment]::NewLine) }
  Write-AtomicUtf8NoBom (Join-Path $RepoRoot $attachRel) ($sbA.ToString())

  $head = (& git rev-parse HEAD).Trim()
  $nextText = Read-Text $nextAbs

  $sb = New-Object System.Text.StringBuilder
  [void]$sb.Append("# LLM Prompt (HEAD $head)" + [Environment]::NewLine + [Environment]::NewLine)
  [void]$sb.Append("Hard guardrails:" + [Environment]::NewLine)
  [void]$sb.Append("- gate ids immutable once merged" + [Environment]::NewLine)
  [void]$sb.Append("- no deletions by default" + [Environment]::NewLine)
  [void]$sb.Append("- planning commits separate from execution commits" + [Environment]::NewLine)
  [void]$sb.Append("- DevTool continues generating docs/generated/01_CONTEXT_PACKET.md; prompt outputs additive" + [Environment]::NewLine)
  [void]$sb.Append("- max 6 attachments per LLM session (exclusive of docs/generated/01_CONTEXT_PACKET.md)" + [Environment]::NewLine)
  [void]$sb.Append([Environment]::NewLine)

  [void]$sb.Append("Routing (deterministic):" + [Environment]::NewLine)
  [void]$sb.Append("- If tests failing OR connectivity violations OR Validate-Gates fails: BASELINE FIX mode." + [Environment]::NewLine)
  [void]$sb.Append("- Else if next_gate_packet.md says Split required: YES: SPLIT mode (add subgates, do not delete parent)." + [Environment]::NewLine)
  [void]$sb.Append("- Else: EXECUTE mode." + [Environment]::NewLine)
  [void]$sb.Append([Environment]::NewLine)

  [void]$sb.Append("Task:" + [Environment]::NewLine)
  [void]$sb.Append("Execute exactly one gate: the Selected gate in next_gate_packet.md." + [Environment]::NewLine)
  [void]$sb.Append("Output required:" + [Environment]::NewLine)
  [void]$sb.Append("1) File edits (exact paths and edits)" + [Environment]::NewLine)
  [void]$sb.Append("2) Test command(s) to run" + [Environment]::NewLine)
  [void]$sb.Append("3) Commit plan (exact files staged + commit message)" + [Environment]::NewLine)
  [void]$sb.Append("Prohibited: planning additional gates, requesting broad extra context, exceeding attachment cap." + [Environment]::NewLine)
  [void]$sb.Append("Always attach docs/generated/01_CONTEXT_PACKET.md separately (excluded from cap). Attach only files listed below." + [Environment]::NewLine)
  [void]$sb.Append([Environment]::NewLine)

  [void]$sb.Append("## Attachments (in order)" + [Environment]::NewLine)
  [void]$sb.Append("- " + $contextRel + [Environment]::NewLine)
  foreach ($p in $final) { [void]$sb.Append("- " + $p + [Environment]::NewLine) }
  [void]$sb.Append([Environment]::NewLine)

  [void]$sb.Append("## Next Gate Packet" + [Environment]::NewLine)
  [void]$sb.Append($nextText + [Environment]::NewLine)

  Write-AtomicUtf8NoBom (Join-Path $genDir "llm_prompt.md") ($sb.ToString())

  Write-Host "New-LlmPrompt: OK (attachments excluding context=$count)"
}
finally {
  Pop-Location
}
