# NPC Industry & Production — Design Spec

> Mechanical specification for NPC-driven industry, production chains, demand
> generation, and market dynamics. Companion to `trade_goods_v0.md` (good
> definitions), `fleet_logistics_v0.md` (supply chains), and
> `dynamic_tension_v0.md` (economic pressure philosophy).

---

## AAA Reference Comparison

| Game | Production Model | Market Integration | NPC Autonomy | Player Impact |
|------|-----------------|-------------------|--------------|--------------|
| **X4 Foundations** | Full supply chain simulation. Every station has explicit input→output recipes. NPC stations buy inputs, produce outputs, sell to market. Stations shut down without inputs. | Fully simulated — NPC stations ARE the market. Price = f(supply, demand). Player can build competing stations. | Complete — NPCs build, trade, and expand. Player optional. | Player can outcompete, monopolize, or disrupt entire sectors by controlling production. |
| **Starsector** | Abstract production per colony. Colony output = f(size, industry, accessibility). No per-tick simulation. | Colony output feeds sector market. Shortages from disrupted colonies create price spikes. | Semi-autonomous — NPCs react to market but don't build new colonies. | Player disrupts via raids/bombardment. Supply chains break regionally. |
| **Stellaris** | Per-pop job system. Pops work jobs → produce resources. District/building determines available jobs. | Internal market (market fee for conversion). Galactic market (empire-wide price). | Full empire AI manages pop/building/district allocation. | Player manages own empire. Can disrupt others via war/trade deals. |
| **Dwarf Fortress** | Per-workshop task queues. Dwarves carry raw materials to workshops, produce goods. Realistic logistics. | Internal barter. External trade via caravans (seasonal). | Dwarves are fully autonomous within player-set orders. | Player sets production orders. Economy emerges from dwarf behavior. |
| **STE (Ours)** | Dual-layer: IndustrySystem (efficiency-based production) + NpcIndustrySystem (demand/reaction). Sites have input→output recipes. Efficiency = min(available/required) across all inputs. | NPC industry drains market inventory (demand). Low stock triggers production boost (supply). Player observes price effects. | Background actors — consume and produce without player input. No NPC station construction. | Player profits from shortages. Can supply deficit goods for premium. War disrupts NPC chains. |

### Best Practice Synthesis

1. **Production chains must be visible** (X4) — the player should see what a station produces, what it needs, and where the bottleneck is. Our bridge exposes this via station tab.
2. **Shortages should cascade** (X4, Starsector) — if munitions production stops, downstream military sites suffer. Our efficiency coupling handles this.
3. **NPC industry should be background, not foreground** — unlike X4 where the player builds stations, our player trades. NPC industry exists to create trade opportunities.
4. **Demand pressure drives prices** (all games) — consuming goods from markets creates scarcity. Our inventory-based pricing model converts scarcity to higher prices automatically.
5. **Production should react to shortages** (Dwarf Fortress) — if a good runs out, nearby sites should increase output. Our reaction system handles this at a coarse granularity.

---

## Current Implementation

### Systems

| System | File | Purpose | Status |
|--------|------|---------|--------|
| `IndustrySystem` | `SimCore/Systems/IndustrySystem.cs` | Efficiency-based production, input consumption, output generation, degradation | Implemented |
| `NpcIndustrySystem` | `SimCore/Systems/NpcIndustrySystem.cs` | NPC demand consumption, low-stock production reaction | Implemented |
| `MaintenanceSystem` | `SimCore/Systems/MaintenanceSystem.cs` | Site health decay, supply consumption, repair | Implemented |
| `LogisticsSystem` | `SimCore/Systems/LogisticsSystem.cs` | Autonomous NPC supply chain management | Implemented |

### Entities

