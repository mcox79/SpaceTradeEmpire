<#
.SYNOPSIS
    SPACE TRADE EMPIRE - MISSION CONTROL (v4.2)
    Fix: Strict Type Casting for Godot Path to prevent array errors.
#>

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# --- CONFIGURATION ---
$ProjectRoot = Get-Location
$ContextFile = Join-Path (Join-Path $ProjectRoot "_scratch") "_FullProjectContext.txt"
$TestScene   = "scenes/tests/test_economy_core.tscn"
$ConfigPath  = Join-Path $ProjectRoot "godot_path.cfg"

# --- GUI SETUP ---
$form = New-Object System.Windows.Forms.Form
$form.Text = "STE :: Mission Control v4.2"
$form.Size = New-Object System.Drawing.Size(450, 520)
$form.StartPosition = "CenterScreen"
$form.BackColor = "#1e1e1e"
$form.ForeColor = "#ffffff"

$fontHeader = New-Object System.Drawing.Font("Consolas", 12, [System.Drawing.FontStyle]::Bold)
$fontNormal = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$fontLog    = New-Object System.Drawing.Font("Consolas", 9)

# --- LOGIC ---

function Log-Output($message) {
    $txtOutput.AppendText("[$((Get-Date).ToString('HH:mm:ss'))] $message`r`n")
    $txtOutput.ScrollToCaret()
}

function Get-GodotExe {
    $exePath = $null

    # 1. Check saved config
    if (Test-Path $ConfigPath) {
        $raw = Get-Content $ConfigPath -Raw
        if ($raw) { $exePath = $raw.Trim() }
    }

    # 2. If valid, return immediately
    if ($exePath -and (Test-Path $exePath)) { 
        return $exePath 
    }

    # 3. If invalid, ask user
    [System.Windows.Forms.MessageBox]::Show("Godot executable not found.`n`nPlease select your godot.exe file.", "Setup Required")
    
    $dlg = New-Object System.Windows.Forms.OpenFileDialog
    $dlg.Title = "SELECT GODOT EXECUTABLE (godot.exe)"
    $dlg.Filter = "Executable|*.exe"
    
    if ($dlg.ShowDialog() -eq "OK") {
        $exePath = $dlg.FileName
        Set-Content -Path $ConfigPath -Value $exePath
        return $exePath
    }
    
    return $null
}

function Run-Tests {
    Log-Output ">>> EXEC: ECONOMY TEST SUITE"
    
    # FORCE STRING CAST (The Fix)
    # We take the last item in case pipeline pollution occurred
    $godotRaw = Get-GodotExe
    $godotExe = @($godotRaw)[-1] 

    if (-not $godotExe -or -not (Test-Path $godotExe)) {
        Log-Output "ERROR: Godot path missing or invalid."
        return
    }

    Log-Output "Target: $godotExe"

    try {
        Log-Output "Running simulation..."
        # Runs Godot in headless mode (-s) or standard, waiting for exit
        $process = Start-Process -FilePath "$godotExe" -ArgumentList "$TestScene" -NoNewWindow -Wait -PassThru
        
        if ($process.ExitCode -eq 0) {
            Log-Output "Cycle Complete. Check Console window."
        } else {
            Log-Output "WARNING: Process exited with code $($process.ExitCode)"
        }
    }
    catch {
        Log-Output "ERROR: $($_.Exception.Message)"
    }
}

