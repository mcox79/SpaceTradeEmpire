# Economy Mechanics Reference — Formulas & Implementation Details

**Quick reference** for price calculation, scarcity computation, warfront demand, embargo enforcement, and NPC trade logic.

---

## Part 1: Market Price Calculation

### Base Price Model (MarketSystem.cs)

Price for a good at a market is computed as:

```
price = base_price * supply_demand_modifier * warfront_multiplier * tariff_modifier
```

Where:

#### base_price
- Comes from MarketTweaksV0.cs price bands
- Low band: 50-100 credits
- Mid band: 150-300 credits
- High band: 400-800 credits
- Very High band: 1000-2000 credits

Currently: **All goods price at Low band** (bug in Phase 1, fixed in Phase 2 via trade_goods_v0.md BasePrice population).

#### supply_demand_modifier
```csharp
// From MarketSystem.cs (existing implementation)
if (idealStock > 0)
{
    float ratio = (float)currentStock / idealStock;
    if (ratio < 0.5f)
        modifier = 2.0f; // 100% price increase at 50% stock
    else if (ratio < 1.0f)
        modifier = 0.5f + (1.5f * ratio); // linear interpolation
    else
        modifier = Math.Min(1.5f, 1.0f + (currentStock - idealStock) / (float)idealStock);
}
```

**Effect**:
- Stock = 25% of ideal → price = 2.0x base
- Stock = 50% of ideal → price = 1.5x base (approx)
- Stock = 100% of ideal → price = 1.0x base
- Stock = 200% of ideal → price = 0.33x base (surplus discount)

#### warfront_multiplier
```csharp
// From WarfrontDemandSystem.cs
if (goodId == WellKnownGoodIds.Munitions && wf.Intensity > WarfrontIntensity.Peace)
    multiplier = 1.0f + (((int)wf.Intensity / (int)WarfrontIntensity.TotalWar) * (WarfrontTweaksV0.MunitionsDemandMultiplierPct - 100) / 100.0f);

// Simplified: at intensity N out of 4 (TotalWar):
// multiplier = 1.0 + (N / 4) * (400% - 100%) / 100% = 1.0 + (N / 4) * 3.0
```

**Effect at contested nodes**:
- Intensity 1 (Skirmish): Munitions 1.75x, Composites 1.375x, Fuel 1.5x
- Intensity 2 (OpenWar): Munitions 2.5x, Composites 1.75x, Fuel 2.0x
- Intensity 3 (Escalation): Munitions 3.25x, Composites 2.125x, Fuel 2.5x
- Intensity 4 (TotalWar): Munitions 4.0x, Composites 2.5x, Fuel 3.0x

Only applies at contested nodes (in wf.ContestedNodeIds). Other nodes unaffected.

#### tariff_modifier
```csharp
// From ReputationSystem.cs + MarketSystem.cs
base_tariff = 0.0f; // no tariff by default

if (faction_rep_tier == RepTier.Hostile)
{
    base_tariff = 0.20f; // +20% tariff for hostile
}
else if (faction_rep_tier == RepTier.Neutral)
{
    base_tariff += warfront_intensity_surcharge;
    // surcharge = (5% per Skirmish/+10%/+15% at OpenWar/TotalWar)
}
else if (faction_rep_tier == RepTier.Allied)
{
    base_tariff = -0.15f; // -15% discount for allied
}

final_price = base_price * (1.0f + base_tariff) * supply_demand_modifier * warfront_multiplier;
```

**Example scenario**:
- Base price: 150 credits (Mid band)
- Supply: 40% of ideal → supply_demand_modifier = 2.0f
- Warfront intensity at contested node: Intensity 2 (OpenWar) → warfront_multiplier = 2.5f for Munitions
- Player reputation: Neutral → tariff = +10% (OpenWar surcharge) → tariff_modifier = 1.10f
- Final price = 150 * 1.10 * 2.0 * 2.5 = **825 credits per Munitions**

Peacetime equivalent: 150 * 1.0 * 1.0 * 1.0 = **150 credits** (5.5x cheaper!)

---

## Part 2: Scarcity Index & Ideal Stock

### Ideal Stock Calculation

```csharp
// From MarketSystem.cs (proposed Phase 2 addition)
int ideal_stock = average_demand_per_tick * buffer_days * 1440;

where:
- average_demand_per_tick = sum of all consumption sources (module sustain, warfront demand, player trades)
- buffer_days = target days of inventory (typically 1-2 days)
- 1440 = ticks per day
```

