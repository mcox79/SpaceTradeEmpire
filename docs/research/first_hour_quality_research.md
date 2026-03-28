# First-Hour Quality Research: Exhaustive Catalog

This document compiles industry research, GDC findings, and AAA studio practices for what matters in the first hour of a video game, with emphasis on space trading/strategy games. Intended as the foundation for an automated first-hour quality evaluation system.

---

## 1. First-Hour Player Retention Research

### 1.1 Critical Statistics

| Metric | Value | Source |
|--------|-------|--------|
| Players returning after first session (F2P) | < 40% | GameAnalytics (Marc Robinson, GDC 2013) |
| Players lost to weak first session | 60-80% leave permanently | deltaDNA analysis of 80+ games |
| First impression formation time | 50-500 milliseconds | UX research (applied to games) |
| Time to First Fun (TTFF) target | 30-60 seconds | Industry consensus |
| Warframe first-hour retention | 80% kept playing after 1 hour | Celia Hodent, GDC 2016 |
| FTUE loss rate (successful F2P) | ~20% lost during onboarding | Celia Hodent, GDC 2016 |
| Tutorial completion rate (poor design) | As low as 13% | GameAnalytics funnel data |
| Tutorial completion rate (good design) | ~50% finish, 85% past first step | GameAnalytics onboarding funnel |
| Player engagement deadline | 15 minutes or risk losing forever | Bruce Shelley (Age of Empires designer) |
| Hades: time to first action | ~15 seconds | Industry observation |
| Steam median refund rate | 9.5% (average 10.8%) | GameDiscoverCo 2025 data |
| Refund rate, 90%+ review score | 7.2-7.4% | GameDiscoverCo |
| Refund rate, <80% review score | >11% | GameDiscoverCo |
| Refund rate, 50+ hour avg playtime | 7.4% | GameDiscoverCo |

### 1.2 Why Players Quit: The First 10 Minutes

**Top 5 reasons (deltaDNA, 80+ game analysis):**
1. **Monetization too harsh/early** -- 70% of analyzed games had this problem
2. **Difficulty/outcomes imbalance** -- 45% of games
3. **Resources depleted too quickly** -- 34% of games
4. **Weak onboarding** -- 31% of games
5. **Insufficient rewards/incentives** -- 28% of games

**Additional churn factors (GameAnalytics, 16 reasons):**
- Poor game introduction (title screen, loading, first level)
- Sessions too long (mobile needs 3min, PC needs 15-30min viable sessions)
- Slow tutorials forcing experienced players through them
- High early difficulty barrier with unresponsive controls
- Cognitive overload: confused players close the game silently
- Toxicity exposure: 320% more likely to churn if abused in first session (League of Legends study, 2014)

### 1.3 The Steam Refund Window Pressure

Steam's 2-hour refund policy creates a hard design constraint:
- Players can refund with <2 hours playtime and <14 days ownership
- Games must deliver their core value proposition within 2 hours
- Short games (<2hr completion) face existential refund risk
- Higher-quality games (better reviews) naturally have lower refund rates
- **Design implication**: The first 2 hours must convince the player the game is worth keeping

### 1.4 Critical Hook Moments Timeline

| Window | What Must Happen | Design Goal |
|--------|-----------------|-------------|
| 0-15 seconds | Player has control, something visually interesting | Overcome inertia of starting a new game |
| 15-60 seconds | First dopamine hit (TTFF) | Deliver a taste of the core fun |
| 1-3 minutes | Core loop demonstrated once | Player understands "what I do in this game" |
| 3-5 minutes | First meaningful choice | Player feels agency, not railroaded |
| 5-15 minutes | First success + reward | Competence established, positive reinforcement |
| 15-30 minutes | Second system introduced | Depth revealed, curiosity about what's next |
| 30-60 minutes | First narrative hook or "wow" moment | Emotional investment, Steam refund window defense |
| 60-120 minutes | First "big purchase" or system mastery | Player feels committed, refund unlikely |

### 1.5 Retention Benchmarks by Stage

| Metric | Industry Standard | Updated Standard (2025) | Top Performers |
|--------|------------------|------------------------|----------------|
| D1 Retention | 40% | 50%+ | 60%+ |
| D7 Retention | 20% | 25%+ | 35%+ |
| D30 Retention | 10% | 12%+ | 20%+ |

**Genre note**: Premium/paid games measure retention through affinity with IP and studio brand loyalty rather than monetization optimization. D30 retention strongly predicts D365 retention.

**Key D1 insight**: D1 measures the strength of the FTUE and sets the upper limit for ALL downstream metrics. If 80% vanish on Day 1, Day 7 cannot mathematically exceed 20%.

---

## 2. Space Game First-Hour Patterns

### 2.1 Comparative Analysis: Space Game Onboarding

#### Freelancer (2003) -- The Gold Standard for Onboarding
- **Opening**: Cinematic showing Freeport 7 destruction, player survives in escape pod
- **First mission**: Simple delivery to Planet Pittsburgh (tutorial disguised as story mission)
- **Combat tutorial**: Allies present, enemies don't target player initially
- **First loot**: Info window teaches tractor beam when first loot appears
- **Key insight**: Compelling narrative hook (mysterious Order attack) delivered DURING tutorial, not before or after
- **Time to learn all basics**: ~30 minutes (takeoff, landing, combat, communication, purchasing, trading)
- **Why it worked**: Story and tutorial are inseparable; you're learning mechanics while discovering a mystery

