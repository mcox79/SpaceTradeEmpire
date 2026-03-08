# Trade Goods & Economy Design — V0

Status: DESIGN LOCKED (pending implementation)
Date: 2026-03-06

---

## Design Pillars

1. **Every good earns its slot** — 2+ demand sinks or a justified scarcity role. No filler, no "expensive thing you sell."
2. **Depth over complexity** — 13 goods, max chain depth 3. A player can hold the full economy in their head without a wiki.
3. **Sustain = supply chains** — Ship modules consume goods, not credits. Your fleet loadout is constrained by your production infrastructure.
4. **Shared bottlenecks create pressure** — Wartime and peacetime compete for the same goods. War should be felt in prices, not just on the battlefield.
5. **Geographic scarcity drives trade** — Universal goods (Fuel, Ore) bootstrap the economy. Scarce goods (Organics, Rare Metals, Exotic Crystals) create the trade routes worth running.
6. **Three branches from one bottleneck** — Metal is the industrial backbone. What you add to it (Fuel, Organics, Electronics) determines whether you build weapons, armor, or automation.
7. **No phantom demands** — Every listed sink must correspond to an implemented or already-planned system. "Station growth" or "crew morale" don't count until they exist.

---

## The 13 Goods

### Tier 1 — Extraction

Harvested directly from the environment. No recipe. Geographic distribution determines trade routes.

| ID | Display Name | Source | Geography | Price Band | Role |
|----|-------------|--------|-----------|------------|------|
| `fuel` | Fuel Cells | Fuel wells | Universal (every region has wells) | Low | Universal energy input. Consumed by nearly every recipe and T1 energy module sustain. The oxygen of the economy. |
| `ore` | Raw Ore | Mining sites | Universal (most systems have deposits) | Low | Mineral feedstock for Metal. High volume, low margin. The foundation of the industrial chain. |
| `organics` | Organics | Agri-nodes, bioform harvests | Geographic (agri-systems only, ~40% of nodes) | Low | Biological feedstock. Feeds Food AND Composites — the core "butter vs guns" fork. Makes agri-systems strategically essential. |
| `rare_metals` | Rare Metals | Rare ore deposits | Scarce (~15% of nodes, clustered) | High | Advanced mineral consumed directly by T2 weapon/sensor module sustain. Think tungsten, niobium — you don't refine it, you use it. Creates dedicated "rare ore" trade routes. |

### Tier 2 — Processed

Manufactured at stations from extraction goods. Each requires a specific recipe and production time.

| ID | Display Name | Recipe | Ticks | Price Band | Role |
|----|-------------|--------|-------|------------|------|
| `metal` | Refined Metal | Ore + Fuel | 20 | Mid | Industrial backbone. Input to Munitions, Composites, AND Components. The most-moved good by volume. 5 demand sinks. |
| `food` | Food Rations | Organics + Fuel | 15 | Low | Crew and station sustain. Universal consumption — every populated node eats Food every cycle. The one good that's always in demand everywhere. |
| `composites` | Composites | Metal + Organics | 30 | Mid | Advanced structural material. T2 module sustain (armor, shields, ECM). Warfront premium supply. Requires both mining AND agricultural inputs — forces cross-lane trade. |
| `electronics` | Electronics | Exotic Crystals + Fuel | 25 | Mid-High | Tech intermediate. The only path to Components. Supply-constrained by Exotic Crystals (fracture-only), making it inherently valuable. Creates processing hubs near fracture space. |
| `munitions` | Munitions | Metal + Fuel | 15 | Mid | Ordnance. Consumed by ALL weapon module sustain AND warfront bulk supply. The "Metal for fighting" split — Metal builds, Munitions destroys. Any industrial system can produce it. |

### Tier 2.5 — Manufactured

Highest-tier craftable goods. Assembled from processed inputs at specialized stations.

| ID | Display Name | Recipe | Ticks | Price Band | Role |
|----|-------------|--------|-------|------------|------|
| `components` | Components | Electronics + Metal | 30 | High | The universal tech-economy sink. Consumed by automation programs, ship refits, and fleet upkeep. As your empire grows, Component demand scales proportionally. You can never have enough. |

### Tier 3 — Exotic

Cannot be manufactured. Acquired only through exploration or fracture access. Geographic and activity-gated.

