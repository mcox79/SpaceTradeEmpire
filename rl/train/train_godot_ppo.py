"""PPO training via the Godot RL agent (full-stack testing).

Unlike train_ppo.py (headless C# only), this trains through the actual Godot
engine, testing SimBridge threading, GDScript presentation, and scene tree.
Slower (~200 steps/sec) but catches integration bugs.

Usage:
    python -m rl.train.train_godot_ppo --timesteps 100000
    python -m rl.train.train_godot_ppo --timesteps 50000 --godot-path "C:/Godot/godot.exe"
"""

import argparse
import sys
from pathlib import Path

repo_root = Path(__file__).resolve().parent.parent.parent
sys.path.insert(0, str(repo_root))

from stable_baselines3 import PPO
from stable_baselines3.common.monitor import Monitor
from stable_baselines3.common.callbacks import CheckpointCallback

from rl.env.godot_env import GodotSpaceTradeEnv


def main():
    parser = argparse.ArgumentParser(description="Train PPO agent via Godot (full-stack)")
    parser.add_argument("--timesteps", type=int, default=100_000, help="Total training timesteps")
    parser.add_argument("--godot-path", type=str, default=None, help="Path to Godot executable")
    parser.add_argument("--port", type=int, default=11008, help="TCP port for Godot RL agent")
    parser.add_argument("--output-dir", type=str, default="reports/rl/godot_training", help="Output directory")
    parser.add_argument("--learning-rate", type=float, default=3e-4)
    parser.add_argument("--n-steps", type=int, default=512, help="Steps per update (lower for slow env)")
    parser.add_argument("--batch-size", type=int, default=64)
    args = parser.parse_args()

    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    print(f"[GodotTrain] Creating Godot RL environment (port={args.port})...")
    print(f"[GodotTrain] NOTE: This is slower than headless training but tests the full Godot stack.")

    env = GodotSpaceTradeEnv(
        godot_path=args.godot_path,
        port=args.port,
    )
    env = Monitor(env)

    print(f"[GodotTrain] Initializing PPO (timesteps={args.timesteps})")
    model = PPO(
        "MlpPolicy",
        env,
        learning_rate=args.learning_rate,
        n_steps=args.n_steps,
        batch_size=args.batch_size,
        n_epochs=5,
        gamma=0.99,
        gae_lambda=0.95,
        clip_range=0.2,
        ent_coef=0.01,
        verbose=1,
        tensorboard_log=str(output_dir / "tb_logs"),
    )

    callbacks = [
        CheckpointCallback(
            save_freq=max(args.timesteps // 5, 5000),
            save_path=str(output_dir / "checkpoints"),
            name_prefix="ppo_godot_space_trade",
        ),
    ]

    print("[GodotTrain] Starting training (this will be slow — ~200 steps/sec)...")
    model.learn(
        total_timesteps=args.timesteps,
        callback=callbacks,
        progress_bar=True,
    )

    model_path = str(output_dir / "ppo_godot_final")
    model.save(model_path)
    print(f"[GodotTrain] Model saved to {model_path}.zip")

    env.close()
    print("[GodotTrain] Done.")


if __name__ == "__main__":
    main()
