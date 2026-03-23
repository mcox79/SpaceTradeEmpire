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

### New Evidence Categories (v2 — depth probes)

The FH bot now emits additional probe lines for systems it exercises beyond the
core first-hour loop. Parse these into supplemental evidence blocks:

```
Systemic Economy:
  FH1|SYSTEMIC|offers=2
  FH1|GOAL|SYSTEMIC|offers=2
  FH1|LEDGER|transactions=5
  FH1|GOAL|ECONOMY|ledger_entries=5
  → 2 dynamic mission offers present. 5 transactions in the ledger.

Factions & Warfronts:
  FH1|FACTIONS|count=5
  FH1|GOAL|ALIVE|factions=5
  FH1|WARFRONT|count=2
  FH1|GOAL|ALIVE|warfronts=2
  → 5 factions exist, 2 active warfronts. Galaxy has political tension.

Knowledge & Depth:
  FH1|KNOWLEDGE|entries=8
  FH1|GOAL|DEPTH|knowledge_entries=8
  FH1|RESEARCH|start=nav_sensors success=true
  FH1|GOAL|DEPTH|research_started=true
  → 8 knowledge web entries discovered. Research is actionable.

Experience Dimensions:
  FH1|EXPERIENCE|flow_credit_growth=0.68
  FH1|EXPERIENCE|tension_min_hull=100
  FH1|EXPERIENCE|factions_visited=2
  FH1|EXPERIENCE|goods_traded=3
  FH1|EXPERIENCE|systems_introduced=["market", "selling", "missions", "combat", "fitting"]
  FH1|GOAL|FLOW|credit_growth=0.68 min_hull=100 factions=2 goods=3
  → Player profited 68%. Never took damage (tension gap). Visited 2 factions.

Coverage:
  FH1|COVERAGE|nodes=8/20(40%) trades=3 goods=2 factions=2 systems_intro=5
  → Explored 40% of galaxy. 3 trades across 2 goods. 5 game systems introduced.
```

These lines feed into the scoring refinements below. Goal scores should account
for these supplemental evidence lines when present.

### Experience Dimension Flags

The bot now tracks and flags experience quality issues:

| Flag | Meaning | Impact |
|------|---------|--------|
| `FLOW_NO_PROFIT` | Player didn't grow credits | Goal 4 score <= 2 |
| `FLOW_TRIVIALLY_RICH` | Credit growth > 1000% | Goal 4 — economy broken |
| `TENSION_NO_DAMAGE_TAKEN` | Hull never dropped below 100% | Goal 1 — galaxy not dangerous |
| `STRANDED_PLAYER` | < 50 credits, no mission, no fuel | CRITICAL — player soft-locked |
| `SOFT_LOCK_<PHASE>` | Bot stuck in a phase for 3+ seconds | CRITICAL — game bug |

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

## Section 2b — Supplemental Scoring Dimensions

These dimensions don't replace the five goals but provide additional scoring
context. Report them as a separate table alongside the goal scores.

### Combat Feel

| Score | Evidence Pattern |
|-------|-----------------|
| 5 | Heat system responds, battle stations spin-up visible, zone armor feedback, AI shoots back, combat feels reactive |
| 4 | Combat resolves correctly, damage numbers visible, NPC destroyed, loot dropped |
| 3 | Combat works but feels instant — one-shot kill, no tension buildup |
| 2 | Combat mechanically present but no feedback (no damage numbers, no VFX evidence) |
| 1 | Combat broken or absent |

**Key evidence:** `FH1|COMBAT|` lines, `FH1|POST_COMBAT|loot=` value, `FH1|EXPERIENCE|tension_min_hull=`.
If `min_hull=100`, player never took damage — combat had zero tension.

### Systemic Economy

| Score | Evidence Pattern |
|-------|-----------------|
| 5 | Systemic offers present (>= 3), offers match world state (warfront goods, price spikes), transaction ledger shows trade history |
| 4 | Some systemic offers present, ledger has entries, profit summary non-zero |
| 3 | Systemic offers exist but feel random, ledger is sparse |
| 2 | Zero systemic offers in 200 ticks, or ledger empty after multiple trades |
| 1 | System broken — methods missing or errors |

**Key evidence:** `FH1|GOAL|SYSTEMIC|offers=`, `FH1|GOAL|ECONOMY|ledger_entries=`, `FH1|LEDGER|profit=`.

