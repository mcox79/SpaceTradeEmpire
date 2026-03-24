"""Gymnasium environment for SpaceTradeEmpire headless RL training.

Wraps the SimCore.RlServer C# subprocess, providing a standard Gymnasium
interface for training with StableBaselines3 or any Gym-compatible framework.
"""

import gymnasium as gym
import numpy as np
from gymnasium import spaces

from .server_process import ServerProcess

# Must match StateEncoder.cs constants
OBS_SIZE = 137
NUM_ACTIONS = 34


class SpaceTradeEnv(gym.Env):
    """SpaceTradeEmpire headless RL environment.

    Each instance spawns its own C# SimCore.RlServer process.
    Communication is via stdin/stdout JSON lines.

    Action space: Discrete(34)
        0       = WAIT
        1-13    = BUY good[i]
        14-26   = SELL good[i]
        27-32   = TRAVEL to neighbor[j]
        33      = COMBAT

    Observation space: Box(137,) — normalized floats
        [0-6]     Player state (credits, cargo fill, hull, shield, fuel, time, exploration)
        [7-45]    Current market (13 goods × 3: stock, buy price, sell price)
        [46-58]   Player cargo (13 goods)
        [59-136]  Neighbor sell prices (6 neighbors × 13 goods)
    """

    metadata = {"render_modes": []}

    def __init__(
        self,
        exe_path: str | None = None,
        star_count: int = 12,
        max_episode_ticks: int = 2000,
        curriculum_stage: int = 0,
        build_first: bool = False,
    ):
        super().__init__()

        self.observation_space = spaces.Box(
            low=-1.0, high=5.0, shape=(OBS_SIZE,), dtype=np.float32
        )
        self.action_space = spaces.Discrete(NUM_ACTIONS)

        self._server = ServerProcess(exe_path=exe_path, build_first=build_first)
        self._star_count = star_count
        self._max_episode_ticks = max_episode_ticks
        self._curriculum_stage = curriculum_stage
        self._last_action_mask: np.ndarray | None = None

    @property
    def curriculum_stage(self) -> int:
        return self._curriculum_stage

    @curriculum_stage.setter
    def curriculum_stage(self, value: int):
        self._curriculum_stage = value

    def reset(self, seed=None, options=None):
        super().reset(seed=seed)

        if options:
            self._curriculum_stage = options.get("curriculum_stage", self._curriculum_stage)

        req_seed = seed if seed is not None else self.np_random.integers(1, 100_000)

        resp = self._server.send({
            "type": "reset",
            "seed": int(req_seed),
            "star_count": self._star_count,
            "curriculum_stage": self._curriculum_stage,
            "max_episode_ticks": self._max_episode_ticks,
        })

        if resp.get("type") == "error":
            raise RuntimeError(f"RlServer reset error: {resp.get('error')}")

        obs = np.array(resp["obs"], dtype=np.float32)
        info = resp.get("info", {})

        if resp.get("action_mask"):
            self._last_action_mask = np.array(resp["action_mask"], dtype=bool)
            info["action_mask"] = self._last_action_mask

        return obs, info

    def step(self, action):
        resp = self._server.send({
            "type": "step",
            "action": int(action),
        })

        if resp.get("type") == "error":
            raise RuntimeError(f"RlServer step error: {resp.get('error')}")

        obs = np.array(resp["obs"], dtype=np.float32)
        reward = float(resp["reward"])
        terminated = bool(resp["terminated"])
        truncated = bool(resp["truncated"])
        info = resp.get("info", {})

        if resp.get("action_mask"):
            self._last_action_mask = np.array(resp["action_mask"], dtype=bool)
            info["action_mask"] = self._last_action_mask

        return obs, reward, terminated, truncated, info

    def action_masks(self) -> np.ndarray:
        """Return the current action mask for use with MaskablePPO."""
        if self._last_action_mask is not None:
            return self._last_action_mask
        return np.ones(NUM_ACTIONS, dtype=bool)

    def close(self):
        self._server.close()
        super().close()
