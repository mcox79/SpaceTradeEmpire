# Combat Mechanics — Design Spec

> Mechanical specification for the strategic resolver, heat system, battle
> stations readiness, zone armor, damage families, and the combat loop.
> Companion to `ship_modules_v0.md` (fitting), `CombatFeel.md` (visual design),
> and `fleet_logistics_v0.md` (repair/sustain costs).

---

## AAA Reference Comparison

| Game | Core Combat Resource | Defensive Layers | Loadout Depth | Player Role |
|------|---------------------|-----------------|---------------|-------------|
| **Starsector** | Dual flux (soft from shields, hard from weapons). Overload at 100% = shields collapse 1-3s. Venting (shields down) clears soft flux. Hard flux requires active vent (vulnerable window). | Shield → Armor (% reduction) → Hull. Armor has per-cell grid. | 4 weapon groups, 3 mount sizes, 4 damage families. Officers add stat multipliers. | Direct pilot. Fleet commander. |
| **FTL** | Power grid (8-12 bars). Reallocate between shields/weapons/engines in real time under fire. | Discrete shield bars (absorb one projectile each). Hull HP. | Room-targeting creates "combat as puzzle." Crew positioning matters. | Captain — power management + targeting decisions. |
| **BattleTech** | Heat. Rises with weapon fire, dissipates via heat sinks. Overheating = random shutdown/ammo explosion. Alpha strikes = maximum burst + maximum heat risk. | Armor HP (11 zones) → Structure HP → Component destruction. Knockdown from stability damage. | Mech customization: weapons × heat sinks × armor allocation × tonnage. | Lance commander — 4 mechs, full tactical control. |
| **Stellaris** | None (fleet attrition). War exhaustion as strategic resource. | Shield HP (%) → Armor HP (%) → Hull HP. Tracking vs evasion determines hit chance. | Fleet composition: corvettes vs destroyers vs battleships. Weapon size × tracking creates counter matrix. | Emperor — strategic fleet orders, no tactical control. |
| **Into the Breach** | Actions (2 per mech). Perfect information — enemy intent visible before you move. | Grid-based positioning. Buildings as strategic objectives. | 3 mechs × unique abilities. Positioning IS the weapon. | Tactician — puzzle solver with full information. |
| **Crying Suns** | Weapon cooldowns (2-30s). Long cooldown = high impact moment. | Flagship HP + squadron HP in 3 lanes. | 4 squadron types × flagship weapons × special abilities. | Commander — direct squadron deployment, auto-fire flagship. |
| **STE (Ours)** | Heat. Accumulates per weapon fire, dissipated by radiators. Overheat = 50% damage. Lockout at 2× capacity = 0% damage. Passive cooling always active. | Shield → Zone Armor (4 directional zones) → Hull. Stance determines zone hit distribution. | 3 damage families × 3 mount types × spin mechanics × heat budget. Battle stations readiness gates output. | Pilot-entrepreneur — combat is risk/cost evaluation, not a career. |

### Best Practice Synthesis

