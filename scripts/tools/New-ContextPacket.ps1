<#
.SYNOPSIS
	Generate a deterministic Context Packet markdown file from docs/templates/01_CONTEXT_PACKET.template.md.

.DESCRIPTION
	Copies the template to an output path and fills in:
	- Date
	- Branch
	- Commit

	Fills "Allowed files" with a deterministic list of changed files, supporting:
	- Working tree changes (unstaged) plus untracked files
	- Index changes (staged), if any

	Does not require a clean repo. Does not stage, commit, or modify git config.

.PARAMETER OutputPath
	Output markdown file path. If relative, it is treated as repo-relative.
	Default: docs/generated/01_CONTEXT_PACKET.md

.PARAMETER AllowedFiles
	Optional explicit allowlist override. If provided, git scanning is skipped and this list is used.

.PARAMETER MaxFiles
	Maximum number of files to include in the Allowed files list (after sorting and de-duplication).
	Default: 6

.PARAMETER Force
	Overwrite OutputPath if it already exists.

.PARAMETER DebugDumpPath
	Optional path to write a verbose diagnostic log (UTF-8 no BOM). If relative, repo-relative.

.EXAMPLE
	powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/New-ContextPacket.ps1 -Verbose

.EXAMPLE
	powershell -NoProfile -ExecutionPolicy Bypass -File scripts/tools/New-ContextPacket.ps1 -Verbose -DebugDumpPath _scratch/contextpacket_debug.log -Force
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

			$split = @()
			if ($text -ne $null -and $text.Length -gt 0) {
				$split = $text -split "(`r`n|`n|`r)"
			}
			Debug-Log ("EXIT " + $code + " | lines=" + $split.Count)
			if ($split.Count -gt 0) {
				$max = [Math]::Min(5, $split.Count)
				for ($i = 0; $i -lt $max; $i++) {
					$line = ($split[$i] + "")
					$line = $line.Replace("`t", "\t")
					Debug-Log ("OUT  [" + $i + "] " + $line)
				}
			}

			if ($code -ne 0) {
				$detail = ($text + "").Trim()
				if ($detail.Length -gt 0) {
					throw ($script:LastGitCommand + " failed with exit code " + $code + ". " + $detail)
				}
				throw ($script:LastGitCommand + " failed with exit code " + $code + ".")
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
	$cwd = (Get-Location).Path
	$root = (Invoke-GitText -Args @("rev-parse", "--show-toplevel") -WorkingDirectory $cwd).Trim()
	if (-not $root) { throw "Unable to determine repo root via git rev-parse --show-toplevel." }
	return $root
}

function To-RepoRelativeForwardSlash {
	param(
		[Parameter(Mandatory = $true)][string]$Path,
		[Parameter(Mandatory = $true)][string]$RepoRoot
	)

	$repoRootFull = [System.IO.Path]::GetFullPath($RepoRoot)

	$full = $null
	try { $full = [System.IO.Path]::GetFullPath($Path) } catch { $full = $Path }

	$rel = $full
	if ($full -and $repoRootFull -and ($full.ToLowerInvariant().StartsWith($repoRootFull.ToLowerInvariant()))) {
		$rel = $full.Substring($repoRootFull.Length).TrimStart('\','/')
	}

	return ($rel -replace '\\', '/')
}

function Sort-Unique-Ordinal {
	param(
		[Parameter(Mandatory = $false)]
		[AllowNull()]
		[AllowEmptyCollection()]
		[object]$Items
	)

	if ($null -eq $Items) { return @() }

	$arrIn = @()
	if ($Items -is [string]) {
		$arrIn = @([string]$Items)
	} elseif ($Items -is [System.Collections.IEnumerable]) {
		foreach ($x in $Items) {
			if ($null -eq $x) { continue }
			$arrIn += @($x.ToString())
		}
	} else {
		$arrIn = @($Items.ToString())
	}

	if ($arrIn.Count -eq 0) { return @() }

	$set = New-Object "System.Collections.Generic.HashSet[string]" -ArgumentList ([System.StringComparer]::Ordinal)
	foreach ($i in $arrIn) {
		if ($null -eq $i) { continue }
		$s = $i.Trim()
		if ($s.Length -eq 0) { continue }
		[void]$set.Add($s)
	}

	if ($set.Count -eq 0) { return @() }

	$out = New-Object string[] $set.Count
	$set.CopyTo($out)
	[System.Array]::Sort($out, [System.StringComparer]::Ordinal)
	return ,$out
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

	return ($Content.TrimEnd() + "`r`n" + ($Key + ": " + $Value) + "`r`n")
}

function Replace-AllowedFiles-Section {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Content,

		[Parameter(Mandatory = $false)]
		[AllowNull()]
		[AllowEmptyCollection()]
		[string[]]$SectionLines
	)

	if ($null -eq $SectionLines) { $SectionLines = @() }
	# Force array semantics in case caller passed a scalar through coercion.
	$SectionLines = @($SectionLines)

	$sectionText = ($SectionLines -join "`r`n")
	$headingPattern = "(?im)^\s*##\s+Allowed files\b.*$"

	if (-not [Regex]::IsMatch($Content, $headingPattern)) {
		$append = New-Object System.Collections.Generic.List[string]
		$append.Add($Content.TrimEnd())
		$append.Add("")
		$append.Add("## Allowed files")
		$append.Add($sectionText.TrimEnd())
		$append.Add("")
		return ($append -join "`r`n")
	}

	$normalized = ($Content -replace "`r?`n", "`r`n")
	$match = [Regex]::Match($normalized, $headingPattern)
	$startIdx = $match.Index + $match.Length

	$after = $normalized.Substring($startIdx)
	$nextHeading = [Regex]::Match($after, "(?im)^\s*##\s+.+$")
	$endIdx = $normalized.Length
	if ($nextHeading.Success) { $endIdx = $startIdx + $nextHeading.Index }

	$before = $normalized.Substring(0, $startIdx)
	$afterTail = $normalized.Substring($endIdx)

	$newMid = "`r`n" + $sectionText.TrimEnd() + "`r`n"
	return ($before.TrimEnd() + $newMid + $afterTail.TrimStart())
}

