---
name: audit
description: "Full-game audit: runs ALL bots, ALL evals, ALL optimizers. Compiles unified problem list, fixes issues, plugs monitoring gaps, iterates until clean."
argument-hint: "[mode: full|eval-only|fix-only|gaps] [iteration-name]"
---

# /audit — Full-Game Audit & Auto-Fix

Uses **every tool at our disposal** to find and fix game issues, then identifies
and fills gaps in our monitoring so future audits catch more.

Parse `$ARGUMENTS`:
- **mode** (first word): `full` (default) | `eval-only` | `fix-only` | `gaps`
  - `full` — evaluate → compile → fix → re-evaluate → evolve coverage → iterate
  - `eval-only` — run all evals and compile, no fixes
  - `fix-only` — skip evals, read latest reports, fix top problems
  - `gaps` — coverage gap analysis only (no bot runs, fast)
- **iteration-name** (second word, optional): e.g., `baseline`, `post-combat-fix`
  - Default: `audit_1`, `audit_2`, etc., based on existing folders

---

## Step 0 — Snapshot & Build

1. **Read audit journal**: If `reports/audit/JOURNAL.md` exists, read it.
   Check for carried-forward items from the last audit. Address any
   self-improvement actions that were proposed but not yet implemented.
2. Record git SHA: `git rev-parse --short HEAD`
3. Build BOTH projects:
   ```bash
   dotnet build SimCore/SimCore.csproj --nologo -v q
   dotnet build "Space Trade Empire.csproj" --nologo -v q
   ```
4. Run the full C# test suite as baseline signal:
   ```bash
   dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q
   ```
   Record: total tests, passed, failed. Any test failures are CRITICAL problems.
5. Create output dir: `reports/audit/<iteration-name>/`
6. If mode is `fix-only`, skip to Step 3. If mode is `gaps`, skip to Step 2.

---

## Step 1 — Run ALL Evaluations

Run everything. Parallelize aggressively — most tools are independent.

### Tier 1: Parallel Bot + Scan Runs (all independent)

Launch ALL of these in parallel:

**1a: First-Hour Bot (visual, single seed)**
```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot.ps1 -Mode visual
```
Output: `reports/first_hour/stdout.txt`, `reports/first_hour/*.png`
Captures: 18+ hard assertions, goal scores, screenshots, pacing metrics.

**Important**: Must use `-Mode visual` (not headless) to capture screenshots.
Headless mode has no framebuffer — `screenshot_capture.gd` skips all captures.
Only use `-Mode headless` in CI environments without a display server.

**1b: Optimize Pass 1 (Grep-based pattern scan)**
```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-OptimizeScan.ps1
```
Output: `reports/optimization/scan_*.md`
Captures: determinism violations, architecture violations, dead code, security,
allocation hygiene, scratch field health.

**1c: Deep Systems Bot (headless)**
```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot-MultiSeed.ps1 -Script deep_systems -Seeds 42
```
Output: `reports/first_hour/stdout.txt` (DS1| prefix lines)
Captures: 5000+ assertions across 15+ domains — combat depth, haven, endgame,
story, fleet, warfront, discovery, knowledge, missions, construction, etc.

**1d: Full Visual Sweep (screenshots)**
```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-Screenshot.ps1 -Mode full
```
Output: `reports/screenshots/*.png` (~24 screenshots, 17 visual phases)
Captures: game states the first-hour bot may not reach (galaxy map, warp transit,
haven panel, discovery sites, etc.).

**1e: Economy Stress Bot**
```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-Bot.ps1 -Mode stress -Cycles 1500
```
Output: `reports/bot/stress/stdout.txt`, `reports/bot/stress/report.json`
Captures: price collapse, economy stall, credit plateau, long-run stability.

**1e-eval: Domain Eval Bots (5 bots, sequential — Godot single-instance)**
```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-EvalBot.ps1
```
Output: `reports/eval/*.txt` (per-bot stdout)
Captures: 5 domain-specific evaluation bots — economy_health (EVE-style monitoring),
narrative_pacing (Hades dialogue queue), dread_pacing (Dead Space intensity),
audio_atmosphere (spatial audio check), flight_feel (handling evaluation).
Each bot produces PASS/WARN/FAIL assertions. Any SCRIPT_ERROR = bot contract issue.

If any bot fails, log the error and continue with other results. Do NOT abort
the entire audit for a single eval failure.