#### Elite Dangerous -- Cautionary Example
- **Tutorial**: Standalone repeatable missions for flight, supercruise, docking, combat
- **First hour problems**: Docking is notoriously difficult; "literally a book dedicated to it"
- **Community consensus**: "Pretty poor job in-game about explaining much of what is going on"
- **Critical gap**: Fuel management and engineering not covered in tutorials
- **What it does right**: Tutorial missions are optional and replayable
- **Lesson**: Separating tutorial from game world creates disengagement; difficulty spikes on essential mechanics (docking) cause immediate frustration

#### X4: Foundations -- Complexity Overload
- **Tutorial**: Teaches docking but poorly guides safe landing procedures
- **Early game**: Money-making through pirating, mining, trading, missions, or scavenging
- **Mid-game transition**: Automation and passive income setup
- **Key insight**: Allows 200+ hours before requiring station building (respects player pace)
- **Problem**: "Cutting players loose can be overwhelming" after tutorial
- **Lesson**: Freedom without guidance creates paralysis in complex games

#### Starsector -- Gradual Disclosure
- **Opening**: Helpful tutorials, basic combat scenarios, then open world
- **First contact**: Transmission from Hegemony requesting assistance (immediate context)
- **Hook mechanism**: "Either grips you after the first few hours or pushes you away at the sheer openness"
- **Lesson**: Tutorial + immediate faction contact provides anchor in open world

#### FTL: Faster Than Light -- Immediate Pressure
- **First run**: Within seconds, player faces first decision (which sector to jump to)
- **Hook**: Roguelike urgency (rebel fleet advancing) creates immediate stakes
- **Progression**: Unlocking new ships provides long-term motivation
- **Lesson**: Constrained choice space (small ship, few options) teaches complex systems gradually through failure

#### Everspace -- Roguelike with Persistence
- **Structure**: Gather fuel to jump, collect credits/resources, craft and upgrade
- **Hook**: Mix of persistent progression (survive run) and temporary progression (within run)
- **Lesson**: Permanent upgrades between runs make failure feel productive

### 2.2 Complex Systems Game Onboarding Patterns

#### Factorio -- The Industry Reference
- **Developer-identified problem**: Brand new players need 30-45 minutes to reach automation (the actual game)
- **Information architecture issues**: Three competing information channels (Objective window, console chat, TAB bubbles) cause information flooding
- **Design goal**: "Create an immediately gripping environment that better sets up the Factorio feel"
- **Early progression**: Mine by hand -> craft smelters -> make plates -> build conveyor belts and inserters -> automation begins
- **Clear macro-objective**: Build a rocket and escape the planet (long-term goal from minute one)
- **Lesson**: Pain before relief works IF the player can see the relief coming (Factorio principle)

#### Stellaris / Paradox Games
- **Problem**: "Such a deep game can seem impenetrable the first time you hit New Game"
- **Tutorial**: Teaches basics but "the game you start is going to look wildly different in a few hours"
- **Approach**: Tutorial bot provides advice, learning happens through play
- **Lesson**: For games that transform over time, tutorials can only cover foundations; the first hour must be engaging even without full system mastery

#### Rimworld -- Scenario-Based Onboarding
- **Approach**: Different starting scenarios for different player skill levels
- **Crashlanded**: Recommended for beginners (3 colonists, abundance of resources, moderate tech)
- **Immediate priorities taught through urgency**: Food -> Defenses -> Happiness -> Storage
- **Lesson**: Starting scenario design IS the tutorial for sandbox games

### 2.3 Common Patterns Across Successful Space/Complex Games

1. **Immediate context**: Player knows WHY they're doing something within 60 seconds
2. **Constrained start**: Limited resources/options force focus on learning one system
3. **Ally support**: NPCs or companions help in early encounters (Freelancer, Starsector)
4. **Story and tutorial fused**: The narrative IS the teaching mechanism (Freelancer, FTL)
5. **Clear macro-goal visible**: Even if distant (Factorio's rocket, FTL's rebel fleet)
6. **First trade/fight has safety net**: Failure is difficult or impossible in the first encounter
7. **Gradual system revelation**: One system per milestone, not all at once

---

## 3. Player Experience Metrics (PX Telemetry)

### 3.1 What AAA Studios Instrument

**Core First-Session Telemetry Events:**

| Event Category | Specific Events | Purpose |
|---------------|-----------------|---------|
| Session Bounds | session_start, session_end, session_duration | Base retention calculation |
| Tutorial Funnel | tutorial_step_N_reached, tutorial_completed, tutorial_skipped | FTUE drop-off analysis |
| First Actions | first_movement, first_interaction, first_combat, first_trade | Time-to-first-X metrics |
| Death/Failure | death_count, death_location, death_cause, time_between_deaths | Difficulty calibration |
| Economy | first_currency_earned, first_purchase, currency_at_session_end | Economy pacing validation |
| Navigation | time_in_menu, time_in_world, zone_transitions, backtracking_count | Confusion/wayfinding signals |
| Engagement | pause_count, settings_accessed, tutorial_re-read | Friction indicators |
| Quit Points | exact_timestamp_of_quit, last_action_before_quit, screen_at_quit | Churn point identification |

**Microsoft Game Studios approach (most widespread):**
- Combine simple pop-up surveys with behavioral telemetry
- Detect experience-related problems through behavior patterns
- Validated with Kane & Lynch: Dog Days -- model detected behaviors aligning with playtester frustration

### 3.2 "Fun Meter" Metrics (2005+)

