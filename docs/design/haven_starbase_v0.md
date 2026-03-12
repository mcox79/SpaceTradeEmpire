# Haven Starbase — Design V0

Status: DRAFT — Under Active Design
Date: 2026-03-11
Companion to: faction_equipment_and_research_v0.md, factions_and_lore_v0.md, ExplorationDiscovery.md, NarrativeDesign.md (First Officer)

---

## Core Fantasy

Haven is the player's HOME. After hours of being a guest in other factions' stations, scraping for rep, paying tariffs — Haven is YOURS. The emotional hook is ownership: "this is MY base." Upgradable, lived-in, narratively central, and the only place in the galaxy where you're not a visitor.

Haven is an ancient Accommodation starbase, stable for millions of years via accommodation geometry. It's proof that the Adaptation faction's approach WORKS — and it's the staging ground for the game's endgame paths.

**Design Philosophy — Home, Not Facility**: Haven should feel like coming home, not opening a management menu. The value is in the *people* who live there and the *evidence of your journey* on its walls — not in facility tiers on a tech tree. Every upgrade should change how Haven FEELS, not just what it DOES. Inspiration: BG3's camp (character moments at home), Dragon Age: Inquisition's Skyhold (physical space that reflects your choices).

---

## Discovery & First Dock

### How the Player Finds Haven

Haven is NOT marked on the galaxy map. It exists in a stable pocket off the main thread network — a Phase 0 zone surrounded by Phase 2+ instability. The player discovers it through one of these paths:

1. **Communion Breadcrumb** (most common): At Communion rep 50+, a Communion NPC says "We've seen the resonance signature before. Not all who follow it are lost." Adds a vague directional marker on the galaxy map — not coordinates, a DIRECTION. The player must explore Phase 2 space in that region to find the hidden one-way thread
2. **Fracture Exploration** (discovery): While exploring Phase 3 space, the player's fracture module detects an anomalous stability pocket. Scanner sweep reveals a hidden thread connection — one-way inbound
3. **Adaptation Fragment** (late path): Fragment #1 (Void Cartography) includes Haven's coordinates as inherited memory

### Haven Location

Fixed general region per save (quadrant of the galaxy), but exact system is randomized within that region. Players can plan routes roughly ("Haven is in the rimward-trailing sector") but must still explore to find it. Consistent with the "earned discovery" theme.

### First Dock Experience

The player arrives at a dormant station. No lights, no docking guidance, no UI. Just ancient geometry — accommodation architecture that looks ALIVE even unpowered. The starbase is enormous — far larger than any faction station. Built for a civilization, now empty.

First dock triggers:
- Letterbox cinematic (existing system — one of the reserved letterbox moments per NarrativeDesign.md)
- Music crossfades to a single sustained note, then Haven's unique ambient: warm, low, ancient hum
- Slow reveal of the station interior
- Power flickers on as the fracture module resonates with the station's accommodation geometry
- First Officer comment (variant-specific reaction):
  - Analyst: "The structural harmonics... these aren't random. This was *calculated*."
  - Veteran: "This is military-grade infrastructure. Pre-Concord military-grade. That shouldn't exist."
  - Pathfinder: "I've felt this before. At the shimmer boundaries. But here it's... intentional."
- Toast: "Haven — Accommodation Starbase"
- Station begins Tier 1 power-up sequence (automatic)

**Critical design note**: The player should feel AWE, not confusion. Haven's first dock is a revelation moment — this is ancient, this is real, and this is YOURS.

---

## The Accommodation Thread — Haven's Secret Connection

