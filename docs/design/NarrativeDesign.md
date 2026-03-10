# Narrative Design — Master Document

> The storytelling strategy for Space Trade Empire. How the game's story is told,
> not what the story is (see `factions_and_lore_v0.md` for content).
> This doc governs how every system — exploration, audio, HUD, combat, economy,
> automation — participates in narrative delivery.
> Content authoring specs: `content/NarrativeContent_TBA.md` (missions, dialogue,
> discovery text), `content/LoreContent_TBA.md` (ancient data logs, fragments, faction
> histories). Epics: `EPIC.S7.NARRATIVE_DELIVERY.V0` (system), `EPIC.S8.NARRATIVE_CONTENT.V0` (content).

## Why This Doc Exists

Great game stories are not written — they are **built into systems**. Homeworld
didn't tell players to grieve for Kharak; it created conditions where grief was
inevitable. Portal didn't explain that GLaDOS was a murderer; it let players
discover it behind the walls. Outer Wilds didn't narrate the Nomai's fate; it
left their conversations on cave walls for players to piece together.

Space Trade Empire has exceptionally strong lore and world design (see
`factions_and_lore_v0.md`). What it needs is a **delivery architecture** —
the systems, techniques, and disciplines that let players *discover* the story
through play rather than receive it through text. This doc defines that
architecture.

---

## The Story at a Glance

*For full lore, see `factions_and_lore_v0.md`. This is the experiential summary
— what the player FEELS, not what they read.*

**Act 1 — The Rules (Tick 0–400)**
You are a trader in a galaxy at war. Threads are safe but expensive. Warfronts
distort prices. Factions compete. The Communion is starving. You learn the rules
of a universe built on containment infrastructure that everyone takes for
granted, the way we take roads for granted.

**Act 2 — The Escape (Tick 400–1200)**
You find a module that lets you break the rules. It's dangerous, it's expensive,
and it opens a universe of profit and mystery. You discover a hidden base.
You start finding artifacts that don't match any faction. The module does things
your engineers can't explain. The comfortable universe of Act 1 starts to feel
like a cage.

**Act 3 — The Truth (Tick 1200–2000+)**
The module is older than the threads. The threads aren't protecting you from
instability — they're suppressing spacetime's natural state. The Lattice is
failing. The factions each have a piece of the answer and none have the whole
picture. You've spent hundreds of trade decisions building relationships that
now determine which ending is available to you. The galaxy's future depends on
a trader who stumbled into a derelict.

---

## The 12 Narrative Design Principles

These principles are derived from analysis of Homeworld, Portal, Outer Wilds,
Dark Souls, Subnautica, Disco Elysium, Return of the Obra Dinn, Mass Effect,
Fallout: New Vegas, Sunless Sea, FTL, Freelancer, Stellaris, and Elite Dangerous.
They are **design laws** for this project.

### 1. The Player Assembles — Never Receives

> *"I don't want to tell the story. I prefer the players to unravel it by using
> their imagination and our hints."* — Hidetaka Miyazaki

Player-assembled meaning is more durable than author-delivered meaning. The
Adaptation Fragment system, the Fracture Module revelation arc, and the
pentagon dependency ring are all stories the player pieces together from
evidence — never from exposition dumps.

**Rule:** No cutscenes. No narrator. No "previously on..." recaps. The
Discovery Web (see ExplorationDiscovery.md) and environmental evidence are
the narrative delivery mechanisms. If the player can't discover it through
play, it doesn't exist.

**Exception:** Faction dialogue at stations is permitted (and encouraged) as
a characterization tool, but it must be *reactive* to the player's history,
not expository about the world's backstory.

### 2. The Mechanic IS the Story

> *"If you lower the delta between the story-story and the gameplay-story, the
> game becomes more satisfying."* — Erik Wolpaw, Portal GDC post-mortem

The best narrative moments in games are impossible to separate from mechanical
systems. Outer Wilds' time loop IS the story. FTL's resource cascade IS the
drama. Sunless Sea's fuel gauge IS the tension.

**STE's mechanical-narrative fusions:**

| Mechanic | Narrative It Embodies |
|----------|----------------------|
| Metric Bleed (cargo quantity shift in drift space) | Instability attacks the preconditions of commerce |
| Pentagon dependency ring (Concord needs Weavers needs Chitin...) | No faction is self-sufficient; philosophy doesn't override material need |
| Warfront demand cascades (Munitions spike → Metal rises → Components starve) | War is felt through prices, not battlefields — you are a trader, not a general |
| Fracture travel (fuel cost, hull stress, Trace accumulation) | Every shortcut has a price; the escape valve creates its own doom clock |
| Module sustain (T3 modules consume Exotic Matter every 60 ticks) | Your endgame ship literally runs on alien artifacts you have to keep finding |
| Maintenance treadmill (standing still costs resources) | Urgency from tick 1 — you must always be earning |
| Neutrality tax (escalating tariffs as wars intensify) | The middle ground shrinks; choosing not to choose IS a choice |

**Diagnostic question for every new system:** Does doing this thing feel like
participating in the story, or does it feel like pausing the story? If it
pauses the story, redesign the system so it doesn't.

### 3. Restraint Is the Instrument

> *"In Homeworld, they are all faceless professionals. The stakes are too high
> for personal drama."* — Stunt Word analysis

Homeworld's Fleet Intelligence reports facts and moves to objectives. The
voice never cracks — except once, at Kharak. That single crack is devastating
because it's the first time the mask slips. Portal's GLaDOS is cheerful and
institutional while committing murder. The calm IS the horror.

**For STE:** Military professionalism in all system communications. Fleet
reports, market updates, program status — all delivered in a clean, clinical
voice. When that voice changes (a Communion station broadcasting distress,
a Lattice node emitting an uncharacteristic signal, a fleet report that
suddenly includes personal language), the player notices because it
*violates the established register*.

**Specific applications:**

- **Concord** stations use bureaucratic euphemisms that the player gradually
  learns are lies: "thread optimization event" = thread failure; "scheduled
  maintenance period" = emergency repair; "regulatory alignment" = censorship.
- **Program failure messages** are clinical: "Trade Charter stalled: Proxima
  tariff now 25%, margin negative. Reroute? [Yes] [Edit Route]." The clinical
  language conveys competence. The failure conveys world-state.
- **The revelation arc** works by contrast: after 12+ hours of professional
  systems language, encountering ancient Adaptation faction texts that are
  *personal* — scientists arguing, worrying, hoping — hits harder because of
  the emotional register change.

### 4. Absence Tells the Story

> Portal's most powerful storytelling is what it doesn't show. Empty cafeterias,
> abandoned offices, daycare centers — all perfectly maintained, all empty.

What is missing tells the story more powerfully than what is present.

**For STE:**

- **The thread builders** are present through absence. The threads are containment
  infrastructure built by beings who are gone. The Lattice maintenance drones
  are repair bots without masters. Empty docking berths sized for ships no
  modern faction builds. Accommodation geometry structures that have been
  stable for millions of years with no one to maintain them.
- **Phase 4 (Void)** — "The Lattice drones are absent. Whatever they were
  maintaining no longer exists here." The most terrifying thing isn't hostile
  drones. It's the place where even the drones have given up.
- **Discovery sites** should sometimes be empty. A ruin that yields no loot,
  only a single data fragment. A derelict with no cargo, only an empty hull
  and a frozen instrument mid-reading. A beacon broadcasting to no one.
  These "empty" discoveries are narratively richer than loot piñatas because
  the player must infer what happened.

### 5. Trust Is Built to Be Broken

> Portal spends the first half building trust with GLaDOS — her jokes are funny,
> the chambers work as described. The humor is capital being deposited so the
> betrayal costs something.

**For STE — two betrayal arcs:**

**The Concord Betrayal (Political Trust):**
- **Build phase (Act 1):** Concord stations are the safest, most comfortable,
  most profitable places to trade. But more importantly: Concord is *visibly
  good*. The player sees Concord relief convoys supplying struggling Communion
  stations. Concord subsidizes Food prices to keep poor stations fed. Concord
  medical supply chains reach everywhere. The bureaucracy is real, but so is
  the public service. The player should genuinely respect Concord — not just
  find them convenient.
- **Cracks (Act 2):** Intelligence briefings with redacted sections. Stations
  near degrading threads with reassuring official statements that the player has
  no reason to doubt yet (because Concord has been competent and honest about
  everything else). Other factions hint at Concord being "too confident" —
  not "corrupt," just "too sure the threads will hold."
- **Break (Act 3):** At high Concord rep, the player discovers the suppression.
  But the betrayal is NOT "the comfort was a lie." The comfort was real. The
  relief convoys were real. The Food subsidies were real. The lie was about
  the *future* — Concord knows the threads are failing and chose not to tell
  anyone because the panic would cause more deaths than the silence. The
  player must decide: was Concord wrong? The suppression saved lives. The
  truth might have saved more. There's no clean answer.

  **Critical design rule:** The player should feel the way you feel learning
  a parent lied to protect you — not "I knew they were corrupt" but "they
  did real good, and they were also hiding the most important truth in the
  galaxy, and I don't know if that makes them wrong."

**The Thread Betrayal (Existential Trust):**
- **Build phase (Act 1):** 3-4 hours of gameplay where threads are safe, reliable,
  fast. The backbone of civilization. The player depends on them completely.
- **Cracks (Act 2):** Fracture travel reveals that off-thread space isn't chaos —
  it's spacetime's natural state. The threads are artificial. The Module works
  better the further you go from threads, not worse.
- **Break (Act 3):** The revelation that the threads are containment infrastructure
  actively error-correcting spacetime's natural turbulence — the galaxy's calm
  is artificial, maintained by machinery that's failing. The thing the player
  depended on for survival is itself a kind of violence imposed on reality. The
  accommodation geometry — the Adaptation faction's approach — works because it
  doesn't fight the turbulence; it shapes it. The Haven has been stable for
  millions of years without containment. The "safe" option was the dangerous one
  all along.

### 6. Knowledge Is the Real Progression

> Outer Wilds strips out every conventional progression system. The player's sole
> currency is knowledge, and knowledge lives in the player's mind, not a save file.

The Adaptation Fragment system should function as a **knowledge web**, not a
collect-a-thon. Each fragment recontextualizes other things the player has
already seen.

**Implementation requirement — the Discovery Web:**
(Cross-reference: `ExplorationDiscovery.md` → Knowledge Graph section)

The Discovery Web is STE's equivalent of the Outer Wilds Ship Log. It
visualizes connections between discoveries as a relationship map:

- "This fragment references the same mathematical notation as the inscription
  on that derelict."
- "This Lattice node responded to your module the same way that ancient relay did."
- "The Communion's shimmer-zone readings match the readings from your Phase 2
  expedition — they're seeing the same phenomenon at different depths."

