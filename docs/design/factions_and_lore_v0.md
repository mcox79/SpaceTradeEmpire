# Factions & Lore Design v0

**Status**: RESEARCH COMPLETE — ready for review
**Last updated**: 2026-03-07

---

## Implementation Status (as of Tranche 20, 2026-03-08)

- Faction identities & territories: ✅ Implemented (FactionTweaksV0.cs, GalaxyGenerator.cs)
- Pentagon dependency ring: ✅ Implemented (FactionTweaksV0.PentagonRing)
- Warfront seeding (2 wars): ✅ Implemented (GalaxyGenerator.SeedWarfrontsV0)
- Embargo system: ✅ Implemented (EmbargoState.cs, GalaxyGenerator.SeedEmbargoesV0)
- Instability phases (5-phase model): ✅ Implemented (InstabilitySystem.cs, InstabilityTweaksV0.cs)
- Reputation system: ✅ Implemented (ReputationSystem.cs — tiers, decay, war profiteering)
- Warfront demand & supply: ✅ Implemented (WarfrontDemandSystem.cs — supply ledger, intensity shift)
- Faction tariffs/aggression: ⚠️ REDESIGN NEEDED — code values diverged from original design (see below)
- Adaptation fragments (16): 🔮 Future — Not Yet Implemented
- Haven starbase: 🔮 Future — Not Yet Implemented
- Resonance pairs: 🔮 Future — Not Yet Implemented
- Endgame paths (Reinforce/Naturalize/Renegotiate): 🔮 Future — Not Yet Implemented
- Metric bleed gameplay effects: 🔮 Future — Not Yet Implemented
- Lattice drones: 🔮 Future — Not Yet Implemented

---

## Core Premise

Star lanes are not natural. They are **containment infrastructure** built by an ancient civilization to make interstellar civilization possible. Reliable measurement and reliable transit are the same problem — both require metric consistency across light-years. The lanes solve both by suppressing spacetime's natural turbulence within fixed corridors. Every faction's identity is shaped by their relationship to this infrastructure.

The player discovers a **fracture module** — Precursor technology that allows off-lane travel. No other faction has this capability. Using it has systemic consequences.

---

## What IS the Instability? — Metric Bleed

**The instability is the breakdown of consistent measurement.**

In stable space (within lane containment), one meter is one meter everywhere. One second is one second. Mass is mass. The lanes enforce metric consistency — they are a coordinate system imposed on spacetime.

In unstable space, metrics drift. Not randomly, but by *leaking into each other*. Distance and time become entangled. Mass and volume decouple.

**Signature manifestation**: Objects in unstable space change their apparent properties depending on how you measure them. A cargo hold that reads as 100 tonnes by mass sensor reads as 80 by volume scanner. A journey predicted at 12 hours takes 9 — or 16. An ore sample assays as iron-rich Tuesday and silicon-rich Thursday, not because it changed, but because the measurement relationship is no longer fixed.

### Why This Works for a Trading Game

The entire economy is built on reliable measurement. Trade requires buyer and seller to agree on what "one unit of metal" means. Insurance requires calculable risk. Navigation requires fuel cost predictable from distance. **Metric bleed attacks the preconditions of commerce, not commerce itself.** You can still trade in unstable space — but margins are uncertain, manifests are unreliable, and price discovery becomes adventure rather than arithmetic.

### Why Containment Makes Sense

The lanes are literally a coordinate grid. They impose a stable reference frame across spacetime — survey stakes driven into reality that define "here" and "now" consistently. Stable geometry between two points is inherently traversable geometry. The ancients didn't build roads and accidentally enable trade — they built infrastructure for civilization, which requires both consistent measurement and reliable transit. These aren't separate goals; they're the same physics.

### Visual Signature

**Parallax errors.** Objects in unstable space appear to shift position when you change viewing angle, as if existing at multiple distances simultaneously. The further instability has progressed, the more severe the parallax — at high levels, objects seem to smear across space as probability distributions. Stars appear to breathe. Station outlines double. Hull readings flicker.

### The Physics: Spacetime Foam Turbulence

Spacetime is not smooth. At the Planck scale (~10^-35 meters), it is a roiling foam of geometric fluctuations — virtual wormholes, topology changes, metric chaos. John Wheeler called it "spacetime foam" in 1957. At everyday scales this turbulence is invisible, the way ocean waves are invisible from orbit. But it is always there.

Within the lane network, foam-scale turbulence is actively suppressed — dampened to irrelevance by infrastructure that functions as **error correction on the geometry of space itself**. The lanes detect metric fluctuations and correct them, enforcing geometric coherence within each corridor. The physical substrate being stabilized is the shape of compactified extra dimensions at each point in space. When that shape drifts, local physics changes — particle masses shift, force strengths wander, measurement becomes unreliable. That drift is metric bleed.

Over interstellar distances without suppression, foam-scale noise amplifies. Microscopic geometric inconsistencies compound into macroscopic measurement breakdown — the metric bleed that makes off-lane space hostile to civilization. This amplification is not linear. Error-correcting systems have a **threshold**: below a certain error rate, they correct indefinitely; above it, they collapse suddenly. This is why lane degradation is gradual for centuries and then catastrophic overnight. The Lattice is approaching its threshold.

The lanes follow natural dark matter micro-filaments between star systems — gravitational channels in the cosmic web's local structure. The ancient builders didn't impose corridors on arbitrary space; they reinforced existing lines of gravitational coherence. This is why the lane network has the topology it does: the routes were already there in the dark matter scaffolding. The builders turned faint paths into highways.

**Two engineering responses to the same turbulence define the ancient schism:**
- **Containment**: Suppress the foam. Force metric consistency by brute-force error correction. Effective but expensive, requires constant maintenance, and fails catastrophically when the error rate exceeds the correction threshold. The lane network and the Lattice are containment engineering.
- **Accommodation**: Shape the foam. Instead of fighting turbulence, read its flow patterns and guide them into self-sustaining stable configurations — the way a riverbed shapes water into a predictable current without damming it. Cheaper, self-maintaining, but requires a deeper understanding of the underlying physics. The fracture module and the Haven are accommodation engineering.

---

## The Ancient Civilization

- Built the lane network and the Lattice maintenance system
- Experienced an internal schism: **Containment** (seal instability away) vs **Adaptation** (learn to coexist with it)
- Containment faction won. Adaptation faction's research was suppressed
- A single rebel/dissident ship carrying adaptation research was hidden — the player's fracture module comes from this lineage
- The civilization disappeared. The Lattice persists autonomously

### The Adaptation Faction's Argument

Metric bleed is not a disease. It is spacetime's natural state — the foam turbulence that exists everywhere, always. The lanes don't "cure" instability — they suppress it, the way a dam suppresses a river. The water doesn't go away. It builds up. The Containment approach works, but requires perpetual maintenance, and the pressure behind the dam never stops growing. Every lane built creates a larger eventual failure point.

The Adaptation faction's alternative: don't dam the river — *read the current and shape a channel*. Turbulence can be guided into self-sustaining stable patterns without being suppressed. A whirlpool sustains itself. An eddy persists for millennia. The Haven system is a calm eddy in the foam — not suppressed, just *shaped right* — and it has been stable for millions of years with no Lattice, no maintenance, no infrastructure. That's the proof of concept the Containment faction refused to acknowledge.

### What the Rebel Ship Carried: Accommodation Geometry

The rebel ship carried research into **accommodation geometry** — structural and material designs that function correctly under metric bleed rather than requiring stable metrics to operate. Ordinary engineering assumes a beam built one meter long stays one meter long. Accommodation geometry designs the beam so it functions as structural support *regardless of what "one meter" currently means in local spacetime*. Function decoupled from metric properties.

The fracture module is an accommodation geometry engine. It doesn't fight instability or suppress it. It allows the ship to move through metric-inconsistent space because its navigation depends on **topology** (what's connected to what) rather than **geometry** (how far apart things are).

### Why This Is Sympathetic

The adaptation faction weren't reckless. They were engineers who looked at the math and concluded containment was a losing long-term strategy. They were right — the Lattice is degrading, and the ancients' containment eventually failed (they disappeared). The rebel ship wasn't carrying a weapon. It was carrying a backup plan.

The tragedy: the Containment faction won the political argument, suppressed the research, and disappeared anyway. The fracture module lineage is the only surviving alternative to an approach that already failed once.

