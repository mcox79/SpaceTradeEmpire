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

$repoRoot = Get-RepoRoot
Set-Location $repoRoot
$scratchRoot = Ensure-Scratch $repoRoot

$form = New-Object System.Windows.Forms.Form
$form.Text = "Apply Patch (Unified Diff)"
$form.Size = New-Object System.Drawing.Size(900,650)
$form.StartPosition = "CenterScreen"

$topPanel = New-Object System.Windows.Forms.FlowLayoutPanel
$topPanel.Dock = "Top"
$topPanel.Height = 46
$topPanel.FlowDirection = "LeftToRight"
$topPanel.WrapContents = $false
$topPanel.Padding = New-Object System.Windows.Forms.Padding(8,8,8,8)

$btnLoad = New-Object System.Windows.Forms.Button
$btnLoad.Text = "Load Patch File..."
$btnLoad.Width = 150
$btnLoad.Height = 28

$btnApply = New-Object System.Windows.Forms.Button
$btnApply.Text = "Apply Patch"
$btnApply.Width = 120
$btnApply.Height = 28

$btnDiag = New-Object System.Windows.Forms.Button
$btnDiag.Text = "Diagnostics"
$btnDiag.Width = 110
$btnDiag.Height = 28

$btnClear = New-Object System.Windows.Forms.Button
$btnClear.Text = "Clear"
$btnClear.Width = 80
$btnClear.Height = 28

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
$logBox.Height = 150

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

$btnDiag.Add_Click({
	$patchText = Strip-MarkdownFences $textBox.Text
	$patchText = $patchText.TrimEnd()

	if (-not $patchText.Trim()) { Fail "No patch text provided."; return }
	if ($patchText -notmatch "(?m)^\s*diff --git\s+") { Fail "Patch must contain a 'diff --git' header line."; return }

	$diffCount = ([regex]::Matches($patchText, "(?m)^\s*diff --git\s+")).Count
	$utf8Bytes = [System.Text.Encoding]::UTF8.GetBytes($patchText)
	$hex10 = First-N-BytesHex $utf8Bytes 10

	Info ("Diagnostics:`r`n`r`nDiff headers: $diffCount`r`nText UTF-8 first 10 bytes: $hex10`r`n`r`nExpected first 10 bytes: 64 69 66 66 20 2D 2D 67 69 74 (diff --git)")
})

$btnLoad.Add_Click({
	try {
		$dlg = New-Object System.Windows.Forms.OpenFileDialog
		$dlg.Title = "Select a unified diff patch file"
		$dlg.Filter = "Patch files (*.diff;*.patch)|*.diff;*.patch|All files (*.*)|*.*"
		$dlg.InitialDirectory = $repoRoot

		if ($dlg.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) { return }

		$bytes = [System.IO.File]::ReadAllBytes($dlg.FileName)
		$text = [System.IO.File]::ReadAllText($dlg.FileName)

		Log-Line ("Loaded patch: " + $dlg.FileName)
		Log-Line ("File first 10 bytes: " + (First-N-BytesHex $bytes 10))

		if (Has-Bom $bytes) {
			Info "Warning: patch file appears to include a BOM (UTF-8 BOM or UTF-16). This commonly breaks git apply. This tool will re-write the patch as UTF-8 without BOM before applying."
		}

		$textBox.Text = $text
	}
	catch {
		Fail $_.Exception.Message
	}
})

$btnApply.Add_Click({
	try {
		$patchText = Strip-MarkdownFences $textBox.Text
		$patchText = $patchText.TrimEnd()

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

		$files = (& git diff --name-only).Trim() | Where-Object { $_ }
		if ($files.Count -gt 5) {
			Fail "Change budget exceeded: $($files.Count) files touched. Revert recommended: git reset --hard"
			return
		}

		if (Test-Path ".\Check-GDScriptIndent.ps1") {
			$indentOut = & powershell -NoProfile -ExecutionPolicy Bypass -File ".\Check-GDScriptIndent.ps1" 2>&1
			if ($LASTEXITCODE -ne 0) {
				Fail ("Tabs-only indentation gate failed:`r`n`r`n" + ($indentOut -join "`r`n"))
				return
			}
		}

		Log-Line "Patch applied successfully."
		Info ("Patch applied successfully.`r`n`r`nTouched files:`r`n" + ($files -join "`r`n"))
	}
	catch {
		Fail $_.Exception.Message
	}
})

$form.Add_Shown({ $form.Activate() })
$form.ShowDialog() | Out-Null