The web is NOT a quest tracker. It does not tell the player what to do next.
It shows what connects to what, and the player draws conclusions. The web
IS the story interface.

**Progression gating through knowledge, not items:**

| Traditional Gate | STE Knowledge Gate |
|-----------------|-------------------|
| "Collect 5 artifacts to unlock door" | "Understanding how the Lattice authentication works lets you interact with Lattice nodes" (Fragment 6) |
| "Reach level 20 to access this zone" | "Learning the fracture weight ratios for each good makes Drift-space trade profitable" (player knowledge, not item) |
| "Buy the key item from the vendor" | "The Communion shares shimmer-zone navigation data after you prove trustworthy by delivering Food" (reputation as knowledge access) |

### 7. The Journey Creates Investment

> Homeworld's fleet persists across missions. Ships that survive Mission 2 carry
> to Mission 10. The player doesn't name their pilots, but watches the same cohort
> dwindle and issues protective orders — not for stats, but for history.

**Fleet ships should accumulate visible history:**

```
THE ARGENT CROSSING — Corvette
Commissioned: Tick 47 | Service: 523 ticks
Trade runs: 47 | Combat engagements: 3
Survived: Valorin ambush at Node 17 (Tick 203)
Currently assigned: Weaver Composites route
```

When a fleet ship is destroyed, its name and service record are logged. The
player should feel the loss of a ship that's been with them for 500 ticks
differently than one commissioned yesterday.

**The Communion remembers you — and has seen this before:**
(From `factions_and_lore_v0.md`) "You're the one who kept Waystation Kell
alive." The Communion is the faction that treats the player as a person, not
a customer. NPCs at other Communion stations greet you by reputation. Named
NPCs reference your specific trade history. This is the emotional hook that
makes the Renegotiate path personal, not just optimal.

But the Communion has a secret: they recognized the player's module
signature the first time the player docked. They've seen this before —
every few generations, someone finds a piece of accommodation geometry.
The Communion calls them *threshold-crossers*. Most don't last long.

The Communion doesn't cultivate the player. They help everyone — it's
their culture. They share shimmer-zone data with anyone who asks. Their
warmth is genuine. But they ARE watching, because they always watch
threshold-crossers. And hoping, because they always hope.

The max-rep reveal is devastating in its simplicity: a Communion elder
tells the truth directly. "You're not the first. Every few generations,
someone finds a piece of it. We've learned that telling people what they
carry doesn't help. So we watch. We help when you ask. We hope. You've
gone further than any of them."

The player is not special. Not chosen. Not destined. They are the latest
in a long line — and the question becomes: am I repeating a pattern that
always ends the same way?

**The First Officer's role:** The player's First Officer (see
`factions_and_lore_v0.md` → "The First Officer") is the personal emotional
anchor that the Communion provides at the faction level. The FO reacts to
revelations in real-time, making abstract twists personal. Three candidates
with distinct archetypes (Analyst, Veteran, Pathfinder) let the player
choose the emotional register of their narrative experience. Each aligns
with a different endgame path but does not require it.

### 8. Economic Pressure IS Story Beats

> Every resource that can become scarce has narrative potential. Every time you
> make a resource abundant and permanent, you remove narrative pressure.

Economic cascades need **narrative voice**, not just price changes:

| Economic Event | Pure Data Display | Narrative Display |
|---------------|------------------|-------------------|
| Rare Metals +200% | "Rare Metals: 400 cr (+200%)" | "Frontier's burning. Valorin can't hold the deposits. We need alloys and we needed them last week." — Chitin station comm |
| Trade Charter stalled | "Stalled: insufficient ore" | "Trade Charter stalled: Sirius ore depleted by warfront demand. Margin negative at current prices. [Reroute] [Wait]" |
| Embargo activated | "Embargo: Composites blocked" | "Weaver embargo on Composites effective immediately. Concord fleet readiness degrading. Patrol strength at border nodes -20% projected within 200 ticks." |

The automation system (`AutomationPrograms.md`) already defines stall/failure
states with reason codes. These reason codes are narrative opportunities.
"ROUTE_HOSTILE" is data. "Route hostile: Valorin patrol interdiction at Node
12. Two convoys lost this cycle. Risk tolerance exceeded." is story.

### 9. Every Faction Must Be Right About Something

> Josh Sawyer's GDC talk on New Vegas: give every faction coherent internal logic,
> then make the player experience the COSTS of that ideology, not just the benefits.

STE's faction design is already strong. The endgame paths must make the player
feel the **cost** of their choice:

| Path | What You Gain | What You Lose |
|------|--------------|---------------|
| **Reinforce** | Threads stabilized. Civilization preserved. Concord gratitude. | Fracture travel sealed forever. Exotic Crystal supply dies. Communion loses their way of life. You chose the cage. |
| **Naturalize** | Adaptation succeeds. New civilization in unstable space. Frontier freedom. | Thread-space becomes dangerous. Concord collapses. The infrastructure billions depend on crumbles. You chose the wilderness. |
| **Renegotiate** | Understanding. The deepest truth. Neither suppress nor adapt — communicate. | Every other faction thinks you're insane. Only the Communion supports you. The outcome is uncertain because it's genuinely new. You chose the unknown. |

**The New Vegas lesson:** None of these should feel like a "good ending." They
should all feel like a *real* ending — one with genuine losses alongside genuine
gains. The game's refusal to resolve whether instability is alive, conscious,
or just physics is correct. The best endings leave the player thinking.

### 10. Environmental Storytelling Beats Exposition

> Subnautica's Degasi bases descend physically deeper and psychologically darker.
> Their story mirrors yours with a time-delay. You read their playthrough while
> making yours.

**Instability phases as environmental narrative:**
(Cross-reference: `factions_and_lore_v0.md` → Instability Phases)

Each phase must have **discoverable scenes** — not just items, but tableaux:

| Phase | Environmental Discovery Example |
|-------|-------------------------------|
| **Shimmer** | A sensor buoy with readings that disagree with your instruments. Not broken — measuring a different version of the same quantity. A Communion research station where the scientists seem calm, almost reverent. |
| **Drift** | A derelict where the navigation log shows a 12-hour journey that took 9. Cargo manifest reads 100t by mass, 82t by volume. The hull is cracked along stress lines that don't match any known force pattern. |
| **Fracture** | An accommodation geometry structure, stable for millions of years. Instruments frozen mid-reading, measuring something that no longer exists in this phase. A docking clamp still holding the outline of a ship that dissolved millennia ago. |
| **Void** | Nothing that makes conventional sense. The instruments read true but what they report contradicts visual reality. A structure that exists in topology but not geometry — you can dock at it because it's *connected* to your ship, but it has no measurable size. |

**These scenes should be wordless.** The environment tells the story. A data
log can provide context, but the primary narrative beat is visual/spatial.
The player walks into a room and understands what happened without reading
anything.

### 11. The Revelation Recontextualizes Everything

> Portal 2: learning that GLaDOS was built from Caroline retroactively reframes
> GLaDOS's hostility as grief. The monster has a human origin.

**STE's five recontextualizations:**

**Recontextualization 1 — "It's Not a Drive" (Module revelation, ~Hour 8)**
The fracture module predates the threads. Every fracture jump the player made
wasn't damaging infrastructure — it was de-containing space. The guilt the
player felt (Concord told them it was dangerous, NPCs warned them) was
manufactured by a faction that benefits from keeping them on the threads.

**Recontextualization 2 — "The Threads Aren't Protection" (~Hour 12)**
The containment approach has been failing for millennia. The Lattice is
degrading. The accommodation approach (the fracture module, the Haven) has
been stable for millions of years. The "safe" option was the dangerous
one all along.

**Recontextualization 3 — "The Economy Is a Cage" (~Hour 15)**
The pentagon dependency ring is not natural. It was engineered by the same
intelligence that built the threads. Every trade the player has run — every
Composites delivery, every Rare Metals haul — maintained containment
infrastructure applied to *civilization*, not just spacetime. The player
discovers this through gameplay: a fracture-space trade route breaks the
ring pattern, a Communion station begins producing its own Food. The
dependency was a rule that only applied inside the cage.

**This is the twist only a trading game can deliver.** No other game can
recontextualize 15 hours of core gameplay (trade routes) as participation
in a conspiracy. The player wasn't a neutral trader. They were a cog in
a machine designed to prevent species independence.

**Pentagon Revelation — Trigger Specification:**
The revelation triggers when the player has completed trade routes touching all
5 faction types. This is the *gameplay discovery* — the player has personally
participated in every link of the chain. Delivery sequence:
1. **Unmissable toast:** "Trade Analysis Complete — Pattern Detected" (Gold toast,
   the only Gold toast in the game — reserved for this moment).
2. **Galaxy map highlight:** All 5 faction trade routes illuminate simultaneously
   for 3 seconds, showing the circular dependency. The pentagon shape is visible.
3. **First Officer reaction** (personality-appropriate):
   - Analyst: "I mapped every major trade route. They form a closed loop. Five
     factions, five dependencies, zero redundancy. Someone designed this."
   - Veteran: "Five factions, each dependent on the next. I've seen supply chains
     weaponized before — but never this elegantly."
   - Pathfinder: "It's a web. The whole economy is a web, and we've been tracing
     its strands. Someone spun this. Someone with patience."
4. **Discovery Web update:** A new gold-colored connection appears linking all 5
   faction nodes in a ring, labeled "Engineered Dependency."

The player cannot miss this moment — it is the narrative's structural keystone.
No other revelation fires without the FO present. This one fires without player
action because the trigger IS accumulated player action across the entire game.

**Recontextualization 4 — "The Module Is Changing Me" (~Hour 18)**
The fracture module isn't adapting to the ship. It's adapting the pilot.
The UI effects in unstable space — chromatic aberration, parallax,
distortion — are not degraded perception. They are the pilot seeing
space *more accurately* than stable-space beings can. Normal space is
the simplified view. The "distortion" is high-fidelity.

This is never stated explicitly. The player infers it: the effects feel
less "wrong" over time, a Communion elder remarks the player "sees like
we do now," a data log describes Adaptation scientists developing "metric
perception." The conclusion is the player's to draw — and it raises the
question of whether the module has shaped their judgment, not just their
senses.

**Recontextualization 5 — "Instability Is Not Entropy" (Endgame)**
The nature of instability itself. Not decay, not chaos, not destruction.
Something else. The game never names what it is. Each endgame path represents
a different response to this ambiguity.

**Design requirement:** For each recontextualization to land, the player must
have **acted on the wrong assumption** for hours before it flips. The hours of
guilt about "damaging threads" make the revelation that you weren't hit harder.
The hours of depending on Concord's safety make the discovery of their lies
sting more. The hours of *running trade routes within the cage* make the
discovery that the economy is engineered devastating. The hours of trusting
your own perception make the discovery that the module changed you unsettling.
The hours of fearing instability make the discovery that it might be something
other than entropy genuinely surprising.

