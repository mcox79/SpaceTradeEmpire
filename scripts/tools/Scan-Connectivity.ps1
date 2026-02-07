<# 
.SYNOPSIS
	Connectivity scanner v0 for Space Trade Empire.

.DESCRIPTION
	Scans repo files (best-effort string/pattern scan) and emits deterministic connectivity artifacts:
	1) docs/generated/connectivity_manifest.json
	2) docs/generated/connectivity_graph.json
	3) docs/generated/connectivity_violations.json

	SCOPE v0:
	- File-level only (no symbol-level call graph).
	- Best-effort detection of patterns:
		- Godot signals: "signal ", ".connect(", "Connect(", "EmitSignal"
		- Resource loads: "load(", "preload(", "ResourceLoader.Load"
		- Scene refs: ".tscn" string refs, PackedScene usage best-effort
		- C# events: "event ", "+=", "-=" (only when plausible)
		- Messaging: "Publish(", "Subscribe(" (best-effort)
	- Default excludes (always):
		addons/, _scratch/, docs/generated/, .git/
	- Optional hardened excludes (opt-in via -Harden):
		_archive/, GameShell/_DISABLED_scripts/, .godot/, SimCore/bin/, SimCore/obj/
	- Deterministic outputs: stable ordering, stable path normalization, stable JSON emit.
	- Evidence required per edge: file path + line number(s) and optionally a short snippet.

	VIOLATIONS v0:
	- Hard invariant: any SimCore file referencing "Godot." is a violation (error).

.PARAMETER RepoRoot
	Optional repo root override. If omitted, will use `git rev-parse --show-toplevel`.

.PARAMETER DebugDumpPath
	Optional path to write a debug log (text). If omitted, no debug log is written.

.PARAMETER Force
	Overwrite output files if they exist.

.PARAMETER Harden
	Enables additional excludes to reduce churn and improve signal:
	_archive/, GameShell/_DISABLED_scripts/, .godot/, SimCore/bin/, SimCore/obj/

.PARAMETER ExtraExcludeDirs
	Optional additional repo-relative directory prefixes to exclude (use forward slashes),
	examples: "third_party/", "tmp/". These are treated as directory prefixes.

.EXAMPLE
	powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Scan-Connectivity.ps1 -Verbose -Force

.EXAMPLE
	powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Scan-Connectivity.ps1 -Verbose -Force -Harden

.EXAMPLE
	powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/Scan-Connectivity.ps1 -Verbose -Force -Harden -ExtraExcludeDirs "third_party/","tmp/"

.NOTES
	PowerShell 5.1 compatible. UTF-8 no BOM output. Stable formatting and sorting.
#>

