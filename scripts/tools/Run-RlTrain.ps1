<#
.SYNOPSIS
    Run RL training or evaluation for SpaceTradeEmpire.

.DESCRIPTION
    Builds the C# RL server, installs Python dependencies, and runs
    PPO training or evaluation. Mirrors the Run-Bot.ps1 pattern.

    Supports two server backends:
    - headless: launches SimCore.RlServer as a background process (fast, pure C#)
    - godot: launches Godot headless with rl_agent_bot.gd (full-stack, slower)

.PARAMETER Mode
    'train' = PPO training, 'eval' = evaluate saved model, 'smoke' = quick sanity check

.PARAMETER Timesteps
    Total training timesteps (default 500000).

.PARAMETER Envs
    Number of parallel environments (default 4).

.PARAMETER Curriculum
    Enable curriculum learning (stages 0-3).

.PARAMETER Model
    Path to trained model .zip (required for eval mode).

.PARAMETER Episodes
    Number of training/evaluation episodes (default 1000).

.PARAMETER Server
    Server backend: 'headless' (default) or 'godot'.

.PARAMETER Port
    TCP port for RL server (default 11008).

.EXAMPLE
    .\Run-RlTrain.ps1 -Mode train -Timesteps 100000 -Curriculum
    .\Run-RlTrain.ps1 -Mode train -Server godot -Episodes 500
    .\Run-RlTrain.ps1 -Mode eval -Model reports/rl/training/ppo_space_trade_final.zip
    .\Run-RlTrain.ps1 -Mode smoke
#>

param(
    [ValidateSet('train', 'eval', 'smoke')]
    [string]$Mode = 'train',

    [int]$Timesteps = 500000,
    [int]$Envs = 4,
    [switch]$Curriculum,
    [string]$Model = '',
    [int]$Episodes = 1000,

    [ValidateSet('headless', 'godot')]
    [string]$Server = 'headless',

    [int]$Port = 11008
)

$ErrorActionPreference = 'Stop'

# Source shared helpers
. (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'common.ps1')

$repoRoot = Get-RepoRoot
Push-Location $repoRoot

# Track background processes for cleanup
$bgProcs = @()

try {
    # -- Build --
    if ($Server -eq 'headless') {
        Write-Host "[RL] Building SimCore.RlServer..." -ForegroundColor Cyan
        dotnet build "$repoRoot\SimCore.RlServer\SimCore.RlServer.csproj" -c Release --nologo -v q
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[RL] Build failed!" -ForegroundColor Red
            exit 1
        }
        Write-Host "[RL] Build OK" -ForegroundColor Green
    } else {
        Write-Host "[RL] Building C# assemblies (Godot mode)..." -ForegroundColor Cyan
        dotnet build 'Space Trade Empire.csproj' --nologo -v q
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[RL] Build failed!" -ForegroundColor Red
            exit 1
        }
        Write-Host "[RL] Build OK" -ForegroundColor Green
    }

    # Ensure output directories
    New-Item -ItemType Directory -Path "$repoRoot\reports\rl\training" -Force | Out-Null
    New-Item -ItemType Directory -Path "$repoRoot\reports\rl\eval" -Force | Out-Null

    $exePath = "$repoRoot\SimCore.RlServer\bin\Release\net8.0\SimCore.RlServer.exe"

    # -- Launch server backend (for train/smoke modes) --
    if ($Mode -in @('train', 'smoke')) {
        if ($Server -eq 'headless') {
            Write-Host "[RL] Launching headless RL server on port $Port..." -ForegroundColor Cyan
            $serverProc = Start-Process -FilePath "dotnet" `
                -ArgumentList @('run', '--project', "$repoRoot\SimCore.RlServer\", '--', '--port', $Port) `
                -PassThru -WindowStyle Hidden `
                -RedirectStandardOutput "$repoRoot\reports\rl\training\server_stdout.txt" `
                -RedirectStandardError "$repoRoot\reports\rl\training\server_stderr.txt"
            $bgProcs += $serverProc
            Write-Host "[RL] Server PID: $($serverProc.Id)" -ForegroundColor Cyan

            # Brief wait for server to start listening
            Start-Sleep -Seconds 3
            if ($serverProc.HasExited) {
                Write-Host "[RL] Server exited prematurely!" -ForegroundColor Red
                exit 1
            }
        } else {
            Write-Host "[RL] Launching Godot RL agent on port $Port..." -ForegroundColor Cyan
            $godotExe = Get-GodotExe -RepoRoot $repoRoot
            $godotArgs = @('--headless', '--path', '.', '-s', 'res://scripts/tests/rl_agent_bot.gd', '--', '--port', $Port)
            $godotProc = Start-Process -FilePath $godotExe -ArgumentList $godotArgs `
                -PassThru -WindowStyle Hidden `
                -RedirectStandardOutput "$repoRoot\reports\rl\training\godot_stdout.txt" `
                -RedirectStandardError "$repoRoot\reports\rl\training\godot_stderr.txt"
            $bgProcs += $godotProc
            Write-Host "[RL] Godot PID: $($godotProc.Id)" -ForegroundColor Cyan

            # Godot takes longer to start
            Start-Sleep -Seconds 5
            if ($godotProc.HasExited) {
                Write-Host "[RL] Godot exited prematurely!" -ForegroundColor Red
                exit 1
            }
        }
    }

    switch ($Mode) {
        'smoke' {
            Write-Host "[RL] Running smoke test (10 episodes, random actions, server=$Server)..." -ForegroundColor Cyan
            $smokeScript = @"
import sys, os
sys.path.insert(0, r'$($repoRoot -replace '\\', '\\\\')' )
from rl.env import SpaceTradeEnv
env = SpaceTradeEnv(exe_path=r'$($exePath -replace '\\', '\\\\')')
for ep in range(10):
    obs, info = env.reset(seed=ep+1)
    total_r = 0
    for step in range(100):
        action = env.action_space.sample()
        obs, reward, term, trunc, info = env.step(action)
        total_r += reward
        if term or trunc:
            break
    print(f'Episode {ep+1}: reward={total_r:.2f}, credits={info.get("credits",0)}, nodes={info.get("nodes_visited",0)}, ticks={info.get("tick",0)}')
env.close()
print('Smoke test PASSED')
"@
            python -c $smokeScript
            if ($LASTEXITCODE -ne 0) {
                Write-Host "[RL] Smoke test FAILED" -ForegroundColor Red
                exit 1
            }
            Write-Host "[RL] Smoke test PASSED" -ForegroundColor Green
        }

        'train' {
            Write-Host "[RL] Starting PPO training (timesteps=$Timesteps, envs=$Envs, episodes=$Episodes, server=$Server, curriculum=$Curriculum)..." -ForegroundColor Cyan

            $trainArgs = @(
                "rl/train_ppo.py",
                "--host", "127.0.0.1",
                "--port", $Port,
                "--episodes", $Episodes,
                "--server", $Server,
                "--timesteps", $Timesteps,
                "--n-envs", $Envs
            )
            if ($Curriculum) { $trainArgs += "--curriculum" }

            python @trainArgs

            if ($LASTEXITCODE -ne 0) {
                Write-Host "[RL] Training failed!" -ForegroundColor Red
                exit 1
            }
            Write-Host "[RL] Training complete" -ForegroundColor Green
        }

        'eval' {
            if (-not $Model) {
                Write-Host "[RL] -Model parameter required for eval mode" -ForegroundColor Red
                exit 1
            }
            Write-Host "[RL] Evaluating model: $Model ($Episodes episodes)..." -ForegroundColor Cyan

            python -m rl.eval.evaluate --model $Model --episodes $Episodes --exe-path $exePath

            if ($LASTEXITCODE -ne 0) {
                Write-Host "[RL] Evaluation failed!" -ForegroundColor Red
                exit 1
            }
            Write-Host "[RL] Evaluation complete" -ForegroundColor Green
        }
    }

} finally {
    # -- Cleanup background processes --
    foreach ($p in $bgProcs) {
        if ($p -and -not $p.HasExited) {
            Write-Host "[RL] Stopping background process PID $($p.Id)..." -ForegroundColor Yellow
            try {
                $p.Kill()
                $p.WaitForExit(5000)
            } catch {
                Write-Host "[RL] Warning: could not kill PID $($p.Id): $_" -ForegroundColor Yellow
            }
        }
    }
    Pop-Location
}
