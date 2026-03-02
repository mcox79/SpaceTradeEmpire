param(
    [switch]$AssertDiscoveryDock,
    [string]$OutFile = "docs/generated/hsb_run.txt"
)

$godot = "C:\Users\marsh\Downloads\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64.exe"
$scriptPath = "scripts/tests/test_hero_ship_scene_boot.gd"
$projectPath = "D:/SGE/SpaceTradeEmpire"

if ($AssertDiscoveryDock) {
    $lines = & $godot --headless --path $projectPath --script $scriptPath -- --assert-discovery-dock 2>$null | Select-String "^HSB\|"
} else {
    $lines = & $godot --headless --path $projectPath --script $scriptPath 2>$null | Select-String "^HSB\|"
}

$lines | Tee-Object -FilePath (Join-Path $projectPath $OutFile)
$hash = (Get-FileHash (Join-Path $projectPath $OutFile) -Algorithm SHA256).Hash
Write-Host "SHA256: $hash"
