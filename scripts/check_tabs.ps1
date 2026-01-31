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
	$rawOut = & git diff --cached --name-only -z --diff-filter=ACMR
	if ($LASTEXITCODE -ne 0) { throw "git diff --cached failed." }

	$text = ""
	if ($null -ne $rawOut) {
		if ($rawOut -is [array]) { $text = ($rawOut -join "") } else { $text = [string]$rawOut }
	}
	if ([string]::IsNullOrEmpty($text)) { return @() }

	$parts = $text -split "`0"
	$out = @()
	foreach ($p in $parts) {
		if ([string]::IsNullOrWhiteSpace($p)) { continue }
		$out += $p
	}
	return @($out)
}


function Get-StagedRelPathsByExt {
	param(
		[Parameter(Mandatory=$true)][string[]]$Paths,
		[Parameter(Mandatory=$true)][string]$ExtLower
	)

	$out = @()
	foreach ($p in $Paths) {
		if (-not $p) { continue }
		$lp = $p.ToLowerInvariant()
		if (-not $lp.EndsWith($ExtLower)) { continue }
		if ($lp.StartsWith("addons/")) { continue }
		if ($lp.StartsWith("_scratch/") -or $lp.StartsWith("._scratch/")) { continue }
		$out += $p
	}
	return @($out)
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

function New-TempDir {
	param([Parameter(Mandatory=$true)][string]$RepoRoot)
	$base = Join-Path $RepoRoot "_scratch"
	if (-not (Test-Path -LiteralPath $base)) { [void](New-Item -ItemType Directory -Force -Path $base) }
	$dir = Join-Path $base ("hook_" + [guid]::NewGuid().ToString("N"))
	[void](New-Item -ItemType Directory -Force -Path $dir)
	return $dir
}

function Remove-TempDir {
	param([Parameter(Mandatory=$true)][string]$Path)
	if (Test-Path -LiteralPath $Path) {
		Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction SilentlyContinue
	}
}

function Get-StagedBlobText {
	param([Parameter(Mandatory=$true)][string]$RepoRelPath)
	$spec = ":" + $RepoRelPath
	$out = & git show $spec 2>$null
	if ($LASTEXITCODE -ne 0) { throw "Failed to read staged content for: $RepoRelPath" }
	return ($out | Out-String)
}

function Invoke-PowerShellParseGate {
	param(
		[Parameter(Mandatory=$true)][string[]]$StagedPs1Paths,
		[Parameter(Mandatory=$true)][string]$TempDir
	)

	if ($StagedPs1Paths.Count -eq 0) { return $false }

	$fail = $false
	foreach ($rel in $StagedPs1Paths) {
		$tmp = Join-Path $TempDir (([System.IO.Path]::GetRandomFileName()) + ".ps1")
		$content = Get-StagedBlobText -RepoRelPath $rel
		Write-Utf8NoBom -Path $tmp -Content $content -EnsureTrailingNewline

		$tokens = $null
		$errors = $null
		[void][System.Management.Automation.Language.Parser]::ParseFile($tmp, [ref]$tokens, [ref]$errors)

		if ($errors -and $errors.Count -gt 0) {
			$fail = $true
			Write-Host ("FATAL: PowerShell parse errors in staged file: {0}" -f $rel) -ForegroundColor Red
			foreach ($e in $errors) {
				Write-Host ("  {0} (Line {1}, Col {2})" -f $e.Message, $e.Extent.StartLineNumber, $e.Extent.StartColumnNumber) -ForegroundColor Red
			}
		}
	}

	return $fail
}

function Resolve-GodotExe {
	# Prefer explicit env var
	if ($env:GODOT_EXE -and (Test-Path -LiteralPath $env:GODOT_EXE)) { return $env:GODOT_EXE }

	# Try PATH
	$cmd = Get-Command godot -ErrorAction SilentlyContinue
	if ($cmd -and $cmd.Source) { return $cmd.Source }

	# Try common Windows install locations, if needed add more here
	$candidates = @(
		"$env:ProgramFiles\Godot\Godot.exe",
		"$env:ProgramFiles\Godot Engine\Godot.exe",
		"$env:LOCALAPPDATA\Programs\Godot\Godot.exe"
	)
	foreach ($c in $candidates) {
		if ($c -and (Test-Path -LiteralPath $c)) { return $c }
	}

	return $null
}

function Invoke-GodotParseGate {
	param(
		[Parameter(Mandatory=$true)][string[]]$StagedGdPaths,
		[Parameter(Mandatory=$true)][string]$TempDir,
		[Parameter(Mandatory=$true)][string]$RepoRoot
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
		$out = & $godotExe --headless --path $RepoRoot --script $agentPath -- $tmp $resPath 2>&1
		$rc = $LASTEXITCODE
		$text = ($out | Out-String)

		if ($out) { $out | ForEach-Object { Write-Host $_ } }

		$bad = $false
		if ($rc -ne 0) { $bad = $true }
		elseif ($text -match '(?mi)\bSCRIPT\s+FATAL\b') { $bad = $true }
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

$overallFail = $false
$tmpDir = $null

try {
	$tmpDir = New-TempDir -RepoRoot $repoRoot

	$staged = @()
	if ($StagedOnly) {
		$staged = @(Get-StagedPaths)
	}

	$stagedGd = @(Get-StagedRelPathsByExt -Paths $staged -ExtLower ".gd")
	$stagedPs1 = @(Get-StagedRelPathsByExt -Paths $staged -ExtLower ".ps1")

	# Gate 1: tabs/BOM/zero-width/trailing whitespace checks for staged .gd
	$tabsResult = Invoke-CheckTabs -StagedOnly:$true
	if ($tabsResult.Lines) {
		foreach ($ln in $tabsResult.Lines) { Write-Host $ln }
	}
	if ($tabsResult.ExitCode -ne 0) { $overallFail = $true }

	# Gate 2: PowerShell parse errors on staged .ps1
	if ($stagedPs1.Count -gt 0) {
		Write-Host ("CHECK: PowerShell parse gate on {0} staged .ps1 file(s)" -f $stagedPs1.Count)
		if (Invoke-PowerShellParseGate -StagedPs1Paths $stagedPs1 -TempDir $tmpDir) { $overallFail = $true }
	}

	# Gate 3: Godot headless parse gate on staged .gd
	if ($stagedGd.Count -gt 0) {
		Write-Host ("CHECK: Godot parse gate on {0} staged .gd file(s)" -f $stagedGd.Count)
		if (Invoke-GodotParseGate -StagedGdPaths $stagedGd -TempDir $tmpDir -RepoRoot $repoRoot) { $overallFail = $true }
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
