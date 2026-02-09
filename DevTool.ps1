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
$GateDeltaScript = Join-Path $ScriptsDir "New-GateClosureDelta.ps1"
$CapIndexScript  = Join-Path $ScriptsDir "New-CapabilityIndex.ps1"

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

# Non-blocking "Generate All" runner state
$global:GenAllJob = $null
$global:GenAllTimer = New-Object System.Windows.Forms.Timer
$global:GenAllTimer.Interval = 250

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

$script:GenAllSeen = 0

$script:GenAllSeen = 0

function On-GenAllTick {
    if (-not $global:GenAllJob) { $global:GenAllTimer.Stop(); return }

    # Pull only the new output since last tick by tracking count
    $chunk = @(Receive-Job -Job $global:GenAllJob -Keep -ErrorAction SilentlyContinue)
    if ($chunk.Count -gt $script:GenAllSeen) {
        for ($i = $script:GenAllSeen; $i -lt $chunk.Count; $i++) {
            $s = $chunk[$i]
            if ($null -ne $s -and -not [string]::IsNullOrWhiteSpace($s.ToString())) {
                Log-Output $s.ToString()
            }
        }
        $script:GenAllSeen = $chunk.Count
    }

    if ($global:GenAllJob.State -eq "Running") { return }

    $global:GenAllTimer.Stop()

    $exitCode = 1
    try {
        $final = @(Receive-Job -Job $global:GenAllJob -ErrorAction SilentlyContinue)
        if ($final.Count -gt 0) { $exitCode = [int]$final[-1] }
    } catch { $exitCode = 1 }

    try { Remove-Job -Job $global:GenAllJob -Force -ErrorAction SilentlyContinue } catch {}
    $global:GenAllJob = $null

    if ($exitCode -eq 0) { Log-Output "GENERATE ALL: OK." }
    else { Log-Output "GENERATE ALL: FAILED. ExitCode=$exitCode" }

    $btnGenAll.Enabled = $true
}

$global:GenAllTimer.Add_Tick({ On-GenAllTick })

# --- LOGIC ---

$script:VerifyLogPath = $null

