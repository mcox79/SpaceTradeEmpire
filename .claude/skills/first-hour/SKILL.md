---
name: first-hour
description: "First-hour experience evaluation loop. Captures goal evidence, LLM-scores against 5 experiential goals, auto-fixes issues, generates manual playtest checklist. Iterative."
argument-hint: "[iteration-name]"
---

# /first-hour — First-Hour Experience Evaluation Loop

Runs the first-hour proof bot, captures goal-specific evidence, dispatches an
LLM evaluation against 5 experiential goals, auto-fixes high-confidence issues,
and generates a manual playtest checklist. Iterative — each run compares against
the previous.

Parse `$ARGUMENTS` for an optional iteration name (e.g., `baseline`, `post-fo-fix`).
Default to `baseline` if first run, or `iteration_N` based on existing folders.

---

## Loop Philosophy

```
1. /first-hour baseline     → Bot + eval → goal scores + auto-fixes + checklist
2. User reviews prescriptions + plays 15 minutes
3. User reports what they saw → Claude resolves
4. /first-hour post-fix     → delta comparison + updated checklist
5. Repeat until all goals score 4+
```

The LLM evaluates and auto-fixes. The human playtests and decides.

---

## Step 1: Determine Iteration Context

1. Check `reports/first-hour-eval/` for existing iteration folders
2. If none: first iteration, default name `baseline`
3. If previous: note most recent for delta comparison
4. Create output dir: `reports/first-hour-eval/<iteration-name>/`

---

## Step 2: Run the First-Hour Bot

```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot.ps1 -Mode visual
```

Wait for exit. Check exit code (0 = PASS, 1 = FAIL or timeout).

If the runner fails:
- Check `reports/first_hour/stderr.txt` for `SCRIPT ERROR` or `Parse Error`
- Report the error and do NOT proceed to evaluation

### Step 2a: Multi-Seed Consistency Check (optional but recommended)

After the primary visual run, launch a headless multi-seed sweep for consistency:

```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-FHBot-MultiSeed.ps1 -Seeds 42,99,1001
```

Parse the `SEED_SWEEP|` summary line from output. If any seed FAILs that the primary
seed PASSed, flag as `SEED_INCONSISTENCY` — the issue may be seed-dependent.

Include per-seed assertion results in the evidence manifest (Step 3) under a
`## Multi-Seed Results` section. This helps the evaluator distinguish universal
bugs from seed-specific edge cases.

---

## Step 2b: Code Audit (Feature Visibility Report)

After the bot runs, build a **Feature Visibility Report** that cross-references
what code implements against what the player actually saw. This prevents
misleading prescriptions like "fix the FO system" when the system is complete
but the UI panel is suppressed.

### 2b.1: Inventory Bridge Methods

Grep all `scripts/bridge/SimBridge*.cs` for public method signatures:

```bash
grep -rn "public.*V0\|public.*Snapshot\|public.*Status" scripts/bridge/SimBridge*.cs | grep -v "//" | head -80
```

Build a list of all public query/command methods (expect ~150+).

### 2b.2: Check Bot Exercise Coverage

Search the bot stdout (`reports/first_hour/stdout.txt`) for `bridge.call` method
names. Cross-reference against the bridge inventory: which methods did the bot
actually call vs which were never exercised?

### 2b.3: Check UI Suppression

Grep for suppression patterns in the UI layer:

```bash
grep -rn "visible = false\|visible=false" scripts/ui/hud.gd scripts/ui/fo_panel.gd scripts/ui/knowledge_web*.gd scripts/ui/data_log*.gd
```

Flag any panel that exists but is force-hidden (e.g., FO panel, knowledge web).

### 2b.4: Check UI Consumers

Grep `scripts/ui/*.gd` and `scripts/core/*.gd` for `bridge.call("MethodName")`
patterns. Methods with zero GDScript consumers are `NO_UI_CONSUMER` — the data
exists in the sim but no UI element renders it.

