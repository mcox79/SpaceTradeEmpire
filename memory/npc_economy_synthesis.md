# NPC Industry & Production Chain Design — Synthesis Report

**Date**: 2026-03-13
**Status**: Complete research synthesis, ready for Phase 2 implementation planning

This document consolidates best practices from leading space trading game economies and validates them against your current STE implementation (13 goods, 9 production chains). Also provides concrete mechanical recommendations for avoiding static equilibrium prices and creating emergent economic pressure.

---

## Executive Summary

Your economy is **structurally sound** but **mechanically incomplete**. You have:
- ✅ Goods with justified multi-demand sinks (Pillar 1 of trade_goods_v0.md)
- ✅ Geographic scarcity at extraction tier (Fuel universal, Organics 40%, Rare Metals 15%)
- ✅ Three-tier production depth (extraction → processed → manufactured)
- ✅ NPC price stabilization via autonomous traders (NpcTradeSystem)
- ✅ Warfront demand shocks creating cascading economic pressure (WarfrontDemandSystem)
- ❌ Only 3 of 9 recipes instantiated as industry sites → production chains cannot break
- ❌ No module sustain enforcement → goods have no teeth (exist in UI, not in mechanics)
- ❌ Fleet fuel consumption not active → no early-game urgency

The gap between design and code is the **production chain instantiation**. The recipes exist. The market mechanics exist. But industry sites produce only Ore, Metal, and Munitions. Food, Composites, Electronics, and Components factories don't exist yet. This is Phase 2 work, and it is critical.

---

## Part 1: What Makes Economies Feel "Alive" vs Static

### The Price Equilibrium Problem

Most trading games slip into static equilibrium: everything costs the same, supply = demand perfectly, margins vanish. This breaks replayability.

**Root causes:**
1. **No supply disruption** — production chains never break, so shortages never emerge
2. **Autonomous NPC arbitrage too efficient** — traders price-converge so aggressively that player margins disappear
3. **Symmetric factions** — all regions produce the same goods, making geography meaningless
4. **No consumption decay** — goods sit in warehouses forever; demand doesn't escalate over time

**Your defenses against this (implemented):**
- **Warfront demand shocks**: Munitions 4x, Composites 2.5x, Fuel 3x at contested nodes. This drains inventories and prevents equilibrium.
- **Embargo enforcement**: War-critical goods blocked when supplier = enemy. Creates scarcity spikes independent of market logic.
- **Geographic extraction scarcity**: Organics and Rare Metals not universal. Industrial systems must import, creating permanent trade routes.
- **Module sustain escalation**: T1 → T2 → T3 progression requires increasingly complex supply chains (Fuel → Composites+Rare Metals → Exotic Matter). This is NOT yet enforced but is designed.

### The Three Ingredients of Emergent Economy

#### 1. Supply Fragmentation
Production chains should naturally break at multiple points, not just one bottleneck.

**X4 Foundations model**: ~35 wares, 4-tier depth, multiple parallel production paths. A single lost ship factory doesn't kill the economy, but losing 3 factories in a region creates a cascade.

**Your implementation**: 9 recipes, 3-tier max depth. Fragmentation is baked in:
- Ore → Metal (single point of failure... BUT)
- Metal → {Munitions, Composites, Components} (three divergent branches)
- Organics → {Food, Composites} (two uses, creating "butter vs guns" pressure)
- Exotic Crystals → Electronics → Components (linear, vulnerable to fracture disruption)

**Phase 2 gap**: Only 3 recipes are instantiated. You need all 9 deployed to unlock the cascade.

#### 2. Demand Diversity (Not Simplicity)
Each good must have 2+ sources of demand, and those demands must escalate *independently*.

**Starsector model**: Fleet sustain (fuel/ammo), colony industry (supplies/organics), and construction (construction materials) are three separate demand systems. War spikes ammo but not fuel. Colony growth spikes supplies but not ammo. A single change cascades unpredictably.

