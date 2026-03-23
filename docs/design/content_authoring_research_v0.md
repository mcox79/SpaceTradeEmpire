# Content Authoring Research — Deep Dive & Recommendations

**Date**: 2026-03-21
**Purpose**: Comprehensive research synthesis for all remaining content authoring needs.
**Sources**: Codebase audit of existing lore/systems + industry best practices from 30+ AAA/indie titles.

---

## Table of Contents

1. [Current Content Inventory — What's Done vs TBA](#1-current-content-inventory)
2. [NARRATIVE_CONTENT — Haven Logs, Endgame Narratives, Expanded Lore](#2-narrative-content)
3. [TEMPLATE_MISSIONS — 50-65 Authored Mission Templates](#3-template-missions)
4. [FACTION_STORYLINES — 5 Faction Chains × 8 Missions](#4-faction-storylines)
5. [MISSION_POLISH — FO Commentary & Consequence Propagation](#5-mission-polish)
6. [FACTION_IDENTITY_REDESIGN — 40 T2 Modules, 5 Signature Mechanics, Ship Variants](#6-faction-identity-redesign)
7. [MEGAPROJECT_SET — Canonical Megaproject Types](#7-megaproject-set)
8. [CONTENT_WAVES — Final Archetype Families](#8-content-waves)
9. [MUSIC — Soundtrack Composition](#9-music)
10. [Authoring Volume Estimates & Priority Order](#10-authoring-volume)

---

## 1. Current Content Inventory

### What's COMPLETE (code + lore fully defined)

| System | Status | Key Files |
|--------|--------|-----------|
| Pentagon ring (5 factions + dependency chain) | COMPLETE | `factions_and_lore_v0.md`, `FactionTweaksV0.cs` |
| Faction lore (philosophy, species, core truths) | COMPLETE | `factions_and_lore_v0.md` |
| Trade goods (11 goods, lore significance) | COMPLETE | `trade_goods_v0.md`, `ContentRegistryLoader.cs` |
| Ship modules (54 modules, 9 categories) | COMPLETE | `ship_modules_v0.md`, `ModuleContentV0.cs` |
| First Officer (3 archetypes, 45+ trigger tokens) | COMPLETE | `FirstOfficerContentV0.cs` |
| Discovery/Exploration framework | COMPLETE | `ExplorationDiscovery.md` |
| Adaptation fragments (12 fragments) | COMPLETE | `NarrativeDesign.md` |
| Dynamic tension (5 phases, lattice decay) | COMPLETE | `dynamic_tension_v0.md` |
| Haven starbase (4 tiers, design doc) | COMPLETE | `haven_starbase_v0.md` |
| Market pricing pipeline | COMPLETE | `market_pricing_v0.md` |
| Combat mechanics | COMPLETE | `combat_mechanics_v0.md` |
| Win conditions (3 paths: Reinforce/Naturalize/Renegotiate) | COMPLETE | `NarrativeDesign.md` |
| Faction equipment & research trees | COMPLETE | `faction_equipment_and_research_v0.md` |
| Tutorial/onboarding (45 phases) | COMPLETE | `TutorialContentV0.cs` |
| Reputation & faction relations | COMPLETE | `reputation_factions_v0.md` |
| Camera & cinematics | COMPLETE | `camera_cinematics_v0.md` |

### What's PLACEHOLDER / TBA

| System | Status | Key Files |
|--------|--------|-----------|
| Lore content (environmental text, data entries) | TBA | `docs/design/content/LoreContent_TBA.md` |
| Narrative content (dialogue, mission text, logs) | TBA | `docs/design/content/NarrativeContent_TBA.md` |
| Visual content (VFX, particle, shader specs) | TBA | `docs/design/content/VisualContent_TBA.md` |
| Audio content (music, SFX, ambience specs) | TBA | `docs/design/content/AudioContent_TBA.md` |
| Mission templates (15-20 designed, 50-65 needed) | PARTIAL | `mission_design_v0.md` |
| Faction storyline chains (sketched, not authored) | PARTIAL | `mission_design_v0.md` |
| Haven logs (tier structure exists, no authored text) | TBA | — |
| Megaproject definitions (3 types mentioned, not detailed) | TBA | — |
| Faction T2 modules (framework exists, 40 needed) | TBA | — |
| Knowledge web entries (69 defined, 0 revealed in play) | PARTIAL | `KnowledgeGraphContentV0.cs` |

---

## 2. NARRATIVE_CONTENT — Haven Logs, Endgame Narratives, Expanded Lore

### 2a. Haven Logs — Three-Layer Architecture

**Industry reference**: Subnautica (Degasi logs), Dragon Age: Inquisition (Skyhold), BioShock (audio logs)

**Core principle**: Logs should reveal the *emotional interior of people who experienced something*, not explain the world. "The recycler efficiency dropped to 40% today. Vael says we need to decide by next cycle whether to evacuate the eastern ring." is better than "Haven was built as an Accommodation facility."

#### Layer A — Ancient Logs (Fixed Content, Discovered at Tier Unlocks)

These are records from the civilization that built Haven (the Accommodation builders). They function like Subnautica's Degasi logs: emotionally specific, NOT expository.

| Tier | Logs | Narrative Function |
|------|------|--------------------|
| Tier 1 (Power-up) | 3 logs | **Routine + emergency**: 2 operational status reports (establishes Haven was *lived in*), 1 emergency log from final days (the builders *chose to leave*, they didn't die) |
| Tier 2 (Research Lab) | 4 logs | **Scientific debate**: 3 research logs arguing about metric foam theory, 1 personal log from a researcher who *disagreed* with the project (moral complexity — even the right side had doubters) |
| Tier 3 (Drydock) | 3 logs | **Engineering revelation**: 2 construction records for the Haven thread itself (reveals the bidirectional thread WAS built before — player can rebuild it), 1 final transmission to unknown recipients (possibly the containment builders — the ancient schism) |
| Tier 4 (Resonance Chamber) | 4 logs | **Philosophical depth**: 3 philosophical logs about what the threads will eventually do, 1 redacted/corrupted fragment (the builders knew the full implications) |

**Tone guidance per log type**:
- Operational logs: Clipped, professional, details about mundane maintenance. The mundanity IS the point — these were real people, not monuments.
- Emergency logs: Controlled urgency. Not panic. The decision to leave was deliberate.
- Research logs: Genuine intellectual passion. Arguments between colleagues. One scientist excited, one cautious.
- Personal logs: Private, unguarded. A researcher recording what they'll do when they get home. The gap between their expectations and what the player knows creates dramatic irony (BioShock technique).
- Philosophical logs: Careful, weighty prose. These people understood the magnitude of what they were building. Modal uncertainty — "if the threading holds" not "when the threading holds."

**Sample ancient log** (Tier 1, Operational):

> *Accommodation Station 7, Cycle 4,281. Atmospheric processing nominal. Eastern ring pressure differential resolved — turned out to be a gasket, not structural. Keeva insists we log every pressure variance as if it's the threading itself. I told her the threading has held for eleven hundred cycles. She said that's exactly why we should worry. I'm logging this because she was right about the conduit array last quarter.*

#### Layer B — FO Haven Reflective Entries (Generated at Triggers)

As the player uses Haven, the First Officer generates personal journal entries. NOT tutorial tips — private observations that reward the player who reads them.

**Triggers for FO Haven entries** (×3 FO types = 18-24 entries):
- First dock at Haven (awe/assessment)
- After first ancient log discovered (reaction to the builders)
- After first faction NPC visits Haven (Tier 3+)
- After Haven takes combat damage from Lattice drones
- After completing each endgame path requirement while docked
- After pentagon break event (if docked at Haven)
- After reaching max FO relationship tier
- After discovering the final adaptation fragment

**Tone per FO archetype**:
- **Analyst**: Starts clinical, gradually becomes less clinical. Entry 1: efficiency metrics. Entry 6: "I'm calculating the probability that we survive this, and the number isn't the interesting part. The interesting part is that I don't want to stop."
- **Veteran**: Starts as threat assessment, gradually becomes something resembling peace. Entry 1: defensive perimeter analysis. Entry 6: "First place I've been since the war where the walls feel like they're keeping something out instead of keeping me in."
- **Pathfinder**: Starts as discovery notes, gradually becomes personal. Entry 1: cataloguing anomalous readings. Entry 6: "I've been everywhere. I think this is the first place that felt like it was waiting for me specifically."

#### Layer C — Player Journey Chronicle (Auto-Generated)

A "Haven Chronicle" panel that accumulates evidence of the player's decisions:
- Resource contributions: "Ore Delivery #1 — Tier 2 expansion. 40 units Rare Metals sourced from Valorin space."
- Combat events: "First Haven Combat — Lattice drone incursion repelled. Hull damage: 23."
- Crew arrivals: "Crew arrival: [Name] from [Faction], recruited after [mission]."
- Artifacts installed: "Fragment #7 — 'Mass Coherence Protocol.' Origin: [System Name]."

This is zero-authoring-cost content that makes Haven feel personal to this specific playthrough.

### 2b. Endgame Narratives — The Three Paths

**Industry reference**: Outer Wilds (knowledge-gated endgame), Mass Effect 2 (Suicide Mission), Fallout: New Vegas (four-faction endings), Endless Space 2 (concurrent faction narrative)

**Core principle**: The endgame should require the player to *understand* something, not just *accumulate* something. The three paths should feel like different *kinds of understanding*, not different resource thresholds.

#### Knowledge Gates (Beyond Mechanical Requirements)

Each path should require discovering specific knowledge before the path-selection moment is available. The FO says: "I don't think we understand the situation well enough to commit to this."

| Path | Knowledge Gate | What Player Must Have Discovered |
|------|---------------|----------------------------------|
| Reinforce | Fragments 1-6 + Concord Archive data | Containment IS failing; intervention is needed |
| Naturalize | Fragments 7-10 + Communion U7 mission chain | The ancient compact — threads were *negotiated*, not imposed |
| Renegotiate | All 12 fragments + Valorin V7 + Resonance Chamber Tier 4 | Full accommodation physics; contact is possible |

#### Asymmetric Pre-Endgame Consequences

The three paths should look different in the *world* during the last 10% of the game:

- **Reinforce**: Player must shore up containment infrastructure — which means actively *reducing* fracture travel, paying tribute to Concord, watching pentagon dependency tighten. The path to stabilization feels like going backward. The galaxy becomes safer but more constrained. Trade routes narrow.
- **Naturalize**: Player must facilitate adaptation — helping the galaxy survive metric bleed rather than stopping it. Deliberately degrading some trade routes to force adaptation. Short-term economic pain for long-term resilience. Factions lose some interdependence but gain self-sufficiency.
- **Renegotiate**: Player must operate in Phase 3-4 space extensively, accepting hull stress and instrument unreliability. The path forward requires *tolerating the thing that felt like danger*. The most terrifying option mechanically — but the only one that addresses root cause.

#### Epilogue System

**Industry best practice** (Disco Elysium, Mass Effect): Epilogue text lands harder when it is *specific*.

For each of 5 factions, write 2 epilogue outcomes (aligned with player's final relationship state). That's 10 epilogue cards total.

**Write in second person, present tense**:
- "The Weavers completed the bridge. You are not there to see it. They named one span after you anyway."
- "The Chitin Collective's probability models adapted to the new threading topology within three cycles. They were ready. They had always been ready."
- "Valorin patrols still fly the old routes. The routes are meaningless now — the borders dissolved years ago. But the patrols continue. Habit is the last thing a soldier gives up."

### 2c. Expanded Lore — Knowledge Web Content

69 knowledge entries are defined in `KnowledgeGraphContentV0.cs` but 0 are revealed in first-hour play. This is working as designed (requires visiting specific discovery site pairs), but the *content* of those entries needs authoring.

**Categories to author**:
- Faction lore entries (history, culture, internal politics)
- Technology entries (how metric foam works, how fracture drives work)
- Economic entries (trade good origins, supply chain histories)
- Discovery entries (void site histories, anomaly explanations)
- Ancient entries (Accommodation civilization, the compact, the threading)

**Tone**: Knowledge entries should function like encyclopedia articles written by an unreliable narrator. Each faction's entries about themselves should be subtly self-serving. Cross-referencing entries reveals contradictions.

---

## 3. TEMPLATE_MISSIONS — 50-65 Authored Mission Templates

### Industry Reference

**Skyrim Radiant Quests (anti-pattern)**: Randomizing *location* without randomizing *reason* produces narrative emptiness. "Retrieve the item from the dungeon" with a random dungeon is grammatically correct but meaningless.

**Warframe (positive pattern)**: Avoids genericity through (1) **character-voiced briefings** (Darvo, Clem, Nora Night flavor the same template differently) and (2) **world-state reactive text** (mission briefings reference current war states and recent player actions).

**Star Citizen**: **"Fiction-first sentence" policy** — every mission brief starts with a sentence that could only be true in this universe. Not "Deliver X to Y" but "The relay station at Orison has been running low on water for eleven days. Their recycler failed. We need 40 units of processed water there before the crew starts making decisions they'll regret."

### Template Architecture

Your existing `mission_design_v0.md` defines template anatomy with binding tokens ($PLAYER_START, $ADJACENT_1, $MARKET_GOOD_1). The gap is authored *text* for each template.

**The Fiction-First Rule**: Every template needs 3 components:
1. **Opening sentence** — could only be true in STE, references a named place or person
2. **Mechanical brief** — what to do (deliver, escort, investigate, etc.)
3. **FO reaction line** — FO's take on why this mission matters or doesn't

**NPC Contact Names**: Add a `$NPC_CONTACT_NAME` slot to all templates. The name is seed-generated but persistent — if the player completes a mission for "Quartermaster Vael," that name persists in the station log. Over playthroughs, "Vael" becomes a real person.

### Proposed Template Families (to reach 50-65)

Based on your existing mission infrastructure and the goods/faction system:

| Family | Count | Description | Example |
|--------|-------|-------------|---------|
| Trade Route | 8-10 | Deliver goods between specific stations | "Quartermaster Vael at Ironpeak needs 20 components before the fabrication run deadline" |
| Supply Crisis | 6-8 | Respond to shortage/surplus events | "Station Helix-9 lost their food supply when the hauler convoy was interdicted" |
| Escort | 4-6 | Protect NPC convoys through dangerous space | "The Chitin data-courier is carrying encrypted market forecasts through contested lanes" |
| Bounty | 6-8 | Eliminate specific threats | "Valorin patrol Captain Drenn has been raiding independent traders near the border" |
| Investigation | 4-6 | Visit locations, scan anomalies, report findings | "Communion frequency readings at Void Site 7 don't match any known pattern" |
| Salvage | 4-6 | Recover goods/modules from derelict sites | "A Weaver construction barge lost power near the belt. Cargo is drifting." |
| Diplomacy | 4-5 | Carry messages, negotiate between factions | "Concord wants a back-channel proposal delivered to Chitin. No record." |
| Construction | 3-4 | Deliver materials for station upgrades | "Haven Tier 3 requires exotic crystals — Communion territory is the only source" |
| Contraband | 3-4 | Smuggle goods past tariff/embargo systems | "This cargo manifest says 'components.' It's actually Valorin munitions. You in?" |
| Rescue | 3-4 | Respond to distress signals, evacuate stations | "Mining crew stranded after a fracture event collapsed their nav beacon" |
| Survey | 3-4 | Map new areas, scan planets, catalog resources | "We need deep-field scans of the outer systems before the next pentagon audit" |

**Total: ~50-65 templates**

### World-State Variants

Each template should have 2-3 text variants based on world state:
- **Warfront active**: "The usual route is hot. Valorin patrols are interdicting everything west of the line."
- **Embargo active**: "Concord locked the trade lanes, but there's a gap at Station 4 if you time it right."
- **Pentagon link broken**: "Nobody's trading along the Chitin-Valorin corridor anymore. Which means someone will pay triple for rare metals."

This yields 100-195 authored text blocks from 50-65 templates.

---

## 4. FACTION_STORYLINES — 5 Chains × 8 Missions

### Industry Reference

**Fallout: New Vegas**: Each faction is correct about one thing and wrong about another. The player learns the flaw through *missions*, not dialogue. NCR corruption isn't told — it's discovered when your supply delivery never arrives.

**SWTOR Class Stories**: Each chain has a **genre identity** that gives authors a tonal north star. Smuggler = heist story. Consular = mystery. Warrior = power fantasy.

**Warframe Syndicates (anti-pattern)**: Reputation grind without story events. The relationship feels like a bank account, not a connection.

### Genre Identity Per Faction Chain

| Faction | Genre | Thesis | Complication (Midpoint Reframe) |
|---------|-------|--------|-------------------------------|
| Concord | **Political thriller** | "Order requires sacrifice" | Player discovers they've been the *instrument* of the sacrifice, not the beneficiary |
| Chitin | **Philosophical detective** | "Collective intelligence finds truth" | The truth the collective finds is one no individual would have chosen |
| Weavers | **Craftsman's tragedy** | "Built to last" | The most durable things are built on foundations the builders didn't understand |
| Valorin | **Frontier western** | "Freedom requires risk" | The frontier isn't empty — the cost of freedom is externalized onto the things you don't see |
| Communion | **Mystical revelation** | "The long listening" | The thing they've been listening to has been trying to *answer* |

### The 8-Mission Arc Structure

Based on the three-act structure used successfully in SWTOR, New Vegas, and Mass Effect companion arcs:

| Mission | Phase | Function | Emotional Beat |
|---------|-------|----------|----------------|
| M1 | Introduction | First contact with faction philosophy | Curiosity — "these people are interesting" |
| M2 | Proving | Demonstrate competence in faction's domain | Competence — "I can do what they need" |
| M3 | Trust building | Access to inner circle, faction-specific privilege | Investment — "I'm part of this now" |
| M4 | **Midpoint revelation** | Discover the faction's uncomfortable truth | Doubt — "wait, they're not what I thought" |
| M5 | Complicity test | Asked to do something morally ambiguous for the faction | Tension — "am I ok with this?" |
| M6 | Consequences | See the results of M5's action in the world | Reckoning — "this is real" |
| M7 | Loyalty test | Faction asks for something that conflicts with player's other allegiances | Choice — "what do I actually believe?" |
| M8 | Resolution | Final mission, reflects player's accumulated choices | Identity — "this is who I became" |

### Per-Faction Mission Sketches

#### Concord Chain (Political Thriller)

| # | Title Concept | Beat |
|---|--------------|------|
| C1 | "Standard Operating Procedure" | Deliver routine supplies. Everything works perfectly. Concord is reliable. |
| C2 | "The Audit Trail" | Investigate a trade irregularity. Discover a minor discrepancy — someone is skimming. |
| C3 | "Signal Integrity" | Install monitoring equipment at a border station. You're helping Concord watch the lanes. |
| C4 | "The Sealed Report" | Access a restricted archive. Discover Concord has data on fracture events they haven't shared. The "irregularity" from C2 was someone trying to leak this data. |
| C5 | "Containment Protocol" | Seal natural fracture vents per Concord orders. This is good, right? Preventing dangerous instability? |
| C6 | "The Cost of Stability" | Visit a system where sealed vents caused pressure buildup elsewhere. The Weavers' construction project there failed because of what you did. |
| C7 | "Whistleblower" | **Choice**: Report the suppressed data to other factions (break Concord trust, gain Chitin/Communion standing) OR help Concord maintain the narrative (keep trust, but the cover-up continues). The FO has strong opinions based on archetype. |
| C8 | "The Foundation" | Final mission depends on C7 choice. If whistleblower: navigate the diplomatic fallout of disclosure. If loyal: help Concord implement "controlled release" of fracture data on their terms. |

#### Chitin Chain (Philosophical Detective)

| # | Title Concept | Beat |
|---|--------------|------|
| H1 | "Market Anomaly" | Chitin hires you to investigate a price discrepancy. They already know the answer; they want to see how you think. |
| H2 | "The Probability Broker" | Run a trade route that's optimized by Chitin models. It works perfectly. Too perfectly. |
| H3 | "Metamorphic Access" | Witness a Chitin molting ceremony. Gain access to their inner data networks. Understand: they see the world as probability fields, not objects. |
| H4 | "The Pattern Break" | Chitin models detect an anomaly they can't explain. It correlates with fracture events. They need you to gather data from a void site. |
| H5 | "Emergent Behavior" | **Midpoint**: The probability models aren't just predicting fracture events. They're detecting *communication patterns* in the lattice decay. The collective is accidentally eavesdropping on something. |
| H6 | "Signal or Noise" | The collective argues over whether the patterns are real. You must gather confirming evidence — but the evidence is in Communion space, and Chitin/Communion don't trust each other. |
| H7 | "The Bet" | Chitin asks you to act on a prediction that will harm Valorin trade routes. The prediction may be right, but acting on it is an act of economic warfare. |
| H8 | "Full Spectrum" | The collective's models achieve convergence. What they've found either validates the communication hypothesis or was an artifact of their own observation. The answer depends on data from your C4/U4/V6 missions (cross-chain dependency). |

#### Weavers Chain (Craftsman's Tragedy)

| # | Title Concept | Beat |
|---|--------------|------|
| W1 | "Raw Materials" | Deliver construction materials. See the Weavers' patient, precise building process. |
| W2 | "Load-Bearing" | Help reinforce a station that's been stressed by metric instability. Good engineering work. |
| W3 | "The Master Builder" | Meet the Weaver chief architect. Tour their greatest construction. Understand: they build for permanence. Their philosophy is "if we build it right, it will outlast any crisis." |
| W4 | "Foundation Scan" | Scan the substrate of a Weaver megastructure. Discover it was built on an ancient Accommodation support pillar. The Weavers didn't know. |
| W5 | "Echo Architecture" | **Midpoint**: The Weavers' best structures work because they unknowingly replicate Accommodation construction techniques. Their "instinct for building" is actually resonance with ancient patterns in the metric itself. Their pride in original craftsmanship is based on inherited knowledge they don't know they have. |
| W6 | "Structural Failure" | A Weaver construction collapses because the underlying Accommodation support is degrading (lattice decay). The Weavers blame themselves. You know the real cause. |
| W7 | "The Retrofit" | The Weavers ask you to help them rebuild using traditional methods. You have the option to share the Accommodation data (which would force them to rebuild differently) or let them proceed their way. |
| W8 | "Keystone" | Final build project. If they have the Accommodation data, they build with new understanding — stronger but philosophically shaken. If not, they build beautifully but on a foundation that may not hold. |

#### Valorin Chain (Frontier Western)

| # | Title Concept | Beat |
|---|--------------|------|
| V1 | "Patrol Duty" | Ride along with a Valorin patrol. See their discipline, their camaraderie, their blunt assessment of threats. |
| V2 | "The Disputed Lane" | Help secure a trade lane that Concord claims is theirs but Valorin patrols. The Valorin don't care about legal claims — they care about who's actually there. |
| V3 | "Weapons Test" | Help test new Valorin munitions at a range asteroid. Everything about their culture is martial but not cruel. They respect competence. |
| V4 | "Border Incident" | A Valorin mining charge detonates near a Weaver construction site. Accident? The Valorin claim they were sealing a fracture vent. The damage is real regardless. |
| V5 | "Extraction Zone" | Help extract rare metals from a deep site. The Valorin method is fast and efficient. It also destabilizes the surrounding area. |
| V6 | "Collateral" | **Midpoint**: Visit a system the Valorin "secured" last year. The independent traders who used to operate there are gone. Not killed — displaced. The Valorin didn't do anything wrong *legally*. They just made the space unsafe for anyone without military-grade hardware. The frontier isn't empty. It was emptied. |
| V7 | "The Old Way" | A Valorin elder proposes a diplomatic solution to a border dispute — a approach the younger officers consider weakness. The elder knows something about the ancient compact. |
| V8 | "Last Patrol" | Final mission. The old patrol routes are becoming untenable as fracture intensity increases. The Valorin must decide whether to retreat to defensible space or push further into the unknown. Your relationship with them determines which choice they make. |

#### Communion Chain (Mystical Revelation)

| # | Title Concept | Beat |
|---|--------------|------|
| U1 | "The Frequency" | Communion invites you to listen to a specific signal at a specific location. You hear nothing. They hear everything. They're patient. |
| U2 | "Exotic Offering" | Deliver exotic crystals to a Communion shrine-station. Watch how they interact with the crystals — not as tools but as instruments of perception. |
| U3 | "The Silent Record" | Access Communion archives. Unlike other factions' data, theirs is experiential — you must sit in a resonance chamber and *listen*. The record is the absence of certain frequencies. |
| U4 | "Previous Visitors" | **Midpoint**: The Communion reveal they know about the Accommodation civilization — not from data, but from listening. They've detected echoes of previous threshold-crossers in the metric. The silence they worship is not the absence of signal. It is the space between signals — and something has been signaling for a very long time. |
| U5 | "The Threshold" | The Communion ask you to reach a specific fracture phase depth and report what you perceive. They cannot go themselves — their ships are too fragile. You are their instrument. |
| U6 | "Resonance Map" | The data you bring back from U5 reshapes the Communion's understanding. The metric structure has intention. It was designed. This confirms the Accommodation hypothesis but from a completely different angle than the Concord data. |
| U7 | "The Question" | Communion asks you to do something unprecedented: attempt to *respond* through the metric. Not just listen — transmit. The other factions would consider this insane. The Communion considers it overdue. |
| U8 | "The Answer" | The metric responds. Or doesn't. What happens depends on the player's endgame path alignment and accumulated knowledge. This is the most narratively variable mission in the game — it can resolve as wonder, terror, or profound silence. |

### Cross-Chain Dependencies

The most satisfying faction chains reference each other. Player who completes Concord C4 (sealed report) has data that changes the meaning of Chitin H5 (pattern break). Player who knows what Communion heard (U4) understands why the Weavers' foundations work (W5).

| Mission | Enhanced By |
|---------|------------|
| H5 (Pattern Break) | C4 data (Concord's suppressed fracture readings) |
| W5 (Echo Architecture) | U4 revelation (Accommodation echoes in the metric) |
| V7 (The Old Way) | U4 knowledge (the elder's "ancient compact" reference) |
| U7 (The Question) | All 12 adaptation fragments + W5 + C4 |
| All M8 missions | Player's endgame path choice colors the resolution |

---

## 5. MISSION_POLISH — FO Commentary & Consequence Propagation

### FO Commentary Architecture

**Industry reference**: Hades (~10,000 lines, condition-gated), Mass Effect (companion opinions on every major decision), BG3 (approval + camp conversations)

#### Tier 1 — Immediate Reactions (1-2 lines, inline)

Fire immediately on event. ~120-180 lines total (15-20 triggers × 2-3 lines × 3 FO types).

**Triggers**:
- Price anomaly above 35% from historical median
- First visit to a new market
- Combat engagement with new enemy type
- Warfront intensity change (Stable→Skirmish, Skirmish→Active)
- Pentagon ring dependency broken
- First fracture jump each session
- Trace accumulation reaching 50% threshold
- Discovering a void site
- Completing a trade for >200% profit
- Taking hull damage below 50%

**Sample lines** (price anomaly trigger):
- **Analyst**: "Rare metals at 147% of historical median. That's not seasonal — something structural changed upstream."
- **Veteran**: "Price is way up. In my experience, that means either a supply chain broke or someone's hoarding for a fight."
- **Pathfinder**: "Look at that spread. Someone is about to make a fortune, and I'd rather it be us."

#### Tier 2 — Dock Commentary (1-2 lines, fires on docking)

Reference the journey just completed. ~60-90 lines total.

**Condition types**:
- Multi-hop journey (3+ systems visited)
- Took hull damage en route
- Profitable trade completed
- Unprofitable trade completed
- Visited a faction system for the first time
- Passed through warfront zone
- Encountered NPC patrol

#### Tier 3 — Haven Reflective Logs (full paragraph, discovered in Haven)

See Section 2a, Layer B above. ~18-24 entries total.

### Consequence Propagation

Missions should leave *traces* in the world. Key patterns:

- **Economic consequence**: Mission C5 (seal fracture vents) causes price disruption at affected stations 10-20 ticks later
- **Reputation consequence**: Mission V6 revelations unlock unique dialogue options with independent traders
- **Knowledge consequence**: Mission H5 data unlocks additional knowledge web entries
- **Physical consequence**: Mission W6 construction failure creates a derelict site scannable by the player later
- **Narrative consequence**: Mission outcomes referenced by FO in later contexts ("Remember what happened at Station 7?")

### The Quiet Beat

**Critical design constraint**: Budget ~1 commentary event per 3-5 minutes of active play. Silence is characterization. After devastating combat, a beat of silence before commentary lands harder than immediate reaction.

Define "silence triggers" — events where the FO would naturally not speak:
- Immediately after player death and respawn
- During the first 30 seconds of a new session
- After the player reads an ancient log (processing time)
- During warp transit (peaceful moment)

---

## 6. FACTION_IDENTITY_REDESIGN — 40 T2 Modules, Signature Mechanics, Ship Variants

### Industry Reference

**Endless Space 2**: Gold standard. Each faction has a different *verb*, not a different *number*. Vodyani harvest (don't colonize). Horatio abduct. Cravers devour.

**EVE Online**: Faction modules offer different *trade-off profiles*, not just better stats. A Caldari module has CPU reduction enabling different fits. A Guristas module has drone bandwidth bonus enabling a specific playstyle.

**Key principle**: Give each faction **one thing it cannot do** and **one thing only it can do**. The restriction is as important as the ability.

### Signature Mechanics — One Unique Verb Per Faction

| Faction | Signature Verb | Mechanical Expression | Restriction When Hostile |
|---------|---------------|----------------------|--------------------------|
| **Concord** | **Arbitrate** | Only Concord-aligned players can propose multi-faction trade treaties. Creates diplomatic leverage. | Hostile Concord flags your transponder — Concord stations impose double tariffs |
| **Chitin** | **Broker** | Sell intel about one faction's trade routes to a third party. Only possible with Chitin standing. | Hostile Chitin blocks intel access — your market price predictions become less accurate (wider confidence intervals) |
| **Weavers** | **Commission** | Place a custom build order for a bespoke module specification (unique stat combos). Only Weaver-aligned. | Hostile Weavers cap hull repairs at 50% — their repair docks refuse full service |
| **Valorin** | **Conscript** | Hire Valorin mercenary escorts that don't count against fleet cap. Short-term military surge. | Hostile Valorin apply a "wanted" tag — their patrols actively interdict you |
| **Communion** | **Commune** | Access a one-way intel channel revealing hidden system states (black markets, cache locations, pre-pirate spawn warnings). | Hostile Communion blocks all exotic crystal supply chains to you |

### T2 Module Design — 40 Faction-Exclusive Modules (8 per faction)

**Design principle**: Each module should **enable a playstyle impossible without it**, not just improve a number.

#### Module Design Framework

Each faction gets 8 T2 modules across these categories:

| Category | Modules per Faction | Design Axis |
|----------|-------------------|-------------|
| Weapon | 2 | Faction combat philosophy |
| Defense | 1 | Faction survival philosophy |
| Drive/Engine | 1 | Faction mobility philosophy |
| Utility | 2 | Faction economic/intel philosophy |
| Special | 2 | Faction signature mechanic enablers |

#### Concord T2 Modules (8)

| Module | Category | Effect | Fantasy |
|--------|----------|--------|---------|
| Regulatory Transponder Mk II | Special | Zero tariffs at Concord stations + 10% rep gain rate. But Concord tracks all your movements. | "I am the law" |
| Standard-Issue Railgun | Weapon | Precisely rated damage, zero variance. Not the strongest, never disappoints. | Reliability over firepower |
| Diplomatic Shield Array | Defense | Absorbs 30% more damage but shares 10% of damage taken with nearby allied NPCs. Concord doctrine: collective defense. | "We protect each other" |
| Logistics Optimizer | Utility | +20% cargo capacity when carrying goods needed by the destination station. Requires intel. | Supply chain mastery |
| Fleet Command Relay | Utility | NPC escorts gain +15% accuracy when within sensor range. Enables fleet coordination gameplay. | Commander, not lone wolf |
| Institutional Drive | Drive | Safe, efficient, boring. 15% less fuel consumption, no speed bonus. | "Proven technology" |
| Audit Scanner | Special | Reveals hidden tariff rates and trade policy at scanned stations. Enables the Arbitrate mechanic. | Information is power |
| Point Defense Matrix | Weapon | Intercepts incoming missiles/drones. Defensive weapon. No offensive use. | "Defense is doctrine" |

#### Chitin T2 Modules (8)

| Module | Category | Effect | Fantasy |
|--------|----------|--------|---------|
| Probability Engine | Special | Market predictions shown with confidence intervals. Higher standing = tighter intervals. | "The hive calculates" |
| Data Siphon Array | Utility | Passively collects trade data from stations you visit. Sells to Chitin network for standing. | Information arbitrage |
| Adaptive Carapace | Defense | Hull plating that hardens after each hit. First hit: full damage. Fifth hit: 40% reduction. | Metamorphosis under pressure |
| Metamorphic Cannon | Weapon | Damage type shifts based on target's weakest defense. Adapts mid-combat. | "We become what kills you" |
| Chitin ECM Suite | Utility | Reduces enemy sensor range by 30%. Makes you harder to interdict. Enables stealth trade routes. | Information warfare |
| Phase-Shift Drive | Drive | Can make micro-jumps to adjacent nodes without using lane gates. Expensive, short range. | "We find our own paths" |
| Hivemind Targeting | Weapon | Accuracy improves by 5% per allied Chitin NPC in system. Swarm coordination. | Collective intelligence in combat |
| Signal Broker Unit | Special | Enables the Broker mechanic — sell gathered trade intel to third-party factions. | The information economy |

#### Weavers T2 Modules (8)

| Module | Category | Effect | Fantasy |
|--------|----------|--------|---------|
| Resonance Forge | Special | Enables the Commission mechanic — craft custom modules at Weaver stations. | "We build to order" |
| Composite Layering | Defense | Hull regenerates 2% per tick when not in combat. Slow but persistent. | "Built to heal" |
| Structural Reinforcement | Utility | +2 module slots. But ship mass increases 20%, reducing speed. | More room, more mass |
| Precision Welding Beam | Weapon | Low damage, but repairs allied ships it hits (dual-purpose weapon). | Builder, not destroyer |
| Tractor Fabricator | Utility | Collects salvage from derelicts automatically when in range. Converts to raw materials. | "Nothing is waste" |
| Echo Drive | Drive | 30% faster warp transit, but causes 5% hull stress per jump (ancient resonance effect from W5). | Speed at a structural cost |
| Load-Bearing Frame | Special | Haven construction costs reduced 25% when this module is equipped. Enables faster Haven building. | "The builder's advantage" |
| Construction Drone Bay | Weapon | Deploys repair drones that fix hull and can repair allied stations. Offensive only vs structures. | "We build, even in battle" |

#### Valorin T2 Modules (8)

| Module | Category | Effect | Fantasy |
|--------|----------|--------|---------|
| Conscription Beacon | Special | Enables Conscript mechanic — summon Valorin mercs for temporary escort duty. | "Call the reinforcements" |
| Kinetic Accelerator | Weapon | Extreme range, extreme damage, extreme power draw. One-shot doctrine. | First strike from distance |
| Swarm Coordinator | Weapon | Launches drone swarm. Individual drones are weak, but quantity overwhelms point defense. | Swarm doctrine expressed |
| Reactive Armor | Defense | When hit, retaliates with shrapnel damage to attacker. Aggressive defense. | "Even our hull fights back" |
| Raid Drive | Drive | Fastest engine in the game. -30% hull integrity (stripped for speed). | Speed is survival |
| Kill-Mark Targeter | Utility | Damage bonus scales with total NPC kills this session. +1% per 5 kills, caps at 20%. | Veteran's edge |
| Salvage Rights Scanner | Utility | Defeated enemies drop better loot. +30% salvage quality from combat. | "The spoils of war" |
| Garrison Beacon | Special | Claims a system for Valorin patrols. NPC Valorin ships begin patrolling that system. | Frontier expansion |

#### Communion T2 Modules (8)

| Module | Category | Effect | Fantasy |
|--------|----------|--------|---------|
| Resonance Receiver | Special | Enables Commune mechanic — reveals hidden system states within 2 hops. | "The signal is everywhere" |
| Crystal Harmonic Lens | Weapon | Damage bypasses shields entirely. Low raw damage but ignores all defense. | "We reach through barriers" |
| Silence Field Generator | Defense | When activated, become invisible to NPC sensors for 10 seconds. Long cooldown. | "Silence is sanctuary" |
| Void Sense Array | Utility | Reveals void sites and discovery locations 3 hops away (normal range: 1). | "We listen further" |
| Meditation Drive | Drive | Fracture drive cooldown -30%. But 5% passive hull stress per jump (crystal resonance). | Speed through the silence |
| Phase Attunement Core | Utility | Reduces Phase 3-4 hull stress by 50%. Enables deep-phase exploration. | "We are comfortable in the deep" |
| Communion Frequency Emitter | Special | Enables the U7 mission "transmission" mechanic. Required for Renegotiate endgame path. | "We speak back" |
| Crystal Hull Coating | Weapon | Self-healing hull: regenerates 3% per tick, but cannot be repaired at stations. Must heal naturally. | Organic, alien maintenance |

### Ship Variant Design

**Visual Language** (silhouette-first, then color):

| Faction | Silhouette Rule | Color Temperature | Material Language |
|---------|----------------|-------------------|-------------------|
| Concord | Clean bilateral symmetry, visible docking collars, rounded hull sections | Neutral white/grey | Polished metal, institutional |
| Chitin | Segmented carapace, insectoid appendages, asymmetric sensor clusters | Dark amber/brown | Organic chitin plates, amber sensor glow |
| Weavers | Layered composite plates, visible construction scaffolding, thick hull | Warm earth tones | Composite panels, exposed framework |
| Valorin | Angular, forward-swept weapon hardpoints, minimal hull, engine-heavy | Military grey/red accents | Ablative armor plates, kill-mark stencils |
| Communion | Crystalline protrusions, no visible crew windows, iridescent hull | Deep purple/iridescent | Crystal growths, phase-shift shimmer |

**Capability Differentiation** (doctrine is fractal — expressed at every ship scale):

| Faction | Strength | Weakness | Doctrine at Every Scale |
|---------|----------|----------|------------------------|
| Concord | Cargo, passive shields | Weapons | Every ship is a platform for logistics, not combat |
| Chitin | Sensors, ECM, encrypted cargo | Raw durability | Every ship is an information node first |
| Weavers | Hull repair, module slots | Speed | Every ship is a workshop that happens to fly |
| Valorin | Speed, weapons, agility | Cargo capacity | Every ship is a weapon that happens to carry things |
| Communion | Fracture travel, stealth | Hull fragility | Every ship is a listening instrument that happens to move |

---

## 7. MEGAPROJECT_SET — Canonical Megaproject Types

### Industry Reference

**Stellaris**: Megastructures are **narrative statements about civilization identity**, not just stat boosts. Dyson Sphere = "I mastered energy." Ring World = "I built a home." Science Nexus = "I chose knowledge."

**Factorio**: The rocket launch demands a **sample of everything** in the game. Cannot be solved by specializing — forces engagement with all systems.

**Satisfactory**: Space Elevator is **always visible** — a physical landmark showing progress. Persistent world presence matters.

**Dyson Sphere Program**: Continuous visible progress (sphere forming in real-time) beats discrete phase gates for engagement.

**Key principle**: A megaproject feels like a capstone when it demonstrates mastery of the *full game*, not just one system.

### Proposed Megaproject Types (4-6)

Each maps to an endgame path and/or a play-identity fantasy:

#### 1. Haven Citadel (Colonization/Economic Fantasy)

*"I built a home in hostile space."*

- **Alignment**: Core progression, required for all paths
- **Stages**: Outpost → Station → Starbase → Citadel (4 stages, already designed)
- **Requirements**: Multi-system supply chains (composites, metals, electronics, exotic crystals from different faction territories)
- **Capstone requirement**: All 5 faction supply chains must contribute — this is the pentagon ring expressed as architecture
- **Visual payoff**: Haven physically transforms at each stage. Citadel stage is the most visually spectacular object in the galaxy

#### 2. Pentagon Resonance Array (Diplomatic Fantasy)

*"I united the five factions."*

- **Alignment**: Naturalize path
- **Stages**: Survey (scan all 5 faction capitals) → Negotiate (get each faction to contribute a resonance crystal) → Construct (build the array at Haven) → Activate (initiate the pentagon harmonic)
- **Requirements**: Standing of at least Neutral with all 5 factions simultaneously (extremely challenging — helping one often hurts another)
- **Capstone requirement**: All 5 faction storyline chains must be at M4+ (midpoint). The player must *understand* each faction before they can unite them.
- **Narrative weight**: This is the "what if they all cooperated" answer to the game's central question

#### 3. Precursor Relay Network (Scientific/Mystery Fantasy)

*"I unlocked the galaxy's deepest secrets."*

- **Alignment**: Renegotiate path
- **Stages**: Discover (find 3 relay fragments at void sites) → Analyze (use Research Lab at Haven Tier 2) → Reconstruct (build relay nodes at 3 void sites) → Activate (initiate two-way communication through the metric)
- **Requirements**: All 12 adaptation fragments, Phase 3-4 rated hull, Communion standing for frequency data
- **Capstone requirement**: Complete Communion U7 ("The Question") — the player must have *attempted contact* before they can build the infrastructure for it
- **Narrative weight**: This is the most dangerous option — the metric responds, and the response changes everything

#### 4. Warfront Fortress (Military Fantasy)

*"My military power ended the wars."*

- **Alignment**: Reinforce path
- **Stages**: Establish (build military infrastructure at Haven) → Fortify (install weapons platforms at 3 warfront zones) → Pacify (resolve 2 active warfronts through military dominance) → Command (become the galaxy's military arbiter)
- **Requirements**: Valorin standing for munitions access, high combat stats, warfront participation across multiple fronts
- **Capstone requirement**: Player must have participated in warfronts on both sides (not just one faction's perspective) — this forces engagement with the moral complexity of military intervention
- **Narrative weight**: Peace through superior firepower — effective, costly, and morally ambiguous

#### 5. Knowledge Singularity (Completionist/Scholar Fantasy)

*"I understood everything."*

- **Alignment**: Any path (bonus/optional megaproject)
- **Stages**: Catalog (fill 60% of knowledge web) → Synthesize (connect 5 cross-faction knowledge threads) → Theorize (unlock the "unified field" knowledge node) → Archive (store the complete knowledge graph at Haven)
- **Requirements**: Extensive exploration, all faction chains to M4+, most discovery sites scanned
- **Capstone requirement**: The final knowledge node requires data from all 5 faction chains + all 12 adaptation fragments. The "unified field" is the player's personal understanding of how the game's systems connect.
- **Narrative weight**: This is the meta-megaproject. The player who completes it *understands the whole game* as a coherent system. The reward is the most complete epilogue, where every faction's outcome is explained.

### Megaproject Implementation Details

**Staged Construction** (Stellaris pattern):
- Each stage has a resource cost, a time cost, and a prerequisite
- Partial benefits at each stage (Haven Tier 2 unlocks Research, Tier 3 unlocks Drydock)
- Visual state change at each stage (model swap or additive geometry)
- Completion ceremony: FO delivers a unique speech, the game acknowledges the achievement

**The Capstone Rule**: Every megaproject must demand engagement with at least 3 of the game's 5 core systems (trade, combat, exploration, diplomacy, research). No single-system completion path.

**Megaproject Log**: A running record visible in Haven UI — every resource contributed, every stage cleared, with timestamp and description. This serves as the personal narrative of the playthrough.

---

## 8. CONTENT_WAVES — Final Archetype Families

### Current Module Inventory

54 modules across 9 categories. Before adding more, audit the existing modules against archetype families.

### The Coherence Test (Slay the Spire pattern)

A good module family passes the coherence test when a player can describe their build in one sentence: "I'm doing a tanky trader build," "I'm a glass cannon scout," "I'm a fleet commander build."

**Proposed Build Identities** (each should be viable with existing + T2 modules):

| Build Identity | Key Modules | Playstyle |
|---------------|-------------|-----------|
| **Long-Haul Trader** | Large cargo, efficient drive, logistics optimizer | Maximum profit per trip, slow but rich |
| **Speed Runner** | Fast drive, minimal hull, navigation upgrades | Cover maximum galaxy distance, discover everything |
| **Combat Specialist** | Weapons, armor, targeting | Clear warfronts, bounty hunting, escort |
| **Fleet Commander** | Command relay, escort modules, fleet buffs | Lead NPC escorts, coordinate group combat |
| **Deep Explorer** | Fracture drive, scanner, phase attunement | Push into Phase 3-4 space, discover Accommodation sites |
| **Information Broker** | Sensors, ECM, data siphon, probability engine | Trade on intel, Broker mechanic, avoid combat |
| **Station Builder** | Construction modules, tractor, load-bearing frame | Haven-focused, Construction missions, repair gameplay |
| **Diplomatic Agent** | Audit scanner, transponder, diplomatic shields | Arbitrate, run diplomacy missions, multi-faction standing |

### Content Wave Strategy for EA

**Wave 0 (EA Launch)**: Ship with 54 existing modules + faction signature mechanics (5 unique verbs). This is sufficient for launch — the verb is more important than the module count.

**Wave 1 (Month 1-2 post-EA)**: Add 16 T2 faction modules (2 per faction, prioritizing the signature mechanic enablers). Each wave should complete one faction's T2 offering.

**Wave 2 (Month 3-4)**: Add remaining 24 T2 faction modules (completing all 40). Each faction now has a full equipment identity.

**Wave 3 (Month 5-6)**: Add Mk II/III tier progression for the 10 most-used base modules. Expands content surface without cognitive load (players understand "better version of thing I know").

**Wave 4+ (Post-EA)**: Precursor modules (T3), ancient ship hulls, and cross-faction hybrid modules (require standing with 2 adjacent pentagon factions).

### Cross-Faction Module Dependencies (Pentagon Ring Expression)

Some T3 modules should require components from two adjacent factions:

| Module | Required Standing | Components |
|--------|------------------|------------|
| Diplomatic Hull Coating | Concord + Weavers | Weaver composites + Concord certification |
| Adaptive Sensor Grid | Chitin + Communion | Chitin probability engine + Communion frequency data |
| Reinforced Swarm Bay | Valorin + Chitin | Valorin drone chassis + Chitin targeting algorithms |
| Crystal-Composite Armor | Communion + Weavers | Communion crystal + Weaver composite layering |
| Strike Logistics Module | Concord + Valorin | Concord supply chain + Valorin weapons platform |

This makes the pentagon ring feel like a web of interdependence at the equipment level.

---

## 9. MUSIC — Soundtrack Composition

### Industry Reference

**FTL** (Ben Prunty): The most cost-effective approach. One composer, synthesizer-forward, ~40 tracks with layered stems. Total budget reportedly under $20K. The layered stem approach enables dozens of perceived variations from one recording session.

**Stellaris** (Andreas Waldetoft): Live orchestra with faction-specific instrumentation. Necroids use church organ. Aquatics use woodwinds. The timbral palette reflects species lore.

**No Man's Sky** (65daysofstatic): Procedural PULSE system — stems assembled based on biome state. Player never hears the same sequence twice, but all sequences cohere because stems were composed to fit together.

**Mass Effect**: 1970s/80s synth (Vangelis, Tangerine Dream) mixed with modern orchestral. "Retro-futurism" that defines the franchise's audio identity.

### Recommended Approach: FTL-Style Stem Architecture

**Target**: 3-4 stems per gameplay context, all composable. Stems stored as `.ogg`, mixed in Godot's audio bus system.

**Budget tier**: DIY synthesizer ($0-2K) or single composer contract ($5K-20K for hybrid orchestral).

### Adaptive Music System (5-State)

Implementable in Godot 4 without Wwise:

| State | Active Stems | Trigger |
|-------|-------------|---------|
| PEACEFUL_TRAVEL | ambient_bed + melodic | Default state during flight |
| ALERT | ambient_bed + melodic + tension_pulse | Entering contested/warfront system |
| COMBAT | ambient_bed + combat_percussion + intensity | Combat engagement |
| FACTION_TERRITORY | ambient_bed + faction_theme | Entering faction-controlled space |
| DOCKED | dock_ambient + trade_ambience | Station docking |

**Transitions**:
- Combat entry: 0.3s fade (shock — make combat feel sudden)
- Combat exit: 3s fade (decompression — let tension bleed away slowly)
- Faction territory entry: 6s crossfade (gradual sense of entering someone else's space)
- Docking: 2s fade to dock ambience (arrival, safety)

**Implementation**: `MusicSystem.gd` autoload, `AudioStreamPlayer` per stem layer, `Tween` for crossfades. The Godot AudioBus architecture handles volume, effects (reverb, compression), and routing natively.

### Faction Musical Identities — Timbral Palettes

| Faction | Timbral Palette | Melodic Character | Reference Feel |
|---------|----------------|-------------------|----------------|
| **Concord** | Clean brass, sustained strings, neutral reverb | Resolved, major-key, slow harmonic rhythm | Civilization opening — ceremonial, welcoming |
| **Chitin** | Processed insect recordings (clicks, chirrups), marimba, gamelan | Irregular meter (5/8 or 7/8), descending chromatic lines | Hollow Knight — precise, alien, curious |
| **Weavers** | Low cello ensemble, hammer dulcimer, rhythmic anvil percussion | Pentatonic, steady pulse, construction-like ostinato | Dwarf Fortress "Forge" feel — laborious, purposeful |
| **Valorin** | Military snare, low brass, short staccato strings | March tempo (120-130 BPM), minor key, punchy | Mass Effect "Suicide Mission" — relentless forward drive |
| **Communion** | Tibetan singing bowls, prepared piano, long reverb tails, choir vowels | Modal, very slow harmonic movement, silence as melodic element | Ico / NMS ambient — ancient, vast, uncommunicative |

### Pentagon Adjacency Blending

Because factions form a pentagon ring, adjacent factions should share a timbral element:
- Concord ↔ Weavers: Both use brass elements
- Weavers ↔ Chitin: Both use percussive elements
- Chitin ↔ Valorin: Both use short/staccato elements
- Valorin ↔ Communion: Both use low register
- Communion ↔ Concord: Both use sustained/pad elements

In mixed-allegiance systems, the music blends adjacent palettes. This makes the pentagon ring *musically tangible*.

### Silence as Design Element

Communion of Silence zones should have noticeably longer silences between musical phrases. This is the most direct way to make their lore ("Silence") mechanical rather than just narrative. Implement as a longer `silence_gap_ms` parameter in the MusicSystem when in Communion territory.

### Track List (Minimum Viable Soundtrack)

| Track | Context | Stems |
|-------|---------|-------|
| "Open Space" | Default flight | ambient_bed, melodic_a, melodic_b |
| "The Lanes" | Lane transit | ambient_bed, transit_pulse, harmonic_drone |
| "Tension Rising" | Contested space | ambient_bed, tension_pulse, threat_pad |
| "Engagement" | Combat | combat_perc, intensity_synth, combat_melodic |
| "Station Air" | Docked (generic) | dock_ambience, trade_hum, station_activity |
| "Concord Standard" | Concord territory | brass_pad, strings_sustain, institutional_hum |
| "Chitin Frequency" | Chitin territory | click_pattern, gamelan_bells, data_pulse |
| "Weaver's Forge" | Weavers territory | dulcimer_rhythm, cello_drone, anvil_perc |
| "Valorin March" | Valorin territory | snare_march, brass_staccato, threat_strings |
| "The Listening" | Communion territory | singing_bowl, prepared_piano, vast_silence |
| "Haven" | Haven station | unique_ambient, crystal_resonance, home_melody |
| "The Threading" | Fracture travel | deep_drone, phase_shift, dissonance_layer |
| "Pentagon Break" | Crisis event | all_faction_motifs_fragmented, chaos_layer |
| "Resolution" | Endgame/credits | depends_on_path (3 variants) |

**14 tracks × 3-4 stems each = ~45-55 stem files.** This is achievable with a single composer contract.

---

## 10. Authoring Volume Estimates & Priority Order

### Total Content Authoring Scope

| Content Type | Items | Estimated Words |
|-------------|-------|-----------------|
| Haven ancient logs (14 entries) | 14 | ~2,800 (200 words avg) |
| FO Haven reflective entries (24) | 24 | ~3,600 (150 words avg) |
| Haven Chronicle templates (auto) | 10 templates | ~500 |
| Knowledge web entries (69) | 69 | ~10,350 (150 words avg) |
| Endgame epilogue cards (10) | 10 | ~1,500 (150 words avg) |
| Mission template briefs (65 × 3 variants) | 195 | ~19,500 (100 words avg) |
| Mission NPC contact flavor (40) | 40 | ~2,000 (50 words avg) |
| Faction chain missions (40 missions) | 40 | ~12,000 (300 words avg) |
| Faction chain midpoint reframe scenes (5) | 5 | ~2,500 (500 words avg) |
| FO Tier 1 commentary (180 lines) | 180 | ~5,400 (30 words avg) |
| FO Tier 2 dock commentary (90 lines) | 90 | ~2,700 (30 words avg) |
| FO mission commentary hooks (120 lines) | 120 | ~3,600 (30 words avg) |
| Music briefs (14 tracks) | 14 | ~2,100 (150 words avg) |
| Module descriptions (40 T2) | 40 | ~2,000 (50 words avg) |
| **TOTAL** | **~851 items** | **~68,550 words** |

### Priority Order for Authoring

Based on player impact × implementation readiness:

| Priority | Epic | Rationale |
|----------|------|-----------|
| **P1** | FACTION_STORYLINES (40 missions) | Highest narrative impact. Uses existing mission infrastructure. Drives faction identity more than any other content. |
| **P2** | TEMPLATE_MISSIONS (65 templates) | Core gameplay loop content. Each template makes the game world feel more alive. Already have the template system. |
| **P3** | MISSION_POLISH (FO commentary) | 390 lines of FO commentary transforms the player experience from "quiet" to "companioned." Low effort, high impact. |
| **P4** | NARRATIVE_CONTENT (Haven logs + endgame) | Haven logs reward investment. Endgame narratives give meaning to the whole playthrough. |
| **P5** | FACTION_IDENTITY_REDESIGN (T2 modules + signature mechanics) | Gives each faction a gameplay verb, not just a lore identity. The signature mechanics are the most important piece. |
| **P6** | MEGAPROJECT_SET (4-5 megaprojects) | Endgame capstones. Design docs can be written now; implementation depends on Haven progression. |
| **P7** | CONTENT_WAVES (module families) | Builds on P5. Module tier progression and cross-faction dependencies. |
| **P8** | MUSIC (soundtrack) | Important for feel but requires external composition. Can be briefed now, implemented later. |

### Recommended Next Steps

1. **Faction Storylines**: Write detailed mission design docs for all 5 chains (40 missions). Start with Concord (political thriller is the most accessible genre) and Communion (mystical revelation is the most unique to STE).

2. **Signature Mechanics**: Design and implement the 5 unique verbs (Arbitrate, Broker, Commission, Conscript, Commune). These are the highest-leverage design decisions for faction identity.

3. **FO Commentary Audit**: Inventory existing FO trigger tokens, identify gaps, and author the Tier 1 immediate reaction lines. This is the lowest-effort, highest-feel improvement available.

4. **Music Brief**: Write the timbral palette spec for a composer. Even if music comes last, defining the palette now ensures all other content aligns tonally.

5. **Haven Log Authoring**: Write the 14 ancient logs. These set the tone for the entire narrative layer and can be tested immediately with the existing Haven system.

---

## Sources

### Games Referenced
- Subnautica (2018) — Unknown Worlds — environmental log design
- Dragon Age: Inquisition (2014) — BioWare — Skyhold companion reactivity
- Hades (2020) — Supergiant — dynamic companion commentary (GDC 2020 talk by Greg Kasavin)
- Fallout: New Vegas (2010) — Obsidian — faction chain design
- Mass Effect 2 (2010) — BioWare — companion investment, Suicide Mission
- Outer Wilds (2019) — Mobius — knowledge-gated endgame
- SWTOR (2011) — BioWare — authored faction chains at scale
- Endless Space 2 (2017) — Amplitude — asymmetric faction identity (GDC 2017)
- Stellaris (2016+) — Paradox — megastructures, adaptive music, faction DLC design
- FTL (2012) — Subset Games — stem-based soundtrack, module design
- Slay the Spire (2017) — MegaCrit — card family coherence (GDC 2019)
- EVE Online (2003+) — CCP — faction module trade-off profiles, meta-level tiering
- Factorio (2020) — Wube — endgame rocket launch, "sample of everything" rule
- Satisfactory (2020) — Coffee Stain — Space Elevator visibility, tier progression
- Dyson Sphere Program (2021) — Youthcat — continuous visible progress
- Into the Breach (2018) — Subset Games — zero-overlap unit roles, combo potential
- No Man's Sky (2016+) — Hello Games — content wave recovery, PULSE music system
- Warframe (various) — Digital Extremes — mission template injection, syndicate design
- BioShock (2007) — 2K Boston — audio log narrative technique
- Disco Elysium (2019) — ZA/UM — epilogue specificity
- Baldur's Gate 3 (2023) — Larian — approval + camp conversation loops
- Star Citizen (various) — CIG — fiction-first sentence policy
- Destiny 2 (various) — Bungie — faction gear lessons (exclusivity without uniqueness fails)
- Homeworld (1999/2015) — Relic/Blackbird — fleet architecture as faction identity
- Civilization series — Firaxis — Wonder systems, first-come competitive pressure
- Hollow Knight (2017) — Team Cherry — alien-but-curious musical palette
- Ico (2001) — Team Ico — vast silence in music

### Design References
- "Designing Endgame Systems" — Chapter 12 of *Game Balance* by Ian Schreiber & Brenda Romero (CRC Press, 2021)
- GDC Vault: Kasavin "Narrative Sorcery" (2020), Wube "Factorio Postmortem" (2018-2022), Amplitude "Endless Space 2 Faction Design" (2017), MegaCrit "Designing Slay the Spire" (2019)
