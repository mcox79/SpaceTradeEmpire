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
| M40 | TBA: The Shape of the Cage | Revelation 3: pentagon ring is engineered | Fracture-space trade breaks ring pattern (gameplay trigger) |
| M41 | TBA: Haven Awakening | Activate Haven starbase | Haven discovered |
| M42 | TBA: Faction Alignment | Lock faction alliance for endgame | Rep Allied with 1+ faction |
| M43 | TBA: The Lattice Speaks | Encounter Lattice drones | Phase 3+ space |
| M44 | TBA: You're Not the First | Revelation 4: Communion elder truth | Communion max rep |
| M45 | TBA: Endgame: Reinforce | Stabilize threads — choose the cage knowingly | Endgame choice |
| M46 | TBA: Endgame: Naturalize | Break the ring — accept the consequences | Endgame choice |
| M47 | TBA: Endgame: Renegotiate | Contact instability — were my choices my own? | Endgame choice |

**Tone:** Weight and consequence. "This changes everything."

**Note on M40 (Economy Revelation):** This mission does NOT trigger from text
or data logs. It triggers from **gameplay**: the player establishes a fracture-space
trade route and observes the dependency ring breaking. The mission confirms
and frames what the player already witnessed. See `LoreContent_TBA.md` →
LORE.PENTAGON_EVIDENCE for the supporting data logs.

**Note on M44 (Communion Truth):** The simplest, most devastating moment in the
game. A Communion elder tells the truth calmly and directly: "You're not the
first. Every few generations, someone finds a piece of it. We've learned that
telling people what they carry doesn't help. So we watch. We help when you ask.
We hope. You've gone further than any of them." No drama. No betrayal music.
The player is not chosen — they are the latest in a long line, and the
Communion has been quietly mourning most of their predecessors. The devastating
question: am I repeating a pattern that always ends the same way?

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

DRLK.SCAN.005: "A vessel hull section, crumpled like paper. Whatever hit this
ship didn't care about armor ratings. Power core: depleted but structurally intact."

DRLK.SCAN.006: "Two ships locked together — boarding action frozen mid-assault.
Both crews long gone. The larger vessel bears {faction} military markings.
The smaller ship bears no markings at all."

DRLK.SCAN.007: "A single escape pod, intact, drifting in slow rotation. Beacon
active but broadcasting on an obsolete frequency. Life signs: none.
Last telemetry update: {age} years ago."

DRLK.SCAN.008: "Cargo containers scattered across a wide debris field.
Contents partially preserved in vacuum. No parent vessel detected —
either it jumped away or disintegrated completely."

DRLK.SCAN.009: "A research vessel, {faction} registry, systems powered down
in an orderly shutdown sequence. Not destroyed — abandoned. Every airlock
sealed from the outside."

DRLK.SCAN.010: "Wreckage embedded in a small asteroid. Impact trajectory
suggests the vessel was traveling at thread-transit speeds when it stopped.
Outside a thread corridor."
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

DRLK.ANALYSIS.004: "Debris analysis complete. This wasn't a battle — it was
a test. Weapon discharge patterns show methodical, calibrated strikes at
increasing power levels. Someone was measuring how much force it took to
destroy a {faction} cruiser. The answer: not much."

DRLK.ANALYSIS.005: "Power core isotope decay confirms age: {age} years.
The core uses a containment geometry that modern engineering considers
impossible — a hexagonal lattice that shouldn't remain stable. It remained
stable. Recovered: {loot}."

DRLK.ANALYSIS.006: "The boarding action was mutual. Both ships deployed
assault teams simultaneously, as if neither expected the other. Recovered
personal logs suggest both crews were searching for the same thing. Neither
log says what."

DRLK.ANALYSIS.007: "Escape pod passenger: single occupant, {faction} science
division. Personal effects include handwritten coordinates for three systems.
Two match known discovery sites. The third: {lead_system}."

DRLK.ANALYSIS.008: "Cargo manifest reconstructed. Contents: standard trade
goods plus one item listed as 'Sample — DO NOT OPEN IN ATMOSPHERE.' The
container for that item is present. It is open. It is empty. Recovered: {loot}."