```
IndustrySite
  Id: string
  NodeId: string (market location)
  Active: bool
  RecipeId: string (content registry reference)
  Inputs: Dictionary<string, int>   (goodId → units per tick)
  Outputs: Dictionary<string, int>  (goodId → units per tick)
  Byproducts: Dictionary<string, int>
  Efficiency: float (0.0–1.0, computed from input availability)
  HealthBps: int (0–10000, basis points)
  DegradePerDayBps: int
  DegradeRemainder: long (accumulator for sub-tick precision)
  BufferDays: int (target input buffer depth)
  SupplyLevel: int
  ConstructionEnabled: bool
```

---

## Mechanical Specification

### 0. Dual-Layer Architecture — Why Two Systems?

**The Problem**: A single production system can't serve both realism and game feel.
Realistic production (IndustrySystem) requires matching inputs to outputs with
efficiency ratios, but this alone doesn't create the steady demand drain that
makes trading profitable. A pure demand system (NpcIndustrySystem) creates
scarcity but doesn't model actual production chains.

**The Solution**: Two layers that operate on the SAME sites but at DIFFERENT
frequencies and with DIFFERENT purposes:

| Layer | System | Tick Frequency | Purpose | Operates On |
|-------|--------|----------------|---------|-------------|
| **Production** | `IndustrySystem` | Every tick | Realistic input→output conversion with efficiency coupling | All active IndustrySites |
| **Background Economy** | `NpcIndustrySystem` | Every 10/20 ticks | Artificial demand drain + low-stock reaction to prevent market death | All active IndustrySites |

**Execution Order** (from `SimKernel.Step()`):
```
Tick N:
  ... (earlier systems) ...
  IndustrySystem.Process()           // line 100: consume inputs, produce outputs
  ... (shortfall events, other systems) ...
  NpcIndustrySystem.ProcessNpcIndustry()  // line 126: drain 2 units per input
  NpcIndustrySystem.ProcessNpcReaction()  // line 127: boost +5 per output if low
```

**Why this works**: IndustrySystem handles the realistic chain (ore → alloys →
munitions) with efficiency scaling. NpcIndustrySystem adds a constant background
drain that prevents markets from reaching equilibrium. Without the background
drain, IndustrySystem would consume exactly what it produces and markets would
stagnate. The drain creates the persistent scarcity gradient that makes trading
profitable.

**KNOWN GAP**: These two systems don't model INTER-SITE dependencies. An ore
shortage at Node A doesn't cascade to an alloys shortage at Node B because
NpcIndustrySystem drains each site independently. Cascading shortages require
LogisticsSystem to fail to deliver inputs, which happens naturally when source
markets are depleted — but the delay is uncontrolled and the cascade path is
invisible to the player. See Design Gaps.

### 1. Production Cycle (IndustrySystem)

**Per-Tick Processing**:
```
For each active IndustrySite (sorted by ID):
  1. Compute efficiency (basis points):
     effBps = min over all inputs of: floor(available × 10000 / required)
     Clamped to [0, 10000]

  2. Set site.Efficiency = effBps / 10000

  3. If effBps < 10000: emit ShortfallEvent per undersupplied input

  4. Apply degradation (health loss proportional to deficit)

  5. If effBps > 0:
     a. Consume inputs: consume = min(available, inputVal × effBps / 10000)
     b. Produce outputs: produced = outputVal × prodBps / 10000
        (prodBps includes advanced_refining tech boost if unlocked)
     c. Produce byproducts: same formula, skipping input/output conflicts

  6. If site.ConstructionEnabled: run minimal construction pipeline
```

**Efficiency Coupling**: Production scales linearly with the scarcest input. If a site needs 10 ore and 5 fuel but only has 3 ore, efficiency = 3000 bps (30%) and all production scales to 30%.

**Tech Boost**: `advanced_refining` tech adds +10% to output production (effBps + effBps/10) without increasing input consumption.

**Bounded Structure Rule**: A site cannot produce a good that is also one of its inputs (prevents self-feeding loops).

### 2. NPC Demand (NpcIndustrySystem)

