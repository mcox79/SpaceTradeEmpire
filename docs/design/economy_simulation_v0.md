# Economy Simulation Design — v0

> **Governing document** for the SimCore economy engine, NPC population,
> ambient life, and off-screen simulation.
>
> Related docs: [trade_goods_v0](trade_goods_v0.md),
> [npc_behavior_v0](npc_behavior_v0.md), [npc_industry_v0](npc_industry_v0.md),
> [market_pricing_v0](market_pricing_v0.md),
> [dynamic_tension_v0](dynamic_tension_v0.md),
> [factions_and_lore_v0](factions_and_lore_v0.md),
> [reputation_factions_v0](reputation_factions_v0.md),
> [fleet_logistics_v0](fleet_logistics_v0.md),
> [haven_starbase_v0](haven_starbase_v0.md)

---

## Table of Contents

1. [Design Philosophy](#1-design-philosophy)
2. [Economy Architecture Overview](#2-economy-architecture-overview)
3. [NPC Fleet Population](#3-npc-fleet-population)
4. [NPC Trade AI](#4-npc-trade-ai)
5. [Production & Industry](#5-production--industry)
6. [Market & Pricing](#6-market--pricing)
7. [Sinks & Faucets Ledger](#7-sinks--faucets-ledger)
8. [Economy Bootstrap & Self-Regulation](#8-economy-bootstrap--self-regulation)
9. [Ambient Life & World Presence](#9-ambient-life--world-presence)
10. [Off-Screen Simulation (LOD Economy)](#10-off-screen-simulation-lod-economy)
11. [Station Ecosystem](#11-station-ecosystem)
12. [Wartime Economic Effects](#12-wartime-economic-effects)
13. [Player Impact on Economy](#13-player-impact-on-economy)
14. [Economy Health Monitoring](#14-economy-health-monitoring)
15. [Industry Best Practices Applied](#15-industry-best-practices-applied)
16. [Known Gaps & Roadmap](#16-known-gaps--roadmap)

---

## 1. Design Philosophy

### Core Thesis

The economy is not a background system — **the economy IS the game**. Every
trade decision, faction relationship, and route choice is an economic act. The
player is Han Solo, not Emperor Palpatine: a trader-pilot feeling galactic wars
through their wallet, not through fleet commands.

### Design Principles

| Principle | Meaning | Reference Game |
|-----------|---------|----------------|
| **Demand-driven circulation** | Permanent sinks create trade pressure; NPCs respond to price gradients, not scripts | X4: Foundations |
| **Structural scarcity by design** | Food production < consumption. Pentagon ring prevents faction self-sufficiency | Eve Online (regional scarcity) |
| **Pain before relief** | Player must feel the treadmill before unlocking automation | Factorio |
| **War felt through prices** | Warfront demand spikes cascade through production chains | Offworld Trading Company |
| **13 goods, infinite depth** | Depth through interconnection, not item count | Escape Velocity |
| **Deterministic simulation** | Integer-only math, sorted iteration, reproducible golden hashes | Unique to STE |
| **Presentation ≠ Simulation** | SimCore runs headless. Godot renders. SimBridge is the only crossing | STE architecture |

### The Value Chain (per LostGarden methodology)

Every good in the economy must satisfy a complete value chain:

```
SOURCE (faucet) → TRANSFORM → SINK (drain) → PLAYER ANCHOR (motivation)
```

**Strong sinks create pull.** If a good has no compelling sink, it pools and
becomes worthless. Every good must trace back to a player motivation:

| Good | Final Sink | Player Anchor |
|------|-----------|---------------|
| Fuel | Ship operation, industry input | Autonomy (freedom to move) |
| Ore | Metal production | Competence (build capability) |
| Munitions | Combat, warfront supply | Mastery (combat effectiveness) |
| Composites | Armor, warfront supply | Survival (defensive capability) |
| Components | Automation programs | Growth (empire building) |
| Exotic Matter | T3 modules, Haven upgrades | Discovery (endgame exploration) |
| Food | Station consumption, crew sustain | Obligation (keep civilization running) |

Goods without strong sinks (e.g., Electronics currently only feeds Components)
need additional demand drivers to maintain value chain pull.

---

## 2. Economy Architecture Overview

### System Tick Order (SimKernel.Step)

The economy processes in this deterministic order every tick:

```
1. IndustrySystem.Process()         — Production: inputs consumed, outputs generated
2. StationConsumptionSystem.Process() — Life-support: Food + Fuel consumed per station
3. NpcIndustrySystem.ProcessNpcIndustry() — Background demand drain (every 10 ticks)
4. NpcIndustrySystem.ProcessNpcReaction()  — Low-stock reactive boost (every 20 ticks)
5. NpcTradeSystem.ProcessNpcTrade()  — Fleet AI: evaluate, buy, travel, sell
6. MarketSystem.Process()            — Price publication cadence, edge heat decay
```

**Why this order matters:** Production happens before consumption, ensuring
fresh goods are available. NPC trade evaluates last, seeing the true post-
production/post-consumption state. This prevents NPCs from trading on stale
price signals.

### The Three Economic Layers

```
┌─────────────────────────────────────────────────────────┐
│  Layer 1: PRODUCTION (IndustrySystem)                   │
│  Deterministic input→output conversion. Efficiency-     │
│  scaled. Creates goods from raw materials.              │
├─────────────────────────────────────────────────────────┤
│  Layer 2: CIRCULATION (NpcTradeSystem + MovementSystem) │
│  NPC fleets move goods between nodes based on price     │
│  gradients. Creates geographic redistribution.          │
├─────────────────────────────────────────────────────────┤
│  Layer 3: CONSUMPTION (StationConsumption + NpcIndustry │
│  + ModuleSustain + WarfrontDemand)                      │
│  Permanent sinks that destroy goods, creating the       │
│  scarcity gradients that drive Layer 2.                 │
└─────────────────────────────────────────────────────────┘
```

---

## 3. NPC Fleet Population

### Current: Fixed Population at Galaxy Gen

NPC fleets are spawned once during `StarNetworkGen.SeedAiFleets()` and never
despawn or reproduce. Fleet count is a galaxy-gen constant.

**Per-node allocation by faction doctrine:**

| Faction | Traders | Haulers | Patrols | Total/Node | Design Intent |
|---------|---------|---------|---------|------------|---------------|
| Concord | 1 | 1 | 1 | 3 | Balanced governance |
| Chitin | 2 | 0 | 1 | 3 | Information traders |
| Weavers | 1 | 2 | 0 | 3 | Heavy logistics |
| Valorin | 2 | 1 | 3 | 6 | Military swarm |
| Communion | 1 | 0 | 1 | 2 | Sparse mystics |
| Unclaimed | 2 | 1 | 0 | 3 | Independent frontier |

**For a 20-node galaxy: ~60-70 NPC fleets total.**

### Fleet Properties at Spawn

| Property | Trader | Hauler | Patrol |
|----------|--------|--------|--------|
| Speed | 0.8 AU/tick | 0.7 AU/tick | 1.0 AU/tick |
| Max cargo | 10 units | 30 units | 0 (no cargo) |
| Eval interval | 15 ticks | 30 ticks | 15 ticks |
| Search radius | 1 hop | 2 hops (BFS) | N/A (circuit) |
| Weapons | Laser | Laser | Cannon |
| Initial cargo | Empty | Empty | N/A |

### RECOMMENDED: Dynamic Fleet Population (Phase 2)

**Problem:** Fixed fleet counts can't respond to economic conditions. A
20-node galaxy with 60 traders works, but topology gaps (isolated nodes with
no profitable routes) leave NPCs idle. Warfront losses permanently reduce
trade capacity.

**Solution: Population Pressure System**

Inspired by X4's faction fleet management, implement a soft-cap system:

```
TARGET_FLEET_COUNT = f(faction_territory_size, economic_health, war_status)

Every POPULATION_EVAL_TICKS (e.g., 500 ticks ≈ 8 game hours):
  For each faction:
    current = count of faction's active fleets
    target = faction.Territory.NodeCount * faction.Doctrine.FleetsPerNode

    if current < target * 0.7:
      // Faction is undersupplied — commission new fleet
      // Fleet spawns at faction's most prosperous station (highest avg inventory)
      // Cost: deduct goods from that station's market (Metal + Components)
      SpawnReplacementFleet(faction, bestStation)

    if current > target * 1.3:
      // Faction is oversupplied — retire oldest idle fleet
      // Fleet "returns to base" (removed from simulation)
      RetireFleet(faction, oldestIdleFleet)
```

**Design constraints:**
- Fleet spawning consumes station goods (Metal + Components) — tying population
  to economy health. A faction whose stations are starved can't replace losses.
- Fleet retirement only targets idle fleets (no cargo, docked, no profitable
  routes found in 3+ evaluation cycles).
- Spawn rate capped at 1 fleet per faction per eval cycle — prevents population
  explosions.
- **Determinism:** Spawn location, fleet ID, and timing are all deterministic
  from tick count + faction state.

**Why not full X4-style dynamic spawning:**
- X4 spawns ships from shipyards using full production chains (hull plates,
  engines, shields). This is elegant but requires 35+ goods and 50+ stations.
- STE's 13-good economy can't support shipbuilding as a production chain
  without feeling forced.
- The compromise: fleet replacement costs goods (economic drain) without
  requiring a full shipyard simulation.

### RECOMMENDED: Fleet Personality Seeds (Phase 2)

Currently all traders of the same role behave identically. Add a per-fleet
personality seed that modulates behavior:

```csharp
// In Fleet entity:
int PersonalitySeed;  // Set at spawn: FNV1a(fleetId)

// Modulates:
float RiskTolerance;     // 0.5-1.5: willingness to trade near warfronts
float ProfitThreshold;   // 2-5 cr: minimum margin to act
float GoodPreference;    // Bias toward specific good categories
int   EvalJitter;        // ±3 ticks: prevents synchronized evaluation
```

**Why:** Creates emergent diversity. Some NPCs are risk-takers who trade near
warfronts (high margin, high danger). Others are conservative, sticking to
safe core routes. The player sees variety in NPC behavior without scripting it.

---

## 4. NPC Trade AI

### The Trade Decision Loop

Every `EvalIntervalTicks` (15 for traders, 30 for haulers), each eligible fleet:

```
1. PRECONDITIONS: Must be NPC-owned, docked, not traveling, not on logistics job
2. SELL: Deliver all cargo to local market (automatic on dock)
3. EVALUATE: Scan reachable markets for profitable opportunities
4. SCORE: rank by (profitPerUnit × units × goodWeight)
5. FILTER: profitPerUnit >= ProfitThresholdCredits (3 cr)
6. ACT: Buy best opportunity, set destination, begin travel
```

### Good Trade Weights (Priority Signals)

| Good | Weight | Rationale |
|------|--------|-----------|
| Rare Metals | 150 | Scarce, high-value, strategic |
| Munitions | 140 | War demand, always consumed |
| Organics | 130 | Food chain bottleneck |
| Ore | 120 | Volume commodity, always needed |
| Components | 120 | Automation demand |
| Food | 110 | Universal consumption |
| Fuel | 100 | Baseline (everywhere) |
| Metal | 100 | Abundant processed good |
| Composites | 100 | Mid-tier processed |
| Electronics | 100 | Mid-tier processed |
| Salvaged Tech | 90 | Niche |
| Exotic Crystals | 80 | Rare, limited routes |
| Exotic Matter | 80 | Very rare, player-focused |

**Design intent:** Weights bias NPCs toward strategically important goods
without hardcoding routes. Munitions weight (140) ensures warfront-adjacent
stations get supplied. Organics weight (130) ensures food chains don't starve.

### Patrol Circuit Generation

Patrols don't trade. They follow deterministic 3-5 node circuits generated
via FNV1a hash:

```
target_hops = 3 + (hash(fleetId) % 3)
For each hop:
  candidates = adjacent unvisited nodes (sorted by ID)
  pick = hash(fleetId + "_" + hop) % candidates.Count
  circuit.add(candidates[pick])
```

**Fallback:** If circuit generation fails (dead-end topology), patrol
alternates between two adjacent nodes.

### RECOMMENDED: Enhanced NPC Trade Intelligence (Phase 2)

**Problem:** NPCs don't consider warfront risk, faction loyalty, or embargo
status when choosing routes. A Concord trader happily travels through a
Valorin warzone.

**Solution: Risk-Adjusted Scoring**

```
adjustedScore = baseScore * safetyMultiplier * loyaltyMultiplier

safetyMultiplier:
  No warfront: 1.0
  Adjacent to warfront: 0.7
  In warfront: 0.3 (only risk-tolerant fleets)

loyaltyMultiplier:
  Own faction market: 1.2
  Allied faction: 1.0
  Neutral: 0.8
  Hostile: 0.0 (won't trade at hostile stations)
```

**Problem:** NPCs have no memory of past trades. A trader that just sold
Munitions at Node A will immediately buy Munitions at Node B to sell back
at Node A if the spread is right.

**Solution: Trade Cooldown**

```
// Per-fleet: Dictionary<(goodId, nodeId), lastTradeTick>
// Skip opportunities where same good was traded at same node within 30 ticks
```

---

## 5. Production & Industry

### Production Chain Map

```
                    ┌──────────┐
            ┌──────▶│  METAL   │──────┬──────────┬──────────┐
            │       └──────────┘      │          │          │
     ┌──────┴──┐                ┌─────▼────┐ ┌───▼───┐ ┌───▼────────┐
     │   ORE   │                │MUNITIONS │ │COMPOS │ │COMPONENTS  │
     └─────────┘                │(+Fuel)   │ │(+Org) │ │(+Electron) │
                                └──────────┘ └───────┘ └────────────┘
     ┌─────────┐                                            ▲
     │ORGANICS │──┬──▶ FOOD (+Fuel)                         │
     └─────────┘  │                                    ┌────┴───────┐
                  └──▶ COMPOSITES (+Metal)             │ELECTRONICS │
                                                       │(+ExoCryst) │
     ┌─────────┐                                       └────────────┘
     │  FUEL   │──────▶ (consumed by everything)            ▲
     └─────────┘                                            │
                                                    ┌───────┴──────┐
     ┌──────────────┐                               │EXOTIC CRYSTALS│
     │ RARE METALS  │──▶ T2 weapon/module sustain   │(Fracture only)│
     └──────────────┘                               └──────────────┘

     ┌──────────────┐
     │ SALVAGED TECH│──▶ Metal (safe) OR Components (valuable)
     └──────────────┘

     ┌──────────────┐
     │EXOTIC MATTER │──▶ T3 modules, Haven upgrades, research
     └──────────────┘    (cannot be manufactured — discovery only)
```

### The Three Strategic Forks from Metal

Metal is the universal backbone. Its three downstream paths create the core
strategic tension:

| Fork | Recipe | End Use | Player Dilemma |
|------|--------|---------|----------------|
| **OFFENSIVE** | Metal + Fuel → Munitions | Combat, warfront supply | "Do I arm the warfront?" |
| **DEFENSIVE** | Metal + Organics → Composites | Armor, shields | "Do I protect my fleet?" |
| **ECONOMIC** | Metal + Electronics → Components | Automation programs | "Do I grow my empire?" |

The Organics fork adds a second layer: Organics → Food (feeds stations) vs.
Organics → Composites (feeds armor). Agricultural nodes face a "butter vs.
guns" decision.

### Industry Site Distribution

Sites are placed deterministically at galaxy gen based on node index:

| Site Type | Placement Rule | Inputs/tick | Outputs/tick |
|-----------|---------------|-------------|--------------|
| Fuel Well | Every 6th node | None | 5 Fuel |
| Mine | Even non-starter nodes | 1 Fuel | 5 Ore |
| Refinery | Odd non-starter nodes | 10 Ore + 1 Fuel | 5 Metal |
| Munitions Fab | Every 7th (offset 3) | 2 Metal + 1 Fuel | 3 Munitions |
| Food Processor | Odd agri nodes | 2 Organics + 1 Fuel | 3 Food |
| Composites Fab | Every 9th (offset 5) | 2 Metal + 1 Organics | 2 Composites |
| Component Asm | Every 11th (offset 7) | 3 Metal + 1 Electronics | 2 Components |
| Salvage Yard | Every 13th | Salvaged Tech | 3 Metal or 1 Components |

### Efficiency Calculation

```
efficiencyBps = min over all inputs of: floor(available × 10000 / required)
// Clamped to [0, 10000] (basis points)

// At 50% efficiency:
//   Consumes 50% of normal inputs
//   Produces 50% of normal outputs
//   Byproducts scale proportionally
```

**Cascading failure:** If a refinery's Ore supply is cut (e.g., mine's Fuel
ran out), it drops to 0% efficiency. This starves downstream Munitions Fab
of Metal, which drops to 0%. The cascade propagates through the entire
production chain. **This is intentional** — it creates the dynamic scarcity
that makes trade profitable.

### RECOMMENDED: Full Recipe Instantiation (Phase 2)

**Current gap:** 8 of 9 recipes have active industry sites. The missing
recipe is `AssembleElectronics` (Exotic Crystals + Fuel → Electronics).

**Impact:** The Electronics→Components chain can't cascade from in-world
production. Electronics exists only as a seeded good, not a produced one.
This breaks the Communion→Chitin→Weavers three-faction dependency chain
that is central to the pentagon ring's economic pressure.

**Priority:** HIGH. This is the most impactful single missing recipe —
it completes the pentagon ring's production chain.

---

## 6. Market & Pricing

### Deterministic Price Formula

```
midPrice = BasePrice + (IdealStock - currentStock)
   // BasePrice = 100, IdealStock = 50
   // stock=0 → mid=150 (scarcity), stock=100 → mid=50 (surplus)

spread = max(MinSpread, midPrice × SpreadBps / 10000)
   // MinSpread = 2, SpreadBps = 1000 (10%)

buyPrice  = midPrice + spread/2    // Player pays (ask)
sellPrice = midPrice - spread/2    // Player receives (bid)
```

### The Six Price Signals

Players read price through six simultaneous signals:

| Signal | Source | Player Question |
|--------|--------|-----------------|
| Scarcity gradient | Stock difference between nodes | "Where should I trade?" |
| Faction affinity | Pentagon ring exports vs imports | "What good, at which faction?" |
| Tariff differential | Faction rep at each endpoint | "Who should I trade with?" |
| War premium | Elevated demand at contested nodes | "When is it worth the risk?" |
| Instability volatility | Unstable nodes = higher prices + risk | "How much risk for how much reward?" |
| Embargo gaps | Blocked goods create secondary markets | "What can I smuggle?" |

### Full Pricing Pipeline

```
1. Base price (good definition or 100)
2. Scarcity adjustment (IdealStock - currentStock)
3. Spread (10% of mid, min 2 cr)
4. Reputation modifier (-15% Allied to +20% Hostile)
5. Faction good affinity (-15% export, +8% import)
6. Instability volatility (0-50% premium)
7. Security goods surcharge (Fuel/Munitions in war zones)
8. Transaction fee (1%, waived with Broker unlock)
9. Tariff (faction-specific, rep-scaled, war-surcharge)
```

See [market_pricing_v0](market_pricing_v0.md) for complete formulas.

### RECOMMENDED: Per-Good Base Prices (Phase 2)

**Current gap:** All goods use BasePrice=100. Design doc specifies price
bands (Low 50-100, Mid 150-300, High 400-800, VHigh 1000-2000) but
`GoodDefV0.BasePrice` and `PriceSpread` fields are unpopulated.

**Impact:** Exotic Matter and Fuel cost the same base price. This flattens
the value curve and eliminates the progression feel of trading higher-tier
goods.

---

## 7. Sinks & Faucets Ledger

### Faucets (Sources of Goods)

| Faucet | Cadence | Volume | Goods Created |
|--------|---------|--------|---------------|
| Fuel Wells | Every tick | 5/tick per well | Fuel |
| Mines | Every tick | 5/tick per mine | Ore |
| Refineries | Every tick | 5/tick per refinery | Metal |
| Food Processors | Every tick | 3/tick per processor | Food |
| Munitions Fab | Every tick | 3/tick per fab | Munitions |
| Composites Fab | Every tick | 2/tick per fab | Composites |
| Component Asm | Every tick | 2/tick per assembler | Components |
| NPC Reaction Boost | Every 20 ticks | 5 units if stock < 10 | Any output good |
| Agri Node Seed | Galaxy gen | 300 units at 40% of nodes | Organics |
| Mining Cluster Seed | Galaxy gen | 150 units at 15% of nodes | Rare Metals |
| Starter Inventory | Galaxy gen | 120-695 units per node | Fuel, Ore, Metal |

### Sinks (Consumers of Goods)

| Sink | Cadence | Volume | Goods Destroyed |
|------|---------|--------|-----------------|
| Station Consumption | Every 10 ticks | 1 Food + 1 Fuel per station | Food, Fuel |
| NPC Industry Demand | Every 10 ticks | 2 units per input per site | All input goods |
| Production Inputs | Every tick | Per recipe (efficiency-scaled) | Ore, Fuel, Metal, Organics, etc. |
| Module Sustain | Every 60 ticks | Per module type | Munitions, Composites, Exotic Matter, etc. |
| Bid-Ask Spread | Every NPC trade | ~20% of trade value | Credits (friction) |
| Transaction Fees | Every player trade | 1% of value | Credits |
| Tariffs | Every cross-faction trade | 3-20% of value | Credits |

### Structural Imbalances (By Design)

| Good | Production/tick | Consumption/tick | Balance | Intent |
|------|-----------------|------------------|---------|--------|
| Food | ~12 | ~20 (station consumption) | **Deficit** | Permanent scarcity drives trade |
| Fuel | ~45 | ~47 (industry + stations) | **Tight** | Marginal — one disruption tips it |
| Ore | ~50 | ~50 (refinery input) | **Balanced** | Stable backbone |
| Metal | ~25 | ~30 (3 downstream forks) | **Slight deficit** | Bottleneck good — always valuable |

**Design intent (per Eve Online methodology):** Structural deficits in Food
and Metal create permanent trade demand. No amount of NPC circulation can
solve a deficit — only the player bringing goods from surplus nodes to
deficit nodes resolves the pressure. This is the core gameplay hook.

### RECOMMENDED: Credit Sink/Faucet Accounting (Phase 2)

**Per Eve Online's Monthly Economic Report pattern:**

Track total credits created and destroyed per tick:

```csharp
// In SimKernel or EconomyMonitor:
long CreditsCreatedThisTick;   // NPC trade profits, mission rewards, loot
long CreditsDestroyedThisTick; // Fees, tariffs, upkeep, repair costs

// Log ratio every 100 ticks:
// Healthy: Created/Destroyed ratio between 0.95 and 1.05
// Inflationary: ratio > 1.1 (too many faucets)
// Deflationary: ratio < 0.9 (too many sinks)
```

This enables detecting economy drift before it becomes visible to players.

---

## 8. Economy Bootstrap & Self-Regulation

### Initial Inventory Seeding (MarketInitGen)

Galaxy gen creates intentional variance to guarantee profitable routes:

**All nodes:** Fuel=500 (high bootstrap to prevent early stall)

**Even nodes (mining archetype):**
- Non-starter: Ore 200-695 (wide variance), Metal 2-36, Fuel 30-149

**Odd nodes (refining archetype):**
- Non-starter: Metal 100-397, Ore 0-39, Fuel 10-89

**Geographic distribution:**
- 40% of nodes: 300 Organics (agri systems)
- 15% of nodes: 150 Rare Metals (mining clusters)

**Starter margin guarantee:** `GuaranteeStarterArbitrageV0` ensures the
player's starting station has ≥50 cr/unit margin to at least one adjacent node.

### Why the Economy Self-Starts

The bootstrap works because of **within-type variance:**

- Mine A: 200 Ore, Mine B: 695 Ore
- Refinery C: 100 Metal, Refinery D: 397 Metal
- With 10% bid-ask spread, the 495-unit Ore difference creates 4-7 cr margins
- The 3 cr profit threshold is low enough that even moderate differentials
  trigger NPC trades

**Faction market biases** (from `FleetPopulationTweaksV0.GetMarketBias`) add
200-unit surplus/100-unit deficit per faction's specialty goods:

| Faction | Surplus (+200) | Deficit (-100) |
|---------|----------------|----------------|
| Concord | Food, Fuel | Composites |
| Chitin | Electronics, Components | Rare Metals |
| Weavers | Composites, Metal | Electronics |
| Valorin | Rare Metals, Munitions | Exotic Crystals |
| Communion | Exotic Crystals | Food, Fuel |

### Self-Regulation Mechanisms

**The Thermostat Pattern** (per game economy best practices):

The economy has built-in negative feedback loops that prevent runaway states:

| Problem | Automatic Correction | Mechanism |
|---------|---------------------|-----------|
| Good stockpiles too high | NPCs stop buying (no profit margin) | ProfitThreshold filter |
| Good stockpiles too low | NPC Reaction Boost: +5 units if < 10 | NpcIndustrySystem |
| All routes unprofitable | NPCs idle, production accumulates, margins return | Natural rebalancing |
| One node hoards all goods | Adjacent nodes' prices rise (scarcity), attracting NPC deliveries | Price-gradient pull |
| Too many NPCs on one route | Route profits erode (goods depleted), NPCs scatter | Market clearing |

**What CAN'T self-correct:**
- Topology dead-ends (node with no profitable neighbors)
- Pentagon ring disruption (faction at war loses access to critical input)
- Player market manipulation (buying out a node's entire stock)

These are **intentional failure modes** — they create gameplay opportunities,
not bugs.

### RECOMMENDED: Thermostat Injection (Phase 2)

For extreme conditions (>100 ticks of zero trades at a node), add a
gradual market normalization:

```
If node has had zero NPC trades for STAGNATION_THRESHOLD ticks:
  For each good where stock > IdealStock * 2:
    stock -= 1 per tick (slow leak — "spoilage")
  For each good where stock < IdealStock * 0.2:
    stock += 1 per tick (slow trickle — "local production")
```

**This is a safety valve, not a primary mechanism.** The leak rate (1/tick)
is slow enough that active trade always dominates. It only matters for
truly isolated nodes.

---

## 9. Ambient Life & World Presence

### Design Goal

The galaxy must feel alive even when the player isn't actively trading.
NPCs should be visibly doing things — docking, undocking, traveling lanes,
orbiting stations. The player should never look at a station and see
emptiness.

### Architecture: SimCore Actors vs. Godot Props

**Critical distinction:** Economy NPCs (traders, haulers, patrols) are
SimCore entities with full simulation state. Ambient life NPCs are
**Godot-only visual props** with no SimCore backing. This preserves
determinism while adding visual richness.

```
┌──────────────────────────┐    ┌──────────────────────────┐
│      SimCore (C#)         │    │     Godot (GDScript)      │
│                           │    │                           │
│  Fleet entities           │    │  NPC ship scenes          │
│  Market state             │───▶│  Ambient traffic props    │
│  Industry sites           │    │  Station activity VFX     │
│  Production state         │    │  Mining operation visuals  │
│                           │    │  Civilian shuttle sprites  │
│  (deterministic,          │    │  (cosmetic only,          │
│   headless, hashed)       │    │   seed-driven,            │
│                           │    │   no gameplay effect)     │
└──────────────────────────┘    └──────────────────────────┘
```

### Ambient NPC Categories

#### Category 1: Station Traffic (Docking/Undocking)

**Source signal:** SimBridge exposes `GetNodeTrafficLevel(nodeId)` derived from:
- Number of economy NPCs with this node as destination
- Station prosperity (avg inventory / IdealStock)
- Faction population factor

**Godot implementation:**
- Spawn 2-6 cosmetic shuttle scenes around each station the player visits
- Shuttles follow simple approach/depart paths (Bezier curves)
- Docking bay lights flash when shuttles arrive
- Traffic density scales with `trafficLevel`:
  - Low (0-3): 1-2 shuttles, slow cadence
  - Medium (4-6): 3-4 shuttles, moderate cadence
  - High (7-10): 5-6 shuttles, busy cadence

**Reference:** Elite Dangerous station mail slots, X4 station docking pads.

#### Category 2: Mining Operations

**Source signal:** SimBridge exposes `GetNodeIndustryType(nodeId)`:
- `Mine` → mining drones visible near asteroids
- `Refinery` → processing VFX on station exterior
- `FuelWell` → extraction beam/pipeline VFX

**Godot implementation:**
- Mining nodes: 3-5 small drone sprites orbiting asteroid belt
- Drones follow elliptical paths, occasionally "return" to station
- Extraction beam VFX between station and nearest asteroid
- Production efficiency affects visual intensity (low efficiency = fewer
  drones, dimmer beams)

**Reference:** Eve Online mining barges, X4 mining stations.

#### Category 3: Patrol Presence

**Source signal:** SimCore patrol fleets already have deterministic circuits.
SimBridge exposes patrol fleet positions.

**Godot implementation:**
- Patrol ships rendered as full NPC ship scenes (same as economy NPCs)
- Patrol ships orbit station at ~15u radius when "on station"
- Transition to lane travel animation when advancing circuit
- Faction-specific ship models + paint schemes

**Reference:** Wing Commander Privateer system patrols.

#### Category 4: Civilian Traffic (Lane Population)

**Source signal:** Number of active NPC transits on a lane edge.

**Godot implementation:**
- When player is near a lane gate, render 1-3 distant ship sprites
  traversing the lane at various speeds
- Ships are billboarded sprites (not full 3D) — cheap to render
- Density proportional to lane usage (SimCore edge heat)
- Ships "arrive" and "depart" at lane gates with brief flash VFX

**Reference:** Freelancer trade lane traffic.

#### Category 5: Warfront Activity

**Source signal:** `GetWarfrontAtNode(nodeId)` — warfront intensity level.

**Godot implementation:**
- Contested nodes: distant explosion VFX, weapons fire particles
- Debris field sprites (destroyed ship hulls)
- Emergency broadcast audio snippets
- Refugee shuttle traffic (cosmetic shuttles fleeing warfront)
- Intensity scales with war level:
  - Skirmish: Occasional distant flashes
  - OpenWar: Frequent explosions, debris
  - TotalWar: Constant battle VFX, damaged station exterior

**Reference:** Homeworld background battles, Freelancer battleship encounters.

### Ambient Life Spawn Rules

```
On player arrival at system:
  trafficLevel = bridge.GetNodeTrafficLevel(nodeId)
  industryType = bridge.GetNodeIndustryType(nodeId)
  warfront = bridge.GetWarfrontAtNode(nodeId)
  factionId = bridge.GetNodeFaction(nodeId)

  SpawnStationTraffic(trafficLevel, factionId)
  SpawnIndustryVisuals(industryType)
  SpawnPatrolPresence(factionId)
  SpawnLaneTraffic(adjacentEdges)
  if warfront: SpawnWarfrontActivity(warfront.intensity)

On player departure:
  DespawnAllAmbient()  // Clean up — only render in current system
```

### RECOMMENDED: Station Interior Life (Phase 3)

When the player docks, the dock menu could show:
- Crowd density indicator (busy/quiet market)
- Faction-specific NPC portraits in trade UI
- Market "chatter" text snippets reflecting economy state
- "Ships docked" counter reflecting real SimCore fleet positions

---

## 10. Off-Screen Simulation (LOD Economy)

### Principle: Full Simulation, Selective Rendering

**SimCore runs the same logic for all nodes, always.** There is no LOD
in the simulation layer. Every tick, every node's production runs, every
NPC evaluates trades, every market updates. This is what makes the
economy deterministic and honest.

**LOD happens only in the Godot presentation layer:**
- Player's current system: Full 3D rendering, ambient life, VFX
- Adjacent systems: Lane gate indicators only (arrow + faction color)
- Distant systems: Galaxy map icons only

### What NPCs Do When Uninstantiated

This is a critical design question. The answer: **NPCs are never
uninstantiated in SimCore.** Every NPC fleet has full state every tick.

However, they are **unrendered** in Godot when the player isn't in their
system. When the player arrives at a system, `game_manager.gd` queries
SimBridge for all fleets at that node and instantiates ship scenes.

**The experience:** When the player warps to a new system, they see NPCs
already there, mid-activity. A trader might be docked selling cargo. A
patrol might be orbiting the station. A hauler might be arriving from
an adjacent lane gate. The world was running without them — and the
player can tell, because the economy state reflects real trades that
happened while they were elsewhere.

### What Makes This Work (vs. X4's Approach)

**X4's problem:** With thousands of NPC ships, X4 must LOD-sim distant
sectors. Ships in distant sectors get simplified AI — they teleport
between stations, skip pathfinding, and trades resolve instantly. This
creates the "quantum tunneling" effect where a distant sector's economy
can shift dramatically because unrendered NPCs trade at superhuman speed.

**STE's advantage:** With ~60-70 NPC fleets (not thousands), full
simulation is cheap. Every fleet runs the same decision loop regardless
of player location. No LOD needed in SimCore.

**The tradeoff:** Fewer NPCs means less visual diversity. This is solved
by the ambient life system (Section 9) adding cosmetic-only traffic.

### RECOMMENDED: Economy Digest for Distant Events (Phase 2)

Players should know the economy is alive even in distant systems. Add
a "Galactic Market Report" that summarizes off-screen economic events:

```
Every REPORT_CADENCE_TICKS (e.g., 100 ticks):
  For each node NOT in player's current system:
    Track: significant price changes (>20%), stockouts, war impacts

  Surface as:
    - "Market Alert: Fuel prices at [Node] up 35% — warfront demand"
    - "Trade Report: [Faction] haulers clearing Organics surplus at [Node]"
    - "Industry Alert: Refinery at [Node] stalled — Ore shortage"
```

This creates the feeling of X4's living universe without requiring
thousands of NPCs. The player reads about the economy running without
them, then decides whether to act on the information.

---

## 11. Station Ecosystem

### Station as Economic Hub

Each node in the galaxy represents a station with its own economic
identity. The station is not just a shop — it's a living economic entity
with production, consumption, traffic, and faction character.

### Station Economic Identity

Stations emerge from their resource distribution and industry sites:

| Archetype | Resources | Industry | Exports | Feel |
|-----------|-----------|----------|---------|------|
| Industrial Hub | Ore, Fuel | Mine + Refinery | Metal, Munitions | Factory: smoke stacks, freight traffic |
| Agri World | Organics, Fuel | Food Processor | Food, Organics | Farm: organic domes, slow traffic |
| Mixed Economy | Ore, Org, Fuel | Multiple | Composites, Food, Metal | City: diverse, busy |
| Rare Deposit | Ore, Fuel, Rare Metals | Mine | Rare Metals, Metal | Fortress: military presence |
| Fracture Border | Fuel, Crystal access | — | Electronics | Frontier: sparse, dangerous |
| Frontier | Fuel only | — | Nothing yet | Outpost: quiet, opportunity |

### Station Visual Tiers (by Prosperity)

**Prosperity = average(inventory / IdealStock) across all goods**

| Tier | Prosperity | Visual Cues | Traffic | Audio |
|------|-----------|-------------|---------|-------|
| Struggling (0-30%) | Low | Dim lights, damaged exterior, few docked ships | 1-2 shuttles | Quiet, static |
| Stable (30-60%) | Medium | Normal lighting, maintained exterior | 3-4 shuttles | Ambient hum |
| Prosperous (60-90%) | High | Bright lights, expanded docking, many ships | 5-6 shuttles | Busy market chatter |
| Booming (90%+) | Very High | Neon signage, construction scaffolding, traffic jams | 7+ shuttles | Crowded, announcements |

### Station NPC Presence (Non-Economy)

Beyond economy fleets, stations should show:

| NPC Type | Trigger | Behavior | Purpose |
|----------|---------|----------|---------|
| **Dockworkers** | Always at stations | Cargo loading animation at docked ships | Station feels staffed |
| **Security** | Faction patrols | 1-2 small ships orbiting at 10u | Faction presence |
| **Maintenance** | Prosperity < 50% | Repair drones on station hull | Station health visible |
| **Refugees** | Warfront adjacent | Shuttle traffic from warfront direction | War has consequences |
| **Faction Envoy** | Rep > Friendly | Unique ship docked, faction banner | Diplomatic presence |
| **Player programs** | Active AutoBuy/TradeCharter | Visible drone/shuttle with player colors | Your empire is working |

### RECOMMENDED: Station Personality Seeds (Phase 3)

Each station gets a personality seed at galaxy gen that determines:
- Station architecture variant (industrial/commercial/military/scientific)
- Ambient NPC dialogue flavor
- Market UI skin (faction-specific trade terminal aesthetics)
- Background music layer (faction theme + prosperity modifier)

---

## 12. Wartime Economic Effects

### War as Economic Event

Wars in STE are not primarily military events — they're economic
disruptions that create trading opportunities. The player doesn't
fight in wars; they profit from (or suffer from) them.

### Warfront Demand Multipliers

| Demand Tier | Good | Peacetime Demand | Wartime Multiplier | Effect |
|-------------|------|-----------------|-------------------|--------|
| Bulk | Munitions | 3/tick | 4× | Metal+Fuel spike |
| Premium | Composites | 2/tick | 2.5× | Organics competition with Food |
| Elite | Rare Metals | 1/tick | 2× | T2 weapon sustain competes |
| Support | Food | 1/tick | 1.5× | Crew sustain under stress |
| Support | Fuel | 1/tick | 2× | Fleet operations |

### Cascade Mechanics

```
War starts at Node X (Concord vs Valorin):
  1. Munitions demand at X rises 4×
  2. Metal demand at X rises (Munitions input)
  3. Ore demand at adjacent mines rises (Metal input)
  4. Fuel demand rises everywhere (universal input)
  5. Components production drops (Metal diverted to Munitions)
  6. Automation programs degrade (Components shortage)
  7. Player's TradeCharter programs slow down

  Time to full cascade: ~200-300 ticks (3-5 game hours)
```

**The player feels this:** Their automation programs start failing
because Components are scarce because Metal is being consumed by
Munitions production for the warfront. They didn't choose to
participate in the war, but the war reached them through the economy.

### Embargo & Smuggling Economics

Pentagon ring dependencies are embargoed during wartime:

| Faction | Embargoes | Creates Opportunity |
|---------|-----------|---------------------|
| Concord | Composites (from Weavers) | Smuggle Composites to Concord at 3× markup |
| Weavers | Electronics (from Chitin) | Smuggle Electronics to Weavers |
| Chitin | Rare Metals (from Valorin) | Smuggle Rare Metals to Chitin |
| Valorin | Exotic Crystals (from Communion) | Smuggle Crystals to Valorin |
| Communion | Food (from Concord) | Smuggle Food to Communion |

**Munitions: always embargoed in wartime.** Universal restriction —
the "blockade runner" archetype's bread and butter.

---

## 13. Player Impact on Economy

### Player as Economic Actor

The player is not an observer — they're the most powerful economic actor
in the galaxy. Their advantages over NPCs:

| Advantage | Player | NPC |
|-----------|--------|-----|
| Search radius | Entire galaxy map | 1-2 hops |
| Cargo capacity | Ship-dependent (upgradeable) | Fixed 10-30 |
| Route optimization | Human intelligence | Greedy algorithm |
| Risk assessment | Strategic, long-term | None (walks into warfronts) |
| Automation | Programs (TradeCharter, AutoBuy) | None |
| Market manipulation | Can buy out entire stocks | Volume-capped |

### Player Economic Actions

| Action | Economic Effect | Magnitude |
|--------|----------------|-----------|
| Buy goods | Raises local price (reduces stock) | Significant at low stock |
| Sell goods | Lowers local price (increases stock) | Significant at high stock |
| Trade charter (automation) | Continuous price pressure on route | Moderate, persistent |
| Haven market | Creates off-grid trade hub | Strategic (bypasses pentagon) |
| Warfront supply | Enables faction war effort | Major (faction viability) |
| Embargo running | Supplies embargoed goods | Major (price arbitrage) |
| Exploration | Discovers new goods sources | Transformative (Exotic Matter) |

### RECOMMENDED: Market Impact Visibility (Phase 2)

Players should see their economic footprint:

```
Trade History Panel:
  - Your trades this session: 47 transactions, 12,340 cr profit
  - Nodes you've most impacted: [Node A] (+23% Fuel price), [Node B] (-15% Ore)
  - NPC trade routes you've disrupted: 3 traders rerouted due to your purchases
  - Faction reputation delta from trade: Concord +12, Valorin -3
```

---

## 14. Economy Health Monitoring

### Telemetry Requirements (per Industry Best Practice)

Track these metrics every 100 ticks for economy health assessment:

| Metric | Healthy Range | Warning | Critical |
|--------|--------------|---------|----------|
| Active trade routes | >60% of possible routes | <40% | <20% |
| Avg NPC idle time | <30% of eval interval | >50% | >80% |
| Goods velocity | >5 units moved/tick galaxy-wide | <3 | <1 |
| Price variance | StdDev > 15 across nodes | <10 (too flat) | <5 (no gradients) |
| Stockout count | <3 nodes at 0 for any good | >5 | >10 |
| Credit inflation | Created/Destroyed ratio 0.95-1.05 | >1.2 or <0.8 | >1.5 or <0.5 |

### Economy Death Spiral Detection

**Deflationary spiral:** All goods accumulate → no profitable routes →
NPCs idle → prices flatten → player can't profit

**Detection:** If goods velocity < 1 for 200+ ticks, trigger thermostat
injection (Section 8).

**Inflationary spiral:** Goods consumed faster than produced → universal
stockouts → all production halts → cascade failure

**Detection:** If >50% of nodes have stockouts on any good for 100+
ticks, trigger production boost at source sites.

**Eve Online reference:** Eve publishes Monthly Economic Reports tracking
ISK velocity, sink/faucet ratios, and regional price indices. STE should
have equivalent internal monitoring, surfaced as in-game "Galactic Trade
Commission" reports for the player.

---

## 15. Industry Best Practices Applied

### From X4: Foundations

| X4 Pattern | STE Application | Status |
|------------|----------------|--------|
| Full production chains (everything manufactured) | 13-good chain with 9 recipes | 8/9 instantiated (Electronics missing) |
| Dynamic fleet spawning from shipyards | Soft-cap fleet replacement consuming goods | RECOMMENDED Phase 2 |
| Station worker bonuses | — (not applicable, no station management) | N/A |
| Sector-level economy simulation | Full per-node simulation every tick | IMPLEMENTED |
| Random ship spawning for content | Ambient cosmetic traffic | RECOMMENDED Phase 2 |

### From Elite Dangerous BGS

| Elite Pattern | STE Application | Status |
|---------------|----------------|--------|
| Background simulation ticks | SimCore ticks (continuous, not daily) | IMPLEMENTED |
| System states (Boom, Bust, War) | Warfront intensity + prosperity | Partially implemented |
| Trade influence on faction standing | Rep gain from trade | IMPLEMENTED |
| NPC traffic density reflects state | Ambient traffic from prosperity level | RECOMMENDED Phase 2 |
| Player actions affect BGS | Player trade impacts market prices | IMPLEMENTED |

### From Eve Online

| Eve Pattern | STE Application | Status |
|-------------|----------------|--------|
| Regional scarcity (goods exist in specific regions) | Pentagon ring + geographic distribution | IMPLEMENTED |
| ISK sinks/faucets tracking | Credit sink/faucet ledger | RECOMMENDED Phase 2 |
| Monthly Economic Report | Galactic Trade Commission reports | RECOMMENDED Phase 2 |
| NPC buy orders (market makers) | Station consumption as permanent demand | IMPLEMENTED |
| Player-driven pricing | Player trades impact prices | IMPLEMENTED |
| Structural material scarcity | Food deficit, Metal tight balance | IMPLEMENTED |

### From LostGarden Value Chains

| Pattern | STE Application | Status |
|---------|----------------|--------|
| Source→Transform→Sink→Anchor | 13-good chain mapped to player motivations | Designed |
| Match source/sink power curves | Production rates vs consumption rates | Implemented (food deficit intentional) |
| Strong sinks create pull | Station consumption, module sustain, warfront demand | IMPLEMENTED (sustain enforced via SustainSystem) |
| Avoid orphaned resources | Every good has at least one sink | Verified |
| Visible causality | Price breakdown UI showing all modifiers | RECOMMENDED Phase 2 |

### From Factorio

| Pattern | STE Application | Status |
|---------|----------------|--------|
| Pain before relief (manual before automation) | Manual trading → TradeCharter programs | IMPLEMENTED |
| Bootstrap economy must self-start | Wide inventory variance + low profit threshold | IMPLEMENTED |
| Production chain visibility | Trade goods chain visualization | RECOMMENDED Phase 3 |
| Bottleneck identification | Price gradient visualization on galaxy map | RECOMMENDED Phase 2 |

---

## 16. Known Gaps & Roadmap

> **Verified 2026-03-21.** Each gap below confirmed against source code.
> Items marked VERIFIED WORKING were previously reported as gaps but are
> actually implemented. See audit notes inline.

### Already Working (Previously Misreported as Gaps)

| System | Status | Evidence |
|--------|--------|----------|
| Module sustain enforcement | WORKING | `SustainSystem.ProcessModuleSustain()` deducts cargo via `InventoryLedger.TryRemoveCargo()` |
| Fleet fuel consumption | WORKING | Player burns every tick; NPCs every 2nd tick via `NpcFuelRateMultiplier`. Auto-refuel at dock |
| PressureSystem wiring | WORKING | 8 active `InjectDelta()` call sites (SustainSystem, BuyCommand, SellCommand, InstabilitySystem, WarfrontDemandSystem, SecurityLaneSystem, FleetUpkeepSystem, ContainmentSystem) |
| Price breakdown UI | WORKING | `SimBridge.Market.GetPriceBreakdownV0()` exposes base, scarcity, rep, tariff, instability, fee, total |
| Credit transaction ledger | WORKING | `state.AppendTransaction()` in Buy/SellCommand. Tariffs, fees, refuel costs all tracked |
| Recipe instantiation (8/9) | MOSTLY DONE | 8 recipes placed as industry sites. Only `AssembleElectronics` missing |

### Phase 2 (High Priority — Economy Depth)

| Gap | Impact | Effort | Description |
|-----|--------|--------|-------------|
| Per-good base prices | All 13 goods price identically at ~100 cr | 1 gate | Populate GoodDefV0.BasePrice per design band (Low 50-100, Mid 150-300, High 400-800, VHigh 1000-2000) |
| AssembleElectronics site | Electronics chain broken — no in-world production | 1 gate | Add placement rule for Electronics factory (Exotic Crystals + Fuel → Electronics) |
| NPC warfront avoidance | NPCs ignore instability/risk in trade eval | 1 gate | Risk-adjusted scoring multiplier in NpcTradeSystem |
| NPC trade memory | Traders ping-pong same good/node | 1 gate | Per-fleet cooldown dict (goodId, nodeId) → lastTradeTick |
| Dynamic fleet population | War losses permanent, topology dead-ends unfixable | 2 gates | Soft-cap fleet replacement consuming station goods |
| Economy digest reports | Galaxy-wide economy invisible to player | 1 gate | Aggregate supply/demand/price-change reporting via SimBridge |
| Macro credit monitoring | No inflation/deflation detection | 1 gate | Aggregate Created/Destroyed ratio tracking per tick window |
| Ambient life bridge signals | No traffic/prosperity/industry-type methods in SimBridge | 1 gate | Expose derived signals for Godot presentation layer |

### Phase 3 (Medium Priority — World Feel)

| Gap | Impact | Effort | Description |
|-----|--------|--------|-------------|
| Ambient station traffic | Stations feel empty | 2 gates | Cosmetic shuttle/drone spawning driven by traffic signals |
| Mining operation visuals | Industry invisible | 1 gate | Extraction beams, mining drones at industry nodes |
| Warfront VFX | Wars invisible except on map | 2 gates | Distant explosions, debris, refugees |
| Station prosperity visuals | All stations look the same | 1 gate | Visual tier system (lighting, scaffolding) |
| Lane traffic cosmetics | Lanes feel empty | 1 gate | Distant ship sprites on active lanes |
| NPC personality seeds | All traders identical | 1 gate | Per-fleet risk/preference modulation |
| Fleet personality in UI | NPCs are anonymous | 1 gate | Named captains, visible trade intentions |
| Convoy behavior | NPCs always solo | 2 gates | Nearby haulers merge for protection |
| Station personality | All stations feel generic | 1 gate | Architecture variants, ambient dialogue |

### Phase 4 (Lower Priority — Polish)

| Gap | Impact | Effort | Description |
|-----|--------|--------|-------------|
| NPC faction loyalty | NPCs trade across enemy lines | 1 gate | Respect faction trade policies |
| Embargo smuggling infrastructure | No black market | 2 gates | Covert trade mechanics |
| Production chain visualization | Players can't see full chain | 1 gate | Interactive flowchart in galaxy map |
| Hauler search radius scaling | Fixed 2-hop in larger galaxies | 1 gate | Scale with galaxy diameter |
| NPC convoy formation | Visual only — cosmetic grouping | 1 gate | Ships travel in formation |
| Dynamic station construction | Stations never change | 3 gates | Factions build new stations based on demand |

---

## Appendix A: Economy Constants Quick Reference

```
// Market
BasePrice            = 100 cr
IdealStock           = 50 units
MinSpread            = 2 cr
SpreadBps            = 1000 (10%)
TransactionFeeBps    = 100 (1%)

// NPC Trade
EvalIntervalTicks    = 15 (traders/patrols)
HaulerEvalInterval   = 30
ProfitThreshold      = 3 cr/unit
MaxTradeUnits        = 10 (traders)
HaulerMaxCargo       = 30
HaulerSearchHops     = 2

// Production
StationFoodPerTick   = 1 (consumed every 10 ticks)
StationFuelPerTick   = 1 (consumed every 10 ticks)
NpcDemandUnits       = 2 (consumed every 10 ticks per input)
ReactionBoost        = 5 units (if stock < 10, every 20 ticks)

// Fleet Speeds (AU/tick)
TraderSpeed          = 0.8
HaulerSpeed          = 0.7
PatrolSpeed          = 1.0

// Reputation
RepDecayInterval     = 1440 ticks (~1 game day)
RepDecayAmount       = 1 per interval
AlliedThreshold      = +75
FriendlyThreshold    = +25
HostileThreshold     = -75
EnemyThreshold       = -75

// Price Publication
PriceCadenceTicks    = 720 (12 game hours)
```

## Appendix B: Faction Economic Profiles

```
Pentagon Dependency Ring (clockwise):
  Concord (Food) → Communion (Exotic Crystals) → Valorin (Rare Metals)
    → Chitin (Electronics) → Weavers (Composites) → Concord

Each faction exports one good cheaply (-15%) and imports one expensively (+8%).
The asymmetry (-15% vs +8%) means the ring has a natural flow direction:
always buy at the exporter, sell at the importer.

Tariff Rates (base):
  Communion:  3%   (traders first — lowest barrier)
  Concord:    5%   (institutional — fair access)
  Weavers:    8%   (builders — moderate toll)
  Chitin:     15%  (information traders — high toll, efficient routes)
  Valorin:    20%  (military — expensive access, worth it at warfronts)
```

## Appendix C: Industry Best Practice Sources

- [X4: Foundations Economy Design](https://steamcommunity.com/app/392160/discussions/0/733658398226259846/) — full production chain simulation
- [Elite Dangerous BGS](https://elite-dangerous.fandom.com/wiki/Background_Simulation) — background simulation methodology
- [Eve Online Monthly Economic Report](https://www.eveonline.com/news/view/monthly-economic-report-october-2025) — sink/faucet tracking at scale
- [LostGarden Value Chains](https://lostgarden.com/2021/12/12/value-chains/) — source→transform→sink→anchor methodology
- [Sinks & Faucets: Virtual Game Economies](https://medium.com/1kxnetwork/sinks-faucets-lessons-on-designing-effective-virtual-game-economies-c8daf6b88d05) — economy balancing patterns
- [GDC: Economic Balancing Through Sink Design](https://www.gdcvault.com/play/1020085/Economic-Balancing-and-Improved-Monetization) — practical sink design
- [Game Economy Design 101](https://gamedevessentials.com/designing-a-game-economy-101-the-ultimate-guide-for-game-devs/) — comprehensive overview
