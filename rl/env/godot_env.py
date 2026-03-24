"""Gymnasium environment that connects to the Godot RL agent bot via TCP.

This tests the FULL Godot stack: SimBridge C# threading, GDScript presentation,
scene tree lifecycle. Slower than headless (~200 steps/sec vs 50K) but catches
integration bugs the headless version cannot.

Usage:
    env = GodotSpaceTradeEnv(godot_path="path/to/godot", project_path="path/to/project")
    obs, info = env.reset()
    obs, reward, terminated, truncated, info = env.step(action)
"""

import json
import os
import socket
import subprocess
import sys
import time
from pathlib import Path

import gymnasium as gym
import numpy as np
from gymnasium import spaces

OBS_SIZE = 137
NUM_ACTIONS = 34
DEFAULT_PORT = 11008


class GodotSpaceTradeEnv(gym.Env):
    """Gymnasium env connected to a Godot RL agent bot via TCP.

    Spawns Godot in headless mode running rl_agent_bot.gd, which opens
    a TCP server. This env connects as a client and sends reset/step commands.
    """

    metadata = {"render_modes": []}

    def __init__(
        self,
        godot_path: str | None = None,
        project_path: str | None = None,
        port: int = DEFAULT_PORT,
        max_episode_ticks: int = 2000,
        startup_timeout: float = 30.0,
    ):
        super().__init__()

        self.observation_space = spaces.Box(
            low=-1.0, high=5.0, shape=(OBS_SIZE,), dtype=np.float32
        )
        self.action_space = spaces.Discrete(NUM_ACTIONS)

        self._godot_path = godot_path or self._find_godot()
        self._project_path = project_path or str(Path(__file__).resolve().parent.parent.parent)
        self._port = port
        self._max_episode_ticks = max_episode_ticks
        self._startup_timeout = startup_timeout

        self._proc: subprocess.Popen | None = None
        self._sock: socket.socket | None = None
        self._recv_buffer = b""
        self._last_action_mask: np.ndarray | None = None

    def _find_godot(self) -> str:
        """Auto-detect Godot binary from godot_path.cfg or common locations."""
        repo_root = Path(__file__).resolve().parent.parent.parent
        cfg = repo_root / "godot_path.cfg"
        if cfg.exists():
            path = cfg.read_text().strip()
            if Path(path).exists():
                return path

        # Common locations
        candidates = [
            r"C:\Godot\Godot_v4.4.1-stable_mono_win64\Godot_v4.4.1-stable_mono_win64.exe",
            r"C:\Godot\godot.exe",
        ]
        for c in candidates:
            if Path(c).exists():
                return c

        return "godot"  # Hope it's on PATH

    def _start_godot(self):
        """Spawn Godot headless with the RL agent bot."""
        if self._proc is not None and self._proc.poll() is None:
            return

        cmd = [
            self._godot_path,
            "--headless",
            "--path", self._project_path,
            "-s", "res://scripts/tests/rl_agent_bot.gd",
            "--", "--port", str(self._port),
        ]

        self._proc = subprocess.Popen(
            cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )

    def _connect(self):
        """Connect to the Godot TCP server with retry."""
        if self._sock is not None:
            return

        self._start_godot()

        deadline = time.time() + self._startup_timeout
        while time.time() < deadline:
            try:
                sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                sock.settimeout(2.0)
                sock.connect(("127.0.0.1", self._port))
                sock.settimeout(30.0)
                self._sock = sock
                self._recv_buffer = b""
                return
            except (ConnectionRefusedError, OSError):
                time.sleep(0.5)
                # Check if Godot crashed
                if self._proc is not None and self._proc.poll() is not None:
                    stdout = self._proc.stdout.read().decode() if self._proc.stdout else ""
                    stderr = self._proc.stderr.read().decode() if self._proc.stderr else ""
                    raise RuntimeError(
                        f"Godot process exited with code {self._proc.returncode}\n"
                        f"stdout: {stdout[:500]}\nstderr: {stderr[:500]}"
                    )

        raise TimeoutError(f"Could not connect to Godot RL agent on port {self._port} within {self._startup_timeout}s")

    def _send(self, request: dict) -> dict:
        """Send JSON line and read JSON line response."""
        if self._sock is None:
            self._connect()

        assert self._sock is not None
        data = json.dumps(request, separators=(",", ":")) + "\n"
        self._sock.sendall(data.encode("utf-8"))

        # Read until we get a complete line
        while b"\n" not in self._recv_buffer:
            chunk = self._sock.recv(65536)
            if not chunk:
                raise RuntimeError("Godot RL agent disconnected")
            self._recv_buffer += chunk

        line, self._recv_buffer = self._recv_buffer.split(b"\n", 1)
        return json.loads(line.decode("utf-8"))

    def reset(self, seed=None, options=None):
        super().reset(seed=seed)

        if self._sock is None:
            self._connect()

        resp = self._send({
            "type": "reset",
            "seed": seed or 0,
            "max_episode_ticks": self._max_episode_ticks,
        })

        if resp.get("type") == "error":
            raise RuntimeError(f"Godot reset error: {resp.get('error')}")

        obs = np.array(resp["obs"], dtype=np.float32)
        info = resp.get("info", {})

        if resp.get("action_mask"):
            self._last_action_mask = np.array(resp["action_mask"], dtype=bool)
            info["action_mask"] = self._last_action_mask

        return obs, info

    def step(self, action):
        resp = self._send({
            "type": "step",
            "action": int(action),
        })

        if resp.get("type") == "error":
            raise RuntimeError(f"Godot step error: {resp.get('error')}")

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
        if self._last_action_mask is not None:
            return self._last_action_mask
        return np.ones(NUM_ACTIONS, dtype=bool)

    def close(self):
        if self._sock is not None:
            try:
                self._send({"type": "shutdown"})
            except Exception:
                pass
            self._sock.close()
            self._sock = None

        if self._proc is not None:
            self._proc.wait(timeout=10)
            self._proc = None

        super().close()
