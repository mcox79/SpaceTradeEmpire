# Warfront Mechanics — Design Spec

> Mechanical specification for the warfront, territory control, and strategic
> warfare systems. Companion to `dynamic_tension_v0.md` (philosophy),
> `factions_and_lore_v0.md` (faction identities), and `trade_goods_v0.md`
> (economic foundations).

---

## AAA Reference Comparison

| Game | Territory Model | War Lifecycle | Economic Integration | Player Role |
|------|----------------|---------------|---------------------|-------------|
| **Stellaris** | Claims system, war goals, status quo. War exhaustion forces peace after prolonged conflict. Occupying systems requires armies. | Declaration → War Score accumulation → Status Quo or Surrender. Exhaustion timer creates mutual pressure. | War consumes alloys/energy. Economy must support fleet upkeep. Conquered systems need integration time. | Emperor — total control. |
| **Starsector** | Faction territory defined by system control. Invasions require ground forces. | Raids → Saturation Bombardment → Ground Invasion. No formal war declaration. | Disrupted supply creates market volatility. Faction industry outputs drop during invasion. | Commander — direct fleet control. |
| **X4 Foundations** | Sector ownership via station presence. War is continuous background. | Station building → Fleet Orders → Territory expansion. Economic warfare (outcompete production). | Full production chain simulation. Stations produce goods, sell to market. War disrupts chains. | CEO — build and manage. |
| **SPAZ 2** | Zone capture via influence. Difficulty-numbered nodes. Connected graph with red edges for contested zones. | Territory shifts as factions gain/lose influence. Player tips balance. | Zone bonuses (mining, research) tied to territory. | Agent — influence outcomes. |
| **STE (Ours)** | Faction territory via BFS from homeworld. Warfronts are seeded at worldgen. Contested nodes form frontline. | Cold War escalation → Hot War attrition → Supply-driven ceasefire. 5 intensity levels. | War consumes goods at contested nodes. Price spikes ripple outward. Player profits from or suffers from war. | **Pilot-entrepreneur** — you supply wars, you don't fight them. |

### Best Practice Synthesis

1. **War exhaustion is essential** — prevents infinite stalemates (Stellaris). Our analog: fleet strength attrition + supply-driven ceasefire.
2. **Economic consequences > military consequences** — for a trading game, the player should feel war through prices, not battlefields (X4, Starsector).
3. **Territory shifts should be visible** — galaxy map must show who controls what, and changes should be telegraphed (SPAZ 2's color-coded nodes).
4. **Player agency is indirect** — you supply a side, you don't command armies. This is Han Solo, not Admiral Ackbar (Mount & Blade's faction-joining model).

---

## Current Implementation

### Systems

| System | File | Purpose | Status |
|--------|------|---------|--------|
| `WarfrontEvolutionSystem` | `SimCore/Systems/WarfrontEvolutionSystem.cs` | Intensity transitions, fleet attrition, objective capture | Implemented |
| `WarConsequenceSystem` | `SimCore/Systems/WarConsequenceSystem.cs` | Narrative feedback for player supply deliveries | Implemented |
| `WarfrontDemandSystem` | `SimCore/Systems/WarfrontDemandSystem.cs` | War-driven goods consumption, supply-shift de-escalation | Implemented |

### Entities

```
WarfrontState
  Id: string
  CombatantA, CombatantB: string (faction IDs)
  Intensity: 0-4 (Peace, Tension, Skirmish, OpenWar, TotalWar)
  WarType: Hot | Cold
  TickStarted: int
  ContestedNodeIds: List<string>
  FleetStrengthA, FleetStrengthB: int (0-100)
  Objectives: List<WarfrontObjective>

WarfrontObjective
  NodeId: string
  Type: SupplyDepot | CommRelay | Factory
  ControllingFactionId: string
  DominantFactionId: string
  DominanceTicks: int (0..CaptureDominanceTicks)
```

---

## Mechanical Specification

### 1. Intensity Ladder

War intensity is a 5-level enum governing all warfront behavior:

| Level | Name | Tariff Surcharge | Goods Drain | Fleet Attrition | Lane Effects |
|-------|------|-----------------|-------------|----------------|-------------|
| 0 | **Peace** | None | None | None | Normal |
| 1 | **Tension** | +500 bps (+5%) | 25% of war rates | None | Normal |
| 2 | **Skirmish** | +1000 bps (+10%) | 50% of war rates | Base rate | +10% delay (Shimmer instability) |
| 3 | **OpenWar** | +1500 bps (+15%) | 75% of war rates | 2× base | +20% delay (Drift instability) |
| 4 | **TotalWar** | +2000 bps (+20%) | 100% of war rates | 3× base | +40% delay (Fracture instability) |

