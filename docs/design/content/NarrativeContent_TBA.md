# Narrative Content — To Be Authored

> **Status: TO_BE_AUTHORED**
> This document catalogs all narrative text content that must be written before the
> corresponding game systems can deliver story. Each entry specifies the format,
> delivery system, volume estimate, and dependencies.
>
> Companion to: `NarrativeDesign.md` (delivery architecture), `ExplorationDiscovery.md`
> (discovery lifecycle), `factions_and_lore_v0.md` (world lore).

---

## How To Use This Document

Each content block has:
- **ID**: Stable reference for gate tracking
- **System**: Which SimCore/GameShell system delivers this content
- **System Ready?**: Whether the code infrastructure exists
- **Format**: Expected data shape (text field, JSON, template with tokens)
- **Volume**: How many pieces need to be written
- **Dependencies**: What must be built before this content can be wired in
- **Priority**: IMMEDIATE / HIGH / MEDIUM / LOW
- **Examples**: Sample content showing tone and format

Mark entries AUTHORED when complete. Move authored content to the appropriate
`SimCore/Content/` file or data pack.

---

## 1. Mission Scripts (System Ready)

**ID:** `NARR.MISSIONS`
**System:** `MissionSystem.cs` + `MissionContentV0.cs`
**System Ready:** YES — MissionDef schema supports title, description, steps, triggers, rewards
**Volume:** 30-50 missions across 3 acts
**Dependencies:** None for Act 1. Act 2 needs fracture module. Act 3 needs adaptation fragments.
**Priority:** HIGH — system works, only 4 tutorial missions exist

### Current Missions (4 authored)

| ID | Title | Act | Purpose |
|----|-------|-----|---------|
| M1_MATCHED_LUGGAGE | Matched Luggage | Tutorial | First trade run |
| M2_BULK_HAULER | Bulk Hauler | Tutorial | Fuel delivery |
| M3_PATROL_DUTY | Patrol Duty | Tutorial | Travel and return |
| M4_LONG_HAUL | Long Haul Express | Tutorial | Multi-hop delivery |

### Missions Needed

#### Act 1 — The Rules (Tick 0-400) — 10-15 missions

These teach core systems through gameplay. Each introduces one capability.

| Mission ID | Title (Working) | Teaches | Trigger Types Needed | Reward |
|------------|----------------|---------|---------------------|--------|
| M1.5 | TBA: First Purchase | Buying at market | HaveCargoMin | Credits + trade intel |
| M5 | TBA: Mining Introduction | ResourceTap deployment | DeployProgram (NEW) | Mining site access |
| M6 | TBA: Fleet Acquisition | Buying first freighter | HaveFleetMin (NEW) | Freighter hull |
| M7 | TBA: Automation Basics | TradeCharter setup | ProgramRunning (NEW) | Automation tutorial |
| M8 | TBA: Scanner Upgrade | Research completion | ResearchComplete (NEW) | Scanner Mk1 |
| M9 | TBA: First Discovery | Scan a discovery site | DiscoveryPhase (NEW) | Intel + lore lead |
| M10 | TBA: Faction Introduction | Visit faction station | FactionRepMin (NEW) | Faction standing |
| M11 | TBA: Warfront Supply | Deliver to warfront node | DeliverToWarfront (NEW) | Credits + rep |
| M12 | TBA: Convoy Escort | Escort fleet through danger | EscortComplete (NEW) | Combat modules |
| M13 | TBA: Route Optimization | Profitable multi-hop route | ProfitThreshold (NEW) | Trade route intel |
| M14 | TBA: Under Pressure | Survive a security incident | SurviveIncident (NEW) | Risk reduction |
| M15 | TBA: Intel Gathering | Scan 3 systems | SystemsScanned (NEW) | Scanner data |

**Tone:** Practical, mentor-like. "Here's how this works." No melodrama.
**Voice:** Neutral — these come from the mission board, not a faction.

#### Act 2 — The Escape (Tick 400-1200) — 10-15 missions

These introduce fracture travel, deeper faction relationships, and the first mystery hints.

