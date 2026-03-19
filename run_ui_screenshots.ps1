# Temp script to run UI screenshot bot
$outDir = "reports/screenshot/ui_all"
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

$godot = "C:\Godot\Godot_v4.6-stable_mono_win64.exe"
$proc = Start-Process -FilePath $godot -ArgumentList "--path . -s res://scripts/tests/ui_screenshot_bot_v0.gd --rendering-method gl_compatibility --resolution 1920x1080" -PassThru -RedirectStandardOutput "reports/screenshot/bot_stdout.txt" -RedirectStandardError "reports/screenshot/bot_stderr.txt"
$exited = $proc.WaitForExit(150000)
if (-not $exited) { Stop-Process -Id $proc.Id -Force; Write-Host "TIMEOUT" }
Write-Host "Exit code: $($proc.ExitCode)"
$pngs = Get-ChildItem -Path $outDir -Filter "*.png" -ErrorAction SilentlyContinue
if ($pngs) {
    Write-Host "Screenshots captured: $($pngs.Count)"
    foreach ($f in $pngs) { Write-Host "  $($f.Name)" }
} else {
    Write-Host "No screenshots found"
}
