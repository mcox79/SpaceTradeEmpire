# Exploration & Discovery — Design Bible

> Design doc for the exploration loop: discovery phases, scanner range, knowledge
> management, anomaly encounters, anomaly chains, automation graduation, and the
> visual language of curiosity and revelation.
> Companion to `GalaxyMap.md` (fog of war, exploration overlay), `factions_and_lore_v0.md`,
> and `dynamic_tension_v0.md` (Pillar 5 — the revelation arc).
> Content authoring specs: `content/NarrativeContent_TBA.md` (discovery text templates),
> `content/LoreContent_TBA.md` (ancient data logs).
> Epics: `EPIC.S6.UI_DISCOVERY`, `EPIC.S6.ANOMALY_ECOLOGY`, `EPIC.S6.ARTIFACT_RESEARCH`,
> `EPIC.S6.TECH_LEADS`, `EPIC.S6.EXPEDITION_PROG`, `EPIC.S6.SCIENCE_CENTER`,
> `EPIC.S6.CLASS_DISCOVERY_PROFILES`, `EPIC.S6.MYSTERY_MARKERS`.
>
> **Last revised**: 2026-03-20 — major update adding best-practice design philosophy
> (12 principles from industry research), anomaly chains, automation graduation,
> discovery-as-trade-intelligence, late-game discovery continuation.

## Why This Doc Exists