Behavioral telemetry events that indicate enjoyment or frustration:
- **Spending patterns**: What players buy reveals what they value
- **Repeated failure**: Dying N times at same point = frustration signal
- **Time-on-task**: Minutes spent unsuccessfully attempting progression
- **Session frequency**: How often players return (D1/D7/D30)
- **Feature adoption**: Which systems players engage with vs ignore

### 3.3 Emotional State Measurement Methods

1. **Behavioral telemetry analysis**: Inferring experience from in-game actions (standard industry practice)
2. **Qualitative methods**: Interviews, surveys, pop-up feedback during playtests
3. **Psycho-physiological measures**: Heart rate, galvanic skin response (academic, not standard in production)

**Critical limitation**: Inferring experience from behavior alone "can be prone to errors" without validation through actual experience measures.

### 3.4 FTUE Funnel Analysis

Standard FTUE funnel benchmarks:
```
100% -- Start tutorial
 85% -- Complete first lesson/step
 70% -- Complete second lesson/step
 50% -- Finish tutorial
 35% -- Play first real match/mission
```

**Key metric**: Where exactly in the funnel do players drop? Each step that loses >15% needs investigation.

### 3.5 Telemetry Events Specific to Space Trading Games

For a space trading game, the following events are critical to instrument:

| Event | What It Measures | Red Flag Threshold |
|-------|-----------------|-------------------|
| first_dock_time | Time from game start to first station dock | >5 minutes |
| first_trade_time | Time from first dock to completing first trade | >3 minutes at station |
| first_profit_amount | Credits earned on first trade | <10% of starting capital |
| first_combat_time | Time from game start to first combat encounter | <3min (too early) or >30min (too late) |
| first_system_jump | Time to first interstellar travel | >20 minutes |
| menu_time_ratio | % of first hour spent in menus vs space | >40% = menu-heavy |
| idle_time_events | Periods of no input >10 seconds | >5 occurrences = confusion |
| objective_clarity | Time between completing objective and starting next | >30 seconds of no action |
| backtrack_count | Times player revisits same location without purpose | >3 = lost/confused |
| ui_tab_discovery | Time to discover each UI tab | Any tab undiscovered after 30min |

---

## 4. Feel & Juice Metrics

### 4.1 The Three Pillars of Game Feel (Steve Swink)

Steve Swink's framework identifies game feel as the intersection of:
1. **Real-time response**: Correction cycle (feedback -> decision -> action -> new feedback) under 100ms
2. **Simulated space**: Physics, collision, spatial relationships
3. **Polish**: Particle effects, screen shake, sound, animation flourishes

**ADSR Envelope for Game Inputs** (borrowed from music synthesis):
- **Attack**: How quickly an input reaches full effect (e.g., acceleration curve)
- **Decay**: How the effect settles from peak to sustain
- **Sustain**: Steady-state while input is held
- **Release**: What happens when input stops (e.g., deceleration, momentum)

### 4.2 Combat Feel: Specific Parameters

#### Hitstop/Hitfreeze
- **Purpose**: Freezes characters at point of collision to sell the impact, gives eyes frames to register hit, makes impact seem powerful
- **Light attacks**: 4-8 frames (~67-133ms at 60fps)
- **Medium attacks**: 8-12 frames (~133-200ms)
- **Heavy attacks**: 12-16 frames (~200-267ms)
- **Street Fighter standard**: ~8 frames hitstop
- **Smash Ultimate**: ~15 frames for 15% damage hit
- **Maximum before negative player satisfaction**: 150ms (0.15 seconds) in speed-oriented genres
- **Sweet spot for imperceptible stasis**: Under 75ms

#### Screen Shake
- **Key finding**: Adding ~100ms still frame between two segments of screen shaking increases perceived strength by ~30%
- **Vestibular system**: Screen shake stimulates "intuitive understanding regarding attack intensity"
- **Combined with hitstop**: Maximum synergy at ~75% intensity threshold where combined effects become "undetectable" as separate elements
- **Vlambeer rule**: Correlate shake intensity to damage dealt, never constant intensity

#### Damage Numbers
- **Purpose**: Explicit quantitative feedback, satisfies player desire to optimize
- **Animation**: Numbers should tween upward from impact point with slight randomized spread
- **Color coding**: Different colors for normal, critical, elemental types
- **Duration**: Visible for 0.8-1.5 seconds before fading
- **Scaling**: Critical hits should use larger font size (1.5-2x)

### 4.3 Trading Feel: Specific Techniques

#### Profit Feedback Design Rules
1. **Direct flow visualization**: Show currency flowing from trade completion to wallet UI location
2. **High visibility**: Currency reward animation must be hard to ignore
3. **Perceived abundance**: Animate slightly more currency particles than actual amount (creates feeling of wealth)
4. **Audio coupling**: Match animations to powerful, weighty audio cues
5. **Unique sound profiles per currency type**: Players should differentiate on sound alone
6. **If sound is lightweight, the reward feels light** (Game Dev Tycoon negative example)

#### Best Currency Animation Practices (from commercial game analysis)
- **Brawl Stars** (top rated): Mid-animation pause forces player attention before bar increment; exact count matches visual amount; non-linear spread pattern
- **Clash Royale**: Heavy, impactful sound effects; tight UX connecting rewards to main menu
- **Beatstar**: Spinning, light-reflecting animations; unique sounds per currency; subwoofer-emphasized audio
- **Robinhood (trading app)**: Completing trades triggered confetti animations, turning transactions into emotionally satisfying moments

