[CmdletBinding()]
param(
  [string] $RepoRoot = "",
  [string] $RegistryRelPath = "docs/gates/gates.json",
  [int]    $MaxActiveGates = 25,
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

function Read-JsonFile([string] $AbsPath) {
  if (-not (Test-Path -LiteralPath $AbsPath)) { throw "Missing file: $AbsPath" }
  $raw = Get-Content -LiteralPath $AbsPath -Raw -Encoding UTF8
  return ($raw | ConvertFrom-Json)
}

function Normalize-Rel([string] $p) {
  return (($p -replace "\\","/").TrimStart("/"))
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) { $RepoRoot = Get-RepoRootLocal }
Push-Location $RepoRoot
try {
  $regPath = Join-Path $RepoRoot $RegistryRelPath
  $reg = Read-JsonFile $regPath
  $gates = @($reg.gates)

  $statusOrder = @{
    "TODO" = 0
    "IN_PROGRESS" = 1
    "BLOCKED" = 2
    "DONE" = 3
  }

  $active = $gates |
    Where-Object { $_.status -ne "DONE" } |
    Sort-Object `
      @{ Expression = { if ($statusOrder.ContainsKey($_.status)) { [int]$statusOrder[$_.status] } else { 99 } }; Ascending = $true }, `
      @{ Expression = { if ($null -ne $_.priority) { [int]$_.priority } else { 500 } }; Ascending = $true }, `
      @{ Expression = { $_.id }; Ascending = $true } |
    Select-Object -First $MaxActiveGates

  if (@($active).Count -eq 0) { throw "No active gates found (all DONE?)" }
  $selected = $active[0]

  $nl = [Environment]::NewLine
  $head = (& git rev-parse HEAD).Trim()

  $attach = New-Object System.Collections.Generic.List[string]

  # Preferred evidence kinds for attachments (deterministic)
  $kindRank = @{
    "test" = 0
    "code" = 1
    "doc"  = 2
  }

  # Collect eligible evidence entries for SELECTED gate only, with a stable rank, then sort
  $eligible = @()
  foreach ($e in @($selected.evidence)) {

    $kRaw = [string]$e.kind
    $k = $kRaw.Trim().ToLowerInvariant()

    # Never attach generated artifacts or commands
    if ($k -eq "generated") { continue }
    if ($k -eq "command") { continue }

    # Only allow ranked kinds (test/code/doc)
    if (-not $kindRank.ContainsKey($k)) { continue }

    $p = Normalize-Rel ([string]$e.path)
    if ([string]::IsNullOrWhiteSpace($p)) { continue }

    # Keep your existing exclusions
    if ($p.StartsWith("docs/generated/")) { continue }
    if ($p.StartsWith("docs/gates/")) { continue }

    # Only attach files that exist on disk
    $abs = Join-Path $RepoRoot ($p -replace "/","\\")
    if (-not (Test-Path -LiteralPath $abs)) { continue }

    $eligible += [pscustomobject]@{
      rank = [int]$kindRank[$k]
      path = $p
    }
  }

  $eligible = $eligible | Sort-Object `
    @{ Expression = { $_.rank }; Ascending = $true }, `
    @{ Expression = { $_.path }; Ascending = $true }

  foreach ($row in $eligible) {
    $p = [string]$row.path
    if (-not $attach.Contains($p)) { $attach.Add($p) | Out-Null }
    if ($attach.Count -ge $MaxAttachments) { break }
  }

  # Split signal: YES if selected gate has more eligible evidence than we can attach under cap
  $splitRequired = ($eligible.Count -gt $MaxAttachments)

  $packet = New-Object System.Text.StringBuilder
  [void]$packet.Append("# Next Gate Packet (HEAD $head)$nl$nl")
  [void]$packet.Append("Hard guardrails:$nl")
  [void]$packet.Append("- gate ids immutable once merged$nl")
  [void]$packet.Append("- no deletions by default$nl")
  [void]$packet.Append("- planning commits separate from execution commits$nl")
  [void]$packet.Append("- DevTool continues generating docs/generated/01_CONTEXT_PACKET.md; prompt outputs additive$nl$nl")

  # Selected gate (single executable gate for this conversation)
  [void]$packet.Append("## Selected gate$nl$nl")
  [void]$packet.Append("Selected gate: $($selected.id)$nl")
  [void]$packet.Append("Status: $($selected.status)  Scope: $($selected.scope)$nl")
  [void]$packet.Append("Split required: " + ($(if ($splitRequired) { "YES" } else { "NO" })) + "$nl$nl")

  [void]$packet.Append("Definition of done:$nl")

  function Get-OptProp([object] $obj, [string] $name) {
    if ($null -eq $obj) { return $null }
    $p = $obj.PSObject.Properties.Match($name) | Select-Object -First 1
    if ($null -eq $p) { return $null }
    return $p.Value
  }

  $dod1 = Get-OptProp $selected "definition_of_done"
  $dod2 = Get-OptProp $selected "definitionOfDone"
  $dod3 = Get-OptProp $selected "dod"

  $dod = $null
  if ($null -ne $dod1 -and @($dod1).Count -gt 0) { $dod = $dod1 }
  elseif ($null -ne $dod2 -and @($dod2).Count -gt 0) { $dod = $dod2 }
  elseif ($null -ne $dod3 -and @($dod3).Count -gt 0) { $dod = $dod3 }

  if ($null -ne $dod -and @($dod).Count -gt 0) {
    foreach ($d in @($dod)) { [void]$packet.Append("- $d$nl") }
  } else {
    [void]$packet.Append("- (missing in gates.json for this gate)$nl")
  }

  [void]$packet.Append($nl)

  [void]$packet.Append("Evidence:$nl")
  if ($null -ne $selected.evidence -and @($selected.evidence).Count -gt 0) {
    foreach ($e in @($selected.evidence)) {
      $p = Normalize-Rel ([string]$e.path)
      $k = [string]$e.kind
      if (-not [string]::IsNullOrWhiteSpace($p)) {
        [void]$packet.Append("- [$k] $p$nl")
      }
    }
  } else {
    [void]$packet.Append("- (none declared)$nl")
  }
  [void]$packet.Append($nl)

  [void]$packet.Append("Attachment shortlist (cap $MaxAttachments, excludes docs/generated/01_CONTEXT_PACKET.md):$nl")
  if ($attach.Count -eq 0) { [void]$packet.Append("- <<none proposed>>$nl") }
  else { foreach ($p in $attach) { [void]$packet.Append("- $p$nl") } }
  [void]$packet.Append($nl)

  [void]$packet.Append("## Other active gates (informational, non-executable this conversation) (up to $MaxActiveGates)$nl$nl")

  foreach ($g in $active) {
    $prio = if ($null -ne $g.priority) { [int]$g.priority } else { 500 }
    [void]$packet.Append("### $($g.id) [$($g.status)] (prio $prio, scope $($g.scope))$nl")
    [void]$packet.Append("$($g.title)$nl$nl")
    if ($null -ne $g.evidence -and @($g.evidence).Count -gt 0) {
      [void]$packet.Append("Evidence:$nl")
      foreach ($e in @($g.evidence)) {
        $p = Normalize-Rel ([string]$e.path)
        $k = [string]$e.kind
        if (-not [string]::IsNullOrWhiteSpace($p)) {
          [void]$packet.Append("- [$k] $p$nl")
        }
      }
      [void]$packet.Append($nl)
    }
  }

  [void]$packet.Append("## LLM attachments (cap $MaxAttachments, excluding docs/generated/01_CONTEXT_PACKET.md)$nl$nl")
  if ($attach.Count -eq 0) {
    [void]$packet.Append("<<none proposed>>$nl")
  } else {
    foreach ($p in $attach) { [void]$packet.Append("- $p$nl") }
  }
  [void]$packet.Append($nl)

  $genDir = Join-Path $RepoRoot "docs/generated"
  Ensure-Dir $genDir

  Write-AtomicUtf8NoBom (Join-Path $genDir "next_gate_packet.md") ($packet.ToString())

  $attachSb = New-Object System.Text.StringBuilder
  foreach ($p in $attach) { [void]$attachSb.Append("$p$nl") }
  Write-AtomicUtf8NoBom (Join-Path $genDir "llm_attachments.txt") ($attachSb.ToString())

  Write-Host "New-NextGatePacket: OK (active=$(@($active).Count), attachments=$($attach.Count))"
}
finally {
  Pop-Location
}
