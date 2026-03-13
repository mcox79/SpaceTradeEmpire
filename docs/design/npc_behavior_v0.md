# NPC Behavior & Fleet AI — Design Spec

> Mechanical specification for NPC fleet decision-making, route planning,
> trade evaluation, patrol circuits, and the intent resolution system.
> Companion to `npc_industry_v0.md` (economic actors), `fleet_logistics_v0.md`
> (supply chains), and `factions_and_lore_v0.md` (faction identities).

---

## AAA Reference Comparison

| Game | NPC AI Model | Decision Quality | Economic Impact | Player Interaction |
|------|-------------|------------------|----------------|-------------------|
| **X4 Foundations** | Per-ship autonomous AI. NPCs evaluate trade opportunities, fly to stations, buy/sell. Complex behavior trees with state machines. | Excellent — NPCs create believable economy. Sometimes suboptimal (creates player advantage). | NPCs ARE the economy. Remove them and markets die. | Player competes with NPC traders for margins. Can hire/command NPC pilots. |
| **Starsector** | Fleet-level AI. Fleets patrol, trade, raid. Route selection based on faction territory + threat assessment. | Good — fleets feel purposeful. Patrols guard lanes. Traders use safe routes. | Fleet disruption creates supply shortages. Player raids → price spikes. | Player encounters NPC fleets in transit. Can ally, fight, or ignore. |
| **Stellaris** | Empire-level AI. No individual ship decision-making. Fleet movement is strategic (defend borders, attack targets). | Strategic — AI manages economy and military as unified policy. | AI builds stations and manages production chains. | Player interacts at diplomatic level. |
| **Mount & Blade** | Lord-level AI. Each lord has personality (aggressive/cautious), evaluates targets, seeks allies. Caravans follow safe routes. | Personality-driven — creates memorable NPCs. Caravans are prey. | Caravans create trade income. Disrupted caravans hurt factions. | Player can intercept caravans, recruit lords, destabilize factions. |
| **STE (Ours)** | Role-based fleet AI. Three roles (Trader/Hauler/Patrol) with different evaluation priorities. Intent system resolves competing route choices deterministically. | Functional — NPCs move goods and create price convergence. No personality or memory. | NPCs drive ~70% of goods movement. Without them, markets stagnate. | Player observes NPC trade patterns. NPC patrols may be hostile. |

### Best Practice Synthesis

1. **Role diversity creates emergent behavior** (X4) — traders, haulers, and patrols have different objectives, creating visible variety in the game world. Our 3-role system (60/25/15 split) achieves this.
2. **NPC traders should be slightly suboptimal** (X4, Starsector) — if NPCs trade perfectly, the player has no advantage. Our NPCs search only 1-2 hops, carrying 10-30 units. The player can see further and carry more.
3. **Patrol circuits create presence** (Starsector) — patrol fleets following fixed circuits make the galaxy feel policed. Our FNV1a-hash-based circuit generation creates deterministic but varied patrol patterns.
4. **Trade opportunity evaluation should be transparent** — the player should be able to understand why NPCs are moving goods where they're going. Our profit-threshold + weight-adjusted scoring is simple enough to predict.
5. **NPC behavior should respond to galaxy state** — war should change NPC patterns (Starsector fleet deployment). Our NPCs don't yet respond to warfront state.

---

## Current Implementation

### Systems

| System | File | Purpose | Status |
|--------|------|---------|--------|
| `IntentSystem` | `SimCore/Systems/IntentSystem.cs` | Role-based route choice resolution | Implemented |
| `MovementSystem` | `SimCore/Systems/MovementSystem.cs` | Lane traversal, fracture travel, route execution | Implemented |
| `NpcTradeSystem` | `SimCore/Systems/NpcTradeSystem.cs` | Trade opportunity evaluation, patrol circuits | Implemented |
| `RoutePlanner` | `SimCore/Systems/RoutePlanner.cs` | Deterministic DFS pathfinding | Implemented |
| `LogisticsSystem` | `SimCore/Systems/LogisticsSystem.cs` | Autonomous supply chain jobs | Implemented |

### Fleet Roles

| Role | Population | Eval Interval | Search Radius | Max Cargo | Behavior |
|------|-----------|---------------|---------------|-----------|----------|
| **Trader** | 60% | 15 ticks | 1 hop (adjacent) | 10 units | Profit-maximizing arbitrage |
| **Hauler** | 25% | 30 ticks | 2 hops (BFS) | 30 units | Volume-maximizing transport |
| **Patrol** | 15% | 15 ticks | N/A | N/A | Circuit-based area patrol |

