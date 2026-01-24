Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Fail([string]$msg) {
	[System.Windows.Forms.MessageBox]::Show(
		$msg,
		"Patch Apply Failed",
		[System.Windows.Forms.MessageBoxButtons]::OK,
		[System.Windows.Forms.MessageBoxIcon]::Error
	) | Out-Null
}

function Info([string]$msg) {
	[System.Windows.Forms.MessageBox]::Show(
		$msg,
		"Patch Apply",
		[System.Windows.Forms.MessageBoxButtons]::OK,
		[System.Windows.Forms.MessageBoxIcon]::Information
	) | Out-Null
}

function Get-RepoRoot {
	$root = (& git rev-parse --show-toplevel 2>$null)
	if (-not $root) { throw "Not in a git repository." }
	return $root.Trim()
}

function Ensure-Scratch([string]$repoRoot) {
	$scratch = Join-Path $repoRoot "_scratch"
	if (-not (Test-Path $scratch)) {
		New-Item -ItemType Directory -Path $scratch | Out-Null
	}
	return $scratch
}

function Write-Utf8NoBom([string]$path, [string]$text) {
	[System.IO.File]::WriteAllText($path, $text, (New-Object System.Text.UTF8Encoding($false)))
}

function Strip-MarkdownFences([string]$s) {
	# Remove fence lines like ``` or ```diff
	$s = $s -replace "(?m)^\s*```(?:diff)?\s*$", ""
	return $s
}

function Has-Bom([byte[]]$bytes) {
	if ($bytes.Length -ge 2) {
		if (($bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) -or ($bytes[0] -eq 0xFE -and $bytes[1] -eq 0xFF)) { return $true }
	}
	if ($bytes.Length -ge 3) {
		if ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { return $true }
	}
	return $false
}

function First-N-BytesHex([byte[]]$bytes, [int]$n) {
	$take = [Math]::Min($n, $bytes.Length)
	if ($take -le 0) { return "" }
	return (($bytes[0..($take-1)] | ForEach-Object { $_.ToString("X2") }) -join " ")
}

function Assert-CleanTree {
	# Always produce arrays (even 0 or 1 results)
	$w = @( & git diff --name-only 2>$null | ForEach-Object { "$_".Trim() } | Where-Object { $_ } )
	$s = @( & git diff --cached --name-only 2>$null | ForEach-Object { "$_".Trim() } | Where-Object { $_ } )

	if (($w.Count -gt 0) -or ($s.Count -gt 0)) {
		$msg = "Edit Script mode requires a clean working tree and index.`r`nCommit or stash first."
		if ($w.Count -gt 0) { $msg += "`r`n`r`nWorking tree changes:`r`n" + ($w -join "`r`n") }
		if ($s.Count -gt 0) { $msg += "`r`n`r`nStaged changes:`r`n" + ($s -join "`r`n") }
		throw $msg
	}
}

function Detect-Newline([string]$text) {
	if ($text -match "`r`n") { return "`r`n" }
	return "`n"
}

function Normalize-ToLF([string]$text) {
	return ($text -replace "`r`n", "`n")
}

function Denormalize-FromLF([string]$text, [string]$newline) {
	if ($newline -eq "`r`n") { return ($text -replace "`n", "`r`n") }
	return $text
}

