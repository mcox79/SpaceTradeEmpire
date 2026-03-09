# Ship Modules & Technology Design — V0

Status: DESIGN LOCKED — Phase 1 Implemented (Tranche 17-18)
Date: 2026-03-06

## Implementation Phases

### Phase 1 — Implemented (Tranche 17-18)
- 8 ship classes with full stat blocks (ShipClassContentV0.cs) ✅
  - Shuttle (3 slots), Corvette (5), Clipper (4), Frigate (6), Hauler (4), Cruiser (8), Carrier (7), Dreadnought (10)
- Zone armor: 4-directional HP with 3-layer damage routing Shield→Zone→Hull (CombatSystem.CalcDamageWithZoneArmor) ✅
- Combat stances: Charge/Broadside/Kite per class (CombatSystem.DetermineStance) ✅
- 10 starter modules in UpgradeContentV0.cs ✅
  - Weapons: Cannon Mk1/Mk2, Laser Mk1/Mk2
  - Defense: Shield Mk2, Hull Plating Mk2
  - Engines: Engine Booster Mk1, Engine Mk2
  - Utility: Scanner Mk2, Cargo Bay Mk2
- Basic fitting: slot count constraint enforced ✅
- PowerDraw field on modules, BasePower on ship classes ✅ (schema only — no enforcement)
- SustainInputs dict on modules ✅ (schema only — resources never consumed)
- Weapon family bonuses: Kinetic 150% vs Hull / Energy 150% vs Shield / PD +200% vs Missile ✅
- Strategic resolver: fleet-vs-fleet with stance-based zone targeting (StrategicResolverV0.cs) ✅

### Phase 2 — Next Priority
- Power budget enforcement (sum of PowerDraw ≤ BasePower; modules cannot activate over budget)
- Sustain consumption system (goods consumed per 60-tick cycle from fleet cargo/station reserves)
- Mount types: Standard (360°), Broadside (120° +30% dmg), Spinal (60° +50% dmg)
- Module degradation from zone/hull damage (engines degrade when aft zone depleted, etc.)
- T2 Military modules: Railgun, FEL, Particle Beam, Torpedo, Reactive Plating, Fusion Torch, etc.
- Slot layout breakdown per class (weapon/engine/utility/cargo/bay slot counts)
- Sustain starvation: 50% power reduction when supply insufficient + 20% safety floor

### Phase 3 — Aspirational
- T3 Precursor modules (discovery-only, exotic matter sustain, cannot be manufactured)
- Missile self-fabrication system (Magazine size, FabRate, in-combat vs peace-time production)
- Drone system (bay slots on Carrier/Dreadnought, Interceptor/Strike/Salvage drone types)
- Electronic warfare (Cargo Siphon, System Disruptor, Hull Cracker, Neural Override)
- Named/Legendary variants (Precursor artifacts with unique modifiers)
- Stat-check boarding (hull < 25% + Boarding Module → crew check → enhanced loot)
- Mining fleet contracts (NPC miner delegation, not direct player activity)

---

## Design Pillars

1. **Philosophy over tiers** — Ion/Fusion/Antimatter are choices, not a ladder
2. **Multi-constraint fitting** — Slots + Power Budget + Sustain Cost
3. **Directional combat** — Zone armor makes facing matter; protect your engines
4. **Sustain = economy** — Modules consume resources, not credits. Your fleet loadout is constrained by your supply chains
5. **No fixed guns** — All turrets (360° coverage). Mount types (Broadside/Spinal) add arc restrictions + damage bonuses on specific ships
6. **Self-fabricating missiles** — No resupply trips. Magazine + fab rate + sustain handles ammo
7. **Three tech tiers** — T1 Standard (purchasable), T2 Military (faction/rare materials), T3 Precursor (found, not built)

---

## Technology Tiers

> **Implementation Note**: T1 modules are implemented (10 in UpgradeContentV0.cs).
> T2 and T3 modules are Phase 2/3 respectively — no faction-locked or discovery-only
> modules exist yet. All current modules are tier-agnostic.

