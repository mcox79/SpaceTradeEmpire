---
name: audit
description: "Full-game audit: runs bots, evals, optimizers, AI analysis. Compiles unified problem list, fixes issues, plugs monitoring gaps, iterates until clean."
argument-hint: "[mode: full|quick|first-hour] [eval-only|fix-only|gaps] [iteration-name]"
---

# /audit — Game Audit & Auto-Fix

Parse `$ARGUMENTS`:
- **mode** (first word): `full` (default) | `quick` | `first-hour`
- **modifier** (optional): `eval-only` | `fix-only` | `gaps` — composable with any mode
- **iteration-name** (optional): e.g., `baseline`, `post-fix`. Default: `audit_N`

| Mode | What it does | Time |
|------|-------------|------|
| `full` | All bots + all evals + fix + iterate | ~30-60 min |
| `quick` | Build + tests + semgrep + optimize scan + coverage gap | ~90s |
| `first-hour` | Experience-focused: 3 bots + multi-seed + 11 LLM evals + fix | ~20-30 min |

| Modifier | Effect |
|----------|--------|
| `eval-only` | Stop after problem compilation (no fixes) |
| `fix-only` | Skip bot runs, read latest reports, compile + fix |
| `gaps` | Skip bot runs, run coverage gap analysis only |

Examples: `/audit first-hour`, `/audit first-hour eval-only`, `/audit full baseline`,
`/audit fix-only`, `/audit gaps`.

**Godot constraint**: Only one Godot process at a time. All bot runs are SEQUENTIAL.
Only non-Godot tools (optimize scan, LLM evals, RL smoke) can truly parallelize.

---

## Shared: Build & Verify (all modes except `fix-only` and `gaps`)

1. Read `reports/audit/JOURNAL.md` if exists — check ALL carried-forward items
   (from any prior mode — full, first-hour, etc.)
2. Record git SHA: `git rev-parse --short HEAD`
3. Build: `dotnet build "Space Trade Empire.csproj" --nologo -v q`
4. Test: `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --nologo -v q`
   — any failures → CRITICAL, fix before proceeding
5. Semgrep (if installed): `semgrep --config .semgrep.yml --quiet SimCore/`
6. GDScript lint (if installed): `gdlint scripts/`
7. Create `reports/audit/<iteration>/`

Full mode also builds RL server:
`dotnet build SimCore.RlServer/SimCore.RlServer.csproj -c Release --nologo -v q`

---

## Quick Mode

```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-AuditQuick.ps1
```
Runs: Build → C# Tests (1539 incl. FsCheck + Coyote) → Optimize Scan →
Coverage Gap → Semgrep → GDScript Lint. Exit 0 = clean. Done.

---

## First-Hour Mode

Focused on player experience quality. Runs experience + first-hour bots, domain
eval bots, multi-seed sweeps, LLM evaluations, coverage gaps, then fixes and iterates.

### FH-1: Sequential Bot Runs

| # | Bot | Command | Output | Time |
|---|-----|---------|--------|------|
| 1 | Experience (visual, seed 42) | `Run-ExperienceBot.ps1 -Mode visual -Seed 42` | `reports/experience/balanced/seed_42/report.json` + `reports/experience/screenshots/*.png` | ~3m |
| 2 | First-Hour Proof (visual) | `Run-FHBot.ps1 -Mode visual` | `reports/first_hour/stdout.txt` + `reports/first_hour/*.png` | ~2m |
| 3 | Domain Eval (5 bots) | `Run-EvalBot.ps1` | `reports/eval/*_stdout.txt` | ~5m |
| 4 | Experience multi-seed (headless) | `Run-ExperienceBot.ps1 -Mode headless -Sweep` | `reports/experience/balanced/seed_*/report.json` | ~5m |
| 5 | FH multi-seed (headless, 3s) | `Run-FHBot-MultiSeed.ps1 -Seeds 42,99,1001` | SEED_SWEEP summary | ~3m |
| 6 | Experience slow-bot (headless, seed 42) | `Run-ExperienceBot.ps1 -Mode headless -Seed 42 -Slow` | `reports/experience/balanced_slow/seed_42/report.json` | ~8m |

