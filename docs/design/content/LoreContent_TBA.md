# Lore Content — To Be Authored

> **Status: TO_BE_AUTHORED**
> This document catalogs all world-building and lore content that must be written.
> This is the "what the story IS" content, as opposed to NarrativeContent_TBA.md
> which covers "how the story is told."
>
> Companion to: `factions_and_lore_v0.md` (world design), `NarrativeDesign.md`
> (delivery architecture), `ExplorationDiscovery.md` (discovery lifecycle).

---

## 1. Ancient Data Logs (System READY — Content Partially Authored)

**ID:** `LORE.ANCIENT_LOGS`
**System:** DataLog entity, DataLogContentV0.cs (25 logs, 6 threads), NarrativePlacementGen.cs (BFS placement), KnowledgeGraphSystem.cs (connections), StoryStateMachineSystem.cs (revelation triggers)
**System Ready:** YES (T18, T37, T39) — entity model, placement, knowledge graph, and story state machine all implemented
**Volume:** 25 authored / ~30 target (20-30 conversation scripts)
**Dependencies:** ~~EPIC.S8.STORY_STATE_MACHINE~~ DONE, ~~EPIC.S7.NARRATIVE_DELIVERY~~ DONE
**Priority:** HIGH — primary vehicle for the revelation arc
**Remaining work:** Author remaining TBA logs (Threads C-E incomplete), in-world display UI polish, anomaly chain integration (per ExplorationDiscovery.md v1)

### Format

Data logs are conversations between thread-builder scientists, inspired by Outer Wilds'
Nomai wall texts. Found at discovery sites (Ruins, Derelicts), they build the core
mystery of the game.

> **CRITICAL DESIGN RULE:** Logs are NEVER addressed to the future. They are
> internal records — lab notebooks, arguments between colleagues, daily memos,
> research notes. No log should contain any variation of "someone will find this,"
> "for those who come after," or "I'm recording this because someday..." The
> scientists recorded because that's what scientists do. The player is not the
> intended audience — they are an accidental witness, eavesdropping on private
> moments between people who have been dead for millions of years. Finding
> someone's unguarded frustration is more haunting than finding their prepared
> message to posterity.

Per NarrativeDesign.md: "The player reads conversations between people who lived
thousands of years ago and pieces together what happened."

```
LOG FORMAT:
  log_id: string (stable)
  location: discovery_site_id
  speakers: [Speaker1, Speaker2, ...]
  entries: [
    { speaker: "Kesh", text: "..." },
    { speaker: "Vael", text: "..." },
    ...
  ]
  revelation_tier: 1|2|3 (which act this log unlocks understanding for)
  connected_fragments: [fragment_ids] (which Adaptation Fragments relate)
```

### Mechanical Hooks — Design Rule

Every lore entry must contain at least one mechanical hook: coordinates, trade
intelligence, calibration data, or location hints. Pure atmosphere without
gameplay utility is prohibited. Players who skip lore should miss tactical
advantages. Players who optimize should encounter lore as a side effect.

**Hook categories:**
- **Coordinate hints:** Reference systems, orbital bodies, or sectors that
  correspond to hidden resource caches or discovery sites.
- **Trade intelligence:** Faction supply routes, embargo histories, or price
  anomalies that inform profitable trade decisions.
- **Calibration data:** Recovered sensor matrices or measurement baselines that
  improve scanner variance reduction or anomaly detection.
- **Resonance location hints:** Fragment descriptions that narrow the search
  area for paired fragments (e.g., "within 3 hops of system X").

### Conversation Threads

The logs should be organized into narrative threads that the player assembles
out of order. Each thread tells one aspect of the thread-builder story.

#### Thread A: The Containment Debate (6-8 logs)

The central philosophical conflict. Some thread builders wanted to contain metric
instability (build the threads). Others wanted to accommodate it (build structures
that work regardless of metric state).