DRLK.ANALYSIS.009: "Ship's research logs intact. Final entry describes
'anomalous metric readings consistent with pre-thread conditions.' The crew
didn't flee an attack. They detected something in the local spacetime and
chose to leave. Calmly. Permanently."

DRLK.ANALYSIS.010: "Impact velocity confirms: this vessel was ejected from a
thread corridor mid-transit. Thread corridors don't do that. Current models
say they can't. Navigational data shows the ship was heading toward
{lead_system} when it was... rejected."
```

### Ruin Family Templates (10 needed)

**On Scan:**

```
RUIN.SCAN.001: "Ancient structures partially buried. Architecture does not
match any known faction. Material samples suggest {age} years of exposure."

RUIN.SCAN.002: "A collapsed research outpost. Equipment markings indicate
{faction} origin. Data cores detected but encrypted."

RUIN.SCAN.003: "A geometric foundation, perfectly level, carved into bedrock.
No superstructure remains. Whatever stood here was either demolished or
removed with extraordinary precision."

RUIN.SCAN.004: "Underground chambers detected beneath surface debris. Energy
readings suggest residual power — something down there is still drawing
current after {age} years."

RUIN.SCAN.005: "A ring of standing stones — except spectral analysis confirms
they aren't stone. They're metallic, coated in mineral accretion. Arrangement
pattern: pentagonal."

RUIN.SCAN.006: "Surface structures destroyed, but subsurface channels remain
intact. The channel network extends beyond scanner range. Fluid residue
detected — not water."

RUIN.SCAN.007: "A single monolithic structure, untouched by weathering. Surface
temperature: exactly ambient. It absorbs precisely as much energy as it
receives. Nothing more. Nothing less."

RUIN.SCAN.008: "Ruins of a settlement — domestic scale, not military. People
lived here. Cooking surfaces, sleeping alcoves, what appear to be children's
play areas. All {age} years old."

RUIN.SCAN.009: "A tower, intact, rising from otherwise leveled surroundings.
Radar passes through it, but visible light does not. It is physically present
and spectrally invisible."

RUIN.SCAN.010: "Fragments of a large spherical structure. Original diameter
estimate: 200 meters. Internal scorch marks consistent with — according to
the scanner — 'metric decoherence.' The scanner shouldn't know that term."
```

**On Analysis:**

```
RUIN.ANALYSIS.001: "The structures predate all five factions by millennia.
Construction technique uses materials that remain stable under metric
variance — a property no modern engineering achieves. Someone built this
to survive conditions that shouldn't exist in thread-stabilized space."

RUIN.ANALYSIS.002: "Data cores decrypted. Fragmentary records describe
a research program studying 'accommodation geometry' — engineering that
functions regardless of local metric state. The research was abandoned.
The reason is not recorded."

RUIN.ANALYSIS.003: "The foundation geometry matches no known architectural
tradition. Each angle is precisely 108 degrees — pentagonal symmetry repeated
at every scale. This was not a building. It was a diagram, built in stone,
meant to be read from above."

RUIN.ANALYSIS.004: "Power source located: a sealed chamber containing a
crystalline lattice still converting ambient radiation to current after {age}
years. Efficiency: 99.7%. Modern best: 34%. Whatever made this understood
energy storage in ways we do not. Recovered: {loot}."

RUIN.ANALYSIS.005: "The 'stones' are antenna elements. When active, the
pentagonal array would have broadcast a signal detectable across multiple
systems. The frequency matches no known standard — but it matches the beacon
at {lead_system}."

RUIN.ANALYSIS.006: "Fluid analysis: a suspension of metallic nanoparticles in
an organic solvent. Function unknown. The channel network connects this site
to at least two other locations beyond scanner range. Whatever flowed here
was being distributed, not contained."

RUIN.ANALYSIS.007: "The monolith is a single piece — no joins, no seams, no
tool marks. It did not erode because its surface repels all molecular
interaction. It is, thermodynamically, a perfect boundary. The engineering
required to create this does not exist. Recovered: {loot}."

