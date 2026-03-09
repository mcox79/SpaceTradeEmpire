---
name: screenshot
description: "Capture game screenshots. Modes: quick (4 shots), full (24 shots), transit (burst), video (AVI), eval (LLM review), regression (baseline diff), scenario (custom bot script)."
argument-hint: "<mode> [script-path]"
---

# /screenshot — Visual Testing Framework

A **programmable visual testing framework**, not a fixed screenshot tool.
Each mode runs a GDScript bot that controls Godot — navigating menus, flying
to locations, triggering UI, and capturing screenshots at each step. You can
test **any visual scenario** by writing a bot script for it.

Parse `$ARGUMENTS` for the mode (first word). Default to `quick` if empty.

## Modes

| Mode | What it does | Time | Output |
|------|-------------|------|--------|
| `quick` | Boot, dock market, galaxy map, final | ~15s | 4 screenshots |
| `full` | Full 17-phase visual sweep | ~60s | ~24 screenshots |
| `transit` | Dense burst capture during lane transit | ~90s | 100+ burst frames |
| `video` | Record full sweep as AVI video | ~60s | AVI file |
| `eval` | Full sweep + LLM multi-perspective evaluation | ~60s + eval | screenshots + issue table |
| `regression` | Full sweep + compare against stored baselines | ~60s + compare | PASS/WARN/FAIL per phase |
| `scenario` | Run any custom bot script | varies | screenshots from bot |

---

## Key Principle: The Framework Is Programmable

The built-in modes (quick, full, etc.) are just **pre-built bot scripts**. The
screenshot framework can test ANY visual scenario by writing a new GDScript bot:

- Gate popup flow (fly to gate → capture popup → dismiss → capture again)
- Combat UI (engage fleet → capture damage numbers → capture loot screen)
- Dock menu tabs (open each tab → capture → verify layout)
- Pause menu (trigger pause → capture overlay → resume)
- Any UI interaction, any game state, any sequence

To test a custom scenario, either:
1. Use `scenario` mode: `/screenshot scenario res://scripts/tests/my_bot.gd`
2. Write a new mode-specific bot and add it to the mode config

**When asked "can the screenshot bot test X?" — the answer is always YES.**
The only question is whether a bot script exists for it yet.

---

## Step 1: Run the Capture

```bash
# Standard modes
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-Screenshot.ps1 -Mode <mode>

# Scenario mode (custom bot script)
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-Screenshot.ps1 -Mode scenario -Script "res://scripts/tests/my_bot.gd" -Prefix "MYBT"
```

Check exit code and bot output for errors. If 0 screenshots captured, check the
stderr output for `SCRIPT ERROR` or `Parse Error` lines.

Expected screenshot counts:
- `quick`: 4 screenshots
- `full`: 20-24 screenshots
- `transit`: 50-200+ burst frames
- `video`: AVI file (no individual screenshots)
- `eval`: 20-24 screenshots (same as full)
- `regression`: 20-24 screenshots (same as full)
- `scenario`: depends on the bot script

---

## Step 2: Mode-Specific Post-Processing

### Mode: quick

1. Read all PNGs from `reports/screenshot/quick/` using the Read tool.
2. Read `reports/screenshot/quick/summary.json` for aesthetic audit results.
3. Give a brief visual assessment: does the game look correct at boot, dock, and galaxy map?
4. Report any critical audit failures.

### Mode: full

1. List all PNGs from `reports/screenshot/full/`.
2. Read `reports/screenshot/full/summary.json` for aesthetic audit results.
3. Read a representative sample of screenshots (boot, dock_market, galaxy_map, final).
4. Report screenshot count and critical failures.

### Mode: transit

1. List all PNGs from `reports/screenshot/transit/`.
2. Report state transitions detected in stdout (STATE_CHANGE lines).
3. Report total burst frames captured and transit duration.
4. Read 3-4 key frames: pre-warp, mid-transit, arrival cinematic, post-arrival.

### Mode: video

1. Report the AVI file path and size from the runner output.
2. Tell the user: "Open `reports/screenshot/video/sweep.avi` in any media player to review."
3. If no AVI file was produced, check stderr and report the issue.
   Note: `--write-movie` with `-s` scripts may not work in all Godot versions.
   Fallback: suggest running the full sweep bot normally (it captures PNG screenshots).

### Mode: eval

After screenshots are captured, evaluate them with aggressive multi-perspective review.

**Step 0 — Analyze the bot log BEFORE looking at screenshots.**

Parse the bot stdout output (VSWP|... lines) to build a phase-by-phase manifest
of what actually happened. Key lines to extract:

- `VSWP|NPC_DAMAGE|hits=N|dmg=N` — confirms combat occurred and how much damage
- `VSWP|WARP_TRIGGER|star_N` — confirms lane transit was initiated
- `VSWP|WARP_VFX|pos=(...)` — confirms warp VFX sequence started
- `VSWP|REBUILD|star_N` — confirms system arrival/rebuild
- `VSWP|DOCK|...|groups=[...]` — confirms docking happened
- `VSWP|UNDOCK` — confirms undock
- `VSWP|AESTHETIC|FAIL|...` — automated audit failures

Use this manifest as ground truth when interpreting screenshots. Do NOT conclude
that a game mechanic "didn't happen" based on screenshot appearance alone — the
bot log is authoritative.

**Common LLM vision pitfalls to avoid:**

- **Lane transit frames** show dark interstellar space with a blue lane line and
  nebula backdrop. Do NOT confuse this with a dock menu background or empty scene.
- **Combat at zoomed-out view** may show no dramatic weapon effects in static
  screenshots. Check bot log for NPC_DAMAGE to confirm combat occurred.
