# First-Hour Experience Evaluation Rubric

> This guide tells an LLM evaluator how to score the first-hour experience
> against five experiential goals. It is the companion to `visual_eval_guide.md`
> (which evaluates aesthetics). This guide evaluates the **player journey**.
>
> Read this file IN FULL before evaluating any screenshots or evidence.

---

## Section 0 — The Five Goals

The first hour of Space Trade Empire must accomplish exactly five things.
Everything the player sees, hears, and does serves one of these goals.

### Goal 1: The Galaxy Is Already Alive

The player enters a world that was here before them. NPC ships fly trade routes.
Stations have prices shaped by geography and war. Faction territory is visible
through color and density. The player is a newcomer in a functioning economy.

**The test:** A player who does nothing for 60 seconds should still see movement
and evidence of a world operating on its own logic.

### Goal 2: Every Action Teaches Something

No tutorials. No popups. The player learns by doing. Each action has a visible
consequence that teaches the next action. One system per encounter — never two
new systems at the same dock.

**The test:** A player should articulate "how to make money" within 10 minutes
without ever reading an instruction.

### Goal 3: The First Officer Is a Person, Not a Tooltip

The FO has a personality, reacts to events after they happen (not before), and
makes at least one observation the player didn't expect. They are an emotional
anchor, not a tutorial system wearing a character skin.

**The test:** If you replaced every FO line with a generic tooltip, the player
should feel a loss.

### Goal 4: Profit Feels Like Discovery

The first trade must feel like the player's own insight, not an assigned task.
The margin is so large it's impossible to miss, but the FO reacts after the
fact — making it a shared experience, not a predicted one.

**The test:** The player's second trade should be self-directed, not suggested.

### Goal 5: The Promise of Depth

By minute 30, the player understands enough to know how much more there is.
They've seen a fraction of the galaxy. They've found some technologies but not
all. They have empty module slots. There are faction borders they haven't crossed.

**The test:** At minute 30, the player should name three things they want to do
next — and those three things should differ between players.

---

## Section 1 — Evidence Reading

The FH bot emits structured log lines. Two types matter for this evaluation:

### Assertion Lines (`FH1|ASSERT_PASS|name|detail` / `FH1|ASSERT_FAIL|...`)

These are mechanical pass/fail checks. They verify the *plumbing* works —
credits changed, node changed, modules exist. A failing assertion means the
experience is structurally broken. All 21 should PASS before evaluating goals.

If any assertions FAIL, note them as CRITICAL issues before scoring goals.

### Goal Probe Lines (`FH1|GOAL|<GOAL>|key=value ...`)

These capture goal-specific evidence. Parse them into a per-goal evidence block:

```
Goal 1 (Alive):
  FH1|GOAL|ALIVE|npc_count=3 npc_have_velocity=2
  FH1|GOAL|ALIVE|price_profiles=4
  → NPCs present and moving. 4 distinct price profiles across stations.

Goal 3 (FO):
  FH1|GOAL|FO|state=promoted=true name=Lira archetype=Pathfinder tier=1
  FH1|GOAL|FO|post_event=SELL dialogue=The station crew already knows...
  FH1|GOAL|FO|post_event=COMBAT dialogue=none
  FH1|GOAL|FO|total_lines=2
  → FO promoted. Reacted to sale but not combat. 2 lines total.
```

Use this evidence to ground your scores. Never score based on what you *assume*
should be happening — score based on what the evidence and screenshots show.

---

## Section 2 — Per-Goal Scoring

Score each goal 1-5 using the evidence + screenshots.

| Score | Meaning |
|-------|---------|
| 5 | A player would specifically praise this aspect |
| 4 | Works as designed, no friction |
| 3 | Intent visible but gaps reduce impact |
| 2 | Mechanically present but experientially flat |
| 1 | Actively harms the experience |

### Goal 1: The Galaxy Is Already Alive

| Score | Evidence Pattern |
|-------|-----------------|
| 5 | NPCs moving in boot screenshot, visually distinct stations across systems, price diversity >= 4, faction labels visible without opening menus |
| 4 | NPCs present and moving, prices differ across stations, some visual variety |
| 3 | NPCs present but static or sparse, prices differ but pattern isn't obvious, stations look similar |
| 2 | NPCs exist but feel like props, prices are numbers with no geographic story |
| 1 | Empty space at boot, identical stations, no sense of a functioning world |

**Key screenshots:** `01_boot` (NPC presence), `04_dock_market` vs `10_dock_2` (price difference), `24_system_5` (variety across systems)

### Goal 2: Every Action Teaches Something

| Score | Evidence Pattern |
|-------|-----------------|
| 5 | Screenshots tell a clear buy→warp→sell story with no tutorial text. Each dock shows one new system. Progressive disclosure visible (fewer tabs early, more later) |
| 4 | No tutorial text found. Systems introduced one at a time. Dock menus readable |
| 3 | Mostly clear but one or two moments where the next step is unclear. Some UI elements unexplained |
| 2 | Tutorial text present, or too many systems dumped at once, or actions have unclear consequences |
| 1 | Confusing UI, tutorial popups, or "what do I do?" visible in multiple screenshots |