| Tier | Name | Source | Sustain Cost | Flavor |
|------|------|--------|-------------|--------|
| T1 | Standard | Manufactured, purchasable | Common goods (metal, fuel) | Near-future fusion era |
| T2 | Military | Faction reputation / rare materials | Uncommon goods (composites, rare metals) | Far-future, physics-respecting |
| T3 | Precursor | Found in anomalies/ruins, not craftable | Rare goods (exotic matter — exploration only, cannot be manufactured) | Ancient alien tech with theoretical physics basis |

---

## Core Ship Stats

| Stat | Description | Source |
|------|-------------|--------|
| Hull HP | Structural integrity. Universal pool. | Ship class base + hull armor modules |
| Shield HP | Energy barrier. Omnidirectional bubble. | Ship class base + shield modules |
| Zone Armor (×4) | Directional buffer: Fore, Port, Starboard, Aft | Ship class base + armor modules assigned to zones |
| Thrust | Linear acceleration / top speed | Engine modules (thrust-focused) |
| Turning | Rotational acceleration | Engine modules (steering-focused) |
| Mass | Affects accel and turning. Every module adds mass. | Ship class base + sum of installed module mass |
| Power Gen | Reactor output (budget ceiling) | Ship class base + reactor modules |
| Power Draw | Total energy consumption | Sum of active module draw |
| Cargo | Trade goods capacity | Ship class base + cargo modules |
| Scan Range | Detection radius | Ship class base + sensor modules |

### Stats NOT included (intentional cuts)

- **Heat** — Too much fitting complexity for a trading empire game. Power budget is sufficient as the energy constraint.
- **Drag** — Folded into thrust/mass relationship. Simpler.
- **Crew** — Cut for v0. Could add later for boarding gate.

---

## Health System — Three Layers

> ✅ **IMPLEMENTED** in CombatSystem.CalcDamageWithZoneArmor (14 tests passing).
> Shield → Zone Armor → Hull routing works exactly as specified below.

```
Shield (omnidirectional bubble)
  ↓ when depleted
Zone Armor (directional, per-face — determined by collision point)
  ↓ when facing zone armor depleted
Hull HP (universal single pool)
```

### Shield Behavior

- Omnidirectional — absorbs damage from any direction
- Regenerates out of combat only (slow passive regen)
- Exception: Shield Capacitor module (T2) enables slow in-combat regen at high power draw
- Kinetic weapons: 50% effectiveness vs shields
- Energy weapons: 150% effectiveness vs shields

### Zone Armor Behavior

- Four zones: Fore, Port, Starboard, Aft
- Each zone has independent armor HP
- When a zone's armor is depleted, hits to that face go straight to hull
- Zone armor does NOT regenerate in combat

### Zone Hit Detection

**Real-time combat (player):** Collision point on mesh, transformed to ship local space:

```
local_hit = ship.to_local(collision_point)
if local_hit.z > threshold   → Fore
if local_hit.z < -threshold  → Aft
if local_hit.x > 0           → Starboard
else                          → Port
```

**Strategic resolver (NPC-vs-NPC):** Class-based stance determines hit distribution:

| Ship Class | Default Stance | Hit Distribution |
|------------|---------------|-----------------|
| Shuttle | Evasive | 25% each zone |
| Corvette | Charge | 50% Fore, 20% Port/Stbd, 10% Aft |
| Clipper | Kite | 15% Fore, 15% Port/Stbd, 55% Aft |
| Frigate | Broadside | 15% Fore, 35% Port/Stbd, 15% Aft |
| Hauler | Evasive | 25% each zone |
| Cruiser | Broadside | 15% Fore, 35% Port/Stbd, 15% Aft |
| Carrier | Kite | 15% Fore, 15% Port/Stbd, 55% Aft |
| Dreadnought | Charge | 50% Fore, 20% Port/Stbd, 10% Aft |

### Module Degradation from Zone Damage

Only two module types are zone-sensitive:

| Module Type | Zone | Degradation triggers when... |
|-------------|------|------------------------------|
| Engines | Aft | Aft zone armor depleted |
| Sensors/Utility | Fore | Fore zone armor depleted |

Weapons degrade from overall hull HP only (below 25% hull).
Reactor degrades from overall hull HP only (below 25% hull).

### Degradation Curve

| Zone Armor State | Module Effectiveness |
|------------------|---------------------|
| 100–50% | 100% (full performance) |
| 50–1% | 70% (-30%) |
| 0% (breached) | 40% (-60%) |

