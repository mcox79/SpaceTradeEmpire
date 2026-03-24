"""PPO training script for SpaceTradeEmpire RL agent.

Usage:
    python -m rl.train.train_ppo --timesteps 1000000 --n-envs 4
    python -m rl.train.train_ppo --timesteps 100000 --curriculum  # with curriculum learning
"""

import argparse
import os
import sys
from pathlib import Path

# Add repo root to path
repo_root = Path(__file__).resolve().parent.parent.parent
sys.path.insert(0, str(repo_root))

from stable_baselines3 import PPO
from stable_baselines3.common.vec_env import SubprocVecEnv
from stable_baselines3.common.monitor import Monitor
from stable_baselines3.common.callbacks import CheckpointCallback

from rl.env import SpaceTradeEnv
from rl.train.curriculum import CurriculumCallback


def make_env(rank: int, exe_path: str | None, curriculum_stage: int = 0):
    """Factory function for vectorized environments."""
    def _init():
        env = SpaceTradeEnv(
            exe_path=exe_path,
            curriculum_stage=curriculum_stage,
        )
        env = Monitor(env)
        return env
    return _init


def main():
    parser = argparse.ArgumentParser(description="Train PPO agent for SpaceTradeEmpire")
    parser.add_argument("--timesteps", type=int, default=500_000, help="Total training timesteps")
    parser.add_argument("--n-envs", type=int, default=4, help="Number of parallel environments")
    parser.add_argument("--curriculum", action="store_true", help="Enable curriculum learning")
    parser.add_argument("--exe-path", type=str, default=None, help="Path to SimCore.RlServer executable")
    parser.add_argument("--output-dir", type=str, default="reports/rl/training", help="Output directory")
    parser.add_argument("--learning-rate", type=float, default=3e-4)
    parser.add_argument("--n-steps", type=int, default=2048)
    parser.add_argument("--batch-size", type=int, default=64)
    parser.add_argument("--n-epochs", type=int, default=10)
    parser.add_argument("--gamma", type=float, default=0.99)
    parser.add_argument("--ent-coef", type=float, default=0.01)
    args = parser.parse_args()

    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    print(f"[Train] Creating {args.n_envs} parallel environments...")
    env = SubprocVecEnv([
        make_env(i, args.exe_path, curriculum_stage=0 if args.curriculum else 2)
        for i in range(args.n_envs)
    ])

    print(f"[Train] Initializing PPO (timesteps={args.timesteps}, lr={args.learning_rate})")
    model = PPO(
        "MlpPolicy",
        env,
        learning_rate=args.learning_rate,
        n_steps=args.n_steps,
        batch_size=args.batch_size,
        n_epochs=args.n_epochs,
        gamma=args.gamma,
        gae_lambda=0.95,
        clip_range=0.2,
        ent_coef=args.ent_coef,
        verbose=1,
        tensorboard_log=str(output_dir / "tb_logs"),
    )

    callbacks = [
        CheckpointCallback(
            save_freq=max(args.timesteps // 10, 10000),
            save_path=str(output_dir / "checkpoints"),
            name_prefix="ppo_space_trade",
        ),
    ]

    if args.curriculum:
        callbacks.append(CurriculumCallback(verbose=1))

    print(f"[Train] Starting training...")
    model.learn(
        total_timesteps=args.timesteps,
        callback=callbacks,
        progress_bar=True,
    )

    model_path = str(output_dir / "ppo_space_trade_final")
    model.save(model_path)
    print(f"[Train] Model saved to {model_path}.zip")

    env.close()
    print("[Train] Done.")


if __name__ == "__main__":
    main()