**Example: Munitions**
- T1 weapons sustain: ~3 per fleet per 60 ticks = 0.05 per tick (starter fleet)
- Warfront demand: 1 per tick at contested nodes (Intensity 1)
- Player trades: varies, assume 1 per tick average
- Total average demand: ~0.06 per tick (summed across galaxy)
- Ideal stock per market: 0.06 * 1 day * 1440 = 86.4 units

If market has 30 units, scarcity_index = 1 - (30 / 86) = 65% scarcity.

### Scarcity Thresholds (Proposed)

```csharp
// From SimBridge.Reports.cs (Phase 2 UI addition)
scarcity_index = 1.0f - (current_stock / ideal_stock);

if (scarcity_index > 0.8f) → "CRITICAL SHORTAGE" (price 4-5x base)
if (scarcity_index > 0.5f) → "MAJOR SHORTAGE" (price 2-3x base)
if (scarcity_index > 0.2f) → "MINOR SHORTAGE" (price 1.2-1.5x base)
if (scarcity_index < -0.5f) → "GLUT" (price 0.5x base or lower)
```

---

## Part 3: Warfront Demand Drain

### Consumption Model (WarfrontDemandSystem.cs)

For each contested node at intensity I (out of 4):

```csharp
consumption_per_tick = (multiplier_pct - 100) * I / (4 * 100)

For Munitions (multiplier = 400%):
  consumption = (400 - 100) * I / 400 = 0.75 * I units per tick

  Intensity 1 (Skirmish): 0.75 units/tick
  Intensity 2 (OpenWar): 1.5 units/tick
  Intensity 3 (Escalation): 2.25 units/tick
  Intensity 4 (TotalWar): 3.0 units/tick
```

**Per cycle (300 ticks = ~5 hours)**:
- Skirmish: 225 Munitions drained
- OpenWar: 450 Munitions drained
- TotalWar: 900 Munitions drained

**Per day (1440 ticks)**:
- TotalWar warfront drains 4,320 Munitions per contested node

If galaxy has 100 markets with ~86 Munitions baseline, total Munitions supply ≈ 8,600.
TotalWar warfront would drain 50% of galaxy supply in ~2 days.

---

## Part 4: Embargo Enforcement

### Supply Chain Blocking (EmbargoState.cs)

An embargo blocks trade between faction A and faction B for a specific good.

```csharp
// Check if trade is embargoed
if (embargo_active)
{
    if (seller_faction == embargo_enforcer && buyer_faction == embargo_target)
    {
        // Seller is on embargo side, buyer is target
        blocked = true;
    }
    else if (seller_faction == embargo_target && buyer_faction == embargo_enforcer)
    {
        // Target trying to trade to embargo side
        blocked = true;
    }
}
```

**Cascade effect**:
1. Warfront between Valorin (A) and Weavers (B)
2. Embargo: Valorin blocks Organics trade to Chitin (allied with Weavers)
3. Chitin can't import Organics
4. Chitin can't produce Composites (recipe: Metal + Organics)
5. Chitin's T2 module sustain breaks (armor requires Composites)
6. Chitin's fleet readiness drops (modules degrade to 50% efficiency at 60-tick warning)

---

## Part 5: NPC Trade Logic

### Profit Calculation (NpcTradeSystem.ProcessFleetTrade)

```csharp
// For each 1-2 hop away from current node:
for (each destination in range):
{
    foreach (good in cargo_types):
    {
        sell_price = destination_market.GetPrice(good);
        buy_price = current_market.GetPrice(good);
        profit_per_unit = sell_price - buy_price;
        profit_margin_pct = (sell_price - buy_price) / buy_price;

        if (profit_margin_pct >= WarfrontTweaksV0.NpcMinProfitMarginPct)
        {
            execute_trade();
        }
    }
}
```

**Current tuning**:
- NPC eval frequency: every 15 ticks (Traders) or 30 ticks (Haulers)
- Search range: 1-2 hops (not full graph)
- Cargo capacity: 10 units (Trader), 30 units (Hauler)
- Min profit margin: 15% (default)

**Effect**: NPCs stabilize prices locally (within 1-2 hops) but cannot prevent regional shocks.

---

## Part 6: Module Sustain Enforcement

### Sustain Consumption Model (Designed in faction_equipment_and_research_v0.md, Implementation TBD Phase 2)

