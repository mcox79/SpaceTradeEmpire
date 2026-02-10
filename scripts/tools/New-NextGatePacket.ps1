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
      @{ Expression = { if ($null -ne $_.priority) { [int]$_.priority } else { 500 } }; Ascending = $true }, `
      @{ Expression = { $statusOrder[$_.status] }; Ascending = $true }, `
      @{ Expression = { $_.id }; Ascending = $true } |
    Select-Object -First $MaxActiveGates

  $nl = [Environment]::NewLine
  $nowUtc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")

  $attach = New-Object System.Collections.Generic.List[string]

  # Preferred evidence kinds for attachments (deterministic)
  $kindRank = @{
    "test" = 0
    "code" = 1
    "doc"  = 2
  }

  foreach ($g in $active) {

    # Collect eligible evidence entries with a stable rank, then sort
    $eligible = @()
    foreach ($e in @($g.evidence)) {

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

    if ($attach.Count -ge $MaxAttachments) { break }
  }

  $packet = New-Object System.Text.StringBuilder
  [void]$packet.Append("# Next Gate Packet (Generated $nowUtc UTC)$nl$nl")
  [void]$packet.Append("Hard guardrails:$nl")
  [void]$packet.Append("- gate ids immutable once merged$nl")
  [void]$packet.Append("- no deletions by default$nl")
  [void]$packet.Append("- planning commits separate from execution commits$nl")
  [void]$packet.Append("- DevTool continues generating docs/generated/01_CONTEXT_PACKET.md; prompt outputs additive$nl$nl")

  [void]$packet.Append("## Active gates (up to $MaxActiveGates)$nl$nl")
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
  [void]$attachSb.Append("docs/generated/01_CONTEXT_PACKET.md$nl")
  foreach ($p in $attach) { [void]$attachSb.Append("$p$nl") }
  Write-AtomicUtf8NoBom (Join-Path $genDir "llm_attachments.txt") ($attachSb.ToString())

  Write-Host "New-NextGatePacket: OK (active=$(@($active).Count), attachments=$($attach.Count))"
}
finally {
  Pop-Location
}