| Mission ID | Title (Working) | Teaches | Dependencies |
|------------|----------------|---------|-------------|
| M20 | TBA: The Derelict | Fracture module discovery | EPIC.S6.FRACTURE_DISCOVERY_EVENT |
| M21 | TBA: First Jump | Fracture travel | Fracture module acquired |
| M22 | TBA: Signal Source | Follow a discovery lead | Discovery lead system |
| M23 | TBA: Faction Favor | Reach Friendly with any faction | Reputation system |
| M24 | TBA: Supply Lines | Maintain warfront supply chain | Production chains |
| M25 | TBA: The Hidden Signal | Discover Haven coordinates | Fragment collection |
| M26 | TBA: Contraband Run | Smuggle goods past inspection | Trace system |
| M27 | TBA: Technology Race | Research faction-locked tech | T2 modules |
| M28 | TBA: Ancient Readings | Analyze 3 anomalies of same family | Discovery outcomes |
| M29 | TBA: Unstable Ground | Operate in Phase 2+ space | Instability effects |

**Tone:** Mystery and ambition. "Something doesn't add up."

#### Act 3 — The Truth (Tick 1200-2000+) — 5-10 missions

These drive toward endgame. Player has enough fragments to understand the truth.

| Mission ID | Title (Working) | Teaches | Dependencies |
|------------|----------------|---------|-------------|
| M40 | TBA: The Containment Argument | Reveal lane purpose | 8+ fragments collected |
| M41 | TBA: Haven Awakening | Activate Haven starbase | Haven discovered |
| M42 | TBA: Faction Alignment | Lock faction alliance for endgame | Rep Allied with 1+ faction |
| M43 | TBA: The Lattice Speaks | Encounter Lattice drones | Phase 3+ space |
| M44 | TBA: Endgame: Reinforce | Stabilize lanes (Concord path) | Endgame choice |
| M45 | TBA: Endgame: Naturalize | Build accommodation (frontier path) | Endgame choice |
| M46 | TBA: Endgame: Renegotiate | Contact instability (Communion path) | Endgame choice |

**Tone:** Weight and consequence. "This changes everything."

### New Trigger Types Needed

| Trigger Type | Description | Epic Dependency |
|-------------|-------------|-----------------|
| DeployProgram | Player deploys a specific program type | None (system exists) |
| HaveFleetMin | Player owns N fleet ships | None (system exists) |
| ProgramRunning | A program of type X is active and not stalled | None (system exists) |
| ResearchComplete | Player completes a specific tech | None (system exists) |
| DiscoveryPhase | Discovery at node reaches phase X | None (system exists) |
| FactionRepMin | Reputation with faction X reaches tier Y | None (system exists) |
| DeliverToWarfront | Deliver goods to a warfront-demand node | S7.WARFRONT_STATE |
| EscortComplete | Escort program completes without loss | None (system exists) |
| ProfitThreshold | Earn N credits in a single trade route | X.LEDGER_EVENTS |
| SurviveIncident | Experience and survive a security incident | None (system exists) |
| SystemsScanned | Visit and scan N distinct systems | None (system exists) |
| FragmentsCollected | Collect N adaptation fragments | S8.ADAPTATION_FRAGMENTS |

---

## 2. Discovery Narrative Text (System Partially Ready)

**ID:** `NARR.DISCOVERY`
**System:** `DiscoveryOutcomeSystem.cs` + `DiscoverySitePanel` (GDScript)
**System Ready:** PARTIAL — phase lifecycle works but no text field on DiscoveryStateV0
**Volume:** ~30 templates (10 per family) + ~15 chain connections
**Dependencies:** Add `scan_text` and `analysis_text` fields to DiscoveryStateV0
**Priority:** HIGH — discoveries currently have mechanics but no story

### Format

Each discovery family has narrative templates with token substitution:

```
{faction} = owning/originating faction name
{age} = estimated age from seed
{system} = current system name
{damage_type} = weapon signature description
{enemy_faction} = opposing faction
{loot} = recovered items summary
{lead_system} = destination of discovery lead
```

### Derelict Family Templates (10 needed)

**On Scan (Scanned phase):**

