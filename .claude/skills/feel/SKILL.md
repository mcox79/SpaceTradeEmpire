---
name: feel
description: "LLM-driven game feel evaluation loop. Captures screenshots, evaluates against feel rubric + AAA references, produces ranked prescriptions. Human-paced iteration."
argument-hint: "[iteration-name]"
---

# /feel — Game Feel Evaluation Loop

Runs the visual sweep bot, then dispatches a Sonnet subagent to evaluate the game's
**feel** across five dimensions (Composition, Readability, Scale & Space, Polish,
Atmosphere) using the extended visual evaluation guide. Produces ranked semantic
prescriptions that the main context can map to code changes.

Parse `$ARGUMENTS` for an optional iteration name (e.g., `baseline`, `post-hud-fix`).
Default to `iteration_1`, `iteration_2`, etc., based on existing iteration folders.

---

## Loop Philosophy

This is a **human-paced** iterative loop, not a fully automated pipeline:

```
1. /feel baseline         → Capture + evaluate → ranked prescriptions
2. User reviews prescriptions, picks what to fix
3. Claude (or user) applies fixes
4. /feel post-fix         → Capture + evaluate → delta comparison
5. Repeat until satisfied
```

The LLM evaluates and compares. The human decides what to act on.

---

## Step 1: Determine Iteration Context

1. Check for existing iteration folders in `reports/feel/`:
   - If none exist, this is the first iteration. Default name: `baseline`.
   - If previous iterations exist, note the most recent for delta comparison.
2. Create the output directory: `reports/feel/<iteration-name>/`

---

## Step 2: Run the Visual Sweep

```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-Screenshot.ps1 -Mode full
```

Wait for exit. Check exit code (0 = success).

If the runner fails, check stderr for `SCRIPT ERROR` or `Parse Error` and report.
Do NOT proceed to evaluation with zero screenshots.

---

## Step 3: Collect Artifacts

1. Glob for all PNGs: `reports/screenshot/full/*.png` and `reports/visual_eval/*.png`
2. Read `reports/screenshot/full/summary.json` or `reports/visual_eval/summary.json`
3. Copy all PNGs + summary.json into `reports/feel/<iteration-name>/`
4. Parse the bot stdout for `VSWP|` lines. Build a phase manifest:

```
Phase manifest:
  boot.png          — tick 0, initial system view
  dock_market.png   — tick N, market tab
  galaxy_map.png    — tick N, strategic view
  system_2.png      — tick N, second system after warp
  ...
```

---

## Step 4: Check for Reference Images

Glob for `reports/references/ref_*.png` (or `.jpg`).

- If reference images exist: include them in the subagent prompt for principle
  comparison (see Section 10 of visual_eval_guide.md).
- If none exist: tell the user "No reference images found. Evaluating against
  text-based AAA standards only. Add reference screenshots to `reports/references/`
  for stronger comparison in future iterations."

---

## Step 5: Check for Previous Iteration

If a previous iteration exists in `reports/feel/`:
1. Read its `evaluation.md` (the saved subagent output)
2. Include it in the subagent prompt for **delta comparison**
3. Tell the subagent: "This is iteration N. Compare against the previous evaluation
   and produce an ITERATION DELTA section."

---

## Step 6: Dispatch Sonnet Subagent

Use the Task tool with `subagent_type: "general-purpose"` and `model: "sonnet"`.

The agent prompt MUST include:

1. **The phase manifest** from Step 3
2. **Instruction to Read the evaluation guide**: `scripts/tools/visual_eval_guide.md`
   — the agent MUST read this file in full before evaluating
3. **Instruction to Glob and Read ALL PNGs** from the iteration output directory
4. **If references exist**: instruction to Read reference PNGs from `reports/references/`
5. **If previous iteration exists**: the previous evaluation.md content, with instruction
   to produce ITERATION DELTA
6. **The five evaluation perspectives:**

   > Evaluate each screenshot, then synthesize across the full set using these five
   > perspectives. You are not five people — you are one expert evaluator who considers
   > all five angles:
   >
   > **As a first-time player:** What's confusing? What's unclear? Where would you get
   > stuck or lost? What would make you close the game in the first 5 minutes?
   >
   > **As an art director:** Is there visual coherence? Does every frame have a clear
   > focal point and composition? Is the color palette working? Does the lighting
   > create mood?
   >
   > **As a UX designer:** Is information hierarchy correct? Can the player find what
   > they need in 3 seconds? Are interactive elements obviously interactive? Is
   > feedback sufficient?
   >
   > **As a game designer:** Does the progression arc show in the screenshots
   > (early=simple, late=complex)? Does the economy feel alive? Do systems communicate
   > their depth gradually?
   >
   > **As a space game fan:** Does this feel like Elite Dangerous, Stellaris, or
   > Everspace 2 — or does it feel like a prototype? Would you want to keep playing?
   > Would you show this to a friend?

7. **Output format**: Follow the RATING TEMPLATE in the evaluation guide exactly.
   End with FEEL SYNTHESIS, then PRESCRIPTIONS (Section 11 format), then OVERALL.

---

## Step 7: Save and Report

1. Save the subagent's full output to `reports/feel/<iteration-name>/evaluation.md`
2. Present to the user:
   - The FEEL SYNTHESIS scores (5 dimensions)
   - If iteration 2+: the ITERATION DELTA
   - The top 5 prescriptions (ranked by severity x confidence)
   - The OVERALL section (strengths, issues, priority fix)

3. End with:

   > **Next steps:** Review the prescriptions above. Tell me which ones to address,
   > or say "fix all high-confidence" to apply the safe changes. Then run `/feel <name>`
   > again to measure the delta.

---

## Convergence Guidance

There is no automatic convergence — the human decides when to stop. But offer
guidance based on the evaluation:

- **All 5 dimensions PASS**: "Feel evaluation complete. All dimensions pass.
  Consider running with a different seed to verify consistency."
- **Only SUGGESTION-level prescriptions remain**: "Core feel is solid. Remaining
  items are polish/opinion — diminishing returns on further iteration."
- **Regression detected**: "Warning: changes from last iteration caused regression
  in [dimension]. Consider reverting [specific change] before continuing."
- **No improvement after changes**: "Prescriptions from last iteration were addressed
  but scores didn't improve. The issues may require structural changes (new VFX,
  layout redesign) rather than parameter tweaks."

---

## Quick Reference

```bash
# First evaluation (baseline):
/feel baseline

# After making changes:
/feel post-hud-fix

# Check what iterations exist:
ls reports/feel/

# Add reference images for comparison:
# Copy AAA screenshots into reports/references/ with names like:
#   ref_flight_view_everspace2.png
#   ref_hud_elite_dangerous.png
#   ref_dock_menu_x4.png
#   ref_galaxy_map_stellaris.png
```

---

## Troubleshooting

- **0 screenshots captured**: Check stderr for GDScript parse errors. Rebuild C#: `dotnet build "Space Trade Empire.csproj"`
- **Gray/blank screenshots**: Game ran headless (no GPU framebuffer). Ensure `-Mode full` not `-Mode headless`
- **Evaluation seems shallow**: Check that the subagent actually read `visual_eval_guide.md`. The guide is 500+ lines — if the evaluation doesn't reference specific AAA games or use the tag system (BUG/UX/POLISH/GAP/OPINION), the agent skipped it
- **Prescriptions reference code files**: The subagent should NOT reference code. If it does, its output is malformed — it should produce semantic prescriptions only
- **Iteration delta says everything regressed**: Screenshot non-determinism (NPC positions, particle states). Run both iterations with the same seed if possible