---

## Why the Fracture Module Can't Be Copied

The module was **grown, not built**. Its internal structure was cultivated in unstable space over centuries of controlled exposure to metric bleed. The material lattice adapted to metric inconsistency the way coral adapts to ocean currents — not by design, but by iterative response to environmental pressure.

Four reasons for irreproducibility:

1. **The substrate doesn't exist anymore.** The base material was engineered using fabrication techniques requiring stable metrics at extreme precision. Paradoxically, the material was designed to *operate* without stable metrics but *manufacturing* it required stability the ancients had but no modern faction can replicate.

2. **Growth conditions can't be replicated safely.** Growing accommodation geometry requires prolonged, controlled exposure to metric bleed — exposure that destroys conventional instruments and kills unprotected crews. The rebel ship was a cultivation vessel with life-support accommodation designs that survive nowhere in modern databases.

3. **The module has adapted to the specific player ship.** It continues its accommodation process, adapting its internal geometry to the hull it's installed on. Removing and reinstalling would require decades of re-accommodation.

4. **Measurement defeats itself.** The module works by being metric-invariant. Any instrument precise enough to analyze its structure introduces a stable reference frame that *changes the thing being measured*. A macroscale uncertainty principle: you can know what the module does, or how it's built, but not both — because precise measurement imposes the metric consistency the module is designed to be independent of.

---

## The Lattice

- **Not a faction** — a distributed maintenance system
- Autonomous nodes that monitor and repair lane infrastructure
- No political agenda, no territory, no diplomacy
- Factions interact with it (Concord protects it, Chitin scavenges from it, etc.)
- Its effectiveness is degrading over time — this is the ticking clock

### Lattice Drones as Escalating Threat

> 🔮 **FUTURE — NOT YET IMPLEMENTED**. This section describes aspirational design for future tranches.

As the Lattice degrades, its maintenance drones malfunction. They were repair
bots; now they attack anything that moves in deteriorating lane segments.

- **Phase 0-1 (Stable/Shimmer)**: Drones are passive. Visible at Lattice
  nodes, repairing infrastructure. Concord patrols avoid disturbing them.
- **Phase 2 (Drift)**: Drones become territorial. They defend Lattice nodes
  aggressively — attacking ships that approach too closely. Not hostile on
  sight, but dangerous if you stray near maintenance infrastructure.
- **Phase 3 (Fracture)**: Drones are fully hostile. They patrol lane segments
  adjacent to failing nodes, attacking any ship. Their numbers increase as
  more nodes degrade. They cannot be reasoned with, bribed, or intimidated.
- **Phase 4 (Void)**: Drones are absent. Whatever they were maintaining no
  longer exists here.

**Why this works**: The Lattice is not a new faction bolted onto the threat
model. It is the existing infrastructure developing teeth as it fails. The
ticking clock is not just economic (lane degradation → trade disruption) — it
is physical (the repair bots that kept you safe are now trying to kill you).
Drone density scales with instability, creating a natural difficulty gradient
that the player can see and plan around.

**Gameplay interaction**: The Adaptation Fragment "Lattice Reading" (Fragment
6) allows the player to interact with Lattice nodes for temporary
stabilization — calming drones in the area. This creates a unique tool for
navigating degraded space that no faction possesses.

---

## Factions (5 + Lattice)

### Concord (Order)
- **Role**: Preserve the existing lane infrastructure and political order
- **Trade Policy**: Open | **Tariff**: 0.08 (low, flat) | **Aggression**: 1 (defensive)
- **What they want**: Stability, predictability, functioning trade
- **What they're wrong about**: The lanes can be maintained indefinitely
- **Endgame alignment**: Reinforce

**The Secret**: Concord knows the lanes are failing and has known for decades. Internal intelligence reports show Lattice degradation curves predicting cascading lane failure within a century. They have suppressed this information because publishing it would cause the panic and territorial scramble that would accelerate collapse. Their public position ("the lanes are eternal") is a conscious, deliberate lie told for what they believe are the best possible reasons.

Three layers:
1. **Public face**: Benevolent regulators. Open trade. Fair tariffs. The adults in the room.
2. **Private reality**: Intelligence apparatus that monitors and suppresses information about lane degradation. They bribe Lattice researchers, classify fracture data, maintain "stability" through information control. An engineering division actively works to reverse-engineer Precursor technology and build human-designed replacement infrastructure.
3. **Genuine belief**: They think they are buying time. Every year of stability is a year of research toward an engineering solution.

**The blindspot**: They've been buying time so long that buying-time became the strategy. They have no plan for what happens if the engineering solution doesn't arrive. Their engineering division is specifically **techno-optimist in a setting where that optimism is dangerous** — they see the player's fracture module and think "mass production," not "sacred artifact" or "existential risk."

**Player experience**: Concord is the best place to trade early game — open markets, low tariffs, predictable pricing. As reputation grows, the player gets intelligence briefings revealing degradation data. At max rep, the player is recruited into maintaining the fiction.

**Personality traits**: Obsessive record-keeping. Politeness that is actually surveillance. Genuine hospitality (most comfortable stations in the game). Undercurrent of paranoia about lane stability questions. Bureaucratic euphemisms ("lane optimization events" = lane failures).