---

## Mechanical Specification

### 1. Intent Resolution System

The IntentSystem resolves competing route choices deterministically via
role-based priority ordering.

**IFleetRouteChoiceIntent interface**:
```
FleetId: string
RouteId: string (stable identity for tie-breaks)
ProfitScore: int (higher = more profitable)
CapacityScore: int (higher = more cargo volume)
RiskScore: int (lower = safer)
```

**Role-Based Selection**:

| Role | Primary Sort | Secondary | Tertiary | Tie-Break |
|------|-------------|-----------|----------|-----------|
| **Trader** | ProfitScore ↓ | CapacityScore ↓ | RiskScore ↑ | RouteId → Seq |
| **Hauler** | CapacityScore ↓ | ProfitScore ↓ | RiskScore ↑ | RouteId → Seq |
| **Patrol** | RiskScore ↑ | ProfitScore ↓ | CapacityScore ↓ | RouteId → Seq |

**Resolution Process**:
1. Group due intents by (CreatedTick, FleetId)
2. For each group, select winner per fleet role ordering
3. Apply winner intent, drop competitors
4. Non-choice intents applied as-is
5. Emit RouteChoice fleet event for debugging

### 2. Trade Evaluation (Trader Role)

**ProcessFleetTrade** (every 15 ticks for idle Trader fleets):

```
1. Deliver cargo: dump all cargo goods into local market
2. Clean up zero-qty cargo entries
3. Find best opportunity among adjacent nodes:

   For each adjacent market:
     For each good with local stock > 0:
       buyPrice = localMarket.GetBuyPrice(goodId)
       sellPrice = adjMarket.GetSellPrice(goodId)
       profitPerUnit = sellPrice - buyPrice

       if profitPerUnit < ProfitThresholdCredits: skip

       units = min(localStock, MaxTradeUnitsPerTrip=10)
       weight = GoodTradeWeights[goodId] or DefaultGoodWeight
       score = profitPerUnit × units × weight

   Best = highest score across all adjacent markets

4. Pick up min(best.Units, MaxTradeUnitsPerTrip) from local market
5. Set destination to best market
```

**Weight-Adjusted Scoring**: Different goods have different trade weights. This
prevents NPCs from exclusively trading the highest-margin good and ignoring
volume goods. Weights are defined in `NpcTradeTweaksV0.GoodTradeWeights`.

### 3. Trade Evaluation (Hauler Role)

**ProcessFleetHaulerTrade** (every 30 ticks for idle Hauler fleets):

Same logic as Trader but with:
- 2-hop BFS search radius (vs 1-hop for traders)
- 30-unit max cargo (vs 10 for traders)
- Uses `FindBestOpportunityMultiHop()` for wider market search

**BFS Adjacency Expansion**:
```
Frontier = {currentNodeId}
For hop in 0..maxHops:
  NextFrontier = {}
  For each node in Frontier:
    For each edge touching node:
      adj = other endpoint
      if not yet visited and not currentNode:
        reachable.add(adj)
        NextFrontier.add(adj)
  Frontier = NextFrontier
```

### 4. Patrol Circuit System

**ProcessPatrolCircuit** (every 15 ticks for idle Patrol fleets):

```
if no circuit or circuit < 2 nodes:
  circuit = GenerateCircuit(state, fleetId, currentNodeId)
  circuitIndex = 0

circuitIndex = (circuitIndex + 1) % circuit.Count
Set destination to circuit[circuitIndex]
```

**Circuit Generation** (deterministic, FNV1a-hash-based):
```
circuit = [startNode]
targetHops = 3 + (hash(fleetId) % 3)  → 3-5 hops

For each hop:
  candidates = adjacent unvisited nodes (sorted)
  pick = hash(fleetId + "_" + hop) % candidates.Count
  circuit.add(candidates[pick])

Result: deterministic 3-5 node patrol circuit per fleet
```

**Properties**:
- Same fleet always generates same circuit (deterministic from fleet ID)
- Circuits cover 3-5 nodes, creating visible area patrols
- No backtracking (visited nodes excluded from candidates)
- If dead-end reached, circuit terminates shorter

