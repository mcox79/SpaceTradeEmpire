# First-Hour Feel Rubric

> Companion to `first_hour_rubric.md` (which evaluates player journey goals).
> This rubric evaluates the **feel layer** — the micro-interactions, emotional
> pacing, and sensory polish that separate "it works" from "it feels right."
>
> Read this file IN FULL before evaluating. Each section corresponds to an
> LLM evaluation agent in the `/audit first-hour` flow.

---

## Section 1 — Moment Quality

The first hour has 5 design-mandated moments. Each must land with emotional weight.
A moment is not just an event — it's an event with **setup**, **payoff**, and
**acknowledgment**.

### The 5 Moments

#### 1. The Heist (First Profitable Trade)

The player discovers that geography is money. They bought low at a mining hub and
sold high at a refinery. This should feel like a personal insight, not an assigned task.

| Score | Evidence Pattern |
|-------|-----------------|
| 5 | Margin > 100 cr/unit, FO reacts after sale ("generous margin"), credit counter visibly jumps, player's second trade is self-directed (different good or route) |
| 4 | Clear profit visible, FO acknowledges trade, credit change obvious |
| 3 | Profit happens but FO is silent, or margin is modest (<50 cr/unit) |
| 2 | Profit happens but credit change barely noticeable, or margin < 20 cr/unit |
| 1 | Player loses money, or profit is invisible, or trade feels like homework |

**Timing target**: Decision 30-80 (minutes 5-15). Too early = no setup. Too late = frustration.
**Critical flag**: If first trade happens after decision 120, flag as DEAD_ZONE — player
couldn't figure out the core loop.

#### 2. The Companion (FO Personality Reveal)

The First Officer speaks for the first time. Their archetype (Analyst/Veteran/Pathfinder)
should be distinguishable from word choice alone. They react AFTER the player acts.

| Score | Evidence Pattern |
|-------|-----------------|
| 5 | FO speaks within first 3 minutes, archetype clear from dialogue, at least one line is atmospheric (not instructional), FO reacts to player action (not scripted timing) |
| 4 | FO promoted and speaks early, personality comes through, reactive timing |
| 3 | FO speaks but feels generic — could be any archetype |
| 2 | FO speaks but reads like a tooltip ("You can buy goods at stations") |
| 1 | FO silent for first 10 minutes, or no archetype distinction, or never promoted |

**Key test**: Replace FO lines with generic tooltips. If nothing is lost, score ≤ 2.
**Timing target**: First FO line within decision 20-40 (post-first-warp).

#### 3. The Danger (First Combat)

The galaxy isn't safe. First combat should create a beat of tension — will I survive? —
followed by resolution. The player should feel "I could have died" even if they won easily.

| Score | Evidence Pattern |
|-------|-----------------|
| 5 | Player takes hull damage, weapon fire has VFX + audio, enemy destruction has explosion, loot drops with visual indicator, FO reacts to combat |
| 4 | Combat resolves with visible damage feedback, enemy destroyed, some tension |
| 3 | Combat works but feels instant — one-shot kill, no buildup |
| 2 | Combat happens but no feedback (no VFX, no audio, no damage numbers) |
| 1 | No combat in first hour, or combat is broken |

**Key test**: Did the player's hull drop below 90%? If not, there was no real danger.
**Timing target**: Decision 100-200 (minutes 15-30).

#### 4. The Power (First Upgrade/Automation)

The ship changes. The player installs a module or sets up their first automation.
This should feel like a tangible improvement — the game plays differently now.

| Score | Evidence Pattern |
|-------|-----------------|
| 5 | Module installed with visual ship change, stat improvement visible, FO acknowledges, gameplay noticeably different after (faster scans, more cargo, etc.) |
| 4 | Module installed, stat change visible, ship feels improved |
| 3 | Module installed but no visual/stat feedback, or automation started but no visible effect yet |
| 2 | Upgrade available but player didn't find it, or UI too confusing to use |
| 1 | No upgrade opportunity in first hour |

**Timing target**: Decision 120-250 (minutes 20-40).

#### 5. The Promise (Depth Glimpse)

The player realizes how much more exists. Galaxy map shows vast unexplored space.
Tech tree has locked branches. Empty module slots promise future power. Faction
borders hint at politics.