**Critical: Recontextualizations 3 and 4 are discovered through GAMEPLAY,
not text.** The player observes the dependency ring breaking in fracture space
(R3) and notices that unstable-space perception feels less disorienting over
time (R4) before any data log or NPC dialogue confirms it. The text confirms.
The experience reveals.

### 12. The Story Serves the Core Fantasy

> Every great game story reinforces the core player fantasy. Homeworld: you are
> the last hope. Portal: you are smarter than the system. New Vegas: you are the
> deciding factor.

**STE's core fantasy:** You are Han Solo — a pilot-entrepreneur caught in a
galactic war, trying to profit and survive while larger forces reshape the galaxy
around you.

Every narrative beat must reinforce this:

| Fantasy Element | How Story Reinforces It |
|----------------|------------------------|
| You are a pilot, not an emperor | You feel wars through prices, not battlefields |
| You are a trader who discovers truth | Ancient mystery unfolds through trade routes and exploration, not quest markers |
| Your importance is earned, not destined | The fracture module found you by accident. Your significance comes from being the only person who can go where it goes, not from prophecy |
| Your choices define the world | The endgame emerges from accumulated trade decisions, not a dialog wheel |
| You navigate between factions, not above them | Every faction has something you need. No faction is simply "the enemy" |

---

## The Five Narrative Layers

Story flows through STE on five simultaneous layers. Each layer uses different
delivery mechanisms and operates at different timescales.

### Layer 1: Ambient Narrative (Always Present)

**What it is:** The world tells its story through existing state — prices, NPC
behavior, thread conditions, station descriptions, market data.

**Delivery mechanisms:**
- Market prices (high Munitions price at a station = warfront nearby)
- NPC convoy frequency (fewer traders on a route = that route is dangerous)
- Station condition (Communion station at 50% supply vs Concord at 100%)
- Thread security band colors (green = safe, red = hostile)
- Instability phase visual effects (shimmer, parallax, distortion)

**Cross-reference:** `GalaxyMap.md` (overlay modes), `HudInformationArchitecture.md`
(progressive disclosure), `RiskMeters.md` (world-state feedback)

**Design rule:** The player should be able to read the state of the galaxy by
looking at it, before opening any menu. A system at war *looks* different from
a peaceful system. A Communion station in distress *feels* different from a
Concord station at capacity.

### Layer 2: Economic Narrative (Per-Trade)

**What it is:** Every trade the player makes is a narrative act — carrying goods
between factions, supporting or undermining supply chains, participating in
wartime economics.

