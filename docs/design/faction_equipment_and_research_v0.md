# Faction Equipment, Research & Ancient Technology — Design V0

Status: DRAFT — Under Active Design
Date: 2026-03-10
Companion to: ship_modules_v0.md, factions_and_lore_v0.md, ExplorationDiscovery.md

---

## Design Thesis

**Your ship loadout IS your trade policy.**

The pentagon dependency ring is engineered to prevent faction self-sufficiency. Each faction's equipment requires goods from ANOTHER faction to sustain. Your choice to install a Valorin Antimatter engine means you need rare metals (Valorin supply) AND exotic crystals (from Communion, Valorin's dependency). Your equipment choices commit you diplomatically and economically — every module is a political statement.

This is what separates a great equipment system from stat sticks: the constraints create meaning. The three-constraint fitting system (Slots + Power + Sustain) combines with the pentagon economy to make every loadout decision ripple through your trade empire.

---

## Part 1: Faction Equipment Identity

### The Problem With Current State

The 26 T2 modules in UpgradeContentV0.cs are arbitrarily assigned:
- Communion (the peaceful exploration faction) has Plasma T2, Plasma Cannon, and Missile Launcher
- Valorin (speed+swarm identity) has Railgun, Autocannon, Hardened Shield, Plasma Engine
- Module stats are flat bonuses (DamageBonusPct, SpeedBonusPct) with no mechanical distinction

Every module is a stat stick with a faction gate. Nothing about using Chitin equipment FEELS different from using Concord equipment. Nothing about fitting a ship creates interesting decisions beyond "equip the biggest number."

### The Solution: Faction Equipment Philosophies

Each faction's equipment should embody their species biology, cultural philosophy, and engineering approach. Not just different numbers — different MECHANICS.

---

### Concord — "The Standard" (Blue, Human, Order)

**Engineering Philosophy**: Institutional excellence. Well-tested, modular, interoperable. Nothing flashy, nothing fragile. Coast guard reliability.

**Equipment Niche**: Shields, point defense, logistics, flexibility
**Preferred Engine Line**: Fusion (balanced institutional choice — the safe, proven option)
**Preferred Ship Classes**: Frigate, Cruiser, Carrier (fleet command vessels)

**What Concord Equipment FEELS Like**:
Concord gear is the baseline everything else is measured against. High compatibility, reliable performance, excellent defensive coverage. You won't win a DPS race with Concord weapons, but you won't lose a ship to a surprise alpha strike either. Concord builds are about SURVIVING — shields up, point defense active, cargo safe.

**Unique Faction Mechanic: Zero Variance** — All Concord T2 modules have exactly zero RNG in their effects. Where Chitin modules have probability ranges and Communion modules fluctuate with instability, Concord modules always perform at exactly their rated values. The institutional guarantee: boring, predictable, reliable. This is mechanically invisible until the player experiences another faction's variance — then the ABSENCE of variance becomes the feature. "My Garrison Shield Array always gives +35 shield HP. The Molt Barrier might give +20... or +40. I know which I trust."

**Signature Module: Regulatory Transponder**
- Slot: Utility
- Effect: Zero tariffs at all Concord stations. Position tracked by Concord patrols (they always know where you are). At max rep: extends to allied faction stations
- Tradeoff: You surrender privacy for economic advantage. Concord can revoke it. Smuggling becomes impossible while equipped
- Sustain: None (bureaucratic, not technological)
- Design intent: Embodies the Concord bargain — order gives you access, but you're inside the system

**Signature Module: Fleet Coordination Nexus**
- Slot: Utility
- Effect: Cross-faction T2 modules installed at Concord stations get -25% install time (Concord mechanics have the best tooling). Escort programs coordinated through this module provide +10% shield regen to escorted fleets. At max rep: refit at ANY Concord-allied station gets the install bonus
- Tradeoff: Occupies a utility slot. Only benefits you at Concord stations — useless in the field. The coordination uplink reports your fleet composition to Concord command (they know your loadout)
- Sustain: 1 composites, 1 electronics
- Design intent: Concord's institutional philosophy — standardization, interoperability. They don't bypass faction requirements; they make the PROCESS smoother. The best mechanics in the galaxy, but they report everything. Facilitator, not shortcut

**Concord T2 Equipment Catalog (8 modules)**:

| Module | Slot | Mechanic | Power | Sustain | Lore |
|--------|------|----------|-------|---------|------|
| Garrison Shield Array | Utility | +35 shield HP, +10% shield regen rate | 14 | 1 fuel, 1 composites | Standard military shield enhancement. Composite layering from Weaver supply chain |
| Patrol Deflector Screen | Utility | +25 shield HP, -25% kinetic damage to shields | 12 | 1 composites, 1 electronics | Magnetic field pre-deflection. Weaver composites in the field coils |
| Interceptor Grid | Weapon | Fast-track, +200% vs missiles, 6 base dmg | 10 | 1 munitions, 1 composites | Automated CIWS battery. Composite tracking gimbals |
| Regulation Composite Plate | Utility | +40 hull HP, -10% all zone damage | 10 | 2 composites | Layered composite armor. Weaver material science, Concord engineering |
| Emergency Restoration Unit | Utility | Auto-repair: 2 hull HP/tick when below 50% hull | 12 | 1 metal, 1 composites | Damage control teams. Composite patch material consumed per repair cycle |
| Standard Logistics Module | Cargo | +30% cargo capacity | 5 | 1 composites | Optimized modular cargo containers. Composite structural frames |
| Fleet Reactor Assembly | Utility | +25% power generation | 0 (generates) | 2 fuel, 1 composites | Standard military reactor. Composite containment lining |
| Standard Patrol Drive | Engine | +35% thrust, +10% turning. Fusion line | 15 | 2 fuel, 1 composites | The institutional engine. Composite exhaust nozzles |

**Pentagon Ring Enforcement**: Every Concord T2 module requires **composites** (from Weavers). Running a Concord loadout means you NEED Weaver trade relations. Your institutional reliability depends on spider-silk engineering.

**Research Techs Concord Unlocks (require Concord rep 25+)**:
- Shield Harmonics: Shield regen begins 5 ticks faster after last hit
- Fleet Coordination: Escort programs provide +10% shield to escorted fleet
- Standardized Logistics: -15% refit time at any Concord-allied station

---

### Chitin Syndicates — "The Metamorphosis" (Amber, Insectoid, Adaptation)

**Engineering Philosophy**: Metamorphic. Their biology is holometabolous — self-dissolution and rebirth. They design equipment that TRANSFORMS under stress, adapting to conditions mid-use. Probability is a tool they use; metamorphosis is WHO THEY ARE. Their engineering doesn't guarantee outcomes — it shifts and adapts. Gambling tests their reality models; transformation IS their engineering ethos.