Simple exploration (visit nodes, see what's there) works fine without a design doc. But
knowledge tracking — how the player records, reviews, and acts on discoveries — fails
catastrophically without architecture. Outer Wilds' Ship Log is praised because every
discovery connects to something larger. No Man's Sky at launch was criticized because
procedural discoveries had no persistent connections.

This doc defines what the game remembers for the player, what connects to what, and how
discovery milestones feel. It prevents exploration from becoming "checking boxes."

**Since v0**: This doc now also defines how discovery connects to the automation core loop
(the Factorio principle), how anomaly chains create multi-session narrative arcs, and how
information asymmetry creates the economic motivation for exploration.

---

## Implementation Status

| Feature | Status | Notes |
|---------|--------|-------|
| IntelBook (observations, routes, prices) | Done | SimCore entity with persistence |
| Three discovery phases (Seen -> Scanned -> Analyzed) | Done | ScanDiscoveryCommand, phase transitions |
| DiscoverySitePanel UI (phase display, scan button) | Done | Polls per second, shows reward |
| DiscoveryOutcomeSystem (loot generation on analysis) | Done | Family-specific: Derelict/Ruin/Signal |
| Discovery seed types (Resource/Corridor/Anomaly) | Done | Deterministic seeding at world gen |
| Discovery phase markers on galaxy map | Done (T36) | Gray/amber/green icons at node positions |
| Knowledge graph entity model | Done (T18) | KnowledgeConnection entity + KnowledgeGraphSystem |
| Knowledge Web panel UI | Done (T36) | Enhanced in T36; breadcrumb trail still aspirational |
| Encounter narrative text / flavor text | Done (T30) | DiscoveryOutcomeSystem template-driven FlavorText |
| Progressive recontextualization | Done (T30) | Surface/Deep/Connection text gated by phase |
| Adaptation Fragments (16 + 8 resonance pairs) | Done (T34) | Dual cover/revealed lore, worldgen placement |
| T3 "Relic" discovery-only modules (9) | Done (T34) | Exotic matter sustain, IsDiscoveryOnly gate |
| Ancient ship hulls (3) | Done (T34) | Bastion/Seeker/Threshold, Haven T3+ restoration |
| Data log content (25 logs, 6 threads) | Done (T18) | DataLogContentV0.cs — 5 scientists authored |
| Narrative placement (BFS-based) | Done (T18) | NarrativePlacementGen.cs |
| Knowledge graph revelation triggers | Done (T39) | R1/R3/R5 seed connections on revelation fire |
| Kepler narrative chain (6 pieces) | Done (T18) | KeplerChainContentV0.cs |
| Active leads HUD display | Done (T36) | Leads surfaced in UI |
| Local system scan visualization | Done (T36) | Pulse effect, discovery site highlight |
| Cover-story naming discipline | Done (T37) | CoverName/RevealedName switch on R1 |
| Fracture derelict discovery gating | Done (T20) | FractureDerelict VoidSite, tick-gated unlock |
| Rumor/lead system | Partial | RumorLeads in IntelBook, leads in UI, full graph view pending |
| Scanner range visualization | Not implemented | No visual frontier ring on map |
| Exploration overlay lens | Not implemented | GalaxyMap.md aspiration |
| Discovery milestone audio/visual feedback | Not implemented | Phase transitions are silent |
| Breadcrumb trail visualization | Not implemented | No "here's what led you here" display |
| Scanner sweep animation on system entry | Not implemented | No ring-expanding animation |
| **Discovery-as-trade-intelligence** | **Not implemented** | **Discoveries should yield economic intel (NEW)** |
| **Anomaly chains (multi-site escalation)** | **Not implemented** | **3-5 site chains with narrative arcs (NEW)** |
| **Automation graduation for scanning** | **Not implemented** | **Manual -> automated scan programs (NEW)** |
| **Information asymmetry / intel decay** | **Not implemented** | **Perishable exclusive knowledge (NEW)** |
| **Late-game discovery continuation** | **Not implemented** | **Economy-triggered anomaly spawning (NEW)** |
| **Audio discovery vocabulary (4 signatures)** | **Not implemented** | **Ping/process/reveal/insight (NEW)** |
| **Anomaly ecology (spatial distribution)** | **Not implemented** | **EPIC.S6.ANOMALY_ECOLOGY** |
| **Artifact research (containment/experiments)** | **Not implemented** | **EPIC.S6.ARTIFACT_RESEARCH** |
| **Science center (analysis throughput)** | **Not implemented** | **EPIC.S6.SCIENCE_CENTER** |

---

## Design Principles

### Core Principles (v0 — unchanged)

1. **Exploration is knowledge acquisition, not map completion.** The player isn't filling
   in a fog-of-war map — they're building understanding. Each discovery should answer a
   question AND raise a new one. "There's a derelict here" -> "What happened to this ship?"
   -> "The wreck matches Valorin construction" -> "What were the Valorin doing this far from
   their territory?" This is the Outer Wilds philosophy: curiosity, not completion percentage.

2. **The game remembers so the player doesn't have to.** The IntelBook exists so the player
   never needs to write notes. Every observation, price, route, and discovery is recorded
   with timestamps. The game surfaces connections: "You saw a Valorin wreck at Kepler. The
   Valorin faction territory starts 3 hops away at Altair." The player's job is deciding
   what to do with knowledge, not remembering it.

3. **Discovery milestones are moments.** A phase transition (Seen -> Scanned, Scanned ->
   Analyzed) deserves celebration. Brief pause, distinct audio chime, visual flourish,
   and a card showing what was learned. These moments are the game's emotional payoff for
   the exploration loop. Silent phase transitions waste the player's emotional investment.

4. **Absence of data is information.** An undiscovered system on the map isn't "nothing" —
   it's a question mark that motivates exploration. "No data available" is more powerful
   than hiding the system entirely. Show what the player DOESN'T know, because that's what
   drives them to explore.

5. **Discoveries connect to each other.** An isolated discovery is trivia. A discovery
   that connects to two others is a story. The knowledge graph should reveal relationships:
   this wreck + that signal + this ruin all point to the same ancient conflict. Connections
   create "aha!" moments that pure exploration cannot.

### Industry Best-Practice Principles (v1 — new)

These principles are derived from GDC postmortems, developer analysis, and critical
evaluation of Outer Wilds, Subnautica, Stellaris, Elite Dangerous, X4: Foundations,
FTL, Mass Effect, Factorio, EVE Online, and No Man's Sky. Each principle cites the
source game(s) that demonstrate it best. See "Reference Games" section for full list.

6. **Discovery feeds automation.** (Factorio, X4, EVE Online) Every discovery should
   yield at minimum ONE of: (a) a new trade route to automate, (b) a technology that
   improves existing automation, (c) trade intelligence that makes current programs more
   profitable. Discovery without actionable follow-through is Subnautica scanning for
   XP — satisfying once, tedious by the 50th time. This is the CRITICAL principle for
   our game: the core loop is automation, and discovery is the fuel that drives it.

   **The rhythm**: Explore -> discover intelligence -> deploy automated programs to
   exploit it -> hit saturation wall -> explore again at higher tier. Discovery is not a
   separate system — it is the engine that propels the automation loop.

7. **Automate the routine, preserve the novel.** (Elite Dangerous lesson, Factorio
   principle) The first time the player encounters any discovery type, it should be a
   manual, atmospheric experience. After that type is understood, the player should be
   able to deploy an automated survey program. This directly serves the core loop:
   "manual experience teaches you the mechanic, automation IS the game."

   Elite Dangerous's fatal flaw: the 500th FSS scan feels identical to the 1st. Scan
   #1 should be a full multi-step process with audio, FO analysis, milestone card. Scan
   #500 should be a SurveyProgram running in the background, reporting results via toast.

8. **Anomaly chains, not one-shots.** (Stellaris) Every significant discovery should open
   a chain: Site A references Site B -> B requires better sensors -> B reveals Site C in
   deep fracture space -> C reveals the revelation. Each step escalates in risk, capability
   requirement, and reward. Chains that span 3-5 sites before resolution create the most
   memorable arcs. One-shot discoveries are trivia. Chains are stories.

   Stellaris proves this: the Alien Box anomaly is remembered because it escalates across
   multiple events with branching consequences. The individual anomaly that yields +50
   minerals is forgotten immediately.

9. **Information asymmetry is economic weapon.** (EVE Online, X4) Discovered trade
   intelligence should be: (a) exclusive to the player initially, (b) perishable (NPCs
   learn routes over time, faction territories shift), (c) deeper at higher scan tiers
   (surface scan = station exists; deep scan = exact price curves and production deficits).
   This creates urgency to exploit discoveries through automation before the advantage
   window closes.

   X4's critical insight: scanning a station reveals its buy/sell orders and current
   inventory. Without scanning, you're trading blind. Exploration directly feeds trading
   efficiency. Our IntelBook already tracks freshness — extend this to make stale intel
   visibly worse than fresh intel in trade program profitability.

10. **Pain before relief.** (Factorio, Dyson Sphere Program) The player should feel a
    bottleneck (a route that's saturated, a good they can't source, a program running at
    negative margin) BEFORE the game surfaces the discovery that resolves it. The tutorial
    sequence establishes this rhythm: struggle -> explore -> discover -> automate -> new
    struggle at higher tier.

    Never give the player a discovery they don't yet need. Wait until they feel the wall.
    Then the discovery isn't "a thing I found" — it's "the answer I was looking for."

11. **Planted seeds pay off late.** (Mass Effect, Outer Wilds) Early discoveries should
    contain elements whose significance only becomes clear later. A Precursor inscription
    that seems decorative in Act 1 becomes the key to understanding a late-game system.
    The Codex should store these ambiguous findings so the player can return to them.

    Mass Effect's Eletania ruin: a throwaway in ME1, significant in ME3. Outer Wilds'
    Nomai inscriptions: incomprehensible until you find the context elsewhere. Our cover-
    story naming system (CoverName -> RevealedName on R1) is already a version of this.
    Extend it to discovery site narratives: early flavor text should contain buried
    references that only make sense after later discoveries.

12. **Discovery must continue into late game.** (Stellaris's critical flaw) Do not let
    the discovery system "dry up" once the map is explored. Late-game discoveries come
    from: (a) deeper fracture layers requiring advanced sensors, (b) faction intelligence
    that shifts with the political landscape, (c) Precursor chains that only unlock after
    sufficient earlier discoveries, (d) procedural anomaly spawning tied to economy state
    rather than just map exploration. (e) Instability progression revealing sites that
    were previously stable (a ruin that appears when the local thread degrades).

    Stellaris's supply of anomalies dries up once borders stabilize. The game effectively
    stops generating discovery content for mature empires. We must avoid this.

13. **Environment shows; FO explains.** (Subnautica, environmental storytelling best
    practice) Discovery sites should tell their story through what the player observes
    (damage patterns, layout, artifacts, visual anomalies) supplemented by First Officer
    analysis (the "text" component). Environment shows *what happened*; FO explains *what
    people thought about it*. Neither alone is sufficient. Together they create the "lean
    forward" moment.

    The FO is our diegetic delivery mechanism: the player character's instruments detected
    the anomaly, the FO analyzed it, the FO reports the findings. This is more immersive
    than a floating text box or codex popup.

14. **Constrained randomness, not pure chaos.** (FTL, No Man's Sky post-launch) Discovery
    outcomes should be drawn from a constrained pool where players develop intuition. "This
    looks like a Type-3 anomaly — those tend to yield rare minerals but have higher hazard."
    Over time, the player learns the risk/reward profile of each discovery family without
    being able to predict specifics. FTL proved this: curated event pools create anticipation.
    Pure randomness creates noise.

    Practical rule: each discovery family has 4-6 outcome templates. The player encounters
    enough instances to learn the family's character, but never enough to predict the exact
    outcome. Variance within a known range is exciting. Variance with no known range is
    exhausting.

15. **Incomplete knowledge is more compelling than complete knowledge.** (Mass Effect,
    Outer Wilds) "Scientists have not yet found a way to transcribe" the ancient discs.
    The mystery is the reward. Never fully explain the thread builders. Never name them.
    Never show them. The five scientists in the data logs are as close as the player gets
    — and even they are assembled from fragments found at different sites.

    Our naming discipline (no canonical name, each faction has their own term) already
    embodies this. Extend to: no single discovery should explain a complete system. Every
    answer should open a door to a deeper question.

16. **Procedural sites with authored skeletons.** (No Man's Sky Desolation update) Pre-
    author 15-20 site narrative templates with dramatic arcs. Procedural generation fills
    specifics (which goods, which faction, which hazards, which logs). Each site should
    have: (a) one guaranteed memorable moment, (b) internal cross-references (a log
    mentions "Section 3" and the player can visit Section 3), (c) a hazard that creates
    tension distinct from normal gameplay.

    No Man's Sky derelict freighters prove this works: procedural layouts with hand-crafted
    story skeletons. Every site feels designed because the dramatic arc is authored; only
    the details are generated.

17. **First-discovery credit.** (Elite Dangerous) When a player is the first to analyze
    a site, their name (or FO name, or ship name) is permanently associated with it in
    the knowledge graph. Cheap to implement. Disproportionately motivating. Elite players
    cross the galaxy for the chance to put their name on a star.

---

## The Discovery Lifecycle

### Phase 1: Seen

**Trigger:** Player enters a system with seeded discoveries. Automatic — no player action.

**What the player learns:** Something is here. Type (Derelict/Ruin/Signal) and location.
No details.

**Visual treatment:**
- Galaxy map: dim gray marker at the node
- Local system: faint marker sphere at discovery position
- Discovery panel: "? Seen" label in muted text

**Emotional beat:** Curiosity. "What is that? Should I investigate?"

### Phase 2: Scanned

**Trigger:** Player clicks "Scan" in the DiscoverySitePanel while at the node.

**What the player learns:** Surface-level data. Material composition, energy signatures,
approximate age. Enough to decide whether to invest time in full analysis.

**NEW (v1):** Scanning now reveals **trade intelligence**: the scanned site's economic
fingerprint. For a Derelict, this includes salvage value estimates. For a Ruin, mineral
survey data. For a Signal, the frequency pattern that indicates what connected system
has a price anomaly. This intelligence is exclusive to the player until decay (see
"Information Asymmetry" section below).

**Visual treatment:**
- Galaxy map: amber marker at the node
- Local system: marker sphere brightens, gains subtle pulse
- Discovery panel: "~ Scanned" in amber, preview data revealed

**Emotional beat:** Investment. "This looks promising. I want to know more."

**Milestone feedback (aspiration):**
```
+-- SCAN COMPLETE -----------------------------------------+
|                                                           |
|  ~ Derelict Wreck -- Kepler System                       |
|                                                           |
|  "Sensor readings detect a hull fragment with             |
|   residual power. Construction pattern matches            |
|   no known active faction."                               |
|                                                           |
|  Energy signature: Fading (est. 200+ years)              |
|  Material: Unknown composite alloy                        |
|  Size: Small vessel (corvette class)                      |
|                                                           |
|  TRADE INTEL:                                             |
|    Salvage yield est.: 120-180 Salvaged Tech              |
|    Nearest buyer: Weaver station (2 hops, ~15cr/unit)     |
|                                                           |
|  [Analyze]  [Later]                                       |
+-----------------------------------------------------------+
```

### Phase 3: Analyzed

**Trigger:** Player clicks "Analyze" in the DiscoverySitePanel. Must be at the node.

**What the player learns:** Full data. Loot recovered, narrative revealed, connections
to other discoveries shown. **NEW (v1):** Full economic data enters the IntelBook with
fresh timestamps. Any connected Leads also include economic previews of the destination.

**Visual treatment:**
- Galaxy map: green marker at the node
- Local system: marker transforms into resolved visual (wreck debris, ruin structure)
- Discovery panel: "! Analyzed" in green, full reward text

**Emotional beat:** Revelation. "So THAT'S what happened here."

**Milestone feedback (aspiration):**
```
+-- ANALYSIS COMPLETE -------------------------------------+
|                                                           |
|  ! Derelict Wreck -- Kepler System                       |
|                                                           |
|  "The hull fragment is a Valorin scout vessel,            |
|   destroyed approximately 300 years ago. The damage       |
|   pattern suggests Communion weapons. This far from       |
|   Valorin space, the scout was likely on a covert         |
|   mission when intercepted."                              |
|                                                           |
|  RECOVERED:                                               |
|    Salvaged Tech x150                                     |
|    Credits: +500                                          |
|                                                           |
|  TRADE INTEL:                                             |
|    Weaver Drydock at Altair pays 18cr/unit (fresh)        |
|    Route margin: +1,200cr (before tariffs)                |
|    Intel freshness: 100% (decays over 200 ticks)          |
|                                                           |
|  CONNECTED DISCOVERY:                                     |
|    -> Signal detected at Altair-7                         |
|    "Similar energy signature. Possible second vessel"     |
|    (Altair trade intel: Electronics deficit, +30% margin) |
|                                                           |
|  [Collect Salvage]  [View in Intel Tab]                  |
+-----------------------------------------------------------+
```

---

## Discovery-as-Trade-Intelligence (NEW)

> **Core principle (#6):** Every discovery feeds the automation loop.
> **Reference:** X4 (scanning = economic intel), EVE (information asymmetry),
> Factorio (discovery resolves current bottleneck).

### Why This Matters

Our game's core loop is automation. The player scouts routes and deploys programs. If
discovery only yields loot (exotic matter, credits, salvaged tech), it's a side activity
disconnected from the main game. Discovery must yield **trade intelligence** — the fuel
that makes automation programs more profitable.

### What Each Discovery Family Yields

| Family | Phase 2 (Scanned) Intel | Phase 3 (Analyzed) Intel |
|--------|------------------------|-------------------------|
| **Derelict** | Salvage value estimate, nearest buyer | Exact salvage manifest, optimal sell route, faction origin (reputation intel) |
| **Ruin** | Mineral survey (which goods are concentrated nearby) | Full resource map, production deficit at nearby stations, technology hint |
| **Signal** | Frequency pattern -> "this system has unusual trade activity" | Exact price anomaly data, supply/demand curves at destination, hidden route |

### How This Connects to Automation

```
Player hits wall: "My TradeCharter on the Kepler-Altair route is earning 3cr/trip.
                    The route is saturated."

Player explores:  Travels to unexplored Deneb system. Discovers a Ruin (Phase 2 scan).
                  Ruin intel: "Heavy mineral concentrations — Rare Metals surplus at
                  Deneb, but Deneb station has no buyer."

Player analyzes:  Phase 3 analysis reveals: Valorin frontier station 2 hops away buys
                  Rare Metals at 22cr (vs 8cr average). Production deficit in Composites.

Player automates: Deploys new TradeCharter: Deneb -> Valorin station. Earning 14cr/trip.
                  The discovery RESOLVED the bottleneck.

Next wall:        Valorin station saturates after 50 ticks. Player needs a new discovery
                  to find the next opportunity.
```

### Intel Freshness & Decay

Discovery intel is perishable. Principle #9: information asymmetry creates urgency.

| Intel Age | Quality | Effect on Programs |
|-----------|---------|-------------------|
| 0-50 ticks (fresh) | 100% — exact values | Programs use optimal routing |
| 51-150 ticks (aging) | 75% — values drift +/-10% | Programs earn slightly less |
| 151-300 ticks (stale) | 50% — values drift +/-25% | Programs may run at loss |
| 300+ ticks (expired) | 25% — unreliable | Programs auto-pause, request re-scan |

**Mechanic:** NPC traders also discover routes over time. The player's exclusive
advantage from a discovery decays as NPC trade programs compete on the same route.
The decay rate is SLOWER for deep-frontier discoveries (fewer NPCs) and FASTER for
near-starter discoveries (many NPCs). This creates a natural pressure toward the
frontier — deeper discoveries stay profitable longer.

**Implementation note:** `IntelBook.IntelFreshness` already tracks per-node age.
Extend to include discovery-derived intel entries with explicit decay curves.

---

## Anomaly Chains (NEW)

> **Core principle (#8):** Chains, not one-shots. 3-5 site escalation.
> **Reference:** Stellaris (anomaly event chains), Outer Wilds (interconnected sites),
> Mass Effect (Prothean artifact trail).

### Why Chains Work

A one-shot discovery (scan, loot, done) is quickly forgotten. A chain that spans 3-5
sites, escalating in difficulty and narrative weight, becomes a memorable story arc that
the player carries across multiple play sessions. Stellaris's most remembered content
is always anomaly chains, never individual anomalies.

### Chain Architecture

Each chain has:
- **Hook**: The first discovery in the chain. Found through normal exploration.
  Low difficulty. Yields modest loot + a Lead pointing to the next site.
- **Development** (1-2 sites): Intermediate discoveries that require better sensors,
  deeper fracture access, or faction reputation. Each adds narrative context and
  escalates the mystery. Each yields better loot + the next Lead.
- **Climax**: The final discovery. Requires the highest-tier capability the chain
  demands. Yields significant loot, a narrative revelation, and a Knowledge Graph
  connection to the broader mystery.

### Authored Chain Templates

Pre-author 8-12 chain templates. Procedural generation fills location, faction, and
specific goods. Each template has a fixed dramatic arc.

**Chain A: The Valorin Expedition (3 sites)**
1. **Hook** (Derelict, near frontier): Valorin scout wreck. Transponder log references
   a second vessel. Lead -> [frontier system].
   *Intel: Valorin territorial expansion routes — profitable smuggling paths.*
2. **Development** (Signal, deep frontier): Emergency beacon. The second vessel was
   attacked. Weapon signatures match no known faction. Lead -> [Phase 2+ system].
   *Intel: Ancient weapons data -> tech lead for research.*
3. **Climax** (Ruin, Phase 2+ space): Ancient defense installation. Automated weapons
   that attacked the Valorin scouts are thread-builder perimeter defenses. The ruins
   predate all modern factions. Data log placement: LOG.LATTICE series.
   *Intel: The defense installation's power source reveals a stable fracture corridor.*
   *Knowledge graph: connects to Lattice degradation thread.*

**Chain B: The Communion Frequency (4 sites)**
1. **Hook** (Signal, Shimmer-zone): Unusual harmonic in local sensor noise. Communion
   pilots call it "the hum." FO notes it matches no known natural phenomenon.
   Lead -> [Communion border system].
   *Intel: Communion crystal harvesting routes — exotic crystal sources.*
2. **Development** (Ruin, Communion territory): Ancient listening post. The "hum" is
   an accommodation geometry test signal, still broadcasting after millions of years.
   The Communion built their settlements near these signals without knowing why they
   felt drawn there. Lead -> [deep Shimmer-zone].
   *Intel: Accommodation resonance data -> fragment location hint.*
3. **Development** (Signal, deep Shimmer): The frequency resolves into structured data
   when measured with a fracture-exposed scanner. FO: "It's not a signal. It's a
   calibration tone. Something is testing whether local spacetime can sustain
   accommodation geometry." Lead -> [Phase 3 space, requires fracture drive].
   *Intel: Calibration data reduces scanner variance by 2% permanently.*
4. **Climax** (Ruin, Phase 3 space): Accommodation geometry prototype. The test site
   where Vael's team ran their experiments. Still functional. The Haven was built on
   the same principles but at larger scale. Data log placement: LOG.ACCOM series.
   *Intel: Haven approach vector — reveals path to Haven if not yet discovered.*
   *Knowledge graph: connects Communion territory to Accommodation research thread.*

**Chain C: The Pentagon Audit (5 sites)**
> This chain is the primary vehicle for Revelation R3 (pentagon ring discovery).
> The player assembles economic evidence BEFORE reading the data logs that explain it.
> Per LoreContent_TBA.md: "The gameplay is the revelation. The logs are the explanation."

1. **Hook** (Signal, faction border): Trade route anomaly. FO notes that goods flowing
   between two factions follow a suspiciously optimal pattern — as if the supply and
   demand were designed to complement each other.
   *Intel: Optimal cross-faction arbitrage route (+30% margin).*
2. **Development** (Ruin, second faction border): Geological survey data. FO: "The
   mineral deposits in this region are... unusual. The distribution pattern looks
   natural, but the statistical probability of five complementary deficiency rings
   forming by chance is approximately zero."
   *Intel: All 5 faction resource dependencies mapped — full pentagon visible.*
3. **Development** (Derelict, Concord space): Classified Concord intelligence vessel.
   Contains sealed data about thread network resource distribution. Concord marked it
   "THREAD OPTIMIZATION — CLASSIFIED." FO: "They knew. Or at least, someone did."
   *Intel: Concord classified trade routes — bypass tariffs via sealed routes.*
4. **Development** (Ruin, fracture space): Ancient resource processing facility. The
   mineral suppression equipment Senn described. Still operational at residual levels.
   FO: "This machine selectively suppresses geological processes. It's been running
   for millions of years." Data log placement: LOG.ECON.001-003.
   *Intel: Locations where suppression has weakened -> untapped resource deposits.*
5. **Climax** (Ruin, deep fracture): Oruth's administrative archive. The approval
   documents for the pentagon ring. Data log placement: LOG.ECON.004-006.
   *Revelation R3 fires if not already triggered by gameplay.*
   *Knowledge graph: connects all 5 faction resource nodes to ancient engineering.*

**Additional chains (to be designed):**
- Chain D: The Lattice Threshold (3 sites) — degradation engineering, Tal's grief
- Chain E: The Rebel Ship (4 sites) — tracing the accommodation vessel's flight path
- Chain F: The Kesh Confession (3 sites) — Kesh's private logs, moral cowardice arc
- Chain G: The Living Geometry (4 sites) — fragment resonance leading to R5
- Chain H: The Departure Record (3 sites) — why the thread builders left

### Chain Gating

| Chain Tier | Minimum Requirement | Discovery Density |
|------------|-------------------|-------------------|
| Tier 1 (hooks) | Basic scanner, any space | Common — 1 hook per 3-4 systems |
| Tier 2 (development) | Mk1+ scanner, faction reputation | Moderate — requires travel to specific regions |
| Tier 3 (climax) | Mk2+ scanner, fracture drive, or high faction rep | Rare — deep frontier or instability zones |

### Anomaly Difficulty Gating

Directly from Stellaris: anomaly difficulty should be gated by scanner/sensor tier.

| Discovery Difficulty | Required Scanner | Scan Duration | Risk |
|---------------------|-----------------|---------------|------|
| 1-3 (common) | Basic | 1 tick | None |
| 4-6 (uncommon) | Mk1 | 3 ticks | Minor hazard (hull stress) |
| 7-9 (rare) | Mk2 | 5 ticks | Moderate hazard (hull + crew) |
| 10 (legendary) | Mk3 + fracture module | 10 ticks | Severe hazard (instability exposure) |

---

## Automation Graduation (NEW)

> **Core principle (#7):** Automate the routine, preserve the novel.
> **Reference:** Elite Dangerous (what NOT to do), Factorio (automation as progression).

### The Anti-Fatigue Mechanism

The player should never scan the same type of discovery the same way 50 times. The
progression:

1. **First encounter with a family**: Full manual process. FO narrates. Milestone card.
   Audio sting. The player LEARNS what this family means.
2. **2nd-3rd encounter**: Still manual, but faster. FO commentary is shorter. The player
   is developing intuition about the family's risk/reward profile.
3. **4th encounter**: FO suggests automation. "I notice we've scanned several derelicts.
   I could configure a survey program to handle the initial scan automatically."
4. **5th+ encounter**: Player can deploy a **SurveyProgram** (new automation program type)
   that auto-scans Seen discoveries of that family within the program's operating range.
   Results arrive via toast notification. Novel discoveries (chain links, unique finds)
   still require manual analysis.

### SurveyProgram Design

```
Program: SurveyProgram
  Type: Derelict | Ruin | Signal | Any
  Home: [station]
  Range: [hops from home]
  Action: Auto-advance Seen -> Scanned for matching discoveries
  Output: Intel report per scan (appended to IntelBook)
  Limitation: Cannot advance Scanned -> Analyzed (requires player presence)
  Limitation: Cannot process chain links (flagged for manual attention)
```

**Why Analyzed stays manual:** The analysis phase is where narrative happens. Where
connections are revealed. Where the "aha" moment lives. Automating Phase 2 eliminates
tedium. Automating Phase 3 would eliminate meaning.

**FO trigger:** The FO's suggestion is gated by `DiscoveriesScannedOfFamily >= 3`.
This ensures the player has experienced the family manually before automation is offered.
The suggestion is a natural dialogue beat, not a tutorial popup.

---

## Information Asymmetry (NEW)

> **Core principle (#9):** Knowledge decays. Deeper knowledge = deeper advantage.
> **Reference:** EVE Online (probe scanning as competitive edge), X4 (scan for trade data).

### Tiered Economic Intelligence

| Scan Depth | What's Revealed | How It Helps |
|------------|----------------|-------------|
| **Seen** (auto) | System exists, discovery type visible | "Something is there" — curiosity only |
| **Scanned** (manual) | Surface intel: estimated values, nearest buyer, rough margins | Enough to decide whether to send a program |
| **Analyzed** (manual) | Full intel: exact prices, production deficits, supply curves, hidden routes | Enough to deploy optimized automation |
| **Deep Scan** (Mk2+ sensor) | Extended intel: price prediction curves, faction supply chain dependencies, seasonal patterns | Enough to build multi-hop trade empires |

### Economic Fog of War

Without scanning, the player is trading blind at nodes they haven't visited recently.
This is the X4 model: exploration IS economic reconnaissance. The galaxy map should
show economic health indicators ONLY for systems with fresh intel.

```
+-- GALAXY MAP (economic overlay) -------------------------+
|                                                           |
|  Kepler [fresh: 12t ago]    Altair [stale: 180t ago]     |
|  Electronics: 48cr (exact)   Electronics: ~40-55cr (est)  |
|  Margin: +12cr/unit          Margin: ??? (needs rescan)   |
|                                                           |
|  Deneb [UNKNOWN]            Vega [expired: 350t ago]      |
|  "No trade data"            "Data unreliable"             |
|                                                           |
+-----------------------------------------------------------+
```

### Intel Competition

NPC traders also gather intel. The player's exclusive advantage from a discovery
erodes as NPC fleets discover the same routes:

| Discovery Location | NPC Discovery Delay | Player Advantage Window |
|-------------------|--------------------|-----------------------|
| Near starter space | 50-100 ticks | Short — exploit fast |
| Mid frontier | 150-250 ticks | Moderate — deploy automation within 100 ticks |
| Deep frontier | 400-600 ticks | Long — exclusive access for extended period |
| Fracture space | Never (NPCs can't fracture-travel) | Permanent — only the player trades here |

**Design implication:** Fracture-space discoveries are the most valuable because NPCs
can never compete on those routes. This creates a natural economic motivation for
fracture travel beyond the narrative motivation.

---

## Late-Game Discovery Continuation (NEW)

> **Core principle (#12):** Discovery must not dry up.
> **Reference:** Stellaris (what NOT to do — anomalies stop spawning in mature empires).

### Five Sources of Late-Game Discovery

| Source | Trigger | Content |
|--------|---------|---------|
| **Fracture layers** | Player upgrades fracture module | Deeper instability zones reveal sites invisible to basic sensors. Phase 3+ space has discoveries no one else can reach |
| **Faction intelligence** | Reputation milestones | High-rep faction contacts share classified intel: sealed trade routes, hidden caches, military supply chains |
| **Economy-triggered anomalies** | Market saturation, price collapse | When a region's economy stagnates, new anomalies spawn: abandoned stations, refugee signals, smuggler derelicts. Economic failure creates exploration opportunities |
| **Chain unlocks** | Completing earlier chains | Tier 3 chains only become visible after completing related Tier 1-2 chains. The pentagon audit chain is invisible until the player has discovered cross-faction trade anomalies |
| **Instability progression** | Thread degradation advances | As instability phases escalate, previously stable systems develop anomalies. A ruin that was always there but hidden by stable-metric suppression becomes detectable when the local thread degrades to Phase 2+. The Lattice failing literally reveals the past |

### The Instability-Reveals-History Mechanic

This is thematically perfect for our setting. The thread builders' containment
infrastructure literally suppresses information (metric consistency = hiding the
turbulence). As containment fails, information that was suppressed for millions of
years becomes detectable. Ancient sites that were invisible in stable space become
scannable in degraded space.

**Implementation:** Discovery sites seeded at world gen can have an `instability_gate`
field. The site exists in the data model but is invisible to the player until the local
instability phase reaches the gate threshold. From the player's perspective, the ruin
"appeared" when the thread degraded — but it was always there, hidden by the same
infrastructure that kept space stable.

**Narrative resonance:** The player discovers that the containment system doesn't just
stabilize spacetime — it suppresses history. The thread builders' infrastructure hides
the evidence of its own construction. As it fails, it reveals itself. This is the
thematic core of the game: the infrastructure that enables civilization also constrains
it, and its failure reveals truths that stability concealed.

---

## The Knowledge Graph

### What It Is

A relationship map showing how discoveries connect to each other and to game systems.
Inspired by Outer Wilds' Ship Log, where each discovery is a node and connections show
how knowledge relates. **Implemented** in `KnowledgeGraphSystem.cs` and
`KnowledgeGraphContentV0.cs` with revelation-triggered connections (R1/R3/R5).

### Structure

```
+-- INTEL -- KNOWLEDGE WEB --------------------------------+
|                                                           |
|       [Valorin Wreck]                                    |
|       Kepler, Analyzed !                                 |
|            |                                              |
|     "same energy signature"                               |
|            |                                              |
|       [Signal Source]              [Ancient Ruin]         |
|       Altair, Scanned ~     ---- Deneb, Analyzed !       |
|            |                        |                     |
|     "beacon frequency"        "Communion weapon marks"    |
|            |                        |                     |
|       [Unknown]                [Communion Territory]      |
|       ???, Unseen               (faction link)            |
|                                                           |
|  Legend: ! Analyzed  ~ Scanned  . Seen  ??? Unknown       |
+-----------------------------------------------------------+
```

### Connection Types

| Connection | Meaning | Visual |
|------------|---------|--------|
| **Same origin** | Two discoveries from the same ancient event | Solid line |
| **Lead** | One discovery points to another location | Dashed arrow |
| **Faction link** | Discovery connects to a known faction | Dotted line to faction node |
| **Tech unlock** | Discovery reveals a researchable technology | Gold line |
| **Lore fragment** | Discovery adds to a narrative thread | Italic label on connection |
| **Chain link** (NEW) | Discovery is part of an anomaly chain | Bold numbered arrow (1->2->3) |
| **Revelation** (NEW) | Knowledge graph connection revealed by story revelation | Glowing line (appears when R1/R3/R5 fires) |

### Rumor Leads

When a discovery reveals a "lead" (the Signal outcome generates a `discovery_lead_node`),
it creates an entry in the knowledge graph pointing to a new location:

```
! Derelict (Kepler) --> "Signal detected at Altair-7" --> . Unknown (Altair)
```

The lead appears on the galaxy map as a blinking marker at the destination, motivating
the player to travel there. This is the breadcrumb trail that chains exploration into
a narrative rather than random wandering.

---

## Scanner Range & Exploration Frontier

### Scanner as Gameplay Mechanic

The scanner determines how far the player can "see" from their current position.
Upgrading scanners through research expands the knowledge frontier.

| Scanner Level | Range (hops) | Unlocked By |
|---------------|-------------|-------------|
| Basic | 1 hop (adjacent only) | Default |
| Mk1 | 2 hops | Sensors Mk1 research |
| Mk2 | 3 hops | Deep Scan research |
| Mk3 | 5 hops | Advanced Sensors research |

### What Scanning Reveals

| Distance | Information | Update Frequency |
|----------|------------|-----------------|
| Current system | Full real-time data (market prices, fleets, industry, security) | Every tick |
| Within scanner range | System existence, star class, discovery presence, stale market data | On scan (once per visit) |
| Beyond scanner range | Nothing — system shows as "?" on galaxy map | N/A |

### Scanner Sweep Visualization

When the player arrives at a new system, a scanner sweep animation plays:

- Ring of light expands from player's position outward
- As the ring passes through systems within range, they transition from Unknown -> Discovered
- Discovery sites within scanner range get "Seen" status automatically
- Audio: radar ping sound, once per system revealed

This makes the scanner feel like a tool the player is using, not a passive stat.

---

### Scanner Unreliability in Instability Zones

In systems experiencing metric bleed, the scanner itself becomes unreliable —
reinforcing the core theme that measurement breaks down.

| Instability Level | Scanner Effect | Player Experience |
|---|---|---|
| None (stable) | Normal readings, exact values | Baseline — player trusts instruments |
| Low (Drift) | +/-5% variance on market prices, fuel estimates | Subtle — player may not notice immediately |
| Medium (Drift+) | +/-15% variance, discovery phase markers occasionally flicker | Noticeable — player realizes instruments are unreliable |
| High (Fracture) | +/-30% variance, phantom discovery markers appear and vanish, ETAs drift | Alarming — player must rely on experience, not data |
| Critical (Collapse) | Scanner sweeps return contradictory data, prices shown as ranges not values | Visceral — the game's UI is telling you that reality is unstable |

### Implementation Notes

- Variance is cosmetic only — underlying SimCore values remain deterministic
- Displayed values = true value * (1 + random_range(-variance, +variance)), rerolled each
  UI refresh
- Phantom markers: 10% chance per tick in Fracture+ zones to display a false discovery
  marker that disappears on the next refresh
- The First Officer comments on scanner unreliability at each escalation threshold
- This mechanic is NEVER explained in a tutorial — the player discovers it by noticing
  their instruments disagree with reality

---

## Encounter Narratives

### Discovery Family Templates

Each discovery family has a set of narrative templates that give discoveries personality.
Generated deterministically from seed data. **Implemented** in
`DiscoveryOutcomeSystem.cs` with progressive recontextualization per phase.

**Derelict Family:**
- "A [faction] scout vessel, destroyed approximately [age] years ago. [Damage type]
  suggests [enemy faction] weapons."
- "An abandoned cargo hauler. Emergency logs indicate the crew evacuated after
  [event]. The cargo hold still contains [loot]."
- "A warship fragment lodged in an asteroid. Serial markings identify it as
  [ship name], lost during [historical event]."

**Ruin Family:**
- "Ancient structures partially buried under [terrain]. Architecture does not match
  any known faction. [Material] samples suggest [age] years of exposure."
- "A collapsed research outpost. Equipment markings indicate [faction] origin.
  Data cores contain fragmentary records of [research topic]."

**Signal Family:**
- "A repeating beacon on frequency [freq]. Signal analysis suggests artificial origin,
  broadcasting for [duration]. Source direction: [nearby system]."
- "Intermittent energy spikes from [location]. Pattern analysis shows non-random
  structure. Possible [theory]."

### Narrative Chaining

When discoveries connect through leads, their narratives should reference each other:

```
Kepler Derelict: "A Valorin scout vessel..."
          | (lead)
Altair Signal: "The beacon frequency matches the Valorin scout's
                emergency transponder. A second vessel was here."
          | (lead)
Deneb Ruin: "Communion weapon marks on ancient structures match
             the damage pattern on both Valorin vessels."
```

The player assembles the story: the Valorin sent scouts into Communion territory 300 years
ago. Two were destroyed. The ruins predate both factions. There's a deeper mystery.

### Progressive Recontextualization (implemented)

Each discovery family has three layers of narrative that reveal at each phase:

| Phase | Layer | Example (RESOURCE_POOL_MARKER) |
|-------|-------|-------------------------------|
| Seen | Surface text | "Sensor echoes suggest mineral concentrations in this system" |
| Scanned | Deep text | "The mineral pattern is unusual — it looks like an ancient excavation site" |
| Analyzed | Connection text | "This site predates all known civilizations. The excavation tools used accommodation geometry" |

The "oh, THAT's what that was" moment: what seemed like a natural mineral deposit at
Phase 1 is revealed as an ancient mining operation at Phase 3. The player's understanding
of the discovery changes completely without the physical evidence changing at all.

---

## Exploration Progress Tracking

### Empire Dashboard — Intel Tab Enhancement

(Cross-reference: `EmpireDashboard.md` Explore tab)

```
+-- EXPLORATION -------------------------------------------+
|                                                           |
|  Scanner: Mk2 (3 hops) | Systems: 14/22 | 64% explored  |
|                                                           |
|  -- DISCOVERY PROGRESS --                                |
|  Derelicts:  3 Analyzed, 1 Scanned, 0 Seen              |
|  Ruins:      1 Analyzed, 2 Scanned, 1 Seen              |
|  Signals:    2 Analyzed, 0 Scanned, 1 Seen              |
|                                                           |
|  -- ACTIVE CHAINS --     (NEW)                           |
|  Valorin Expedition: 2/3 complete --> next: Deneb        |
|  Communion Frequency: 1/4 complete --> next: Shimmer-7   |
|                                                           |
|  -- ACTIVE LEADS --                                      |
|  -> Signal at Altair-7 (from Kepler Derelict) [Waypoint] |
|  -> Energy at Wolf-3 (from Deneb Ruin)        [Waypoint] |
|                                                           |
|  -- SURVEY PROGRAMS --   (NEW)                           |
|  Derelict survey (Kepler): 2 scans this cycle, +340cr    |
|  Signal survey (Altair): idle (no unseen signals)        |
|                                                           |
|  -- KNOWLEDGE WEB --                                     |
|  [View full knowledge graph]                             |
|                                                           |
|  -- RECENT DISCOVERIES --                                |
|  ! Deneb Ruin — Analyzed 45t ago — Ancient samples +300  |
|  ~ Barnard Signal — Scanned 120t ago — Awaiting analysis |
|  . Kepler Anomaly — Seen 200t ago — Unscanned           |
+-----------------------------------------------------------+
```

### Exploration Completionism vs. Curiosity

The design must balance two player types:

| Player Type | What They Need | Design Approach |
|-------------|---------------|-----------------|
| **Completionist** | "How much have I found?" | Progress bar: 14/22 systems, 6/12 discoveries resolved |
| **Curious** | "What don't I know yet?" | Active leads pointing to new mysteries, "?" markers on map |

Both are served by the same system. The completionist sees "64% explored." The curious
player sees "2 active leads -> Altair-7 and Wolf-3." Neither should feel the game is
pushing them toward the other's playstyle.

**Rule:** Never show a global completion percentage on the HUD. That turns exploration
into a progress bar, which kills curiosity. Show it ONLY in the Intel tab where the
player deliberately looks for it.

---

## Discovery Density Rules — Mid-Game Pacing

The mid-game (hours 3-8) is the exploration loop's most vulnerable period. Players have
exhausted nearby systems but haven't reached deep frontier discoveries. Without careful
density tuning, exploration becomes "fly to empty system, fly to next empty system."

### Density Gradient

| Distance from Start | Discovery Density | Rationale |
|---|---|---|
| 0-1 hops (starter space) | 1 guaranteed discovery in first 2 hops | Tutorial — teaches the scan/analyze loop |
| 2-3 hops (near frontier) | 60% of systems contain discoveries | Mid-game density — maintains exploration momentum |
| 4-5 hops (deep frontier) | 40% of systems contain discoveries | Natural thinning — but offset by higher-value finds |
| 6+ hops (outer reach) | 25% of systems, but all are high-value | Rarity creates anticipation — every find is significant |

### Jump Event Density

Jump events (scanner anomalies, communication fragments, thread turbulence) bridge
the gaps between discovery sites:

| Zone | Jump Event Frequency | Types Available |
|---|---|---|
| Starter space | Every 5th jump | Routine only (calibration, minor readings) |
| Near frontier | Every 3rd jump | Routine + anomaly + faction encounters |
| Deep frontier | Every 2nd jump | Full range including instability hints |
| Outer reach | Nearly every jump | Instability-heavy, ancient signal fragments |

### Anti-Drought Rule

No player should go more than 10 minutes of active play without encountering SOME
narrative touchpoint (discovery site, jump event, faction dialogue, or First Officer
observation). If the current route would create a drought, seed a jump event to bridge
the gap.

### Late-Game Discovery Injection (NEW)

When the player has analyzed 80%+ of seeded discoveries, new sources activate:

1. **Economy-triggered anomalies**: market crashes, route saturation, or faction
   conflict spawn new Derelict/Signal discoveries at affected nodes
2. **Instability-gated reveals**: Phase 2+ instability reveals previously hidden sites
3. **Chain tier escalation**: completing a Tier 1-2 chain reveals Tier 3 chain hooks
4. **Faction intelligence drops**: high-rep NPCs share classified site locations

This prevents the Stellaris problem: mature empires with nothing left to discover.

---

## Discovery Audio & Visual Milestone Feedback

Each phase transition deserves celebration. Cross-reference `AudioDesign.md` for
audio specifications.

### Audio Vocabulary (4 Signatures) — NEW

Discovery needs a distinct audio language with four signatures that the player learns
to distinguish instantly. Reference: Zelda's secret-found jingle, Subnautica's scanner
beep, Metroid Prime's scan visor lock-on.

| Signature | When | Character | Duration |
|-----------|------|-----------|----------|
| **Anomaly Ping** | Scanner detects something at a new system | Short, sharp, attention-grabbing. A radar blip with harmonic overtone. Must be rare enough to trigger immediate attention. Should be audible above ambient music | 0.3s |
| **Scan Process** | During active scan (Phase 1->2 transition) | Tension-building ambient. Rising tone or rhythmic pulse that resolves on completion. Tempo increases as scan approaches completion | 2-5s (scales with scan duration) |
| **Discovery Reveal** | Scan or analysis complete | Brief musical phrase (3-5 notes). Variants by discovery tier. Tier 1 = simple ascending. Tier 2 = more complex, minor key. Tier 3 = full fanfare with reverb | 1-2s |
| **Insight Chime** | Knowledge graph connection formed, or FO explains significance | Gentler, contemplative. Two-note bell-like tone. Signals "here's context" rather than "here's loot." Distinct from the reveal sting | 0.5s |

### Phase Transition Feedback

| Phase Transition | Visual | Audio | Duration |
|-----------------|--------|-------|----------|
| -> Seen (auto) | Marker appears with subtle fade-in | Anomaly Ping | 0.5s |
| -> Scanned (player action) | Marker brightens, scan-line sweeps | Scan Process -> Discovery Reveal | 1.0-5.0s |
| -> Analyzed (player action) | Full reveal card, marker transforms | Discovery Reveal (tier-appropriate) | 2.0s + card |
| Lead discovered | Blinking marker at destination | Insight Chime | 1.0s |
| Chain link found | Chain progress indicator pulses | Insight Chime + chain-specific motif | 1.5s |
| Knowledge connection | Graph line animates between nodes | Insight Chime (softer variant) | 0.5s |
| **Revelation fires** | Full-screen flash, FO speaks | Unique revelation fanfare (reserved for R1-R5 only) | 3.0s + dialogue |

---

## Lore Integration — How Discovery Delivers the Mystery

### The Thread Builder Mystery Arc

Discovery is the primary vehicle for the game's central mystery. The player assembles
understanding through gameplay evidence, data logs, and fragment collection — in that
order. Per `factions_and_lore_v0.md`:

**Layer 1: Physical evidence (exploration)**
- Find derelicts, ruins, signals -> evidence of an ancient civilization
- No explanation yet. Just: "someone was here before, a long time ago"

**Layer 2: Fragments (collection)**
- Collect adaptation fragments -> understand what the ancients BUILT
- Cover-story names ("Regenerative Polymer Sample") until R1 fires
- Dual lore: mundane interpretation flips to alien truth after revelation

**Layer 3: Data logs (narrative)**
- Read conversations between scientists at discovery sites
- Assemble the Containment-vs-Accommodation debate across 25+ logs
- Five named scientists with contradictions revealed across multiple sites
- CRITICAL: logs are private records, never addressed to posterity

**Layer 4: Personal experience (fracture travel)**
- Using the fracture module IS accommodation geometry in action
- The UI distortions in unstable space are the pilot seeing MORE accurately
- The module adapts the pilot neurologically (never stated, only implied)
- Scanner precision improves with cumulative fracture exposure

**Layer 5: Revelations (story state machine)**
- R1 (Module Origin): "The module is not technology I built — it was waiting"
- R3 (Pentagon Ring): "The resource distribution is engineered"
- R5 (Living Geometry): "The instability is not chaos — it's alive"
- Each revelation recontextualizes everything the player has already seen

### Ancient Data Log Placement Rules

Per `content/LoreContent_TBA.md` and `factions_and_lore_v0.md`:

1. **Logs are NEVER addressed to the future.** They are internal records.
2. **No single log reveals a scientist's full contradiction.** Assembly required.
3. **Every log contains at least one mechanical hook** (coordinates, trade intel,
   calibration data, resonance location hint).
4. **Logs are placed at anomaly chain nodes** — the chain leads the player to the
   log, not the other way around.
5. **Revelation tier gating**: Tier 1 logs found in safe space, Tier 3 logs found in
   Phase 3+ space requiring fracture drive. The player must go deeper to learn more.

### The Cover-Story System

Until Revelation R1 fires, all ancient artifacts use mundane cover names:

| Real Name | Cover Name | Flipped At |
|-----------|-----------|-----------|
| Growth Lattice | Regenerative Polymer Sample | R1 |
| Void Girder | Structural Resonance Amplifier | R1 |
| Cascade Core | Exotic Energy Matrix | R1 |
| Pattern Engine | Advanced Computation Substrate | R1 |
| Fracture Drive | Structural Resonance Engine | R1 |

The cover names are completely plausible scientific terms. The player never suspects
these artifacts are alien technology — they seem like advanced materials science. R1
flips all names simultaneously, creating a cascade of "wait, ALL of those were...?"

---

## Anti-Patterns to Avoid

| Anti-Pattern | Game That Failed | Our Rule |
|---|---|---|
| **Exploration as checkbox** | No Man's Sky at launch | Discoveries connect to each other and to faction lore |
| **No knowledge persistence** | Games where the player must take notes | IntelBook records everything with timestamps |
| **Silent phase transitions** | Current implementation | Audio + visual card + brief pause for every milestone |
| **100% completion pressure** | Ubisoft open worlds | Completion % only in Intel tab, never on HUD |
| **Discoveries without narrative** | Procedural games with no context | Every discovery has flavor text from family templates |
| **No breadcrumb trail** | Random exploration without direction | Leads chain discoveries into narrative sequences |
| **Scanner as passive stat** | "Range: 3" with no visual impact | Scanner sweep animation when entering new systems |
| **Scan fatigue** (NEW) | Elite Dangerous FSS | Automation graduation: manual first, programs after 4th scan of a type |
| **Discovery disconnected from economy** (NEW) | Many exploration games | Every discovery yields actionable trade intelligence |
| **One-shot discoveries** (NEW) | Stellaris individual anomalies | Anomaly chains (3-5 sites) create memorable arcs |
| **Discovery dries up** (NEW) | Stellaris mature empires | 5 late-game sources keep discoveries flowing |
| **Pure randomness** (NEW) | No Man's Sky at launch | Constrained pools where players develop intuition |
| **Full explanation** (NEW) | Over-exposited sci-fi | Incomplete knowledge is more compelling. Never name the thread builders |

---

## Reference Games — Expanded

| Mechanic | Best Reference | Key Lesson |
|---|---|---|
| Knowledge graph | **Outer Wilds** Ship Log | Every discovery connects to something — web of knowledge, not a list. The only progression is what you know. Rumor mode creates interconnected curiosity web |
| Discovery phases | **Subnautica** | Find -> scan -> understand lifecycle. PDA as diegetic delivery (our FO fills this role). Scanning is brief but requires proximity, creating a "lean in" moment |
| Scanner as tool | **Mass Effect** planetary scan | Scanning FEELS like an action — visual sweep, audio feedback. But: avoid ME2's tedium (scan every planet for resources) |
| Narrative chaining | **Return of the Obra Dinn** | Fragments assemble into a story the player pieces together. No single fragment tells the whole story |
| Exploration motivation | **Outer Wilds** | Curiosity-driven, not reward-driven. "I wonder what's there" > "+50 XP" |
| Intel freshness | **Elite Dangerous** community tools | Stale data shown differently from fresh — motivates re-scanning |
| Anomaly chains | **Stellaris** | Multi-event chains with branching choices are the most remembered content. One-shots are forgotten immediately |
| Discovery-as-economy | **X4: Foundations** | Scanning a station reveals its buy/sell orders. Exploration directly feeds trading efficiency |
| Information asymmetry | **EVE Online** | Discovered wormhole routes = real economic advantage. Knowledge is perishable and exclusive |
| Anti-fatigue | **Elite Dangerous** (what NOT to do) | Scan #500 must not feel like scan #1. Automate the routine, preserve the novel |
| Pain before relief | **Factorio** | Feel the bottleneck BEFORE the discovery that resolves it. Discovery is the answer to a question the player is already asking |
| Procedural + authored | **No Man's Sky** Desolation update | Derelict freighters: procedural layouts with hand-crafted story skeletons. Every site feels designed because the dramatic arc is authored |
| Constrained randomness | **FTL** | Curated event pools where players develop intuition. 2-4 outcomes per event type. Variance within a known range |
| Planted seeds | **Mass Effect** Eletania ruin | Throwaway in ME1, significant in ME3. Early discoveries contain buried significance |
| Incomplete knowledge | **Mass Effect** Prothean discs | "Scientists cannot yet transcribe." Mystery is the reward. Never fully explain |
| First-discovery credit | **Elite Dangerous** exploration | Player's name permanently on a first-discovery. Cheap to implement, disproportionately motivating |
| Environmental storytelling | **Subnautica** alien facilities | Environment shows what happened; text explains what people thought about it. Neither alone is sufficient |
| Diegetic discovery UI | **Dead Space**, **Subnautica** | The scanning tool exists in the game world. Our FO is the diegetic delivery mechanism |
| Audio discovery palette | **Zelda** secret-found jingle, **Metroid Prime** scan visor | Distinct signatures for detection, scanning, revelation, and insight. Must be rare enough to trigger immediate attention |

---

## SimBridge Queries — Existing and Needed

### Existing (implemented)
| Query | Purpose | Location |
|-------|---------|----------|
| `DispatchScanDiscoveryV0(siteId)` | Advance discovery phase | SimBridge.cs |
| `AdvanceDiscoveryPhaseV0(discoveryId)` | Inline phase advance with reason-code | SimBridge.cs |
| `GetDiscoveryListSnapshotV0()` | All discoveries ordered by ID | SimBridge.Reports.cs |
| `GetDiscoverySnapshotV0(nodeId)` | Discoveries at a node with phase status | SimBridge.TradeIntel.cs |
| `GetDiscoveryOutcomesV0()` | All AnomalyEncounter outcomes | SimBridge.TradeIntel.cs |
| `GetDiscoveryPhaseMarkersV0()` | Per-node phase for galaxy map icons | SimBridge.GalaxyMap.cs |
| `GetAnomalyEncounterSnapshotV0(id)` | Single encounter snapshot | SimBridge.Combat.cs |
| `GetActiveEncountersV0()` | All AnomalyEncounters sorted | SimBridge.Combat.cs |
| `GetFractureDiscoveryStatusV0()` | FractureUnlocked + derelict location | SimBridge.Fracture.cs |
| `GetAvailableVoidSitesV0()` | Discovered/surveyed void sites | SimBridge.Fracture.cs |
| `GetAdaptationFragmentsV0()` | All 16 fragments with collection status | SimBridge.Haven.cs |
| `CollectFragmentV0(fragmentId)` | Collect adaptation fragment | SimBridge.Haven.cs |
| `GetResonancePairsV0()` | 8 pairs with bonus descriptions | SimBridge.Haven.cs |
| `GetTrophyWallV0()` | Collected fragments + resonance completion | SimBridge.Haven.cs |
| `GetIntelFreshnessByNodeV0()` | Per-node intel age | SimBridge.TradeIntel.cs |

### Needed (new design requirements)
| Query | Purpose | Design Section |
|-------|---------|---------------|
| `GetKnowledgeGraphV0()` | All discoveries + connections + chain progress | Knowledge Graph |
| `GetActiveLeadsV0()` | Unresolved leads pointing to undiscovered locations | Rumor Leads |
| `GetExplorationProgressV0()` | Systems visited, discoveries by phase, completion stats | Progress Tracking |
| `GetScannerRangeV0()` | Current scanner level and hop range | Scanner Range |
| `GetDiscoveryNarrativeV0(discoveryId)` | Generated flavor text for a discovery | Encounter Narratives |
| `GetConnectedDiscoveriesV0(discoveryId)` | Discoveries linked to this one | Knowledge Graph |
| `GetDiscoveryTradeIntelV0(discoveryId)` | Economic intel from a discovery (NEW) | Discovery-as-Trade-Intelligence |
| `GetChainProgressV0(chainId)` | Anomaly chain completion status (NEW) | Anomaly Chains |
| `GetActiveChainsV0()` | All in-progress chains with next-step hints (NEW) | Anomaly Chains |
| `GetSurveyProgramStatusV0()` | Active survey programs and their results (NEW) | Automation Graduation |
| `GetIntelDecayStatusV0(nodeId)` | Intel freshness with economic impact estimate (NEW) | Information Asymmetry |
| `GetInstabilityRevealedSitesV0()` | Sites newly visible due to instability (NEW) | Late-Game Discovery |

---

## Implementation Roadmap

### Priority Order (mapped to principles)

| Priority | Epic / Feature | Principles | Why Now |
|----------|---------------|-----------|---------|
| **P0** | Discovery-as-trade-intelligence | #6 (feeds automation) | Core loop alignment — without this, discovery is disconnected from the game's reason for existing |
| **P0** | Audio discovery vocabulary (4 signatures) | #3 (milestones are moments), #10 (audio) | Silent phase transitions waste the player's emotional investment. The audio signatures define the feel of discovery |
| **P1** | Anomaly chains (8-12 templates) | #8 (chains not one-shots) | Most impactful content addition. Transforms exploration from "scan and forget" to "follow the trail" |
| **P1** | SurveyProgram automation graduation | #7 (automate the routine) | Prevents scan fatigue. Directly serves automation core loop |
| **P1** | Discovery milestone cards (visual) | #3 (milestones are moments) | The visual complement to audio. SCAN COMPLETE / ANALYSIS COMPLETE cards |
| **P2** | Information asymmetry / intel decay | #9 (economic weapon) | Creates urgency. Makes exploration economically meaningful beyond loot |
| **P2** | Scanner sweep animation | existing design | Visual frontier that makes the scanner feel like a tool |
| **P2** | Breadcrumb trail visualization | existing design | Visual chain threading on galaxy map |
| **P2** | EPIC.S6.CLASS_DISCOVERY_PROFILES | #14 (constrained randomness) | WorldClass influences discovery families and outcomes |
| **P3** | Late-game discovery injection | #12 (must not dry up) | Economy-triggered anomalies, instability reveals, chain escalation |
| **P3** | EPIC.S6.ANOMALY_ECOLOGY | #14 (constrained randomness) | Procedural anomaly distribution with spatial logic |
| **P3** | EPIC.S6.ARTIFACT_RESEARCH | #15 (incomplete knowledge) | Identification, containment, experiments — deep engagement |
| **P3** | EPIC.S6.SCIENCE_CENTER | relates to #6 | Analysis throughput, reverse engineering — late-game depth |
| **P3** | EPIC.S6.TECH_LEADS | relates to #6 | Tech leads become prototype candidates |
| **P3** | EPIC.S6.EXPEDITION_PROG | relates to #7 | Multi-step expedition programs |
| **P4** | EPIC.S6.MYSTERY_MARKERS | relates to #15 | Mystery style policy — systemic vs explicit markers |
| **P4** | First-discovery credit system | #17 | Player name on first-discoveries. Low effort, high reward |
| **P4** | Exploration overlay lens | existing design | GalaxyMap.md aspiration |

### Implementation Dependencies

```
Discovery-as-Trade-Intelligence
  |
  +-- IntelBook.IntelFreshness (DONE)
  +-- DiscoveryOutcomeSystem (DONE — extend with trade intel fields)
  +-- SurveyProgram
  |     +-- ProgramSystem (DONE — extend with SurveyProgram type)
  |     +-- FO suggestion trigger (FirstOfficerSystem)
  |
  +-- Information Asymmetry
        +-- NPC trade route discovery (NpcTradeSystem — extend)
        +-- Intel decay curves (IntelBook — extend)

Anomaly Chains
  |
  +-- KnowledgeGraphSystem (DONE)
  +-- Chain templates (Content layer — new AnomalyChainContentV0.cs)
  +-- Chain progression tracking (new entity: AnomalyChain.cs)
  +-- DiscoveryOutcomeSystem (DONE — extend with chain Lead generation)
  +-- NarrativePlacementGen (DONE — extend with chain site placement)

Late-Game Discovery
  |
  +-- Instability-gated reveals (DiscoverySeedGen — extend with instability_gate)
  +-- Economy-triggered anomalies (new system: DynamicAnomalySystem.cs)
  +-- Chain tier escalation (AnomalyChain — tier unlock logic)

Audio Vocabulary
  |
  +-- 4 audio signatures (assets — composition/sourcing)
  +-- Phase transition hooks in GameShell (GDScript signal handlers)
  +-- Tier-variant reveal stings (3 variants per signature)
```