#### Profit Number Display
- **Number counting up from 0 to final value** (not instant display)
- **Counting duration**: 0.5-1.5 seconds depending on magnitude
- **Sound**: Tick/click per increment, pitch rising slightly
- **Color**: Green for profit, red for loss, gold for exceptional
- **Comparison**: Show delta vs previous ("+42 credits" not just "142 credits")

### 4.4 Exploration Feel: Specific Techniques

#### Discovery Stingers (Audio)
- **Definition**: Brief musical phrases superimposed over current music, triggered by game events
- **Implementation**: Wwise/FMOD trigger system with stinger music segments
- **Timing**: 2-5 second stinger, cross-faded with ambient music
- **Usage**: New location discovered, rare item found, story clue uncovered, achievement unlocked

#### Fog of War / Map Reveal
- **The unknown fuels exploration**: Shrouded areas create tension and anticipation
- **Surprise factor**: Not knowing what you'll find creates genuine excitement
- **Subnautica emotional loop**: Joyful exploration -> sudden dread -> discovery of helpful thing -> accumulation -> return to safety -> build -> explore again (70% tranquil, 20% isolation, 10% visceral terror)
- **Outer Wilds principle**: "Show don't tell" -- bombastic moments serve as both lessons and narrative hooks (being jettisoned into space by a cyclone teaches planet mechanics AND is unforgettable)

#### Wayfinding Effectiveness by Method
| Method | % Players Who Perceive It |
|--------|--------------------------|
| Subtle (color temperature, slight geometry) | 1-20% |
| Coarse (lighting corridors, marked paths) | 35-60% |
| Situational (NPC hints, audio cues) | 70-93% |
| Direct (waypoints, arrows, minimaps) | 95-98% |

### 4.5 The "Juice Problem" Warning

Exaggerated feedback can harm game design when:
- Screen shake is constant regardless of impact severity (desensitization)
- Every action triggers particles/effects (information overload)
- Juice masks fundamental gameplay problems
- Effects don't match game tone (heavy juice in a meditative game)

**Rule**: Juice should echo core gameplay. Screen shake, squash and stretch, bounciness are only relevant in dynamic games. A trading game needs different juice than a fighting game.

---

## 5. Onboarding Best Practices

### 5.1 Cognitive Load Rules (Celia Hodent, GDC 2016)

**The "3 items maximum" rule**: During learning mode, players can process at most 3 simultaneous items. Working memory baseline is ~5 items +/- 2, but this drops to 3 when actively learning new things.

**Attention management principles:**
- Never combine competing inputs of the same cognitive type (e.g., voice-over + text both demand phonological processing)
- "Inattentional blindness": Players literally cannot perceive unattended information (gorilla experiment)
- Multitasking during onboarding reduces learning retention
- Every point of friction can turn the player away

**Memory retention strategies (three causes of memory lapse):**
1. **Encoding deficit** (shallow processing) -> Fix: emotional engagement + active participation
2. **Storage deficit** (time decay) -> Fix: spaced repetition across varied contexts
3. **Recall deficit** (retrieval failure) -> Fix: contextual reminders when mechanics reappear

**Key principle**: "The deeper the process the better the retention." Learning by doing outperforms loading screen text.

### 5.2 The Fortnite Onboarding Method (Celia Hodent)

1. List everything players must learn
2. Prioritize features based on game pillars
3. Define order and depth needed for each feature
4. Core movement and combat taught early through contextual dynamic text
5. "Show the locks before giving the keys" -- reveal limitations before providing solutions

**Specific discoveries:**
- Players intuitively used axes for wood harvesting despite design intent (false affordances)
- Red corner indicators for occupied worker slots went unnoticed (sensory affordance failure)
- Reorganizing weapons/gadgets/abilities into separate HUD sections increased usage of new items
- Pinning crafting ingredients to HUD reduced memory load

### 5.3 Progressive Disclosure Timeline

**Recommended mechanic introduction pacing:**

| Time Window | What to Introduce | Method |
|------------|-------------------|--------|
| 0-2 minutes | Movement + primary action | Contextual prompt, immediate practice |
| 2-5 minutes | Core interaction (dock/trade/talk) | Guided first attempt with ally/companion |
| 5-10 minutes | First consequence (profit/damage/discovery) | Let result speak for itself |
| 10-15 minutes | Second system (map/inventory/upgrades) | Triggered by need (e.g., inventory full) |
| 15-25 minutes | First real challenge | Reduced safety net, player applies learning |
| 25-40 minutes | Third system (e.g., automation, crafting) | Shown when first two systems are routine |
| 40-60 minutes | Meta-system preview (faction standing, research) | Tease only, don't teach fully |

**Tutorial length target**: 1-3 minutes of explicit instruction, integrated into gameplay. Never isolated from the game world.

### 5.4 Onboarding Anti-Patterns

1. **Wall of text before gameplay**: Players skip it, learn nothing
2. **Unskippable cutscenes before input**: Frustrates replay and experienced players
3. **Teaching everything at once**: Cognitive overload causes silent exits
4. **Punishment during learning**: Don't penalize players while they master mechanics
5. **Separating tutorial from game world**: Creates sense of "this doesn't count yet"
6. **Teaching mechanics without purpose**: "Press X to interact" without showing WHY
7. **Multiple information channels competing**: Objectives window + NPC dialogue + floating text simultaneously
8. **Teaching rare-use mechanics early**: Players forget by the time they need them

### 5.5 The "Never Lost" Principle