```
LOG.CONTAIN.001 (Revelation Tier 1):
  Location: Ruin near Concord space
  Speakers: Kesh (Containment advocate), Vael (Accommodation advocate)
  [Sample from NarrativeDesign.md:]
  Kesh: "The metrics are drifting again in Sector 12. We can stabilize
         with another lattice node, but Vael's team wants to redesign
         the outpost instead."
  Vael: "Redesign is the wrong word. We want to build something that
         doesn't NEED stable metrics. The lattice is a crutch."
  Kesh: "A crutch that has kept 400 billion people alive for six
         millennia. Your 'accommodation geometry' is untested."
  Vael: "Untested because you won't let us test it."

LOG.CONTAIN.002 (Revelation Tier 1):
  Location: Ruin near Weaver space
  "TBA — Escalation of debate. Kesh presents containment data. Vael
   presents accommodation prototype results."
  Mechanical hook: Kesh references calibration site gamma — coordinates
  correlate to system Kepler-7, third orbital body. Resource density:
  anomalous. (Coordinate hint → hidden resource cache)

LOG.CONTAIN.003-008: "TBA"
```

#### Thread B: The Lattice Construction (4-6 logs)

How and why the thread network was built. Technical discussions that the player
won't fully understand until later, but that recontextualize everything.

```
LOG.LATTICE.001 (Revelation Tier 2):
  Location: Derelict near thread nexus
  "TBA — Engineers discussing lattice node placement. References to
   'natural metric boundaries' that the lattice reinforces."
  Mechanical hook: Recovered calibration matrices from lattice node
  instrumentation. Applying to current sensor array yields ±2% variance
  reduction in anomaly detection. (Calibration data → scanner improvement)

LOG.LATTICE.002-006: "TBA"
```

#### Thread C: The Departure (4-6 logs)

Why the thread builders left. The final conversations before they disappeared.

```
LOG.DEPART.001 (Revelation Tier 3):
  Location: Haven starbase
  "TBA — Final discussion. Decision to leave. References to 'those
   who will come after' and 'the lattice will hold for millennia.'"

LOG.DEPART.002-006: "TBA"
```

#### Thread D: The Accommodation Experiments (4-6 logs)

Vael's team running secret accommodation geometry tests. These logs are
found at sites that show impossible engineering — structures that work
despite local metric variance.

```
LOG.ACCOM.001 (Revelation Tier 2):
  Location: Ruin in Phase 2+ space
  "TBA — Excitement about results. Accommodation geometry works.
   But it requires understanding of how metrics vary, not just
   suppressing the variance."
  Mechanical hook: Vael's notes mention fragment resonance detected
  in the Outer Reach. The paired fragment may lie within 3 hops of
  Vega. (Resonance location hint → fragment search area)

LOG.ACCOM.002-006: "TBA"
```

#### Thread E: The Warning (2-4 logs)

Late-stage logs that directly warn about the lattice's limited lifespan.
These are the "oh no" moments.

```
LOG.WARN.001 (Revelation Tier 3):
  Location: Deep void-phase space
  "TBA — Mathematical proof that the lattice has a finite lifespan.
   Estimate: 'sufficient for any civilization to develop accommodation
   technology independently.' The thread builders assumed their successors
   would figure it out."

LOG.WARN.002-004: "TBA"
```

---

## 2. Adaptation Fragment Lore (System READY — Content Authored)

**ID:** `LORE.FRAGMENTS`
**System:** AdaptationFragment entity, AdaptationFragmentContentV0.cs (16 fragments + 8 resonance pairs with full dual cover/revealed lore), AdaptationFragmentSystem.cs (collection + resonance resolution), DiscoverySeedGen.SeedAdaptationFragmentsV0 (worldgen placement), SimBridge.Haven.cs (bridge queries)
**System Ready:** YES (T34) — 16 fragments authored with CoverName/CoverLore + RevealedName/RevealedLore, 8 resonance pairs with gameplay bonuses
**Volume:** 16 fragments (expanded from 12) + 8 resonance pairs — FULLY AUTHORED
**Dependencies:** ~~EPIC.S8.ADAPTATION_FRAGMENTS~~ DONE
**Priority:** ~~HIGH~~ DONE — lore content complete. Remaining: UI polish for fragment inspection panel

