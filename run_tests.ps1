$godot = 'C:\Users\marsh\Downloads\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64.exe'
$base = 'D:\SGE\SpaceTradeEmpire'
$tests = @(
    'test_catalog_epic_close.gd',
    'test_equip_panel.gd',
    'test_station_dock_v0.gd',
    'test_module_model_epic_close.gd',
    'test_station_loop_v1.gd'
)

foreach ($t in $tests) {
    Write-Host "=== $t ===" -ForegroundColor Cyan
    $outFile = "$base\tmp_out.txt"
    $errFile = "$base\tmp_err.txt"
    $p = Start-Process -FilePath $godot -ArgumentList '--headless','--path',$base,'-s',"res://scripts/tests/$t" -NoNewWindow -PassThru -RedirectStandardOutput $outFile -RedirectStandardError $errFile -WorkingDirectory $base
    $finished = $p.WaitForExit(35000)
    if (-not $finished) {
        $p.Kill()
        Write-Host "TIMEOUT: $t" -ForegroundColor Red
    } else {
        if (Test-Path $outFile) { Get-Content $outFile }
        if (Test-Path $errFile) {
            $errs = Get-Content $errFile | Select-String 'SCRIPT ERROR|ERROR'
            if ($errs) { Write-Host "STDERR:" -ForegroundColor Yellow; $errs }
        }
    }
    Remove-Item $outFile,$errFile -ErrorAction SilentlyContinue
}