### 5. Route Planning (RoutePlanner)

**DFS-based pathfinding** with deterministic ordering:

```
TryPlan(state, from, to, speed) → RoutePlan:
  1. Build outgoing edge adjacency (sorted by edge ID)
  2. DFS from source, collecting up to 8 candidate paths
  3. No cycles (simple paths only)
  4. MaxHops = min(8, node count)

Candidate Scoring (default, no risk override):
  Primary: fewer hops
  Secondary: lower risk score (milli-AU distance)
  Tertiary: lexicographic route ID

Candidate Scoring (with risk knobs):
  Primary: lower (travel_ticks + scaled_risk_cost)
  Secondary: fewer hops
  Tertiary: lexicographic route ID
```

**Risk Scoring**:
```
EdgeRiskScoreV0(edge) = round(edge.Distance × 1000) milli-AU
Risk bands: LOW (<1000), MED (<3000), HIGH (<5000), EXTREME (≥5000)
```

**Fracture Route Planning**:
```
TryPlanFractureRoute(from, to, speed):
  Requires at least one fracture node
  distance = Euclidean(from.Position, to.Position)
  travelTicks = ceil(distance / speed) × FractureFuelCostMultiplierPct / 100
  riskRating = distance-based band (LOW/MED/HIGH/EXTREME)
  Single hop — no intermediate stops
```

### 6. Movement Execution (MovementSystem)

**Per-tick fleet movement**:
```
For each fleet (sorted by ID):
  1. Ensure route planned (TryEnsureRoutePlanned)
     - ManualOverrideNodeId takes precedence
     - Solo DestinationNodeId treated as final destination
     - RoutePlanner.TryPlan() generates edge sequence

  2. If delayed: decrement DelayTicksRemaining, skip

  3. If FractureTraveling: advance fracture travel (10x slower)

  4. If not Traveling: try start next edge
     - Fuel check: FuelCurrent <= 0 → Immobilized:NoFuel
     - Capacity check: edge.UsedCapacity >= edge.TotalCapacity → wait
     - Reserve capacity slot, set Traveling state

  5. If Traveling: advance along current edge
     - effectiveSpeed = fleet.Speed × tech bonus × mass penalty
     - step = effectiveSpeed / edge.Distance
     - TravelProgress += step

  6. On arrival (TravelProgress >= 1.0):
     - Free edge capacity slot
     - Update CurrentNodeId
     - Record arrival for JumpEventSystem
     - Apply SeenFromNodeEntry (discovery marks)
     - Advance RouteEdgeIndex
     - If route complete: clear route state
     - Set Idle (one edge per tick per fleet)
```

**Mass Penalty**:
```
massPenalty = shipClass.Mass × MassSpeedPenaltyPerUnit
multiplier = max(MinMassSpeedMultiplier, 1 - massPenalty)
effectiveSpeed = speed × multiplier
```

Heavier ship classes (Cruiser, Dreadnought) move meaningfully slower than
light ships (Shuttle, Corvette), creating a speed-vs-cargo trade-off.

---

## Player Experience

### NPC Fleet Ecosystem

```
Galaxy with 30 NPC fleets:
  18 Traders (60%): Short-range profit-seeking arbitrage
     → Creates price convergence between adjacent nodes
     → Player sees: small ships moving goods on popular routes

  7 Haulers (25%): Medium-range volume transport
     → Moves large quantities over 2+ hops
     → Player sees: cargo ships on longer routes, carrying bulk

  5 Patrols (15%): Area security circuits
     → 3-5 node circuits, visible presence
     → Player sees: patrol ships orbiting near stations
     → May be hostile (faction-dependent)
```

### Emergent Trade Dynamics

NPC traders create the "living market" baseline:
1. **Price convergence**: NPCs buy low, sell high → prices equalize across adjacent nodes
2. **Incomplete arbitrage**: NPCs only search 1-2 hops → inter-region spreads persist
3. **Volume limitations**: NPCs carry 10-30 units → large price differentials survive
4. **Trade weight bias**: NPCs prefer certain goods → some goods undertrated (player opportunity)

The player's advantage: longer routes, bigger cargo, better intel (knowledge graph), and warfront awareness that NPCs lack.

---

## System Interactions

