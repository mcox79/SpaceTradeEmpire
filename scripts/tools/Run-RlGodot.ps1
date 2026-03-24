<#
.SYNOPSIS
    Run RL training or smoke test through the Godot engine (full-stack).

.DESCRIPTION
    Builds C# assemblies, launches Godot with the RL agent bot,
    and connects Python trainer via TCP. Tests the FULL stack:
    SimBridge threading, GDScript, scene tree.

    Slower than headless Run-RlTrain.ps1 but catches integration bugs.

.PARAMETER Mode
    'train' = PPO training via Godot, 'smoke' = quick sanity check

.PARAMETER Timesteps
    Total training timesteps (default 100000 — lower than headless due to speed).

.PARAMETER Port
    TCP port for Godot RL agent (default 11008).

.EXAMPLE
    .\Run-RlGodot.ps1 -Mode smoke
    .\Run-RlGodot.ps1 -Mode train -Timesteps 50000
#>

param(
    [ValidateSet('train', 'smoke')]
    [string]$Mode = 'smoke',

    [int]$Timesteps = 100000,
    [int]$Port = 11008
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# Resolve Godot path
$godotPath = ""
$cfgFile = Join-Path $repoRoot "godot_path.cfg"
if (Test-Path $cfgFile) {
    $godotPath = (Get-Content $cfgFile -Raw).Trim()
}
if (-not $godotPath -or -not (Test-Path $godotPath)) {
    Write-Host "[RL-Godot] WARNING: godot_path.cfg not found or invalid. Using 'godot' from PATH." -ForegroundColor Yellow
    $godotPath = "godot"
}

Write-Host "[RL-Godot] Building C# assemblies..." -ForegroundColor Cyan
dotnet build "$repoRoot\Space Trade Empire.csproj" --nologo -v q
if ($LASTEXITCODE -ne 0) {
    Write-Host "[RL-Godot] Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "[RL-Godot] Build OK" -ForegroundColor Green

New-Item -ItemType Directory -Path "$repoRoot\reports\rl\godot_training" -Force | Out-Null

switch ($Mode) {
    'smoke' {
        Write-Host "[RL-Godot] Running Godot RL smoke test (port=$Port)..." -ForegroundColor Cyan
        $smokeScript = @"
import sys, os, time, socket, json
sys.path.insert(0, r'$($repoRoot -replace '\\', '\\\\')' )
from rl.env.godot_env import GodotSpaceTradeEnv

env = GodotSpaceTradeEnv(
    godot_path=r'$($godotPath -replace '\\', '\\\\')',
    port=$Port,
    startup_timeout=30.0,
)

print('[Smoke] Connected to Godot RL agent')
obs, info = env.reset()
print(f'[Smoke] Reset OK: obs_len={len(obs)}, info={info}')

total_r = 0
for step in range(50):
    action = env.action_space.sample()
    obs, reward, term, trunc, info = env.step(action)
    total_r += reward
    if term or trunc:
        print(f'[Smoke] Episode ended at step {step}: reward={total_r:.2f}')
        break

print(f'[Smoke] Final: reward={total_r:.2f}, credits={info.get("credits",0)}')
env.close()
print('[Smoke] Godot RL smoke test PASSED')
"@
        python -c $smokeScript
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[RL-Godot] Smoke test FAILED" -ForegroundColor Red
            exit 1
        }
        Write-Host "[RL-Godot] Smoke test PASSED" -ForegroundColor Green
    }

    'train' {
        Write-Host "[RL-Godot] Starting Godot PPO training (timesteps=$Timesteps, port=$Port)..." -ForegroundColor Cyan
        Write-Host "[RL-Godot] NOTE: This is ~250x slower than headless. Use for integration testing, not production training." -ForegroundColor Yellow

        Push-Location $repoRoot
        python -m rl.train.train_godot_ppo `
            --timesteps $Timesteps `
            --godot-path $godotPath `
            --port $Port
        Pop-Location

        if ($LASTEXITCODE -ne 0) {
            Write-Host "[RL-Godot] Training failed!" -ForegroundColor Red
            exit 1
        }
        Write-Host "[RL-Godot] Training complete" -ForegroundColor Green
    }
}
