# Exploration & Discovery — Design Bible

> Design doc for the exploration loop: discovery phases, scanner range, knowledge
> management, anomaly encounters, and the visual language of curiosity and revelation.
> Companion to `GalaxyMap.md` (fog of war, exploration overlay) and `factions_and_lore_v0.md`.
> Content authoring specs: `content/NarrativeContent_TBA.md` (discovery text templates),
> `content/LoreContent_TBA.md` (ancient data logs). Epic: `EPIC.S6.UI_DISCOVERY`.

## Why This Doc Exists

Simple exploration (visit nodes, see what's there) works fine without a design doc. But
knowledge tracking — how the player records, reviews, and acts on discoveries — fails
catastrophically without architecture. Outer Wilds' Ship Log is praised because every
discovery connects to something larger. No Man's Sky at launch was criticized because
procedural discoveries had no persistent connections.

This doc defines what the game remembers for the player, what connects to what, and how
discovery milestones feel. It prevents exploration from becoming "checking boxes."

---

## Implementation Status

| Feature | Status | Notes |
|---------|--------|-------|
| IntelBook (observations, routes, prices) | Done | SimCore entity with persistence |
| Three discovery phases (Seen → Scanned → Analyzed) | Done | ScanDiscoveryCommand, phase transitions |
| DiscoverySitePanel UI (phase display, scan button) | Done | Polls per second, shows reward |
| DiscoveryOutcomeSystem (loot generation on analysis) | Done | Family-specific: Derelict/Ruin/Signal |
| Discovery seed types (Resource/Corridor/Anomaly) | Done | Deterministic seeding at world gen |
| Scanner range visualization | Not implemented | No visual frontier on map |
| Discovery phase markers on galaxy map | Not implemented | No ▪/░/✓ icons at node positions |
| Exploration overlay lens | Not implemented | GalaxyMap.md aspiration |
| Knowledge graph / ship log | Not implemented | No relationship view between discoveries |
| Discovery milestone audio/visual feedback | Not implemented | Phase transitions are silent |
| Rumor/lead system | Partial | RumorLeads in IntelBook, not surfaced in UI |
| Encounter narrative text | Not implemented | No flavor text for discoveries |
| Breadcrumb trail visualization | Not implemented | No "here's what led you here" display |

---

## Design Principles

1. **Exploration is knowledge acquisition, not map completion.** The player isn't filling
   in a fog-of-war map — they're building understanding. Each discovery should answer a
   question AND raise a new one. "There's a derelict here" → "What happened to this ship?"
   → "The wreck matches Valorin construction" → "What were the Valorin doing this far from
   their territory?" This is the Outer Wilds philosophy: curiosity, not completion percentage.

2. **The game remembers so the player doesn't have to.** The IntelBook exists so the player
   never needs to write notes. Every observation, price, route, and discovery is recorded
   with timestamps. The game surfaces connections: "You saw a Valorin wreck at Kepler. The
   Valorin faction territory starts 3 hops away at Altair." The player's job is deciding
   what to do with knowledge, not remembering it.

3. **Discovery milestones are moments.** A phase transition (Seen → Scanned, Scanned →
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

---

## The Discovery Lifecycle

### Phase 1: Seen

**Trigger:** Player enters a system with seeded discoveries. Automatic — no player action.

**What the player learns:** Something is here. Type (Derelict/Ruin/Signal) and location.
No details.

**Visual treatment:**
- Galaxy map: dim gray marker (▪) at the node
- Local system: faint marker sphere at discovery position
- Discovery panel: "? Seen" label in muted text

**Emotional beat:** Curiosity. "What is that? Should I investigate?"

### Phase 2: Scanned

**Trigger:** Player clicks "Scan" in the DiscoverySitePanel while at the node.

**What the player learns:** Surface-level data. Material composition, energy signatures,
approximate age. Enough to decide whether to invest time in full analysis.

**Visual treatment:**
- Galaxy map: amber marker (░) at the node
- Local system: marker sphere brightens, gains subtle pulse
- Discovery panel: "~ Scanned" in amber, preview data revealed

**Emotional beat:** Investment. "This looks promising. I want to know more."

**Milestone feedback (aspiration):**
```
┌─ SCAN COMPLETE ────────────────────────────────────┐
│                                                      │
│  ░ Derelict Wreck — Kepler System                   │
│                                                      │
│  "Sensor readings detect a hull fragment with        │
│   residual power. Construction pattern matches       │
│   no known active faction."                          │
│                                                      │
│  Energy signature: Fading (est. 200+ years)         │
│  Material: Unknown composite alloy                   │
│  Size: Small vessel (corvette class)                 │
│                                                      │
│  [Analyze]  [Later]                                  │
└──────────────────────────────────────────────────────┘
```

### Phase 3: Analyzed

**Trigger:** Player clicks "Analyze" in the DiscoverySitePanel. Must be at the node.

**What the player learns:** Full data. Loot recovered, narrative revealed, connections
to other discoveries shown.

**Visual treatment:**
- Galaxy map: green marker (✓) at the node
- Local system: marker transforms into resolved visual (wreck debris, ruin structure)
- Discovery panel: "! Analyzed" in green, full reward text

**Emotional beat:** Revelation. "So THAT'S what happened here."

**Milestone feedback (aspiration):**
```
┌─ ANALYSIS COMPLETE ─────────────────────────────────┐
│                                                       │
│  ✓ Derelict Wreck — Kepler System                    │
│                                                       │
│  "The hull fragment is a Valorin scout vessel,        │
│   destroyed approximately 300 years ago. The damage   │
│   pattern suggests Communion weapons. This far from   │
│   Valorin space, the scout was likely on a covert     │
│   mission when intercepted."                          │
│                                                       │
│  RECOVERED:                                           │
│    Salvaged Tech ×150                                 │
│    Credits: +500                                      │
│                                                       │
│  CONNECTED DISCOVERY:                                 │
│    → Signal detected at Altair-7                      │
│    "Similar energy signature. Possible second vessel" │
│                                                       │
│  [Collect Salvage]  [View in Intel Tab]              │
└───────────────────────────────────────────────────────┘
```

---

## The Knowledge Graph

### What It Is

A relationship map showing how discoveries connect to each other and to game systems.
Inspired by Outer Wilds' Ship Log, where each discovery is a node and connections show
how knowledge relates.

### Structure

```
┌─ INTEL — KNOWLEDGE WEB ────────────────────────────────────────────────┐
│                                                                         │
│       [Valorin Wreck]                                                  │
│       Kepler, Analyzed ✓                                               │
│            │                                                            │
│     "same energy signature"                                             │
│            │                                                            │
│       [Signal Source]              [Ancient Ruin]                       │
│       Altair, Scanned ░     ──── Deneb, Analyzed ✓                    │
│            │                        │                                   │
│     "beacon frequency"        "Communion weapon marks"                  │
│            │                        │                                   │
│       [Unknown]                [Communion Territory]                    │
│       ???, Unseen ▪              (faction link)                        │
│                                                                         │
│  Legend: ✓ Analyzed  ░ Scanned  ▪ Seen  ??? Unknown (lead only)       │
└─────────────────────────────────────────────────────────────────────────┘
```

### Connection Types

| Connection | Meaning | Visual |
|------------|---------|--------|
| **Same origin** | Two discoveries from the same ancient event | Solid line |
| **Lead** | One discovery points to another location | Dashed arrow |
| **Faction link** | Discovery connects to a known faction | Dotted line to faction node |
| **Tech unlock** | Discovery reveals a researchable technology | Gold line |
| **Lore fragment** | Discovery adds to a narrative thread | Italic label on connection |

### Rumor Leads

When a discovery reveals a "lead" (the Signal outcome generates a `discovery_lead_node`),
it creates an entry in the knowledge graph pointing to a new location:

```
✓ Derelict (Kepler) ──→ "Signal detected at Altair-7" ──→ ▪ Unknown (Altair)
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

```
╭─── sweep expanding ───╮
│    ╱   ╲              │
│   ╱  ●  ╲  → reveals  │
│  ╱   ◆   ╲   nearby   │
│ ╱    ▪    ╲  systems  │
│╱           ╲          │
╰─────────────╯
```

- Ring of light expands from player's position outward
- As the ring passes through systems within range, they transition from Unknown → Discovered
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
| Low (Drift) | ±5% variance on market prices, fuel estimates | Subtle — player may not notice immediately |
| Medium (Drift+) | ±15% variance, discovery phase markers occasionally flicker | Noticeable — player realizes instruments are unreliable |
| High (Fracture) | ±30% variance, phantom discovery markers appear and vanish, ETAs drift | Alarming — player must rely on experience, not data |
| Critical (Collapse) | Scanner sweeps return contradictory data, prices shown as ranges not values | Visceral — the game’s UI is telling you that reality is unstable |

### Implementation Notes

- Variance is cosmetic only — underlying SimCore values remain deterministic
- Displayed values = true value × (1 + random_range(-variance, +variance)), rerolled each
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
Generated deterministically from seed data.

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
          ↓ (lead)
Altair Signal: "The beacon frequency matches the Valorin scout's
                emergency transponder. A second vessel was here."
          ↓ (lead)
Deneb Ruin: "Communion weapon marks on ancient structures match
             the damage pattern on both Valorin vessels."
```

The player assembles the story: the Valorin sent scouts into Communion territory 300 years
ago. Two were destroyed. The ruins predate both factions. There's a deeper mystery.

---

## Exploration Progress Tracking

### Empire Dashboard — Intel Tab Enhancement

(Cross-reference: `EmpireDashboard.md` Explore tab)

```
┌─ EXPLORATION ──────────────────────────────────────────────────────────┐
│                                                                         │
│  Scanner: Mk2 (3 hops)  │  Systems: 14/22 visited  │  64% explored   │
│                                                                         │
│  ── DISCOVERY PROGRESS ──                                              │
│  Derelicts:  3 Analyzed, 1 Scanned, 0 Seen                            │
│  Ruins:      1 Analyzed, 2 Scanned, 1 Seen                            │
│  Signals:    2 Analyzed, 0 Scanned, 1 Seen                            │
│                                                                         │
│  ── ACTIVE LEADS ──                                                    │
│  → Signal detected at Altair-7 (from Kepler Derelict)     [Waypoint]  │
│  → Energy reading at Wolf-3 (from Deneb Ruin)             [Waypoint]  │
│                                                                         │
│  ── KNOWLEDGE WEB ──                                                   │
│  [View full knowledge graph]                                           │
│                                                                         │
│  ── RECENT DISCOVERIES ──                                              │
│  ✓ Deneb Ruin — Analyzed 45t ago — Ancient samples + 300 cr          │
│  ░ Barnard Signal — Scanned 120t ago — Awaiting analysis             │
│  ▪ Kepler Anomaly — Seen 200t ago — Unscanned                        │
└─────────────────────────────────────────────────────────────────────────┘
```

### Exploration Completionism vs. Curiosity

The design must balance two player types:

| Player Type | What They Need | Design Approach |
|-------------|---------------|-----------------|
| **Completionist** | "How much have I found?" | Progress bar: 14/22 systems, 6/12 discoveries resolved |
| **Curious** | "What don't I know yet?" | Active leads pointing to new mysteries, "?" markers on map |

Both are served by the same system. The completionist sees "64% explored." The curious
player sees "2 active leads → Altair-7 and Wolf-3." Neither should feel the game is
pushing them toward the other's playstyle.

**Rule:** Never show a global completion percentage on the HUD. That turns exploration
into a progress bar, which kills curiosity. Show it ONLY in the Intel tab where the
player deliberately looks for it.

---

## Discovery Density Rules — Mid-Game Pacing

The mid-game (hours 3–8) is the exploration loop’s most vulnerable period. Players have
exhausted nearby systems but haven’t reached deep frontier discoveries. Without careful
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

---

## Discovery Audio & Visual Milestone Feedback

Each phase transition deserves celebration. Cross-reference `AudioDesign.md` for
audio specifications.

| Phase Transition | Visual | Audio | Duration |
|-----------------|--------|-------|----------|
| → Seen (auto) | Marker appears with subtle fade-in | Quiet radar ping | 0.5s |
| → Scanned (player action) | Marker brightens, scan-line sweeps over it | Discovery chime (rising tone) | 1.0s |
| → Analyzed (player action) | Full reveal card, marker transforms to resolved state | Revelation fanfare (brief, distinct) | 2.0s + card display |
| Lead discovered | Blinking marker appears at lead destination | Curious motif (questioning tone) | 1.0s |

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

---

## Reference Games

| Mechanic | Best Reference | Key Lesson |
|---|---|---|
| Knowledge graph | Outer Wilds Ship Log | Every discovery connects to something — web of knowledge, not a list |
| Discovery phases | Subnautica | Scan → analyze lifecycle with environmental storytelling breadcrumbs |
| Scanner as tool | Mass Effect (planetary scan) | Scanning FEELS like an action — visual sweep, audio feedback |
| Narrative chaining | Return of the Obra Dinn | Fragments assemble into a story the player pieces together |
| Exploration motivation | Outer Wilds | Curiosity-driven, not reward-driven. "I wonder what's there" > "+50 XP" |
| Intel freshness | Elite Dangerous (community tools) | Stale data shown differently from fresh — motivates re-scanning |

---

## SimBridge Queries — Existing and Needed

### Existing
| Query | Purpose |
|-------|---------|
| `GetDiscoverySnapshotV0(nodeId)` | Discoveries at a node with phase status |
| `DispatchScanDiscoveryV0(siteId)` | Advance discovery to Scanned |
| `GetIntelFreshnessByNodeV0()` | Per-node intel age |

### Needed
| Query | Purpose |
|-------|---------|
| `GetKnowledgeGraphV0()` | All discoveries + connections between them |
| `GetActiveLeadsV0()` | Unresolved leads pointing to undiscovered locations |
| `GetExplorationProgressV0()` | Systems visited, discoveries by phase, completion stats |
| `GetScannerRangeV0()` | Current scanner level and hop range |
| `GetDiscoveryNarrativeV0(discoveryId)` | Generated flavor text for a discovery |
| `GetConnectedDiscoveriesV0(discoveryId)` | Discoveries linked to this one |