```
DRLK.SCAN.001: "Sensor readings detect a hull fragment with residual power.
Construction pattern matches no known active faction."

DRLK.SCAN.002: "An abandoned cargo hauler. Emergency logs indicate the crew
evacuated. Cargo hold integrity: {cargo_pct}%."

DRLK.SCAN.003: "A warship fragment lodged in an asteroid. Serial markings
partially legible. Age estimate: {age} years."

DRLK.SCAN.004: "Scattered debris field. Energy signatures suggest recent
destruction — within the last {age} years."

DRLK.SCAN.005: "TBA"
DRLK.SCAN.006: "TBA"
DRLK.SCAN.007: "TBA"
DRLK.SCAN.008: "TBA"
DRLK.SCAN.009: "TBA"
DRLK.SCAN.010: "TBA"
```

**On Analysis (Analyzed phase):**

```
DRLK.ANALYSIS.001: "The hull fragment is a {faction} scout vessel, destroyed
approximately {age} years ago. The damage pattern suggests {enemy_faction}
weapons. This far from {faction} space, the scout was likely on a covert
mission when intercepted."

DRLK.ANALYSIS.002: "Emergency logs recovered. The crew attempted to jettison
cargo before abandoning ship. Manifest shows {loot}. Final log entry:
'They came from the shimmer. Not raiders. Something older.'"

DRLK.ANALYSIS.003: "Serial markings identify this as the [{ship_name}], lost
during the {historical_event}. Historical records list it as 'presumed
destroyed in transit.' It was not in transit."

DRLK.ANALYSIS.004-010: "TBA"
```

### Ruin Family Templates (10 needed)

**On Scan:**

```
RUIN.SCAN.001: "Ancient structures partially buried. Architecture does not
match any known faction. Material samples suggest {age} years of exposure."

RUIN.SCAN.002: "A collapsed research outpost. Equipment markings indicate
{faction} origin. Data cores detected but encrypted."

RUIN.SCAN.003-010: "TBA"
```

**On Analysis:**

```
RUIN.ANALYSIS.001: "The structures predate all five factions by millennia.
Construction technique uses materials that remain stable under metric
variance — a property no modern engineering achieves. Someone built this
to survive conditions that shouldn't exist in lane-stabilized space."

RUIN.ANALYSIS.002: "Data cores decrypted. Fragmentary records describe
a research program studying 'accommodation geometry' — engineering that
functions regardless of local metric state. The research was abandoned.
The reason is not recorded."

RUIN.ANALYSIS.003-010: "TBA"
```

### Signal Family Templates (10 needed)

**On Scan:**

```
SGNL.SCAN.001: "A repeating beacon on frequency {freq}. Signal analysis
suggests artificial origin, broadcasting for approximately {age} years.
Source direction: toward {lead_system}."

SGNL.SCAN.002: "Intermittent energy spikes. Pattern analysis shows non-random
structure. Possible encoded message."

SGNL.SCAN.003-010: "TBA"
```

**On Analysis:**

```
SGNL.ANALYSIS.001: "The beacon frequency matches the {faction} emergency
transponder standard — but the encoding predates {faction} civilization by
centuries. Someone was using this frequency long before {faction} existed."

SGNL.ANALYSIS.002: "Message decoded. Contents are coordinate sets — not for
systems, but for specific positions within systems. Three coordinates
correspond to known discovery sites. One does not match any cataloged
location: {lead_system}."

SGNL.ANALYSIS.003-010: "TBA"
```

### Narrative Chain Connections (15 needed)

When discoveries connect through leads, the analysis text should reference
the source discovery. Format:

```
CHAIN.001: Source=Derelict@{system_a}, Target=Signal@{system_b}
Connection text: "The beacon frequency matches the emergency transponder
recovered from the {system_a} derelict. A second vessel was here."

CHAIN.002: Source=Signal@{system_a}, Target=Ruin@{system_b}
Connection text: "Coordinates from the {system_a} signal match these
ruins. {enemy_faction} weapon marks on the structures match the damage
pattern on both vessels."

CHAIN.003-015: "TBA"
```

---

## 3. Faction Station Dialogue (System NOT Ready)

**ID:** `NARR.FACTION_DIALOGUE`
**System:** No dialogue system exists yet
**System Ready:** NO — needs text display panel + faction voice styling
**Volume:** ~50 dialogue sets (5 factions x 5 rep tiers x 2 variants)
**Dependencies:** EPIC.S7.NARRATIVE_DELIVERY (dialogue display system)
**Priority:** MEDIUM — factions are mechanically present but narratively silent

