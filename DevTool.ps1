[CmdletBinding()]
param(
    [ValidateSet("ui","scan","next","prompt")]
    [string] $Mode = "ui"
)

if (-not (Test-Path variable:global:DEVTOOL_HEADLESS)) { $global:DEVTOOL_HEADLESS = $false }
if ($Mode -ne "ui") { $global:DEVTOOL_HEADLESS = $true }

<#
.SYNOPSIS
    SPACE TRADE EMPIRE - MISSION CONTROL (v5.3)

    Status:
    1. Verify Logic (dotnet test)
    2. Context Packet (LLM)
    3. Connectivity Scan (Architecture)
    4. Next Gate Packet (Registry)
    5. LLM Prompt (Attachments)
    6. Git Snapshot
#>

# --- CONFIGURATION ---
$ProjectRoot = (& git rev-parse --show-toplevel 2>$null)
if (-not $ProjectRoot) { throw "Not in a git repo (git rev-parse failed)." }
$ProjectRoot = $ProjectRoot.Trim()
Set-Location $ProjectRoot

$ScriptsDir      = Join-Path $ProjectRoot "scripts\tools"
$ContextScript   = Join-Path $ScriptsDir "New-ContextPacket.ps1"
$ScanScript      = Join-Path $ScriptsDir "Scan-Connectivity.ps1"
$ValidateGates   = Join-Path $ScriptsDir "Validate-Gates.ps1"
$NextGateScript  = Join-Path $ScriptsDir "New-NextGatePacket.ps1"
$PromptScript    = Join-Path $ScriptsDir "New-LlmPrompt.ps1"

$StatusScript    = Join-Path $ScriptsDir "New-StatusPacket.ps1"
$GateDeltaScript = Join-Path $ScriptsDir "New-GateClosureDelta.ps1"
$CapIndexScript  = Join-Path $ScriptsDir "New-CapabilityIndex.ps1"

function Ensure-Dir([string] $Dir) {
    if (-not (Test-Path -LiteralPath $Dir)) { New-Item -ItemType Directory -Force -Path $Dir | Out-Null }
}

function Write-AtomicUtf8NoBom([string] $Path, [string] $Content) {
    $dir = Split-Path -Parent $Path
    Ensure-Dir $dir
    $tmp = $Path + ".tmp"
    $enc = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($tmp, $Content, $enc)
    Move-Item -Force -LiteralPath $tmp -Destination $Path
}

# --- VALIDATION SURFACE (artifact-visible, deterministic) ---
$script:GatesValidation = "UNKNOWN"       # OK|FAIL|UNKNOWN
$script:GatesValidationError = ""         # short reason, normalized
$script:FreezeChecked = $false            # true once Validate-Gates is invoked
$script:BaselineRefUsed = "UNKNOWN"       # e.g. HEAD or a commit hash

function Invoke-ValidateGatesAndCapture {
    $script:FreezeChecked = $true
    $script:BaselineRefUsed = ((& git rev-parse HEAD).Trim())

    try {
        & $ValidateGates | Out-Null
        $script:GatesValidation = "OK"
        $script:GatesValidationError = ""
    } catch {
        $script:GatesValidation = "FAIL"
        $msg = $_.Exception.Message
        if ($null -eq $msg) { $msg = "Validate-Gates failed" }
        # normalize to first line only (more stable)
        $msg = ($msg -split "(`r`n|`n|`r)")[0]
        $script:GatesValidationError = $msg
        throw
    }
}