1. **Heat/flux creates the core decision** (Starsector, BattleTech) — "fire more and risk overload, or hold fire and stay safe." Our heat system directly implements this. The missing element: active venting decision (Starsector's shields-down vent). Our passive radiator cooling is correct for a trader-captain who shouldn't micromanage combat.

2. **Defensive layers must be distinct** (Starsector Shield→Armor→Hull, BattleTech 11-zone) — each layer should demand different weapons to strip. Our 3-layer stack (Shield → Zone → Hull) with damage family effectiveness (Kinetic strong vs hull, Energy strong vs shields) achieves this.

3. **Zone/directional armor creates positional decisions** (BattleTech per-zone, our per-facing) — presenting your strongest zone to the enemy and protecting weak zones is the positional gameplay. Our stance system (Charge/Broadside/Kite) mechanizes this.

4. **Combat should connect to economy** (Starsector CR, BattleTech repair time) — winning a fight should cost something (repair, ammo, time). Our sustain system (munitions consumed per cycle) and repair costs achieve this. Missing: repair TIME that makes damaged ships unavailable (Starsector CR recovery model).

5. **Auto-resolved combat needs pre-combat agency** (Stellaris, Crying Suns) — since our player is a trader, the interesting decision is BEFORE combat (should I fight?) and AFTER (what did I lose?). Pre-combat projected outcomes and post-combat salvage/repair costs are the player's interface with combat.

6. **Show enemy intent before commitment** (Into the Breach, XCOM) — projected combat outcomes should be visible before the player engages. "Estimated: Victory (78%), hull damage 15-25, 3 rounds."

---

## Current Implementation

### Systems

| System | File | Purpose | Status |
|--------|------|---------|--------|
| `StrategicResolverV0` | `SimCore/Systems/StrategicResolverV0.cs` | Multi-round deterministic combat resolution | Implemented |
| `NpcFleetCombatSystem` | `SimCore/Systems/NpcFleetCombatSystem.cs` | NPC fleet destruction, respawn, loot drops | Implemented |
| `CombatSystem` | `SimCore/Systems/CombatSystem.cs` | Encounter log, per-frame replay verification | Implemented |

### Entities

```
Fleet Combat State:
  HullHp / HullHpMax: int
  ShieldHp / ShieldHpMax: int
  ZoneArmorHp[4]: int (indexed by ZoneFacing)
  BattleStations: enum { StandDown, SpinningUp, BattleReady }
  HeatCurrent: int (accumulated thermal load)

StrategicResult:
  Winner: enum { A, B, Draw }
  RoundsPlayed: int
  FleetAHullRemaining / FleetBHullRemaining: int
  SalvageValue: int
  Frames: List<ReplayFrame>   // per-round snapshots for determinism proof
```

---

## Mechanical Specification

### 1. Damage Family System

Four weapon families with distinct effectiveness against defensive layers:

| Family | vs Shield | vs Hull | Special |
|--------|-----------|---------|---------|
| **Kinetic** | 50% | 150% | Penetrates shields poorly, devastating to exposed hull |
| **Energy** | 150% | 50% | Strips shields efficiently, weak against hull plating |
| **PointDefense** | 100% | 100% | Standard unless counter-active |
| **Neutral** | 100% | 100% | Balanced baseline |

**PointDefense Counter Bonus**: When a PD weapon fires at a target using Missile
or Torpedo weapons, damage is multiplied by 200% (2× bonus). Applied BEFORE
family effectiveness multipliers.

**Design Intent**: The family system forces loadout decisions. A pure-Kinetic
loadout struggles against shields but destroys exposed hull. A pure-Energy
loadout strips shields but can't close the kill. Optimal loadouts mix families —
Energy to strip shields, Kinetic to finish.

### 2. Three-Layer Damage Routing (CalcDamageWithZoneArmor)

Each weapon shot routes through three defensive layers in sequence:

```
INCOMING DAMAGE (raw × family effectiveness)
        │
        ▼
┌─── SHIELD LAYER ───┐
│ Absorb = min(effective_dmg, shieldHp)          │
│ shieldHp -= absorb                              │
│ Overflow = (raw - absorb) × hull_mult / shield_mult │
└─────────┬───────────┘
          │ overflow (hull-effective damage)
          ▼
┌─── ZONE ARMOR LAYER ───┐
│ Zone = determined by defender's combat stance    │
│ Absorb = min(remaining, zoneArmorHp[facing])     │
│ zoneArmorHp[facing] -= absorb                    │
│ Overflow continues to hull                       │
└─────────┬──────────────┘
          │ remaining
          ▼
┌─── HULL LAYER ───┐
│ hullHp -= remaining                              │
│ If hullHp ≤ 0: fleet destroyed                   │
└──────────────────┘
```

**Shield overflow conversion**: When damage bleeds through shields, it converts
from shield-effective to hull-effective. A Kinetic shot (50% vs shields, 150%
vs hull) that overflows shields does 3× more damage to hull per overflow unit
than it did to shields. This makes shield-stripping with Energy weapons
strategically important — once shields drop, Kinetic damage amplifies.

### 3. Zone Armor and Combat Stances

Each ship class has a **combat stance** determining how incoming fire distributes
across the four zones:

| Stance | Ship Classes | Fore | Port | Stbd | Aft |
|--------|-------------|------|------|------|-----|
| **Charge** | Frigate, Dreadnought | 50% | 20% | 20% | 10% |
| **Broadside** | Corvette, Cruiser, Hauler, Carrier | 15% | 35% | 35% | 15% |
| **Kite** | Clipper, Shuttle | 10% | 15% | 15% | 60% |

**Default Zone Armor HP**:
- Player: Fore 25 / Port 20 / Starboard 20 / Aft 15
- NPC/AI: Fore 20 / Port 15 / Starboard 15 / Aft 10

**Deterministic Hit Assignment**: Weapons distributed round-robin based on
accumulated percentage buckets. With 4 weapons vs Charge stance: weapons 0-1
→ Fore (50%), weapon 2 → Port (20%), weapon 3 → Starboard (20%).

**Depleted Zone Consequence**: When a zone reaches 0 HP, all remaining hits
on that facing go directly to hull. Additionally, radiator modules are linked
to the Aft zone — depleting Aft destroys the radiator bonus.

### 4. Heat System

Heat tracks thermal accumulation during combat. It creates the core
"fire more vs. stay safe" decision.

```
Per round:
  1. Each weapon fired adds HeatPerShot (100) to fleet heat
  2. Check heat state:
     heat ≤ capacity:         Normal    → 100% damage
     capacity < heat ≤ 2×cap: Overheat  → 50% damage
     heat > 2× capacity:     Lockout   → 0% damage (weapons silent)
  3. After all firing: heat -= rejectionRate (passive cooling)
     heat = max(0, heat)
```

**Constants**:
- Heat Capacity: 1000
- Heat Per Shot: 100 (per weapon per round)
- Default Rejection Rate: 150 (passive cooling per round)
- Overheat Damage Multiplier: 50%
- Lockout Threshold: 2× capacity (2000)

**Heat Budget**: A ship with 4 weapons generates 400 heat/round and cools
150/round. Net accumulation: +250/round. Time to overheat: 4 rounds.
Time to lockout: 8 rounds. This creates a ~4-round window of full output
followed by degraded performance — matching Starsector's flux tension.

### 5. Radiator Modules

Radiators increase the rejection rate (cooling), extending the window before
overheat. They are physically linked to the Aft zone.

| Module | Bonus Rate | Total Rejection | Rounds to Overheat |
|--------|-----------|----------------|-------------------|
| None | 0 | 150 | 4 rounds |
| Basic Radiator | +75 | 225 | 5.7 rounds |
| Advanced Radiator | +150 | 300 | 10 rounds |

**Radiator Destruction**: When Aft zone armor HP ≤ 0, radiator bonus is lost.
Rejection rate drops to base (150). If the ship was managing heat with the
radiator, losing it mid-combat causes a heat spike leading to overheat.

**Design Intent**: The radiator creates a targetable subsystem (FTL pattern).
Attacking an enemy's Aft zone to destroy their radiator is a strategic choice —
it doesn't deal direct damage but cripples their heat economy, forcing overheat.
This is the "shoot the cooling system" tactic.

### 6. Battle Stations Readiness

A three-state machine controlling damage output independently from heat:

```
StandDown (0) → SpinningUp (1) → BattleReady (2)

StandDown:   max 25% damage (weapons cold, crew at rest)
SpinningUp:  max 50% damage (gyros activating, weapons warming)
             Duration: BattleStationsSpinUpTicks (3 ticks)
BattleReady: max 100% damage (full combat RPM)
```

**Combined Damage**: Each round's effective damage =
`min(heatDamagePct, readinessDamagePct)`. Both heat and readiness limit
independently — the worst of two wins.

**Design Intent**: Battle stations creates a **commitment cost**. Spinning up
takes 3 ticks and announces combat intent. A trader who keeps weapons cold
(StandDown) has 25% damage cap but no heat accumulation. Spinning up is a
decision to commit to combat — with the heat consequences that follow.

### 7. Spin & Turn Mechanics

Gyroscopic spin during combat affects weapon accuracy:

```
spinPenaltyBps = min(spinRpm × TurnPenaltyBpsPerRpm, MaxTurnPenaltyBps)
               = min(spinRpm × 100, 5000)

Default SpinRpm = 20 (2% penalty)
Max SpinRpm = 50 (5% penalty cap)
```

**Mount Type Efficiency** (during spin):

| Mount | Arc Efficiency | Fire Cadence | Turn Affected? | Net Output |
|-------|---------------|--------------|----------------|------------|
| **Standard** | 100% - spinPenalty | 60% | Yes | ~48% at 20 RPM |
| **Broadside** | 70% fixed | 50% | No | 35% always |
| **Spinal** | 100% | 100% | No | 100% always |

**Design Intent**: Spinal mounts are most efficient during spin but fire only
forward. Broadside mounts are consistent but never reach full output. Standard
mounts suffer from spin but are versatile. This creates fitting tradeoffs:
Charge stance ships favor Spinal; Broadside stance ships favor Broadside mounts.

### 8. Strategic Resolver (Multi-Round Attrition)

The deterministic multi-round combat engine:

```
ResolveStrategicCombat(profileA, profileB):
  for round in 1..StrategicMaxRounds (50):
    1. Compute aEffDmgPct = min(aHeatPct, aReadinessPct)
    2. For each weapon in A's loadout:
       - Apply mount efficiency + fire cadence
       - Scale by aEffDmgPct
       - Route through B's shield → zone → hull
    3. A accumulates heat: aHeat += sum(weapon.HeatPerShot)
    4. A cools: aHeat = max(0, aHeat - aRejection)
    5. Check radiator: if B's aft zone depleted, remove B's radiator bonus
    6. If B alive: B fires back at A (symmetric process)
    7. If either hull ≤ 0: break
    8. Capture ReplayFrame (round, hull, shield, zone HP, heat, damage)

  Determine winner:
    A hull > 0, B hull ≤ 0 → A wins
    B hull > 0, A hull ≤ 0 → B wins
    Both alive after 50 rounds → Draw (attacker flees)
    Both dead → Draw
```

**Deterministic Replay**: Each frame serializes to pipe-delimited format.
Frames concatenated and SHA256 hashed for golden-hash verification.

### 9. Weapon Sustain (Economy Connection)

Weapons require munitions to fire, checked per sustain cycle (60 ticks):

| Weapon | Sustain Input | Effect if Missing |
|--------|--------------|-------------------|
| Cannon MK1 | Munitions × 1 | Module disabled — cannot fire |
| Laser MK1 | Munitions × 1, Fuel × 1 | Module disabled |
| Advanced Cannon | Munitions × 2 | Module disabled |
| T2 Faction weapons | Faction goods × N | Module disabled |
| T3 Ancient weapons | Exotic Matter × N | Module disabled |

**Design Intent**: Combat has ongoing economic cost. A player who fights
frequently must maintain a munitions supply chain. Running out of munitions
mid-campaign silences weapons — creating the "trader who forgot to resupply"
failure mode. This connects combat directly to the trade economy.

### 10. Loot System

NPC fleet destruction triggers deterministic loot rolls:

| Rarity | Weight | Credits | Goods | Module |
|--------|--------|---------|-------|--------|
| Common | 60% | 10-30 | None | None |
| Uncommon | 25% | 25-75 | 3× (Fuel/Metal/Ore/Electronics/Munitions) | None |
| Rare | 12% | 50-150 | None | Module (rare pool) |
| Epic | 3% | 100-300 | None | Module (premium pool) |

**Roll**: `FNV1a64(fleetId + "_loot_" + state.Tick) % TotalWeight`
**Despawn**: 3600 ticks (~1 game hour)

### 11. NPC Fleet Combat (Destruction & Respawn)

```
NPC fleet destroyed when HullHp ≤ 0:
  1. Free edge capacity if traveling
  2. Roll loot at death node
  3. Queue respawn entry:
     - FleetId, HomeNodeId, DestructionTick, Role, OwnerId
     - Cooldown: NpcShipTweaksV0.RespawnCooldownTicks
  4. After cooldown: fleet respawns at home node with fresh HP
```

### 12. Escort Doctrine

Escorting fleets provide shield damage reduction to their target:

```
if targetFleet.IsEscorted:
  incomingShieldDamage *= (100 - EscortShieldDamageReductionPct) / 100
                        = 75% of original (25% reduction)
```

Zone armor and hull damage are NOT affected by escort bonus.

---

## Player Experience

### Combat as Cost-Benefit Analysis

The player-entrepreneur's combat loop is NOT "how do I fight better" but
"should I fight at all?"

```
Pre-combat evaluation:
  1. What's the enemy? (ship class, estimated loadout)
  2. What's my heat budget? (rounds until overheat)
  3. What's the projected outcome? (estimated hull damage)
  4. What's the repair cost? (credits + dock time)
  5. What's the loot potential? (rarity distribution)
  6. Can I afford the downtime? (trades missed during repair)

Decision: fight (profit from loot), flee (preserve hull), or
          avoid (reroute around hostile space).
```

### The Heat Arc in a Typical Fight

```
Round 1-4:  Full damage output. Both sides exchanging fire.
            Heat rising steadily (net +250/round with 4 weapons).
            Shields absorbing energy damage. Zone armor taking kinetic.
Round 5-6:  One side hits overheat. Damage drops to 50%.
            Opponent with radiator still at full output — advantage.
Round 7-8:  Overheated side approaches lockout. Must hope opponent
            hull breaks first or accept combat draw.
Round 9+:   Extended fights are heat-economy fights. The side with
            better cooling (radiators intact) grinds out the win.
```

---

## System Interactions

```
StrategicResolverV0
  ← reads Fleet HP (hull, shield, zone armor)
  ← reads Fleet.Slots (weapons, mount types, radiators)
  ← reads CombatTweaksV0 (all constants)
  → returns StrategicResult (winner, hull remaining, salvage, frames)

NpcFleetCombatSystem
  ← reads Fleet.HullHp (destruction check)
  → writes LootDrop (deterministic roll)
  → writes NpcRespawnEntry (respawn queue)
  → frees Edge capacity

SustainSystem
  ← reads Fleet.Slots[].SustainInputs
  → writes Fleet.Slots[].Disabled (sustain failure = weapons offline)

MaintenanceSystem
  → writes Fleet.Slots[].Condition (module wear per 360 ticks)
  → repair costs connect combat damage to credits

SimBridge.Combat
  → exposes GetCombatStatusV0, GetHeatSnapshotV0, GetBattleStationsStateV0
  → exposes GetRadiatorStatusV0, GetSpinStateV0, ToggleBattleStationsV0
  → exposes ResolveCombatV0, DamageNpcFleetV0, ApplyTurretShotV0
```

---

## Design Gaps and Future Work

| Gap | Priority | Effort | Description |
|-----|----------|--------|-------------|
| **Pre-combat outcome projection** | CRITICAL | 1 gate | Player cannot see projected combat result before engaging. Need "Estimated: Victory 78%, hull damage 15-25" display. Run resolver on estimated enemy profile, show outcome range. |
| **Soft-kill states from depleted zones** | HIGH | 2 gates | Depleted zones only increase hull damage rate. Gate 1: zone-module mapping (Fore zone depleted → weapon module offline, Aft → thrust -50%). Gate 2: resolver integration + bridge display. BattleTech knockdown equivalent. |
| **Tracking/evasion by weapon size** | HIGH | 2 gates | No weapon-vs-target-size effectiveness. Gate 1: tracking stat on weapons, evasion on ship classes. Gate 2: hit probability modifier in resolver. Stellaris/Starsector pattern — makes fleet composition matter. |
| **Damage variance** | MEDIUM | 1 gate | Damage is fully deterministic (no variance). ±20% variance per hit would make near-equal fights unpredictable. Deterministic via hash (not RNG). |
| **Armor penetration stat** | MEDIUM | 1 gate | No weapon-specific armor penetration. Adding pen% per weapon creates "zone-stripper" weapon role (BattleTech HE equivalent). |
| **Combat duration pressure** | MEDIUM | 1 gate | Heat creates intensity pressure but not duration pressure. Battle Stations endurance timer (performance degrades after N ticks at combat RPM) would create Starsector CR equivalent. |
| **Subsystem targeting orders** | LOW | 2 gates | Pure HP attrition in resolver — no tactical choice during auto-resolution. Gate 1: pre-combat tactical order ("target radiators first"). Gate 2: resolver priority queue modifying hit distribution. |
| **Repair dock time** | LOW | 1 gate | Repairs are instant. Adding repair duration (proportional to damage) creates opportunity cost — ship unavailable for trading during repair. |
| **Component destruction within zones** | FUTURE | 2 gates | Depleted zone could randomly destroy weapon/module installed there. Gate 1: zone-to-slot mapping. Gate 2: destruction roll + replacement economy. |

---

## Constants Reference

All values in `SimCore/Tweaks/CombatTweaksV0.cs`:

```
# HP Defaults
DefaultHullHpMax             = 100    (player)
DefaultShieldHpMax           = 50     (player)
AiHullHpMax                  = 80     (NPC)
AiShieldHpMax                = 30     (NPC)
DefaultZoneArmor (Player)    = Fore 25 / Port 20 / Stbd 20 / Aft 15
DefaultZoneArmor (AI)        = Fore 20 / Port 15 / Stbd 15 / Aft 10

# Damage Families
KineticVsShieldPct           = 50
KineticVsHullPct             = 150
EnergyVsShieldPct            = 150
EnergyVsHullPct              = 50
PointDefenseCounterPct       = 200   (2× vs Missile/Torpedo users)

# Stances (Fore/Port/Stbd/Aft %)
Charge:                      50 / 20 / 20 / 10
Broadside:                   15 / 35 / 35 / 15
Kite:                        10 / 15 / 15 / 60

# Heat
DefaultHeatCapacity          = 1000
DefaultHeatPerShot           = 100
DefaultRejectionRate         = 150
OverheatDamagePct            = 50
LockoutThresholdMultiplier   = 2

# Radiators
BasicRadiatorBonusRate       = 75
AdvancedRadiatorBonusRate    = 150

# Battle Stations
BattleStationsSpinUpTicks    = 3
StandDownDamagePct           = 25
SpinningUpDamagePct          = 50

# Spin/Turn
DefaultSpinRpm               = 20
MaxSpinRpm                   = 50
TurnPenaltyBpsPerRpm         = 100
MaxTurnPenaltyBps            = 5000
StandardFireCadenceBps       = 6000
BroadsideFireCadenceBps      = 5000
SpinalFireCadenceBps         = 10000

# Strategic Resolver
StrategicMaxRounds           = 50
FleeMinRounds                = 3

# Escort
EscortShieldDmgReductionPct  = 25

# Loot
CommonWeight / UncommonWeight / RareWeight / EpicWeight = 60 / 25 / 12 / 3
DespawnTicks                 = 3600
```