### 2. Escalation and De-escalation

**Cold War Escalation** (Tension → OpenWar):
- Window: tick 200 to tick 600
- Probability: 5% per tick (deterministic: `hash % 20 == 0`)
- Intensity increases by 1 per event
- Maximum one transition per evaluation

**Hot War Ceasefire** (OpenWar → Peace):
- Window: tick 600 to tick 1200
- Probability: 3% per tick (deterministic: `hash % 33 == 0`)
- Intensity decreases by 1 per event
- Accelerated by supply deliveries (see Supply Shift below)

**Supply-Driven Shift**:
- Player delivers war goods (munitions, composites, fuel) to contested nodes
- Cumulative deliveries tracked in `WarSupplyLedger[warfrontId][goodId]`
- When total deliveries ≥ `SupplyShiftThreshold` (500 units): intensity −1, ledger resets
- Design intent: the player can actively broker peace through trade

### 3. Fleet Attrition

Applies only at Skirmish intensity (≥2):

```
attrition_per_tick = BaseAttritionPerTick × (intensity - 1)

if faction has no recent WarSupplyLedger entries:
    attrition_per_tick += UnsuppliedAttritionBonus (2)

FleetStrength = max(0, FleetStrength - attrition_per_tick)
```

When either faction's fleet strength hits 0: war de-escalates to Tension.

**Constants**:
- `BaseAttritionPerTick`: 1
- `UnsuppliedAttritionBonus`: 2
- `MaxFleetStrength`: 100
- `SupplyRestorePerDelivery`: 5

### 4. Strategic Objectives

Three capturable points per warfront:

| Type | Control Bonus | Strategic Value |
|------|--------------|----------------|
| **SupplyDepot** | +3 fleet strength restore/tick to controller | Sustains military presence |
| **CommRelay** | Intelligence advantage (future: reveal enemy fleet positions) | Information warfare |
| **Factory** | +1 fleet strength regen/tick | Industrial capacity |

**Capture Mechanic**:
- Dominant faction = whichever has higher fleet strength at the objective's node
- Accumulates `DominanceTicks` each tick the dominant faction leads
- At `CaptureDominanceTicks` (30 ticks): objective captured by dominant faction
- Losing dominance resets counter to 0

**CRITICAL: Territory Shift on Capture (NOT YET IMPLEMENTED)**

When an objective is captured, the following SHOULD happen but currently does NOT:

```
On objective captured by faction F:
  1. objective.ControllingFactionId = F
  2. Recompute contested node list:
     - BFS from each combatant homeworld (depth ≤3)
     - Contested = nodes where A-controlled borders B-controlled
     - Update warfront.ContestedNodeIds
  3. If all 3 objectives held by same faction:
     - War de-escalates by 2 intensity levels (decisive victory)
     - Losing faction's fleet strength set to 25 (not 0 — avoidable rout)
  4. Territory disc on galaxy map updates (SPAZ 2 model):
     - Faction color shifts at captured node
     - Toast: "Valorin captured Supply Depot at Proxima"
  5. Market consequences at newly-controlled node:
     - Tariff regime switches to capturing faction's trade policy
     - Embargo list updates (winner's embargo replaces loser's)
  6. PressureSystem.InjectDelta("warfront", "territory_shift", 800)
```

Without this mechanic, objectives are captured but nothing changes. Territory
is frozen at worldgen. The warfront "ebb and flow" described in the Player
Experience section cannot occur.

### 5. War Goods Consumption

`WarfrontDemandSystem` drains contested node markets each tick:

```
drain = (multiplierPct - 100) × intensity / (TotalWarIntensity × 100)
```

| Good | Multiplier | At TotalWar (intensity=4) |
|------|-----------|--------------------------|
| Munitions | 400% | 4× normal drain |
| Composites | 250% | 2.5× normal drain |
| Fuel | 300% | 3× normal drain |

### 6. War Consequences (Narrative Layer)

When the player sells war goods at a warfront-adjacent node:
- Creates `WarConsequence` entity with 150-tick delay
- Immediate manifest text (what you did)
- Delayed consequence text (what happened because of it)

Example narrative strings:
- Munitions → "Your munitions fueled an offensive. A civilian transport was caught in the crossfire."
- Composites → "Your composites reinforced a military station. The school module was converted to barracks."
- Fuel → "Your fuel extended patrol range. Three smugglers were intercepted. One was carrying medicine."

Design intent: make the player feel the moral weight of war profiteering.

---

## Player Experience

### Phase 1: War as Backdrop (ticks 0–150)
The player sees warfront indicators on the galaxy map. Prices at contested nodes
are elevated. Toast notifications report frontline shifts. The war is opportunity.

