param(
    [string]$RunNum = "1",
    [string]$OutFile = ""
)

$godot = "C:\Users\marsh\Downloads\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64.exe"
$scriptPath = "scripts/tests/test_galaxy_map_discovery_states.gd"
$projectPath = "D:/SGE/SpaceTradeEmpire"

if ($OutFile -eq "") {
    $OutFile = "docs/generated/gmd_run$RunNum.txt"
}

$lines = & $godot --headless --path $projectPath --script $scriptPath 2>$null | Select-String "^GMD\|"

$lines | Tee-Object -FilePath (Join-Path $projectPath $OutFile)
$hash = (Get-FileHash (Join-Path $projectPath $OutFile) -Algorithm SHA256).Hash
Write-Host "SHA256: $hash"