function Write-DevtoolSummary([string] $Command, [string[]] $Outputs) {
    $root = (& git rev-parse --show-toplevel 2>$null)
    if (-not $root) { throw "Write-DevtoolSummary: Not in a git repo (git rev-parse failed)." }
    $root = $root.Trim()

    $genDir = Join-Path $root "docs\generated"
    Ensure-Dir $genDir

    $obj = [ordered]@{
        schema_version         = 1
        command                = $Command
        outputs                = $Outputs
        gates_validation        = $script:GatesValidation
        gates_validation_error  = $script:GatesValidationError
        baseline_ref_used       = $script:BaselineRefUsed
        freeze_checked          = [bool]$script:FreezeChecked
    } | ConvertTo-Json -Depth 10

    Write-AtomicUtf8NoBom (Join-Path $genDir "devtool_summary.json") ($obj + [Environment]::NewLine)
}

# --- HEADLESS DISPATCH (must run before any WinForms Add-Type / UI setup) ---
if ($global:DEVTOOL_HEADLESS) {

    if (-not (Test-Path -LiteralPath $ScanScript))      { throw "Missing Scan script: $ScanScript" }
    if (-not (Test-Path -LiteralPath $ContextScript))   { throw "Missing Context script: $ContextScript" }
    if (-not (Test-Path -LiteralPath $ValidateGates))   { throw "Missing Gates validator: $ValidateGates" }
    if (-not (Test-Path -LiteralPath $NextGateScript))  { throw "Missing Next gate script: $NextGateScript" }
    if (-not (Test-Path -LiteralPath $PromptScript))    { throw "Missing Prompt script: $PromptScript" }

    switch ($Mode) {

        "scan" {
            & $ScanScript -Force -Harden
            Write-DevtoolSummary "scan" @(
                "docs/generated/devtool_summary.json",
                "docs/generated/connectivity_manifest.json",
                "docs/generated/connectivity_graph.json",
                "docs/generated/connectivity_violations.json"
            )
            exit 0
        }

        "next" {
            Invoke-ValidateGatesAndCapture
            & $NextGateScript | Out-Null
            # DevTool is the sole writer of docs/generated/devtool_summary.json
            Write-DevtoolSummary "next" @(
                "docs/generated/devtool_summary.json",
                "docs/generated/next_gate_packet.md",
                "docs/generated/llm_attachments.txt"
            )
            exit 0
        }

        "prompt" {
            Invoke-ValidateGatesAndCapture
            & $NextGateScript | Out-Null
            & $PromptScript | Out-Null
            Write-DevtoolSummary "prompt" @(
                "docs/generated/devtool_summary.json",
                "docs/generated/next_gate_packet.md",
                "docs/generated/llm_prompt.md",
                "docs/generated/llm_attachments.txt"
            )
            exit 0
        }

        default {
            exit 0
        }
    }
}

if (-not $global:DEVTOOL_HEADLESS) { Add-Type -AssemblyName System.Windows.Forms }
if (-not $global:DEVTOOL_HEADLESS) { Add-Type -AssemblyName System.Drawing }

# --- LOGGING (GUI only) ---
function Log-Output($message) {
    if ($global:DEVTOOL_HEADLESS) {
        Write-Host $message
        return
    }
    $line = "[$((Get-Date).ToString('HH:mm:ss'))] $message"
    $txtOutput.AppendText($line + "`r`n")
    $txtOutput.ScrollToCaret()
}

# 1. LOGIC VERIFICATION (GUI uses background job as before)
$global:VerifyJob = $null
$global:VerifyTimer = $null
$script:VerifyLogPath = $null

if (-not $global:DEVTOOL_HEADLESS) {
    $global:VerifyTimer = New-Object System.Windows.Forms.Timer
    $global:VerifyTimer.Interval = 250
}

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

