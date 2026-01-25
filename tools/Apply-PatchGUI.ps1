Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ==============================================================================
# 1. THE SERVICE LAYER (Git Operations & I/O)
# ==============================================================================
function Get-RepoRoot {
    $root = (& git rev-parse --show-toplevel 2>$null)
    if (-not $root) { throw "Not in a valid Git repository." }
    return $root.Trim()
}

function Ensure-Scratch([string]$repoRoot) {
    $scratch = Join-Path $repoRoot "_scratch"
    if (-not (Test-Path $scratch)) { New-Item -ItemType Directory -Path $scratch | Out-Null }
    return $scratch
}

function Assert-CleanTree {
    $lines = @( & git status --porcelain --untracked-files=no 2>$null | Where-Object { $_ -and $_.Trim() } )
    if ($lines.Length -gt 0) {
        throw "Working directory must be clean of tracked changes.`r`nDetected:`r`n$($lines -join "`r`n")"
    }
}

function Invoke-AtomicRevert {
    & git reset --hard HEAD 2>$null | Out-Null
    & git clean -fd 2>$null | Out-Null
}

function Write-Utf8NoBom([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text, (New-Object System.Text.UTF8Encoding($false)))
}

function Normalize-ToLF([string]$text) { return $text -replace "`r`n", "`n" }

function Parse-EditScript([string]$scriptText) {
    $lines = $scriptText -split "`r?`n"
    $opsList = New-Object System.Collections.Generic.List[PSCustomObject]
    $currentFile = $null; $i = 0

    function Read-Block([string]$startToken, [string]$endToken, [ref]$idx) {
        if ($idx.Value -ge $lines.Length) { throw "Unexpected EOF looking for $startToken" }
        $idx.Value++; $buf = New-Object System.Collections.Generic.List[string]
        while ($idx.Value -lt $lines.Length) {
            if ($lines[$idx.Value].Trim() -eq $endToken) { $idx.Value++; return $buf -join "`n" }
            $buf.Add($lines[$idx.Value]); $idx.Value++
        }
        throw "Missing $endToken"
    }

    while ($i -lt $lines.Length) {
        $raw = $lines[$i]; $clean = $raw.Trim(); $i++
        if (-not $clean -or $clean.StartsWith("#")) { continue }
        
        $cmd = ($clean -split '\s+')[0]
        $arg = ($raw -replace "^\s*$cmd\s*", "").Trim()

        switch ($cmd) {
            "FILE" { $currentFile = $arg }
            "REPLACE_BLOCK" { $opsList.Add([pscustomobject]@{ file=$currentFile; op="replace_block"; old=(Read-Block "BEGIN_OLD" "END_OLD" ([ref]$i)); new=(Read-Block "BEGIN_NEW" "END_NEW" ([ref]$i)) }) }
            "INSERT_AFTER" { $opsList.Add([pscustomobject]@{ file=$currentFile; op="insert_after"; anchor=$arg.Trim('"'); text=(Read-Block "BEGIN" "END" ([ref]$i)) }) }
        }
    }
    return @($opsList.ToArray())
}

function Apply-Ops-ToContent([string]$original, [object[]]$opsForFile) {
    $cur = Normalize-ToLF $original
    foreach ($op in $opsForFile) {
        if ($op.op -eq "replace_block") {
            $old = Normalize-ToLF $op.old; $new = Normalize-ToLF $op.new
            if ($cur.IndexOf($old) -lt 0) { throw "Target code block not found in $($op.file)." }
            $cur = $cur.Replace($old, $new)
        } elseif ($op.op -eq "insert_after") {
            $anchor = $op.anchor; $ins = Normalize-ToLF $op.text; $lines = @($cur -split "`n")
            $matches = New-Object System.Collections.Generic.List[int]
            for($j=0; $j -lt $lines.Length; $j++) { if ($lines[$j].Trim() -eq $anchor.Trim()) { $matches.Add($j) } }
            if ($matches.Count -ne 1) { throw "Anchor mismatch ($($matches.Count) found) for: $anchor" }
            $k = $matches[0]
            $cur = (($lines[0..$k] + $ins + $lines[($k+1)..($lines.Length-1)]) -join "`n")
        }
    }
    return $cur
}

