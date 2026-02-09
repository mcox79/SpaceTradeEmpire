param(
    [int] $MaxRows = 250
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    $root = (& git rev-parse --show-toplevel 2>$null)
    if (-not $root) { throw "Not a git repo (git rev-parse failed). Run from inside the repo." }
    return $root.Trim()
}

function Ensure-Dir([string] $Path) {
    if (-not (Test-Path $Path)) { New-Item -ItemType Directory -Force -Path $Path | Out-Null }
}

function Write-AllTextUtf8NoBom([string] $Path, [string] $Content) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $utf8NoBom)
}

function Normalize-Rel([string] $p) {
    if (-not $p) { return "" }
    $p = $p.Trim().Replace("\", "/")
    if ($p.StartsWith("./")) { $p = $p.Substring(2) }
    return $p
}

function Parse-GateEvidenceMap([string] $devPlanFullPath) {
    # Map evidence path string -> list of gate ids (based on B5..B8 tables only)
    $text = Get-Content -LiteralPath $devPlanFullPath -Raw
    $gateToEvidence = @{}

    $sections = @("### B5.", "### B6.", "### B7.", "### B8.")
    foreach ($sec in $sections) {
        $idx = $text.IndexOf($sec, [System.StringComparison]::Ordinal)
        if ($idx -lt 0) { continue }
        $sub = $text.Substring($idx)
        $tableStart = $sub.IndexOf("| Gate ID | Gate | Status | Evidence |", [System.StringComparison]::Ordinal)
        if ($tableStart -lt 0) { continue }
        $sub2 = $sub.Substring($tableStart)
        $stop = $sub2.IndexOf("### B", 1, [System.StringComparison]::Ordinal)
        if ($stop -gt 0) { $sub2 = $sub2.Substring(0, $stop) }
        $lines = $sub2 -split "`r?`n"

        foreach ($line in $lines) {
            $l = $line.Trim()
            if (-not $l.StartsWith("|")) { continue }
            if ($l -match '^\|\s*---') { continue }
            if ($l -match '^\|\s*Gate ID\s*\|') { continue }
            $parts = $l.Trim('|') -split '\|'
            if ($parts.Count -lt 4) { continue }
            $gateId = $parts[0].Trim()
            $evid = ($parts[3..($parts.Count-1)] -join "|").Trim()
            if ($gateId) { $gateToEvidence[$gateId] = $evid }
        }
    }

    return $gateToEvidence
}

function GateTagsForPath([hashtable] $gateToEvidence, [string] $relPath) {
    $rel = Normalize-Rel $relPath
    $tags = New-Object System.Collections.Generic.List[string]
    foreach ($k in ($gateToEvidence.Keys | Sort-Object)) {
        $e = $gateToEvidence[$k]
        if (-not $e) { continue }
        if ($e -like "*$rel*") { $tags.Add($k) | Out-Null }
    }
    return @($tags)
}

$repoRoot = Get-RepoRoot
Set-Location $repoRoot

$devPlan = Join-Path $repoRoot "docs\54_DEVELOPMENT_PLAN.md"
$gateToEvidence = Parse-GateEvidenceMap -devPlanFullPath $devPlan

$outDir = Join-Path $repoRoot "docs\generated"
Ensure-Dir $outDir
$outPath = Join-Path $outDir "capability_index.md"
$tmpPath = $outPath + ".tmp"
$now = Get-Date

$rows = New-Object System.Collections.Generic.List[object]

# Commands: any .cs under SimCore/Commands that contains ": ICommand"
$cmdDir = Join-Path $repoRoot "SimCore\Commands"
if (Test-Path $cmdDir) {
    $cmdFiles = Get-ChildItem -Path $cmdDir -Filter "*.cs" -File -ErrorAction SilentlyContinue | Sort-Object FullName
    foreach ($f in $cmdFiles) {
        $rel = Normalize-Rel ($f.FullName.Substring($repoRoot.Length).TrimStart("\"))
        $content = Get-Content -LiteralPath $f.FullName -Raw
        if ($content -notmatch ':\s*ICommand\b') { continue }

        $name = [System.IO.Path]::GetFileNameWithoutExtension($f.Name)
        $payload = ""

        # Heuristic: list public properties (very small, deterministic)
        $props = [regex]::Matches($content, 'public\s+[A-Za-z0-9_<>\[\]\?]+\s+([A-Za-z0-9_]+)\s*\{\s*get;')
        if ($props.Count -gt 0) {
            $payload = (@($props | ForEach-Object { $_.Groups[1].Value }) -join ", ")
        }

        $gates = GateTagsForPath -gateToEvidence $gateToEvidence -relPath $rel

        $rows.Add([pscustomobject]@{
            Kind = "Command"
            Name = $name
            SignatureOrPayload = $payload
            Path = $rel
            Gates = ($gates -join ", ")
        }) | Out-Null
    }
}

# Bridge: public methods on scripts/bridge/SimBridge.cs
$bridgePath = Join-Path $repoRoot "scripts\bridge\SimBridge.cs"
if (Test-Path $bridgePath) {
    $content = Get-Content -LiteralPath $bridgePath -Raw
    $rel = Normalize-Rel ("scripts/bridge/SimBridge.cs")

    # Very conservative method regex (no attributes handling, but stable and usually enough)
    $rx = [regex]'(?m)^\s*public\s+(?:static\s+)?([A-Za-z0-9_<>\[\]\?,\s]+)\s+([A-Za-z0-9_]+)\s*\(([^)]*)\)\s*\{'
    $ms = $rx.Matches($content)

    $gates = GateTagsForPath -gateToEvidence $gateToEvidence -relPath $rel

    foreach ($m in $ms) {
        $ret = ($m.Groups[1].Value -replace '\s+', ' ').Trim()
        $name = $m.Groups[2].Value.Trim()
        $args = ($m.Groups[3].Value -replace '\s+', ' ').Trim()
        $sig = "$ret $name($args)"

        $rows.Add([pscustomobject]@{
            Kind = "BridgeMethod"
            Name = $name
            SignatureOrPayload = $sig
            Path = $rel
            Gates = ($gates -join ", ")
        }) | Out-Null
    }
}

# Sort + cap
$final = @(
    $rows |
    Sort-Object Kind, Name, Path |
    Select-Object -First $MaxRows
)

$sb = New-Object System.Text.StringBuilder
$sb.AppendLine("# Capability Index") | Out-Null
$sb.AppendLine("") | Out-Null
$sb.AppendLine(("generated_at_local: {0}" -f $now.ToString("yyyy-MM-dd HH:mm:ss"))) | Out-Null
$sb.AppendLine("scope: B5..B8 evidence tags only") | Out-Null
$sb.AppendLine("") | Out-Null
$sb.AppendLine("| Kind | Name | SignatureOrPayload | Path | GateTags |") | Out-Null
$sb.AppendLine("|---|---|---|---|---|") | Out-Null

foreach ($r in $final) {
    $kind = $r.Kind
    $name = $r.Name
    $sig  = ($r.SignatureOrPayload -replace '\|','\/')
    $path = $r.Path
    $gts  = $r.Gates
    $sb.AppendLine(("| {0} | {1} | {2} | {3} | {4} |" -f $kind, $name, $sig, $path, $gts)) | Out-Null
}

Write-AllTextUtf8NoBom -Path $tmpPath -Content $sb.ToString()
Move-Item -Force -LiteralPath $tmpPath -Destination $outPath

Write-Host ("WROTE: {0}" -f $outPath)
exit 0
