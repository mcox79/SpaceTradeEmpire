param(
	[switch]$StagedOnly = $true
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepoRoot {
	$root = (& git rev-parse --show-toplevel 2>$null)
	if (-not $root) { throw "Not in a git repository." }
	return $root.Trim()
}

function Get-StagedPaths {
	$out = & git diff --cached --name-only --diff-filter=ACMR 2>$null
	if ($LASTEXITCODE -ne 0) { throw "git diff --cached failed." }

	$paths = @()
	foreach ($line in ($out -split "`n")) {
		$p = $line.Trim()
		if ($p) { $paths += ($p -replace '\\','/') }
	}
	return $paths
}

function New-TempDir {
	$base = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "ste_precommit")
	[void](New-Item -ItemType Directory -Force -Path $base)
	$dir = Join-Path $base ([System.Guid]::NewGuid().ToString("N"))
	[void](New-Item -ItemType Directory -Force -Path $dir)
	return $dir
}

function Remove-TempDir {
	param([Parameter(Mandatory=$true)][string]$Path)
	try { Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction SilentlyContinue } catch { }
}

function Write-Utf8NoBom {
	param(
		[Parameter(Mandatory=$true)][string]$Path,
		[Parameter(Mandatory=$true)][string]$Content,
		[switch]$EnsureTrailingNewline
	)
	$enc = New-Object System.Text.UTF8Encoding($false)
	$text = $Content
	if ($EnsureTrailingNewline -and -not $text.EndsWith("`n")) { $text += "`n" }
	[System.IO.File]::WriteAllText($Path, $text, $enc)
}

function Get-StagedBlobText {
	param([Parameter(Mandatory=$true)][string]$RepoRelPath)
	$spec = ":" + $RepoRelPath
	$out = & git show $spec 2>$null
	if ($LASTEXITCODE -ne 0) { throw "Failed to read staged content for: $RepoRelPath" }
	return ($out | Out-String)
}

function Resolve-GodotExe {
	foreach ($cmdName in @("godot", "godot4")) {
		$cmd = Get-Command $cmdName -ErrorAction SilentlyContinue
		if ($cmd -and $cmd.Source -and (Test-Path -LiteralPath $cmd.Source)) { return $cmd.Source }
	}

	foreach ($envName in @("GODOT_EXE","GODOT4_EXE","GODOT","GODOT4")) {
		$v = [Environment]::GetEnvironmentVariable($envName)
		if ($v -and (Test-Path -LiteralPath $v)) { return $v }
	}

	$roots = @()
	$pf = [Environment]::GetFolderPath("ProgramFiles")
	$pfx = [Environment]::GetFolderPath("ProgramFilesX86")
	if ($pf)  { $roots += $pf }
	if ($pfx) { $roots += $pfx }

	foreach ($r in $roots) {
		foreach ($sub in @("Godot","Godot Engine")) {
			$d = Join-Path $r $sub
			if (-not (Test-Path -LiteralPath $d)) { continue }
			$hit = Get-ChildItem -LiteralPath $d -Filter "Godot*.exe" -File -ErrorAction SilentlyContinue |
				Sort-Object LastWriteTime -Descending |
				Select-Object -First 1
			if ($hit -and (Test-Path -LiteralPath $hit.FullName)) { return $hit.FullName }
		}
	}

	return $null
}

function Invoke-PowerShellParseGate {
	param(
		[Parameter(Mandatory=$true)][string[]]$StagedPs1Paths,
		[Parameter(Mandatory=$true)][string]$TempDir
	)

	$fail = $false

	foreach ($rel in $StagedPs1Paths) {
		$tmp = Join-Path $TempDir (([System.IO.Path]::GetRandomFileName()) + ".ps1")
		$content = Get-StagedBlobText -RepoRelPath $rel
		Write-Utf8NoBom -Path $tmp -Content $content

		$tokens = $null
		$errors = $null
		[void][System.Management.Automation.Language.Parser]::ParseFile($tmp, [ref]$tokens, [ref]$errors)

		if ($errors -and $errors.Count -gt 0) {
			$fail = $true
			Write-Host ("FATAL: PowerShell parse errors in staged file: {0}" -f $rel) -ForegroundColor Red
			foreach ($e in $errors) {
				$at = ""
				try {
					$ex = $e.Extent
					if ($ex) { $at = ("{0}:{1}:{2}" -f $ex.File, $ex.StartLineNumber, $ex.StartColumnNumber) }
				} catch { }
				if ($at) {
					Write-Host ("  {0}  {1}" -f $at, $e.Message) -ForegroundColor Red
				} else {
					Write-Host ("  {0}" -f $e.Message) -ForegroundColor Red
				}
			}
		}
	}

	return $fail
}