| Hull HP State | Weapon/Reactor Effectiveness |
|---------------|------------------------------|
| 100–25% | 100% (full performance) |
| Below 25% | 50% (-50%) |

**HARD FLOOR: No module can drop below 25% effectiveness.** A degraded T2 module at 25%
should still be roughly comparable to a T1 module at full power — never worse than starter
equipment. This floor applies regardless of how many degradation sources stack.

---

## Ship Classes (8 Total)

### Progression Path

```
Shuttle → Corvette → Clipper / Frigate / Hauler (mid-tier sidegrades)
                        → Cruiser / Carrier (late-game)
                           → Dreadnought (endgame aspirational — Precursor find)
```

### Slot Layouts & Mount Types

| Class | Standard Mounts | Broadside Mounts | Spinal Mounts | Engine | Utility | Cargo | Bays | How to Get |
|-------|----------------|-----------------|--------------|--------|---------|-------|------|------------|
| Shuttle | 1 | — | — | 1 | 1 | — | — | Starter ship |
| Corvette | 2 | — | — | 1 | 2 | 1 | — | Purchase (cheap) |
| Clipper | 1 | — | — | 2 | 1 | 3 | — | Purchase |
| Frigate | 1 | 2 | — | 1 | 3 | 1 | — | Purchase (expensive) or faction rep |
| Hauler | 1 | — | — | 1 | 2 | 5 | — | Purchase |
| Cruiser | 2 | 2 | — | 2 | 4 | 2 | — | Faction rep + credits |
| Carrier | 2 | — | — | 1 | 3 | 2 | 2 | Faction rep + credits |
| Dreadnought | 2 | 2 | 2 | 3 | 6 | 4 | 2 | Found in Precursor ruin (1-2 per save) |

### Mount Types

> ❌ **NOT YET IMPLEMENTED** (Phase 2). No MountType field exists on ModuleSlot.
> All weapons currently fire as Standard (360°) without arc restrictions or damage bonuses.

| Mount | Arc | Damage Modifier | Found On |
|-------|-----|----------------|----------|
| Standard | 360° full tracking | Base damage | All ships |
| Broadside | 120° port OR starboard | +30% damage | Frigate, Cruiser, Dreadnought |
| Spinal | 60° forward cone | +50% damage | Dreadnought only |

### Base Zone Armor by Class

| Class | Fore | Port | Stbd | Aft | Core Hull | Shield | Identity |
|-------|------|------|------|-----|-----------|--------|----------|
| Shuttle | 15 | 10 | 10 | 10 | 40 | 20 | Fragile everywhere |
| Corvette | 25 | 20 | 20 | 15 | 60 | 35 | Balanced, thin aft |
| Clipper | 15 | 15 | 15 | 30 | 50 | 30 | Armored rear — built to run |
| Frigate | 35 | 25 | 25 | 15 | 70 | 40 | Armored fore — built to charge, then broadside |
| Hauler | 20 | 25 | 25 | 25 | 80 | 30 | Heavy core, even sides |
| Cruiser | 30 | 30 | 30 | 25 | 100 | 50 | Solid everywhere — fleet command ship |
| Carrier | 20 | 25 | 25 | 20 | 90 | 45 | Heavy core — protect the drone bays |
| Dreadnought | 50 | 45 | 45 | 35 | 150 | 80 | Thick everywhere. Aft is "thinnest" but still massive |

### Ship Class Identities

- **Shuttle**: Starter. Nimble, fragile, cheap. Can access restricted docking that larger ships cannot.
- **Corvette**: First real ship. Balanced fighter-trader. Jack of all trades.
- **Clipper**: Fast freighter. Outrun what you can't outfight. Armored aft for fleeing. Smuggler's choice.
- **Frigate**: Broadside brawler. Charge in with armored fore, swing broadside for +30% damage.
- **Hauler**: Maximum cargo. Slow, tough core, escort-dependent. The trade baron's ship.
- **Cruiser**: Multi-role flagship. Fleet command bonus — passive buff when escorted by player-owned ships. Good at everything, great at nothing alone.
- **Carrier**: Drone commander. Weak personal guns but deploys combat/salvage drones. Different playstyle.
- **Dreadnought**: Precursor find. 1-2 per save. Massive reactivation cost and sustain. Spinal mounts for devastating forward weapons. "What did the Precursors need this to fight?"