function Parse-EditScript([string]$scriptText) {
	# Script format (v1):
	# Comments: lines starting with #
	# FILE <path>
	# INSERT_AFTER <exact line>
	# INSERT_BEFORE <exact line>
	# BEGIN
	# <multiline text>
	# END
	#
	# REPLACE_BLOCK
	# BEGIN_OLD
	# <old block exact>
	# END_OLD
	# BEGIN_NEW
	# <new block>
	# END_NEW
	#
	# DELETE_BLOCK
	# BEGIN_OLD
	# <old block exact>
	# END_OLD

	$lines = ($scriptText -split "`r?`n", 0)
	$ops = @()
	$currentFile = $null
	$i = 0

	function Dump-Codepoints([string]$s) {
		if ($null -eq $s) { return "<null>" }
		$chars = $s.ToCharArray()
		$hex = $chars | ForEach-Object { ("U+{0:X4}" -f [int]$_) }
		return ($hex -join " ")
	}

	function Sanitize-Line([string]$s) {
		if ($null -eq $s) { return "" }

		# Remove any Unicode "format" chars (includes BOM, zero-width joiners, etc.)
		$s = [regex]::Replace($s, '\p{Cf}', '')

		# Remove control chars except tab
		$s = [regex]::Replace($s, '[\p{Cc}&&[^\t]]', '')

		# Normalize exotic Unicode spaces to ASCII space
		$s = [regex]::Replace($s, '[\u00A0\u1680\u2000-\u200A\u202F\u205F\u3000]', ' ')

		# Collapse whitespace runs to a single ASCII space, then trim
		$s = [regex]::Replace($s, '\s+', ' ').Trim()

		return $s
	}

	function Unquote-IfQuoted([string]$s) {
		if ($null -eq $s) { return "" }
		$t = $s.Trim()
		if ($t.Length -ge 2 -and $t[0] -eq '"' -and $t[$t.Length-1] -eq '"') {
			# Remove surrounding quotes but preserve inner whitespace exactly
			return $t.Substring(1, $t.Length-2)
		}
		return $s
	}

	function Read-Block([string]$startToken, [string]$endToken, [ref]$idx) {
		if ($idx.Value -ge $lines.Length) {
			throw "Expected '$startToken' but reached end of script."
		}
	
		$startRaw = $lines[$idx.Value]
		$startSan = Sanitize-Line $startRaw
		$startLineNo = $idx.Value + 1
	
		if ($startSan -ne $startToken) {
			throw ("Expected '{0}' on line {1}.`r`nRAW: [{2}]`r`nSAN: [{3}]`r`nRAW codepoints: {4}" -f $startToken, $startLineNo, $startRaw, $startSan, (Dump-Codepoints $startRaw))
		}
	
		$idx.Value++
		$buf = New-Object System.Collections.Generic.List[string]
	
		while ($idx.Value -lt $lines.Length) {
			$raw = $lines[$idx.Value]
			$san = Sanitize-Line $raw
	
			if ($san -eq $endToken) {
				$idx.Value++
				return ($buf -join "`n")
			}
	
			$buf.Add($raw)
			$idx.Value++
		}
	
		throw "Missing '$endToken' for block starting at '$startToken' (line $startLineNo)."
	}



	while ($i -lt $lines.Length) {
		$raw = $lines[$i]
		$lineNo = $i + 1
		$line = Sanitize-Line $raw
		$i++
	
		if (-not $line) { continue }
		if ($line.StartsWith("#")) { continue }
	
		# Tokenize command using sanitized line (robust to BOM/odd spaces),
		# but extract the argument from the RAW line to preserve exact whitespace.
		$mTok = [regex]::Match($line, '^(?<cmd>\S+)(?:\s+(?<arg>.*))?$')
		if (-not $mTok.Success) {
			throw ("Cannot tokenize line {0}.`r`nRAW: [{1}]`r`nSAN: [{2}]`r`nRAW codepoints: {3}" -f $lineNo, $raw, $line, (Dump-Codepoints $raw))
		}

		$cmd = $mTok.Groups["cmd"].Value

		# RAW parse: take everything after the first token (cmd), preserving internal whitespace
		$mRaw = [regex]::Match($raw, '^\s*(?<cmd>\S+)(?<rest>.*)$')
		$arg = ""
		if ($mRaw.Success) {
			# remove exactly the leading whitespace between cmd and arg, keep arg exactly otherwise
			$arg = ($mRaw.Groups["rest"].Value -replace '^\s+', '')
		}

		# Optional: strip Unicode "format" chars from arg only (BOM/zero-width), without altering spacing.
		$arg = [regex]::Replace($arg, '\p{Cf}', '')

		# Normalize exotic Unicode spaces to ASCII space in arg only (do NOT collapse runs).
		$arg = [regex]::Replace($arg, '[\u00A0\u1680\u2000-\u200A\u202F\u205F\u3000]', ' ')
		
		# Remove control chars except tab in arg (guards against invisible garbage)
		$arg = [regex]::Replace($arg, '[\p{Cc}&&[^\t]]', '')


		if ($cmd -eq "FILE") {
			$currentFile = $arg.Trim()
			if (-not $currentFile) { throw "FILE requires a path." }
			continue
		}
		
		if (-not $currentFile) {
			throw "No FILE set before operations. Add: FILE <path>"
		}
		
		if ($cmd -eq "INSERT_AFTER") {
			$anchor = Unquote-IfQuoted $arg
			if (-not $anchor) { throw "INSERT_AFTER requires an exact anchor line." }
			$text = Read-Block "BEGIN" "END" ([ref]$i)
			$ops += [pscustomobject]@{ file=$currentFile; op="insert_after"; anchor=$anchor; text=$text }
			continue
		}
		
		if ($cmd -eq "INSERT_BEFORE") {
			$anchor = Unquote-IfQuoted $arg
			if (-not $anchor) { throw "INSERT_BEFORE requires an exact anchor line." }
			$text = Read-Block "BEGIN" "END" ([ref]$i)
			$ops += [pscustomobject]@{ file=$currentFile; op="insert_before"; anchor=$anchor; text=$text }
			continue
		}
		
		if ($cmd -eq "REPLACE_BLOCK") {
			$old = Read-Block "BEGIN_OLD" "END_OLD" ([ref]$i)
			$new = Read-Block "BEGIN_NEW" "END_NEW" ([ref]$i)
			$ops += [pscustomobject]@{ file=$currentFile; op="replace_block"; old=$old; new=$new }
			continue
		}
		
		if ($cmd -eq "DELETE_BLOCK") {
			$old = Read-Block "BEGIN_OLD" "END_OLD" ([ref]$i)
			$ops += [pscustomobject]@{ file=$currentFile; op="delete_block"; old=$old }
			continue
		}


		throw ("Unrecognized command on line {0}.`r`nRAW: [{1}]`r`nSAN: [{2}]`r`nRAW codepoints: {3}" -f $lineNo, $raw, $line, (Dump-Codepoints $raw))

	}

	return $ops

}