if (-not $global:DEVTOOL_HEADLESS) {
    $global:VerifyTimer.Add_Tick({ On-VerifyTick })
}

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

        $btnTest.Enabled = $false

        $global:VerifyJob = Start-Job -ArgumentList $repoRoot, $logPath -ScriptBlock {
            param($root, $lp)
            Set-Location $root
            $out = (dotnet test SimCore.Tests -v minimal 2>&1) -join "`r`n"
            $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
            [System.IO.File]::WriteAllText($lp, $out + "`r`n", $utf8NoBom)
            return $LASTEXITCODE
        }

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
        Write-DevtoolSummary "context" @("docs/generated/devtool_summary.json","docs/generated/01_CONTEXT_PACKET.md")
        Log-Output "CONTEXT PACKET UPDATED."
    } catch { Log-Output ("ERROR: " + ($_.Exception.Message)) }
}
if (-not (Test-Path -LiteralPath "docs/generated/01_CONTEXT_PACKET.md")) {
    Log-Output "Generate All: abort (Context packet missing)."
    return $false
}

# 3. CONNECTIVITY SCAN
function Run-ConnectivityScan {
    Log-Output ">>> EXEC: ARCHITECTURE SCAN"
    if (-not (Test-Path $ScanScript)) {
        Log-Output "ERROR: Script missing at $ScanScript"
        return
    }

    try {
        & $ScanScript -Force -Harden
        Write-DevtoolSummary "scan" @("docs/generated/devtool_summary.json","docs/generated/connectivity_manifest.json","docs/generated/connectivity_graph.json","docs/generated/connectivity_violations.json")
        Log-Output "CONNECTIVITY SCAN COMPLETE."
    } catch { Log-Output ("ERROR: " + ($_.Exception.Message)) }
}

$scanReq = @(
  "docs/generated/connectivity_manifest.json",
  "docs/generated/connectivity_graph.json",
  "docs/generated/connectivity_violations.json"
)
foreach ($p in $scanReq) {
  if (-not (Test-Path -LiteralPath $p)) {
    Log-Output "Generate All: abort (Connectivity output missing: $p)."
    return $false
  }
}

function Run-GenerateAll {
    param(
        [switch] $IncludeTests
    )

    Log-Output ">>> EXEC: GENERATE ALL (Prep for LLM)"

    # Tests optional and default off (fast GUI flow)
    if ($IncludeTests) {
        Log-Output "Generate All: running tests (async). Not waiting for completion in MVP."
        Run-SimCoreTests
    }

    # Non negotiable ordering
    Run-ContextGen

    Run-ConnectivityScan

    $ok = Run-NextGatePacket
    if (-not $ok) { Log-Output "Generate All: abort (NextGatePacket failed)."; return $false }

    $ok = Run-LlmPrompt -SkipNextGate
    if (-not $ok) { Log-Output "Generate All: abort (LlmPrompt failed)."; return $false }

    # Clipboard only after successful prompt generation
    $ok = Copy-LLMPromptToClipboard
    if (-not $ok) { Log-Output "Generate All: prompt generated, but clipboard copy failed."; return $false }

    # Summary must exist for MVP success criteria
    Write-DevtoolSummary "generate_all" @(
        "docs/generated/devtool_summary.json",
        "docs/generated/01_CONTEXT_PACKET.md",
        "docs/generated/connectivity_manifest.json",
        "docs/generated/connectivity_graph.json",
        "docs/generated/connectivity_violations.json",
        "docs/generated/next_gate_packet.md",
        "docs/generated/llm_prompt.md",
        "docs/generated/llm_attachments.txt"
    )


    Log-Output "GENERATE ALL: OK."
    return $true
}

# 4. NEXT GATE PACKET
function Run-NextGatePacket {
    Log-Output ">>> EXEC: NEXT GATE PACKET"

    if (-not (Test-Path -LiteralPath $ValidateGates)) { Log-Output "ERROR: Missing $ValidateGates"; return $false }
    if (-not (Test-Path -LiteralPath $NextGateScript)) { Log-Output "ERROR: Missing $NextGateScript"; return $false }

    try {
        Invoke-ValidateGatesAndCapture
        & $NextGateScript | Out-Null
        Log-Output "NEXT GATE PACKET WRITTEN: docs/generated/next_gate_packet.md"
        return $true
    } catch {
        Log-Output "ERROR (full):"
        Log-Output ($_ | Out-String)
        return $false
    }
}

