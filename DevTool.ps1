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
$ProjectRoot = (& git rev-parse --show-toplevel 2>$null)
if (-not $ProjectRoot) { throw "Not in a git repo (git rev-parse failed)." }
$ProjectRoot = $ProjectRoot.Trim()
Set-Location $ProjectRoot

$ScriptsDir     = Join-Path $ProjectRoot "scripts\tools"
$ContextScript  = Join-Path $ScriptsDir "New-ContextPacket.ps1"
$ScanScript     = Join-Path $ScriptsDir "Scan-Connectivity.ps1" # <--- The Connectivity Scanner
$StatusScript   = Join-Path $ScriptsDir "New-StatusPacket.ps1"

# --- GUI SETUP ---
$form = New-Object System.Windows.Forms.Form
$form.Text = "STE :: Mission Control v5.2"
$form.Size = New-Object System.Drawing.Size(450, 620)

$form.StartPosition = "CenterScreen"
$form.BackColor = "#1e1e1e"
$form.ForeColor = "#ffffff"

$fontHeader = New-Object System.Drawing.Font("Consolas", 12, [System.Drawing.FontStyle]::Bold)
$fontNormal = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$fontLog    = New-Object System.Drawing.Font("Consolas", 9)

# Non-blocking test runner state (WinForms UI thread must stay free)
$global:VerifyJob = $null
$global:VerifyTimer = New-Object System.Windows.Forms.Timer
$global:VerifyTimer.Interval = 250

$script:VerifyLogPath = $null

function On-VerifyTick {
    if (-not $global:VerifyJob) { $global:VerifyTimer.Stop(); return }
    if ($global:VerifyJob.State -eq "Running") { return }

    $global:VerifyTimer.Stop()

    $exitCode = 1
    try {
        $result = @(Receive-Job -Job $global:VerifyJob -ErrorAction SilentlyContinue)
        if ($result.Count -gt 0) { $exitCode = [int]$result[-1] }
    } catch { $exitCode = 1 }

    try { Remove-Job -Job $global:VerifyJob -Force -ErrorAction SilentlyContinue } catch {}
    $global:VerifyJob = $null

    if ($script:VerifyLogPath -and (Test-Path $script:VerifyLogPath)) {
        Get-Content -LiteralPath $script:VerifyLogPath -Tail 200 | ForEach-Object { Log-Output $_ }
    } else {
        Log-Output "ERROR: Missing test log at $script:VerifyLogPath"
    }

    if ($exitCode -eq 0) { Log-Output "GREEN BOARD: Logic Verified." }
    else { Log-Output "TEST FAILURE: ExitCode=$exitCode" }

    $btnTest.Enabled = $true
}

$global:VerifyTimer.Add_Tick({ On-VerifyTick })


function On-VerifyTick {
    if (-not $global:VerifyJob) { $global:VerifyTimer.Stop(); return }
    if ($global:VerifyJob.State -eq "Running") { return }

    $global:VerifyTimer.Stop()

    $exitCode = 1
    try {
        $result = @(Receive-Job -Job $global:VerifyJob -ErrorAction SilentlyContinue)
        if ($result.Count -gt 0) { $exitCode = [int]$result[-1] }
    } catch { $exitCode = 1 }

    try { Remove-Job -Job $global:VerifyJob -Force -ErrorAction SilentlyContinue } catch {}
    $global:VerifyJob = $null

    if ($script:VerifyLogPath -and (Test-Path $script:VerifyLogPath)) {
        Get-Content -LiteralPath $script:VerifyLogPath -Tail 200 | ForEach-Object { Log-Output $_ }
    } else {
        Log-Output "ERROR: Missing test log at $script:VerifyLogPath"
    }

    if ($exitCode -eq 0) { Log-Output "GREEN BOARD: Logic Verified." }
    else { Log-Output "TEST FAILURE: ExitCode=$exitCode" }

    $btnTest.Enabled = $true
}

$global:VerifyTimer.Add_Tick({ On-VerifyTick })



# --- LOGIC ---

$DevToolLogPath = Join-Path (Get-Location) "docs\generated\devtool_log.txt"

$script:VerifyLogPath = $null

function Log-Output($message) {
    $line = "[$((Get-Date).ToString('HH:mm:ss'))] $message"
    $txtOutput.AppendText($line + "`r`n")
    $txtOutput.ScrollToCaret()

    try {
        $dir = Split-Path -Parent $DevToolLogPath
        if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
        Add-Content -LiteralPath $DevToolLogPath -Value $line
    } catch {
        # ignore logging failures
    }
}

# 1. LOGIC VERIFICATION
function Run-SimCoreTests {
    Log-Output ">>> EXEC: SIMCORE LOGIC TESTS (dotnet)"

    if ($global:VerifyJob -and $global:VerifyJob.State -eq "Running") {
        Log-Output "Verify already running."
        return
    }

    $repoRoot = $ProjectRoot
	$script:VerifyLogPath = Join-Path $ProjectRoot "docs\generated\05_TEST_SUMMARY.txt"
	$logPath = $script:VerifyLogPath


    try {
        $logDir = Split-Path -Parent $logPath
        if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Force -Path $logDir | Out-Null }
        if (Test-Path $logPath) { Remove-Item -Force -LiteralPath $logPath }

        # Disable the button while running (prevents double-click)
        $btnTest.Enabled = $false

        # Run in background job so the GUI thread stays responsive
        $global:VerifyJob = Start-Job -ArgumentList $repoRoot, $logPath -ScriptBlock {
            param($root, $lp)
            Set-Location $root

            $out = (dotnet test SimCore.Tests -v minimal 2>&1) -join "`r`n"
	    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
	    [System.IO.File]::WriteAllText($lp, $out + "`r`n", $utf8NoBom)

	    return $LASTEXITCODE

        }

        # Timer polls for completion and then re-enables the UI
        $global:VerifyTimer.Stop()
        $global:VerifyTimer.Start()
        Log-Output "Verify running in background. Log: $logPath"

	
    } catch {
        Log-Output "ERROR (full):"
        Log-Output ($_ | Out-String)
        $btnTest.Enabled = $true
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

# 5. STATUS
if (-not $global:DEVTOOL_HEADLESS) { Add-Type -AssemblyName Microsoft.VisualBasic }

function Run-StatusPacket {
    Log-Output ">>> EXEC: STATUS PACKET"

    if (-not (Test-Path $StatusScript)) {
        Log-Output "ERROR: Missing $StatusScript"
        Log-Output "   - Please add 'scripts\tools\New-StatusPacket.ps1'"
        return
    }

    try {
	    & $StatusScript
	    Log-Output "STATUS PACKET WRITTEN: docs/generated/02_STATUS_PACKET.txt"
	} catch {
	    Log-Output "ERROR (full):"
	    Log-Output ($_ | Out-String)
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

# 5. STATUS PACKET (Gray)
$btnStatus = New-Object System.Windows.Forms.Button
$btnStatus.Location = New-Object System.Drawing.Point(20, 260)
$btnStatus.Size = New-Object System.Drawing.Size(400, 45)
$btnStatus.Text = "5. GENERATE STATUS PACKET"
$btnStatus.BackColor = "#444444"
$btnStatus.ForeColor = "White"
$btnStatus.Font = $fontNormal
$btnStatus.FlatStyle = "Flat"
$btnStatus.Add_Click({ Run-StatusPacket })
$form.Controls.Add($btnStatus)

# Console Output
$txtOutput = New-Object System.Windows.Forms.TextBox
$txtOutput.Location = New-Object System.Drawing.Point(20, 320)
$txtOutput.Size = New-Object System.Drawing.Size(400, 190)
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