function Apply-Ops-ToContent([string]$original, [object[]]$opsForFile) {
	$nl = Detect-Newline $original
	$cur = Normalize-ToLF $original

	foreach ($op in $opsForFile) {
		if ($op.op -eq "insert_after" -or $op.op -eq "insert_before") {
			$anchor = [string]$op.anchor
			$insert = Normalize-ToLF ([string]$op.text)

			$curLines = $cur -split "`n", 0
			$matches = New-Object System.Collections.Generic.List[int]
			for ($j=0; $j -lt $curLines.Length; $j++) {
				$lineJ = $curLines[$j]

				# Remove Unicode "format" chars (BOM/zero-width) and normalize exotic spaces,
				# but do NOT collapse normal spaces/tabs.
				$lineJ = [regex]::Replace($lineJ, '\p{Cf}', '')
				$lineJ = [regex]::Replace($lineJ, '[\u00A0\u1680\u2000-\u200A\u202F\u205F\u3000]', ' ')
				$lineJ = [regex]::Replace($lineJ, '[\p{Cc}&&[^\t]]', '')

				if ($lineJ -eq $anchor) { $matches.Add($j) }
			}

			if ($matches.Count -ne 1) {
				function Dump-Hex([string]$s) {
					if ($null -eq $s) { return "<null>" }
					$bytes = [System.Text.Encoding]::UTF8.GetBytes($s)
					$take = [Math]::Min(80, $bytes.Length)
					if ($take -le 0) { return "" }
					return (($bytes[0..($take-1)] | ForEach-Object { $_.ToString("X2") }) -join " ")
				}

				# Show best "near matches" (same text after trimming) to diagnose whitespace/invisible chars
				$near = New-Object System.Collections.Generic.List[string]
				for ($j=0; $j -lt $curLines.Length; $j++) {
					$rawJ = $curLines[$j]
					$normJ = [regex]::Replace($rawJ, '\p{Cf}', '')
					$normJ = [regex]::Replace($normJ, '[\u00A0\u1680\u2000-\u200A\u202F\u205F\u3000]', ' ')
					$normJ = [regex]::Replace($normJ, '[\p{Cc}&&[^\t]]', '')
					if ($normJ.Trim() -eq $anchor.Trim()) {
						$near.Add(("{0}: RAW=[{1}] HEX={2}" -f ($j+1), $rawJ, (Dump-Hex $rawJ)))
						if ($near.Count -ge 5) { break }
					}
				}

				$nearText = ""
				if ($near.Count -gt 0) {
					$nearText = "`r`nNear matches (after Trim):`r`n" + ($near -join "`r`n")
				}

				throw ("Anchor must match exactly once in file.`r`nMatches={0}`r`nAnchor=[{1}]`r`nAnchor HEX={2}{3}" -f $matches.Count, $anchor, (Dump-Hex $anchor), $nearText)
			}

			$k = $matches[0]
			$insLines = @()
			if ($insert -ne "") { $insLines = $insert -split "`n", 0 }

			if ($op.op -eq "insert_after") {
				$before = @($curLines[0..$k])
				$after = @()
				if ($k+1 -le $curLines.Length-1) { $after = @($curLines[($k+1)..($curLines.Length-1)]) }
				$curLines = @($before + $insLines + $after)
			} else {
				$before = @()
				if ($k-1 -ge 0) { $before = @($curLines[0..($k-1)]) }
				$after = @($curLines[$k..($curLines.Length-1)])
				$curLines = @($before + $insLines + $after)
			}

			$cur = ($curLines -join "`n")
			continue
		}

		if ($op.op -eq "replace_block") {
			$old = Normalize-ToLF ([string]$op.old)
			$new = Normalize-ToLF ([string]$op.new)

			$first = $cur.IndexOf($old)
			if ($first -lt 0) { throw "replace_block: old block not found." }
			$second = $cur.IndexOf($old, $first + 1)
			if ($second -ge 0) { throw "replace_block: old block matches multiple times. Refuse." }

			$cur = $cur.Replace($old, $new)
			continue
		}

		if ($op.op -eq "delete_block") {
			$old = Normalize-ToLF ([string]$op.old)

			$first = $cur.IndexOf($old)
			if ($first -lt 0) { throw "delete_block: old block not found." }
			$second = $cur.IndexOf($old, $first + 1)
			if ($second -ge 0) { throw "delete_block: old block matches multiple times. Refuse." }

			$cur = $cur.Replace($old, "")
			continue
		}

		throw "Unknown op: $($op.op)"
	}

	return (Denormalize-FromLF $cur $nl)
}