# 5. LLM PROMPT

function Copy-LLMPromptToClipboard {
    if ($global:DEVTOOL_HEADLESS) { return $false }

    $promptPath = Join-Path $ProjectRoot "docs\generated\llm_prompt.md"
    if (-not (Test-Path -LiteralPath $promptPath)) {
        Log-Output "ERROR: Missing prompt file at $promptPath"
        return $false
    }

    try {
        $text = Get-Content -LiteralPath $promptPath -Raw -Encoding UTF8
        [System.Windows.Forms.Clipboard]::SetText($text)
        Log-Output "COPIED: LLM prompt to clipboard."
        return $true
    } catch {
        Log-Output "ERROR (clipboard):"
        Log-Output ($_ | Out-String)
        return $false
    }
}

function Run-LlmPrompt {
    param(
        [switch] $SkipNextGate
    )

    Log-Output ">>> EXEC: LLM PROMPT"

    if (-not (Test-Path -LiteralPath $ValidateGates)) { Log-Output "ERROR: Missing $ValidateGates"; return $false }
    if (-not (Test-Path -LiteralPath $PromptScript))  { Log-Output "ERROR: Missing $PromptScript";  return $false }

    # Fix dependency at the source: prompt generation is never allowed to run without a fresh next gate packet.
    if (-not $SkipNextGate) {
        $ok = Run-NextGatePacket
        if (-not $ok) { return $false }
    }

    try {
        Invoke-ValidateGatesAndCapture
        & $PromptScript | Out-Null
        Log-Output "LLM PROMPT WRITTEN: docs/generated/llm_prompt.md"
        return $true
    } catch {
        Log-Output "ERROR (full):"
        Log-Output ($_ | Out-String)
        return $false
    }
}

# 6. SNAPSHOT
function Run-GitSave {
    Log-Output ">>> EXEC: GIT SNAPSHOT"
    git add .
    $res = git commit -m "wip: snapshot via Mission Control $(Get-Date -Format 'HH:mm')" 2>&1
    if ($LASTEXITCODE -eq 0) { Log-Output "SNAPSHOT SECURED." }
    else { Log-Output $res }
}

# --- GUI SETUP ---
$form = New-Object System.Windows.Forms.Form
$form.Text = "STE :: Mission Control v5.3"
$form.Size = New-Object System.Drawing.Size(450, 800)
$form.StartPosition = "CenterScreen"
$form.BackColor = "#1e1e1e"
$form.ForeColor = "#ffffff"

$fontHeader = New-Object System.Drawing.Font("Consolas", 12, [System.Drawing.FontStyle]::Bold)
$fontNormal = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$fontLog    = New-Object System.Drawing.Font("Consolas", 9)

# Header
$lblHeader = New-Object System.Windows.Forms.Label
$lblHeader.Text = "COMMAND DECK"
$lblHeader.Location = New-Object System.Drawing.Point(20, 10)
$lblHeader.Font = $fontHeader
$lblHeader.AutoSize = $true
$form.Controls.Add($lblHeader)

# Include tests checkbox (default OFF)
$chkIncludeTests = New-Object System.Windows.Forms.CheckBox
$chkIncludeTests.Text = "Include tests (optional)"
$chkIncludeTests.Location = New-Object System.Drawing.Point(20, 32)
$chkIncludeTests.AutoSize = $true
$chkIncludeTests.Checked = $false
$chkIncludeTests.ForeColor = "#ffffff"
$form.Controls.Add($chkIncludeTests)