| Score | Evidence Pattern |
|-------|-----------------|
| 5 | < 40% galaxy explored, 3+ locked tech branches visible, empty module slots, faction borders visible, player can name 3+ things they want to do next |
| 4 | < 50% explored, tech depth visible, some mystery remaining |
| 3 | Galaxy mostly seen but tech/modules provide depth. Some "more" feeling |
| 2 | Galaxy fully explored OR no visible locked content — nowhere to go |
| 1 | Everything visible, nothing locked, no reason to continue |

**Timing target**: Decision 200-400 (minutes 30-60).

### Moment Interaction Quality

Beyond individual moments, evaluate how moments **relate to each other**:

- **Spacing**: Moments should be 30-80 decisions apart. Clustered moments overwhelm;
  gaps between moments create boredom.
- **Escalation**: Each moment should feel bigger than the last. Heist < Companion <
  Danger < Power < Promise.
- **Callback**: Does a later moment reference an earlier one? (FO mentions the first
  trade during the upgrade moment? Combat loot enables the upgrade?)

---

## Section 2 — Juice & Feedback

"Juice" = the visual/audio/haptic feedback that makes actions feel satisfying.
Every player action should produce immediate, proportional, multi-channel feedback.

### Action Categories

#### Core Actions (CRITICAL — must have juice)

| Action | Expected Feedback | Score 5 | Score 1 |
|--------|-------------------|---------|---------|
| Buy goods | Cargo counter animates, credits decrease with number roll, purchase sound | Counter ticks up, credit roll, satisfying click | Nothing changes visibly |
| Sell goods | Credits increase prominently, profit highlighted (green text?), cha-ching | Dramatic credit jump, profit callout, audio reward | Credit counter changes but no emphasis |
| Dock at station | Transition animation, station name appears, ambient change | Smooth dock sequence, station name slides in | Instant teleport, no transition |
| Undock | Engine power-up feel, camera pulls back, controls activate | Thrust VFX, camera sweep, engine audio | Instant placement in space |
| Warp transit | Speed lines or tunnel VFX, destination loading | Dramatic transit effect, sense of distance | Instant teleport between systems |
| Take damage | Screen flash/shake, hull bar drops, warning audio | Red flash, camera shake, alarm sound | Hull number changes silently |
| Destroy enemy | Explosion VFX, debris, loot indicator | Satisfying explosion, wreckage, loot glow | Enemy disappears |
| Install module | Ship visual change, stat comparison, confirmation | Module appears on ship model, stats highlight | Menu checkbox, no feedback |

#### Secondary Actions (IMPORTANT — should have juice)

| Action | Expected Feedback |
|--------|-------------------|
| Open galaxy map | Zoom transition, system labels appear progressively |
| Accept mission | Objective marker appears, FO acknowledges, new waypoint |
| Discover anomaly | Scan pulse VFX, discovery sound, data readout animation |
| Research complete | Unlock fanfare, new capability highlighted |
| Automation starts | Program indicator appears, profit projection visible |

### Scoring Dimensions

For each action category:

- **Immediacy** (1-5): Feedback within 1 frame of action? Within 0.5s? Within 2s? Delayed? None?
- **Proportionality** (1-5): Big action = big feedback, small action = small feedback?
  Buying 1 unit vs 100 units should feel different.
- **Multi-channel** (1-5): Visual + audio + camera? Visual only? Nothing?
  Best: visual change + sound + camera motion. Worst: only a number changes.
- **Variety** (1-5): Do different actions feel different? Or does everything produce
  the same generic click sound?

### Flags

| Flag | Condition | Severity |
|------|-----------|----------|
| SILENT_ACTION | Core action with zero feedback (no visual, no audio) | MAJOR |
| DELAYED_FEEDBACK | Feedback > 1s after action | MINOR |
| DISPROPORTIONATE | Tiny action, huge effect (or vice versa) | MINOR |
| MONOTONE_FEEDBACK | All actions produce identical feedback | MINOR |
| NO_NEGATIVE_FEEDBACK | Damage/loss has no distinct feeling | MAJOR |

---

## Section 3 — Spatial Navigation Clarity