$repoRoot = Get-RepoRoot
Set-Location $repoRoot
$scratchRoot = Ensure-Scratch $repoRoot

$form = New-Object System.Windows.Forms.Form
$form.Text = "Apply Patch (Unified Diff)"
$form.Size = New-Object System.Drawing.Size(980,700)
$form.StartPosition = "CenterScreen"

$topPanel = New-Object System.Windows.Forms.FlowLayoutPanel
$topPanel.Dock = "Top"
$topPanel.Height = 52
$topPanel.FlowDirection = "LeftToRight"
$topPanel.WrapContents = $false
$topPanel.Padding = New-Object System.Windows.Forms.Padding(8,8,8,8)

$rbPatch = New-Object System.Windows.Forms.RadioButton
$rbPatch.Text = "Patch"
$rbPatch.Checked = $true
$rbPatch.AutoSize = $true

$rbScript = New-Object System.Windows.Forms.RadioButton
$rbScript.Text = "Edit Script"
$rbScript.AutoSize = $true

$btnLoad = New-Object System.Windows.Forms.Button
$btnLoad.Text = "Load File..."
$btnLoad.Width = 110
$btnLoad.Height = 28

$btnApply = New-Object System.Windows.Forms.Button
$btnApply.Text = "Apply"
$btnApply.Width = 90
$btnApply.Height = 28

$btnDiag = New-Object System.Windows.Forms.Button
$btnDiag.Text = "Validate"
$btnDiag.Width = 90
$btnDiag.Height = 28