### Tier 2: Multi-Seed Stability (depends on 1a completing — reuses build)

After the first-hour single-seed completes, run multi-seed sweep:

**1f: First-Hour Multi-Seed Sweep (headless, 5 seeds)**
```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot-MultiSeed.ps1 -Seeds 42,99,1001,31337,77777
```
Output: SEED_SWEEP report + SEED_VARIANCE metrics + Goal Score Aggregation
Captures: cross-seed stability, credit growth variance, hull variance, visited variance.
Any seed that FAILs → CRITICAL problem (game is not robust across galaxy shapes).

**1g: Tutorial Bot Multi-Seed (headless, 3 seeds)**
```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot-MultiSeed.ps1 -Script tutorial -Seeds 42,100,1001
```
Output: TUT| prefix lines, 155+ assertions per seed
Captures: FO rotation coverage (seed % 3), all 45 tutorial phases, onboarding integrity.
Seeds 42/100/1001 cover all 3 FO types (Analyst/Veteran/Pathfinder).

**1h-chaos: Chaos Tutorial Bot (headless, 5 seeds)**
```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot-MultiSeed.ps1 -Script chaos_tutorial -Seeds 42,99,1001,31337,77777
```
Output: CHAOS| prefix lines, 8 adversarial scenarios per seed
Captures: out-of-order actions, spam inputs, sequence breaks, state corruption,
soft locks, phantom transactions, skip+restart contamination, dialogue-during-travel.
Any CHAOS|ASSERT_FAIL → CRITICAL problem (tutorial state machine is fragile).

### Tier 3: LLM Evaluations (depend on bot outputs from Tier 1)

**1h: First-Hour LLM Evaluation (depends on 1a)**

1. Read `reports/first_hour/stdout.txt`
2. Parse all `FH1|ASSERT_*`, `FH1|GOAL|*`, `FH1|FLAG|*`, `FH1|PERF|*` lines
3. Read `scripts/tools/first_hour_rubric.md` for scoring criteria
4. Score each of the 5 goals (1-5) with evidence
5. Generate prescriptions: BUG/UX/POLISH/GAP/SUPPRESSED/UNWIRED
6. Write to `reports/audit/<iteration>/first_hour_eval.md`

**1i: Deep Systems LLM Evaluation (depends on 1c)**

1. Read deep systems bot stdout
2. Parse all `DS1|ASSERT_*`, `DS1|GOAL|*`, `DS1|FLAG|*` lines
3. Categorize by domain: combat, haven, endgame, story, fleet, warfront, etc.
4. Flag any domain with >5% warn rate as needing investigation
5. Flag any ASSERT_FAIL (hard failure) as CRITICAL
6. Write to `reports/audit/<iteration>/deep_systems_eval.md`

**1j: Feel LLM Evaluation (depends on 1a + 1d)**

Using screenshots from BOTH the first-hour bot and the full visual sweep:

1. Read all PNG files from `reports/first_hour/` AND `reports/screenshots/`
2. Read `scripts/tools/visual_eval_guide.md` for visual vocabulary and reference standards
3. Evaluate each screenshot across 5 dimensions: Composition, Readability, Scale & Space, Polish, Atmosphere
4. Apply 5 perspectives: first-time player, art director, UX designer, game designer, space game fan
5. Rate each screenshot PASS/NEEDS_WORK/FAIL per dimension
6. Write to `reports/audit/<iteration>/feel_eval.md`

**Token budget**: Limit to 10 most representative screenshots (not all). Pick one
per distinct game state: boot, docked, flight, combat, galaxy map, warp, haven,
discovery, FO dialogue, trade menu.

**1k: Optimize Deep Passes (depends on 1b)**

Run ALL optimize passes (not just 2-3):

**Pass 2: Hot-Path Allocation Scan** — Haiku agents scan System files for LINQ,
new collections, boxing in `Process()` methods.

**Pass 3: Architecture & Lock Discipline** — Scan SimBridge partials for lock
pattern violations, bridge contract mismatches, layer violations. Also scan for
**duplicate method names across SimBridge partials** — GDScript `call()` crashes
on overloaded C# methods (e.g., `SetDoctrineV0` in both Automation and Combat).

**Pass 4: Code Consistency** — Cross-file consistency across Systems, bridge
partials, and content files.

**Pass 5: Dead Code & Duplication** — Unused public methods, semantic duplication,
orphaned files.