**Key screenshots:** `04_dock_market` (first dock — is it clear?), `06_post_buy` (did buying change something visible?), `11_post_sell` (is the profit obvious?)

### Goal 3: The First Officer Is a Person, Not a Tooltip

| Score | Evidence Pattern |
|-------|-----------------|
| 5 | FO promoted, 3+ dialogue lines with distinct personality, reacts after events, at least one line that's atmosphere not instruction |
| 4 | FO promoted, 2+ lines, reacts after events, lines feel character-specific |
| 3 | FO promoted, 1-2 lines, but timing or content feels generic |
| 2 | FO promoted but silent during the run, or lines read as tooltips |
| 1 | FO not promoted, or no dialogue observed, or dialogue is dev text |

**Key evidence:** `FH1|GOAL|FO|` lines. Check: Is the FO's dialogue *after* the player acted (not before)? Does the archetype (Analyst/Veteran/Pathfinder) come through?

**Key screenshots:** `05_fo_panel` (FO visible and named?), any screenshot showing toast overlay with FO dialogue

### Goal 4: Profit Feels Like Discovery

| Score | Evidence Pattern |
|-------|-----------------|
| 5 | Margin > 100 cr/unit, credit delta > 50% of starting capital, FO reacts to profit, credit counter visibly different in pre/post sell screenshots |
| 4 | Clear profit (> 200 cr total), margin was obvious (> 50 cr/unit), FO may or may not react |
| 3 | Profit exists but modest (< 200 cr or < 20% of starting capital), margin wasn't dramatic |
| 2 | Profit exists but tiny, or player lost money then recovered, or margin invisible |
| 1 | Player lost money on first trade, or profit is indistinguishable from starting credits |

**Key evidence:** `FH1|GOAL|PROFIT|` lines. `margin` should be >> 10 cr. `delta` should be > 500 cr.

**Key screenshots:** `06_post_buy` vs `11_post_sell` — compare credit counter values

### Goal 5: The Promise of Depth

| Score | Evidence Pattern |
|-------|-----------------|
| 5 | Galaxy shows < 50% explored, tech_count >= 8, empty_slots >= 2, galaxy map screenshot shows vast unvisited space, multiple systems with different visual character |
| 4 | < 50% explored, tech and module depth visible, galaxy map shows more to discover |
| 3 | Galaxy explored but depth systems (tech, modules) present. Some sense of "more" |
| 2 | Most of galaxy visited, or tech/modules feel complete, not enough remaining mystery |
| 1 | Everything visible, nothing left to discover, no reason to keep playing |

**Key evidence:** `FH1|GOAL|DEPTH|` lines.

**Key screenshots:** `27_deep_explore` (how much of galaxy is seen?), `22_ship_fitted` (empty slots?), `31_final` (sense of scale?)

---

## Section 3 — Screenshot-to-Goal Map

| Screenshot | Goal 1 | Goal 2 | Goal 3 | Goal 4 | Goal 5 |
|------------|--------|--------|--------|--------|--------|
| `01_boot` | PRIMARY | minor | - | - | - |
| `03_hud` | minor | PRIMARY | - | - | - |
| `04_dock_market` | minor | PRIMARY | - | minor | - |
| `05_fo_panel` | - | - | PRIMARY | - | - |
| `06_post_buy` | - | PRIMARY | - | PRIMARY | - |
| `07_flight_cargo` | minor | minor | - | - | - |
| `09_arrival_1` | PRIMARY | minor | - | - | - |
| `10_dock_2` | PRIMARY | minor | - | minor | - |
| `11_post_sell` | - | minor | - | PRIMARY | - |
| `14_mission_accepted` | - | PRIMARY | - | - | - |
| `16_system_3` | PRIMARY | - | - | - | minor |
| `18_combat` | minor | - | - | - | - |
| `20_dock_upgrade` | - | PRIMARY | - | - | minor |
| `22_ship_fitted` | - | PRIMARY | - | - | PRIMARY |
| `24_system_5` | PRIMARY | - | - | - | PRIMARY |
| `27_deep_explore` | - | - | - | - | PRIMARY |
| `30_final_trade` | minor | - | - | - | - |
| `31_final` | minor | - | - | - | PRIMARY |

- **PRIMARY** = this screenshot is a key data point for scoring the goal
- **minor** = supports the goal evaluation but isn't definitive
- **-** = not relevant

---

## Section 4 — Anti-Hallucination Rules

1. **Never claim absence without certainty.** If you can't see the FO panel in a
   screenshot, say "FO panel not visible in this frame" not "FO panel is missing."
   It may be off-screen, obscured, or not yet promoted.

2. **Tag every issue.** Use one of: `BUG` (broken), `UX` (usable but confusing),
   `POLISH` (works but feels unfinished), `GAP` (designed feature not yet built),
   `OPINION` (subjective preference).

3. **Check evidence before scoring.** If `FH1|GOAL|FO|total_lines=3` says the
   FO spoke 3 times, don't score Goal 3 as 1. The evidence trumps screenshot
   interpretation.