**Your goods (all pass the "2+ demand" rule)**:
- **Fuel**: 5+ sinks (Metal refining, Food processing, Electronics assembly, Munitions fabrication, T1 module sustain)
- **Metal**: 5 sinks (Munitions, Composites, Components, T1 armor sustain, ship repair)
- **Organics**: 2 sinks (Food processing, Composites fabrication) — the "butter vs guns" fork
- **Munitions**: 2 sinks (weapon module sustain, warfront bulk supply) — separate pressure systems
- **Composites**: 2 sinks (T2 module sustain, warfront premium supply)
- **Food**: Universal consumption at every populated node (crew/station sustain)
- **Electronics**: 1 sink (Components manufacturing) — BUT supply-constrained by Exotic Crystals, making it strategic
- **Components**: 3 sinks (automation sustain, refit sustain, fleet upkeep)
- **Exotic Matter**: 2 sinks (T3 module sustain, research program sustain)

Zero goods have only one demand with no escalation mechanism. This is excellent design.

#### 3. Feedback Loops That Create Pressure

The economy should push back. When the player succeeds too much, prices drop and profitability evaporates. When they fail, prices spike and opportunities emerge.

**Patrician / Port Royale model**: NPC merchants compete with you. If you monopolize a route, they stop trading it (margins vanish). If you neglect a route, they take it over and prices stabilize.

**Your implementation**: NpcTradeSystem.ProcessFleetTrade() evaluates trades deterministically, avoiding loss-making routes. This creates two feedback loops:

1. **Price convergence loop**: Traders move goods from surplus (low price) to deficit (high price). Removes easy arbitrage within 1-2 hops.
2. **Cargo movement loop**: Fleets deliver cargo at each node, which restocks markets and reduces price spikes caused by temporary shortages.

**BUT**: The feedback loop is too gentle. Traders search only 1-2 hops out and carry small cargo (10 units). They stabilize prices locally but cannot prevent regional shocks. This is intentional — leaving room for the player to profit.

---

## Part 2: Your Economy vs Leading Games (Comparative Analysis)

### X4 Foundations (~35 wares, 4-tier depth, NPC economy simulated hourly)

**How it works:**
- Stations produce specific goods based on module setup. Weapon factories → ammunition, mining stations → ore, refineries → metals.
- NPC traders evaluate profit margins every ~60 minutes. If profitable (accounting for tariffs), they move goods.
- Piracy and player wars disrupt production. If a station is contested, production halts.
- Production chains create demand cascades: mining station needs energy (fuel), sends ore to refinery, refinery needs fuel, creates demand for fuel traders.

**What makes it feel alive:**
- Supply disruption is player-triggered (destroy a refinery, ore floods the market)
- Production modules are expensive; players compete for resources to build them
- NPC traders are visible; you can see them moving goods and react
- Trade routes change dynamically based on production site locations

**Your equivalent strength**: Your geographic distribution (Organics at 40%, Rare Metals at 15%) creates natural scarcity without player disruption. X4 requires player destruction to disrupt supply. You get disruption for free at warfronts.

**Your equivalent weakness**: Only 3 production sites instantiated. X4 has ~50 station types. The cascades don't exist yet.

---

### Starsector (~10 wares, 3-tier depth, fleet sustain + colony supply + construction)

**How it works:**
- Fleets consume fuel and supplies passively. Standing army costs money.
- Colonies need food and supplies to grow. Growth unlocks production buildings.
- Three independent demand systems create cascading pressure: military (fuel/ammo), civilian (food), industrial (supplies/metals).
- Wars disrupt colony supply lines. Price spikes follow territory changes.