RUIN.ANALYSIS.008: "The settlement was not attacked. Personal items remain in
place. Meals left mid-preparation. The inhabitants did not flee — they
simply stopped being here. No remains. No evacuation signs. {age} years of
silence."

RUIN.ANALYSIS.009: "The tower is a waveguide. It channels energy along a
single axis — straight up, through the atmosphere, aimed at a point in deep
space. That point corresponds to the coordinates of {lead_system}. The tower
has been pointing there for {age} years."

RUIN.ANALYSIS.010: "The sphere was a containment vessel. Interior metric
readings are still unstable — measurements taken 10 seconds apart disagree
by 3-7%. Something was held here that damaged the consistency of local
spacetime. It is no longer held here."
```

### Signal Family Templates (10 needed)

**On Scan:**

```
SGNL.SCAN.001: "A repeating beacon on frequency {freq}. Signal analysis
suggests artificial origin, broadcasting for approximately {age} years.
Source direction: toward {lead_system}."

SGNL.SCAN.002: "Intermittent energy spikes. Pattern analysis shows non-random
structure. Possible encoded message."

SGNL.SCAN.003: "A directional transmission, tight-beam, aimed at a specific
point in deep space. Signal strength suggests the transmitter is nearby but
concealed — possibly underground or embedded in an asteroid."

SGNL.SCAN.004: "Carrier wave detected on a frequency reserved for emergency
distress calls. But the modulation is wrong — too slow, too regular. This
isn't a distress call. It's a heartbeat."

SGNL.SCAN.005: "Faint signal, nearly lost in background radiation. Spectral
analysis shows it originates from the thread corridor itself — not from any
object in normal space. The thread is broadcasting."

SGNL.SCAN.006: "Burst transmission, repeating every {age} seconds. Each burst
contains what appears to be navigational data — system coordinates in a
format that predates Concord standardization."

SGNL.SCAN.007: "Two signals, same frequency, slightly out of phase. One
source is local. The other is responding from the direction of {lead_system}.
They've been having this conversation for {age} years."

SGNL.SCAN.008: "Broadband emission across all scanner frequencies
simultaneously. Not noise — structured. Like someone shouting in every
language at once to make sure someone, anyone, hears."

SGNL.SCAN.009: "Signal embedded in the local thread's carrier frequency.
You'd never detect it without looking. It's piggy-backing on the thread
infrastructure — using the threads themselves as an antenna."

SGNL.SCAN.010: "A signal that wasn't there 30 seconds ago. Your approach
triggered it. Proximity-activated beacon — still functional after {age} years.
Someone wanted to know when visitors arrived."
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

SGNL.ANALYSIS.003: "Transmitter located: buried 40 meters below the surface
of the third moon. It has been transmitting toward the same deep-space
coordinates for {age} years. The target point is empty. Or it was empty
{age} years ago. The signal's travel time to its destination: 3 days.
Someone at {lead_system} has been receiving this for a very long time."

SGNL.ANALYSIS.004: "The 'heartbeat' is a status report. Decoded fields
include: metric stability index, thread integrity percentage, population
count (zero for {age} years), and a field labeled 'accommodation state'
that reads 'SUSPENDED.' This station was monitoring something. It's still
monitoring."

SGNL.ANALYSIS.005: "The thread itself is the antenna. The signal is encoded
in micro-variations in the thread's energy density — variations so small
they fall below standard sensor resolution. Your module detected them. The
signal is a map. It shows thread connections that don't appear on any chart.
One leads to {lead_system}."

SGNL.ANALYSIS.006: "Coordinates decoded. The navigation data describes a
route through 7 systems, using thread corridors that official charts show
as inactive. The route terminates at {lead_system}. Timestamp on the data:
{age} years before the Concord existed."

SGNL.ANALYSIS.007: "Both signals decoded. They're exchanging metric
calibration data — comparing measurements of the same physical constants
from two different locations. The measurements don't agree. They haven't
agreed for {age} years. The signals are documenting metric bleed in real time."

SGNL.ANALYSIS.008: "The broadband emission is a warning. In twelve languages,
seven of which correspond to known faction linguistic roots, it says
approximately the same thing: 'The measurements are failing. Do not trust
the constants. Seek stable ground.' Recovered: {loot}."