### 2b.5: Build the Report

Classify each major bridge system into one of three categories:

| Category | Definition |
|----------|-----------|
| `ACTIVE` | Bridge method exists AND UI calls it AND bot exercised it (or player can reach it) |
| `BUILT_BUT_HIDDEN` | Bridge method exists, may have UI consumer, but panel is suppressed or timing prevents visibility |
| `NO_UI_CONSUMER` | Bridge method exists but no GDScript code calls it — data invisible to player |

Focus on goal-relevant systems:
- **Goal 1 (Alive)**: warfront overlay, faction territory, NPC visibility
- **Goal 2 (Teaches)**: onboarding disclosure, progressive tab unlock
- **Goal 3 (FO)**: FO state, candidates, dialogue, panel visibility
- **Goal 4 (Profit)**: transaction ledger, profit summary, cost basis
- **Goal 5 (Depth)**: tech tree, knowledge web, milestones, discoveries

Save the report to `reports/first-hour-eval/<name>/visibility_report.md`.

---

## Step 3: Parse Evidence

Read `reports/first_hour/stdout.txt`. Extract three categories:

### 3a. Assertion Results

All lines matching `FH1|ASSERT_PASS|` and `FH1|ASSERT_FAIL|`. Build summary:
```
Assertions: 21/21 PASS (or N/21 — list failures)
```

If ANY assertions FAIL, flag as CRITICAL blockers before goal evaluation.

### 3b. Goal Evidence

All lines matching `FH1|GOAL|`. Group by goal:

```
== Goal 1: Alive ==
  FH1|GOAL|ALIVE|npc_count=3 npc_have_velocity=2
  FH1|GOAL|ALIVE|price_profiles=4

== Goal 3: FO ==
  FH1|GOAL|FO|state=promoted=true name=Lira archetype=Pathfinder tier=1
  FH1|GOAL|FO|post_event=SELL dialogue=The station crew already knows our name...
  FH1|GOAL|FO|total_lines=2
...
```

### 3c. Performance Data

All lines matching `FH1|PERF|`. Extract fps_min, fps_max, fps_avg:
```
FH1|PERF|fps_min=58.2 fps_max=60.0 fps_avg=59.8 samples=40
```
Flag `FPS_BELOW_30` or `FPS_AVG_LOW` if present in `FH1|FLAG|` lines.

### 3d. Dispatch Failure Data

All lines matching `FH1|DISPATCH|` and `FH1|FLAG|DISPATCH_SILENT_FAIL`.
Silent failures indicate bridge commands that return without error but don't
change game state — typically caused by lock contention or validation bugs.

### 3e. Content Quality Data

All lines matching `FH1|FLAG|DEV_JARGON*` and `FH1|FLAG|LABEL_TOO_LONG`.
Dev jargon flags indicate internal IDs, version suffixes, or underscore_case
identifiers leaking into player-visible text.

### 3f. Save/Load Data

Lines matching `FH1|SAVE_LOAD|`. If `roundtrip=false`, the save/load cycle
corrupts player state — tag as critical.

### 3g. Summary Stats

The `FH1|SUMMARY|` line: visited, trades, combats, flags, audit_critical.

Save all parsed evidence to `reports/first-hour-eval/<name>/evidence.md`.

---

## Step 4: Collect Screenshots

Glob `reports/first_hour/*.png`. If zero PNGs found (headless run or no GPU),
note: "No screenshots — evidence-only evaluation."

---

## Step 5: Check Previous Iteration

If a previous iteration exists in `reports/first-hour-eval/`:
1. Read its `evaluation.md`
2. Include in subagent prompt for delta comparison

---

## Step 6: Dispatch Sonnet Subagent

Use the Task tool with `subagent_type: "general-purpose"` and `model: "sonnet"`.

The agent prompt MUST include:

1. **The goal evidence manifest** from Step 3 (ALL parsed `FH1|GOAL|` lines)
2. **The assertion summary** from Step 3
3. **The Feature Visibility Report** from Step 2b (full content)
4. **Instruction to Read the rubric**: `scripts/tools/first_hour_rubric.md` — MUST read in full
5. **Instruction to Glob and Read ALL PNGs** from `reports/first_hour/`
6. **If previous iteration exists**: the previous `evaluation.md` content
7. **Instructions:**

   > You are evaluating the first-hour experience of Space Trade Empire against
   > 5 experiential goals defined in the rubric. You have THREE data sources:
   >
   > 1. **Goal evidence lines** — structured data from the bot (FH1|GOAL|...)
   > 2. **Screenshots** — visual captures at 20 key moments
   > 3. **Feature Visibility Report** — code audit showing what's ACTIVE vs
   >    BUILT_BUT_HIDDEN vs NO_UI_CONSUMER
   >
   > Read the rubric first. Then read all screenshots. Then score each goal 1-5
   > using the evidence + screenshots together. Evidence trumps screenshot
   > interpretation for timing and count data. Screenshots provide visual context
   > for composition, atmosphere, and clarity.
   >
   > **Critical: Use the Feature Visibility Report to classify prescriptions.**
   > When scoring goals, check the report. If a goal scores low because a feature
   > is BUILT_BUT_HIDDEN, tag the prescription as `SUPPRESSED` — not `BUG` or
   > `GAP`. The fix is UI visibility, not new code. If a feature has
   > NO_UI_CONSUMER, tag as `UNWIRED` — bridge method exists but needs a
   > GDScript consumer.
   >
   > For each issue found, produce a prescription in Section 5 format.
   > Flag prescriptions as auto-fixable if they can be resolved by code change
   > without design input. SUPPRESSED issues are almost always auto-fixable
   > (remove a `visible = false` line or add an F-key toggle).
   >
   > After the goal scores table, include a **Code vs Experience Gap** section
   > that reports: how many systems are ACTIVE, how many are BUILT_BUT_HIDDEN,
   > and how many have NO_UI_CONSUMER. This tells the user how much latent
   > capability exists in the codebase that players cannot access.
   >
   > Generate the manual playtest checklist from Section 6, pre-filled with your
   > automated scores and one-line summaries.
   >
   > Return your output in the exact format specified in the rubric's Output Format
   > section.
   >
   > **NEW: EA Readiness Classification**
   > After the goal scores and prescriptions, include an EA READINESS section:
   >
   > | Status | Criteria |
   > |--------|----------|
   > | BLOCKED | Any assertion hard-fails OR dispatch silent failures prevent core loop |
   > | NOT_READY | Total score < 15/25 OR any goal scores 1/5 |
   > | CONDITIONAL | Total score 15-19/25, no goal below 2/5, all hard assertions pass |
   > | READY | Total score >= 20/25, no goal below 3/5, zero critical flags |
   >
   > Also evaluate these supplemental EA dimensions:
   > - **Performance**: FPS floor (min >= 30), avg >= 50. Use FH1|PERF| data.
   > - **Stability**: Save/load roundtrip passes, no SOFT_LOCK flags.
   > - **Content Quality**: Zero DEV_JARGON flags, zero LABEL_TOO_LONG flags.
   > - **Dispatch Reliability**: Zero DISPATCH_SILENT_FAIL flags.
   > - **Seed Consistency**: If multi-seed data provided, all seeds should PASS.

8. **If iteration 2+**: "Compare against the previous evaluation and produce an
   ITERATION DELTA section."

---

## Step 7: Save Output

1. Save full subagent output to `reports/first-hour-eval/<name>/evaluation.md`
2. Save evidence manifest to `reports/first-hour-eval/<name>/evidence.md`
3. Extract the manual checklist section and save to `reports/first-hour-eval/<name>/checklist.md`
4. Copy screenshots to the iteration folder (optional — they live in `reports/first_hour/` already)