$btnClear = New-Object System.Windows.Forms.Button
$btnClear.Text = "Clear"
$btnClear.Width = 80
$btnClear.Height = 28

$topPanel.Controls.Add($rbPatch)
$topPanel.Controls.Add($rbScript)
$topPanel.Controls.Add($btnLoad)
$topPanel.Controls.Add($btnApply)
$topPanel.Controls.Add($btnDiag)
$topPanel.Controls.Add($btnClear)

$textBox = New-Object System.Windows.Forms.TextBox
$textBox.Multiline = $true
$textBox.ScrollBars = "Both"
$textBox.WordWrap = $false
$textBox.AcceptsTab = $true
$textBox.Font = New-Object System.Drawing.Font("Consolas",10)
$textBox.Dock = "Fill"

$logBox = New-Object System.Windows.Forms.TextBox
$logBox.Multiline = $true
$logBox.ScrollBars = "Vertical"
$logBox.WordWrap = $true
$logBox.ReadOnly = $true
$logBox.Font = New-Object System.Drawing.Font("Consolas",9)
$logBox.Dock = "Bottom"
$logBox.Height = 190

function Log-Line([string]$line) {
	$ts = (Get-Date).ToString("HH:mm:ss")
	$logBox.AppendText(("[$ts] $line`r`n"))
}

$form.Controls.Add($textBox)
$form.Controls.Add($logBox)
$form.Controls.Add($topPanel)

$btnClear.Add_Click({
	$textBox.Text = ""
	$logBox.Text = ""
})

$btnLoad.Add_Click({
	try {
		$dlg = New-Object System.Windows.Forms.OpenFileDialog
		$dlg.Title = "Select a file to load"
		$dlg.Filter = "All files (*.*)|*.*"
		$dlg.InitialDirectory = $repoRoot

		if ($dlg.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) { return }

		$bytes = [System.IO.File]::ReadAllBytes($dlg.FileName)
		$text = [System.IO.File]::ReadAllText($dlg.FileName)

		Log-Line ("Loaded: " + $dlg.FileName)
		Log-Line ("First 10 bytes: " + (First-N-BytesHex $bytes 10))

		if (Has-Bom $bytes) {
			Info "Warning: file appears to include a BOM (UTF-8 BOM or UTF-16). Patch-first pipeline can break if you write patches with BOM."
		}

		$textBox.Text = $text
	}
	catch {
		Fail $_.Exception.Message
	}
})

$btnDiag.Add_Click({
	try {
		if ($rbPatch.Checked) {
			$patchText = Strip-MarkdownFences $textBox.Text

			if (-not $patchText.Trim()) { Fail "No patch text provided."; return }
			if ($patchText -notmatch "(?m)^\s*diff --git\s+") { Fail "Patch must contain a 'diff --git' header line."; return }

			$diffCount = ([regex]::Matches($patchText, "(?m)^\s*diff --git\s+")).Count
			$utf8Bytes = [System.Text.Encoding]::UTF8.GetBytes($patchText)
			$hex10 = First-N-BytesHex $utf8Bytes 10

			Info ("Patch Validate OK:`r`n`r`nDiff headers: $diffCount`r`nText UTF-8 first 10 bytes: $hex10`r`nExpected: 64 69 66 66 20 2D 2D 67 69 74")
			return
		}

		$scriptText = $textBox.Text
		if (-not $scriptText.Trim()) { Fail "No edit script provided."; return }

		$ops = @(Parse-EditScript $scriptText)
		$files = @($ops | Select-Object -ExpandProperty file | Sort-Object -Unique)
		$summary = "Edit Script Validate OK:`r`n`r`nOps: $($ops.Count)`r`nFiles:`r`n" + ($files -join "`r`n")
		Info $summary
	}
	catch {
		Fail $_.Exception.Message
	}
})