SGNL.ANALYSIS.009: "The piggy-backed signal is a census. It counts every
vessel that transits this thread corridor and transmits the tally to
{lead_system} every 100 transits. Current count since last reset: 4,291.
Someone is tracking thread traffic. They have been tracking it for {age} years."

SGNL.ANALYSIS.010: "The proximity beacon's payload is a single data packet.
Contents: a coordinate set, a timestamp ({age} years ago), and a phrase
in an unknown language. Linguistic analysis matches Communion root phonemes
but precedes their civilization by centuries. Translation estimate:
'You found it. Now decide.'"
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

CHAIN.003: Source=Ruin@{system_a}, Target=Ruin@{system_b}
Connection text: "The pentagonal foundation at {system_a} and the antenna
array here share identical angular geometry. Built by the same architects,
separated by light-years."

CHAIN.004: Source=Derelict@{system_a}, Target=Ruin@{system_b}
Connection text: "The derelict's navigational logs at {system_a} list these
ruins as a waypoint. The crew was headed here. They didn't make it."

CHAIN.005: Source=Signal@{system_a}, Target=Signal@{system_b}
Connection text: "Same carrier frequency, same modulation pattern. The
{system_a} signal and this one are part of the same network — nodes in a
communication grid that predates every faction."

CHAIN.006: Source=Ruin@{system_a}, Target=Signal@{system_b}
Connection text: "The waveguide tower at {system_a} was aimed at this exact
position. Whatever it was transmitting to, it was transmitting here."

CHAIN.007: Source=Derelict@{system_a}, Target=Derelict@{system_b}
Connection text: "Same hull alloy, same construction technique. These two
vessels were part of the same fleet. They died in different systems, {age}
years apart."

CHAIN.008: Source=Signal@{system_a}, Target=Ruin@{system_b}
Connection text: "The navigational data in the {system_a} signal terminates
at these coordinates. End of the route. Whatever was being directed here,
this is where it was supposed to arrive."

CHAIN.009: Source=Ruin@{system_a}, Target=Derelict@{system_b}
Connection text: "The fluid channels at {system_a} contained trace
elements matching this vessel's hull composition. The ship was built using
materials sourced from the ruins — or the ruins were built from the same
materials as the ship."

CHAIN.010: Source=Derelict@{system_a}, Target=Signal@{system_b}
Connection text: "The 'Sample' container from the {system_a} cargo manifest
matches the material signature of this signal's transmitter casing. The
sample was a piece of the beacon. Someone was collecting them."

CHAIN.011: Source=Signal@{system_a}, Target=Derelict@{system_b}
Connection text: "The census beacon at {system_a} logged this vessel's
final transit. Timestamp matches the estimated time of destruction. It
was being watched as it died."

CHAIN.012: Source=Ruin@{system_a}, Target=Ruin@{system_b}
Connection text: "Both sites show the same 'metric decoherence' scarring.
Whatever was contained at {system_a} was also contained here. Two cages for
the same phenomenon."

CHAIN.013: Source=Derelict@{system_a}, Target=Ruin@{system_b}
Connection text: "The research logs from the {system_a} vessel describe
'pre-thread conditions.' These ruins were built to function in exactly
those conditions. The researchers found what they were looking for."

CHAIN.014: Source=Signal@{system_a}, Target=Signal@{system_b}
Connection text: "The metric calibration exchange between {system_a} and
{system_b} has been running for {age} years. The disagreement between
their measurements has been growing. Slowly. Steadily."

CHAIN.015: Source=ANY@{system_a}, Target=Haven
Connection text: "Every signal, every ruin, every wreck. The coordinates
all point inward — toward a convergence. The network wasn't random. It was
a map, and this is where the map leads."
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
| **Concord** | Bureaucratic | Euphemisms, formal | "A thread optimization event occurred in Sector 7." (= thread failure) |
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

### Authored Greeting Lines (5 factions x 5 tiers)

