Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$common = Join-Path $PSScriptRoot "common.ps1"
if (-not (Test-Path -LiteralPath $common)) { throw "Missing tools common: $common" }
. $common

function Write-Utf8NoBomFile {
	param(
		[Parameter(Mandatory=$true)][string]$Path,
		[Parameter(Mandatory=$true)][string]$Content
	)
	$enc = New-Object System.Text.UTF8Encoding($false)
	[System.IO.File]::WriteAllText($Path, $Content, $enc)
}

function New-TempDir {
	$base = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "ste_validate_gd")
	[void](New-Item -ItemType Directory -Force -Path $base)
	$dir = Join-Path $base ([System.Guid]::NewGuid().ToString("N"))
	[void](New-Item -ItemType Directory -Force -Path $dir)
	return $dir
}

function Remove-TempDir {
	param([Parameter(Mandatory=$true)][string]$Path)
	try { Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction SilentlyContinue } catch { }
}

function Get-RepoRoot {
	$repo = (& git rev-parse --show-toplevel 2>$null)
	if (-not $repo) { throw "Not in a git repository." }
	return $repo.Trim()
}

function Get-RepoRelativePath {
	param(
		[Parameter(Mandatory=$true)][string]$RepoRoot,
		[Parameter(Mandatory=$true)][string]$FullPath
	)

	$repoNorm = $RepoRoot.TrimEnd('\','/')
	$fullNorm = $FullPath

	$prefix1 = $repoNorm + [System.IO.Path]::DirectorySeparatorChar
	if ($fullNorm.StartsWith($prefix1, [System.StringComparison]::OrdinalIgnoreCase)) {
		return (($fullNorm.Substring($prefix1.Length)) -replace '\\','/')
	}

	$prefix2 = $repoNorm + '/'
	if ($fullNorm.StartsWith($prefix2, [System.StringComparison]::OrdinalIgnoreCase)) {
		return (($fullNorm.Substring($prefix2.Length)) -replace '\\','/')
	}

	return $null
}

function Get-IndentViolations {
	param([Parameter(Mandatory=$true)][string]$Text)

	$violations = New-Object System.Collections.Generic.List[object]
	$lines = $Text -split "`n", -1

	for ($i = 0; $i -lt $lines.Length; $i++) {
		$line = $lines[$i]
		$lineNo = $i + 1

		if ($line.EndsWith("`r")) { $line = $line.Substring(0, $line.Length - 1) }

		# Any leading spaces are forbidden (even on whitespace-only lines).
		if ($line -match '^( +)') {
			$violations.Add([pscustomobject]@{ Line = $lineNo; Kind = "leading_spaces"; Text = $line })
			continue
		}

		# Mixed indentation is forbidden (tabs then spaces, or spaces then tabs).
		if ($line -match '^\t+ +') {
			$violations.Add([pscustomobject]@{ Line = $lineNo; Kind = "tabs_then_spaces"; Text = $line })
			continue
		}
		if ($line -match '^ +\t+') {
			$violations.Add([pscustomobject]@{ Line = $lineNo; Kind = "spaces_then_tabs"; Text = $line })
			continue
		}
	}

	return $violations
}

function Validate-GodotScript {
	param(
		[Parameter(Mandatory=$true)][string]$TargetScript,
		[switch]$NormalizeIndentation
	)

	if (-not (Test-Path -LiteralPath $TargetScript)) { throw "TargetScript not found: $TargetScript" }

	$repoRoot = Get-RepoRoot
	$targetFull = (Resolve-Path -LiteralPath $TargetScript).Path
	$targetRel = Get-RepoRelativePath -RepoRoot $repoRoot -FullPath $targetFull

	$text = [System.IO.File]::ReadAllText($targetFull)

	if ($NormalizeIndentation) {
		$normalized = [regex]::Replace($text, '(?m)^(?: {4})+', {
			param($m)
			"`t" * ($m.Value.Length / 4)
		})

		if ($normalized -ne $text) {
			Write-Utf8NoBomFile -Path $targetFull -Content $normalized
			$text = $normalized
			Write-Host "OK: Converted leading 4-space blocks to tabs (wrote UTF-8 no BOM)."
		} else {
			Write-Host "OK: No leading 4-space blocks found. No rewrite needed."
		}
	}

	$violations = Get-IndentViolations -Text $text
	if ($violations.Count -gt 0) {
		Write-Host "FATAL: Indentation policy violated (tabs-only leading indentation)." -ForegroundColor Red
		$max = [Math]::Min(30, $violations.Count)
		for ($i = 0; $i -lt $max; $i++) {
			$v = $violations[$i]
			Write-Host ("  Line {0} [{1}]: {2}" -f $v.Line, $v.Kind, $v.Text) -ForegroundColor Red
		}
		if ($violations.Count -gt $max) {
			Write-Host ("  ...and {0} more violation(s)." -f ($violations.Count - $max)) -ForegroundColor Red
		}
		throw "FAILURE: Fix indentation and re-run Validate-GodotScript."
	}

	$godotExe = Get-GodotExe -RepoRoot $repoRoot
	if (-not $godotExe) { throw "FAILURE: Godot executable not found. Configure godot_path.cfg or set GODOT_EXE." }

	$tmpDir = New-TempDir
	$agentPath = $null
	$tempCopy = $null
	$scratchDir = Join-Path $repoRoot "_scratch"
	$scratchExisted = (Test-Path -LiteralPath $scratchDir)

	try {
		if (-not $scratchExisted) {
			[void](New-Item -ItemType Directory -Force -Path $scratchDir)
		}

		$agentName = "temp_validator_{0}.gd" -f ([System.Guid]::NewGuid().ToString("N"))
		$agentPath = Join-Path $scratchDir $agentName

		$tab = [char]9
		$agentCode = @(
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
			("{0}OS.set_exit_code(0)" -f $tab),
			("{0}return true" -f $tab)
		) -join "`n"

		Write-Utf8NoBomFile -Path $agentPath -Content ($agentCode + "`n")

		$tempCopy = Join-Path $tmpDir "target.gd"
		Write-Utf8NoBomFile -Path $tempCopy -Content $text

		$resPath = if ($targetRel) { "res://$targetRel" } else { "res://" + [System.IO.Path]::GetFileName($targetFull) }

		$out = & $godotExe --headless --path $repoRoot --script $agentPath -- $tempCopy $resPath 2>&1
		$rc = $LASTEXITCODE
		if ($out) { $out | ForEach-Object { Write-Host $_ } }

		if ($rc -ne 0) {
			throw "FAILURE: Godot parse gate failed for $TargetScript (exit code $rc)."
		}

		$textOut = ($out | Out-String)
		$hasFatal =
			($textOut -match '(?mi)\bSCRIPT\s+FATAL\b') -or
			($textOut -match '(?mi)\bSCRIPT\s+ERROR\b') -or
			($textOut -match '(?mi)\bParse Error\b') -or
			($textOut -match '(?mi)\bError while parsing\b') -or
			($textOut -match '(?mi)\bERROR:\b')

		if ($hasFatal) {
			throw "FAILURE: Godot parse gate failed for $TargetScript."
		}

		Write-Host "OK: Godot parse gate passed."
	}
	finally {
		if ($agentPath -and (Test-Path -LiteralPath $agentPath)) {
			Remove-Item -LiteralPath $agentPath -Force -ErrorAction SilentlyContinue
		}
		if ($tempCopy -and (Test-Path -LiteralPath $tempCopy)) {
			Remove-Item -LiteralPath $tempCopy -Force -ErrorAction SilentlyContinue
		}
		Remove-TempDir -Path $tmpDir

		# If we created _scratch, remove it if empty to avoid dirtying the repo.
		if (-not $scratchExisted -and (Test-Path -LiteralPath $scratchDir)) {
			try {
				$items = Get-ChildItem -LiteralPath $scratchDir -Force -ErrorAction SilentlyContinue
				if (-not $items -or $items.Count -eq 0) {
					Remove-Item -LiteralPath $scratchDir -Force -ErrorAction SilentlyContinue
				}
			} catch { }
		}
	}

	return
}