Haven connects to thread-space via an accommodation-geometry passage — NOT a containment thread. This is proof that threads CAN be built without containment (Concord's engineering division would kill for this data). The thread is invisible to the Lattice, which only monitors containment infrastructure.

**Why one-way?** Accommodation engineering shapes spacetime turbulence into stable flow patterns — and flow has a direction. The Haven thread is a shaped vortex: turbulence guided into a self-sustaining current that runs outbound from Haven to thread-space. Like a river, you can ride it one way without effort. Going upstream (inbound) requires actively shaping a counter-current — a harder trick that requires deeper mastery of accommodation geometry.

- **Tier 1-2**: One-way outbound only. Haven TO thread-space, not anywhere TO Haven. To return home, the player must fracture-travel back. This preserves the "do I push further or turn back?" tension — getting home costs fracture fuel and hull stress
- **Tier 3**: Bidirectional. The upgrade represents a qualitative leap: learning to create a counter-vortex. This is why it requires multiple adaptation fragments and significant resources — it's not a power increase, it's a breakthrough in understanding the physics
- **Late game**: The player can choose to **reveal the thread to faction allies** (see Tier 4 — Expanded). Permanent reputation boost, but faction NPCs can now visit Haven — trading exclusivity for alliance

This mechanic ensures Haven stays special: early game, getting home is an *investment* (fracture fuel, planning). The bidirectional thread at Tier 3 is one of Haven's most satisfying upgrades — the moment "going home" stops being costly.

---

## Station Layout

Haven is organized into wings. The layout evolves as tiers unlock — not just mechanically but visually.

```
                    ┌─────────────┐
                    │  RESONANCE  │ Tier 4
                    │   CHAMBER   │
                    └──────┬──────┘
           ┌───────────────┼───────────────┐
           │               │               │
     ┌─────┴─────┐  ┌─────┴─────┐  ┌─────┴─────┐
     │  RESEARCH  │  │   CORE    │  │  HANGAR   │ Tier 2-3
     │    LAB     │  │  (BRIDGE) │  │ & DRYDOCK │
     └─────┬─────┘  └─────┬─────┘  └─────┬─────┘
           │               │               │
     ┌─────┴─────┐  ┌─────┴─────┐  ┌─────┴─────┐
     │  CREW     │  │  MARKET   │  │  POWER    │ Tier 1
     │ QUARTERS  │  │  & HALL   │  │  SYSTEMS  │
     └───────────┘  └───────────┘  └───────────┘
```

**Crew Quarters** (formerly "Living Quarters"): Where the player's people live. The FO's room, the secondary crew quarters, and eventually Communion representative quarters. This wing should feel the most personal — evidence of habitation, personality, and history.

**Market & Hall**: The central gathering space. Market stalls, trophy wall (see Haven Reflects Your Journey), and the lore terminal alcove. This is where the player sees their journey reflected back at them.

---

## Haven Residents — Who Lives Here

### Design Philosophy

Haven works because of the PEOPLE there, not just the facilities. An empty station with a research lab is a facility. A station where your crew waits for you, where the Keeper greets you with light, where your Pathfinder has been mapping while you were away — that's HOME.

**Rule**: Haven should never feel empty after the first visit. Even at Tier 1, the Keeper is present. After secondary crew relocate, there are always at least 2-3 beings at Haven when the player docks.

### The Keeper — Ancient Accommodation Construct

Haven has a caretaker. Not AI, not organic — something in between. An accommodation-geometry construct that has been maintaining Haven for millions of years. The Keeper is not a character with dialogue trees — it's an *ambient presence* that communicates through the station itself.

**How the Keeper communicates**:
- **Tier 1**: Purely ambient. Lights brighten when the player brings exotic matter. Geometry shifts subtly when fragments are installed. Doors open before the player reaches them. The Keeper notices you but can't speak to you
- **Tier 2**: Faint patterns appear on walls near lore terminals — the Keeper guiding you toward data logs it thinks are relevant. When the player returns damaged (hull < 50%), the station geometry visibly wraps around the ship during repair — protective, almost parental
- **Tier 3**: The Keeper develops enough resonance with the fracture module to communicate in fragments of ancient data — not conversation, but impressions. Short text flashes: "...stable for 2.3 million cycles..." or "...they said they would return..." Not dialogue — memory leaking through
- **Tier 4+**: The Keeper can project simple visual displays (accommodation geometry schematics, ancient star charts). Still not conversational — it shows, it doesn't tell. The Keeper remembers the builders. It has been waiting

**Design rule**: The Keeper is NOT a quest-giver, NOT a shop, NOT an upgrade menu. It is an ambient emotional presence. The player should feel *cared for* at Haven without the Keeper ever asking for anything.

### Secondary Crew — The Unpromoted FO Candidates

Per `factions_and_lore_v0.md` → "The First Officer," three candidates serve as crew from tick 1. The player promotes one to First Officer (travels with the player). The other two remain as secondary crew.

**After Haven discovery, the secondary crew relocate to Haven.** They have their own reasons:

| Candidate | Why They Stay | What They Do at Haven |
|-----------|---------------|----------------------|
| **Analyst** | The accommodation geometry data is unprecedented. "The probability curves here don't match any known physics. I need more time with this." | Studies Haven's systems. Provides market intelligence when the player docks (faction price trends, demand shifts). At Tier 3+, begins correlating fragment data — occasionally offers insights about fragment combinations |
| **Veteran** | Haven's operational infrastructure needs someone competent. "Someone has to keep the power grid balanced. Might as well be me." | Manages station operations. Reports on Haven's sustain status, supply levels, and any events while the player was away. At Tier 3+, coordinates Communion trader visits |
| **Pathfinder** | The fracture signatures around Haven are unlike anything they've mapped. "The shimmer here is *old*. Older than anything I've charted." | Explores Haven's local system. Reports on instability patterns in adjacent space. At Tier 3+, provides fracture navigation intelligence for nearby Phase 2-3 zones |

**Dialogue evolution**: Secondary crew have Haven-specific dialogue that evolves based on:
1. **Player actions**: "You've been running a lot of Valorin trade routes. Watch the tariffs — they'll bleed you." (Analyst, after 10+ Valorin trades)
2. **Time at Haven**: "I found something in the lower decks. A room I hadn't noticed before." (Veteran, after 500+ ticks at Haven)
3. **Story progression**: "The pentagon. Five factions, five dependencies... and we're living in the proof that it was designed." (Analyst, post-revelation)
4. **Fragment installation**: Each fragment triggers a reaction from the crew member whose specialty it touches (navigation fragments → Pathfinder, structural fragments → Veteran, analytical fragments → Analyst)

**Dialogue budget**: ~15 lines per secondary crew member across the full game. Restraint is the instrument — when they speak, it matters because it's rare. Same principle as the First Officer's 30-line total budget.

### Communion Representative (Tier 3+)

Not just a trader — a character with a relationship arc. A Drifter who volunteered to study Haven after learning of its existence through the bidirectional thread.

- **Arrival**: At Tier 3 + Communion rep 50+. Arrives via the bidirectional thread (thread travel operates within the Lattice network regardless of surrounding instability phases — Communion doesn't need a fracture drive to use it)
- **Early relationship**: Fascinated by Haven's geometry. Offers exotic crystal trades and Communion perspective on data logs. "We have songs about places like this. I thought they were metaphors."
- **Mid relationship** (rep 75+): Shares fracture navigation guidance. Occasionally has requests: "Bring me a fragment from [sector] — I want to compare its resonance to Haven's geometry"
- **Late relationship**: Becomes an advocate for the Renegotiate path. Offers unique Communion insights about the instability's nature. Not pushy — observational. "The shimmer isn't noise. I've always known that. But here, I can almost hear what it's saying"

**Why Communion only**: Other factions don't know Haven exists. This is part of Haven's value — the one place in the galaxy no one can tax, claim, or threaten. The "reveal thread to allies" choice at Tier 4 is a deliberate sacrifice of this exclusivity.

---

## Coming Home — The Return Transition

Every Haven dock should feel emotionally distinct from docking at a faction station. This is the "coming home after a long day" moment.

### Dock Transition Sequence

1. **Audio**: Music crossfades to Haven's unique ambient theme — warm, low hum (per NarrativeDesign.md: "warm, safe, ancient — a unique soundscape"). Faction station audio cuts cleanly; Haven audio *fades in* during approach
2. **Camera**: Slower settle than standard dock (0.5s longer per NarrativeDesign.md). The camera lingers, letting the player absorb where they are
3. **Keeper greeting**: Station lights pulse gently — a slow wave from the docking bay inward. Visual "welcome home"
4. **Crew check-in**: If the player has been away 200+ ticks, a secondary crew member comments: "Welcome back. [Observation about what happened while you were away]"
5. **Post-danger return**: If hull < 50% when docking, the Keeper initiates slow repair and the station geometry visibly wraps around the ship. A secondary crew member reacts: Veteran ("Let's get you patched up"), Analyst ("Hull integrity at [X]%. That's... concerning"), Pathfinder ("You pushed too deep, didn't you?")

### First Return (After First Dock)

The *second* time the player visits Haven is almost as important as the first. The first visit is awe. The second visit is HOME — the moment the player realizes "I can always come back here."

- The Keeper remembers. Lights come on faster. Dock guidance activates (it didn't exist on first visit)
- If secondary crew have relocated, they're visible in the crew quarters — unpacking, settling in
- Toast: "Welcome home" (only on second visit — never repeated)

---

## Hangar System — Ship Storage

### Design Philosophy

Haven is the ONLY location where the player can own and store additional ships. This creates:
- **Build diversity**: Different ships for different missions (combat frigate vs. exploration clipper)
- **Strategic choice**: "Which ship do I take for THIS mission?"
- **Progression milestone**: Getting your second ship is a major moment

### Hangar Capacity

| Haven Tier | Hangar Bays | Purpose |
|------------|-------------|---------|
| 1 (Powered) | 1 (current ship only) | Docking only |
| 3 (Operational) | 2 (+1 stored ship) | Backup / specialist ship |
| 5 (Awakened) | 3 (+2 stored ships) | Small fleet |

**Why 3, not 8**: The emotional value peaks at ship #2 ("I have a backup!") and diminishes rapidly after. 3 ships is a meaningful choice ("combat ship, trade ship, or explorer?"). 8 ships is fleet management, which is a different game. Haven is HOME, not a fleet depot.

### Ship Management

**Stored Ships**:
- Parked at Haven. Cannot be used remotely
- Retain all installed modules and cargo
- No sustain cost while stored (systems in standby)
- Can be deployed by visiting Haven and swapping active ship

**Ship Swapping**:
- Dock at Haven → open Hangar panel
- See all stored ships with loadout summary
- "Deploy" a stored ship → current ship moves to storage
- Transfer cargo between ships via Hangar UI
- Transfer modules between ships (requires Drydock, Tier 3+)

**Ship Acquisition**:
- Buy from faction stations (existing system) — ship appears at purchase station
- Fly new ship to Haven for storage (or fly your main ship back to pick it up)
- Restore ancient hulls at Haven Drydock (appears in hangar when restoration complete)

**No separate fleet management tab, cargo warehouse, or module storage facility.** Modules stay on ships. Cargo stays on ships or in the market. Haven is simple — dock, swap, go.

### Ship Naming

Players can NAME their ships. Each ship gets a player-assigned name displayed in:
- Hangar bay label
- HUD ship status
- Save game summary

Default names follow faction variant conventions (e.g., "Fang-class Corvette" for unnamed Valorin variant). Player can rename at any time from Hangar.

---

## Haven Reflects Your Journey

Haven should look different for every player based on what they've DONE, not just what they've SPENT. This is what transforms a facility into a home.

### Trophy Wall (Market & Hall)

Key items from the player's adventures displayed in the central hall. These accumulate automatically — the player doesn't curate them, Haven collects evidence of their journey:

- **First discovery artifact**: The object from the player's first completed discovery site
- **Deepest phase marker**: A visual representation of the deepest instability phase the player has survived (Phase 2 shimmer sample → Phase 3 fracture crystal → Phase 4 void fragment)
- **Hardest combat salvage**: Debris from the toughest enemy the player has destroyed
- **Trade milestone**: A cargo manifest from the player's most profitable single trade
- **Fragment display**: Each installed Adaptation Fragment has a dedicated alcove

All of this data already exists in the game's systems — trophies are a *display layer* on existing progression tracking, not a new system.

### Fragment Geometry — Visual Personalization

Each Adaptation Fragment installed at Haven subtly changes Haven's visual geometry. The accommodation architecture *responds* to the fragment's nature:

| Fragment Category | Visual Effect |
|---|---|
| Navigation (Void Cartography, Current Reading, Depth Sensing) | Star charts glow on corridor walls. Navigation displays activate in the bridge wing |
| Structural (Lattice Echo, Resonance Mapping, Threshold Geometry) | Wall geometry shifts — hexagonal patterns appear, surfaces become smoother, structural supports reshape |
| Analytical (Pattern Recognition, Signal Archaeology, Metric Translation) | Data displays illuminate throughout the station. The Keeper's ambient communication becomes more complex |
| Communion (Shimmer Dialogue, Boundary Meditation, Harmonic Memory) | Bioluminescent accents intensify. Ambient hum gains harmonic overtones. The station feels more *alive* |

No two players' Havens look quite the same, because no two players find fragments in the same order or combination. Your Haven is a visual fingerprint of your exploration choices.

### Trade Evidence (Market & Hall)

The market area reflects the player's trading patterns:
- If you trade rare metals frequently, metal crates appear stacked near the market stalls
- If you specialize in exotic goods, the market area gains a crystalline aesthetic
- If you're primarily a combat player who rarely trades, the market stays sparse — but combat salvage appears near the hangar

This is cosmetic — ambient set dressing generated from existing trade/combat data.

---

## Upgrade Tiers

### Tier 1 — Powered (Automatic on first dock)

**What Happens**: The fracture module's resonance triggers Haven's dormant accommodation geometry. Emergency power initializes. Basic systems come online. The Keeper stirs — lights begin to follow the player.

**Unlocks**:
- Docking (safe harbor — no tariffs, no faction ownership)
- Basic market (buy/sell with Haven's tiny initial stock — exotic crystals only, seeded from ancient reserves)
- Fragment identification (can analyze fragments to learn what they do)
- Hangar bay 1 (current ship only)
- One-way outbound thread (return to thread network at a random adjacent system)
- Haven lore terminal (first 3 data logs available)
- The Keeper (ambient presence — lights, doors, geometry responses)

**Upgrade Cost**: FREE (automatic)
**Duration**: Instant

**Haven feel at Tier 1**: Dark, cavernous, mostly unpowered. Bioluminescent accents provide the only light beyond emergency systems. The Keeper's presence is subtle — doors opening, lights tracking. The player is alone here. It should feel like discovering an ancient tomb that *recognizes* you.

### Tier 2 — Inhabited

**What Happens**: Crew quarters pressurize. Research wing powers up. Secondary crew members relocate to Haven. The station transitions from "discovered ruin" to "inhabited outpost."

**Unlocks**:
- Research Lab (1 slot): Can research T3 utility modules
- Crew Quarters: Secondary crew members move in. Haven now has residents
- Market expansion: Basic goods trading (fuel, metal, organics — limited supply)
- Fragment Library: Visual catalog of all found fragments with identified effects
- Additional lore terminals (5 more logs)
- Keeper evolves: guides player toward relevant data logs via wall patterns

**Upgrade Cost**: 500 credits + 20 exotic matter + 10 composites + 10 electronics
**Duration**: 50 ticks
**Sustain**: 2 exotic matter per 100 ticks (station power draw)

**Haven feel at Tier 2**: Warm. Crew quarters have personal touches — the Analyst's probability charts on the wall, the Veteran's organized supply manifests, the Pathfinder's shimmer maps pinned everywhere. Haven feels *lived in* for the first time. The Keeper responds to residents: lights adjust to their routines.

### Tier 3 — Operational

**What Happens**: Drydock comes online. Bidirectional thread established. Communion representative arrives. Haven becomes a real base of operations.

**Unlocks**:
- Drydock: Refit ships with any owned module. Restore ancient ship hulls (Seeker, Bastion)
- Research Lab (2 slots): Can research T3 weapons and defense modules
- Bidirectional thread: Secret thread connection becomes two-way (counter-vortex breakthrough — can return to Haven from thread network)
- Hangar bay 2: Store 1 additional ship
- Module transfer: Move modules between ships at Drydock
- Market expansion: Full goods trading (all standard goods, moderate supply)
- Communion Representative arrives (if Communion rep 50+)
- Keeper evolves: communicates in fragments of ancient memory
- Deep-space scanner: Reveals all void site positions on galaxy map

**Upgrade Cost**: 1000 credits + 50 exotic matter + 20 rare_metals + 20 composites + 1 navigation fragment
**Duration**: 100 ticks
**Sustain**: 5 exotic matter per 100 ticks

**Haven feel at Tier 3**: Alive. The bidirectional thread means Haven is no longer isolated — it's connected. Communion NPCs dock occasionally. The Keeper's memory fragments add an eerie, beautiful layer. The crew have settled in — they have opinions about the data logs, about the fragments, about what the player is doing out there. The trophy wall is growing. Haven is HOME.

### Tier 4 — Expanded

**What Happens**: Full fabrication capability. Resonance chamber operational. The player can choose to reveal Haven's thread to faction allies.

**Unlocks**:
- Research Lab (3 slots): Can research ALL T3 modules
- Resonance Chamber: Combine fragment pairs for emergent effects
- Fabricator: Manufacture T3 modules (with appropriate fragments + exotic matter)
- Market expansion: Full trading including exotic goods
- Restore Threshold hull (Phase 4 ancient cruiser)
- **Reveal Thread choice**: The player can choose to reveal Haven's accommodation thread to one faction ally (permanent rep boost + that faction's NPCs can now visit Haven). This is a meaningful tradeoff — Haven's exclusivity vs. a powerful alliance. Once revealed, it cannot be undone

**Upgrade Cost**: 2000 credits + 100 exotic matter + 30 rare_metals + 30 electronics + 20 exotic_crystals + 1 structural fragment
**Duration**: 200 ticks
**Sustain**: 10 exotic matter per 100 ticks

**Haven feel at Tier 4**: Powerful. The Resonance Chamber is visually spectacular — crystalline geometry that shimmers with accommodation energy. The Keeper's displays show ancient star charts and builder schematics. If the player revealed the thread, faction NPCs add new voices to Haven's population. The station feels like a seat of power, not just a hideout.

### Tier 5 — Awakened

**What Happens**: Haven's own accommodation geometry begins actively adapting. The station is no longer dormant infrastructure — it's ALIVE in the way the ancient builders intended. This is the endgame.

**Unlocks**:
- Endgame-exclusive modules (unique to Awakened Haven)
- Hangar bay 3: Store 2 additional ships (small fleet)
- Haven becomes a strategic asset for endgame path:
  - **Reinforce**: Haven can project Lattice-compatible stabilization field (helps contain instability in adjacent systems)
  - **Naturalize**: Haven's accommodation geometry can be expanded outward (slowly naturalizes adjacent systems)
  - **Renegotiate**: Haven's communication arrays can reach into Phase 4+ space (Dialogue Protocol enabled)
- All sustain costs -25% (accommodation geometry self-optimizes)
- Haven generates small amounts of exotic matter passively (2 per 100 ticks)
- Haven generates trace exotic crystals passively (2 per 100 ticks)
- Keeper fully awakened: can project visual displays, share builder memories, and interact (still not conversational — it SHOWS, it doesn't TELL)

**Upgrade Cost**: 5000 credits + 200 exotic matter + 50 rare_metals + 50 electronics + 50 exotic_crystals + 10 salvaged_tech + 3 fragments (any category)
**Duration**: 500 ticks
**Sustain**: 8 exotic matter per 100 ticks (reduced from Tier 4 due to self-optimization)

**Haven feel at Tier 5**: Sacred. The accommodation geometry is active — walls reshape slowly, patterns emerge and dissolve, the station breathes. The Keeper is fully present. The crew are changed by living here — their dialogue reflects months of coexistence with ancient technology. Haven is no longer the player's outpost. It's the seed of something new.

---

## Haven Market

Haven's market is unique — it's not faction-controlled. No tariffs, no faction rep requirements. But it trades only in **ancient goods** — exotic crystals, exotic matter, salvaged tech, and basic survival commodities. Haven does NOT stock faction-produced goods (composites, electronics, munitions, etc.). The pentagon ring must remain intact as an economic pressure system.

**Critical design rule**: Haven supplements the faction economy; it does NOT replace it. A player cannot use Haven to bypass the pentagon dependency ring. If you need composites for your Concord modules, you still need Weaver trade relations.

### Market Evolution

| Tier | Goods Available | Supply Level | Notes |
|------|----------------|-------------|-------|
| 1 | Exotic crystals only | Very low (10 units) | Ancient reserves. Finite until resupplied |
| 2 | + Fuel, metal, organics | Low (20 each) | Extraction goods only. Life support + basic operations |
| 3 | + Exotic matter, salvaged tech | Moderate (30 each) | Ancient goods. Player can SELL anything to build stock |
| 4 | All Tier 3 goods + food | Good (50+ each) | NPC Communion traders bring food. Ancient goods expand |
| 5 | Tier 4 goods, self-replenishing | Excellent | Accommodation geometry generates trace exotic_crystals + exotic_matter (2 each per 100 ticks) |

**What Haven NEVER stocks** (regardless of tier): composites, electronics, munitions, rare_metals, exotic faction goods. These are faction-produced goods tied to the pentagon ring. Haven is ancient infrastructure, not a replacement economy.

**Player selling exception**: The player CAN sell any good at Haven (cargo → market inventory). But Haven has no demand for faction goods — sell price is 50% of baseline (terrible margin). This prevents Haven from becoming a trade hub that competes with faction stations.

### Building Haven's Economy

The player BUILDS Haven's market by:
1. Selling goods at Haven (transfers from cargo to market inventory — good prices for ancient goods, poor for faction goods)
2. NPC trade routes (at Tier 3+, Communion traders visit Haven with food and exotic crystals)
3. Fragment analysis sometimes yields small exotic matter deposits
4. Tier 5 passive generation (accommodation geometry produces ancient goods only)

This creates a satisfying feedback loop: the player invests in Haven for ancient goods and T3 research access, while faction stations remain essential for T2 equipment sustain. Haven is HOME; faction space is WORK.

---

## Narrative Integration

### Data Logs at Haven

Haven is the primary lore delivery location. Each tier unlock reveals new data log terminals:

| Tier | Logs Available | Content |
|------|---------------|---------|
| 1 | 3 logs | The builders. Who made this place and why. First glimpse of Accommodation philosophy |
| 2 | 5 logs | The schism. Containment vs Accommodation debate. Kesh and Vael's early arguments |
| 3 | 5 logs | The departure. Why the Accommodation faction left. What they found |
| 4 | 5 logs | The pentagon. How the ring was engineered. Senn's design documents |
| 5 | 2 logs | The message. What the builders wanted to say to whoever found this place |

Total: 20 logs across 5 tiers. Each log is a conversation between the five scientists (Kesh, Vael, Oruth, Senn, Tal) per NarrativeDesign.md.

**Crew reactions to logs**: When the player reads a new data log, secondary crew at Haven may comment on it next time the player docks. The Analyst finds statistical patterns across logs. The Veteran recognizes military/institutional language. The Pathfinder notices navigation references. These reactions make lore consumption feel less solitary — you're not reading alone, you're discussing with your crew.

### Haven as Emotional Anchor

Haven should evoke:
- **Ownership**: This is mine. Not rented, not taxed, not conditional on reputation
- **Belonging**: People live here. They know me. They care that I came back
- **Mystery**: Each upgrade reveals more about the ancient builders — and the Keeper remembers more
- **Investment**: I've poured resources into this base and I can see the results *and* my journey on its walls
- **Home**: A place to return to. Safe harbor in a dangerous galaxy. The one place where the ambient soundscape shifts from tension to safety
- **Purpose**: Haven isn't just convenient — it's the staging ground for the endgame

### Haven is INVIOLABLE

Haven cannot be attacked, raided, or threatened. This is a deliberate design choice.

The galaxy is dangerous. Faction space has tariffs. Fracture space has instability. Combat zones have hostiles. Haven is the ONE place where the player is safe. If Haven can be attacked, it stops being home and becomes another liability — another thing to defend, another source of stress.

The Lattice doesn't know Haven exists (accommodation thread is invisible to containment monitoring). Faction navies can't reach it (surrounded by Phase 2+ instability). Lattice Drones can't detect it (accommodation geometry doesn't trigger containment sensors). Haven's safety is a consequence of its nature, not plot armor.

**Exception**: Endgame path consequences may introduce tension around Haven (e.g., Naturalize expansion drawing attention), but this is a player-initiated choice, not an external threat.

---

## Visual Design Notes

Haven should look distinctly DIFFERENT from any faction station:
- Organic geometry (not blocky or industrial)
- Flowing curves that suggest the accommodation approach — working WITH spacetime, not against it
- Bioluminescent lighting (glows faintly even unpowered)
- Each tier upgrade adds visible geometric complexity (fractals of structure)
- Fragment installations visibly alter local geometry (see "Fragment Geometry" section)
- The Resonance Chamber (Tier 4) should be visually spectacular — crystalline geometry that shimmers
- Crew quarters should have personal touches unique to each resident (not generic rooms)

Color palette: Deep purple-blue with amber bioluminescent accents. NOT any faction's color. Haven is ancient, pre-faction.

### Visual Evolution by Tier

| Tier | Visual State |
|------|-------------|
| 1 | Dark, vast, mostly unpowered. Bioluminescent accents in corridors. Emergency lighting. Ancient and dormant |
| 2 | Warm lighting in crew quarters and research lab. Personal items visible. Market area illuminated. The inhabited section contrasts with the still-dark outer wings |
| 3 | Full lighting. Drydock operational glow. Communion visitor quarters lit. The station feels operational. Trophy wall growing. The Keeper's wall patterns visible |
| 4 | Resonance Chamber crystalline glow visible from exterior. Fragment geometry effects throughout. If faction thread revealed, new visual elements from that faction's aesthetic |
| 5 | The station breathes. Geometry actively reshapes. Bioluminescence pulses slowly. Accommodation energy visible as golden threads in the architecture. The Keeper's presence is everywhere |

---

## Resolved Questions

These were previously open. Decisions recorded here for design continuity:

| # | Question | Decision | Rationale |
|---|----------|----------|-----------|
| 1 | Haven Location | Fixed region per save, exact system randomized | Players can plan roughly but must explore. Consistent with earned discovery |
| 2 | NPC Visitors | Communion only (default). Faction ally access via Tier 4 "reveal thread" choice | Haven's exclusivity is part of its value. Sharing is a meaningful sacrifice |
| 3 | Haven Defense | Haven is INVIOLABLE. Cannot be attacked | Home must be safe. The galaxy provides all the danger the player needs |
| 4 | Save Game Housing | Max 3 stored ships (each a full Fleet entity with modules/cargo) | Manageable save complexity. 3 ships = meaningful choice, 8 = fleet management game |
| 5 | Return Mechanic | One-way Tier 1-2, bidirectional Tier 3. One-way creates valuable tension | The fracture fuel cost of "going home" is good gameplay. Bidirectional is an earned reward |
| 6 | Multi-ship | Ships stay at Haven. No multi-ship fleets — pick one, fly it | Fleet convoys are a different game. Haven provides ship CHOICE, not fleet COMMAND |
| 7 | Upgrade Cadence | Hybrid: resources AND specific fragments at Tier 3+ | Links Haven progression to exploration, not just grinding. Tier 3 requires 1 navigation fragment, Tier 4 requires 1 structural fragment, Tier 5 requires 3 any-category fragments |

---

## Cross-References

| Topic | Document | Section |
|-------|----------|---------|
| First Officer candidates (Analyst, Veteran, Pathfinder) | factions_and_lore_v0.md | "The First Officer — Player-Chosen Anchor Character" |
| Adaptation Fragments (12 total, 6 resonance pairs) | factions_and_lore_v0.md | "Adaptation Fragment Web" |
| Fragment → T3 module mapping | faction_equipment_and_research_v0.md | Part 4 |
| Haven ambient audio design | NarrativeDesign.md | Narrative enhancement table, Audio Design Notes |
| Haven camera behavior (slower settle, letterboxing) | NarrativeDesign.md | Camera/Transition table |
| Haven first visit as Silence Principle moment | NarrativeDesign.md | "Silence Principle" |
| Exotic matter income expectations | trade_goods_v0.md | "Exotic Matter Income Expectations" |
| Pentagon ring (what Haven must NOT replace) | factions_and_lore_v0.md | "The Pentagon Dependency Ring" |
| Module sustain (why faction stations stay essential) | faction_equipment_and_research_v0.md | Part 14 |
| Ancient ship hulls (Seeker, Bastion, Threshold) | ship_modules_v0.md | Hull classes |
| Endgame paths (Reinforce, Naturalize, Renegotiate) | NarrativeDesign.md | "Three Paths" |
| Original Haven aspirational design | factions_and_lore_v0.md | "The Haven — Player Hideout & Ancient Starbase" |
| Title screen Haven silhouette (post-completion) | NarrativeDesign.md | "Title Screen" |

---

## Changelog

- v0 (2026-03-11): Initial design — facility tiers, hangar system, market, lore integration
- v0.1 (2026-03-11): Major revision — Haven Residents (Keeper, secondary crew, Communion representative), Coming Home transition, Haven Reflects Your Journey (trophies, fragment geometry, trade evidence), hangar simplified (8→3 bays), accommodation thread physics, all 7 open questions resolved, inviolability rule, crew reactions to data logs, visual evolution table, cross-references, fragment-gated tier upgrades
