<#
.SYNOPSIS
	Generate a deterministic Context Packet markdown file from docs/templates/01_CONTEXT_PACKET.template.md.

.DESCRIPTION
	Copies the template to an output path and fills in:
	- Date
	- Branch
	- Commit
	- Objective (non-journal, 1-3 lines; defaults to a forcing placeholder)
	- Modes (OUTPUT_MODE, GIT_MODE, PROFILE)
	- Allowed files (deterministic union of staged + working tree + untracked OR explicit override)
	- Recent commits (subjects only)
	- Validation commands
	- Definition of Done

	Rationale:
	The Context Packet is a session contract and scope limiter, not a session journal.
	This generator ensures critical contract fields are never left as template placeholders,
	while keeping output low-noise, deterministic, and resilient to template edits.

	Does not require a clean repo. Does not stage, commit, or modify git config.

.PARAMETER OutputPath
	Output markdown file path. If relative, it is treated as repo-relative.
	Default: docs/generated/01_CONTEXT_PACKET.md

.PARAMETER AllowedFiles
	Optional explicit allowlist override. If provided, git scanning is skipped and this list is used.

.PARAMETER MaxFiles
	Maximum number of files to include in the Allowed files list (after sorting and de-duplication).
	Default: 6

.PARAMETER Objective
	1-3 lines describing the session objective. If omitted, a forcing placeholder is emitted.

.PARAMETER OutputMode
	Explicit OUTPUT_MODE value to write into the packet.
	Default: POWERSHELL

.PARAMETER GitMode
	Explicit GIT_MODE value to write into the packet.
	Default: NO_STAGE

.PARAMETER Profile
	Explicit PROFILE value to write into the packet.
	Default: EXPERIMENTATION

.PARAMETER RecentCommitsCount
	How many recent commits (subjects only) to include. Default: 5

.PARAMETER ValidationCommands
	Optional explicit list of validation command bullets. If omitted, minimal defaults are used.

.PARAMETER DefinitionOfDone
	Optional explicit list of DoD bullets. If omitted, minimal defaults are used.

.PARAMETER Force
	Overwrite OutputPath if it already exists.

.PARAMETER DebugDumpPath
	Optional path to write a verbose diagnostic log (UTF-8 no BOM). If relative, repo-relative.

.EXAMPLE
	powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/New-ContextPacket.ps1 -Verbose -Force

.EXAMPLE
	powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/New-ContextPacket.ps1 -Verbose -Force `
		-Objective "Implement vNext context packet fields" `
		-OutputMode POWERSHELL -GitMode NO_STAGE -Profile EXPERIMENTATION
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
	[Parameter(Mandatory = $false)]
	[string]$OutputPath = "docs/generated/01_CONTEXT_PACKET.md",

	[Parameter(Mandatory = $false)]
	[string[]]$AllowedFiles,

	[Parameter(Mandatory = $false)]
	[ValidateRange(1, 1000)]
	[int]$MaxFiles = 6,

	[Parameter(Mandatory = $false)]
	[string[]]$Objective,

	[Parameter(Mandatory = $false)]
	[ValidateSet("POWERSHELL", "FULL_FILES", "ANALYSIS_ONLY")]
	[string]$OutputMode = "POWERSHELL",

	[Parameter(Mandatory = $false)]
	[string]$GitMode = "NO_STAGE",

	[Parameter(Mandatory = $false)]
	[ValidateSet("EXPERIMENTATION", "NORMAL")]
	[string]$Profile = "EXPERIMENTATION",

	[Parameter(Mandatory = $false)]
	[ValidateRange(0, 50)]
	[int]$RecentCommitsCount = 5,

	[Parameter(Mandatory = $false)]
	[string[]]$ValidationCommands,

	[Parameter(Mandatory = $false)]
	[string[]]$DefinitionOfDone,

	[Parameter(Mandatory = $false)]
	[switch]$Force,

	[Parameter(Mandatory = $false)]
	[string]$DebugDumpPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$script:LastGitCommand = ""
