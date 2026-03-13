# Economy Deep Dive: Leading Games vs STE

**Comparative analysis** of how X4 Foundations, Starsector, Stellaris, Patrician, and Star Traders Frontiers handle NPC production, demand disruption, and economic emergent behavior.

---

## X4 Foundations: The Simulation Standard

### The Model
- **35 wares**, 4-tier production depth (ore → refined → component → advanced)
- **50+ station archetypes**, each produces specific goods via module setup
- **NPC production is player-disruption-driven**: destroy a station, supply collapses
- **NPC trade evaluated hourly** with full-graph pathfinding; profitable routes are executed deterministically
- **Wars are destructive**: station capture stops production; blockades prevent supply

### Key Mechanics

#### 1. Production-Driven Demand
```
Mining Station (Ore extraction) produces ore
↓ (ore is heavy, needs refining)
↓ (ore must be transported to nearest Refinery)
↓ (Refinery needs energy/Neon to run)
↓ (creates demand for Fuel/Neon trade routes)
↓ (fuel traders emerge; establish routes)
↓ (if fuel supply breaks, miners slow down)
↓ (ore accumulates at mines; price drops)
↓ (ore becomes profitable to transport elsewhere)
↓ (new trade routes emerge)
```

**The feedback loop**: production creates demand, which creates trading behavior, which responds to scarcity.

#### 2. Supply Disruption = Player Agency
- When the player destroys a station or faction loses territory, production halts
- Dependent stations starve (factories waiting for inputs)
- Prices spike only after supply breaks
- This is **destructive emergentism**: the player creates the pressure

#### 3. NPC Trader Behavior
- Traders move goods until profit margin = 0
- Search radius: can travel 5-10 hops if profit justifies it
- Cargo capacity: varies by ship class (1000-5000 units per ship, not 10 units)
- Frequency: evaluated hourly (real-time game, no tick system)

**Outcome**: Prices converge aggressively toward equilibrium unless supply is disrupted

#### 4. The Scalability Problem
X4's economy is **too detailed for most players**. You can spend 100 hours optimizing a single station's module configuration. Most players experience price equilibrium (everything is always available at baseline price) by mid-game.

---

### STE vs X4: Strengths & Weaknesses

| Aspect | X4 | STE |
|---|---|---|
| **Production complexity** | 50+ archetypes, infinite module combos | 9 recipes, fixed production per site |
| **Supply disruption** | Destructive (must attack stations) | Automatic (warfront demand, embargo) |
| **NPC visibility** | Can see traders moving goods, name them | Traders are invisible; only see price movement |
| **Scalability** | Requires meta-learning (reddit wikis exist) | Intentionally simple (player can hold in head) |
| **Margin longevity** | Prices converge fast (nerfing) | Warfront/embargo prevents equilibrium |
| **Geographic meaning** | Depends on player placement of stations | Baked into world generation (fixed forever) |

**Verdict**: STE intentionally trades X4's complexity for comprehension. Your 9 recipes vs 35 wares is a feature, not a limitation. The question is whether emergent behavior emerges WITHOUT player destruction (answer: yes, via warfront demand).

---

## Starsector: The Supply Chain + Sustain Model

### The Model
- **10 wares** (fuel, supplies, metals, rare alloys, volatiles, organs, ships, weapons, crew, commodities)
- **Fleet sustain**: Ships consume fuel/supplies passively. Standing army costs money.
- **Colony supply**: Colonies need food (organics) + supplies (manufactured) to grow
- **Three demand systems**: Military (fuel/ammo), Civilian (food/supplies), Industrial (metals/organics)
- **Wars are economically cascading**: Territory change → supply route disruption → price spike → margin opportunity

### Key Mechanics

#### 1. Sustain Enforcement (Early Game Pressure)
```
Turn 0: Player has 1 ship, 10000 credits
Turn 1-50: Passive fuel consumption eats 100 cr/turn
Turn 51: Player is broke if they don't trade
Turn 1-100: Necessity drives first trade (haul ore, sell for profit)
```

**The effect**: No safe idle state. Player is forced to move.

#### 2. Fleet Scaling Creates Economic Pressure
```
1 ship: 100 cr/turn sustain
3 ships: 300 cr/turn sustain
10 ships: 1000 cr/turn sustain (now you need 100+ cr/turn income = real trade routes)
```

Bigger fleet = proportionally more sustain burden. Economy scales with player growth.

