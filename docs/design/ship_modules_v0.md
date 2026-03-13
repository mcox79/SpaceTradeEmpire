# Ship Modules & Technology Design — V0

Status: DESIGN LOCKED — Phase 1 Implemented (Tranche 17-18), Phase 2 Design Expanded
Date: 2026-03-11

## Implementation Phases

### Phase 1 — Implemented (Tranche 17-18)
- 8 ship classes with full stat blocks (ShipClassContentV0.cs)
  - Shuttle (3 slots), Corvette (5), Clipper (4), Frigate (6), Hauler (4), Cruiser (8), Carrier (7), Dreadnought (10)
- Zone armor: 4-directional HP with 3-layer damage routing Shield→Zone→Hull (CombatSystem.CalcDamageWithZoneArmor)
- Combat stances: Charge/Broadside/Kite per class (CombatSystem.DetermineStance)
- 10 starter modules in UpgradeContentV0.cs
  - Weapons: Cannon Mk1/Mk2, Laser Mk1/Mk2
  - Defense: Shield Mk2, Hull Plating Mk2
  - Engines: Engine Booster Mk1, Engine Mk2
  - Utility: Scanner Mk2, Cargo Bay Mk2
- Basic fitting: slot count constraint enforced
- PowerDraw field on modules, BasePower on ship classes (schema only — no enforcement)
- SustainInputs dict on modules (schema only — resources never consumed)
- Weapon family bonuses: Kinetic 150% vs Hull / Energy 150% vs Shield / PD +200% vs Missile
- Strategic resolver: fleet-vs-fleet with stance-based zone targeting (StrategicResolverV0.cs)

### Phase 2 — Next Priority
- **Battle Stations mode**: spin-up trigger, RPM management, transit↔combat state
- **Heat system**: heat-per-shot on weapons, heat capacity + rejection rate per ship, overheat cascade
- **Combat spin**: class-based spin rates, effective armor multiplier vs energy weapons, spin-fire cadence
- **Radiator targeting**: radiators as destructible subsystems, crippling heat rejection as soft-kill
- Power budget enforcement (sum of PowerDraw ≤ BasePower; dock-only fitting constraint)
- Sustain consumption system (goods consumed per 60-tick cycle from fleet cargo/station reserves)
- Mount types: Standard (360°), Broadside (120° +30% dmg), Spinal (60° +50% dmg)
- Spinal mounts on Frigate (1 small) and Cruiser (1 medium), not just Dreadnought
- Module degradation from zone/hull damage (engines degrade when aft zone depleted, etc.)
- T2 Military modules: Railgun, FEL, Particle Beam, Torpedo, Reactive Plating, Fusion Torch, etc.
- Slot layout breakdown per class (weapon/engine/utility/cargo/bay slot counts)
- Sustain starvation: 50% power reduction when supply insufficient + 20% safety floor

### Phase 3 — Aspirational
- T3 Relic modules (discovery-only, exotic matter sustain, cannot be manufactured)
- Missile self-fabrication system (Magazine size, FabRate, in-combat vs peace-time production)
- Drone system (bay slots on Carrier/Dreadnought, Interceptor/Strike/Salvage drone types)
- Electronic warfare (Cargo Siphon, System Disruptor, Hull Cracker, Neural Override)
- Named/Legendary variants (ancient artifacts with unique modifiers)
- Stat-check boarding (hull < 25% + Boarding Module → crew check → enhanced loot)
- Mining fleet contracts (NPC miner delegation, not direct player activity)
- Retractable armored radiators (deploy for rejection, retract behind armor under fire)
- Sensor quality gradient (bearing-only → rough range → hard lock) with EW countermeasures

---

## Design Pillars

1. **Philosophy over tiers** — Ion/Fusion/Antimatter are choices, not a ladder
2. **Multi-constraint fitting** — Slots + Power Budget + Sustain Cost (decided at dock)
3. **Directional combat** — Zone armor makes facing matter; protect your engines
4. **Sustain = economy** — Modules consume resources, not credits. Your fleet loadout is constrained by your supply chains
5. **All turrets, all the time** — Every weapon is a turret with 360° base coverage. Mount types (Broadside/Spinal) add arc restrictions + damage bonuses on specific ships. No fixed guns.
6. **Ships spin in combat** — Battle Stations triggers hull rotation. Turrets spin with the ship, firing in cadence as they sweep past the engagement arc. Spin distributes both incoming laser energy and outgoing weapon heat across the hull. The one exception: spinal mounts fire along the spin axis and are unaffected by rotation.
7. **Heat is the combat clock** — Every weapon generates heat. Heat capacity and radiator rejection rate determine how long you can fight at full power. Overheat forces ceasefire. Radiators are targetable — destroying them is a soft-kill. Heat replaces any notion of "energy per shot" or "weapon capacitor." There is no in-combat energy resource.
8. **Two combat resources, never both on the same gun** — Beam/kinetic weapons are heat-limited (unlimited shots, limited by thermal budget). Missiles are ammo-limited (magazine + fabrication rate, minimal heat). The player never tracks two resources for one weapon.
9. **Self-fabricating missiles** — No resupply trips. Magazine + fab rate + sustain handles ammo
10. **Three tech tiers** — T1 Standard (purchasable), T2 Military (faction/rare materials), T3 Relic (found, not built)

---

## Technology Tiers

> **Implementation Note**: T1 modules are implemented (10 in UpgradeContentV0.cs).
> T2 and T3 modules are Phase 2/3 respectively — no faction-locked or discovery-only
> modules exist yet. All current modules are tier-agnostic.

| Tier | Name | Source | Sustain Cost | Flavor |
|------|------|--------|-------------|--------|
| T1 | Standard | Manufactured, purchasable | Common goods (metal, fuel) | Near-future fusion era |
| T2 | Military | Faction reputation / rare materials | Uncommon goods (composites, rare metals) | Far-future, physics-respecting |
| T3 | Relic | Found in anomalies/ruins, not craftable | Rare goods (exotic matter — exploration only, cannot be manufactured) | Ancient alien tech with theoretical physics basis |

---

## Core Ship Stats

| Stat | Description | Source |
|------|-------------|--------|
| Hull HP | Structural integrity. Universal pool. | Ship class base + hull armor modules |
| Shield HP | Energy barrier. Omnidirectional bubble. | Ship class base + shield modules |
| Zone Armor (x4) | Directional buffer: Fore, Port, Starboard, Aft | Ship class base + armor modules assigned to zones |
| Thrust | Linear acceleration / top speed | Engine modules (thrust-focused) |
| Turning | Rotational acceleration | Engine modules (steering-focused) |
| Mass | Affects accel and turning. Every module adds mass. | Ship class base + sum of installed module mass |
| Power Gen | Reactor output (fitting budget ceiling). Dock-only constraint. | Ship class base + reactor modules |
| Power Draw | Total energy consumption. Must be ≤ Power Gen at fitting time. | Sum of active module draw |
| Heat Capacity | Thermal mass — how much heat the ship can absorb before overheating. | Ship class base (scales with hull mass) + heat sink modules |
| Heat Rejection | Radiator output — heat dissipated per tick during combat. | Ship class base (minimal) + radiator modules |
| Spin Rate | Combat rotation speed (RPM). Determines effective armor vs energy weapons and turret firing cadence. | Ship class base (inverse of mass). Modified by engine modules. |
| Cargo | Trade goods capacity | Ship class base + cargo modules |
| Scan Range | Detection radius | Ship class base + sensor modules |