### Voice Guidelines (from NarrativeDesign.md)

| Faction | Voice | Style | Example |
|---------|-------|-------|---------|
| **Concord** | Bureaucratic | Euphemisms, formal | "A lane optimization event occurred in Sector 7." (= lane failure) |
| **Chitin** | Probabilistic | Data-driven, collective | "Probability of successful transit: 73.2%. The hive recommends." |
| **Weavers** | Structural | Engineering-focused | "The lattice integrity at this junction reads nominal." |
| **Valorin** | Frontier | Bravado, direct | "You want the good routes? Earn them. Nothing's free past the line." |
| **Communion** | Experiential | Personal, mystical | "The space between the stars... it remembers. Can you feel it?" |

### Dialogue Structure

Each dialogue set consists of:
- **Greeting** (1-2 lines): What the station rep says when you dock
- **Intel snippet** (1-2 lines): Faction-flavored local information
- **Farewell** (1 line): Departure line

Keyed by reputation tier:

| Tier | Concord Greeting (Example) | Chitin Greeting (Example) |
|------|---------------------------|--------------------------|
| **Enemy** | "Your trade license is suspended. Security has been notified." | "Threat assessment: maximum. Hive consensus: deny all services." |
| **Hostile** | "You may dock, but your activities are being monitored." | "Probability of deception: 67%. Limited services authorized." |
| **Neutral** | "Welcome to {station}. Standard tariffs apply." | "New entity cataloged. The hive observes with interest." |
| **Friendly** | "Good to see you again. We've set aside some priority cargo." | "Favorable probability matrix established. The hive shares willingly." |
| **Allied** | "Commander. Your contributions to stability are noted at the highest levels." | "Symbiosis confirmed. The hive integrates your patterns as beneficial." |

### Content Blocks To Be Authored

```
DIAL.CONCORD.ENEMY.GREET.001: "TBA"
DIAL.CONCORD.ENEMY.INTEL.001: "TBA"
DIAL.CONCORD.ENEMY.FAREWELL.001: "TBA"
DIAL.CONCORD.HOSTILE.GREET.001: "TBA"
... (5 factions x 5 tiers x 3 types = 75 entries)
```

---

## 4. Jump Event Flavor Text (System Partially Ready)

**ID:** `NARR.JUMP_EVENTS`
**System:** `JumpEvent.cs` — has Kind (Salvage/Signal/Turbulence) but no text field
**System Ready:** PARTIAL — needs `flavor_text` field on JumpEvent
**Volume:** ~15 templates (5 per kind)
**Dependencies:** Add text field to JumpEvent entity
**Priority:** MEDIUM

### Current State

Jump events are purely mechanical: "Salvage: found 3 fuel."

### Templates Needed

**Salvage Kind:**
```
JUMP.SALVAGE.001: "Debris field in the lane corridor. Fuel cells scattered
— emergency jettison. Hull markings indicate {faction} cargo hauler."

JUMP.SALVAGE.002: "Abandoned supply crate tethered to a lane buoy.
Contents intact: {loot}. Someone left this here deliberately."

JUMP.SALVAGE.003-005: "TBA"
```

**Signal Kind:**
```
JUMP.SIGNAL.001: "Your scanner picks up a burst transmission. Encrypted,
but the header matches {faction} military channels. Coordinates logged."

JUMP.SIGNAL.002-005: "TBA"
```

**Turbulence Kind:**
```
JUMP.TURB.001: "Lane field fluctuation. Your hull groans as local metrics
shift. Instruments read normal a moment later — but the readings were
wrong for 0.3 seconds."

JUMP.TURB.002-005: "TBA"
```

---

## 5. Risk Threshold Warning Messages (System NOT Ready)

**ID:** `NARR.RISK_WARNINGS`
**System:** Risk meter UI (not implemented) + toast system
**System Ready:** NO — needs EPIC.S7.RISK_METER_UI
**Volume:** 15 messages (3 meters x 5 thresholds)
**Dependencies:** EPIC.S7.RISK_METER_UI
**Priority:** MEDIUM

### Messages