#### 3. Colony Growth via Supply Chain
- Colonies need food + supplies to grow
- Supply sources are scattered (not every system produces supplies)
- Building colony industry requires infrastructure investment
- Wars disrupt supply chains → colonies shrink
- Shrinking colonies lose production capacity

**Outcome**: Wars have visible feedback (colony bars decline, income drops)

#### 4. Visible Economic Pressure
Players see:
- **Cargo hold**: "I have 100 fuel left, how many jumps until I'm empty?"
- **Profit calculation**: "Ore: buy at 200, sell at 350, profit 150 per unit. 10 units = 1500 credits. Minus 50 fuel cost = net 1450."
- **Fleet status**: "Fleet sustain: 300 cr/day. Income: 400 cr/day (2 trade ships). Profit: 100/day."

All decisions are visible and quantifiable.

---

### STE vs Starsector: Alignment

| Aspect | Starsector | STE Current | STE Designed |
|---|---|---|---|
| **Module sustain** | Active (fleet must consume supplies) | Inactive (designed but not enforced) | Will be enforced Phase 2 |
| **Sustain scaling** | Proportional to fleet size | Proportional to fleet size (theory) | Same design, not yet enforced |
| **Early-game urgency** | High (fuel consumption forces trade) | Low (no sustain enforcement) | Will be high once enforced |
| **War consequence** | Supply disruption cascades to economy | Warfront demand directly disrupts prices | Both mechanisms + embargo |
| **Visibility** | Player sees sustain needs explicitly | Sustain exists in backend, not surfaced | Will add UI snapshot for sustain needs |
| **Geographic meaning** | Supply sources are scattered | Extraction goods are scattered (Organics 40%) | Same design, more deterministic |

**Verdict**: STE's design is nearly identical to Starsector's, but enforcement is incomplete. Once sustain is active, the economic pressure dynamics will be the same (and arguably stronger because sustain is tied to production chains that can break via embargo).

---

## Patrician / Port Royale: NPC Competition & Visibility

