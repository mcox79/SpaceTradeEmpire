---
name: screenshot
description: "Run the first-hour experience bot for visual verification. Default mode runs 31-phase player journey with 21 assertions + screenshots. Also supports regression, eval, transit, and custom scenario modes."
argument-hint: "<mode> [script-path]"
---

# /screenshot — Visual Verification Framework

The **first-hour experience bot** is the primary visual verification tool. It runs
a 31-phase, 6-act simulation of the full first-hour player journey — trading,
combat, missions, exploration — with 21 hard assertions at every milestone and
screenshot capture at key moments.

Parse `$ARGUMENTS` for the mode (first word). Default to `first-hour` if empty.

## Modes

| Mode | What it does | Time | Output |
|------|-------------|------|--------|
| `first-hour` | **Default.** 31-phase player journey with assertions + screenshots | ~90s | 18 screenshots + PASS/FAIL |
| `full` | 17-phase visual sweep (baseline-compatible for regression) | ~60s | ~24 screenshots |
| `transit` | Dense burst capture during lane transit | ~90s | 100+ burst frames |
| `video` | Record full sweep as AVI video | ~60s | AVI file |
| `eval` | Full sweep + LLM multi-perspective evaluation | ~60s + eval | screenshots + issue table |
| `regression` | Full sweep + compare against stored baselines | ~60s + compare | PASS/WARN/FAIL per phase |
| `scenario` | Run any custom bot script | varies | screenshots from bot |

---

## Step 1: Run the Capture

### Default mode (first-hour) — use the dedicated runner:

```bash
# Headless (assertions only, CI/verify compatible):
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot.ps1 -Mode headless

# Visual (assertions + screenshots):
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot.ps1 -Mode visual

# With seed variation (different galaxy each run):
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot.ps1 -Mode headless -Seed 42
```

### Other modes — use Run-Screenshot.ps1:

```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-Screenshot.ps1 -Mode <mode>

# Scenario mode (custom bot script):
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-Screenshot.ps1 -Mode scenario -Script "res://scripts/tests/my_bot.gd" -Prefix "MYBT"
```

Check exit code: 0 = PASS, 1 = FAIL.

---

## Step 2: Mode-Specific Post-Processing

### Mode: first-hour (default)

1. Check exit code: 0 = all 21 assertions passed, 1 = at least one failed.
2. Read the bot stdout for `FH1|` prefixed lines. Key lines:
   - `FH1|ASSERT_PASS|name|detail` — assertion passed
   - `FH1|ASSERT_FAIL|name|detail` — assertion failed (hard fail)
   - `FH1|FLAG|description` — soft warning (not a failure)
   - `FH1|SUMMARY|visited=N trades=N combats=N flags=N audit_critical=N hard_fail=bool`
   - `FH1|PASS|screenshots=N` or `FH1|FAIL|reason`
3. If FAIL: read `reports/first_hour/stdout.txt` and find all `ASSERT_FAIL` lines.
   Present a diagnostic table:

   | Assertion | Detail | Where to Look |
   |-----------|--------|---------------|
   | `boot_credits_positive` | credits=0 | WorldLoader initial state |
   | `dock_market_goods` | goods=0 | Market seeding, ContentRegistryLoader |
   | `buy_credits_decreased` | before=X after=X | BuyCommand.cs, SimBridge.Market.cs |
   | `arrival_different_system` | home=X current=X | Travel system, lane gates |
   | `profit_net_positive` | start=X now=X | Economy balance, price differentials |
   | `missions_available` | count=0 | MissionSystem, mission seeding |
   | `modules_available` | count=0 | Module content, GetAvailableModulesV0 |
   | `galaxy_nodes` | count=N | GalaxyGenerator, galaxy graph |
   | `price_diversity` | profiles=N | Market price variation per system |
   | `tech_available` | count=0 | TechTree content, GetTechTreeV0 |
   | `deep_explore_6_nodes` | visited=N | Graph connectivity, lane edges |

4. If visual mode: read a sample of screenshots from `reports/first_hour/`.
5. If PASS: brief confirmation with assertion count.

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

### Mode: eval

After screenshots are captured, evaluate them using a **Sonnet subagent** for
cost-efficient multi-perspective review (~48K image tokens saved from main context).

