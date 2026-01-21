Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# --- CONFIGURATION ---
$gameName = "Space Trade Empire"
$projectPath = "$HOME\Documents\$gameName"
$dumpFile = "$projectPath\_FullProjectContext.txt"
$masterContext = "$projectPath\_PROJECT_CONTEXT.md"

# --- WINDOW SETUP ---
$form = New-Object System.Windows.Forms.Form
$form.Text = "Space Trade Empire - DevOps Console v3.0"
$form.Size = New-Object System.Drawing.Size(500, 450)
$form.StartPosition = "CenterScreen"
$form.BackColor = "#1e1e1e"
$form.ForeColor = "#ffffff"

# --- STYLES ---
$btnFont = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$logFont = New-Object System.Drawing.Font("Consolas", 9)

function Log-Message($msg) {
    $textBox.AppendText("[$((Get-Date).ToString('HH:mm:ss'))] $msg`r`n")
    $textBox.ScrollToCaret()
}

# --- FUNCTION 1: CONTEXT DUMP ---
$btnDump = New-Object System.Windows.Forms.Button
$btnDump.Location = New-Object System.Drawing.Point(20, 20)
$btnDump.Size = New-Object System.Drawing.Size(440, 50)
$btnDump.Text = "1. GENERATE SMART CONTEXT DUMP"
$btnDump.BackColor = "#4488ff"
$btnDump.ForeColor = "White"
$btnDump.Font = $btnFont

$btnDump.Add_Click({
    Log-Message "Starting Smart Dump..."
    try {
        $finalContent = @()
        $finalContent += "dump_timestamp: $(Get-Date)"
        
        if (Test-Path $masterContext) {
            $finalContent += "`n`n=== MASTER ARCHITECTURE & WORKFLOW ==="
            $finalContent += Get-Content $masterContext
            $finalContent += "========================================`n"
        }

        $finalContent += "`n`n=== LIVE PROJECT STRUCTURE ==="
        try {
            $tree = Get-ChildItem -Path $projectPath -Recurse | 
                Where-Object { $_.FullName -notmatch "\\.godot|\\.git" } |
                ForEach-Object { 
                    $rel = $_.FullName.Substring($projectPath.Length + 1)
                    $indent = "  " * ($rel.Split('\').Count - 1)
                    if ($_.PSIsContainer) { "+ $indent[$($_.Name)]" } else { "- $indent$($_.Name)" }
                }
            $finalContent += $tree
        } catch {}
        $finalContent += "================================`n"

        $files = Get-ChildItem -Path $projectPath -Recurse | 
            Where-Object { 
                (-not $_.PSIsContainer) -and 
                ($_.Extension -match "\.(gd|tscn|godot)$") -and 
                ($_.FullName -notmatch "\\.godot\\") 
            }

        foreach ($file in $files) {
            $relativePath = $file.FullName.Substring($projectPath.Length + 1)
            $finalContent += "`n`n--- FILE: $relativePath ---"
            $finalContent += "---------------------------------"
            $finalContent += Get-Content $file.FullName
        }
        
        $finalContent | Set-Content -Path $dumpFile
        Log-Message "SUCCESS: Context Dump Updated."
    } catch {
        Log-Message "ERROR: $($_.Exception.Message)"
    }
})

# --- FUNCTION 2: SAVE STATE ---
$btnSave = New-Object System.Windows.Forms.Button
$btnSave.Location = New-Object System.Drawing.Point(20, 90)
$btnSave.Size = New-Object System.Drawing.Size(210, 50)
$btnSave.Text = "2. SAVE STATE (Commit)"
$btnSave.BackColor = "#228822"
$btnSave.ForeColor = "White"
$btnSave.Font = $btnFont

$btnSave.Add_Click({
    Log-Message "Saving State..."
    Set-Location $projectPath
    git add . 2>&1 | Out-Null
    $res = git commit -m "Manual Save Point" 2>&1
    Log-Message $res
    Log-Message "State Locked."
})

# --- FUNCTION 3: UNDO ---
$btnUndo = New-Object System.Windows.Forms.Button
$btnUndo.Location = New-Object System.Drawing.Point(250, 90)
$btnUndo.Size = New-Object System.Drawing.Size(210, 50)
$btnUndo.Text = "3. F*CK UP UNDO (Reset)"
$btnUndo.BackColor = "#cc2222"
$btnUndo.ForeColor = "White"
$btnUndo.Font = $btnFont

$btnUndo.Add_Click({
    $confirm = [System.Windows.Forms.MessageBox]::Show("Are you sure? This deletes all changes since the last Save.", "Nuclear Option", [System.Windows.Forms.MessageBoxButtons]::YesNo)
    if ($confirm -eq "Yes") {
        Log-Message "Reverting to last save..."
        Set-Location $projectPath
        git reset --hard 2>&1 | Out-Null
        git clean -fd 2>&1 | Out-Null
        Log-Message "SUCCESS: Project reset to last stable point."
    }
})

$textBox = New-Object System.Windows.Forms.TextBox
$textBox.Location = New-Object System.Drawing.Point(20, 160)
$textBox.Size = New-Object System.Drawing.Size(440, 230)
$textBox.Multiline = $true
$textBox.ScrollBars = "Vertical"
$textBox.ReadOnly = $true
$textBox.BackColor = "#000000"
$textBox.ForeColor = "#00ff00"
$textBox.Font = $logFont
$textBox.Text = "DevOps Console v3.0 Ready...`r`n"

$form.Controls.Add($btnDump)
$form.Controls.Add($btnSave)
$form.Controls.Add($btnUndo)
$form.Controls.Add($textBox)
$form.ShowDialog()
