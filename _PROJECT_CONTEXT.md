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
#
# [PhantomCamera Status]
#   The `addons/phantom_camera` folder may exist on disk, but it is not enabled as a plugin and no autoload is configured.
#   Do not use PhantomCamera unless we explicitly choose it and update this document.
## 3. ARCHITECTURE & STANDARDS (STRICT)
# -------------------------------------
# - **File Org:**
#     - `/scenes`: Visuals & Prefabs.
#     - `/scripts`: Logic.
#     - `/assets`: Raw imports (models, sounds).
# - **Naming:** #     - Nodes: PascalCase (e.g., `ScoreLabel`).
#     - Scripts/Files: snake_case (e.g., `game_manager.gd`).
# - **Design Patterns:**
#     - **Signal Bus (CURRENT):** Use direct signals for now (e.g., Player -> HUD). Add an Autoload bus only when two unrelated systems need it.
#     - **Composition:** Avoid deep inheritance. Use Child Nodes to add features.
# - **Camera Law (ACTIVE)**
#     - Camera is defined in `scenes/player.tscn` at `CameraMount/Camera3D`.
#     - No runtime camera creation in scripts. (`Camera3D.new()` is banned in gameplay code.)
#     - Only `scripts/player.gd` may set `my_camera.current = true`, and only for `CameraMount/Camera3D`.
#     - No other scene may define a gameplay camera without an explicit decision and doc update.
#     - Debug cameras must live in dedicated debug-only scenes (never attached to production nodes).

## 4. CURRENT SPRINT STATUS
# -------------------------
# - **Phase:** Phase 0 Cleanup (truth alignment + hygiene)
# - **Done:**
#     - Player camera is scene-defined at `scenes/player.tscn` -> `CameraMount/Camera3D`.
#     - No runtime camera spawning in gameplay scripts.
#     - PhantomCamera plugin and autoload are disabled in `project.godot` (addon may exist on disk).
# - **Next (Phase 1):** Economy spine unification and 2-station arbitrage loop (buy, haul, sell).

# DEVTOOL_RESET_TEST_MARKER 2026-01-22T14:24:01.5755565-05:00