function Format-AllowedFilesSection {
	param(
		[Parameter(Mandatory = $false)][string[]]$UnionList = @(),
		[Parameter(Mandatory = $false)][string[]]$StagedList = @(),
		[Parameter(Mandatory = $false)][string[]]$WorkingList = @(),
		[Parameter(Mandatory = $true)][int]$MaxCount
	)

	if ($null -eq $UnionList) { $UnionList = @() }
	if ($null -eq $StagedList) { $StagedList = @() }
	if ($null -eq $WorkingList) { $WorkingList = @() }

	$lines = New-Object System.Collections.Generic.List[string]

	$lines.Add("Generated by scripts/tools/New-ContextPacket.ps1")
	$lines.Add("")
	$lines.Add("Summary:")
	$lines.Add("- Staged: " + $StagedList.Count)
	$lines.Add("- Working tree (unstaged + untracked): " + $WorkingList.Count)
	$lines.Add("- Union: " + $UnionList.Count)
	$lines.Add("")

	$emit = $UnionList
	$truncated = $false
	if ($emit.Count -gt $MaxCount) {
		$emit = $emit[0..($MaxCount - 1)]
		$truncated = $true
	}

	$lines.Add("Allowed files:")
	if ($emit.Count -eq 0) {
		$lines.Add("- (none)")
	} else {
		foreach ($p in $emit) { $lines.Add("- " + $p) }
	}

	if ($truncated) {
		$lines.Add("")
		$lines.Add("_Truncated to MaxFiles = " + $MaxCount + "._")
	}

	$lines.Add("")
	$lines.Add("Details:")
	$lines.Add("")
	$lines.Add("Staged (index):")
	if ($StagedList.Count -eq 0) {
		$lines.Add("- (none)")
	} else {
		foreach ($p in $StagedList) { $lines.Add("- " + $p) }
	}

	$lines.Add("")
	$lines.Add("Working tree (unstaged + untracked):")
	if ($WorkingList.Count -eq 0) {
		$lines.Add("- (none)")
	} else {
		foreach ($p in $WorkingList) { $lines.Add("- " + $p) }
	}

	$lines.Add("")

	# CRITICAL: force array semantics even if it would otherwise stream
	return ,$lines.ToArray()
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

	if ($AllowedFiles -and $AllowedFiles.Count -gt 0) {
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

		Debug-Log ("ParsedStagedLines=" + @($stagedRaw).Count)
		Debug-Log ("ParsedUnstagedLines=" + @($unstagedRaw).Count)
		Debug-Log ("ParsedUntrackedLines=" + @($untrackedRaw).Count)

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

	# Force array semantics at the call site.
	$sectionLines = @(Format-AllowedFilesSection -UnionList $union -StagedList $staged -WorkingList $working -MaxCount $MaxFiles)

	Debug-Log ("SectionLinesType=" + (($sectionLines | Get-Member -Name GetType -MemberType Method -ErrorAction SilentlyContinue) | Out-String).Trim())
	Debug-Log ("SectionLinesCount=" + @($sectionLines).Count)
	if (@($sectionLines).Count -gt 0) {
		Debug-Log ("SectionLinesFirst=" + ($sectionLines[0] + ""))
	}

	Write-Verbose "Filling template..."
	$templateContent = [System.IO.File]::ReadAllText($templatePath)
	$templateContent = ($templateContent -replace "`r?`n", "`r`n")

	$filled = $templateContent
	$filled = Replace-Or-Append-KeyLine -Content $filled -Key "Date" -Value $dateStr
	$filled = Replace-Or-Append-KeyLine -Content $filled -Key "Branch" -Value $branch
	$filled = Replace-Or-Append-KeyLine -Content $filled -Key "Commit" -Value $commit
	$filled = Replace-AllowedFiles-Section -Content $filled -SectionLines $sectionLines

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