While the exact "never lost for 30 seconds" formulation isn't universally cited, the principle is well-established:
- **Matt Allmer's Gameplay Principle #1 (Focal Point)**: "Never allow the player to guess what they should focus on"
- **Player should always have a clear next action available** within their field of view or UI
- **Idle input detection**: If player has no input for >10 seconds, something should guide them
- **Objective clarity**: After completing any task, the next task should be apparent within 5-10 seconds
- **Halo "30 seconds of fun" (Jaime Griesemer, Bungie)**: Not "repeat the same 30 seconds" but "the core loop delivers fun in 30-second bursts, with constantly changing context"

---

## 6. Audio & Music in First Hour

### 6.1 Music State Machine Architecture

**Standard states for a space trading game:**

```
SILENCE
  |
  v
AMBIENT_CALM (exploration, safe space)
  |
  v
AMBIENT_TENSE (unknown territory, low resources)
  |
  v
COMBAT_LIGHT (minor skirmish, pirates spotted)
  |
  v
COMBAT_INTENSE (serious battle, hull damage)
  |
  v
COMBAT_BOSS (major encounter)
  |
  v
VICTORY_STINGER (short, returns to ambient)
  |
  v
DISCOVERY_STINGER (overlaid on current state)
```

**Transition types (from most to least dramatic):**
1. Direct splice (instant switch, for sudden combat)
2. Cross-fade (2-4 seconds, for zone transitions)
3. Bridge transition (composed connecting section)
4. Vertical layering (add/remove instrument layers)
5. Silence gap (deliberate pause for impact)
6. Smart transitions (auto-detect silence points in tracks)

### 6.2 Silence as a Design Tool

**Key insight**: "Using silence or near-silence can often elicit a more emotional response than constant music"

**When to use silence:**
- After a major story revelation (let it sink in)
- Before the first combat encounter (builds tension)
- During the opening seconds (let visuals speak)
- After a player's first death (weight of failure)
- During dock approach in a space game (focus on environmental audio)

**Psychological effect**: "No music or extraneous noise to tell the player how to feel enables a deeper attachment with the character. The player will be forced to feel genuine emotion because they aren't being told what to feel by over-the-top music and sound effects."

### 6.3 First-Hour Audio Pacing Recommendations

| Time | Audio State | Rationale |
|------|------------|-----------|
| 0:00-0:30 | Silence or minimal | Let visual introduction breathe |
| 0:30-3:00 | Gentle ambient, wonder-evoking | Establish world mood without pressure |
| 3:00-10:00 | Calm exploration music | Player is learning, don't add stress |
| 10:00-15:00 | Brief silence or musical transition | Mark shift from tutorial to gameplay |
| 15:00-25:00 | Richer exploration with harmonic development | Reward continued play with audio depth |
| 25:00-35:00 | First tension music (NPC contact, danger hint) | Prepare player emotionally for combat |
| 35:00-45:00 | First combat music | Match first real threat; audio should validate danger |
| 45:00-55:00 | Return to calm with new instruments/themes | Post-combat relief; new musical elements reward survival |
| 55:00-60:00 | Discovery or narrative stinger | Hook for continued play, something new revealed |

### 6.4 Sound Feedback for Game Actions

**Economy sounds:**
- Each currency type needs a unique sound profile
- Profit sounds should feel "weighty" -- subwoofer presence matters
- Counting-up sounds should have rising pitch
- Trade completion needs a satisfying "closure" sound (not just a click)

**Combat sounds:**
- If combat music doesn't appear during combat, it "startles a player who will not be prepared" (disjointed experience)
- DOOM (2016): Music selects instruments dynamically, switches between verse/chorus based on fight intensity
- Sound must arrive before visual effect for maximum impact perception

**Exploration sounds:**
- Discovery stingers: 2-5 second musical phrases overlaid on current music
- New zone entry should have unique ambient signature
- Silence between zones emphasizes transition

### 6.5 Implementation: Middleware

- **FMOD**: Vertical layering, real-time parameter control, user-friendly interface
- **Wwise**: Smart transitions, auto-detect silence, stinger system built-in
- Both support state machine-driven music with parameter-driven transitions

---

## 7. Narrative Pacing in First Hour

### 7.1 Narrative Opening Rules

**The opening is NOT the tutorial**: "A narrative opening is different from a gameplay one, and it's an art to seamlessly combine the opening area as a tutorial while moving the player through story and world-building."

**Common mistake**: "Developers often tend to push story and exposition over actual gameplay, frontloading exposition before the player can move their character or through overly long cutscenes."

**Best practice**: Story and tutorial should be inseparable. The player should be DOING something narratively meaningful while learning mechanics.

### 7.2 When Story Beats Should Hit

| Time | Story Element | Purpose |
|------|--------------|---------|
| 0-60 seconds | World context (where am I? why?) | Orientation, not exposition |
| 2-5 minutes | First NPC contact | Human connection, someone to talk to |
| 5-10 minutes | First objective with narrative meaning | "I'm not just pressing buttons, this matters" |
| 10-20 minutes | First conflict or mystery hint | Curiosity hook for continued play |
| 20-30 minutes | Companion/FO introduction | Relationship anchor, ongoing dialogue partner |
| 30-45 minutes | First narrative consequence | Player action affected the world |
| 45-60 minutes | Major revelation or cliffhanger | "I need to keep playing to find out what happens" |

### 7.3 Companion/NPC Introduction: The Hades Model

