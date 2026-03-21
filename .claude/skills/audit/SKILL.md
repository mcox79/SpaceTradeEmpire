---
name: audit
description: "Full-game audit: runs all evaluation skills, compiles unified problem list, fixes issues, identifies and fills evaluation coverage gaps. Iterative."
argument-hint: "[mode: full|eval-only|fix-only|gaps] [iteration-name]"
---

# /audit — Full-Game Audit & Auto-Fix

Orchestrates all evaluation skills (`/screenshot`, `/first-hour`, `/feel`,
`/optimize`), compiles a unified problem list, fixes issues by priority, and
identifies gaps in evaluation coverage — then fills those gaps iteratively.

Parse `$ARGUMENTS`:
- **mode** (first word): `full` (default) | `eval-only` | `fix-only` | `gaps`
  - `full` — evaluate → compile → fix → re-evaluate → evolve coverage
  - `eval-only` — run all evals and compile, no fixes
  - `fix-only` — skip evals, read latest reports, fix top problems
  - `gaps` — coverage gap analysis only (no bot runs, fast)
- **iteration-name** (second word, optional): e.g., `baseline`, `post-combat-fix`
  - Default: `audit_1`, `audit_2`, etc., based on existing folders

---

## Step 0 — Snapshot & Build

1. Record git SHA: `git rev-parse --short HEAD`
2. Build C#:
   ```bash
   dotnet build SimCore/SimCore.csproj --nologo -v q
   ```
3. Create output dir: `reports/audit/<iteration-name>/`
4. If mode is `fix-only`, skip to Step 3. If mode is `gaps`, skip to Step 2.

---

## Step 1 — Run Evaluations

Run evaluations in dependency order. Parallelize where possible.

### Step 1a + 1b (parallel)

Launch these two in parallel — neither depends on the other:

**1a: First-Hour Bot** (produces screenshots + structured stdout)
```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot.ps1 -Mode headless
```
Output: `reports/first_hour/stdout.txt`, `reports/first_hour/*.png`

**1b: Optimize Pass 1** (Grep-based pattern scan)
```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-OptimizeScan.ps1
```
Output: `reports/optimization/scan_*.md`

If either fails, log the error and continue with the other results. Do NOT abort
the entire audit for a single eval failure.

### Step 1c — First-Hour LLM Evaluation (depends on 1a)

After the bot completes, run the first-hour LLM evaluation inline:

1. Read `reports/first_hour/stdout.txt`
2. Parse all `FH1|ASSERT_*`, `FH1|GOAL|*`, `FH1|FLAG|*`, `FH1|PERF|*` lines
3. Read `scripts/tools/first_hour_rubric.md` for scoring criteria
4. Score each of the 5 goals (1-5) with evidence
5. Generate prescriptions using the rubric's tag system (BUG/UX/POLISH/GAP/SUPPRESSED/UNWIRED)
6. Write to `reports/audit/<iteration>/first_hour_eval.md`

### Step 1d — Feel LLM Evaluation (depends on 1a)

Using screenshots from the bot run:

1. Read all PNG files from `reports/first_hour/`
2. Read `scripts/tools/visual_eval_guide.md` for visual vocabulary and reference standards
3. Evaluate each screenshot across 5 dimensions: Composition, Readability, Scale & Space, Polish, Atmosphere
4. Apply 5 perspectives: first-time player, art director, UX designer, game designer, space game fan
5. Rate each screenshot PASS/NEEDS_WORK/FAIL per dimension
6. Write to `reports/audit/<iteration>/feel_eval.md`

**Token budget**: Limit to 10 most representative screenshots (not all). Pick one
per distinct game state: boot, docked, flight, combat, galaxy map, warp, haven,
discovery, FO dialogue, trade menu.

### Step 1e — Optimize Deep Passes (depends on 1b)

Run optimize passes 2-3 (highest signal after Pass 1):

**Pass 2: Hot-Path Allocation Scan** — Use a Haiku agent to scan 3-4 System files
at a time for LINQ chains, new collections, boxing in `Process()` methods.

**Pass 3: Architecture & Lock Discipline** — Scan SimBridge partials for lock
pattern violations, bridge contract mismatches.