| ID | Display Name | Source | Price Band | Role |
|----|-------------|--------|------------|------|
| `exotic_crystals` | Exotic Crystals | Fracture node extraction | High | The only input to Electronics. Fracture-exclusive — the reason players venture into dangerous space. Geographic scarcity engine for the entire tech chain. |
| `salvaged_tech` | Salvaged Technology | Derelict discovery reward | High | Recyclable loot. Player chooses conversion path: → Metal (bulk, safe) or → Components (valuable, needs Electronics). The exploration-to-economy bridge. |
| `exotic_matter` | Exotic Matter | Anomaly/ruin discovery ONLY | Very High | Cannot be manufactured, ever. Sustains ALL T3 Precursor modules. Also consumed by research programs. A Dreadnought with 3 Precursor modules eats 4-6 Exotic Matter per cycle — your endgame ship literally runs on alien artifacts you have to keep finding. |

---

## Goods NOT Included (Intentional Cuts)

| Rejected Good | Why It Was Cut |
|---------------|----------------|
| **Polymers** | Middleman. Was Organics + Fuel → Polymers → Composites. The player decision is "Food or Composites?" — Polymers added a manufacturing step without adding a decision. Cut the middleman, connect Organics → Composites directly. |
| **Hull Plating** | "Metal but one step later." Module doc T1 armor sustains from Metal directly. Ship repair uses Metal. Single-input, single-output good with no fork = manufacturing busywork. |
| **Medical Supplies** | Two phantom demands: crew health system and station population growth. Neither system exists or is planned for near-term. If crew health is added later, Medical Supplies is the strongest candidate for good #14. |
| **Water Ice** | "Second Fuel with geography." Organics already fills the geographic-scarcity-at-extraction-tier role better because it enables an entire parallel production lane, not just one recipe input. |
| **Luxury Goods** | Needs a population happiness system. "Expensive thing you sell" isn't a role, it's filler. |
| **Weapons / Arms** | Needs faction legality system. Munitions handles the military supply fantasy without the moral-choice scope creep. |
| **Contraband** | Needs legal/reputation system. Scope trap. |

---

## Production Chains

### Recipe Table

| Recipe ID | Display Name | Ticks | Inputs | Outputs |
|-----------|-------------|-------|--------|---------|
| `recipe_extract_ore` | Extract Ore | 10 | 1 Fuel | 5 Ore |
| `recipe_refine_metal` | Refine Metal | 20 | 10 Ore, 1 Fuel | 5 Metal |
| `recipe_process_food` | Process Food | 15 | 2 Organics, 1 Fuel | 3 Food |
| `recipe_fabricate_composites` | Fabricate Composites | 30 | 3 Metal, 2 Organics | 2 Composites |
| `recipe_assemble_electronics` | Assemble Electronics | 25 | 1 Exotic Crystals, 1 Fuel | 2 Electronics |
| `recipe_manufacture_munitions` | Manufacture Munitions | 15 | 2 Metal, 1 Fuel | 3 Munitions |
| `recipe_assemble_components` | Assemble Components | 30 | 2 Electronics, 3 Metal | 1 Components |
| `recipe_salvage_to_metal` | Salvage to Metal | 20 | 1 Salvaged Tech | 5 Metal |
| `recipe_salvage_to_components` | Salvage to Components | 30 | 1 Salvaged Tech, 1 Electronics | 2 Components |

### Chain Graph

```
EXTRACTION              PROCESSING                MANUFACTURING
──────────              ──────────                ─────────────

Fuel (universal)  ──┬──→ input to most recipes
                    │
Ore (universal)   ──┼──→ Metal ─────────┬──→ Munitions (+ Fuel)     [OFFENSIVE]
                    │                   ├──→ Composites (+ Organics) [DEFENSIVE]
                    │                   └──→ Components (+ Electronics) [ECONOMIC]
                    │
Organics (agri)   ──┼──→ Food (+ Fuel)        [CREW SUSTAIN]
                    └──→ Composites (+ Metal)  [T2 MODULE SUSTAIN]

Rare Metals (scarce) ──→ consumed directly by T2 modules


FRACTURE                 DISCOVERY
────────                 ─────────

Exotic Crystals ────→ Electronics (+ Fuel) ──→ Components (+ Metal)

                     Salvaged Tech ──→ Metal OR Components (player choice)
                     Exotic Matter ──→ T3 module sustain + Research
```