**Pass 6: GDScript Quality** — Bridge call correctness, anti-patterns, scene health.

**Pass 7: Security & Dependencies** — NuGet vulnerabilities, save file safety,
export flags.

Write ALL findings to `reports/audit/<iteration>/optimize_eval.md`

**1l: Seed Variance Analysis (depends on 1f)**

1. Read multi-seed sweep output
2. Compute variance metrics: credit_growth stdev, min_hull stdev, visited stdev
3. Flag high-variance dimensions (credit stdev > 2.0, hull stdev > 30)
4. Compare goal scores across seeds — flag any goal with range > 2 (e.g., min=2, max=5)
5. Write to `reports/audit/<iteration>/seed_variance_eval.md`

**1m: Tutorial Integrity Evaluation (depends on 1g)**

1. Read tutorial bot outputs for all 3 seeds
2. Verify all 45 phases hit across all seeds
3. Flag phase coverage gaps (missed phases due to headless frame-racing)
4. Verify FO rotation: all 3 types exercised
5. Check warn count — expected 0-4 per seed
6. Write to `reports/audit/<iteration>/tutorial_eval.md`

**1n: Economy Health Evaluation (depends on 1e)**

1. Read stress bot output
2. Parse economy metrics: net profit, trade count, price stability
3. Flag: PRICE_COLLAPSE, ECONOMY_STALL, CREDIT_PLATEAU, NET_LOSS
4. Write to `reports/audit/<iteration>/economy_eval.md`

**1o: Domain Eval Bot Analysis (depends on 1e-eval)**

1. Read all 5 eval bot outputs from `reports/eval/`
2. Parse PASS/WARN/FAIL assertions per bot
3. Flag any bot with SCRIPT_ERROR (contract mismatch = CRITICAL)
4. Flag any domain with >10% WARN rate as needing investigation
5. Summarize per-domain health: economy, narrative, dread, audio, flight
6. Write to `reports/audit/<iteration>/domain_eval.md`

---

## Step 2 — Coverage Gap Analysis

Detect what our evaluation framework does NOT cover.

### 2a — Bridge Method Coverage

**Use the pre-computation script** instead of manual greps:
```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-CoverageGap.ps1
```
Output: EXERCISED/UI_ONLY/UNCALLED per method + UI script coverage summary.
This replaces ~100 manual grep operations with a single script invocation.

### 2b — UI Screen Coverage

```
1. List all UI scripts:
   Glob pattern="scripts/ui/*.gd"

2. For each UI script, check if any bot references it or its key functions:
   Grep pattern="<script_name_stem>" path="scripts/tests/" type="gd"

3. Check screenshot coverage — which game states have screenshots:
   Glob pattern="reports/first_hour/*.png"
   Glob pattern="reports/screenshots/*.png"

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

### 2d — Bot Script Coverage

```
1. List all bot scripts:
   Glob pattern="scripts/tests/test_*_v0.gd"

2. Check which bots are exercised by the audit:
   - First-hour: test_first_hour_proof_v0.gd ✓
   - Deep systems: test_deep_systems_v0.gd ✓
   - Tutorial: test_tutorial_proof_v0.gd ✓
   - Exploration: test_exploration_bot_v0.gd (via /bot stress) ✓

3. Flag bots that exist but are NOT run by audit — these may test
   functionality that no audit tool exercises
```

### 2e — Eval Dimension Gaps

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

4. Cross-seed coverage:
   - Check if multi-seed sweep covers all 5 standard seeds
   - Check if tutorial covers all 3 FO types
   - Flag any seed/FO type never tested
```

### Output

Write `reports/audit/<iteration>/coverage_gaps.md` with the standard format
(see Step 2 output template in previous version).

---

## Step 3 — Compile Unified Problem List

Parse ALL evaluation outputs (Step 1 + Step 2) into a single ranked list.

### Input Sources