**Step 0 — Analyze the bot log in main context BEFORE dispatching the agent.**

Parse the bot stdout output (VSWP|... lines) to build a phase-by-phase manifest.

**Step 1 — Dispatch a Sonnet subagent for visual evaluation.**

Use the Task tool with `subagent_type: "general-purpose"` and `model: "sonnet"`.
Include the bot log manifest in the prompt.

The agent prompt MUST include:
1. The full bot log manifest
2. Instructions to Glob for `reports/screenshot/eval/*.png` and Read ALL PNGs
3. Instructions to Read the evaluation guide at `scripts/tools/visual_eval_guide.md`
4. The four evaluation perspectives (Player, Art Director, UX Designer, Game Designer)
5. Instructions to return a severity-ranked issue table

**Step 2 — Relay agent results to the user.**

### Mode: regression

1. Read the JSON output from the comparison script.
2. Report per-image verdicts in a table:

   | Phase | Verdict | MAD | Changed% | Notes |
   |-------|---------|-----|----------|-------|

3. For any FAIL verdicts: read both baseline and current PNGs and describe changes.
4. If no baselines exist, tell the user to run `/screenshot full` and copy to `reports/baselines/full/`.

### Mode: scenario

1. List all PNGs from the output directory.
2. Read each PNG and describe what it shows.
3. Report findings based on what the custom bot was testing.

---

## Seed Variation

The first-hour bot supports `--seed=N` to produce different galaxies, markets, and
NPC encounters each run. Use this to avoid screenshot fatigue (same images every time).

```bash
# Run with different seeds to see different game states:
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot.ps1 -Mode headless -Seed 42
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot.ps1 -Mode visual -Seed 1001
```

Each seed produces a different galaxy layout, different station markets, different
NPC fleet positions, and different trade opportunities. The 21 assertions validate
that the game works correctly regardless of seed.

---

## Verify Pipeline Integration

The first-hour bot can be used in `gates.json` verify arrays for UI/bridge gates:

```json
"verify": [
    "dotnet build SimCore/SimCore.csproj --nologo -v q",
    "powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot.ps1 -Mode headless"
]
```

Exit code 0 = all assertions pass. Exit code 1 = at least one assertion failed.

---

## Creating Baselines (for regression mode)

1. Run `/screenshot full` to capture current "known good" screenshots
2. Copy phase PNGs from `reports/visual_eval/` to `reports/baselines/full/`,
   renaming to just the phase label (e.g., `boot.png`)
3. Commit baselines to git

---

## Bot Scripts Reference

| Mode | Bot | Prefix | Output dir |
|------|-----|--------|-----------|
| first-hour | `scripts/tests/test_first_hour_proof_v0.gd` | `FH1\|` | `reports/first_hour/` |
| full/eval/regression | `scripts/tests/visual_sweep_bot_v0.gd` | `VSWP\|` | `reports/visual_eval/` |
| transit | `scripts/tests/lane_transfer_diag_bot.gd` | `LTDG\|` | `reports/lane_transfer_diag/` |
| video | `scripts/tests/visual_sweep_bot_v0.gd` | `VSWP\|` | `reports/screenshot/video/` |
| scenario | user-supplied script | user-supplied | `reports/screenshot/scenario_<name>/` |

## Runners Reference

| Runner | Purpose | Modes |
|--------|---------|-------|
| `scripts/tools/Run-FHBot.ps1` | **Primary.** First-hour bot with headless/visual + seed. | headless, visual |
| `scripts/tools/Run-Screenshot.ps1` | Secondary. Visual sweep, regression, eval, transit, scenario. | first-hour, full, transit, video, eval, regression, scenario |

## Troubleshooting

- **ASSERT_FAIL**: Read the assertion name and detail. Check the diagnostic table above.
- **0 screenshots**: Check stderr for `SCRIPT ERROR` or `Parse Error`. Likely a GDScript parse error.
- **Timeout**: Bot didn't complete in time. Increase timeout: `-TimeoutSec 180`.
- **Bridge not found**: C# build may have failed. Check `dotnet build` output.
- **Gray/blank screenshots**: Scene didn't load or window was minimized. Ensure Godot runs windowed.