---

## Fitting Constraints

### 1. Slots (Physical Limit)

Each ship has a fixed number of weapon, engine, utility, and cargo slots. Mount types (Standard/Broadside/Spinal) are baked into the ship class definition. A module must match the slot type to be installed.

### 2. Power Budget (Energy Limit)

> ⚠️ **SCHEMA ONLY** (Phase 2). PowerDraw and BasePower fields exist but enforcement
> logic is not implemented. Modules over budget are not prevented or degraded.

- Each ship class has a base power generation value
- Reactor modules add to power generation
- Each installed module has a power draw value
- **Hard cap**: Total power draw cannot exceed total power generation
- If a module would put you over budget, it cannot be activated
- V1: Simple hard cap. No power profiles or toggling.

### 3. Sustain Cost (Economic Limit)

> ⚠️ **SCHEMA ONLY** (Phase 2). SustainInputs dict populated for weapon modules
> (Munitions, Fuel). No system consumes these resources per cycle. No degradation
> when supply insufficient. No 20% safety floor enforcement.

Sustain is **resource flow, not credits**. Each module consumes specific goods per cycle.

**Resource tiers:**

| Resource | Source | Used By |
|----------|--------|---------|
| Metal | Ore mines | T1 kinetic weapons, hull armor, basic engines |
| Fuel | Fuel refineries | T1 energy weapons, engines, power modules |
| Composite | Metal + Fuel (industrial) | T2 armor, shields |
| Rare Metal | Rare ore deposits | T2 weapons, advanced sensors |
| Exotic Matter | Anomaly exploration ONLY (cannot be manufactured) | T3 Precursor modules |

**Sustain mechanics:**

- Each module has a sustain recipe: `{resource: quantity}` consumed per cycle (default 60 ticks)
- Sustain draws from production flow first, then station reserves
- **Sustain CANNOT deplete reserves below safety threshold (20%)**
- Reserves are a buffer for supply chain disruption, not normal operations
- If production flow is insufficient AND reserve is at safety floor:
  - Module enters **reduced power** (50% effectiveness)
  - Module does NOT go fully offline unless reserve hits absolute zero (catastrophic failure)
- Tech research can reduce sustain costs (e.g., "Efficient Power Routing": -15% fuel for engines)

**Sustain creates the empire pressure:**

- T1 loadout: One ore mine + one fuel refinery. Easy.
- T2 loadout: Composite production chain (metal + fuel → composite). Real economy required.
- T3 loadout: Steady exotic matter from exploration. Dreadnought literally needs you to keep exploring Precursor ruins.

---

## Weapons

### Damage Families

| Family | vs Hull | vs Shield | Notes |
|--------|---------|-----------|-------|
| Kinetic | 150% | 50% | Slugs punch through armor, deflect off shields |
| Energy | 50% | 150% | Lasers/beams melt shields, scatter off hull |
| PointDefense | 100% | 100% | +200% vs Missile family weapons |
| Missile | 100% | 100% | Self-guided, interceptable by PD |
| Gravitonic (T3) | 150% | 150% | Ignores all conventional defense |
| Exotic (T3) | 200% | 100% | Antimatter — devastating to matter |

### Tracking Speed

All turrets have 360° base coverage (or mount-restricted arc). Within their arc, turrets vary by tracking speed:

| Tracking Class | Speed | Damage Mod | Best Against |
|---------------|-------|-----------|--------------|
| Fast-Track | High | -15% damage | Small/fast targets, missiles, drones |
| Standard | Medium | Base damage | General purpose |
| Heavy | Slow | +25% damage | Large/slow targets, capitals. Loses aim on fast ships. |

### Weapon Catalog

#### T1 — Standard

