<#
SPACE TRADE EMPIRE - MISSION CONTROL (v5.0)

What changed vs v4.2
- Keeps all existing buttons + behavior (tests, save, reset, GUI)
- Context dump now:
  - Inlines first-party: .gd .tscn .shader .ps1 project.godot export_presets.cfg .tres .res .cfg .ini .md .txt .json .csv .yml .yaml
  - References 3rd party dirs (inventory + appears in tree) but does NOT inline their contents
  - Adds per-file bytes + SHA256 + deterministic ordering
  - Adds omitted/errors lists + END OF DUMP marker
  - Excludes editor/system junk: .git .import .godot
  - Excludes the dump file itself from being re-inlined
- Adds strong provenance: script path + script hash written into the dump header

How to install correctly (do this exactly)
1) Save THIS file as: C:\Users\marsh\Documents\Space Trade Empire\DevTool.ps1
2) In PowerShell, verify it is v5.0:
   Select-String -Path .\DevTool.ps1 -Pattern "v4\.2","v5\.0" | ft LineNumber,Line -AutoSize
   (should show v5.0, nothing for v4.2)
3) Run it:
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\DevTool.ps1

If you still see v4.2 after that, you did not overwrite the right file.
#>

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# --- CONFIGURATION ---
$ProjectRoot = Get-Location
$ContextFile = Join-Path (Join-Path $ProjectRoot "_scratch") "_FullProjectContext.txt"
Assert-SafeOutputPath $ContextFile
$TestScene   = "scenes/tests/test_economy_core.tscn"
$ConfigPath  = Join-Path $ProjectRoot "godot_path.cfg"
$DevToolVersion = "5.0"


