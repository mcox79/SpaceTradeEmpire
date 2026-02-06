if (-not (Test-Path variable:global:DEVTOOL_HEADLESS)) { $global:DEVTOOL_HEADLESS = $false }

<#
.SYNOPSIS
    SPACE TRADE EMPIRE - MISSION CONTROL (v5.2)
    
    Status:
    1. Verify Logic (dotnet test)
    2. Context Packet (LLM)
    3. Connectivity Scan (Architecture) -> NEW BUTTON
    4. Git Snapshot
#>

if (-not $global:DEVTOOL_HEADLESS) { Add-Type -AssemblyName System.Windows.Forms }
if (-not $global:DEVTOOL_HEADLESS) { Add-Type -AssemblyName System.Drawing }

# --- CONFIGURATION ---
$ProjectRoot    = Get-Location
$ScriptsDir     = Join-Path $ProjectRoot "scripts\tools"
$ContextScript  = Join-Path $ScriptsDir "New-ContextPacket.ps1"
$ScanScript     = Join-Path $ScriptsDir "Scan-Connectivity.ps1" # <--- The Connectivity Scanner

# --- GUI SETUP ---
$form = New-Object System.Windows.Forms.Form
$form.Text = "STE :: Mission Control v5.2"
$form.Size = New-Object System.Drawing.Size(450, 560)
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

# 1. LOGIC VERIFICATION
function Run-SimCoreTests {
    Log-Output ">>> EXEC: SIMCORE LOGIC TESTS (dotnet)"
    try {
        $p = Start-Process -FilePath "dotnet" -ArgumentList "test SimCore.Tests" -NoNewWindow -Wait -PassThru -RedirectStandardOutput "test_out.log"
        
        $output = Get-Content "test_out.log"
        $output | ForEach-Object { Log-Output $_ }
        Remove-Item "test_out.log" -ErrorAction SilentlyContinue

        if ($p.ExitCode -eq 0) {
            Log-Output "GREEN BOARD: Logic Verified."
        } else {
            Log-Output "TEST FAILURE: Check logs."
        }
    } catch {
        Log-Output "ERROR: dotnet command failed. Is .NET installed?"
    }
}

# 2. CONTEXT GENERATION
function Run-ContextGen {
    Log-Output ">>> EXEC: CONTEXT GENERATOR"
    if (-not (Test-Path $ContextScript)) { Log-Output "ERROR: Missing $ContextScript"; return }

    try {
        & $ContextScript
        Log-Output "CONTEXT PACKET UPDATED."
    } catch { Log-Output "ERROR: $($_.Exception.Message)" }
}

# 3. CONNECTIVITY SCAN (NEW)
function Run-ConnectivityScan {
    Log-Output ">>> EXEC: ARCHITECTURE SCAN"
    if (-not (Test-Path $ScanScript)) { 
        Log-Output "ERROR: Script missing at $ScanScript"
        Log-Output "   - Please verify 'scripts\tools\Scan-Connectivity.ps1' exists."
        return 
    }

    try {
        & $ScanScript -Force
        Log-Output "CONNECTIVITY SCAN COMPLETE."
    } catch { Log-Output "ERROR: $($_.Exception.Message)" }
}

# 4. SNAPSHOT
function Run-GitSave {
    Log-Output ">>> EXEC: GIT SNAPSHOT"
    git add .
    $res = git commit -m "wip: snapshot via Mission Control $(Get-Date -Format 'HH:mm')" 2>&1
    if ($LASTEXITCODE -eq 0) {
        Log-Output "SNAPSHOT SECURED."
    } else {
        Log-Output $res
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
$btnTest.Text = "1. VERIFY LOGIC (Tests)"
$btnTest.Location = New-Object System.Drawing.Point(20, 40)
$btnTest.Size = New-Object System.Drawing.Size(400, 45)
$btnTest.Font = $fontNormal
$btnTest.BackColor = "#007acc"
$btnTest.ForeColor = "White"
$btnTest.FlatStyle = "Flat"
$btnTest.Add_Click({ Run-SimCoreTests })
$form.Controls.Add($btnTest)

# 2. CONTEXT (Purple)
$btnContext = New-Object System.Windows.Forms.Button
$btnContext.Text = "2. GENERATE PACKET"
$btnContext.Location = New-Object System.Drawing.Point(20, 95)
$btnContext.Size = New-Object System.Drawing.Size(400, 45)
$btnContext.Font = $fontNormal
$btnContext.BackColor = "#6a00ff"
$btnContext.ForeColor = "White"
$btnContext.FlatStyle = "Flat"
$btnContext.Add_Click({ Run-ContextGen })
$form.Controls.Add($btnContext)

# 3. CONNECTIVITY (Orange - The New Button)
$btnScan = New-Object System.Windows.Forms.Button
$btnScan.Text = "3. SCAN CONNECTIVITY (Arch)"
$btnScan.Location = New-Object System.Drawing.Point(20, 150)
$btnScan.Size = New-Object System.Drawing.Size(400, 45)
$btnScan.Font = $fontNormal
$btnScan.BackColor = "#d46a00"
$btnScan.ForeColor = "White"
$btnScan.FlatStyle = "Flat"
$btnScan.Add_Click({ Run-ConnectivityScan })
$form.Controls.Add($btnScan)

# 4. SAVE (Green)
$btnSave = New-Object System.Windows.Forms.Button
$btnSave.Location = New-Object System.Drawing.Point(20, 205)
$btnSave.Size = New-Object System.Drawing.Size(400, 45)
$btnSave.Text = "4. GIT SNAPSHOT"
$btnSave.BackColor = "#228822"
$btnSave.ForeColor = "White"
$btnSave.Font = $fontNormal
$btnSave.FlatStyle = "Flat"
$btnSave.Add_Click({ Run-GitSave })
$form.Controls.Add($btnSave)

# Console Output
$txtOutput = New-Object System.Windows.Forms.TextBox
$txtOutput.Location = New-Object System.Drawing.Point(20, 265)
$txtOutput.Size = New-Object System.Drawing.Size(400, 240)
$txtOutput.Multiline = $true
$txtOutput.ScrollBars = "Vertical"
$txtOutput.BackColor = "#000000"
$txtOutput.ForeColor = "#00ff00"
$txtOutput.Font = $fontLog
$txtOutput.Text = "Mission Control v5.2 Online...`r`n"
$form.Controls.Add($txtOutput)

# --- LAUNCH ---
$form.Add_Shown({ $form.Activate() })
if (-not $global:DEVTOOL_HEADLESS) { [void] $form.ShowDialog() }