### Stats NOT included (intentional cuts)

- **Drag** — Folded into thrust/mass relationship. Simpler.
- **Crew** — Cut for v0. Could add later for boarding gate.
- **In-combat energy/capacitor** — Replaced by heat system. Power budget is fitting-only (dock constraint). No "weapon energy" resource exists in combat.

---

## Battle Stations — Combat Spin System

> **NOT YET IMPLEMENTED** (Phase 2). No spin-related fields exist on Fleet or ShipClassDef.

### The Physics

In vacuum, a spinning ship distributes incoming laser energy across its entire circumference rather than allowing dwell on one spot. A laser must hold on a single point long enough to burn through armor. If the hull rotates, the beam traces a spiral, spreading energy across a much larger surface area. This creates an effective armor multiplier against energy weapons that scales with spin rate.

Kinetic projectiles and missiles impact instantaneously — spin does not help against them.

Because turrets rotate with the hull (no counter-rotating barbettes), each turret has a **firing window** per revolution — the arc where the target is in line of sight. The rest of the revolution, the turret faces away and cools. This creates a natural burst-fire cadence tied to RPM.

Spinal mounts fire along the spin axis (the ship's nose). They are the one weapon type unaffected by spin — they fire continuously regardless of rotation. A spinning ship's spinal gun is its only sustained-DPS weapon while everything else pulses.

### Battle Stations Trigger

Ships do not spin during transit or docking. Spin is a combat mode:

1. **Transit mode (0 RPM)**: Turrets track freely with continuous fire. No spin armor bonus. No spin heat distribution. Cargo operations and docking permitted.
2. **"Battle Stations" command**: Player triggers manually, or auto-triggers on hostile detection. Ship begins spinning up.
3. **Spin-up period**: Takes 2-8 seconds depending on ship class (small = fast, large = slow). During spin-up, effective armor multiplier ramps linearly from 1.0x to combat value. DPS drops as firing windows narrow.
4. **Combat mode (combat RPM)**: Full spin benefits. Turrets fire in cadence. Heat distributed across hull.
5. **Spin-down**: Player disengages or combat ends. Ship decelerates to 0 RPM. Docking and cargo operations resume.

Being caught at 0 RPM by energy weapons is devastating — the laser sits on one spot and burns straight through. This creates ambush dynamics: surprising a hauler before it spins up is far more effective than fighting one at combat RPM.

### Spin Rate by Ship Class

Smaller ships spin faster (less rotational inertia). This is the primary defense advantage of small ships against energy weapons — they are physically hard to burn through. Large ships rely on raw armor thickness instead.

| Class | Combat RPM | Spin-Up Time | Effective Armor vs Energy | Spin-Fire DPS | Turn Penalty | Identity |
|-------|-----------|-------------|--------------------------|---------------|-------------|----------|
| Shuttle | 4.0 | 2s | 3.5x zone armor | ~45% of continuous | -50% turn rate | Gyrates wildly — lasers can barely touch it, but committed to the spin |
| Corvette | 3.5 | 2.5s | 3.0x zone armor | ~48% of continuous | -45% turn rate | Fast spin, good all-round defense. Penalty hurts in chases |
| Clipper | 3.0 | 3s | 2.5x zone armor | ~50% of continuous | -40% turn rate | Quick spin for a freighter |
| Frigate | 2.5 | 3.5s | 2.2x zone armor | ~53% of continuous | -35% turn rate | Good spin, charge + broadside cadence |
| Hauler | 1.5 | 5s | 1.5x zone armor | ~65% of continuous | -25% turn rate | Sluggish rotation, escort-dependent |
| Cruiser | 1.5 | 5s | 1.5x zone armor | ~65% of continuous | -25% turn rate | Mass limits spin, compensates with thick armor |
| Carrier | 1.0 | 6s | 1.3x zone armor | ~70% of continuous | -20% turn rate | Barely rotates — relies on drones + escorts |
| Dreadnought | 0.5 | 8s | 1.2x zone armor | ~80% of continuous | -15% turn rate | Crawling spin. Raw armor thickness is the defense. |

**Spin-Fire DPS** is the fraction of continuous-fire DPS a turret achieves while spinning, because each turret can only fire during its engagement arc (~120° of the 360° revolution). Faster spin = shorter windows = lower DPS. Broadside mounts (120° perpendicular arc) get proportionally longer firing windows during spin, making them more efficient than standard turrets on spinning ships.

### Spin + Zone Armor Interaction

Spin affects **only energy weapon** damage against zone armor:

```
effective_zone_armor_vs_energy = base_zone_armor * spin_multiplier
effective_zone_armor_vs_kinetic = base_zone_armor  (no spin benefit)
effective_zone_armor_vs_missile = base_zone_armor  (no spin benefit)
```

This creates a natural counter-dynamic:
- **Energy weapons** (lasers, particle beams) dominate against slow-spinning capitals but are inefficient against fast-spinning small ships
- **Kinetic weapons** (railguns, coilguns) ignore spin entirely — the equalizer against nimble corvettes
- **Missiles** ignore spin entirely — the alpha-strike answer to any target

### Spin + Heat Distribution

A spinning ship distributes weapon heat across the hull circumference. Each turret fires during its engagement arc, then spends the rest of the revolution facing cold vacuum. This provides a passive cooling bonus proportional to spin rate:

```
heat_rejection_bonus = base_heat_rejection * (1 + spin_rate * 0.1)
```

At 4 RPM (Shuttle), this is a +40% heat rejection bonus — significant.
At 0.5 RPM (Dreadnought), this is only +5% — negligible.

Small ships spin fast, reject heat well, but deal less DPS (short firing windows).
Large ships spin slow, reject heat poorly, but deal more sustained DPS (long firing windows).
The tradeoff is intrinsic to one mechanic.

### Spin + Maneuverability

A spinning ship fights gyroscopic precession when changing heading. Thrusters must overcome existing angular momentum before redirecting the ship, reducing effective turn rate proportional to spin RPM.

```
effective_turn_rate = base_turn_rate * (1 - turn_penalty_at_combat_rpm)
```

Turn penalty scales **linearly during spin-up/spin-down** — at half RPM, half the penalty applies. This means the spin-up window is also a maneuverability commitment window: the player is progressively trading agility for defense as RPM climbs.

**Design rationale:** Without this penalty, spinning is a strict upgrade — zone armor distribution across all 4 faces with no cost. The turn penalty creates the core tactical decision:

| Mode | Defense | Offense / Maneuver |
|------|---------|-------------------|
| **Not spinning** | Enemy focuses one zone → fast breach | Full turn rate, full weapon tracking, full chase capability |
| **Spinning** | Damage spread across 4 zones → up to 4x effective zone HP | Degraded turn rate — harder to bring guns to bear, harder to chase or flee |

**Weapon interaction:** Turreted weapons compensate for spin (independent tracking). Fixed-forward weapons (spinal mounts) suffer convergence scatter proportional to spin rate — the ship's rotation means the bore axis sweeps, degrading aim on distant targets. This naturally favors turret-heavy builds for spin-tanking and spinal builds for stationary jousting.

**Class identity implications:**
- **Shuttles/Corvettes**: Highest spin → highest turn penalty. They WANT to spin (best energy defense), but spinning locks them into a slugfight instead of using their natural speed to disengage. A spinning Corvette is a *tank*; a non-spinning Corvette is a *dogfighter*. The player picks a role per engagement.
- **Haulers/Cruisers**: Moderate spin, moderate penalty. They're slow anyway — the turn penalty matters less because their base turn rate is already low. Spin is almost always correct for capitals.
- **Dreadnoughts**: Minimal spin, minimal penalty. The penalty barely registers because the Dreadnought was never going to out-turn anything. Raw armor thickness is the defense.

This creates a natural skill curve: small-ship pilots must read the fight and decide whether to spin (tank) or stay nimble (kite). Capital pilots almost always spin. The decision space lives where it should — in the cockpit of the ship that's fast enough for it to matter.

---

## Heat System — Combat Thermal Management

> **NOT YET IMPLEMENTED** (Phase 2). No heat-related fields exist on Fleet, ModuleDef,
> or ShipClassDef. The Droplet Radiator module exists in the utility catalog but its
> "+30% sustained combat endurance" effect has no backing system.

### Design Rationale

In vacuum, the only way to reject waste heat is radiation (Stefan-Boltzmann law). Every weapon fired, every engine burn generates heat. Generating energy is easy (reactors are compact). Rejecting the waste heat from using that energy is the hard constraint.

Heat replaces any notion of "weapon energy per shot" or "capacitor drain." There is no in-combat energy resource. Power Budget is a dock-only fitting constraint (can I physically run all these modules?). Heat is the combat-time constraint (how long can I fight before I cook?).

**Each weapon family is limited by exactly one combat resource:**

| Family | Combat Limiter | The Other Resource | Player Mental Model |
|--------|---------------|-------------------|---------------------|
| Kinetic (Coilgun, Railgun) | **Heat** (moderate) | No ammo | "I can fire until I overheat" |
| Energy (Laser, Particle Beam) | **Heat** (high!) | No ammo | "Lasers run hot — burst fire or cook" |
| Point Defense | **Heat** (low) | No ammo | "PD runs cool, keep it on" |
| Missile (Pod, Torpedo, Swarm) | **Ammo** (magazine + fab) | Heat (negligible) | "8 missiles, make them count" |
| Gravitonic / Exotic (T3) | **Heat** (extreme) | No ammo | "Ancient weapons run impossibly hot" |

The player never tracks heat AND ammo for the same weapon. Beams/kinetics watch the heat gauge. Missile pilots watch the magazine counter. Clean separation.

### Heat Budget Mechanics

Each ship has:
- **Heat Capacity** (HCap): Total thermal energy the ship can absorb before critical overheat. Scales with hull mass — bigger ships store more heat. Units: abstract heat points (HP-thermal, not to be confused with Hull HP).
- **Heat Rejection Rate** (HRej): Heat dissipated per tick via radiators. Ship class provides a minimal base rate (hull radiation). Radiator modules provide the real rejection capacity.

Each weapon has:
- **Heat/Shot**: Heat generated per firing. Replaces any "energy per shot" concept. This is the sole in-combat cost of firing beam/kinetic weapons.

Each engine burn generates:
- **Heat/Tick**: Passive heat from thrust. Higher-performance engines run hotter.

### Heat Thresholds and Overheat Cascade

| Heat Level | % of HCap | Effect |
|-----------|----------|--------|
| Cold | 0-50% | No penalty. Full combat performance. |
| Warm | 50-75% | Warning indicator. No mechanical penalty yet. |
| Hot | 75-90% | Weapon accuracy -15%. Turret tracking speed -10%. HUD warning pulses. |
| Overheat | 90-100% | Weapon accuracy -30%. Tracking -25%. Shield regen disabled. "OVERHEAT" warning. |
| Critical | 100% | **Forced ceasefire.** All weapons go offline for 5 seconds. Engines throttled to 50%. Ship vents heat (dramatic visual: radiators glow white, hull shimmer). Heat drops to 75% after vent cycle. |

**Critical overheat is recoverable but punishing.** You lose 5 seconds of weapons in a fight — potentially lethal. The incentive is to manage heat proactively: fire in bursts, use spin cooling, prioritize radiator modules, or accept lower-heat weapons.

### Heat and Weapon Choice

This is the core gameplay loop heat creates. Weapon selection is now a thermal budget decision:

| Weapon | Damage | Heat/Shot | Shots Before Overheat (Corvette, base HCap 100) | Character |
|--------|--------|----------|------------------------------------------------|-----------|
| Coilgun Turret (T1) | 12 | 8 | ~12 shots | Reliable, moderate heat. Workhorse. |
| Pulse Laser (T1) | 8 | 12 | ~8 shots | Runs hot for its damage. Burns shields fast but cooks you. |
| PDC Array (T1) | 6 | 3 | ~33 shots | Cool-running. Leave it on. |
| Railgun (T2) | 22 | 18 | ~5 shots | Five devastating shots then you're cooking. |
| Free-Electron Laser (T2) | 15 | 20 | ~5 shots | Melts shields but is the hottest weapon in the game per shot. |
| Particle Beam (T2) | 18 | 16 | ~6 shots | Hot but efficient damage-per-heat. |
| Plasma Carronade (T2) | 25 | 22 | ~4 shots | Four shots of plasma hell, then forced to cool. Short range makes this a commitment weapon. |
| Missile Pod (T1) | 20/missile | 2 | N/A (ammo-limited) | Thermally free. Dump the magazine guilt-free. |

**The thermal hierarchy mirrors physics:**
- Kinetics: moderate heat (energy goes into the projectile, not the ship)
- Energy: high heat (most of the laser's energy becomes waste heat in the emitter optics)
- Missiles: near-zero heat (the missile carries its own propulsion and warhead energy away from the ship)
- PD: low heat (small caliber, rapid but individually low-energy shots)

This teaches the player the damage model through *feel*: lasers feel expensive to fire (hot), kinetics feel workmanlike (warm), missiles feel free (cool) but are count-limited.

### Radiator Modules — Combat-Critical, Not Utility

Radiators are the most important defensive module category after armor. Without radiators, a ship's base heat rejection is minimal — you overheat in seconds of sustained fire. With radiators, you can fight for minutes.

**Radiators are also the most vulnerable external system.** They must be large (surface area = rejection capacity) and exposed (facing vacuum to radiate). Enemies who target your radiators are executing a **soft-kill** — they don't need to destroy your ship, just cripple your ability to reject heat. Once radiators are gone, you're on a countdown: fight at full power for X more seconds before forced ceasefire.

This creates a dominant opening-move strategy in hard sci-fi combat (validated by Children of a Dead Earth): **shoot the radiators first.**

Radiator modules are listed in the Utility Modules section with full stats. The Droplet Radiator (T2) is the key combat radiator — liquid tin sprayed into vacuum, magnetically recollected. It is targetable and destructible.

### Radiator Targeting Rules

- Radiator modules have their own HP pool (separate from zone armor)
- Radiators can be specifically targeted by the player (aim-at-subsystem) or are hit probabilistically in the strategic resolver (5% chance per hit to strike a radiator if one is installed)
- When a radiator is destroyed, heat rejection drops by that module's contribution
- A ship with 0 radiator HP falls back to base hull rejection (very low) — effectively a death sentence in a sustained fight
- **Radiator debris**: destroyed radiators scatter hot metal droplets, creating a brief local hazard (visual flavor, minor AoE damage to nearby ships)

### Heat in the Strategic Resolver

For NPC-vs-NPC fleet combat (StrategicResolverV0), heat integrates per-round:

1. Each round, weapons fire → heat accumulates per fleet
2. Heat rejection subtracts per round (base + radiator + spin bonus)
3. If fleet heat exceeds threshold → accuracy/damage penalties applied
4. If fleet heat hits 100% → fleet skips next round (forced vent)
5. Radiator HP tracked per fleet; damage can destroy radiator capacity mid-fight
6. All integer math, deterministic

---

## Health System — Three Layers

> IMPLEMENTED in CombatSystem.CalcDamageWithZoneArmor (14 tests passing).
> Shield → Zone Armor → Hull routing works exactly as specified below.

```
Shield (omnidirectional bubble)
  ↓ when depleted
Zone Armor (directional, per-face — determined by collision point or stance)
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
- **Spin interaction**: Zone armor HP is multiplied by the spin effectiveness factor against energy weapons only. Kinetic and missile damage uses raw zone HP.

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
| **Radiators** | **Port/Stbd** | **Side zone armor depleted → radiators on that side exposed** |

Weapons degrade from overall hull HP only (below 25% hull).
Reactor degrades from overall hull HP only (below 25% hull).

**Radiator zone sensitivity** is new: radiators are mounted on the ship's flanks (port/starboard) where they have maximum vacuum exposure. When side zone armor is depleted, radiators on that side take direct hits more frequently (radiator hit chance doubles from 5% to 10% per incoming hit on that facing).

### Degradation Curve

| Zone Armor State | Module Effectiveness |
|------------------|---------------------|
| 100-50% | 100% (full performance) |
| 50-1% | 70% (-30%) |
| 0% (breached) | 40% (-60%) |

| Hull HP State | Weapon/Reactor Effectiveness |
|---------------|------------------------------|
| 100-25% | 100% (full performance) |
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
                           → Dreadnought (endgame aspirational — ancient find)
```

### Slot Layouts & Mount Types

| Class | Standard | Broadside | Spinal | Engine | Utility | Cargo | Bays | How to Get |
|-------|---------|-----------|--------|--------|---------|-------|------|------------|
| Shuttle | 1 | — | — | 1 | 1 | — | — | Starter ship |
| Corvette | 2 | — | — | 1 | 2 | 1 | — | Purchase (cheap) |
| Clipper | 1 | — | — | 2 | 1 | 3 | — | Purchase |
| Frigate | 1 | 2 | 1 (small) | 1 | 3 | 1 | — | Purchase (expensive) or faction rep |
| Hauler | 1 | — | — | 1 | 2 | 5 | — | Purchase |
| Cruiser | 2 | 2 | 1 (medium) | 2 | 4 | 2 | — | Faction rep + credits |
| Carrier | 2 | — | — | 1 | 3 | 2 | 2 | Faction rep + credits |
| Dreadnought | 2 | 2 | 2 (large) | 3 | 6 | 4 | 2 | Found in ancient ruin (1-2 per save) |

### Mount Types

> NOT YET IMPLEMENTED (Phase 2). No MountType field exists on ModuleSlot.
> All weapons currently fire as Standard (360°) without arc restrictions or damage bonuses.

| Mount | Arc | Damage Modifier | Spin Behavior | Found On |
|-------|-----|----------------|---------------|----------|
| Standard | 360° full tracking | Base damage | Fires in burst cadence per revolution (engagement arc ~120°) | All ships |
| Broadside | 120° port OR starboard | +30% damage | Best spin efficiency — arc is perpendicular to spin axis, gets longest firing window per revolution | Frigate, Cruiser, Dreadnought |
| Spinal | 60° forward cone | +50% damage | **Unaffected by spin** — fires along the rotation axis. Continuous DPS regardless of RPM. The ship's only sustained-fire weapon while spinning. | Frigate (small), Cruiser (medium), Dreadnought (large) |

**Spinal mount physics rationale:** Spinal weapons are mounted along the ship's structural spine. This gives them three advantages that justify their rarity and power:
1. **Recoil management** — Firing recoil goes straight through the strongest structural axis (same direction as engine thrust). A turreted railgun sends recoil laterally, requiring massive reinforcement.
2. **Power delivery** — Direct fixed power bus from the reactor. No rotating joints to route gigawatts through.
3. **Barrel length** — A spinal weapon can be as long as the entire ship. Longer barrel = higher muzzle velocity (kinetic) or better beam coherence (energy).

**Spinal size classes:**
| Size | Max Damage | Max Heat/Shot | Available On | Flavor |
|------|-----------|--------------|-------------|--------|
| Small | 20 base | 15 | Frigate | Nose-mounted railgun. The charging weapon. |
| Medium | 30 base | 22 | Cruiser | Flagship lance. Punishes anything in your path. |
| Large | 45 base | 35 | Dreadnought | Ship-length accelerator. "What was this built to kill?" |

### Base Stats by Class

| Class | Fore | Port | Stbd | Aft | Hull | Shield | HCap | HRej (base) | Spin RPM | Identity |
|-------|------|------|------|-----|------|--------|------|-------------|----------|----------|
| Shuttle | 15 | 10 | 10 | 10 | 40 | 20 | 60 | 2 | 4.0 | Fragile but gyrates — hard to burn |
| Corvette | 25 | 20 | 20 | 15 | 60 | 35 | 100 | 3 | 3.5 | Balanced, fast spin, thin aft |
| Clipper | 15 | 15 | 15 | 30 | 50 | 30 | 80 | 3 | 3.0 | Armored rear, quick spin — built to run |
| Frigate | 35 | 25 | 25 | 15 | 70 | 40 | 120 | 4 | 2.5 | Charge with spinal, swing broadside |
| Hauler | 20 | 25 | 25 | 25 | 80 | 30 | 140 | 4 | 1.5 | Heavy core, sluggish spin, escort-dependent |
| Cruiser | 30 | 30 | 30 | 25 | 100 | 50 | 180 | 5 | 1.5 | Solid everywhere, spinal lance, fleet command |
| Carrier | 20 | 25 | 25 | 20 | 90 | 45 | 160 | 5 | 1.0 | Barely rotates — drones + escorts do the fighting |
| Dreadnought | 50 | 45 | 45 | 35 | 150 | 80 | 300 | 8 | 0.5 | Massive thermal mass, crawling spin, ship-length spinals |

**HCap** (Heat Capacity) scales roughly with hull mass — bigger ships can absorb more heat before overheating.
**HRej** (base Heat Rejection) is minimal without radiator modules — represents hull radiation only. A Corvette with base HRej of 3 and a weapon generating 8 heat/shot can fire about 12 shots in ~24 seconds before overheating, assuming spin cooling bonus. With a Droplet Radiator (+8 HRej), that extends to sustained fire nearly indefinitely.

### Ship Class Identities

- **Shuttle**: Starter. Nimble, fragile, cheap. Spins so fast lasers barely touch it. Can access restricted docking that larger ships cannot.
- **Corvette**: First real ship. Balanced fighter-trader. Fast spin makes it surprisingly tough vs energy weapons. Jack of all trades.
- **Clipper**: Fast freighter. Outrun what you can't outfight. Armored aft for fleeing, quick spin for a cargo ship. Smuggler's choice.
- **Frigate**: Nose-first brawler. Charge in behind armored fore with the spinal railgun firing continuously, then swing broadside — broadside turrets get the longest spin-fire windows of any mount type. The ship that most rewards spin management.
- **Hauler**: Maximum cargo. Slow spin, tough core, escort-dependent. Without escorts, a hauler caught unsupported will overheat before it can fight off raiders. The trade baron's ship.
- **Cruiser**: Multi-role flagship. Medium spinal lance + broadside turrets + decent spin. Fleet command bonus — passive buff when escorted by player-owned ships. Good at everything, great at nothing alone.
- **Carrier**: Drone commander. Barely spins (1.0 RPM). Weak personal guns but deploys combat/salvage drones from bays. Relies on escorts for thermal defense — its own turrets run hot on the slow rotation.
- **Dreadnought**: Ancient find. 1-2 per save. Massive heat capacity (300 HCap) compensates for crawling 0.5 RPM spin — it can absorb enormous heat before overheating. Twin ship-length spinal mounts fire continuously along the spin axis. "What did the thread builders need this to fight?"

---

## Fitting Constraints

### 1. Slots (Physical Limit)

Each ship has a fixed number of weapon, engine, utility, and cargo slots. Mount types (Standard/Broadside/Spinal) are baked into the ship class definition. A module must match the slot type to be installed.

### 2. Power Budget (Dock-Only Fitting Constraint)

> SCHEMA ONLY (Phase 2). PowerDraw and BasePower fields exist but enforcement
> logic is not implemented. Modules over budget are not prevented or degraded.

- Each ship class has a base power generation value
- Reactor modules add to power generation
- Each installed module has a power draw value
- **Hard cap**: Total power draw cannot exceed total power generation
- If a module would put you over budget, it cannot be activated
- **This is a dock-only constraint.** You check it when fitting modules. There is no in-combat power management, no toggling modules, no capacitor drain. If it fits at the dock, it runs in combat. Heat is the combat-time resource.

### 3. Heat Budget (Combat Constraint)

> NOT YET IMPLEMENTED (Phase 2). See "Heat System" section above for full design.

Heat is the in-combat resource that determines how long you can fight. It is NOT a fitting constraint — you can install any weapons regardless of heat. The heat budget determines your **combat endurance**, not your loadout legality.

- **At the dock**: Irrelevant. Fit whatever you want (within slots + power).
- **In combat**: Every shot generates heat. Heat capacity determines your thermal runway. Heat rejection (radiators + spin bonus) determines your sustainable fire rate. The player manages heat by choosing when to fire, how fast to spin, and whether to accept overheat penalties or pause to cool.

This means a player CAN fit a Corvette with two Railguns (each 18 heat/shot) even though it will overheat in 3 volleys. That is a valid glass-cannon build — devastating alpha strike, then forced to cool. The fitting system does not prevent it. The heat system makes the player live with it.

### 4. Sustain Cost (Economic Limit)

> SCHEMA ONLY (Phase 2). SustainInputs dict populated for weapon modules
> (Munitions, Fuel). No system consumes these resources per cycle. No degradation
> when supply insufficient. No 20% safety floor enforcement.

Sustain is **resource flow, not credits**. Each module consumes specific goods per cycle.

**Resource tiers:**

| Resource | Source | Used By |
|----------|--------|---------|
| Metal | Ore mines | T1 kinetic weapons, hull armor, basic engines |
| Fuel | Fuel refineries | T1 energy weapons, engines, power modules |
| Composite | Metal + Fuel (industrial) | T2 armor, shields, radiators |
| Rare Metal | Rare ore deposits | T2 weapons, advanced sensors |
| Exotic Matter | Anomaly exploration ONLY (cannot be manufactured) | T3 Relic modules |

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
- T2 loadout: Composite production chain (metal + fuel → composite). Real economy required. Radiator modules add composite sustain cost — a combat-capable fleet is economically expensive.
- T3 loadout: Steady exotic matter from exploration. Dreadnought literally needs you to keep exploring ancient ruins.

### Constraint Summary — Three Timescales

| Constraint | Timescale | When Player Thinks About It | What It Limits |
|-----------|----------|---------------------------|---------------|
| Slots + Power | At dock (fitting) | Module installation screen | "Can I physically run this loadout?" |
| Heat + Ammo | Seconds (in combat) | During engagement | "How long can I fight? When do I burst vs sustain?" |
| Sustain | Hours (empire) | Supply chain management | "Can my economy support this fleet?" |

The player never manages all three simultaneously. Each operates at its own cadence. This is the key to avoiding cognitive overload: **three constraints, three contexts, zero overlap.**

---

## Weapons

### Damage Families

| Family | vs Hull | vs Shield | Heat Character | Notes |
|--------|---------|-----------|---------------|-------|
| Kinetic | 150% | 50% | Moderate heat — energy goes into projectile | Slugs punch through armor, deflect off shields. Ignores spin. |
| Energy | 50% | 150% | High heat — waste heat stays in ship | Lasers melt shields, scatter off hull. Reduced by target spin. |
| PointDefense | 100% | 100% | Low heat — small caliber | +200% vs Missile family. Runs cool enough to leave on. |
| Missile | 100% | 100% | Negligible heat — energy leaves with missile | Self-guided, interceptable by PD. Ammo-limited instead of heat-limited. Ignores spin. |
| Gravitonic (T3) | 150% | 150% | Extreme heat — spacetime manipulation | Ignores all conventional defense. Ignores spin. |
| Exotic (T3) | 200% | 100% | Extreme heat — antimatter containment bleed | Devastating to matter. Ignores spin (instantaneous impact). |

### Tracking Speed

All turrets have 360° base coverage (or mount-restricted arc). Within their arc, turrets vary by tracking speed:

| Tracking Class | Speed | Damage Mod | Best Against |
|---------------|-------|-----------|--------------|
| Fast-Track | High | -15% damage | Small/fast targets, missiles, drones |
| Standard | Medium | Base damage | General purpose |
| Heavy | Slow | +25% damage | Large/slow targets, capitals. Loses aim on fast ships. |

### Weapon Catalog

#### T1 — Standard

| Module | Family | Tracking | Damage | Heat/Shot | Magazine | Fab Rate | Sustain/Cycle | Flavor |
|--------|--------|---------|--------|----------|----------|----------|---------------|--------|
| Coilgun Turret | Kinetic | Standard | 12 | 8 | — | — | 1 metal | EM coil slugs. Reliable workhorse. Moderate heat. |
| Pulse Laser | Energy | Standard | 8 | 12 | — | — | 1 fuel | Pulsed IR. Low damage but melts shields. Runs hot for its damage class. |
| PDC Array | PD | Fast-Track | 6 | 3 | — | — | 1 metal | Rapid-fire 20mm CIWS. Cool-running — leave it on. |
| Missile Pod | Missile | N/A (guided) | 20/missile | 2 | 8 | 1 per 15 ticks | 1 metal, 1 fuel | Guided munitions. Thermally free. Dump the magazine. |

#### T2 — Military

| Module | Family | Tracking | Damage | Heat/Shot | Magazine | Fab Rate | Sustain/Cycle | Flavor |
|--------|--------|---------|--------|----------|----------|----------|---------------|--------|
| Railgun | Kinetic | Heavy | 22 | 18 | — | — | 2 metal, 1 composite | Lorentz accelerator. Five shots then you're cooking. Devastating in broadside mounts. |
| Free-Electron Laser | Energy | Standard | 15 | 20 | — | — | 2 fuel, 1 rare metal | Tunable wavelength. Ignores 30% hull armor. Hottest conventional weapon in the game. |
| Particle Beam | Energy | Heavy | 18 | 16 | — | — | 2 fuel, 1 rare metal | Near-c neutral particles. Best damage-per-heat ratio of energy weapons. |
| Plasma Carronade | Kinetic | Standard | 25 | 22 | — | — | 2 fuel, 1 composite | MARAUDER plasma toroid. Short range (15u). Applies heat to TARGET — one of few weapons that heats the enemy. |
| Torpedo Launcher | Missile | N/A (guided) | 40/torpedo | 3 | 4 | 1 per 30 ticks | 2 metal, 1 rare metal | Nuclear-pumped shaped charge. Almost no heat — the warhead does the work. |
| Swarm Battery | Missile | N/A (guided) | 8/missile x 6 salvo | 4 | 18 | 3 per 15 ticks | 2 metal, 2 fuel | Fires in salvos of 6. Overwhelms PDC through volume. Low heat for devastating burst. |
| Casaba Lance | Kinetic | Heavy | 35 | 25 | 2 | 1 per 40 ticks | 2 composite, 1 rare metal | Nuclear shaped plasma spear. Ammo-limited AND hot. The most brutal weapon in T2. |

**Plasma Carronade special — applies heat to target:** This weapon's plasma toroid transfers thermal energy ON impact. Each hit adds heat to the target ship's heat budget. Against a ship with damaged radiators, a Plasma Carronade can force overheat even if the target stops firing. The only weapon that uses heat offensively. Short range (15u) makes it a commitment weapon.

#### T3 — Relic

| Module | Family | Tracking | Damage | Heat/Shot | Magazine | Fab Rate | Sustain/Cycle | Flavor |
|--------|--------|---------|--------|----------|----------|----------|---------------|--------|
| Graviton Shear | Gravitonic | Standard | 30 | 30 | — | — | 1 exotic matter, 1 rare metal | Tidal gradient. Ignores ALL defense types. Runs blisteringly hot. |
| Annihilation Beam | Exotic | Heavy | 45 | 40 | — | — | 2 exotic matter | Antiproton stream. +100% vs hull. Best in spinal mount. Hottest weapon in the game. |
| Void Lance | Energy | Standard | 35 | 28 | — | — | 1 exotic matter, 1 rare metal | Gamma-ray laser. Pierces shields entirely, hull-only damage. |
| Void Seekers | Missile | N/A (guided) | 50/missile | 2 | 6 | 1 per 10 ticks | 2 exotic matter | Phase through shields. PD cannot intercept. Thermally negligible. |

**T3 heat philosophy:** Relic weapons are devastatingly powerful AND devastatingly hot. The Dreadnought's enormous heat capacity (300 HCap) is the reason it was built — it is the only ship that can sustain T3 weapons for more than a handful of shots. A Corvette mounting a Graviton Shear (30 heat/shot against 100 HCap) gets three shots before critical overheat. A Dreadnought gets ten. This is the thermal gating that makes the Dreadnought's T3 weapon slots meaningful beyond raw damage numbers.

---

## Defense / Armor Modules

Armor modules are assigned to a **specific zone** on installation.

### Hull Armor

| Module | Tier | Zone | Armor HP | Special | Sustain/Cycle |
|--------|------|------|----------|---------|---------------|
| Whipple Barrier | T1 | Any (player chooses) | +25 | — | 1 metal |
| Ablative Coating | T1 | Any | +20 | -20% energy weapon damage to zone (ablative material disrupts laser dwell) | 1 metal |
| Reinforced Bulkhead | T1 | Core (hull HP) | +30 hull HP | Last line of defense | 1 metal |
| SiC Composite | T2 | Any | +40 | -10% all damage to zone | 1 composite, 1 rare metal |
| Reactive Plating | T2 | Any | +35 | -25% kinetic damage to zone | 2 composite |
| Angled Deflector | T2 | Fore only | +50 | Designed for head-on charges. Best paired with spinal mount. | 2 composite, 1 rare metal |
| Engine Shroud | T2 | Aft only | +45 | Protects vulnerable rear | 2 composite |
| Heat Sink Armor | T2 | Any | +20 | +30 Heat Capacity (absorbs thermal energy into armor mass) | 1 composite, 1 rare metal |
| Null-Mass Lattice | T3 | ALL zones | +30 to all | +15% speed, mass reduction, +0.5 RPM spin rate | 1 exotic matter |
| Gravitational Lens | T3 | Any | +70 | Deflects all projectile types around zone | 2 exotic matter |

**New: Heat Sink Armor** — A T2 armor module that trades raw armor HP for increased heat capacity. Instead of stopping damage, it absorbs thermal energy into its crystalline matrix. Gives +30 HCap per module installed, allowing the ship to sustain fire longer before overheating. A Corvette with two Heat Sink Armor modules goes from 100 to 160 HCap — 60% more thermal runway. The tradeoff: only +20 armor HP vs +40 for SiC Composite.

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

| Line | Philosophy | Thrust | Turning | Power Draw | Heat/Tick | Mass | Sustain | Best For |
|------|-----------|--------|---------|------------|----------|------|---------|----------|
| Ion | Efficient | Low | Medium | Very Low | Very Low | Light | Very low | Explorers, traders. Run forever. Thermally invisible. |
| Fusion | Balanced | Medium | Medium | Medium | Medium | Medium | Medium | All-rounders. Safe choice. Manageable heat. |
| Antimatter | Raw Power | High | High | High | High | Heavy | High | Combat. Blazing fast, power-hungry, runs hot. |

**Engine heat matters in prolonged chases and evasion.** A ship running antimatter engines at full thrust while also firing weapons stacks engine heat on top of weapon heat. Ion engines produce almost no heat — an explorer or trader running ion drives can allocate nearly all their heat budget to weapons if ambushed.

### Engine Catalog

| Module | Line | Tier | Thrust | Turning | Power Draw | Heat/Tick | Sustain/Cycle | Flavor |
|--------|------|------|--------|---------|------------|----------|---------------|--------|
| Ion Drive | Ion | T1 | +20% | +5% | Very Low | 1 | 1 fuel | Hall-effect thrusters. Quiet, efficient. Thermally invisible. |
| Ion Steering | Ion | T1 | +5% | +25% | Very Low | 1 | 1 fuel | Vectored ion array. Nimble but not fast. Cool-running. |
| Fusion Torch | Fusion | T2 | +35% | +10% | Medium | 4 | 2 fuel | D-T magnetic confinement. The Epstein Drive. |
| Fusion Gyro | Fusion | T2 | +10% | +40% | Medium | 3 | 2 fuel | Reaction-wheel + fusion thruster. Snap-turns. |
| D-He3 Drive | Fusion | T2 | +30% | +15% | Medium | 2 | 1 fuel, 1 rare metal | Clean fusion. Runs 50% cooler than D-T. Rare He-3 fuel. |
| Antimatter Catalyst | AM | T2 | +50% | +15% | High | 8 | 2 fuel, 1 rare metal | Explosive acceleration. Runs very hot under thrust. |
| Antimatter Vectoring | AM | T2 | +15% | +55% | High | 7 | 2 fuel, 1 rare metal | Directed annihilation jets. Turns like a fighter. Hot. |
| Metric Drive Core | Relic | T3 | +60% | +60% | Low | 3 | 2 exotic matter | Local containment bubble. Exotic physics = low waste heat. Finite lifespan. |
| Void Sail | Relic | T3 | +50% | +30% | None | 0 | 1 exotic matter | Reads spacetime turbulence. Zero heat. Zero fuel. The ultimate engine. |

**D-He3 Drive thermal advantage:** Helium-3 fusion produces charged particles directly (no neutron radiation), meaning far less waste heat than D-T fusion. The D-He3 Drive at 2 heat/tick is half the heat of a Fusion Torch at 4 — a significant advantage in prolonged combat where engine heat stacks with weapon heat. The tradeoff: rare metal sustain cost, and slightly less thrust.

**Void Sail — zero heat:** The T3 Relic engine generates literally no waste heat. A ship running a Void Sail can allocate 100% of its heat budget to weapons. This is why T3 Relic ships are terrifying: T3 weapons are the hottest in the game, and the T3 engine frees the entire thermal budget for them.

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
| **Droplet Radiator** | **T2** | **Thermal** | **+8 Heat Rejection, 40 radiator HP. Targetable.** | **1 composite** | **Liquid tin sprayed into vacuum, magnetically recollected. The difference between a warship and a coffin.** |
| **Emergency Heat Sink** | **T2** | **Thermal** | **Active: dump 50 heat instantly (single use per fight, 120-tick recharge).** | **1 composite** | **Expendable lithium heat sink. One "oh shit" button per engagement.** |
| Quantum Vacuum Cell | T3 | Power | +40% power gen | 1 exotic matter | Casimir-effect extraction. Near-unlimited, near-silent. |
| Graviton Tether | T3 | Tractor | Tractor beam, 50u range, can grab disabled hulks | 1 exotic matter | Drag entire ships for station salvage. |
| Resonance Comm | T3 | Utility | See trade prices at distant nodes | 1 exotic matter | FTL market intel. God-tier for traders. |
| Seed Fabricator | T3 | Utility | Generates repair materials from asteroids | 1 exotic matter | Self-replicating repair. Never need a station. |
| Stasis Vault | T3 | Cargo | Cargo doesn't decay + extra capacity | 1 exotic matter | Time-dilated internal space. |
| **Null-Point Cooler** | **T3** | **Thermal** | **+15 Heat Rejection, 80 radiator HP. -50% thermal signature.** | **1 exotic matter** | **Dumps heat into local spacetime curvature. No visible radiator panels — nothing to target.** |

**Radiator modules are now a first-class combat category**, not a utility afterthought. A ship without radiators can fire roughly 5-12 shots before critical overheat (depending on class and weapon). A ship with one Droplet Radiator can sustain moderate fire indefinitely. Two radiators enables sustained heavy fire. The fitting tradeoff: every utility slot spent on radiators is a slot NOT spent on sensors, ECM, tractors, or cargo.

**Emergency Heat Sink** — A single-use active ability (recharges between fights). When activated, instantly dumps 50 heat points — enough to drop a Corvette from Critical to Warm in one action. The "oh shit" button when you realize you're about to overheat mid-fight. Experienced players hold this in reserve; new players burn it early.

**Null-Point Cooler** — The T3 radiator has no physical panels to target. It rejects heat by coupling to local spacetime geometry (relic tech). This means it cannot be destroyed by radiator targeting — it persists until the ship itself is destroyed. Combined with its -50% thermal signature, this module makes T3 ships thermally invisible and thermally unkillable. The only counter to a Null-Point Cooler is raw damage to the hull.

---

## Electronic Warfare Modules (Boarding Alternative)

> FUTURE (Phase 3). Not yet implemented.

Instead of boarding, electronic warfare delivers the "clever pirate" fantasy:

| Module | Tier | Slot | Effect | Sustain/Cycle |
|--------|------|------|--------|---------------|
| Cargo Siphon | T1 | Utility | Disable trader shields → steal % of cargo over time. Faction rep hit. | 1 fuel |
| System Disruptor | T2 | Utility | Temporarily disable enemy subsystem (weapons/shields/engines/radiators) during combat | 1 composite, 1 rare metal |
| Hull Cracker | T2 | Utility | After destroying a ship, +50% loot quality/quantity | 1 rare metal |
| Neural Override | T3 | Utility | Disable NPC ship entirely for 30s. Steal cargo, copy data. | 1 exotic matter |

**System Disruptor + radiators:** Disabling an enemy's radiators via EW is a soft-kill without needing to physically destroy the panels. The enemy is forced to cease fire or overheat. This makes the System Disruptor one of the most powerful T2 modules in extended engagements.

---

## Drones (Deferred — Post-Core Gate)

> FUTURE (Phase 3). Not yet implemented.

Carrier and Dreadnought have bay slots. Drones are a separate implementation gate.

| Drone | Tier | Role | Behavior |
|-------|------|------|----------|
| Interceptor Drone | T1 | Anti-missile PD | Orbits mothership, shoots down missiles. Auto-regenerates slowly. |
| Strike Drone | T2 | Anti-ship attack | Attacks current target. Good vs small ships, melts against PD. |
| Salvage Drone | T2 | Auto-loot collection | Flies to nearby wrecks/drops, brings loot back. QoL upgrade. |

---

## Missile Self-Fabrication System

> FUTURE (Phase 3). Not yet implemented.

Missile weapons manufacture their own ammo. No resupply trips.

| Stat | Description |
|------|-------------|
| Magazine | Max stored missiles |
| Fabrication Rate | Missiles manufactured per tick (slow in combat, fast between fights) |
| Sustain Cost | Resource recipe for the fabricator (per sustain cycle) |

**In-combat fabrication**: Slow (1 per 15-40 ticks depending on weapon). Provides trickle.
**Between-combat fabrication**: Fast (full magazine in ~60 seconds of non-combat).

**Design tradeoff — Heat-limited vs Ammo-limited weapons:**

| | Beam/Kinetic Turrets | Missile Turrets |
|---|---|---|
| Combat Limiter | **Heat** (thermal budget) | **Ammo** (magazine + fab rate) |
| Burst DPS | Moderate (limited by heat-per-shot) | Very High (dump magazine) |
| Sustained DPS | High (fire as fast as radiators reject heat) | Low (fabrication-limited) |
| Heat Cost | Moderate to High | Negligible |
| Sustain Cost | Lower | Higher (fabrication materials) |
| Counter | Armor/shields + spin (vs energy) | PDC Array intercepts |
| Counter-Counter | Kinetics ignore spin; hot weapons force overheat | Swarm salvos overwhelm PD |
| Best For | Long fights, attrition, sustained engagements | Alpha strikes, ambushes, killing fast before they spin up |
| Worst For | Prolonged fights without radiators | Extended engagements after magazine spent |

**The combat rhythm these two resources create:**
> Open with missile salvo (dump magazine, overwhelm PD, thermally free) → switch to sustained kinetic/laser fire (heat-managed, spin-cadenced) → missiles trickle back from fabrication → second volley when magazine is half-full → manage heat between volleys by adjusting spin rate or pausing fire

This is burst → sustain → burst pacing from two independent resources with zero cognitive overlap.

---

## Space Loot System

### Loot Sources

- Destroyed ships drop cargo + salvage
- Anomalies yield artifacts
- Asteroids yield minerals (via fleet contract miners)
- Derelicts yield rare modules
- Ancient ruins yield exotic matter + T3 modules
- **Destroyed radiators scatter hot metal droplets** — minor loot (scrap composite) + brief AoE hazard

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
| Gold | Relic | T3 modules, exotic matter, named variants (future) |

### Audio/Visual Feedback

- Tractor beam: visible energy beam pulling loot
- Pickup sound scales with rarity (subtle clink → satisfying whoosh → dramatic chime)
- Rare+ loot has visible beam/pillar from distance
- Gold loot should make the player grin

---

## Named / Legendary Variants (Deferred)

> FUTURE (Phase 3). Not yet implemented.

Ancient artifacts only. Each has a base module type + unique name + 1-2 modifiers with tradeoffs.
Not strictly better — different. Lore-carrying names that hint at the thread builders' civilization.

Examples (not final):

| Base | Named Variant | Modifier | Tradeoff |
|------|--------------|----------|----------|
| Graviton Shear | "Tide of Esh'kara" | Stun target 2s every 5th hit | +15% sustain cost |
| Metric Drive Core | "Last Light of Meridian" | Leaves speed-boosting wake for allies | Finite lifespan (shorter) |
| Phase Matrix | "The Flickering" | 30% dodge (up from 20%) | Shield HP -20 |
| Quantum Vacuum Cell | "Heart of Nothing" | +50% power gen (up from 40%) | Increased scan signature |
| Null-Point Cooler | "The Quiet" | +25 Heat Rejection (up from 15) | Emits low-frequency hum detectable by gravimetric sensors |
| Annihilation Beam | "Wrath of Meridian" | -10 Heat/Shot (30 instead of 40) | +25% sustain cost |

---

## Stat-Check Boarding (Deferred — Future Gate)

> FUTURE (Phase 3). Not yet implemented.

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
5. **NEW — Heat per round**: accumulate weapon heat, subtract rejection (base + radiators + spin bonus), apply threshold penalties
6. **NEW — Spin modifier**: multiply zone armor vs energy damage by class spin factor
7. **NEW — Radiator HP**: track radiator damage per fleet; destroyed radiators reduce rejection rate
8. All integer math, deterministic

### New Entities/Fields Required

- `Fleet.ZoneArmor` — int[4] (Fore/Port/Stbd/Aft)
- `Fleet.InstalledModules` — list of module instances with zone assignments
- `Fleet.PowerGen` / `Fleet.PowerDraw` — int (fitting constraint only)
- `Fleet.HeatCurrent` / `Fleet.HeatCapacity` / `Fleet.HeatRejection` — int (combat thermal state)
- `Fleet.SpinRPM` / `Fleet.SpinActive` — int, bool (combat spin state)
- `Fleet.RadiatorHP` — int (targetable subsystem HP)
- `Fleet.SustainRecipe` — aggregated from installed modules
- `ModuleDef` — expanded with power_draw, sustain_recipe, tracking_class, mount_type_required, **heat_per_shot**, **heat_per_tick** (engines), **radiator_hp** (radiators)
- `ShipClassDef` — new content type with slot layout, mount types, base zone armor, base power, **base_heat_capacity**, **base_heat_rejection**, **base_spin_rpm**, **spin_up_time**

### Golden Hash Impact

This is hash-affecting. All zone armor, module degradation, sustain consumption, **heat accumulation/rejection**, **spin state**, and **radiator HP** must be deterministic and included in tick processing.

---

## Design Sources & Inspiration

- Endless Sky: Engine philosophy lines (Ion/Plasma/Atomic), multi-space fitting budgets
- **Starsector**: Flux as unified combat resource (validated that one combat resource is better than two overlapping ones). Our heat system follows the same principle: heat is the universal combat limiter for non-missile weapons.
- FTL: Risk/reward boarding, subsystem targeting, power redistribution
- Elite Dangerous: Module power management (our dock-only power budget avoids Elite's in-combat complexity)
- The Expanse: Epstein drive, PDC turrets, realistic damage models, **ship rotation during combat**
- **Atomic Rockets (projectrho.com)**: Hard sci-fi weapon/drive physics basis. Ship spin analysis, radiator vulnerability, heat rejection as dominant combat constraint. Primary physics reference.
- Everspace 2: Diablo-in-space loot model, legendary items
- Gratuitous Space Battles: "Unsure tradeoffs" design principle
- **Children of a Dead Earth**: Full thermodynamics simulation validated heat-as-combat-resource. Radiator targeting as dominant opening strategy. Proved that thermal management creates richer tactical depth than energy capacitors.
- **Nebulous Fleet Command**: Sensor quality gradient as first-class combat mechanic. Validated that information warfare is more fun than realistic propulsion for strategy games. Aspiration for Phase 3 sensor system.
- **MechWarrior series**: 30+ years of heat + ammo coexistence. Proved that two combat resources work when each weapon type is limited by exactly one (beams = heat, ballistics/missiles = ammo). Our system follows this model precisely.
- **Naval combat theory**: Wayne P. Hughes' salvo model (missile volleys vs defensive capacity). Barbette rotation concept. Rolling ship armor distribution.