$script:RepoRoot = ""
$script:DebugLines = New-Object System.Collections.Generic.List[string]

function Debug-Log {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Line
	)
	$ts = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fff K")
	$script:DebugLines.Add(($ts + " | " + $Line))
}

function Flush-DebugLog {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Path
	)
	$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
	[System.IO.File]::WriteAllText($Path, ($script:DebugLines -join "`r`n") + "`r`n", $utf8NoBom)
}

function Assert-GitAvailable {
	$cmd = Get-Command git -ErrorAction SilentlyContinue
	if (-not $cmd) {
		throw "git was not found in PATH for this PowerShell instance. Confirm 'git --version' works in the same shell."
	}
}

function Invoke-GitText {
	param(
		[Parameter(Mandatory = $true)]
		[string[]]$Args,

		[Parameter(Mandatory = $true)]
		[string]$WorkingDirectory
	)

	$script:LastGitCommand = "git " + ($Args -join " ")
	Write-Verbose $script:LastGitCommand
	Debug-Log ("RUN  " + $script:LastGitCommand + " | cwd=" + $WorkingDirectory)

	$oldPager = $env:GIT_PAGER
	$oldPrompt = $env:GIT_TERMINAL_PROMPT
	$oldOptionalLocks = $env:GIT_OPTIONAL_LOCKS
	$env:GIT_PAGER = "cat"
	$env:GIT_TERMINAL_PROMPT = "0"
	$env:GIT_OPTIONAL_LOCKS = "0"

	try {
		Push-Location -LiteralPath $WorkingDirectory
		try {
			$cmdArgs = @()
			foreach ($a in $Args) {
				if ($a -match '[\s"]') {
					$cmdArgs += '"' + ($a -replace '"', '\"') + '"'
				} else {
					$cmdArgs += $a
				}
			}

			$cmdLine = "git " + ($cmdArgs -join " ") + " 2>&1"
			$outLines = & cmd.exe /d /c $cmdLine
			$code = $LASTEXITCODE

			$text = ""
			if ($outLines -ne $null) {
				if ($outLines -is [string]) {
					$text = $outLines
				} else {
					$text = ($outLines | ForEach-Object { $_.ToString() }) -join "`r`n"
				}
			}

			if ($code -ne 0) {
				throw ("git command failed: " + $script:LastGitCommand + "`r`n" + ($text.TrimEnd()))
			}

			return ($text + "")
		} finally {
			Pop-Location
		}
	} finally {
		$env:GIT_PAGER = $oldPager
		$env:GIT_TERMINAL_PROMPT = $oldPrompt
		$env:GIT_OPTIONAL_LOCKS = $oldOptionalLocks
	}
}

function Get-RepoRoot {
	$root = Invoke-GitText -Args @("rev-parse", "--show-toplevel") -WorkingDirectory (Get-Location).Path
	$lines = $root -split "(`r`n|`n|`r)"
	foreach ($l in $lines) {
		$s = ($l + "").Trim()
		if ($s.Length -gt 0) { return $s }
	}
	throw "Unable to resolve repo root."
}

function To-RepoRelativeForwardSlash {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Path,
		[Parameter(Mandatory = $true)]
		[string]$RepoRoot
	)
	$p = $Path
	if ([System.IO.Path]::IsPathRooted($p)) {
		$p = [System.IO.Path]::GetFullPath($p)
		$root = [System.IO.Path]::GetFullPath($RepoRoot)
		if ($p.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
			$p = $p.Substring($root.Length)
			$p = $p.TrimStart('\', '/')
		}
	}
	$p = $p -replace '\\', '/'
	return $p
}

function Sort-Unique-Ordinal {
	param(
		[Parameter(Mandatory = $false)]
		[string[]]$Items
	)
	if ($null -eq $Items) { return @() }

	$seen = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::Ordinal)
	$out = New-Object System.Collections.Generic.List[string]
	foreach ($i in $Items) {
		if ($null -eq $i) { continue }
		$s = $i.Trim()
		if ($s.Length -eq 0) { continue }
		if ($seen.Add($s)) { $out.Add($s) }
	}

	$arr = $out.ToArray()
	[Array]::Sort($arr, [System.StringComparer]::Ordinal)
	return $arr
}