```csharp
// Each module consumes specific goods every sustain_interval ticks (default 60)

T1 modules:
- Armor: 1 Metal per 60 ticks
- Energy weapon: 1 Fuel per 60 ticks
- Kinetic weapon: 1 Munitions per 60 ticks

T2 modules:
- Armor: 1 Composites per 60 ticks
- Precision weapon: 1 Munitions + 1 Rare Metals per 60 ticks
- Sensor: 0.5 Rare Metals per 60 ticks

T3 Relic modules:
- All: 1-3 Exotic Matter per 60 ticks (varies by module power)
```

### Sustain Failure Behavior (Designed in faction_equipment_and_research_v0.md Part 14)

```
Tick N: Resource depleted, sustain check fails
Tick N+1 to N+59: Warning toast every 10 ticks ("T2 armor sustain low, 50 ticks until degradation")
Tick N+60: Module degrades to 50% effectiveness

Degraded module behavior:
- Armor with 50% effectiveness: takes 2x damage
- Engine with 50% effectiveness: speed reduced by 50%
- Weapon with 50% effectiveness: damage reduced by 50%

Recovery: Supply the resource. On next sustain cycle, module recovers to 100%.
```

---

## Part 7: Geographic Scarcity Distribution

### Extraction Good Distribution (MarketInitGen.cs)

```csharp
// Deterministic pseudo-random based on node index
geoHash = (i * 7919 + 1301) % 100;

Fuel:
- Every 6th node has a fuel well (isFuelWell = i % 6 == 0)
- Deployment: ~17% of nodes

Ore:
- Every even node has ore (i % 2 == 0)
- Deployment: 50% of nodes

Organics:
- Nodes where geoHash < CatalogTweaksV0.OrganicsNodePct (default 40)
- Deployment: 40% of nodes

Rare Metals:
- Nodes where geoHash >= (100 - CatalogTweaksV0.RareMetalsNodePct) (default 15)
- Deployment: 15% of nodes

Exotic Crystals:
- Fracture-only (no surface wells)
- Deployment: varies per session (discovery-driven)
```

**Geographic consequences**:
- Industrial system (Ore, no Organics): must import Organics for Composites
- Agri world (Organics, no Ore): must import Metal for Composites
- Rare deposit (Ore, Rare Metals, no Organics): exports Rare Metals, imports Composites
- Fuel well (isolated): exports Fuel, imports everything else

---

## Part 8: Procedural Variation (Seed-Based)

### Warfront Placement Variability (GalaxyGenerator.SeedWarfrontsV0)

```csharp
// Same seed always produces same warfront locations
// Different seed produces different warfront locations

Example:
Seed 42: Valorin vs Weavers conflict at nodes 15-30 (eastern half)
Seed 100: Valorin vs Weavers conflict at nodes 50-65 (western half)

Player experienced outcome:
- Seed 42: Early-game Munitions pressure (conflict near start)
- Seed 100: Early-game Organics pressure (conflict at agri region)
```

**Replayability engine**: Different seeds pose different trade puzzles without meta-progression.

---

## Part 9: Testing Formulas

### Validation: Price Volatility

```
For each good G at each market M:
  price_history = [price_tick_0, price_tick_1, ..., price_tick_1000]
  mean_price = average(price_history)
  std_dev = sqrt(avg((price - mean)^2))
  coefficient_of_variation = std_dev / mean_price

Expected: CV >= 0.20 (20% volatility minimum)
Red flag: CV < 0.10 (prices too stable, equilibrium achieved)
```

### Validation: Supply Chain Breakage

```
For each recipe R:
  production_sites = count of sites with RecipeId == R
  production_at_tick_T = sum of outputs from all sites

  For T in 0..2000:
    Track production_at_tick_T
    If production drops below 50% of baseline: flag "chain broken"

Expected: At least 1 chain breaks per 1000 ticks (varies per seed)
Red flag: No chains break in 2000 ticks (demand insufficient)
```

### Validation: Warfront Impact

```
For each warfront WF:
  contested_nodes = WF.ContestedNodeIds
  peaceful_nodes = all other nodes

  For each good G in [Munitions, Composites, Fuel]:
    contested_price = avg price at contested nodes (ticks 500-1500)
    peaceful_price = avg price at peaceful nodes (ticks 500-1500)
    ratio = contested_price / peaceful_price

Expected: ratio >= 1.5 (at minimum Skirmish intensity)
At TotalWar intensity: ratio >= 3.0
```