In a space game, the player must always know: Where am I? Where can I go?
What's near me? What's dangerous?

### Contexts

#### In-Flight (System View)

| Criterion | Score 5 | Score 1 |
|-----------|---------|---------|
| Current system identity | System name prominent, star type visible, unique visual character | No system identifier, all systems look identical |
| Station locations | Labeled, distinct models, approachable | Unlabeled dots, indistinguishable from debris |
| Lane gates | Glowing, labeled with destination, visible from distance | Invisible until close, no destination info |
| Player ship | Distinct from NPCs, heading indicator, velocity feel | Same model as NPCs, no heading cue |
| NPC ships | Faction colors, movement patterns visible | All same color, static or invisible |

#### Galaxy Map

| Criterion | Score 5 | Score 1 |
|-----------|---------|---------|
| Player position | Clearly marked, stands out from all nodes | Ambiguous, hard to find |
| Visited vs unvisited | Strong visual distinction (color, opacity, icon) | All nodes look the same |
| Faction territory | Color-coded borders or overlays, faction names | No faction visualization |
| Route planning | Can trace lanes, see connections, plan trips | Spaghetti of lines, no clarity |
| Threat indicators | Warfront zones highlighted, danger levels shown | No threat info on map |

#### Combat

| Criterion | Score 5 | Score 1 |
|-----------|---------|---------|
| Friend vs foe | Enemy highlighted red/orange, allies green/blue | All ships same color |
| Engaged target | Clear targeting indicator, distance shown | No indication of which enemy you're fighting |
| Escape route | Nearest gate/station visible during combat | No exit information |
| Damage source | Direction indicator when hit from offscreen | Hit comes from nowhere |

### Flags

| Flag | Condition | Severity |
|------|-----------|----------|
| LOST_PLAYER | No current-location indicator in any view | CRITICAL |
| INVISIBLE_LANES | Lane gates not visible from standard camera distance | MAJOR |
| AMBUSH_UNFAIR | Enemy engaged player before being visible on screen | MAJOR |
| IDENTICAL_SYSTEMS | Two systems with no visual distinguishing features | MINOR |
| NO_HEADING | Player ship heading not visually apparent | MINOR |

---

## Section 4 — Audio Atmosphere

Audio in STE follows the Homeworld x FTL x Sunless Sea principle:
**restraint with impact**. Silence is a tool, not a bug.

### Phase-Audio Alignment

| Game Phase | Expected Audio State | Design Intent |
|------------|---------------------|---------------|
| Boot / intro | Sparse, single notes in silence | Contemplation, mystery |
| First dock | Gentle ambient, station hum | Safety, curiosity |
| First flight | Subtle melodic motif, engine undertone | Freedom, vastness |
| Normal trade run | Calm ambient, occasional musical phrase | Routine, competence |
| Approach to new system | Building tension, new tonal color | Anticipation |
| Combat engagement | Percussion, urgency, layered intensity | Adrenaline, stakes |
| Post-combat | Silence → gradual return to calm | Relief, processing |
| Deep space / fracture | Dissonant undertones, wrong notes | Unease, alienness |
| Discovery | Unique audio signature, revelatory tone | Wonder, reward |
| Haven arrival | Warm, resonant, home-feeling chord | Safety, accomplishment |

### Scoring Dimensions

- **Restraint** (1-5): Does the game know when to be quiet? Is silence used for effect?
  5 = intentional quiet moments amplify the loud ones. 1 = constant music with no dynamic range.
- **Impact** (1-5): Do peak moments (combat, discovery, first profit) have peak audio?
  5 = combat music makes your heart rate increase. 1 = same music everywhere.
- **Layering** (1-5): Multiple simultaneous audio streams creating depth?
  5 = music + ambient + SFX + engine hum. 1 = single music track, nothing else.
- **Transitions** (1-5): Smooth crossfades between states?
  5 = seamless mood shifts. 1 = hard cuts between tracks.
- **Emotional match** (1-5): Does audio mood align with gameplay mood?
  5 = you could close your eyes and know what's happening. 1 = random music selection.

### Flags

