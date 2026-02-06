<#
.SYNOPSIS
Generates a Context Packet for LLM injection.
Ironclad Mode: Single quotes only. Writes to docs/generated/01_CONTEXT_PACKET.md.
#>

param (
    [string[]]$Focus = @()
)

$NL = [Environment]::NewLine
$DateStr = Get-Date -Format 'yyyy-MM-dd HH:mm'
$Header = '## REPO CONTEXT (Generated ' + $DateStr + ')'

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine($Header)

# --- CONFIGURATION ---
$Root = Get-Location
$SourceExtensions = @('*.cs', '*.tscn', '*.gd', '*.json', '*.md') 
$Ignore = @('bin', 'obj', '.git', '.vs', '_scratch', 'addons', 'TestResults', 'docs\generated', '_archive', '_shadow_godot_tree')
$MaxLines = 30
# TARGET OUTPUT FILE
$OutputPath = Join-Path $Root 'docs\generated\01_CONTEXT_PACKET.md'

# --- 1. HEALTH ---
$ConnectFile = Join-Path $Root 'docs\generated\connectivity_violations.json'
$TestFile = Join-Path $Root 'SimCore\Tests\TestResults.json' 

[void]$sb.AppendLine('')
[void]$sb.AppendLine('### [SYSTEM HEALTH]')

if (Test-Path $ConnectFile) {
    try {
        $Json = Get-Content $ConnectFile | ConvertFrom-Json
        $Count = $Json.violations.Count
        if ($Count -gt 0) {
            $Msg = '- [CRITICAL] Connectivity Violations: ' + $Count
            [void]$sb.AppendLine($Msg)
        } else {
            [void]$sb.AppendLine('- [OK] Connectivity: Clean')
        }
    } catch { [void]$sb.AppendLine('- [WARN] Bad Connectivity JSON.') }
} else { [void]$sb.AppendLine('- [WARN] No Connectivity Scan found.') }

if (Test-Path $TestFile) {
    [void]$sb.AppendLine('- [INFO] Test Results found.')
} else { [void]$sb.AppendLine('- [WARN] No Test Results found.') }

# --- 2. FILE SCAN ---
[void]$sb.AppendLine('')
[void]$sb.AppendLine('### [FILE MAP]')

$Files = Get-ChildItem -Path $Root -Recurse -Include $SourceExtensions | Where-Object {
    $P = $_.FullName
    $Skip = $false
    foreach ($I in $Ignore) { if ($P -like "*\$I\*") { $Skip = $true; break } }
    return -not $Skip
}

foreach ($F in $Files) {
    $Rel = $F.FullName.Replace($Root.Path + '\', '').Replace('\', '/')
    $Txt = Get-Content $F.FullName
    $Lines = $Txt.Count
    
    # FOCUS CHECK
    $IsFocus = $false
    foreach ($T in $Focus) { if ($Rel -like "*$T*") { $IsFocus = $true; break } }

    $Ext = $F.Extension.Replace('.', '')
    if ($Ext -eq 'cs') { $Ext = 'csharp' }

    $Fence = '```' + $Ext
    $EndFence = '```'

    if ($IsFocus) {
        [void]$sb.AppendLine('')
        [void]$sb.AppendLine('#### [FOCUS] ' + $Rel)
        [void]$sb.AppendLine($Fence)
        [void]$sb.AppendLine($Txt -join $NL)
        [void]$sb.AppendLine($EndFence)
    } else {
        $Top = $Txt | Select-Object -First $MaxLines
        [void]$sb.AppendLine('')
        [void]$sb.AppendLine('#### [FILE] ' + $Rel + ' (Lines: ' + $Lines + ')')
        [void]$sb.AppendLine($Fence)
        [void]$sb.AppendLine($Top -join $NL)
        if ($Lines -gt $MaxLines) { [void]$sb.AppendLine('... (truncated)') }
        [void]$sb.AppendLine($EndFence)
    }
}

# --- 3. WRITE TO FILE ---
$Dir = Split-Path $OutputPath
if (-not (Test-Path $Dir)) { New-Item -ItemType Directory -Force -Path $Dir | Out-Null }
[System.IO.File]::WriteAllText($OutputPath, $sb.ToString(), [System.Text.Encoding]::UTF8)

Write-Host ('SUCCESS: Context Packet written to: ' + $OutputPath) -ForegroundColor Green