function Split-NameOnlyOutput {
	param(
		[Parameter(Mandatory = $false)]
		[AllowNull()]
		[AllowEmptyString()]
		[string]$Text
	)

	if ($null -eq $Text -or $Text.Length -eq 0) { return @() }

	$lines = @()
	$raw = $Text -split "(`r`n|`n|`r)"
	foreach ($l in $raw) {
		$line = ($l + "").Trim()
		if ($line.Length -eq 0) { continue }

		if ($line -match '^(warning:)\s') { continue }
		if ($line -match '^(will be replaced by)\s') { continue }
		if ($line -match 'Git touches it$') { continue }

		$lines += @($line)
	}

	return ,$lines
}

function Replace-Or-Append-KeyLine {
	param(
		[Parameter(Mandatory = $true)][string]$Content,
		[Parameter(Mandatory = $true)][string]$Key,
		[Parameter(Mandatory = $true)][string]$Value
	)

	$pattern = "(?im)^\s*" + [Regex]::Escape($Key) + "\s*:\s*.*$"
	if ([Regex]::IsMatch($Content, $pattern)) {
		return [Regex]::Replace($Content, $pattern, ($Key + ": " + $Value), 1)
	}

	# Special-case: if template lacks Branch:, insert it directly after Date:
	if ($Key -eq "Branch") {
		$datePattern = "(?im)^\s*Date\s*:\s*.*$"
		if ([Regex]::IsMatch($Content, $datePattern)) {
			return [Regex]::Replace(
				$Content,
				$datePattern,
				{ param($m) $m.Value + "`r`n" + ("Branch: " + $Value) },
				1
			)
		}
	}

	# Fallback: append at end
	return ($Content.TrimEnd() + "`r`n" + ($Key + ": " + $Value) + "`r`n")
}


function Replace-Mode-Line {
	param(
		[Parameter(Mandatory = $true)][string]$Content,
		[Parameter(Mandatory = $true)][string]$Key,
		[Parameter(Mandatory = $true)][string]$Value
	)

	$pattern = "(?im)^\s*" + [Regex]::Escape($Key) + "\s*=\s*.*$"
	if ([Regex]::IsMatch($Content, $pattern)) {
		return [Regex]::Replace(
			$Content,
			$pattern,
			($Key + " = " + $Value),
			1
		)
	}
	return $Content

}

function Replace-Objective-Block {
	param(
		[Parameter(Mandatory = $true)][string]$Content,
		[Parameter(Mandatory = $false)][string[]]$ObjectiveLines
	)

	# Rationale: Objective must exist and must not be the template placeholder.
	if ($null -eq $ObjectiveLines -or @($ObjectiveLines).Count -eq 0) {
		$ObjectiveLines = @("TBD: Fill a 1-3 line objective before proceeding.")
	}
	$objText = (@($ObjectiveLines) -join "`r`n")

	$normalized = ($Content -replace "`r?`n", "`r`n")

	# Preferred replacement: between "Commit:" line and the "## Modes" heading.
	$pattern = "(?s)(^.*?^Commit:\s*.*?\r\n\r\n)(.*?)(\r\n\r\n\#\#\s+Modes\b)"
	if ([Regex]::IsMatch($normalized, $pattern)) {
		return [Regex]::Replace($normalized, $pattern, ('$1' + $objText + '$3'), 1)
	}

	# Fallback: if template contains "(1 to 3 lines)", replace that token.
	$fallback = "\(\s*1\s*to\s*3\s*lines\s*\)"
	if ([Regex]::IsMatch($normalized, $fallback)) {
		return [Regex]::Replace($normalized, $fallback, $objText, 1)
	}

	return $Content
}

