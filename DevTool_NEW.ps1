[CmdletBinding()]
param(
    [ValidateSet("ui","scan","phase2","next","prompt")]
    [string] $Mode = "ui"
)

if (-not (Test-Path variable:global:DEVTOOL_HEADLESS)) { $global:DEVTOOL_HEADLESS = $false }
if ($Mode -ne "ui") { $global:DEVTOOL_HEADLESS = $true }

<#
.SYNOPSIS
    SPACE TRADE EMPIRE - MISSION CONTROL (v6.0)

    Queue v2.2 pipeline orchestration

    Authoritative registry:
    - docs/gates/gates.json

    Phase map:
    0) Verify Logic (dotnet test) -> docs/generated/05_TEST_SUMMARY.txt
    1) Repo truth refresh:
       - scripts/tools/New-ContextPacket.ps1 -> docs/generated/01_CONTEXT_PACKET.md
       - scripts/tools/Scan-Connectivity.ps1 -Force -Harden -> docs/generated/connectivity_*.json
    2) Gate queue build (Stage 2):
       - scripts/tools/Scan-GatesFromLedgerIr.ps1 -Mode FULL -> docs/generated/gates_queue_full.json + docs/generated/gates_scan_preflight.md
    2b) Apply queue to registry:
       - Copy docs/generated/gates_queue_full.json to docs/gates/gates.json
    3) Work session packet + prompt:
       - scripts/tools/Validate-Gates.ps1
       - scripts/tools/New-NextGatePacket.ps1 -> docs/generated/next_gate_packet.md + docs/generated/llm_attachments.txt
       - scripts/tools/New-LlmPrompt.ps1 -> docs/generated/llm_prompt.md
    4) Closeout (WIP):
       - scripts/tools/New-GateClosureDelta.ps1 -> writes to docs/generated (no registry mutation)
#>

# --- CONFIGURATION ---
$ProjectRoot = (& git rev-parse --show-toplevel 2>$null)
if (-not $ProjectRoot) { throw "Not in a git repo (git rev-parse failed)." }
$ProjectRoot = $ProjectRoot.Trim()
Set-Location $ProjectRoot

$ScriptsDir         = Join-Path $ProjectRoot "scripts\tools"
$ContextScript      = Join-Path $ScriptsDir "New-ContextPacket.ps1"
$ScanScript         = Join-Path $ScriptsDir "Scan-Connectivity.ps1"
$ValidateGates      = Join-Path $ScriptsDir "Validate-Gates.ps1"
$ValidateGodotScript = Join-Path $ScriptsDir "Validate-GodotScript.ps1"
$NextGateScript     = Join-Path $ScriptsDir "New-NextGatePacket.ps1"
$PromptScript       = Join-Path $ScriptsDir "New-LlmPrompt.ps1"
$GateDeltaScript    = Join-Path $ScriptsDir "New-GateClosureDelta.ps1"
$Stage2QueueScript  = Join-Path $ScriptsDir "Scan-GatesFromLedgerIr.ps1"

# Canonical artifacts (paths are part of determinism)
$ContextPacketPath  = Join-Path $ProjectRoot "docs\generated\01_CONTEXT_PACKET.md"
$LedgerIrPath       = Join-Path $ProjectRoot "docs\generated\gate_ledger_ir.json"
$SchemaPath         = Join-Path $ProjectRoot "docs\gates\gates.schema.json"

$QueueAppendPath    = Join-Path $ProjectRoot "docs\generated\gates_queue_append.json"
$QueueFullPath      = Join-Path $ProjectRoot "docs\generated\gates_queue_full.json"
$QueueReportPath    = Join-Path $ProjectRoot "docs\generated\gates_scan_preflight.md"
$RegistryPath       = Join-Path $ProjectRoot "docs\gates\gates.json"

$NextGatePacketPath = Join-Path $ProjectRoot "docs\generated\next_gate_packet.md"
$LlmPromptPath      = Join-Path $ProjectRoot "docs\generated\llm_prompt.md"
$LedgerIrPromptPath      = "docs/generated/ledger_ir_prompt.md"
$LedgerIrAttachmentsPath = "docs/generated/ledger_ir_attachments.txt"

$LlmAttachPath      = Join-Path $ProjectRoot "docs\generated\llm_attachments.txt"

# Defaults (Freeze rules typically want 10..25)
$DefaultQueueCap    = 25
$DefaultEnableRepairs = $false

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

function Get-FileTimeUtcOrMin([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return [DateTime]::MinValue }
    return (Get-Item -LiteralPath $Path).LastWriteTimeUtc
}

function Should-ApplyQueue {
    if (-not (Test-Path -LiteralPath $QueueFullPath)) { return $false }
    if (-not (Test-Path -LiteralPath $RegistryPath)) { return $true }
    return (Get-FileTimeUtcOrMin $QueueFullPath) -gt (Get-FileTimeUtcOrMin $RegistryPath)
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
        gates_validation       = $script:GatesValidation
        gates_validation_error = $script:GatesValidationError
        baseline_ref_used      = $script:BaselineRefUsed
        freeze_checked         = [bool]$script:FreezeChecked
    } | ConvertTo-Json -Depth 10

    Write-AtomicUtf8NoBom (Join-Path $genDir "devtool_summary.json") ($obj + [Environment]::NewLine)
}