**Concord:**
```
DIAL.CONCORD.ENEMY.GREET.001: "Concord Customs: Your trade license is
flagged. Dock at your own risk. Enforcement has been notified."

DIAL.CONCORD.HOSTILE.GREET.001: "Concord Customs: License verified. Be
advised — your recent activity is under review. Trade normally."

DIAL.CONCORD.NEUTRAL.GREET.001: "Welcome to {station}. Concord Customs:
All manifests subject to standard inspection. Fair trade, fair passage."

DIAL.CONCORD.FRIENDLY.GREET.001: "Concord Customs: Priority clearance
granted. Your compliance record is noted. Preferred docking bay assigned."

DIAL.CONCORD.ALLIED.GREET.001: "Concord Customs: Executive clearance.
The Arbiter sends regards. Your contributions to stability are... appreciated."
```

**Valorin:**
```
DIAL.VALORIN.ENEMY.GREET.001: "Valorin Port Authority: You are not welcome
here. Your vessel will be escorted. Make no aggressive moves."

DIAL.VALORIN.HOSTILE.GREET.001: "Valorin Port Authority: Dock in the outer
ring. Your movements will be monitored. Honor your word and we'll honor ours."

DIAL.VALORIN.NEUTRAL.GREET.001: "Valorin Station Control: Permission to dock.
The fleet acknowledges your neutrality. Trade with honor."

DIAL.VALORIN.FRIENDLY.GREET.001: "Welcome, trader. The Admiralty recognizes
your service. Inner ring access granted. May your cargo honor its destination."

DIAL.VALORIN.ALLIED.GREET.001: "The fleet salutes you. Valorin command extends
full port privileges. You have proven yourself a worthy partner in these waters."
```

**Weaver:**
```
DIAL.WEAVER.ENEMY.GREET.001: "Weaver Anchorage: Your thread is tangled here.
Dock briefly if you must, but the loom remembers debts."

DIAL.WEAVER.HOSTILE.GREET.001: "Weaver Anchorage: You may dock. The
pattern-readers note your approach. Thread carefully."

DIAL.WEAVER.NEUTRAL.GREET.001: "Welcome to the Weaver Anchorage. All threads
converge in trade. Browse our markets at your leisure."

DIAL.WEAVER.FRIENDLY.GREET.001: "Ah, a returning thread. The loom has a place
for you. Preferred rates apply — patience is rewarded here."

DIAL.WEAVER.ALLIED.GREET.001: "The Weavers greet a fellow pattern-reader.
You understand the threads as few outsiders do. Our markets are your markets."
```

**Chitin:**
```
DIAL.CHITIN.ENEMY.GREET.001: "Chitin Exchange: Bad odds, pilot. Your credit's
no good here. We don't forget a bad bet."

DIAL.CHITIN.HOSTILE.GREET.001: "Chitin Exchange: You can dock, but the house
is watching. Play straight and we'll deal straight."

DIAL.CHITIN.NEUTRAL.GREET.001: "Welcome to the Chitin Exchange! Everything's
a trade, friend. Step up, place your bets. The market's always open."

DIAL.CHITIN.FRIENDLY.GREET.001: "The house likes you, pilot. You know when
to hold and when to deal. Preferred spreads — you've earned them."

DIAL.CHITIN.ALLIED.GREET.001: "VIP floor access, high roller. The Syndicate
remembers every winning bet, and you've run the table. Name your terms."
```

**Communion:**
```
DIAL.COMMUNION.ENEMY.GREET.001: "Communion Relay: Your signal is... discordant.
Dock if you seek understanding. But know that we observe."

DIAL.COMMUNION.HOSTILE.GREET.001: "Communion Relay: We sense your approach.
You carry questions you haven't asked yet. Dock. Listen."

DIAL.COMMUNION.NEUTRAL.GREET.001: "Welcome, traveler. The Communion studies
connection. All who arrive here have been drawn. That is not coincidence."

DIAL.COMMUNION.FRIENDLY.GREET.001: "We have felt your journeys through the
threads. You are beginning to sense what we have always known. Welcome home,
in a way."

DIAL.COMMUNION.ALLIED.GREET.001: "The resonance is unmistakable now. You carry
a fragment of the old connection — whether you know it or not. We are kin,
traveler."
```