function Run-ContextGen {
    Log-Output ">>> EXEC: SMART CONTEXT DUMP"
    try {
        $content = "=== SPACE TRADE EMPIRE PROJECT DUMP ===`n"
        $content += "dump_timestamp: $(Get-Date)`n`n"
        
        $content += "=== LIVE PROJECT STRUCTURE ===`n"
        try {
            $tree = Get-ChildItem -Path $ProjectRoot -Recurse | 
                Where-Object { $_.FullName -notmatch "\\.godot|\\.git|\\.import" } |
                ForEach-Object { 
                    $rel = $_.FullName.Replace($ProjectRoot.Path, "")
                    $indent = "  " * ($rel.Split('\').Count - 1)
                    if ($_.PSIsContainer) { "+ $indent[$($_.Name)]" } else { "- $indent$($_.Name)" }
                }
            $content += $tree
        } catch {}
        $content += "`n================================`n"

        $files = Get-ChildItem -Path $ProjectRoot -Recurse -Include *.gd, *.tscn, *.shader, *.ps1 | 
                 Where-Object { $_.FullName -notmatch ".git" -and $_.FullName -notmatch ".import" }

        foreach ($file in $files) {
            $relPath = $file.FullName.Replace($ProjectRoot.Path, "")
            $content += "`n--- FILE: $relPath ---`n"
            $content += "---------------------------------`n"
            $content += Get-Content $file.FullName -Raw
            $content += "`n"
        }
        
        $tmp = "$ContextFile.tmp"
Set-Content -Path $tmp -Value $content -Encoding UTF8
Move-Item -Force -Path $tmp -Destination $ContextFile
        Log-Output "SUCCESS: Context refreshed."
    }
    catch {
        Log-Output "ERROR: $($_.Exception.Message)"
    }
}

function Run-GitSave {
    Log-Output ">>> EXEC: GIT SNAPSHOT"
    git add -u
    $res = git commit -m "Manual Save Point $(Get-Date)" 2>&1
    Log-Output $res
    Log-Output "State Saved."
}

function Run-GitReset {
    $confirm = [System.Windows.Forms.MessageBox]::Show("NUKE IT? This reverts to the last SAVE.", "DANGER ZONE", [System.Windows.Forms.MessageBoxButtons]::YesNo, [System.Windows.Forms.MessageBoxIcon]::Warning)
    if ($confirm -eq "Yes") {
        Log-Output ">>> EXEC: HARD RESET"
        git reset --hard 2>&1 | Out-Null
        git clean -fdX 2>&1 | Out-Null
        Log-Output "SUCCESS: Time machine activated. Changes reverted."
    }
}

# --- UI COMPONENTS ---

# Header
$lblHeader = New-Object System.Windows.Forms.Label
$lblHeader.Text = "COMMAND DECK"
$lblHeader.Location = New-Object System.Drawing.Point(20, 10)
$lblHeader.Font = $fontHeader
$lblHeader.AutoSize = $true
$form.Controls.Add($lblHeader)

# 1. TEST (Blue)
$btnTest = New-Object System.Windows.Forms.Button
$btnTest.Text = "1. RUN UNIT TESTS"
$btnTest.Location = New-Object System.Drawing.Point(20, 40)
$btnTest.Size = New-Object System.Drawing.Size(400, 45)
$btnTest.Font = $fontNormal
$btnTest.BackColor = "#007acc"
$btnTest.ForeColor = "White"
$btnTest.FlatStyle = "Flat"
$btnTest.Add_Click({ Run-Tests })
$form.Controls.Add($btnTest)

# 2. DUMP (Gray)
$btnContext = New-Object System.Windows.Forms.Button
$btnContext.Text = "2. GENERATE CONTEXT"
$btnContext.Location = New-Object System.Drawing.Point(20, 95)
$btnContext.Size = New-Object System.Drawing.Size(400, 45)
$btnContext.Font = $fontNormal
$btnContext.BackColor = "#2d2d30"
$btnContext.ForeColor = "White"
$btnContext.FlatStyle = "Flat"
$btnContext.Add_Click({ Run-ContextGen })
$form.Controls.Add($btnContext)

# 3. SAVE (Green)
$btnSave = New-Object System.Windows.Forms.Button
$btnSave.Location = New-Object System.Drawing.Point(20, 150)
$btnSave.Size = New-Object System.Drawing.Size(195, 45)
$btnSave.Text = "3. SAVE STATE"
$btnSave.BackColor = "#228822"
$btnSave.ForeColor = "White"
$btnSave.Font = $fontNormal
$btnSave.FlatStyle = "Flat"
$btnSave.Add_Click({ Run-GitSave })
$form.Controls.Add($btnSave)

# 4. RESET (Red)
$btnUndo = New-Object System.Windows.Forms.Button
$btnUndo.Location = New-Object System.Drawing.Point(225, 150)
$btnUndo.Size = New-Object System.Drawing.Size(195, 45)
$btnUndo.Text = "4. HARD RESET"
$btnUndo.BackColor = "#cc2222"
$btnUndo.ForeColor = "White"
$btnUndo.Font = $fontNormal
$btnUndo.FlatStyle = "Flat"
$btnUndo.Add_Click({ Run-GitReset })
$form.Controls.Add($btnUndo)

# Console Output
$txtOutput = New-Object System.Windows.Forms.TextBox
$txtOutput.Location = New-Object System.Drawing.Point(20, 210)
$txtOutput.Size = New-Object System.Drawing.Size(400, 250)
$txtOutput.Multiline = $true
$txtOutput.ScrollBars = "Vertical"
$txtOutput.BackColor = "#000000"
$txtOutput.ForeColor = "#00ff00"
$txtOutput.Font = $fontLog
$txtOutput.Text = "Mission Control v4.2 Online...`r`n"
$form.Controls.Add($txtOutput)

# --- LAUNCH ---
$form.Add_Shown({ $form.Activate() })
[void] $form.ShowDialog()