| Module | Family | Tracking | Damage | Magazine | Fab Rate | Sustain/Cycle | Flavor |
|--------|--------|---------|--------|----------|----------|---------------|--------|
| Coilgun Turret | Kinetic | Standard | 12 | — | — | 1 metal | EM coil slugs. Reliable workhorse. |
| Pulse Laser | Energy | Standard | 8 | — | — | 1 fuel | Pulsed IR. Low damage, consistent. |
| PDC Array | PD | Fast-Track | 6 | — | — | 1 metal | Rapid-fire 20mm CIWS. Keeps missiles off you. |
| Missile Pod | Missile | N/A (guided) | 20/missile | 8 | 1 per 15 ticks | 1 metal, 1 fuel | Guided munitions. Good burst opener. |

#### T2 — Military

| Module | Family | Tracking | Damage | Magazine | Fab Rate | Sustain/Cycle | Flavor |
|--------|--------|---------|--------|----------|----------|---------------|--------|
| Railgun | Kinetic | Heavy | 22 | — | — | 2 metal, 1 composite | Lorentz accelerator. Devastating in broadside mounts. |
| Free-Electron Laser | Energy | Standard | 15 | — | — | 2 fuel, 1 rare metal | Tunable wavelength. Ignores 30% hull armor. |
| Particle Beam | Energy | Heavy | 18 | — | — | 2 fuel, 1 rare metal | Near-c neutral particles. Bypasses EM shields. |
| Plasma Carronade | Kinetic | Standard | 25 | — | — | 2 fuel, 1 composite | MARAUDER plasma toroid. Short range (15u). Applies heat to target. |
| Torpedo Launcher | Missile | N/A (guided) | 40/torpedo | 4 | 1 per 30 ticks | 2 metal, 1 rare metal | Nuclear-pumped shaped charge. |
| Swarm Battery | Missile | N/A (guided) | 8/missile × 6 salvo | 18 | 3 per 15 ticks | 2 metal, 2 fuel | Fires in salvos of 6. Overwhelms PDC through volume. |
| Casaba Lance | Kinetic | Heavy | 35 | 2 | 1 per 40 ticks | 2 composite, 1 rare metal | Nuclear shaped plasma spear. Devastating. |

#### T3 — Precursor

| Module | Family | Tracking | Damage | Magazine | Fab Rate | Sustain/Cycle | Flavor |
|--------|--------|---------|--------|----------|----------|---------------|--------|
| Graviton Shear | Gravitonic | Standard | 30 | — | — | 1 exotic matter, 1 rare metal | Tidal gradient. Ignores ALL defense types. No known counter. |
| Annihilation Beam | Exotic | Heavy | 45 | — | — | 2 exotic matter | Antiproton stream. +100% vs hull. Best in spinal mount. |
| Void Lance | Energy | Standard | 35 | — | — | 1 exotic matter, 1 rare metal | Gamma-ray laser. Pierces shields entirely, hull-only damage. |
| Void Seekers | Missile | N/A (guided) | 50/missile | 6 | 1 per 10 ticks | 2 exotic matter | Phase through shields. PD cannot intercept. |

---

## Defense / Armor Modules

Armor modules are assigned to a **specific zone** on installation.

### Hull Armor

| Module | Tier | Zone | Armor HP | Special | Sustain/Cycle |
|--------|------|------|----------|---------|---------------|
| Whipple Barrier | T1 | Any (player chooses) | +25 | — | 1 metal |
| Ablative Coating | T1 | Any | +20 | -20% laser damage to zone | 1 metal |
| Reinforced Bulkhead | T1 | Core (hull HP) | +30 hull HP | Last line of defense | 1 metal |
| SiC Composite | T2 | Any | +40 | -10% all damage to zone | 1 composite, 1 rare metal |
| Reactive Plating | T2 | Any | +35 | -25% kinetic damage to zone | 2 composite |
| Angled Deflector | T2 | Fore only | +50 | Designed for head-on charges | 2 composite, 1 rare metal |
| Engine Shroud | T2 | Aft only | +45 | Protects vulnerable rear | 2 composite |
| Null-Mass Lattice | T3 | ALL zones | +30 to all | +15% speed, mass reduction | 1 exotic matter |
| Gravitational Lens | T3 | Any | +70 | Deflects all projectile types around zone | 2 exotic matter |

### Shield Modules