The **slow bot** (bot #6) runs the same experience bot logic but with human-paced
delays (1-3 second pauses between actions). This reveals whether dead zones and
pacing issues are real player problems or bot-speed artifacts. Compare slow-bot
dead zone count against fast-bot to calibrate pacing findings. If dead zones
disappear at human pace, downgrade from CRITICAL to MAJOR.

All commands: `powershell -ExecutionPolicy Bypass -File scripts/tools/<command>`.

**In parallel with bot runs** (non-Godot):
- Optimize scan: `Run-OptimizeScan.ps1` → `reports/optimization/scan_*.md`

**After each bot run**: check `stderr.txt` for `SCRIPT ERROR`. If found, log as
CRITICAL finding (bot contract broken) and continue with remaining bots.

**If a bot hangs** (>2× expected time): kill the process, log TIMEOUT finding, continue.

**Bot health check** (after all bots complete): Compare experience bot key metrics
against verification bot baseline (FH-1b). Flag any metric with >50% discrepancy
as BOT_BUG — these are measurement errors in the experience bot, not game bugs.
Bot bugs must be fixed before the next audit iteration.

**What each captures:**
- **Experience bot** — 27 dimensions scored, classified as ACTIVE or STABLE:
  - **ACTIVE** (report these — producing signal): economy, pacing, combat, exploration,
    grind, FO, disclosure, progression, market intel, narrative, combat depth, story,
    dread, diplomacy, pressure, construction, cognitive load, valence arc, competence,
    pacing rhythm, economy depth.
  - **STABLE** (suppress from reports — consecutive PASS across 3+ audits): security,
    haven, dead-end, missions, fleet, retention.
  Only ACTIVE dimensions appear in the scorecard and issue list. STABLE dimensions
  are still measured but omitted from reports unless they regress to non-PASS.
  Plus: credit curve shape, issue detection with severity + prescriptions + file refs.
- **FH proof bot** — 18+ hard assertions, 5 goal evidence (`FH1|GOAL|`),
  perf (`FH1|PERF|`), flags, dispatch reliability, content quality.
- **Domain eval bots** — economy_health (Gini, price stability, faucet-sink),
  narrative_pacing (dialogue density, silence ratio), dread_pacing (tension curves,
  relief ratio), audio_atmosphere, flight_feel.
- **Multi-seed** — cross-seed stability: which issues are universal vs seed-specific.

### FH-1b: Verification Bot (early false-positive filter)

Run verification BEFORE LLM evals. This catches false positives from bot
measurement bugs, preventing eval agents from wasting cycles analyzing phantom
issues. The probes are predefined and do not depend on a compiled problem list.

**Run both modes sequentially:**

```bash
# 1. Headless — fast metric verification (~60s)
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-VerifyIssues.ps1 -Mode headless -Seed 42

# 2. Visual — screenshot + scene-tree verification (~90s)
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-VerifyIssues.ps1 -Mode visual -Seed 42
```

After both runs, parse `VFY|VERIFY|probe|status|evidence` lines from stdout.
Write preliminary `reports/audit/<iter>/verification_preliminary.md` with
CONFIRMED/UNCONFIRMED status per probe. This data is consumed by LLM eval agents
in FH-2 (agents should skip analyzing UNCONFIRMED metrics) and by FH-4 when
compiling the unified problem list.

See FH-4b probe-to-issue mapping table for the full probe list.

### FH-2: LLM Evaluations (parallel agents)

Launch ALL as parallel Agent calls.

**IMPORTANT — Agent isolation rule**: Each agent writes exactly ONE file. Never
combine two agents into one dispatch (e.g., "Agent A+B"). Compound agents risk
silent partial failures where one output file is written and the other is 0 bytes.

**IMPORTANT — Verification data**: All agents should read
`reports/audit/<iter>/verification_preliminary.md` (from FH-1b). When an issue is
marked UNCONFIRMED by verification, the agent should note it as a likely false
positive rather than scoring it as a real problem.

**A: Goal Evaluation (Sonnet)**
1. Parse `FH1|GOAL|*`, `FH1|ASSERT_*`, `FH1|FLAG|*`, `FH1|PERF|*` from `reports/first_hour/stdout.txt`
2. Read `scripts/tools/first_hour_rubric.md`
3. Build Feature Visibility Report: grep bridge methods → classify
   ACTIVE / BUILT_BUT_HIDDEN / NO_UI_CONSUMER
4. Read ≤10 screenshots from `reports/first_hour/*.png`
5. Score 5 goals (1-5), generate prescriptions (BUG/UX/POLISH/GAP/SUPPRESSED/UNWIRED)
6. Classify EA readiness: BLOCKED / NOT_READY / CONDITIONAL / READY
7. Write `reports/audit/<iter>/first_hour_goals.md`

**B: Experience Analysis (Sonnet)**
1. Read `reports/experience/balanced/seed_42/report.json` (20 dimensions + issues)
2. Read `reports/experience/identified_issues.md` if exists
3. Cross-reference with multi-seed reports (universal vs seed-specific?)
4. Merge with domain eval findings (economy_health→ECONOMY, narrative_pacing→FO/NARRATIVE,
   dread_pacing→COMBAT/SECURITY)
5. Deduplicate: same system from multiple sources → keep highest severity
6. Write `reports/audit/<iter>/experience_analysis.md`

**C: Feel Evaluation (Sonnet)**
1. Read ≤10 representative screenshots from `reports/experience/screenshots/` +
   `reports/first_hour/*.png` (experience bot captures ~58, select representative subset)
2. Read `scripts/tools/visual_eval_guide.md`
3. Evaluate: Composition, Readability, Scale & Space, Polish, Atmosphere
4. 5 perspectives: first-time player, art director, UX designer, game designer, space fan
5. Rate each PASS/NEEDS_WORK/FAIL
6. Write `reports/audit/<iter>/feel_eval.md`

**D: Seed Variance — Primary Diagnostic (Sonnet)**

Seed variance is the most diagnostic tool in the audit. It instantly separates
universal issues (same on all seeds → design problem) from stochastic ones
(varies wildly → RNG/topology problem). Promoted from inline to full agent.

1. Read ALL multi-seed experience reports (`reports/experience/balanced/seed_*/report.json`)
2. For each ACTIVE dimension, compute mean, stdev, min, max across seeds
3. Classify each dimension:
   - **UNIVERSAL** (stdev/mean < 0.15): Same issue on all seeds → design problem, highest priority
   - **VARIABLE** (stdev/mean 0.15-0.50): Moderate variance → topology or RNG sensitivity
   - **CHAOTIC** (stdev/mean > 0.50): Wildly different per seed → investigate root cause
4. Compare FH goal scores across seeds, flag range > 2
5. For VARIABLE/CHAOTIC dimensions, identify the outlier seed and hypothesize cause
   (map topology, NPC spawn patterns, market init, etc.)
6. Produce a **triage recommendation**: which problems to fix first based on universality
7. Compare slow-bot results (if available) against fast-bot to flag bot-speed artifacts
8. Write `reports/audit/<iter>/seed_variance.md` with triage table

**E: Moment Quality Evaluation (Sonnet)**

The first hour has 5 design-mandated moments. Each must land with emotional weight,
not just mechanical correctness. This agent evaluates whether each moment *felt right*.

1. Parse bot telemetry for timing of key events:
   - **Heist Moment**: First profitable trade. When did it happen (decision #)?
     Was margin > 100 cr/unit? Did FO react? Did credits visibly jump?
   - **Companion Moment**: FO personality reveal. When did FO first speak?
     Was it after a player action (reactive) or scripted (proactive)?
     Did archetype come through in word choice?
   - **Danger Moment**: First combat encounter. Did player take damage?
     Was there a beat of tension before resolution? Did the world feel unsafe?
   - **Power Moment**: First upgrade or automation setup. Did the ship change
     visually? Did the FO acknowledge the upgrade? Did gameplay feel different after?
   - **Promise Moment**: First glimpse of depth (galaxy map, tech tree, empty slots).
     How much of galaxy remains unexplored? Are there visible "locked" systems?
     Does the player have 3+ things they could do next?
2. Read ≤5 screenshots capturing these moments (post-sell, post-combat, post-upgrade,
   galaxy map, final state)
3. Read `scripts/tools/first_hour_feel_rubric.md` Section 1 (Moment Quality)
4. Score each moment 1-5 on: **timing** (when in the session), **impact** (visual/audio
   feedback intensity), **emotional resonance** (did it feel personal, not mechanical),
   **follow-through** (did the game acknowledge it happened)
5. Flag moments that were ABSENT (never triggered), FLAT (triggered but no feedback),
   or RUSHED (multiple moments in < 30 decisions)
6. Write `reports/audit/<iter>/moment_quality.md`

**F: Emotional Arc Analysis (Sonnet)**

Maps the intensity curve of the first hour. The design mandates peaks and valleys —
not a flat line or a single ramp.

1. Parse experience bot credit curve + decision log + combat events + FO dialogue events
2. Build an intensity timeline with these event types:
   - HIGH: first profit, combat, upgrade, automation unlock, discovery, warfront encounter
   - MEDIUM: new station dock, price comparison, mission accept, FO dialogue
   - LOW: travel between nodes, waiting, repeated trade at same station
   - NEGATIVE: loss (credits dropped), death/respawn, confusion (no clear next step)
3. Analyze the curve for:
   - **Dead zones**: > 50 decisions with no HIGH or MEDIUM event → flag as CRITICAL
   - **Plateau**: > 100 decisions at same intensity level → flag as WARNING
   - **Rush**: 3+ HIGH events within 30 decisions → flag as WARNING (overwhelming)
   - **Crescendo quality**: Does intensity generally increase over the session? Or peak early and decline?
   - **Valley depth**: Are quiet moments truly quiet (travel, contemplation) or just empty (nothing happening)?
   - **Rhythm**: Is there an alternating pattern (action→rest→action) or random clustering?
4. Compare against reference arcs:
   - FTL: peaks every 2-3 minutes, valleys are choices (not waiting)
   - Subnautica: slow build to first scare, then escalating
   - Factorio: gradual build, satisfaction of first automation is the climax
5. Score: **crescendo** (1-5), **rhythm** (1-5), **dead_zones** (count), **valley_quality** (1-5)
6. Write `reports/audit/<iter>/emotional_arc.md`

**G: Juice & Feedback Evaluation (Sonnet)**

Evaluates whether player actions have satisfying visual/audio feedback. "Juice" is
the difference between "it works" and "it feels good."

1. Read ≤10 screenshots focusing on action moments:
   - Combat screenshots: Is there VFX on weapon fire? Shield/hull impact flash?
     Damage numbers? Screen shake? Explosion on kill?
   - Trade screenshots: Credit counter animation? Profit highlight? Cargo change indicator?
   - Dock screenshots: Docking animation/transition? Station proximity feedback?
   - Upgrade screenshots: Module installation VFX? Ship visual change? Stat change highlight?
   - Discovery screenshots: Scan progress VFX? Discovery reveal animation? Audio cue indicator?
2. Read `scripts/tools/first_hour_feel_rubric.md` Section 2 (Juice)
3. For each action category, score:
   - **Immediacy** (1-5): Does feedback happen instantly on action?
   - **Proportionality** (1-5): Does feedback intensity match action importance?
   - **Multi-channel** (1-5): Does feedback use multiple senses (visual + audio + haptic)?
   - **Variety** (1-5): Do different actions feel different, or all the same?
4. Flag: SILENT_ACTION (player did something, nothing visible happened),
   DELAYED_FEEDBACK (feedback > 1s after action), DISPROPORTIONATE (tiny action, huge effect or vice versa)
5. Reference standards: Hades (every hit feels powerful), Factorio (satisfying placement click),
   FTL (weapon charge + fire + impact = 3-stage feedback)
6. Write `reports/audit/<iter>/juice_eval.md`

**H: Spatial Navigation Clarity (Sonnet)**

Evaluates whether the player always knows WHERE they are, WHERE they can go, and
WHAT is nearby. Critical for a space game where disorientation = frustration.

1. Read ≤8 screenshots covering different spatial contexts:
   - System view (in-flight): Can you identify the current system? Are lanes/gates visible?
     Are stations labeled? Is the player ship distinguishable from NPCs?
   - Galaxy map: Are visited/unvisited nodes clearly distinguished? Are faction borders
     visible? Can you trace a route? Is the player's location marked?
   - Arrival at new system: Does the camera reveal the system layout? Are points of
     interest visible (stations, gates, anomalies)?
   - Dock approach: Is it clear which station you're approaching? Is the "press E" prompt visible?
   - Combat: Can you tell friend from foe? Is the engaged enemy highlighted?
2. Read `scripts/tools/first_hour_feel_rubric.md` Section 3 (Spatial Clarity)
3. Score each context:
   - **Orientation** (1-5): "Where am I?" answerable in < 2 seconds
   - **Wayfinding** (1-5): "How do I get there?" clear from current view
   - **Threat awareness** (1-5): Dangers visible before they engage
   - **Destination clarity** (1-5): Next objective/target visually obvious
4. Flag: LOST_PLAYER (no orientation cues), INVISIBLE_LANES (can't see route options),
   AMBUSH_UNFAIR (threat not visible before combat), IDENTICAL_SYSTEMS (can't tell systems apart)
5. Reference: Elite Dangerous (compass + destination marker), Freelancer (lane visible as light trail),
   Stellaris (faction borders clear at a glance)
6. Write `reports/audit/<iter>/spatial_clarity.md`

**I: Audio Atmosphere Deep Eval (Sonnet)**

Goes beyond the domain eval bot's state-machine validation to evaluate audio QUALITY
and emotional contribution. Uses domain eval output + screenshots + design docs.

0. Pre-check: Grep `scripts/audio/music_manager.gd` for `_has_real_audio`. If the value
   is `false`, **SKIP this entire eval**. Write a 3-line report to
   `reports/audit/<iter>/audio_atmosphere_deep.md`: "Audio suppressed by placeholder
   guard (_has_real_audio=false). All buses silent by design. Eval skipped — no signal
   until real audio stems are loaded." Do NOT evaluate state machine logic or transition
   design — those evaluations produce zero actionable findings while audio is suppressed.
1. Read domain eval bot output: `reports/eval/audio_atmosphere_eval_v0_stdout.txt`
2. Read `scripts/tools/first_hour_feel_rubric.md` Section 4 (Audio)
3. Cross-reference with design intent from `docs/design/music_production_brief_v0.md`
   (if exists) or known audio design goals:
   - Opening should be sparse, contemplative
   - First trade should feel safe (gentle melody)
   - Exploration should feel vast (ambient pads)
   - Combat should feel intense but not cacophonous
   - Silence is a deliberate tool, not a bug
4. Evaluate from screenshots + bot telemetry:
   - **Music presence**: Is music playing at each phase? (boot, dock, flight, combat)
   - **State transitions**: Do transitions feel smooth or jarring? (combat→calm crossfade)
   - **Silence-as-design**: Are quiet moments intentional (post-combat, deep space) or gaps?
   - **Audio layering**: Are there multiple audio streams (music + ambient + SFX)?
   - **Emotional match**: Does the audio mood match the gameplay moment?
5. Score: **restraint** (1-5 — is silence used well?), **impact** (1-5 — do peak moments
   have peak audio?), **layering** (1-5 — depth of soundscape), **transitions** (1-5 —
   crossfade smoothness), **emotional_match** (1-5 — mood alignment)
6. Write `reports/audit/<iter>/audio_atmosphere_deep.md`

**J: Typography & Visual Identity (Sonnet)**

Evaluates whether the game has a cohesive visual language — consistent fonts, colors,
and faction-specific visual identity. AAA games are instantly recognizable from a
screenshot.

1. Read ≤10 screenshots covering all major UI surfaces:
   - HUD (in-flight labels, credit counter, hull bar)
   - Dock menu (tabs, goods list, prices, buttons)
   - Galaxy map (node labels, faction colors, route indicators)
   - Overlay panels (J/K/L screens — missions, knowledge, fleet)
   - Combat HUD (heat bar, weapon status, damage numbers)
   - FO panel (portrait, dialogue text, speaker name)
   - Toast notifications (FO hails, event notifications)
2. Read `scripts/tools/first_hour_feel_rubric.md` Section 5 (Typography & Identity)
3. Evaluate:
   - **Font hierarchy** (1-5): Clear title/body/caption distinction? Headers bigger than body?
     Numbers in monospace? Flavor text differentiated from data?
   - **Color palette** (1-5): Consistent color scheme across panels? Faction colors distinct?
     Alert colors reserved for alerts? Background/foreground contrast sufficient?
   - **Faction identity** (1-5): Can you tell which faction owns a station from visuals alone?
     Do faction stations have distinct colors, models, or decorative elements?
   - **UI consistency** (1-5): Do all panels use the same button style, spacing, padding?
     Are scrollbars and tooltips consistent? Does every panel feel like the same game?
   - **Visual character** (1-5): Does the game have a recognizable aesthetic identity?
     Could you identify a screenshot as STE without seeing the title?
4. Flag: FONT_CHAOS (3+ different font sizes with no hierarchy), COLOR_CLASH (clashing
   hues in same panel), FACTION_IDENTICAL (two factions indistinguishable), DEVELOPER_UI
   (panels look like debug tools, not a game)
5. Reference: EVE Online (consistent UI identity), Stellaris (faction colors), Starsector
   (readable dense information), Dead Cells (one recognizable font family, one color palette)
6. Write `reports/audit/<iter>/typography_identity.md`

**K: Onboarding Clarity Evaluation (Sonnet)**

Evaluates whether a first-time player can understand what to do without external help.
The game doesn't use tutorials — so the world itself must teach.

1. Parse FH bot output for progressive disclosure data:
   - Tab disclosure state at each phase (which tabs visible when?)
   - FO dialogue content (does FO guide without instructing?)
   - Action-consequence pairs (did buying change something visible?)
2. Read ≤5 screenshots from early phases (boot, first dock, first undock, first arrival, second dock)
3. Read `scripts/tools/first_hour_feel_rubric.md` Section 6 (Onboarding)
4. Evaluate:
   - **Direction** (1-5): At each phase, is there a clear "suggested next step"?
     Not a tutorial arrow — but an environmental cue (station nearby, lane glowing, FO hint)
   - **Permission** (1-5): Can the player deviate from the suggested path without punishment?
     Are side paths visible and accessible? Does the game accommodate curiosity?
   - **Consequence visibility** (1-5): When the player acts, is the result immediate and obvious?
     Buy → cargo counter changes, sell → credits jump, warp → prices differ
   - **Recovery** (1-5): If the player makes a "bad" decision (sells cheap, buys wrong good),
     can they recover without restarting? Is the penalty proportional (not game-ending)?
   - **Control discoverability** (1-5): Are movement keys (WASD), dock (E), galaxy map (Tab/M)
     discoverable? Is there a control hint visible at boot? Or does the player guess?
   - **Pacing** (1-5): Are new systems introduced one at a time with breathing room?
     Or does the game dump trade + combat + modules + missions in one dock?
5. Score the "7 First-Hour Commandments" from the design doc:
   - Minute 1 = identity? Direction + permission? Core loop in 10 min? First trade = heist?
   - Front-load upgrades? One system per encounter? Never lost for 30s?
6. Flag: TUTORIAL_POPUP (explicit instruction breaks immersion), SYSTEM_DUMP (2+ new
   systems at once), DEAD_END (player stuck with no clear path), INVISIBLE_CONTROLS
   (key controls undiscoverable)
7. Write `reports/audit/<iter>/onboarding_clarity.md`

**L: FO Writing Quality Evaluation (Sonnet)**

The First Officer is the game's primary voice. Bad writing kills immersion faster
than any visual bug. This eval checks whether FO dialogue reads like a real character.

1. Collect the FO dialogue corpus from bot telemetry:
   - Parse all `FH1|FO|` and `EXP|FO|` lines from bot stdout
   - Read FO dialogue content files: Grep `scripts/bridge/SimBridge*.cs` and
     `SimCore/Content/` for dialogue text arrays, trigger conditions, archetype data
   - Read `docs/design/NarrativeDesign.md` for intended FO personality profiles
2. Evaluate the corpus on 5 axes:
   - **Tone consistency** (1-5): Does the FO sound like the same person across all lines?
     Mixed register (formal + slang) = inconsistent. Archetype voice drift = inconsistent.
   - **Archetype distinctiveness** (1-5): Can you tell Analyst from Veteran from Pathfinder
     by word choice alone (without seeing the speaker label)? Or do all three sound generic?
   - **Emotional appropriateness** (1-5): Does the FO react differently to profit vs danger
     vs discovery? Or does every line have the same neutral tone? Post-combat lines should
     feel different from post-trade lines.
   - **Information density** (1-5): Do FO lines convey useful game state (prices, threats,
     opportunities) or are they flavor-only? Best FO lines do BOTH — personality + intel.
   - **Brevity** (1-5): Are lines short enough to read during gameplay without pausing?
     Max 2 sentences for reactive lines. Tutorial/narrative beats can be longer.
3. Flag specific problems:
   - VOICE_DRIFT: Same archetype uses conflicting registers across lines
   - ARCHETYPE_CLONE: Two archetypes produce indistinguishable dialogue
   - ROBO_SPEAK: Lines sound generated/template-filled rather than authored
   - INFO_VACUUM: FO speaks but says nothing useful about game state
   - WALL_OF_TEXT: Any single FO line > 80 words
4. Score the overall FO writing quality and compare against reference:
   - Stellaris advisors (distinct voice per type, useful intel)
   - FTL crew (terse, contextual, personality in word choice)
   - Hades Zagreus banter (reactive, character-building, never blocks gameplay)
5. Write `reports/audit/<iter>/fo_writing_quality.md`

### FH-3: Coverage Gap Analysis

```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-CoverageGap.ps1 -OutputDir reports/audit/<iter>
```

The enhanced coverage tool produces:
- **JSON report** (`coverage_report.json`): machine-readable for downstream tools
- **Markdown report** (`coverage_report.md`): human-readable with full detail

Output includes:
- **Per-bridge-partial coverage**: e.g., SimBridge.Fleet.cs 8/12 (67%)
- **Per-bot breakdown**: which bot exercises which methods
- **UI_ONLY methods**: called by UI code but never bot-tested (priority targets)
- **UNCALLED methods**: not referenced anywhere — potentially dead code
- **Critical untested UI scripts**: first-hour-relevant `.gd` files no bot loads
- **Historical delta**: coverage change vs previous run

Copy the markdown report to `reports/audit/<iter>/coverage_gaps.md`.
Use the JSON report in FH-4 problem compilation for structured gap entries.

### FH-4: Compile Unified Problem List (split + capped)

Merge all outputs using the shared severity mapping and problem format
(see Shared: Problem Format section below).

**Split into two sections:**

**Section 1: Code Bugs** (auto-fixable=yes, Tag=BUG/SUPPRESSED/UNWIRED)
These are concrete code defects the audit can fix without design input.
Force-rank by severity, then by seed universality (universal > variable > chaotic).
**Cap at 15 items.** If more than 15, keep top 15 and note "N additional items
deferred to next audit" at the bottom.

**Section 2: Design Decisions** (auto-fixable=needs-design, Tag=GAP/OPINION/UX)
These require human judgment. Do NOT attempt to fix these. Present as a prioritized
backlog with 2-3 sentence context per item. No cap — list all for design review.
Include seed variance classification (UNIVERSAL/VARIABLE/CHAOTIC) for each.

Use seed variance results from agent D to annotate every problem with its
universality classification. Universal issues sort above variable ones.

Write `reports/audit/<iter>/unified_problems.md`.
If modifier is `eval-only`, STOP HERE.

### FH-4b: Finalize Verification Report

Verification probes already ran in FH-1b. This step maps probe results to the
compiled unified problem list and produces the final verification report.

1. Read `reports/audit/<iter>/verification_preliminary.md` (from FH-1b)
2. Match each probe to issues in unified_problems.md by category
3. Update each issue with verification status (CONFIRMED/UNCONFIRMED/SKIP)
4. Write `reports/audit/<iter>/verification_report.md`
5. **Only fix CONFIRMED issues** in FH-5. Flag UNCONFIRMED for manual investigation.

If FH-1b was skipped (e.g., time budget), run the verification bot here instead:

```bash
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-VerifyIssues.ps1 -Mode headless -Seed 42
powershell -ExecutionPolicy Bypass -File scripts/tools/Run-VerifyIssues.ps1 -Mode visual -Seed 42
```

**Probe-to-issue mapping:**

| Probe | Verifies Issue Category | Mode | What It Checks |
|-------|------------------------|------|----------------|
| tab_disclosure | COGNITIVE_LOAD / SYSTEM_DUMP | both | Tabs visible at first dock (expect ≤2) |
| system_dump | COGNITIVE_LOAD | both | Systems introduced per dock |
| fo_dock_greeting | FO silence | both | FO speaks at first dock |
| keybind_hints | INVISIBLE_CONTROLS | visual | Control hints visible in HUD |
| dock_panel_visible | Dock panel appearance | visual | HeroTradeMenu renders on dock |
| credit_feedback | SILENT_PROFIT | visual | CreditsFlash fires on sell |
| heist_margin | Heist moment quality | both | First trade margin > 50 cr |
| lane_visibility | INVISIBLE_LANES | visual | LaneLine/LaneGate nodes in scene |
| camera_distance | Camera too far | visual | Camera Y < 200 units |
| heading_indicator | Spatial orientation | visual | Heading/compass node exists |
| system_identity | IDENTICAL_SYSTEMS | visual | Star visuals differ across systems |
| combat_damage_flash | Combat juice | visual | DamageFlash alpha > 0 on hit |
| combat_vignette | Combat juice | visual | CombatVignette activates |
| combat_screen_shake | Combat juice | visual | Shake intensity > 0 |
| combat_banner | Combat juice | visual | Combat banner text visible |
| combat_loot | Combat loot rate | both | Loot in cargo after kill |
| module_available | Power moment | both | Modules available + slots open |
| galaxy_map_nodes | Galaxy map rendering | visual | Beacon nodes visible in GalaxyView |
| galaxy_player_marker | LOST_PLAYER | visual | "You are here" marker exists |
| galaxy_faction_overlay | Faction visibility | visual | Territory/faction nodes in map |
| economy_sinks | Economy exponential | both | Sink/faucet ratio > 0.05 |
| route_diversity | Route grind | both | Unique routes / total > 0.3 |
| fo_silence | FO silence gap | both | Max silence < 100 ticks |
| competence_margins | Margin regression | both | Late margins ≥ 80% of early |
| discovery_count | Zero discoveries | both | At least 1 discovery |
| fps_minimum | FPS blocker | both | Min FPS ≥ 30 |
| toast_type_differentiation | DEVELOPER_UI | visual | Toasts have type/color coding |

**Output**: `reports/verification/stdout.txt`, `reports/verification/report.json`,
`reports/verification/*.png` (visual mode screenshots)

### FH-5: Fix Pass

Work highest severity first. Rules in Shared: Fix Rules section below.
Track in `reports/audit/<iter>/fix_log.md`.

### FH-6: Re-Evaluate

**FH-6 is mandatory when fixes are applied. Run headless bots only if visual bots would exceed time budget.** Do NOT skip this step. If ANY SimCore fix was applied in FH-5, FH-6 MUST run the affected bots to verify the fix and capture before/after deltas.

Re-run only affected bots:
- SimCore logic changed → experience bot (headless) + FH bot (headless) — MANDATORY after SimCore fixes
- UI/bridge changed → experience bot (visual)
- Feel fixes → screenshot capture only
- Audio changes → audio atmosphere eval bot (headless)

Re-run only affected LLM evals:
- Visual changes → feel eval + juice eval + spatial clarity + typography eval
- Audio changes → audio atmosphere deep eval
- Pacing/economy changes → emotional arc + moment quality
- Onboarding changes → onboarding clarity

Compare before/after: 20-dimension scorecard + 5 goal scores + 5 moment scores +
feel layer dashboard (7 dimensions).
Write `reports/audit/<iter>/fix_delta.md`.

### FH-7: Final Report + Self-Improvement

Write `reports/audit/<iter>/audit_report.md` using the first-hour report template
(see Shared: Report Templates below).

Reflect and write `reports/audit/<iter>/self_improvement.md`:
- What did this first-hour audit miss?
- Should any eval bot capture different metrics?
- Were experience bot thresholds well-calibrated?
- Did any bot crash or timeout? Why?
- Were the 5 design-mandated moments all detectable from bot telemetry?
- Did any feel-layer eval lack sufficient data? (e.g., audio eval needs more bot samples?)
- Which AAA reference patterns are hardest to evaluate automatically?
- Are the moment timing targets (decision ranges) well-calibrated for this game's pace?

Append to `reports/audit/JOURNAL.md` (see Shared: Journal Format).
Iteration cap: 3 loops.

---

## Full Mode

Everything. All bots, all evals, all fixes. Steps 0-10.

### Step 1 — Bot Runs (sequential) + Scans

| # | Tool | Command | Key Output |
|---|------|---------|-----------|
| 1 | FH proof bot (visual) | `Run-FHBot.ps1 -Mode visual` | 18+ assertions, goal evidence, screenshots |
| 2 | Deep systems bot (headless) | `Run-FHBot-MultiSeed.ps1 -Script deep_systems -Seeds 42` | 5000+ assertions, 15+ domains |
| 3 | Visual sweep (visual) | `Run-Screenshot.ps1 -Mode full` | ~24 screenshots |
| 4 | Experience bot (visual, seed 42) | `Run-ExperienceBot.ps1 -Mode visual -Seed 42` | 20 dimensions + 58 screenshots |
| 5 | Stress bot (headless) | `Run-Bot.ps1 -Mode stress -Cycles 1500` | Economy stability |
| 6 | Domain eval bots (headless) | `Run-EvalBot.ps1` | 5 domain reports |
| 7 | Experience multi-seed (headless) | `Run-ExperienceBot.ps1 -Mode headless -Sweep` | 20 dimensions × 5 seeds |
| 8 | FH multi-seed (headless, 5s) | `Run-FHBot-MultiSeed.ps1 -Seeds 42,99,1001,31337,77777` | Seed stability |
| 9 | Tutorial multi-seed (headless, 3s) | `Run-FHBot-MultiSeed.ps1 -Script tutorial -Seeds 42,100,1001` | 45 phases × 3 FO types |
| 10 | Chaos tutorial (headless, 5s) | `Run-FHBot-MultiSeed.ps1 -Script chaos_tutorial -Seeds 42,99,1001,31337,77777` | Adversarial scenarios |

All commands: `powershell -ExecutionPolicy Bypass -File scripts/tools/<command>`.

**In parallel with bot runs** (non-Godot):
- Optimize scan: `Run-OptimizeScan.ps1`
- RL smoke: `Run-RlTrain.ps1 -Mode smoke`

After each bot: check stderr for `SCRIPT ERROR`. Hung bot (>2× time): kill, log TIMEOUT.

### Step 2 — LLM Evaluations (parallel agents)

| Agent | Depends On | Output |
|-------|-----------|--------|
| FH goal eval | Bot #1 | `first_hour_eval.md` |
| Deep systems eval | Bot #2 | `deep_systems_eval.md` |
| Experience analysis | Bot #4 | `experience_analysis.md` |
| Feel eval (≤10 screenshots) | Bots #1,3,4 | `feel_eval.md` |
| Moment quality eval | Bots #1,4 | `moment_quality.md` |
| Emotional arc eval | Bots #1,4 | `emotional_arc.md` |
| Juice & feedback eval | Bots #1,3,4 | `juice_eval.md` |
| Spatial clarity eval | Bots #1,3,4 | `spatial_clarity.md` |
| Audio atmosphere deep | Bot #6 (audio) | `audio_atmosphere_deep.md` |
| Typography & identity | Bots #1,3,4 | `typography_identity.md` |
| Onboarding clarity | Bots #1,4 | `onboarding_clarity.md` |
| Optimize deep passes (2-7) | Optimize scan | `optimize_eval.md` |
| Seed variance (experience) | Bot #7 | `experience_seed_variance.md` |
| Seed variance (FH) | Bot #8 | `seed_variance_eval.md` |
| Tutorial eval | Bot #9 | `tutorial_eval.md` |
| Economy eval | Bot #5 | `economy_eval.md` |
| Domain eval analysis | Bot #6 | `domain_eval.md` |

**Optional (slow):**
- Stryker: `dotnet tool restore && dotnet stryker`
- SSIM regression (needs baselines):
  `python scripts/tools/compare_screenshots.py --current reports/screenshots/ --baseline reports/baselines/full/ --metric ssim`

### Step 3 — Coverage Gap Analysis

Same as FH-3 but broader scope: include deep systems, tutorial, stress coverage.

### Step 4 — Compile Unified Problem List

Merge ALL outputs using shared severity mapping (see below).
Write `reports/audit/<iter>/unified_problems.md`.

### Step 4b — Targeted Issue Verification

Same as FH-4b (see First-Hour Mode section). Run both headless and visual modes
of the verification bot to confirm every issue. Only fix CONFIRMED issues in Step 5.

### Step 5 — Fix Pass

Shared fix rules (see below). Additionally for full mode:
- Coverage gaps: add bot phases to `test_deep_systems_v0.gd`, rubric dimensions,
  optimize patterns.

### Step 6 — Re-Evaluate

Re-run ONLY affected evals:
- SimCore logic → experience + FH + deep systems bots
- UI/bridge → experience bot (visual) + screenshots
- Economy → stress bot
- Tutorial → tutorial bot (3 seeds)
- Code quality → optimize scan

Write `reports/audit/<iter>/fix_delta.md`.

### Step 7 — Coverage Evolution

For unfilled gaps: write proposals, present to user, implement approved.
Write `reports/audit/<iter>/eval_evolution.md`.

### Step 8 — Iterate

Loop if progress, cap at 3. UX-only fixes → skip deep systems + stress re-runs.

### Step 9 — Final Report

Write `reports/audit/<iter>/audit_report.md` using full report template (see below).

### Step 10 — Self-Improvement

Reflect on process, tools, game systemic issues.
Write `reports/audit/<iter>/self_improvement.md`.
Append to journal (see below).

---

## Shared: Problem Format

```
PROBLEM #N
  Source:       [experience | first-hour | deep-systems | feel | domain-eval |
                 seed-variance | tutorial | economy | optimize | coverage-gap | test-suite |
                 moments | emotional-arc | juice | spatial | audio-deep | typography | onboarding]
  Severity:     [critical | major | minor | suggestion]
  Confidence:   [high | medium | low]
  Tag:          [BUG | UX | POLISH | GAP | SUPPRESSED | UNWIRED | STABILITY | PERF | ARCH | FEEL]
  Domain:       [combat | economy | exploration | tutorial | haven | fleet | narrative |
                 missions | security | FO | pacing | grind | UI | diplomacy | dread |
                 moments | juice | audio | navigation | typography | onboarding | emotional-arc]
  Issue:        One sentence
  Evidence:     Report + line/screenshot
  Prescription: Actionable fix
  Auto-fixable: [yes | no | needs-design]
  Files:        [list]
```

## Shared: Severity Mapping

| Source | Condition | → Severity |
|--------|----------|-----------|
| C# test failure | any | → critical |
| Bot ASSERT_FAIL (hard) | any | → critical |
| Seed FAIL | any seed | → critical |
| Semgrep violation | any | → critical |
| Experience bot | CRITICAL issue | → critical |
| Optimize scan | CRITICAL finding | → critical |
| Economy eval | PRICE_COLLAPSE / ECONOMY_STALL | → critical |
| Bot SCRIPT_ERROR in stderr | any | → critical |
| Experience bot | MAJOR issue | → major |
| First-hour eval | BUG or UX tag | → major |
| Feel eval | FAIL rating | → major |
| Coverage gap | UNCOVERED system | → major |
| Stryker survived mutant | combat/market system | → major |
| Seed variance | stdev > threshold | → major |
| Tutorial eval | phase gap > 3 | → major |
| Deep systems | domain warn rate > 5% | → major |
| Experience bot | MINOR issue | → minor |
| Feel eval | NEEDS_WORK rating | → minor |
| Coverage gap | UNIT_ONLY system | → minor |
| Tutorial eval | phase gap 1-3 | → minor |
| Optimize scan | WARNING | → minor |
| Optimize scan | SUGGESTION | → suggestion |
| Any source | OPINION tag | → suggestion |
| Moment eval | moment ABSENT | → critical |
| Moment eval | moment FLAT (score 1-2) | → major |
| Moment eval | moment RUSHED | → major |
| Emotional arc | dead zone > 50 decisions | → critical |
| Emotional arc | plateau > 100 decisions | → major |
| Emotional arc | rush (3+ HIGH in 30 decisions) | → warning |
| Juice eval | SILENT_ACTION on core action | → major |
| Juice eval | score < 2 on any category | → major |
| Juice eval | DELAYED_FEEDBACK | → minor |
| Spatial eval | LOST_PLAYER in any context | → critical |
| Spatial eval | score < 2 on any context | → major |
| Spatial eval | INVISIBLE_LANES | → major |
| Audio deep eval | emotional_match < 2 | → major |
| Audio deep eval | score < 2 on any category | → minor |
| Typography eval | FONT_CHAOS or DEVELOPER_UI | → major |
| Typography eval | COLOR_CLASH | → minor |
| Typography eval | score < 2 on any category | → minor |
| Onboarding eval | DEAD_END or SYSTEM_DUMP | → critical |
| Onboarding eval | TUTORIAL_POPUP | → major |
| Onboarding eval | INVISIBLE_CONTROLS | → major |
| Onboarding eval | score < 2 on any category | → major |
| Onboarding eval | commandment violated | → major |
| FO writing eval | VOICE_DRIFT or ARCHETYPE_CLONE | → major |
| FO writing eval | ROBO_SPEAK | → major |
| FO writing eval | INFO_VACUUM | → minor |
| FO writing eval | WALL_OF_TEXT | → minor |
| FO writing eval | score < 2 on any axis | → major |
| Experience bot | COGNITIVE_LOAD: SYSTEM_DUMP (>2 systems/dock) | → critical |
| Experience bot | COGNITIVE_LOAD: TAB_OVERLOAD (>5 tabs first dock) | → major |
| Experience bot | COGNITIVE_LOAD: WALL_OF_TEXT (>80 word FO msg) | → minor |
| Experience bot | COGNITIVE_LOAD: INFORMATION_DESERT (200+ decisions no new system) | → major |
| Experience bot | DEAD_END: TRAP_STATE (credits<buy, cargo=0, no missions) | → critical |
| Experience bot | DEAD_END: action_reversals > 3 | → major |
| Experience bot | RETENTION: first_profit > 80 decisions | → major |
| Experience bot | RETENTION: core_loop > 100 decisions | → major |
| Experience bot | RETENTION: declining_action_rate | → major |
| Experience bot | PACING_RHYTHM: monotone intervals (CoV < 0.2) | → major |
| Experience bot | PACING_RHYTHM: event clustering (3+ HIGH in 30 decisions) | → minor |
| Experience bot | VALENCE_ARC: zero crossings (monotone valence) | → major |
| Experience bot | VALENCE_ARC: zero catharsis events | → major |
| Experience bot | VALENCE_ARC: zero wonder moments | → minor |
| Experience bot | COMPETENCE: margin regression (late < early) | → major |
| Experience bot | COMPETENCE: zero milestone acknowledgments | → minor |
| Economy eval | money_velocity < 0.001 | → major |
| Economy eval | inflation_rate > 20% or < -20% | → major |
| Economy eval | route_viability < 5% | → major |
| Economy eval | price_convergence_cov < 0.02 (dead market) | → major |

Dedup: same file + issue from multiple evals → merge, keep highest severity.

### Experience Bot Threshold Reference

The experience bot uses these thresholds for automatic issue detection:

| Dimension | CRITICAL threshold | MAJOR threshold | MINOR threshold |
|-----------|-------------------|-----------------|-----------------|
| COMBAT | 0 kills after 10+ combats | — | — |
| PACING | max_gap > 100 decisions | entropy < 0.8, streak > 100 | — |
| FLEET | — | 0 weapons after combat, 0 modules after 720 decisions | 0 techs after 720 decisions |
| MISSIONS | — | 0 available after 720 decisions | — |
| FO | — | max_silence > 200 decisions | — |
| ECONOMY | — | sink/faucet = 0.00, curve = EXPONENTIAL | — |
| GRIND | — | grind_score > 0.5, route_repeat > 100 | — |
| SECURITY | — | — | 0 threat bands |
| HAVEN | — | — | not discovered after 15+ nodes |
| NARRATIVE | — | — | 0 NPCs, revelation stage 0, 0 knowledge |
| MARKET_INTEL | — | — | 0 supply shocks |
| COMBAT_DEPTH | — | — | doctrine not set after combats |

## Shared: Fix Rules

- **Auto-fixable** (Confidence: high, Tag: BUG/UX/SUPPRESSED): apply fix
- **Verify build+tests every 5-10 fixes**
- **Needs-design** (Tag: GAP/OPINION/UNWIRED): present 2-3 approaches to user
- **NEVER auto-fix**: OPINION tags, low confidence, SimCore tick logic (golden hashes)
- **SUPPRESSED fixes** are highest-leverage: remove `visible = false`, add F-key toggle,
  wire bridge method to existing UI element
- Track in `reports/audit/<iter>/fix_log.md`

## Shared: Journal Format

Append to `reports/audit/JOURNAL.md` after every audit run:

```markdown
## <iteration> — <date> — SHA <sha> — mode: <full|first-hour|quick>

**Score**: N found, N fixed, N deferred
**Coverage**: Bridge N%→N%, Systems N%→N%
**Key findings**: [1-3 sentences]
**Key fixes**: [1-3 sentences]
**Carried forward**: [deferred items for next audit — ANY mode should pick these up]
```

When reading the journal at audit start, read ALL entries regardless of mode.
Items carried forward from a `first-hour` audit are valid work for a `full` audit
and vice versa.

## Shared: Report Templates

### First-Hour Report

```markdown
# First-Hour Audit — <date> — <iteration>

## Snapshot
- Git SHA: <sha> | C# Tests: N/N | Bots: EXP=P/F, FH=P/F, EVAL=N/5
- Optimize criticals: N | Semgrep: P/F | GDScript lint: N warnings

## Experience Scorecard (ACTIVE dimensions only)
| # | Dimension | Key Metric | Seed Class | Verdict |
|---|-----------|-----------|-----------|---------|
| 1 | Economy | growth=X%, curve=Y, sink_faucet=X | UNIVERSAL/VARIABLE | CRITICAL/MAJOR/PASS |
| 2 | Grind | score=X, route_repeat=N | ... | ... |
| 3 | Cognitive Load | tabs_first_dock=N, max_sys/dock=N | ... | ... |
| 4 | Valence Arc | crossings=N, catharsis=N, wonder=N | ... | ... |
| 5 | FO | max_silence=N, lines=N | ... | ... |
| 6 | Competence | early_margin=N, late_margin=N (delta%) | ... | ... |
| 7 | Pacing | max_gap=N, entropy=X | ... | ... |
| 8 | Pacing Rhythm | density/100=N, longest_streak=N | ... | ... |
| 9 | Combat | kills=N, hull_min=X% | ... | ... |
| 10 | Combat Depth | heat=X, doctrine=T/F | ... | ... |
| 11 | Exploration | visited=X%, backtrack=X% | ... | ... |
| 12 | Narrative | revelation=N, NPCs=N | ... | ... |
| 13 | Economy Depth | velocity=X, CoV=X, inflation=X% | ... | ... |
| 14 | Disclosure | systems=N at decision D | ... | ... |
| 15 | Progression | milestones=N, avg_ppt=N | ... | ... |

STABLE dimensions (suppressed — all PASS): Security, Haven, Dead-End, Missions, Fleet, Retention

## Goal Scores
| Goal | Score | Summary |
|------|-------|---------|
| 1. Galaxy Alive | N/5 | ... |
| 2. Actions Teach | N/5 | ... |
| 3. FO Is Person | N/5 | ... |
| 4. Profit = Discovery | N/5 | ... |
| 5. Promise of Depth | N/5 | ... |

**EA Readiness**: BLOCKED | NOT_READY | CONDITIONAL | READY

## Code Bugs (≤15, force-ranked)
(from unified_problems.md Section 1 — auto-fixable issues, ranked by severity + universality)

## Design Decisions (backlog)
(from unified_problems.md Section 2 — needs-design items, NOT auto-fixed)

## Issues Fixed
(from fix_log.md)

## Seed Stability
| Seed | Verdict | Key Variance |
|------|---------|-------------|
| 42 | PASS | baseline |

## Feel Summary
(pass/needs_work/fail counts per dimension)

## Moment Quality (5 moments)
| Moment | Timing | Impact | Resonance | Follow-through | Overall |
|--------|--------|--------|-----------|----------------|---------|
| Heist (first profit) | N/5 | N/5 | N/5 | N/5 | N/5 |
| Companion (FO reveal) | N/5 | N/5 | N/5 | N/5 | N/5 |
| Danger (first combat) | N/5 | N/5 | N/5 | N/5 | N/5 |
| Power (first upgrade) | N/5 | N/5 | N/5 | N/5 | N/5 |
| Promise (depth glimpse) | N/5 | N/5 | N/5 | N/5 | N/5 |

## Emotional Arc
| Metric | Value | Verdict |
|--------|-------|---------|
| Dead zones (>50 decisions) | N | PASS/FAIL |
| Plateaus (>100 decisions) | N | PASS/WARN |
| Crescendo quality | N/5 | |
| Rhythm quality | N/5 | |
| Valley quality | N/5 | |

## Feel Layer Dashboard
| Dimension | Score | Key Finding |
|-----------|-------|-------------|
| Juice & Feedback | N/5 | ... |
| Spatial Clarity | N/5 | ... |
| Audio Atmosphere | N/5 or SKIP | ... |
| Typography & Identity | N/5 | ... |
| Onboarding Clarity | N/5 | ... |
| FO Writing Quality | N/5 | ... |

## 7 Commandments Check
| Commandment | Verdict | Evidence |
|-------------|---------|----------|
| Min 1 = identity | PASS/FAIL | ... |
| Core loop in 10 min | PASS/FAIL | ... |
| First trade = heist | PASS/FAIL | ... |
| Front-load upgrades | PASS/FAIL | ... |
| One system per encounter | PASS/FAIL | ... |
| Direction AND permission | PASS/FAIL | ... |
| Never lost for 30s | PASS/FAIL | ... |

## AAA Quality Gate
| Reference Game | Pattern | STE Delivers? |
|----------------|---------|---------------|
| Factorio | Pain before relief | PASS/FAIL |
| FTL | 2-min loop density | PASS/FAIL |
| Outer Wilds | Knowledge gates | PASS/FAIL |
| Subnautica | World-first | PASS/FAIL |
| Homeworld | Voice restraint | PASS/FAIL |
| Elite Dangerous | Fast return visits | PASS/FAIL |

## Coverage Gaps
(top gaps from coverage analysis)

## Manual Playtest Checklist
(pre-filled from goal evaluation)

## Next Steps
```

### Full Report

Same as first-hour plus:
- Deep Systems (pass/warn across 15+ domains)
- Tutorial Coverage (phase %, FO rotation)
- Economy Stress (stability verdict)
- Optimize findings (criticals/warnings)
- Coverage improvements made
- Eval evolution proposals

---

## Metric Targets

Use these as "good enough" baselines — audit should flag when metrics fall below:

| Metric | Target | Source |
|--------|--------|--------|
| Goal scores (each) | ≥ 4/5 | First-hour rubric |
| Goal scores (average) | ≥ 3.5/5 | First-hour rubric |
| Hard assertions | 100% pass | FH proof bot |
| C# tests | 100% pass | Test suite |
| Experience CRITICAL issues | 0 | Experience bot |
| Experience MAJOR issues | ≤ 3 | Experience bot |
| Seed pass rate | 5/5 | Multi-seed sweep |
| Feel FAIL screenshots | 0 | Feel eval |
| Bridge coverage | ≥ 40% | Coverage gap |
| Optimize criticals | 0 | Optimize scan |
| Bot SCRIPT_ERRORs | 0 | stderr |
| EA Readiness | CONDITIONAL or READY | Goal eval |
| Moment scores (each) | ≥ 3/5 | Moment quality eval |
| Moments ABSENT | 0 | Moment quality eval |
| Dead zones (>50 decisions) | 0 | Emotional arc |
| Crescendo quality | ≥ 3/5 | Emotional arc |
| Juice score (avg) | ≥ 3/5 | Juice eval |
| Silent core actions | 0 | Juice eval |
| Spatial clarity (avg) | ≥ 3/5 | Spatial eval |
| LOST_PLAYER flags | 0 | Spatial eval |
| Audio emotional match | ≥ 3/5 | Audio deep eval |
| Typography score (avg) | ≥ 3/5 | Typography eval |
| FONT_CHAOS/DEVELOPER_UI | 0 | Typography eval |
| Onboarding score (avg) | ≥ 3/5 | Onboarding eval |
| DEAD_END/SYSTEM_DUMP | 0 | Onboarding eval |
| Commandments passed | 7/7 | Onboarding eval |
| AAA reference patterns | ≥ 4/6 PASS | AAA quality gate |
| FO writing quality (avg) | ≥ 3/5 | FO writing eval |
| VOICE_DRIFT/ARCHETYPE_CLONE | 0 | FO writing eval |
| Slow-bot dead zones | ≤ fast-bot dead zones | Slow-bot comparison |

---

## Hard Invariants

- **Build after every fix** — even "safe" changes can break
- **Never auto-fix OPINION tags** — require human design judgment
- **Never modify golden hashes** without determinism tests
- **Coverage is additive** — never remove bot phases or rubric dimensions
- **One bot run, many evals** — never run the same bot twice
- **≤10 screenshots per feel eval** — diminishing returns beyond that
- **Report all failures** — never silently skip a crashed eval
- **Immutable history** — never overwrite previous iteration reports
- **Iterate with purpose** — only loop if measurable progress. 3 loops max.
- **Respect bot timeouts** — 5400 frames = 90s. Timeout is itself a finding.
- **Phase-advance-first** — set `_phase = Next` BEFORE bridge calls in bot functions
- **Bot init retry** — `GetGalaxySnapshotV0` can return empty on lock contention; retry 200-300ms
- **Check stderr after every bot** — SCRIPT_ERROR = CRITICAL finding, not ignorable noise
- **Visual hull guard** — experience bot's `_visual_hull_guard()` prevents death screen
  overlay in visual mode; if adding new visual bots, implement the same pattern
- **Cross-mode journal** — ALL audit modes read/write the same JOURNAL.md;
  carried-forward items apply to all subsequent audits regardless of mode