**Demand Consumption** (every `ProcessIntervalTicks` = 10 ticks):
```
For each active IndustrySite:
  For each input good:
    consumed = min(currentStock, NpcDemandConsumptionUnits)  // 2 units
    market.Inventory[good] -= consumed
```

**Design Intent**: NPC demand is a continuous drain on market inventory. This creates the baseline scarcity that makes trading profitable. Without NPC demand, markets would fill up and prices would bottom out.

### 3. NPC Production Reaction (NpcIndustrySystem)

**Reaction Production** (every `ReactionIntervalTicks` = 20 ticks):
```
For each active IndustrySite:
  For each output good:
    if currentStock < LowStockThreshold (10):
      market.Inventory[good] += ReactionProductionBoost (5)
```

**Design Intent**: When output goods run low, NPC sites boost production. This prevents complete market collapse — goods never permanently disappear. But the reaction is slow (20 ticks vs 10 for demand), creating windows of scarcity the player can exploit.

### 4. Degradation Model

**Health Loss** (continuous, per-tick):
```
if site.DegradePerDayBps > 0 and site.HealthBps > 0:
  deficitBps = 10000 - effBps
  if deficitBps > 0:
    numer = DegradePerDayBps × deficitBps
    denom = TicksPerDay (1440) × 10000
    site.DegradeRemainder += numer
    dec = DegradeRemainder / denom
    site.HealthBps -= dec
```

**Key Properties**:
- Health only decays when efficiency is below 100% (inputs are scarce)
- Decay rate scales with deficit severity (more starved = faster decay)
- Uses remainder accumulation for sub-tick precision (no floating point)
- At HealthBps = 0, site is non-functional

### 5. Construction Pipeline (Minimal V0)

Two-stage looping recipe (CAP_MODULE_V0):
```
Stage 0: "fabrication"
  Input: composites (IndustryTweaksV0.Stage0InQty)
  Duration: Stage0DurationTicks
  Output: components (Stage0OutQty)

Stage 1: "assembly"
  Input: components (Stage1InQty)
  Duration: Stage1DurationTicks
  Output: cap_module (Stage1OutQty)

After Stage 1 completes → loop back to Stage 0
```

**Blocker Reporting**: When inputs are insufficient, the system reports `BlockerReason` and `SuggestedAction` for UI display.

### 6. Recipe Validation

`ValidateRecipeBindings()` verifies at startup that every IndustrySite with a non-empty RecipeId references a recipe in the content registry. Prevents orphan recipes from silently failing.

---

## Production Chain Architecture

### Chain Depth (from trade_goods_v0.md)

```
Tier 0 (Raw):     organics, ore, rare_metals, gases, ice
                   ↓ (extracted by natural sources)
Tier 1 (Refined): fuel, composites, alloys, chemicals
                   ↓ (processed by refineries)
Tier 2 (Advanced): munitions, electronics, exotic_matter
                   ↓ (manufactured by factories)
Tier 3 (Complex):  luxury_goods (consumer demand endpoint)
```

Max chain depth: 3 (raw → refined → advanced → complex).

### Geographic Distribution

Production sites are hash-distributed across the galaxy:
- Core systems: higher concentration of Tier 2-3 production
- Frontier: dominated by Tier 0-1 extraction
- Rim: sparse, specialized (exotic_matter, rare_metals)
- Void: no standard production

This creates natural trade routes: raw materials flow inward, manufactured goods flow outward.

---

## Player Experience

### The Demand-Supply Dance

```
Tick 0:    Galaxy spawns with balanced supply/demand at each node
Tick 10:   NPC demand fires — each site consumes 2 units of each input
Tick 20:   NPC reaction fires — low-stock outputs get +5 boost
Tick 50:   Player sees price differentials between nodes
           (Node A: ore surplus, cheap. Node B: ore deficit, expensive.)
Tick 100:  If player trades ore A→B, they profit from the spread
           If player ignores it, NPC reaction slowly rebalances
Tick 200:  War starts at a contested node → WarfrontDemandSystem
           drains munitions/composites/fuel at elevated rates
Tick 300:  Wartime drain outpaces NPC reaction → persistent shortage
           Player can supply war goods for premium prices
```