**What makes it feel alive:**
- Sustain enforcement creates constant pressure (you can't sit idle)
- Wars have economic consequences visible in prices within 1-2 turns
- Scaling is automatic: bigger army → more fuel consumption → harder to stay neutral
- Colonies are visual feedback on war impact (they grow or shrink based on supply pressure)

**Your equivalent strength**: Your module sustain system (designed in faction_equipment_and_research_v0.md) mirrors Starsector's structure exactly. T1 → T2 → T3 escalation creates proportional supply chain burden.

**Your equivalent weakness**: Sustain is NOT enforced yet. Ships don't consume fuel. Modules don't require goods. The economic pressure exists in design docs, not in the simulation.

---

### Patrician / Port Royale (~15 wares, 2-tier depth, NPC competitor simulation)

**How it works:**
- Cities are suppliers (demand goods) and consumers (need food, luxuries).
- NPCs are visible competitors. They buy low, sell high, and monopolize routes you ignore.
- Production is time-locked: craftsmen create goods slowly (days of in-game time).
- Seasons affect prices: harvest time = low food prices, winter = high prices.

**What makes it feel alive:**
- NPC monopolization creates time pressure (if you don't buy a route, they do)
- Seasonal variation prevents equilibrium (prices cycle, not stabilize)
- Visible NPCs create narrative (you recognize competitors and react emotionally)
- Production bottlenecks are temporal, not spatial (everything available, just late)

**Your equivalent strength**: NPC traders are simulated (though not visible in UI yet). Hauler/Trader/Patrol roles create a small ecosystem.

**Your equivalent weakness**: NPCs are not visible as competitors. You see price movement but not the actor causing it. Also, no seasonal variation (Starsector equivalent would be warfront intensity cycles).

---

### Stellaris (~40 resources, 2-tier consumption, planetary jobs + districts)

**How it works:**
- Pops are job-assigned. Jobs produce goods (minerals, energy, research).
- Resources scale with empire size: bigger empire = proportionally more pop = proportionally more consumption.
- Trade routes are permanent, player-constructed (not NPC-autonomous).
- Wars disrupt resource trade and force scaling down.

**What makes it feel alive:**
- Scaling pressure: bigger empire = harder to balance supply
- Trade routes are strategic assets (blockading them is a war goal)
- Job reassignment creates dynamic supply (reassign pops from food to ammo during war)

**Your equivalent strength**: Empire scaling (more ships = more sustain = more supply chain burden) is designed the same way.

**Your equivalent weakness**: Player must manually construct and manage everything. No autonomous NPC factories self-organizing. STE intentionally delegates this to NPC systems (better for a space trader game than a 4X).

---

## Part 3: Best Practices for Your Design

### Principle 1: Instantiate All Production Recipes (Phase 2 Critical)

**Current state**: Only 3 recipes deployed as industry sites (ExtractOre 50%, RefineMetal 50%, ManufactureMunitions 14%).

**Why it matters**: Without all 9 recipes, supply chains cannot form. You can buy components, but nowhere produces them at scale. This breaks the whole economy.

**The six missing recipes**:
1. **ProcessFood** (Organics+Fuel → Food) — seed at agri nodes (40%)
2. **FabricateComposites** (Metal+Organics → Composites) — seed at mixed economies (both Ore and Organics)
3. **AssembleElectronics** (Exotic Crystals+Fuel → Electronics) — seed at fracture-border nodes only
4. **AssembleComponents** (Electronics+Metal → Components) — seed at industrial hubs (Ore-rich, no Organics)
5. **SalvageToMetal** (Salvaged Tech → Metal) — seed randomly (discovery sites)
6. **SalvageToComponents** (Salvaged Tech+Electronics → Components) — seed randomly

**Deployment strategy** (from trade_goods_v0.md):
- **ProcessFood**: ~40% of agri nodes (where Organics exist)
- **FabricateComposites**: ~10-15% of all nodes (requires both Ore and Organics nearby or imported)
- **AssembleElectronics**: Fracture-border stations only (~5% of galaxy). This is the geographic constraint that makes Electronics valuable.
- **AssembleComponents**: ~8-10% of nodes (collocate with Metal refineries for proximity advantage)
- **SalvageToMetal**: ~5% of nodes (randomly placed, only active when salvaged_tech exists locally)
- **SalvageToComponents**: ~5% of nodes (same)

**Implementation pattern** (from MarketInitGen.cs):
```csharp
// Example: Composites fabrication
if (i % CatalogTweaksV0.CompositesNodeModulus == CatalogTweaksV0.CompositesNodeOffset)
{
    state.IndustrySites[$"compfab_{i}"] = new IndustrySite
    {
        Id = $"compfab_{i}",
        NodeId = node.Id,
        RecipeId = WellKnownRecipeIds.FabricateComposites,
        Inputs = new Dictionary<string, int>
        {
            { WellKnownGoodIds.Metal, 3 },
            { WellKnownGoodIds.Organics, 2 }
        },
        Outputs = new Dictionary<string, int>
        { { WellKnownGoodIds.Composites, 2 } },
        BufferDays = 2,
        DegradePerDayBps = 500
    };
}
```

This pattern already exists for Munitions. Replicate it for the remaining 6 recipes, with deployment percentages tuned in CatalogTweaksV0.cs.

---

### Principle 2: Lock Module Sustain Enforcement to Production Chains

**Current state**: Module sustain goods are defined in faction_equipment_and_research_v0.md, but enforcement is not active. Ships have modules, but modules consume nothing.

**Why it matters**: Without sustain enforcement, goods have no weight. The economy is decoration.

**The cascade**:
1. Player equips T2 modules (armor, shields, weapons)
2. Modules consume Composites/Rare Metals every 60 ticks
3. To sustain, player must either:
   - Trade for Composites/Rare Metals (expensive), or
   - Build a supply chain (requires Agri node access + industry investment)
4. When warfront embargoes Organics, player's armor sustain breaks
5. Player must either:
   - Reroute supply through Fracture (costs Trace), or
   - Strip T2 modules and revert to T1 (loses combat effectiveness), or
   - Join the embargo-free faction (costs neutrality freedom)

This creates the "shrinking middle" dynamic from dynamic_tension_v0.md Pillar 4.

**Implementation**: See SimBridge.Maintenance.cs and MaintenanceSystem.cs. The skeleton exists; turn on enforcement in MaintenanceSystem.Process().

---

### Principle 3: Create Temporal Asymmetry (Seasons/Cycles)

**Current state**: Markets are stable over time. Prices oscillate via supply/demand but don't have seasonal spikes.

**Why it matters**: Temporal asymmetry breaks price equilibrium. If Fuel is always 50 credits, arbitrage collapses. If Fuel is 30 credits in summer (wells overflow) and 80 credits in winter (extraction slows), traders hoard and prices oscillate.

**Your option**: Warfront intensity cycles (already designed in dynamic_tension_v0.md).

Warfronts are not static. They ebb and flow:
- **Peace phase**: Warfront intensity decays. Tariffs drop. Margins on war goods collapse. Traders retract.
- **Escalation phase**: Warfront intensity rises. Munitions spike 4x. Composites spike 2.5x. Fuel spikes 3x. Player profits spike.
- **Ceasefire phase**: Cumulative supply deliveries reduce intensity by 1. This is player-driven (the player can broker peace by supplying war goods).

**Implementation**: Warfront state is in WarfrontState.Intensity (enum: Peace/Skirmish/OpenWar/TotalWar). Spawn a system tick that either escalates or de-escalates intensity based on cumulative supply deliveries (already in WarfrontDemandSystem.CheckSupplyShift()).

Modification: Instead of one shift per threshold, make intensity oscillate naturally:
- At peace, intensity drifts downward (factions cool off)
- At active war, intensity can spike if supply is disrupted
- Player can stabilize at mid-intensity by maintaining supply routes

This creates the "seasonal" pressure without literal seasons.

---

### Principle 4: Make Scarcity Visible and Recoverable

**Current state**: Markets show inventory, but scarcity is not highlighted. Player sees "Composites: 45 units" but doesn't know if that's abundance or starvation.

**Why it matters**: Visibility lets players make strategic decisions. Invisibility makes the economy feel random.

**From Starsector**: When a planet is undersupplied, the UI shows a warning ("Food Shortage") and prices spike. Player knows they can profit by supplying food or that they need to redirect supply away from military.

**Your implementation**:
1. Define "ideal stock" for each good at each market: `ideal = average_demand_per_tick * buffer_days * 1440 ticks`
2. In MarketSystem, compute a scarcity index: `scarcity = 1 - (current_stock / ideal_stock)`
3. Price modifier scales with scarcity: price rises 5% per 10% scarcity above ideal
4. In SimBridge.Reports, expose a "Market Alerts" snapshot: goods currently in shortage, which production chains are struggling, etc.
5. In UI, show shortage warnings for goods approaching zero (scarcity > 80%)

This is already partially implemented in MarketSystem (supply/demand price modifiers). Expose it in the bridge.

---

### Principle 5: Prevent Soft Equilibrium via Cost of Holding

**Current state**: NPCs and the player can sit on goods indefinitely. Munitions can be stored forever.

**Why it matters**: Static storage kills scarcity. If traders can buy cheap and hold forever, all price spikes erode away.

**X4 model**: Stations have limited storage. Once full, production stops. Forces movement.

**Starsector model**: Storage requires upkeep. Fleet supply pools decay over time.

**Your option**: Inventory ledger (already designed in trade_goods_v0.md Open Questions).

Add an optional "spoilage" system:
- **Food**: Decays 1-2% per 100 ticks (spoilage). Incentivizes quick turnover.
- **Organics**: Decays 0.5% per 100 ticks (oxidation).
- **Other goods**: No decay (they're stable: metal, ammo, fuel, etc.)

With decay, traders must move goods or lose them. This prevents indefinite hoarding and keeps margins alive. Tighten sufficiently and Food becomes genuinely urgent to supply.

**Phase 2 or 3 addition**: Not critical to launch, but high-impact for economy liveliness.

---

### Principle 6: Amplify Warfront Consequence via Embargo + Tariff Compounding

**Current state**: Warfronts drain goods and spike prices. Embargoes block trade. Tariffs scale with reputation.

**Why it matters**: These three mechanisms can stack to create inescapable economic pressure.

**Example scenario**:
- Warfront between Valorin (military) and Weavers (balanced) at 85% intensity (OpenWar)
- You are neutral (no faction rep)

**Consequences**:
1. Munitions drain 4x at contested nodes (WarfrontDemandSystem)
2. Metal availability tightens (upstream of Munitions production)
3. Metal → Composites bottleneck emerges (both use Metal)
4. You need Composites for T2 armor, but Organics (input) are embargoed by Weavers (ally of Valorin's enemy)
5. Workaround: Fracture trade for Organics elsewhere. But Trace accumulates.
6. Tariff penalty for neutrality: +5% at Skirmish, +10% at OpenWar, +15% at TotalWar

At OpenWar, you're paying 10% tariff on everything neutral-faced factions trade with you. This eats margin.

**In aggregate**: Warfront pressure (good shortage) + embargo (impossible to buy) + tariff (expensive to move) + Trace cost (Fracture alternative) creates a *choice*: align with someone (lose freedom) or accept reduced profitability (lose margin).

This is the dynamic_tension_v0.md Pillar 4 design. It works if all three are active simultaneously.

---

## Part 4: Avoiding Static Equilibrium — Concrete Mechanics

### Anti-Pattern: "Everything Costs the Same"

**What causes it**: NPC traders are too efficient and have unlimited capital. They arbitrage all margins away within 1-2 cycles.

**Why your design resists this**:
1. **NPC traders have limited range** (1-2 hops, not full graph search)
2. **NPC traders have limited cargo** (10 units per Trader, 30 per Hauler, not 1000)
3. **Warfront demand is deterministic and severe** (Munitions 4x is more drain than NPCs can compensate)
4. **Geography is not uniform** (Organics at only 40% of nodes — real scarcity)

**If price equilibrium still emerges**:
- Reduce NPC trader eval frequency (from every 15 ticks to every 30)
- Reduce NPC cargo capacity (from 10 to 5 units per Trader)
- Increase warfront demand multipliers (from 400% Munitions to 500%)
- Add spoilage to Food (forces turnover)

Tune conservatively. If you over-correct, margins spike and the economy becomes too easy again.

---

### Anti-Pattern: "Wars Don't Matter"

**What causes it**: Warfront demand is small relative to total galaxy supply.

**Example**: If there are 200 markets with ~50 Munitions each = 10,000 total supply, and warfront drains 20/tick, 500 ticks to empty one region. That's imperceptible.

**Why your design resists this**:
1. **Embargo blocks entire supply chains** (not just prices, but availability)
2. **Warfront is multi-good** (Munitions, Composites, Fuel all spike together)
3. **Warfront tariffs escalate** (add 5-15% on neutral trades)

**If wars still feel static**:
- Increase warfront drain multipliers
- Reduce initial Munitions stock at world generation (start smaller, make wars scarcer faster)
- Add secondary effects: warfront node loses tariff revenue, productivity drops, factories degrade

---

### Anti-Pattern: "Exploration Is Optional"

**What causes it**: Exotic Matter is only needed for T3 modules, which are end-game. Early-game player never feels pressure.

**Why your design resists this**:
1. **Haven upgrades consume Exotic Matter** (passive income, competes with T3 sustain)
2. **Exotic Crystals gates Electronics production** (fracture-only input, creates exploration revenue)
3. **Fracture provides escape valve** (but costs Trace, creating a different pressure)

**If exploration still feels optional**:
- Front-load Haven unlock (make Haven available at Hour 2-3, not Hour 10)
- Add Exotic Matter sustain to mid-tier modules (T2 modules consume a tiny amount, creating baseline exploration pressure)
- Add discovery-based mission rewards (exploration unlocks rare trades)

---

## Part 5: Recommended Phase 2 Implementation Checklist

### Tier 1: Production Chain Instantiation (CRITICAL)
- [ ] Deploy ProcessFood factories at ~40% of agri nodes
- [ ] Deploy FabricateComposites factories at ~10% of all nodes
- [ ] Deploy AssembleElectronics at fracture-border nodes only
- [ ] Deploy AssembleComponents at ~8% of nodes (colocate with Metal refineries)
- [ ] Deploy SalvageToMetal at ~5% of nodes
- [ ] Deploy SalvageToComponents at ~5% of nodes
- [ ] Verify all 13 goods appear in at least one market at tick 0
- [ ] Test: Can player buy all 13 goods somewhere in the galaxy?

### Tier 2: Module Sustain Enforcement (HIGH IMPACT)
- [ ] Turn on MaintenanceSystem.ProcessFleetModuleSustain() in SimKernel.Step()
- [ ] Define sustain costs per T1/T2/T3 module (see faction_equipment_and_research_v0.md Part 14)
- [ ] Implement sustain failure behavior (degrade to 50% effectiveness at 60-tick warning)
- [ ] Test: Equip a T2 module, run until sustain fails, confirm degradation
- [ ] Test: Build supply chain to sustain T2 module, confirm failure clears

### Tier 3: Price Base Population (HIGH CONFIDENCE)
- [ ] Populate GoodDefV0.BasePrice and GoodDefV0.PriceSpread in ContentRegistryLoader.cs per trade_goods_v0.md
- [ ] Verify all prices in MarketSystem use BasePrice as anchor (not hardcoded)
- [ ] Test: Price bands match expected (Low 50-100, Mid 150-300, High 400-800, Very High 1000-2000)

### Tier 4: Scarcity Visibility (NICE TO HAVE)
- [ ] Compute ideal_stock for each good per market
- [ ] Expose scarcity_index in SimBridge.Reports
- [ ] Add "Market Alerts" snapshot (goods in shortage)
- [ ] Test: Run 1000 ticks, verify alerts trigger at expected scarcity thresholds

### Tier 5: Production Site Balancing (TUNING)
- [ ] Run deterministic simulations (1-10 seed, 2000 ticks each)
- [ ] Measure: Average price per good, price volatility, per-good availability
- [ ] Identify bottlenecks (e.g., Composites always scarce, Munitions always abundant)
- [ ] Adjust production site deployment percentages and/or input/output ratios

### Tier 6: NPC Trade Tuning (OPTIONAL)
- [ ] Reduce NPC trader eval frequency to 30 ticks (from 15) to reduce price convergence
- [ ] Reduce Trader cargo capacity to 5 units (from 10)
- [ ] Measure impact on player margin opportunities
- [ ] If margins too thin, reduce further or add NPC fleet count reduction

---

## Part 6: Design Metrics for Validation

Once Phase 2 is implemented, measure the economy against these criteria:

| Metric | Target | How to Measure |
|--------|--------|----------------|
| **Price volatility** | Goods vary 20-40% above/below baseline within 500 ticks | Compute StdDev(price_history) / BasePrice for each good |
| **Margin availability** | At least 3-5 profitable routes at any tick for the player | Simulate best-profit-route search across galaxy, measure max margin |
| **Supply chain breakage** | Production chains break at least once per 1000 ticks at some node | Measure "total production output dropped below 50% normal" per recipe per region |
| **Warfront impact** | War goods (Munitions, Composites) spike 2.5-4x at contested nodes | Measure price at contested vs peaceful nodes |
| **NPC trade realism** | NPCs execute ~10-20 trades per 500 ticks (they move goods continuously) | Count NpcTradeSystem executions |
| **Geographic differentiation** | Agri nodes export Organics, Industrial nodes export Metal/Munitions | Measure net flow of goods by region type |
| **Scarcity creation** | At least 1 good falls below 50% ideal stock once per 500 ticks | Measure scarcity_index > 50% for any good |

---

## Part 7: Cross-Reference to Your Existing Design

All recommendations above are **consistent with** your existing docs:

| Recommendation | Source Doc | Alignment |
|---|---|---|
| Deploy all 9 recipes | trade_goods_v0.md Phase 2 | Exact |
| Sustain enforcement creates pressure | dynamic_tension_v0.md Pillar 2 | Exact |
| Geographic scarcity drives trade | trade_goods_v0.md Design Pillars #5 | Exact |
| Warfront embargo + tariff compound | dynamic_tension_v0.md Pillar 3 | Exact |
| Electronics gates on Exotic Crystals | trade_goods_v0.md Electronics Chain | Exact |
| Three branches from Metal | trade_goods_v0.md Module Sustain | Exact |
| Margin opportunities via arbitrage | trade_goods_v0.md Margin Opportunities table | Exact |

No recommendations contradict existing design. They are all implementations of intent already documented.

---

## Part 8: What NOT to Do (Anti-Patterns from Failed Economies)

### ❌ Don't Add Phantom Demands
If you add a good, give it 2+ real, implemented demand sources **from day 1**. Medical Supplies was cut from v0 because crew health and station population growth don't exist yet. Don't add goods expecting systems to follow.

### ❌ Don't Let Static Goods Sit Idle
Every production recipe must be instantiated by Phase 2. If ProcessFood is defined but no factories produce it, Food is phantom. Cut the recipe or deploy the site. No middle ground.

### ❌ Don't Over-Centralize Production
If all Components are produced at one location, a single embargo breaks T2 armor sustain galaxy-wide. Deploy at least 3 of every recipe. Your current design does this correctly (factories distributed by modulus).

### ❌ Don't Make NPC Trade Omniscient
NPCs searching the entire graph for best prices is unrealistic and kills margins. Limit search to 1-2 hops. This is already implemented; don't expand it.

### ❌ Don't Enforce Sustain Before Presenting Supply Chains
If you turn on module sustain enforcement before all 9 recipes are deployed, players will be unable to sustain T2 modules (no Composites source). Deploy production sites first, enforce sustain second. Sequential order matters.

### ❌ Don't Add Decay Without Playtesting
Spoilage systems (Food rots, Organics oxidize) are high-impact. Test first in sandbox, tune decay rates to be "noticeable, not punishing." Wrong tuning kills player engagement.

---

## Conclusion

Your economy has the **structure** of a best-in-class simulation. What's missing is **completeness**: all recipes must be deployed, all mechanics must be enforced, all feedback loops must be live.

Phase 2 work is largely mechanical (copy the MarketInitGen pattern 6 times for the remaining recipes) and straightforward. After Phase 2, you'll have an economy with:

1. **Supply fragmentation** — production chains break at multiple points
2. **Demand diversity** — each good has 2+ independent pressure sources
3. **Feedback loops** — player success → price drops → scarcity emerges elsewhere
4. **Temporal pressure** — warfronts cycle, creating seasonal-like variation
5. **Geographic meaning** — trade routes emerge from scarcity, not decoration

This is the X4/Starsector/Patrician level of economy simulation in a game that respects the player's time (no spreadsheet required, all visible in UI).

---

## Appendix A: Production Chain Deployment Reference

For each recipe, the recommendation is to deploy at:
- `isFactionBase` (some % derived from geography or modulus), OR
- `isNodeArchetype` (Agri nodes, Mining hubs, Fracture-border, etc.)

| Recipe | Deployment Condition | Estimated % of Galaxy | Justification |
|--------|-----|--------|---|
| ExtractOre | i % 2 == 0 (even nodes) | 50% | Currently deployed |
| RefineMetal | i % 2 == 1 (odd nodes) | 50% | Currently deployed |
| ManufactureMunitions | i % CatalogTweaksV0.MunitionsNodeModulus == CatalogTweaksV0.MunitionsNodeOffset | ~14% | Currently deployed |
| ProcessFood | hasOrganics && i % 2 == 1 (agri + odd) | ~20% | Odd nodes avoid mine overlap; hasOrganics gates to agri regions |
| FabricateComposites | i % CatalogTweaksV0.CompositesNodeModulus == CatalogTweaksV0.CompositesNodeOffset | ~8-10% | Requires both Ore (mining) and Organics (agri) sources |
| AssembleElectronics | (geoHash < 15% Rare Metals range) OR fractureBorder | ~3-5% | Fracture-border only; scarcity is intentional (gates Electronics) |
| AssembleComponents | i % CatalogTweaksV0.ComponentsNodeModulus == CatalogTweaksV0.ComponentsNodeOffset | ~8-10% | Colocate with Metal refineries for proximity advantage |
| SalvageToMetal | (seed-based random) && i % CatalogTweaksV0.SalvageNodeModulus == CatalogTweaksV0.SalvageNodeOffset | ~5% | Random; only active when salvaged_tech exists locally |
| SalvageToComponents | (seed-based random) && i % CatalogTweaksV0.SalvageComponentsNodeModulus == CatalogTweaksV0.SalvageComponentsNodeOffset | ~5% | Random; only active when salvaged_tech exists locally |

All percentages are tuning parameters. Adjust via CatalogTweaksV0.cs per playtest feedback.

---

## Appendix B: Module Sustain Resource Mapping (from faction_equipment_and_research_v0.md)

When sustain enforcement is active, modules will consume goods as follows. This creates the economic pressure that drives production chain instantiation.

| Module Tier | Sustain Resource | Source Chain | Why Chosen |
|-------|-----|--------|---|
| T1 Armor | Metal | Ore + Fuel | Raw, foundational. Available everywhere. |
| T1 Energy | Fuel | Wells (extraction) | Universal. Cheapest tier. |
| T1 Weapons | Munitions | Metal + Fuel | Ammo makes narrative sense. Differentiates from T2. |
| T1 Engines | Fuel | Wells | Energy for propulsion. |
| T2 Armor | Composites | Metal + Organics (processed) | Military structural material. Rare Organics drive cost. |
| T2 Shields | Composites | Metal + Organics (processed) | Composite plating. Same as armor. |
| T2 Weapons | Munitions + Rare Metals | Munitions (processed) + extraction | Precision ammo requires rare minerals. Dual sustain creates scaling pressure. |
| T2 Sensors | Rare Metals | Rare deposits (extraction) | Advanced electronics. Scarcity amplifies value. |
| T3 Relic | Exotic Matter | Discovery only (exotic) | Cannot be manufactured. Exploration-gated. |

---

## Appendix C: Testing Script Template (Phase 2 Validation)

Once all production sites are deployed, run this deterministic test to validate the economy:

```csharp
// Pseudo-code
[Test]
public void ValidateProductionChainInstantiation()
{
    // Seed 42 => deterministic galaxy
    var state = WorldLoader.CreateNewWorld(seed: 42);

    // Verify all 13 goods exist in at least one market
    var goodsFound = new HashSet<string>();
    foreach (var market in state.Markets.Values)
    {
        foreach (var goodId in market.Inventory.Keys)
        {
            goodsFound.Add(goodId);
        }
    }
    Assert.AreEqual(13, goodsFound.Count, "All 13 goods should exist at tick 0");

    // Verify all 9 recipes have at least 1 factory
    var recipesDeployed = new HashSet<string>();
    foreach (var site in state.IndustrySites.Values)
    {
        if (!string.IsNullOrEmpty(site.RecipeId))
        {
            recipesDeployed.Add(site.RecipeId);
        }
    }
    Assert.AreEqual(9, recipesDeployed.Count, "All 9 recipes should have factories");

    // Run 1000 ticks, measure price volatility
    for (int t = 0; t < 1000; t++)
    {
        state = SimKernel.Step(state);
    }

    // Verify prices vary by at least 20% from baseline
    foreach (var good in WellKnownGoodIds.AllGoods)
    {
        var prices = new List<int>();
        // ... collect price history per good ...
        var stdDev = CalculateStdDev(prices);
        var cv = stdDev / prices.Average(); // coefficient of variation
        Assert.GreaterOrEqual(cv, 0.20f, $"{good} should have >=20% price volatility");
    }
}
```

---

**End of Document**