Write findings to `reports/audit/<iteration>/optimize_eval.md`

Skip passes 4-7 unless mode is `full` — they are lower signal-to-cost.

---

## Step 2 — Coverage Gap Analysis

This is the core differentiator of `/audit` vs running skills individually.
Detect what the evaluation framework does NOT cover.

### 2a — Bridge Method Coverage

```
1. Grep all public methods in bridge:
   Grep pattern="public .+ \w+V0\(" path="scripts/bridge/" type="cs"

2. Grep all bridge method calls in bots:
   Grep pattern="bridge\.call\(\"(\w+)\"" path="scripts/tests/" type="gd"

3. Diff: methods that exist in bridge but are never called by any bot
4. For each uncovered method, check if any UI file calls it:
   Grep pattern="<method_name>" path="scripts/ui/" type="gd"

5. Classify each uncovered method:
   - UNEXERCISED_WITH_UI: bridge method exists, UI calls it, but no bot tests it
   - UNEXERCISED_NO_UI: bridge method exists, nothing calls it (dead or unwired)
   - EXERCISED: called by at least one bot
```

### 2b — UI Screen Coverage

```
1. List all UI scripts:
   Glob pattern="scripts/ui/*.gd"

2. For each UI script, check if any bot references it or its key functions:
   Grep pattern="<script_name_stem>" path="scripts/tests/" type="gd"

3. Check screenshot coverage — which game states have screenshots:
   Glob pattern="reports/first_hour/*.png"

4. Flag UI screens that have NEVER been visually captured or bot-tested
```

### 2c — System Coverage

```
1. List all simulation systems:
   Glob pattern="SimCore/Systems/*.cs"

2. For each system, check:
   a. Does a bot exercise it? Grep system name in bot scripts
   b. Does it have a dedicated test? Grep in SimCore.Tests/
   c. Does the first-hour rubric mention it?

3. Flag systems with:
   - NO bot exercise AND no targeted test → UNCOVERED
   - Has test but no bot exercise → UNIT_ONLY (no integration coverage)
   - Has bot exercise → COVERED
```

### 2d — Eval Dimension Gaps

```
1. First-hour rubric dimensions vs actual bot probes:
   - Read rubric sections from scripts/tools/first_hour_rubric.md
   - Grep "PROBE_" lines in bot stdout
   - Flag rubric dimensions with no corresponding probe

2. Game states NOT screenshot-covered:
   - Enumerate known states: boot, docked, flight, combat, warp_transit,
     galaxy_map, haven, discovery_site, fracture, loss_screen, victory_screen
   - Check which have screenshots in reports/
   - Flag uncaptured states

3. Optimize scope gaps:
   - Check if these directories are scanned: SimCore/Gen/, SimCore/Entities/,
     SimCore/Content/, SimCore/Tweaks/
   - Flag directories with zero optimize findings (may be unscanned)
```

### Output

Write `reports/audit/<iteration>/coverage_gaps.md`:

```markdown
# Coverage Gap Analysis — <iteration>

## Bridge Method Coverage
- Total methods: N
- Exercised by bots: N (X%)
- Unexercised with UI: N (list)
- Unexercised no UI: N (list)

## UI Screen Coverage
- Total screens: N
- Bot-tested: N (X%)
- Never tested: (list)

## System Coverage
- Total systems: N
- Covered (bot + test): N
- Unit-only: N
- Uncovered: N (list with severity)

## Eval Dimension Gaps
- Rubric dimensions without probes: (list)
- Game states without screenshots: (list)
- Optimize blind spots: (list)

## Priority Coverage Gaps (ranked)
1. [most impactful gap] — why it matters
2. ...
```

---

## Step 3 — Compile Unified Problem List

Parse ALL evaluation outputs (from Step 1 + Step 2) into a single ranked list.

### Input Sources

| Source | Report File | Problem Format |
|--------|------------|----------------|
| First-hour | `first_hour_eval.md` | Prescriptions with goal/severity/tag |
| Feel | `feel_eval.md` | Per-screenshot NEEDS_WORK/FAIL ratings |
| Optimize | `optimize_eval.md` | CRITICAL/WARNING/SUGGESTION findings |
| Coverage | `coverage_gaps.md` | Gap classifications |

