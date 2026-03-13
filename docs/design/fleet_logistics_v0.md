# Fleet Logistics & Upkeep — Design Spec

> Mechanical specification for fleet fuel consumption, ship maintenance,
> module sustain, construction, and the economic pressure loop that makes
> "standing still costs money." Companion to `ship_modules_v0.md` (fitting),
> `dynamic_tension_v0.md` (Pillar 2: Maintenance Treadmill), and
> `haven_starbase_v0.md` (station upgrades).

---

## AAA Reference Comparison

| Game | Drain Model | Degradation | Decision Quality |
|------|-------------|-------------|-----------------|
| **Starsector** | Three independent drains: supplies, fuel, CR (combat readiness). CR decays passively, drops fast in combat, repairs cost supplies+credits. Ships at 0% CR are crippled (weapon malfunction, engine stress). | Linear CR decay in transit; threshold penalties at 50%, 25%, 0%. Supply shortage triggers exponential morale collapse after 30 days. | Gold standard — three interlocking drains force real trade-offs. Shortage isn't instant death; you can limp home. |
| **X4 Foundations** | Crew salaries (monthly), ship maintenance (periodic), ammunition/fuel consumed in combat/travel. Real-time repair takes hours. | Unmaintained ships lose 1% efficiency/day to 50%. Below 50%, weapons jam (20% miss chance). | Repair queue bottlenecks create logistics gameplay. Supply chains are player-driven. |
| **Stellaris** | Naval capacity hard cap. Over-cap incurs exponential upkeep penalty (+5% per 1% over). Monthly energy+alloys drain per fleet. | Supply shortage: 0.5% ship damage/day. No active repair — heals only in friendly territory. | Naval cap is an elegant constraint. Upkeep is a tuning dial, not a catastrophe. |
| **Sunless Sea/Skies** | Fuel per travel action, supplies per time, Terror accumulation in darkness. Zero fuel = stranded. | Hull integrity from combat + hazards. Terror at 100% = crew mutiny. | Terror adds narrative weight. Fuel forces route planning with return-trip calculation. |
| **FTL** | Scrap (single currency), fuel per jump, supplies per tick. Every resource choice is permanent. | Permanent damage until station visit. Hull breaches expand exponentially. | Extreme scarcity makes every choice matter. Transparency creates visceral pressure. |

### Best Practice Synthesis

1. **Three independent drains** (Starsector) — one drain is ignorable, two are manageable, three force trade-offs. Our analog: fuel + maintenance + module sustain.
2. **Threshold-based penalties** (Starsector CR) — the player cares when condition hits 50%, not every 1%. Avoids micromanagement.
3. **Monthly batching** (Stellaris upkeep) — strategic planning, not per-tick bookkeeping. Our analog: sustain cycle every 360 ticks (1 game day).
4. **Narrative weight** (Sunless Sea) — logistics should feel like adventure, not accounting. Our FO commentary system can provide this.
5. **Visible runway** — the player should always see "you have N ticks of fuel remaining" with clear consequences.

---

## Current Implementation

### Systems

| System | File | Purpose | Status |
|--------|------|---------|--------|
| `SustainSystem` | `SimCore/Systems/SustainSystem.cs` | Fleet fuel consumption, module sustain cycle, auto-refuel | Implemented |
| `MaintenanceSystem` | `SimCore/Systems/MaintenanceSystem.cs` | Industry site health decay, efficiency coupling, repair | Implemented |
| `ConstructionSystem` | `SimCore/Systems/ConstructionSystem.cs` | Station/facility construction projects | Implemented |
| `LogisticsSystem` | `SimCore/Systems/LogisticsSystem.cs` | Autonomous NPC supply chain management | Implemented |

---

## Mechanical Specification

### 1. Fuel System (Operational Drain)

**Per-Tick Consumption**:
```
Player fleet: -FuelPerMoveTick (1) per tick while moving
NPC fleets:   -1 per 2 ticks (NpcFuelRateMultiplier = 0.5)
```

**Auto-Refuel** (at any node while idle/docked):
- Player: costs `RefuelCreditCostPerUnit` (1 credit) per unit
- NPC: free refuel to capacity
- Refuel amount = min(deficit, affordable credits)