| Module | Tier | Slot | Shield HP | Special | Sustain/Cycle |
|--------|------|------|----------|---------|---------------|
| EM Deflector | T1 | Utility | +20 | — | 1 fuel |
| Shield Capacitor | T2 | Utility | +15 | Enables slow in-combat shield regen | 1 fuel, 1 rare metal |
| Magnetic Confinement Shield | T2 | Utility | +35 | -25% kinetic damage (charged particles deflected) | 2 fuel, 1 composite |
| Phase Matrix | T3 | Utility | +30 | 20% chance to negate any hit entirely | 1 exotic matter |

---

## Engine Modules

### Three Engine Philosophies (sidegrades, not tiers)

| Line | Philosophy | Thrust | Turning | Power Draw | Mass | Sustain | Best For |
|------|-----------|--------|---------|------------|------|---------|----------|
| Ion | Efficient | Low | Medium | Very Low | Light | Very low | Explorers, traders. Run forever. |
| Fusion | Balanced | Medium | Medium | Medium | Medium | Medium | All-rounders. Safe choice. |
| Antimatter | Raw Power | High | High | High | Heavy | High | Combat. Blazing fast, power-hungry. |

### Engine Catalog

| Module | Line | Tier | Thrust | Turning | Power Draw | Sustain/Cycle | Flavor |
|--------|------|------|--------|---------|------------|---------------|--------|
| Ion Drive | Ion | T1 | +20% | +5% | Very Low | 1 fuel | Hall-effect thrusters. Quiet, efficient. |
| Ion Steering | Ion | T1 | +5% | +25% | Very Low | 1 fuel | Vectored ion array. Nimble but not fast. |
| Fusion Torch | Fusion | T2 | +35% | +10% | Medium | 2 fuel | D-T magnetic confinement. The Epstein Drive. |
| Fusion Gyro | Fusion | T2 | +10% | +40% | Medium | 2 fuel | Reaction-wheel + fusion thruster. Snap-turns. |
| D-He3 Drive | Fusion | T2 | +30% | +15% | Medium | 1 fuel, 1 rare metal | Clean fusion. Runs cooler. Rare He-3 fuel. |
| Antimatter Catalyst | AM | T2 | +50% | +15% | High | 2 fuel, 1 rare metal | Explosive acceleration. Power-hungry. |
| Antimatter Vectoring | AM | T2 | +15% | +55% | High | 2 fuel, 1 rare metal | Directed annihilation jets. Turns like a fighter. |
| Metric Drive Core | Precursor | T3 | +60% | +60% | Low | 2 exotic matter | Local containment bubble — brute-force metric stabilization at ship scale. Finite lifespan: containment always degrades. |
| Void Sail | Precursor | T3 | +50% | +30% | None | 1 exotic matter | Reads spacetime turbulence patterns and shapes them into thrust. No fuel — accommodation works with the flow. |

---

## Utility Modules

| Module | Tier | Category | Effect | Sustain/Cycle | Flavor |
|--------|------|----------|--------|---------------|--------|
| Phased Array Radar | T1 | Sensor | +1 scan range | 1 fuel | Active scan. Reveals your position. |
| Passive IR Array | T1 | Sensor | +1 scan range, no signature | — | See without being seen. |
| Fission Reactor | T1 | Power | +15% power gen | 1 metal, 1 fuel | Uranium core. Reliable, heavy. |
| Repair Module | T1 | Utility | Slow hull regen out of combat | 1 metal | Stay in the field longer. |
| Magnetic Grapple | T1 | Tractor | Tractor beam, 15u range, basic materials only | 1 metal | Collect space loot. |
| Fuel Scoop | T1 | Utility | Slow fuel regen near stars | — | Self-sufficiency. "One more jump." |
| Compact Tokamak | T2 | Power | +25% power gen | 2 fuel | Miniaturized fusion. Standard military reactor. |
| Gravimetric Sensor | T2 | Sensor | +2 scan range, detects hidden anomalies | 1 rare metal | Forward Mass Detector. See what others can't. |
| ECM Suite | T2 | Electronic | -25% incoming missile accuracy, signature masking | 1 composite | Electronic warfare. Smuggling enabler. |
| EM Tractor Array | T2 | Tractor | Tractor beam, 30u range, all loot types, auto-targets | 1 composite | Loot vacuums toward you. |
| Fuel Processor | T2 | Utility | Fast fuel regen near gas giants | 1 composite | Deep space self-sufficiency. |
| Cargo Concealer | T2 | Cargo | Hides portion of cargo from scans | 1 composite | Smuggling. Alternate playstyle. |
| Antimatter Flask | T2 | Power | +35% power gen. Risk: hull damage chance on critical hit | 1 rare metal | Penning-trapped antihydrogen. Massive output, dangerous. |
| Droplet Radiator | T2 | Utility | +30% sustained combat endurance | 1 composite | Liquid tin sprayed into vacuum. Targetable. |
| Quantum Vacuum Cell | T3 | Power | +40% power gen | 1 exotic matter | Casimir-effect extraction. Near-unlimited, near-silent. |
| Graviton Tether | T3 | Tractor | Tractor beam, 50u range, can grab disabled hulks | 1 exotic matter | Drag entire ships for station salvage. |
| Resonance Comm | T3 | Utility | See trade prices at distant nodes | 1 exotic matter | FTL market intel. God-tier for traders. |
| Seed Fabricator | T3 | Utility | Generates repair materials from asteroids | 1 exotic matter | Self-replicating repair. Never need a station. |
| Stasis Vault | T3 | Cargo | Cargo doesn't decay + extra capacity | 1 exotic matter | Time-dilated internal space. |