function Assert-SafeOutputPath([string]$path) {
    $full = (Resolve-Path $path).Path

    $deny = @(
        (Join-Path $ProjectRoot "_PROJECT_CONTEXT.md"),
        (Join-Path $ProjectRoot "project_context.md"),
        (Join-Path $ProjectRoot "README.md"),
        (Join-Path $ProjectRoot "project.godot")
    ) | ForEach-Object { try { (Resolve-Path <#
SPACE TRADE EMPIRE - MISSION CONTROL (v5.0)

What changed vs v4.2
- Keeps all existing buttons + behavior (tests, save, reset, GUI)
- Context dump now:
  - Inlines first-party: .gd .tscn .shader .ps1 project.godot export_presets.cfg .tres .res .cfg .ini .md .txt .json .csv .yml .yaml
  - References 3rd party dirs (inventory + appears in tree) but does NOT inline their contents
  - Adds per-file bytes + SHA256 + deterministic ordering
  - Adds omitted/errors lists + END OF DUMP marker
  - Excludes editor/system junk: .git .import .godot
  - Excludes the dump file itself from being re-inlined
- Adds strong provenance: script path + script hash written into the dump header

How to install correctly (do this exactly)
1) Save THIS file as: C:\Users\marsh\Documents\Space Trade Empire\DevTool.ps1
2) In PowerShell, verify it is v5.0:
   Select-String -Path .\DevTool.ps1 -Pattern "v4\.2","v5\.0" | ft LineNumber,Line -AutoSize
   (should show v5.0, nothing for v4.2)
3) Run it:
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\DevTool.ps1

If you still see v4.2 after that, you did not overwrite the right file.
#>

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# --- CONFIGURATION ---
$ProjectRoot = Get-Location
$ContextFile = Join-Path (Join-Path $ProjectRoot "_scratch") "_FullProjectContext.txt"
Assert-SafeOutputPath $ContextFile
$TestScene   = "scenes/tests/test_economy_core.tscn"
$ConfigPath  = Join-Path $ProjectRoot "godot_path.cfg"
$DevToolVersion = "5.0"

# --- GUI SETUP ---
$form = New-Object System.Windows.Forms.Form
$form.Text = "STE :: Mission Control v$DevToolVersion"
$form.Size = New-Object System.Drawing.Size(450, 520)
$form.StartPosition = "CenterScreen"
$form.BackColor = "#1e1e1e"
$form.ForeColor = "#ffffff"

$fontHeader = New-Object System.Drawing.Font("Consolas", 12, [System.Drawing.FontStyle]::Bold)
$fontNormal = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$fontLog    = New-Object System.Drawing.Font("Consolas", 9)

# Console Output (declared early so Log-Output can be used anywhere)
$txtOutput = New-Object System.Windows.Forms.TextBox
$txtOutput.Location = New-Object System.Drawing.Point(20, 210)
$txtOutput.Size = New-Object System.Drawing.Size(400, 250)
$txtOutput.Multiline = $true
$txtOutput.ScrollBars = "Vertical"
$txtOutput.BackColor = "#000000"
$txtOutput.ForeColor = "#00ff00"
$txtOutput.Font = $fontLog
$txtOutput.Text = "Mission Control v$DevToolVersion Online...`r`n"
$form.Controls.Add($txtOutput)

function Log-Output($message) {
    $txtOutput.AppendText("[$((Get-Date).ToString('HH:mm:ss'))] $message`r`n")
    $txtOutput.ScrollToCaret()
}

function Get-GodotExe {
    $exePath = $null

    if (Test-Path $ConfigPath) {
        $raw = Get-Content $ConfigPath -Raw
        if ($raw) { $exePath = $raw.Trim() }
    }

    if ($exePath -and (Test-Path $exePath)) {
        return $exePath
    }

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

    $godotRaw = Get-GodotExe
    $godotExe = @($godotRaw)[-1]

    if (-not $godotExe -or -not (Test-Path $godotExe)) {
        Log-Output "ERROR: Godot path missing or invalid."
        return
    }

    Log-Output "Target: $godotExe"

    try {
        Log-Output "Running simulation..."
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

# ----------------------------
# CONTEXT DUMP HELPERS
# ----------------------------

function Normalize-RelPath([string]$fullPath) {
    $rp = $fullPath.Replace($ProjectRoot.Path, "")
    $rp = $rp.TrimStart("\","/")
    return $rp -replace "/", "\"
}

function Is-UnderAnyDir([string]$fullPath, [string[]]$dirNames) {
    foreach ($d in $dirNames) {
        if ($fullPath -match [regex]::Escape("\$d\") -or $fullPath.EndsWith("\$d")) { return $true }
        if ($fullPath -match [regex]::Escape("/$d/") -or $fullPath.EndsWith("/$d")) { return $true }
    }
    return $false
}

function Get-Sha256([string]$path) {
    try {
        return (Get-FileHash -Algorithm SHA256 -Path $path).Hash
    } catch {
        return "HASH_ERROR"
    }
}

function Try-ReadText([string]$path) {
    try {
        return Get-Content -Path $path -Raw -ErrorAction Stop
    } catch {
        return $null
    }
}

function Build-TreeListing([string]$rootPath, [string[]]$treeExclusionRegex) {
    $lines = New-Object System.Collections.Generic.List[string]
    try {
        $items = Get-ChildItem -Path $rootPath -Recurse -Force -ErrorAction Stop
        foreach ($it in $items) {
            $full = $it.FullName
            $excluded = $false
            foreach ($rx in $treeExclusionRegex) {
                if ($full -match $rx) { $excluded = $true; break }
            }
            if ($excluded) { continue }

            $rel = Normalize-RelPath $full
            $depth = ($rel.Split('\').Count - 1)
            $indent = "  " * $depth
            if ($it.PSIsContainer) {
                $lines.Add("+ $indent[$($it.Name)]")
            } else {
                $lines.Add("- $indent$($it.Name)")
            }
        }
    } catch {
        $lines.Add("TREE_ERROR: $($_.Exception.Message)")
    }
    return $lines
}

function Summarize-DirInventory([string]$dirPath, [string]$label, [string[]]$excludeRegexForCounting) {
    $summary = [ordered]@{
        label = $label
        path  = (Normalize-RelPath $dirPath)
        file_count = 0
        total_bytes = 0
        top_extensions = @()
    }

    if (-not (Test-Path $dirPath)) { return $summary }

    $extCount = @{}
    try {
        $files = Get-ChildItem -Path $dirPath -Recurse -File -Force -ErrorAction Stop | Where-Object {
            $ok = $true
            foreach ($rx in $excludeRegexForCounting) {
                if ($_.FullName -match $rx) { $ok = $false; break }
            }
            $ok
        }
        $summary.file_count = $files.Count
        $summary.total_bytes = ($files | Measure-Object -Property Length -Sum).Sum

        foreach ($f in $files) {
            $e = [IO.Path]::GetExtension($f.Name).ToLowerInvariant()
            if (-not $e) { $e = "<none>" }
            if (-not $extCount.ContainsKey($e)) { $extCount[$e] = 0 }
            $extCount[$e]++
        }

        $top = $extCount.GetEnumerator() | Sort-Object -Property Value -Descending | Select-Object -First 12
        $summary.top_extensions = $top | ForEach-Object { "$($_.Key):$($_.Value)" }
    } catch {
        $summary.top_extensions = @("INVENTORY_ERROR:$($_.Exception.Message)")
    }

    return $summary
}

function Run-ContextGen {
    Log-Output ">>> EXEC: FULL CONTEXT DUMP (SCOPED, VERIFIED)"

    # Text extensions we embed
    $IncludeExtensions = @(
        ".gd", ".tscn", ".shader", ".ps1",
        ".godot",
        ".tres", ".res",
        ".cfg", ".ini",
        ".md", ".txt",
        ".json", ".csv", ".tsv",
        ".yml", ".yaml"
    )

    # Always include these if present
    $AlwaysIncludeRel = @(
        "project.godot",
        "export_presets.cfg",
        "_PROJECT_CONTEXT.md"
    )

    # Exclude from embedded text dump (but still referenced in inventory + tree)
    $ThirdPartyDirs = @("addons", "third_party", "vendor", "externals", "external")

    # Exclude from everything (tree + inline + inventory)
    $HardExcludeRegex = @(
        "\\.git\\", "/\.git/",
        "\\.import\\", "/\.import/",
        "\\.godot\\", "/\.godot/"
    )

    # For TREE view, hide only system folders. Keep third party visible.
    $TreeExcludeRegex = $HardExcludeRegex

    # Very high cap to avoid surprising first-party omissions.
    # If you want truly unlimited, set this to [int64]::MaxValue
    $MaxInlineBytes = 50MB

    $errors = New-Object System.Collections.Generic.List[string]
    $omitted = New-Object System.Collections.Generic.List[string]

    try {
        $scriptHash = if ($PSCommandPath -and (Test-Path $PSCommandPath)) { (Get-FileHash -Algorithm SHA256 -Path $PSCommandPath).Hash } else { "UNKNOWN" }

        $header = New-Object System.Text.StringBuilder
        $null = $header.AppendLine("=== SPACE TRADE EMPIRE PROJECT DUMP ===")
        $null = $header.AppendLine("format_version: $DevToolVersion")
        $null = $header.AppendLine("dump_timestamp: $(Get-Date -Format o)")
        $null = $header.AppendLine("project_root: $($ProjectRoot.Path)")
        $null = $header.AppendLine("devtool_path: $PSCommandPath")
        $null = $header.AppendLine("devtool_sha256: $scriptHash")
        $null = $header.AppendLine("max_inline_bytes: $MaxInlineBytes")
        $null = $header.AppendLine()

        # Project tree
        $null = $header.AppendLine("=== LIVE PROJECT STRUCTURE (SYSTEM FOLDERS HIDDEN) ===")
        $treeLines = Build-TreeListing -rootPath $ProjectRoot -treeExclusionRegex $TreeExcludeRegex
        foreach ($line in $treeLines) { $null = $header.AppendLine($line) }
        $null = $header.AppendLine("========================================")
        $null = $header.AppendLine()

        # Third party inventory
        $null = $header.AppendLine("=== THIRD PARTY / EXTERNAL INVENTORY (REFERENCED, NOT INLINED) ===")
        $null = $header.AppendLine("third_party_dirs: " + ($ThirdPartyDirs -join ", "))
        foreach ($d in $ThirdPartyDirs) {
            $p = Join-Path $ProjectRoot $d
            if (Test-Path $p) {
                $inv = Summarize-DirInventory -dirPath $p -label "third_party" -excludeRegexForCounting $HardExcludeRegex
                $null = $header.AppendLine("INVENTORY: $($inv.path)")
                $null = $header.AppendLine("  file_count: $($inv.file_count)")
                $null = $header.AppendLine("  total_bytes: $($inv.total_bytes)")
                $null = $header.AppendLine("  top_extensions: $($inv.top_extensions -join ', ')")
            }
        }
        $null = $header.AppendLine("========================================")
        $null = $header.AppendLine()

        # Discover files
        $allFiles = Get-ChildItem -Path $ProjectRoot -Recurse -File -Force -ErrorAction Stop

        # Always include set
        $alwaysFull = @()
        foreach ($rel in $AlwaysIncludeRel) {
            $fp = Join-Path $ProjectRoot $rel
            if (Test-Path $fp) { $alwaysFull += (Get-Item $fp).FullName }
        }

        $inlineFiles = New-Object System.Collections.Generic.List[System.IO.FileInfo]

        foreach ($f in $allFiles) {
            $full = $f.FullName

            # Exclude system
            $hardExcluded = $false
            foreach ($rx in $HardExcludeRegex) {
                if ($full -match $rx) { $hardExcluded = $true; break }
            }
            if ($hardExcluded) { continue }

            # Never inline the dump itself (avoids self-omission noise)
            if ($full -ieq $ContextFile) { continue }

            # Exclude third party from inline
            if (Is-UnderAnyDir -fullPath $full -dirNames $ThirdPartyDirs) { continue }

            $ext = [IO.Path]::GetExtension($f.Name).ToLowerInvariant()

            if ($alwaysFull -contains $full) {
                $inlineFiles.Add($f)
                continue
            }

            if ($IncludeExtensions -contains $ext) {
                $inlineFiles.Add($f)
            }
        }

        $inlineFilesSorted = $inlineFiles | Sort-Object -Property FullName

        # Build dump
        $sb = New-Object System.Text.StringBuilder
        $null = $sb.Append($header.ToString())

        $null = $sb.AppendLine("=== INLINE FILE MANIFEST (NON-3RD-PARTY) ===")
        $null = $sb.AppendLine("inline_file_count: $($inlineFilesSorted.Count)")
        $null = $sb.AppendLine("===========================================")
        $null = $sb.AppendLine()

        $totalInlineBytes = 0
        $fileIndex = 0

        foreach ($file in $inlineFilesSorted) {
            $fileIndex++
            $relPath = Normalize-RelPath $file.FullName
            $size = $file.Length
            $hash = Get-Sha256 $file.FullName

            $null = $sb.AppendLine("<<< BEGIN FILE >>>")
            $null = $sb.AppendLine("index: $fileIndex")
            $null = $sb.AppendLine("path: $relPath")
            $null = $sb.AppendLine("bytes: $size")
            $null = $sb.AppendLine("sha256: $hash")
            $null = $sb.AppendLine("<<< CONTENT >>>")

            if ($size -gt $MaxInlineBytes) {
                $omitted.Add("OMITTED_TOO_LARGE|$relPath|bytes=$size|sha256=$hash")
                $null = $sb.AppendLine("[OMITTED: file exceeds max_inline_bytes. See OMITTED FILES section.]")
                $null = $sb.AppendLine("<<< END FILE >>>")
                $null = $sb.AppendLine()
                continue
            }

            $text = Try-ReadText $file.FullName
            if ($null -eq $text) {
                $errors.Add("READ_ERROR|$relPath|bytes=$size|sha256=$hash")
                $null = $sb.AppendLine("[ERROR: could not read file as text. See ERRORS section.]")
                $null = $sb.AppendLine("<<< END FILE >>>")
                $null = $sb.AppendLine()
                continue
            }

            $null = $sb.AppendLine($text)
            if (-not $text.EndsWith("`n")) { $null = $sb.AppendLine() }
            $null = $sb.AppendLine("<<< END FILE >>>")
            $null = $sb.AppendLine()

            $totalInlineBytes += $size
        }

        $null = $sb.AppendLine("=== OMITTED FILES (NOT INLINED) ===")
        $null = $sb.AppendLine("omitted_count: $($omitted.Count)")
        foreach ($o in $omitted) { $null = $sb.AppendLine($o) }
        $null = $sb.AppendLine("===================================")
        $null = $sb.AppendLine()

        $null = $sb.AppendLine("=== ERRORS ===")
        $null = $sb.AppendLine("error_count: $($errors.Count)")
        foreach ($e in $errors) { $null = $sb.AppendLine($e) }
        $null = $sb.AppendLine("==============")
        $null = $sb.AppendLine()

        $null = $sb.AppendLine("=== DUMP SUMMARY ===")
        $null = $sb.AppendLine("inline_file_count: $($inlineFilesSorted.Count)")
        $null = $sb.AppendLine("inline_total_bytes: $totalInlineBytes")
        $null = $sb.AppendLine("third_party_dirs_referenced: " + ($ThirdPartyDirs -join ", "))
        $null = $sb.AppendLine("max_inline_bytes: $MaxInlineBytes")
        $null = $sb.AppendLine("====================")
        $null = $sb.AppendLine()
        $null = $sb.AppendLine("=== END OF DUMP ===")

        Set-Content -Path $ContextFile -Value $sb.ToString() -Encoding UTF8 -ErrorAction Stop

        Log-Output "SUCCESS: Context refreshed."
        Log-Output "Inline files: $($inlineFilesSorted.Count) | Inline bytes: $totalInlineBytes | Omitted: $($omitted.Count) | Errors: $($errors.Count)"
        Log-Output "Dump: $ContextFile"
        Log-Output "Script: $PSCommandPath"
    }
    catch {
        Log-Output "ERROR: $($_.Exception.Message)"
    }
}

function Run-GitSave {
    Log-Output ">>> EXEC: GIT SNAPSHOT"
    git add .
    $res = git commit -m "Manual Save Point $((Get-Date).ToString('o'))" 2>&1
    Log-Output $res

    # Savepoint tag used by HARD RESET
    git tag -f ste_savepoint 2>&1 | Out-Null
    Log-Output "Savepoint updated: ste_savepoint"

    Log-Output "State Saved."
}

function Run-GitReset {
    $confirm = [System.Windows.Forms.MessageBox]::Show(
        "Reset tracked files back to the last SAVE (ste_savepoint)?",
        "DANGER ZONE",
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Warning
    )

    if ($confirm -ne "Yes") { return }

    Log-Output ">>> EXEC: HARD RESET (to ste_savepoint)"

    $tag = (git rev-parse -q --verify ste_savepoint 2>$null)
    if (-not $tag) {
        Log-Output "ERROR: ste_savepoint not found. Click SAVE STATE once to create it."
        return
    }

    git reset --hard ste_savepoint 2>&1 | Out-Null
    Log-Output "Tracked files reset to ste_savepoint."

    $confirmClean = [System.Windows.Forms.MessageBox]::Show(
        "Also delete ALL untracked (non-ignored) files and folders (git clean -fd)?",
        "DELETE UNTRACKED FILES",
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Warning
    )

    if ($confirmClean -eq "Yes") {
        git clean -fd 2>&1 | Out-Null
        Log-Output "Untracked files deleted."
    } else {
        Log-Output "Untracked files preserved."
    }

    Log-Output "SUCCESS: Reset complete."
}

# --- UI COMPONENTS ---

$lblHeader = New-Object System.Windows.Forms.Label
$lblHeader.Text = "COMMAND DECK"
$lblHeader.Location = New-Object System.Drawing.Point(20, 10)
$lblHeader.Font = $fontHeader
$lblHeader.AutoSize = $true
$form.Controls.Add($lblHeader)

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

$form.Add_Shown({
    $form.Activate()
    Log-Output "DevTool path: $PSCommandPath"
})

[void] $form.ShowDialog()

).Path } catch { <#
SPACE TRADE EMPIRE - MISSION CONTROL (v5.0)

What changed vs v4.2
- Keeps all existing buttons + behavior (tests, save, reset, GUI)
- Context dump now:
  - Inlines first-party: .gd .tscn .shader .ps1 project.godot export_presets.cfg .tres .res .cfg .ini .md .txt .json .csv .yml .yaml
  - References 3rd party dirs (inventory + appears in tree) but does NOT inline their contents
  - Adds per-file bytes + SHA256 + deterministic ordering
  - Adds omitted/errors lists + END OF DUMP marker
  - Excludes editor/system junk: .git .import .godot
  - Excludes the dump file itself from being re-inlined
- Adds strong provenance: script path + script hash written into the dump header

How to install correctly (do this exactly)
1) Save THIS file as: C:\Users\marsh\Documents\Space Trade Empire\DevTool.ps1
2) In PowerShell, verify it is v5.0:
   Select-String -Path .\DevTool.ps1 -Pattern "v4\.2","v5\.0" | ft LineNumber,Line -AutoSize
   (should show v5.0, nothing for v4.2)
3) Run it:
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\DevTool.ps1

If you still see v4.2 after that, you did not overwrite the right file.
#>

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# --- CONFIGURATION ---
$ProjectRoot = Get-Location
$ContextFile = Join-Path (Join-Path $ProjectRoot "_scratch") "_FullProjectContext.txt"
Assert-SafeOutputPath $ContextFile
$TestScene   = "scenes/tests/test_economy_core.tscn"
$ConfigPath  = Join-Path $ProjectRoot "godot_path.cfg"
$DevToolVersion = "5.0"

# --- GUI SETUP ---
$form = New-Object System.Windows.Forms.Form
$form.Text = "STE :: Mission Control v$DevToolVersion"
$form.Size = New-Object System.Drawing.Size(450, 520)
$form.StartPosition = "CenterScreen"
$form.BackColor = "#1e1e1e"
$form.ForeColor = "#ffffff"

$fontHeader = New-Object System.Drawing.Font("Consolas", 12, [System.Drawing.FontStyle]::Bold)
$fontNormal = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$fontLog    = New-Object System.Drawing.Font("Consolas", 9)

# Console Output (declared early so Log-Output can be used anywhere)
$txtOutput = New-Object System.Windows.Forms.TextBox
$txtOutput.Location = New-Object System.Drawing.Point(20, 210)
$txtOutput.Size = New-Object System.Drawing.Size(400, 250)
$txtOutput.Multiline = $true
$txtOutput.ScrollBars = "Vertical"
$txtOutput.BackColor = "#000000"
$txtOutput.ForeColor = "#00ff00"
$txtOutput.Font = $fontLog
$txtOutput.Text = "Mission Control v$DevToolVersion Online...`r`n"
$form.Controls.Add($txtOutput)

function Log-Output($message) {
    $txtOutput.AppendText("[$((Get-Date).ToString('HH:mm:ss'))] $message`r`n")
    $txtOutput.ScrollToCaret()
}

function Get-GodotExe {
    $exePath = $null

    if (Test-Path $ConfigPath) {
        $raw = Get-Content $ConfigPath -Raw
        if ($raw) { $exePath = $raw.Trim() }
    }

    if ($exePath -and (Test-Path $exePath)) {
        return $exePath
    }

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

    $godotRaw = Get-GodotExe
    $godotExe = @($godotRaw)[-1]

    if (-not $godotExe -or -not (Test-Path $godotExe)) {
        Log-Output "ERROR: Godot path missing or invalid."
        return
    }

    Log-Output "Target: $godotExe"

    try {
        Log-Output "Running simulation..."
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

# ----------------------------
# CONTEXT DUMP HELPERS
# ----------------------------

function Normalize-RelPath([string]$fullPath) {
    $rp = $fullPath.Replace($ProjectRoot.Path, "")
    $rp = $rp.TrimStart("\","/")
    return $rp -replace "/", "\"
}

function Is-UnderAnyDir([string]$fullPath, [string[]]$dirNames) {
    foreach ($d in $dirNames) {
        if ($fullPath -match [regex]::Escape("\$d\") -or $fullPath.EndsWith("\$d")) { return $true }
        if ($fullPath -match [regex]::Escape("/$d/") -or $fullPath.EndsWith("/$d")) { return $true }
    }
    return $false
}

function Get-Sha256([string]$path) {
    try {
        return (Get-FileHash -Algorithm SHA256 -Path $path).Hash
    } catch {
        return "HASH_ERROR"
    }
}

function Try-ReadText([string]$path) {
    try {
        return Get-Content -Path $path -Raw -ErrorAction Stop
    } catch {
        return $null
    }
}

function Build-TreeListing([string]$rootPath, [string[]]$treeExclusionRegex) {
    $lines = New-Object System.Collections.Generic.List[string]
    try {
        $items = Get-ChildItem -Path $rootPath -Recurse -Force -ErrorAction Stop
        foreach ($it in $items) {
            $full = $it.FullName
            $excluded = $false
            foreach ($rx in $treeExclusionRegex) {
                if ($full -match $rx) { $excluded = $true; break }
            }
            if ($excluded) { continue }

            $rel = Normalize-RelPath $full
            $depth = ($rel.Split('\').Count - 1)
            $indent = "  " * $depth
            if ($it.PSIsContainer) {
                $lines.Add("+ $indent[$($it.Name)]")
            } else {
                $lines.Add("- $indent$($it.Name)")
            }
        }
    } catch {
        $lines.Add("TREE_ERROR: $($_.Exception.Message)")
    }
    return $lines
}

function Summarize-DirInventory([string]$dirPath, [string]$label, [string[]]$excludeRegexForCounting) {
    $summary = [ordered]@{
        label = $label
        path  = (Normalize-RelPath $dirPath)
        file_count = 0
        total_bytes = 0
        top_extensions = @()
    }

    if (-not (Test-Path $dirPath)) { return $summary }

    $extCount = @{}
    try {
        $files = Get-ChildItem -Path $dirPath -Recurse -File -Force -ErrorAction Stop | Where-Object {
            $ok = $true
            foreach ($rx in $excludeRegexForCounting) {
                if ($_.FullName -match $rx) { $ok = $false; break }
            }
            $ok
        }
        $summary.file_count = $files.Count
        $summary.total_bytes = ($files | Measure-Object -Property Length -Sum).Sum

        foreach ($f in $files) {
            $e = [IO.Path]::GetExtension($f.Name).ToLowerInvariant()
            if (-not $e) { $e = "<none>" }
            if (-not $extCount.ContainsKey($e)) { $extCount[$e] = 0 }
            $extCount[$e]++
        }

        $top = $extCount.GetEnumerator() | Sort-Object -Property Value -Descending | Select-Object -First 12
        $summary.top_extensions = $top | ForEach-Object { "$($_.Key):$($_.Value)" }
    } catch {
        $summary.top_extensions = @("INVENTORY_ERROR:$($_.Exception.Message)")
    }

    return $summary
}

function Run-ContextGen {
    Log-Output ">>> EXEC: FULL CONTEXT DUMP (SCOPED, VERIFIED)"

    # Text extensions we embed
    $IncludeExtensions = @(
        ".gd", ".tscn", ".shader", ".ps1",
        ".godot",
        ".tres", ".res",
        ".cfg", ".ini",
        ".md", ".txt",
        ".json", ".csv", ".tsv",
        ".yml", ".yaml"
    )

    # Always include these if present
    $AlwaysIncludeRel = @(
        "project.godot",
        "export_presets.cfg",
        "_PROJECT_CONTEXT.md"
    )

    # Exclude from embedded text dump (but still referenced in inventory + tree)
    $ThirdPartyDirs = @("addons", "third_party", "vendor", "externals", "external")

    # Exclude from everything (tree + inline + inventory)
    $HardExcludeRegex = @(
        "\\.git\\", "/\.git/",
        "\\.import\\", "/\.import/",
        "\\.godot\\", "/\.godot/"
    )

    # For TREE view, hide only system folders. Keep third party visible.
    $TreeExcludeRegex = $HardExcludeRegex

    # Very high cap to avoid surprising first-party omissions.
    # If you want truly unlimited, set this to [int64]::MaxValue
    $MaxInlineBytes = 50MB

    $errors = New-Object System.Collections.Generic.List[string]
    $omitted = New-Object System.Collections.Generic.List[string]

    try {
        $scriptHash = if ($PSCommandPath -and (Test-Path $PSCommandPath)) { (Get-FileHash -Algorithm SHA256 -Path $PSCommandPath).Hash } else { "UNKNOWN" }

        $header = New-Object System.Text.StringBuilder
        $null = $header.AppendLine("=== SPACE TRADE EMPIRE PROJECT DUMP ===")
        $null = $header.AppendLine("format_version: $DevToolVersion")
        $null = $header.AppendLine("dump_timestamp: $(Get-Date -Format o)")
        $null = $header.AppendLine("project_root: $($ProjectRoot.Path)")
        $null = $header.AppendLine("devtool_path: $PSCommandPath")
        $null = $header.AppendLine("devtool_sha256: $scriptHash")
        $null = $header.AppendLine("max_inline_bytes: $MaxInlineBytes")
        $null = $header.AppendLine()

        # Project tree
        $null = $header.AppendLine("=== LIVE PROJECT STRUCTURE (SYSTEM FOLDERS HIDDEN) ===")
        $treeLines = Build-TreeListing -rootPath $ProjectRoot -treeExclusionRegex $TreeExcludeRegex
        foreach ($line in $treeLines) { $null = $header.AppendLine($line) }
        $null = $header.AppendLine("========================================")
        $null = $header.AppendLine()

        # Third party inventory
        $null = $header.AppendLine("=== THIRD PARTY / EXTERNAL INVENTORY (REFERENCED, NOT INLINED) ===")
        $null = $header.AppendLine("third_party_dirs: " + ($ThirdPartyDirs -join ", "))
        foreach ($d in $ThirdPartyDirs) {
            $p = Join-Path $ProjectRoot $d
            if (Test-Path $p) {
                $inv = Summarize-DirInventory -dirPath $p -label "third_party" -excludeRegexForCounting $HardExcludeRegex
                $null = $header.AppendLine("INVENTORY: $($inv.path)")
                $null = $header.AppendLine("  file_count: $($inv.file_count)")
                $null = $header.AppendLine("  total_bytes: $($inv.total_bytes)")
                $null = $header.AppendLine("  top_extensions: $($inv.top_extensions -join ', ')")
            }
        }
        $null = $header.AppendLine("========================================")
        $null = $header.AppendLine()

        # Discover files
        $allFiles = Get-ChildItem -Path $ProjectRoot -Recurse -File -Force -ErrorAction Stop

        # Always include set
        $alwaysFull = @()
        foreach ($rel in $AlwaysIncludeRel) {
            $fp = Join-Path $ProjectRoot $rel
            if (Test-Path $fp) { $alwaysFull += (Get-Item $fp).FullName }
        }

        $inlineFiles = New-Object System.Collections.Generic.List[System.IO.FileInfo]

        foreach ($f in $allFiles) {
            $full = $f.FullName

            # Exclude system
            $hardExcluded = $false
            foreach ($rx in $HardExcludeRegex) {
                if ($full -match $rx) { $hardExcluded = $true; break }
            }
            if ($hardExcluded) { continue }

            # Never inline the dump itself (avoids self-omission noise)
            if ($full -ieq $ContextFile) { continue }

            # Exclude third party from inline
            if (Is-UnderAnyDir -fullPath $full -dirNames $ThirdPartyDirs) { continue }

            $ext = [IO.Path]::GetExtension($f.Name).ToLowerInvariant()

            if ($alwaysFull -contains $full) {
                $inlineFiles.Add($f)
                continue
            }

            if ($IncludeExtensions -contains $ext) {
                $inlineFiles.Add($f)
            }
        }

        $inlineFilesSorted = $inlineFiles | Sort-Object -Property FullName

        # Build dump
        $sb = New-Object System.Text.StringBuilder
        $null = $sb.Append($header.ToString())

        $null = $sb.AppendLine("=== INLINE FILE MANIFEST (NON-3RD-PARTY) ===")
        $null = $sb.AppendLine("inline_file_count: $($inlineFilesSorted.Count)")
        $null = $sb.AppendLine("===========================================")
        $null = $sb.AppendLine()

        $totalInlineBytes = 0
        $fileIndex = 0

        foreach ($file in $inlineFilesSorted) {
            $fileIndex++
            $relPath = Normalize-RelPath $file.FullName
            $size = $file.Length
            $hash = Get-Sha256 $file.FullName

            $null = $sb.AppendLine("<<< BEGIN FILE >>>")
            $null = $sb.AppendLine("index: $fileIndex")
            $null = $sb.AppendLine("path: $relPath")
            $null = $sb.AppendLine("bytes: $size")
            $null = $sb.AppendLine("sha256: $hash")
            $null = $sb.AppendLine("<<< CONTENT >>>")

            if ($size -gt $MaxInlineBytes) {
                $omitted.Add("OMITTED_TOO_LARGE|$relPath|bytes=$size|sha256=$hash")
                $null = $sb.AppendLine("[OMITTED: file exceeds max_inline_bytes. See OMITTED FILES section.]")
                $null = $sb.AppendLine("<<< END FILE >>>")
                $null = $sb.AppendLine()
                continue
            }

            $text = Try-ReadText $file.FullName
            if ($null -eq $text) {
                $errors.Add("READ_ERROR|$relPath|bytes=$size|sha256=$hash")
                $null = $sb.AppendLine("[ERROR: could not read file as text. See ERRORS section.]")
                $null = $sb.AppendLine("<<< END FILE >>>")
                $null = $sb.AppendLine()
                continue
            }

            $null = $sb.AppendLine($text)
            if (-not $text.EndsWith("`n")) { $null = $sb.AppendLine() }
            $null = $sb.AppendLine("<<< END FILE >>>")
            $null = $sb.AppendLine()

            $totalInlineBytes += $size
        }

        $null = $sb.AppendLine("=== OMITTED FILES (NOT INLINED) ===")
        $null = $sb.AppendLine("omitted_count: $($omitted.Count)")
        foreach ($o in $omitted) { $null = $sb.AppendLine($o) }
        $null = $sb.AppendLine("===================================")
        $null = $sb.AppendLine()

        $null = $sb.AppendLine("=== ERRORS ===")
        $null = $sb.AppendLine("error_count: $($errors.Count)")
        foreach ($e in $errors) { $null = $sb.AppendLine($e) }
        $null = $sb.AppendLine("==============")
        $null = $sb.AppendLine()

        $null = $sb.AppendLine("=== DUMP SUMMARY ===")
        $null = $sb.AppendLine("inline_file_count: $($inlineFilesSorted.Count)")
        $null = $sb.AppendLine("inline_total_bytes: $totalInlineBytes")
        $null = $sb.AppendLine("third_party_dirs_referenced: " + ($ThirdPartyDirs -join ", "))
        $null = $sb.AppendLine("max_inline_bytes: $MaxInlineBytes")
        $null = $sb.AppendLine("====================")
        $null = $sb.AppendLine()
        $null = $sb.AppendLine("=== END OF DUMP ===")

        Set-Content -Path $ContextFile -Value $sb.ToString() -Encoding UTF8 -ErrorAction Stop

        Log-Output "SUCCESS: Context refreshed."
        Log-Output "Inline files: $($inlineFilesSorted.Count) | Inline bytes: $totalInlineBytes | Omitted: $($omitted.Count) | Errors: $($errors.Count)"
        Log-Output "Dump: $ContextFile"
        Log-Output "Script: $PSCommandPath"
    }
    catch {
        Log-Output "ERROR: $($_.Exception.Message)"
    }
}

function Run-GitSave {
    Log-Output ">>> EXEC: GIT SNAPSHOT"
    git add .
    $res = git commit -m "Manual Save Point $((Get-Date).ToString('o'))" 2>&1
    Log-Output $res

    # Savepoint tag used by HARD RESET
    git tag -f ste_savepoint 2>&1 | Out-Null
    Log-Output "Savepoint updated: ste_savepoint"

    Log-Output "State Saved."
}

function Run-GitReset {
    $confirm = [System.Windows.Forms.MessageBox]::Show(
        "Reset tracked files back to the last SAVE (ste_savepoint)?",
        "DANGER ZONE",
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Warning
    )

    if ($confirm -ne "Yes") { return }

    Log-Output ">>> EXEC: HARD RESET (to ste_savepoint)"

    $tag = (git rev-parse -q --verify ste_savepoint 2>$null)
    if (-not $tag) {
        Log-Output "ERROR: ste_savepoint not found. Click SAVE STATE once to create it."
        return
    }

    git reset --hard ste_savepoint 2>&1 | Out-Null
    Log-Output "Tracked files reset to ste_savepoint."

    $confirmClean = [System.Windows.Forms.MessageBox]::Show(
        "Also delete ALL untracked (non-ignored) files and folders (git clean -fd)?",
        "DELETE UNTRACKED FILES",
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Warning
    )

    if ($confirmClean -eq "Yes") {
        git clean -fd 2>&1 | Out-Null
        Log-Output "Untracked files deleted."
    } else {
        Log-Output "Untracked files preserved."
    }

    Log-Output "SUCCESS: Reset complete."
}

# --- UI COMPONENTS ---

$lblHeader = New-Object System.Windows.Forms.Label
$lblHeader.Text = "COMMAND DECK"
$lblHeader.Location = New-Object System.Drawing.Point(20, 10)
$lblHeader.Font = $fontHeader
$lblHeader.AutoSize = $true
$form.Controls.Add($lblHeader)

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

$form.Add_Shown({
    $form.Activate()
    Log-Output "DevTool path: $PSCommandPath"
})

[void] $form.ShowDialog()

 } }

    foreach ($d in $deny) {
        if ($full -ieq $d) {
            throw "Refusing to write to protected file: $full"
        }
    }

    $scratchRoot = (Resolve-Path (Join-Path $ProjectRoot "_scratch")).Path
    if ($full -notlike "$scratchRoot*") {
        throw "Refusing to write outside _scratch: $full"
    }
}
# --- GUI SETUP ---
$form = New-Object System.Windows.Forms.Form
$form.Text = "STE :: Mission Control v$DevToolVersion"
$form.Size = New-Object System.Drawing.Size(450, 520)
$form.StartPosition = "CenterScreen"
$form.BackColor = "#1e1e1e"
$form.ForeColor = "#ffffff"

$fontHeader = New-Object System.Drawing.Font("Consolas", 12, [System.Drawing.FontStyle]::Bold)
$fontNormal = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$fontLog    = New-Object System.Drawing.Font("Consolas", 9)

# Console Output (declared early so Log-Output can be used anywhere)
$txtOutput = New-Object System.Windows.Forms.TextBox
$txtOutput.Location = New-Object System.Drawing.Point(20, 210)
$txtOutput.Size = New-Object System.Drawing.Size(400, 250)
$txtOutput.Multiline = $true
$txtOutput.ScrollBars = "Vertical"
$txtOutput.BackColor = "#000000"
$txtOutput.ForeColor = "#00ff00"
$txtOutput.Font = $fontLog
$txtOutput.Text = "Mission Control v$DevToolVersion Online...`r`n"
$form.Controls.Add($txtOutput)

function Log-Output($message) {
    $txtOutput.AppendText("[$((Get-Date).ToString('HH:mm:ss'))] $message`r`n")
    $txtOutput.ScrollToCaret()
}

function Get-GodotExe {
    $exePath = $null

    if (Test-Path $ConfigPath) {
        $raw = Get-Content $ConfigPath -Raw
        if ($raw) { $exePath = $raw.Trim() }
    }

    if ($exePath -and (Test-Path $exePath)) {
        return $exePath
    }

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

    $godotRaw = Get-GodotExe
    $godotExe = @($godotRaw)[-1]

    if (-not $godotExe -or -not (Test-Path $godotExe)) {
        Log-Output "ERROR: Godot path missing or invalid."
        return
    }

    Log-Output "Target: $godotExe"

    try {
        Log-Output "Running simulation..."
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

# ----------------------------
# CONTEXT DUMP HELPERS
# ----------------------------

function Normalize-RelPath([string]$fullPath) {
    $rp = $fullPath.Replace($ProjectRoot.Path, "")
    $rp = $rp.TrimStart("\","/")
    return $rp -replace "/", "\"
}

function Is-UnderAnyDir([string]$fullPath, [string[]]$dirNames) {
    foreach ($d in $dirNames) {
        if ($fullPath -match [regex]::Escape("\$d\") -or $fullPath.EndsWith("\$d")) { return $true }
        if ($fullPath -match [regex]::Escape("/$d/") -or $fullPath.EndsWith("/$d")) { return $true }
    }
    return $false
}

function Get-Sha256([string]$path) {
    try {
        return (Get-FileHash -Algorithm SHA256 -Path $path).Hash
    } catch {
        return "HASH_ERROR"
    }
}

function Try-ReadText([string]$path) {
    try {
        return Get-Content -Path $path -Raw -ErrorAction Stop
    } catch {
        return $null
    }
}

function Build-TreeListing([string]$rootPath, [string[]]$treeExclusionRegex) {
    $lines = New-Object System.Collections.Generic.List[string]
    try {
        $items = Get-ChildItem -Path $rootPath -Recurse -Force -ErrorAction Stop
        foreach ($it in $items) {
            $full = $it.FullName
            $excluded = $false
            foreach ($rx in $treeExclusionRegex) {
                if ($full -match $rx) { $excluded = $true; break }
            }
            if ($excluded) { continue }

            $rel = Normalize-RelPath $full
            $depth = ($rel.Split('\').Count - 1)
            $indent = "  " * $depth
            if ($it.PSIsContainer) {
                $lines.Add("+ $indent[$($it.Name)]")
            } else {
                $lines.Add("- $indent$($it.Name)")
            }
        }
    } catch {
        $lines.Add("TREE_ERROR: $($_.Exception.Message)")
    }
    return $lines
}

function Summarize-DirInventory([string]$dirPath, [string]$label, [string[]]$excludeRegexForCounting) {
    $summary = [ordered]@{
        label = $label
        path  = (Normalize-RelPath $dirPath)
        file_count = 0
        total_bytes = 0
        top_extensions = @()
    }

    if (-not (Test-Path $dirPath)) { return $summary }

    $extCount = @{}
    try {
        $files = Get-ChildItem -Path $dirPath -Recurse -File -Force -ErrorAction Stop | Where-Object {
            $ok = $true
            foreach ($rx in $excludeRegexForCounting) {
                if ($_.FullName -match $rx) { $ok = $false; break }
            }
            $ok
        }
        $summary.file_count = $files.Count
        $summary.total_bytes = ($files | Measure-Object -Property Length -Sum).Sum

        foreach ($f in $files) {
            $e = [IO.Path]::GetExtension($f.Name).ToLowerInvariant()
            if (-not $e) { $e = "<none>" }
            if (-not $extCount.ContainsKey($e)) { $extCount[$e] = 0 }
            $extCount[$e]++
        }

        $top = $extCount.GetEnumerator() | Sort-Object -Property Value -Descending | Select-Object -First 12
        $summary.top_extensions = $top | ForEach-Object { "$($_.Key):$($_.Value)" }
    } catch {
        $summary.top_extensions = @("INVENTORY_ERROR:$($_.Exception.Message)")
    }

    return $summary
}

function Run-ContextGen {
    Log-Output ">>> EXEC: FULL CONTEXT DUMP (SCOPED, VERIFIED)"

    # Text extensions we embed
    $IncludeExtensions = @(
        ".gd", ".tscn", ".shader", ".ps1",
        ".godot",
        ".tres", ".res",
        ".cfg", ".ini",
        ".md", ".txt",
        ".json", ".csv", ".tsv",
        ".yml", ".yaml"
    )

    # Always include these if present
    $AlwaysIncludeRel = @(
        "project.godot",
        "export_presets.cfg",
        "_PROJECT_CONTEXT.md"
    )

    # Exclude from embedded text dump (but still referenced in inventory + tree)
    $ThirdPartyDirs = @("addons", "third_party", "vendor", "externals", "external")

    # Exclude from everything (tree + inline + inventory)
    $HardExcludeRegex = @(
        "\\.git\\", "/\.git/",
        "\\.import\\", "/\.import/",
        "\\.godot\\", "/\.godot/"
    )

    # For TREE view, hide only system folders. Keep third party visible.
    $TreeExcludeRegex = $HardExcludeRegex

    # Very high cap to avoid surprising first-party omissions.
    # If you want truly unlimited, set this to [int64]::MaxValue
    $MaxInlineBytes = 50MB

    $errors = New-Object System.Collections.Generic.List[string]
    $omitted = New-Object System.Collections.Generic.List[string]

    try {
        $scriptHash = if ($PSCommandPath -and (Test-Path $PSCommandPath)) { (Get-FileHash -Algorithm SHA256 -Path $PSCommandPath).Hash } else { "UNKNOWN" }

        $header = New-Object System.Text.StringBuilder
        $null = $header.AppendLine("=== SPACE TRADE EMPIRE PROJECT DUMP ===")
        $null = $header.AppendLine("format_version: $DevToolVersion")
        $null = $header.AppendLine("dump_timestamp: $(Get-Date -Format o)")
        $null = $header.AppendLine("project_root: $($ProjectRoot.Path)")
        $null = $header.AppendLine("devtool_path: $PSCommandPath")
        $null = $header.AppendLine("devtool_sha256: $scriptHash")
        $null = $header.AppendLine("max_inline_bytes: $MaxInlineBytes")
        $null = $header.AppendLine()

        # Project tree
        $null = $header.AppendLine("=== LIVE PROJECT STRUCTURE (SYSTEM FOLDERS HIDDEN) ===")
        $treeLines = Build-TreeListing -rootPath $ProjectRoot -treeExclusionRegex $TreeExcludeRegex
        foreach ($line in $treeLines) { $null = $header.AppendLine($line) }
        $null = $header.AppendLine("========================================")
        $null = $header.AppendLine()

        # Third party inventory
        $null = $header.AppendLine("=== THIRD PARTY / EXTERNAL INVENTORY (REFERENCED, NOT INLINED) ===")
        $null = $header.AppendLine("third_party_dirs: " + ($ThirdPartyDirs -join ", "))
        foreach ($d in $ThirdPartyDirs) {
            $p = Join-Path $ProjectRoot $d
            if (Test-Path $p) {
                $inv = Summarize-DirInventory -dirPath $p -label "third_party" -excludeRegexForCounting $HardExcludeRegex
                $null = $header.AppendLine("INVENTORY: $($inv.path)")
                $null = $header.AppendLine("  file_count: $($inv.file_count)")
                $null = $header.AppendLine("  total_bytes: $($inv.total_bytes)")
                $null = $header.AppendLine("  top_extensions: $($inv.top_extensions -join ', ')")
            }
        }
        $null = $header.AppendLine("========================================")
        $null = $header.AppendLine()

        # Discover files
        $allFiles = Get-ChildItem -Path $ProjectRoot -Recurse -File -Force -ErrorAction Stop

        # Always include set
        $alwaysFull = @()
        foreach ($rel in $AlwaysIncludeRel) {
            $fp = Join-Path $ProjectRoot $rel
            if (Test-Path $fp) { $alwaysFull += (Get-Item $fp).FullName }
        }

        $inlineFiles = New-Object System.Collections.Generic.List[System.IO.FileInfo]

        foreach ($f in $allFiles) {
            $full = $f.FullName

            # Exclude system
            $hardExcluded = $false
            foreach ($rx in $HardExcludeRegex) {
                if ($full -match $rx) { $hardExcluded = $true; break }
            }
            if ($hardExcluded) { continue }

            # Never inline the dump itself (avoids self-omission noise)
            if ($full -ieq $ContextFile) { continue }

            # Exclude third party from inline
            if (Is-UnderAnyDir -fullPath $full -dirNames $ThirdPartyDirs) { continue }

            $ext = [IO.Path]::GetExtension($f.Name).ToLowerInvariant()

            if ($alwaysFull -contains $full) {
                $inlineFiles.Add($f)
                continue
            }

            if ($IncludeExtensions -contains $ext) {
                $inlineFiles.Add($f)
            }
        }

        $inlineFilesSorted = $inlineFiles | Sort-Object -Property FullName

        # Build dump
        $sb = New-Object System.Text.StringBuilder
        $null = $sb.Append($header.ToString())

        $null = $sb.AppendLine("=== INLINE FILE MANIFEST (NON-3RD-PARTY) ===")
        $null = $sb.AppendLine("inline_file_count: $($inlineFilesSorted.Count)")
        $null = $sb.AppendLine("===========================================")
        $null = $sb.AppendLine()

        $totalInlineBytes = 0
        $fileIndex = 0

        foreach ($file in $inlineFilesSorted) {
            $fileIndex++
            $relPath = Normalize-RelPath $file.FullName
            $size = $file.Length
            $hash = Get-Sha256 $file.FullName

            $null = $sb.AppendLine("<<< BEGIN FILE >>>")
            $null = $sb.AppendLine("index: $fileIndex")
            $null = $sb.AppendLine("path: $relPath")
            $null = $sb.AppendLine("bytes: $size")
            $null = $sb.AppendLine("sha256: $hash")
            $null = $sb.AppendLine("<<< CONTENT >>>")

            if ($size -gt $MaxInlineBytes) {
                $omitted.Add("OMITTED_TOO_LARGE|$relPath|bytes=$size|sha256=$hash")
                $null = $sb.AppendLine("[OMITTED: file exceeds max_inline_bytes. See OMITTED FILES section.]")
                $null = $sb.AppendLine("<<< END FILE >>>")
                $null = $sb.AppendLine()
                continue
            }

            $text = Try-ReadText $file.FullName
            if ($null -eq $text) {
                $errors.Add("READ_ERROR|$relPath|bytes=$size|sha256=$hash")
                $null = $sb.AppendLine("[ERROR: could not read file as text. See ERRORS section.]")
                $null = $sb.AppendLine("<<< END FILE >>>")
                $null = $sb.AppendLine()
                continue
            }

            $null = $sb.AppendLine($text)
            if (-not $text.EndsWith("`n")) { $null = $sb.AppendLine() }
            $null = $sb.AppendLine("<<< END FILE >>>")
            $null = $sb.AppendLine()

            $totalInlineBytes += $size
        }

        $null = $sb.AppendLine("=== OMITTED FILES (NOT INLINED) ===")
        $null = $sb.AppendLine("omitted_count: $($omitted.Count)")
        foreach ($o in $omitted) { $null = $sb.AppendLine($o) }
        $null = $sb.AppendLine("===================================")
        $null = $sb.AppendLine()

        $null = $sb.AppendLine("=== ERRORS ===")
        $null = $sb.AppendLine("error_count: $($errors.Count)")
        foreach ($e in $errors) { $null = $sb.AppendLine($e) }
        $null = $sb.AppendLine("==============")
        $null = $sb.AppendLine()

        $null = $sb.AppendLine("=== DUMP SUMMARY ===")
        $null = $sb.AppendLine("inline_file_count: $($inlineFilesSorted.Count)")
        $null = $sb.AppendLine("inline_total_bytes: $totalInlineBytes")
        $null = $sb.AppendLine("third_party_dirs_referenced: " + ($ThirdPartyDirs -join ", "))
        $null = $sb.AppendLine("max_inline_bytes: $MaxInlineBytes")
        $null = $sb.AppendLine("====================")
        $null = $sb.AppendLine()
        $null = $sb.AppendLine("=== END OF DUMP ===")

        Set-Content -Path $ContextFile -Value $sb.ToString() -Encoding UTF8 -ErrorAction Stop

        Log-Output "SUCCESS: Context refreshed."
        Log-Output "Inline files: $($inlineFilesSorted.Count) | Inline bytes: $totalInlineBytes | Omitted: $($omitted.Count) | Errors: $($errors.Count)"
        Log-Output "Dump: $ContextFile"
        Log-Output "Script: $PSCommandPath"
    }
    catch {
        Log-Output "ERROR: $($_.Exception.Message)"
    }
}

function Run-GitSave {
    Log-Output ">>> EXEC: GIT SNAPSHOT"
    git add .
    $res = git commit -m "Manual Save Point $((Get-Date).ToString('o'))" 2>&1
    Log-Output $res

    # Savepoint tag used by HARD RESET
    git tag -f ste_savepoint 2>&1 | Out-Null
    Log-Output "Savepoint updated: ste_savepoint"

    Log-Output "State Saved."
}

function Run-GitReset {
    $confirm = [System.Windows.Forms.MessageBox]::Show(
        "Reset tracked files back to the last SAVE (ste_savepoint)?",
        "DANGER ZONE",
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Warning
    )

    if ($confirm -ne "Yes") { return }

    Log-Output ">>> EXEC: HARD RESET (to ste_savepoint)"

    $tag = (git rev-parse -q --verify ste_savepoint 2>$null)
    if (-not $tag) {
        Log-Output "ERROR: ste_savepoint not found. Click SAVE STATE once to create it."
        return
    }

    git reset --hard ste_savepoint 2>&1 | Out-Null
    Log-Output "Tracked files reset to ste_savepoint."

    $confirmClean = [System.Windows.Forms.MessageBox]::Show(
        "Also delete ALL untracked (non-ignored) files and folders (git clean -fd)?",
        "DELETE UNTRACKED FILES",
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Warning
    )

    if ($confirmClean -eq "Yes") {
        git clean -fd 2>&1 | Out-Null
        Log-Output "Untracked files deleted."
    } else {
        Log-Output "Untracked files preserved."
    }

    Log-Output "SUCCESS: Reset complete."
}

# --- UI COMPONENTS ---

$lblHeader = New-Object System.Windows.Forms.Label
$lblHeader.Text = "COMMAND DECK"
$lblHeader.Location = New-Object System.Drawing.Point(20, 10)
$lblHeader.Font = $fontHeader
$lblHeader.AutoSize = $true
$form.Controls.Add($lblHeader)

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

$form.Add_Shown({
    $form.Activate()
    Log-Output "DevTool path: $PSCommandPath"
})

[void] $form.ShowDialog()

