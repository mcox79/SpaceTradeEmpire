#!/usr/bin/env python3
from __future__ import annotations

import argparse
import subprocess
from pathlib import Path


def leading_ws(line: str) -> str:
    i = 0
    while i < len(line) and line[i] in ("\t", " "):
        i += 1
    return line[:i]


def check_file(path: Path) -> list[str]:
    problems: list[str] = []

    try:
        text = path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        problems.append(f"{path}:0: file is not UTF-8 decodable")
        return problems

    for idx, line in enumerate(text.splitlines(), start=1):
        ws = leading_ws(line)
        if not ws:
            continue

        # Tabs only for leading indentation
        if " " in ws:
            problems.append(f"{path}:{idx}: leading indentation contains spaces")

        if "\t" in ws and " " in ws:
            problems.append(f"{path}:{idx}: leading indentation mixes tabs and spaces")

    return problems


def git_staged_files(repo_root: Path) -> list[Path]:
    out = subprocess.check_output(
        ["git", "diff", "--cached", "--name-only", "--diff-filter=ACMR"],
        cwd=str(repo_root),
    )
    files = out.decode("utf-8", errors="replace").splitlines()
    return [repo_root / f for f in files if f.strip()]


def git_all_tracked_files(repo_root: Path) -> list[Path]:
    out = subprocess.check_output(["git", "ls-files"], cwd=str(repo_root))
    files = out.decode("utf-8", errors="replace").splitlines()
    return [repo_root / f for f in files if f.strip()]


def is_under(path: Path, root: Path) -> bool:
    try:
        path.resolve().relative_to(root.resolve())
        return True
    except Exception:
        return False


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--mode", choices=["staged", "all"], default="staged")
    ap.add_argument("--include-addons", action="store_true")
    args = ap.parse_args()

    repo_root = Path(__file__).resolve().parents[2]  # scripts/tools -> scripts -> repo root

    files = git_staged_files(repo_root) if args.mode == "staged" else git_all_tracked_files(repo_root)
    gd_files = [p for p in files if p.suffix.lower() == ".gd" and p.exists()]

    # Scope: enforce only on scripts/ by default
    scripts_root = repo_root / "scripts"
    addons_root = repo_root / "addons"

    scoped: list[Path] = []
    for p in gd_files:
        if is_under(p, scripts_root):
            scoped.append(p)
            continue
        if args.include_addons and is_under(p, addons_root):
            scoped.append(p)

    problems: list[str] = []
    for p in scoped:
        problems.extend(check_file(p))

    if problems:
        print("GDScript indentation check failed. Tabs only for leading indentation.\n")
        for msg in problems:
            print(msg)
        print("\nFix: convert leading indentation to tabs. No leading spaces allowed.")
        print(f"\nChecked {len(scoped)} .gd files (mode={args.mode}, include_addons={args.include_addons}).")
        return 1

    print(f"OK: checked {len(scoped)} .gd files (mode={args.mode}, include_addons={args.include_addons}).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())