### Price Formation

Markets use inventory-based pricing. NPC industry creates the baseline:
- **High inventory** → low price (supply exceeds demand)
- **Low inventory** → high price (demand exceeds supply)
- **Zero inventory** → maximum price (complete scarcity)

NPC demand ensures inventory slowly drains. NPC reaction ensures it slowly refills. The gap between drain rate (2/10 ticks) and refill rate (5/20 ticks) creates the "living market" feel.

---

## System Interactions

```
NpcIndustrySystem
  ← reads IndustrySites (active, inputs, outputs)
  ← reads Markets (inventory levels)
  → writes Market.Inventory (demand consumption)
  → writes Market.Inventory (reaction production)

IndustrySystem
  ← reads IndustrySites (recipes, inputs, outputs, byproducts)
  ← reads Markets (input availability)
  ← reads Tech (advanced_refining boost)
  → writes Site.Efficiency (computed each tick)
  → writes Market.Inventory (consume inputs, produce outputs)
  → writes Site.HealthBps (degradation)
  → emits ShortfallEvents (undersupplied inputs)
  → emits IndustryEvents (construction lifecycle)

WarfrontDemandSystem
  → drains Market.Inventory at contested nodes (war consumption)
  → overrides NPC reaction rates during wartime

LogisticsSystem
  → moves goods between markets via NPC fleets
  → responds to ShortfallEvents (shortage detection)

MarketSystem
  ← reads Market.Inventory for price computation
  → price = f(inventory, demand) — NPC industry shapes both
```

---

## Design Gaps and Future Work

| Gap | Priority | Effort | Description |
|-----|----------|--------|-------------|
| **Production chain visualization** | HIGH | 2 gates | Player cannot see full chain (ore → alloys → munitions). Gate 1: chain traversal query in bridge. Gate 2: GDScript panel with node-edge layout. |
| **NPC station construction** | HIGH | 3 gates | NPCs cannot build new production sites. Gate 1: profitability evaluator. Gate 2: NPC build decision system. Gate 3: construction + market integration. |
| **Demand elasticity** | MEDIUM | 1 gate | NPC demand is fixed (2 units/10 ticks regardless of price). Add price-responsive drain: consume less when price > 2× base. |
| **Production scaling** | MEDIUM | 1 gate | Sites produce at fixed rates. Add upgrade tiers (player-funded capacity increase at existing sites). |
| **Byproduct economy** | LOW | 2 gates | Byproducts exist but aren't economically significant. Gate 1: waste accumulation + environmental penalty. Gate 2: recycling chain recipes. |
| **Seasonal demand** | LOW | 1 gate | No temporal variation. Add event-driven demand spikes (tied to PressureSystem events or calendar cycles). |
| **Worker population** | FUTURE | 4+ gates | No crew/worker system. Major new entity (population, jobs, migration). Stellaris-scale feature. |

---

## Constants Reference

All values in `SimCore/Tweaks/NpcIndustryTweaksV0.cs` and `SimCore/Tweaks/IndustryTweaksV0.cs`:

```
# NPC Industry
ProcessIntervalTicks         = 10    (demand consumption frequency)
NpcDemandConsumptionUnits    = 2     (units consumed per input per cycle)
ReactionIntervalTicks        = 20    (production reaction frequency)
LowStockThreshold            = 10    (stock level triggering reaction)
ReactionProductionBoost      = 5     (units produced per reaction)

# Industry System
TicksPerDay                  = 1440
Bps                          = 10000 (basis point scale)
ProductionEfficiencyBoostDivisor = 10 (advanced_refining: +10%)

# Degradation
# DegradePerDayBps: per-site (set at worldgen)
# Health loss per tick = DegradePerDayBps × (10000 - effBps) / (1440 × 10000)
```