**Delivery mechanisms:**
- Trade goods with faction-specific origins ("Valorin-refined Rare Metals")
- Program stall/failure messages that explain WHY (embargo, warfront, depletion)
- Price history showing warfront impact ("Munitions +200% since Tick 400
  — Valorin-Weaver front opened")
- Reputation changes from trade acts ("Sold Munitions to Concord: +2 Concord
  rep, -1 Chitin rep")

**Cross-reference:** `AutomationPrograms.md` (failure feedback), `trade_goods_v0.md`
(supply chain design), `dynamic_tension_v0.md` (warfront cascades)

**Design rule:** The player should never make a trade that feels like "just
moving numbers." Every significant trade should have a visible consequence in
the world — a price that changed, a faction that noticed, a supply chain that
moved. Small trades are ambient; large trades are events.

### Layer 3: Discovery Narrative (Per-Exploration)

**What it is:** The ancient mystery, unfolding through exploration. Derelicts,
ruins, signals, adaptation fragments, Lattice interactions, void sites.

**Delivery mechanisms:**
- Discovery lifecycle (Seen → Scanned → Analyzed) with milestone feedback
- Discovery Web connecting related findings
- Rumor/lead system chaining discoveries into sequences
- Narrative templates for discovery families (see `ExplorationDiscovery.md`)
- Adaptation Fragments with solo effects and resonance pair bonuses

**Cross-reference:** `ExplorationDiscovery.md` (discovery system),
`factions_and_lore_v0.md` (fragment web, revelation arc)

**Design rule:** Every discovery must answer a question AND raise a new one.
"What is this?" → "A Valorin scout wreck." → "What were the Valorin doing this
far from their territory?" → "The energy signature matches an ancient beacon."
→ "Who built the beacon?" The chain never terminates with a closed answer until
the endgame.

### Layer 4: Faction Narrative (Per-Relationship)

**What it is:** The factions react to the player's accumulated history. New
dialogue, new trade terms, new information, new demands — all driven by
reputation and accumulated actions.

**Delivery mechanisms:**
- Station dialogue that changes with reputation tier
- Faction intel briefings at reputation milestones
- Exclusive supply contract offers during wartime
- Faction-specific module unlocks at max rep
- Faction reactions to fracture module use

**Cross-reference:** `factions_and_lore_v0.md` (faction personalities, pentagon
dependency), `dynamic_tension_v0.md` (neutrality tax, shrinking middle)

**Design rule:** Each faction reveals a different piece of the truth. Concord
reveals the suppression. Chitin reveals the probability models. Weavers reveal
the stress patterns. Valorin reveal the frontier. Communion reveals the
experience. The player who allies with only one faction gets only one piece.
The player who maintains multiple relationships gets a more complete picture —
but at higher cost (neutrality tax, time investment).

### Layer 5: Revelation Narrative (Five Major Beats)

**What it is:** The paradigm-shifting moments that recontextualize the player's
understanding of the world. These are the game's biggest emotional beats.

**The five revelations:**

1. **The Module Revelation (~Tick 800, ~Hour 8)**
   The fracture module is not an experimental drive. It's Adaptation faction
   technology, older than the threads. The player's guilt about "damaging threads"
   was misplaced — the module de-contains space, which weakens nearby threads as
   a side effect, not the primary function.

2. **The Concord Revelation (~Tick 1200, ~Hour 12)**
   At high Concord reputation, the player discovers the suppression. But
   Concord is not a villain — they are a genuinely good institution that made
   a terrible compromise. The relief convoys were real. The Food subsidies
   were real. The lie was about the future. The player must decide whether
   Concord was wrong.

3. **The Economy Revelation (~Tick 1500, ~Hour 15)**
   The pentagon dependency ring is engineered. Every trade route the player
   has been running maintained containment infrastructure applied to
   civilization. The player discovers this through *gameplay* — a fracture-space
   trade route breaks the ring pattern, a Communion station produces its own
   Food — before any data log confirms it. This is the twist only a trading
   game can deliver: 15 hours of core gameplay recontextualized as
   participation in a system of control.

4. **The Communion Revelation (~Tick 1800, ~Hour 18)**
   The Communion elder tells the truth: they recognized the module from
   the first dock. They've seen this before — every few generations,
   someone finds a piece of accommodation geometry. Most don't last long.
   The simplest, most devastating moment in the game — after every other
   faction hid things, one faction tells the truth. What they reveal is
   not a conspiracy but a pattern: the player is the latest in a long
   line of threshold-crossers, and the Communion has been quietly mourning
   most of their predecessors. Forces the Renegotiate-path player to ask:
   am I repeating a pattern that always ends the same way?

5. **The Instability Revelation (Endgame, ~Tick 2000+)**
   Instability is not entropy. It is not decay. It is *process*. The
   containment wasn't just suppressing physics — it was interrupting something.
   What that something is remains ambiguous. The three endgame paths each
   represent a different response. And the player cannot be certain their
   judgment hasn't been shaped by the module's perceptual adaptation.

**Design rule:** Each revelation requires the player to have been wrong for
hours before it arrives. The wrongness must be felt through gameplay, not text.
The player must have made decisions based on the wrong assumption — used
fracture travel while believing it was dangerous, traded with Concord while
believing they were honest, *run trade routes within the cage without knowing
it was a cage*, trusted the Communion's innocence, feared instability while
misunderstanding what it was. The flip hits harder because of the investment.

**Critical:** Revelations 3 (Economy) and 4 (Communion) are the game's
strongest because they emerge from the player's *present* relationship with
the game world, not from the archaeological past. Genre-savvy players predict
ancient-mystery twists. Nobody predicts "the trade loop is a cage" or "the
most honest faction was guiding you all along."

---

## Ancient Data Logs — Conversation Format

Inspired by Outer Wilds' Nomai wall texts, ancient data logs should be
written as **conversations between scientists**, not as encyclopedia entries.

### Why Conversations

- They imply personalities — the reader cares about people who disagree
- They can be found in any order and still make sense (each log is a
  complete exchange)
- They embed technical information in emotional context (a scientist who
  is worried about their calculations is more compelling than a dry report)
- They create the impression of a real community of minds, now absent

### Format

```
── DATA LOG: ACCOMMODATION STUDY 7.4.2 ──

KESH: Your simulations assume the metric field is locally
uniform within the test volume. It is not.

VAEL: The variance is within tolerance. The accommodation
lattice compensates for local fluctuation.

KESH: We tested this. [See: Site Theta measurements, cycle
4,107.] The lattice compensates for KNOWN fluctuation patterns.
Novel fluctuations — the kind the Containment faction's models
don't predict — propagate through the lattice as resonance.
The material adapts, but the adaptation takes time. During
adaptation, function degrades.

VAEL: How much degradation?

KESH: 12-30% depending on the fluctuation frequency. For
navigation: acceptable. For structural load-bearing: concerning.
For life support: [section damaged]

VAEL: We need to tell the Council.

KESH: The Council shut down Site Theta. They classified the
cycle 4,107 measurements. Vael is transferring to the orbital
platform. I don't think she knows they're reassigning her to
keep her away from the data.

VAEL: I know, Kesh. I've known since the audit.

[Log ends]
```

### What This Format Achieves

- **Kesh and Vael are people.** The player develops feelings about them.
- **The conversation implies a larger conflict.** The Council suppressed
  research — this parallels Concord's current behavior.
- **Technical details are embedded in argument.** The player learns about
  accommodation geometry through a dispute, not a textbook.
- **The final exchange is devastating because it's private.** Neither
  scientist is addressing the future. Neither knows anyone will ever read
  this. Vael's quiet "I know, Kesh" is a private moment of grief between
  colleagues, not a message in a bottle. The player is eavesdropping on
  people who have been dead for millions of years. That intimacy — finding
  someone's unguarded frustration rather than their prepared statement — is
  more haunting than any letter to posterity.

**Design rule for all ancient logs:** Logs are NEVER addressed to the
future. They are internal records — lab notebooks, arguments, daily memos.
Scientists recorded because that's what scientists do. No log should
contain any variation of "someone will find this" or "for those who come
after." The player is not the intended audience. They are an accidental
witness. This makes every log feel more real and more invasive.

### Naming Convention

The thread-builder scientists should have short, distinct names (Kesh, Vael,
Oruth, Senn, Tal) that the player encounters across multiple logs at different
sites. Finding a log from Kesh at a new ruin creates recognition — "I know
this person" — which deepens investment.

**Each scientist has a personal contradiction** that transcends their debate
position (see `factions_and_lore_v0.md` → "The Scientists Behind the Data
Logs"). Kesh privately agrees with Vael but is too afraid to say so. Vael
hides doubts about accommodation at extreme phases. Oruth approved the
pentagon ring knowing it was wrong. Senn is genuinely amoral — incapable of
seeing systems as having moral dimensions. Tal grieves the failing infrastructure
more than any political argument. These contradictions are revealed across
multiple logs — early logs show the public position, later logs reveal the
private truth. No single log reveals a full personality.

---

## The Silence Principle

(Cross-reference: `AudioDesign.md` → The Silence Palette)

Homeworld's score is powerful because it is sparse. The silence between notes
creates emotional weight. Portal's empty facility is powerful because the
absence of people is the story. Outer Wilds' final moments work because the
music stops.

**STE's silence moments:**

| Moment | What Is Silent | Why |
|--------|---------------|-----|
| First entry into Phase 2 (Drift) space | Music stops. Engine hum continues. Instruments disagree. | The player should feel the wrongness before any UI tells them something changed. |
| Finding an empty discovery site | No loot sound. No fanfare. Just a data log and a frozen scene. | The absence of reward IS the narrative — this place has been picked clean by time. |
| The Module revelation | All audio dims except the module's own resonance (a new sound the player has never heard). | The module "speaking" for the first time should be the only sound in the world. |
| Post-combat | Weapon sounds stop. Music fades to ambient. Engine hum returns slowly. Debris drifts. | The relief of survival. The cost of what was spent. Homeworld's post-mission silence. |
| Entering the Haven for the first time | Music fades to a single sustained note. The Haven's stabilizer hum is warm and low. | The first truly safe place outside thread-space. Safety should sound different. |

---

## Cover-Story Naming Discipline

The fracture module's player-facing name is **"Structural Resonance Engine"**
(abbreviated "SRE") until the Module Revelation (~Hour 8). The term "fracture"
must NEVER appear in any UI element, tooltip, dialogue, loading screen, or
player-visible text before that beat.

| Context | Before Revelation | After Revelation |
|---------|------------------|-----------------|
| Module name in inventory | Structural Resonance Engine | Adaptation Drive |
| Travel action label | SRE Transit | Fracture Transit |
| Hull stress tooltip | "SRE field interference" | "Accommodation recalibration" |
| Concord NPC dialogue | "That experimental drive is unlicensed" | "Where did you *get* that?" |
| Trace meter tooltip | "Regulatory attention from SRE emissions" | "Lattice detection of accommodation signature" |

**Rationale:** The name "fracture" telegraphs the mystery — players immediately
connect it to the threads. "Structural Resonance Engine" sounds corporate and
boring, which is exactly right. The cover story should feel like mundane
technology. The revelation that it's ancient Adaptation tech should reframe
something the player thought was ordinary, not confirm something they already
suspected.

**Internal dev docs** may continue using "fracture module" for clarity. This
rule applies only to player-facing strings and design doc sections that define
player-visible text.

### Cover-Story Naming Enforcement

All player-facing strings must be linted for pre-revelation leaks. This is not
optional — a single misplaced word destroys the Module revelation's impact.

**Forbidden terms in player-facing text before the Module revelation (~hour 8):**
- `fracture`, `adaptation`, `accommodation`, `ancient`, `organism`

**Post-revelation unlocks:**
- After Module revelation: `fracture` and `adaptation` become permitted
- After Communion revelation: `accommodation` becomes permitted
- `ancient` permitted only in Discovery Web context (never in UI chrome)

**CI enforcement:** Grep-based check against all string literals in
`scripts/ui/*.gd`, `scripts/bridge/*.cs`, and toast message definitions.
Exclude code comments and variable names — check only quoted string content.
False positive resolution: add to an allowlist file
(`docs/design/coverstory_allowlist.txt`) with justification.

**QA process:** Before any release build, run the cover-story lint. Any
violation is a blocker. The cost of one leaked term ("fracture drive" in a
tooltip) is the entire Module revelation landing flat.

---

## Epilogue System

After the player commits to an endgame path (Reinforce, Naturalize, or
Renegotiate), a 60-90 second epilogue montage shows the consequences — not of
the path chosen, but of the **paths not chosen.** This is the New Vegas ending
slides principle: the player sees the full cost of their decision.

### Epilogue Structure

**Format:** Sequence of 4-5 text cards over a slowly zooming galaxy map.
Ambient music only — no narration, no player input. Duration: 45-90 seconds.

**Reinforce epilogue — "The Cage Holds":**
> "The threads stabilize. Commerce resumes. The Concord broadcasts
> 'All-clear' on every channel."
>
> "The frontier factions you never contacted continue their isolation.
> The Communion's research sites go dark, one by one."
>
> "The fracture module in your cargo hold grows cold. Whatever it was
> becoming, it has stopped."
>
> "The galaxy is safe. The galaxy is contained. The question the Communion
> asked — was the instability something more than physics? — will not be
> answered in your lifetime."

**Naturalize epilogue — "The Cage Opens":**
> "The Haven's accommodation geometry propagates. New stable zones form
> in what was once drift space. Traders venture beyond the threads."
>
> "The Concord's monitoring systems detect the gap you left. New trade
> dependencies form — less elegant, more fragile. Three stations lose
> supply lines in the first cycle."
>
> "The Communion celebrates. Then grows quiet. They expected to feel the
> old connection return. It hasn't. Not yet."
>
> "The frontier is free. The cost is unknown. The threads still stand —
> but they are no longer the only way."

**Renegotiate epilogue — "The Question":**
> "Your mapped corridors transmit data to every faction simultaneously.
> The instability is visible now — not as threat, but as structure."
>
> "The factions you excluded from your data discover the instability on
> their own terms. Chaotically."
>
> "The Communion elder who told you the truth sends one final message:
> 'You chose the answer none of us considered. We don't know what
> happens next. Neither do you. That's the point.'"
>
> "The galaxy does not stabilize. It does not collapse. It changes.
> And for the first time in millions of years, the change is observed."

### Design Rule

The epilogue must make the player feel the weight of their choice without
punishing them. Every path has real losses. No path is "the good ending."
The epilogue's job is to ensure the player thinks about their choice after
the credits roll.

---

## Failure State Narratives

Player death or bankruptcy should not feel like a game-over screen — it should
feel like the end of a story. The failure screen transforms frustration into
narrative closure by honoring the journey, not just marking its end.

### On Death (Ship Destroyed)

> "The *{ship_name}*'s final transmission was logged by {nearest_faction}
> monitoring systems. Among the wreckage: {trade_count} trade records,
> {discovery_count} discoveries, and one module of unknown origin that
> defied salvage analysis."

### On Bankruptcy (Credits Depleted)

> "The Concord's debt recovery office processed another closure.
> {player_name}'s trade license was revoked after {tick_count} cycles.
> The *{ship_name}* was impounded at {last_station}. Its unusual module
> was cataloged as 'anomalous hardware' and placed in deep storage."

### Journey Statistics Display

Both failure screens show:
- Systems visited (out of total)
- Discoveries made (with phase breakdown)
- Credits earned (lifetime)
- Factions encountered (with highest reputation)
- Fleet ships commissioned / lost
- Time played

### Design Rule

Failure is not punishment — it is the end of a particular story. The
statistics honor the player's time. The narrative framing (the module
"defied salvage analysis," the module was placed in "deep storage") hints
that the story continues without this particular pilot — someone else will
find the module eventually. This is the Communion elder's truth made
structural: "You're not the first."

---

## Named Fleet Ships — The Homeworld Principle

(Cross-reference: `ship_modules_v0.md`, `AutomationPrograms.md`)

Every fleet ship should have:

- **A name** (procedurally generated from a faction-appropriate name list)
- **A commissioning date** (tick number)
- **A service record** (trade runs completed, combat engagements survived,
  goods delivered, systems visited)
- **A destruction log** (if destroyed: when, where, by whom, what was lost)

The Programs tab (`AutomationPrograms.md`) should reference ship names, not
generic "Fleet 1." "The *Argent Crossing* completed Trade Charter Sirius→Proxima:
+340 cr" is more narratively engaging than "Fleet 1 completed Trade Charter."

When a ship is destroyed:
- **Toast notification with ship name:** "The *Argent Crossing* destroyed at
  Node 17 by Valorin patrol."
- **Service record preserved** in a "Lost Ships" section of the Fleet tab.
- **Crew loss implied** (ship had a named captain from commissioning).

This is the Homeworld principle: persistent units accumulate history, and
history creates attachment. The DeMartino observation: players restart missions
to save cryo trays with no mechanical penalty, because narrative investment
transcends mechanical incentive.

---

## Faction Voice — Communication Register Guide

Each faction should have a distinct **communication register** — the way their
text reads. This isn't accent or dialect; it's the underlying assumptions
embedded in how they talk.

| Faction | Register | Example Market Message |
|---------|----------|----------------------|
| **Concord** | Bureaucratic, reassuring, information-controlled. Passive voice. Euphemisms for bad news. | "Market conditions have been adjusted to reflect current regional optimization parameters. Thank you for your continued participation in Concord commerce." |
| **Chitin** | Probabilistic, hedged, bet-structured. Everything framed as odds. | "Current spread on Rare Metals: 3.2:1 against delivery within the cycle. We offer 1.8:1 if you carry. Interested?" |
| **Weavers** | Patient, structural, tension-aware. Metaphors of load-bearing, weaving, waiting. | "The route holds. Load capacity remains within tolerance. We have positioned supplies at the junction and will wait for the pattern to bring buyers." |
| **Valorin** | Direct, energetic, informal. Short sentences. Clan/family references. | "Need alloys. Three clans moved past the rim last cycle — can't smelt enough to keep up. You bring, we pay. Fast." |
| **Communion** | Experiential, present-tense, sensory. Descriptions of what things *feel like*. | "The shimmer-boundary shifted last night. We felt it in the hull before the instruments registered. The crystals grew differently this morning. Something is changing." |

**The Lattice** (when interacted with via Fragment 6) should have no natural
language at all — only **data patterns** that the player's instruments
translate. Authentication sequences. Maintenance schedules. The Lattice doesn't
communicate; it executes protocols. When it responds to the fracture module
with *recognition* rather than alarm, the significance is in what DOESN'T
happen (no defensive response) rather than what does.

---

## Narrative Integration Points Across Design Docs

This section maps specific narrative opportunities to existing systems defined
in other design documents.

### AudioDesign.md

| Existing System | Narrative Enhancement |
|----------------|---------------------|
| Star class ambient tinting | Instability phases should override star class ambient: Drift space should have a low resonant hum regardless of star class. The hum deepens with phase. |
| Warp transit silence gap (0.5s) | First arrival at an unstable system should have a LONGER silence gap (1.5s) — the player's senses reset before encountering something wrong. |
| Combat music crossfade | Lattice drone encounters should have unique music — not standard combat. Something mechanical, ancient, mournful. These aren't enemies; they're broken maintenance workers. |
| Discovery phase audio | Adaptation Fragment discoveries should have a distinct audio signature — not the standard "analyzed" fanfare but something resonant, harmonic, as if the fragment is responding to the module. |

### ExplorationDiscovery.md

| Existing System | Narrative Enhancement |
|----------------|---------------------|
| Discovery family templates (Derelict/Ruin/Signal) | Add "Ancient" family: accommodation geometry sites that only appear in Phase 2+ space. These use the conversation-format data logs. |
| Knowledge graph connections | Adaptation Fragment discoveries should create gold connections (unique color) that form a separate sub-web within the Knowledge Graph — the "ancient mystery" thread visually distinguished from faction/contemporary discoveries. |
| Narrative chaining | The three-step chain (Kepler Derelict → Altair Signal → Deneb Ruin) should culminate in chains that point toward thread-builder sites. Contemporary mystery chains lead to ancient mystery chains. |

### RiskMeters.md

| Existing System | Narrative Enhancement |
|----------------|---------------------|
| Trace meter (stealth risk) | Trace should be recontextualized at the Module revelation: before revelation, Trace is "the authorities notice you." After revelation, Trace is "the Lattice detects your accommodation signature" — the same mechanic, new narrative frame. |
| Cross-meter compound effects | "All three High+" should trigger faction-specific responses that hint at deeper lore: Concord sends an intelligence operative who offers to "help you disappear" (recruitment into the suppression program). |

### CombatFeel.md

| Existing System | Narrative Enhancement |
|----------------|---------------------|
| Enemy destroyed event | Lattice drone destruction should feel different from faction ship destruction: no explosion, just a power-down hum and the drone going dark. They're not alive. They're infrastructure failing. |
| Shield break flash | Accommodation-geometry shields (Haven, ancient sites) should have a distinct visual: instead of blue-white flash, a geometric pattern — hexagons → pentagons → irregular tessellation. The shield is adapting, not failing. |

### HudInformationArchitecture.md

| Existing System | Narrative Enhancement |
|----------------|---------------------|
| Toast notifications | Discovery-related toasts should use Purple/Light coloring to distinguish them from economic/combat toasts. The player should be able to glance at a toast and know "this is about the mystery" before reading it. |
| Tier 2 contextual info | When in Drift+ space, a new Tier 2 element: "Metric Stability" indicator showing local spacetime consistency. This is ambient narrative — the number tells the story of where you are. |

### AutomationPrograms.md

| Existing System | Narrative Enhancement |
|----------------|---------------------|
| Program stall/failure messages | Warfront-caused failures should include one-line faction context: "EMBARGO_ACTIVE: Weaver embargo on Composites — retaliation for Chitin Electronics blockade. Pentagon ring disrupted at link 2." |
| ProgramExplain snapshot | Include fleet ship name in explain output. "The *Wayward Sun* completed Trade Charter: +340 cr" — the ship is a character in the automation narrative. |

### EmpireDashboard.md

| Existing System | Narrative Enhancement |
|----------------|---------------------|
| Overview tab — Needs Attention queue | Attention items should carry narrative weight. Not "Fleet stalled (no ore)" but "Fleet *Argent Crossing* stalled: Sirius ore depleted by Valorin warfront demand. Reroute to Barnard (+45t)? [Yes] [Edit Route]." The queue IS the player's daily briefing — it should read like one. |
| Intel tab (F6) — market freshness | Intel age communicates narrative time. Stale intel on a Communion station should feel ominous: "Last contact: 340 ticks ago. Station status: Unknown." Freshness decay IS the fog of war — it tells the player the galaxy moves without them. |
| Overview summary cards — trend arrows | Trend arrows are micro-narratives. Economy ↓ during a warfront escalation tells a story. Research stalled tells a different one. The Overview tab should function as a newspaper front page — 6 headlines, each implying a deeper article (the sub-tab). |
| Factions tab (F7) — rep bars | Reputation bars should show recent events that changed rep: "+2 Concord (sold Munitions at Sol, Tick 847)" / "-1 Chitin (sold Munitions to their enemy)." Rep changes are narrative choices made visible. The player's faction history is their character arc. |
| Warfronts tab — faction warfare display | Warfront summaries should read as intelligence briefings. Not "Valorin vs Weavers: Intensity 3" but "Valorin frontier offensive, 3rd cycle. Contested nodes: Wolf, Barnard, Kepler. Weavers requesting Composites shipments. Munitions premium: +180%." The warfront tab IS the war correspondent's desk. |
| Fleet tab (aspirational) — ship list | When implemented, Fleet tab should show named ships with service records. Each ship entry is a character sheet: name, age, trade runs, combat survivals. Destroyed ships preserved in "Lost Ships" memorial section. This is the Homeworld fleet roster — persistent units the player develops attachment to. |

### GalaxyMap.md

| Existing System | Narrative Enhancement |
|----------------|---------------------|
| Star class visual language (O through M) | Star colors map to narrative zones. Warm gold G-types = home, safety, Act 1 comfort. Cold blue O/B-types = industrial, power, faction strongholds. Deep red M-types = frontier, danger, where derelicts and mysteries live. The color gradient IS the story gradient — safe→dangerous, known→unknown. |
| Fog of war / exploration states | Three fog states are three narrative states: black (unknown = "what's out there?"), desaturated (stale = "things have changed since I was there"), bright (fresh = "I know what's happening here"). The fog IS the player's ignorance made visible. Dramatic fog contrast rewards exploration viscerally. |
| Security overlay (green→red thread colors) | Security coloring tells the story of factional control. A thread that was green and turns orange is a narrative event — the warfront shifted, patrols thinned, this route got dangerous. Overlay changes over time ARE the war story told through cartography. |
| Intel Freshness overlay | Freshness decay creates the "war correspondent" narrative: the player must re-visit to re-learn. A cluster of red (stale) nodes near a warfront implies "something happened there and I don't know what." Freshness is narrative uncertainty made spatial. |
| Trade Flow overlay (gold/gray threads) | Active trade routes (gold) are the player's economic biography drawn on the map. Seeing your routes light up across the galaxy should feel like seeing your empire's circulatory system. Gray threads adjacent to your gold ones are narrative opportunities: "I haven't explored that connection yet." |
| Node detail popup | Popups should carry faction voice when showing territory info. A Concord-controlled node popup might say "Concord Commerce Zone — tariff schedule in effect." A Communion waystation popup: "Waystation Kell — supplies low, requesting assistance." The map speaks with the voice of whoever controls the space. |
| Faction territory discs | Territory visualization IS the political map. Territory changes (disc color shifting as factions gain/lose nodes) should be among the most narratively significant visual events in the game. A Valorin disc appearing at a previously Weaver node = the frontier moved. This is the Stellaris moment: watching borders shift on the galaxy map. |

### camera_cinematics_v0.md

| Existing System | Narrative Enhancement |
|----------------|---------------------|
| Warp arrival flyby (Euler spiral orbit) | First arrival at a new system is a narrative moment. The full 270° sweep + letterbox is correct — it says "you've never been here before, take it in." Return visit 60% sweep says "you know this place." The camera IS the narrator: its behavior communicates familiarity vs. novelty. |
| Star class visual spectacle during orbit | The flyby reveals the star's personality. A red dwarf flyby should feel cold and isolated. A blue giant flyby should feel awe-inspiring and industrial. Vary orbit altitude and speed by star class: slower, closer orbits for dramatic M-types; wider, faster sweeps for familiar G-types. |
| Phase 5 handoff (camera settles to flight mode) | The settle phase is the "now you're here" transition. At narrative milestone systems (Haven, first ancient site, the system where the Module was found), the settle should be slower — the camera lingers, letting the player absorb where they are. A 0.5s longer settle at narratively significant locations. |
| Letterbox overlay on first visits | Letterboxing is a cinematic grammar the player understands: "this is a moment." Reserve letterboxing for: first visits, revelation beats (arriving at a system where a major discovery awaits), and entering the Haven. Never letterbox on routine returns — it would dilute the signal. |

### dynamic_tension_v0.md

| Existing System | Narrative Enhancement |
|----------------|---------------------|
| Player Experience Arc (Scramble→Endgame) | Each phase transition should be narratively marked. Not just difficulty shifting — the player's relationship with the world changes. Scramble→Establish: "I understand the rules." Establish→Revelation: "The rules can be broken." Revelation→Choose: "The rules were a lie." Choose→Commit: "I'm choosing new rules." Commit→Endgame: "The galaxy follows my rules or collapses." |
| Warfront economic cascades | War cascades are narrative events, not just price changes. When a warfront causes a Munitions spike that starves Components production, the player should be able to trace the causal chain in the Economy tab. "Munitions +180% (Valorin offensive) → Metal diverted → Components -40% → 3 programs degraded." The cascade IS the story of interconnected civilization under stress. |
| The Shrinking Middle (neutrality tax) | Neutrality erosion is the game's central political narrative. Each tariff increase should feel like a door closing. "Concord neutrality surcharge increased to 10% — open war declared against Chitin. Estimated annual cost: 2,400 cr." The numbers tell the story: staying in the middle is getting expensive. Eventually one number — one specific tariff, one specific embargo — becomes the straw that breaks neutrality. That moment should be memorable. |
| Fracture Temptation (dual doom clock) | The Fracture temptation is the game's Faustian narrative. Each use should feel like a conscious bargain: "This jump solves my immediate problem (warfront blocking my trade route) but creates a future one (Trace accumulation toward interdiction)." The UI should make both sides of the bargain visible simultaneously: the profit gained AND the Trace cost, in the same toast. |
| Replayability through seed variation | Different seeds tell different stories. A seed where the player starts near the Valorin-Weaver warfront is a story about military supply and frontier danger. A seed near the Concord-Chitin cold war is a story about political maneuvering and embargo navigation. The galaxy generator IS the story generator — different topologies create different narrative pressures. |

### trade_goods_v0.md

| Existing System | Narrative Enhancement |
|----------------|---------------------|
| Three branches from Metal (Offensive/Defensive/Economic) | The Metal fork is a narrative choice disguised as an economic one. "Metal + Fuel = Munitions" means "I chose to supply war." "Metal + Organics = Composites" means "I chose to protect." "Metal + Electronics = Components" means "I chose to grow." The player's production chain IS their political statement. Factions should notice and comment on which branch a player favors. |
| The Organics Fork (Food vs. Composites) | The butter-vs-guns decision is the game's most human narrative moment. An agri-system choosing Food keeps people alive. Choosing Composites keeps fleets armored. When a Communion station is starving and the player routes Organics to Composites production instead, that should FEEL like a moral choice — not just a production optimization. NPC dialogue at Communion stations should reflect this: "The convoys are carrying armor plating. Not food. We notice." |
| Exotic Matter (T3 sustain) | Exotic Matter is the game's narrative endgame resource. Your Dreadnought with 3 Relic modules "literally runs on alien artifacts you have to keep finding." This is the mechanical expression of the ancient mystery — the player's endgame power depends on understanding (and finding) things the thread builders left behind. Running low on Exotic Matter should feel existential, not just inconvenient. |
| Fracture-exclusive goods (Exotic Crystals) | The fact that the entire tech chain depends on Exotic Crystals — which only exist in fracture space — is itself a narrative. Thread civilization's most advanced technology requires materials from the space it fears. This irony should be surfaced in faction dialogue: "Your electronics run on crystals from drift space. You know that, right? Concord doesn't like to talk about where their sensor arrays come from." |
| Warfront demand shocks (Munitions 4x) | Warfront demand spikes are narrative pressure expressed as numbers. "Munitions 4x at contested nodes" means the war is hungry. The price of Munitions at a besieged station IS the story of that siege. When prices normalize, the ceasefire arrived. When they spike again, the front reopened. Price history charts are war diaries. |

### ship_modules_v0.md

| Existing System | Narrative Enhancement |
|----------------|---------------------|
| Three tech tiers (Standard/Military/Relic) | Tech tiers are narrative tiers. T1 = "the galaxy as it presents itself." T2 = "the galaxy as factions have shaped it" (locked behind reputation, requiring faction trust). T3 = "the galaxy as it actually is" (ancient tech that challenges everything you thought you knew). Each tier transition recontextualizes the player's understanding of technology. |
| T3 Relic modules (discovery-only) | Relic modules are narrative artifacts, not just stat upgrades. Each should come with a data log fragment (conversation format). The module's in-game tooltip should include a line of ancient text: "Kesh's accommodation lattice prototype — 'the material remembers what the field was, not what it is.'" This transforms every T3 module from "better stats" to "a piece of the mystery I can install on my ship." |
| Zone armor system (4 directional zones) | Zone damage tells combat micro-stories. A ship with depleted aft armor and intact fore tells the story of a fighting retreat. A ship with depleted port tells the story of a broadside exchange. The Fleet tab's ship detail view should show zone damage history as narrative: "*Argent Crossing*: aft armor rebuilt twice (Tick 203 Valorin ambush, Tick 567 Chitin raid)." |
| Combat stances (Charge/Broadside/Kite) | Stances are tactical personalities. Charge = aggressive, closing to kill. Broadside = measured, trading fire. Kite = cautious, running while fighting. The stance a player's fleet uses tells a story about their combat philosophy. Fleet ships could develop "preferred stances" based on combat history — a ship that's survived 10 kite engagements "prefers" kiting. |
| Module sustain (goods consumption per cycle) | Sustain consumption is the mechanical expression of "your empire has a metabolism." Fielding T2 modules means your fleet eats Composites and Rare Metals every 60 ticks. This creates supply chain dependencies that ARE narrative dependencies: "I can't field my best ships unless I maintain my Rare Metals trade route through Valorin space — and the Valorin are at war." The module loadout IS the supply chain story. |

### factions_and_lore_v0.md

| Existing System | Narrative Enhancement |
|----------------|---------------------|
| Pentagon dependency ring | The dependency ring is the game's deepest political narrative. Each faction needs something from a faction it philosophically opposes. This should be surfaced explicitly at high reputation: "You've been trading with us for 300 ticks. You should know — we cannot produce Composites without Weaver materials. And the Weavers cannot produce Electronics without Chitin precision tools. We are all connected, even those of us at war. Especially those of us at war." |
| 12 Adaptation Fragments + 6 Resonance Pairs | Fragment discovery should trigger Discovery Web connections that gradually reveal the thread-builder civilization's internal debate. The 6 Resonance Pairs (Containment debate, Measurement problem, etc.) are 6 conversations. Each pair, when assembled, should unlock an ancient data log conversation between two scientists arguing about that exact topic. The fragments ARE the footnotes; the logs ARE the text they reference. |
| Three endgame paths (Reinforce/Naturalize/Renegotiate) | The endgame should emerge from accumulated play, not a dialog choice. The path available to the player should depend on which factions they allied with, which fragments they found, and what they understand. A player who never visited Communion space cannot Renegotiate — they don't have the knowledge. A player who never found Fragment 6 (Lattice authentication) cannot Reinforce — they can't operate the infrastructure. The endgame is a knowledge test disguised as a moral choice. |
| Haven Starbase (ancient safe harbor) | The Haven is the game's emotional anchor point. The first visit should be one of the Silence Principle moments (music fades, warm ambient hum). The Haven should feel like coming home to a place you've never been — ancient, stable, designed for beings like you but not by beings like you. It is the one place in the game where the ambient narrative shifts from tension to safety. Every subsequent Haven visit should feel like relief after danger. |
| Instability Phases (Stable→Void) | Each phase transition at a system should be a narrative event the player can witness. Shimmer arriving at a previously stable system = "something changed here." The galaxy map should show phase spread over time — systems near fracture activity gradually shifting from Stable to Shimmer to Drift. The player can WATCH the consequences of fracture travel spreading across the map. This is the doom clock made spatial. |

### MainMenu.md

| Existing System | Narrative Enhancement |
|----------------|---------------------|
| Title screen foreground silhouette | The foreground element adapts to game state: a lone gate (no saves = the beginning), the player's ship class (mid-campaign = your ship is waiting), Haven starbase (completed = you've been here before). The title screen IS act zero — foreshadowing without spoiling. |
| Rotating subtitle quote | A different ancient fragment quote each session. The player accumulates these across sessions — mystery through repetition and variation. Never show the same quote twice until all are exhausted. The menu teaches the player to read thread-builder text before they find their first fragment. |
| Galaxy generation messages | "Seeding fracture topology..." / "Igniting warfronts..." — generation text is world-building, not loading feedback. The player reads lore while the galaxy is born. These messages should reference systems from `factions_and_lore_v0.md` without spoiling specifics. |
| Difficulty descriptions | Written in-universe, not in game-design language. "The galaxy is indifferent to your survival" (Standard) carries narrative weight that "Normal difficulty" does not. The difficulty selection IS the player's first narrative choice — what kind of story am I signing up for? |
| First-launch silence (2s void) | The AudioDesign.md "void, then you're alive" principle starts at the main menu, not at tick 1. The very first thing a new player experiences is silence and darkness. Then a single note. Then stars. The menu IS the creation myth. |
| Milestones screen | Milestone names ("First Trade," "Pathfinder," "Tycoon") use pilot-fantasy language, not achievement-game language. The milestones screen is a captain's log, not a trophy case. Lifetime stats across voyages tell the meta-story of the player's career. |
| "New Voyage" naming | "Voyage" instead of "Game" — reinforces the pilot identity. The player embarks on voyages. The save slots are "Voyage 1," "Voyage 2." The language of the menu is the language of the world. |

---

## Narrative Gaps and Priorities

### Critical (Required for Story to Work)

| Gap | Impact | Notes |
|-----|--------|-------|
| **No ancient data logs** | The ancient mystery has no voice. Thread-builder scientists are concepts, not characters. | Write 20-30 conversation-format logs featuring 5 named scientists with personal contradictions. Distribute across ancient discovery sites. |
| **No Discovery Web UI** | Player cannot see connections between discoveries. Lore fragments feel isolated. | Implement Knowledge Graph in Intel/Explore tab (ExplorationDiscovery.md aspirational design exists). |
| **No faction dialogue at stations** | Factions have personality in design docs but are silent in-game. Player has no relationship with them. | Station dock menu should have a "Comms" or faction message area showing faction-voiced text. |
| **No First Officer system** | **#1 NARRATIVE PRIORITY.** Without the FO, all five recontextualizations land as information rather than story. The FO transforms "the economy is a cage" from a fact into a moment. The FO is the player's emotional proxy — the character who reacts to revelations the way the player feels. | Implement FO candidate selection (3 archetypes: Analyst, Veteran, Pathfinder). FO reactive lines at all 10 milestone moments (~30 lines total, authored in `NarrativeContent_TBA.md`). FO must be present before ANY revelation triggers. See `factions_and_lore_v0.md` → "The First Officer." |
| **No revelation triggers** | The five paradigm shifts have no implementation path. | Define specific triggers: R1 (module age evidence), R2 (Concord rep threshold), R3 (fracture-space trade breaks ring pattern — **#1 PRIORITY**), R4 (Communion max rep), R5 (endgame fragments + void site data). R3 and R4 are gameplay-triggered, not text-triggered. |

### Important (Enriches Story Significantly)

| Gap | Impact | Notes |
|-----|--------|-------|
| **No fleet ship names** | Fleet automation feels mechanical, not personal. | Add procedural name generation + service record tracking. |
| **No environmental scenes at discovery sites** | Discoveries are loot drops, not narrative moments. | Design 10-15 "tableaux" scenes for each instability phase. |
| **No metric instability UI feedback** | Drift/Fracture space doesn't FEEL different mechanically. | Manifest discrepancy display, instrument disagreement, travel time variance. |
| **Silent discovery phases** | Phase transitions have no audio/visual celebration. | Connect discovery milestone audio (AudioDesign.md already specifies sounds). |
| **No Communion personal relationship** | The faction designed to "remember you" has no dialogue system. | Priority for faction dialogue implementation. |

### Nice-to-Have (Polish Layer)

| Gap | Impact | Notes |
|-----|--------|-------|
| Ancient-scale structures visible in Phase 2+ | Environmental awe, sense of scale | Background 3D objects at extreme distance in unstable systems |
| Faction communication register variation | Faction identity through text style | Per-faction message templates with distinct voice |
| Haven ambient audio | Haven should feel emotionally distinct | Warm, safe, ancient — a unique soundscape |
| Fleet ship destruction emotional beat | Loss should sting | Named ship + service record in death notification |
| Trade goods with faction-specific origin labels | Carrying goods = carrying story | "Valorin-mined Rare Metals" vs generic "Rare Metals" |

---

## Narrative Pacing Timeline

This timeline maps when each narrative layer peaks, which story beats fire, and
what the player's emotional state should be at each phase. Cross-references the
Player Experience Arc from `dynamic_tension_v0.md`.

### A Note on Time Units

**The primary axis is hours of player time, not ticks.** The sim currently runs
at 10 ticks/second (TickDelayMs=100), but the player spends most of their real
time NOT advancing the sim — flying in real-time, reading menus, managing fleets,
exploring. The tick-to-real-time ratio is undetermined and will change as
gameplay loops mature. A "15-30 hour campaign" might involve anywhere from
5,000 to 50,000 ticks depending on pacing tuning.

**Rule:** Narrative beats should be gated by **player progression milestones**
(systems visited, reputation thresholds, fragments found, technologies
researched) — not by tick count. Tick counts in `dynamic_tension_v0.md` are
illustrative estimates, not triggers.

### Phase-by-Phase Narrative Map

```
HOURS   PHASE          NARRATIVE LAYERS ACTIVE                     KEY STORY BEATS
────────────────────────────────────────────────────────────────────────────────────
0       SCRAMBLE       Ambient ████                                "Where do I trade?"
        │              Economic ██                                 Galaxy at war from minute 1
0.5     │              Discovery ░                                 First NPC encounters
        │                                                          Faction voices at stations
1       │              Ambient ████                                Learn market patterns
        │              Economic ███                                Feel warfront in prices
2       ├─ ESTABLISH ─ Ambient ████                                "My routes are working"
        │              Economic ████                               Automation programs running
3       │              Discovery ██                                First scan/analyze cycle
        │              Faction ██                                  Reputation building
        │              Discovery ███                               Derelict/ruin encounters
4       │              Faction ███                                 Faction intel briefings begin
        ├─ REVELATION  Economic ████                               ┌─ SRE MODULE FOUND ───────┐
        │              Discovery █████                             │ First SRE transit          │
5       │              Faction ████                                │ Trace meter activates      │
        │              Ambient ████ (instability effects begin)    │ Haven discovered           │
7       ├─ CHOOSE      Economic █████                              ┌─ REVELATION 1: ──────────┐
        │              Discovery █████                             │ "It's Not a Drive"        │
        │              Faction █████                               │ Module predates the threads  │
        │              Revelation ███                              │ "SRE" → Adaptation Drive   │
        │                                                          └────────────────────────────┘
9       │              All layers at high intensity                Neutrality tax biting
        │                                                          Faction contracts offered
11      │              Economic █████ (cascading)                  Concord intel briefings
        │              Discovery ██████ (fragments accumulating)   Discovery Web filling in
13      ├─ COMMIT      All layers ██████                           ┌─ REVELATION 2: ──────────┐
        │              Revelation █████                            │ "The Threads Aren't       │
        │                                                          │  Protection"               │
        │                                                          │ Concord suppression exposed │
        │                                                          └────────────────────────────┘
15      │              Economic ███████ (cascading failures)       ┌─ REVELATION 3: ──────────┐
        │              Faction ██████ (alliances tested)           │ "The Economy Is a Cage"   │
        │                                                          │ Pentagon ring is engineered │
        │                                                          │ (discovered via gameplay)   │
        │                                                          └────────────────────────────┘
18      │              Discovery ██████ (final fragments)          ┌─ REVELATION 4: ──────────┐
        │              Faction ███████ (Communion truth)           │ "You're Not the First"    │
        │                                                          │ Communion elder tells truth │
        │                                                          │ Module adapting the pilot   │
        │                                                          └────────────────────────────┘
20      ├─ ENDGAME     All layers maximum                          ┌─ REVELATION 5: ──────────┐
        │              Revelation ████████                         │ "Instability Is Not       │
        │                                                          │  Entropy"                  │
        │                                                          │ The ambiguity. The choice. │
        │                                                          │ Were my choices my own?     │
        │                                                          └────────────────────────────┘
20-30   │              Resolution                                  Endgame path executed
        └──────────────────────────────────────────────────────────────────────────────────────
```

### Layer Intensity Rules

| Layer | Early (Hours 0-4) | Mid (Hours 4-13) | Late (Hours 13-30) |
|-------|-------------------|-------------------|---------------------|
| **Ambient** | High from minute 1. Wars, prices, NPC behavior. The world speaks through its state. | Instability effects join ambient layer. The world feels increasingly unstable. | Full ambient pressure. Instability visible everywhere. Galaxy feels like it's holding together by threads. |
| **Economic** | Moderate. Learning prices, building routes. "How do I profit?" | High. Warfront cascades, embargo disruptions, supply chain breaks. "How do I survive?" | Maximum. Everything is connected, everything is breaking. "Which fires do I fight?" |
| **Discovery** | Low. A few Seen discoveries, maybe one Scan. Seeding curiosity. | Rising. Fracture travel opens deep-space discoveries. Knowledge web begins forming. | High. Final fragments, final connections. The picture becomes clear. |
| **Faction** | Low. Factions are service providers. You trade, they react. | Rising. Factions become political actors. Neutrality costs money. Alliances form. | Maximum. Factions react to the crisis. Each offers their answer. You're forced to choose. |
| **Revelation** | Zero. The rules are solid. The world makes sense. (This confidence is structural — it's being built to be broken.) | Emerging. The fracture module challenges assumptions. Cracks in Concord's story. | Maximum. The paradigm shifts. Everything the player assumed is reframed. |

### Pacing Principle: The Silence Between Beats

Not every minute should advance the narrative. Long stretches of pure economic
gameplay (running routes, building automation, upgrading ships) serve the story
by creating baseline comfort that makes disruption meaningful. If narrative
events fire every 5 minutes, none of them feel special. If a narrative event
fires after 30 minutes of quiet trade, it feels like the world changed.

**Target cadence (in real player time):**
- Ambient narrative: continuous (always present)
- Economic narrative events: every 5-15 minutes (trade stalls, price shifts)
- Discovery milestones: every 15-30 minutes (new scan, new analysis)
- Faction narrative beats: every 30-60 minutes (rep threshold, intel briefing)
- Revelation beats: 3 total in a 15-30 hour campaign (hours apart, back-loaded)

### Mid-Game Narrative Density (Hours 3–8)

The mid-game is the narrative's most vulnerable period. The player has exhausted
early-game novelty but hasn't reached the first major revelation. Without
intervention, hours 3–8 become "fly to system, trade, fly to next system" —
the exploration loop loses its curiosity hook and the economy loop hasn't yet
developed dramatic cascades.

**Design response — four density layers:**

1. **Jump event frequency increases in frontier space.** Every 3rd jump in
   near-frontier systems triggers a micro-narrative: scanner anomaly, distress
   signal fragment, faction patrol encounter, thread turbulence. See
   `ExplorationDiscovery.md` → Discovery Density Rules for full frequency tables.

2. **Discovery sites seeded densely 2-3 hops from starter space.** 60% of
   near-frontier systems contain discoveries (vs 25% in outer reach). This
   ensures the scan/analyze loop has frequent targets during the exploration
   ramp-up. The player should never go more than 2 jumps without seeing a
   discovery marker.

3. **Faction reputation milestones unlock new dialogue every 45-60 minutes.**
   Station greetings change at each rep tier (see `NarrativeContent_TBA.md` →
   Faction Station Greetings). Intel snippets unlock at Friendly tier. Named
   NPC encounters begin at Allied tier. The social layer provides narrative
   variety between trade runs.

4. **First Officer unprompted observations.** The FO comments on trading
   patterns, foreshadowing the pentagon revelation: "Three runs between Concord
   and Valorin space. The margins are identical every time." These observations
   fire at trade-count milestones (5th trade, 10th trade, first multi-faction
   route) — not on a timer. The FO notices what the player does, not what
   the clock says.

**Anti-drought rule:** No player should experience more than 10 minutes of
active play without encountering SOME narrative touchpoint (discovery site,
jump event, faction dialogue, FO observation, or economic cascade notification).
If route analysis predicts a drought on the player's current heading, seed a
jump event to bridge the gap.

---

## Narrative Delivery Checklist for Implementers

When implementing any system that touches the player's experience, run through
this checklist. It's derived from the 12 Principles and ensures narrative
considerations are not afterthoughts.

### Before Writing Code

- [ ] **Which narrative layer does this system serve?** (Ambient / Economic /
  Discovery / Faction / Revelation) If "none," ask whether the system should
  have a narrative dimension. Most systems should.

- [ ] **Does this system have voice?** Who is "speaking" through this system's
  text output? Market data speaks in numbers. Faction stations speak in their
  register. Automation programs speak in clinical professionalism. The Discovery
  Web speaks through connections, not sentences. Toasts speak in urgency tiers.
  Identify the voice before writing any strings.

- [ ] **Does this system show, or does it tell?** A price spike IS the story of
  a warfront escalation. A Needs Attention entry IS the story of a supply chain
  failing. If the system would need a separate tooltip to explain "why this
  matters," the system's primary display needs redesign.

### During Implementation

- [ ] **Three-channel feedback for significant events.** Any event the player
  should notice must register on at least two of: visual, audio, UI text. A
  price change is UI text + toast. A discovery milestone is visual + audio +
  toast + Discovery Web update. A revelation beat is visual + audio + UI + camera.

- [ ] **Faction voice consistency.** All text that the player reads should come
  from an identifiable source. Market prices = neutral data. Station messages =
  faction-voiced. Fleet reports = clinical professionalism. Ancient data logs =
  conversation format. Mixed register is a design bug.

- [ ] **Progressive disclosure respected.** New information should appear at the
  correct tier (Tier 1 glanceable / Tier 2 readable / Tier 3 studyable). A
  ancient data log is Tier 3 (dedicated panel). A risk meter threshold is
  Tier 2 (contextual toast). Hull HP is Tier 1 (always visible). Information
  at the wrong tier is either invisible or clutter.

- [ ] **The "so what?" test.** Every number displayed must answer "so what?"
  within its context. Not "Tariff: 12%" but "Tariff: 12% (Concord war
  surcharge, expires at ceasefire)." Not "Program stalled" but "Program stalled:
  Sirius ore depleted by warfront demand. [Reroute]." If the player must
  navigate to another screen to understand the significance, the display has
  failed.

### After Implementation

- [ ] **Does this feel like participating in the story, or pausing it?** The
  diagnostic question from Principle 2. If using the system feels like "I left
  the game to do management," the system needs a narrative skin — faction voice,
  world-state context, consequence visibility.

- [ ] **Restraint check.** Is the text too much? The default should be less text,
  not more. Clinical language, not flavor text on every button. Save emotional
  language for threshold moments. If every message is dramatic, nothing is
  dramatic.

- [ ] **Silence check.** Does this system know when to be quiet? A system that
  fires notifications every tick creates noise. Define the system's silence
  floor — what is the minimum activity level below which it says nothing?

- [ ] **Connection check.** Does this system's output reference anything else in
  the game world? A stalled trade program should reference the warfront that
  caused it. A discovery should reference the faction territory it's in. A
  faction reputation change should reference the trade that triggered it.
  Isolated information is trivia. Connected information is narrative.

---

## Positioning & First-Impression Strategy

### The Positioning Challenge

Space Trade Empire faces a core positioning problem: it's a narrative-driven game
wearing a trading sim's clothes. Players who want trading will find a story;
players who want story may never try a trading game. Both audiences are served —
but only if they start playing.

The challenge is asymmetric: narrative players self-select OUT of trading games
faster than trading players self-select out of narrative games. A Starsector fan
who discovers a story is delighted. An Outer Wilds fan who sees "trade empire"
in the title never clicks.

### First 30 Minutes — Signal Strategy

The game must signal "there's a story here" within the first 30 minutes without
spoiling the story. These signals are planted in the opening gameplay:

1. **The module notification (minute 5):** "Module Status: Nominal. Note:
   Instrument calibration variance detected." This is meaningless on first read
   but plants the seed that something is being monitored. Players who revisit
   this notification after the Module revelation will realize the module was
   already adapting from minute 5.

2. **First Officer introduction (minute 10):** The FO's first line must be
   characterful and hint at depth, not tutorial instruction. Analyst: "I've
   been tracking the price patterns between here and {destination}. They're...
   unusually consistent." Veteran: "Another trade run. You know, I've seen
   this route a hundred times. Something about the margins never changes."
   Pathfinder: "Do you ever wonder why the threads go where they go? Not the
   engineering — the *why*."

3. **First discovery site (minute 15-20):** Guaranteed discovery within 2 hops
   of start. The scan result mentions "construction patterns that don't match
   any known faction." This is the first bread crumb — unremarkable alone, but
   it begins the curiosity chain. Players who skip it lose nothing mechanical.
   Players who investigate find a data log.

4. **First jump anomaly (minute 25-30):** A scanner glitch during a thread
   transit — numbers flicker, then stabilize. The FO comments: "Did you see
   that? Probably nothing." This is the first hint that the threads aren't
   perfectly stable. It costs the player nothing and tells them nothing — but
   it tells them the game is *watching*.

### Target Audience Mapping

**Primary:** Players who enjoyed Outer Wilds, Return of the Obra Dinn, or
Sunless Sea — narrative-exploration players willing to engage with systems.
These players value mystery and player-assembled meaning over hand-delivered
plot. They will play a trading game IF they know a story awaits.

**Secondary:** Trading sim fans (X4, Elite Dangerous, Starsector) who are
curious about *why* their economy works the way it does. These players already
love the genre — the story is a bonus that elevates a familiar loop.

**Tertiary:** Strategy/roguelike fans (FTL, Into the Breach, Stellaris) attracted
by the fleet management, automation, and procedural world layers. The faction
politics and warfront dynamics appeal to this audience.

### Store Page / Marketing Language

Lead with mystery, not mechanics. The store page must signal narrative depth
without spoiling the narrative:

**Do this:**
> "The trade routes work. The economy flows. Everything is perfectly balanced.
> You're starting to wonder why."
>
> "Build a trade empire. Manage a fleet. Discover why the galaxy runs like
> clockwork — and what happens when the clock breaks."

**Not this:**
> "Build a trade empire across 22 star systems with 5 factions and 34 ship
> modules! Manage automated trade routes! Discover ancient artifacts!"

The first version invites curiosity. The second describes a spreadsheet. The
game IS a spreadsheet — but it's a spreadsheet with a secret, and the secret
is what makes it worth playing for 30 hours.

### The "First Trade to First Mystery" Pipeline

The critical conversion funnel:

```
Trade tutorial (min 0-5) → "I understand how this works"
         ↓
Module notification (min 5) → "Huh, what was that?"
         ↓
FO introduction (min 10) → "This character is interesting"
         ↓
First discovery (min 15-20) → "There's something here..."
         ↓
First jump anomaly (min 25-30) → "This game is hiding something"
         ↓
Player is hooked → Story and trading reinforce each other
```

If any step in this pipeline fails (tutorial is boring, FO is generic, discovery
is missable, anomaly is too subtle), the narrative player bounces. If any step
is too heavy-handed (module notification says "WARNING: ANCIENT ORGANISM
DETECTED"), the mystery is spoiled. The calibration between "too subtle" and
"too obvious" is the single most important design challenge in the first 30
minutes.

---

## Mechanical Hooks for Lore Delivery

### The Problem

Lore that exists only for atmosphere creates two failure modes: lore-seeking
players feel unrewarded mechanically, and mechanic-focused players skip lore
entirely, missing the story. Both failures are solved by the same design:
**every piece of lore must contain at least one mechanical hook.**

### What a Mechanical Hook Is

A mechanical hook is information embedded in lore that is **useful for gameplay.**
Not "read this to feel immersed" but "read this to gain a tactical advantage."
The lore is still atmospheric — but it ALSO gives you something to act on.

### Hook Types

| Hook Type | Example in Lore | Gameplay Benefit |
|-----------|----------------|-----------------|
| **Coordinate hint** | "Calibration site gamma: readings correlate to Kepler-7, third orbital body." | Hidden resource cache at those coordinates |
| **Trade intelligence** | "The Chitin stopped shipping rare minerals through Altair after the incident. Smart captains avoid that corridor too." | Route optimization — avoid Altair for rare minerals |
| **Scanner calibration** | "Applying recovered calibration matrices from lattice node instrumentation. Estimated improvement: ±2% variance reduction." | Permanent scanner accuracy improvement |
| **Fragment location** | "This fragment resonates with signals detected in the Outer Reach. The paired fragment may lie within 3 hops of Vega." | Search area for resonance pair |
| **Faction insight** | "Weaver alloy markup through Concord trade hubs is 12-18% above direct purchase. The middleman takes their cut." | Price optimization — buy direct from Weavers |

### Implementation Rule

Every lore delivery system must enforce the mechanical hook requirement:
- **Ancient data logs:** Must contain at least one coordinate, price, or
  calibration data point
- **Faction dialogue:** Must contain at least one trade tip, route warning,
  or political intelligence item
- **Discovery analysis results:** Must include scanner calibration data or
  fragment location hints
- **Adaptation fragment descriptions:** Must hint at resonance pair locations

### The Virtuous Cycle

When lore contains mechanical hooks, two things happen:
1. **Lore-seekers are mechanically rewarded.** They gain tactical advantages
   from reading carefully. This validates their playstyle.
2. **Optimizers encounter lore as a side effect.** They read the data log
   because it mentions coordinates — and in the process, they learn about
   Kesh and Vael's argument. The story reaches them through the gameplay
   channel they already trust.

This is the Sunless Sea principle: stories ARE trade goods. Knowledge IS profit.
The mechanical and narrative loops are the same loop.

---

## Open Narrative Design Questions

These require player testing or further design iteration to resolve:

1. **How much ancient log text is too much?** Dark Souls uses 2-sentence item
   descriptions. Outer Wilds uses full conversational exchanges. STE's ancient
   logs are closer to Outer Wilds — but there's a risk of text fatigue in a
   game about trading, not archaeology. Test: do players read the logs, or
   skip them? If skip rate > 50%, compress to 3-line fragments.

2. **Should the endgame paths be announced?** New Vegas's ending slides reveal
   consequences. STE could show the three paths emerging on the Discovery Web
   as the player's alignment becomes clear — or it could keep them hidden until
   the crisis forces a choice. The first is more transparent (the player can
   optimize). The second is more dramatic (the player discovers their path).

3. **Does the Concord betrayal alienate players who allied with Concord?**
   If the player spent 12 hours building Concord reputation and then discovers
   the suppression, they might feel cheated rather than betrayed. The betrayal
   must feel like a real revelation that adds complexity, not a "gotcha" that
   punishes their faction choice. Concord should remain a viable ally AFTER the
   revelation — flawed but not evil. The player can choose to help Concord
   reform, not just abandon them.

4. **How does audio serve the revelation beats?** The Module revelation should
   have a distinct audio moment — but what? Silence + a new sound? A shift in
   the ambient score? The module's resonance changing pitch? This needs sound
   design experimentation.

---

## References

### Games Studied

| Game | Key Principle Extracted |
|------|----------------------|
| **Homeworld** | Restraint across every layer. Military professionalism. Fleet persistence as emotional investment. The journey structure. Music as structural element. |
| **Portal / Portal 2** | Environmental storytelling through absence. Unreliable narrator. Trust built to be broken. Cave Johnson: exposition through character voice. |
| **Outer Wilds** | Knowledge as sole progression currency. Non-linear information architecture. The mechanic IS the story. Ship Log as discovery web. |
| **Dark Souls / Elden Ring** | Item descriptions as lore. Intentional ambiguity. Community as lore layer. Player inference as the storytelling medium. |
| **Subnautica** | Depth as unified narrative/mechanical/emotional axis. PDA as diegetic interface. Degasi bases as parallel story. |
| **Disco Elysium** | Internal voices as unreliable narrators. Failure as content. Political history through urban archaeology. |
| **Return of the Obra Dinn** | Deduction as empathy engine. Caring about people you never meet alive. Mundane work as narrative weight. |
| **Mass Effect** | Consequence architecture across 100 hours. Personal ownership through "my Shepard." Suicide mission as design driver. |
| **Fallout: New Vegas** | Ideological diversity without a "right answer." Reputation as narrative record. Emergent endings from accumulated decisions. |
| **Sunless Sea** | Story as tradeable resource. Trade routes as narrative journeys. Quality-based narrative. |
| **FTL** | Scarcity chains create dramatic arcs. Named characters as emotional hooks. Permadeath as narrative seal. |
| **Freelancer** | Authored frame / open interior. Progressive world unlocking. Trade routes as world geography. |
| **Stellaris** | Ancient chain structure. Event text that implies more than it states. Ancient catastrophes as warning. |
| **Elite Dangerous** | Community goals as narrative events. Existential threat gives sandbox urgency. "The outcome was yours." |

### GDC Talks Referenced

- Kim Swift & Erik Wolpaw, "A Portal Post-Mortem" (GDC 2008)
- Kelsey Beachum, "Sparking Curiosity-Driven Exploration Through Narrative
  in Outer Wilds" (GDC 2021)
- Josh Sawyer, "Do (Say) The Right Thing: Choice Architecture, Player
  Expression, and Narrative Design in Fallout: New Vegas"
- Paul Ruskay (Homeworld), Fists of Heaven interview on score design
- Alex Garden (Homeworld), Dev Game Club interview on diaspora narrative

---

## Version History

- v0.2 (2026-03-10): Major additions from narrative review. Pentagon revelation
  trigger specification. Mid-game narrative density rules (hours 3-8). Epilogue
  system (post-endgame montage). Cover-story CI lint enforcement. Failure state
  narratives (death/bankruptcy screens). Positioning & first-impression strategy.
  Mechanical hooks for lore delivery. FO elevated to #1 narrative priority.
  Fragment count updated 16→12, resonance pairs 8→6.
- v0.1 (2026-03-09): Added narrative integration points for all 13 companion
  design docs (previously only 6). Added Narrative Pacing Timeline mapping
  story beats to tick progression. Added Narrative Delivery Checklist for
  implementers.
- v0 (2026-03-09): Initial document. 12 principles, 5 narrative layers,
  delivery mechanisms, ancient log format, integration map, gap analysis.
