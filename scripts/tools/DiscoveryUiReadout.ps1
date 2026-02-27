Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot/common.ps1"

$repo = Get-RepoRoot
$godot = Get-GodotExe -RepoRoot $repo

$outPath = Join-Path $repo "docs/generated/discovery_ui_readout_seed_42_v0.txt"

# Run headless and request deterministic seed 42 via cmdline user args (consumed by SimBridge).
# Note: Godot logs may include other lines; we filter on "DUIR|" prefix only.
$raw = & $godot --headless --path "$repo" --script "res://scripts/tests/test_discovery_ui_readout.gd" -- --seed=42 2>&1 | Out-String
$allLines = $raw -split "`r?`n"

$lines = @()
foreach ($l in $allLines) {
    $idx = $l.IndexOf("DUIR|", [StringComparison]::Ordinal)
    if ($idx -ge 0) {
        $lines += $l.Substring($idx + 5)
    }
}

if ($lines.Count -eq 0) {
    throw ("No DUIR transcript lines captured. First 80 lines:`n" + (($allLines | Select-Object -First 80) -join "`n"))
}

$text = ($lines -join "`n") + "`n"
Write-TextUtf8NoBom -Path $outPath -Text $text

Write-Host ("Wrote: " + $outPath)