function Replace-SectionByHeading {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Content,

		[Parameter(Mandatory = $true)]
		[string]$HeadingPattern,

		[Parameter(Mandatory = $true)]
		[string]$HeadingLine,

		[Parameter(Mandatory = $true)]
		[string]$SectionText
	)

	# Normalize content newlines to CRLF deterministically.
	$normalized = ($Content -replace "`r?`n", "`r`n")

	# Normalize section newlines to CRLF, trim outer whitespace only.
	$sec = ($SectionText + "") -replace "`r?`n", "`r`n"
	$sec = $sec.Trim()

	if (-not [Regex]::IsMatch($normalized, $HeadingPattern)) {
		# Append deterministically if missing.
		return ($normalized.TrimEnd() + "`r`n`r`n" + $HeadingLine + "`r`n`r`n" + $sec + "`r`n")
	}

	$match = [Regex]::Match($normalized, $HeadingPattern)
	$startIdx = $match.Index + $match.Length
	$after = $normalized.Substring($startIdx)

	$nextHeading = [Regex]::Match($after, "(?im)^\s*##\s+.+$")
	$endIdx = $normalized.Length
	if ($nextHeading.Success) { $endIdx = $startIdx + $nextHeading.Index }

	$before = $normalized.Substring(0, $startIdx).TrimEnd()
	$afterTail = $normalized.Substring($endIdx).TrimStart()

	$out = $before + "`r`n`r`n" + $sec + "`r`n"
	if ($afterTail.Length -gt 0) { $out += "`r`n" + $afterTail.TrimEnd() + "`r`n" }
	return $out
}

function Format-AllowedFilesSection {
	param(
		[Parameter(Mandatory = $false)][string[]]$UnionList = @(),
		[Parameter(Mandatory = $false)][string[]]$StagedList = @(),
		[Parameter(Mandatory = $false)][string[]]$WorkingList = @(),
		[Parameter(Mandatory = $true)][int]$MaxCount,
		[Parameter(Mandatory = $true)][bool]$WasExplicitOverride
	)

	if ($null -eq $UnionList) { $UnionList = @() }
	if ($null -eq $StagedList) { $StagedList = @() }
	if ($null -eq $WorkingList) { $WorkingList = @() }

	$emit = $UnionList
	$truncated = $false
	if ($emit.Count -gt $MaxCount) {
		$emit = $emit[0..($MaxCount - 1)]
		$truncated = $true
	}

	$lines = New-Object System.Collections.Generic.List[string]
	$lines.Add("Generated by scripts/tools/New-ContextPacket.ps1")
	$lines.Add("")
	$lines.Add("Summary:")
	$lines.Add("- Staged: " + $StagedList.Count)
	$lines.Add("- Working tree (unstaged + untracked): " + $WorkingList.Count)
	$lines.Add("- Union: " + $UnionList.Count)
	$lines.Add("")
	$lines.Add("Allowed files:")

	if ($emit.Count -eq 0) {
		if ($WasExplicitOverride) { $lines.Add("- (none)") }
		else { $lines.Add("- (none yet) Stage files or pass -AllowedFiles") }
	} else {
		foreach ($p in $emit) { $lines.Add("- " + $p) }
	}

	if ($truncated) {
		$lines.Add("")
		$lines.Add("_Truncated to MaxFiles = " + $MaxCount + "._")
	}

	return ($lines.ToArray() -join "`r`n")
}

function Format-RecentCommitsLines {
	param(
		[Parameter(Mandatory = $false)]
		[string[]]$CommitLines
	)

	$CommitLines = @($CommitLines)
	$lines = New-Object System.Collections.Generic.List[string]
	if ($CommitLines.Count -eq 0) {
		$lines.Add("- (unavailable)")
		return ($lines.ToArray() -join "`r`n")
	}

	foreach ($c in $CommitLines) {
		$s = [Regex]::Replace(($c + ""), "\s+", " ").Trim()
		if ($s.Length -eq 0) { continue }
		$lines.Add("- " + $s)
	}

	if ($lines.Count -eq 0) { $lines.Add("- (unavailable)") }
	return ($lines.ToArray() -join "`r`n")
}

