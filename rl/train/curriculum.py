"""Curriculum learning callback for SpaceTradeEmpire RL training.

Monitors mean episode reward and advances the curriculum stage
when the agent reaches performance thresholds. Each stage increases
galaxy complexity and episode length.

Stages:
  0: 4 stars, 500 ticks, learn buy-travel-sell loop
  1: 8 stars, 1000 ticks, multi-hop trade routes
  2: 12 stars, 2000 ticks, full economy
  3: 12 stars, 2000 ticks, full economy + combat
"""

from collections import deque
from stable_baselines3.common.callbacks import BaseCallback


STAGE_THRESHOLDS = [0.5, 1.0, 2.0]  # Mean reward to advance from stage N to N+1
MAX_STAGE = 3


class CurriculumCallback(BaseCallback):
    """Advance curriculum stage based on mean episode reward."""

    def __init__(self, window_size: int = 100, verbose: int = 1):
        super().__init__(verbose)
        self._episode_rewards: deque = deque(maxlen=window_size)
        self._current_stage = 0

    @property
    def current_stage(self) -> int:
        return self._current_stage

    def _on_step(self) -> bool:
        # Collect episode rewards from monitor wrapper
        infos = self.locals.get("infos", [])
        for info in infos:
            if "episode" in info:
                self._episode_rewards.append(info["episode"]["r"])

        # Check for stage advancement
        if len(self._episode_rewards) >= 50 and self._current_stage < MAX_STAGE:
            mean_reward = sum(self._episode_rewards) / len(self._episode_rewards)
            threshold = STAGE_THRESHOLDS[self._current_stage] if self._current_stage < len(STAGE_THRESHOLDS) else float("inf")

            if mean_reward >= threshold:
                self._current_stage += 1
                if self.verbose > 0:
                    print(f"\n[Curriculum] Advancing to stage {self._current_stage} "
                          f"(mean_reward={mean_reward:.3f} >= {threshold})")

                # Update environments with new stage
                env = self.training_env
                if hasattr(env, "env_method"):
                    try:
                        env.env_method("set_curriculum_stage", self._current_stage)
                    except Exception:
                        pass  # Not all vec envs support env_method

        return True

    def _on_training_end(self) -> None:
        if self.verbose > 0:
            mean = sum(self._episode_rewards) / len(self._episode_rewards) if self._episode_rewards else 0
            print(f"\n[Curriculum] Training ended at stage {self._current_stage}, "
                  f"final mean reward: {mean:.3f}")