### Fragment Catalog

> **Fragment count reduced from 16 to 12 to prevent collection fatigue.**
> Resonance pairs reduced from 8 to 6. Each remaining fragment carries more
> narrative weight. Four fragments consolidated into adjacent entries — specific
> pruning handled in `factions_and_lore_v0.md`.

Per `factions_and_lore_v0.md`, 12 named fragments exist in design:

| # | Name | Location Type | Discovery Context | Resonance Pair |
|---|------|--------------|-------------------|----------------|
| 1 | The Containment Argument | Ruin | Thread nexus system | Pairs with #2 |
| 2 | The Accommodation Thesis | Ruin | Phase 2+ space | Pairs with #1 |
| 3 | The Pentagon Compact | Derelict | Faction border system | Pairs with #4 |
| 4 | The Isolation Doctrine | Signal | Remote frontier system | Pairs with #3 |
| 5 | The Metric Drift Record | Ruin | Unstable system | Pairs with #6 |
| 6 | The Lattice Reading | Derelict | Near lattice node | Pairs with #5 |
| 7 | The Accommodation Lattice | Ruin | Haven approaches | Pairs with #8 |
| 8 | The Stabilization Formula | Signal | Concord core space | Pairs with #7 |
| 9 | The First Communion | Ruin | Communion territory | Pairs with #10 |
| 10 | The Silence Protocol | Derelict | Abandoned sector | Pairs with #9 |
| 11 | The Builder's Intent | Ruin | Weaver territory | Pairs with #12 |
| 12 | The Expansion Record | Derelict | Valorin frontier | Pairs with #11 |

### Fragment Content Format

Each fragment needs:

```
FRAG.001:
  name: "The Containment Argument"
  opaque_name: "Fragment α-1" (shown before identification)
  discovery_text: "TBA — what the player sees when first found"
  identified_text: "TBA — full lore text revealed after analysis"
  gameplay_hint: "TBA — what this fragment teaches about game mechanics"
  resonance_unlock: "TBA — what combining with pair fragment enables"
```

### Content To Be Authored

```
FRAG.001.DISCOVERY: "TBA — The Containment Argument discovery text"
FRAG.001.IDENTIFIED: "TBA — The Containment Argument full lore"
FRAG.001.HINT: "TBA — gameplay implication"
FRAG.001.RESONANCE: "TBA — resonance pair unlock description"
... (12 fragments x 4 fields = 48 entries)
```

### Resonance Pair Descriptions (6 needed)

```
RESONANCE.001 (Fragments 1+2): "Containment + Accommodation"
  Unlock: "TBA — understanding of why both approaches exist"
  Gameplay: "TBA — enables reading instability phase boundaries"

RESONANCE.002-006: "TBA"
```

---

## 3. Haven Starbase Lore (System READY — Content TBA)

**ID:** `LORE.HAVEN`
**System:** Haven entity (T33), HavenSystem.cs, HavenTweaksV0.cs, SimBridge.Haven.cs (tier queries, trophy wall, fragment collection). Haven has 5 upgrade tiers, hangar bays, ancient hull restoration (T34), visual tier system (T40)
**System Ready:** YES (T33-T34, T40) — Haven entity, tier upgrades, fragment collection, ancient hull restoration all implemented
**Volume:** 5 tier descriptions + 10-15 environmental logs — CONTENT TBA
**Dependencies:** ~~EPIC.S8.HAVEN_STARBASE~~ DONE
**Priority:** MEDIUM — endgame location. Systems ready, narrative content not yet authored

### Tier Upgrade Narratives