### Unified Format

For each problem:

```
PROBLEM #N
  Source:       [first-hour | feel | optimize | coverage-gap]
  Severity:     [critical | major | minor | suggestion]
  Confidence:   [high | medium | low]
  Tag:          [BUG | UX | POLISH | GAP | PERF | ARCH | COVERAGE]
  Domain:       [combat | economy | discovery | tutorial | haven | UI | ...]
  Issue:        One sentence describing the problem
  Evidence:     Which report + line/screenshot supports this
  Prescription: Specific actionable fix
  Auto-fixable: [yes | no | needs-design]
  Files:        [list of files to modify]
```

### Severity Mapping

| Source | Source Level | Unified Severity |
|--------|-------------|-----------------|
| Optimize | CRITICAL | critical |
| First-hour | critical + BUG tag | critical |
| First-hour | major + UX tag | major |
| Feel | FAIL rating | major |
| Coverage | UNCOVERED system | major |
| Feel | NEEDS_WORK | minor |
| Coverage | UNIT_ONLY system | minor |
| Optimize | SUGGESTION | suggestion |
| Any | OPINION tag | suggestion (always lowest) |

### Deduplication

- Same file + same issue from multiple evals → merge, keep highest severity
- Coverage gaps that overlap with existing prescriptions → merge into the prescription
- Feel + first-hour flagging the same screenshot → merge

### Output

Write `reports/audit/<iteration>/unified_problems.md`:

```markdown
# Unified Problem List — <iteration>

## Summary
- Total: N problems (X critical, Y major, Z minor, W suggestion)
- Auto-fixable: N
- Needs-design: N
- Coverage gaps: N

## Critical Problems
PROBLEM #1 ...
PROBLEM #2 ...

## Major Problems
...

## Minor Problems
...

## Suggestions
...
```

If mode is `eval-only`, STOP HERE. Present the unified list to the user.

---

## Step 4 — Fix Pass

Work through unified problems, highest severity first.

### 4a — Auto-Fixable Problems

For each problem tagged `Auto-fixable: yes`:

1. Read the target file(s)
2. Apply the fix
3. After every 5-10 fixes, verify:
   ```bash
   dotnet build SimCore/SimCore.csproj --nologo -v q
   dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q
   ```
4. If a fix breaks tests, revert it and mark as `needs-design`

### 4b — Needs-Design Problems

For each problem tagged `Auto-fixable: needs-design`:

1. Present the problem + 2-3 possible approaches to the user
2. Wait for user decision
3. Apply the chosen approach
4. Verify build + tests

### 4c — Coverage Gap Fixes

For each coverage gap (from Step 2):

**New bridge method exercises:**
- Append a new test phase to `scripts/tests/test_deep_systems_v0.gd`
- Pattern: `bridge.call("<MethodV0>")` + assert result is valid
- Group by domain (combat methods together, economy together, etc.)

**New eval dimensions:**
- Add new probe lines to bot scripts (`FH1|GOAL|...` or `DS1|PROBE|...`)
- Update `scripts/tools/first_hour_rubric.md` with new dimension definitions
- Update `scripts/tools/visual_eval_guide.md` if new visual states need coverage

**New optimize patterns:**
- Add new Grep patterns to optimize SKILL.md Pass 1 tables
- Or add new LLM scan targets to Pass 2-7 instructions

After coverage fixes, verify:
```bash
dotnet build "Space Trade Empire.csproj" --nologo
```
Then re-run the bot to confirm new phases don't crash:
```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot.ps1 -Mode headless
```

### Fix Log

Track all fixes in `reports/audit/<iteration>/fix_log.md`:

```markdown
# Fix Log — <iteration>

| # | Problem | File(s) | Fix Applied | Verified |
|---|---------|---------|-------------|----------|
| 1 | ... | ... | ... | BUILD OK / TEST OK |
```

---

## Step 5 — Re-Evaluate (verify fixes)

After the fix pass, re-run ONLY the evals that found fixed problems:

- If first-hour problems were fixed → re-run bot + first-hour eval
- If feel problems were fixed → re-run screenshot + feel eval
- If optimize problems were fixed → re-run optimize Pass 1

