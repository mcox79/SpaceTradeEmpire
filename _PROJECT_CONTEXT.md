# SPACE TRADE EMPIRE - MASTER CONTEXT
# ===================================

## 1. THE FOUNDER PROTOCOL
# ------------------------
# - **Voice:** Pragmatic, Commercial, Technical.
# - **Output:** Direct Code Blocks -> Implementation Steps -> Verification.
# - **Meta-Rule:** If the project structure changes significantly (new systems), you MUST propose an updated version of this file.

## 2. RECOVERY & TOOLCHAIN
# ------------------------
# [DevTools Recovery Block]
#   If `DevTool.ps1` is lost, write a script that:
#   1. Concatenates all .gd/.tscn files into `_FullProjectContext.txt`.
#   2. Wraps `git add/commit` and `git reset --hard` into GUI buttons.

## 3. ARCHITECTURE & STANDARDS (STRICT)
# -------------------------------------
# - **File Org:**
#     - `/scenes`: Visuals & Prefabs.
#     - `/scripts`: Logic.
#     - `/assets`: Raw imports (models, sounds).
# - **Naming:** #     - Nodes: PascalCase (e.g., `ScoreLabel`).
#     - Scripts/Files: snake_case (e.g., `game_manager.gd`).
# - **Design Patterns:**
#     - **Signal Bus:** Use `GameManager` (Autoload) to pass data between unrelated objects (e.g., Asteroid -> UI).
#     - **Composition:** Avoid deep inheritance. Use Child Nodes to add features.
# - **The "Drone" Camera:**
#     - MUST be Top-Level or Code-Spawned. NEVER parented to Player.

## 4. CURRENT SPRINT STATUS
# -------------------------
# - **Phase:** MVP Economy Implementation.
# - **Active Task:** Wiring `Asteroid` -> `GameManager` -> `HUD`.
# - **Next Up:** "Infinite Demand" (Spawner System).
