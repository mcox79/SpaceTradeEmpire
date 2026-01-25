function Validate-GodotScript {

param(
[Parameter(Mandatory=$true)]
[string]$TargetScript,
[switch]$KeepAgent
)

$ErrorActionPreference = "Stop"
Write-Host "INITIALIZING PIPELINE for: $TargetScript" -ForegroundColor Yellow

if (-not (Test-Path -LiteralPath $TargetScript)) { throw "TargetScript not found: $TargetScript" }

$repo = (& git rev-parse --show-toplevel 2>$null)
if (-not $repo) { throw "Not in a git repository." }
$repo = $repo.Trim()

# Preflight: require clean tree except target file (unstaged)
$st = & git status --porcelain=1 2>&1
if ($LASTEXITCODE -ne 0) { throw "git status failed: $st" }

$targetRel = ((Resolve-Path $TargetScript).Path.Substring($repo.Length).TrimStart('\','/')).Replace('\','/')
$dirtyOther = @()

foreach ($line in ($st -split "`n")) {
$line = $line.TrimEnd()
if (-not $line) { continue }
# porcelain format: XY<space>path
if ($line.Length -ge 4) {
$p = $line.Substring(3).Trim()
$p = $p -replace '\\','/'
if ($p -ne $targetRel) { $dirtyOther += $p }
}
}

if ($dirtyOther.Count -gt 0) {
throw "Preflight failed. Working tree dirty outside target: $($dirtyOther -join ', ')"
}

# 1) Pre-processing: convert leading 4-space blocks to literal tabs
$scriptContent = Get-Content -LiteralPath $TargetScript -Raw -Encoding UTF8
$compliantContent = [regex]::Replace($scriptContent, '(?m)^( {4})+', {
param($match) "`t" * ($match.Length / 4)
})

if ($compliantContent -ne $scriptContent) {
Set-Content -LiteralPath $TargetScript -Value $compliantContent -Encoding UTF8 -NoNewline
Write-Host "SUCCESS: Script sanitized. Leading spaces converted to tabs." -ForegroundColor Green
} else {
Write-Host "OK: No leading-space blocks found. No rewrite needed." -ForegroundColor Green
}

# 2) Agent generation in _scratch (never dirties repo)
$sanitizedPath = ((Resolve-Path $TargetScript).Path -replace '\\','/')
$scratch = Join-Path $repo "_scratch"
New-Item -ItemType Directory -Force $scratch | Out-Null
$agentPath = Join-Path $scratch "temp_validator.gd"

$tab = [char]9
$agentCode = @(
"extends MainLoop",
"func _process(_delta):",
("{0}var gdscript = GDScript.new()" -f $tab),
("{0}gdscript.source_code = FileAccess.get_file_as_string(""{1}"")" -f $tab, $sanitizedPath),
("{0}var err = gdscript.reload()" -f $tab),
("{0}if err != OK:" -f $tab),
("{0}{0}printerr(""SCRIPT FATAL: reload() failed with code: %s"" % err)" -f $tab),
("{0}{0}return true" -f $tab),
("{0}print(""SCRIPT OK: parsed successfully"")" -f $tab),
("{0}return true" -f $tab)
) -join "`n"

Set-Content -LiteralPath $agentPath -Value $agentCode -Encoding UTF8 -NoNewline

# 3) Execution: capture output directly so we can inspect it
$godotExe = "C:\Godot\Godot_v4.5.1-stable_win64.exe"
$godotOut = & $godotExe --headless --script $agentPath 2>&1
if ($godotOut) { $godotOut | ForEach-Object { Write-Host $_ } }

$godotText = ($godotOut | Out-String)

# Trust model: ANY of these banners is failure, regardless of exit code
$hasFatal =
($godotText -match '(?mi)\bSCRIPT\s+(ERROR|FATAL)\b') -or
($godotText -match '(?mi)\bParse Error\b') -or
($godotText -match '(?mi)\bError while parsing\b') -or
($godotText -match '(?mi)\bERROR:\b')

if ($hasFatal) { throw "FAILURE: Godot parse gate failed for $TargetScript" }

# 4) Git gate: only commit if there is an actual diff for the target file
& git diff --quiet -- $TargetScript
$diffCode = $LASTEXITCODE

if ($diffCode -eq 0) {
Write-Host "OK: No changes to commit for $TargetScript" -ForegroundColor Green
if (-not $KeepAgent) { Remove-Item -LiteralPath $agentPath -Force -ErrorAction SilentlyContinue }
return
}

if ($diffCode -ne 1) { throw "git diff failed with exit code $diffCode for $TargetScript" }

$addOut = & git add -- $TargetScript 2>&1
$addCode = $LASTEXITCODE
if ($addOut) { $addOut | ForEach-Object { Write-Host $_ } }
if ($addCode -ne 0) { throw "git add failed ($addCode) for $TargetScript" }

$msg = "fix: standardize logic and formatting in $([System.IO.Path]::GetFileName($TargetScript))"
$commitOut = & git commit -m $msg 2>&1
$commitCode = $LASTEXITCODE
if ($commitOut) { $commitOut | ForEach-Object { Write-Host $_ } }

# Treat "nothing to commit" as success, not failure
if ($commitCode -ne 0) {
if (($commitOut | Out-String) -match '(?mi)nothing to commit') {
Write-Host "OK: Nothing to commit." -ForegroundColor Green
} else {
throw "FAILURE: git commit failed ($commitCode)"
}
} else {
Write-Host "SUCCESS: Operation verified and baseline secured in Git." -ForegroundColor Green
}

if (-not $KeepAgent) { Remove-Item -LiteralPath $agentPath -Force -ErrorAction SilentlyContinue }

}