**Supergiant's techniques for NPC engagement:**
1. **State-checking dialogue triggers**: Game monitors player conditions (health below threshold, items collected, areas visited) and delivers pre-written narrative moments reactively
2. **Variable encounter sequencing**: Who players meet and when depends on individual playthroughs (no fixed NPC order beyond a few key moments)
3. **Event weighting system**: Developers can probabilistically weight certain narrative events to appear more frequently
4. **Death as narrative device**: Failure is rewarding because it advances relationships and reveals new dialogue
5. **"The game is paying attention"**: Small acknowledments of player behavior make characters feel alive

**Key rule**: Companions should react to what the player DID, not just deliver scripted lines. Check player state before delivering dialogue.

### 7.4 Environmental Storytelling

**Outer Wilds approach**: Every planet is a chapter, every ruin a paragraph. Discovery through observation, not exposition.

**Subnautica approach**: "The most horrifying discoveries are found through exploration, not exposition."

**Design rule**: The world should reveal story through:
- Object placement and environmental details
- NPC behavior (not just dialogue)
- Consequences visible in the environment
- Audio logs / text fragments discovered organically

### 7.5 Emotional Arc Structure

**The TV Drama Model (most applicable to game levels):**
1. Opening spike -- high-intensity event
2. Initial drop -- exposition and dialogue
3. Escalating peaks -- each subsequent action surpasses the previous
4. Frequency increase -- events occur in progressively shorter intervals toward climax
5. Final cliffhanger -- maximum intensity ending

**Key pacing insight**: "The contrast between peaks and valleys is what makes the action riveting. Deliberately plan lull periods to amplify subsequent peaks through contrast."

**Emotional pacing rule**: Alternate tense scenarios with intimate character interactions. Players form emotional connections during rest periods, not during action.

**The feeling curve**: Designers should map emotional structure as a "feeling curve" rather than a difficulty curve. Peaks = intensity. Troughs = calm. Sketch this alongside level progression.

---

## 8. Economy Feel in First Hour

### 8.1 First Trade Profit Design

**No universal formula exists**, but these principles are well-established:

**First trade must feel meaningful but not trivializing:**
- Profit should be >10% of trade investment (below this feels like wasted time)
- Profit should be <50% of starting capital (above this makes everything else feel unnecessary)
- **Recommended range**: First trade profit = 15-30% of starting capital
- Player should feel "I can see how doing more of this leads somewhere"

**Emotional framing of first trade:**
1. Show the before/after clearly (credits before -> credits after)
2. Highlight the delta ("+42 credits" in green)
3. Play a satisfying audio sting
4. Briefly show what this enables ("You're 30% toward your first upgrade!")

### 8.2 Currency Accumulation Curve

**General principles:**
- Earnings curve should grow exponentially or linearly, never plateau in the first hour
- Calculate: How many core loops (trades) per session? How many sessions to first major purchase?
- "2-3 emotions per game session" through alternating surplus and deficit
- Expenses and income can follow sine wave patterns, creating cyclical shortages and abundances

**First hour economy timeline:**

| Time | Economy Event | Player Feeling |
|------|--------------|----------------|
| 0-5 min | Starting capital given | Possibility, potential |
| 5-10 min | First small earning | "The system works" |
| 10-20 min | First significant profit | Competence, satisfaction |
| 20-30 min | First expense/cost encountered | Tension, motivation to earn more |
| 30-45 min | Surplus from good play | Reward, mastery validation |
| 45-60 min | First "big purchase" affordable | Achievement, progression milestone |

### 8.3 First "Big Purchase" Timing

- Should be achievable within 30-60 minutes of play
- Should cost 3-5x the player's first trade profit (enough to feel earned, not too far away)
- Should provide a visible gameplay improvement (not just a number increase)
- **Show the locks before giving the keys** (Celia Hodent): Display upgrade options before player can afford them
- Empty UI slots visually tease advancement opportunities

### 8.4 Economy Feedback Loops

**Positive feedback (reinforcing good play):**
- Visible profit on each trade
- Net worth tracking over time
- Comparison to "average" or NPC traders
- Achievement notifications for economy milestones

**Negative feedback (preventing runaway):**
- Market prices respond to player trading (price equilibrium)
- Maintenance/operating costs scale with fleet size
- Risk increases with valuable cargo

### 8.5 Resource Sink/Faucet Balance

**Roblox/industry standard formula:**
- EV (Expected Value) = Sum of (Item Value x Probability of Acquisition)
- Track: Who purchases what, actions yielding currency, earned vs purchased ratios
- Sources and sinks should remain "relatively elevated but moving close together"
- Event economies should use unique event currencies to protect main economy

**Rule**: "If you can balance your sources and sinks effectively while keeping players between boredom and frustration, they won't resent your economic mechanics -- they will be motivated by them."

---

## 9. Automated First-Hour Quality Evaluation: Metrics Catalog

Based on all research above, the following metrics should be tracked by an automated evaluation system:

### 9.1 Timeline Metrics (measure WHEN things happen)

| Metric ID | Metric | Target | Red Flag |
|-----------|--------|--------|----------|
| T01 | Time to first player input | <5 seconds | >15 seconds |
| T02 | Time to first fun (TTFF) | <60 seconds | >120 seconds |
| T03 | Time to first meaningful choice | <5 minutes | >10 minutes |
| T04 | Time to first reward/profit | <10 minutes | >15 minutes |
| T05 | Time to first narrative beat | <5 minutes | >10 minutes |
| T06 | Time to first combat | 15-35 minutes | <5min or >45min |
| T07 | Time to first "wow" moment | <30 minutes | >45 minutes |
| T08 | Time to first big purchase affordable | 30-60 minutes | >90 minutes |
| T09 | Time to second system introduction | 10-20 minutes | >30 minutes |
| T10 | Time to companion introduction | 5-20 minutes | >30 minutes |