| Flag | Condition | Severity |
|------|-----------|----------|
| SILENT_COMBAT | No audio during combat engagement | MAJOR |
| WRONG_MOOD | Calm music during combat or combat music at dock | MAJOR |
| NO_TRANSITION | Hard audio cut between states (no crossfade) | MINOR |
| AUDIO_MONOTONE | Same track/mood for > 5 minutes without variation | MINOR |
| MISSING_SFX | Core action (dock, buy, sell, shoot) has no sound effect | MINOR |

---

## Section 5 — Typography & Visual Identity

AAA games are recognizable from a single screenshot. The visual language should be
consistent, readable, and distinctive.

### Font Hierarchy

| Level | Purpose | Expected Treatment |
|-------|---------|-------------------|
| Title | System names, panel headers, station names | Largest, boldest, distinctive typeface |
| Body | Descriptions, dialogue, mission text | Clear, readable, moderate size |
| Data | Prices, quantities, coordinates, stats | Monospace or tabular, right-aligned numbers |
| Caption | Tooltips, secondary info, timestamps | Smallest, subdued color |
| Alert | Warnings, critical notifications | Contrasting color (red/orange), possibly animated |

### Color Palette Evaluation

| Aspect | Score 5 | Score 1 |
|--------|---------|---------|
| Consistency | All panels share same background, accent, text colors | Each panel has different random colors |
| Faction colors | Each faction has distinct, memorable color (used on stations, ships, map) | All factions same color or clashing colors |
| Semantic colors | Green = positive/profit, Red = negative/danger, Blue = neutral/info | Colors used randomly, green for danger |
| Contrast | All text readable against all backgrounds, including in bright/dark scenes | Text lost against similar-colored backgrounds |
| Restraint | 3-4 primary colors, used consistently | Rainbow of colors, no hierarchy |

### Visual Identity Check

| Question | PASS | FAIL |
|----------|------|------|
| Could you identify this game from a single screenshot? | Distinctive art style, UI, color palette | Generic space game look |
| Do all UI panels feel like the same game? | Consistent styling, spacing, typography | Mix of styles, some panels look like debug tools |
| Are numbers easy to compare? | Tabular/monospace alignment, decimal alignment | Proportional font, numbers jump around |
| Is there visual hierarchy in dense panels? | Headers > sections > items, clear grouping | Wall of text, no visual structure |
| Do icons/symbols have consistent style? | Same line weight, color treatment, size | Mix of pixel art, vector, text symbols |

### Flags

| Flag | Condition | Severity |
|------|-----------|----------|
| FONT_CHAOS | 3+ different font families with no hierarchy | MAJOR |
| DEVELOPER_UI | Panel looks like a debug tool (mono font, no styling, raw data dump) | MAJOR |
| COLOR_CLASH | Two clashing hues in the same panel (e.g., lime green + magenta) | MINOR |
| FACTION_IDENTICAL | Two factions with identical visual treatment | MINOR |
| NUMBER_JUMBLE | Prices/quantities in proportional font, misaligned | MINOR |
| LOW_CONTRAST | Text color within 30% luminance of background | MINOR |

---

## Section 6 — Onboarding Clarity

STE doesn't use tutorials. The world teaches. But "no tutorials" doesn't mean
"no guidance" — it means the guidance is environmental and consequential.

### The 7 First-Hour Commandments

Evaluate each as PASS/FAIL with evidence:

1. **Minute 1 establishes identity**: The world, not a menu. Boot screenshot should
   show space, ships, a star — not a settings screen or loading bar.

2. **Core loop in 10 minutes**: Player has completed fly → dock → buy → fly → sell
   within decision 60-80. If the loop takes longer, something is blocking understanding.

3. **First trade = heist, not homework**: The margin is so large it's impossible to miss.
   If margin < 50 cr/unit, the design has failed — the player can't feel the insight.

4. **Front-load upgrades, not information**: A tangible ship improvement (module, weapon)
   should appear before minute 20. Information panels (tech tree, knowledge graph)
   come later.

5. **One system per encounter**: Each dock introduces ONE new concept. If the first dock
   shows trade + missions + fleet + automation tabs, score FAIL — information overload.
   Progressive disclosure should hide advanced tabs early.