[CmdletBinding()]
param(
	[Parameter(Mandatory = $false)]
	[string] $RepoRoot = "",

	[Parameter(Mandatory = $false)]
	[string] $DebugDumpPath = "",

	[Parameter(Mandatory = $false)]
	[switch] $Force,

	[Parameter(Mandatory = $false)]
	[switch] $Harden,

	[Parameter(Mandatory = $false)]
	[string[]] $ExtraExcludeDirs = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function IntCount {
	param([object] $Value)
	return [int](@($Value).Count)
}

function AsIntArray {
	param([object] $Value)
	$vals = @($Value)
	$list = New-Object 'System.Collections.Generic.List[int]'
	for ($i = 0; $i -lt (IntCount $vals); $i++) {
		$list.Add([int]$vals[$i])
	}
	return ,([int[]]$list.ToArray())
}

function Write-DebugLogLine {
	param(
		[string] $Path,
		[string] $Line
	)
	if ([string]::IsNullOrWhiteSpace($Path)) { return }
	$dir = Split-Path -Parent $Path
	if (-not [string]::IsNullOrWhiteSpace($dir) -and -not (Test-Path -LiteralPath $dir)) {
		New-Item -ItemType Directory -Path $dir -Force | Out-Null
	}
	Add-Content -LiteralPath $Path -Value $Line -Encoding UTF8
}

function Get-RepoRoot {
	param([string] $Override)
	if (-not [string]::IsNullOrWhiteSpace($Override)) {
		return (Resolve-Path -LiteralPath $Override).Path
	}

	$root = ""
	try {
		$root = (& git rev-parse --show-toplevel 2>$null)
	} catch {
		$root = ""
	}

	if (-not $root) {
		throw "Unable to determine repo root. Provide -RepoRoot."
	}

	return $root.ToString().Trim()
}

function Normalize-RepoRelPath {
	param(
		[string] $AbsPath,
		[string] $RootAbs
	)
	$full = (Resolve-Path -LiteralPath $AbsPath).Path
	$root = (Resolve-Path -LiteralPath $RootAbs).Path

	if ($full.Length -lt $root.Length) {
		throw "Path normalization error: file path not under repo root."
	}

	$rel = $full.Substring($root.Length)
	if ($rel.StartsWith("\") -or $rel.StartsWith("/")) {
		$rel = $rel.Substring(1)
	}

	$rel = $rel -replace "\\", "/"
	return $rel
}

function Normalize-DirPrefix {
	param([string] $Prefix)
	if ([string]::IsNullOrWhiteSpace($Prefix)) { return $null }
	$p = $Prefix.Trim() -replace "\\", "/"
	while ($p.StartsWith("/")) { $p = $p.Substring(1) }
	if (-not $p.EndsWith("/")) { $p = $p + "/" }
	return $p
}

function Get-ActiveExcludePrefixes {
	param(
		[switch] $UseHarden,
		[string[]] $Extra
	)

	$base = @(
        ".git/",
        ".godot/",
        ".import/",
        ".mono/",
        "addons/",
        "_scratch/",
        "docs/generated/",
        "bin/",
        "obj/"
    )

	$harden = @(
        "_archive/",
        "GameShell/_DISABLED_scripts/"
    )

	$out = @()
	foreach ($x in $base) { $out += ,(Normalize-DirPrefix $x) }

	if ($UseHarden) {
		foreach ($x in $harden) { $out += ,(Normalize-DirPrefix $x) }
	}

	$extraArr = @($Extra)
	for ($i = 0; $i -lt (IntCount $extraArr); $i++) {
		$nx = Normalize-DirPrefix $extraArr[$i]
		if (-not [string]::IsNullOrWhiteSpace($nx)) {
			$out += ,$nx
		}
	}

	$out = @($out | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
	return @($out)
}

function Is-ExcludedPath {
	param(
		[string] $RepoRelPath,
		[string[]] $ExcludePrefixes
	)

	$p = ($RepoRelPath -replace "\\", "/").ToLowerInvariant()

	$ex = @($ExcludePrefixes)
	for ($i = 0; $i -lt (IntCount $ex); $i++) {
		$pref = ($ex[$i] -replace "\\", "/").ToLowerInvariant()

		if ([string]::IsNullOrWhiteSpace($pref)) { continue }

		if ($p.StartsWith($pref)) { return $true }

		# Subdirectory match: ".../<pref>..."
		# pref already ends with "/"
		$needle = "/" + $pref
		if ($p.Contains($needle)) { return $true }
	}

	return $false
}

function Get-AllScanFiles {
	param(
		[string] $RootAbs,
		[string[]] $ExcludePrefixes
	)

	$all = Get-ChildItem -LiteralPath $RootAbs -Recurse -File -Force |
		Where-Object {
			$rel = Normalize-RepoRelPath -AbsPath $_.FullName -RootAbs $RootAbs
			-not (Is-ExcludedPath -RepoRelPath $rel -ExcludePrefixes $ExcludePrefixes)
		}

	$sorted = $all | Sort-Object @{ Expression = { Normalize-RepoRelPath -AbsPath $_.FullName -RootAbs $RootAbs }; Ascending = $true }

	return @($sorted)
}

function Read-AllLinesSafe {
	param([string] $AbsPath)

	try {
		$text = [System.IO.File]::ReadAllText($AbsPath, [System.Text.Encoding]::UTF8)
	} catch {
		$text = [System.IO.File]::ReadAllText($AbsPath)
	}

	$text = $text -replace "`r`n", "`n"
	$text = $text -replace "`r", "`n"
	$lines = $text.Split(@("`n"), [System.StringSplitOptions]::None)

	return ,@($lines)
}

function Ensure-Dir {
	param([string] $AbsDir)
	if (-not (Test-Path -LiteralPath $AbsDir)) {
		New-Item -ItemType Directory -Path $AbsDir -Force | Out-Null
	}
}

function New-Utf8NoBom {
	return (New-Object System.Text.UTF8Encoding($false))
}

function ConvertTo-OrderedObject {
	param([object] $Value)

	if ($null -eq $Value) { return $null }

	if ($Value -is [System.Collections.IDictionary]) {
		$keys = @($Value.Keys | ForEach-Object { $_.ToString() }) | Sort-Object
		$ord = New-Object System.Collections.Specialized.OrderedDictionary
		foreach ($k in $keys) {
			$ord.Add($k, (ConvertTo-OrderedObject -Value $Value[$k]))
		}
		return $ord
	}

	if ($Value -is [psobject] -and -not ($Value -is [string])) {
		$props = @($Value.PSObject.Properties | Where-Object { $_.MemberType -eq "NoteProperty" -or $_.MemberType -eq "Property" }) |
			ForEach-Object { $_.Name } |
			Sort-Object

		if ((IntCount $props) -gt 0) {
			$ord2 = New-Object System.Collections.Specialized.OrderedDictionary
			foreach ($p in $props) {
				$ord2.Add($p, (ConvertTo-OrderedObject -Value $Value.$p))
			}
			return $ord2
		}
	}

	if ($Value -is [System.Collections.IEnumerable] -and -not ($Value -is [string])) {
		$arr = @()
		foreach ($item in $Value) {
			$arr += ,(ConvertTo-OrderedObject -Value $item)
		}
		return ,@($arr)
	}

	return $Value
}

function Write-StableJsonFile {
	param(
		[string] $AbsPath,
		[object] $Data,
		[switch] $Overwrite
	)

	if ((Test-Path -LiteralPath $AbsPath) -and (-not $Overwrite)) {
		throw "Output file exists and -Force not specified: $AbsPath"
	}

	$dir = Split-Path -Parent $AbsPath
	Ensure-Dir -AbsDir $dir

	$ordered = ConvertTo-OrderedObject -Value $Data
	$json = $ordered | ConvertTo-Json -Depth 60

	if (-not $json.EndsWith("`n")) { $json = $json + "`n" }

	[System.IO.File]::WriteAllText($AbsPath, $json, (New-Utf8NoBom))
}

function Is-SimCoreFile {
	param([string] $RepoRelPath)
	$p = ($RepoRelPath -replace "\\", "/").ToLowerInvariant()
	if ($p.StartsWith("simcore/")) { return $true }
	if ($p -match "(^|/)simcore/") { return $true }
	return $false
}

function Truncate-Snippet {
	param([string] $Text, [int] $MaxLen)
	if ($null -eq $Text) { return "" }
	$t = $Text.Trim()
	if ($t.Length -le $MaxLen) { return $t }
	return $t.Substring(0, $MaxLen)
}

function Resolve-RefToRepoPath {
	param(
		[string] $RawRef,
		[string] $FromRepoRelPath,
		[hashtable] $KnownNodesSet
	)

	if ([string]::IsNullOrWhiteSpace($RawRef)) { return $null }

	$ref = ($RawRef.Trim() -replace "\\", "/")

	if ($ref.ToLowerInvariant().StartsWith("res://")) {
		$ref = $ref.Substring(6)
	}

	while ($ref.StartsWith("/")) { $ref = $ref.Substring(1) }

	$c1 = $ref

	$fromDir = ""
	if ($FromRepoRelPath -match "/") {
		$fromDir = $FromRepoRelPath.Substring(0, $FromRepoRelPath.LastIndexOf("/"))
	}

	$c2 = $c1
	if (-not [string]::IsNullOrWhiteSpace($fromDir)) {
		$c2 = ($fromDir + "/" + $ref)
	}

	$c1n = ($c1 -replace "/{2,}", "/").Trim()
	$c2n = ($c2 -replace "/{2,}", "/").Trim()

	if ($KnownNodesSet.ContainsKey($c1n)) { return $c1n }
	if ($KnownNodesSet.ContainsKey($c2n)) { return $c2n }

	return $null
}

function Add-Hit {
	param(
		[hashtable] $HitsByFile,
		[string] $FileRel,
		[string] $HitKey
	)
	if (-not $HitsByFile.ContainsKey($FileRel)) {
		$HitsByFile[$FileRel] = @{}
	}
	$h = $HitsByFile[$FileRel]
	if (-not $h.ContainsKey($HitKey)) { $h[$HitKey] = 0 }
	$h[$HitKey] = [int]$h[$HitKey] + 1
}

function Add-EdgeEvidence {
	param(
		[hashtable] $EdgeMap,
		[string] $FromRel,
		[string] $ToRel,
		[string] $EdgeType,
		[string] $SourceRel,
		[int] $LineNumber1Based,
		[string] $Snippet
	)

	$key = ($FromRel + "||" + $ToRel + "||" + $EdgeType)

	if (-not $EdgeMap.ContainsKey($key)) {
		$EdgeMap[$key] = [pscustomobject]@{
			from = $FromRel
			to = $ToRel
			type = $EdgeType
			evidence = @()
		}
	}

	$edge = $EdgeMap[$key]
	$evList = @($edge.evidence)

	$bucket = $null
	for ($i = 0; $i -lt (IntCount $evList); $i++) {
		if ($evList[$i].file -eq $SourceRel) {
			$bucket = $evList[$i]
			break
		}
	}

	if ($null -eq $bucket) {
		$bucket = [pscustomobject]@{
			file = $SourceRel
			lines = [int[]]@()
			snippet = ""
		}
		$evList += ,$bucket
	}

	$linesArr = @($bucket.lines)
	$linesArr += ,([int]$LineNumber1Based)
	$bucket.lines = AsIntArray (@($linesArr | Sort-Object -Unique))

	if ([string]::IsNullOrWhiteSpace($bucket.snippet) -and -not [string]::IsNullOrWhiteSpace($Snippet)) {
		$bucket.snippet = $Snippet
	}

	$edge.evidence = $evList
	$EdgeMap[$key] = $edge
}

function Normalize-EdgesForJson {
	param([object[]] $Edges)
	for ($i = 0; $i -lt (IntCount $Edges); $i++) {
		$e = $Edges[$i]
		$ev = @($e.evidence)
		for ($j = 0; $j -lt (IntCount $ev); $j++) {
			$ev[$j].lines = AsIntArray (@($ev[$j].lines | Sort-Object -Unique))
			if ($null -eq $ev[$j].snippet) { $ev[$j].snippet = "" }
		}
		$e.evidence = $ev
		$Edges[$i] = $e
	}
	return ,@($Edges)
}

function Scan-Repo {
	param(
		[string] $RootAbs,
		[string] $OutDirAbs,
		[string] $DbgPath,
		[string[]] $ExcludePrefixes
	)

	$files = Get-AllScanFiles -RootAbs $RootAbs -ExcludePrefixes $ExcludePrefixes

	$nodeRelPaths = @()
	foreach ($f in $files) {
		$nodeRelPaths += ,(Normalize-RepoRelPath -AbsPath $f.FullName -RootAbs $RootAbs)
	}
	$nodeRelPaths = @($nodeRelPaths | Sort-Object)

	$knownSet = @{}
	foreach ($p in $nodeRelPaths) { $knownSet[$p] = $true }

	$hitsByFile = @{}
	$edgeMap = @{}
	$violations = @()

	$rxLoad = New-Object System.Text.RegularExpressions.Regex('(?i)\b(load|preload)\s*\(\s*["'']([^"'']+)["'']', [System.Text.RegularExpressions.RegexOptions]::Compiled)
	$rxResLoader = New-Object System.Text.RegularExpressions.Regex('(?i)\bResourceLoader\.Load\b[^\(]*\(\s*["'']([^"'']+)["'']', [System.Text.RegularExpressions.RegexOptions]::Compiled)
	$rxTscnString = New-Object System.Text.RegularExpressions.Regex('(?i)["'']([^"'']+\.tscn)["'']', [System.Text.RegularExpressions.RegexOptions]::Compiled)

	$rxSignalDecl = New-Object System.Text.RegularExpressions.Regex('(?i)^\s*signal\s+\w+', [System.Text.RegularExpressions.RegexOptions]::Compiled)
	$rxConnectDot = New-Object System.Text.RegularExpressions.Regex('(?i)\.connect\s*\(', [System.Text.RegularExpressions.RegexOptions]::Compiled)
	$rxConnectCS = New-Object System.Text.RegularExpressions.Regex('(?i)\bConnect\s*\(', [System.Text.RegularExpressions.RegexOptions]::Compiled)
	$rxEmitSignal = New-Object System.Text.RegularExpressions.Regex('(?i)\bEmitSignal\b', [System.Text.RegularExpressions.RegexOptions]::Compiled)

	$rxPackedScene = New-Object System.Text.RegularExpressions.Regex('(?i)\bPackedScene\b', [System.Text.RegularExpressions.RegexOptions]::Compiled)

	$rxEventDecl = New-Object System.Text.RegularExpressions.Regex('(?i)\bevent\s+\w', [System.Text.RegularExpressions.RegexOptions]::Compiled)
	$rxAddAssign = New-Object System.Text.RegularExpressions.Regex('\+=', [System.Text.RegularExpressions.RegexOptions]::Compiled)
	$rxSubAssign = New-Object System.Text.RegularExpressions.Regex('-=', [System.Text.RegularExpressions.RegexOptions]::Compiled)

	$rxPublish = New-Object System.Text.RegularExpressions.Regex('(?i)\bPublish\s*\(', [System.Text.RegularExpressions.RegexOptions]::Compiled)
	$rxSubscribe = New-Object System.Text.RegularExpressions.Regex('(?i)\bSubscribe\s*\(', [System.Text.RegularExpressions.RegexOptions]::Compiled)

	$rxGodotNamespace = New-Object System.Text.RegularExpressions.Regex('Godot\.', [System.Text.RegularExpressions.RegexOptions]::Compiled)

	Write-DebugLogLine -Path $DbgPath -Line "Scan-Connectivity v0"
	Write-DebugLogLine -Path $DbgPath -Line ("RepoRoot: " + $RootAbs)
	Write-DebugLogLine -Path $DbgPath -Line ("Harden: " + [string]$Harden)
	Write-DebugLogLine -Path $DbgPath -Line ("ExcludePrefixes: " + (($ExcludePrefixes -join ", ") ))
	Write-DebugLogLine -Path $DbgPath -Line ("FileCount: " + (IntCount $nodeRelPaths))

	foreach ($f in $files) {
		$abs = $f.FullName
		$rel = Normalize-RepoRelPath -AbsPath $abs -RootAbs $RootAbs
		$lines = Read-AllLinesSafe -AbsPath $abs

		$isSimCore = Is-SimCoreFile -RepoRelPath $rel
		$simcoreViolationLines = @()

		for ($i = 0; $i -lt (IntCount $lines); $i++) {
			$line = $lines[$i]
			$ln = $i + 1

			if ($rxSignalDecl.IsMatch($line)) { Add-Hit -HitsByFile $hitsByFile -FileRel $rel -HitKey "godot_signal_decl" }
			if ($rxConnectDot.IsMatch($line)) { Add-Hit -HitsByFile $hitsByFile -FileRel $rel -HitKey "godot_signal_connect" }
			if ($rxConnectCS.IsMatch($line)) { Add-Hit -HitsByFile $hitsByFile -FileRel $rel -HitKey "godot_signal_connect" }
			if ($rxEmitSignal.IsMatch($line)) { Add-Hit -HitsByFile $hitsByFile -FileRel $rel -HitKey "godot_signal_emit" }

			if ($rxPackedScene.IsMatch($line)) { Add-Hit -HitsByFile $hitsByFile -FileRel $rel -HitKey "packedscene_mention" }

			if ($rxEventDecl.IsMatch($line)) { Add-Hit -HitsByFile $hitsByFile -FileRel $rel -HitKey "csharp_event_decl" }
			if (($rel.ToLowerInvariant().EndsWith(".cs")) -and $rxAddAssign.IsMatch($line)) { Add-Hit -HitsByFile $hitsByFile -FileRel $rel -HitKey "csharp_add_assign" }
			if (($rel.ToLowerInvariant().EndsWith(".cs")) -and $rxSubAssign.IsMatch($line)) { Add-Hit -HitsByFile $hitsByFile -FileRel $rel -HitKey "csharp_sub_assign" }

			if ($rxPublish.IsMatch($line)) { Add-Hit -HitsByFile $hitsByFile -FileRel $rel -HitKey "messaging_publish" }
			if ($rxSubscribe.IsMatch($line)) { Add-Hit -HitsByFile $hitsByFile -FileRel $rel -HitKey "messaging_subscribe" }

			if ($isSimCore -and $rxGodotNamespace.IsMatch($line)) {
				$simcoreViolationLines += ,$ln
			}

			$ml = $rxLoad.Matches($line)
			if ($ml.Count -gt 0) {
				foreach ($m in $ml) {
					$path = $m.Groups[2].Value
					Add-Hit -HitsByFile $hitsByFile -FileRel $rel -HitKey "resource_load"
					$target = Resolve-RefToRepoPath -RawRef $path -FromRepoRelPath $rel -KnownNodesSet $knownSet
					if ($null -ne $target) {
						$etype = "resource_load"
						if ($path.ToLowerInvariant().EndsWith(".tscn")) { $etype = "scene_ref" }
						Add-EdgeEvidence -EdgeMap $edgeMap -FromRel $rel -ToRel $target -EdgeType $etype -SourceRel $rel -LineNumber1Based $ln -Snippet (Truncate-Snippet -Text $line -MaxLen 140)
					}
				}
			}

			$mr = $rxResLoader.Matches($line)
			if ($mr.Count -gt 0) {
				foreach ($m in $mr) {
					$path = $m.Groups[1].Value
					Add-Hit -HitsByFile $hitsByFile -FileRel $rel -HitKey "resource_load"
					$target = Resolve-RefToRepoPath -RawRef $path -FromRepoRelPath $rel -KnownNodesSet $knownSet
					if ($null -ne $target) {
						$etype = "resource_load"
						if ($path.ToLowerInvariant().EndsWith(".tscn")) { $etype = "scene_ref" }
						Add-EdgeEvidence -EdgeMap $edgeMap -FromRel $rel -ToRel $target -EdgeType $etype -SourceRel $rel -LineNumber1Based $ln -Snippet (Truncate-Snippet -Text $line -MaxLen 140)
					}
				}
			}

			$mt = $rxTscnString.Matches($line)
			if ($mt.Count -gt 0) {
				foreach ($m in $mt) {
					$path = $m.Groups[1].Value
					Add-Hit -HitsByFile $hitsByFile -FileRel $rel -HitKey "scene_ref"
					$target = Resolve-RefToRepoPath -RawRef $path -FromRepoRelPath $rel -KnownNodesSet $knownSet
					if ($null -ne $target) {
						Add-EdgeEvidence -EdgeMap $edgeMap -FromRel $rel -ToRel $target -EdgeType "scene_ref" -SourceRel $rel -LineNumber1Based $ln -Snippet (Truncate-Snippet -Text $line -MaxLen 140)
					}
				}
			}
		}

		if ($isSimCore -and (IntCount $simcoreViolationLines) -gt 0) {
			$violations += ,([pscustomobject]@{
				id = ("simcore_godot_namespace::" + $rel)
				severity = "error"
				rule = "simcore_must_not_reference_godot_namespace"
				message = "SimCore file references Godot. namespace"
				file = $rel
				lines = AsIntArray (@($simcoreViolationLines | Sort-Object -Unique))
				evidence_snippet = "Godot."
			})
		}
	}

	$nodes = @()
	foreach ($p in $nodeRelPaths) {
		$nodes += ,([pscustomobject]@{
			path = $p
			ext = ([System.IO.Path]::GetExtension($p)).ToLowerInvariant()
		})
	}

	$edges = @()
	$edgeValues = @($edgeMap.Values)

	$edgeValuesSorted = $edgeValues | Sort-Object `
		@{ Expression = { $_.from }; Ascending = $true }, `
		@{ Expression = { $_.to }; Ascending = $true }, `
		@{ Expression = { $_.type }; Ascending = $true }

	foreach ($e in $edgeValuesSorted) {
		$evSorted = @($e.evidence) | Sort-Object @{ Expression = { $_.file }; Ascending = $true }
		$edges += ,([pscustomobject]@{
			from = $e.from
			to = $e.to
			type = $e.type
			evidence = $evSorted
		})
	}

	$edges = Normalize-EdgesForJson -Edges $edges

	$fileSummaries = @()
	foreach ($p in $nodeRelPaths) {
		$hitObj = @{}
		if ($hitsByFile.ContainsKey($p)) {
			$hitObj = $hitsByFile[$p]
		}

		$hitKeys = @($hitObj.Keys | ForEach-Object { $_.ToString() }) | Sort-Object
		$hitsStable = New-Object System.Collections.Specialized.OrderedDictionary
		foreach ($k in $hitKeys) { $hitsStable.Add($k, [int]$hitObj[$k]) }

		$fileSummaries += ,([pscustomobject]@{
			path = $p
			hits = $hitsStable
		})
	}

	$totalHits = @{}
	foreach ($p in $hitsByFile.Keys) {
		$h = $hitsByFile[$p]
		foreach ($k in $h.Keys) {
			if (-not $totalHits.ContainsKey($k)) { $totalHits[$k] = 0 }
			$totalHits[$k] = [int]$totalHits[$k] + [int]$h[$k]
		}
	}
	$totalKeys = @($totalHits.Keys | ForEach-Object { $_.ToString() }) | Sort-Object
	$totalStable = New-Object System.Collections.Specialized.OrderedDictionary
	foreach ($k in $totalKeys) { $totalStable.Add($k, [int]$totalHits[$k]) }

	$manifest = @{
		tool = @{ name = "Scan-Connectivity"; version = "v0" }
		scope = @{
			file_level_only = $true
			best_effort = $true
			excluded_dirs = @($ExcludePrefixes)
		}
		counts = @{
			nodes = [int](IntCount $nodes)
			edges = [int](IntCount $edges)
			files_with_hits = [int](IntCount $hitsByFile.Keys)
		}
		total_hits = $totalStable
		files = $fileSummaries
	}

	$graph = @{
		tool = @{ name = "Scan-Connectivity"; version = "v0" }
		nodes = $nodes
		edges = $edges
	}

	$violationsOut = @{
		tool = @{ name = "Scan-Connectivity"; version = "v0" }
		rules = @(
			@{
				id = "simcore_must_not_reference_godot_namespace"
				severity = "error"
				description = "Any SimCore file referencing 'Godot.' is a hard invariant violation."
			}
		)
		violations = @($violations | Sort-Object @{ Expression = { $_.file }; Ascending = $true })
		counts = @{
			errors = [int](IntCount $violations)
			warnings = 0
		}
	}

	$manifestPath = Join-Path $OutDirAbs "connectivity_manifest.json"
	$graphPath = Join-Path $OutDirAbs "connectivity_graph.json"
	$violPath = Join-Path $OutDirAbs "connectivity_violations.json"

	Write-StableJsonFile -AbsPath $manifestPath -Data $manifest -Overwrite:$Force
	Write-StableJsonFile -AbsPath $graphPath -Data $graph -Overwrite:$Force
	Write-StableJsonFile -AbsPath $violPath -Data $violationsOut -Overwrite:$Force

	Write-DebugLogLine -Path $DbgPath -Line ("Wrote: " + (Normalize-RepoRelPath -AbsPath $manifestPath -RootAbs $RootAbs))
	Write-DebugLogLine -Path $DbgPath -Line ("Wrote: " + (Normalize-RepoRelPath -AbsPath $graphPath -RootAbs $RootAbs))
	Write-DebugLogLine -Path $DbgPath -Line ("Wrote: " + (Normalize-RepoRelPath -AbsPath $violPath -RootAbs $RootAbs))
}

try {
	$rootAbs = Get-RepoRoot -Override $RepoRoot
	$outDirAbs = Join-Path $rootAbs "docs\generated"
	Ensure-Dir -AbsDir $outDirAbs

	$excludePrefixes = Get-ActiveExcludePrefixes -UseHarden:$Harden -Extra $ExtraExcludeDirs

	if (-not [string]::IsNullOrWhiteSpace($DebugDumpPath)) {
		if ((Test-Path -LiteralPath $DebugDumpPath) -and (-not $Force)) {
			throw "DebugDumpPath exists and -Force not specified: $DebugDumpPath"
		}
		$ddir = Split-Path -Parent $DebugDumpPath
		if (-not [string]::IsNullOrWhiteSpace($ddir)) { Ensure-Dir -AbsDir $ddir }
		[System.IO.File]::WriteAllText($DebugDumpPath, "", (New-Utf8NoBom))
	}

	Scan-Repo -RootAbs $rootAbs -OutDirAbs $outDirAbs -DbgPath $DebugDumpPath -ExcludePrefixes $excludePrefixes

	Write-Verbose "OK: Connectivity scan complete."
	exit 0
} catch {
	Write-Error $_.Exception.Message
	exit 1
}