### Faction & Warfront Presence

| Score | Evidence Pattern |
|-------|-----------------|
| 5 | Multiple factions visible, territory access varies, warfronts active, player visited 2+ faction territories |
| 4 | Factions exist, territory queryable, at least one warfront |
| 3 | Faction data present but player stayed in one faction's territory |
| 2 | Factions exist in data but zero visual/experiential presence |
| 1 | No faction data or zero warfronts |

**Key evidence:** `FH1|GOAL|ALIVE|factions=`, `FH1|GOAL|ALIVE|warfronts=`, `FH1|EXPERIENCE|factions_visited=`.

### Mission Quality

| Score | Evidence Pattern |
|-------|-----------------|
| 5 | Missions available (3+), accepted mission has clear reward preview, prerequisites visible, mission types vary |
| 4 | Missions available and accepted, reward preview works |
| 3 | Missions exist but rewards unclear, or only one mission type offered |
| 2 | Only 1 mission available, or accept fails, or reward preview empty |
| 1 | Zero missions or system broken |

**Key evidence:** `FH1|ASSERT_PASS|missions_available|count=`, mission accept lines.

### Player Progression Tracking

| Score | Evidence Pattern |
|-------|-----------------|
| 5 | Player stats tracked (nodes, trades, credits earned), milestones accumulating, coverage > 30% |
| 4 | Stats present and non-zero, some milestones |
| 3 | Stats present but sparse, milestones = 0 |
| 2 | Stats method exists but returns empty data |
| 1 | Stats system missing |

**Key evidence:** `FH1|GOAL|STATS|`, `FH1|STATS|milestones=`, `FH1|COVERAGE|`.

### Upkeep Tension

| Score | Evidence Pattern |
|-------|-----------------|
| 5 | Fleet upkeep visible in HUD after undock, costs scale with fleet size, upkeep creates meaningful spending pressure alongside trade profit |
| 4 | Upkeep present and non-zero after undocking, HUD shows running cost |
| 3 | Upkeep exists in data but not visible to player, or negligible amount |
| 2 | Upkeep method returns zero or empty — no spending pressure |
| 1 | Upkeep system missing or broken |

**Key evidence:** `FH1|GOAL|UPKEEP|cost=`, `GetFleetUpkeepV0` return value after undocking.

### Template Mission Discovery

| Score | Evidence Pattern |
|-------|-----------------|
| 5 | Template missions available (3+) with varied types (delivery, escort, scan), reward preview clear, at least one matches current cargo/route |
| 4 | Template missions present and queryable, some variety |
| 3 | Template missions exist but only one type, or none match player context |
| 2 | Zero template missions available, or method returns empty |
| 1 | Template mission system missing or broken |

**Key evidence:** `GetAvailableTemplateMissionsV0` return array size, mission type variety.

### Anomaly Chain Progression

| Score | Evidence Pattern |
|-------|-----------------|
| 5 | Anomaly chains active (1+), chain progression visible, discovery site variety, chains create meaningful exploration incentive |
| 4 | Anomaly chain data present and queryable, at least one chain started |
| 3 | Anomaly chains exist in data but player hasn't encountered any |
| 2 | Anomaly chain method returns empty — no active chains |
| 1 | Anomaly system missing or broken |

**Key evidence:** `GetActiveAnomalyChainsV0` return array, chain state progression.

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

## Section 3b — Boot Experience Checks

These are critical first-30-seconds checks that determine whether the player
even gets to the five-goal evaluation. Score each PASS/WARN/FAIL.

### Welcome / Onboarding
- Is there a welcome overlay, intro popup, or clear "what to do first" guidance?
- If the player is dropped into the game with zero context, score FAIL.
- A subtle toast ("Welcome back, Captain") is WARN — better than nothing but
  insufficient for a first-time player.
- A proper welcome overlay with controls and first objective is PASS.

### Camera Introduction
- Does the game open with any kind of cinematic reveal? (Camera descent, system
  sweep, flyby, zoom from galaxy to system level?)
- If the camera just snaps to default position with no fanfare, score WARN.
- If there's a dramatic camera movement that reveals the starting system, PASS.

### Starting System Quality
- Is the starting system visually interesting? (Star, planets, station nearby,
  NPC activity?)
- If the starting system is empty space with only a star, score FAIL.
- If it has a station but no planets or is sparse, score WARN.