function Log-Output($message) {
    $line = "[$((Get-Date).ToString('HH:mm:ss'))] $message"
    $txtOutput.AppendText($line + "`r`n")
    $txtOutput.ScrollToCaret()
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
        & $ScanScript -Force -Harden
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

# 4.5 GATE CLOSURE DELTA
function Run-GateClosureDelta {
    Log-Output ">>> EXEC: GATE CLOSURE DELTA"

    if (-not (Test-Path $GateDeltaScript)) {
        Log-Output "ERROR: Missing $GateDeltaScript"
        Log-Output "   - Please add 'scripts\tools\New-GateClosureDelta.ps1'"
        return
    }

    try {
        & $GateDeltaScript
        Log-Output "GATE CLOSURE DELTA WRITTEN: docs/generated/gate_closure_delta.md"
    } catch {
        Log-Output "ERROR (full):"
        Log-Output ($_ | Out-String)
    }
}

# 4.6 CAPABILITY INDEX
function Run-CapabilityIndex {
    Log-Output ">>> EXEC: CAPABILITY INDEX"

    if (-not (Test-Path $CapIndexScript)) {
        Log-Output "ERROR: Missing $CapIndexScript"
        Log-Output "   - Please add 'scripts\tools\New-CapabilityIndex.ps1'"
        return
    }

    try {
        & $CapIndexScript
        Log-Output "CAPABILITY INDEX WRITTEN: docs/generated/capability_index.md"
    } catch {
        Log-Output "ERROR (full):"
        Log-Output ($_ | Out-String)
    }
}

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

function Run-GenerateAll {
    Log-Output ">>> EXEC: GENERATE ALL (Scan -> GateDelta -> CapIndex -> Tests -> Status -> Context)"

    if ($global:GenAllJob -and $global:GenAllJob.State -eq "Running") {
        Log-Output "Generate All already running."
        return
    }

    $script:GenAllSeen = 0
    $btnGenAll.Enabled = $false

    $global:GenAllJob = Start-Job -ArgumentList $ProjectRoot, $ScanScript, $GateDeltaScript, $CapIndexScript, $StatusScript, $ContextScript -ScriptBlock {
        param($root, $scan, $gateDelta, $capIndex, $status, $context)

        try {
            Set-Location $root

            Write-Output "1) Connectivity scan"
            & $scan -Force -Harden | Out-Null
            Write-Output "Connectivity: done"

            Write-Output "2) Gate closure delta"
            & $gateDelta | Out-Null
            Write-Output "Gate closure delta: done"

            Write-Output "3) Capability index"
            & $capIndex  | Out-Null
            Write-Output "Capability index: done"

            Write-Output "4) Tests"
            $out = (dotnet test SimCore.Tests -v minimal 2>&1) -join "`r`n"
            $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
            [System.IO.File]::WriteAllText((Join-Path $root "docs\generated\05_TEST_SUMMARY.txt"), $out + "`r`n", $utf8NoBom)
            if ($LASTEXITCODE -ne 0) { throw "Tests failed. ExitCode=$LASTEXITCODE" }
            Write-Output "Tests: passed"

            Write-Output "5) Status packet"
            & $status -Force | Out-Null
            Write-Output "Status: done"

            Write-Output "6) Context packet"
            & $context | Out-Null
            Write-Output "Context: done"

            return 0
        } catch {
            Write-Output ("ERROR: " + ($_ | Out-String))
            return 1
        }
    }

    $global:GenAllTimer.Stop()
    $global:GenAllTimer.Start()
}

# --- UI COMPONENTS ---

# Header
$lblHeader = New-Object System.Windows.Forms.Label
$lblHeader.Text = "COMMAND DECK"
$lblHeader.Location = New-Object System.Drawing.Point(20, 10)
$lblHeader.Font = $fontHeader
$lblHeader.AutoSize = $true
$form.Controls.Add($lblHeader)

# 0. GENERATE ALL (Gray)
$btnGenAll = New-Object System.Windows.Forms.Button
$btnGenAll.Text = "0. GENERATE ALL"
$btnGenAll.Location = New-Object System.Drawing.Point(20, 40)
$btnGenAll.Size = New-Object System.Drawing.Size(400, 45)
$btnGenAll.Font = $fontNormal
$btnGenAll.BackColor = "#888888"
$btnGenAll.ForeColor = "White"
$btnGenAll.FlatStyle = "Flat"
$btnGenAll.Add_Click({ Run-GenerateAll })
$form.Controls.Add($btnGenAll)

# 1. TEST (Blue)
$btnTest = New-Object System.Windows.Forms.Button
$btnTest.Text = "1. VERIFY LOGIC (Tests)"
$btnTest.Location = New-Object System.Drawing.Point(20, 95)
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
$btnContext.Location = New-Object System.Drawing.Point(20, 150)
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
$btnScan.Location = New-Object System.Drawing.Point(20, 205)
$btnScan.Size = New-Object System.Drawing.Size(400, 45)
$btnScan.Font = $fontNormal
$btnScan.BackColor = "#d46a00"
$btnScan.ForeColor = "White"
$btnScan.FlatStyle = "Flat"
$btnScan.Add_Click({ Run-ConnectivityScan })
$form.Controls.Add($btnScan)

# 4. SAVE (Green)
$btnSave = New-Object System.Windows.Forms.Button
$btnSave.Location = New-Object System.Drawing.Point(20, 260)
$btnSave.Size = New-Object System.Drawing.Size(400, 45)
$btnSave.Text = "4. GIT SNAPSHOT"
$btnSave.BackColor = "#228822"
$btnSave.ForeColor = "White"
$btnSave.Font = $fontNormal
$btnSave.FlatStyle = "Flat"
$btnSave.Add_Click({ Run-GitSave })
$form.Controls.Add($btnSave)

# 5. GATE CLOSURE DELTA (Gray)
$btnGateDelta = New-Object System.Windows.Forms.Button
$btnGateDelta.Location = New-Object System.Drawing.Point(20, 315)
$btnGateDelta.Size = New-Object System.Drawing.Size(400, 45)
$btnGateDelta.Text = "5. GATE CLOSURE DELTA"
$btnGateDelta.BackColor = "#444444"
$btnGateDelta.ForeColor = "White"
$btnGateDelta.Font = $fontNormal
$btnGateDelta.FlatStyle = "Flat"
$btnGateDelta.Add_Click({ Run-GateClosureDelta })
$form.Controls.Add($btnGateDelta)

# 6. CAPABILITY INDEX (Gray)
$btnCapIndex = New-Object System.Windows.Forms.Button
$btnCapIndex.Location = New-Object System.Drawing.Point(20, 360)
$btnCapIndex.Size = New-Object System.Drawing.Size(400, 45)
$btnCapIndex.Text = "6. CAPABILITY INDEX"
$btnCapIndex.BackColor = "#444444"
$btnCapIndex.ForeColor = "White"
$btnCapIndex.Font = $fontNormal
$btnCapIndex.FlatStyle = "Flat"
$btnCapIndex.Add_Click({ Run-CapabilityIndex })
$form.Controls.Add($btnCapIndex)

# 7. STATUS PACKET (Gray)
$btnStatus = New-Object System.Windows.Forms.Button
$btnStatus.Location = New-Object System.Drawing.Point(20, 405)
$btnStatus.Size = New-Object System.Drawing.Size(400, 45)
$btnStatus.Text = "7. GENERATE STATUS PACKET"
$btnStatus.BackColor = "#444444"
$btnStatus.ForeColor = "White"
$btnStatus.Font = $fontNormal
$btnStatus.FlatStyle = "Flat"
$btnStatus.Add_Click({ Run-StatusPacket })
$form.Controls.Add($btnStatus)

# Console Output
$txtOutput = New-Object System.Windows.Forms.TextBox
$txtOutput.Location = New-Object System.Drawing.Point(20, 465)
$txtOutput.Size = New-Object System.Drawing.Size(400, 140)
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