**Equipment Niche**: Sensors, evasion, information warfare, adaptive systems
**Preferred Engine Line**: Ion (efficient, nimble — "see everything, commit to nothing" means don't waste fuel on raw power)
**Preferred Ship Classes**: Corvette, Clipper (fast, light, high scan range)

**What Chitin Equipment FEELS Like**:
Chitin gear is unreliable in the best way. Their Probability Engine might save you fuel or might not. Their Adaptive Shield adjusts to incoming damage types mid-combat. Their sensors see things other factions' sensors can't detect. Chitin builds are about INFORMATION — knowing the odds, seeing the angles, being where the money is before anyone else.

**Signature Module: Probability Engine**
- Slot: Utility
- Effect: Each tick, 15% chance to consume zero fuel for travel. At max rep: "manifest scramble" — cargo scan results randomized for 60 ticks when activated (active ability)
- Tradeoff: When it doesn't fire (85% of ticks), slightly HIGHER fuel consumption (+5%) than standard engines. Variance, not pure upside
- Sustain: 1 fuel, 1 rare_metals
- Design intent: The casino. Sometimes you win big, sometimes you pay the house. Over long distances the expected value is positive, but any single jump might cost more. Chitin players learn to think in expected values

**Signature Module: Pheromone Relay** (NEW — not in original lore)
- Slot: Utility
- Effect: See buy/sell SPREAD (bid/ask gap) at all Chitin stations — you know the volatility but NOT the exact price. Prices shown are 5-10 ticks delayed, creating profitable but imperfect information. At high rep: delay reduces to 2-3 ticks
- Tradeoff: Chitin Syndicates take a 2% cut of all trades you make while relay is active (automatic tithe). The spread information tempts you into trades you might not take blind
- Sustain: 1 rare_metals
- Design intent: Information has a price, and imperfect information IS the casino. You know the table limits but not the cards. The relay makes you a BETTER gambler, not an omniscient one. Perfect for the casino metaphor — the house always gives you just enough to keep betting

**Chitin T2 Equipment Catalog (8 modules)**:

| Module | Slot | Mechanic | Power | Sustain | Lore |
|--------|------|----------|-------|---------|------|
| Molt Barrier | Utility | +20 shield HP. After taking 3 hits of same type (kinetic/energy), shield ADAPTS: +30% resistance to that type for 30 ticks | 14 | 1 rare_metals, 1 electronics | Chitin shields metamorphose under stress. Rare metal alloy enables molecular restructuring |
| Compound-Eye Array | Utility | +2 scan range, detects hidden anomalies, reveals discovery phases at range | 10 | 1 rare_metals | Multi-spectrum compound-eye array. Rare metal sensor filaments |
| Pheromone Scatter | Utility | -30% incoming missile accuracy, -20% enemy scan range vs you | 12 | 1 electronics, 1 rare_metals | Pheromone-pattern signal masking. Rare metal antenna mesh |
| Flicker Drive | Engine | +20% thrust, +25% turning, very low power draw. Ion line | 6 | 1 fuel, 1 rare_metals | Efficient vectored ion array. Rare metal ion grids |
| Swarm Vectoring Unit | Engine | +5% thrust, +45% turning. Ion line | 8 | 1 rare_metals | Extreme maneuverability. Rare metal gyroscope bearings |
| Metamorphic Turret | Weapon | Fast-track, +200% vs missiles. Targeting adapts: after 3 missed shots, accuracy +20% for next burst | 10 | 1 munitions, 1 rare_metals | Adaptive fire control. Learns from misses, transforms targeting pattern |
| Scent Mask Hold | Utility | Hides 40% of cargo from scans | 8 | 1 rare_metals | Scent-masking containers with rare metal EM shielding |
| Chitin Dissonance Field | Utility | Enemy targeting accuracy -15% in combat. Adapts: penalty increases by 5% each tick of sustained combat (max -30%) | 12 | 1 electronics, 1 rare_metals | Active jamming that metamorphoses its frequency pattern. Gets harder to track over time |

**Pentagon Ring Enforcement**: Every Chitin T2 module requires **rare_metals** (from Valorin). The Syndicates' metamorphic alloys need Valorin's mineral wealth. Your adaptive advantage depends on your most aggressive trading partner.

**Research Techs Chitin Unlocks (require Chitin rep 25+)**:
- Probability Modeling: Trade price predictions +-2% accuracy for visited systems
- Metamorphic Plating: Hull slowly regenerates in Phase 1+ space (1 HP/20 ticks)
- Information Arbitrage: See price history graphs at Chitin stations (commodity trends)

---

### Weavers — "The Foundation" (Green, Spider-like, Structure)

**Engineering Philosophy**: Seven types of silk, each with different properties. Weaver engineering is about STRUCTURE — load-bearing calculations, stress distribution, tensile strength. They don't make fast things. They make things that last. Ambush predators who build and wait.

**Equipment Niche**: Zone armor, hull reinforcement, repair, structural integrity, tractor beams
**Preferred Engine Line**: Fusion (reliable, medium — speed is irrelevant when you're the heaviest thing in the fight)
**Preferred Ship Classes**: Hauler, Cruiser (tanky, heavy, slow — mobile stations)

**What Weaver Equipment FEELS Like**:
Weaver gear turns your ship into a fortress. Silk Lattice redistributes armor damage across zones so no single facing breaks easily. Repair drones constantly restore hull integrity. Weaver cruisers with full Weaver loadouts are NIGHTMARES to kill — they just absorb everything and grind you down. But they're slow. Agonizingly slow. A Weaver build is a commitment to patience.

**Signature Module: Silk Lattice Reinforcement**
- Slot: Utility
- Effect: ACTIVE ABILITY (cooldown: 60 ticks). When activated, redistributes armor evenly across all zones — the highest zone donates to the lowest until all are equal. Player chooses WHEN to trigger. At max rep: cooldown reduced to 40 ticks
- Tradeoff: -10% speed (structural mass). Cannot be installed alongside Engine Mk2 or better (structural interference). Using it mid-combat is a tactical decision: redistribute NOW or save it for when things get worse?
- Sustain: 1 composites, 1 electronics
- Design intent: Directional combat becomes a PUZZLE for enemies AND for you. The redistribution is powerful but requires timing. Too early and you waste it; too late and a zone is already gone. This is the Weaver identity: patience, structural awareness, choosing the right moment to act. The player makes the spider's decision, not the module

**Signature Module: Structural Resonance Web** (NEW)
- Slot: Utility
- Effect: +15% zone armor to all zones. While docked at a Weaver station, refit time -30% (the station's structural web integrates with your ship)
- Tradeoff: Module CAN be removed at any station, but removal DESTROYS the module — the web has grown into the hull and cannot survive extraction. Reinstallation requires purchasing a new one (rep 50+). The web is alive; ripping it out kills it
- Sustain: 1 composites, 1 electronics
- Design intent: Weaver technology literally becomes part of your ship. Commitment comes from the COST of leaving — you lose the module, not your freedom. The player's knowledge that removal is destructive IS the commitment, not a hard gate. Beautiful metaphor for spider silk: strong, integrated, but you can always cut free if you're willing to pay

**Weaver T2 Equipment Catalog (8 modules)**:

| Module | Slot | Mechanic | Power | Sustain | Lore |
|--------|------|----------|-------|---------|------|
| Dragline Plate | Utility | +40 zone armor to assigned zone, -10% all damage to that zone | 8 | 1 composites, 1 electronics | Silicon carbide weave. Electronic stress monitors calibrate the weave |
| Tensile Reactive Weave | Utility | +35 zone armor to assigned zone, -25% kinetic damage to that zone | 10 | 1 composites, 1 electronics | Pressure-responsive composite. Electronic impact sensors trigger hardening |
| Mending Strand Drone | Utility | 3 hull HP/tick out of combat, 1 HP/tick in combat | 10 | 1 metal, 1 electronics | Silk-strand repair drones. Electronic pathfinding for strand placement |
| Aft Cocoon | Utility | +45 aft zone armor. Protects engines specifically | 6 | 1 composites, 1 electronics | Structural cage around drive section. Electronic thermal regulation |
| Web Pivot Drive | Engine | +10% thrust, +40% turning. Fusion line | 14 | 1 fuel, 1 electronics | Reaction wheel + fusion thruster. Electronic gyroscope control |
| Load-Bearing Strut | Utility | +50 hull HP | 6 | 1 composites, 1 electronics | Structural reinforcement nano-weave. Electronic load distribution |
| Spindle Tractor | Utility | Tractor beam, 25u range, auto-targets salvage | 8 | 1 electronics | Structural web tractor. Electronic targeting and beam focus |
| Ambush Driver | Weapon | Kinetic, Heavy tracking, 20 base damage, +25% vs large targets (Cruiser+) | 16 | 1 metal, 1 electronics | Electromagnetic mass driver. Electronic fire control waits for the perfect moment |

**Pentagon Ring Enforcement**: Every Weaver T2 module requires **electronics** (from Chitin). The Weavers build structures; the Chitin provide the nervous system. Your fortress depends on insectoid precision engineering.

**Research Techs Weaver Unlocks (require Weaver rep 25+)**:
- Tensile Optimization: Zone armor values +10% when all four zones are above 50%
- Silk Integration: Refit at Weaver stations costs 20% less credits
- Structural Memory: After combat, zone armor slowly regenerates out of combat (1/30 ticks per zone)

---

### Valorin Clans — "The Swarm" (Red, Rodent-like, Expansion)

**Engineering Philosophy**: Cheap, fast, mass-produced. Valorin don't build the BEST anything — they build ENOUGH of everything. Neurologically fearless (dark-burrow adaptation), so their equipment has risk profiles that other species wouldn't tolerate. Antimatter engines that might explode? Acceptable. Clan engineering is about speed, volume, and acceptable losses.

**Equipment Niche**: Speed, cheap weapons, cargo capacity, caching, engine power
**Preferred Engine Line**: Antimatter (raw power, explosively fast — matches "neurologically fearless")
**Preferred Ship Classes**: Corvette, Clipper, Frigate (fast combat ships — not the big slow ones)

**What Valorin Equipment FEELS Like**:
Valorin gear makes you FAST and AGGRESSIVE. Their Antimatter engines burn hot and their weapons hit hard per-credit. Nothing is elegant — everything is functional and slightly dangerous. Cache Beacons let you stash cargo in hidden locations. Valorin builds are about SPEED — get there first, grab everything, outrun what you can't outfight. The Valorin solution to any problem is "more ships, faster."

**Signature Module: Cache Beacon**
- Slot: Utility
- Effect: Deploy at current location. Creates invisible cargo cache (hidden from all scans). Store up to 50 cargo units. Retrieve later. Max 3 active beacons
- Tradeoff: If you die, beacons persist but coordinates are lost (recovery mission). Beacons decay after 2000 ticks if not visited. Contents can be discovered by Chitin deep scanners at close range
- Sustain: None (passive hardware)
- Design intent: The hoarder's dream. Stash valuable cargo in safe locations along trade routes. Pre-position fuel or munitions for combat operations. Create your own supply chain independent of stations. Pure Valorin — expand your reach, cache your treasures

**Signature Module: Burrow Protocol** (NEW)
- Slot: Utility
- Effect: ACTIVE ABILITY (cooldown: 120 ticks). When activated, ship enters "burrow" mode for 30 ticks: +40% speed, -80% weapon power, +50% ECM. Player CHOOSES when to burrow. At max rep: cooldown reduced to 80 ticks
- Tradeoff: During burrow mode, cannot trade, dock, or use tractor beams. Pure flight response. The decision of WHEN to trigger is the gameplay: burrow too early and you abandon a winnable fight; too late and you're already dead
- Sustain: 1 fuel, 1 exotic_crystals
- Design intent: Dark-burrow adaptation made technological. The Valorin don't fear death — but they have INSTINCTS about when to run. The module ENABLES the escape; the player DECIDES to use it. This creates tension: "Do I burrow now at 30% hull, or fight on?" The Valorin designed the tool; you provide the judgment

**Valorin T2 Equipment Catalog (8 modules)**:

| Module | Slot | Mechanic | Power | Sustain | Lore |
|--------|------|----------|-------|---------|------|
| Burnclaw Drive | Engine | +50% thrust, +15% turning. Antimatter line. HIGH power draw | 20 | 2 fuel, 1 exotic_crystals | Explosive acceleration. Exotic crystal containment lattice for antihydrogen |
| Snapjaw Vectoring | Engine | +15% thrust, +55% turning. Antimatter line. HIGH power draw | 18 | 1 fuel, 1 exotic_crystals | Directed annihilation jets. Crystal-regulated magnetic nozzles |
| Fang Repeater | Weapon | Kinetic, Fast-track, 10 base damage, fires 2x per round | 12 | 1 munitions, 1 exotic_crystals | Mass-produced slug thrower. Crystal-tuned feed mechanism |
| Clan Railgun | Weapon | Kinetic, Heavy tracking, 22 base damage | 16 | 1 metal, 1 exotic_crystals | Lorentz accelerator. Exotic crystal capacitor bank |
| Burrow Barrier | Utility | +30 shield HP, shield takes -15% less kinetic damage | 12 | 1 fuel, 1 exotic_crystals | Crude but effective energy barrier. Crystal-resonant emitters |
| Packrunner Drive | Engine | +40% thrust, +20% turning. Hybrid AM/Fusion | 14 | 1 fuel, 1 exotic_crystals | Compromise drive. Crystal governor regulates fuel mix |
| Hoard Bay | Cargo | +40% cargo capacity, -5% speed | 4 | 1 exotic_crystals | Bolted-on cargo modules. Crystal-locked environmental seals |
| Tunnel-Sense Scanner | Utility | +1 scan range, reveals Cache Beacons within range | 8 | 1 exotic_crystals | Burrow-adapted sensors. Crystal resonance detection |

**Pentagon Ring Enforcement**: Every Valorin T2 module requires **exotic_crystals** (from Communion). The Clans' antimatter technology and even their cargo bays depend on Communion crystal harvesting. Your speed and firepower depend on the galaxy's most peaceful faction.

**Research Techs Valorin Unlocks (require Valorin rep 25+)**:
- Swarm Logistics: Escort programs support 1 additional fleet
- Frontier Cartography: Unexplored systems revealed on galaxy map (shows star class, not details)
- Burrow Instinct: When hull drops below 20%, warp drive charges 50% faster and evasion +25%. Rewards aggressive play with a natural escape window — the Valorin instinct kicks in when you're bloodied

---

### Drifter Communion — "The Resonance" (Purple, Human, Understanding)

**Engineering Philosophy**: Edge-dwelling. Their equipment is calibrated for Phase 1 (Shimmer) space where measurements flutter and conventional instruments disagree. Communion technology doesn't fight instability — it LISTENS to it. Exotic crystal harvesting requires extreme sensor sensitivity. Their gear is fragile but perceives what others can't.

**Equipment Niche**: Fracture navigation, exotic sensing, metric harmonics, crystal processing
**Preferred Engine Line**: Ion initially, then Adaptation tech (Void Sail) — Communion are the natural path to T3 drives
**Preferred Ship Classes**: Shuttle, Clipper (small, nimble, eyes not fists)

**What Communion Equipment FEELS Like**:
Communion gear is... strange. Their Metric Harmonics Array makes fracture travel cheaper and reveals things hidden to other sensors. Their Navigation Resonator lets you sense instability patterns before you arrive. In unstable space, Communion equipment actually works BETTER while conventional gear degrades. Communion builds are about PERCEPTION — seeing the universe as it actually is, not as the Lattice presents it. You sacrifice combat power for revelation.

**Signature Module: Metric Harmonics Array**
- Slot: Utility
- Effect: Fracture travel fuel cost -25%. In Phase 1+ space, scan range +50%. Reveals "resonance signatures" at anomalies (additional discovery information not visible to other sensors). At max rep: enhanced void sites yield +50% exotic matter
- Tradeoff: In Phase 0 (stable) space, scan range -10%. The array is calibrated for instability — stability is noise to it. Also: high power draw
- Sustain: 1 exotic_crystals, 1 food
- Design intent: The Communion bargain — their technology works better in dangerous space. This creates a natural gameplay loop: Communion alignment pulls you toward the edge, toward fracture space, toward the discoveries that others can't see. It's the gentle hand guiding the player toward the Renegotiate endgame path

**Signature Module: Phase-Lock Extractor** (NEW)
- Slot: Utility
- Effect: Exotic crystal extraction yield +30% at Communion sites. Can extract exotic crystals from Phase 2+ anomalies (normally requires Phase 3+). Crystals harvested with this module have +10% quality (affects sustain efficiency)
- Tradeoff: Module is attuned to specific metric frequencies — incompatible with Lattice Drone pacification technology. Having this equipped makes Lattice Drones always hostile to you
- Sustain: 1 exotic_crystals, 1 food
- Design intent: Communion lives on the edge. Their harvesting tech is superior but draws attention from the Lattice infrastructure. Beautiful narrative tension: the better you are at harvesting the Communion way, the more the ancient Containment system treats you as a threat

**Communion T2 Equipment Catalog (8 modules)**:

| Module | Slot | Mechanic | Power | Sustain | Lore |
|--------|------|----------|-------|---------|------|
| Depth Tremor Sensor | Utility | +2 scan range, detects anomalies 1 phase earlier (see Phase 2 anomalies from Phase 1 space) | 12 | 1 food, 1 rare_metals | Forward mass detector. Living crystal substrate requires nutrient solution |
| Current Reader | Utility | Reveals instability level of adjacent systems without visiting. In Phase 2+, travel time variance -50% | 10 | 1 food | Metric frequency tuning fork. Crystal-organic sensor membrane fed by nutrient bath |
| Quiet Eye | Utility | +1 scan range, zero signature (stealth scan). Detects thermal signatures of ships/stations at range | 6 | 1 food | Passive crystal-optic sensor. Living crystal lattice requires feeding |
| Shimmer Drive | Engine | +25% thrust. In Phase 1+ space, +additional 15% thrust (total +40%). Ion line variant | 10 | 1 fuel, 1 food | Crystal-tuned for unstable metrics. Living crystal navigation matrix |
| Star Drinker | Utility | Moderate fuel regen near stars. In Phase 1+ space, also regenerates from ambient metric energy | 4 | 1 food | Crystal energy harvester. Phase-locked crystal collector array |
| Phase-Lock Cradle | Cargo | Exotic crystal cargo doesn't decay. +10 cargo capacity for crystals only | 8 | 1 food | Phase-locked crystal storage. Living crystal-symbiont cradle |
| Drift Shield | Utility | +20 shield HP. In Phase 1+ space, shield regen rate +50% | 10 | 1 fuel, 1 food | Metric-tuned crystal barrier. Resonant crystal lattice layer |
| Revelation Lens | Utility | Discovery scan speed +30%. Analysis time -20% | 10 | 1 food, 1 electronics | Crystal-perceptual enhancement. Living lens crystal requires feeding |

**Pentagon Ring Enforcement**: Every Communion T2 module requires **food** (from Concord). Communion technology uses crystal-organic hybrid substrates — living crystal matrices that require organic nutrient solutions to maintain phase coherence. The food sustain isn't for the crew; it's for the CRYSTALS. Exotic crystals are harvested alive, and Communion engineering keeps them alive in the equipment. Your perception depends on Concord's agricultural supply chain. The galaxy's most institutional faction feeds the galaxy's most spiritual technology.

**Secondary dependencies (intentional asymmetry)**: Two Communion T2 modules require non-pentagon scarcity goods: Depth Tremor Sensor (`1 rare_metals` — Valorin) and Revelation Lens (`1 electronics` — Chitin). This makes Communion the ONLY faction with secondary dependencies beyond their pentagon partner. This is intentional: Communion technology draws on the widest range of materials because their crystal-organic engineering integrates sensor filaments (rare metal), computational optics (electronics), and biological substrate (food) into unified instruments. The Communion's role as cultural bridge — edge-dwellers who interact with all factions — is reflected in their broader supply needs. A full Communion loadout requires trade relations with three factions (Concord, Valorin, Chitin), making Communion alignment the most diplomatically demanding choice.

**Research Techs Communion Unlocks (require Communion rep 25+)**:
- Metric Sensitivity: Instrument readings become more precise in Phase 1+ space (scanner jitter -50%)
- Crystal Attunement: Exotic crystal sustain costs reduced 20% for all modules
- Threshold Sense: In Phase 2+ space, your ship emits a passive warning pulse 10 ticks before a topology shift or instability spike occurs. Changes HOW you play in dangerous space — you can prepare, reposition, or flee before the event. The Communion's gift: not power, but AWARENESS

---

## Part 2: Research System Redesign

### Current State (Problems)

The current research system is a simple queue:
- 13 generic techs, faction-neutral
- Credit cost + goods sustain per tick
- Linear prereq chain
- One concurrent research
- No connection to exploration or discovery

This is BORING. Research is a menu you click through. It doesn't integrate with the exploration system, doesn't create interesting choices, and doesn't reward the player for engaging with the world.

### New Design: Three Research Pillars

Research happens in three distinct contexts, each with its own flavor:

#### Pillar 1: Faction Labs (T2 Faction Equipment)

**Where**: Any faction station where you have rep 25+ (Friendly tier)
**What**: Faction-specific technology — unlocks faction T2 modules, faction ship variants, faction-exclusive abilities
**How**: Credits + faction-specific goods + time. Standard research queue. One concurrent per faction
**Cost driver**: Pentagon ring goods (Concord research needs composites from Weavers, etc.)

**Why it's interesting**: To research Concord Shield Harmonics, you need composites (from Weavers). To research Valorin Swarm Logistics, you need exotic crystals (from Communion). Your research agenda is constrained by your trade network. Want to research everything? You need good relations with everyone — which the warfronts make increasingly difficult.

**Progression**: Each faction has 3 tiers of faction research:
- Tier 1 (rep 25+): Basic faction modules + 1 research tech
- Tier 2 (rep 50+): Advanced faction modules + signature module + 1 research tech
- Tier 3 (rep 75+): Faction ship variants + 1 research tech + signature module upgrade

This means the player must CHOOSE which factions to invest in deeply. Getting rep 75+ with one faction during a warfront probably means losing rep with their enemy. Your research path reflects your political alignment.

#### Pillar 2: Field Research (Discovery-Driven Universal Tech)

**Where**: The field — exploration discoveries, anomaly encounters, derelict analysis
**What**: Universal technology advances (engine improvements, sensor upgrades, materials science) that come from FINDING things in the world
**How**: Discovery → Analysis → Tech Lead → Research at any station

**The Pipeline**:
1. **DISCOVER**: Find an anomaly, derelict, or ruin during exploration
2. **SCAN**: Use scanner modules to advance from Seen → Scanned
3. **ANALYZE**: Use analysis time/modules to advance from Scanned → Analyzed. This yields a **Tech Lead** — a data package that describes a technological principle
4. **RESEARCH**: Bring the Tech Lead to any station with lab facilities. Research converts the lead into a usable technology
5. **UNLOCK**: Technology unlocks new modules for purchase, stat bonuses, or capability upgrades

**Tech Lead Categories** (what you find determines what you can research):
- **Propulsion Leads**: Found in derelicts with unusual engine signatures → engine upgrades, fuel efficiency
- **Materials Leads**: Found in ruins with exotic construction → armor upgrades, hull improvements
- **Weapons Leads**: Found in combat derelicts, war-era ruins → weapon upgrades, new weapon types
- **Sensor Leads**: Found in observation posts, scanner arrays → sensor upgrades, detection improvements
- **Energy Leads**: Found in reactor ruins, power facilities → reactor upgrades, shield improvements
- **Navigation Leads**: Found in Phase 2+ anomalies → fracture navigation, instability resistance

**Why it's interesting**: You can't just queue up "better engines" from a menu. You have to FIND a propulsion lead in the world, ANALYZE it, and THEN research it. This means:
- Exploration has direct progression value (not just credits and XP)
- Different saves yield different tech leads (replayability)
- The player's research tree is shaped by their exploration path
- "I found a Propulsion Lead at that derelict near Valorin space!" creates stories

**Anti-frustration**: The discovery seeding system already has anti-drought rules (no 10+ minutes without narrative touchpoint). Tech leads are seeded with similar density rules — the player should find their first tech lead within the first 2 hours, and average one every 3-4 hours of active exploration.

#### Pillar 3: Haven Research (T3/Adaptation Technology)

**Where**: Haven Starbase only (must discover and upgrade it)
**What**: T3 Relic modules, Adaptation Fragment effects, ancient technology reconstruction
**How**: Fragments + exotic matter + Haven lab tier + time

**Haven Lab Tiers**:

| Haven Tier | Lab Capability | Research Slots | What You Can Research |
|------------|---------------|----------------|---------------------|
| 1 (Powered) | Fragment identification | 0 | Can identify what fragments DO but not use them |
| 2 (Lab) | Basic reconstruction | 1 | T3 utility modules (sensors, reactors, tractor beams) |
| 3 (Operational) | Advanced reconstruction + drydock | 2 | T3 weapons + defense modules. Ancient ship hull restoration |
| 4 (Expanded) | Full fabrication + resonance chamber | 3 | All T3 modules. Fragment resonance pairs (combine 2 fragments for emergent effects) |
| 5 (Awakened) | Accommodation geometry active | 3 | Endgame-exclusive modules. Haven itself becomes an asset for the endgame path |

**T3 Research Requirements** (per module):
- 1-2 specific Adaptation Fragments (determines WHICH T3 modules you can research)
- Exotic matter (ongoing sustain cost — exploration must continue)
- Haven at appropriate tier
- Research time (longer than T2, 50-100 ticks)

**Fragment → T3 Module Mapping** (which fragments unlock which modules):

| Fragment | Solo Effect | T3 Modules Unlocked |
|----------|------------|-------------------|
| Void Cartography | Map reveals Phase 3+ geography | Metric Drive Core, Void Sail |
| Current Reading | Travel time prediction in unstable space | Navigation-related T3 |
| Depth Sensing | Detect anomaly depth/richness before visiting | Gravimetric T3 sensors |
| Substrate Shaping | Hull slowly adapts to fracture stress | Null-Mass Lattice |
| Lattice Reading | Temporary Lattice Drone pacification | Gravitational Lens |
| Resonance Tuning | Module efficiency +10% in Phase 2+ | Phase Matrix |
| Phase Tolerance | Reduced hull damage in Phase 3 | Void Seekers |
| Geometric Suspension | Cargo capacity +20% in fracture space | Stasis Vault |
| Adaptive Plating | Zone armor slowly redistributes over time | Null-Mass Lattice (alt path) |
| Pattern Recognition | Discovery scan speed +50% | Resonance Comm |
| Frequency Matching | Communication with... something | Graviton Shear, Seed Fabricator |
| Dialogue Protocol | Opens the Renegotiate path | Quantum Vacuum Cell + endgame modules |

**Fragment Resonance Pairs** (combining two fragments at Haven Tier 4):

| Pair | Combined Effect | Design Intent |
|------|----------------|---------------|
| Void Cartography + Current Reading | Full fracture space navigation (no hull damage in Phase 3, reduced in Phase 4) | Navigation mastery |
| Substrate Shaping + Adaptive Plating | Hull self-heals in ANY space, not just fracture | Defense mastery |
| Lattice Reading + Resonance Tuning | Lattice Drones become permanent allies (escort you) | Lattice coexistence |
| Pattern Recognition + Depth Sensing | Instant discovery analysis (skip Scanned phase) | Exploration mastery |
| Frequency Matching + Dialogue Protocol | Direct communication with instability patterns | Renegotiate path prerequisite |
| Phase Tolerance + Geometric Suspension | Phase 4 Void travel without Metric Drive Core | Alternative endgame access |

**Why it's interesting**: Haven research creates the game's deepest progression system. Each fragment unlocks different T3 modules, so the order you find fragments determines your research priorities. Fragment resonance pairs create powerful combinations, but you need Haven Tier 4 to use them — which requires substantial exotic matter investment. The endgame equipment tree is shaped by your exploration path.

**Fragment → T3 Cross-Check** (verified against `ship_modules_v0.md`):

T3 modules in `ship_modules_v0.md`: Graviton Shear, Void Seekers, Null-Mass Lattice,
Gravitational Lens, Phase Matrix, Metric Drive Core, Void Sail, Quantum Vacuum Cell,
Resonance Comm, Seed Fabricator, Stasis Vault = **11 modules**.

Coverage gaps:
- "Current Reading → Navigation-related T3" — should specify: Metric Drive Core (navigation mastery)
- "Depth Sensing → Gravimetric T3 sensors" — should specify: Gravitational Lens (gravity sensing)
- Previous "Graviton Tether" entry was a typo for Graviton Shear (corrected above)
- All 11 T3 modules now have at least one fragment path. Module count corrected from "13" to "11" in Summary.

---

## Part 3: Research Location & The Science Question

### Who Does the Research?

**Not a single "science person."** The research system is facility-based, not character-based:

- **Faction stations** have labs as part of their infrastructure. The faction's scientists do the work. Your contribution is goods, credits, and time
- **Field research** is YOUR ship's analysis capability — scanner modules and analysis time. Better sensors = faster tech leads
- **Haven's lab** is ancient infrastructure. YOU are the researcher, using the accommodation equipment

The **First Officer** (future system) provides an accent, not a requirement:
- Analyst (ex-Chitin): +15% research speed at any faction station, +probability modeling for tech leads
- Veteran (ex-Concord): +10% research speed at Concord stations, +20% at Haven
- Pathfinder (Communion-adjacent): +20% tech lead discovery rate from exploration, +faster fragment identification

This keeps research SYSTEMIC (facilities + goods + time) with character FLAVOR (First Officer bonus). The First Officer doesn't gatekeep research — they accelerate it.

### Where Can You Research?

| Research Type | Location | Requirement |
|--------------|----------|-------------|
| Faction T2 Tier 1 | Any faction station | Rep 25+ with that faction |
| Faction T2 Tier 2 | Faction capital station | Rep 50+ with that faction |
| Faction T2 Tier 3 | Faction capital station | Rep 75+ with that faction |
| Universal (Field) | Any station with market | Tech Lead in cargo + goods + credits |
| T3 Relic | Haven Lab | Fragment(s) + exotic matter + Haven tier |
| Fragment Resonance | Haven Resonance Chamber | 2 fragments + Haven Tier 4 |

---

## Part 4: Faction Ship Variants

### Design Philosophy

Rather than creating entirely new ship models, faction variants modify existing classes with bonuses and maluses that embody each faction's engineering philosophy. Each faction produces 2-3 ship class variants.

A faction variant:
- Starts from a base ship class (same hull, same slot count)
- Applies faction-specific stat modifications
- May have 1 modified slot (e.g., a weapon slot becomes a utility slot, reflecting engineering priorities)
- Has a faction-prefixed name (e.g., "Concord-pattern Frigate")
- Requires faction rep 75+ to purchase
- Costs 30% more credits than the base class
- Has faction-colored visual trim (uses existing faction color system)

### Variant Table

| Faction | Class | Variant Name | Modifications | Design Intent |
|---------|-------|-------------|---------------|---------------|
| **Concord** | Frigate | Watchman-class | +20% shield, 1 weapon slot → utility slot, -10% speed | Fleet escort. Sacrifices firepower for defensive flexibility |
| **Concord** | Cruiser | Sentinel-class | +15% shield, +15% zone armor all, -15% thrust | Fleet command. The rock enemies crash against |
| **Concord** | Carrier | Guardian-class | +20% shield, 1 cargo slot → utility slot, -10% weapons damage | Drone command. Defense through drone screen, less haul capacity |
| **Chitin** | Corvette | Gambit-class | +25% scan range, +15% speed, 1 utility slot → engine slot, -20% hull | Glass cannon scout. Two engine slots for extreme mobility |
| **Chitin** | Clipper | Wager-class | +20% scan range, +20% speed, -15% shield, -10% hull | Fast trader/smuggler. Outrun, outsmart |
| **Weavers** | Hauler | Spindle-class | +30% zone armor all, +20% hull, 1 engine slot → utility slot, -25% speed | Fortress hauler. Trades mobility for another armor/repair slot |
| **Weavers** | Cruiser | Loom-class | +25% zone armor all, +15% hull, -20% speed | Assault fortress. The wall that moves toward you |
| **Valorin** | Corvette | Fang-class | +25% speed, +15% weapon damage, -20% shield | Wolfpack fighter. Fast, hits hard, fragile shields |
| **Valorin** | Clipper | Runner-class | +20% speed, 1 utility slot → cargo slot, -15% shield, -10% hull | Smuggler/trader. Cargo slot replaces utility for raw hauling |
| **Valorin** | Frigate | Raider-class | +20% speed, +10% weapon damage, +15% cargo, -20% shield | Light warship/raider. Hit and loot |
| **Communion** | Shuttle | Wanderer-class | +40% scan range, 1 weapon slot → utility slot, -30% weapon damage, -10% hull | Explorer. Weapon slot becomes sensor/perception slot |
| **Communion** | Clipper | Pilgrim-class | +30% scan range, +fracture resistance (50% less hull damage in Phase 2+), -25% weapon damage | Deep explorer. Built for the edge |

---

## Part 5: Ancient Ships

### The Narrative Through Equipment

The ancient civilization had two factions: Containment (suppress turbulence) and Accommodation (shape turbulence). Their ships reflect this schism:

- **Containment ships** are heavy, armored, weapon-laden — built to ENFORCE compliance and suppress anomalies by force
- **Accommodation ships** are sensor-rich, graceful, utility-focused — built to NAVIGATE and understand anomalies, not fight them

The player discovers BOTH types. The contrast tells the story of the schism through gameplay, not exposition.

### Pre-Revelation Naming

Per cover-story naming discipline, ancient ships are NOT identified by faction origin until after the Module Revelation (~Hour 8):

| Stage | What the Player Sees | What They Learn |
|-------|---------------------|-----------------|
| **Discovery** | "Unidentified Hulk" | Just a wreck. Scanner shows it's VERY old |
| **First Scan** | "Ancient Hull — Class Unknown" | Pre-dates all known factions. Enormous |
| **Analysis Complete** | Player/First Officer proposes a name based on function | "The sensor array configuration... this was built to FIND things." → Seeker. "This is an enforcement platform." → Bastion |
| **Post-Revelation** | True faction-origin names available | "Containment Bastion", "Accommodation Seeker", "Accommodation Threshold" |

The naming is a micro-revelation moment per Narrative Principle #1. The player ASSEMBLES the identity of these ships through analysis, not exposition.

### Three Ancient Ship Classes

#### Bastion (Containment Capital Ship) — The Dreadnought

The ship already described in ship_modules_v0.md. Now with lore context:

- **Found**: Ancient military installation ruins (Phase 3+ space). 1-2 per save
- **Restoration**: Haven Tier 3 drydock. 200 ticks + massive exotic matter cost
- **Lore**: Containment faction enforcement vessel. Built during the schism to force compliance. Brute-force metric stabilization at ship scale — essentially a mobile Lattice node. "What did the thread builders need this to fight?" Each other
- **Stats**: As currently defined — 10 slots, 100 power, massive zone armor. 2 spinal mounts. Devastating
- **Special**: Lattice Drones treat Bastions as ALLIES (Containment IFF still active). Built-in Lattice frequency emitter pacifies nearby drones
- **Gameplay**: The ultimate combat ship. But its Containment-heritage systems conflict with Adaptation modules. Cannot install more than 2 Adaptation-origin T3 modules simultaneously (Containment architecture rejects accommodation geometry)

#### Seeker (Accommodation Scout) — NEW

- **Found**: Phase 3+ anomalies with "resonance signature" markers (Communion Metric Harmonics Array reveals them). 2-3 per save (more common than Bastions — Accommodation built more scouts than warships)
- **Restoration**: Haven Tier 3 drydock. 100 ticks + moderate exotic matter
- **Lore**: Accommodation faction exploration vessel. Designed to READ spacetime turbulence, not suppress it. These ships went looking for patterns, for understanding. Many never came back — but their sensor data was transmitted home
- **Stats**: Between Clipper and Frigate

| Stat | Value | Identity |
|------|-------|----------|
| Slot Count | 7 (1 weapon, 2 engine, 4 utility) | Perception over firepower |
| Base Power | 50 | Moderate — utilities need power, but no heavy weapons |
| Cargo | 40 | Moderate |
| Scan Range | 150 | MASSIVE — 5x Clipper (30), 3x Frigate (50). Best scanning platform in the game |
| Zone Armor | Fore 20, Port 15, Stbd 15, Aft 25 | Light. Aft reinforced (meant to flee, not fight) |
| Core Hull | 55 | Moderate |
| Base Shield | 35 | Moderate |

- **Special**: In Phase 2+ space, all scanner abilities enhanced +30%. Discovery analysis time -40%. Fracture travel hull damage -50%. Cannot mount spinal or broadside weapons (accommodation geometry isn't compatible with the energy patterns)
- **Gameplay**: The ultimate exploration ship. Terrible in combat (1 weapon slot), incredible for discovery. Finding a Seeker transforms the mid-to-late game — suddenly you can scan deeper, analyze faster, and survive fracture space more comfortably. But you need escorts

#### Threshold (Accommodation Cruiser) — NEW

- **Found**: Phase 4 Void sites only. 1 per save (extremely rare). Requires Metric Drive Core to reach Phase 4
- **Restoration**: Haven Tier 4 drydock (full fabrication capability). 300 ticks + extreme exotic matter
- **Lore**: The vessels that crossed thresholds between stable and unstable space. Built for the deepest exploration. Only the Accommodation faction's best pilots flew these — and most didn't return. The ones that did brought back the knowledge that became the Haven
- **Stats**: Cruiser-equivalent with unique properties

| Stat | Value | Identity |
|------|-------|----------|
| Slot Count | 9 (2 weapon, 2 engine, 5 utility) | Balanced but utility-heavy |
| Base Power | 80 (increases to 100 in Phase 2+ space) | Accommodation geometry FEEDS on instability |
| Cargo | 70 | Good — long-range expeditions |
| Scan Range | 130 | Excellent |
| Zone Armor | Fore 35, Port 30, Stbd 30, Aft 30 | Even distribution. No weak facing |
| Core Hull | 90 | Strong |
| Base Shield | 50 | Moderate |

- **Special**:
  - Accommodation Hull: In Phase 2+ space, hull slowly regenerates (2 HP/20 ticks). Zone armor redistributes automatically over time (like Silk Lattice but passive)
  - Power Surge: Power generation increases in unstable space (+20% Phase 2, +40% Phase 3, +60% Phase 4)
  - Cannot install Lattice-origin technology (Containment modules conflict with accommodation hull geometry)
  - All installed Adaptation Fragment effects are amplified by 25%
- **Gameplay**: The endgame exploration/combat hybrid. In stable space, it's a decent cruiser. In fracture space, it becomes a BEAST — regenerating, powerful, with enhanced sensors. The Threshold is the game's argument for the Accommodation path: this technology WORKS. The question is whether you trust it enough to push further

### Ancient Ship Design Summary

| Ship | Origin | Found | Qty/Save | Combat | Exploration | Endgame Role |
|------|--------|-------|----------|--------|-------------|-------------|
| Bastion | Containment | Phase 3+ ruins | 1-2 | DEVASTATING | Poor | Reinforce path flagship |
| Seeker | Accommodation | Phase 3+ anomalies | 2-3 | Terrible | INCREDIBLE | Exploration platform |
| Threshold | Accommodation | Phase 4 Void only | 1 | Very strong (in fracture) | Excellent | Naturalize/Renegotiate flagship |

**Narrative Through Ships**: The Bastion is armored, weaponized, suppressive — Containment's answer to everything. The Seeker and Threshold are perceptive, adaptive, responsive — Accommodation's answer. The player's choice of which ancient ship to invest in (restoration is expensive) reflects and reinforces their endgame path alignment.

---

## Part 6: The Full Equipment Pipeline

### How It All Connects

```
EARLY GAME (Hours 1-4)
├── T1 Standard modules (purchased at any station, common goods sustain)
├── Begin building faction rep through trade
├── Find first Tech Lead through exploration
└── Research universal techs at visited stations

MID GAME (Hours 4-10)
├── T2 Faction modules unlock (rep 25+ at faction stations)
├── Discover signature modules at rep 50+ (Probability Engine, Cache Beacon, etc.)
├── Find more Tech Leads — research agenda shaped by what you discover
├── Pentagon ring sustain pressure grows (T2 modules need cross-faction goods)
├── Module Revelation (~Hour 8) — fracture drive revealed as ancient tech
├── First Adaptation Fragments discovered
└── Haven discovered (possibly)

LATE GAME (Hours 10-16)
├── Faction ship variants available at rep 75+
├── Haven Lab online — begin T3 research
├── First T3 modules fabricated (exotic matter sustain begins)
├── Ancient ship hulls discovered (Seeker in Phase 3, Bastion in Phase 3)
├── Fragment resonance pairs explored at Haven Tier 4
├── Pentagon Revelation (~Hour 15) — equipment sustain IS the cage
└── Choice: which faction's equipment ecosystem do you invest in deeply?

ENDGAME (Hours 16-20)
├── Threshold discovered (Phase 4, requires Metric Drive Core)
├── Full T3 loadout possible but exotic matter sustain constraining
├── Endgame path crystallizes through equipment choices:
│   ├── Reinforce: Bastion + Concord/Weaver T2 + Containment-compatible T3
│   ├── Naturalize: Threshold + Valorin/Chitin T2 + Accommodation T3
│   └── Renegotiate: Seeker or Threshold + Communion T2 + Dialogue Protocol fragment
└── Equipment loadout physically embodies your political/philosophical choice
```

### The Masterwork Moment

The pentagon revelation (~Hour 15) recontextualizes EVERYTHING about equipment:

The player realizes their T2 module sustain costs are ENGINEERED to maintain the dependency ring. Concord modules need composites from Weavers. Weaver modules need electronics from Chitin. The thread infrastructure suppresses geological processes that would allow faction self-sufficiency. Your carefully built equipment loadout — which you've spent 15 hours optimizing — IS participation in the cage.

And then the fracture module adapts. In fracture space, a Communion station produces its own food. The pentagon ring doesn't apply there. The ancient Accommodation faction's technology was designed for FREEDOM from the ring.

This means the player's endgame equipment choice has philosophical weight:
- **Reinforce path**: Keep using containment-origin T2 modules. They're powerful, reliable, well-tested. But the cage remains
- **Naturalize path**: Shift to Accommodation T3 modules. Break the pentagon sustain chains. Freedom — but the existing trade infrastructure (which feeds billions) collapses
- **Renegotiate path**: Use the Dialogue Protocol fragment to ask the instability itself for a third option. The most uncertain, most revelatory path

---

## Part 6b: Heat System Integration

> **Cross-reference:** `ship_modules_v0.md` → Heat System, `CombatFeel.md` → Heat Gauge HUD

The heat system defined in `ship_modules_v0.md` (Phase 2 priority) interacts directly with
faction equipment. Every weapon and engine module generates heat during operation, but the
current T2 module catalogs above specify **power draw** without specifying **heat generation**.

**Design requirement**: When the heat system is implemented, every T2 weapon and engine
module needs a `HeatPerShot` (weapons) or `HeatRate` (engines) value. These values should
reflect faction identity:

| Faction | Heat Philosophy | Rationale |
|---------|---------------|-----------|
| **Concord** | Moderate, predictable (Zero Variance applies to heat too) | Institutional reliability — no thermal surprises |
| **Chitin** | Low baseline, adaptive (heat profile shifts under sustained fire) | Metamorphic cooling — Chitin equipment adapts thermally |
| **Weavers** | Low (structural heat sinks integrated into silk lattice) | Patience — Weaver weapons fire slow and cool |
| **Valorin** | HIGH (antimatter = extreme heat, aggressive cooling) | Fearless engineering — run hot, fight fast, burrow before overheat |
| **Communion** | Very low (crystal resonance is thermally efficient) | Edge-dwelling tech — designed for sustained observation, not combat |

**T3 Relic weapons** have explicit heat philosophy in `ship_modules_v0.md`: "devastatingly
powerful AND devastatingly hot." Graviton Shear at 30 heat/shot vs Corvette 100 HCap = 3
shots before critical overheat. The Dreadnought's 300 HCap is the reason it exists.

This is a **future implementation gate** — heat values will be specified when the heat system
is built. The faction heat philosophies above are design targets for that work.

---

## Part 7: Cover-Story Naming Compliance

All equipment follows cover-story naming discipline per NarrativeDesign.md:

**Before Module Revelation (~Hour 8)**:
- Fracture drive is "Structural Resonance Engine"
- All T3 modules described as "experimental" or "prototype"
- Ancient ships are "unidentified hulks" until analyzed
- Haven is "anomalous stable zone" until docked

**After Module Revelation**:
- True names revealed: "Adaptation Drive", "Accommodation Hull", "Containment Architecture"
- T3 modules use their real names (Graviton Shear, Void Sail, etc.)
- Ancient ships identified by faction of origin (Containment Bastion, Accommodation Seeker)

**CI enforcement**: grep-based lint on all player-facing strings per existing naming discipline.

---

## Summary: Module Count

| Category | Count | Status |
|----------|-------|--------|
| T1 Standard modules | 10 | IMPLEMENTED |
| T2 Faction modules (5 factions x 8) | 40 | REDESIGN (26 exist, poorly assigned) |
| Signature unique modules (5 factions x 2) | 10 | NEW |
| Faction ship variants (12 total) | 12 | NEW |
| T3 Relic modules | 11 | DESIGNED in ship_modules_v0.md (see cross-check note below) |
| Ancient ship hulls | 3 | NEW (Bastion = existing Dreadnought) |
| Universal Tech Leads | ~15-20 | NEW |
| Faction research techs (5 x 3) | 15 | NEW |
| Named/Legendary T3 variants | 4-8 | DESIGNED in ship_modules_v0.md |
| **Total module/equipment items** | **~120-130** | |

---

## Part 8: Module Naming Overhaul

### Design Principle

Every module gets a lore-grounded name that communicates WHAT IT IS, not what version number it is.

Reasoning (from EVE Online's tiericide, Starsector's faction naming, The Expanse's functional naming):
- Raw numerical suffixes (Mk1/Mk2/Mk3) feel like placeholder labels, not in-universe designations
- If Module B is T2, it should sound like a DIFFERENT THING than Module A at T1, not "Module A but bigger"
- Faction modules should be identifiable by name alone — a player should guess the faction from the name
- Named variants (T3/Legendary) use proper nouns with implied histories

### Naming Conventions by Faction

| Faction | Naming Culture | Pattern | Examples |
|---------|---------------|---------|----------|
| **Concord** | Military designation. Institutional, standardized. Sounds like NATO equipment names | [Adjective] + [Function] + [Type] | "Garrison Shield Array", "Patrol Interceptor Grid", "Standard Logistics Module" |
| **Chitin** | Probabilistic, compound words from insectoid biology. Bet/odds language | [Biology term] + [Function word] | "Molt Shield", "Compound-Eye Scanner", "Swarm Probability Core", "Chitin Odds-Scatter" |
| **Weavers** | Textile/construction metaphors. Patient, structural, load-bearing | [Silk/Weave term] + [Structural term] | "Dragline Reinforcement", "Tensile Hull Wrap", "Spindle Tractor", "Load-Bearing Strut" |
| **Valorin** | Short, aggressive, clan-style. Animal/predator references | [Predator/Clan word] + [Action word] | "Burnclaw Drive", "Fang Cannon", "Cache Burrow", "Packrunner Thruster" |
| **Communion** | Experiential, sensory, present-tense. What things FEEL like | [Sensory word] + [Phenomenon] | "Shimmer Resonator", "Drift Lens", "Phase-Whisper Scanner", "Current Reader" |
| **Universal T1** | Hard sci-fi functional. The Expanse style — engineer names, not marketing names | [Technology] + [Function] | "Coilgun Turret", "Pulse Laser", "Ion Thruster", "EM Deflector" |
| **T3 Relic** | Ancient, evocative, slightly alien. Hints at physics beyond current understanding | [Physics concept] + [Poetic noun] | "Graviton Shear", "Void Sail", "Phase Matrix", "Null-Mass Lattice" |
| **Named/Legendary** | Proper nouns. Imply a history, a builder, a story | "[Name] of [Place/Person]" or poetic phrase | "Tide of Esh'kara", "Last Light of Meridian", "The Flickering" |

### T1 Module Rename Table (Current Code → New Name)

| Current Code ID | Current Display | New Display Name | New Lore Name Rationale |
|-----------------|----------------|-----------------|------------------------|
| weapon_cannon_mk1 | Cannon Mk1 | Coilgun Turret | Electromagnetic coil accelerator — functional engineering name |
| weapon_laser_mk1 | Laser Mk1 | Pulse Laser | Pulsed infrared laser — physics-accurate |
| weapon_cannon_mk2 | Cannon Mk2 | Heavy Coilgun | Same technology, larger caliber — not a "version 2" |
| weapon_laser_mk2 | Laser Mk2 | Beam Laser | Continuous-wave laser — different firing mode, not just "better" |
| shield_mk2 | Shield Mk2 | EM Deflector | Electromagnetic deflection field — describes what it does |
| engine_booster_mk1 | Engine Booster Mk1 | Thrust Augmentor | Chemical reaction booster — supplemental, not replacement |
| engine_mk2 | Engine Mk2 | Fusion Torch | Deuterium-tritium magnetic confinement — different technology tier |
| scanner_mk2 | Scanner Mk2 | Phased Array Radar | Active phased array — specific sensor technology |
| hull_plating_mk2 | Hull Plating Mk2 | Composite Bulkhead | Layered composite armor — material science name |
| cargo_bay_mk2 | Cargo Bay Mk2 | Modular Hold | Standardized container system — functional |

### Concord T2 Rename Table

| Current Generic Name | New Faction-Voiced Name | Rationale |
|---------------------|------------------------|-----------|
| Shield Matrix T2 | Garrison Shield Array | Military installation naming — "Garrison" = defensive posture |
| Deflector Shield T2 | Patrol Deflector Screen | "Patrol" reflects coast guard identity |
| Point Defense Array T2 | Interceptor Grid | NATO-style functional designation |
| Hull Plating T2 | Regulation Composite Plate | "Regulation" = institutional standard |
| Damage Control T2 | Emergency Restoration Unit | Bureaucratic name for damage control |
| Cargo Expander T2 | Standard Logistics Module | Institutional, modular, standardized |
| Compact Tokamak T2 | Fleet Reactor Assembly | Military reactor designation |
| Fusion Torch T2 | Standard Patrol Drive | The institutional default — "Standard" IS the brand |

### Chitin T2 Rename Table

| Current Generic Name | New Faction-Voiced Name | Rationale |
|---------------------|------------------------|-----------|
| Adaptive Shield T2 | Molt Barrier | Metamorphosis — the shield "molts" to adapt |
| Deep Scanner T2 | Compound-Eye Array | Insectoid biology → multi-spectrum vision |
| ECM Suite T2 | Pheromone Scatter | Chemical signal masking → electronic warfare |
| Ion Drive T2 | Flicker Drive | Quick, efficient, darting movement |
| Ion Steering T2 | Swarm Vectoring Unit | Swarm movement patterns → extreme maneuverability |
| Point Defense Array T2 | Metamorphic Turret | Adaptive fire control — learns from misses, transforms targeting pattern |
| Cargo Concealer T2 | Scent Mask Hold | Pheromone-based hiding → cargo concealment |
| Sensor Jammer T2 | Chitin Dissonance Field | Active jamming that metamorphoses its frequency pattern over time |

### Weaver T2 Rename Table

| Current Generic Name | New Faction-Voiced Name | Rationale |
|---------------------|------------------------|-----------|
| SiC Composite Armor T2 | Dragline Plate | Dragline silk = strongest spider silk type |
| Reactive Plating T2 | Tensile Reactive Weave | Hardens on impact like spider silk under stress |
| Repair Module T2 | Mending Strand Drone | Silk repair strands — slow, thorough |
| Engine Shroud T2 | Aft Cocoon | Protective silk wrapping around engines |
| Fusion Gyro T2 | Web Pivot Drive | Reaction wheel that snaps like a spider pivoting on web |
| Hull Nanite T2 | Load-Bearing Strut | Structural reinforcement — the term they'd actually use |
| Magnetic Grapple T2 | Spindle Tractor | Web-strand tractor beam |
| Gauss Cannon T2 | Ambush Driver | Patient, devastating — ambush predator weapon |

### Valorin T2 Rename Table

| Current Generic Name | New Faction-Voiced Name | Rationale |
|---------------------|------------------------|-----------|
| Antimatter Catalyst T2 | Burnclaw Drive | Short, aggressive, animalistic |
| Antimatter Vectoring T2 | Snapjaw Vectoring | Quick direction changes — jaw-snap movement |
| Autocannon T2 | Fang Repeater | Volume of fire, cheap, aggressive |
| Railgun T2 | Clan Railgun | Simple, direct — "Clan" prefix marks ownership |
| Hardened Shield T2 | Burrow Barrier | Dark-burrow adaptation → defensive instinct |
| Plasma Engine T2 | Packrunner Drive | Pack animal speed — compromise drive |
| Expanded Hold T2 | Hoard Bay | Hoarding instinct → cargo expansion |
| Scanner Array T2 | Tunnel-Sense Scanner | Dark-burrow adapted sensors — finds hidden things |

### Communion T2 Rename Table

| Current Generic Name | New Faction-Voiced Name | Rationale |
|---------------------|------------------------|-----------|
| Gravimetric Sensor T2 | Depth Tremor Sensor | Experiential — you FEEL the gravitational ripples |
| Navigation Resonator T2 | Current Reader | Reads spacetime currents — present-tense, sensory |
| Passive IR Array T2 | Quiet Eye | Sees without being seen — poetic, minimal |
| Warp Engine T2 | Shimmer Drive | Runs better in Shimmer (Phase 1) space |
| Fuel Scoop T2 | Star Drinker | Harvests energy — experiential, almost reverent |
| Crystal Resonance Chamber T2 | Phase-Lock Cradle | Crystals cradled in phase-locked stability |
| EM Deflector T2 | Drift Shield | Shield that improves in unstable space |
| Drift Lens T2 | Revelation Lens | Sees deeper — communion with discovery |

---

## Part 9: T2 Lateral Trade-offs

### Design Principle

T2 modules are NOT strictly better than T1. They have a higher CEILING but also a higher FLOOR.

From EVE Online's tiericide and Starsector's ordnance point system: if Module B is strictly better than Module A in all situations, Module A should not exist. T1 modules must remain viable for specific builds.

### The Trade-off Axes

Every T2 module is more powerful than its T1 equivalent BUT:

| Trade-off Axis | T1 Advantage | T2 Advantage | Design Intent |
|---------------|-------------|-------------|---------------|
| **Power Draw** | Low (3-8) | High (10-20) | Tight power budgets favor T1. Shuttle/Corvette can't run all T2 |
| **Sustain Cost** | Minimal or zero | 1-3 goods/cycle | T1 is self-sufficient, T2 requires supply chains |
| **Mass** | Light | Heavy (+20-40%) | T2 reduces speed/turning on small ships |
| **Install Time** | Fast (3-6 ticks) | Slow (8-15 ticks) | T1 swaps quickly, T2 commits you |
| **Faction Rep** | None | 25-75 required | T1 is universal, T2 locks you into faction relationships |
| **Credit Cost** | Cheap (50-90) | Expensive (150-400) | T1 is replaceable, T2 is an investment |

### Example Build Scenarios Where T1 Wins

**"The Smuggler" (Clipper, tight power budget)**:
- 4 slots, 35 power. Needs: engine (speed), cargo concealer (smuggling), sensor (avoiding patrols), weapon (self-defense)
- T2 loadout attempt: Flicker Drive (12 power) + Scent Mask Hold (8 power) + Compound-Eye Array (10 power) + Fang Repeater (14 power) = 44 power. OVER BUDGET by 9
- T1 loadout: Pulse Laser (8) + Thrust Augmentor (8) + Coilgun Turret (5) + EM Deflector (5) = 26 power. Under budget with room to spare. Less flashy, but WORKS

**"The Frontier Trader" (Corvette, far from supply lines)**:
- Trading in remote systems, far from faction stations. Can't resupply easily
- T2 weapons consume 2 munitions/cycle. Player can't maintain the supply chain
- T1 Coilgun (1 metal sustain or zero if sustain enforcement is soft) keeps working indefinitely
- T1 is the "reliable workhorse" — boring but never fails you

**"The Early Game Explorer" (Shuttle, 3 slots, 20 power)**:
- Only 3 slots and 20 power. T2 modules are literally too power-hungry
- T1 is the ONLY option for small ships. This is progressive disclosure — new players with Shuttles only see T1 options

### T1 Sustain Philosophy

T1 modules should have MINIMAL sustain cost:
- T1 Weapons: 0-1 goods per cycle (Coilgun: 1 metal, Pulse Laser: 1 fuel)
- T1 Defense: 0 goods (passive armor/shields — just materials in the hull)
- T1 Engines: 0-1 goods (Thrust Augmentor: 1 fuel)
- T1 Utility: 0 goods (passive sensors, basic repair)

This ensures the early game is NOT about supply chain management — the player is learning to trade, not worrying about module sustain. Sustain pressure arrives with T2 modules as a mid-game complexity layer.

---

## Part 10: Wear Traits (Salvaged Module System)

### Design Principle

Modules found in derelicts, ruins, and combat salvage should have permanent quirks that make them unique — not just "free modules." Inspired by Starsector's d-mod system, which turns stat penalties into narrative and creates the "junker fleet" playstyle.

### How Wear Traits Work

When a module is FOUND (not purchased), it has a chance to carry 1-2 Wear Traits — permanent modifications that cannot be removed. Wear Traits are rolled deterministically from the discovery hash (same discovery always yields same traits).

**Trait Generation Rules**:
- Modules purchased from faction stations: NEVER have Wear Traits (factory fresh)
- Modules salvaged from combat: 60% chance of 1 trait, 20% chance of 2 traits
- Modules found in derelicts: 80% chance of 1 trait, 40% chance of 2 traits
- Modules found in ancient ruins: 30% chance of 1 trait (ancient tech was built better)
- T3 Relic modules: NEVER have Wear Traits (they're grown, not manufactured)

### Wear Trait Catalog

#### Negative Traits (reduce stats, provide economy benefits)

| Trait | Stat Effect | Compensating Benefit | Flavor |
|-------|-----------|---------------------|--------|
| **Corroded** | -15% effectiveness (damage/shield/speed) | -30% sustain cost | Worn but efficient — the unnecessary parts have rusted away |
| **Power-Hungry** | +25% power draw | +10% effectiveness | Draws more than rated — but runs hot and hits hard |
| **Fragile** | Module disabled when hull < 40% (normally 25%) | -20% mass | Lightweight but delicate — breaks under stress |
| **Intermittent** | 10% chance per tick to not fire/activate | -40% sustain cost | Faulty contacts — works most of the time. Basically free to run |
| **Loud** | +20% scan signature when active | +15% effectiveness | Unmistakable energy signature — powerful but visible |
| **Sluggish** | -20% tracking speed (weapons) or -10% turning bonus (engines) | -25% power draw | Worn bearings, loose tolerances. Power-efficient though |

#### Positive Traits (rare, found only in specific locations)

| Trait | Stat Effect | Found In | Flavor |
|-------|-----------|---------|--------|
| **Overclocked** | +20% effectiveness | Combat salvage from elite NPC ships | Previous owner pushed it past spec |
| **Jury-Rigged** | Fits in any slot type (utility in weapon slot, etc.). Module retains its ORIGINAL type for synergy counting, power draw, and sustain. Cannot override the ship's LAST slot of a given type (e.g., can't put a utility in the only weapon slot) | Derelicts in Phase 2+ space | Someone needed it to work where it shouldn't |
| **Ancient Calibration** | +15% effectiveness in Phase 2+ space, -10% in Phase 0 | Ancient ruins only | Calibrated for unstable metrics. Struggles with stability |
| **Veteran** | Immune to module degradation from zone damage | Combat salvage from 5+ kill NPC ships | This module has been through hell and doesn't flinch |
| **Resonant** | When installed near an Adaptation Fragment, fragment effect +10% | Phase 3+ anomalies only | Something about its material responds to accommodation geometry |

### Why Wear Traits Create Great Gameplay

1. **Stories**: "I found this Corroded Clan Railgun in a derelict near Weaver space. It barely works, but it costs almost nothing to run. Named it 'The Rusty Fang.' It's killed 14 ships"
2. **Decisions**: Do I use the Intermittent Garrison Shield Array (saves sustain) or buy a clean one (reliable)?
3. **Identity**: Your ship's loadout becomes unique. No two players have the same combination of Wear Traits
4. **Economy**: Corroded/Intermittent modules let budget-conscious players run "junker builds" — cheap, unreliable, but functional
5. **Discovery value**: Even finding a T2 module you already own is interesting if it has different Wear Traits

---

## Part 11: Research Eureka Moments

### Design Principle

Research should be accelerated by GAMEPLAY ACTIONS, not just passive timers. Inspired by Civilization VI's boost system: completing a related in-game action provides 40% progress toward a technology.

### How Eureka Moments Work

Each researchable technology has 1-2 associated gameplay conditions. When the player fulfills a condition during active research, they receive a **Research Boost** — 40% of the total research time completed instantly.

Eureka moments:
- Can only trigger ONCE per technology per research session
- Only trigger if the technology is currently being researched
- Show a toast notification: "Eureka! [Action] advanced [Tech] research by 40%"
- Are tracked in the research UI so the player can see what actions would help

**Eureka-to-Pillar mapping**: Eurekas apply to the research pillar where that technology lives:
- **Universal Tech Eurekas** → Pillar 2 (Field Research). These techs are researched at any station from Tech Leads
- **Faction Tech Eurekas** → Pillar 1 (Faction Labs). These techs are researched at faction stations with rep
- **Haven/T3 Eurekas** → Pillar 3 (Haven Research). These techs are researched at Haven Lab only

### Eureka Catalog

#### Universal Tech Eurekas

**Design rule**: The player sees the CATEGORY of action that helps ("Long-distance travel") but NOT the specific threshold. Eurekas fire as pleasant surprises, not checkboxes. The UI shows: "Boost: Extended travel" not "Boost: Travel 15+ systems."

| Technology | Eureka Condition (HIDDEN from player) | Player-Visible Hint | Why It Makes Sense |
|-----------|--------------------------------------|--------------------|--------------------|
| Engine Efficiency | Travel 15+ systems without docking | "Extended travel" | Long-distance travel teaches fuel optimization |
| Sensor Suite | Scan 5 discovery sites | "Active scanning experience" | Scanning experience improves sensor design |
| Reinforced Hull | Survive a combat at <20% hull | "Combat survival under stress" | Near-death teaches you what breaks first |
| Advanced Refining | Sell 500+ credits of refined goods in one trade | "Profitable refining trade" | Understanding the market teaches refining priorities |
| Weapon Calibration | Win 3 combats without losing shields | "Flawless combat execution" | Perfect combat execution teaches precision |
| Fracture Drive | Complete 10 fracture jumps | "Cumulative fracture experience" | Fracture exposure builds understanding |
| Trade Network | Establish trade at 8+ different stations | "Broad trading network" | Network building teaches network tech |
| Planetary Landing | Visit 5 different planet types | "Diverse planetary survey" | Atmospheric diversity teaches landing adaptations |

#### Faction Tech Eurekas

**Same rule applies**: Player sees the hint, not the threshold. Eurekas fire naturally during aligned gameplay.

| Faction | Technology | Eureka Condition (HIDDEN) | Player-Visible Hint | Why It Makes Sense |
|---------|-----------|--------------------------|--------------------|--------------------|
| Concord | Shield Harmonics | Absorb 200+ shield damage in one combat | "Heavy shield engagement" | Shield stress-testing |
| Concord | Fleet Coordination | Complete 5 escort program runs | "Escort operations" | Escorting teaches coordination |
| Concord | Standardized Logistics | Refit at 3 different Concord stations | "Multi-station refit" | Experiencing the standard across locations |
| Chitin | Probability Modeling | Win 3 trades with >50% margin | "Speculative trading" | Successful speculation teaches probability |
| Chitin | Metamorphic Plating | Survive Phase 1+ space for 100 ticks | "Extended Shimmer exposure" | Prolonged instability teaches adaptation |
| Chitin | Information Arbitrage | Buy low/sell high on same good within 20 ticks | "Rapid arbitrage" | Arbitrage teaches information flow |
| Weavers | Tensile Optimization | Have all 4 zone armors above 50% at combat end | "Balanced armor discipline" | Demonstrating structural balance |
| Weavers | Silk Integration | Install 3+ Weaver modules on one ship | "Full Weaver integration" | Integration experience |
| Weavers | Structural Memory | Repair from <30% hull to full at a station | "Complete structural repair" | Understanding repair processes |
| Valorin | Swarm Logistics | Run 3+ escort programs simultaneously | "Multi-fleet coordination" | Multi-fleet management |
| Valorin | Frontier Cartography | Visit 10+ unexplored systems | "Frontier exploration" | Frontier exploration |
| Valorin | Burrow Instinct | Escape a combat after dropping below 20% hull | "Survival under pressure" | Near-death escape validates the instinct |
| Communion | Metric Sensitivity | Spend 200+ ticks in Phase 1+ space | "Prolonged edge dwelling" | Extended instability exposure |
| Communion | Crystal Attunement | Harvest exotic crystals at 3+ sites | "Crystal harvesting" | Crystal handling experience |
| Communion | Threshold Sense | Survive 2 topology shifts in Phase 2+ space | "Instability survival" | Direct topology experience teaches anticipation |

#### Haven/T3 Eurekas

| Technology | Eureka Condition (HIDDEN) | Player-Visible Hint | Why It Makes Sense |
|-----------|--------------------------|--------------------|--------------------|
| Metric Drive Core | Travel in Phase 3 space for 50 ticks | "Deep fracture experience" | Understanding extreme instability |
| Void Sail | Find 2 Navigation-category fragments | "Navigation fragment study" | Cartographic knowledge enables sail design |
| Null-Mass Lattice | Lose a zone armor in combat and win anyway | "Combat under structural failure" | Understanding what breaks teaches what shouldn't |
| Phase Matrix | Survive 5 hits in Phase 2+ space | "Instability combat exposure" | Phase exposure teaches phase shielding |
| Graviton Shear | Destroy a Lattice Drone | "Lattice technology analysis" | Studying Lattice tech teaches gravitonic principles |
| Resonance Comm | Achieve rep 50+ with 3 factions | "Broad diplomatic experience" | Communication across cultures teaches resonance |

### Progressive Disclosure of Eurekas

- **Early game**: Player doesn't see eureka conditions. Research is just a timer. Clean, simple
- **After first eureka fires**: Toast explains the system. Research UI now shows "Boost conditions" for active research
- **Mid game**: Player begins planning activities around research agenda — "I need to explore Phase 1 space to boost my Communion tech"
- **Late game**: Eureka hunting becomes a meta-game. Which tech do I research before that Phase 3 expedition so I can earn the eureka?

This creates a beautiful loop: research motivates exploration, exploration yields tech leads for more research, research eurekas motivate specific gameplay activities.

---

## Part 12: T3 Relic Module Trade-offs

### Design Principle

T3 modules are ancient accommodation/containment technology — alien, powerful, and DANGEROUS. They are NOT simply "T2 but bigger." Best-in-class games (EVE's deadspace modules, Starsector's redacted tech) make top-tier equipment carry significant costs. T3 modules should feel like wielding something you don't fully understand.

### T3 Trade-off Axes

Every T3 Relic module carries ALL of these costs simultaneously:

| Trade-off | Effect | Design Intent |
|-----------|--------|---------------|
| **Extreme Power Draw** | 18-30 power per module. Often exceeds a Corvette's entire budget | Only large ships (Cruiser+, ancient hulls) can field multiple T3 modules. Small ships can run ONE at most |
| **Exotic Matter Sustain** | 1-3 exotic_matter per sustain cycle. Must be continuously sourced from fracture space | T3 loadouts demand ONGOING exploration. You can't just buy exotic matter at stations — you have to find it. Your endgame power requires endgame engagement |
| **Lattice Aggro Escalation** | Each installed T3 module adds +1 to your accommodation signature. At sig 3+, Lattice Drones treat you as hostile on sight, even in Phase 0 space | The containment system detects accommodation technology. More T3 = more danger. Creates a meaningful choice: how much ancient tech can you carry before the galaxy turns against you? |
| **Instrument Interference** | T3 weapons create metric distortion on your OWN ship. Scanner jitter +5% per active T3 weapon. Phase Matrix mitigates this | T3 weapons are so powerful they disturb your own instruments. You hit harder but see less clearly. Communion Metric Harmonics Array partially compensates — creating a cross-system synergy |
| **Incompatibility** | Containment-origin T3 modules conflict with Accommodation-origin T3 modules. Max 2 cross-faction T3 modules per ship. Beyond that, efficiency drops 30% per additional conflicting module | You cannot fully embrace BOTH ancient philosophies. Your T3 loadout reflects your endgame alignment |

### Example T3 Build Budget

**Threshold (Accommodation Cruiser, 80 power in Phase 2+)**:
- Void Sail (T3 engine): 22 power, 2 exotic_matter sustain, +1 accommodation sig
- Phase Matrix (T3 shield): 20 power, 1 exotic_matter sustain, +1 accommodation sig
- Graviton Shear (T3 weapon): 25 power, 2 exotic_matter sustain, +1 accommodation sig, +5% scanner jitter
- Total: 67/80 power. 5 exotic_matter/cycle. Accommodation sig 3 — Lattice Drones hostile. Scanner jitter +5%
- Remaining: 13 power for 6 remaining slots (T1/T2 modules only)

This loadout is DEVASTATING but demands: constant exotic matter supply, hostile Lattice Drones everywhere, degraded sensor performance. The player must weigh: is this T3 weapon worth the jitter? Do I need the Phase Matrix to survive the Lattice Drones I'm now attracting?

---

## Part 13: Faction Equipment Synergies

### Design Principle

Faction commitment should create emergent power beyond individual module stats. When you specialize in one faction's equipment, the whole becomes greater than the sum of parts. This rewards dedication while creating meaningful diversity between loadouts.

### Synergy Rules (1 per faction)

| Faction | Synergy Name | Trigger | Effect | Design Intent |
|---------|-------------|---------|--------|---------------|
| **Concord** | Institutional Standard | 3+ Concord T2 modules installed | +10% shield regen rate, -5% all sustain costs | Concord equipment is designed for interoperability. More Concord gear = better integration. The institutional advantage: standardization compounds |
| **Chitin** | Swarm Resonance | 3+ Chitin T2 modules installed | +15% scan range, evasion +5% | Chitin compound-eye technology cross-references between modules. More eyes = better composite image. Metamorphic synergy |
| **Weavers** | Structural Lattice | 3+ Weaver T2 modules installed | Repair drone effectiveness +50%, zone armor regen +1/30 ticks out of combat | Weaver modules integrate structurally. The silk web connects. More Weaver modules = stronger structural mesh |
| **Valorin** | Pack Instinct | 3+ Valorin T2 modules installed | +10% speed, Burrow Protocol cooldown -20% | Valorin systems are designed for speed-at-scale. Full Valorin loadout = maximum clan engineering synergy |
| **Communion** | Metric Harmony | 3+ Communion T2 modules installed | In Phase 1+ space: all module effectiveness +10%. In Phase 0: -5% | Communion crystal-organic technology harmonizes. More living crystal modules = deeper resonance. But ONLY in unstable space — stable space remains noise to them |

### Synergy Activation Rule

**Majority-based**: A faction synergy activates when that faction's modules are the **majority** (>50%) of your installed T2+ modules. A ship with 4 Weaver modules and 2 Chitin modules activates Weaver synergy. A ship with 2/2/2 split activates nothing. This scales naturally with ship size — a Corvette (5 slots) needs 3+ faction modules, a Cruiser (8 slots) needs 5+. Generalist builds are viable but don't get faction bonuses. Having 1-2 cross-faction modules for specific needs is never punished — you just need a clear majority.

### Why Synergies Work

1. **Build identity**: "I'm running a full Weaver fortress build" vs "I'm a Chitin scout build" — distinct playstyles
2. **Pentagon amplification**: More faction modules = more dependency on that faction's pentagon partner. Full Weaver build = very dependent on Chitin electronics
3. **Progressive discovery**: Players discover synergy by accident ("Wait, my scan range went up when I added the third Chitin module?") — Principle #1
4. **Loadout diversity**: Prevents "best in slot" thinking. The best weapon might be Valorin, but if you're running Weaver synergy, the Weaver weapon is better FOR YOUR BUILD

---

## Part 14: Design Clarifications

### Sustain Failure Behavior

When a module's sustain goods are unavailable at cycle time:

1. **Warning**: Toast notification 60 ticks before the sustain cycle: "Low [good] — [Module] performance will degrade next cycle"
2. **Degradation**: Module enters "degraded" mode — effectiveness reduced by 50% (damage, shield HP, speed bonus, scan range, etc.)
3. **NOT disabled**: Modules never fully shut off from sustain failure (fuel exhaustion is the only full-disable trigger). A degraded module is weak but functional
4. **Recovery**: Supplying the good on the NEXT cycle restores full effectiveness immediately
5. **Priority queue** (future): Player can rank modules by priority. When supply is limited, highest-priority modules get sustained first. Others degrade. This creates meaningful triage decisions: "Do I sustain my weapons or my scanner?"

Sustain cost AMOUNTS will be part of significant balancing work later. The current values in this document are design targets, not final numbers.

### NPC Fracture Drive Availability

**NPCs do NOT have fracture drives.** The fracture module is unique — grown over centuries from controlled metric bleed exposure, adapted to the player's specific ship. It cannot be reproduced (substrate gone, growth conditions lethal, measurement defeats itself per lore).

Faction-specific behavior in fracture-adjacent space:
- **Communion**: Operate naturally in Phase 1 (Shimmer) space without fracture drives. They're "edge-dwellers, not divers" — their conventional sensors are calibrated for Phase 1 instability. They CANNOT enter Phase 2+
- **Valorin**: Neurologically fearless, some scout at Phase 1 borders. Cannot enter Phase 2+
- **All factions**: Phase 2+ space is player-exclusive territory. This is what makes fracture exploration special — you're genuinely alone out there. NPC trade and patrol routes stay on thread-space lanes
- **Lattice Drones**: Not faction NPCs. These are ancient automated systems that operate in ALL phases. They're the only "NPCs" the player encounters in deep fracture space

This restriction is critical for the solo exploration fantasy and the Communion narrative ("You're not the first. Most don't survive.").

### Resource Extraction as Supply Pathway

The player can source sustain goods through **three pathways**, not just trade:

1. **Trade** (buy at faction stations): Fastest, simplest. Pentagon ring dependency applies — you need relations with the producing faction
2. **Extraction** (build resource operations): Player identifies resource-rich locations through exploration, then establishes extraction:
   - **Extraction stations**: Built at surveyed sites. Produce specific goods over time. Require initial investment (credits + components) and periodic maintenance
   - **Fleet extraction programs**: Assign fleet operations to extract at a site without building permanent infrastructure. Lower yield but mobile
   - **Survey quality matters**: Better sensors + tech → more accurate resource estimates → better extraction yield. Creates exploration→extraction→sustain loop
3. **Salvage/Discovery** (find goods in the world): Derelicts, combat salvage, anomaly loot. Unpredictable but free

The pentagon ring creates **pressure** toward faction trade, not a hard lock. A resourceful player who invests in extraction infrastructure can partially self-supply. But full self-sufficiency is extremely difficult by design — the ring is engineered to prevent it. Even in fracture space where the ring weakens, extraction requires significant exploration investment.

**Key design principle**: Extraction is the "long game" alternative to trade. It rewards exploration and investment but never fully replaces the pentagon ring. A player running full Weaver T2 modules needs electronics (from Chitin) — they can trade for it, extract it from the right sites, or some combination. The pentagon makes trading EASIER but doesn't make it MANDATORY.

---

## Open Questions for Iteration

1. **Research queue depth**: Should faction research be 1 concurrent per faction (5 total possible) or 1 concurrent across all factions? Former is more permissive but less pressure
2. **Tech Lead specificity**: Should tech leads be specific ("Propulsion Lead: Ion Efficiency III") or generic ("Propulsion Lead" → choose from available propulsion techs when researching)?
3. **Faction variant exclusivity**: Can you fly a Valorin Fang-class corvette while having Concord rep 75+? Or does faction variant purchase lock you into that faction's ecosystem?
4. **Ancient ship restoration**: Should restoration happen exclusively at Haven drydock, or could Weaver drydock (at max rep) also restore ancient hulls? The Weavers ARE structural engineers
5. **Power budget enforcement**: The design doc specifies modules with power draw but no enforcement exists. Should power enforcement be part of this design or a separate system gate?
6. **Engine line commitment**: Should a ship be limited to ONE engine philosophy line (can't mix Ion and Antimatter) or allowed to mix with diminishing returns?
7. **Synergy threshold tuning**: Is 3+ modules the right trigger for faction synergies? Too low makes it trivial; too high makes it impractical on small ships
8. **T3 accommodation signature**: Should the Lattice aggro threshold (sig 3+) scale with instability phase? More lenient in Phase 2+ (where Lattice is already hostile), stricter in Phase 0?

### Resolved Questions (from critical evaluation)

| Question | Resolution | Rationale |
|----------|-----------|-----------|
| Pentagon sustain enforcement | Every faction's T2 modules require their pentagon dependency good | Thesis demands it — without enforcement, "loadout IS trade policy" is hollow |
| Auto-trigger modules (Burrow, Silk Lattice) | Converted to active abilities with cooldowns | Narrative Principle #1 — player assembles, never receives. Agency over automation |
| Universal Mount Adapter | Replaced with Fleet Coordination Nexus (facilitator, not bypass) | UMA directly undermined the design thesis by bypassing faction rep |
| Structural Resonance Web permanence | Changed to destructive-removal (module destroyed on extraction) | Principle #6 — knowledge progression, not item lock-in. Freedom to choose |
| Pheromone Relay omniscience | Changed to delayed/spread info (imperfect information) | Omniscience destroys the casino metaphor. Imperfect info IS the casino |
| Chitin identity | "The Metamorphosis" not "The Calculation" | Lore says Adaptation through self-dissolution. Probability is a tool, metamorphosis is identity |
| T3 trade-offs | Added Part 12 — power draw, exotic sustain, Lattice aggro, instrument interference, incompatibility | Top-tier equipment must feel alien and costly, not just "T2 but bigger" |
| Ancient ship naming | Pre-revelation: "Unidentified Hulk" → analysis → player-named | Principle #1 — player assembles identity through discovery |
| Eureka thresholds | Hidden from player, category hints only | Checkboxes kill discovery. "Extended travel" not "15+ systems" |
| Haven market scope | Ancient goods only (exotic crystals, exotic matter, salvaged tech). NOT faction-produced goods | Pentagon ring integrity — Haven supplements, doesn't replace |
| Signature module sustain | All 10 signature modules include their faction's pentagon dependency good | Signature modules are the FACE of each faction's equipment — they must enforce the thesis |
| Chitin equipment niche | "Adaptive systems" not "probability manipulation" | Metamorphosis IS the identity; probability is a tool. Consistent with "The Metamorphosis" rename |
| Burrow Instinct research tech | Reworked from auto-retreat to warp charge +50% / evasion +25% at <20% hull | Player flies solo, not fleet — auto-retreat has no target. Solo-relevant mechanic |
| Communion tech rationale | Crystal-organic hybrid — living crystals need nutrient solutions, not "bio-organic humans" | Communion is Human species. The crystals are the living part, not the crew |
| Concord unique mechanic | Zero Variance — all Concord T2 effects are deterministic, no RNG | Every faction needs a unique mechanic. Concord's is the institutional guarantee |
| Synergy activation rule | Majority-based (>50% of T2+ modules) replaces 4+-faction anti-synergy | Scales with ship size, doesn't punish 1-2 cross-faction modules, intuitive math |
| Sustain failure behavior | Degradation to 50% effectiveness, NOT full disable. Warning 60 ticks before. Priority queue future | Modules should degrade gracefully, giving player time to respond |
| NPC fracture drives | NPCs do NOT have fracture drives. Communion operates in Phase 1 only. Phase 2+ is player-exclusive | Fracture module is unique per lore. Solo exploration fantasy requires player-only territory |
| Resource extraction | Three supply pathways: trade, extraction (stations/fleet ops), salvage. Pentagon creates pressure, not hard lock | Player agency — self-sufficiency is difficult but possible with exploration investment |
| Eureka-to-pillar mapping | Universal→Pillar 2, Faction→Pillar 1, Haven→Pillar 3 | Each eureka applies to the pillar where its technology is researched |
| Jury-Rigged wear trait | Module retains original type for synergy/power/sustain. Cannot override last slot of a type | Prevents build-breaking edge cases while preserving creative fitting |
| Electronics two-hop chain | Intentional design. Communion crystals → Chitin electronics → Weaver sustain. Documented in trade_goods and factions_and_lore | Creates cascading economic pressure (Pillar 3), makes Communion strategically essential, motivates fracture exploration |
| Communion secondary dependencies | Intentional asymmetry. 2 modules need rare_metals (Valorin) or electronics (Chitin) beyond pentagon food | Communion as cultural bridge — broadest supply needs reflects edge-dweller identity. Most diplomatically demanding faction choice |
| Heat system integration | T2 modules need HeatPerShot/HeatRate values when heat system is implemented. Faction heat philosophies defined in Part 6b | Heat system is Phase 2 in ship_modules. Faction identity should extend to thermal profiles |
| Cover-story naming in combat | CombatFeel.md updated with naming compliance section. Combat HUD must use cover-story names pre-revelation | Single leaked "fracture" or "adaptation" term in combat UI destroys Module Revelation impact |
| Battle Stations module timing | CombatFeel.md updated with module activation timing during 2s spin-up. Turrets wait; shields/abilities immediate | Spin-up vulnerability window rewards ambush tactics (Weaver identity). Spinal weapons fire during spin-up (capital ship advantage) |
| Fragment → T3 cross-check | All 11 T3 modules verified against ship_modules. "Graviton Tether" was typo for Graviton Shear. Count corrected 13→11 | Every T3 module has at least one fragment path |
| Galaxy generation resource alignment | Each faction's territory must contain resources for their pentagon ring production good. Added to trade_goods open questions | Without this constraint, pentagon ring can break at world generation |
| Seeker scan range | 150 = 5x Clipper (30), 3x Frigate (50). Contextualized in stats table | Raw numbers need baselines to be meaningful |
| Exotic matter income rates | Design targets added to trade_goods. Phase 2: 1-3/discovery, Phase 3: 3-6, Phase 4: 5-10. Haven Tier 3 achievable ~Hour 14-16 | Haven upgrade costs need validated income expectations |