- **Warp VFX** may be one prominent frame (large cyan sphere) followed by several
  fadeout frames. Don't rate the whole sequence by the later frames alone.

1. Read ALL PNGs from `reports/screenshot/eval/` using the Read tool in parallel
   batches of 6 for efficiency.
2. Read the evaluation guide at `scripts/tools/visual_eval_guide.md`.
3. For EACH screenshot, evaluate from four perspectives:

   **A. Player First Impression**
   - Would a new player understand what they're seeing?
   - Does this screen make them want to keep playing?
   - Is there a "wow" moment or is it flat/boring?

   **B. Art Director**
   - Composition: balanced frame? Clear focal point?
   - Color: coherent palette? Contrast against space?
   - Visual hierarchy: what does the eye see first?
   - Label/text rendering: overlap, clipping, wrong size?
   - Lighting: scene feels lit or flat?
   - Object scale: ships/stations/planets at right relative size?

   **C. UX Designer**
   - Information density: too much, too little, right amount?
   - Text readability: font size, contrast, background
   - Menu layout: alignment, spacing, visual grouping
   - State communication: does HUD show current state?
   - Debug leaks: any developer-facing text visible?

   **D. Game Designer**
   - Feeling: does this moment create the intended emotion?
     (Boot → wonder, Dock → commerce, Combat → tension, Warp → speed, Map → strategy)
   - Differentiation: do different systems feel different?
   - Drama: are dramatic moments visually dramatic?

4. Output a severity-ranked issue table:

   | # | Severity | Issue | Screenshots |
   |---|----------|-------|-------------|
   | 1 | CRITICAL | Description | which frames |
   | 2 | HIGH | Description | which frames |
   | ... | MEDIUM/LOW | ... | ... |

   Severity: CRITICAL = visually broken, HIGH = harms experience, MEDIUM = noticeable, LOW = polish.

5. Note what's working well — things to preserve or expand.

### Mode: regression

1. The runner automatically invokes `compare_screenshots.py` against baselines
   in `reports/baselines/full/`.
2. Read the JSON output from the comparison script.
3. Report per-image verdicts in a table:

   | Phase | Verdict | MAD | Changed% | Notes |
   |-------|---------|-----|----------|-------|

4. For any FAIL verdicts: read both the baseline and current PNGs and describe
   what changed visually.
5. If no baselines exist, tell the user:
   "No baselines found. Run `/screenshot full` first, then copy the phase PNGs
   to `reports/baselines/full/` with standardized names (strip tick/timestamp)."

### Mode: scenario

1. List all PNGs from the output directory shown in the runner output.
2. Read each PNG and describe what it shows.
3. Check stderr for errors.
4. Report findings based on what the custom bot was testing.

---

## Writing Custom Bot Scripts

To test a specific visual scenario, write a GDScript bot that `extends SceneTree`:

```gdscript
extends SceneTree

# Use screenshot_capture.gd for captures
var _capturer = preload("res://scripts/tools/screenshot_capture.gd").new()

func _initialize() -> void:
    print("MYBT|START")
    # Load your scene, set up state
    call_deferred("_run")

func _run() -> void:
    # ... navigate to the state you want to test ...
    # ... trigger UI interactions ...
    _capturer.capture_v0(self, "phase_name", "res://reports/screenshot/scenario_mybot/")
    # ... continue testing ...
    quit(0)
```

Key patterns from existing bots:
- `screenshot_capture.gd` has `capture_v0(tree, label, output_dir)` — writes PNGs
- Use `SimBridge` calls to set up game state (dock, travel, buy/sell, combat)
- Use `aesthetic_audit.gd` for automated pixel checks (`run_pixel_audit_v0(img)`)
- State machine pattern: WAIT_BRIDGE → WAIT_READY → your test phases → DONE
- Call `bridge.call("StopSimV0")` before `quit()` to avoid process hang

---

## Baseline Management (for regression mode)

To create or update baselines:

1. Run `/screenshot full` to capture current "known good" screenshots
2. Copy phase PNGs from `reports/screenshot/full/` (or `reports/visual_eval/`)
   to `reports/baselines/full/`, renaming to just the phase label (e.g., `boot.png`)
3. Commit baselines to git

To check regression:
```
/screenshot regression
```

---

## Bot Scripts Reference

| Mode | Bot | Output prefix | Output dir |
|------|-----|--------------|-----------|
| quick | `scripts/tests/quick_screenshot_bot.gd` | `QSCR\|` | `reports/screenshot/quick/` |
| full/eval/regression | `scripts/tests/visual_sweep_bot_v0.gd` | `VSWP\|` | `reports/visual_eval/` → copied |
| transit | `scripts/tests/lane_transfer_diag_bot.gd` | `LTDG\|` | `reports/lane_transfer_diag/` → copied |
| video | `scripts/tests/visual_sweep_bot_v0.gd` | `VSWP\|` | `reports/screenshot/video/` |
| scenario | user-supplied script | user-supplied | `reports/screenshot/scenario_<name>/` |

## Troubleshooting

- **0 screenshots**: Check stderr for `SCRIPT ERROR` or `Parse Error`. Likely a GDScript parse error.
- **Timeout**: Bot didn't complete in time. Increase timeout: `-TimeoutSec 180`.
- **Gray/blank screenshots**: Scene didn't load or window was minimized. Ensure Godot runs windowed.
- **Video mode fails**: `--write-movie` may not work with `-s` scripts. Use `full` mode instead.
- **Regression has no baselines**: Run `full` first, copy screenshots to `reports/baselines/full/`.
- **"Can the bot test X?"**: YES. Write a bot script that navigates to that state and captures. Use `scenario` mode to run it.