### Remaining Content Blocks To Be Authored

```
DIAL.{FACTION}.{TIER}.INTEL.001: TBA (25 entries — faction-flavored local intel)
DIAL.{FACTION}.{TIER}.FAREWELL.001: TBA (25 entries — departure lines)
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
JUMP.SALVAGE.001: "Debris field in the thread corridor. Fuel cells scattered
— emergency jettison. Hull markings indicate {faction} cargo hauler."

JUMP.SALVAGE.002: "Abandoned supply crate tethered to a thread buoy.
Contents intact: {loot}. Someone left this here deliberately."

JUMP.SALVAGE.003: "A shipping container wedged into the thread wall — half in
normal space, half... elsewhere. Contents recoverable: {loot}. The container
itself is best left where it is."

JUMP.SALVAGE.004: "Micro-debris field. Scanner identifies it as pulverized hull
plating — too fine to reconstruct origin. But the dust contains trace
amounts of {loot}. Someone's misfortune, your gain."

JUMP.SALVAGE.005: "A {faction} supply pod, trajectory-locked to this thread
corridor. Automated delivery system — except no destination was programmed.
It's been bouncing between endpoints for {age} years. Contents: {loot}."
```

**Signal Kind:**
```
JUMP.SIGNAL.001: "Your scanner picks up a burst transmission. Encrypted,
but the header matches {faction} military channels. Coordinates logged."

JUMP.SIGNAL.002: "A fragment of conversation bleeds through the thread wall —
two voices, neither speaking a language your translator recognizes. The
exchange lasted 0.4 seconds. Coordinates logged."

JUMP.SIGNAL.003: "Your module registers a resonance spike. For a moment, the
thread corridor felt... wider. The instruments show nothing unusual. But the
module's power draw increased by 3% and hasn't come back down."

JUMP.SIGNAL.004: "Navigation beacon detected — not on any official chart.
Broadcasting a simple repeating sequence: system coordinates for a location
two hops from here. {faction} encoding, but the signal predates {faction}."

JUMP.SIGNAL.005: "A ghost echo of your own ship's transponder, reflected back
at you from somewhere deeper in the thread. The echo is 0.7 seconds delayed.
Threads don't have echoes."
```

**Turbulence Kind:**
```
JUMP.TURB.001: "Thread field fluctuation. Your hull groans as local metrics
shift. Instruments read normal a moment later — but the readings were
wrong for 0.3 seconds."

JUMP.TURB.002: "Brief shudder through the hull. The thread corridor's energy
density spiked — like turbulence in atmosphere. Your cargo shifted 2cm in
the hold. Everything reads normal now. Everything always reads normal after."

JUMP.TURB.003: "For 1.3 seconds, your instruments displayed two different
positions for your ship simultaneously. Both readings were internally
consistent. Both claimed to be correct. Then one disappeared."

JUMP.TURB.004: "The thread walls rippled. Visible, iridescent distortion
passing along the corridor like a wave. Beautiful, actually. Your fuel
consumption ticked up by 0.1% during the event. Correlation uncertain."

JUMP.TURB.005: "Navigation drift: your projected arrival shifted by 4 seconds
mid-transit. Not a calculation error — the distance itself changed. The
thread contracted, then expanded. You arrived on time. The thread decided
you would."
```

### General Jump Flavor Text (Ambient — No Mechanical Effect)

These fire on routine jumps to keep the thread corridors feeling alive:

```
JUMP.AMBIENT.001: "Clean transit. The thread hums steadily beneath you. For a
moment, through the corridor walls, you glimpse stars that shouldn't be
visible from this angle."

JUMP.AMBIENT.002: "Smooth jump. Your module's power draw dips slightly in
transit — as if it's resting. It resumes normal draw on arrival."

JUMP.AMBIENT.003: "A {faction} cargo hauler passes you in the thread corridor,
running the opposite direction. You catch a glimpse of its hold — packed full.
Business as usual on this route."

JUMP.AMBIENT.004: "The thread corridor narrows briefly, then widens. Like
breathing. You've done this run a dozen times and never noticed that before."

JUMP.AMBIENT.005: "Mid-transit, the stars outside the corridor walls shift
from white to faint blue, then back. Doppler effect from thread velocity,
according to the manual. It doesn't explain why it's beautiful."

JUMP.AMBIENT.006: "You pass through a section of corridor where the walls
are thinner — you can see the void outside more clearly. For a moment, it
feels less like traveling through a tunnel and more like walking a tightrope."

JUMP.AMBIENT.007: "Transit complete. Total fuel consumed: exactly the predicted
amount, to four decimal places. The threads are remarkably consistent.
Remarkably."

JUMP.AMBIENT.008: "A brief moment of absolute silence mid-transit. Your
engines, your life support, even the hull stress — all quiet for 0.2 seconds.
Then everything resumes as if nothing happened."

JUMP.AMBIENT.009: "Your scanner picks up other ships in adjacent thread
corridors — parallel travelers, heading different directions. For a moment
you're all moving together, separated by nothing but the thread walls."

JUMP.AMBIENT.010: "Arrival. The thread deposits you precisely where it
promised. You've never questioned why the threads are so reliable. Maybe
you should start."
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
| FUEL | "TBA — thread-compatible energy medium, consumed by every vessel." |
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
             Enhanced: "TBA — reference to thread field harmonics research"

TECH.SENSORS_I: Current: "Improves scanner range"
                Enhanced: "TBA — reference to ancient sensor fragment recovery"

... (12 total)
```

---

## 8. First Officer Reactive Lines (System NOT Ready)

**ID:** `NARR.FIRST_OFFICER`
**System:** Needs FO dialogue system (toast or panel integration)
**System Ready:** NO — needs EPIC.S7.NARRATIVE_DELIVERY
**Volume:** 30 lines (10 per candidate x 10 milestone moments)
**Dependencies:** First Officer selection system, revelation triggers
**Priority:** CRITICAL — the FO is the player's emotional anchor for all five recontextualizations

### Candidate Profiles

- **Analyst** — Data-driven, precise. Speaks in observations and patterns. Trusts numbers until they betray her.
- **Veteran** — Experienced, cynical. Speaks from scars. Has seen systems fail before.
- **Pathfinder** — Curious, intuitive. Speaks in feelings and metaphors. Trusts instinct over instruments.

### Reactive Lines (Authored)

**Moment 1: Module Revelation (~hour 8)**

```
FO.ANALYST.MODULE: "I've been running the numbers on our module's power draw.
It doesn't match any known engineering curve. It's... learning."

FO.VETERAN.MODULE: "I've served on ships with experimental tech before. None
of them adjusted their own power consumption. This module is watching us."

FO.PATHFINDER.MODULE: "Do you feel that? When we jump, the module... hums
differently each time. Like it's tasting the thread."
```

**Moment 2: First Trade Pattern Noticed**

```
FO.ANALYST.PATTERN: "Three runs between Concord and Valorin space. The margins
are identical every time. That's not market behavior — that's architecture."

FO.VETERAN.PATTERN: "Twenty years in fleet service and I never noticed the
pattern. Same goods, same routes, same margins. It's too clean."

FO.PATHFINDER.PATTERN: "I plotted our routes on the galaxy map. They make a
shape. Has anyone else noticed they make a shape?"
```

**Moment 3: Pentagon Revelation (~hour 15)**

```
FO.ANALYST.PENTAGON: "I mapped every major trade route. They form a closed
loop. Five factions, five dependencies, zero redundancy. Someone designed this."

FO.VETERAN.PENTAGON: "Five factions, each dependent on the next. I've seen
supply chains weaponized before — but never this elegantly."

FO.PATHFINDER.PENTAGON: "It's a web. The whole economy is a web, and we've
been tracing its strands. Someone spun this. Someone with patience."
```

**Moment 4: Concord Revelation (~hour 12)**

```
FO.ANALYST.CONCORD: "The Concord's regulatory framework isn't maintaining
order — it's maintaining the loop. Every tariff, every restriction keeps the
pentagon intact."

FO.VETERAN.CONCORD: "The Concord aren't peacekeepers. They're zookeepers.
And we've been performing on schedule."

FO.PATHFINDER.CONCORD: "The Concord sits at the center of the web. Not
spinning it — just... making sure nobody pulls too hard on any one thread."
```