### Three Branches from Metal

The core economic fork. Metal is the universal industrial input. The second ingredient determines the product category:

| Branch | Recipe | Product | Role |
|--------|--------|---------|------|
| **Offensive** | Metal + Fuel → Munitions | Weapons + warfront | Fighting |
| **Defensive** | Metal + Organics → Composites | Armor + shields | Surviving |
| **Economic** | Metal + Electronics → Components | Automation + refits | Growing |

This creates a single decision point with three meaningful outcomes. Maximum depth, minimum complexity.

### The Organics Fork

The "butter vs guns" decision at agri-nodes:

| Path | Recipe | What It Sustains |
|------|--------|-----------------|
| **Butter** | Organics + Fuel → Food | Keeps your people alive |
| **Guns** | Organics + Metal → Composites | Keeps your fleet armored |

Agri-systems must choose how to allocate their Organics supply. Neither path is wrong — the choice depends on whether the player needs crew sustain or military readiness.

---

## Module Sustain Alignment

Every ship module consumes specific goods per sustain cycle (default 60 ticks). The 13 trade goods include all 6 module sustain resources.

### Sustain Resource Mapping

| Sustain Resource | Trade Good | Source Chain | Module Tier |
|-----------------|-----------|-------------|-------------|
| Fuel | Fuel | Wells (extraction) | T1 energy weapons, engines, power, shields |
| Metal | Metal | Ore + Fuel (processed) | T1 armor, structural repair |
| Munitions | Munitions | Metal + Fuel (processed) | T1-T2 all weapons (kinetic, missile, energy sidearms) |
| Composites | Composites | Metal + Organics (processed) | T2 armor, shields, ECM, EW |
| Rare Metals | Rare Metals | Rare deposits (extraction) | T2 precision weapons, sensors, advanced engines |
| Exotic Matter | Exotic Matter | Discovery only (exotic) | T3 Precursor modules (all) |

### Updated Module Sustain Recipes

Weapon modules now consume Munitions instead of raw Metal (matching the Metal-for-building / Munitions-for-fighting split):

| Module | Tier | Original Sustain | Updated Sustain |
|--------|------|-----------------|-----------------|
| Coilgun Turret | T1 | 1 metal | 1 munitions |
| PDC Array | T1 | 1 metal | 1 munitions |
| Missile Pod | T1 | 1 metal, 1 fuel | 1 munitions, 1 fuel |
| Pulse Laser | T1 | 1 fuel | 1 fuel (unchanged — pure energy weapon) |
| Railgun | T2 | 2 metal, 1 composite | 2 munitions, 1 composites |
| Torpedo Launcher | T2 | 2 metal, 1 rare metal | 2 munitions, 1 rare metals |
| Swarm Battery | T2 | 2 metal, 2 fuel | 2 munitions, 2 fuel |
| Casaba Lance | T2 | 2 composite, 1 rare metal | 2 composites, 1 rare metals (unchanged — shaped nuclear, not conventional ammo) |
| Plasma Carronade | T2 | 2 fuel, 1 composite | 2 fuel, 1 composites (unchanged — plasma containment) |
| FEL / Particle Beam | T2 | 2 fuel, 1 rare metal | 2 fuel, 1 rare metals (unchanged — pure energy) |

Non-weapon modules are unchanged — armor sustains from Metal/Composites, engines from Fuel, utilities from various.

### Sustain Escalation by Tech Tier

| Fleet Loadout | Sustain Needs | Supply Chain Required |
|--------------|--------------|----------------------|
| T1 (starter) | Metal, Fuel, Munitions | One ore mine + one fuel well. Easy. |
| T2 (military) | + Composites, Rare Metals | Bio-supply chain (Organics) + rare deposit access. Real empire required. |
| T3 (Precursor) | + Exotic Matter | Steady exploration of Precursor ruins. Dreadnought literally needs you to keep finding alien artifacts. |

---

## Warfront Supply

The warfront mechanic consumes goods at three supply tiers. During wartime, these goods compete with module sustain for the same supply chains — creating economic pressure felt across the entire empire.