### 9.2 Flow Metrics (measure QUALITY of experience)

| Metric ID | Metric | Target | Red Flag |
|-----------|--------|--------|----------|
| F01 | Maximum idle time (no input) | <10 seconds | >15 seconds |
| F02 | Menu time ratio (% in menus vs world) | <30% | >40% |
| F03 | Backtrack count (revisiting without purpose) | <3 | >5 |
| F04 | Death count in first 30 minutes | 0-1 | >2 |
| F05 | Credit balance trend (monotonically increasing first hour) | Upward | Flat or declining |
| F06 | Systems discovered count by 60 minutes | 4+ systems | <3 systems |
| F07 | NPC interactions in first hour | 3+ | <2 |
| F08 | Tutorial steps completed | >80% | <50% |
| F09 | Unique locations visited | 3+ | <2 |
| F10 | Cognitive load events (simultaneous new concepts) | <=3 | >3 |

### 9.3 Emotional Arc Metrics (measure PACING)

| Metric ID | Metric | Target | Red Flag |
|-----------|--------|--------|----------|
| E01 | Number of tension peaks in first hour | 3-5 | <2 or >7 |
| E02 | Longest calm period without event | <5 minutes | >8 minutes |
| E03 | Longest intense period without rest | <3 minutes | >5 minutes |
| E04 | Silence-to-music ratio in first 10 min | 20-40% silence | 0% or >60% |
| E05 | Combat music timing (first combat) | Within 2s of threat | >5s delay |
| E06 | Discovery stingers played in first hour | 2+ | 0 |
| E07 | Profit celebrations in first hour | 3+ | <2 |
| E08 | Story beats delivered | 4+ | <3 |
| E09 | Companion reactive dialogue events | 2+ | 0 |
| E10 | Post-combat relief period | 15-60 seconds calm | <5s or absent |

### 9.4 Feel/Juice Metrics (measure FEEDBACK QUALITY)

| Metric ID | Metric | Target | Red Flag |
|-----------|--------|--------|----------|
| J01 | Hitstop present on combat hits | Yes, 50-150ms | Absent |
| J02 | Screen shake on significant impacts | Yes, scaled to damage | Absent or constant |
| J03 | Damage numbers visible | Yes, with tween animation | Static or absent |
| J04 | Profit number count-up animation | Yes, 0.5-1.5s | Instant display |
| J05 | Currency flow animation on trade | Yes, source-to-wallet | Absent |
| J06 | Unique sound per currency/resource type | Yes | All same sound |
| J07 | Discovery stinger on new location/item | Yes, 2-5s | Absent |
| J08 | Input-to-response latency | <100ms | >150ms |
| J09 | Combat audio preceding/matching visual | Within 1 frame | Delayed |
| J10 | Transition animations between UI states | Yes, 0.2-0.5s | Instant jump-cuts |

---

## 10. Reference Game Benchmarks

### 10.1 First-Hour Emotional Arc Comparison

```
Freelancer:  Calm -> Mystery -> Tutorial -> Combat(safe) -> Story -> Freedom
FTL:         Pressure -> Choice -> Combat -> Reward -> Pressure -> Choice (loop)
Subnautica:  Wonder -> Explore -> Dread -> Discovery -> Relief -> Explore (loop)
Outer Wilds: Curiosity -> Explore -> Surprise -> Death -> Knowledge -> Curiosity (loop)
Hades:       Combat -> Death -> Story -> Combat -> Progress -> Story (loop)
Factorio:    Manual labor -> Small automation -> Relief -> Bigger problem -> Bigger automation
Dark Souls:  Death -> Learning -> Death -> Mastery -> Death -> Breakthrough
Elite:       Tutorial(isolated) -> Freedom(overwhelming) -> Confusion -> Self-directed learning
```

### 10.2 Design Pattern Reference

| Pattern | Best Example | How It Works |
|---------|-------------|-------------|
| Pain before relief | Factorio | Manual mining THEN automation -- relief proportional to pain |
| World-first, explanation-second | Subnautica, Outer Wilds | Discover, THEN understand; no exposition dumps |
| Character reactivity | Hades | NPCs react to what you did, not just scripted lines |
| Fragment assembly | Dark Souls | Story through scattered environmental clues |
| Knowledge gates | Outer Wilds | Progress through understanding, not items/levels |
| Ally-supported first combat | Freelancer | Enemies target allies first; player learns safely |
| Immediate macro-goal | Factorio, FTL | Clear distant objective visible from minute one |
| Scenario-based tutorial | Rimworld | Starting scenario IS the difficulty/tutorial selector |
| 30 seconds of fun | Halo | Core loop delivers fun in 30s, context constantly changes |

---

## Sources

