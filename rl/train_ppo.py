"""MaskablePPO training script for SpaceTradeEmpire RL agent.

Usage:
    # With headless C# server (launched automatically):
    python rl/train_ppo.py --server headless --episodes 500

    # With Godot (launch Godot separately first):
    python rl/train_ppo.py --server godot --port 11008

    # With WandB logging:
    python rl/train_ppo.py --server headless --wandb
"""

from __future__ import annotations

import argparse
import logging
import os
import signal
import subprocess
import sys
import time
from pathlib import Path

import numpy as np

# Repo root (for dotnet project paths)
REPO_ROOT = Path(__file__).resolve().parent.parent

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)
logger = logging.getLogger("train_ppo")


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(
        description="Train MaskablePPO agent for SpaceTradeEmpire"
    )
    p.add_argument(
        "--host", type=str, default="127.0.0.1",
        help="RL server host (default: 127.0.0.1)",
    )
    p.add_argument(
        "--port", type=int, default=11008,
        help="RL server port (default: 11008)",
    )
    p.add_argument(
        "--episodes", type=int, default=1000,
        help="Number of training episodes (default: 1000)",
    )
    p.add_argument(
        "--server", type=str, choices=["godot", "headless"],
        default="headless",
        help="Server mode: godot (external) or headless (auto-launch C#)",
    )
    p.add_argument(
        "--seed", type=int, default=42,
        help="Random seed (default: 42)",
    )
    p.add_argument(
        "--wandb", action="store_true",
        help="Enable Weights & Biases logging",
    )
    p.add_argument(
        "--timesteps", type=int, default=500_000,
        help="Total training timesteps (default: 500000)",
    )
    p.add_argument(
        "--learning-rate", type=float, default=3e-4,
        help="Learning rate (default: 3e-4)",
    )
    p.add_argument(
        "--n-steps", type=int, default=2048,
        help="Steps per rollout (default: 2048)",
    )
    p.add_argument(
        "--batch-size", type=int, default=64,
        help="Minibatch size (default: 64)",
    )
    p.add_argument(
        "--checkpoint-dir", type=str, default="rl/checkpoints",
        help="Directory for model checkpoints",
    )
    return p.parse_args()


def launch_headless_server(
    port: int, seed: int
) -> subprocess.Popen:
    """Launch SimCore.RlServer as a subprocess."""
    project_path = str(REPO_ROOT / "SimCore.RlServer")
    cmd = [
        "dotnet", "run",
        "--project", project_path,
        "--",
        "--port", str(port),
        "--seed", str(seed),
    ]
    logger.info("Launching headless server: %s", " ".join(cmd))
    proc = subprocess.Popen(
        cmd,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        cwd=str(REPO_ROOT),
    )
    # Give server time to start
    time.sleep(3.0)
    if proc.poll() is not None:
        stdout = proc.stdout.read().decode() if proc.stdout else ""
        stderr = proc.stderr.read().decode() if proc.stderr else ""
        raise RuntimeError(
            f"Headless server exited immediately "
            f"(code={proc.returncode})\n"
            f"stdout: {stdout[:500]}\nstderr: {stderr[:500]}"
        )
    logger.info("Headless server started (pid=%d)", proc.pid)
    return proc


def make_env(host: str, port: int):
    """Create and return a SpaceTradeEmpireEnv instance."""
    from rl.gymnasium_env import SpaceTradeEmpireEnv
    return SpaceTradeEmpireEnv(
        host=host,
        port=port,
        connect_timeout=30.0,
    )