| Tier | Facility | Narrative |
|------|----------|-----------|
| 0 | Dormant | "TBA — first dock description. Dark corridors, ancient machinery, faint hum." |
| 1 | Laboratory | "TBA — basic analysis restored. Fragment identification becomes possible." |
| 2 | Drydock | "TBA — ship refit bay operational. T3 module installation unlocked." |
| 3 | Research Wing | "TBA — bidirectional thread access. Deep lore terminal activated." |
| 4 | Living Quarters | "TBA — Haven becomes a functional base. Final ancient records accessible." |

### Environmental Logs

Found throughout Haven at various tiers:

```
HAVEN.LOG.001 (Tier 0): "TBA — automated systems log. Last entry: millennia ago."
HAVEN.LOG.002 (Tier 1): "TBA — lab notebook. Research on accommodation geometry."
... (10-15 logs total, gated by tier upgrades)
```

---

## 4. Faction Backstory Expansion (System Partially Ready)

**ID:** `LORE.FACTION_BACKSTORY`
**System:** `FactionTweaksV0.cs` has species/philosophy fields; no extended lore delivery
**System Ready:** PARTIAL — mechanical identity exists, narrative identity absent
**Volume:** 5 faction histories (1-2 pages each)
**Dependencies:** EPIC.S7.NARRATIVE_DELIVERY for display
**Priority:** MEDIUM

### Content Needed Per Faction

| Faction | History Topic | Connection to Main Mystery |
|---------|--------------|---------------------------|
| **Concord** | Founding as thread-order federation; suppression of instability knowledge | Concord KNOWS about the lattice decay and is covering it up |
| **Chitin** | Hive evolution; probability-based adaptation philosophy | Chitin probability models are detecting thread decay patterns |
| **Weavers** | Construction using thread-compatible materials; builder culture | Weaver materials accidentally mirror accommodation geometry |
| **Valorin** | Frontier expansion; first contact with void space | Valorin scouts found ancient sites first but couldn't read them |
| **Communion** | Direct experience of metric variance; mystic tradition | Communion mystics are closest to understanding the truth |

**Mechanical hook rule applies to faction backstories.** Each history must embed
trade intelligence that rewards attentive readers. Examples:
- Chitin: "The Hive ceased rare mineral shipments through Altair after the
  Concordance Embargo. Informed captains reroute via Deneb." (Trade route hint)
- Valorin: "Frontier surveys cataloged anomalous resource concentrations at
  three systems along the Vega corridor. Official records were sealed."
  (Coordinate hint → hidden caches)
- Concord: "Standard tariff schedules list Weaver composite alloys at 40% markup
  through Concord hubs. Direct Weaver stations sell at base cost." (Price intel)

```
FACTION.CONCORD.HISTORY: "TBA — 1-2 pages"
FACTION.CHITIN.HISTORY: "TBA — 1-2 pages"
FACTION.WEAVERS.HISTORY: "TBA — 1-2 pages"
FACTION.VALORIN.HISTORY: "TBA — 1-2 pages"
FACTION.COMMUNION.HISTORY: "TBA — 1-2 pages"
```

---

## 5. Endgame Path Narratives (System READY — Content TBA)

**ID:** `LORE.ENDGAME`
**System:** StoryStateMachineSystem.cs (5 revelations, 3 acts), WinConditionSystem.cs (Reinforce/Naturalize/Renegotiate paths), WinRequirementsTweaksV0.cs (path requirements), SimBridge.Story.cs (story state queries)
**System Ready:** YES (T37, T39) — story state machine, win conditions, and endgame paths all implemented
**Volume:** 3 path narratives + resolution text — CONTENT TBA
**Dependencies:** ~~EPIC.S8.WIN_SCENARIOS~~ DONE, ~~EPIC.S8.STORY_STATE_MACHINE~~ DONE
**Priority:** MEDIUM — systems ready, narrative content needed for endgame payoff

### Three Paths (from factions_and_lore_v0.md)