### Research Papers & Studies
- [The First Hour Experience (Microsoft Research, CHI Play 2014)](https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/First20Hour20-20CHIPlay20201420-20preprint2.pdf)
- [Design Influence on Player Retention (HAL, 2020)](https://hal.science/hal-02436692/document)
- [Exploring Video Game Design and Player Retention (ACM, 2018)](https://dl.acm.org/doi/10.1145/3275116.3275140)
- [Screen Shake and Hit Stop Effects Research (Oreate AI)](https://www.oreateai.com/blog/research-on-the-mechanism-of-screen-shake-and-hit-stop-effects-on-game-impact/decf24388684845c565d0cc48f09fa24)

### GDC Talks & Industry Presentations
- [The Gamer's Brain Part 2: UX of Onboarding (Celia Hodent, GDC 2016)](https://celiahodent.com/gamers-brain-ux-onboarding/)
- [Half-Minute Halo: Interview with Jaime Griesemer (2011)](https://www.engadget.com/2011-07-14-half-minute-halo-an-interview-with-jaime-griesemer.html)
- [Optimization of Online Games through Telemetry (GDC Vault)](https://www.gdcvault.com/play/1015740/Optimization-of-Online-Games-through)
- [Playtesting and Metrics: Getting the Most Out of Usability Testing (GDC Vault)](https://gdcvault.com/play/1015581/Playtesting-and-Metrics-Getting-the)
- [Development Telemetry in Video Games (GDC Vault)](https://www.gdcvault.com/play/1012227/Development-Telemetry-in-Video-Games)
- [Supergiant's Narrative Design in Hades (Game Developer)](https://www.gamedeveloper.com/design/how-supergiant-weaves-narrative-rewards-into-i-hades-i-cycle-of-perpetual-death)

### Industry Articles & Analysis
- [16 Reasons Why Players Are Leaving Your Game (GameAnalytics)](https://www.gameanalytics.com/blog/16-reasons-players-leaving-game)
- [Why Do Players Leave? Top 5 Reasons (Game Developer / deltaDNA)](https://www.gamedeveloper.com/business/why-do-players-leave-your-game-top-5-reasons-revealed)
- [Steam Refunds: How Many Should You Expect (GameDiscoverCo)](https://newsletter.gamediscover.co/p/steam-refunds-how-many-should-you)
- [Telemetry-Supported Game Design (Game Developer)](https://www.gamedeveloper.com/design/telemetry-supported-game-design)
- [Measuring Player Experience (GameAnalytics)](https://www.gameanalytics.com/blog/measuring-player-experience)
- [The 13 Basic Principles of Gameplay Design (Matt Allmer, Game Developer)](https://www.gamedeveloper.com/design/the-13-basic-principles-of-gameplay-design)
- [Gameplay Fundamentals: Pacing & Intensity (Game Developer)](https://www.gamedeveloper.com/design/gameplay-fundamentals-revisited-harnessed-pacing-intensity)
- [5 Steps to Balanced In-Game Economy (Game Developer)](https://www.gamedeveloper.com/design/5-basic-steps-in-creating-balanced-in-game-economy)
- [Outer Wilds: Show Don't Tell Storytelling (Game Developer)](https://www.gamedeveloper.com/business/explaining-the-value-of-show-don-t-tell-storytelling-in-i-outer-wilds-i-)
- [Best Currency Animations of All Time (Game Economist Consulting)](https://www.gameeconomistconsulting.com/the-best-currency-animations-of-all-time/)
- [D1, D7, D30 Retention Drivers (Solsten)](https://solsten.io/blog/d1-d7-d30-retention-in-gaming)
- [Retention Benchmarks (Roblox Creator Docs)](https://create.roblox.com/docs/production/analytics/retention)
- [Balance Virtual Economies (Roblox Creator Docs)](https://create.roblox.com/docs/production/game-design/balance-virtual-economies)

### Audio & Music Design
- [Design With Music In Mind: Adaptive Audio Guide (Game Developer)](https://www.gamedeveloper.com/audio/design-with-music-in-mind-a-guide-to-adaptive-audio-for-game-designers)
- [Silence in Sound Design (gamesounddesign.com)](https://gamesounddesign.com/Silence-In-Sound-Design.html)
- [Adaptive Music Wikipedia](https://en.wikipedia.org/wiki/Adaptive_music)
- [Wwise Tutorial: Creating Stingers (Audiokinetic)](https://www.audiokinetic.com/learn/videos/fLrz463kVEI/)

### Game Feel & Juice
- [Game Feel by Steve Swink (book)](https://www.amazon.com/Game-Feel-Designers-Sensation-Kaufmann/dp/0123743281)
- [Squeezing More Juice Out of Your Game Design (GameAnalytics)](https://www.gameanalytics.com/blog/squeezing-more-juice-out-of-your-game-design)
- [The Juice Problem: How Exaggerated Feedback Harms Design (Wayline)](https://www.wayline.io/blog/the-juice-problem-how-exaggerated-feedback-is-harming-game-design)
- [Hitstop Deep Dive (CritPoints)](https://critpoints.net/2017/05/17/hitstophitfreezehitlaghitpausehitshit/)
- [Sakurai on Hitstop (Source Gaming)](https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/)
- [Hitstop in Capcom Beat 'Em Ups (Shane Sicienski)](https://shane-sicienski.com/blog/blog-post-title-one-55pmn)
- [Wayfinding (Level Design Book)](https://book.leveldesignbook.com/process/blockout/wayfinding)

### Onboarding & Narrative
- [Game UX: Best Practices for Video Game Onboarding (Inworld AI)](https://inworld.ai/blog/game-ux-best-practices-for-video-game-onboarding)
- [Proper Pacing of the Video Game Narrative (Lost to the Aether)](https://losttotheaether.wordpress.com/2014/02/08/proper-pacing-of-the-video-game-narrative-part-1/)
- [Designing for Emotional Pacing (The Design Lab Blog)](https://thedesignlab.blog/2025/06/16/designing-for-emotional-pacing-crafting-moments-that-matter/)
- [Story Pacing (Meegle)](https://www.meegle.com/en_us/topics/game-design/story-pacing)
