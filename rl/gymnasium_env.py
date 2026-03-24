"""Gymnasium environment for SpaceTradeEmpire -- TCP JSON-lines protocol.

Unified wrapper that connects to either:
  - The Godot RL agent bot (rl_agent_bot.gd) running as a TCP server
  - The headless C# SimCore.RlServer when it supports TCP mode

Protocol (JSON lines over TCP to localhost):
  Python -> Server: {"type":"reset","seed":42,"max_episode_ticks":2000}
  Python -> Server: {"type":"step","action":7}
  Python -> Server: {"type":"observe"}
  Python -> Server: {"type":"shutdown"}
  Server -> Python: {"type":"reset_ok","obs":[...],"action_mask":[...],"info":{...}}
  Server -> Python: {"type":"step_ok","obs":[...],"reward":0.1,...}
"""

from __future__ import annotations

import json
import logging
import socket
import time
from typing import Any

import gymnasium as gym
import numpy as np
from gymnasium import spaces

logger = logging.getLogger(__name__)

# Defaults -- configurable via __init__
DEFAULT_OBS_SIZE = 232
DEFAULT_NUM_ACTIONS = 120
DEFAULT_HOST = "127.0.0.1"
DEFAULT_PORT = 11008


class SpaceTradeEmpireEnv(gym.Env):
    """Gymnasium environment wrapping the SpaceTradeEmpire TCP RL protocol.

    Action space: Discrete(num_actions) with invalid-action masking
    via action_masks().
    Observation space: Box(obs_size,) of normalized floats in [-1, 5].

    The server (Godot or headless C#) must be listening on (host, port)
    before reset() is called, or the env will retry connection with
    exponential backoff.
    """

    metadata = {"render_modes": []}

    def __init__(
        self,
        host: str = DEFAULT_HOST,
        port: int = DEFAULT_PORT,
        obs_size: int = DEFAULT_OBS_SIZE,
        num_actions: int = DEFAULT_NUM_ACTIONS,
        max_episode_ticks: int = 2000,
        connect_timeout: float = 60.0,
        recv_timeout: float = 30.0,
    ):
        """Initialize the environment.

        Args:
            host: TCP host the RL server listens on.
            port: TCP port the RL server listens on.
            obs_size: Observation vector length (must match server).
            num_actions: Number of discrete actions (must match server).
            max_episode_ticks: Max sim ticks per episode before truncation.
            connect_timeout: Seconds to retry TCP connection on reset.
            recv_timeout: Seconds to wait for a server response.
        """
        super().__init__()

        self._host = host
        self._port = port
        self._obs_size = obs_size
        self._num_actions = num_actions
        self._max_episode_ticks = max_episode_ticks
        self._connect_timeout = connect_timeout
        self._recv_timeout = recv_timeout

        self.observation_space = spaces.Box(
            low=-1.0, high=5.0, shape=(obs_size,), dtype=np.float32
        )
        self.action_space = spaces.Discrete(num_actions)

        self._sock: socket.socket | None = None
        self._recv_buffer: bytes = b""
        self._last_action_mask: np.ndarray = np.ones(
            num_actions, dtype=bool
        )
        self._connected = False

    # -- TCP lifecycle ------------------------------------------------

    def _connect(self) -> None:
        """Connect to the RL server with retry + exponential backoff."""
        if self._connected and self._sock is not None:
            return

        self._disconnect()
        deadline = time.time() + self._connect_timeout
        delay = 0.25

        while time.time() < deadline:
            try:
                sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                sock.settimeout(2.0)
                sock.connect((self._host, self._port))
                sock.settimeout(self._recv_timeout)
                self._sock = sock
                self._recv_buffer = b""
                self._connected = True
                logger.info(
                    "Connected to RL server at %s:%d",
                    self._host, self._port,
                )
                return
            except (
                ConnectionRefusedError, ConnectionResetError, OSError
            ) as exc:
                logger.debug(
                    "Connection attempt failed: %s, retrying in %.1fs",
                    exc, delay,
                )
                time.sleep(delay)
                delay = min(delay * 1.5, 4.0)

        raise TimeoutError(
            f"Could not connect to RL server at "
            f"{self._host}:{self._port} "
            f"within {self._connect_timeout}s"
        )

    def _disconnect(self) -> None:
        """Close the TCP socket if open."""
        if self._sock is not None:
            try:
                self._sock.close()
            except OSError:
                pass
            self._sock = None
        self._connected = False
        self._recv_buffer = b""

    def _send_json(self, obj: dict) -> None:
        """Send a JSON line to the server."""
        if self._sock is None:
            raise RuntimeError("Not connected to RL server")
        data = json.dumps(obj, separators=(",", ":")) + "\n"
        self._sock.sendall(data.encode("utf-8"))

    def _recv_json(self) -> dict:
        """Read one JSON line from the server."""
        if self._sock is None:
            raise RuntimeError("Not connected to RL server")

        while b"\n" not in self._recv_buffer:
            try:
                chunk = self._sock.recv(65536)
            except socket.timeout:
                raise TimeoutError(
                    "Timed out waiting for RL server response"
                )
            if not chunk:
                self._connected = False
                raise ConnectionError("RL server disconnected")
            self._recv_buffer += chunk

        line, self._recv_buffer = self._recv_buffer.split(b"\n", 1)
        return json.loads(line.decode("utf-8"))

    def _send_recv(self, request: dict) -> dict:
        """Send request, return response. Reconnect once on failure."""
        try:
            self._send_json(request)
            return self._recv_json()
        except (ConnectionError, BrokenPipeError, OSError):
            logger.warning("Connection lost, attempting reconnect...")
            self._disconnect()
            self._connect()
            self._send_json(request)
            return self._recv_json()

    # -- Gymnasium API ------------------------------------------------

    def reset(
        self,
        seed: int | None = None,
        options: dict[str, Any] | None = None,
    ) -> tuple[np.ndarray, dict]:
        """Reset the episode.

        Connects/reconnects to the server if needed.
        """
        super().reset(seed=seed)

        # (Re)connect -- handles first call and dropped connections
        self._connect()

        req_seed = seed if seed is not None else int(
            self.np_random.integers(1, 100_000)
        )

        request: dict[str, Any] = {
            "type": "reset",
            "seed": req_seed,
            "max_episode_ticks": self._max_episode_ticks,
        }
        if options:
            request.update(options)

        resp = self._send_recv(request)

        if resp.get("type") == "error":
            raise RuntimeError(
                "RL server reset error: "
                + str(resp.get("error", "unknown"))
            )

        obs = self._parse_obs(resp)
        info = resp.get("info", {})
        self._update_mask(resp, info)

        return obs, info

    def step(
        self, action: int | np.integer,
    ) -> tuple[np.ndarray, float, bool, bool, dict]:
        """Execute one action and return the Gymnasium 5-tuple."""
        resp = self._send_recv({
            "type": "step",
            "action": int(action),
        })

        if resp.get("type") == "error":
            raise RuntimeError(
                "RL server step error: "
                + str(resp.get("error", "unknown"))
            )

        obs = self._parse_obs(resp)
        reward = float(resp.get("reward", 0.0))
        terminated = bool(resp.get("terminated", False))
        truncated = bool(resp.get("truncated", False))
        info = resp.get("info", {})
        self._update_mask(resp, info)

        return obs, reward, terminated, truncated, info

    def action_masks(self) -> np.ndarray:
        """Return the current action mask for sb3-contrib MaskablePPO.

        True = action is valid, False = action is invalid.
        """
        return self._last_action_mask.copy()

    def close(self) -> None:
        """Send shutdown and close the TCP connection."""
        if self._connected and self._sock is not None:
            try:
                self._send_json({"type": "shutdown"})
            except (OSError, BrokenPipeError):
                pass
        self._disconnect()
        super().close()

    # -- Helpers ------------------------------------------------------

    def _parse_obs(self, resp: dict) -> np.ndarray:
        """Extract observation array from a server response."""
        raw = resp.get("obs", [])
        obs = np.array(raw, dtype=np.float32)
        if obs.shape != (self._obs_size,):
            padded = np.zeros(self._obs_size, dtype=np.float32)
            n = min(len(obs), self._obs_size)
            padded[:n] = obs[:n]
            obs = padded
        return obs

    def _update_mask(self, resp: dict, info: dict) -> None:
        """Update the cached action mask from the server response."""
        raw_mask = resp.get("action_mask")
        if raw_mask is not None:
            mask = np.array(raw_mask, dtype=bool)
            if mask.shape == (self._num_actions,):
                self._last_action_mask = mask
            else:
                padded = np.ones(self._num_actions, dtype=bool)
                n = min(len(mask), self._num_actions)
                padded[:n] = mask[:n]
                if len(mask) < self._num_actions:
                    padded[len(mask):] = False
                self._last_action_mask = padded
            info["action_mask"] = self._last_action_mask

    def __repr__(self) -> str:
        return (
            f"SpaceTradeEmpireEnv(host={self._host!r}, "
            f"port={self._port}, "
            f"obs_size={self._obs_size}, "
            f"num_actions={self._num_actions})"
        )