---

## Electronic Warfare Modules (Boarding Alternative)

> 🔮 **FUTURE** (Phase 3). Not yet implemented.

Instead of boarding, electronic warfare delivers the "clever pirate" fantasy:

| Module | Tier | Slot | Effect | Sustain/Cycle |
|--------|------|------|--------|---------------|
| Cargo Siphon | T1 | Utility | Disable trader shields → steal % of cargo over time. Faction rep hit. | 1 fuel |
| System Disruptor | T2 | Utility | Temporarily disable enemy subsystem (weapons/shields/engines) during combat | 1 composite, 1 rare metal |
| Hull Cracker | T2 | Utility | After destroying a ship, +50% loot quality/quantity | 1 rare metal |
| Neural Override | T3 | Utility | Disable NPC ship entirely for 30s. Steal cargo, copy data. | 1 exotic matter |

---

## Drones (Deferred — Post-Core Gate)

> 🔮 **FUTURE** (Phase 3). Not yet implemented.

Carrier and Dreadnought have bay slots. Drones are a separate implementation gate.

| Drone | Tier | Role | Behavior |
|-------|------|------|----------|
| Interceptor Drone | T1 | Anti-missile PD | Orbits mothership, shoots down missiles. Auto-regenerates slowly. |
| Strike Drone | T2 | Anti-ship attack | Attacks current target. Good vs small ships, melts against PD. |
| Salvage Drone | T2 | Auto-loot collection | Flies to nearby wrecks/drops, brings loot back. QoL upgrade. |

---

## Missile Self-Fabrication System

> 🔮 **FUTURE** (Phase 3). Not yet implemented.

Missile weapons manufacture their own ammo. No resupply trips.

| Stat | Description |
|------|-------------|
| Magazine | Max stored missiles |
| Fabrication Rate | Missiles manufactured per tick (slow in combat, fast between fights) |
| Sustain Cost | Resource recipe for the fabricator (per sustain cycle) |

**In-combat fabrication**: Slow (1 per 15-40 ticks depending on weapon). Provides trickle.
**Between-combat fabrication**: Fast (full magazine in ~60 seconds of non-combat).

**Design tradeoff — Beam vs Missile philosophy:**

| | Beam/Kinetic Turrets | Missile Turrets |
|---|---|---|
| Burst DPS | Consistent | Much higher (dump magazine) |
| Sustained DPS | Higher (never runs dry) | Low (fabrication-limited) |
| Power Draw | Higher | Lower |
| Sustain Cost | Lower | Higher (fabrication materials) |
| Counter | Armor/shields | PDC Array intercepts |
| Best For | Long fights, attrition | Alpha strikes, killing fast |

---

## Space Loot System

### Loot Sources

- Destroyed ships drop cargo + salvage
- Anomalies yield artifacts
- Asteroids yield minerals (via fleet contract miners)
- Derelicts yield rare modules
- Precursor ruins yield exotic matter + T3 modules

### Collection Mechanics

