if (-not (Test-Path variable:global:DEVTOOL_HEADLESS)) { $global:DEVTOOL_HEADLESS = $false }

<#








.SYNOPSIS








    SPACE TRADE EMPIRE - MISSION CONTROL (v4.2)








    Fix: Strict Type Casting for Godot Path to prevent array errors.








#>

















if (-not $global:DEVTOOL_HEADLESS) { Add-Type -AssemblyName System.Windows.Forms }








if (-not $global:DEVTOOL_HEADLESS) { Add-Type -AssemblyName System.Drawing }

















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

















$common = Join-Path $PSScriptRoot "scripts\tools\common.ps1"; if (Test-Path -LiteralPath $common) { . $common } else { throw "Missing tools common: $common" }





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

















        # Ensure scratch output directory exists








        $scratchDir = Join-Path $ProjectRoot.Path "_scratch"








        if (-not (Test-Path -LiteralPath $scratchDir)) {








            New-Item -ItemType Directory -Path $scratchDir | Out-Null








        }








        








        $content += "=== LIVE PROJECT STRUCTURE ===`n"








        try {








            $tree = Get-ChildItem -Path $ProjectRoot -Recurse | 








                Where-Object {








                    $p = $_.FullName








                    ($p -notmatch "\\\.godot(\\|$)") -and








                    ($p -notmatch "\\\.git(\\|$)") -and








                    ($p -notmatch "\\\.import(\\|$)") -and








                    ($p -notmatch "\\addons(\\|$)") -and








                    ($p -notmatch "\\_scratch(\\|$)") -and








                    ($p -notmatch "\\\._scratch(\\|$)")








                } |








                ForEach-Object { 








                    $rel = $_.FullName.Replace($ProjectRoot.Path, "")








                    $indent = "  " * ($rel.Split('\').Count - 1)








                    if ($_.PSIsContainer) { "+ $indent[$($_.Name)]" } else { "- $indent$($_.Name)" }








                }








            $content += $tree








        } catch {}








        $content += "`n================================`n"

















        $files = Get-ChildItem -Path $ProjectRoot -Recurse -Include *.gd, *.tscn, *.shader | 








                 Where-Object { $_.FullName -notmatch "\\.git" -and $_.FullName -notmatch "\\.import" -and $_.FullName -notmatch "\\addons\\" -and $_.FullName -notmatch "\\_scratch\\" -and $_.FullName -notmatch "\\\._scratch\\" }

















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








    Log-Output ">>> EXEC: CI/CD ENFORCED GIT SNAPSHOT"








    








    # 1. Identify Modified Godot Scripts








    $modifiedScripts = git diff --name-only | Where-Object { $_ -match "\.gd$" }








    








    # 2. Enforce the Gatekeeper








    if ($modifiedScripts) {








        Log-Output "Scanning modified GDScript assets..."








        foreach ($script in $modifiedScripts) {








            $fullPath = Join-Path $ProjectRoot.Path $script








            Log-Output "Validating: $script"








            








            # Invoke the system CI/CD tool








            Validate-GodotScript $fullPath








            








            # Audit the exit code








            if ($LASTEXITCODE -ne 0) {








                Log-Output "FATAL: Syntax/Indentation error detected in $script."








                Log-Output "ABORT: Commit blocked to prevent technical debt."








                return # Exits the function immediately, preventing the commit.








            }








        }








        Log-Output "PASSED: All modified scripts verified."








    }

















    # 3. Secure the Asset








    git add -u








    $res = git commit -m "chore: state snapshot via Mission Control $(Get-Date -Format 'yyyy-MM-dd HH:mm')" 2>&1








    Log-Output $res








    Log-Output "SUCCESS: Operation verified and baseline secured in Git."








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








if (-not $global:DEVTOOL_HEADLESS) { [void] $form.ShowDialog() }








# ======================================================================


# OVERRIDE: Contract-compliant context dump (last definition wins).


# Writes to _scratch and filters out tooling/artifacts to avoid friction.


# ======================================================================


function Run-ContextGen {


Log-Output ">>> EXEC: SMART CONTEXT DUMP (contract override)"


try {


$repoRoot = Get-RepoRoot


$projectRoot = Get-Item -LiteralPath $repoRoot


$scratch = Ensure-Scratch -RepoRoot $repoRoot


$outPath = Join-Path $scratch "_FullProjectContext.txt"





$content = "=== SPACE TRADE EMPIRE PROJECT DUMP ===`n"


$content += "dump_timestamp: $(Get-Date -Format o)`n"


$content += "repo_root: $repoRoot`n`n"





$content += "=== LIVE PROJECT STRUCTURE (filtered) ===`n"





# Exclude dirs by substring match on full path (fast, deterministic)


$excludeDirRegex = "\\\.godot\\|\\\.git\\|\\\.import\\|\\addons\\|\\_scratch\\|\\\._scratch\\|\\\.vscode\\|\\\.vs\\|\\\.idea\\|\\node_modules\\"





# Exclude file extensions and name patterns


$excludeFileExt = @(".ps1", ".lnk", ".uid")


$excludeNameLike = @(


"*_FullProjectContext*.txt",


"*.bak",


"*.bak.*",


"* - Copy.*",


"temp_validator.gd"


)





# Only include file contents for these extensions (contract-oriented)


$includeContentExt = @(".gd", ".tscn", ".tres", ".md", ".txt", ".json", ".cfg", ".ini", ".yml", ".yaml")





$items = Get-ChildItem -Path $projectRoot.FullName -Recurse -Force |


Where-Object {


$full = $_.FullName


if ($full -match $excludeDirRegex) { return $false }





if ($_.PSIsContainer) { return $true }





$ext = $_.Extension


if ($excludeFileExt -contains $ext) { return $false }





foreach ($pat in $excludeNameLike) {


if ($_.Name -like $pat) { return $false }


}





return $true


} |


Sort-Object FullName





foreach ($it in $items) {


$rel = $it.FullName.Substring($projectRoot.FullName.Length).TrimStart('\','/')


$depth = 0


if ($rel) { $depth = ($rel -split '[\\/]').Length - 1 }


$indent = "  " * [Math]::Max(0, $depth)


if ($it.PSIsContainer) {


$content += "$indent$rel\`n"


} else {


$content += "$indent$rel`n"


}


}





$content += "`n=== PROJECT CONTEXT (authoritative) ===`n"


$ctxPath = Join-Path $repoRoot "_PROJECT_CONTEXT.md"


if (Test-Path -LiteralPath $ctxPath) {


$ctxBytes = [System.IO.File]::ReadAllBytes($ctxPath)


if ($ctxBytes.Length -ge 3 -and $ctxBytes[0] -eq 0xEF -and $ctxBytes[1] -eq 0xBB -and $ctxBytes[2] -eq 0xBF) {


$ctxBytes = $ctxBytes[3..($ctxBytes.Length-1)]


}


$ctx = [System.Text.Encoding]::UTF8.GetString($ctxBytes)


$ctx = $ctx.Replace([string][char]0xFEFF, "")


$ctx = $ctx.Replace([string][char]0x200B, "").Replace([string][char]0x200C, "").Replace([string][char]0x200D, "").Replace([string][char]0x2060, "")


$content += $ctx


if (-not $content.EndsWith("`n")) { $content += "`n" }


} else {


$content += "MISSING: _PROJECT_CONTEXT.md`n"


}





$content += "`n=== FILE CONTENTS (filtered) ===`n"


foreach ($it in $items) {


if ($it.PSIsContainer) { continue }





$ext = $it.Extension


if (-not ($includeContentExt -contains $ext)) { continue }





$rel = $it.FullName.Substring($projectRoot.FullName.Length).TrimStart('\','/')


$content += "`n--- FILE: \$rel ---`n"





# Read bytes to strip BOM safely


$bytes = [System.IO.File]::ReadAllBytes($it.FullName)


if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {


$bytes = $bytes[3..($bytes.Length-1)]


}


$text = [System.Text.Encoding]::UTF8.GetString($bytes)


$text = $text.Replace([string][char]0xFEFF, "")


$text = $text.Replace([string][char]0x200B, "").Replace([string][char]0x200C, "").Replace([string][char]0x200D, "").Replace([string][char]0x2060, "")


$content += $text


if (-not $content.EndsWith("`n")) { $content += "`n" }


}





# Final normalization


$content = $content.Replace([string][char]0xFEFF, "")


$content = $content.Replace([string][char]0x200B, "").Replace([string][char]0x200C, "").Replace([string][char]0x200D, "").Replace([string][char]0x2060, "")





$utf8NoBom = [System.Text.UTF8Encoding]::new($false)


[System.IO.File]::WriteAllText($outPath, $content, $utf8NoBom)





Log-Output "OK: wrote context dump to: $outPath"


}


catch {


Log-Output "ERROR: $($_.Exception.Message)"


throw


}


}


