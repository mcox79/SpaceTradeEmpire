# Planet Scanning System — Design Document v1

**Status:** DRAFT
**Last revised:** 2026-03-20
**Depends on:** ExplorationDiscovery.md, factions_and_lore_v0.md, trade_goods_v0.md, dynamic_tension_v0.md

---

## Design Philosophy

Planets are currently inert data blobs — generated with rich physical properties (gravity, atmosphere, temperature), specializations, and type distributions per world class, but offering zero player interaction beyond "land or not." The discovery system operates on abstract discovery IDs seeded at graph nodes. Planet scanning bridges these two parallel systems, making planets the primary *source* of actionable intelligence while connecting every scan result to the economic loop and the ancient mystery.

**Core thesis:** The scanner is a *lens* — the player chooses what to look for, the planet determines what there is to find, and the scanner's charge budget forces prioritization.

### Three design pillars

1. **Player directs the scanner.** You choose what to look for. Wrong choice = weaker results, not zero. Right choice = you're rewarded for understanding the world. (Subnautica Scanner Room: pick your target.)
2. **Charge budget forces prioritization.** You can't scan everything in one visit. The constraint is your tool's capacity, not countdown timers. Nothing in the world expires because you were slow — but you can't do it all at once. (Factorio: throughput is limited by what you've built, not by artificial clocks.)
3. **The tool itself transforms.** Scanning at hour 15 must feel fundamentally different from hour 3 — not because the rewards are bigger, but because the scanner does different things. (No Man's Sky visor → ship → deep. Subnautica basic scanner → Scanner Room → upgrades.)

### Reference Games & Lessons

| Game | What Works | What Fails | Our Take |
|------|-----------|------------|----------|
| **Stellaris** | Survey → anomaly → chain. Planet classes feel distinct | Surveying becomes mindless checkbox by mid-game | Scan modes create strategic choice; automation graduates the routine |
| **Elite Dangerous (FSS)** | Each body type has distinct scan signature. FSS = active tuning | Scan #500 = scan #1. No narrative payoff | Mode selection = player-directed tuning. Results connect to lore |
| **No Man's Sky** | Scanner evolution (visor → ship → deep). Flora/fauna catalogs | Repetitive. No economic consequence | Scanner physically evolves. Results always feed economy |
| **Mass Effect 2** | Planet scanning for resources feeds upgrades | Boring: no choice in HOW to scan, just WHERE | Mode choice + planet affinity = the player's skill expression |
| **Subnautica** | Scanner Room: set target, get directional pings | Narrow scope (one planet) | Mode selection mirrors "set target." Survey automation mirrors room |
| **EVE Online (PI/probing)** | Probe triangulation. Heat maps. Intel has market value | Opaque UI, disconnected from exploration | Scan charge budget forces prioritization like probe count |
| **Outer Wilds** | Knowledge IS the progression gate. Observation rewards attention | Not repeatable | FO teaches property→result correlations. Player builds intuition |
| **Dyson Sphere Program** | Planet resources visible from orbit, surveying reveals veins | No narrative component | Orbital = preview, Landing = commit. Matching DSP's orbit/surface split |

---

## The Scanner: An Evolving Tool

### Scan Modes

The player doesn't just "scan" — they choose a **scan mode** that determines what the scanner is optimized to detect. The planet still determines what's there; the mode determines how well you detect each category.

| Mode | Unlocked | Optimized For | Icon Concept |
|------|----------|--------------|--------------|
| **Mineral Survey** | Default (hour 0) | Resource Intel | Pickaxe/crystal |
| **Signal Sweep** | Scanner Mk1 research | Signal Leads | Waveform |
| **Archaeological Scan** | Scanner Mk2 research | Physical Evidence + Data Archives | Compass/ruin |

**Mode × Planet Type Affinity Matrix** (result quality multiplier):

| | Terrestrial | Ice | Sand | Lava | Gaseous | Barren |
|---|---|---|---|---|---|---|
| **Mineral Survey** | 0.8 | 1.0 | **1.5** | 1.2 | 0.7 | **1.3** |
| **Signal Sweep** | 0.7 | 0.8 | 0.6 | **1.4** | **1.5** | 0.5 |
| **Archaeological Scan** | **1.3** | 1.0 | 1.1 | 0.8 | 0.3 | **1.5** |

The player LEARNS this matrix through play, not a tooltip. FO comments teach it:
- First time Mineral Survey on Sand world yields great results: *"Desert geology is ideal for mineral detection. Good instinct."*
- First time Signal Sweep on Barren yields poor results: *"Not much signal activity here. The electromagnetic silence is useful for other things, though."*
- After 5+ scans: *"I'm starting to see a pattern — your signal sweep works best near volcanic activity."*

**Why 3 modes, not more:** Maps cleanly to the three core discovery families (RESOURCE_POOL_MARKER, SIGNAL/CORRIDOR_TRACE, RUIN/DERELICT). The player learns 3 things, not 12. The planet type provides the second axis of complexity — 3 modes × 6 planet types = 18 combinations, enough depth without cognitive overload.

### Scanner Charge Budget

The scanner is not free. It draws from a **charge pool** that regenerates over time:

| Scanner Tier | Charges Per System | Recharge Rate | Unlocked By |
|-------------|-------------------|---------------|-------------|
| Basic | 2 | 1 per 30 ticks | Default |
| Mk1 | 3 | 1 per 25 ticks | Sensors Mk1 research |
| Mk2 | 4 | 1 per 20 ticks | Deep Scan research |
| Mk3 | 5 + dual-mode | 1 per 15 ticks | Advanced Sensors research |

**What costs a charge:**
- Orbital scan: 1 charge
- Landing scan: 1 charge (separate from orbital — landing always available if docked and tech permits)
- Atmospheric sampling (Gaseous): 1 charge + 1 fuel

**Why charges matter:** At a system with a Gas Giant, an Ice World, and a Terrestrial, the Basic scanner can only orbital-scan 2 of them. The player must CHOOSE. This creates:
- A real decision per system visit (not "scan everything, read later")
- A meaningful automation payoff (SurveyProgram handles the budget for you, scanning what you'd choose)
- A reason to upgrade (Mk3 scanner = you can survey the whole system + land on the best target)
- Trade-route planning that accounts for scan opportunities ("this route passes through 3 unsurveyed ice systems")

**Charges are per-visit, not per-planet.** If you orbital-scan the Ice World and then want to orbital-scan the Terrestrial too, that's 2 charges. Leaving the system and returning resets the budget.

### Scanner Evolution

The scanner doesn't just get "more range" — it gains new capabilities:

| Tier | New Capability | How It Changes the Experience |
|------|---------------|------------------------------|
| **Basic** | Mineral Survey only. 2 charges. | Scanning = economic intelligence gathering. Simple, direct |
| **Mk1** | +Signal Sweep mode. 3 charges. Orbital scan range +1 hop | Now choosing which mode matters. Signals point to mysteries |
| **Mk2** | +Archaeological Scan mode. 4 charges. Can scan tech-gated planets | Full strategic scanning. Lava/Barren worlds open up |
| **Mk3** | Dual-mode (scan with 2 modes simultaneously). 5 charges | Efficiency upgrade — experienced players don't have to choose |
| **Fracture Scanner** | Can scan in high-instability zones without signal degradation | Late-game capability. Sees clearly where others see noise |

**The critical escalation:** At Basic, scanning is "point at planet, get resource data." At Mk2, scanning is "I have 4 charges, this system has an Ice world and a Lava world and a Gas Giant, the Lava world orbital showed Thread resonance blooms last sweep, I should Archaeological Scan the Ice world because I need structural fragments for Haven, then Signal Sweep the Lava world to decode the bloom, and save my last charge for the Gas Giant atmospheric sample because this is an O-type star system and those have resonance pockets." That's a GAME.

---

## Two-Phase Interaction: Orbital → Landing

### Orbital Scan (from anywhere in-system)

**Cost:** 1 scanner charge + mode selection
**Result:** Planet-type-dependent findings at Seen or Scanned phase. Always produces SOMETHING, but mode affinity determines quality and which categories surface.

The orbital scan card shows:
- **Primary finding** (best match for your mode × planet type)
- **Hint line** (what a DIFFERENT mode might find — teaches the matrix). Example on an Ice world with Mineral Survey: *"Strong rare metal signature detected. [faint signal anomaly also present — Signal Sweep recommended]"*
- **Landing prospect** (if landable): "Surface scan available. Estimated depth: [quality indicator based on mode affinity]"

**The hint line is the teaching mechanism.** It tells the player "there's more here if you use a different tool" without being a tooltip. It's the Outer Wilds approach: the world tells you what to look for next.

### Landing Scan (while docked at landable planet)

**Cost:** 1 scanner charge + 1 fuel + mode selection
**Requires:** Planet must be landable (Gaseous = never; Lava/Barren = tech-gated)
**Result:** Findings at Scanned or Analyzed phase. Higher quality. Always includes a guaranteed finding appropriate to mode + planet type.

Landing scans skip ahead in the discovery pipeline (Scanned or Analyzed, never just Seen), providing immediate value. This is the "commit" action — you chose to invest in THIS planet.

**Gaseous planets** cannot be landed on. Instead, they offer **Atmospheric Sampling** — a unique orbital-only deep scan (1 charge + 1 fuel) that accesses deep atmosphere data. The fuel cost represents probe deployment. This ensures Gas Giants have a "deep scan" equivalent without breaking the no-surface rule.

---

## Five Finding Categories

Every scan result falls into exactly one of 5 categories. Planet type determines the *flavor* and *specific content* within each category, but the player only needs to learn 5 things.

### 1. Resource Intel
**What it is:** Actionable economic data — where to buy, where to sell, how much profit.

**How it works:** Generates `TradeRouteIntel` with `SourceDiscoveryId`. The route includes: source node, destination node, good ID, and estimated profit per unit. Routes feed into T41's existing IntelSystem, which naturally ages intel by distance band (Near=50t, Mid=150t, Deep=400t). This is NOT a planet-scanning-specific timer — it's the same aging that applies to all trade intelligence. Prices change because markets are dynamic, not because the game is punishing slow play.

**The decision it creates:** "This ice world has rare_metals at 12cr below regional average. Nearest buyer is 3 hops away. That's 30cr/unit profit on 50 units. Do I divert from my current route, or finish my current delivery first?" The opportunity cost is the player's time and scanner charges, not a countdown.

**Planet type flavor:**
- Terrestrial: Supply/demand trends for specialization goods (food, electronics, components)
- Ice: Rare metal purity grades, fuel extraction quality
- Sand: Ore deposit density, rare metal traces
- Lava: Exotic crystal formations, composites availability
- Gaseous: Fuel extraction efficiency, gas composition profiles
- Barren: Pure mineral deposits (highest purity in the game, no contamination)

---

### 2. Signal Lead
**What it is:** A breadcrumb pointing to another discovery. NOT the discovery itself — it's a bearing, a frequency fragment, a coordinate hint. Following the lead is a separate action.

**How it works:** Creates a `SIGNAL` or `CORRIDOR_TRACE` discovery at Seen phase. The signal includes a `COORDINATE_HINT` or `RESONANCE_LOCATION` mechanical hook that narrows the search to a specific region (within N hops).

**The decision it creates:** "This gas giant trapped a signal fragment pointing somewhere within 2 hops of here. I can follow it now, or log it and let my SurveyProgram try to triangulate when it auto-surveys this region next week." Signals don't expire. They sit in your IntelBook until you follow them. The pull is curiosity and the promise of what's at the other end, not a timer.

**Signal leads chain.** Finding Signal A points to location B. Scanning at B with Signal Sweep may reveal Signal C pointing to location D. This is the **pull chain** — scanning creates the reason to scan more. Anomaly chain steps are the authored version of this mechanic; signal leads are the procedural version.

**Triangulation:** A single signal gives a direction and distance band (within N hops). Scanning a second signal of the same type from a different system triangulates the source — now you have exact coordinates. One signal = vague marker on galaxy map. Two signals = precise pin. The SurveyProgram can eventually triangulate if it orbital-scans enough systems in the region, but manual follow-up finds the source faster and always yields better results (landing scan vs orbital scan).

**Planet type flavor:**
- Terrestrial: Faction comm intercepts (R2/R3 feeders — Concord suppression, trade pattern anomalies)
- Ice: Thread Lattice resonance (Tal's infrastructure, LOG.LATTICE thread)
- Sand: Geological conductance patterns (Fossilized Thread conduits in bedrock)
- Lava: Thread Resonance Blooms (Vael's accommodation geometry, LOG.ACCOM thread). Collecting 3+ bloom timestamps from different lava worlds reveals a frequency pattern
- Gaseous: Trapped ancient transmissions (LOG.WARN thread — warnings still echoing through hydrogen clouds)
- Barren: Electromagnetic anomalies (faint, because barren worlds are quiet — but what you DO find is high-fidelity)

---

### 3. Physical Evidence
**What it is:** A tangible artifact of the ancient civilization — ruins, fossils, derelicts, installations. The "show, don't tell" layer. Each planet type preserves a different chapter of the ancient story.

**How it works:** Creates a `RUIN` or `DERELICT` discovery. Always includes flavor text describing what the player sees. Creates 1-3 `KnowledgeConnection` entries in the Knowledge Graph. May include a mechanical hook (`TRADE_INTEL`, `CALIBRATION_DATA`, `RESONANCE_LOCATION`).

**The decision it creates:** Physical Evidence findings offer an **investigation option** — the player can spend additional time (5-15 ticks docked) to extract bonus data, or leave and come back later. The site doesn't degrade or expire. Investigating yields bonus KnowledgeGraph connections and may reveal a Signal Lead pointing elsewhere. The cost is opportunity: time docked = time not trading or following other leads. But the ruin will still be there tomorrow.

**Planet type determines what's preserved and WHY:**

| Planet Type | Evidence Type | Why It's Here | Ancient Voice | Lore Thread |
|------------|--------------|--------------|---------------|-------------|
| **Terrestrial** | Faction Archives | The pentagon's paper trail — faction records of trade dependencies, founding documents | Senn (economic topology), Oruth (political decisions) | R2 (Concord Suppression), R3 (Engineered Dependency) |
| **Ice** | Thread Lattice Fossils | Cold stopped degradation. Infrastructure preserved in ice cores | Tal (infrastructure grief: "junction twenty-nine had the best acoustics") | LOG.LATTICE |
| **Sand** | Excavation Sites | Ancient industrial scarring. Mining operations half-buried by millennia of wind | Senn (pentagon design notes — he studied these extraction zones) | R3, LOG.ECON |
| **Lava** | Thread Emergence Points | Living phenomena — Thread energy pushing through planetary crust. NOT ruins | Vael (accommodation geometry — this is the math made physical) | R5 ("Instability Is Not What You Feared") |
| **Gaseous** | Resonance Pockets | Thread energy self-stabilizing in atmospheric equilibrium. Contradicts Containment theory | Communion tradition (prayer stations near gas giants — they understood first) | R4, R5 |
| **Barren** | Intact Installations | No atmosphere = no degradation. Pristine archaeological sites. Deliberately archived | Oruth (departure records), Vael (hidden error margins: "±2% published, ±11% actual") | R1, LOG.DEPART, LOG.ACCOM |

**The key insight:** Each planet type isn't just "more lore" — it's a SPECIFIC chapter. Ice = how they built. Sand = how they extracted. Lava = what they feared/hoped. Barren = what they chose to save. Terrestrial = how they governed. Gaseous = what they couldn't contain. A player who scans all 6 types has assembled the full picture without anyone explaining it.

---

### 4. Fragment Cache
**What it is:** An Adaptation Fragment — one of 16 ancient technology components that combine into resonance pairs granting permanent bonuses. Drives Haven starbase progression.

**How it works:** Directly adds a fragment to the player's collection. Fragment kind is biased by planet type but not deterministic. Each cache includes a cover-story name (pre-R1) and a true name (post-R1). The cover story always makes sense for where you found it.

**The decision it creates:** Fragments form resonance pairs. Finding frag_bio_01 (Growth Lattice) creates a pull toward frag_str_01 (Void Girder) to complete pair_01 (+5% trade margin). The player must decide: "I need a Structural fragment. Barren and Sand worlds favor Structural finds. Do I divert to that Barren system in RIM space, knowing it's 5 hops from my trade route and I'll need landing tech?"

**Planet type affinity:**
- Ice: Biological favored (Growth Lattice, Symbiont Cortex — cryopreserved organic tech)
- Sand: Structural favored (Compression Seed, Lattice Shard — ancient mining tools)
- Lava: Energetic favored (Cascade Core, Resonance Coil — heat-forged energy systems)
- Gaseous: Cognitive favored (Pattern Engine, Memory Lattice — signal-preserved thought-tech)
- Barren: Any kind (the ancients' safety deposit box — no bias, highest base chance)
- Terrestrial: Rare (fragments were kept away from populated worlds — lowest base chance)

**Fragment caches are LANDING SCAN ONLY.** You cannot find fragments from orbit. This ensures the commit-to-land decision always has high stakes when fragment collection is the goal.

---

### 5. Data Archive
**What it is:** A data log or faction-specific archive fragment. The narrative layer — character voices, personal contradictions, the emotional core of the ancient mystery.

**How it works:** Adds a data log to the KnowledgeGraph. Each log has: scientist voice, thread ID, revelation tier, and a mechanical hook (COORDINATE_HINT, CALIBRATION_DATA, RESONANCE_LOCATION, TRADE_INTEL, or none).

**The decision it creates:** Data Archives always contain a mechanical hook that feeds another system. A log with COORDINATE_HINT points to a Signal Lead. A log with CALIBRATION_DATA improves scanner accuracy for a specific world class. A log with TRADE_INTEL reveals a hidden trade route. The narrative is never "just story" — it always connects to gameplay.

**The emotional architecture:**

| World Class | Log Tone | What You Learn |
|------------|---------|---------------|
| **CORE** | Professional, political | How the pentagon was governed. Senn's economic calculations. Oruth's policy decisions. The machinery of control. |
| **FRONTIER** | Anxious, observational | Infrastructure reports from the edges. Tal noticing things breaking. Kesh worrying about colleagues. The first signs. |
| **RIM** | Raw, personal, desperate | Vael's hidden error margins. Oruth's one-word entries ("Timeline." "I know."). Kesh asking "Have you eaten today?" The human cost. |

**Data Archives are found through ARCHAEOLOGICAL SCAN mode.** This means the player who invests in Archaeological scanning (unlocked at Mk2) gains access to the narrative layer. This is deliberate — by hour 8+ when Mk2 unlocks, the player has enough context to care about ancient scientists' personal lives. The narrative gate matches the scanner gate.

---

## Physical Properties as Discoverable Knowledge

Planet properties (GravityBps, AtmosphereBps, TemperatureBps) affect scan results, but the player learns this through **observation and FO commentary**, not tooltips.

### The Learning Loop

1. **First scan:** Player sees result + planet properties in the dock header
2. **Pattern emerges (3-5 scans):** FO comments on the correlation
3. **Player internalizes:** Starts choosing scan targets based on properties
4. **Mastery:** Player can predict what a planet will yield from its orbital stats

### FO Commentary Teaches the Matrix

| Observation | FO Line (first occurrence) | FO Line (pattern confirmed) |
|------------|--------------------------|---------------------------|
| High gravity + good mineral results | Analyst: "Dense core. Gravity compressed the deposits." | "High-G worlds consistently show richer mineral yields." |
| Low atmosphere + good ruin results | Veteran: "Nothing to erode the structures out here." | "Low-atmosphere worlds preserve ruins better. Worth noting." |
| High temperature + signal detection | Pathfinder: "Heat activates the old conduits somehow." | "Volcanic worlds light up on signal sweep. The energy's still there." |
| Extreme property (>8000 or <2000) | Any: "Extreme readings. This is an unusual world." | "Unusual worlds tend to have unusual finds. Keep scanning." |

**Composite scores (hidden but discoverable):**
- High gravity + Low temperature → best for Physical Evidence (compression + preservation)
- Low gravity + Low atmosphere → best for Data Archives (pristine, undisturbed sites)
- High temperature + High instability → best for Signal Leads (active Thread energy)

The player who pays attention to FO comments and planet stats will outperform the player who randomly scans — but the random scanner still finds things. **The matrix biases results, it doesn't gate them.** This is the Outer Wilds principle: knowledge is power, but ignorance isn't a wall.

---

## What Each Planet Type Offers (Lore Architecture)

### Terrestrial Worlds — The Pentagon's Paper Trail
*CORE-heavy. Agriculture/Manufacturing/HighTech.*

The inhabited backbone. Where the political story lives.

- **Resource Intel:** Supply/demand for specialization goods (food, electronics, components). Faction-controlled pricing patterns
- **Signal Leads:** Faction comm intercepts. Trade anomaly patterns that feed R3 (Engineered Dependency). Concord internal memos that predate the official timeline (R2)
- **Physical Evidence:** Faction Archives — short, fragmentary, always specific to the controlling faction. Concord founding documents. Weaver trade ledgers. Chitin biological surveys. Valorin expedition logs. Communion prayer-records-as-incident-reports (R4)
- **Fragment Cache:** Rare (fragments kept away from populated worlds)
- **Data Archive:** Senn's economic topology notes. Oruth's policy decisions. The machinery of governance

**Why terrestrial matters:** Scanning enough Concord-controlled terrestrials reveals their import dependencies. Scanning enough Weaver worlds reveals export monopolies. The PLAYER assembles R3 by noticing the pentagon pattern across scans. No one tells them — they see it.

### Ice Worlds — The Preservers
*RIM-heavy. FuelExtraction/Mining.*

Cold stopped degradation. The ancient infrastructure's best preservation medium.

- **Resource Intel:** Rare metal concentrations in ice layers (purity grades). Fuel extraction viability (thermal vent count correlates with TemperatureBps variance)
- **Signal Leads:** Thread Lattice resonance signatures. Tal's infrastructure was built with materials that still hum at specific frequencies. Each lattice signal feeds LOG.LATTICE
- **Physical Evidence:** Thread Lattice Fossils — compressed Thread infrastructure in ice cores. Frozen Derelicts (ships/stations flash-frozen, only at InstabilityLevel >= 2). Cryogenic Stasis Pods (rare, FRONTIER/RIM only — still powered by residual Thread energy)
- **Fragment Cache:** Biological favored. Growth Lattice, Symbiont Cortex. Cover stories like "Regenerative Polymer Sample" make sense as cryogenic finds
- **Data Archive:** Kesh's worried voice. LOG.CONTAIN thread (should we cage the instability?). LOG.DEPART (Oruth's terse departure records, preserved in cold)

**Why ice matters:** *"Someone should remember that junction twenty-nine had the best acoustics in the network."* The ice preserved what the ancients couldn't. Tal's grief is frozen here.

### Sand/Desert Worlds — The Industrial Face
*FRONTIER-heavy. Mining.*

Where the ancients extracted, refined, and built their economic topology.

- **Resource Intel:** Ore deposit density (highest in the game). Rare metal traces. Occasionally exotic_crystals in deep desert formations
- **Signal Leads:** Geological conductance patterns — Thread conduits fossilized in high-gravity bedrock (GravityBps > 6000). Points to fracture sites and anomaly chain steps
- **Physical Evidence:** Excavation Sites — ancient mining operations, half-buried by millennia of wind. Mining equipment remnants. The physical infrastructure of the pentagon's extraction backbone
- **Fragment Cache:** Structural favored. Compression Seed, Lattice Shard — the tools the ancients used to extract Thread-adjacent materials
- **Data Archive:** Senn's Economic Survey notes — the pentagon ring was designed by studying which worlds could produce what. Sand worlds were designated "extraction zones." Finding Senn's notes HERE feeds R3 because you can see the logic: he stood in the dust, counted resource nodes, drew dependency arrows

**Why sand matters:** The ancients didn't just observe — they mined, extracted, shaped. Senn's amoral efficiency is written in the geology.

### Lava/Volcanic Worlds — The Schism Made Physical
*RIM-heavy. Manufacturing/Mining. Tech-gated landing (planetary_landing_mk1+).*

Where Thread energy bleeds closest to the surface. Most dangerous, most rewarding.

- **Resource Intel:** Exotic crystal formations (heat-forged, impossible to replicate industrially). Salvaged tech deposits (pre-schism technology melted into the crust)
- **Signal Leads:** Thread Resonance Blooms — periodic energy bursts detectable from orbit. Each bloom is a timestamped data point. **Collecting 3+ bloom timestamps from different lava worlds reveals a frequency pattern** — a `SIGNAL` discovery with `CALIBRATION_DATA` hook. This is Vael's accommodation geometry predictions made measurable
- **Physical Evidence:** Thread Emergence Points — NOT ruins. A location where Thread energy actively pushes through planetary crust. A living phenomenon. FO Analyst: "Impossible under standard physics." FO Veteran: "Looks like a weapon test site." FO Pathfinder: "It's not broken. It's growing." Directly feeds R5
- **Fragment Cache:** Energetic favored. Cascade Core, Resonance Coil — heat-forged energy systems
- **Data Archive:** Vael's accommodation calculations (LOG.ACCOM). The math behind the hope. "If the geometry holds, we don't have to contain it. We can shape it."

**Why lava matters:** The landing tech gate ensures the player has progressed far enough to contextualize what they find. A Thread Emergence Point — energy pushing through rock — is geology arguing with physics. Containment tried to cage this; Accommodation tried to shape it. The player sees both arguments written in magma.

### Gaseous Worlds — The Listeners
*Distributed. FuelExtraction. NEVER landable.*

Massive, beautiful, impossible to land on. The galaxy's acoustic memory.

- **Resource Intel:** Fuel extraction efficiency rating (derived from AtmosphereBps + GravityBps). Gas composition profiles
- **Signal Leads:** Trapped ancient transmissions — gas giant atmospheres act as natural signal amplifiers. Ancient broadcasts bounced between atmospheric layers for millennia. **The warning is still playing, on loop, for anyone with the right receiver.** Feeds LOG.WARN. The Communion built prayer stations near gas giants because they understood this first
- **Physical Evidence:** Resonance Pockets (rare, ClassO/ClassA star systems only) — stable pockets where Thread energy achieves temporary equilibrium. The stability contradicts Containment theory. FO Analyst: "The readings are self-correcting. That shouldn't be possible." FO Pathfinder: "It found its own shape"
- **Fragment Cache:** Cognitive favored. Pattern Engine, Memory Lattice — signal-preserved thought-tech. Found only through Atmospheric Sampling (the deep scan equivalent)
- **Data Archive:** LOG.WARN thread excerpts decoded from atmospheric signal noise

**Why gas giants matter:** They're the *listeners*. They caught transmissions no one else preserved. The Communion's mysticism makes sense when you realize their prayer frequencies match the gas giant resonance bands.

### Barren Worlds — The Archives
*Distributed. Mining/FuelExtraction. Tech-gated landing (planetary_landing_mk1+).*

No atmosphere, no life, no interference. Deliberately chosen preservation sites.

- **Resource Intel:** Surface composition (most accurate orbital scan — no atmospheric distortion). Pure mineral deposits (highest purity, no contamination)
- **Signal Leads:** Electromagnetic anomalies — rare, but what you DO find is high-fidelity. The silence amplifies faint signals
- **Physical Evidence:** Intact Installations — complete ancient structures, surviving because there's nothing to degrade them. **Highest Knowledge Graph density of any planet type** (2-3 connections per find). Precursor Vaults (rare, RIM + InstabilityLevel >= 2) — deliberately sealed chambers. The vault seal responds to your fracture drive — not because it's a key, but because the vault was *designed for someone who could get here*
- **Fragment Cache:** Any kind (no bias — the ancients' safety deposit boxes). Highest base chance of any planet type
- **Data Archive:** Oruth's departure records (LOG.DEPART). Vael's hidden error margins (LOG.ACCOM): *"Published error margins: ±2%. Actual error margins: ±11%. If anyone finds this log: the published numbers are wrong."* Stored where no one would casually find them

**Why barren matters:** The ancients *chose* to archive their most sensitive data on worlds with no atmosphere — where nothing degrades, where no one casually visits. They knew their civilization would end. They planned for *us*. Barren worlds are the answer to "did they know?" Yes. They knew.

---

## World Class Depth Progression

| World Class | Scanner Experience | What Changes |
|------------|-------------------|-------------|
| **CORE** | Routine economic intelligence. Mode choice is straightforward (Mineral Survey dominates). Signal Leads point to faction politics | Teaches the mechanic. Builds trade routes. The pentagon's paper trail |
| **FRONTIER** | Mixed economic + anomalous. Signal Sweep becomes valuable. First "what IS this?" moments on ice/sand worlds. Physical Evidence starts appearing | Introduces the mystery. First Thread signatures. First fragment finds. The frontier between known and unknown |
| **RIM** | Dominated by anomalous readings. Scanner unreliability increases in high-instability zones. Archaeological Scan becomes the most valuable mode. FO commentary shifts from economic to existential | Delivers the revelations. Thread Emergence Points. Precursor Vaults. The ancient civilization did their real work out here, far from everyone |

This creates a natural arc: CORE scanning teaches the mechanic. FRONTIER scanning introduces the mystery. RIM scanning delivers the revelations. The scanner ITSELF evolves in parallel (Basic → Mk1 → Mk2), so the tool and the territory mature together.

---

## Engagement Model: Charge Budget, Not Timers

**Nothing the player discovers expires because they were slow.** Ruins don't crumble. Signals don't fade. Fragments don't decay. The only time-sensitive element is Resource Intel — and that's because market prices naturally shift (T41's IntelSystem applies this uniformly to all trade intelligence, not just planet scans).

The engagement constraint is the **scanner charge budget** — you can't scan everything in one visit.

### The Charge Budget Creates Natural Tension

At a system with an Ice World, a Lava World, and a Gas Giant, a Basic scanner (2 charges) forces a choice:
- Mineral Survey the Ice World for rare metal intel? Or Signal Sweep the Lava World for Thread resonance data?
- Use both charges on orbital scans? Or save one for a landing scan if the orbital looks promising?
- Come back later with a Mk1 scanner (3 charges) and scan all three? But that's a round trip...

The tension is capacity vs. opportunity, not clock vs. player. Every system visit is a small puzzle: "What's most valuable to scan here, given what I'm looking for?"

### Signal Lead Accumulation

Signal Leads sit in the IntelBook permanently. They don't expire. But they DO accumulate — and accumulation creates its own gentle pressure:
- 1 signal: vague marker on galaxy map
- 2+ signals of same type from different systems: triangulation → precise pin
- 10+ unresolved signals: the IntelBook starts feeling cluttered, and the FO starts commenting: *"We've got quite a backlog of unresolved signals. Might be worth following up on a few."*

This is the Stellaris archaeology model done right — sites wait for you, but having too many open leads creates a natural pull to resolve some.

### Fragment Scarcity (Natural, Not Timed)

Only 16 fragments exist, each found at most once. Fragment finds become rarer as the player collects more (the remaining pool shrinks). Late-game fragment hunts become expeditions: "I need frag_cog_04 to complete pair_08 and unlock the Precursor Core module. Cognitive fragments favor gas giants near O/A stars. There are only 3 O-type systems in the galaxy, and I've already scanned 2 of them."

This is natural scarcity, not manufactured urgency. The fragment is there — the challenge is finding it.

### Investigation as Opportunity Cost (Not Timed Window)

Physical Evidence sites can be investigated immediately or later. The site doesn't change. But spending 5-15 ticks docked means you're not trading, not following signal leads, not scanning other systems. The cost is your time and attention, not an expiring window.

**Design principle: the world is patient. The player's time is the scarce resource.**

---

## Instability Interaction: New Readings, Not Backtracking

**Problem with v0:** "Return to re-scan" = backtracking = chore.

**v1 solution:** When instability changes reveal new scan data at a previously-visited planet, the game creates a **Signal Lead** at the player's CURRENT location pointing to the revealed site. The player sees it on their galaxy map as a new lead, not as "go back and re-scan."

**How it works:**
1. Player scans Ice World at node B (InstabilityLevel = 1). Normal results
2. Later, instability at node B rises to 3. A Frozen Derelict that was InstabilityGated at 2 becomes visible
3. The player does NOT need to go back and re-scan. Instead, the rising instability itself acts as a signal — a new `SIGNAL` discovery appears at node B at Seen phase, visible on the galaxy map
4. If the player has a SurveyProgram covering that region, it auto-scans the signal and generates a report
5. The player can choose to go investigate (landing scan for the derelict) or not

**The key difference:** The game BRINGS the information to you. You don't have to go looking for it. The instability-reveal mechanic creates new leads on the galaxy map, not obligations to revisit. If you go, it's because the lead is worth pursuing, not because you're completionism-grinding.

---

## SurveyProgram Automation Integration

The SurveyProgram (T41) handles the routine orbital scanning that becomes tedious after the learning phase. The graduation flow:

1. **Manual phase (3 scans per discovery family):** Player learns mode × planet type affinities by doing. FO tracks progress: *"That's your third mineral survey. I could configure an automated survey program to handle the routine scanning."*
2. **Unlock gate:** After 3 manual scans of a family, `CreateSurveyProgramV0` is offered. Player chooses: home node, range (hops), scan mode, cadence
3. **Automation phase:** SurveyProgram orbital-scans planets within range using the configured mode. Generates Resource Intel and Signal Leads at Scanned phase. Reports findings to the IntelBook
4. **Manual premium persists:** Landing scans are NEVER automated. Fragment Caches, investigation bonuses, and high-tier Physical Evidence require the player to dock and commit. Automation handles the scouting; the player handles the expeditions

**The Factorio parallel:** Early game, you hand-carry ore from mines to furnaces. Later, you build belts. But you still manually place and configure the belts — automation doesn't remove the player, it amplifies them. Our SurveyProgram automates orbital scanning the way belts automate ore transport. Landing scans are the player placing new machines.

---

## Discovery Family Pipeline Mapping

| Finding Category | Discovery Family | Phase at Orbital | Phase at Landing | Automation |
|-----------------|-----------------|-----------------|-----------------|------------|
| Resource Intel | `RESOURCE_POOL_MARKER` | Scanned | Analyzed | SurveyProgram |
| Signal Lead | `SIGNAL` / `CORRIDOR_TRACE` | Seen | Scanned | SurveyProgram |
| Physical Evidence | `RUIN` / `DERELICT` | Seen (hint only) | Analyzed | Never |
| Fragment Cache | N/A (direct to collection) | Never (landing only) | N/A | Never |
| Data Archive | N/A (direct to KnowledgeGraph) | Never (landing only) | N/A | Never |

Landing scans always produce findings at a higher phase than orbital scans. This is the commit reward.

---

## First Officer Integration

| Trigger | When | Purpose |
|---------|------|---------|
| `FIRST_PLANET_SURVEYED` | First orbital scan completes | Tutorial: explains mode selection, hints at landing scan |
| `SCAN_MODE_MISMATCH` | Player uses wrong mode for planet type (low affinity result) | Teaching: "Signal sweep isn't ideal here. This world's geology favors mineral detection" |
| `PATTERN_RECOGNIZED` | 5+ scans with same mode | Mastery: FO notes the pattern the player is building. "You're mapping the resource network. I see it" |
| `RARE_FIND` | Landing scan reveals Fragment Cache, Precursor Vault, or Thread Emergence | Moment: FO gives type-specific reaction (see lore tables above) |
| `SIGNAL_TRIANGULATED` | Two signal leads of same type resolve to exact coordinates | Pull: "Cross-referencing with the first signal... I have a location" |
| `LORE_DISCOVERY` | Data Archive found | Narrative: FO contextualizes which scientist's voice this is, what it connects to |

---

## Data Model Impact

### New Entity: `PlanetScanResult`
```
PlanetScanResult:
  ScanId: string
  NodeId: string
  ScanMode: ScanMode enum (MineralSurvey, SignalSweep, Archaeological)
  ScanPhase: ScanPhase enum (Orbital, Landing, AtmosphericSample)
  FindingCategory: FindingCategory enum (ResourceIntel, SignalLead, PhysicalEvidence, FragmentCache, DataArchive)
  DiscoveryId: string (if generated)
  FlavorText: string
  Tick: int
  InvestigationAvailable: bool (Physical Evidence only — can player spend ticks for bonus data?)
  Investigated: bool (false until player completes investigation)
```

### Planet Entity Extensions
- `Planet.OrbitalScans` (Dictionary<ScanMode, int>) — which modes have been used from orbit (tick of scan, 0 = not scanned)
- `Planet.LandingScanTick` (int) — tick of landing scan (0 = not landed-scanned)
- `Planet.LandingScanMode` (ScanMode?) — mode used for landing scan (null = not scanned)
- `Planet.ScanResults` (List<string>) — ScanIds for re-display

### SimState Scanner State
- `SimState.ScannerChargesUsed` (int) — charges consumed at current node (resets on travel)
- `SimState.ScannerTier` (int) — current scanner tier (0=Basic, 1=Mk1, 2=Mk2, 3=Mk3, 4=Fracture)

### New Enums
- `ScanMode`: MineralSurvey, SignalSweep, Archaeological
- `FindingCategory`: ResourceIntel, SignalLead, PhysicalEvidence, FragmentCache, DataArchive

### System Touchpoints
- `PlanetScanSystem.cs` (new) — Processes orbital/landing scans with mode selection, charge budget, affinity matrix
- `DiscoveryOutcomeSystem.cs` — Extended to handle planet-scan-generated discoveries
- `ProgramSystem.cs` — SurveyProgram extended to execute orbital scans with configured mode
- `IntelSystem.cs` — Instability-reveal generates Signal Leads at player location (not re-scan obligation)
- `FirstOfficerSystem.cs` — 6 new planet-scan triggers
- `SimBridge.Planet.cs` (new partial) — `OrbitalScanV0(nodeId, mode)`, `LandingScanV0(nodeId, mode)`, `AtmosphericSampleV0(nodeId, mode)`, `GetPlanetScanResultsV0(nodeId)`, `InvestigateFindingV0(scanId)`, `GetScanChargesV0()`

---

## Implementation Priority

**P0 (Next tranche — the mechanic):**
- ScanMode enum + FindingCategory enum + PlanetScanResult entity
- Planet entity extensions (charges, scan tracking)
- PlanetScanSystem with orbital scan logic + mode affinity matrix
- Scanner charge budget (Basic tier only — 2 charges)
- Resource Intel generation (TradeRouteIntel pipeline integration)
- SimBridge.Planet.cs partial

**P1 (Depth — landing + leads):**
- Landing scan logic with guaranteed findings
- Signal Lead generation + triangulation mechanic
- Physical Evidence with investigation option (ticks spent docked)
- Fragment Cache drops (planet type affinity)
- Scanner Mk1 unlock (Signal Sweep mode, 3 charges)

**P2 (Narrative + evolution):**
- Scanner Mk2 unlock (Archaeological Scan, 4 charges, tech-gated planets)
- Data Archive findings + KnowledgeGraph connections
- FO commentary for all 6 triggers
- Planet-type-specific lore flavor text (all tables above)
- Physical property → result correlation (hidden math + FO teaching loop)

**P3 (Polish + late-game):**
- Scanner Mk3 (dual-mode, 5 charges)
- Fracture Scanner (instability zone scanning)
- Instability-reveal as Signal Lead generation (not backtracking)
- SurveyProgram extension for orbital auto-scanning with mode
- World class progression tuning
- Atmospheric Sampling for gaseous planets
- Audio/visual scan feedback per planet type and mode