- **No module**: Fly through loot to pick up (slow, tedious)
- **Tractor beam module**: Pull loot from range (tiered: Magnetic Grapple → EM Tractor → Graviton Tether)
- **Salvage Drone**: Auto-collects loot in area (Carrier/Dreadnought)

### Rarity Tiers (visual color coding)

| Color | Rarity | Examples |
|-------|--------|---------|
| White | Common | Scrap metal, basic fuel |
| Green | Uncommon | Useful components, composites |
| Blue | Rare | T2 modules, rare metals |
| Purple | Alien | Alien tech, faction-specific |
| Gold | Precursor | T3 modules, exotic matter, named variants (future) |

### Audio/Visual Feedback

- Tractor beam: visible energy beam pulling loot
- Pickup sound scales with rarity (subtle clink → satisfying whoosh → dramatic chime)
- Rare+ loot has visible beam/pillar from distance
- Gold loot should make the player grin

---

## Named / Legendary Variants (Deferred)

> 🔮 **FUTURE** (Phase 3). Not yet implemented.

Precursor artifacts only. Each has a base module type + unique name + 1-2 modifiers with tradeoffs.
Not strictly better — different. Lore-carrying names that hint at Precursor civilization.

Examples (not final):

| Base | Named Variant | Modifier | Tradeoff |
|------|--------------|----------|----------|
| Graviton Shear | "Tide of Esh'kara" | Stun target 2s every 5th hit | +15% sustain cost |
| Metric Drive Core | "Last Light of Meridian" | Leaves speed-boosting wake for allies | Finite lifespan (shorter) |
| Phase Matrix | "The Flickering" | 30% dodge (up from 20%) | Shield HP -20 |
| Quantum Vacuum Cell | "Heart of Nothing" | +50% power gen (up from 40%) | Increased scan signature |

---

## Stat-Check Boarding (Deferred — Future Gate)

> 🔮 **FUTURE** (Phase 3). Not yet implemented.

If players request it post-launch:

- Trigger: Enemy hull < 25% AND player has Boarding Module equipped
- Resolution: Single stat check (crew strength vs enemy defense)
- Success: 2-3x loot + rare boarding-only components, lose 0-2 crew
- Failure: 1x loot (same as destroy), lose 3-5 crew
- **No ship capture.** No fleet growth from boarding.
- Gated behind mid-game tech

---

## Mining

Mining is handled via the fleet mechanic — player contracts out mining to a fleet NPC miner.
Not a direct player activity. No mining laser module needed.

---

## Implementation Notes

### Integration with Existing Combat System

The `StrategicResolverV0` round-based combat extends with:

1. Add `ZoneArmor[4]` to `Fleet` entity (Fore/Port/Stbd/Aft)
2. Each round: determine facing from class stance (NPC) or collision point (player)
3. Damage flow: Shield → Facing Zone Armor → Hull HP
4. Check zone thresholds → apply degradation modifiers to fleet profile
5. All integer math, deterministic

### New Entities/Fields Required

- `Fleet.ZoneArmor` — int[4] (Fore/Port/Stbd/Aft)
- `Fleet.InstalledModules` — list of module instances with zone assignments
- `Fleet.PowerGen` / `Fleet.PowerDraw` — int
- `Fleet.SustainRecipe` — aggregated from installed modules
- `ModuleDef` — expanded with power_draw, sustain_recipe, tracking_class, mount_type_required
- `ShipClassDef` — new content type with slot layout, mount types, base zone armor, base power

### Golden Hash Impact

This is hash-affecting. All zone armor, module degradation, and sustain consumption must be
deterministic and included in tick processing.

---

## Design Sources & Inspiration

- Endless Sky: Engine philosophy lines (Ion/Plasma/Atomic), multi-space fitting budgets
- Starsector: Ordnance points, flux as unified combat resource, D-mod system
- FTL: Risk/reward boarding, subsystem targeting
- Elite Dangerous: Module power management, engineering depth
- The Expanse: Epstein drive, PDC turrets, realistic damage models
- Atomic Rockets (projectrho.com): Hard sci-fi weapon/drive physics basis
- Everspace 2: Diablo-in-space loot model, legendary items
- Gratuitous Space Battles: "Unsure tradeoffs" design principle
