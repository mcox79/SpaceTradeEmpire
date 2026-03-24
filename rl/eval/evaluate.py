"""Evaluate a trained SpaceTradeEmpire RL agent.

Runs N episodes with fixed seeds, collects metrics, writes a JSON report.

Usage:
    python -m rl.eval.evaluate --model reports/rl/training/ppo_space_trade_final.zip --episodes 50
"""

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

repo_root = Path(__file__).resolve().parent.parent.parent
sys.path.insert(0, str(repo_root))

import numpy as np
from stable_baselines3 import PPO

from rl.env import SpaceTradeEnv


def evaluate(model_path: str, episodes: int, exe_path: str | None, output_dir: str):
    print(f"[Eval] Loading model from {model_path}")
    model = PPO.load(model_path)

    env = SpaceTradeEnv(exe_path=exe_path, curriculum_stage=2)

    episode_rewards = []
    episode_credits = []
    episode_nodes_visited = []
    episode_ticks = []
    episode_outcomes = []

    for ep in range(episodes):
        seed = 10000 + ep  # Fixed seeds for reproducibility
        obs, info = env.reset(seed=seed)
        total_reward = 0.0
        done = False

        while not done:
            action, _ = model.predict(obs, deterministic=True)
            obs, reward, terminated, truncated, info = env.step(action)
            total_reward += reward
            done = terminated or truncated

        episode_rewards.append(total_reward)
        episode_credits.append(info.get("credits", 0))
        episode_nodes_visited.append(info.get("nodes_visited", 0))
        episode_ticks.append(info.get("tick", 0))

        if terminated:
            # Determine outcome from credits (rough heuristic)
            outcome = "death" if info.get("credits", 0) <= 0 else "survived"
        else:
            outcome = "truncated"
        episode_outcomes.append(outcome)

        if (ep + 1) % 10 == 0:
            print(f"[Eval] Episode {ep + 1}/{episodes}: reward={total_reward:.2f}, "
                  f"credits={info.get('credits', 0)}, nodes={info.get('nodes_visited', 0)}")

    env.close()

    # Aggregate metrics
    report = {
        "model_path": model_path,
        "episodes": episodes,
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "metrics": {
            "reward_mean": float(np.mean(episode_rewards)),
            "reward_std": float(np.std(episode_rewards)),
            "reward_min": float(np.min(episode_rewards)),
            "reward_max": float(np.max(episode_rewards)),
            "credits_mean": float(np.mean(episode_credits)),
            "credits_std": float(np.std(episode_credits)),
            "nodes_visited_mean": float(np.mean(episode_nodes_visited)),
            "ticks_mean": float(np.mean(episode_ticks)),
            "survival_rate": sum(1 for o in episode_outcomes if o != "death") / episodes,
            "outcomes": {
                "survived": episode_outcomes.count("survived"),
                "death": episode_outcomes.count("death"),
                "truncated": episode_outcomes.count("truncated"),
            },
        },
    }

    # Write report
    out_dir = Path(output_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    ts = datetime.now().strftime("%Y%m%d_%H%M%S")
    report_path = out_dir / f"eval_{ts}.json"

    with open(report_path, "w") as f:
        json.dump(report, f, indent=2)

    print(f"\n[Eval] Report written to {report_path}")
    print(f"[Eval] Mean reward: {report['metrics']['reward_mean']:.3f} +/- {report['metrics']['reward_std']:.3f}")
    print(f"[Eval] Mean credits: {report['metrics']['credits_mean']:.0f}")
    print(f"[Eval] Mean nodes visited: {report['metrics']['nodes_visited_mean']:.1f}")
    print(f"[Eval] Survival rate: {report['metrics']['survival_rate']:.1%}")

    return report


def main():
    parser = argparse.ArgumentParser(description="Evaluate trained SpaceTradeEmpire RL agent")
    parser.add_argument("--model", type=str, required=True, help="Path to trained model .zip")
    parser.add_argument("--episodes", type=int, default=50, help="Number of evaluation episodes")
    parser.add_argument("--exe-path", type=str, default=None, help="Path to SimCore.RlServer executable")
    parser.add_argument("--output-dir", type=str, default="reports/rl/eval", help="Output directory")
    args = parser.parse_args()

    evaluate(args.model, args.episodes, args.exe_path, args.output_dir)


if __name__ == "__main__":
    main()
