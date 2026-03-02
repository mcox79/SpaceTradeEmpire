[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# --- PROJECT ROOT -----------------------------------------------------------
$ProjectRoot = (& git rev-parse --show-toplevel 2>$null)
if (-not $ProjectRoot) { throw "Not in a git repo (git rev-parse failed)." }
$ProjectRoot = $ProjectRoot.Trim()
Set-Location $ProjectRoot

# --- CONFIG -----------------------------------------------------------------
$ConfigPath = Join-Path $ProjectRoot "devtool.config.json"
if (-not (Test-Path -LiteralPath $ConfigPath)) {
    throw "Missing devtool.config.json at repo root."
}
$Config = (Get-Content -LiteralPath $ConfigPath -Raw -Encoding UTF8 | ConvertFrom-Json)

function Resolve-Cfg([string] $rel) {
    return (Join-Path $ProjectRoot ($rel -replace "/", "\"))
}

$GatesFile         = Resolve-Cfg $Config.gates_file
$EpicsFile         = Resolve-Cfg $Config.epics_file
$GatesLedger       = Resolve-Cfg $Config.gates_ledger
$GeneratedDir      = Resolve-Cfg $Config.generated_dir
$SessionDropDir    = Resolve-Cfg $Config.session_drop_dir
$ContextScript     = Resolve-Cfg $Config.context_script
$NextGateScript    = Resolve-Cfg $Config.next_gate_script
$PromptScript      = Resolve-Cfg $Config.prompt_script

$ContextPacketPath = Join-Path $GeneratedDir "01_CONTEXT_PACKET.md"
$LlmPromptPath     = Join-Path $GeneratedDir "llm_prompt.md"
$LlmAttachPath     = Join-Path $GeneratedDir "llm_attachments.txt"
$TestLogPath       = Join-Path $GeneratedDir "05_TEST_SUMMARY.txt"

# --- HELPERS ----------------------------------------------------------------
function Ensure-Dir([string] $d) {
    if (-not (Test-Path -LiteralPath $d)) { New-Item -ItemType Directory -Force -Path $d | Out-Null }
}

function Format-Stamp([string] $path) {
    if (-not (Test-Path -LiteralPath $path)) { return "-" }
    return (Get-Item -LiteralPath $path).LastWriteTime.ToString("HH:mm:ss")
}

function Get-CurrentTask {
    if (-not (Test-Path -LiteralPath $GatesFile)) { return $null }
    try {
        $reg   = (Get-Content -LiteralPath $GatesFile -Raw -Encoding UTF8 | ConvertFrom-Json)
        $tasks = @($reg.tasks)
        $order = @{ "IN_PROGRESS" = 0; "TODO" = 1 }
        $indexed = for ($i = 0; $i -lt $tasks.Count; $i++) {
            [pscustomobject]@{ Idx = $i; Task = $tasks[$i] }
        }
        $found = $indexed |
            Where-Object { $_.Task.status -in @("IN_PROGRESS","TODO") } |
            Sort-Object @{ Expression = { if ($order.ContainsKey($_.Task.status)) { $order[$_.Task.status] } else { 99 } } }, Idx |
            Select-Object -First 1
        if ($found) { return $found.Task } else { return $null }
    } catch { return $null }
}

function Test-Dep([string] $cmd) {
    return ($null -ne (Get-Command $cmd -ErrorAction SilentlyContinue))
}

# --- WINFORMS ---------------------------------------------------------------
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName Microsoft.VisualBasic

# --- LOGGING ----------------------------------------------------------------
$script:txtLog = $null
function Log([string] $msg) {
    $line = "[$((Get-Date).ToString('HH:mm:ss'))] $msg"
    if ($null -ne $script:txtLog) {
        $script:txtLog.AppendText($line + "`r`n")
        $script:txtLog.ScrollToCaret()
    } else { Write-Host $line }
}

# --- UI STATE REFS (assigned during GUI build, used by action functions) ----
$script:btnRunTests   = $null
$script:btnCommit     = $null
$script:lblStatus     = $null
$script:lblGateId     = $null
$script:lblGateTitle  = $null
$script:lblCtxStamp   = $null
$script:lblPrmStamp   = $null

# --- BACKGROUND GIT JOB -----------------------------------------------------
$global:GitJob   = $null
$global:GitTimer = New-Object System.Windows.Forms.Timer
$global:GitTimer.Interval = 250
$global:GitTimer.Add_Tick({
    if (-not $global:GitJob) { $global:GitTimer.Stop(); return }
    if ($global:GitJob.State -eq "Running") { return }
    $global:GitTimer.Stop()

    try {
        $lines = @(Receive-Job -Job $global:GitJob -ErrorAction SilentlyContinue)
        $lines | ForEach-Object { Log "  $_" }
    } catch {}
    try { Remove-Job -Job $global:GitJob -Force -ErrorAction SilentlyContinue } catch {}
    $global:GitJob = $null

    if ($null -ne $script:btnCommit) { $script:btnCommit.Enabled = $true }
    Update-Status
})

# --- BACKGROUND TEST JOB ----------------------------------------------------
$global:TestJob   = $null
$global:TestTimer = New-Object System.Windows.Forms.Timer
$global:TestTimer.Interval = 250
$global:TestTimer.Add_Tick({
    if (-not $global:TestJob) { $global:TestTimer.Stop(); return }
    if ($global:TestJob.State -eq "Running") { return }
    $global:TestTimer.Stop()

    $exitCode = 1
    try {
        $result = @(Receive-Job -Job $global:TestJob -ErrorAction SilentlyContinue)
        if ($result.Count -gt 0) { $exitCode = [int]$result[-1] }
    } catch {}
    try { Remove-Job -Job $global:TestJob -Force -ErrorAction SilentlyContinue } catch {}
    $global:TestJob = $null

    if (Test-Path -LiteralPath $TestLogPath) {
        Get-Content -LiteralPath $TestLogPath -Tail 25 | ForEach-Object { Log $_ }
    }
    if ($exitCode -eq 0) { Log "TESTS: PASS" } else { Log "TESTS: FAIL (exit $exitCode)" }
    if ($null -ne $script:btnRunTests) { $script:btnRunTests.Enabled = $true }
    Update-Status
})

# --- ACTION FUNCTIONS -------------------------------------------------------

function Run-ContextRefresh([switch] $FullMap) {
    $label = ""
    if ($FullMap) { $label = "(full file map)" }
    Log ">>> Context refresh $label"
    if (-not (Test-Path -LiteralPath $ContextScript)) {
        Log "CONTEXT: FAIL - missing $ContextScript"; return $false
    }
    try {
        if ($FullMap) { & $ContextScript -IncludeFileMap | Out-Null }
        else          { & $ContextScript | Out-Null }
        Log "CONTEXT: OK"
        return $true
    } catch { Log "CONTEXT: FAIL - $($_.Exception.Message)"; return $false }
}

function Run-NextGatePkt {
    Log ">>> Next gate packet"
    if (-not (Test-Path -LiteralPath $NextGateScript)) {
        Log "NEXT GATE: FAIL - missing script"; return $false
    }
    try { & $NextGateScript | Out-Null; Log "NEXT GATE: OK"; return $true }
    catch { Log "NEXT GATE: FAIL - $($_.Exception.Message)"; return $false }
}

function Run-LlmPromptGen {
    Log ">>> LLM prompt"
    if (-not (Test-Path -LiteralPath $PromptScript)) {
        Log "PROMPT: FAIL - missing script"; return $false
    }
    try { & $PromptScript | Out-Null; Log "PROMPT: OK"; return $true }
    catch { Log "PROMPT: FAIL - $($_.Exception.Message)"; return $false }
}

function Copy-PromptToClipboard {
    if (-not (Test-Path -LiteralPath $LlmPromptPath)) {
        Log "COPY: no prompt file yet - run Start Session first"; return
    }
    try {
        $text = Get-Content -LiteralPath $LlmPromptPath -Raw -Encoding UTF8
        [System.Windows.Forms.Clipboard]::SetText($text)
        Log "COPIED: prompt to clipboard"
    } catch { Log "COPY: clipboard failed - $($_.Exception.Message)" }
}

function Run-StartSession {
    Log "=== START SESSION ==="
    $ok = Run-ContextRefresh;  if (-not $ok) { Log "Session: aborted"; return }
    $ok = Run-NextGatePkt;     if (-not $ok) { Log "Session: aborted"; return }
    $ok = Run-LlmPromptGen;    if (-not $ok) { Log "Session: aborted"; return }

    Copy-PromptToClipboard

    if (Test-Path -LiteralPath $LlmAttachPath) {
        Log "Attachments for this session:"
        Get-Content -LiteralPath $LlmAttachPath -Encoding UTF8 |
            ForEach-Object { $s = $_.Trim(); if ($s) { Log "  - $s" } }
    }
    Log "Session drop: $SessionDropDir"
    Log "=== READY ==="
    Update-Status
}

function Run-GenGatesPrep {
    Log "=== GENERATE NEXT GATES (prep) ==="

    $ok = Run-ContextRefresh -FullMap
    if (-not $ok) { Log "Gen-gates prep: aborted"; return }

    Ensure-Dir $SessionDropDir
    $toStage = @(
        [pscustomobject]@{ Src = $EpicsFile;   Name = "EPICS.md" }
        [pscustomobject]@{ Src = $GatesLedger; Name = "GATES_LEDGER.md" }
        [pscustomobject]@{ Src = (Join-Path $ProjectRoot "docs\gates\GATE_FREEZE_RULES.md"); Name = "GATE_FREEZE_RULES.md" }
        [pscustomobject]@{ Src = $GatesFile;   Name = "gates.json" }
        [pscustomobject]@{ Src = $ContextPacketPath; Name = "01_CONTEXT_PACKET.md" }
    )
    foreach ($f in $toStage) {
        if (Test-Path -LiteralPath $f.Src) {
            Copy-Item -LiteralPath $f.Src -Destination (Join-Path $SessionDropDir $f.Name) -Force
            Log "Staged: $($f.Name)"
        } else { Log "WARNING: missing $($f.Src)" }
    }

    try { [System.Windows.Forms.Clipboard]::SetText("/gen-gates") } catch {}
    Log "COPIED: /gen-gates to clipboard"
    Log "  -> Claude Code: paste /gen-gates"
    Log "  -> Other LLM:   use files in docs/generated/session_drop/"
    Log "=== DONE ==="
    Update-Status
}

function Run-Tests {
    if ($global:TestJob -and $global:TestJob.State -eq "Running") {
        Log "Tests: already running."; return
    }
    Log ">>> Tests: $($Config.test_command)"
    Ensure-Dir $GeneratedDir
    if (Test-Path -LiteralPath $TestLogPath) { Remove-Item -Force -LiteralPath $TestLogPath }
    if ($null -ne $script:btnRunTests) { $script:btnRunTests.Enabled = $false }

    $repoRoot = $ProjectRoot
    $logPath  = $TestLogPath
    $testCmd  = $Config.test_command

    $global:TestJob = Start-Job -ArgumentList $repoRoot, $logPath, $testCmd -ScriptBlock {
        param($root, $lp, $cmd)
        Set-Location $root
        $out = (Invoke-Expression $cmd 2>&1) -join "`r`n"
        $enc = New-Object System.Text.UTF8Encoding($false)
        [System.IO.File]::WriteAllText($lp, $out + "`r`n", $enc)
        return $LASTEXITCODE
    }
    $global:TestTimer.Start()
    Log "Tests running in background..."
}

function Open-DropFolder {
    try {
        Ensure-Dir $SessionDropDir
        Start-Process "explorer.exe" -ArgumentList $SessionDropDir
        Log "Opened: session drop folder"
    } catch { Log "OPEN FOLDER: $($_.Exception.Message)" }
}

function Run-GitStatus {
    try {
        $branch  = (git rev-parse --abbrev-ref HEAD 2>$null).Trim()
        $changes = @(git status --porcelain 2>$null)
        Log "Branch: $branch"
        if ($changes.Count -eq 0) { Log "Working tree: clean" }
        else {
            Log "Changes: $($changes.Count)"
            $changes | Select-Object -First 30 | ForEach-Object { Log "  $_" }
        }
    } catch { Log "GIT STATUS: $($_.Exception.Message)" }
}

function Run-CommitPush {
    if ($global:GitJob -and $global:GitJob.State -eq "Running") {
        Log "Git: already running."; return
    }

    $default = "wip: $($Config.project_name) " + (Get-Date -Format "yyyy-MM-dd HH:mm")
    $msg = [Microsoft.VisualBasic.Interaction]::InputBox(
        "Commit message (blank to cancel):", "Commit + Push", $default)
    $msg = ($msg + "").Trim()
    if ([string]::IsNullOrWhiteSpace($msg)) { Log "Commit: cancelled."; return }

    Log ">>> Commit + Push: $msg"
    if ($null -ne $script:btnCommit) { $script:btnCommit.Enabled = $false }

    $repoRoot = $ProjectRoot
    $global:GitJob = Start-Job -ArgumentList $repoRoot, $msg -ScriptBlock {
        param($root, $commitMsg)
        Set-Location $root
        $out = @()
        $out += (git add -A 2>&1)
        if ($LASTEXITCODE -ne 0) { $out += "FAIL: git add"; return $out }
        $staged = @(git status --porcelain 2>&1)
        if ($staged.Count -eq 0) { $out += "Nothing to commit."; return $out }
        $out += (git commit -m $commitMsg 2>&1)
        if ($LASTEXITCODE -ne 0) { $out += "FAIL: git commit"; return $out }
        $out += (git push 2>&1)
        if ($LASTEXITCODE -ne 0) { $out += "FAIL: git push"; return $out }
        $out += "COMMIT + PUSH: OK"
        return $out
    }
    $global:GitTimer.Start()
    Log "Git: running in background..."
}

# --- STATUS UPDATE ----------------------------------------------------------
function Update-Status {
    if ($null -ne $script:lblStatus) {
        $branch = "?"
        try { $branch = (git rev-parse --abbrev-ref HEAD 2>$null).Trim() } catch {}

        $godotPart = ""
        if ($Config.engine -eq "godot") {
            if (Test-Dep "godot") { $godotPart = " | godot: ok" } else { $godotPart = " | godot: NOT FOUND" }
        }

        $testPart = "-"
        if (Test-Path -LiteralPath $TestLogPath) { $testPart = "ran" }

        $script:lblStatus.Text = "git: $branch | test: $testPart$godotPart"
    }

    $task = Get-CurrentTask
    if ($null -ne $script:lblGateId) {
        if ($task) { $script:lblGateId.Text = $task.gate_id } else { $script:lblGateId.Text = "(no active gate - run Generate Next Gates)" }
    }
    if ($null -ne $script:lblGateTitle) {
        if ($task) { $script:lblGateTitle.Text = $task.title } else { $script:lblGateTitle.Text = "" }
    }
    if ($null -ne $script:lblCtxStamp) {
        $script:lblCtxStamp.Text = "Context: " + (Format-Stamp $ContextPacketPath)
    }
    if ($null -ne $script:lblPrmStamp) {
        $script:lblPrmStamp.Text = "Prompt:  " + (Format-Stamp $LlmPromptPath)
    }
}

# --- GUI LAYOUT -------------------------------------------------------------
$fntHeader  = New-Object System.Drawing.Font("Consolas",  11, [System.Drawing.FontStyle]::Bold)
$fntSection = New-Object System.Drawing.Font("Segoe UI",  10, [System.Drawing.FontStyle]::Bold)
$fntNormal  = New-Object System.Drawing.Font("Segoe UI",  9,  [System.Drawing.FontStyle]::Regular)
$fntMono    = New-Object System.Drawing.Font("Consolas",  9,  [System.Drawing.FontStyle]::Regular)
$fntSmall   = New-Object System.Drawing.Font("Segoe UI",  8,  [System.Drawing.FontStyle]::Regular)

$clrBg     = [System.Drawing.ColorTranslator]::FromHtml("#1e1e1e")
$clrFg     = [System.Drawing.Color]::White
$clrBlue   = [System.Drawing.ColorTranslator]::FromHtml("#007acc")
$clrGreen  = [System.Drawing.ColorTranslator]::FromHtml("#228822")
$clrTeal   = [System.Drawing.ColorTranslator]::FromHtml("#2d6a4f")
$clrGray   = [System.Drawing.ColorTranslator]::FromHtml("#444444")
$clrDkGray = [System.Drawing.ColorTranslator]::FromHtml("#333333")
$clrMuted  = [System.Drawing.ColorTranslator]::FromHtml("#888888")

function New-Lbl([string] $text, [int] $x, [int] $y, [int] $w, [int] $h,
                 [System.Drawing.Font] $font = $null,
                 [System.Drawing.Color] $fg = [System.Drawing.Color]::Empty) {
    $l           = New-Object System.Windows.Forms.Label
    $l.Text      = $text
    $l.Location  = New-Object System.Drawing.Point($x, $y)
    $l.Size      = New-Object System.Drawing.Size($w, $h)
    $l.BackColor = $clrBg
    if ($font) { $l.Font = $font } else { $l.Font = $fntNormal }
    if ($fg -ne [System.Drawing.Color]::Empty) { $l.ForeColor = $fg } else { $l.ForeColor = $clrFg }
    return $l
}

function New-Btn([string] $text, [int] $x, [int] $y, [int] $w, [int] $h,
                 [System.Drawing.Color] $bg, [scriptblock] $onClick) {
    $b            = New-Object System.Windows.Forms.Button
    $b.Text       = $text
    $b.Location   = New-Object System.Drawing.Point($x, $y)
    $b.Size       = New-Object System.Drawing.Size($w, $h)
    $b.Font       = $fntSection
    $b.BackColor  = $bg
    $b.ForeColor  = $clrFg
    $b.FlatStyle  = [System.Windows.Forms.FlatStyle]::Flat
    $b.Add_Click($onClick)
    return $b
}

function New-GroupBox([string] $title, [int] $x, [int] $y, [int] $w, [int] $h) {
    $g            = New-Object System.Windows.Forms.GroupBox
    $g.Text       = $title
    $g.Location   = New-Object System.Drawing.Point($x, $y)
    $g.Size       = New-Object System.Drawing.Size($w, $h)
    $g.Font       = $fntSection
    $g.ForeColor  = $clrMuted
    $g.BackColor  = $clrBg
    return $g
}

# Form
$form             = New-Object System.Windows.Forms.Form
$form.Text        = "DevTool - $($Config.project_name)"
$form.Size        = New-Object System.Drawing.Size(500, 750)
$form.MinimumSize = New-Object System.Drawing.Size(500, 700)
$form.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
$form.BackColor   = $clrBg
$form.ForeColor   = $clrFg

# Header
$lblHeader = New-Lbl "MISSION CONTROL" 16 10 280 26 $fntHeader
$form.Controls.Add($lblHeader)

$script:lblStatus = New-Lbl "..." 16 36 460 18 $fntSmall $clrMuted
$form.Controls.Add($script:lblStatus)

$SEP = 60   # Y start of first section
$W   = 460  # content width
$L   = 16   # left margin

# PLAN section
$grpPlan = New-GroupBox "PLAN" $L $SEP $W 120
$form.Controls.Add($grpPlan)

$script:lblGateId    = New-Lbl "(reading gates...)" 10 20 440 20 $fntMono
$script:lblGateTitle = New-Lbl "" 10 42 440 36 $fntSmall $clrMuted
$grpPlan.Controls.Add($script:lblGateId)
$grpPlan.Controls.Add($script:lblGateTitle)

$btnCtxFull = New-Btn "Refresh Context (Full)" 10 82 200 30 $clrGray {
    $btnCtxFull.Enabled = $false
    try { Run-ContextRefresh -FullMap | Out-Null; Update-Status }
    finally { $btnCtxFull.Enabled = $true }
}
$grpPlan.Controls.Add($btnCtxFull)

$btnGenGates = New-Btn "Generate Next Gates" 220 82 220 30 $clrTeal {
    $btnGenGates.Enabled = $false
    try { Run-GenGatesPrep; Update-Status }
    finally { $btnGenGates.Enabled = $true }
}
$grpPlan.Controls.Add($btnGenGates)

# SESSION section
$grpSession = New-GroupBox "SESSION" $L ($SEP + 128) $W 140
$form.Controls.Add($grpSession)

$btnStart = New-Btn "> Start Session" 10 20 440 44 $clrBlue {
    $btnStart.Enabled = $false
    try { Run-StartSession; Update-Status }
    finally { $btnStart.Enabled = $true }
}
$grpSession.Controls.Add($btnStart)

$btnCopyPrm = New-Btn "Copy Prompt" 10 72 210 28 $clrGray {
    Copy-PromptToClipboard
}
$grpSession.Controls.Add($btnCopyPrm)

$btnDrop = New-Btn "Open Session Drop" 230 72 210 28 $clrGray {
    Open-DropFolder
}
$grpSession.Controls.Add($btnDrop)

$script:lblCtxStamp = New-Lbl "Context: -" 10 106 210 18 $fntSmall $clrMuted
$script:lblPrmStamp = New-Lbl "Prompt:  -" 230 106 210 18 $fntSmall $clrMuted
$grpSession.Controls.Add($script:lblCtxStamp)
$grpSession.Controls.Add($script:lblPrmStamp)

# VERIFY section
$grpVerify = New-GroupBox "VERIFY" $L ($SEP + 276) $W 80
$form.Controls.Add($grpVerify)

$script:btnRunTests = New-Btn "Run Tests" 10 20 210 44 $clrBlue {
    Run-Tests
}
$grpVerify.Controls.Add($script:btnRunTests)

$btnOpenTestLog = New-Btn "Open Test Log" 230 20 210 44 $clrGray {
    if (Test-Path -LiteralPath $TestLogPath) {
        Start-Process "notepad.exe" -ArgumentList $TestLogPath
    } else { Log "No test log yet - run tests first." }
}
$grpVerify.Controls.Add($btnOpenTestLog)

# SHIP section
$grpShip = New-GroupBox "SHIP" $L ($SEP + 364) $W 80
$form.Controls.Add($grpShip)

$btnGitStatus = New-Btn "Git Status" 10 20 210 44 $clrDkGray {
    Run-GitStatus; Update-Status
}
$grpShip.Controls.Add($btnGitStatus)

$script:btnCommit = New-Btn "Commit + Push" 230 20 210 44 $clrGreen {
    Run-CommitPush
}
$grpShip.Controls.Add($script:btnCommit)

# LOG section
$grpLog = New-GroupBox "LOG" $L ($SEP + 452) $W 192
$grpLog.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor
                 [System.Windows.Forms.AnchorStyles]::Bottom -bor
                 [System.Windows.Forms.AnchorStyles]::Left -bor
                 [System.Windows.Forms.AnchorStyles]::Right
$form.Controls.Add($grpLog)

$script:txtLog = New-Object System.Windows.Forms.TextBox
$script:txtLog.Location   = New-Object System.Drawing.Point(10, 18)
$script:txtLog.Size       = New-Object System.Drawing.Size(440, 166)
$script:txtLog.Multiline  = $true
$script:txtLog.ScrollBars = [System.Windows.Forms.ScrollBars]::Vertical
$script:txtLog.ReadOnly   = $true
$script:txtLog.BackColor  = [System.Drawing.ColorTranslator]::FromHtml("#111111")
$script:txtLog.ForeColor  = [System.Drawing.ColorTranslator]::FromHtml("#cccccc")
$script:txtLog.Font       = $fntMono
$script:txtLog.Anchor     = [System.Windows.Forms.AnchorStyles]::Top -bor
                            [System.Windows.Forms.AnchorStyles]::Bottom -bor
                            [System.Windows.Forms.AnchorStyles]::Left -bor
                            [System.Windows.Forms.AnchorStyles]::Right
$grpLog.Controls.Add($script:txtLog)

$btnClearLog = New-Btn "Clear" 380 ($SEP + 652) 72 22 $clrDkGray {
    $script:txtLog.Clear()
}
$btnClearLog.Font = $fntSmall
$form.Controls.Add($btnClearLog)

# --- STARTUP ----------------------------------------------------------------
$form.Add_Load({
    Update-Status
    Log "DevTool ready - $($Config.project_name)"

    if (-not (Test-Dep "git"))  { Log "WARNING: git not found in PATH" }
    $testRunner = ($Config.test_command -split " ", 2)[0]
    if (-not (Test-Dep $testRunner)) { Log "WARNING: test runner '$testRunner' not found in PATH" }
    if ($Config.engine -eq "godot" -and -not (Test-Dep "godot")) {
        Log "WARNING: godot not found in PATH"
    }

    $task = Get-CurrentTask
    if ($task) {
        Log "Active gate: $($task.gate_id)"
        Log "  $($task.title)"
    } else {
        Log "No active gates found. Use 'Generate Next Gates' to create some."
    }
})

$form.Add_FormClosing({
    $global:TestTimer.Stop()
    $global:GitTimer.Stop()
    if ($global:TestJob) {
        try { Remove-Job -Job $global:TestJob -Force -ErrorAction SilentlyContinue } catch {}
    }
    if ($global:GitJob) {
        try { Remove-Job -Job $global:GitJob -Force -ErrorAction SilentlyContinue } catch {}
    }
})

[System.Windows.Forms.Application]::Run($form)