| Source | Report File | Problem Format |
|--------|------------|----------------|
| C# Tests | Build + test output | Test failures = CRITICAL |
| First-hour | `first_hour_eval.md` | Prescriptions with goal/severity/tag |
| Deep Systems | `deep_systems_eval.md` | ASSERT_FAIL = CRITICAL, high warn = major |
| Tutorial | `tutorial_eval.md` | Phase gaps, FO coverage holes |
| Economy | `economy_eval.md` | Collapse/stall/plateau flags |
| Feel | `feel_eval.md` | Per-screenshot NEEDS_WORK/FAIL ratings |
| Optimize | `optimize_eval.md` | CRITICAL/WARNING/SUGGESTION findings |
| Seed Variance | `seed_variance_eval.md` | High-variance dimensions |
| Domain Evals | `domain_eval.md` | Per-bot PASS/WARN/FAIL + SCRIPT_ERROR |
| Coverage | `coverage_gaps.md` | Gap classifications |

### Unified Format

For each problem:

```
PROBLEM #N
  Source:       [test-suite | first-hour | deep-systems | tutorial | economy |
                 feel | optimize | seed-variance | coverage-gap]
  Severity:     [critical | major | minor | suggestion]
  Confidence:   [high | medium | low]
  Tag:          [BUG | UX | POLISH | GAP | PERF | ARCH | COVERAGE | STABILITY]
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
| C# test failure | Any | critical |
| Bot ASSERT_FAIL (hard) | Any | critical |
| Seed FAIL (any seed) | Any | critical |
| Optimize | CRITICAL | critical |
| First-hour | critical + BUG tag | critical |
| Economy | PRICE_COLLAPSE / ECONOMY_STALL | critical |
| First-hour | major + UX tag | major |
| Feel | FAIL rating | major |
| Coverage | UNCOVERED system | major |
| Seed variance | stdev > threshold | major |
| Tutorial | phase gap > 3 | major |
| Feel | NEEDS_WORK | minor |
| Coverage | UNIT_ONLY system | minor |
| Tutorial | phase gap 1-3 | minor |
| Optimize | SUGGESTION | suggestion |
| Any | OPINION tag | suggestion (always lowest) |

### Deduplication

- Same file + same issue from multiple evals → merge, keep highest severity
- Coverage gaps that overlap with existing prescriptions → merge into the prescription
- Feel + first-hour flagging the same screenshot → merge
- Deep systems + first-hour flagging the same bridge method → merge

### Output

Write `reports/audit/<iteration>/unified_problems.md` with summary + problems
grouped by severity.

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

**New bot phases for uncovered bots:**
- If a bot script (test_*_v0.gd) exists but isn't run by audit, add it
  to the audit pipeline or merge its coverage into an existing bot

After coverage fixes, verify:
```bash
dotnet build "Space Trade Empire.csproj" --nologo
dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q
```
Then re-run affected bots to confirm new phases don't crash.

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

- If first-hour problems were fixed → re-run first-hour bot + eval
- If deep systems problems were fixed → re-run deep systems bot
- If feel problems were fixed → re-run screenshot + feel eval
- If optimize problems were fixed → re-run optimize Pass 1
- If economy problems were fixed → re-run stress bot
- If tutorial problems were fixed → re-run tutorial bot (3 seeds)
- If seed variance problems were fixed → re-run multi-seed sweep

Compare before vs after:

```markdown
# Fix Delta — <iteration>

| Dimension | Before | After | Delta |
|-----------|--------|-------|-------|
| C# Tests | N pass / N fail | N pass / N fail | +N/-N |
| First-Hour Goals (avg) | X.X | X.X | +X.X |
| Deep Systems Assertions | N pass / N warn | N pass / N warn | +N/-N |
| Tutorial Phase Coverage | X% | X% | +X% |
| Economy Stability | PASS/FAIL | PASS/FAIL | — |
| Feel Avg Score | X.X | X.X | +X.X |
| Optimize Criticals | N | N | -N |
| Seed Pass Rate | N/5 | N/5 | +N |
| Bridge Coverage % | N% | N% | +N% |
| System Coverage % | N% | N% | +N% |
```

Write to `reports/audit/<iteration>/fix_delta.md`

If any dimension regressed, flag it immediately and investigate.

---

## Step 6 — Coverage Evolution

For gaps identified in Step 2 that were NOT filled in Step 4:

1. Write proposals to `reports/audit/<iteration>/eval_evolution.md`
2. Present proposals to the user. Ask which to implement now vs defer.
3. Implement approved proposals.
4. After implementing, re-run Step 2 (gap analysis) to confirm gaps are closed.

Proposals should cover:
- New bot phases for uncovered bridge methods
- New eval dimensions for scoring rubrics
- New optimize patterns for code quality checks
- New bot scripts if existing ones don't cover a domain
- New screenshot scenarios for uncaptured game states

---

## Step 7 — Iterate Until Clean

This is the key difference from a one-shot audit. After Steps 4-6:

1. **Check convergence**: Did the fix pass reduce total problems? Did coverage improve?
2. **If problems remain and progress was made**: Loop back to Step 1 (re-evaluate everything)
3. **If problems remain but no progress**: Present blockers to user, stop iterating
4. **If no problems remain**: Proceed to final report

**Smart re-eval**: If only UX/POLISH fixes were applied (no SimCore logic changes),
skip deep systems and economy stress re-runs — only re-run first-hour bot, feel
eval, and screenshot captures. This avoids expensive re-runs for cosmetic fixes.

**Godot single-instance constraint**: Only one Godot headless process can run at
a time (shared resources). Bot runs in Tier 1 and Tier 2 must be SEQUENTIAL.
LLM evaluations (Tier 3) can run in parallel since they only read report files.

**End-state testing gap**: Current bots do not reach loss/victory/death screens.
Future audit should add end-state capture scenarios to the visual sweep bot.

Iteration cap: 3 loops maximum per audit invocation. Each loop gets a sub-folder:
`reports/audit/<iteration>/loop_1/`, `loop_2/`, etc.

---

## Step 8 — Final Report

Compile everything into `reports/audit/<iteration>/audit_report.md`:

```markdown
# Full Game Audit — <date> — <iteration>