---

## Part 10: Tuning Parameters (Master List)

All parameters live in CatalogTweaksV0.cs and related Tweaks files. Here's the master reference:

### MarketTweaksV0 (Price Bands)
```csharp
public const int LowBandMin = 50;
public const int LowBandMax = 100;
public const int MidBandMin = 150;
public const int MidBandMax = 300;
public const int HighBandMin = 400;
public const int HighBandMax = 800;
public const int VeryHighBandMin = 1000;
public const int VeryHighBandMax = 2000;
```

### WarfrontTweaksV0 (Demand)
```csharp
public const int MunitionsDemandMultiplierPct = 400; // 4x at TotalWar
public const int CompositesDemandMultiplierPct = 250; // 2.5x at TotalWar
public const int FuelDemandMultiplierPct = 300; // 3x at TotalWar
public const int DefaultDemandMultiplierPct = 100; // baseline (no mult)
public const int TotalWarIntensity = 4; // max intensity level
public const int SupplyShiftThreshold = 10000; // deliveries to reduce intensity
```

### CatalogTweaksV0 (Geographic Distribution)
```csharp
public const int OrganicsNodePct = 40; // 40% of nodes
public const int RareMetalsNodePct = 15; // 15% of nodes
public const int MunitionsNodeModulus = 7; // every 7th node
public const int MunitionsNodeOffset = 1; // offset 1 → nodes 1, 8, 15, ...
// (Phase 2) All remaining recipe deployment parameters
```

### ReputationTweaksV0 (Tariffs)
```csharp
public const int AlliedTariffBps = -1500; // -15% discount
public const int NeutralTariffBps = 0; // baseline
public const int HostileTariffBps = 2000; // +20%
public const int SkirmishNeutralSurchargeBps = 500; // +5% at Skirmish for neutral
public const int OpenWarNeutralSurchargeBps = 1000; // +10% at OpenWar
public const int TotalWarNeutralSurchargeBps = 1500; // +15% at TotalWar
```

---

## Part 11: Quick Decision Trees

### "Should I Build a Supply Chain?"

```
1. What module do I want to sustain?
   → T1 armor (requires Metal)
   → T2 armor (requires Composites = Metal + Organics)
   → T2 weapons (requires Munitions + Rare Metals)
   → T3 Relic (requires Exotic Matter)

2. What's the supply situation?
   → Can I buy locally at <2x margin cost? YES → buy
   → Can I buy from 1-2 hops away? YES → route trade
   → Is the input good embargoed? YES → must build chain or use Fracture
   → Is the input geographic? YES → requires territory control

3. Decision:
   → BUILD if: input is rare/embargoed AND I control the territory
   → TRADE if: input is common AND supply is available
   → FRACTURE if: input is embargoed AND I can afford Trace cost
   → STRIP if: sustain breaks AND I can't rebuild in time (downgrade module)
```

### "Why Did Prices Spike?"

```
1. Check warfront intensity
   → Warfront active at contested nodes? YES → warfront demand

2. Check embargo status
   → Good is embargoed? YES → supply blocked

3. Check scarcity
   → Stock < 50% ideal? YES → scarcity spike

4. Check reputation
   → My reputation is hostile? YES → tariff added

5. Conclusion:
   → Warfront spike: temporary, wait for ceasefire
   → Embargo spike: sustained, need alternate supply
   → Scarcity spike: route supply from surplus regions
   → Tariff spike: change faction rep or avoid that region
```

---

## Conclusion

The economy operates via **stacked multipliers**: base price × scarcity × warfront × tariff × embargo.

Each multiplier is:
- **Observable** (prices visible in UI)
- **Deterministic** (same seed = same prices)
- **Responsive** (player trade routes affect future prices)
- **Interconnected** (one system's output is another's input)

Phase 2 implementation will:
1. Populate BasePrice fields (fixes all goods at correct price band)
2. Deploy all 9 recipes (unlocks production chain cascades)
3. Enforce module sustain (gives goods real weight in sustain consumption)
4. Expose scarcity metrics (make pressure visible to player)

Once complete, the economy will have **emergent oscillation**: prices swing, supplies tighten, trade routes break and form, player responds, equilibrium temporarily emerges, warfront shifts, equilibrium breaks, repeat.

This is the signature of a **living economy**.

---

**End of Document**
