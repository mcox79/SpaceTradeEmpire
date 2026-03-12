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

### 3c. Summary Stats

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
3. **Instruction to Read the rubric**: `scripts/tools/first_hour_rubric.md` — MUST read in full
4. **Instruction to Glob and Read ALL PNGs** from `reports/first_hour/`
5. **If previous iteration exists**: the previous `evaluation.md` content
6. **Instructions:**

   > You are evaluating the first-hour experience of Space Trade Empire against
   > 5 experiential goals defined in the rubric. You have two data sources:
   >
   > 1. **Goal evidence lines** — structured data from the bot (FH1|GOAL|...)
   > 2. **Screenshots** — visual captures at 20 key moments
   >
   > Read the rubric first. Then read all screenshots. Then score each goal 1-5
   > using the evidence + screenshots together. Evidence trumps screenshot
   > interpretation for timing and count data. Screenshots provide visual context
   > for composition, atmosphere, and clarity.
   >
   > For each issue found, produce a prescription in Section 5 format.
   > Flag prescriptions as auto-fixable if they can be resolved by code change
   > without design input.
   >
   > Generate the manual playtest checklist from Section 6, pre-filled with your
   > automated scores and one-line summaries.
   >
   > Return your output in the exact format specified in the rubric's Output Format
   > section.

7. **If iteration 2+**: "Compare against the previous evaluation and produce an
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
- `Tag: BUG` or `Tag: UX` AND
- `Auto-fixable: yes`

Attempt the fix:
1. Identify the file and code change needed
2. Apply it (Edit tool for bot code, UI code, bridge code)
3. Log what was changed
4. Mark the prescription as "AUTO-FIXED — verify in next /first-hour run"

For prescriptions that are NOT auto-fixable (Tag: GAP, OPINION, or design
decisions), leave them for the user to review.

**Do NOT auto-fix:**
- Anything tagged OPINION or GAP (requires design input)
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
[x] #1 (BUG, high) FO silent during entire run — auto-fixed: added FO promotion in BOOT phase
[ ] #2 (GAP, medium) No cost-basis display in market — needs design input
[ ] #3 (UX, medium) Dock tabs all visible at first dock — needs milestone gating
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