4. **Screenshots can't show timing.** You cannot determine whether the FO spoke
   *before* or *after* an event from a static image. Use the `post_event` evidence
   lines for timing information.

5. **Headless screenshots are blank.** If screenshots are all gray/black, the bot
   ran in headless mode. Score based on evidence lines only and note: "No visual
   data — headless run. Scores are evidence-only."

6. **Development stage awareness.** Placeholder text, debug labels, and missing
   art are `POLISH` or `GAP`, not `BUG`. Score the experience design, not the
   production value.

---

## Section 5 — Prescription Format

For each issue found, produce a prescription block:

```
PRESCRIPTION #N
  Goal:         [alive | teaches | fo_person | profit_discovery | promise_depth]
  Confidence:   [high | medium | low]
  Severity:     [critical | major | minor | suggestion]
  Tag:          [BUG | UX | POLISH | GAP | OPINION]
  Issue:        One sentence describing the problem
  Evidence:     Which goal probes / screenshots support this
  Standard:     What the design doc says should happen
  Prescription: Specific actionable change
  Metric:       How to verify the fix in the next iteration
  Auto-fixable: [yes | no] — can this be resolved by code change without design input?
```

**Confidence definitions:**
- **high** = objectively measurable from evidence (FO spoke 0 times, margin was -28). Act immediately.
- **medium** = subjective but evidence-supported. Worth addressing.
- **low** = pure aesthetic/feel opinion. Present as option only.

**Iteration delta format (iterations 2+):**
```
ITERATION DELTA:
  Improved:   [goals/prescriptions that improved]
  Regressed:  [goals that got worse — side effects]
  Unchanged:  [not addressed or no visible effect]
  New issues: [appeared in this iteration but not the previous]
```

---

## Section 6 — Manual Playtest Checklist Template

After automated evaluation, generate a checklist pre-filled with findings.
The user plays for 15 minutes and scores each goal from their own experience.

```markdown
# First-Hour Playtest Checklist — <iteration_name>

## Automated Findings (pre-filled)
- Goal 1 (Alive): AUTO=<score>/5 — <one-line summary>
- Goal 2 (Teaches): AUTO=<score>/5 — <one-line summary>
- Goal 3 (FO): AUTO=<score>/5 — <one-line summary>
- Goal 4 (Profit): AUTO=<score>/5 — <one-line summary>
- Goal 5 (Depth): AUTO=<score>/5 — <one-line summary>

## Your Turn — Play 15 minutes, then score:

### Goal 1: The Galaxy Is Already Alive
- [ ] On first load, did you see NPC ships moving? (Y/N)
- [ ] Did different stations feel different? (Y/N — what was different?)
- [ ] Could you tell faction territory without opening a menu? (Y/N)
- [ ] Did prices make geographic sense? (Y/N — example?)
- Your score (1-5): ___
- Notes:

### Goal 2: Every Action Teaches Something
- [ ] Did you see any tutorial text or "press X" prompts? (Y/N)
- [ ] After your first trade, could you explain how to make money? (Y/N)
- [ ] Did each dock introduce exactly one new thing? (Y/N — what?)
- [ ] Were you ever confused about what to do next? (Y/N — when?)
- Your score (1-5): ___
- Notes:

### Goal 3: The First Officer Is a Person
- [ ] Did the FO speak in the first 5 minutes? (Y/N)
- [ ] Write a specific FO line you remember:
- [ ] Did the FO ever surprise you? (Y/N — what did they say?)
- [ ] Did the FO react to something you did, or predict it? (react/predict)
- Your score (1-5): ___
- Notes:

### Goal 4: Profit Feels Like Discovery
- [ ] Was your first sale rewarding? (Y/N — why?)
- [ ] After selling, did you think "I should have bought more"? (Y/N)
- [ ] Did the FO react to your profit? (Y/N)
- [ ] Did you know what to trade before the FO told you? (Y/N)
- Your score (1-5): ___
- Notes:

### Goal 5: The Promise of Depth
- [ ] Name 3 things you want to do next:
  1.
  2.
  3.
- [ ] Does the galaxy map make you want to explore? (Y/N)
- [ ] Do you feel like you've only scratched the surface? (Y/N)
- [ ] Is there something specific you're curious about? (what?)
- Your score (1-5): ___
- Notes:

## Overall
- What was the best moment?
- What was the worst moment?
- Would you keep playing? (Y/N)
- Would you show this to a friend? (Y/N)
```

---

## Output Format

The evaluator must return all of the following, in this order:

1. **ASSERTION CHECK** — list any FAIL assertions as CRITICAL blockers
2. **GOAL EVIDENCE SUMMARY** — parsed goal probe data, one block per goal
3. **GOAL SCORES** — table of 5 goals with score and one-line justification
4. **PRESCRIPTIONS** — ranked by (severity x confidence), auto-fixable flagged
5. **ITERATION DELTA** — if previous iteration exists
6. **MANUAL PLAYTEST CHECKLIST** — filled-in template from Section 6
7. **OVERALL** — top 3 strengths, top 3 issues, single priority fix