### Phase 2: War as Pressure (ticks 150–600)
Escalation drives up tariffs. The neutrality tax grows. The player's automation
programs stall as margins shrink. War is now a constraint to work around.

### Phase 3: War as Choice (ticks 600–1200)
The shrinking middle forces a decision: ally with a faction (cheap access, lost
freedom) or stay independent (climbing costs, universal access). Supply deliveries
can broker ceasefire — but that requires committing cargo capacity to war goods
instead of profit.

### Phase 4: War as Crisis (ticks 1200+)
Total War intensity destabilizes entire trade regions. Instability cascades break
lane infrastructure. The player must choose between fracture routes (trace risk)
or expensive overland paths through contested space.

---

## System Interactions

```
GalaxyGenerator.SeedWarfrontsV0
  → creates WarfrontState entities at worldgen
  → seeds 1 hot + 1 cold warfront

WarfrontEvolutionSystem
  ← reads WarSupplyLedger (player deliveries)
  → writes FleetStrength (attrition/restoration)
  → writes Objective state (capture/loss)
  → intensity transitions (escalation/ceasefire)

WarfrontDemandSystem
  ← reads Warfronts + Markets + Nodes
  → drains Market.Inventory at contested nodes
  → writes WarSupplyLedger (tracks cumulative deliveries)
  → triggers intensity shift at SupplyShiftThreshold

WarConsequenceSystem
  ← triggered by player trade events at warfront nodes
  → creates delayed narrative consequences

InstabilitySystem
  ← reads warfront intensity for contested nodes
  → raises node instability → lane delay scaling

MarketSystem
  ← reads warfront intensity for tariff surcharges
  → applies neutrality tax at intensity ≥2

ReputationSystem
  ← war profiteering: sell war goods → +2 buyer rep, -1 enemy rep
  → rep tiers gate dock access, tech access, trade access
```

---

## Design Gaps and Future Work

| Gap | Priority | Effort | Description |
|-----|----------|--------|-------------|
| **Territory shift on capture** | CRITICAL | 3 gates | Objectives captured but nothing changes. See "Territory Shift on Capture" above. Blocked by: BFS recomputation, embargo update, tariff switch, map UI update. |
| **Starter placement** | HIGH | 1 gate | Player starter system should border a warfront, not be arbitrary. GalaxyGenerator change only. |
| **Pressure injection wiring** | HIGH | 1 gate | WarfrontDemandSystem must call PressureSystem.InjectDelta on intensity changes and goods shortages. See `dynamic_tension_v0.md` injection sources table. |
| **Fleet strength visibility** | HIGH | 1 gate | Player cannot see battle outcome odds or faction military status. Bridge query + UI panel. |
| **Dynamic escalation** | MEDIUM | 2 gates | Escalation probability is fixed 5%/3%. Should respond to economic conditions (supply level, fleet strength delta). |
| **Asymmetric consumption** | MEDIUM | 1 gate | War goods drain is symmetric — no advantage to cutting supply lines. Need per-faction supply tracking. |
| **Multi-faction wars** | LOW | 4+ gates | Current model is binary (A vs B). No coalition or three-way wars. Major architecture change. |
| **War narrative variety** | LOW | 1 gate | Consequence text is static (3 variants). Should be procedural with faction/node context. |
| **Faction contracts** | FUTURE | 3 gates | Explicit supply contracts from factions ("deliver 100 munitions for allied pricing"). Needs mission system integration. |
| **Ceasefire diplomacy** | FUTURE | 2 gates | Player-initiated peace negotiation beyond supply threshold. Needs diplomacy UI. |

---

## Constants Reference

All values in `SimCore/Tweaks/WarfrontTweaksV0.cs` and `SimCore/Tweaks/TerritoryRegimeTweaksV0.cs`:

```
MaxFleetStrength             = 100
BaseAttritionPerTick         = 1
UnsuppliedAttritionBonus     = 2
AttritionMinIntensity        = 2   (Skirmish)
SupplyRestorePerDelivery     = 5
SupplyShiftThreshold         = 500
CaptureDominanceTicks        = 30
FactoryRegenPerTick          = 1
SupplyDepotRestoreBonus      = 3
ColdWarEscalateMinTick       = 200
ColdWarEscalateMaxTick       = 600
HotWarCeasefireMinTick       = 600
HotWarCeasefireMaxTick       = 1200
NarrativeConsequenceDelay    = 150 ticks
MunitionsDemandMultiplier    = 400%
CompositesDemandMultiplier   = 250%
FuelDemandMultiplier         = 300%
NeutralityTax_Skirmish       = 500 bps
NeutralityTax_OpenWar        = 1000 bps
NeutralityTax_TotalWar       = 1500 bps
```