**Gameplay niche**:
- **Produces**: Food (stable, subsidized), Components (precision manufacturing — civilizational specialty), Munitions
- **Needs**: Composites (from Weavers — can't armor fleet without them)
- **Ships**: Balanced cruisers/frigates. Higher BaseShield. Highest SlotCount for weight class — the ship is a platform; modules make it what you need. Coast guard with engineering flexibility
- **Tech focus**: Sensor, Shield, and Utility modules. Unique: "Regulatory Transponder" — zero tariffs and best prices at Concord stations, but position trackable by Concord patrols. Transparency for advantage. Secondary unique: "Universal Mount Adapter" — converts one weapon slot to utility or vice versa (engineering heritage)
- **Max rep unlock**: Concord patrol escorts for convoys. Intelligence briefings reveal all lane degradation locations. *And the truth about the suppression program.* Engineering division partnership — submit Salvaged Tech and Exotic Matter for analysis, yielding unique cross-faction module blueprints (slow: 100+ ticks). Engineering analysis of fracture module reduces hull stress from fracture travel

### Chitin Syndicates (Adaptation)
- **Species**: Beetle-like, metamorphic lifecycle
- **Trade Policy**: Guarded | **Tariff**: 0.12 (variable — fluctuates every 50 ticks) | **Aggression**: 0 (peaceful)
- **What they want**: Optionality — never be locked into one system
- **What they're wrong about**: You can always hedge your way out of existential risk
- **Endgame alignment**: Naturalize (pragmatically)

**Biology -> Philosophy**: Holometabolous insects undergo complete metamorphosis — during the pupal stage, the organism *literally dissolves its own body* and reconstructs from imaginal discs. The pre-pupal individual is a different being than the post-pupal one. Every Chitin has personally experienced total self-dissolution and reconstitution. Their concept of "self" is a probability distribution across possible selves they could have become. Gambling isn't vice — **making bets is how you test your model of reality**. A bet forces you to quantify uncertainty. Anyone who refuses to bet has never honestly confronted uncertainty.

The elytra principle extends to economics: maintain a hard shell of guaranteed survival around volatile core capabilities. Never go all-in (the elytra protect), never play entirely safe (flight wings under elytra need freedom).

Chemical communication -> syndicate structure: information flows through market signals, not commands. No central authority tells a Chitin trader what to do; they read the market the way ancestors read the wind.

Molt cycle drinking: they metabolize ethanol-analogues to stabilize neurochemistry during metamorphic transitions. Their bars are **metabolic support infrastructure** that doubles as social space. Offering a drink = offering trust.

**Gameplay niche**:
- **Produces**: Electronics (fracture-border processing hubs), Munitions
- **Needs**: Rare Metals (from Valorin frontier territories)
- **Ships**: Fast clippers/corvettes. Highest ScanRange. Low armor, moderate shields. "See everything, commit to nothing"
- **Tech focus**: Scanner and Engine modules. Unique: "Probability Engine" — 15% chance each tick of consuming zero fuel for travel. At max rep, also scrambles cargo manifest data during inspections
- **Max rep unlock**: Real-time price data for all Chitin stations. Chitin probability models for lane degradation. Access to futures contracts (pre-purchase goods at today's price for delivery in 100 ticks)

### Weavers (Structure)
- **Species**: Spider-like, silk-based engineering
- **Trade Policy**: Guarded | **Tariff**: 0.15 (high, non-negotiable) | **Aggression**: 1 (defensive)
- **What they want**: To be the ones who build whatever comes next
- **What they're wrong about**: Structure is neutral — the builders should decide what gets built
- **Endgame alignment**: Reinforce or Renegotiate (depends on who's paying)

**Biology -> Philosophy**: Orb-weavers produce up to seven types of silk with distinct mechanical properties. They sense the world through vibration — every strand is a sensor. Ambush predators whose survival strategy is *building the right structure and waiting*.

Weavers think in tensions, not positions. A web is a stress diagram — every node exists because of forces acting on it. They see the lane network as a stress management system where every lane bears load that would otherwise destabilize the region. When a Weaver says "I should build this," they mean "I can feel the stress pattern and I know where the load-bearing points are."

Vibration sensing = sensing changes in lane network traffic patterns, load fluctuations, maintenance drift through almost proprioceptive awareness. They don't just use infrastructure; they feel it. Deeply uncomfortable when anyone introduces forces the network wasn't designed to handle.

Patience of ambush predation -> trade policy. They don't chase opportunities. They build structures and wait for opportunities to come. "Guarded" not because hostile, but because they've positioned themselves at chokepoints. You trade through their infrastructure, on their terms.

Seven types of silk = seven types of infrastructure service: shipyards, lane repair, station construction, orbital habitats, cargo handling, communications relays, defensive installations.

**Gameplay niche**:
- **Produces**: Composites (silk-based manufacturing, best in known space), Metal
- **Needs**: Electronics (from Chitin — sensor-web maintenance systems)
- **Ships**: Tanky haulers/cruisers. Highest BaseZoneArmor, lowest speed. Mobile stations, not nimble fighters
- **Tech focus**: Armor and Hull modules. Unique: "Silk Lattice Reinforcement" — when armor on any zone reaches 0, automatically redistributes 10 armor from highest-armor zone. Structure adapts load
- **Max rep unlock**: Weaver Drydock Access — commission Weaver-built hull variants (+2 module slots, +30% armor, -15% speed). Contracts to repair Lattice nodes (direct interaction with lane degradation mechanic)

### Valorin Clans (Expansion)
- **Species**: Small-bodied, rodent-like (hamster adjacent), neurologically fearless
- **Trade Policy**: Open | **Tariff**: 0.03 (minimal) | **Aggression**: 2 (hostile to strangers, loyal to friends)
- **What they want**: New space, new worlds, room to grow
- **What they're wrong about**: There's always more frontier — expansion solves everything
- **Endgame alignment**: Naturalize

**Biology -> Philosophy**: Rodents are ~40% of all mammal species. Success from: rapid reproduction, burrowing (microclimates independent of surface), high metabolism, whisker-based spatial awareness in darkness, hoarding.

Fearlessness is not bravery — it's a **neurological adaptation to living in darkness**. Ancestors navigated pitch-black burrow networks via whiskers. They evolved without seeing threats before they arrived. Their amygdala-equivalent processes threat without producing paralysis. They feel danger, they don't feel dread.

Other factions look at off-lane space and see cosmic darkness full of unknown threats. Valorin see a burrow they haven't mapped yet. Not ideology — perceptual. They literally do not process "unknown space" as threatening.

Rapid reproduction -> clan structure and frontier settlement. A clan that settles has a viable population within a generation. Not reckless — a species-level strategy that works because individual failure rate is acceptable with many simultaneous attempts.

Hoarding -> supply chain philosophy. Distributed caches, not centralized warehouses. Multiple small stores, never one big one. Resilient to disruption but "messy" by Concord standards.

High metabolism -> economic pace. Need to eat more, move more, trade more. Highest goods turnover. Most willing to run marginal routes.

**Military threat — the swarm**: Valorin aggression 2 does not mean strong ships. It means **many ships**. Individual Valorin corvettes are the weakest in the game. But they appear in groups of 8-12. Entering Valorin territory without Blood-Kin status triggers a swarm response — one corvette is nothing; twelve appearing on your sensors is terrifying. Zerglings, not Ultralisks. The player can kill one. The player cannot kill twelve.

**Gameplay niche**:
- **Produces**: Rare Metals (settle where no one else will), Ore (raw extraction)
- **Needs**: Exotic Crystals (from Drifter Communion — frontier sensor calibration for increasingly unstable expansion space)
- **Ships**: Cheap shuttles/corvettes. Lowest individual stats but highest NPC density (3-4x other factions). They swarm
- **Tech focus**: Engine and Cargo modules. Unique: "Cache Beacon" — establish hidden cargo caches at any node, invisible to other faction scans. Hoarding instinct as technology
- **Max rep unlock**: Clan Blood-Kin status — Valorin share frontier maps (extended visibility), NPC fleets become escorts, access to distributed cache network across frontier space

### Drifter Communion (Understanding)
- **Species**: Human
- **Trade Policy**: Open (but chaotic — inventory unpredictable) | **Tariff**: 0.02 (nearly zero) | **Aggression**: 0 (peaceful)
- **What they want**: Direct, embodied knowledge of what instability actually is. Not through sensors — through exposure
- **What they're wrong about**: Individual experience scales to civilizational wisdom. A pilot who "feels" the drift is not a theory of drift
- **Endgame alignment**: Renegotiate (the player's natural ally for the third path)

Anti-institutional in a setting full of institutions. The only faction that actively seeks out instability. Their "spirituality" is the insistence that embodied experience is a valid epistemology. Think Le Guin's Anarresti or The Expanse's Belters — people whose identity comes from living in the margins.

**Shimmer-zone edge-dwellers**: The Communion lives in Phase 1 (Shimmer) space — the boundary between stable and unstable. They harvest phase-locked crystals from shimmer-space asteroid fields, wade ankle-deep into instability, and interpret sensor jitter as profound experience. They do NOT have fracture capability. They are edge-dwellers, not divers. The player is the one who dives.

This sharpens the contrast: the Communion's "spirituality" comes from wading in the shallows and finding it transformative. When the player arrives with fracture capability and descriptions of Phase 3-4 space, the Communion is simultaneously awed and terrified — the player has gone deeper than any Communion pilot has ever been or could survive.

**Gameplay niche**:
- **Produces**: Exotic Crystals (harvested from shimmer-zone asteroid fields at the boundary of stable space), Salvaged Tech (entire economy is recovery and reuse)
- **Needs**: Food and Fuel (from Concord — can't sustain themselves without lane-space supply lines)
- **Ships**: Scout shuttles/clippers. Highest ScanRange, lowest Mass, minimal cargo/armor. Eyes, not fists
- **Tech focus**: Scanner and Navigation modules. Unique: "Metric Harmonics Array" — fracture travel fuel -25%, reveals additional void site details
- **Max rep unlock**: Communion Pathfinder status — Drifter navigators join crew (reduced hull stress, hazard warnings during fracture travel). Most critically: observations about patterns in instability — first data pointing toward Renegotiate path

---

## Faction Relationships — Pentagon Dependency

### Primary Ring

| Faction | Needs from | Good type |
|---------|-----------|-----------|
| Concord | Weavers | Composites (infrastructure materials) |
| Weavers | Chitin | Electronics (sensor-web maintenance) |
| Chitin | Valorin | Rare Metals (frontier resources) |
| Valorin | Drifter Communion | Exotic Crystals (frontier sensor calibration) |
| Drifter Communion | Concord | Food, Fuel (baseline survival) |

Every faction depends on someone they philosophically disagree with.

### Secondary Cross-Links (Web)

The primary ring is the backbone. Secondary needs create cross-links that add
arbitrage depth and prevent single-link fragility:

| Faction | Primary Need (ring) | Secondary Need (web) |
|---------|-------------------|---------------------|
| Concord | Composites (Weavers) | Rare Metals (Valorin — military hardware) |
| Weavers | Electronics (Chitin) | Exotic Crystals (Communion — lattice resonance tuning) |
| Chitin | Rare Metals (Valorin) | Composites (Weavers — hull materials for clipper fleet) |
| Valorin | Exotic Crystals (Communion) | Components (Concord — precision engineering) |
| Communion | Food, Fuel (Concord) | Metal (Weavers — station hull patching) |

The player learns the ring first through early trade. Cross-links emerge as
the player visits more stations and sees secondary buy orders with smaller
volumes but viable margins.

**Reputation tension mechanic**: When you trade with Faction A at a contested node, their rival in the dependency chain loses 1 reputation. Pure trading slowly raises everyone, which is too neutral — cross-faction tension creates meaningful choices.

---

## Instability Phases (Per-Node Integer, 0-100+)

> ✅ **IMPLEMENTED** in InstabilityTweaksV0.cs and InstabilitySystem.cs.
> Phase thresholds: Stable 0-24, Shimmer 25-49, Drift 50-74, Fracture 75-99, Void 100+.
> Gain: BaseGainPerTick * warfront intensity at contested nodes. Decay: 1 per 100 ticks.
> Note: Phase effects (price jitter ±5%, lane delay +20%, trade failure 10%, market closure)
> are defined in tweaks but not yet mechanically applied to MarketSystem/LaneFlowSystem.

### Phase 0: Stable (0-24)
*"Normal space. The lanes hold."*
- All instruments accurate. Standard everything. No special mechanics.

### Phase 1: Shimmer (25-49)
*"Your instruments disagree with each other, but only sometimes."*
- **Sensor jitter**: Scan results +/-10% error. Trade good assay values flicker
- **Price flutter**: Market prices update with +/-5% random offsets each tick
- **Visual**: Subtle chromatic aberration on distant objects. Faint skybox shimmer
- **Opportunity**: Phase-locked crystals harvestable from shimmer-space asteroid fields. This is where Drifter Communion stations operate
- **Narrative**: "Space weather." Concord issues advisories. Valorin ignore it. Chitin price it in

### Phase 2: Drift (50-74)
*"Distance lies. Your nav computer says 12 hours. It takes 9. Or 16."*
- **Travel time variance**: Fracture travel takes 0.7x-1.3x predicted time (deterministic by seed)
- **Cargo metric shift**: Goods purchased in drift space have a **fixed fracture weight ratio** per good type. When brought to stable space, actual quantity differs from what you paid for. Each good's ratio is discoverable through experience or scanning — once known, it becomes reliable arbitrage. Ore tends to measure heavy (buy cheap, sell more). Electronics tend to measure light (buy at a loss, avoid). **This is a knowledge puzzle, not random noise** — the player invests effort to learn which goods are profitable in drift space
- **Lattice interference**: Nearby lane connections have reduced throughput
- **Lattice Drones**: Territorial around maintenance nodes. Dangerous if provoked
- **Visual**: Objects at slightly wrong positions. Parallax noticeable. Distant stars doubled. Low resonant audio hum
- **Opportunity**: Null-mass alloy deposits accessible. Metric arbitrage becomes a viable trading strategy for players who've learned the weight ratios
- **Narrative**: Fracture module becomes genuinely useful. Valorin send scout clans. Chitin price the variance into their models

### Phase 3: Fracture (75-99)
*"Space is broken here. The rules work differently."*
- **Topology shift**: Connections between points can change. Routes may not exist between visits. Local map unreliable — must re-scan
- **Fracture refinery**: The player can choose to expose specific cargo to instability at designated sites, with **discoverable transmutation recipes**. "Ore + Phase 3 instability = chance of Exotic Crystal." Risk/reward the player opts into — not random silent mutation. Recipes are found through experimentation and ancient data logs. Wrong combinations destroy cargo. Right combinations produce rare materials unavailable through any other means
- **Accommodation requirement**: Ships without accommodation hull modifications take hull damage per tick. Fracture module protects drive; hull needs separate protection (gated behind tech progression)
- **Lattice Drones**: Fully hostile. Patrol failing lane segments, attack any ship
- **Visual**: Heavy spatial distortion. Objects stretch/compress. Color shifts ultraviolet. Sounds arrive at wrong times
- **Opportunity**: Ancient accommodation-geometry ruins (T3 components, dreadnought fragments, research data cores). Highest-value trade goods in the game. Fracture refinery recipes
- **Narrative**: Endgame exploration territory. Only the player and malfunctioning Lattice drones operate here

### Phase 4: Void (100+)
*"This is not space anymore."*
- **No conventional navigation**: Requires fully accommodation-geometry hull with Metric Drive Core (T3 engine). Navigate by topological connection only — no coordinates, no map, no distance
- **Visual distortion only**: Screen edge warping, color shifts, ship hull appears to refract, stars in wrong positions, UI frame jitter, audio dissonance. **The instruments still read true** — the ship's sensors are the player's last tether to comprehensible reality. The alienness is conveyed through atmosphere, not through lying to the player about gameplay-critical numbers
- **Void sites only**: AnomalyRifts — not ruins or natural formations, but points where spacetime's organizational principle is fundamentally different
- **Lattice Drones**: Absent. Whatever they were maintaining no longer exists here
- **Opportunity**: The endgame revelation. What instability actually is. The three endgame paths each interpret what's found here differently

Phase boundaries are **hard snaps, not gradual fades**. When instability crosses from 49 to 50, drift mechanics engage immediately. Creates meaningful spatial boundaries on the galaxy map. **Transitions are announced**: rising instability triggers visual/audio warnings and UI alerts ("Instability rising: Shimmer threshold in ~50 ticks") so the player can prepare. The snap is dramatic but never a surprise.

---

## Unstable Space Economy (Three Layers)

### Layer 1: Substrate Materials (consumable, ongoing demand)
Unstable space preserves pre-containment material states. Lanes crystallized matter into safe, predictable forms. Off-lane systems have pockets of original-state material — more energetic, more useful, more dangerous.

- **Uncontained Alloys** (`exotic_matter`): Required input for T3 module fabrication. Off-lane void sites are the renewable source; anomaly encounters are the discovery source
- **Resonance Crystals** (`exotic_crystals`): Required for accommodation geometry progression. Found in Phase 1+ systems (harvested by Drifter Communion in shimmer zones)
- **Salvaged Tech**: Ancient derelicts in off-lane space are higher-value because containment infrastructure recycles wreckage in lane space, but unstable zones preserve ancient wrecks indefinitely

**Key hook**: T3 module sustain costs consume exotic matter and exotic crystals every 60 ticks. A Dreadnought with T3 loadout needs 6-8 exotic matter per cycle. Fracture travel to off-lane void sites is the T3 supply line.

### Layer 2: Metric Arbitrage (Phase 2+ trading)

> 🔮 **FUTURE — NOT YET IMPLEMENTED**. This section describes aspirational design for future tranches.

In Phase 2 (Drift) space, cargo metric shift means goods have fixed fracture weight ratios — each good type consistently measures heavier or lighter when brought to stable space. Players learn these ratios through experience or scanning, creating a knowledge-based trading strategy. This is a unique opportunity that only the player can exploit — no NPC faction has fracture capability to reach these markets.

Maps to existing `FractureSystem.FracturePricingV0` with 1.5x volatility and 2x spread.

### Layer 3: Adaptation Fragments (exploration rewards)
Off-lane void sites are the primary source of adaptation research fragments. The Adaptation faction's experimental sites were in regions of natural instability. The Containment faction couldn't destroy what was already outside containment.

Three reasons to return repeatedly: (1) sustain materials for T3 equipment, (2) metric arbitrage margins, (3) research fragments unlocking new capabilities.

---

## Fracture Module Revelation Arc

### Phase 1: "Experimental Drive" (Hours 3-12)
**Cover story**: Prototype drive from a defunct research group. Found in a derelict. Works by "resonating with micro-fractures in the lane lattice" to create temporary shortcuts. Abandoned because dangerous and damages infrastructure.

UI text: "Structural Resonance Engine," "Lattice Microfracture Drive." Player treats TechLevel progression as "drive calibration." Accurate description of observations, completely wrong about underlying mechanism.

Note: The player has already spent Hours 0-3 as a pure lane trader. They know
what normal space feels like. The fracture module's strangeness has a baseline
to contrast against.

### Phase 2: "That's Not How Resonance Works" (Hours 12-22)
Clues accumulate through the discovery system:
- Anomaly encounters yield data logs from ancient facilities. Dates are wrong. Technology is too advanced for a "defunct corporation"
- Off-lane instability zones follow geometric patterns, not random distribution. Map reveals spokes radiating from hubs
- Fracture module activates sensors the player didn't know they had near certain void sites
- Faction reactions shift: Concord stops saying "that drive is illegal" and starts saying "where did you *get* that?" Chitin offer to buy scans of the module itself

At TechLevel 2, discoveries start including `ADAPTATION_FRAGMENT` items. First one is ambiguously labeled. Second references the first using terminology matching no known faction.

### Phase 3: "It's Not a Drive" (Hours 22-30+)
Paradigm shift: the fracture module predates the lanes. It's Adaptation faction technology — designed not to break containment but to operate where containment doesn't exist. The lanes aren't protecting you from instability; the module is adapting you to it.

Convergence triggers:
1. Research fragment includes a schematic of the player's own module — millions of years old
2. Module's Trace signature matches patterns in the oldest Adaptation ruins
3. A Lattice maintenance node responds to the module not with alarm but with *recognition* — using authentication protocols the Lattice was designed to accept

What changes: Module UI updates to true name. FractureTier becomes "adaptation depth" not "drive calibration." New VoidSiteFamily types become visible. Five systemic effects are recontextualized — the module isn't damaging lanes, it's de-containing space, which weakens nearby lanes as side effect.

---

## The Haven — Player Hideout & Ancient Starbase

> 🔮 **FUTURE — NOT YET IMPLEMENTED**. This section describes aspirational design for future tranches.

### Discovery

Near where the player first finds the fracture module, there's a system one short fracture jump away — close enough that even the untested drive can reach it safely. This is the player's first fracture destination: a **low-instability pocket** (Phase 0-1) that shouldn't exist off the lane network.

The reason it's stable: the Adaptation faction built a **local stabilizer** here using accommodation geometry, not containment. It doesn't suppress instability — it *shapes* it into a calm eddy. This is proof-of-concept that the Adaptation approach works. The system has been sitting here, self-maintaining, for millions of years.

### What's There

- **An ancient starbase** — Adaptation faction staging outpost, dormant but intact. Recognizes the fracture module's authentication signature and powers up when the player docks
- **A secret star lane** — not a containment lane but an accommodation-geometry passage. **One-way outbound only**: Haven TO lane-space, not anywhere TO Haven. To return home, the player must fracture-travel back. This preserves the Haven as a staging base and launching pad while keeping fracture exploration tense — the "do I push further or turn back?" question is never trivialized by a free teleport home. Late-game upgrade (Tier 3-4 starbase) unlocks bidirectional travel, rewarding investment
- **A small system** — a dim star, 1-2 rocky bodies, an asteroid field. Unimpressive to look at. The value is entirely in the starbase and what you build

### Starbase Upgrades (Resource Sink / Progression)

The starbase starts minimal — a powered-up dock with basic repair. The player invests resources (trade goods, exotic matter, adaptation fragments) to unlock tiers:

| Tier | Investment | Unlocks |
|------|-----------|---------|
| **0 — Powered** | (automatic on first dock) | Basic repair, cargo storage, save point |
| **1 — Restored** | Metal, Components, credits | Full repair bay, module swap station, personal cargo vault (unlimited storage) |
| **2 — Operational** | Composites, Electronics, Exotic Crystals | Research lab (study adaptation fragments for lore/bonuses), basic shipyard (refit existing ship) |
| **3 — Expanded** | Rare Metals, Exotic Matter, multiple fragments | Advanced shipyard (build accommodation-geometry hull variants), trade depot (set buy/sell orders that NPC Drifter Communion traders fill over time). **Secret lane becomes bidirectional** |
| **4 — Awakened** | Late-game fragments + high resource cost | Accommodation fabrication (craft T3 modules here instead of finding them), resonance pulse emitter (affect instability in adjacent systems from home base), deep-space scanner (reveals all void sites in the galaxy) |

### Why This Works

- **Early game anchor**: The player has a safe place to stash cargo and repair from Hour 1 of fracture exploration. No more limping back to Concord space after every off-lane trip
- **Progression sink**: Resources have a destination beyond "sell for credits." Every trade good has a use case in starbase upgrades
- **Lore delivery**: The starbase is where the player learns about the Adaptation faction. Upgrading the research lab unlocks lore entries. The starbase's own existence is the strongest argument for the Adaptation philosophy — it's been stable for millions of years without containment
- **Endgame relevance**: The Tier 4 starbase becomes a strategic asset in all three endgame paths — Reinforce (hand it to Concord/Weavers as proof accommodation works), Naturalize (expand it as the seed of a new civilization), Renegotiate (use it as the base for final void expeditions)
- **Player identity**: "This is MY base" — the emotional hook that other faction stations don't provide. You built this. It's yours

### The Secret Lane

The accommodation-geometry lane is important because:
- It proves lanes CAN be built without containment (Concord's engineering division would kill for this data)
- It's invisible to the Lattice (which only monitors containment infrastructure)
- One-way outbound initially — the player can deploy from Haven but must fracture-travel home. Bidirectional at Tier 3
- Other factions can't follow you here unless you share the route
- Late game: the player can choose to reveal the lane to faction allies (permanent reputation boost + faction gets access to Haven system)

**Why one-way?** Accommodation engineering shapes spacetime turbulence into stable flow patterns — and flow has a direction. The Haven lane is a shaped vortex: turbulence guided into a self-sustaining current that runs outbound from Haven to lane-space. Like a river, you can ride it one way without effort. Going upstream (inbound) requires actively shaping a counter-current — a harder trick that requires deeper mastery of accommodation geometry. The Tier 3 bidirectional upgrade represents exactly this breakthrough: learning to create a counter-vortex. This is why it requires multiple adaptation fragments and significant resources — it's not a power increase, it's a qualitative leap in understanding the physics.

---

## Adaptation Fragment Web (16 Fragments, 8 Resonance Pairs)

> 🔮 **FUTURE — NOT YET IMPLEMENTED**. This section describes aspirational design for future tranches.

Each fragment is independently useful. When two fragments in a resonance pair are both found, a combined effect activates that's more than the sum of parts. No linear progression — discovery in any order.

### Navigation Fragments (found in off-lane void sites)
1. **Void Cartography** — Reveals void site positions within 2 systems. (Solo: fracture fuel -20%)
2. **Current Reading** — Shows instability flow patterns. (Solo: fracture speed +30%)
3. **Depth Sensing** — Detects resource deposits in unstable space. (Solo: survey markers give exact estimates)
4. **Wake Analysis** — Reads ancient fracture traces. (Solo: reveals hidden AnomalyRift sites)

### Material Fragments (found in ancient ruins/derelicts)
5. **Substrate Shaping** — Work with uncontained alloys. (Solo: T3 sustain costs -25%)
6. **Lattice Reading** — Decode Lattice protocols. (Solo: interact with Lattice nodes for temporary stabilization — calms malfunctioning drones in the area)
7. **Resonance Tuning** — Calibrate equipment for unstable space. (Solo: no sensor degradation in unstable space)
8. **Phase Tolerance** — Hull adapts to instability. (Solo: fracture hull stress eliminated)

### Structural Fragments (found in Accommodation Geometry sites)
9. **Geometric Suspension** — Reduce instability interaction. (Solo: +20% zone armor in unstable space)
10. **Harmonic Insulation** — Cancel instability effects on shields. (Solo: full shield capacity in unstable space)
11. **Adaptive Plating** — Hull reconfigures to conditions. (Solo: slow hull regen in unstable space)
12. **Distributed Load** — Spread instability stress across hull. (Solo: zone damage distributed across all zones)

### Communication Fragments (found in deepest/most dangerous sites)
13. **Signal Isolation** — Filter signals from noise. (Solo: detect other ships in unstable space)
14. **Pattern Recognition** — Identify repeating structures. (Solo: reveals instability has patterns, not just chaos)
15. **Frequency Matching** — Produce signals instability responds to. (Solo: calm local instability temporarily)
16. **Dialogue Protocol** — Framework for structured interaction. (Solo: opens Renegotiate endgame path)

### Resonance Pairs

> 🔮 **FUTURE — NOT YET IMPLEMENTED**. This section describes aspirational design for future tranches.

| Fragment A | Fragment B | Combined Effect |
|---|---|---|
| 1 Void Cartography | 3 Depth Sensing | Full galaxy void-site map with resource estimates |
| 2 Current Reading | 4 Wake Analysis | Follow ancient ship routes to undiscovered sites |
| 5 Substrate Shaping | 7 Resonance Tuning | Fabricate T3 modules at the Haven |
| 6 Lattice Reading | 8 Phase Tolerance | Travel through Lattice internal network (fast travel) |
| 9 Geometric Suspension | 10 Harmonic Insulation | Build permanent outposts in unstable space |
| 11 Adaptive Plating | 12 Distributed Load | Ship becomes instability-tolerant (no negative effects) |
| 13 Signal Isolation | 14 Pattern Recognition | Predict instability events before they happen |
| 15 Frequency Matching | 16 Dialogue Protocol | Initiate Renegotiate endgame sequence |

Players don't see a checklist of 16 blanks. Fragments are discovered through exploration, initially named opaquely ("Artifact XV-7"). Resonance pair completion triggers automatically with fanfare. Different players with different fragment sets have very different play experiences.

---

## Three Endgame Paths (Emergent, Not Chosen)

> 🔮 **FUTURE — NOT YET IMPLEMENTED**. This section describes aspirational design for future tranches.

### How Endgame Emerges

Three accumulation vectors tracked throughout play:

**Vector 1: Faction Reputation Balance**
- Concord rep > 50 AND Weaver rep > 50 -> Strong Reinforce alignment
- Valorin rep > 50 AND Chitin rep > 50 -> Strong Naturalize alignment
- Communion rep > 50 AND mixed others -> Opens Renegotiate

**Vector 2: Fracture Usage Intensity**
- Low total Trace -> Lane-economy player -> Favors Reinforce
- High total Trace -> Off-lane life -> Favors Naturalize
- High Trace + Communication Fragments -> Tried to understand -> Renegotiate signal

**Vector 3: Research Fragment Portfolio**
- Reinforce requires: Lattice Reading (6)
- Naturalize requires: Phase Tolerance (8) + Geometric Suspension (9)
- Renegotiate requires: Dialogue Protocol (16) — rarest fragment, deepest site

### Reinforce
**What the player has been doing**: Running stable lane routes, supplying Concord with Composites, commissioning Weaver repairs.
**The crisis demand**: Lattice needs cascading repair operations across multiple systems simultaneously. Only a player with deep Concord/Weaver ties has the intelligence data and engineering contracts to coordinate.
**Endgame activity**: A series of supply-chain missions — deliver Composites to Lattice Node Alpha, Components to Node Beta. The most important trade routes of your career.
**What it costs**: Sealing off-lane space. Permanently losing fracture travel. Exotic Crystal supply chain dies. Drifter Communion loses their way of life.

### Naturalize
**What the player has been doing**: Running frontier routes, supplying Valorin with Exotic Crystals, trading Chitin speculative markets, building cache networks.
**The crisis demand**: When lanes fail catastrophically, the player's alternative network is the only functioning trade system.
**Endgame activity**: Expand frontier network to rescue lane-dependent populations. Deliver Food to starving Concord stations whose supply lines died. Establish new off-lane routes.
**What it costs**: Accepting permanent instability. Lane-space becomes more dangerous. Concord collapses. Lattice shuts down permanently.

### Renegotiate
**What the player has been doing**: Extensive fracture exploration, Exotic Crystal/Matter trading, Communion relationship-building, void site surveying.
**The crisis demand**: Neither Reinforce nor Naturalize works permanently. Only understanding what instability actually is can end the cycle.
**Endgame activity**: Final fracture expeditions to specific void sites. Use accumulated data, engineering analysis, and fracture module to map instability's structure. Final discovery: instability is not entropy or decay — it is *process*. The containment wasn't just suppressing physics; it was interrupting something.
**What it costs**: Abandoning both lane system and adaptation approach. Every other faction thinks this is insane. Only the Communion supports it.

**The game does NOT resolve** whether instability is alive, conscious, intelligent, or just physics. It resolves whether the player is willing to act as though it matters — which is a richer question than any definitive answer.

### The New Vegas Principle

The endgame is not "choose your ending." By the time the crisis hits, the player has already made their choice through hundreds of trade decisions. If strongly aligned with Reinforce factions but high Trace and communication fragments, the game creates genuine tension — economic relationships pull one way, exploration knowledge pulls another. The "I tried to please everyone" player faces the hardest endgame: no faction fully backs them and they haven't committed to understanding instability.

---

## Existing Mechanical Framework

| System | How factions differ |
|--------|-------------------|
| **FactionReputation** (-100 to 100) | Rep unlocks tech, ships, preferred pricing |
| **TradePolicy** (Open/Guarded/Closed) | Affects dock access and trade availability |
| **TariffRate** (0.0-1.0) | Different tariff structures per faction |
| **AggressionLevel** (0/1/2) | NPC patrol behavior, combat frequency |
| **PreferredGoods** | Each faction has distinct supply/demand curves |
| **Tech trees** (via reputation gating) | Unique modules/upgrades per faction |
| **Ship classes** | Unique ship designs per faction |

---

## Summary Table

> **⚠️ REDESIGN PENDING**: Tariff rates and aggression levels in both this document
> and FactionTweaksV0.cs need holistic revision to align with species/philosophy
> lore. See EPIC.S7.FACTION_IDENTITY_REDESIGN.V0. Until redesign completes,
> code values (FactionTweaksV0.cs) are authoritative for gameplay.
>
> Current code values: Concord 5%, Chitin 15%, Weavers 8%, Valorin 20%, Communion 3%.
> Aggression: Concord 0, Chitin 1, Weavers 0, Valorin 2, Communion 0.

| Faction | Policy | Tariff | Aggr | Produces | Needs | Ships | Tech | Path |
|---------|--------|--------|------|----------|-------|-------|------|------|
| Concord | Open | 0.08 | 1 | Food, Components, Munitions | Composites | Balanced cruisers (shield+slots) | Sensors, Shields, Utility | Reinforce |
| Chitin | Guarded | 0.12* | 0 | Electronics, Munitions | Rare Metals | Fast clippers (scan) | Scanners, Engines | Naturalize |
| Weavers | Guarded | 0.15 | 1 | Composites, Metal | Electronics | Tanky haulers (armor) | Armor, Hull | Reinforce |
| Valorin | Open | 0.03 | 2 | Rare Metals, Ore | Exotic Crystals | Cheap swarm (numbers, 3-4x density) | Engines, Cargo | Naturalize |
| Communion | Open | 0.02 | 0 | Exotic Crystals, Salvage | Food, Fuel | Scout clippers (scan) | Scanners, Nav | Renegotiate |

*Chitin tariff fluctuates every 50 ticks.

---

## Warfront Seeding

> See also: `dynamic_tension_v0.md` — Pillar 1 (The Galaxy Starts at War)

> ✅ **IMPLEMENTED** in GalaxyGenerator.SeedWarfrontsV0.
> Hot war: Valorin vs Weavers at OpenWar (intensity 3).
> Cold war: Concord vs Chitin at Tension (intensity 1).
> Contested nodes: BFS depth 1 from faction borders.

The galaxy does not begin at peace. The procedural generator seeds active
conflicts as part of world creation. The player spawns into a galaxy already
shaped by war.

### Natural Fault Lines

Faction philosophies and the dependency ring create two tiers of conflict
probability:

| Tier | Pair | Conflict Driver | War Character |
|------|------|----------------|---------------|
| **Hot war** | Valorin vs Weavers | Territorial: Valorin expand into Weaver chokepoints. Weavers block frontier access to protect infrastructure. Aggression 2 vs 1 | Shooting war. Lane disruptions. Refugee flows. Ore/Composites and Rare Metals/Exotic Crystals supply chains under direct pressure |
| **Cold war** | Concord vs Chitin | Informational: Concord suppresses lane degradation data. Chitin price and trade that data. Concord's surveillance state vs Chitin's information markets | Espionage, trade restrictions, inspection harassment. Chitin tariffs spike. Concord closes ports to Chitin-flagged vessels |

The Drifter Communion is too weak and peaceful to initiate conflict. They are
collateral damage — their supply lines break when the factions they depend on
go to war.

### Seed Rules

1. **Every seed has exactly one hot war at tick 0.** The generator places the
   Valorin-Weaver contested border within 2-3 hops of the player's starting
   system. The player's first trade decisions are shaped by this conflict.

2. **Every seed has one simmering conflict.** The Concord-Chitin cold war
   begins at elevated tension (not yet shooting). This conflict escalates to
   open hostility between tick 200-600 based on seed-deterministic triggers.
   The player sees it coming and can position accordingly.

3. **The player's starting system belongs to a non-combatant faction.** You
   start in Concord or Chitin space — close enough to the front to feel
   prices, far enough to not get shot immediately. This gives the player a
   safe-ish base while the warfront is 1-2 hops away with its inflated
   margins.

4. **Warfront location determines which dependency chain links are stressed.**
   A Valorin-Weaver war disrupts Rare Metals (Chitin starves) and Composites
   (Concord starves). A Concord-Chitin cold war disrupts Food/Components
   (Communion and Valorin starve) and Electronics (Weavers starve). The
   generator picks the war; the pentagon dependency determines the economic
   blast radius.

### Cascade Example: Valorin-Weaver War

```
Valorin territory contested
  -> Rare Metals production drops 40%
    -> Chitin can't sustain Electronics output
      -> Weavers lose sensor-web maintenance capability
        -> Composites quality degrades
          -> Concord fleet armor weakens
            -> Concord border patrols thin out
              -> Piracy rises in Concord space
                -> Player's safe trade routes become less safe
```

The player feels this war through **six degrees of economic separation**. They
never see a Valorin fleet fire on a Weaver station. They see Rare Metal prices
triple at Chitin markets, and their Composites trade route margins collapse.

### Warfront Evolution

Wars are not static. Seed-deterministic event chains drive warfront shifts:

- **Tick 0-200**: Initial hot war. One simmering conflict. Player learns the
  economic landscape.
- **Tick 200-600**: Simmering conflict escalates. Second front opens. Supply
  chains that survived the first war now face cross-pressure.
- **Tick 600-1200**: Ceasefire possible on first front (temporary relief).
  The dependency ring is now stressed at multiple points. Lattice drones
  become territorial in degraded sectors.
- **Tick 1200+**: Warfronts interact with Fracture threat. Lattice
  degradation weakens lanes near combat zones. Factions blame each other.
  Instability phases advance faster in war-damaged regions. Lattice drones
  become fully hostile in Phase 3 sectors.

Ceasefires create breathing room — the player can rebuild disrupted supply
chains — but they don't last. New conflicts open as old ones cool. The galaxy
is never fully at peace and never fully at war.

---

## Wartime Economics

> See also: `dynamic_tension_v0.md` — Pillars 3 and 4

### Tariff Scaling

Base tariffs in the Summary Table are **peacetime floors**. During active
conflict, tariffs scale with warfront intensity:

```
Effective Tariff = BaseTariff + (WarSurcharge * WarfrontIntensity)
```

| Warfront Intensity | War Surcharge | Example: Concord (base 0.08) |
|--------------------|---------------|------------------------------|
| 0 — Peace | 0.00 | 0.08 |
| 1 — Tension | 0.03 | 0.11 |
| 2 — Skirmish | 0.07 | 0.15 |
| 3 — Open War | 0.12 | 0.20 |
| 4 — Total War | 0.20 | 0.28 |

Warfront intensity is per-system, not per-faction. A Concord system on the
front line charges 0.20. A Concord system in the deep interior charges 0.08.
The player sees the surcharge broken out in UI: "Tariff: 20% (base 8% + war
surcharge 12%)."

### Wartime Demand Shocks

When a faction is actively fighting, it consumes goods at elevated rates. This
creates scarcity that ripples through connected markets:

| Good | Peacetime Demand | Wartime Demand | Effect |
|------|-----------------|----------------|--------|
| Munitions | 1x | 3-5x | Price spike at faction stations. Shortages downstream |
| Composites | 1x | 2-3x | Armor repair and fleet maintenance. Weavers can't keep up |
| Fuel | 1x | 2-4x | Military logistics burn fuel. Civilian routes face fuel shortages |
| Metal | 1x | 1.5-2x | Ship repair, ammunition casings. Mild but persistent pressure |
| Food | 1x | 1.5x | Refugee populations, garrison supply. Concord stations especially |
| Components | 1x | 2x | Equipment replacement, sensor repair. Concord bottleneck |

These multipliers apply to NPC faction consumption, not player prices directly.
The player sees the effect as **market prices rising** at stations near the
front — buy prices climb because the station's inventory is being consumed
faster than NPCs can resupply it.

**The player opportunity**: Wartime demand shocks are the source of the best
margins in the game. Running Munitions to a front-line station during open war
can yield 200-400% markup. But the route passes through contested space with
hostile patrols, inspections, and potential interdiction.

### The Neutrality Tax

Factions tolerate neutral traders during peacetime. During war, neutrality
becomes suspicious — and expensive.

| Warfront Intensity | Neutral Trader Experience |
|--------------------|--------------------------|
| 0 — Peace | Full access. No questions asked |
| 1 — Tension | Occasional patrol scans. "Papers, please" flavor text. No gameplay effect |
| 2 — Skirmish | Inspection frequency 2x. Cargo scanned for contraband. +5% tariff surcharge for unaligned traders |
| 3 — Open War | Inspection frequency 4x. Cargo scan + reputation check. +10% neutrality surcharge. Faction-exclusive goods restricted (cannot buy military-grade modules) |
| 4 — Total War | Mandatory allegiance declaration to dock. Unaligned traders denied port access at front-line stations. Interior stations still accessible but at +15% surcharge. Faction offers "emergency trade contract" — commit now for Allied pricing, or lose access |

**The shrinking middle**: At Intensity 0-1, neutrality is free. At Intensity
2-3, neutrality costs money but preserves access to all factions. At Intensity
4, neutrality becomes nearly unplayable in the war zone — the player must
either commit to a side or retreat to interior systems with thin margins.

### Exclusive Supply Contracts

Starting at Warfront Intensity 2, factions offer the player exclusive supply
contracts:

```
CONCORD CONTRACT OFFER
"Deliver Munitions exclusively to Concord stations for the duration of
hostilities. In exchange: Allied pricing (-15% buy, +15% sell), patrol
escort on Concord routes, access to military-grade Shield modules."

PENALTY: Selling Munitions to any non-Concord station while under contract
triggers immediate reputation drop to Unfriendly (-30). Contract voided.
Concord patrol response becomes hostile for 200 ticks.

[Accept]  [Decline — no penalty]  [Counter-offer: 100-tick term limit]
```

Key design rules for contracts:
- **Declining has no penalty.** The player is never punished for saying no.
  Pressure comes from the escalating neutrality tax, not from contract
  refusal.
- **Contracts are time-limited** (100-500 ticks). The player is not
  permanently locked in. But breaking a contract is catastrophic for
  reputation.
- **Counter-offers are possible.** The player can negotiate term length,
  which goods are covered, and which stations count. This is a trade
  negotiation, not a binary choice.
- **Both sides of a war offer contracts.** The player can see both offers
  and choose which (if either) to accept. Accepting one automatically
  declines the other.
- **Contracts stack with reputation.** Contract performance builds reputation
  faster than normal trading. A 200-tick contract completed successfully is
  worth +15-20 reputation — equivalent to hundreds of individual trades.

### War Profiteering

The player CAN supply both sides of a conflict — but it's risky:

- Selling to Faction A's enemy while under no contract: reputation with A
  drops by 1 per transaction (slow bleed, manageable).
- Selling to Faction A's enemy while under contract with A: contract breach,
  reputation crashes, patrol response hostile. Catastrophic.
- Selling to both sides with no contracts: viable but increasingly difficult
  as inspections intensify. Cargo scan reveals goods purchased at enemy
  stations. The player can mitigate with Chitin "Probability Engine" (cargo
  manifest scrambling at max rep), or by laundering goods through neutral
  third-party stations.

War profiteering is not forbidden — it's a high-risk, high-reward playstyle
that maps naturally to the Chitin/smuggler identity.

---

## Drifter Communion Early-Game Vulnerability

> See also: `dynamic_tension_v0.md` — Pillar 5 (Fracture Temptation)

The Communion depends on Concord for Food and Fuel — the most basic survival
goods. When the galaxy starts at war and Concord is involved (even indirectly
through cascade effects), the Communion's supply line is immediately at risk.

### Why This Is Good for Gameplay

The Communion is the player's natural ally for the Renegotiate endgame path —
the most narratively rich and hardest-to-reach ending. By putting the Communion
in early crisis, the game creates:

1. **An early humanitarian hook.** The player sees Communion stations running
   low on Food within the first 100 ticks. Running Food to Communion stations
   is profitable (scarcity pricing), morally sympathetic, and builds
   reputation toward the faction that eventually opens the Renegotiate path.

2. **A reason to engage with Fracture early.** The Communion trades in Exotic
   Crystals and Salvaged Tech — goods harvested from shimmer-zone boundaries.
   If the player befriends the Communion by running Food, the Communion offers
   shimmer-zone navigation data in return. This is the on-ramp to the
   Fracture temptation: you start helping refugees, and they introduce you to
   the boundary economy.

3. **A faction that needs YOU specifically.** Every other faction has robust
   NPC trade networks. The Communion's supply lines are fragile because
   they're marginal — few NPC traders bother running Food to remote Communion
   stations. The player's individual contribution matters more here than
   anywhere else. This is the "useful neutral" pattern: you matter because
   you go where others don't.

4. **A contrast with Concord's stability.** Concord stations are comfortable,
   well-supplied, predictable. Communion stations are desperate, chaotic,
   grateful. The player experiences the galaxy's inequality firsthand through
   trade — the "haves" and "have-nots" are not an abstract statistic but a
   difference in docking experience.

### Mechanical Expression

- Communion stations start with **50% Food and Fuel inventory** at tick 0
  (all other factions start at 80-100%).
- NPC trade frequency to Communion stations is **half** the rate of other
  factions (fewer ships run marginal routes).
- Communion buy prices for Food and Fuel are **1.5x base** from tick 0 —
  immediate arbitrage opportunity for the player.
- At **tick 100** without resupply, Communion stations begin rationing:
  services degrade (repair takes longer, module swap unavailable). At
  **tick 200**, station begins broadcasting distress — visible on galaxy map
  as a pulsing icon.
- Running Food or Fuel to a Communion station in distress gives **3x normal
  reputation gain.** The player is rewarded for helping, not just trading.

### Narrative Beat

The Communion is the first faction that treats the player as a person rather
than a customer. Concord is polite-but-surveillance. Chitin are transactional.
Weavers are patient-but-aloof. Valorin are friendly-but-chaotic.

The Communion remembers. The captain who ran Food to a starving station gets
named in Communion internal communications. NPCs at other Communion stations
greet the player by reputation. "You're the one who kept Waystation Kell
alive." This is the emotional hook — not credits, not modules, but a faction
that actually cares that you showed up.

---

## Fracture Module Timing (Clarification)

> Reconciles `dynamic_tension_v0.md` Pillar 5 with the Revelation Arc above.

The fracture module is **not available at game start.** The player spends their
first 2-4 hours (roughly tick 0-400) in pure lane-space: learning to trade,
feeling warfront pressure, upgrading with standard T1 equipment, and
establishing their first trade routes. The module's arrival is a narrative
turning point that recontextualizes the game.

### Why Delayed Discovery Works

1. **The player learns the rules before they can break them.** Lane-space
   trading, warfront economics, faction reputation, tariffs, inspections —
   the player internalizes these systems when they're the ONLY systems. When
   the fracture module arrives, the player understands exactly what they're
   circumventing and what the stakes are.

2. **Standard equipment first.** The player should have time to visit
   multiple stations, compare T1 modules, make fitting decisions, and feel
   ownership of a ship they built with conventional tech. The fracture module
   and exotic materials are more exciting when you already have a baseline to
   compare them against.

3. **The warfront is personal before the escape valve appears.** By tick 300,
   the player has lost a trade route to a warfront shift, paid war tariffs,
   been inspected by hostile patrols. They WANT an alternative. The fracture
   module arrives when the player is already frustrated with the constraints
   of lane-space — making it feel like a genuine temptation, not a tutorial
   unlock.

4. **The Communion relationship builds naturally.** The player encounters
   struggling Communion stations during lane-space play (they're on the
   margins of the lane network). Running Food to them is profitable even
   without fracture capability. When the module arrives, the Communion are
   already friends — and they have navigational data to share.

### Pacing

- **Tick 0-150 (Hour 0-2): Pure lane trader.** Learn markets, feel the
  warfront, make first credits. Upgrade to T1 modules. Encounter Communion
  stations in need. No hint of fracture tech.

- **Tick 150-300 (Hour 2-3): Foreshadowing.** Anomalous scanner readings at
  the edge of explored space. A derelict that doesn't match any known
  faction's design language. NPC dialogue hints: Drifter Communion mentions
  "the old routes." Chitin traders reference "off-ledger goods" with no
  source station.

- **Tick 300-400 (Hour 3-4): Discovery.** The player finds the derelict
  containing the fracture module. The discovery is gated behind a light
  exploration requirement (visit N systems, or reach a specific frontier
  node near the warfront). The module installs automatically — it's too
  valuable to leave behind, too strange to fully understand.

- **Tick 400-500 (Hour 4-5): First fracture jump.** Fuel is expensive.
  Range is limited. The Haven is discoverable within the module's initial
  range. The player's first off-lane experience is disorienting — sensor
  readings flicker, travel time is uncertain, the galaxy looks different
  from out here.

- **Tick 500-800 (Hour 5-8): The temptation grows.** Warfront pressure
  intensifies (second conflict activates). Lane tariffs climb. The player
  discovers that fracture routes bypass blockades. Exotic materials start
  appearing. The cost-benefit calculation shifts — fracture travel goes from
  "expensive curiosity" to "economically rational alternative."

- **Tick 800+ (Hour 8+): Dual doom clock.** Lane routes are expensive and
  dangerous. Fracture routes accumulate Trace. The player must balance
  both pressures. The Revelation Arc (Phase 2: "That's Not How Resonance
  Works") kicks in as discoveries accumulate.

### Discovery Trigger

The fracture module derelict appears at a **frontier node adjacent to the
active warfront.** This is deliberate — the player is drawn toward the front
by profit margins, and the derelict is one hop past the last safe station. The
player must make a risky journey to reach it, reinforcing the
danger-profit correlation (Pillar 1) at the moment of the game's biggest
unlock.

The derelict does NOT appear on the map until the player has:
- Visited at least 4 distinct star systems (ensures basic exploration)
- Completed at least 3 trade transactions (ensures understanding of economy)
- Reached tick 150+ (ensures time for warfront to be felt)

These are soft gates, not hard quests. The player hits them naturally through
normal play. The derelict simply appears on scanners when ready — no quest
marker, no "go here" directive. The player discovers it.

---

## Research Inspirations

### Fiction
- **Vernor Vinge, A Deepness in the Sky** — spider civilization, Zones of Thought (containment layers)
- **Adrian Tchaikovsky, Children of Time** — spider engineering culture, portid silk technology
- **Iain M. Banks, The Culture** — Affront, post-scarcity faction dynamics
- **Peter Watts, Blindsight** — alien intelligence without consciousness, reality-breaking physics
- **Alastair Reynolds, Revelation Space** — Melding Plague (infrastructure decay), Inhibitors (containment enforcement)
- **Liu Cixin, Three-Body Problem** — dark forest theory, existential threat from physics itself
- **Ursula K. Le Guin, The Dispossessed** — Anarresti alternative economics
- **China Mieville, Embassytown** — truly alien communication
- **Jeff VanderMeer, Annihilation** — mutation as communication, Area X's phase boundaries
- **Stanislav Lem, Solaris** — phenomenon that resists understanding

### Games
- **EVE Online** — lane topology as economic geography, wormhole space (unique resources feeding main economy)
- **Starsector** — faction personality through trade policy + military doctrine, remnant systems
- **Sunless Skies/Sea** — cosmic horror integrated with trade, narratively textured trade goods
- **Fallout: New Vegas** — endgame emerging from accumulated faction relationships
- **Alpha Centauri** — factions defined by philosophical stance, not species
- **Outer Wilds** — archaeology-driven revelation, knowledge graph progression
- **Subnautica** — environmental storytelling, biome-distributed progression

### Economic Models
- **Kula Ring** — circular exchange, inspiration for circular faction dependency
- **Hawala** — trust-based transfer, model for reputation-gated trade
- **Potlatch** — competitive generosity, alternative to profit-maximization
- **Damascus Steel / Roman Concrete** — technology that can't be reverse-engineered