**Moment 5: Communion Revelation (~hour 18)**

```
FO.ANALYST.COMMUNION: "The Communion's research isn't about connection. It's
about the thing that connected everything before us. Before the threads."

FO.VETERAN.COMMUNION: "The Communion knows. They've always known. Everything
they do — the rituals, the research — it's preparation, not worship."

FO.PATHFINDER.COMMUNION: "The Communion isn't studying connection — they're
remembering it. Something was connected here, long before factions. Before us."
```

**Moment 6: First Instability Event**

```
FO.ANALYST.INSTABILITY: "Scanner variance just spiked to ±12%. That's not
instrument error — the measurements themselves are becoming unreliable."

FO.VETERAN.INSTABILITY: "That flicker in the nav system? I saw the same thing
once, years ago. My CO told me to ignore it. I should have listened harder."

FO.PATHFINDER.INSTABILITY: "The thread felt different that jump. Thinner.
Like crossing a bridge that's started to fray."
```

**Moment 7: Entering High-Instability Zone**

```
FO.ANALYST.HIGHINSTAB: "I can't give you accurate readings here. The data
contradicts itself. Prices, distances, fuel consumption — nothing is consistent."

FO.VETERAN.HIGHINSTAB: "Trust your instincts, not your instruments. I've
flown through worse than bad data."

FO.PATHFINDER.HIGHINSTAB: "I can hear it — not literally, but... the space
here is wrong. The measurements disagree because the space itself can't decide
what it is."
```

**Moment 8: Discovery of Ancient Site**

```
FO.ANALYST.ANCIENT: "These readings predate every faction by centuries.
Whatever was here... it understood the threads better than we do."

FO.VETERAN.ANCIENT: "Whoever built this didn't just use the threads — they
made them. And something went wrong."

FO.PATHFINDER.ANCIENT: "This place is old. Not ruins-old. Foundations-old.
Whatever happened here, it's why the threads exist at all."
```

**Moment 9: Player Reaches Max Reputation with Any Faction**

```
FO.ANALYST.MAXREP: "We're as deep inside {faction} operations as any outsider
has ever been. We see their supply chains. Their vulnerabilities."

FO.VETERAN.MAXREP: "They trust us now. That means we know enough to be
dangerous — to them, and to whatever they're protecting."

FO.PATHFINDER.MAXREP: "They've shown us their world. Their fears, their needs,
their secrets. Every faction is just people trying to hold something together."
```

**Moment 10: Endgame Approach**

```
FO.ANALYST.ENDGAME: "We have enough data to understand the whole system now.
The question isn't what's happening — it's what we do about it."

FO.VETERAN.ENDGAME: "I've watched empires make this choice. Preserve what
works, or tear it down and hope. There's no middle ground. Except... maybe
there is."

FO.PATHFINDER.ENDGAME: "We've seen the whole map now. Every thread, every
knot. The question isn't can we change it — it's should we. And I don't know
the answer."
```

---

## Summary

| Block | ID | Volume | Authored | System Ready | Priority |
|-------|-----|--------|----------|-------------|----------|
| Mission Scripts | NARR.MISSIONS | 30-50 | 4 | YES | HIGH |
| Discovery Text | NARR.DISCOVERY | ~45 | 45 | PARTIAL | HIGH |
| Faction Dialogue | NARR.FACTION_DIALOGUE | ~75 | 25 greetings | NO | MEDIUM |
| Jump Event Text | NARR.JUMP_EVENTS | ~25 | 22 | PARTIAL | MEDIUM |
| Risk Warnings | NARR.RISK_WARNINGS | 15 | 13 | NO | MEDIUM |
| Trade Good Descs | NARR.TRADE_GOODS | 13 | 0 | PARTIAL | LOW |
| Tech Descriptions | NARR.TECH_DESCS | 12 | 0 | YES | LOW |
| First Officer Lines | NARR.FIRST_OFFICER | 30 | 30 | NO | CRITICAL |
| **Total** | | **~255** | **~139** | | |