### Supply Tiers

| Tier | Good | Recipe | Who Produces It | Warfront Role |
|------|------|--------|----------------|---------------|
| **Bulk** | Munitions | Metal + Fuel | Any industrial system | High volume. Keeps the front fighting. "Beans and bullets." |
| **Premium** | Composites | Metal + Organics | Systems with mining + agriculture | Medium volume. Armor and shields for the front line. |
| **Elite** | Rare Metals | Rare deposits (extraction) | Only systems with deposits | Low volume. T2 weapons for front-line fleets. Strategically critical. |

### Wartime Economic Pressure

During wartime, the front consumes Munitions, Composites, and Rare Metals at elevated rates. This creates cascading economic effects:

- **Munitions spike** → Metal and Fuel prices rise → Component factories compete for Metal → automation programs degrade
- **Composites spike** → Organics demand rises → Food production competes → crew sustain pressure
- **Rare Metals spike** → T2 weapon sustain competes with warfront → fleet readiness degrades

The warfront doesn't need its own isolated economy. It creates pressure on the SAME supply chains the player's empire depends on. This is intentional — war should be felt in prices, not just on the battlefield.

### Player Trade Fantasy

- **War profiteer**: Run Munitions to the front at 3x peacetime prices
- **Humanitarian**: Ship Food to war-disrupted systems where supply chains collapsed
- **Strategist**: Secure Rare Metal deposits before the enemy does
- **Industrialist**: Ramp up Composites production to supply both your fleet and the front

---

## Geographic Distribution

Trade routes emerge from geographic scarcity. Each extraction good has a different distribution pattern.

| Extraction Good | Distribution | Design Intent |
|----------------|-------------|---------------|
| Fuel | Universal — every region has wells | Bootstrap good. No system should starve for energy. |
| Ore | Universal — most systems have deposits | Foundation good. Metal is always available but may require local refining. |
| Organics | Geographic — ~40% of nodes (agri-systems) | Creates agri/industrial split. Industrial systems must import Organics for Composites and Food. |
| Rare Metals | Scarce — ~15% of nodes, clustered | Creates "rare ore" trade routes. Systems with deposits become strategic targets in wartime. |
| Exotic Crystals | Fracture-only | The incentive to venture into dangerous fracture space. |

### System Archetypes (Emergent)

No system is explicitly typed — archetypes emerge from which extraction goods are locally available:

| Archetype | Local Resources | Imports | Exports | Identity |
|-----------|----------------|---------|---------|----------|
| Industrial Hub | Ore, Fuel | Organics | Metal, Munitions | Factory system. Builds everything kinetic. |
| Agri World | Organics, Fuel | Ore (for Metal) | Food, Organics | Breadbasket. Essential for Composites production. |
| Mixed Economy | Ore, Organics, Fuel | Rare Metals | Composites, Food, Metal | Self-sufficient in basics. Ideal Composites manufacturing. |
| Rare Deposit | Ore, Fuel, Rare Metals | Organics | Rare Metals, Metal | Strategically critical. Wartime target. |
| Fracture Border | Fuel, Exotic Crystals access | Everything else | Electronics | Dangerous but lucrative. Processing hub for the tech chain. |
| Frontier | Fuel only | Everything | Nothing (yet) | Expansion opportunity. Needs investment to become productive. |

---

## Demand Audit

Every good verified against the "2+ demands or justified" rule.

| Good | Demand Sources | Count | Status |
|------|---------------|-------|--------|
| Fuel | Metal refining, Food processing, Electronics assembly, Munitions fabrication, T1 module sustain | 5+ | Strong |
| Ore | Metal refining | 1 | Justified — high-throughput raw material |
| Organics | Food processing, Composites fabrication | 2 | Strong — the core "butter vs guns" fork |
| Rare Metals | T2 module sustain (weapons, sensors), warfront elite supply | 2 | Strong — scarcity amplifies value |
| Metal | Munitions, Composites, Components, T1 armor sustain, ship repair | 5 | Strongest — universal industrial bottleneck |
| Food | Station consumption, fleet crew sustain | Universal | Strong — always in demand everywhere |
| Composites | T2 module sustain (armor, shields, ECM), warfront premium | 2+ | Strong |
| Electronics | Components manufacturing | 1 | Justified — high-throughput pass-through, supply-constrained by Exotic Crystals |
| Munitions | Weapon module sustain (all weapons), warfront bulk | 2 | Strong |
| Components | Automation sustain, ship refit sustain, fleet upkeep | 3 | Strong — universal tech-economy sink |
| Exotic Crystals | Electronics manufacturing | 1 | Justified — geographic scarcity driver (fracture-only) |
| Salvaged Tech | → Metal conversion, → Components conversion | 2 paths | Strong — player-choice recycling |
| Exotic Matter | T3 Precursor module sustain, research program sustain | 2 | Strong — exploration-gated, cannot be manufactured |