| Meter | Threshold | Toast Text |
|-------|-----------|------------|
| Heat | Noticed | "Heat: Noticed — merchants are adjusting to your presence." |
| Heat | Elevated | "Heat: Elevated — competitors are watching your routes." |
| Heat | High | "Warning: Heat High — expect supply restrictions at overtraded markets." |
| Heat | Critical | "ALERT: Heat Critical — markets may close to outsiders." |
| Influence | Noticed | "Influence: Noticed — factions are aware of your activities." |
| Influence | Elevated | "Influence: Elevated — expect diplomatic attention." |
| Influence | High | "Warning: Influence High — neutral factions are choosing sides." |
| Influence | Critical | "ALERT: Influence Critical — hostile factions are mobilizing." |
| Trace | Noticed | "Trace: Noticed — inspection frequency increasing." |
| Trace | Elevated | "Trace: Elevated — patrol fleets appearing on your routes." |
| Trace | High | "Warning: Trace High — expect cargo confiscation on inspection." |
| Trace | Critical | "ALERT: Trace Critical — active pursuit fleet dispatched." |
| Any | Decay below Noticed | "Risk level reduced. Situation normalizing." |

---

## 6. Trade Good Descriptions (System NOT Ready)

**ID:** `NARR.TRADE_GOODS`
**System:** Market UI, trade_goods_v0.md
**System Ready:** PARTIAL — goods exist in registry but no description field
**Volume:** 13 descriptions
**Dependencies:** Add description field to GoodDefV0
**Priority:** LOW

### Descriptions Needed

| Good ID | Description |
|---------|-------------|
| ORE | "TBA — raw mineral aggregate extracted from asteroid fields." |
| METAL | "TBA — refined metallic alloys, basis of all construction." |
| FUEL | "TBA — lane-compatible energy medium, consumed by every vessel." |
| FOOD | "TBA — organic nutrition packs, essential for crewed stations." |
| RARE_METALS | "TBA — exotic metallic compounds found in specific geological formations." |
| CHEMICALS | "TBA — industrial reagents for manufacturing and processing." |
| COMPOSITES | "TBA — engineered materials combining multiple base components." |
| ELECTRONICS | "TBA — computational and sensor components." |
| COMPONENTS | "TBA — assembled mechanical sub-systems." |
| MUNITIONS | "TBA — ammunition and ordnance for combat systems." |
| MEDICINE | "TBA — pharmaceutical supplies for crew health and station services." |
| LUXURY_GOODS | "TBA — high-value consumer products for wealthy station populations." |
| SALVAGED_TECH | "TBA — recovered technology from derelicts and ruins, not yet analyzed." |

---

## 7. Tech Description Enhancement (System Partially Ready)

**ID:** `NARR.TECH_DESCS`
**System:** `TechContentV0.cs` — has Description field (functional text only)
**System Ready:** YES — field exists, just needs better text
**Volume:** 12 enhanced descriptions
**Dependencies:** None
**Priority:** LOW

Current descriptions are purely functional ("Increases fleet travel speed").
Need narrative-flavored descriptions that connect to world lore.

```
TECH.WARP_I: Current: "Increases fleet travel speed"
             Enhanced: "TBA — reference to lane field harmonics research"

TECH.SENSORS_I: Current: "Improves scanner range"
                Enhanced: "TBA — reference to ancient sensor fragment recovery"

... (12 total)
```

---

## Summary

| Block | ID | Volume | System Ready | Priority |
|-------|-----|--------|-------------|----------|
| Mission Scripts | NARR.MISSIONS | 30-50 | YES | HIGH |
| Discovery Text | NARR.DISCOVERY | ~45 | PARTIAL | HIGH |
| Faction Dialogue | NARR.FACTION_DIALOGUE | ~75 | NO | MEDIUM |
| Jump Event Text | NARR.JUMP_EVENTS | ~15 | PARTIAL | MEDIUM |
| Risk Warnings | NARR.RISK_WARNINGS | 15 | NO | MEDIUM |
| Trade Good Descs | NARR.TRADE_GOODS | 13 | PARTIAL | LOW |
| Tech Descriptions | NARR.TECH_DESCS | 12 | YES | LOW |
| **Total** | | **~225** | | |