def main():
    args = parse_args()

    # Create checkpoint directory
    ckpt_dir = Path(args.checkpoint_dir)
    ckpt_dir.mkdir(parents=True, exist_ok=True)

    # Optional WandB setup
    wandb_run = None
    if args.wandb:
        try:
            import wandb
            wandb_run = wandb.init(
                project="space-trade-empire",
                config=vars(args),
                tags=["maskable-ppo", args.server],
            )
            logger.info("WandB logging enabled: %s", wandb_run.url)
        except ImportError:
            logger.warning(
                "wandb not installed, skipping WandB logging"
            )
        except Exception as e:
            logger.warning("WandB init failed: %s", e)

    # Launch server if headless mode
    server_proc = None
    if args.server == "headless":
        server_proc = launch_headless_server(args.port, args.seed)
    else:
        logger.info(
            "Godot mode: expecting server on %s:%d",
            args.host, args.port,
        )

    try:
        _run_training(args, ckpt_dir, wandb_run)
    finally:
        # Cleanup
        if server_proc is not None:
            logger.info("Terminating headless server...")
            server_proc.terminate()
            try:
                server_proc.wait(timeout=5)
            except subprocess.TimeoutExpired:
                server_proc.kill()
        if wandb_run is not None:
            wandb_run.finish()


def _run_training(args, ckpt_dir: Path, wandb_run):
    """Core training loop using sb3-contrib MaskablePPO."""
    from sb3_contrib import MaskablePPO
    from sb3_contrib.common.wrappers import ActionMasker
    from stable_baselines3.common.callbacks import (
        BaseCallback,
        CheckpointCallback,
    )

    # Create environment with action masking
    raw_env = make_env(args.host, args.port)
    env = ActionMasker(raw_env, lambda env: env.action_masks())

    logger.info(
        "Environment created: obs_space=%s action_space=%s",
        env.observation_space, env.action_space,
    )

    # Build model
    model = MaskablePPO(
        "MlpPolicy",
        env,
        learning_rate=args.learning_rate,
        n_steps=args.n_steps,
        batch_size=args.batch_size,
        n_epochs=10,
        gamma=0.99,
        gae_lambda=0.95,
        clip_range=0.2,
        ent_coef=0.01,
        verbose=1,
        seed=args.seed,
        tensorboard_log=str(ckpt_dir / "tb_logs"),
    )

    logger.info(
        "MaskablePPO initialized (lr=%.1e, timesteps=%d)",
        args.learning_rate, args.timesteps,
    )

    # Callbacks
    callbacks = []

    # Checkpoint every 10% of training
    save_freq = max(args.timesteps // 10, 5000)
    callbacks.append(
        CheckpointCallback(
            save_freq=save_freq,
            save_path=str(ckpt_dir),
            name_prefix="maskable_ppo_ste",
        )
    )

    # Optional WandB callback
    if wandb_run is not None:
        callbacks.append(_WandBCallback(wandb_run))

    # Episode logging callback
    callbacks.append(_EpisodeLogCallback())

    # Train
    logger.info("Starting training for %d timesteps...", args.timesteps)
    model.learn(
        total_timesteps=args.timesteps,
        callback=callbacks,
        progress_bar=True,
    )

    # Save final model
    final_path = str(ckpt_dir / "maskable_ppo_ste_final")
    model.save(final_path)
    logger.info("Final model saved to %s.zip", final_path)

    env.close()
    logger.info("Training complete.")


from stable_baselines3.common.callbacks import BaseCallback as _BC


class _EpisodeLogCallback(_BC):
    """Logs episode reward and length after each rollout."""

    def __init__(self, verbose=0):
        super().__init__(verbose)
        self._ep_count = 0

    def _on_step(self) -> bool:
        # Check for completed episodes in the info buffer
        infos = self.locals.get("infos", [])
        for info in infos:
            ep_info = info.get("episode")
            if ep_info is not None:
                self._ep_count += 1
                logger.info(
                    "Episode %d: reward=%.2f length=%d",
                    self._ep_count,
                    ep_info["r"],
                    ep_info["l"],
                )
        return True


class _WandBCallback(_BC):
    """Log training metrics to Weights & Biases."""

    def __init__(self, wandb_run, verbose=0):
        super().__init__(verbose)
        self._run = wandb_run
        self._ep_count = 0

    def _on_step(self) -> bool:
        import wandb

        infos = self.locals.get("infos", [])
        for info in infos:
            ep_info = info.get("episode")
            if ep_info is not None:
                self._ep_count += 1
                wandb.log({
                    "episode": self._ep_count,
                    "episode_reward": ep_info["r"],
                    "episode_length": ep_info["l"],
                    "timestep": self.num_timesteps,
                })
        return True


if __name__ == "__main__":
    main()