# ==============================================================================
# 2. THE VIEW LAYER (UI Components)
# ==============================================================================
$repoRoot = Get-RepoRoot; Set-Location $repoRoot; $scratch = Ensure-Scratch $repoRoot
$form = New-Object System.Windows.Forms.Form; $form.Text = "Enterprise Patch Deployer v4.3"; $form.Size = "980,700"; $form.StartPosition = "CenterScreen"

# Navbar
$nav = New-Object System.Windows.Forms.FlowLayoutPanel; $nav.Dock = "Top"; $nav.Height = 45; $nav.BackColor = "#2d2d30"

$btnApply = New-Object System.Windows.Forms.Button; $btnApply.Text = "Deploy Patch"; $btnApply.Width = 150; $btnApply.Height = 35; $btnApply.BackColor = "#007acc"; $btnApply.ForeColor = "White"; $btnApply.FlatStyle = "Flat"
$btnClear = New-Object System.Windows.Forms.Button; $btnClear.Text = "Clear"; $btnClear.Width = 100; $btnClear.Height = 35; $btnClear.BackColor = "#cc2222"; $btnClear.ForeColor = "White"; $btnClear.FlatStyle = "Flat"

$nav.Controls.AddRange(@($btnApply, $btnClear))

# Editors
$txt = New-Object System.Windows.Forms.TextBox; $txt.Multiline = $true; $txt.Dock = "Fill"; $txt.ScrollBars = "Both"; $txt.Font = "Consolas, 10"; $txt.WordWrap = $false
$log = New-Object System.Windows.Forms.TextBox; $log.Multiline = $true; $log.Dock = "Bottom"; $log.Height = 150; $log.ReadOnly = $true; $log.BackColor = "Black"; $log.ForeColor = "Lime"

# Enable Ctrl+A
$txt.Add_KeyDown({
    if ($_.Control -and $_.KeyCode -eq 'A') { $txt.SelectAll(); $_.SuppressKeyPress = $true }
})

# ==============================================================================
# 3. THE CONTROLLER LAYER 
# ==============================================================================
$btnClear.Add_Click({ $txt.Clear() })

$btnApply.Add_Click({
    $log.AppendText("[$((Get-Date).ToString('HH:mm:ss'))] Validating state...`r`n")
    try {
        Assert-CleanTree
        $aOps = @(Parse-EditScript $txt.Text)
        if ($aOps.Length -eq 0) { throw "No valid operations found." } 
        
        $files = [string[]]($aOps | Select-Object -ExpandProperty file | Sort-Object -Unique)
        $log.AppendText("[$((Get-Date).ToString('HH:mm:ss'))] Target files: $($files -join ', ')`r`n")

        foreach ($f in $files) {
            $path = Join-Path $repoRoot $f
            if (-not (Test-Path $path)) { throw "File not found: $f" }
            $content = [System.IO.File]::ReadAllText($path)
            Write-Utf8NoBom $path (Apply-Ops-ToContent $content (@($aOps | Where-Object { $_.file -eq $f })))
        }

        foreach ($f in $files) { & git add --intent-to-add $f 2>$null }
        $diffText = & git --no-pager diff -- $files 2>$null | Out-String
        
        Invoke-AtomicRevert

        if ([string]::IsNullOrWhiteSpace($diffText)) { throw "No changes detected after parsing." }

        $p = Join-Path $scratch "edit.patch"; Write-Utf8NoBom $p $diffText
        & git apply $p

        $log.AppendText("[$((Get-Date).ToString('HH:mm:ss'))] SUCCESS: Patch applied cleanly.`r`n")
    } catch {
        Invoke-AtomicRevert
        # NEW: Print full error to the copyable log first
        $log.AppendText("[$((Get-Date).ToString('HH:mm:ss'))] FATAL ERROR:`r`n")
        $log.AppendText("$($_.Exception.Message)`r`n")
        $log.AppendText("[$((Get-Date).ToString('HH:mm:ss'))] REVERT SUCCESS: Codebase restored to safety.`r`n")
        
        [System.Windows.Forms.MessageBox]::Show($_.Exception.Message, "Deployment Failed", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null
    }
})

$form.Controls.AddRange(@($log, $txt, $nav))
$form.ShowDialog() | Out-Null