"""Manages the C# SimCore.RlServer subprocess.

Communicates via stdin/stdout JSON lines. Each send() writes one JSON line
and reads one JSON line response. The subprocess is spawned on first send()
or explicitly via start().
"""

import json
import os
import subprocess
import sys
from pathlib import Path


class ServerProcess:
    """Wrapper around the SimCore.RlServer subprocess."""

    def __init__(self, exe_path: str | None = None, build_first: bool = False):
        """
        Args:
            exe_path: Path to the compiled SimCore.RlServer executable.
                      If None, auto-detects from standard build output.
            build_first: If True, run dotnet build before spawning.
        """
        self._exe_path = exe_path or self._find_exe()
        self._build_first = build_first
        self._proc: subprocess.Popen | None = None

    def _find_exe(self) -> str:
        """Auto-detect the RlServer executable path."""
        repo_root = Path(__file__).resolve().parent.parent.parent
        # Release build output
        candidates = [
            repo_root / "SimCore.RlServer" / "bin" / "Release" / "net8.0" / "SimCore.RlServer.exe",
            repo_root / "SimCore.RlServer" / "bin" / "Release" / "net8.0" / "SimCore.RlServer",
            repo_root / "SimCore.RlServer" / "bin" / "Debug" / "net8.0" / "SimCore.RlServer.exe",
            repo_root / "SimCore.RlServer" / "bin" / "Debug" / "net8.0" / "SimCore.RlServer",
        ]
        for c in candidates:
            if c.exists():
                return str(c)
        # Fallback to dotnet run
        return "dotnet"

    def start(self):
        """Start the subprocess if not already running."""
        if self._proc is not None and self._proc.poll() is None:
            return

        if self._build_first:
            repo_root = Path(__file__).resolve().parent.parent.parent
            subprocess.run(
                ["dotnet", "build", "SimCore.RlServer/SimCore.RlServer.csproj", "-c", "Release", "--nologo", "-v", "q"],
                cwd=str(repo_root),
                check=True,
                capture_output=True,
            )

        if self._exe_path == "dotnet":
            repo_root = Path(__file__).resolve().parent.parent.parent
            cmd = ["dotnet", "run", "--project", "SimCore.RlServer/SimCore.RlServer.csproj", "-c", "Release"]
            cwd = str(repo_root)
        else:
            cmd = [self._exe_path]
            cwd = None

        self._proc = subprocess.Popen(
            cmd,
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL,
            text=True,
            bufsize=1,  # line-buffered
            cwd=cwd,
        )

    def send(self, request: dict) -> dict:
        """Send a JSON request and read the JSON response."""
        if self._proc is None or self._proc.poll() is not None:
            self.start()

        assert self._proc is not None
        assert self._proc.stdin is not None
        assert self._proc.stdout is not None

        line = json.dumps(request, separators=(",", ":"))
        self._proc.stdin.write(line + "\n")
        self._proc.stdin.flush()

        response_line = self._proc.stdout.readline()
        if not response_line:
            raise RuntimeError("RlServer process closed unexpectedly")

        return json.loads(response_line)

    def close(self):
        """Gracefully shut down the server."""
        if self._proc is not None and self._proc.poll() is None:
            try:
                self.send({"type": "shutdown"})
            except Exception:
                pass
            self._proc.wait(timeout=5)
            self._proc = None

    def __del__(self):
        self.close()