## Snapshot
- Git SHA: <sha>
- Build status: PASS/FAIL
- C# Test count: <N> passed / <N> failed
- Bot pass rates: FH=N/N, DS=N/N, TUT=N/N, STRESS=PASS/FAIL

## Summary
| Metric | Value |
|--------|-------|
| Problems found | N (X critical, Y major, Z minor) |
| Problems fixed | N |
| Problems deferred | N |
| Coverage gaps found | N |
| Coverage gaps filled | N |
| Iterations completed | N |
| Eval framework improvements | N |

## Score Card
| Dimension | Start | End | Delta |
|-----------|-------|-----|-------|
| C# Tests | N/N | N/N | +N |
| First-Hour Goals (avg) | X.X | X.X | +X.X |
| Deep Systems (pass/warn) | N/N | N/N | +N/-N |
| Tutorial Coverage | X% | X% | +X% |
| Economy Stress | PASS/FAIL | PASS/FAIL | — |
| Feel Score (avg) | X.X | X.X | +X.X |
| Optimize Criticals | N | N | -N |
| Seed Stability | N/5 | N/5 | +N |
| Bridge Coverage % | N% | N% | +N% |
| UI Screen Coverage % | N% | N% | +N% |
| System Coverage % | N% | N% | +N% |

## Tools Used
| Tool | Status | Key Finding |
|------|--------|-------------|
| First-hour bot (visual) | PASS/FAIL | ... |
| Deep systems bot | PASS/FAIL | ... |
| Tutorial bot (3 seeds) | PASS/FAIL | ... |
| Economy stress bot | PASS/FAIL | ... |
| Multi-seed sweep (5 seeds) | N/5 PASS | ... |
| Full visual sweep | N screenshots | ... |
| Optimize (7 passes) | N findings | ... |
| C# test suite | N/N | ... |
| Coverage gap analysis | N gaps | ... |

## Problems Fixed
(from fix_log.md)

## Problems Deferred
(remaining items, with reason for deferral)

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

## Step 9 — Self-Improvement Reflection

After the final report, reflect on this audit run and identify improvements to
three domains: the audit process itself, the tools it uses, and the game.

### 9a — Audit Process Improvements

Ask: What did this audit miss, do poorly, or waste time on?

- Did any bot crash or timeout? → Should we increase timeout, add retry logic,
  or fix the bot script?
- Did any LLM eval produce low-signal results? → Should we tune the prompt,
  change the rubric, or remove a pass?
- Did deduplication miss overlapping findings? → Should we add a new merge rule?
- Did the severity mapping miscategorize anything? → Should we adjust thresholds?
- Did coverage gap analysis miss a domain? → Should we add a new gap category?
- Were there false positives that wasted fix time? → Should we tune patterns?
- Did the iteration loop converge or spin uselessly? → Adjust loop logic?

### 9b — Tool Improvements

Ask: What should each tool do better?