6. **Direction AND permission**: There's always a visible "suggested path" (nearest
   profitable station, FO hint, mission marker) but the player can ignore it freely.
   No invisible walls, no forced corridors.

7. **Never lost for 30 seconds**: At every point in the first hour, the player should
   have at least one clear action available. If they're floating in space with no
   visible station, no map prompt, and no FO hint, they're lost.

### Control Discoverability

| Control | Discoverability Score |
|---------|----------------------|
| Movement (WASD) | 5 = HUD shows WASD on first undock. 1 = no hint anywhere |
| Dock (E) | 5 = prompt appears on station approach. 1 = player must guess |
| Galaxy map (Tab/M) | 5 = button visible in HUD, or FO mentions it. 1 = hidden |
| Menu panels (J/K/L) | 5 = tabs visible with keybind hints. 1 = undiscoverable |
| Combat (weapons) | 5 = auto-fire or clear "click to shoot". 1 = must read manual |

### Progressive Disclosure Timeline

Check that UI elements appear in the right order:

| Decision Range | Should Be Visible | Should Be Hidden |
|----------------|-------------------|-----------------|
| 0-30 | HUD (credits, hull, cargo), navigation, dock prompt | Missions, fleet, automation, tech, modules |
| 30-80 | Trade panel, FO panel | Automation, tech tree, knowledge graph |
| 80-150 | Missions tab, combat HUD (when engaged) | Advanced fleet, research, haven |
| 150-300 | Modules/fitting, automation (after 3 trades) | Advanced research, deep tech |
| 300+ | Full UI unlocked progressively | Nothing hidden that player has earned |

### Flags

| Flag | Condition | Severity |
|------|-----------|----------|
| DEAD_END | Player has no clear next action for > 30 decisions | CRITICAL |
| SYSTEM_DUMP | 2+ new game systems introduced at same dock | CRITICAL |
| TUTORIAL_POPUP | Explicit instruction text breaking world immersion | MAJOR |
| INVISIBLE_CONTROLS | Key control (dock, map, menu) undiscoverable | MAJOR |
| PREMATURE_DISCLOSURE | Advanced tabs visible before player needs them | MINOR |
| NO_FO_GUIDANCE | FO silent when player appears stuck (> 60 decisions, no action) | MINOR |

---

## Section 7 — Emotional Arc Reference Curves

Use these reference curves when evaluating the emotional arc of a first-hour playthrough.

### Ideal Curve Shape

```
Intensity
  ^
5 |              *           *
4 |         *         *           *
3 |    *         .         .
2 |  .    .         .    .    .
1 | .                              .
  +-----------------------------------> Decisions
  0   50   100  150  200  250  300  350

  * = HIGH moment (heist, combat, upgrade, discovery)
  . = Valley (travel, contemplation, routine trade)
```

- **Valleys are not zero**: They're calm, not empty. Travel with scenery, FO
  conversation, price comparison. Score 2 on the intensity scale, not 0.
- **Peaks escalate**: Each peak slightly higher or more complex than the last.
- **The 50-decision rule**: No gap between events > 50 decisions without at least
  a MEDIUM event (FO dialogue, new station dock, price discovery).

### Anti-Patterns

| Pattern | Problem | Fix |
|---------|---------|-----|
| Flat line (all 2s) | Nothing exciting happens | Add combat encounter, FO react |
| Front-loaded (peaks at 0-50, then flat) | All excitement in first 5 min | Spread moments across 60 min |
| Rear-loaded (flat until 250+) | Player bored before anything happens | Move first profit to decision 50 |
| Chaotic (random peaks/valleys) | No rhythm, exhausting | Space events by 30-50 decisions |
| Single spike (one peak, rest flat) | One-trick pony feel | Ensure 5 distinct moments |

---

## Output Format

Each feel evaluation agent must return:

1. **DIMENSION SCORES** — table of scored dimensions with evidence
2. **FLAGS** — any raised flags with screenshot/evidence references
3. **AAA COMPARISON** — how this aspect compares to reference games
4. **TOP 3 STRENGTHS** — what's already working well
5. **TOP 3 GAPS** — highest-leverage improvements
6. **PRESCRIPTIONS** — actionable fixes using the tag format from first_hour_rubric.md