Zero phantom demands. Every sink is an implemented or already-planned system.

---

## Price Band & Market Mechanics

### Base Price Bands

| Band | Base Price (credits) | Typical Goods | Spread |
|------|---------------------|---------------|--------|
| Low | 50–100 | Fuel, Ore, Organics, Food | 20–30% |
| Mid | 150–300 | Metal, Composites, Munitions, Electronics | 30–50% |
| High | 400–800 | Components, Rare Metals, Exotic Crystals, Salvaged Tech | 40–60% |
| Very High | 1000–2000 | Exotic Matter | 50–80% |

### Supply/Demand Price Modifiers

Prices float based on local stock vs ideal stock. This is the existing Market system — unchanged.

- Stock below ideal → price rises (scarcity premium)
- Stock above ideal → price falls (surplus discount)
- Wartime demand multiplier on military goods (Munitions, Composites, Rare Metals)

### Margin Opportunities

The best trade margins emerge from geographic arbitrage:

| Route | Buy Cheap At | Sell Dear At | Why |
|-------|-------------|-------------|-----|
| Organics | Agri worlds (surplus) | Industrial hubs (need for Composites) | Industrial systems can't grow Organics |
| Rare Metals | Rare deposit systems | Military staging areas | Warfront consumes at premium |
| Electronics | Fracture-border processors | Deep lane systems | Long haul from fracture source to interior demand |
| Food | Agri worlds | Frontier / war zones | Disrupted supply chains spike prices |
| Munitions | Industrial hubs | Warfront systems | War profiteering — high risk, high reward |

---

## Progression Curve

The economy reveals itself gradually as the player expands.

### Early Game (Starter System)

**Goods encountered:** Fuel, Ore, Metal, Food, Munitions
**Activity:** Local trade loops. Haul Ore to refineries, sell Metal. Buy Food. Basic weapon sustain from Munitions.
**Decision complexity:** Low. "Buy low, sell high" within one system cluster.

### Mid Game (Multi-System)

**Goods encountered:** + Organics, Composites, Electronics, Components, Rare Metals
**Activity:** Cross-system trade routes. Set up production chains. First automation programs consuming Components. T2 module upgrades requiring Composites + Rare Metals.
**Decision complexity:** Medium. "Where do I build my Composites factory? Near Organics or near Metal?" Butter vs guns at agri-nodes.

### Late Game (Empire)

**Goods encountered:** + Exotic Crystals, Salvaged Tech, Exotic Matter
**Activity:** Fracture expeditions for Exotic Crystals → Electronics → Components. Discovery runs for Exotic Matter to sustain Precursor modules. Warfront supply logistics.
**Decision complexity:** High. "Do I use my Exotic Matter for the Dreadnought's Graviton Shear or invest in research? Do I divert Metal from Components to Munitions for the warfront?"

### Endgame Pressure

The sustain system creates natural scaling pressure:

| Empire Size | Approximate Sustain Drain / Cycle |
|-------------|----------------------------------|
| 1 ship, T1 loadout | ~3 Metal, ~3 Fuel, ~2 Munitions |
| 3 ships, mixed T1/T2 | ~10 Metal, ~8 Fuel, ~6 Munitions, ~4 Composites, ~2 Rare Metals |
| Fleet + automation + warfront | ~25 Metal, ~20 Fuel, ~15 Munitions, ~10 Composites, ~5 Rare Metals, ~3 Exotic Matter |

Each tier of growth demands proportionally more supply chain investment. The economy scales with the player, not ahead of them.

---

## Migration from Current System

### Goods Added (4)

