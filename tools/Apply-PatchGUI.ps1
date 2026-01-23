Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Fail($msg) {
	[System.Windows.Forms.MessageBox]::Show($msg, "Patch Apply Failed",
		[System.Windows.Forms.MessageBoxButtons]::OK,
		[System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null
}

function Info($msg) {
	[System.Windows.Forms.MessageBox]::Show($msg, "Patch Apply",
		[System.Windows.Forms.MessageBoxButtons]::OK,
		[System.Windows.Forms.MessageBoxIcon]::Information) | Out-Null
}

function Get-RepoRoot {
	$root = (& git rev-parse --show-toplevel 2>$null)
	if (-not $root) { throw "Not in a git repository." }
	return $root.Trim()
}

$repoRoot = Get-RepoRoot
Set-Location $repoRoot

# ---------- UI ----------

$form = New-Object System.Windows.Forms.Form
$form.Text = "Apply Patch (Unified Diff)"
$form.Size = New-Object System.Drawing.Size(900,600)
$form.StartPosition = "CenterScreen"

$textBox = New-Object System.Windows.Forms.TextBox
$textBox.Multiline = $true
$textBox.ScrollBars = "Both"
$textBox.Font = New-Object System.Drawing.Font("Consolas",10)
$textBox.Dock = "Fill"

$button = New-Object System.Windows.Forms.Button
$button.Text = "Apply Patch"
$button.Dock = "Bottom"
$button.Height = 40

$form.Controls.Add($textBox)
$form.Controls.Add($button)

# ---------- Logic ----------

$button.Add_Click({
	try {
		$patchText = $textBox.Text
		if (-not $patchText.Trim()) {
			Fail "No patch text provided."
			return
		}

		# Strip markdown fences
		$patchText = $patchText -replace "(?m)^\s*```(?:diff)?\s*$", ""
		$patchText = $patchText -replace "(?m)^\s*```\s*$", ""

		if ($patchText -notmatch "(?m)^\s*(diff --git|---\s|\*\*\* Begin Patch)") {
			Fail "Text does not look like a unified diff patch."
			return
		}

		if (-not (Test-Path "_scratch")) {
			New-Item -ItemType Directory -Path "_scratch" | Out-Null
		}

		$patchPath = "_scratch\patch.diff"
		Set-Content -Path $patchPath -Value $patchText -Encoding UTF8

		& git apply --check $patchPath
		if ($LASTEXITCODE -ne 0) {
			Fail "git apply --check failed. Patch is invalid."
			return
		}

		& git apply $patchPath
		if ($LASTEXITCODE -ne 0) {
			Fail "git apply failed."
			return
		}

		# Change budget gate
		$files = (& git diff --name-only).Trim() | Where-Object { $_ }
		if ($files.Count -gt 5) {
			Fail "Change budget exceeded: $($files.Count) files touched."
			return
		}

		# Indentation gate
		if (Test-Path ".\Check-GDScriptIndent.ps1") {
			& powershell -NoProfile -ExecutionPolicy Bypass -File ".\Check-GDScriptIndent.ps1"
			if ($LASTEXITCODE -ne 0) {
				Fail "Tabs-only indentation gate failed."
				return
			}
		}

		Info "Patch applied successfully.`n`nTouched files:`n$($files -join "`n")"
	}
	catch {
		Fail $_.Exception.Message
	}
})

$form.ShowDialog() | Out-Null