# 0. GENERATE ALL
$btnGenAll = New-Object System.Windows.Forms.Button
$btnGenAll.Text = "0. GENERATE ALL (Prep for LLM)"
$btnGenAll.Location = New-Object System.Drawing.Point(20, 40)
$btnGenAll.Size = New-Object System.Drawing.Size(400, 45)
$btnGenAll.Font = $fontNormal
$btnGenAll.BackColor = "#444444"
$btnGenAll.ForeColor = "White"
$btnGenAll.FlatStyle = "Flat"
$btnGenAll.Add_Click({
    $btnGenAll.Enabled = $false
    try {
        $ok = Run-GenerateAll -IncludeTests:($chkIncludeTests.Checked)
        if (-not $ok) { Log-Output "Generate All: FAILED." }
    } finally {
        $btnGenAll.Enabled = $true
    }
})
$form.Controls.Add($btnGenAll)

# 1. TEST
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

# 2. CONTEXT
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

# 3. CONNECTIVITY
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

# 4. NEXT GATE
$btnNext = New-Object System.Windows.Forms.Button
$btnNext.Text = "4. NEXT GATE PACKET"
$btnNext.Location = New-Object System.Drawing.Point(20, 260)
$btnNext.Size = New-Object System.Drawing.Size(400, 45)
$btnNext.Font = $fontNormal
$btnNext.BackColor = "#444444"
$btnNext.ForeColor = "White"
$btnNext.FlatStyle = "Flat"
$btnNext.Add_Click({ Run-NextGatePacket })
$form.Controls.Add($btnNext)

# 5. LLM PROMPT
$btnPrompt = New-Object System.Windows.Forms.Button
$btnPrompt.Text = "5. GENERATE LLM PROMPT"
$btnPrompt.Location = New-Object System.Drawing.Point(20, 315)
$btnPrompt.Size = New-Object System.Drawing.Size(400, 45)
$btnPrompt.Font = $fontNormal
$btnPrompt.BackColor = "#444444"
$btnPrompt.ForeColor = "White"
$btnPrompt.FlatStyle = "Flat"
$btnPrompt.Add_Click({ Run-LlmPrompt })
$form.Controls.Add($btnPrompt)

# 5b. COPY PROMPT
$btnCopyPrompt = New-Object System.Windows.Forms.Button
$btnCopyPrompt.Text = "COPY LLM PROMPT TO CLIPBOARD"
$btnCopyPrompt.Location = New-Object System.Drawing.Point(20, 370)
$btnCopyPrompt.Size = New-Object System.Drawing.Size(400, 45)
$btnCopyPrompt.Font = $fontNormal
$btnCopyPrompt.BackColor = "#444444"
$btnCopyPrompt.ForeColor = "White"
$btnCopyPrompt.FlatStyle = "Flat"
$btnCopyPrompt.Add_Click({ Copy-LLMPromptToClipboard | Out-Null })
$form.Controls.Add($btnCopyPrompt)

# 6. SAVE
$btnSave = New-Object System.Windows.Forms.Button
$btnSave.Location = New-Object System.Drawing.Point(20, 420)
$btnSave.Size = New-Object System.Drawing.Size(400, 45)
$btnSave.Text = "6. GIT SNAPSHOT"
$btnSave.BackColor = "#228822"
$btnSave.ForeColor = "White"
$btnSave.Font = $fontNormal
$btnSave.FlatStyle = "Flat"
$btnSave.Add_Click({ Run-GitSave })
$form.Controls.Add($btnSave)

# Console Output
$txtOutput = New-Object System.Windows.Forms.TextBox
$txtOutput.Location = New-Object System.Drawing.Point(20, 475)
$txtOutput.Size = New-Object System.Drawing.Size(400, 260)
$txtOutput.Multiline = $true
$txtOutput.ScrollBars = "Vertical"
$txtOutput.BackColor = "#000000"
$txtOutput.ForeColor = "#00ff00"
$txtOutput.Font = $fontLog
$txtOutput.Text = "Mission Control v5.3 Online...`r`n"
$form.Controls.Add($txtOutput)

# --- LAUNCH ---
$form.Add_Shown({ $form.Activate() })
[void] $form.ShowDialog()
