# Lore Content — To Be Authored

> **Status: TO_BE_AUTHORED**
> This document catalogs all world-building and lore content that must be written.
> This is the "what the story IS" content, as opposed to NarrativeContent_TBA.md
> which covers "how the story is told."
>
> Companion to: `factions_and_lore_v0.md` (world design), `NarrativeDesign.md`
> (delivery architecture), `ExplorationDiscovery.md` (discovery lifecycle).

---

## 1. Precursor Data Logs (System NOT Ready)

**ID:** `LORE.PRECURSOR_LOGS`
**System:** No data log entity or display system exists
**System Ready:** NO — needs entity model + Discovery Web UI
**Volume:** 20-30 conversation scripts
**Dependencies:** EPIC.S8.STORY_STATE_MACHINE, EPIC.S7.NARRATIVE_DELIVERY
**Priority:** HIGH — primary vehicle for the revelation arc

### Format

Data logs are conversations between Precursor scientists, inspired by Outer Wilds'
Nomai wall texts. Found at discovery sites (Ruins, Derelicts), they build the core
mystery of the game.

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

### Conversation Threads

The logs should be organized into narrative threads that the player assembles
out of order. Each thread tells one aspect of the Precursor story.

#### Thread A: The Containment Debate (6-8 logs)

The central philosophical conflict. Some Precursors wanted to contain metric
instability (build the lanes). Others wanted to accommodate it (build structures
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

LOG.CONTAIN.003-008: "TBA"
```

#### Thread B: The Lattice Construction (4-6 logs)

How and why the lane network was built. Technical discussions that the player
won't fully understand until later, but that recontextualize everything.

```
LOG.LATTICE.001 (Revelation Tier 2):
  Location: Derelict near lane nexus
  "TBA — Engineers discussing lattice node placement. References to
   'natural metric boundaries' that the lattice reinforces."

LOG.LATTICE.002-006: "TBA"
```

#### Thread C: The Departure (4-6 logs)

Why the Precursors left. The final conversations before they disappeared.

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
   technology independently.' The Precursors assumed their successors
   would figure it out."

LOG.WARN.002-004: "TBA"
```

---

## 2. Adaptation Fragment Lore (System NOT Ready)

**ID:** `LORE.FRAGMENTS`
**System:** No fragment entity exists
**System Ready:** NO — needs EPIC.S8.ADAPTATION_FRAGMENTS
**Volume:** 16 fragment entries + 8 resonance pair descriptions
**Dependencies:** EPIC.S8.ADAPTATION_FRAGMENTS
**Priority:** HIGH — core collectible/knowledge system

### Fragment Catalog

Per `factions_and_lore_v0.md`, 16 named fragments exist in design:

| # | Name | Location Type | Discovery Context | Resonance Pair |
|---|------|--------------|-------------------|----------------|
| 1 | The Containment Argument | Ruin | Lane nexus system | Pairs with #2 |
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
| 13 | The Probability Archive | Signal | Chitin territory | Pairs with #14 |
| 14 | The Certainty Cost | Ruin | War-scarred system | Pairs with #13 |
| 15 | The Departure Manifest | Derelict | Haven starbase | Pairs with #16 |
| 16 | The Last Message | Signal | Deep void space | Pairs with #15 |

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
... (16 fragments x 4 fields = 64 entries)
```

### Resonance Pair Descriptions (8 needed)

```
RESONANCE.001 (Fragments 1+2): "Containment + Accommodation"
  Unlock: "TBA — understanding of why both approaches exist"
  Gameplay: "TBA — enables reading instability phase boundaries"

RESONANCE.002-008: "TBA"
```

---

## 3. Haven Starbase Lore (System NOT Ready)

**ID:** `LORE.HAVEN`
**System:** No Haven entity exists
**System Ready:** NO — needs EPIC.S8.HAVEN_STARBASE
**Volume:** 5 tier descriptions + 10-15 environmental logs
**Dependencies:** EPIC.S8.HAVEN_STARBASE
**Priority:** MEDIUM — endgame location

### Tier Upgrade Narratives

| Tier | Facility | Narrative |
|------|----------|-----------|
| 0 | Dormant | "TBA — first dock description. Dark corridors, ancient machinery, faint hum." |
| 1 | Laboratory | "TBA — basic analysis restored. Fragment identification becomes possible." |
| 2 | Drydock | "TBA — ship refit bay operational. T3 module installation unlocked." |
| 3 | Research Wing | "TBA — bidirectional lane access. Deep lore terminal activated." |
| 4 | Living Quarters | "TBA — Haven becomes a functional base. Final Precursor records accessible." |

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
| **Concord** | Founding as lane-order federation; suppression of instability knowledge | Concord KNOWS about the lattice decay and is covering it up |
| **Chitin** | Hive evolution; probability-based adaptation philosophy | Chitin probability models are detecting lane decay patterns |
| **Weavers** | Construction using lane-compatible materials; builder culture | Weaver materials accidentally mirror accommodation geometry |
| **Valorin** | Frontier expansion; first contact with void space | Valorin scouts found Precursor sites first but couldn't read them |
| **Communion** | Direct experience of metric variance; mystic tradition | Communion mystics are closest to understanding the truth |

```
FACTION.CONCORD.HISTORY: "TBA — 1-2 pages"
FACTION.CHITIN.HISTORY: "TBA — 1-2 pages"
FACTION.WEAVERS.HISTORY: "TBA — 1-2 pages"
FACTION.VALORIN.HISTORY: "TBA — 1-2 pages"
FACTION.COMMUNION.HISTORY: "TBA — 1-2 pages"
```

---

## 5. Endgame Path Narratives (System NOT Ready)

**ID:** `LORE.ENDGAME`
**System:** No win condition / story state machine exists
**System Ready:** NO — needs EPIC.S8.WIN_SCENARIOS + EPIC.S8.STORY_STATE_MACHINE
**Volume:** 3 path narratives + resolution text
**Dependencies:** EPIC.S8.WIN_SCENARIOS
**Priority:** LOW — late-game content

### Three Paths (from factions_and_lore_v0.md)

```
ENDGAME.REINFORCE:
  Name: "Reinforce the Lattice"
  Alignment: Concord-adjacent (order, stability)
  Narrative: "TBA — Strengthen the existing lane network. Buy time.
   The lattice will last another millennium. But the underlying
   problem remains."
  Resolution: "TBA — bittersweet ending. Stability preserved at the
   cost of potential. The galaxy remains in its cage."

ENDGAME.NATURALIZE:
  Name: "Build Accommodation"
  Alignment: Frontier/Weaver-adjacent (engineering, adaptation)
  Narrative: "TBA — Apply Precursor accommodation geometry to modern
   infrastructure. Lanes become optional as structures learn to
   exist in variable metric space."
  Resolution: "TBA — hopeful ending. Difficult transition, but the
   galaxy gains true freedom. Lanes fade over centuries."

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

## Summary

| Block | ID | Volume | System Ready | Priority |
|-------|-----|--------|-------------|----------|
| Precursor Data Logs | LORE.PRECURSOR_LOGS | 20-30 scripts | NO | HIGH |
| Adaptation Fragment Lore | LORE.FRAGMENTS | 64 entries | NO | HIGH |
| Haven Starbase Lore | LORE.HAVEN | 15-20 entries | NO | MEDIUM |
| Faction Backstories | LORE.FACTION_BACKSTORY | 5 histories | PARTIAL | MEDIUM |
| Endgame Path Narratives | LORE.ENDGAME | 3 paths | NO | LOW |
| Warfront Commentary | LORE.WARFRONT_COMMENTARY | ~20 lines | PARTIAL | LOW |
| **Total** | | **~130-150** | | |