### Input Conflicts
- Do any movement keys (WASD) conflict with UI toggle keys?
- Check project.godot for duplicate physical_keycode bindings.
- If a movement key also opens/closes a UI panel, score CRITICAL — this is a
  game-breaking UX bug that makes the game appear broken.

### Visual Artifacts at Boot
- Are there any combat effects visible at game start? (Red vignette, heat bar,
  damage numbers, shield effects?)
- Combat visual elements should be invisible until combat starts.
- If any combat overlay is visible at boot, score FAIL — player thinks the game
  is broken.

### Physics Walls
- Can the player fly into a star? Is there a hard invisible wall or a
  progressive soft repulsion?
- Hard walls (ship suddenly stops) = FAIL.
- Progressive nudge (ship gradually pushed away) = PASS.

---

## Section 4 — Anti-Hallucination Rules

1. **Never claim absence without certainty.** If you can't see the FO panel in a
   screenshot, say "FO panel not visible in this frame" not "FO panel is missing."
   It may be off-screen, obscured, or not yet promoted.

2. **Tag every issue.** Use one of: `BUG` (broken), `UX` (usable but confusing),
   `POLISH` (works but feels unfinished), `GAP` (designed feature not yet built),
   `OPINION` (subjective preference), `SUPPRESSED` (fully implemented but UI
   hidden/disabled), `UNWIRED` (bridge method exists but no UI consumer).

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

7. **Check for suppression before scoring low.** If a system scores 1/5 and the
   Feature Visibility Report (from Step 2b of the /first-hour skill) shows it as
   `BUILT_BUT_HIDDEN`, note that the fix is a UI toggle, not a system rebuild.
   Report both the **player experience score** (what they see now) and the **code
   capability score** (what the system could deliver if unsuppressed). Example:
   "Goal 3 (FO): Player=1/5, Code=4/5 — FO system fully built with 87 dialogue
   lines but panel is force-hidden in hud.gd."

---

## Section 4b — Tag Definitions

| Tag | Meaning | Typical Fix | Auto-fixable? |
|-----|---------|-------------|---------------|
| `BUG` | Code is broken — wrong behavior | Fix the logic | Usually yes |
| `UX` | Works but confusing to player | Adjust UI flow | Usually yes |
| `POLISH` | Works but feels unfinished | Visual/audio pass | Sometimes |
| `GAP` | Feature not yet built | New code required | No |
| `OPINION` | Subjective preference | Optional | No |
| `SUPPRESSED` | Fully implemented in code but UI is hidden/disabled. The system works — the player just can't see it. These are typically the highest-leverage fixes: large impact, small code change (remove a `visible = false` line or add a keybind) | Toggle visibility | Almost always yes |
| `UNWIRED` | Bridge method exists and returns real data, but no GDScript UI code calls it. The data pipeline is complete through the sim layer — it just needs a UI consumer | Build UI widget | No (design needed) |

---

## Section 5 — Prescription Format

For each issue found, produce a prescription block:

```
PRESCRIPTION #N
  Goal:         [alive | teaches | fo_person | profit_discovery | promise_depth]
  Confidence:   [high | medium | low]
  Severity:     [critical | major | minor | suggestion]
  Tag:          [BUG | UX | POLISH | GAP | OPINION | SUPPRESSED | UNWIRED]
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
4. **SUPPLEMENTAL DIMENSIONS** — table with:
   - Performance: FPS min/avg/max from FH1|PERF| data. Score: PASS (min>=30, avg>=50) / WARN / FAIL
   - Stability: Save/load roundtrip, soft-lock count. Score: PASS / WARN / FAIL
   - Content Quality: Dev jargon flags, label overflow. Score: PASS (zero flags) / WARN / FAIL
   - Dispatch Reliability: Silent failure count. Score: PASS (zero) / WARN / FAIL
5. **EA READINESS** — classification: BLOCKED / NOT_READY / CONDITIONAL / READY with justification
6. **CODE vs EXPERIENCE GAP** — ACTIVE / BUILT_BUT_HIDDEN / NO_UI_CONSUMER counts
7. **PRESCRIPTIONS** — ranked by (severity x confidence), EA tier tagged, auto-fixable flagged
8. **ITERATION DELTA** — if previous iteration exists
9. **MANUAL PLAYTEST CHECKLIST** — filled-in template from Section 6
10. **OVERALL** — top 3 strengths, top 3 issues, single priority fix
