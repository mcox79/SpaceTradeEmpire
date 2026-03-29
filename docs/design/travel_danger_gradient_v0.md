# Travel Danger Gradient v0 — Distance = Danger in Normal Space

**Design philosophy**: Traveling far from Haven should feel increasingly dangerous,
creating a constant tension between "safe-known-space" and "dangerous-unknown-space."
This is NOT the Deep Dread system (25+ hops, lattice fauna, void phases). This covers
the **normal star lane network** (0-10+ hops from Haven) where the first hour happens.

**Reference**: Starcom Nexus (escalating faction difficulty zones), Elite Dangerous
(security ratings per system), Sunless Skies (distance = resource risk), FTL (every
jump is a gamble).

---

## The Problem

Currently, every lane transit is identical: flat 150cr fee, no events, no consequences.
The galaxy feels like a flat parking lot. A system 1 hop from Haven feels exactly
like a system 8 hops away. There is no reason to fear exploration, no reason to value
safety, and no gradient that makes "pushing one more hop" feel like a real decision.

The audit data confirms this: backtrack rate is 96.5% because the only reason to travel
is trade margins. There's no danger that makes staying close appealing, and no reward
gradient that makes going far worthwhile despite risk.

---

## Design: Two-Layer Danger Model

### Layer 1: System Threat Level (what you find when you arrive)

Each star system has a **Threat Level** (0-5) derived from BFS hop count from the
player's Haven (or nearest friendly faction capital if Haven not yet discovered).

| Threat Level | Hop Range | What It Means |
|---|---|---|
| 0 (Safe) | 0-1 hops | Haven + immediate neighbors. Patrol presence. No pirate spawns. Lowest prices, well-arbitraged by NPCs. |
| 1 (Patrolled) | 2-3 hops | Regular patrol coverage. Rare pirate encounters (10% per visit). Moderate margins. |
| 2 (Frontier) | 4-5 hops | Patrol gaps. Pirates spawn regularly (30% per visit). Better trade margins. FO comments on reduced patrols. |
| 3 (Contested) | 6-7 hops | Active warfront proximity. Pirates + hostile faction patrols (50% encounter). High margins but high risk. Faction-specific dangers. |
| 4 (Wild) | 8-9 hops | No patrols. Guaranteed pirate presence. NPC pirates are higher tier (better ships, weapons). Best conventional trade margins. Repair stations rare. |
| 5 (Deep) | 10+ hops | Transition zone to Deep Dread. Information fog. Ancient hazards. Premium goods and discoveries. Gateway to fracture space. |

**Key design constraints**:
- Threat level is derived from **Haven hop count** (not fixed geography)
- If the player builds Haven closer to dangerous space, their safe zone SHIFTS
- Faction capitals also provide a "safe zone" effect within their territory
- This creates a dynamic frontier that moves with the player

### Layer 2: Lane Transit Events (what happens during travel)

The existing RiskSystem uses edge-distance (milli-AU) for lane events. This should
be **combined with** system threat level so that lanes connecting high-threat systems
are more dangerous:

| Transit Event | Trigger | Effect | Threat Threshold |
|---|---|---|---|
| **Clear passage** | Default at low threat | No event. Normal transit. | 0-1 |
| **Patrol scan** | Entering patrolled space with cargo | Brief delay (1-2 ticks). Faction identifies you. Heat if smuggling. | 1-2 |
| **Pirate interdiction** | Traveling between frontier+ systems | Mid-transit combat encounter. Must fight or pay tribute. | 2+ |
| **Debris field** | Random on long routes | Hull damage (5-15%) if no scanner module. Avoidable with tech. | 3+ |
| **Distress signal** | Random event | Choice: investigate (risk + reward) or ignore (guilt + FO comment). | 2+ |
| **Ambush** | High threat + high-value cargo | Multiple pirates. Harder combat. Better loot if survived. | 4+ |
| **Lattice instability** | Deep space lanes | Lane delay, fuel cost increase. Precursor to Deep Dread. | 5 |

**Key design constraints**:
- Lane events should be **telegraphed** before the player commits to transit
  (FO warning, galaxy map threat coloring, scanner intel)
- The player can always CHOOSE to take the dangerous route — agency over all
- Higher threat = higher reward (better margins, rarer goods, discoveries)
- NO time pressure — events are per-transit, not per-tick. No urgency to rush.

---

## NPC Difficulty Scaling