---

## Step 8: Auto-Fix Pass

Review each prescription from the subagent. For prescriptions where:
- `Confidence: high` AND
- `Tag: BUG` or `Tag: UX` or `Tag: SUPPRESSED` AND
- `Auto-fixable: yes`

Attempt the fix:
1. Identify the file and code change needed
2. Apply it (Edit tool for bot code, UI code, bridge code)
3. Log what was changed
4. Mark the prescription as "AUTO-FIXED — verify in next /first-hour run"

**SUPPRESSED fixes** are typically the highest-leverage changes (large impact,
small code change). Common patterns:
- Remove `panel.visible = false` suppression lines in `hud.gd`
- Add an F-key toggle or auto-show trigger for a hidden panel
- Wire a bridge method to an existing UI element via `bridge.call()`

For prescriptions that are NOT auto-fixable (Tag: GAP, OPINION, UNWIRED, or
design decisions), leave them for the user to review.

**Do NOT auto-fix:**
- Anything tagged OPINION or GAP (requires design input)
- Anything tagged UNWIRED that needs a new UI panel (requires design input)
- Anything with Confidence: low
- Anything that would change SimCore tick logic (affects golden hashes)
- Anything the rubric flags as needing manual playtest verification

---

## Step 9: Report to User

Present to the user:

### Goal Scores

```
| Goal | Score | Summary |
|------|-------|---------|
| 1. Galaxy Alive | N/5 | one-line |
| 2. Actions Teach | N/5 | one-line |
| 3. FO Is Person | N/5 | one-line |
| 4. Profit = Discovery | N/5 | one-line |
| 5. Promise of Depth | N/5 | one-line |
```

### Iteration Delta (if applicable)

```
Improved: [what got better]
Regressed: [what got worse]
New issues: [what appeared]
```

### Prescriptions

Top prescriptions ranked by severity x confidence. Auto-fixed ones checked off:
```
[x] #1 (SUPPRESSED, high) FO panel force-hidden — auto-fixed: removed visible=false suppression
[ ] #2 (UNWIRED, medium) No cost-basis display in market — bridge exists, needs UI panel
[ ] #3 (BUG, high) First trade loses money — market guarantee not reaching bot path
```

### Code vs Experience Gap

```
| Category | Count | Examples |
|----------|-------|---------|
| ACTIVE | N | GetPlayerMarketViewV0, GetNeighborIdsV0, ... |
| BUILT_BUT_HIDDEN | N | GetFirstOfficerStateV0 (FO panel suppressed), ... |
| NO_UI_CONSUMER | N | GetCargoWithCostBasisV0, GetActiveWarConsequencesV0, ... |

Latent capability: X bridge methods exist but are invisible to the player.
Unsuppressing BUILT_BUT_HIDDEN items is the highest-leverage work available.
```

### Manual Playtest Checklist

The pre-filled checklist from the subagent.

### Next Steps

> **Your turn:** Play for 15 minutes. Score each goal in the checklist.
> Tell me what you saw — especially anything the automation missed.
> Then run `/first-hour post-fix` (or whatever name) to measure the delta.

---

## Convergence Guidance

- **All 5 goals score 4+**: "First-hour experience meets design targets. Consider
  testing with a different seed for consistency."
- **Only SUGGESTION-level prescriptions remain**: "Core experience is solid.
  Remaining items are polish — diminishing returns on further iteration."
- **Regression detected**: "Warning: [goal] regressed from N to M. Check whether
  [specific change] had side effects."
- **User playtest scores diverge from automated**: "Your experience differs from
  automation — this is expected for feel-based goals. Prioritize your scores."

---

## Quick Reference

```bash
# First evaluation:
/first-hour baseline

# After fixes:
/first-hour post-fix

# After playtest feedback:
/first-hour post-playtest

# Check iterations:
ls reports/first-hour-eval/
```