# --- HEADLESS DISPATCH (must run before any WinForms Add-Type / UI setup) ---
if ($global:DEVTOOL_HEADLESS) {

    if (-not (Test-Path -LiteralPath $ScanScript))        { throw "Missing Scan script: $ScanScript" }
    if (-not (Test-Path -LiteralPath $ContextScript))     { throw "Missing Context script: $ContextScript" }
    if (-not (Test-Path -LiteralPath $ValidateGates))     { throw "Missing Gates validator: $ValidateGates" }
    if (-not (Test-Path -LiteralPath $NextGateScript))    { throw "Missing Next gate script: $NextGateScript" }
    if (-not (Test-Path -LiteralPath $PromptScript))      { throw "Missing Prompt script: $PromptScript" }
    if (-not (Test-Path -LiteralPath $Stage2QueueScript)) { throw "Missing Stage 2 queue script: $Stage2QueueScript" }

    function Invoke-ApplyQueueHeadless {
        if (-not (Test-Path -LiteralPath $QueueFullPath)) { throw "Missing generated queue: $QueueFullPath" }
        Copy-Item -LiteralPath $QueueFullPath -Destination $RegistryPath -Force
    }

    function Invoke-BuildQueueHeadless {
        if (-not (Test-Path -LiteralPath $LedgerIrPath)) { throw "Missing Stage 1 IR: $LedgerIrPath" }
        if (-not (Test-Path -LiteralPath $SchemaPath)) { throw "Missing schema: $SchemaPath" }
        if (-not (Test-Path -LiteralPath $ContextPacketPath)) { throw "Missing context packet: $ContextPacketPath" }

        $args = @(
            "-IrPath", $LedgerIrPath,
            "-SchemaPath", $SchemaPath,
            "-ContextPacketPath", $ContextPacketPath,
            "-OutAppendPath", $QueueAppendPath,
            "-OutFullPath", $QueueFullPath,
            "-OutReportPath", $QueueReportPath,
            "-Mode", "FULL",
            "-QueueCap", "$DefaultQueueCap"
        )
        if ($DefaultEnableRepairs) { $args += "-EnableRepairs" }

        $stage2Output = & powershell -ExecutionPolicy Bypass -File $Stage2QueueScript @args 2>&1
        $stage2Exit = $LASTEXITCODE

        if ($stage2Output) {
            foreach ($line in ($stage2Output | ForEach-Object { "$_" })) {
                Log-Output ("[Stage2] " + $line)
            }
        }
    }

    function Ensure-QueueAppliedIfNeededHeadless {
        if (Should-ApplyQueue) { Invoke-ApplyQueueHeadless }
    }

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

        "phase2" {
            & $ContextScript | Out-Null
            Invoke-BuildQueueHeadless
            Invoke-ApplyQueueHeadless
            Invoke-ValidateGatesAndCapture

            Write-DevtoolSummary "phase2" @(
                "docs/generated/devtool_summary.json",
                "docs/generated/01_CONTEXT_PACKET.md",
                "docs/generated/gates_queue_full.json",
                "docs/generated/gates_scan_preflight.md",
                "docs/gates/gates.json"
            )
            exit 0
        }

        "next" {
            Ensure-QueueAppliedIfNeededHeadless
            Invoke-ValidateGatesAndCapture
            & $NextGateScript | Out-Null

            Write-DevtoolSummary "next" @(
                "docs/generated/devtool_summary.json",
                "docs/generated/next_gate_packet.md",
                "docs/generated/llm_attachments.txt"
            )
            exit 0
        }

        "prompt" {
            Ensure-QueueAppliedIfNeededHeadless
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
if (-not $global:DEVTOOL_HEADLESS) { Add-Type -AssemblyName Microsoft.VisualBasic }


# --- LOGGING (GUI only) ---
function Log-Output($message) {
    if ($global:DEVTOOL_HEADLESS) {
        Write-Host $message
        return
    }

    $line = "[$((Get-Date).ToString('HH:mm:ss'))] $message"

    if ($null -eq $txtOutput) {
        Write-Host $line
        return
    }

    $txtOutput.AppendText($line + "`r`n")
    $txtOutput.ScrollToCaret()
}

# 0. LOGIC VERIFICATION (GUI uses background job)
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

# Phase 1. CONTEXT GENERATION
function Run-ContextGen {
    Log-Output ">>> EXEC: PHASE 1 CONTEXT PACKET"
    if (-not (Test-Path $ContextScript)) { Log-Output "ERROR: Missing $ContextScript"; return $false }

    try {
        & $ContextScript | Out-Null
        Write-DevtoolSummary "context" @("docs/generated/devtool_summary.json","docs/generated/01_CONTEXT_PACKET.md")
        Log-Output "CONTEXT PACKET UPDATED."
        return $true
    } catch {
        Log-Output ("ERROR: " + ($_.Exception.Message))
        return $false
    }
}

# Phase 1. CONNECTIVITY SCAN
function Run-ConnectivityScan {
    Log-Output ">>> EXEC: PHASE 1 CONNECTIVITY SCAN"
    if (-not (Test-Path $ScanScript)) {
        Log-Output "ERROR: Script missing at $ScanScript"
        return $false
    }

    try {
        & $ScanScript -Force -Harden
        Write-DevtoolSummary "scan" @(
            "docs/generated/devtool_summary.json",
            "docs/generated/connectivity_manifest.json",
            "docs/generated/connectivity_graph.json",
            "docs/generated/connectivity_violations.json"
        )
        Log-Output "CONNECTIVITY SCAN COMPLETE."
        return $true
    } catch {
        Log-Output ("ERROR: " + ($_.Exception.Message))
        return $false
    }
}

# Phase 2. BUILD QUEUE (Stage 2)
function Run-BuildQueue {
    param(
        [int] $QueueCap = $DefaultQueueCap,
        [switch] $EnableRepairs
    )

    Log-Output ">>> EXEC: PHASE 2 BUILD QUEUE (Stage 2)"

    if (-not (Test-Path -LiteralPath $Stage2QueueScript)) { Log-Output "ERROR: Missing $Stage2QueueScript"; return $false }
    if (-not (Test-Path -LiteralPath $LedgerIrPath)) {
    Log-Output "ERROR: Missing Stage 1 IR at $LedgerIrPath"
    if (Test-Path -LiteralPath $QueueFullPath) {
        Log-Output "NOTE: Existing queue found at $QueueFullPath. You can still run Phase 2b Apply + Validate to use it."
    } else {
        Log-Output "NOTE: No existing queue found. You must generate gate_ledger_ir.json before Phase 2 can run."
    }
    return $false
    }

    if (-not (Test-Path -LiteralPath $SchemaPath)) { Log-Output "ERROR: Missing schema at $SchemaPath"; return $false }
    if (-not (Test-Path -LiteralPath $ContextPacketPath)) { Log-Output "ERROR: Missing context packet at $ContextPacketPath"; return $false }

    try {
        $args = @(
            "-IrPath", $LedgerIrPath,
            "-SchemaPath", $SchemaPath,
            "-ContextPacketPath", $ContextPacketPath,
            "-OutAppendPath", $QueueAppendPath,
            "-OutFullPath", $QueueFullPath,
            "-OutReportPath", $QueueReportPath,
            "-Mode", "FULL",
            "-QueueCap", "$QueueCap"
        )
        if ($EnableRepairs) { $args += "-EnableRepairs" }

        $stage2Output = & powershell -ExecutionPolicy Bypass -File $Stage2QueueScript @args 2>&1
        $stage2Exit = $LASTEXITCODE

        if ($stage2Output) {
            foreach ($line in ($stage2Output | ForEach-Object { "$_" })) {
                Log-Output ("[Stage2] " + $line)
            }
        }

    if ($stage2Exit -ne 0) {
        Log-Output "ERROR: Phase 2 script exited with code $stage2Exit"
        if (Test-Path -LiteralPath $QueueFullPath) {
            try {
                $qInfo = Get-Item -LiteralPath $QueueFullPath -ErrorAction Stop
                Log-Output "DEBUG: queue file exists but size=$($qInfo.Length) bytes"
            } catch {}
        }
        return $false
    }

    if (-not (Test-Path -LiteralPath $QueueFullPath)) {
        Log-Output "ERROR: Phase 2 completed but queue file was not created: $QueueFullPath"
        return $false
    }

    $qInfo = Get-Item -LiteralPath $QueueFullPath -ErrorAction Stop
    if ($qInfo.Length -le 0) {
        Log-Output "ERROR: Phase 2 produced an empty queue file: $QueueFullPath"
        return $false
    }

    Log-Output "QUEUE BUILT: docs/generated/gates_queue_full.json ($($qInfo.Length) bytes)"
    Log-Output "PREFLIGHT: docs/generated/gates_scan_preflight.md"
    Log-Output "NOTE: docs/gates/gates.json is unchanged until Phase 2b Apply Queue To Registry runs."
    return $true
    } catch {
        Log-Output "ERROR (full):"
        Log-Output ($_ | Out-String)

        # Invalidate stale Phase 3 artifacts so we cannot copy an old prompt after a failure.
        $ngp = Join-Path $ProjectRoot "docs/generated/next_gate_packet.md"
        if (Test-Path -LiteralPath $ngp) {
            Remove-Item -Force -LiteralPath $ngp
            Log-Output "DELETED (stale): docs/generated/next_gate_packet.md"
        }
        if (Test-Path -LiteralPath $LlmPromptPath) {
            Remove-Item -Force -LiteralPath $LlmPromptPath
            Log-Output "DELETED (stale): docs/generated/llm_prompt.md"
        }

        return $false
    }
}

# Phase 2b. APPLY QUEUE TO REGISTRY
function Run-ApplyQueue {
    Log-Output ">>> EXEC: PHASE 2b APPLY QUEUE TO REGISTRY"

    if (-not (Test-Path -LiteralPath $QueueFullPath)) {
        Log-Output "ERROR: Missing generated queue at $QueueFullPath"
        return $false
    }

    try {
        $qInfo = Get-Item -LiteralPath $QueueFullPath -ErrorAction Stop
        if ($qInfo.Length -le 0) {
        throw "Queue file is empty: $QueueFullPath"
        }

        $qInfo = Get-Item -LiteralPath $QueueFullPath
        Log-Output ("QUEUE FILE bytes: " + $qInfo.Length)

        $content = Get-Content -LiteralPath $QueueFullPath -Raw -ErrorAction Stop
        if ($null -eq $content) {
        throw "Queue file read returned null: $QueueFullPath"
        }

        $contentNormalized = ([string]$content).TrimEnd("`r","`n") + [Environment]::NewLine
        Write-AtomicUtf8NoBom -Path $RegistryPath -Content $contentNormalized

        Log-Output "REGISTRY UPDATED: docs/gates/gates.json"
        return $true
    } catch {
        Log-Output "ERROR (full):"
        Log-Output ($_ | Out-String)

        return $false
    }
}

function Ensure-QueueAppliedIfNeededGui {
    if (Should-ApplyQueue) {
        Log-Output "AUTO-APPLY: generated queue is newer than registry. Applying now."
        $ok = Run-ApplyQueue
        if (-not $ok) { return $false }
    }
    return $true
}

# Validation helper (explicit button + used by pipelines)
function Run-ValidateRegistry {
    Log-Output ">>> EXEC: VALIDATE REGISTRY (Validate-Gates)"

    if (-not (Test-Path -LiteralPath $ValidateGates)) { Log-Output "ERROR: Missing $ValidateGates"; return $false }

    try {
        Invoke-ValidateGatesAndCapture
        Log-Output "GATES VALIDATION: OK"
        return $true
    } catch {
        Log-Output "GATES VALIDATION: FAIL"
        Log-Output ($_ | Out-String)
        return $false
    }
}

function Run-ValidateGodotScript {
    param(
        [string] $TargetScript = "",
        [switch] $FromAttachmentShortlist
    )

    Log-Output ">>> EXEC: VALIDATE GODOT SCRIPT"

    if (-not (Test-Path -LiteralPath $ValidateGodotScript)) {
        Log-Output "ERROR: Missing $ValidateGodotScript"
        return $false
    }

    # If no direct target provided and caller wants shortlist, extract .gd paths from llm_attachments.txt
    if ([string]::IsNullOrWhiteSpace($TargetScript) -and $FromAttachmentShortlist) {
        if (-not (Test-Path -LiteralPath $LlmAttachPath)) {
            Log-Output "ERROR: Missing $LlmAttachPath (run Phase 3 Next Gate Packet first)"
            return $false
        }

        $paths = @(
            (Get-Content -LiteralPath $LlmAttachPath -Encoding UTF8) |
            ForEach-Object { (($_ + "").Trim()) } |
            Where-Object { $_ -ne "" -and $_.ToLowerInvariant().EndsWith(".gd") }
        )

        if ($paths.Count -eq 0) {
            Log-Output "No .gd files found in attachment shortlist."
            return $true
        }

        $okAll = $true
        foreach ($p in $paths) {
            $full = Join-Path $ProjectRoot $p
            Log-Output "Validate-GodotScript: $p"
            try {
                & powershell -NoProfile -ExecutionPolicy Bypass -File $ValidateGodotScript -TargetScript $full | Out-Null
            } catch {
                $okAll = $false
                Log-Output "FAIL: $p"
                Log-Output (($_ | Out-String))
                break
            }
        }

        if ($okAll) { Log-Output "VALIDATE GODOT SCRIPT: OK (shortlist)" }
        else { Log-Output "VALIDATE GODOT SCRIPT: FAIL (shortlist)" }

        return $okAll
    }

    # Otherwise validate a single target script (prompt user if needed)
    if ([string]::IsNullOrWhiteSpace($TargetScript)) {
        if ($global:DEVTOOL_HEADLESS) {
            Log-Output "ERROR: TargetScript required in headless mode."
            return $false
        }

        try {
            $TargetScript = [Microsoft.VisualBasic.Interaction]::InputBox(
                "Enter repo-relative path to .gd script (example: scripts/core/sim/rng_streams.gd):",
                "Validate Godot Script",
                ""
            )
            $TargetScript = ($TargetScript + "").Trim()
        } catch {
            Log-Output "ERROR: input dialog failed."
            return $false
        }

        if ([string]::IsNullOrWhiteSpace($TargetScript)) {
            Log-Output "Validate Godot Script: cancelled."
            return $false
        }
    }

    $fullPath = $TargetScript
    if (-not [System.IO.Path]::IsPathRooted($fullPath)) {
        $fullPath = Join-Path $ProjectRoot $TargetScript
    }

    try {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $ValidateGodotScript -TargetScript $fullPath | Out-Null
        Log-Output "VALIDATE GODOT SCRIPT: OK"
        return $true
    } catch {
        Log-Output "VALIDATE GODOT SCRIPT: FAIL"
        Log-Output (($_ | Out-String))
        return $false
    }
}

# Phase 3. NEXT GATE PACKET
function Run-NextGatePacket {
    Log-Output ">>> EXEC: PHASE 3 NEXT GATE PACKET"

    if (-not (Test-Path -LiteralPath $NextGateScript)) { Log-Output "ERROR: Missing $NextGateScript"; return $false }

    $ok = Ensure-QueueAppliedIfNeededGui
    if (-not $ok) { return $false }

    $ok = Run-ValidateRegistry
    if (-not $ok) { return $false }

    try {
        & $NextGateScript | Out-Null
        Log-Output "NEXT GATE PACKET WRITTEN: docs/generated/next_gate_packet.md"
        return $true
    } catch {
        Log-Output "ERROR (full):"
        Log-Output ($_ | Out-String)
        return $false
    }
}

# Phase 3. LLM PROMPT

function Copy-LLMPromptToClipboard {
    if ($global:DEVTOOL_HEADLESS) { return $false }

    if (-not (Test-Path -LiteralPath $LlmPromptPath)) {
        Log-Output "ERROR: Missing prompt file at $LlmPromptPath"
        return $false
    }

    try {
        $text = Get-Content -LiteralPath $LlmPromptPath -Raw -Encoding UTF8
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

    Log-Output ">>> EXEC: PHASE 3 GENERATE LLM PROMPT"

    if (-not (Test-Path -LiteralPath $PromptScript))  { Log-Output "ERROR: Missing $PromptScript";  return $false }

    $ok = Ensure-QueueAppliedIfNeededGui
    if (-not $ok) { return $false }

    # Prompt generation is not allowed to run without a fresh next gate packet unless explicitly requested.
    if (-not $SkipNextGate) {
        $ok = Run-NextGatePacket
        if (-not $ok) { return $false }
    } else {
        $ok = Run-ValidateRegistry
        if (-not $ok) { return $false }
    }

    try {
        & $PromptScript | Out-Null
        Log-Output "LLM PROMPT WRITTEN: docs/generated/llm_prompt.md"

        # Always copy prompt (with instructions + attachments header) to clipboard
        $copied = Copy-LLMPromptToClipboard
        if (-not $copied) {
            Log-Output "WARNING: Prompt generated but clipboard copy failed."
        } else {
            Log-Output "EXPECTED ATTACHMENTS (ONLY):"
            if (Test-Path -LiteralPath $LlmAttachPath) {
                Get-Content -LiteralPath $LlmAttachPath -Encoding UTF8 | ForEach-Object {
                    $s = (($_ + "").Trim())
                    if ($s) { Log-Output ("- " + $s) }
                }
            } else {
                Log-Output "- ERROR: missing docs/generated/llm_attachments.txt (run Phase 3 Next Gate Packet first)"
            }
        }

        return $true

    } catch {
        Log-Output "ERROR (full):"
        Log-Output ($_ | Out-String)
        return $false
    }
}

function Run-ProofsFromNextGatePacket {
    Log-Output ">>> EXEC: RUN PROOFS (from next_gate_packet Definition of done)"

    if (-not (Test-Path -LiteralPath $NextGatePacketPath)) {
        Log-Output "ERROR: Missing next gate packet at $NextGatePacketPath. Run Phase 3 Next Gate Packet first."
        return $false
    }

    $txt = Get-Content -LiteralPath $NextGatePacketPath -Raw -Encoding UTF8
    if ([string]::IsNullOrWhiteSpace($txt)) {
        Log-Output "ERROR: next_gate_packet.md is empty."
        return $false
    }

    # Extract Selected task id
    $m = [regex]::Match($txt, '(?im)^\s*Selected task:\s*([A-Za-z0-9._-]+)\s*$')
    $taskId = ""
    if ($m.Success) { $taskId = ($m.Groups[1].Value + "").Trim() }
    if ([string]::IsNullOrWhiteSpace($taskId)) {
        Log-Output "ERROR: Could not find Selected task in next_gate_packet.md."
        return $false
    }

    # Focus only the Selected section (stop at "## Other active gates")
    $selOnly = $txt
    $stop = [regex]::Match($txt, '(?im)^\s*##\s+Other active gates\b')
    if ($stop.Success) {
        $selOnly = $txt.Substring(0, $stop.Index)
    }

    # Pull proof lines under "Definition of done:"
    $lines = @($selOnly -split "(`r`n|`n|`r)")
    $inDod = $false
    $proofs = New-Object System.Collections.Generic.List[string]
    foreach ($ln in $lines) {
        if ($ln -match '^\s*Definition of done\s*:\s*$') { $inDod = $true; continue }
        if (-not $inDod) { continue }

        # End section on a new header
        if ($ln -match '^\s*##\s+' -or $ln -match '^\s*Evidence\s*:\s*$' -or $ln -match '^\s*Attachment shortlist') { break }

        $pm = [regex]::Match($ln, '^\s*-\s*proof\s*:\s*(.+?)\s*$')
        if ($pm.Success) {
            $p = ($pm.Groups[1].Value + "").Trim()
            if (-not [string]::IsNullOrWhiteSpace($p)) { $proofs.Add($p) | Out-Null }
        }
    }

    if ($proofs.Count -eq 0) {
        Log-Output "ERROR: No '- proof:' lines found under Definition of done for Selected task $taskId."
        Log-Output "Fix: ensure next_gate_packet.md contains runnable proof commands."
        return $false
    }

    Log-Output "Selected task: $taskId"
    Log-Output "Proof items found: $($proofs.Count)"
    Log-Output "Proof plan (will run only items that look like commands):"

    $runnable = New-Object System.Collections.Generic.List[string]
    $manual = New-Object System.Collections.Generic.List[string]

    foreach ($p in $proofs) {
        $isCmd =
            $p.StartsWith("powershell ", [System.StringComparison]::OrdinalIgnoreCase) -or
            $p.StartsWith("pwsh ", [System.StringComparison]::OrdinalIgnoreCase) -or
            $p.StartsWith("git ", [System.StringComparison]::OrdinalIgnoreCase) -or
            $p.StartsWith(".\", [System.StringComparison]::OrdinalIgnoreCase) -or
            $p.StartsWith("scripts/", [System.StringComparison]::OrdinalIgnoreCase)

        if ($isCmd) { $runnable.Add($p) | Out-Null } else { $manual.Add($p) | Out-Null }
    }

    foreach ($c in $runnable) { Log-Output ("RUN: " + $c) }
    foreach ($c in $manual) { Log-Output ("MANUAL: " + $c) }

    if ($runnable.Count -eq 0) {
        Log-Output "ERROR: 0 runnable proof commands found. All proof items were manual text."
        return $false
    }

    foreach ($cmd in $runnable) {
        try {
            Log-Output ("EXEC: " + $cmd)

            # Run via cmd.exe so quotes behave consistently (PowerShell parsing of arbitrary strings is tricky)
            & cmd.exe /c $cmd
            if ($LASTEXITCODE -ne 0) {
                Log-Output ("FAIL: command exited with code " + $LASTEXITCODE)
                return $false
            }
        } catch {
            Log-Output "FAIL: exception while running proof command"
            Log-Output ($_ | Out-String)
            return $false
        }
    }

    Log-Output "PROOFS: OK."
    return $true
}

# Phase 4. CLOSEOUT (WIP, delta only)
function Run-CloseoutWip {
    Log-Output ">>> EXEC: PHASE 4 CLOSEOUT (WIP, delta only)"
    Log-Output "DISABLED: Closeout WIP (New-GateClosureDelta.ps1) is deprecated. Use Step D Helper (New-StepDCloseoutPatch.ps1) instead."
    return $false


    if (-not (Test-Path -LiteralPath $GateDeltaScript)) { Log-Output "ERROR: Missing $GateDeltaScript"; return $false }

    try {
        & $GateDeltaScript | Out-Null
        Write-DevtoolSummary "closeout_wip" @(
            "docs/generated/devtool_summary.json"
        )
        Log-Output "CLOSEOUT DELTA GENERATED (see docs/generated)."
        return $true
    } catch {
        Log-Output "ERROR (full):"
        Log-Output ($_ | Out-String)
        return $false
    }
}

function Run-LedgerIrPrompt {
    param(
        [int] $Cap = 8
    )

    Log-Output ">>> EXEC: PHASE 1b LEDGER IR PROMPT (LLM-driven)"

    if (-not (Test-Path -LiteralPath $ContextPacketPath)) {
        Log-Output "ERROR: Missing context packet at $ContextPacketPath. Run Phase 1 Context Packet first."
        return $false
    }

    $prompt = @"
You are in STAGE_1: Ledger Extraction Mode (capped, no inference).

Goal
Extract a capped list of queue-eligible work items from docs/55_GATES.md into a deterministic intermediate representation (IR) for a Stage 2 tool that will do deep repo scanning and compile schema v2.2 queue objects.
Queue-eligible means: gate_status is TODO or IN_PROGRESS.

Attachments (only)
1) docs/55_GATES.md
2) docs/gates/gates.schema.json
3) docs/generated/01_CONTEXT_PACKET.md

Cap and ordering
- cap = $Cap
- ORDER_V1: sort by gate_status rank (IN_PROGRESS before TODO), then gate_id lex ascending
- Extract all eligible gates first, compute totals, then apply the cap.

Hard rules
- Output must be exactly one JSON object inside a single ```json code fence.
- Output JSON only. No prose before or after the code fence.
- No inference:
  - Do NOT invent shards. Only extract shards if explicitly present in 55 as distinct sub-items.
  - Do NOT invent evidence paths. Only extract paths explicitly listed in 55.
  - Do NOT invent expected_touch_paths. Only include if explicitly present in 55; otherwise omit the field.
  - If evidence path are TBD, generate the JSON, and then ask the user if they would like you to switch to evidence gathering mode, in which you are authorized to request additional files if necessary to build the evidence files
- Normalize extracted paths:
  - use / separators
  - trim leading/trailing whitespace
  - reject any path containing ./ or .. segments
  - preserve exact casing
- HEAD must be copied verbatim from docs/generated/01_CONTEXT_PACKET.md line: head: <sha>

IR schema (exact keys only, no extras)
{
  "ir_version": "1.3",
  "head": "<sha copied verbatim>",
  "source_files": {
    "ledger": "docs/55_GATES.md",
    "schema": "docs/gates/gates.schema.json",
    "context_packet": "docs/generated/01_CONTEXT_PACKET.md"
  },
  "cap": $Cap,
  "ordering": "ORDER_V1",
  "eligible_total_found": <int>,
  "eligible_total_emitted": <int>,
  "eligible_gates": [
    {
      "gate_id": "...",
      "gate_title": "...",
      "gate_status": "TODO|IN_PROGRESS",
      "acceptance_text": "verbatim from 55 (may be empty)",
      "evidence_universe": ["repo/relative.ext", "..."],
      "shards": [
        {
          "shard_title": "verbatim from 55",
          "shard_status": "TODO|IN_PROGRESS",
          "evidence_subset": ["repo/relative.ext", "..."]
        }
      ],
      "notes": "verbatim from 55 (may be empty)"
    }
  ],
  "dropped_gate_ids_by_cap": ["..."],
  "extraction_warnings": ["..."],
  "self_check": {
    "head_present": true|false,
    "cap_applied_correctly": true|false,
    "eligible_counts_consistent": true|false
  }
}

Self-check computation rules (must be computed, not asserted)
- head_present is true iff head is a 40-hex sha.
- eligible_counts_consistent is true iff:
    eligible_total_emitted == length(eligible_gates)
    eligible_total_found == eligible_total_emitted + length(dropped_gate_ids_by_cap)
- cap_applied_correctly is true iff:
    eligible_total_emitted <= cap
    and if eligible_total_found > cap then eligible_total_emitted == cap else eligible_total_emitted == eligible_total_found

STOP rules
- If you cannot locate the HEAD line in the context packet, output inside the code fence:
  { "ir_version":"1.3", "fatal":"MISSING_HEAD_IN_CONTEXT_PACKET", "details":[...] }
- If you cannot find any TODO or IN_PROGRESS gates in 55, output inside the code fence:
  { "ir_version":"1.3", "fatal":"NO_QUEUE_ELIGIBLE_GATES_FOUND", "details":[...] }
"@

    try {
        Set-Clipboard -Value $prompt
        Log-Output "COPIED TO CLIPBOARD: Stage 1 ledger IR prompt"
        Log-Output "ATTACH (only): docs/55_GATES.md"
        Log-Output "ATTACH (only): docs/gates/gates.schema.json"
        Log-Output "ATTACH (only): docs/generated/01_CONTEXT_PACKET.md"
        return $true
    } catch {
        Log-Output "ERROR: Clipboard copy failed. $($_.Exception.Message)"
        return $false
    }
}


# GIT SNAPSHOT (unchanged)
function Run-GitStatus {
    Log-Output ">>> EXEC: GIT STATUS"
    try {
        $branch = (git rev-parse --abbrev-ref HEAD 2>$null)
        if ($LASTEXITCODE -ne 0) { $branch = "<unknown>" }
        $changes = @(git status --porcelain 2>$null)
        Log-Output "Branch: $branch"
        if ($changes.Count -eq 0) {
            Log-Output "Working tree: clean"
        } else {
            Log-Output ("Changes: " + $changes.Count)
            $changes | Select-Object -First 50 | ForEach-Object { Log-Output $_ }
            if ($changes.Count -gt 50) { Log-Output "NOTE: showing first 50 lines only" }
        }
        return $true
    } catch {
        Log-Output "ERROR (git status):"
        Log-Output ($_ | Out-String)
        return $false
    }
}

function Run-CloseoutCheck {
    Log-Output ">>> EXEC: CLOSEOUT CHECK (read-only)"

    try {
        $branch = (git rev-parse --abbrev-ref HEAD 2>$null)
        $branch = ($branch + "").Trim()
        if ([string]::IsNullOrWhiteSpace($branch)) { $branch = "<unknown>" }

        if ($branch -eq "HEAD") {
            Log-Output "FAIL: Detached HEAD. Fix: checkout a branch."
            return $false
        }

        $changes = @(git status --porcelain 2>$null)
        if ($changes.Count -ne 0) {
            Log-Output "FAIL: Working tree not clean."
            $changes | Select-Object -First 50 | ForEach-Object { Log-Output $_ }
            if ($changes.Count -gt 50) { Log-Output "NOTE: showing first 50 lines only" }
            return $false
        }

        # gates.json pending_completion must be null for a clean closeout state
        if (-not (Test-Path -LiteralPath $RegistryPath)) {
            Log-Output "FAIL: Missing registry at $RegistryPath"
            return $false
        }

        $qRaw = Get-Content -LiteralPath $RegistryPath -Raw -Encoding UTF8
        $q = ($qRaw | ConvertFrom-Json)

        $pcProp = $q.PSObject.Properties.Match("pending_completion") | Select-Object -First 1
        if ($pcProp -and $null -ne $pcProp.Value) {
            $tid = ""
            try { $tid = (($pcProp.Value.task_id + "").Trim()) } catch { $tid = "" }
            Log-Output "FAIL: gates.json pending_completion is set. Resolve Step D first."
            if ($tid) { Log-Output "pending_completion.task_id: $tid" }
            return $false
        }

        # Session log exists check
        if (-not (Test-Path -LiteralPath $SessionLogPath)) {
            Log-Output "FAIL: Missing session log at $SessionLogPath"
            return $false
        }

        Log-Output "CLOSEOUT CHECK: OK."
        return $true

    } catch {
        Log-Output "ERROR (Closeout Check):"
        Log-Output ($_ | Out-String)
        return $false
    }
}

function Get-CommitMessageFromUser([string] $Default = "") {
    if ($global:DEVTOOL_HEADLESS) { return $null }
    try {
        $msg = [Microsoft.VisualBasic.Interaction]::InputBox(
            "Enter commit message (Cancel or blank to abort):",
            "Commit Message",
            $Default
        )
        $msg = ($msg + "").Trim()
        if ([string]::IsNullOrWhiteSpace($msg)) { return $null }
        return $msg
    } catch {
        Log-Output "ERROR: Commit message prompt failed."
        Log-Output ($_ | Out-String)
        return $null
    }
}

function Run-GitCommitAndPush {
    Log-Output ">>> EXEC: GIT COMMIT + PUSH"

    $msg = Get-CommitMessageFromUser -Default ("wip: mission control " + (Get-Date -Format 'yyyy-MM-dd HH:mm'))
    if (-not $msg) {
        Log-Output "Commit+Push: aborted (no message)."
        return $false
    }

    try {
        $porcelainBefore = @(git status --porcelain 2>$null)
        if ($porcelainBefore.Count -eq 0) {
            Log-Output "Commit+Push: nothing to commit (working tree clean)."
            return $true
        }

        Log-Output "Staging: git add -A"
        $addOut = (git add -A 2>&1) -join "`r`n"
        if ($LASTEXITCODE -ne 0) {
            Log-Output "ERROR: git add failed"
            if ($addOut) { Log-Output $addOut }
            return $false
        }

        $porcelainAfter = @(git status --porcelain 2>$null)
        if ($porcelainAfter.Count -eq 0) {
            Log-Output "Commit+Push: nothing staged after add (unexpected)."
            return $true
        }

        Log-Output "Committing: $msg"
        $commitOut = (git commit -m $msg 2>&1) -join "`r`n"
        if ($LASTEXITCODE -ne 0) {
            Log-Output "ERROR: git commit failed"
            if ($commitOut) { Log-Output $commitOut }
            return $false
        }
        if ($commitOut) { Log-Output $commitOut }

        Log-Output "Pushing: git push"
        $pushOut = (git push 2>&1) -join "`r`n"
        if ($LASTEXITCODE -ne 0) {
            Log-Output "ERROR: git push failed"
            if ($pushOut) { Log-Output $pushOut }
            return $false
        }
        if ($pushOut) { Log-Output $pushOut }

        Log-Output "Commit+Push: OK."
        return $true
    } catch {
        Log-Output "ERROR (commit+push):"
        Log-Output ($_ | Out-String)
        return $false
    }
}

function Run-GenerateAll {
    param(
        [switch] $IncludeTests,
        [int] $QueueCap = $DefaultQueueCap,
        [switch] $IncludeConnectivity
    )

    Log-Output ">>> EXEC: GENERATE ALL (Prep for LLM, queue v2.2)"

    if ($IncludeTests) {
        Log-Output "Generate All: running tests (async). Not waiting for completion in MVP."
        Run-SimCoreTests
    }

    $ok = Run-ContextGen
    if (-not $ok) { Log-Output "Generate All: abort (Context failed)."; return $false }

    if ($IncludeConnectivity) {
        $ok = Run-ConnectivityScan
        if (-not $ok) { Log-Output "Generate All: abort (Connectivity failed)."; return $false }
    }

    $ok = Run-BuildQueue -QueueCap $QueueCap
    if (-not $ok) {
        if (Test-Path -LiteralPath $QueueFullPath) {
            Log-Output "Generate All: Build Queue failed, but existing queue_full.json is present. Proceeding with Apply + Validate using existing queue."
        } else {
            Log-Output "Generate All: abort (Build Queue failed and no existing queue_full.json present)."
            return $false
        }
    }

    $ok = Run-ApplyQueue
    if (-not $ok) { Log-Output "Generate All: abort (Apply Queue failed)."; return $false }

    $ok = Run-ValidateRegistry
    if (-not $ok) { Log-Output "Generate All: abort (Validate failed)."; return $false }

    $ok = Run-NextGatePacket
    if (-not $ok) { Log-Output "Generate All: abort (NextGatePacket failed)."; return $false }

    $ok = Run-LlmPrompt -SkipNextGate
    if (-not $ok) { Log-Output "Generate All: abort (LlmPrompt failed)."; return $false }

    $ok = Copy-LLMPromptToClipboard
    if (-not $ok) { Log-Output "Generate All: prompt generated, but clipboard copy failed."; return $false }

    Write-DevtoolSummary "generate_all" @(
        "docs/generated/devtool_summary.json",
        "docs/generated/01_CONTEXT_PACKET.md",
        "docs/generated/connectivity_manifest.json",
        "docs/generated/connectivity_graph.json",
        "docs/generated/connectivity_violations.json",
        "docs/generated/gates_queue_full.json",
        "docs/generated/gates_scan_preflight.md",
        "docs/gates/gates.json",
        "docs/generated/next_gate_packet.md",
        "docs/generated/llm_prompt.md",
        "docs/generated/llm_attachments.txt"
    )

    Log-Output "GENERATE ALL: OK."
    return $true
}

function Run-StepDCloseoutPatch {
    Log-Output ">>> EXEC: STEP D HELPER (generate closeout patch)"

    $ok = Require-Apply "Step D Helper (writes docs/generated/phase4_closeout_patch.md)" @(
        "docs/generated/phase4_closeout_patch.md"
    )
    if (-not $ok) { return $false }

    $scriptPath = Join-Path $ProjectRoot "scripts/tools/New-StepDCloseoutPatch.ps1"
    if (-not (Test-Path -LiteralPath $scriptPath)) {
        Log-Output "ERROR: Missing $scriptPath"
        return $false
    }

    try {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $scriptPath -AllowHeuristicFinalizePick | Out-Null
        Log-Output "WROTE: docs/generated/phase4_closeout_patch.md"
        return $true
    } catch {
        Log-Output "ERROR (Step D helper):"
        Log-Output ($_ | Out-String)
        return $false
    }
}

# --- GUI SETUP ---
$form = New-Object System.Windows.Forms.Form
$form.Text = "STE :: Mission Control v6.0"
$form.Size = New-Object System.Drawing.Size(930, 640)
$form.StartPosition = "CenterScreen"
$form.BackColor = "#1e1e1e"
$form.ForeColor = "#ffffff"

$fontHeader = New-Object System.Drawing.Font("Consolas", 12, [System.Drawing.FontStyle]::Bold)
$fontNormal = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$fontSmall  = New-Object System.Drawing.Font("Segoe UI", 9,  [System.Drawing.FontStyle]::Regular)
$fontLog    = New-Object System.Drawing.Font("Consolas", 9)

function Get-QueueCapFromUi {
    param([System.Windows.Forms.TextBox] $TextBox)

    $cap = $DefaultQueueCap
    try {
        $v = (($TextBox.Text + "").Trim())
        if ($v -match '^\d+$') { $cap = [int]$v }
    } catch { $cap = $DefaultQueueCap }

    if ($cap -lt 1) { $cap = 1 }
    return $cap
}

function Format-Stamp([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return "missing" }
    try {
        $t = (Get-Item -LiteralPath $Path).LastWriteTime
        return $t.ToString("yyyy-MM-dd HH:mm:ss")
    } catch {
        return "unknown"
    }
}

function Set-StatusText {
    if ($null -eq $txtStatus) { return }
    $applyNeeded = $false
    try { $applyNeeded = [bool](Should-ApplyQueue) } catch { $applyNeeded = $false }

    $lines = @()
    $lines += "ContextPacket:  " + (Format-Stamp $ContextPacketPath)
    $lines += "Ledger IR:      " + (Format-Stamp $LedgerIrPath)
    $lines += "Queue Full:     " + (Format-Stamp $QueueFullPath)
    $lines += "Registry:       " + (Format-Stamp $RegistryPath)
    $lines += "Next Packet:    " + (Format-Stamp $NextGatePacketPath)
    $lines += "LLM Prompt:     " + (Format-Stamp $LlmPromptPath)
    $lines += "Apply needed:   " + ($(if ($applyNeeded) { "YES" } else { "no" }))
    $txtStatus.Text = ($lines -join "`r`n")
}

function Require-Apply([string] $ActionName, [string[]] $WillWrite) {
    if ($global:DEVTOOL_HEADLESS) { return $true }

    Log-Output "ACTION: $ActionName"
    if ($WillWrite -and $WillWrite.Count -gt 0) {
        Log-Output "FILES TO BE WRITTEN:"
        foreach ($p in $WillWrite) { Log-Output ("- " + $p) }
    } else {
        Log-Output "FILES TO BE WRITTEN: (none declared)"
    }

    $resp = ""
    try {
        $resp = [Microsoft.VisualBasic.Interaction]::InputBox(
            "Type APPLY to proceed:",
            "Confirm write action",
            ""
        )
    } catch {
        # Fallback if VB prompt fails
        $resp = Read-Host "Type APPLY to proceed"
    }

    $resp = ($resp + "").Trim()
    if ($resp -ne "APPLY") {
        Log-Output "CANCELLED: confirmation not received."
        return $false
    }
    return $true
}

# Button styling helper
function New-DevtoolButton([System.Windows.Forms.Control] $Parent, [string] $Text, [int] $X, [int] $Y, [int] $W, [int] $H, [string] $BackColor, [scriptblock] $OnClick) {
    $btn = New-Object System.Windows.Forms.Button
    $btn.Text = $Text
    $btn.Location = New-Object System.Drawing.Point($X, $Y)
    $btn.Size = New-Object System.Drawing.Size($W, $H)
    $btn.Font = $fontNormal
    $btn.BackColor = $BackColor
    $btn.ForeColor = "White"
    $btn.FlatStyle = "Flat"
    $btn.Add_Click($OnClick)
    $Parent.Controls.Add($btn)
    return $btn
}

# Header
$lblHeader = New-Object System.Windows.Forms.Label
$lblHeader.Text = "MISSION CONTROL"
$lblHeader.Location = New-Object System.Drawing.Point(20, 10)
$lblHeader.Font = $fontHeader
$lblHeader.AutoSize = $true
$form.Controls.Add($lblHeader)

# Tabs
$tabs = New-Object System.Windows.Forms.TabControl
$tabs.Location = New-Object System.Drawing.Point(20, 40)
$tabs.Size = New-Object System.Drawing.Size(420, 560)
$tabs.Appearance = "Normal"
$form.Controls.Add($tabs)

$tabPipeline = New-Object System.Windows.Forms.TabPage
$tabPipeline.Text = "Gate Pipeline"
$tabPipeline.BackColor = "#1e1e1e"
$tabPipeline.ForeColor = "#ffffff"
$tabs.TabPages.Add($tabPipeline)

$tabExec = New-Object System.Windows.Forms.TabPage
$tabExec.Text = "Shard Execution"
$tabExec.BackColor = "#1e1e1e"
$tabExec.ForeColor = "#ffffff"
$tabs.TabPages.Add($tabExec)

# --- Gate Pipeline tab layout ---
$grpOptions = New-Object System.Windows.Forms.GroupBox
$grpOptions.Text = "Options"
$grpOptions.Location = New-Object System.Drawing.Point(10, 10)
$grpOptions.Size = New-Object System.Drawing.Size(375, 85)
$grpOptions.ForeColor = "#ffffff"
$tabPipeline.Controls.Add($grpOptions)

$chkIncludeTests = New-Object System.Windows.Forms.CheckBox
$chkIncludeTests.Text = "Include tests (optional)"
$chkIncludeTests.Location = New-Object System.Drawing.Point(10, 20)
$chkIncludeTests.AutoSize = $true
$chkIncludeTests.Checked = $false
$chkIncludeTests.ForeColor = "#ffffff"
$grpOptions.Controls.Add($chkIncludeTests)

$chkIncludeConnectivity = New-Object System.Windows.Forms.CheckBox
$chkIncludeConnectivity.Text = "Include connectivity scan (default ON)"
$chkIncludeConnectivity.Location = New-Object System.Drawing.Point(10, 42)
$chkIncludeConnectivity.AutoSize = $true
$chkIncludeConnectivity.Checked = $true
$chkIncludeConnectivity.ForeColor = "#ffffff"
$grpOptions.Controls.Add($chkIncludeConnectivity)

$lblQueueCap = New-Object System.Windows.Forms.Label
$lblQueueCap.Text = "Queue cap:"
$lblQueueCap.Location = New-Object System.Drawing.Point(240, 22)
$lblQueueCap.AutoSize = $true
$lblQueueCap.ForeColor = "#ffffff"
$grpOptions.Controls.Add($lblQueueCap)

$txtQueueCap = New-Object System.Windows.Forms.TextBox
$txtQueueCap.Location = New-Object System.Drawing.Point(305, 19)
$txtQueueCap.Size = New-Object System.Drawing.Size(55, 20)
$txtQueueCap.Text = "$DefaultQueueCap"
$grpOptions.Controls.Add($txtQueueCap)

$btnRefreshStatusTop = New-DevtoolButton $grpOptions "Refresh Status" 240 45 120 28 "#333333" {
    Set-StatusText
}

# Pipeline buttons (Gate Pipeline tab)
# Goal: build/refresh registry inputs (context, connectivity, queue, registry), NOT shard execution.
$y = 105

function Run-BuildRegistryFresh {
    param(
        [int] $QueueCap,
        [bool] $IncludeTests,
        [bool] $IncludeConnectivity
    )

    Log-Output ">>> EXEC: BUILD REGISTRY (FRESH)"

    if ($IncludeTests) {
        Log-Output "Build Registry: running tests"
        Run-SimCoreTests
    }

    if ($IncludeConnectivity) {
        Log-Output "Build Registry: running connectivity scan"
        $ok = Run-ConnectivityScan
        if (-not $ok) { Log-Output "Build Registry: abort (Connectivity failed)."; return $false }
    }

    Log-Output "Build Registry: generating context packet"
    $ok = Run-ContextGen
    if (-not $ok) { Log-Output "Build Registry: abort (Context failed)."; return $false }

    Log-Output "Build Registry: building queue (Stage 2)"
    $ok = Run-BuildQueue -QueueCap $QueueCap
    if (-not $ok) {
        if (Test-Path -LiteralPath $QueueFullPath) {
            Log-Output "Build Registry: Build Queue failed, but existing queue_full.json present. Proceeding with Apply + Validate."
        } else {
            Log-Output "Build Registry: abort (Build Queue failed and no existing queue_full.json present)."
            return $false
        }
    }

    Log-Output "Build Registry: apply queue to registry"
    $ok = Run-ApplyQueue
    if (-not $ok) { Log-Output "Build Registry: abort (Apply Queue failed)."; return $false }

    Log-Output "Build Registry: validate registry"
    $ok = Run-ValidateRegistry
    if (-not $ok) { Log-Output "Build Registry: abort (Validate failed)."; return $false }

    Log-Output "BUILD REGISTRY (FRESH): OK."
    return $true
}

$btnRunTests = New-DevtoolButton $tabPipeline "Run Tests (SimCore)" 10 $y 375 40 "#007acc" {
    Run-SimCoreTests
    Set-StatusText
}
$y += 45

$btnBuildRegistryFresh = New-DevtoolButton $tabPipeline "Build Registry (Fresh)" 10 $y 375 40 "#444444" {
    $btnBuildRegistryFresh.Enabled = $false
    try {
        $cap = Get-QueueCapFromUi -TextBox $txtQueueCap
        $ok = Run-BuildRegistryFresh -QueueCap $cap -IncludeTests:($chkIncludeTests.Checked) -IncludeConnectivity:($chkIncludeConnectivity.Checked)
        if (-not $ok) { Log-Output "Build Registry (Fresh): FAILED." }
    } finally {
        $btnBuildRegistryFresh.Enabled = $true
        Set-StatusText
    }
}
$y += 45

$btnFromIrToRegistry = New-DevtoolButton $tabPipeline "From IR -> Build + Apply + Validate" 10 $y 375 40 "#2d6a4f" {
    $btnFromIrToRegistry.Enabled = $false
    try {
        $cap = Get-QueueCapFromUi -TextBox $txtQueueCap

        Log-Output "IR->Registry: generating context packet"
        $ok = Run-ContextGen
        if (-not $ok) { Log-Output "IR->Registry: abort (Context failed)."; return }

        Log-Output "IR->Registry: building queue (Stage 2)"
        $ok = Run-BuildQueue -QueueCap $cap
if (-not $ok) {
    if (Test-Path -LiteralPath $QueueFullPath) {
        try {
            $qInfo = Get-Item -LiteralPath $QueueFullPath -ErrorAction Stop
            if ($qInfo.Length -gt 0) {
                Log-Output "IR->Registry: Build Queue failed, but existing non-empty queue_full.json present ($($qInfo.Length) bytes). Proceeding with Apply + Validate."
            } else {
                Log-Output "IR->Registry: abort (Build Queue failed and queue_full.json is empty)."
                return
            }
        } catch {
            Log-Output "IR->Registry: abort (Build Queue failed and queue_full.json could not be inspected)."
            return
        }
    } else {
        Log-Output "IR->Registry: abort (Build Queue failed and no existing queue_full.json present)."
        return
    }
}

        Log-Output "IR->Registry: apply queue to registry"
        $ok = Run-ApplyQueue
        if (-not $ok) { Log-Output "IR->Registry: abort (Apply Queue failed)."; return }

        Log-Output "IR->Registry: validate registry"
        $ok = Run-ValidateRegistry
        if (-not $ok) { Log-Output "IR->Registry: abort (Validate failed)."; return }

        Log-Output "IR->Registry: OK."
    } finally {
        $btnFromIrToRegistry.Enabled = $true
        Set-StatusText
    }
}
$y += 45

$btnLedgerIr = New-DevtoolButton $tabPipeline "Stage 1b: Copy Ledger IR Prompt (LLM)" 10 $y 375 40 "#555555" {
    $cap = Get-QueueCapFromUi -TextBox $txtQueueCap
    Run-LedgerIrPrompt -Cap $cap | Out-Null
    Set-StatusText
}
$y += 45

# Advanced (readable single-purpose buttons)
$btnContextOnly = New-DevtoolButton $tabPipeline "Advanced: Context Packet only" 10 $y 375 40 "#6a00ff" {
    Run-ContextGen | Out-Null
    Set-StatusText
}
$y += 45

$btnConnOnly = New-DevtoolButton $tabPipeline "Advanced: Connectivity Scan only" 10 $y 375 40 "#d46a00" {
    Run-ConnectivityScan | Out-Null
    Set-StatusText
}
$y += 45

$btnBuildQueueOnly = New-DevtoolButton $tabPipeline "Advanced: Build Queue only (Stage 2, no gates.json)" 10 $y 375 40 "#444444" {
    $cap = Get-QueueCapFromUi -TextBox $txtQueueCap
    Run-BuildQueue -QueueCap $cap | Out-Null
    Set-StatusText
}
$y += 45

$btnApplyValidateOnly = New-DevtoolButton $tabPipeline "Advanced: Apply Queue -> gates.json + Validate" 10 $y 375 40 "#444444" {
    $ok = Run-ApplyQueue
    if (-not $ok) { Set-StatusText; return }
    Run-ValidateRegistry | Out-Null
    Set-StatusText
}
$y += 45


# --- Shard Execution tab layout ---
# Goal: start shard prompt, run proofs, closeout, git.
$y2 = 15

function Open-TextFile([string] $RelPath) {
    try {
        $p = Join-Path $ProjectRoot ($RelPath.Replace("/", "\"))
        if (-not (Test-Path -LiteralPath $p)) { Log-Output "ERROR: Missing $RelPath"; return }
        Start-Process -FilePath "notepad.exe" -ArgumentList @($p) | Out-Null
    } catch {
        Log-Output "ERROR (open file):"
        Log-Output ($_ | Out-String)
    }
}

function Run-RunProofsCurrent {
    Log-Output ">>> EXEC: RUN PROOFS (CURRENT)"
    Run-SimCoreTests
    if (-not (Test-Path -LiteralPath $LlmAttachPath)) {
        Log-Output "NOTE: Missing docs/generated/llm_attachments.txt, skipping Validate-GodotScript shortlist."
    } else {
        Run-ValidateGodotScript -FromAttachmentShortlist | Out-Null
    }
}

$btnStartShardFresh = New-DevtoolButton $tabExec "Start Shard (Fresh): Build Prompt" 10 $y2 375 50 "#007acc" {
    $btnStartShardFresh.Enabled = $false
    try {
        Run-ContextGen | Out-Null
        $ok = Run-LlmPrompt
        if ($ok) {
            Copy-LLMPromptToClipboard | Out-Null
        } else {
            Log-Output "Skip copy: LLM prompt generation failed."
        }
    } finally {
        $btnStartShardFresh.Enabled = $true
        Set-StatusText
    }
}
$y2 += 60

$btnShowCurrentShard = New-DevtoolButton $tabExec "Show Current Shard (Read-only)" 10 $y2 375 40 "#444444" {
    Open-TextFile "docs/generated/next_gate_packet.md"
    Set-StatusText
}
$y2 += 45

$btnShowLlmPrompt = New-DevtoolButton $tabExec "Show LLM Prompt (Read-only)" 10 $y2 375 40 "#444444" {
    Open-TextFile "docs/generated/llm_prompt.md"
    Set-StatusText
}
$y2 += 45

$btnNextGateOnly = New-DevtoolButton $tabExec "Advanced: Next Gate Packet only" 10 $y2 375 40 "#444444" {
    Run-NextGatePacket | Out-Null
    Set-StatusText
}
$y2 += 45

$btnPromptOnly = New-DevtoolButton $tabExec "Advanced: LLM Prompt only (runs NextGate unless skipped)" 10 $y2 375 40 "#444444" {
    Run-LlmPrompt | Out-Null
    Set-StatusText
}
$y2 += 45

$btnCopyPrompt = New-DevtoolButton $tabExec "Copy LLM Prompt to Clipboard" 10 $y2 375 40 "#444444" {
    Copy-LLMPromptToClipboard | Out-Null
    Set-StatusText
}
$y2 += 55

$btnRunProofs = New-DevtoolButton $tabExec "Run Proofs (current)" 10 $y2 375 50 "#007acc" {
    Run-RunProofsCurrent
    Set-StatusText
}
$y2 += 60

$btnStepD = New-DevtoolButton $tabExec "Step D: Generate Closeout Patch" 10 $y2 375 40 "#444444" {
    $ok = Run-StepDCloseoutPatch
    if (-not $ok) { Log-Output "Step D: FAILED." }
    Set-StatusText
}
$y2 += 45

$btnCloseoutCheck = New-DevtoolButton $tabExec "Closeout Check (read-only)" 10 $y2 375 40 "#444444" {
    Run-CloseoutCheck | Out-Null
    Set-StatusText
}
$y2 += 45

$btnGitStatus = New-DevtoolButton $tabExec "Git Status" 10 $y2 375 40 "#333333" {
    Run-GitStatus | Out-Null
    Set-StatusText
}
$y2 += 45

$btnCommitPush = New-DevtoolButton $tabExec "Commit + Push" 10 $y2 375 50 "#228822" {
    $btnCommitPush.Enabled = $false
    try {
        Run-GitCommitAndPush | Out-Null
    } finally {
        $btnCommitPush.Enabled = $true
        Set-StatusText
    }
}
$y2 += 60

$lblExecNote = New-Object System.Windows.Forms.Label
$lblExecNote.Text = "Shard loop: Start Shard -> paste prompt -> do work -> Run Proofs -> Step D -> Closeout Check -> Commit"
$lblExecNote.Location = New-Object System.Drawing.Point(10, $y2)
$lblExecNote.Size = New-Object System.Drawing.Size(375, 60)
$lblExecNote.Font = $fontSmall
$lblExecNote.ForeColor = "#bbbbbb"
$tabExec.Controls.Add($lblExecNote)

$tabExec.Controls.Add($lblExecNote)

# Status panel (below tabs, above log)
$grpStatus = New-Object System.Windows.Forms.GroupBox
$grpStatus.Text = "Status"
$grpStatus.Location = New-Object System.Drawing.Point(460, 40)
$grpStatus.Size = New-Object System.Drawing.Size(440, 200)
$grpStatus.ForeColor = "#ffffff"
$form.Controls.Add($grpStatus)


# Status panel (below tabs, above log)
$grpStatus = New-Object System.Windows.Forms.GroupBox
$grpStatus.Text = "Status"
$grpStatus.Location = New-Object System.Drawing.Point(460, 40)
$grpStatus.Size = New-Object System.Drawing.Size(440, 200)
$grpStatus.ForeColor = "#ffffff"
$form.Controls.Add($grpStatus)

$txtStatus = New-Object System.Windows.Forms.TextBox
$txtStatus.Location = New-Object System.Drawing.Point(10, 20)
$txtStatus.Size = New-Object System.Drawing.Size(420, 170)
$txtStatus.Multiline = $true
$txtStatus.ScrollBars = "Vertical"
$txtStatus.BackColor = "#111111"
$txtStatus.ForeColor = "#ffffff"
$txtStatus.Font = $fontSmall
$txtStatus.ReadOnly = $true
$grpStatus.Controls.Add($txtStatus)

# Console Output (containerized so CLEAR LOG is always visible)
$grpConsole = New-Object System.Windows.Forms.GroupBox
$grpConsole.Text = "Console"
$grpConsole.Location = New-Object System.Drawing.Point(460, 250)
$grpConsole.Size = New-Object System.Drawing.Size(440, 350)
$grpConsole.ForeColor = "#ffffff"
$form.Controls.Add($grpConsole)

$btnClearLog = New-DevtoolButton $grpConsole "CLEAR LOG" 10 20 420 26 "#333333" {
    $txtOutput.Text = "Mission Control v6.0 Online...`r`n"
}

$txtOutput = New-Object System.Windows.Forms.TextBox
$txtOutput.Location = New-Object System.Drawing.Point(10, 50)
$txtOutput.Size = New-Object System.Drawing.Size(420, 290)
$txtOutput.Multiline = $true
$txtOutput.ScrollBars = "Vertical"
$txtOutput.BackColor = "#000000"
$txtOutput.ForeColor = "#00ff00"
$txtOutput.Font = $fontLog
$txtOutput.Text = "Mission Control v6.0 Online...`r`n"
$grpConsole.Controls.Add($txtOutput)


# Initial status paint
Set-StatusText

# --- LAUNCH ---
$form.Add_Shown({ $form.Activate() })
[void] $form.ShowDialog()