function Format-BulletLines {
	param(
		[Parameter(Mandatory = $true)]
		[string[]]$Items
	)

	$Items = @($Items)
	$lines = New-Object System.Collections.Generic.List[string]
	foreach ($i in $Items) {
		$s = ($i + "").Trim()
		if ($s.Length -eq 0) { continue }
		$lines.Add("- " + $s)
	}
	if ($lines.Count -eq 0) { $lines.Add("- (none)") }
	return ($lines.ToArray() -join "`r`n")
}


try {
	Assert-GitAvailable

	Write-Verbose "Resolving repo root..."
	$script:RepoRoot = Get-RepoRoot
	Write-Verbose ("Repo root: " + $script:RepoRoot)
	Debug-Log ("RepoRoot=" + $script:RepoRoot)

	$debugResolved = $null
	if ($DebugDumpPath -and $DebugDumpPath.Trim().Length -gt 0) {
		$debugResolved = $DebugDumpPath
		if (-not [System.IO.Path]::IsPathRooted($debugResolved)) {
			$debugResolved = Join-Path $script:RepoRoot $debugResolved
		}
		$debugResolved = [System.IO.Path]::GetFullPath($debugResolved)
		Debug-Log ("DebugDumpPath=" + $debugResolved)
	}

	$templatePath = Join-Path $script:RepoRoot "docs/templates/01_CONTEXT_PACKET.template.md"
	if (-not (Test-Path -LiteralPath $templatePath -PathType Leaf)) {
		throw ("Template not found: " + $templatePath)
	}

	$outPathResolved = $OutputPath
	if (-not [System.IO.Path]::IsPathRooted($outPathResolved)) {
		$outPathResolved = Join-Path $script:RepoRoot $outPathResolved
	}
	$outPathResolved = [System.IO.Path]::GetFullPath($outPathResolved)
	Debug-Log ("OutputPath=" + $outPathResolved)

	$outDir = [System.IO.Path]::GetDirectoryName($outPathResolved)
	if (-not (Test-Path -LiteralPath $outDir -PathType Container)) {
		if ($PSCmdlet.ShouldProcess($outDir, "Create directory")) {
			Write-Verbose ("Creating directory: " + $outDir)
			[void](New-Item -ItemType Directory -Path $outDir -Force)
		}
	}

	if ((Test-Path -LiteralPath $outPathResolved -PathType Leaf) -and (-not $Force)) {
		throw ("Output exists. Use -Force to overwrite: " + $outPathResolved)
	}

	Write-Verbose "Reading git metadata..."
	$branch = (Invoke-GitText -Args @("rev-parse", "--abbrev-ref", "HEAD") -WorkingDirectory $script:RepoRoot).Trim()
	$commit = (Invoke-GitText -Args @("rev-parse", "HEAD") -WorkingDirectory $script:RepoRoot).Trim()
	$dateStr = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss K")
	Debug-Log ("Branch=" + $branch)
	Debug-Log ("Commit=" + $commit)
	Debug-Log ("Date=" + $dateStr)

	Write-Verbose "Computing allowed files..."
	$staged = @()
	$working = @()
	$union = @()
	$explicitOverride = $false

	if ($AllowedFiles -and $AllowedFiles.Count -gt 0) {
		$explicitOverride = $true
		$norm = @()
		foreach ($p in $AllowedFiles) {
			$norm += @((To-RepoRelativeForwardSlash -Path $p -RepoRoot $script:RepoRoot))
		}
		$union = Sort-Unique-Ordinal -Items $norm
		Debug-Log ("AllowedFilesOverrideCount=" + @($union).Count)
	} else {
		$stagedText = Invoke-GitText -Args @("diff", "--cached", "--name-only") -WorkingDirectory $script:RepoRoot
		$unstagedText = Invoke-GitText -Args @("diff", "--name-only") -WorkingDirectory $script:RepoRoot
		$untrackedText = Invoke-GitText -Args @("ls-files", "--others", "--exclude-standard") -WorkingDirectory $script:RepoRoot

		$stagedRaw = Split-NameOnlyOutput -Text $stagedText
		$unstagedRaw = Split-NameOnlyOutput -Text $unstagedText
		$untrackedRaw = Split-NameOnlyOutput -Text $untrackedText

		$stagedNorm = @()
		foreach ($p in @($stagedRaw)) { $stagedNorm += @((To-RepoRelativeForwardSlash -Path $p -RepoRoot $script:RepoRoot)) }
		$staged = Sort-Unique-Ordinal -Items $stagedNorm

		$workingNorm = @()
		foreach ($p in @($unstagedRaw)) { $workingNorm += @((To-RepoRelativeForwardSlash -Path $p -RepoRoot $script:RepoRoot)) }
		foreach ($p in @($untrackedRaw)) { $workingNorm += @((To-RepoRelativeForwardSlash -Path $p -RepoRoot $script:RepoRoot)) }
		$working = Sort-Unique-Ordinal -Items $workingNorm

		$union = Sort-Unique-Ordinal -Items ($staged + $working)

		Debug-Log ("StagedCount=" + @($staged).Count)
		Debug-Log ("WorkingCount=" + @($working).Count)
		Debug-Log ("UnionCount=" + @($union).Count)
	}

	# Recent commits (subjects only).
	# Robust parsing: do NOT rely on newlines. Use ASCII record/field separators.
	$recent = @()
	if ($RecentCommitsCount -gt 0) {
		# RS = 0x1E, US = 0x1F
		$fmt = "--pretty=format:%h%x1F%s%x1E"
		$recentText = Invoke-GitText -Args @("log", ("-n" + $RecentCommitsCount), $fmt) -WorkingDirectory $script:RepoRoot

		# Split records on RS, then split fields on US.
		$records = $recentText -split ([char]0x1E)
		foreach ($r in $records) {
			$rec = ($r + "")
			if ($rec.Trim().Length -eq 0) { continue }

			$fields = $rec -split ([char]0x1F), 2
			if ($fields.Count -lt 2) { continue }

			$h = ($fields[0] + "").Trim()
			$s = ($fields[1] + "")

			# Sanitize subject: collapse all whitespace (including newlines) to single spaces.
			$s = [Regex]::Replace($s, "\s+", " ").Trim()

			if ($h.Length -eq 0 -or $s.Length -eq 0) { continue }
			$recent += @($h + " " + $s)
		}
	}

	# Build section text blocks (single string each).
	$allowedSectionText = Format-AllowedFilesSection -UnionList $union -StagedList $staged -WorkingList $working -MaxCount $MaxFiles -WasExplicitOverride:$explicitOverride
	$recentSectionText  = Format-RecentCommitsLines -CommitLines $recent


	# Defaults that keep the packet actionable without pretending to know your whole plan.
	if ($null -eq $ValidationCommands -or @($ValidationCommands).Count -eq 0) {
		$ValidationCommands = @(
			"powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/New-ContextPacket.ps1 -Verbose -Force",
			"git status"
		)
	}
	if ($null -eq $DefinitionOfDone -or @($DefinitionOfDone).Count -eq 0) {
		$DefinitionOfDone = @(
			"Context Packet regenerated and reflects current branch/commit and allowlist.",
			"Validation commands executed with no errors.",
			"Any code changes are committed with a subject + body describing what changed, why, and how it was validated."
		)
	}

	$validationText = Format-BulletLines -Items $ValidationCommands
	$dodText        = Format-BulletLines -Items $DefinitionOfDone


	Write-Verbose "Filling template..."
	$templateContent = [System.IO.File]::ReadAllText($templatePath)
	$templateContent = ($templateContent -replace "`r?`n", "`r`n")

	$filled = $templateContent
	$filled = Replace-Or-Append-KeyLine -Content $filled -Key "Date" -Value $dateStr
	$filled = Replace-Or-Append-KeyLine -Content $filled -Key "Branch" -Value $branch
	$filled = Replace-Or-Append-KeyLine -Content $filled -Key "Commit" -Value $commit

	$filled = Replace-Objective-Block -Content $filled -ObjectiveLines $Objective
	$filled = Replace-Mode-Line -Content $filled -Key "OUTPUT_MODE" -Value $OutputMode
	$filled = Replace-Mode-Line -Content $filled -Key "GIT_MODE" -Value $GitMode
	$filled = Replace-Mode-Line -Content $filled -Key "PROFILE" -Value $Profile
	
	# Replace Allowed files section (use Replace-SectionByHeading so line normalization applies)
	$filled = Replace-SectionByHeading `
		-Content $filled `
		-HeadingPattern "(?im)^\s*##\s+Allowed files\s*$" `
		-HeadingLine "## Allowed files" `
		-SectionText $allowedSectionText

	$filled = Replace-SectionByHeading `
		-Content $filled `
		-HeadingPattern "(?im)^\s*##\s+Validation commands to run after the step\s*$" `
		-HeadingLine "## Validation commands to run after the step" `
		-SectionText $validationText

	$filled = Replace-SectionByHeading `
		-Content $filled `
		-HeadingPattern "(?im)^\s*##\s+Recent commits\s*\(subjects only\)\b.*$" `
		-HeadingLine "## Recent commits (subjects only)" `
		-SectionText $recentSectionText

	$filled = Replace-SectionByHeading `
		-Content $filled `
		-HeadingPattern "(?im)^\s*##\s+Definition of Done\b.*$" `
		-HeadingLine "## Definition of Done" `
		-SectionText $dodText
	
	# Guardrails: prevent packed-section regressions
	$mustHave = @(
	    "## Allowed files",
	    "## Validation commands to run after the step",
	    "## Recent commits (subjects only)",
	    "## Definition of Done"
	)
	
	foreach ($h in $mustHave) {
	    if ($filled -notmatch [Regex]::Escape($h)) {
	        throw ("Missing heading in output: " + $h)
	    }
	}
	
	# Canary: Summary must be on its own line (prior failure mode packed it)
	if ($filled -notmatch "(?m)^Summary:\s*$") {
	    throw "Allowed files section formatting regressed: 'Summary:' is not on its own line."
	}
	
	# Catch packed bullet lines like "- a - b" that should be multiple lines
	if ($filled -match "(?m)^\s*-\s+.+\s-\s+.+$") {
	    throw "Packed bullets detected (pattern: '- a - b') in generated output."
	}

	Write-Verbose "Writing output..."
	$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
	if ($PSCmdlet.ShouldProcess($outPathResolved, "Write context packet")) {
		[System.IO.File]::WriteAllText($outPathResolved, $filled, $utf8NoBom)
	}

	if ($debugResolved) {
		Flush-DebugLog -Path $debugResolved
		Write-Verbose ("Wrote debug log: " + $debugResolved)
	}

	Write-Output $outPathResolved
	exit 0
} catch {
	[Console]::Error.WriteLine("ERROR: New-ContextPacket.ps1 failed.")
	if ($script:LastGitCommand -and $script:LastGitCommand.Trim().Length -gt 0) {
		[Console]::Error.WriteLine("LastGitCommand: " + $script:LastGitCommand)
	}
	[Console]::Error.WriteLine(($_ | Out-String).TrimEnd())

	try {
		if ($DebugDumpPath -and $DebugDumpPath.Trim().Length -gt 0) {
			$path = $DebugDumpPath
			if ($script:RepoRoot -and -not [System.IO.Path]::IsPathRooted($path)) {
				$path = Join-Path $script:RepoRoot $path
			}
			$path = [System.IO.Path]::GetFullPath($path)
			Flush-DebugLog -Path $path
			[Console]::Error.WriteLine("Debug log: " + $path)
		}
	} catch { }

	exit 1
}