- **Bots**: Should any bot exercise new bridge methods? New game states? New edge cases?
- **Optimize**: Should any new Grep pattern be added to Pass 1? New LLM scan target?
- **Screenshot**: Should any new scenario be captured? Are existing scenarios stale?
- **Rubric**: Should any scoring dimension be added, removed, or reweighted?
- **Multi-seed**: Should the seed set change? Are 5 seeds enough?
- **Runner scripts**: Should timeouts, build steps, or output parsing change?

### 9c — Game Improvements (beyond current problems)

Ask: What systemic issues does the game have that no single problem captures?

- Are there design patterns that keep producing bugs? (e.g., "every system that
  touches DataLogs has the same wiring issue")
- Are there player experience gaps that no eval measures? (e.g., "we never test
  what happens when the player ignores the tutorial")
- Are there architectural debts that will compound? (e.g., "SimBridge has 20
  partials and growing — should we split further or consolidate?")

### Output

Write `reports/audit/<iteration>/self_improvement.md`:

```markdown
# Self-Improvement Reflection — <iteration>

## Audit Process
| Issue | Impact | Proposed Fix | Priority |
|-------|--------|-------------|----------|
| Bot X timed out on seed Y | Missed N assertions | Increase frame cap / fix phase | high |
| ... | ... | ... | ... |

## Tool Improvements
| Tool | Issue | Proposed Change | Priority |
|------|-------|----------------|----------|
| deep_systems bot | No diplomacy coverage | Add DIPLOMACY_CHECK phase | high |
| ... | ... | ... | ... |

## Game Systemic Issues
| Pattern | Examples | Root Cause | Proposed Fix |
|---------|----------|-----------|-------------|
| DataLog/Discovery mismatch | KnowledgeGraph 0/72 | Two entity types, one lookup | Unify or dual-lookup |
| ... | ... | ... | ... |

## Action Items for Next Audit
1. [highest priority improvement] — what to do and why
2. ...
3. ...
```

### Persistent Audit Journal

Append a summary entry to `reports/audit/JOURNAL.md` (persistent across all
audit runs — this file grows over time):

```markdown
## <iteration> — <date> — SHA <sha>

**Score**: N problems found, N fixed, N deferred
**Coverage**: Bridge N% → N%, Systems N% → N%, UI N% → N%
**Key findings**: [1-3 sentence summary of most impactful discoveries]
**Key fixes**: [1-3 sentence summary of what was fixed]
**Self-improvement**: [1-3 sentence summary of process/tool improvements identified]
**Carried forward**: [problems or improvements deferred to next audit]
```

This journal lets future audit runs learn from past runs:
- Read JOURNAL.md at the start of each audit to check for carried-forward items
- Compare current coverage metrics against the trajectory
- Avoid re-discovering the same systemic issues
- Track whether self-improvement proposals were actually implemented

---

## Hard Invariants

- **Never skip build verification** after fixes — even "safe" changes can break.
- **Never auto-fix OPINION-tagged items** — these require human design judgment.
- **Never modify golden hash baselines** without running determinism tests.
- **Coverage gap filling is additive** — never remove existing bot phases or
  rubric dimensions, only add new ones.
- **One bot run, many evals** — do NOT run the same bot twice in Tier 1.
  Run once, reuse stdout and screenshots across all LLM evaluations.
- **Token budget on feel eval** — limit to 10 representative screenshots per
  evaluation pass. More screenshots = diminishing returns + context overflow.
- **Report all failures** — if an eval skill crashes or times out, log it in the
  final report. Do not silently skip.
- **Iteration history is immutable** — never overwrite a previous iteration's
  reports. Each audit run gets its own folder.
- **Iterate with purpose** — only loop if the previous iteration made measurable
  progress. 3 loops max to prevent infinite cycling.
- **Respect bot timeouts** — bots have built-in frame caps (5400 frames = 90s).
  If a bot times out, that is itself a finding (TIMEOUT flag), not a retry trigger.
- **Phase-advance-first pattern** — in bot phase functions, set `_phase = NextPhase`
  BEFORE any bridge calls. If a call crashes (e.g., overloaded method), GDScript
  error recovery skips the rest of the function. Without phase advance at top,
  the function re-enters every frame until timeout.
- **Bot init retry pattern** — `GetGalaxySnapshotV0` and other init-time bridge
  calls can return empty due to `TryExecuteSafeRead(0)` contention. Standard fix:
  retry with 200-300ms delay if result is empty.