```
ENDGAME.REINFORCE:
  Name: "Reinforce the Lattice"
  Alignment: Concord-adjacent (order, stability)
  Narrative: "TBA — Strengthen the existing thread network. Buy time.
   The lattice will last another millennium. But the underlying
   problem remains."
  Resolution: "TBA — bittersweet ending. Stability preserved at the
   cost of potential. The galaxy remains in its cage."

ENDGAME.NATURALIZE:
  Name: "Build Accommodation"
  Alignment: Frontier/Weaver-adjacent (engineering, adaptation)
  Narrative: "TBA — Apply ancient accommodation geometry to modern
   infrastructure. Threads become optional as structures learn to
   exist in variable metric space."
  Resolution: "TBA — hopeful ending. Difficult transition, but the
   galaxy gains true freedom. Threads fade over centuries."

ENDGAME.RENEGOTIATE:
  Name: "Contact the Instability"
  Alignment: Communion-adjacent (understanding, communication)
  Narrative: "TBA — The instability isn't chaos — it's the natural
   state of spacetime. The Communion was right: it can be
   understood, even communicated with."
  Resolution: "TBA — transcendent ending. The galaxy and its metric
   variance find symbiosis. Everything changes."
```

---

## 6. Warfront Commentary (System Partially Ready)

**ID:** `LORE.WARFRONT_COMMENTARY`
**System:** Warfront systems exist; no narrative commentary layer
**System Ready:** PARTIAL — events fire but carry no text
**Volume:** ~20 commentary lines
**Dependencies:** EPIC.S7.NARRATIVE_DELIVERY
**Priority:** LOW

### Commentary Templates

Faction-flavored warfront commentary for toasts and intel briefings:

```
WAR.CONCORD.ADVANCE: "Concord forces have secured {system}. 'Order restored,' says the communique."
WAR.CONCORD.RETREAT: "Concord has withdrawn from {system}. No official statement."
WAR.CHITIN.ADVANCE: "The Hive has assimilated {system}. Probability of resistance: declining."
WAR.CHITIN.RETREAT: "Hive forces have recalculated. {system} deemed non-optimal."
WAR.VALORIN.ADVANCE: "Valorin vanguard has taken {system}. Frontier expands."
WAR.VALORIN.RETREAT: "Valorin forces fell back from {system}. 'A tactical repositioning.'"
... (4 event types x 5 factions = 20 entries, many TBA)
```

---

## 7. Pentagon Ring Evidence (System NOT Ready)

**ID:** `LORE.PENTAGON_EVIDENCE`
**System:** Ancient data log system (same as LORE.ANCIENT_LOGS)
**System Ready:** NO — shares dependencies with Ancient Data Logs
**Volume:** 4-6 logs + gameplay trigger system
**Dependencies:** EPIC.S8.STORY_STATE_MACHINE, fracture-space trade mechanics
**Priority:** HIGH — supports the game's central narrative twist

### Design Context

The pentagon dependency ring is engineered — designed by the same intelligence
that built the threads. The player discovers this through **gameplay first**
(fracture-space trade breaks the ring pattern, a Communion station produces its
own Food) and data logs **confirm** afterward. The logs are NOT the revelation.
The gameplay is the revelation. The logs are the explanation.

### Thread F: Economic Topology (4-6 logs)

These logs show ancient engineers discussing the economic design of the
containment system. They should feel like internal planning documents, not
grand pronouncements.