| New Good | Replaces | Notes |
|----------|----------|-------|
| `organics` | — | New extraction good. Agri-node distribution. |
| `rare_metals` | — | New extraction good. Scarce deposits. |
| `composites` | `composite_armor` | Renamed and re-reciped. Was Exotic Crystals + Metal → now Metal + Organics. Broader role (T2 sustain, not just armor). |
| `munitions` | — | New processed good. Weapon sustain + warfront. |
| `exotic_matter` | `anomaly_samples` | Renamed. Same source (discovery only). Expanded role (T3 module sustain + research). |

### Goods Removed (2)

| Removed Good | Absorbed By | Notes |
|-------------|-------------|-------|
| `hull_plating` | Metal (direct use) | Ship repair uses Metal directly. Module doc T1 armor sustains from Metal. Single-input single-output manufacturing step with no decision. |
| `composite_armor` | `composites` | Renamed, re-reciped, broader role. |
| `anomaly_samples` | `exotic_matter` | Renamed to match ship_modules_v0.md terminology. |

### Recipes Removed

| Removed Recipe | Reason |
|----------------|--------|
| `recipe_forge_hull_plating` | Hull Plating cut. Repair uses Metal. |
| `recipe_forge_composite_armor` | Replaced by `recipe_fabricate_composites`. |
| `recipe_refine_ore_to_food` | Replaced by `recipe_process_food` (uses Organics, not Ore). |

### Recipes Added

| New Recipe | Inputs | Outputs |
|-----------|--------|---------|
| `recipe_process_food` | 2 Organics, 1 Fuel | 3 Food |
| `recipe_fabricate_composites` | 3 Metal, 2 Organics | 2 Composites |
| `recipe_manufacture_munitions` | 2 Metal, 1 Fuel | 3 Munitions |
| `recipe_salvage_to_components` | 1 Salvaged Tech, 1 Electronics | 2 Components |

---

## Design Sources & Rationale

### Research Basis

- **12–20 goods sweet spot**: Games praised for their economy (Foundation 13, Escape Velocity 12, Freelancer ~35 but half is filler) land in this range. Below 10 feels thin. Above 25 needs external wikis.
- **Max chain depth 3**: Colony sims work best at 1–3 steps, factory builders 1–4. Deeper chains have "massive complexity cost" (Leafwing Studios, *Production Chains*).
- **Shared bottlenecks > dedicated tokens**: Warfront consuming existing goods creates more economic pressure than isolated military tokens (Lost Garden, *Value Chains*).
- **Comprehension ≤ Tracking ≤ Depth**: Minimize the first two, maximize the third (Gamasutra, *Design 101: Complexity vs Depth*).
- **Volitional sinks**: Best sinks are investments players choose, not taxes they endure (1kx Network, *Sinks & Faucets*).
- **Single-edge goods are weak**: Goods with only one consumer should transform automatically or be cut (Leafwing Studios).

### Inspiration

- **X4: Foundations** (~35 wares): Deeply simulated economy. Overcomplex for a non-factory-builder, but the "production creates demand for other production" loop is the model.
- **Escape Velocity** (~12 goods): Elegant simplicity. Proved you don't need 50 goods for a rich trade game.
- **EVE Planetary Industry** (82 items, 5 tiers): Structure is excellent (clear tier progression), item count is absurd for a non-spreadsheet game.
- **The Expanse**: Realistic resource economics. Water, food, air as strategic resources. Composites and rare metals as military bottlenecks.
- **Starsector**: Supply/demand fleet logistics. The feeling that your fleet is an economic entity, not just a combat entity.

---

## Open Questions (Deferred)

- **NPC faction preferences**: Should certain factions pay premium for specific goods? (e.g., militarist factions value Munitions higher)
- **Contraband / smuggling**: If faction reputation is added, some goods could become illegal in certain territories. Medical Supplies is the natural candidate for "humanitarian but faction-restricted" contraband.
- **Decay / spoilage**: Should Food or Organics decay over time? Creates urgency but adds tracking complexity. Defer until playtesting reveals whether trade routes are too static.
- **Good #14 (Medical Supplies)**: If crew health/morale is added as a system, Medical Supplies (Organics + Electronics) is the strongest candidate. Two clear demands: fleet crew buff + station population growth. Adds a third Organics demand and second Electronics demand.