**Fuel Capacity**:
- Default: 500 units (`DefaultFuelCapacity`)
- FuelTankMk1: +150 bonus
- FuelTankMk2: +350 bonus

**Empty Tank Consequence**:
- Fleet immobilized — cannot initiate lane traversal
- Modules with PowerDraw > 0 disabled
- Player must purchase fuel or wait for logistics delivery

**Design Intent**: Fuel is the "runway timer." A starting ship with 500 fuel
and 1/tick drain has 500 ticks of movement. At ~3-5 ticks per trade leg,
that's 100-170 trade runs before refueling. Fuel should rarely be a crisis
but always a consideration for long exploration runs.

### 2. Module Sustain Cycle (Equipment Drain)

Every `SustainCycleTicks` (360 ticks ≈ 1 game day):

```
For each equipped module with PowerDraw > 0:
  if fleet.FuelCurrent == 0:
    module.Disabled = true    → "No fuel — module offline"
  else:
    for each (goodId, qty) in module.SustainInputs:
      if cargo[goodId] < qty:
        module.Disabled = true  → "Missing [goodId] — module offline"
        break
    else:
      deduct all SustainInputs from cargo
      module.Disabled = false   → module operational
```

**All-or-nothing**: partial sustain inputs do not partially power a module.
If you need 2 Munitions and have 1, the module goes offline entirely.

**Design Intent**: Module sustain creates the "pentagon dependency" pressure
from `faction_equipment_and_research_v0.md`. T2 faction modules require
faction-specific goods to sustain, tying your loadout to your trade routes
and faction relationships. When warfronts embargo those goods, your
equipment degrades.

### 3. Ship/Module Condition (Maintenance Drain)

**Module Condition Decay** (per `ModuleConditionDecayCycleTicks` = 360 ticks):
```
For each equipped module:
  Condition -= ModuleConditionDecayPct (1%)
  if Condition <= 0:
    module.Disabled = true    → "Module worn out"
```

**Industry Site Health Decay** (per tick):
```
if site.SupplyLevel > 0:
  decay = site.DegradePerDayBps / TicksPerDay
else:
  decay = site.DegradePerDayBps × NoSupplyDecayMultiplier (2) / TicksPerDay

site.HealthBps -= decay  (with remainder accumulation)
```

**Efficiency Coupling** (industry sites):
```
if HealthBps >= CriticalHealthBps (5000 = 50%):
  efficiency = 100%
else:
  deficit = CriticalHealthBps - HealthBps
  penalty = (deficit / 1000) × EfficiencyPenaltyPer1000BpsBelowCritical (10 pct)
  efficiency = max(0%, 100% - penalty%)

Example: 25% health → deficit=2500 → penalty=25% → efficiency=75%
Example: 10% health → deficit=4000 → penalty=40% → efficiency=60%
```

**Repair (Credit-Based)**:
```
cost = (bpsToRepair / BpsBucketSize) × RepairCostPer1000Bps (5 credits)
Restores to MaxHealthBps (10000 = 100%)
Instant — no repair timer (see Future Work)
```

**Repair (Supply-Based)**:
```
Each supply unit restores BpsPerSupplyUnit (500) health
Consumes from site supply inventory
```

### 4. Construction System

**Project Lifecycle**:
1. Player initiates construction at a node
2. Validates: tech prerequisites met, credits available, capacity not exceeded
3. Project advances `ProgressPerTick` (1) each tick if credits sufficient
4. Each step costs `CreditCostPerStep / TicksPerStep` credits per tick
5. On step completion: logs event, checks if all steps done
6. Final completion: sets `Completed = true`

**Constraints**:
- `MaxTotalProjects`: 3 concurrent across galaxy
- `MaxProjectsPerNode`: 1 per station
- **Stalling**: insufficient credits pauses progress (no cancellation, no partial refund)

### 5. Logistics System (NPC Supply Chains)

**Autonomous fleet-based supply chain management**:

1. **Shortage Detection**: Scan industry sites for input deficits below buffer target
2. **Fleet Assignment**: Assign idle/docked fleet to highest-priority shortage
3. **Route Planning**: `TryPlanFromBestReachableSupplierDeterministic()` — find market with highest inventory, plan pickup+delivery legs
4. **Execution**: Pickup phase → Delivery phase, with retry logic (max 3 zero-pickup retries)
5. **Reservation System**: Optionally reserve supplier inventory during job execution

**Job Structure**: `GoodId`, `SourceNodeId`, `TargetNodeId`, `Amount`, `Phase` (Pickup/Deliver)

---

## Player Experience

### The Three Drains in Practice

| Drain | Frequency | Visibility | Crisis Threshold |
|-------|-----------|-----------|-----------------|
| **Fuel** | Per-tick when moving | HUD fuel bar | 0 = immobilized |
| **Module Sustain** | Every 360 ticks (1 day) | Module status icons | Missing goods = offline modules |
| **Site Maintenance** | Continuous decay | Station tab health bar | Below 50% = efficiency penalty |

### The Pressure Loop

```
Tick 0:   Player has 1000 credits, 500 fuel, full-condition ship
Tick 50:  First trade complete. +200 credits. Fuel at 450.
Tick 100: Module sustain cycle fires. Deducts 2 Munitions from cargo.
Tick 200: Station health at 90%. No action needed yet.
Tick 360: Second sustain cycle. If cargo empty: weapon module goes offline.
Tick 500: Fuel at 0 if player explored without trading. Immobilized.
```

**Key Insight**: The player should never feel punished, but should always feel
a gentle pull toward productive activity. The ~50-80 tick runway before going
broke (from `dynamic_tension_v0.md`) means the first trade decision matters.

---

## System Interactions

```
SustainSystem
  ← reads Fleet (fuel, cargo, modules)
  → writes Fleet.FuelCurrent (consumption)
  → writes Fleet.Slots[].Disabled (sustain failure)
  → writes PlayerCredits (refuel costs)
  → reads UpgradeContentV0 (module sustain input definitions)

MaintenanceSystem
  ← reads IndustrySites (health, supply level)
  → writes Site.HealthBps (decay)
  → writes Site.Efficiency (coupling)
  → writes Fleet.Slots[].Condition (module wear)
  → writes PlayerCredits (repair costs)

ConstructionSystem
  ← reads Tech unlocks, PlayerCredits
  → writes Construction.Projects (progress)
  → writes PlayerCredits (construction costs)

LogisticsSystem
  ← reads IndustrySites, Markets, Fleets
  → writes Fleet.CurrentJob (assignment)
  → enqueues LoadCargo/UnloadCargo intents

IndustrySystem
  ← reads Site.Efficiency (from MaintenanceSystem)
  → production scaled by efficiency
  → shortfall events when undersupplied
```

---

## Design Gaps and Future Work

### 6. Fleet Standing Costs (NOT YET IMPLEMENTED)

The doc's core thesis — "standing still costs money" — currently has no mechanical
implementation. Ships sit docked indefinitely at zero cost. This removes Pillar 2
(Maintenance Treadmill) from `dynamic_tension_v0.md` entirely.

**Proposed Mechanic** (Stellaris naval upkeep + Starsector CR decay hybrid):

```
Per DockUpkeepCycleTicks (360 ticks = 1 game day):
  For each player fleet:
    baseCost = ShipClassUpkeepCredits[fleet.ShipClass]
    moduleCost = sum of (module.UpkeepCreditsPerCycle) for equipped modules
    totalCost = baseCost + moduleCost

    if fleet.State == Docked:
      actualCost = totalCost × DockedUpkeepMultiplier (0.5)  // 50% while docked
    else:
      actualCost = totalCost

    if PlayerCredits >= actualCost:
      PlayerCredits -= actualCost
    else:
      // Delinquency: modules start failing
      fleet.DelinquentCycles += 1
      if DelinquentCycles >= DelinquencyGracePeriod (3 cycles):
        Disable highest-PowerDraw module first
        // Cascading: each cycle disables the next most expensive module
```

**Ship Class Base Upkeep**:

| Ship Class | Base Upkeep/Cycle | Typical Module Upkeep | Total/Cycle | Runway at 1000 credits |
|------------|-------------------|----------------------|-------------|----------------------|
| Shuttle | 2 | 1-3 | 3-5 | 200-333 cycles |
| Corvette | 5 | 3-8 | 8-13 | 77-125 cycles |
| Frigate | 10 | 8-15 | 18-25 | 40-56 cycles |
| Cruiser | 20 | 15-25 | 35-45 | 22-29 cycles |
| Dreadnought | 50 | 30-60 | 80-110 | 9-13 cycles |

**Target Runway**: A starting Corvette with 500 credits and 8 credits/cycle upkeep
has ~63 cycles (22,680 ticks ≈ 15.75 game days). Combined with fuel drain and
module sustain, this creates the 50-80 tick effective runway described in
`dynamic_tension_v0.md`.

**Docked vs. Idle Distinction**:
- **Docked** (at station): 50% upkeep — crew rests, systems in standby
- **Idle** (in space, no route): 100% upkeep — full operational readiness
- **Moving** (on route): 100% upkeep + fuel drain

**Design Intent**: The player should always feel a gentle pressure to trade.
A Corvette that sits docked for 60 cycles (21,600 ticks) burns through 240
credits doing nothing. That's a trade run's profit — wasted. But the delinquency
grace period (3 cycles) prevents instant punishment for brief AFK periods.

---

## Design Gaps and Future Work

| Gap | Priority | Effort | Description |
|-----|----------|--------|-------------|
| **Fleet standing costs** | CRITICAL | 2 gates | Ships sit docked at zero cost. See Section 6 above. Gate 1: SustainSystem credit drain per cycle + ship class upkeep table. Gate 2: delinquency cascade + bridge query + HUD display. |
| **Module sustain enforcement** | HIGH | 1 gate | SustainInputs schema exists but many modules have empty sustain requirements. T2 faction modules need real sustain costs. Content population gate. |
| **Repair timers** | MEDIUM | 1 gate | Repairs are instant. Should have dock time proportional to damage (Starsector CR recovery model). Creates repair queue bottlenecks. |
| **Construction cancellation** | MEDIUM | 1 gate | Cannot cancel stalled projects. Need cancel with partial refund (50-75%). Command + bridge + UI. |
| **Monthly logistics report** | MEDIUM | 2 gates | Dashboard panel showing: income vs. upkeep, fleet status, site health, 12-month projection, break-even line. Gate 1: data aggregation. Gate 2: UI rendering. |
| **Crew/morale** | LOW | 3 gates | No crew system. Future: officer morale from `FirstOfficerSystem`, crew satisfaction tied to profit months. Major new entity. |
| **Fuel depot networks** | LOW | 2 gates | No supply chain for fuel delivery. Only auto-refuel at any node. Gate 1: depot entity + logistics integration. Gate 2: player placement UI. |

---

## Constants Reference

All values in `SimCore/Tweaks/SustainTweaksV0.cs`, `MaintenanceTweaksV0.cs`, `ConstructionTweaksV0.cs`:

```
# Fuel
FuelPerMoveTick              = 1
NpcFuelRateMultiplier        = 0.5
DefaultFuelCapacity          = 500
RefuelCreditCostPerUnit      = 1
FuelTankMk1Capacity          = +150
FuelTankMk2Capacity          = +350

# Module Sustain
SustainCycleTicks            = 360  (1 game day)

# Maintenance
MaxHealthBps                 = 10000  (100%)
CriticalHealthBps            = 5000   (50%)
BpsPerSupplyUnit             = 500
SupplyConsumptionIntervalTicks = 10
NoSupplyDecayMultiplier      = 2
ModuleConditionDecayPct      = 1  (per cycle)
ModuleConditionDecayCycleTicks = 360
RepairCostPer1000Bps         = 5 credits
EfficiencyPenaltyPer1000BpsBelowCritical = 10 pct points

# Construction
MaxProjectsPerNode           = 1
MaxTotalProjects             = 3
ProgressPerTick              = 1
```