```
LOG.ECON.001 (Revelation Tier 2):
  Location: Ruin near faction border system
  Speakers: Oruth (project lead), Tal (infrastructure engineer)
  Oruth: "The containment lattice stabilizes metric fields along
         defined corridors. But we're still seeing independent
         resource development in the interstitial volumes."
  Tal:   "Define 'independent.'"
  Oruth: "Three substrate clusters have achieved productive
         self-sufficiency within 200 cycles. They don't need
         the corridors for material exchange. They've found
         local alternatives."
  Tal:   "And the problem with that is...?"
  Oruth: "The problem is that self-sufficient clusters don't
         maintain the lattice. Why would they? It costs resources
         to support infrastructure you don't use."
  [Log ends]

LOG.ECON.002 (Revelation Tier 2):
  Location: Derelict near thread nexus
  Speakers: Oruth, Senn (experimentalist)
  Senn:  "I've run the topology optimization. If we redistribute
         the mineral profiles to create complementary deficiency
         patterns—"
  Oruth: "In plain language, Senn."
  Senn:  "Make each region need something only its neighbor has.
         Circular dependency. They maintain the corridors because
         they need the corridors. The lattice becomes self-funding."
  Oruth: "And the clusters that are already self-sufficient?"
  Senn:  "The metric correction fields can be tuned to suppress
         specific geological processes. Selectively. It would look
         natural — like the local geology simply doesn't support
         certain mineral formation."
  Oruth: "You're describing economic engineering on a civilizational
         scale."
  Senn:  "I'm describing infrastructure maintenance. The lattice
         needs users. Users need incentives. This provides both."
  [Log ends]

LOG.ECON.003 (Revelation Tier 3):
  Location: Ruin in deep space
  Speakers: Vael (accommodation advocate), Kesh
  Vael:  "I found Senn's topology reports. The resource distribution
         isn't geological. It's engineered."
  Kesh:  "I know."
  Vael:  "You KNOW? How long have you known?"
  Kesh:  "Since cycle 3,800. The mineral surveys from Site Theta
         showed impossible distribution patterns. No natural process
         creates complementary deficiency rings across five stellar
         clusters."
  Vael:  "And you didn't—"
  Kesh:  "What would I have said? 'The containment system includes
         economic dependency by design'? The Council would classify
         it. They'd classify me."
  [Log ends]

LOG.ECON.004-006: "TBA — Escalation. The Adaptation faction's realization
that accommodation geometry doesn't just enable off-thread travel — it enables
off-system economics. Freedom from the threads IS freedom from the dependency
ring. This is what the Containment faction was actually afraid of."
```

### Player Discovery Sequence

1. **Gameplay trigger (primary):** Player establishes fracture-space trade
   route. A Communion station in fracture space begins producing goods it
   "shouldn't" be able to produce. The pentagon ring breaks locally. UI
   notification: "Waystation [name] has developed local Food production.
   Concord supply dependency reduced."

2. **Data logs (confirmation):** After the gameplay trigger, ancient logs
   about economic topology become available at nearby discovery sites. The
   player reads engineering documents that explain what they just observed.

3. **Never the reverse:** The player should NEVER read about the engineered
   ring before seeing it break through gameplay. The experience reveals.
   The text confirms.

---

## Summary (Updated 2026-03-20)

| Block | ID | Volume | System Ready | Content Status | Priority |
|-------|-----|--------|-------------|---------------|----------|
| Ancient Data Logs | LORE.ANCIENT_LOGS | 25/30 scripts | YES (T18) | 80% authored | HIGH — finish remaining threads |
| Pentagon Ring Evidence | LORE.PENTAGON_EVIDENCE | 3/6 logs | YES (T18) | 50% authored | HIGH — LOG.ECON.004-006 TBA |
| Adaptation Fragment Lore | LORE.FRAGMENTS | 16 frags + 8 pairs | YES (T34) | DONE | -- |
| Haven Starbase Lore | LORE.HAVEN | 0/15-20 entries | YES (T33-T40) | TBA | MEDIUM |
| Faction Backstories | LORE.FACTION_BACKSTORY | 0/5 histories | PARTIAL | TBA | MEDIUM |
| Endgame Path Narratives | LORE.ENDGAME | 0/3 paths | YES (T37-T39) | TBA | MEDIUM |
| Warfront Commentary | LORE.WARFRONT_COMMENTARY | ~20 lines | PARTIAL | TBA | LOW |
| **Total** | | **~125-150** | **5/7 READY** | **~35% authored** | |

**Key change from v0:** Most systems marked "NOT Ready" are now fully implemented.
The bottleneck has shifted from engineering to content authoring. Fragment lore is
complete. Data logs are mostly authored. Haven, endgame, and faction backstory content
are the primary remaining writing tasks.