### The Model
- **15 wares** (raw + processed goods)
- **Cities are suppliers AND consumers** (demand food, cloth, spices; produce raw goods)
- **NPC merchants are visible competitors** (you can see them buying/selling)
- **Seasonal price variation** (harvest = low food prices, winter = high prices)
- **Production is time-locked** (craftsmen make goods slowly; can't instant-produce)
- **NPC monopolization** (if you ignore a route, NPCs take it over)

### Key Mechanics

#### 1. Visible NPC Competition
```
Turn 1: You buy 50 cloth at Port A (price 100)
Turn 2: NPC merchant also buys from Port A (price 102, slightly higher)
Turn 3: Port A cloth runs out (you took 50, NPC took 20)
Turn 4: You sail to Port B, but NPC merchant is already there selling cloth at 105
Turn 5: You must find another market or wait for Port A to resupply
```

**The effect**: NPC behavior is transparent and reactive. You can predict it, counter it, or cooperate with it.

#### 2. Seasonal Cycles
- Harvest (Summer): Food abundant, price low
- Growing season (Spring): Seeds needed, price high
- Winter: Food scarce, price spikes

**The effect**: Same route at different times = different margin. Timing is a strategic decision.

#### 3. Supply Chain Fragility
```
Cloth requires: sheep (raw) → wool (processed) → cloth (manufactured)
↓ (each step takes time; each step is a city)
↓ (if any city is blocked by NPC monopoly, chain breaks)
↓ (player must decide: fight for access or find alternate chain)
```

#### 4. Market Saturation
If you monopolize a route:
- NPCs stop trading it (margins become yours alone)
- You must supply the entire demand by yourself (capital intensive)
- If you stop, the market goes empty (expensive re-entry)

---

### STE vs Patrician: Divergence

| Aspect | Patrician | STE |
|---|---|---|
| **NPC visibility** | Highly visible (can name merchants, track) | Invisible (only see price effects) |
| **Seasonality** | Explicit (harvest/winter) | Implicit (warfront intensity cycles) |
| **Production time** | Explicit (craftsmen take 5 days) | Implicit (industry sites produce each tick) |
| **Player monopoly** | Possible; NPCs react by stopping | Less relevant (NPCs can't monopolize because they're invisible) |
| **Geographic permanence** | Static (same cities, same routes forever) | Procedural (routes change per seed) |

**Verdict**: Patrician focuses on visible NPC narrative + temporal cycles. STE focuses on geographic scarcity + systemic pressure. Different goals, both valid. STE should eventually add NPC visibility (show which faction owns a trade route, which NPC merchants control a route), but it's not Phase 2 critical.

---

## Stellaris: Economic Scaling

### The Model
- **40+ resources** (minerals, energy, alloys, food, research, etc.)
- **Pop jobs** produce goods; pops are assigned to districts
- **Trade routes** are permanent, player-constructed
- **Scaling pressure**: bigger empire = more pops = more consumption = harder to balance

### Key Mechanics

#### 1. Automatic Scaling Pressure
```
Empire size: 5 planets, 50 pops
Food production: 5 pops assigned to farms → 5 food/turn
Food consumption: 50 pops eat 5 food/turn
Result: balanced

Empire size: 50 planets, 500 pops
Food production: 50 pops assigned to farms → 50 food/turn (only if you built farms!)
Food consumption: 500 pops eat 50 food/turn
Result: balanced IF you built farms; starvation IF you prioritized industry

The key: you can't build EVERYTHING. Trade-offs are forced.
```

#### 2. Trade Routes as Strategic Assets
- You build trade routes to import deficits
- Enemy can blockade trade routes (military action = economic consequence)
- Routes take 6 months to establish (some delay before relief arrives)

**The effect**: Wars are about denying enemy resources, not just killing ships.

#### 3. Job Reassignment Flexibility
During wartime, you can reassign pops:
- From food farms → weapon factories (accept hunger to arm military)
- From research → alloys (sacrifice tech to fight)

**The effect**: Playable response to supply crisis; not just "wait for trade route."

---

### STE vs Stellaris: Scope Difference

| Aspect | Stellaris | STE |
|---|---|---|
| **Scale** | Entire empire (50+ planets) | Single player (1-20 ships) |
| **Production control** | Player assigns pops to jobs | NPC industry sites produce autonomously |
| **Trade routes** | Player-constructed permanent links | Autonomous NPC trade (player observes prices) |
| **Strategic pressure** | Resource trade-offs (alloys vs food) | Sustain cascades (T2 armor vs T1 weapons) |
| **Visibility** | Explicit (you see each planet's production) | Implicit (you infer from prices) |

**Verdict**: Stellaris is a 4X empire simulator; STE is a space trader sandbox. Different scope. STE's scaling model is tighter (player ship sustain escalates with fleet size) but less visible.

---

## Star Traders Frontiers: Faction Economy & Disruption

### The Model
- **15 wares** (luxuries, armor, ammunition, etc.)
- **Faction reputation affects trade** (hostile factions ban you from markets)
- **Supply chains can break** (faction loses territory → goods disappear from markets)
- **Black market and contraband** (some goods illegal in some regions)
- **Economic warfare** (raiding supply convoys damages faction economy)

### Key Mechanics

#### 1. Faction-Based Supply Gating
```
Faction A controls ore supplies (4 systems)
You are allied with Faction A
Result: You can buy ore at friendly price from 4 systems

You become hostile to Faction A
Result: You can't buy ore anywhere (embargo enforced)
Alternative: Black market ore exists but at 5x price

You switch allegiance to Faction B
Result: Faction A now blocks trade with Faction B
Economic consequence: Faction B's war goods (munitions) are restricted
```

#### 2. Economic Consequence of Territory Loss
When a faction loses territory:
- Trade goods tied to that territory disappear
- Dependent supply chains break
- Prices spike in dependent goods
- Player can profit by supplies goods to warfront

**Example**: If Faction A loses its ore territory, ore prices spike 2x everywhere. Metal-dependent goods become expensive. The player can profit by transporting ore from distant sources to the metal shortage.

#### 3. Player-Driven Economic Warfare
- Raid faction cargo convoys (stealing reduces goods flowing through economy)
- Smuggle contraband goods (creates black market supply)
- Sabotage supply routes (NPC traders can be ambushed)

---

### STE vs Star Traders: Resonance

| Aspect | Star Traders | STE |
|---|---|---|
| **Faction reputation gating** | Explicit (hostile = no trade) | Implemented (tariff + access tiers) |
| **Supply chains break** | Via territory loss | Via embargo + warfront damage |
| **Economic cascades** | Consequential (chain breaks at key nodes) | Designed (3-tier depth, intended bottlenecks) |
| **Player economic warfare** | Raiding + sabotage | Warfront supply delivery (can broker ceasefire) |
| **Black market alternative** | Exists as contraband option | Planned (future: fracture-based black market) |

**Verdict**: STE's embargo system mirrors Star Traders' faction gating. The Phase 2 addition of sustain enforcement will create the same cascading pressure that disrupting faction supply lines creates in Star Traders.

---

## Elite Dangerous: Background Simulation (Light Model)

### The Model
- **Limited wares** (mainly ships/modules, cargo is abstract)
- **Factional background simulation** (factions fight wars off-screen)
- **Supply/demand affects prices** (wartime goods are expensive)
- **Economy is secondary to combat/exploration**

### Why ED is Limited (and How STE Avoids It)
ED's economy doesn't feel "alive" because:
1. **No production chains** (cargo is fungible; no conversion)
2. **Factions are abstract** (you can't see faction trade routes)
3. **Supply is invisible** (you don't know why prices changed)
4. **Wars don't disrupt supply** (background sim doesn't gate access)

**STE improvement**: STE has explicit production chains, visible economic pressure (warfront demand, embargo), and gated access (faction reputation).

---

## Synthesis: What Makes STE's Economy Unique

### Combining Best Practices

| System | Source Game | Why STE Implementation is Better |
|---|---|---|
| **Production chains** | X4 (35 wares) | STE: 9 wares (comprehensible), 3-tier depth, forced bottlenecks |
| **Sustain enforcement** | Starsector (fuel consumption) | STE: Sustain tied to production chains (breaks together), warfront-linked |
| **Geographic scarcity** | Patrician (city locations) | STE: Procedural distribution (Organics 40%, Rare Metals 15%, Exotic Crystals fracture-only) |
| **Faction gating** | Star Traders (rep-based access) | STE: Embargo + reputation + tariff compound (3-layer pressure) |
| **Economic cycles** | Patrician (seasonal) | STE: Warfront intensity cycles (automatic, no calendar) |
| **NPC trade** | X4 (autonomous traders) | STE: Simpler range (1-2 hops), smaller cargo (prevents over-convergence) |

### What STE Is NOT Trying to Be
- **Not X4**: Too complex. STE is intentionally simple (13 goods, 9 recipes).
- **Not Stellaris**: Not a 4X. Player is a trader, not an empire builder.
- **Not Patrician**: No visible NPC merchants (design choice; could be added later).
- **Not Elite Dangerous**: Economy is primary, not secondary.

STE is a **space trader sandbox** where:
1. The economy is visible (prices, market alerts, supply chain snapshots)
2. The economy is consequential (sustain mechanics create pressure)
3. The economy is procedural (different per seed, not hand-crafted)
4. The economy is comprehensible (13 goods vs 40, player can plan trades)

---

## Recommendations for Phase 2+ Based on Comparative Analysis

### Immediate (Phase 2)
- ✅ Deploy all 9 production recipes
- ✅ Enforce module sustain
- ✅ Add scarcity visibility (market alerts)

### Near-term (Phase 3)
- ➕ Add NPC merchant visibility (show faction/type of trader owning a route)
- ➕ Add "supply chain status" UI snapshot (this recipe is stalled because Organics are embargoed)
- ➕ Implement sustain failure messaging (toasts: "T2 armor sustain low, degrading in 60 ticks")

### Medium-term (Phase 4+)
- ➕ Add warfront intensity oscillation (wars aren't static, they ebb/flow)
- ➕ Add spoilage to Food (creates turnover pressure)
- ➕ Add black market mechanics (fracture-based contraband supply)
- ➕ Add faction-specific supply preferences (militarist factions value Munitions, traders value Composites)

### Aspirational (Post-launch)
- ➕ Add visible NPC fleets named after merchants
- ➕ Add seasonal modifiers (in-fiction: "Harvest season approaching, Organics price will drop")
- ➕ Add player-driven economic warfare (sabotage supply routes, raid convoys)
- ➕ Add contraband reputation consequences (selling illegal goods in faction territory)

---

## Conclusion

Your economy **learns from all five games** while maintaining **unique scope and mechanics**:
- **X4's depth** (production chains) with **STE's simplicity** (9 recipes, not 35)
- **Starsector's urgency** (sustain enforcement) applied to **production chains** (breaks via embargo)
- **Patrician's visibility** (market alerts, price history) without **NPC bloat** (simple AI)
- **Star Traders' consequences** (faction gating) via **multiple layers** (embargo + tariff + access tiers)
- **Stellaris' scaling pressure** (bigger fleet = more sustain burden)

Phase 2 deployment will unlock this potential. Once production chains are fully instantiated and sustain is enforced, the economy will feel **alive** — emergent pressure without player destruction, supply consequences without hand-crafted events.

---

**End of Document**