$btnApply.Add_Click({
	try {
		if ($rbPatch.Checked) {
			$patchText = Strip-MarkdownFences $textBox.Text

			if (-not $patchText.Trim()) { Fail "No patch text provided."; return }
			if ($patchText -notmatch "(?m)^\s*diff --git\s+") { Fail "Patch must contain a 'diff --git' header line."; return }

			$patchPath = Join-Path $scratchRoot "patch.diff"
			Write-Utf8NoBom $patchPath $patchText
			Log-Line ("Wrote patch as UTF-8 no BOM: " + $patchPath)

			$checkOut = & git apply --check $patchPath 2>&1
			if ($LASTEXITCODE -ne 0) {
				Log-Line "git apply --check failed."
				Fail ("git apply --check failed:`r`n`r`n" + ($checkOut -join "`r`n"))
				return
			}

			$applyOut = & git apply $patchPath 2>&1
			if ($LASTEXITCODE -ne 0) {
				Log-Line "git apply failed."
				Fail ("git apply failed:`r`n`r`n" + ($applyOut -join "`r`n"))
				return
			}

			$filesTouched = @(& git diff --name-only 2>$null) | Where-Object { $_ -and $_.Trim() }
			Log-Line "Patch applied successfully."
			Info ("Patch applied successfully.`r`n`r`nTouched files:`r`n" + ($filesTouched -join "`r`n"))
			return
		}

		Assert-CleanTree

		$scriptText = $textBox.Text
		if (-not $scriptText.Trim()) { Fail "No edit script provided."; return }

		$ops = @(Parse-EditScript $scriptText)
		$files = @($ops | Select-Object -ExpandProperty file | Sort-Object -Unique)

		if ($files.Length -gt 5) {
			Fail "Change budget exceeded: script touches $($files.Count) files (max 5)."
			return
		}

		Log-Line ("Edit Script files: " + ($files -join ", "))

		$originalByFile = @{}
		$newByFile = @{}

		foreach ($f in $files) {
			$full = Join-Path $repoRoot $f
			if (-not (Test-Path $full)) {
				throw "File not found: $f"
			}
			$orig = [System.IO.File]::ReadAllText($full)
			$originalByFile[$f] = $orig
			$opsForFile = @($ops | Where-Object { $_.file -eq $f })
			$new = Apply-Ops-ToContent $orig $opsForFile
			$newByFile[$f] = $new
		}

		# Controlled internal bootstrap: write new content, generate patch via git, revert, then apply patch.
		foreach ($f in $files) {
			$full = Join-Path $repoRoot $f
			Write-Utf8NoBom $full $newByFile[$f]
		}

		$diffText = (& git --no-pager diff -- $files 2>&1 | Out-String)
		if (-not ($diffText -match "(?m)^\s*diff --git\s+")) {
			foreach ($f in $files) { & git checkout -- $f | Out-Null }
			throw "No diff generated. Refuse."
		}

		$scriptPatchPath = Join-Path $scratchRoot "edit_script.patch"
		Write-Utf8NoBom $scriptPatchPath $diffText
		Log-Line ("Generated patch from edit script: " + $scriptPatchPath)

		foreach ($f in $files) { & git checkout -- $f | Out-Null }

		$checkOut = & git apply --check $scriptPatchPath 2>&1
		if ($LASTEXITCODE -ne 0) {
			Fail ("git apply --check failed:`r`n`r`n" + ($checkOut -join "`r`n"))
			return
		}

		$applyOut = & git apply $scriptPatchPath 2>&1
		if ($LASTEXITCODE -ne 0) {
			Fail ("git apply failed:`r`n`r`n" + ($applyOut -join "`r`n"))
			return
		}

		$filesTouched = @(& git diff --name-only 2>$null) | Where-Object { $_ -and $_.Trim() }
		Log-Line "Edit Script applied successfully."
		Info ("Edit Script applied successfully.`r`n`r`nTouched files:`r`n" + ($filesTouched -join "`r`n"))
	}
	catch {
		$pos = ""
		if ($_.InvocationInfo -and $_.InvocationInfo.PositionMessage) {
			$pos = "`r`n`r`n" + $_.InvocationInfo.PositionMessage
		}
		Fail ($_.Exception.Message + $pos)
	}
})

$form.Add_Shown({ $form.Activate() })
$form.ShowDialog() | Out-Null