function Invoke-GodotParseGate {
	param(
		[Parameter(Mandatory=$true)][string[]]$StagedGdPaths,
		[Parameter(Mandatory=$true)][string]$TempDir
	)

	if ($StagedGdPaths.Count -eq 0) { return $false }

	$godotExe = Resolve-GodotExe
	if (-not $godotExe) {
		Write-Host "FATAL: Godot executable not found. Add 'godot' to PATH or set GODOT_EXE." -ForegroundColor Red
		return $true
	}

	$agentPath = Join-Path $TempDir "ste_gd_parse_agent.gd"
	$tab = [char]9
	$agent = @(
		"extends MainLoop",
		"func _process(_delta):",
		("{0}var args = OS.get_cmdline_user_args()" -f $tab),
		("{0}if args.size() < 2:" -f $tab),
		("{0}{0}printerr(""SCRIPT FATAL: missing args. expected: <temp_file> <res_path>"")" -f $tab),
		("{0}{0}OS.set_exit_code(2)" -f $tab),
		("{0}{0}return true" -f $tab),
		("{0}var temp_file = args[0]" -f $tab),
		("{0}var res_path = args[1]" -f $tab),
		("{0}var src = FileAccess.get_file_as_string(temp_file)" -f $tab),
		("{0}var s = GDScript.new()" -f $tab),
		("{0}s.source_code = src" -f $tab),
		("{0}s.resource_path = res_path" -f $tab),
		("{0}var err = s.reload()" -f $tab),
		("{0}if err != OK:" -f $tab),
		("{0}{0}printerr(""SCRIPT FATAL: reload() failed for %s with code: %s"" % [res_path, err])" -f $tab),
		("{0}{0}OS.set_exit_code(1)" -f $tab),
		("{0}{0}return true" -f $tab),
		("{0}print(""SCRIPT OK: parsed %s"" % res_path)" -f $tab),
		("{0}return true" -f $tab)
	) -join "`n"
	Write-Utf8NoBom -Path $agentPath -Content $agent -EnsureTrailingNewline

	$fail = $false

	foreach ($rel in $StagedGdPaths) {
		$tmp = Join-Path $TempDir (([System.IO.Path]::GetRandomFileName()) + ".gd")
		$content = Get-StagedBlobText -RepoRelPath $rel
		Write-Utf8NoBom -Path $tmp -Content $content -EnsureTrailingNewline

		$resPath = "res://" + $rel
		$out = & $godotExe --headless --script $agentPath -- $tmp $resPath 2>&1
		$text = ($out | Out-String)

		if ($out) { $out | ForEach-Object { Write-Host $_ } }

		$bad = $false
		if ($text -match '(?mi)\bSCRIPT\s+FATAL\b') { $bad = $true }
		elseif ($text -match '(?mi)\bParse Error\b') { $bad = $true }
		elseif ($text -match '(?mi)\bError while parsing\b') { $bad = $true }
		elseif ($text -match '(?mi)\bERROR:\b') { $bad = $true }

		if ($bad) {
			$fail = $true
			Write-Host ("FATAL: Godot parse gate failed for staged file: {0}" -f $rel) -ForegroundColor Red
		}
	}

	return $fail
}

$repoRoot = Get-RepoRoot
Set-Location $repoRoot
[Environment]::CurrentDirectory = $repoRoot

. (Join-Path $PSScriptRoot "tools\check_tabs_lib.ps1")
$result = Invoke-CheckTabs -StagedOnly:$StagedOnly

$overallFail = $false
$rcTabs = [int]$result["ExitCode"]
$lines = $result["Lines"]

foreach ($line in $lines) {
	if ($rcTabs -ne 0) { Write-Host $line -ForegroundColor Red } else { Write-Host $line }
}
if ($rcTabs -ne 0) { $overallFail = $true }

$staged = Get-StagedPaths
$stagedPs1 = @($staged | Where-Object { $_.ToLowerInvariant().EndsWith(".ps1") })
$stagedGd = @(
	$staged |
	Where-Object { $_.ToLowerInvariant().EndsWith(".gd") } |
	Where-Object { $_ -notmatch '^(addons|_scratch|\._scratch)/' }
)

$tmpDir = New-TempDir
try {
	if ($stagedPs1.Count -gt 0) {
		Write-Host ("CHECK: PowerShell parse gate on {0} staged .ps1 file(s)" -f $stagedPs1.Count)
		if (Invoke-PowerShellParseGate -StagedPs1Paths $stagedPs1 -TempDir $tmpDir) { $overallFail = $true }
	}

	if ($stagedGd.Count -gt 0) {
		Write-Host ("CHECK: Godot parse gate on {0} staged .gd file(s)" -f $stagedGd.Count)
		if (Invoke-GodotParseGate -StagedGdPaths $stagedGd -TempDir $tmpDir) { $overallFail = $true }
	}
}
finally {
	Remove-TempDir -Path $tmpDir
}

if ($overallFail) {
	$global:LASTEXITCODE = 1
	exit 1
}

$global:LASTEXITCODE = 0
exit 0