Compare before vs after:

```markdown
# Fix Delta — <iteration>

| Dimension | Before | After | Delta |
|-----------|--------|-------|-------|
| First-Hour Goal 1 (Alive) | 3 | 4 | +1 |
| First-Hour Goal 2 (Teaches) | 3 | 3 | 0 |
| Feel Avg Score | 6.5 | 7.0 | +0.5 |
| Optimize Criticals | 38 | 31 | -7 |
| Bridge Coverage % | 72% | 82% | +10% |
```

Write to `reports/audit/<iteration>/fix_delta.md`

If any dimension regressed, flag it immediately and investigate.

---

## Step 6 — Coverage Evolution

For gaps identified in Step 2 that were NOT filled in Step 4 (too large, needs
design input, or lower priority):

1. Write proposals to `reports/audit/<iteration>/eval_evolution.md`:

```markdown
# Evaluation Framework Evolution Proposals

## Proposed New Bot Phases
1. **Haven Management Phase** — exercise HavenUpgradeV0, HavenFabricatorV0,
   HavenMarketV0 in sequence. Assert upgrade completes, fabricator produces item.
   *Why*: Haven systems have 5 bridge methods, 0 bot coverage.

2. **Endgame Detection Phase** — advance to win condition trigger, verify
   victory screen displays. Assert GetWinConditionStateV0 returns non-null.
   *Why*: Win/loss screens are never screenshot-captured.

## Proposed New Rubric Dimensions
1. **Haven Experience** — Does the player's base feel like home? Progression
   visible? Upgrades meaningful?
   *Why*: 6 haven systems exist but no experiential evaluation.

## Proposed New Optimize Patterns
1. **Content consistency** — Grep for mismatched enum values between Content/
   and Systems/ files.
   *Why*: 15+ content files, zero cross-reference validation.

## Implementation Priority
1. [highest impact proposal]
2. ...
```

2. Present proposals to the user. Ask which to implement now vs defer.
3. Implement approved proposals.

---

## Step 7 — Final Report

Compile everything into `reports/audit/<iteration>/audit_report.md`:

```markdown
# Full Game Audit — <date> — <iteration>

## Snapshot
- Git SHA: <sha>
- Test count: <N>
- Build status: PASS/FAIL

## Summary
| Metric | Value |
|--------|-------|
| Problems found | N (X critical, Y major, Z minor) |
| Problems fixed | N |
| Problems deferred | N |
| Coverage gaps found | N |
| Coverage gaps filled | N |
| Eval framework improvements | N |

## Score Card
| Dimension | Before | After | Delta |
|-----------|--------|-------|-------|
| First-Hour Goals (avg) | X.X | X.X | +X.X |
| Feel Score (avg) | X.X | X.X | +X.X |
| Optimize Criticals | N | N | -N |
| Bridge Coverage % | N% | N% | +N% |
| UI Screen Coverage % | N% | N% | +N% |
| System Coverage % | N% | N% | +N% |

## Problems Fixed
(from fix_log.md)

## Problems Deferred
(remaining items from unified_problems.md, with reason for deferral)

## Coverage Improvements Made
(new bot phases, rubric dimensions, optimize patterns added)

## Eval Evolution Proposals (for next audit)
(from eval_evolution.md)

## Recommended Next Steps
1. ...
2. ...
3. ...
```

---

## Hard Invariants

- **Never skip build verification** after fixes — even "safe" changes can break.
- **Never auto-fix OPINION-tagged items** — these require human design judgment.
- **Never modify golden hash baselines** without running determinism tests.
- **Coverage gap filling is additive** — never remove existing bot phases or
  rubric dimensions, only add new ones.
- **One bot run, many evals** — do NOT run the bot separately for first-hour and
  feel. Run once, reuse screenshots and stdout across both evaluations.
- **Token budget on feel eval** — limit to 10 representative screenshots per
  evaluation pass. More screenshots = diminishing returns + context overflow.
- **Report all failures** — if an eval skill crashes or times out, log it in the
  final report. Do not silently skip.
- **Iteration history is immutable** — never overwrite a previous iteration's
  reports. Each audit run gets its own folder.