```
NpcTradeSystem
  ← reads Fleets (role, position, cargo, job status)
  ← reads Markets (inventory, prices)
  ← reads Edges (adjacency)
  → writes Fleet.Cargo (pickup)
  → writes Market.Inventory (delivery, pickup)
  → writes Fleet.FinalDestinationNodeId (route target)

IntentSystem
  ← reads PendingIntents (route choice intents)
  ← reads Fleets (role for priority ordering)
  → applies winning intent
  → emits FleetEvents.RouteChoice

MovementSystem
  ← reads Fleets (state, route, speed)
  ← reads Edges (distance, capacity)
  ← reads Tech (speed bonuses)
  → writes Fleet.TravelProgress
  → writes Fleet.CurrentNodeId (arrival)
  → writes Edge.UsedCapacity (lane reservation)
  → triggers IntelSystem (discovery marks)
  → records ArrivalsThisTick

RoutePlanner
  ← reads Nodes, Edges (graph structure)
  ← reads Tweaks (risk knobs)
  → returns RoutePlan (edge sequence, risk, travel ticks)

LogisticsSystem
  ← reads IndustrySites (shortage detection)
  ← reads Markets (supplier inventory)
  → assigns fleet jobs (pickup/delivery)
  → enqueues LoadCargo/UnloadCargo intents
```

---

## Design Gaps and Future Work

| Gap | Priority | Effort | Description |
|-----|----------|--------|-------------|
| **Warfront-aware routing** | HIGH | 1 gate | NPCs don't avoid contested nodes or reroute around warfronts. Add warfront intensity as risk multiplier in RoutePlanner scoring. |
| **NPC personality/memory** | HIGH | 2 gates | All NPCs of same role behave identically. Gate 1: personality enum + per-fleet seed. Gate 2: personality-weighted trade/risk evaluation. |
| **Trade opportunity caching** | MEDIUM | 1 gate | NPCs re-evaluate every 15/30 ticks from scratch. Cache recent evaluations, invalidate on market change. |
| **Convoy behavior** | MEDIUM | 2 gates | NPCs travel solo. Gate 1: convoy grouping logic (nearby haulers merge). Gate 2: shared route planning + visual grouping. |
| **Patrol response** | MEDIUM | 1 gate | Patrols follow fixed circuits regardless of threats. Add threat-aware circuit deviation when combat detected nearby. |
| **NPC faction loyalty** | LOW | 1 gate | NPCs trade freely across faction lines. Add faction trade policy check in NpcTradeSystem opportunity evaluation. |
| **Emergent specialization** | LOW | 1 gate | All Traders evaluate all goods equally (modulo weights). Track per-NPC trade history, boost weight for familiar goods. |
| **NPC fleet construction** | FUTURE | 3 gates | No NPC fleet growth/replacement beyond respawn. Gate 1: faction economic health metric. Gate 2: fleet build decision. Gate 3: shipyard construction. |
| **Trade caravan system** | FUTURE | 3 gates | Scheduled NPC trade caravans between faction capitals (Mount & Blade model). Gate 1: caravan entity + schedule. Gate 2: escort assignment. Gate 3: interception mechanics. |

---

## Constants Reference

All values in `SimCore/Tweaks/NpcTradeTweaksV0.cs`, `FleetPopulationTweaksV0.cs`,
`MovementTweaksV0.cs`:

```
# NPC Trade
EvalIntervalTicks            = 15    (Trader/Patrol evaluation)
ProfitThresholdCredits       = 2     (minimum profit/unit to trade)
MaxTradeUnitsPerTrip         = 10    (Trader max cargo)
DefaultGoodWeight            = 100   (base trade weight)

# Hauler
HaulerEvalIntervalTicks      = 30
HaulerEvalRadiusHops         = 2
HaulerMaxCargoUnits          = 30

# Fleet Population
TraderPct                    = 60%
HaulerPct                    = 25%
PatrolPct                    = 15%

# Movement
ImprovedThrustersMultiplier  = 1.2   (improved_thrusters tech)
MassSpeedPenaltyPerUnit      = per ShipClassContentV0
MinMassSpeedMultiplier       = floor for mass penalty

# Route Planning
DefaultMaxCandidates         = 8     (DFS path limit)
FractureFuelCostMultiplierPct = from FractureTweaksV0
FractureRiskMultiplierPct    = from FractureTweaksV0
FractureSpeedDivisor         = 10    (10x slower than lane travel)

# Patrol Circuits
CircuitHops                  = 3-5   (3 + hash % 3)
```