Currently all NPCs spawn with the same ship class regardless of location. This must
change:

| Threat Level | Pirate Ship Class | Pirate Count | Pirate Behavior |
|---|---|---|---|
| 0-1 | None (no pirates) | 0 | — |
| 2 | Scout (weak) | 1 | Flee if player has escort |
| 3 | Raider (medium) | 1-2 | Standard combat |
| 4 | Marauder (strong) | 2-3 | Pursue, coordinate, won't flee |
| 5 | Dreadnought (elite) | 1 (boss) | Territorial, drops rare loot |

**Faction fleets also scale**: Hostile faction patrols in contested space are
higher tier. Friendly faction patrols in safe space are lower tier (they don't
need to be strong — they're protecting, not hunting).

---

## The "Push" — Why Go Deeper?

Danger without reward is just punishment. Each threat level must offer escalating
reasons to push further:

| Threat Level | Reward Gradient |
|---|---|
| 0-1 | Lowest margins (5-15%). Safe. Boring. NPC traders have arbitraged everything. |
| 2-3 | Moderate margins (15-30%). First-visit bonuses. Occasional rare goods. |
| 4-5 | Best margins (30-50%+). Exclusive high-value goods. Discovery fragments. Module drops from elite pirates. |
| 5+ (Deep) | Fracture-only goods. Pentagon archives. Void research data. Unique ship modules. |

**Escalating trade goods by threat level**:
- TL 0-1: Common goods (metals, food, textiles) — low margin
- TL 2-3: Industrial goods (composites, electronics, fuel) — moderate margin
- TL 4-5: Luxury/strategic goods (rare minerals, weapons, xenotech) — high margin, risk premium

**Discovery seeding by threat level**:
- TL 0-1: No discoveries (safe = explored = known)
- TL 2-3: First anomaly chain fragments (breadcrumbs pulling player outward)
- TL 4-5: Rich discovery sites, research opportunities
- TL 5+: Deep discoveries requiring fracture drive

---

## Player Perception: How It Should Feel

### First hour (TL 0-2)
The player trades safely near Haven. FO occasionally mentions "reports of pirate
activity in the outer systems." Galaxy map shows color-coded threat zones. The player
sees high margins on the map in orange/red zones but knows it's risky. First
encounter with a TL 2 pirate is a wake-up call — combat has stakes now.

### Mid-game (TL 2-4)
The player has upgraded their ship (weapons, shields, scanner). They can now handle
TL 2-3 pirates and choose to push into frontier space for better margins. Each
new system is a genuine decision: "Do I have enough hull to make this trip? Do I
have a repair module? Is the margin worth the risk?"

### Late-game (TL 4-5+)
The player is an experienced trader-fighter. They venture into Wild space
deliberately, planning routes around repair stations, carrying escort modules,
equipped for the dangers. Deep space transitions into the Deep Dread system.

---

## FO Danger Commentary

The First Officer should react to threat level changes during travel:

| Trigger | FO Line Examples |
|---|---|
| Entering TL 2 | "We're past the patrols now. Keep your eyes open." |
| Entering TL 3 | "This is contested space. Faction patrols and pirates both operating here." |
| Entering TL 4 | "No patrols this far out. We're on our own." |
| Entering TL 5 | "I'm reading lattice instability. This is deep frontier." |
| Returning to TL 0-1 | "Back in safe space. I can breathe again." |
| Pirate encounter | "Contact! Hostile vessel on intercept course." |
| Post-combat (TL 3+) | "Good fight. But there'll be more where we're going." |
| High-value cargo in TL 3+ | "We're carrying a fortune through pirate territory. Stay sharp." |

---

## Galaxy Map Visualization

The galaxy map must communicate threat level at a glance:

- **Color gradient**: Green (TL 0) → Yellow (TL 1-2) → Orange (TL 3) → Red (TL 4) → Purple (TL 5)
- **Patrol coverage overlay**: Blue rings around Haven and faction capitals showing patrol radius
- **Pirate activity markers**: Skull icons at known pirate hotspots (player intel, not omniscient)
- **FO route assessment**: When hovering over a route, FO provides a 1-line danger assessment
- **"Here be dragons" effect**: Unvisited TL 4+ systems have a fog-of-war that's darker/more ominous

---

## Implementation Architecture

### SimCore Changes

1. **Node.ThreatLevel** (int 0-5): Computed on galaxy generation and updated when Haven moves.
   - `ComputeThreatLevel(state, nodeId)`: BFS from Haven, faction capitals, patrol bases
   - Stored in `SimState.Galaxy.Nodes[id].ThreatLevel` — deterministic, recomputed on Haven discovery

2. **NPC spawn scaling**: `FleetPopulationTweaksV0` gets threat-level-aware ship class selection.
   - `PickShipClassForThreatLevel(threatLevel, faction, role)`: Returns appropriate ship variant

3. **Lane event system**: Extend `RiskSystem` to factor in threat level of destination node.
   - `GetThreatAdjustedRiskBps(edgeId, destThreatLevel)`: Combines edge-distance risk with threat level
   - New event types: pirate interdiction, debris field, distress signal

4. **Trade margin scaling**: `MarketInitGen` seeds higher base margins at higher threat levels.
   - `MarketTweaksV0.ThreatLevelMarginBonusBps`: Per-TL margin bonus (e.g., 500 bps per level)

5. **Discovery placement**: `DiscoverySeedGen` uses threat level to gate discovery placement.
   - TL 0-1: No discoveries. TL 2-3: Common fragments. TL 4+: Rare/deep discoveries.

### Bridge + UI Changes

6. **SimBridge.GetNodeThreatLevelV0(nodeId)**: Returns threat level for galaxy map coloring.
7. **Galaxy map threat overlay**: Color-coded nodes + patrol coverage rings.
8. **FO threat-reactive dialogue**: New trigger `THREAT_LEVEL_CHANGE` in FirstOfficerSystem.
9. **Route danger preview**: Hover route shows aggregated threat assessment.

### Tweaks

10. **ThreatLevelTweaksV0**: All constants for the system.
    - `HopsPerThreatLevel = 2` (2 hops per level: 0-1=TL0, 2-3=TL1, etc.)
    - `PirateSpawnChanceBps` per TL: `[0, 0, 1000, 3000, 5000, 7000]`
    - `MarginBonusBps` per TL: `[0, 0, 500, 1000, 2000, 3500]`
    - `PatrolStrengthMultiplier` per TL: `[150, 100, 50, 25, 0, 0]`

---

## What This Is NOT

- **NOT Deep Dread**: That system handles 25+ hops, lattice fauna, void phases. This covers 0-10 hops.
- **NOT time pressure**: No urgency to rush. Events are per-transit, not per-tick. The player can sit at a station forever with zero penalty.
- **NOT punishing exploration**: The player is always REWARDED for pushing further. Danger is the COST of access, not a punishment for curiosity.
- **NOT random unfairness**: All danger is telegraphed (map colors, FO warnings, scanner intel). No invisible one-shot kills.
- **NOT a difficulty wall**: The player can always retreat to safe space. Equipment and knowledge reduce danger over time.

---

## Relationship to Existing Systems

| Existing System | How It Connects |
|---|---|
| RiskSystem (edge-distance) | Augmented with threat level. Both factors combine. |
| SecurityLaneSystem (heat) | Heat accumulation is faster in high-threat lanes. |
| InformationFogSystem | Already hop-count-based. Threat level aligns with fog distance bands. |
| WarfrontDemandSystem | Warfront proximity overlays with threat level (contested zones have both warfront danger and pirate danger). |
| Deep Dread | TL 5 is the transition zone. Deep Dread takes over at 25+ hops. |
| NpcFleetCombatSystem | NPC ship class now varies by threat level. |
| MarketSystem | Margins scale with threat level (risk premium). |
| DiscoverySeedGen | Discoveries gated by threat level (push outward to find them). |

---

## Open Questions (for user design review)

1. **Haven-relative vs fixed geography?** Recommended: Haven-relative (threat recalculates
   when Haven discovered/moved). This makes Haven placement strategic.

2. **Faction territory = safe?** Should friendly faction territory reduce threat level?
   Recommended: Yes, but only in systems with active faction patrol presence.

3. **Pirate interdiction during transit vs at arrival?** Recommended: At arrival (simpler,
   more visible). Mid-transit combat is technically complex and less player-visible.

4. **Threat level visible on galaxy map before visiting?** Recommended: Yes for adjacent
   systems (FO/scanner intel). Fog for distant unvisited systems (scanner upgrade reveals).

5. **Can threat level change dynamically?** Recommended: Slowly. Patrol presence drifts.
   Warfront movement shifts contested zones. Player actions (clearing pirates) temporarily
   reduce TL in a system.
